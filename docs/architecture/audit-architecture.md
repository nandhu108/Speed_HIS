# Audit Architecture — Prixo HIS

**Status:** design, 2026-08-13. **Phase 1 — the critical path of the whole roadmap.**

---

## 1. Problem statement

Two audit trails exist today and both work well for what they were designed for:

- `audit.audit_log` — automatic row diffs from `AuditSaveChangesInterceptor`, written in the same
  transaction as the change, changed columns only, `jsonb` old/new.
- `audit.activity_log` — ~120 explicit business/security event types via `IActivityLogger`.

Neither can answer an auditor's questions, because:

| Missing | Consequence |
|---|---|
| `PatientId` | **The audit trail cannot be filtered by patient at all** |
| `EncounterId` / type | Cannot scope to a visit or admission |
| `CorrelationId` | Cannot chain registration → order → result → bill |
| `ActorRole`, `DepartmentId` | "Which role/department did this" needs a point-in-time lookup that doesn't exist |
| `SessionId`, `DeviceId` | Partial "from where" (only `IpAddress`) |
| `Reason` | Reasons live on business rows, not audit rows |
| `ApprovalId` | Approval and approved change are unlinked |
| Semantic event type on row diffs | Only Created/Updated/Deleted |
| **Read/access logging** | 25 file-returning endpoints log nothing — "who opened this record" is unanswerable |
| DB-level immutability | No trigger, rule, or revoked grant exists; both tables are writable EF entities |

---

## 2. Design principles

1. **Additive.** The existing interceptor and activity logger keep running. `audit_event` is a new,
   richer stream beside them, not a replacement. Nothing is migrated destructively.
2. **Append-only by construction**, not by convention.
3. **Never edit, never delete.** A correction is a *new* event referencing `OriginalEventId`.
4. **Patient and encounter are first-class columns**, not values buried in JSON.
5. **Write is internal-only.** No API, no service, and no role can author an audit event directly —
   events arrive from the interceptor, the activity logger, and the event dispatcher.
6. **No PHI beyond identifiers.** Clinical narrative stays in the owning table; the event points to
   it.
7. **Reads are audited too.** Access to a patient record or document is itself an event.

---

## 3. The `audit.audit_event` model

```
audit.audit_event                            -- append-only, PARTITION BY RANGE (occurred_at)
  event_id            uuid        PK
  event_type          text        NOT NULL   -- e.g. 'LabResultApproved'
  schema_version      int         NOT NULL DEFAULT 1
  occurred_at         timestamptz NOT NULL
  recorded_at         timestamptz NOT NULL   -- when the platform stored it

  -- actor (point-in-time)
  actor_user_id       bigint      NOT NULL
  actor_username      text
  actor_role_codes    text[]                 -- roles AS AT occurred_at
  department_id       bigint
  branch_id           bigint      NOT NULL

  -- subject
  patient_id          bigint                 -- ● whenever a patient is involved
  encounter_type      text                   -- OpVisit | Admission | EmergencyVisit
  encounter_id        bigint
  entity_type         text        NOT NULL
  entity_id           text        NOT NULL
  module              text        NOT NULL   -- OPD, LAB, BILLING, ...

  -- action
  action              text        NOT NULL   -- Created|Updated|Deleted|Approved|Rejected|
                                             -- Cancelled|Accessed|Exported|Overridden|Corrected
  before_value        jsonb                  -- changed columns only
  after_value         jsonb
  reason              text                   -- ● for cancel/override/correction
  approval_id         bigint                 -- ● when approval-gated
  approver_user_id    bigint

  -- context
  correlation_id      uuid        NOT NULL
  causation_id        uuid
  session_id          uuid
  ip_address          inet
  device_id           text
  user_agent          text
  source_service      text        NOT NULL

  -- linkage (nullable; populated when relevant)
  order_id bigint · result_id bigint · bill_id bigint · payment_id bigint ·
  document_id bigint · admission_id bigint · procedure_id bigint

  -- correction chain
  original_event_id   uuid                   -- set only on corrective events
```

**Indexes**
`(patient_id, occurred_at)` · `(encounter_type, encounter_id, occurred_at)` ·
`(correlation_id)` · `(actor_user_id, occurred_at)` · `(entity_type, entity_id)` ·
`(branch_id, occurred_at)` · `(event_type, occurred_at)` · GIN on `after_value` if needed later.

**Partitioning:** monthly range partitions on `occurred_at`, created ahead by a maintenance job.
Decided at creation, not retrofitted — this table receives every change in the system.

---

## 4. Write paths

Three producers, all internal:

| Producer | Source | Enrichment |
|---|---|---|
| `AuditSaveChangesInterceptor` (existing, extended) | every `IAuditable` insert/update/soft-delete | Adds patient/encounter/correlation/session/device from an **ambient audit context** (see §4.1). Keeps writing `audit_log` unchanged for continuity. |
| `IActivityLogger` (existing, extended) | ~120 explicit business events | Gains a structured subject (`patientId`, `encounterRef`, `entityType/Id`, `reason`, `approvalId`) instead of free-text description only |
| **Event dispatcher** (Phase 8) | every domain event | Direct mapping — the envelope in [domain-events.md](domain-events.md) is deliberately shaped to fit this table |

### 4.1 Ambient audit context

A scoped `IAuditContext` (mirroring the existing `ICurrentUser` idiom) carries, per request:
`correlationId`, `patientId`, `encounterType/Id`, `reason`, `accessReason`. Services set it once at
the top of an operation (`audit.ForPatient(patientId).ForEncounter(ref)`); the interceptor reads it
when stamping rows. This avoids threading audit parameters through 144 services.

Where the context is empty, the event still records entity/actor/correlation — degraded, never
missing.

---

## 5. Access logging

New: every **read** of a patient record or document produces an event.

```
audit.audit_access_log                      -- append-only, partitioned
  id · occurred_at · actor_user_id · actor_role_codes · department_id · branch_id
  patient_id · encounter_type · encounter_id
  access_type   -- RecordViewed | DocumentDownloaded | ReportExported | ListSearched | BreakGlass
  resource_type · resource_id · document_id
  access_reason           -- captured when policy requires it (§8)
  ip_address · device_id · session_id · correlation_id
  result        -- Granted | Denied
```

**Scope of enforcement (Phase 1):**
- All 25 file/PDF-returning endpoints.
- Patient record detail views (EMR timeline, patient dashboard, clinical workspace open).
- Every report export (CSV/Excel/PDF) — with the filter parameters recorded.
- Every cross-branch read (`REPORT.CROSSBRANCH.VIEW` path).
- Denied access attempts (`result = Denied`) — a denied attempt is more interesting than a granted one.

Implemented as an MVC action filter plus an explicit call in the export/PDF paths, so it cannot be
forgotten per-endpoint.

---

## 6. Immutability

Defence in depth — all four, not one:

1. **Database grants.** The application role gets `INSERT` and `SELECT` on `audit.*` and **no**
   `UPDATE`/`DELETE`. A separate migration role performs partition maintenance.
2. **Triggers.** `BEFORE UPDATE OR DELETE ON audit.audit_event → RAISE EXCEPTION`. Same for
   `audit_access_log`, `audit_log`, `activity_log`.
3. **EF model.** Audit entities are mapped read-only for query purposes; only the internal writers
   insert. No `DbSet` update path is exposed and no controller accepts one.
4. **Hash chaining (Phase 10, at extraction).** Each event stores `prev_hash` and `row_hash =
   SHA256(prev_hash ‖ canonical(event))` per partition, making silent tampering detectable. A
   verification job walks the chain and reports breaks.

Retention and archival are configured per record type via the Evidence/retention model, never by
deletion from the live table without an approved, audited destruction record (the pattern
`mrd.destruction_register_entry` already establishes).

---

## 7. Corrections

```
Original event  (event_id = E1, action = 'Approved', ...)      ← never touched
Corrective event (event_id = E2, action = 'Corrected',
                  original_event_id = E1, reason = '...',
                  actor = who corrected, approval_id = ...)
```

Queries and reports render the chain, showing the original and every correction with its reason. The
journey timeline marks corrected events visually rather than hiding them.

---

## 8. Access reason (break-glass)

Hospital-configurable, off by default:

| Setting | Effect |
|---|---|
| `Audit.AccessReason.Mode` = `Off` \| `FlaggedOnly` \| `All` | Whether a reason is prompted before opening a patient record |
| `Audit.AccessReason.FlaggedCategories` | Which patient categories count as flagged (VIP, MLC, staff, psychiatric — all existing master values, none hard-coded) |
| `Audit.BreakGlass.Enabled` | Allows a permissioned user to access outside their normal scope with a mandatory reason, always producing a `BreakGlassAccessUsed` event and a notification to the quality owner |

Settings live in the existing generic `admin.system_setting` store — the same mechanism used for the
Front Office control panel and consultation-charge settings.

---

## 9. Auditor role and permissions

A new **read-only permission namespace**. Read-only by construction: the namespace contains no write
verbs at all.

```
AUDIT.JOURNEY.VIEW        -- patient audit journey timeline
AUDIT.EVENT.VIEW          -- raw audit event search
AUDIT.ACCESS.VIEW         -- who accessed what (access log)
AUDIT.EVIDENCE.VIEW       -- evidence vault, read-only
AUDIT.GAP.VIEW            -- audit gap register
AUDIT.COMPLETENESS.VIEW   -- documentation completeness
AUDIT.READINESS.VIEW      -- audit readiness score
AUDIT.REPORT.GENERATE     -- produce/export an audit report
AUDIT.CROSSBRANCH.VIEW    -- audit scope across branches (explicit, itself audited)
```

Three personas, all read-only, differing only in breadth:

| Persona | Codes | Notes |
|---|---|---|
| **Internal quality team** | all `AUDIT.*` except cross-branch by default, plus existing `QUALITY.*` write codes for CAPA/NC work | Their quality write access lives in the `QUALITY.*` namespace and is deliberately separate from `AUDIT.*` |
| **External assessor** | `AUDIT.JOURNEY.VIEW`, `AUDIT.EVIDENCE.VIEW`, `AUDIT.GAP.VIEW`, `AUDIT.COMPLETENESS.VIEW`, `AUDIT.READINESS.VIEW`, `AUDIT.REPORT.GENERATE` | Time-boxed account (validity window on the user), scoped to a named audit engagement + date range; every read logged; no raw event search; no cross-branch |
| **Hospital management** | `AUDIT.READINESS.VIEW`, `AUDIT.GAP.VIEW`, `AUDIT.REPORT.GENERATE`, `AUDIT.CROSSBRANCH.VIEW` | Dashboard/report level; no per-record clinical drill-down |

**Guarantees enforced in code, not policy:**
- The auditor role presets in `DefaultRoleDefinitions` contain **zero** codes ending in
  `CREATE|UPDATE|DELETE|APPROVE|CANCEL|MANAGE`. A unit test asserts this and fails the build otherwise.
- No audit endpoint accepts a write verb.
- Auditors cannot delete evidence, edit audit history, or approve clinical records — there is no code
  path that would allow it even with a mis-assigned permission.
- Every auditor read is itself written to `audit_access_log`.

---

## 10. Query surface

Replaces today's `AuditLogQuery` (user/entity/action/date only):

```
GET /api/v1/audit/events
    ?patientId= &uhid= &encounterType= &encounterId= &correlationId=
    &actorUserId= &roleCode= &departmentId= &branchId=
    &module= &eventType= &action= &entityType= &entityId=
    &from= &to= &hasReason= &approvalOnly=      → PagedResult<AuditEventDto>

GET /api/v1/audit/events/{eventId}              → full detail incl. correction chain
GET /api/v1/audit/access?patientId=&from=&to=   → access history
GET /api/v1/audit/journey/...                   → see patient-audit-journey.md
```

All under `AUDIT.*` permissions, all branch-filtered by the existing global filter, cross-branch only
with `AUDIT.CROSSBRANCH.VIEW`.

---

## 11. Volume and operations

- Every insert/update in the system already writes an `audit_log` row; `audit_event` roughly doubles
  audit write volume at introduction.
- **Mitigations:** monthly partitions from day one; `before_value`/`after_value` limited to changed
  columns (existing behaviour); access-log writes batched where safe; archival of partitions older
  than the configured retention to cold storage with the chain preserved.
- **Monitoring:** audit write latency, partition size, dispatcher lag, chain-verification failures.
- **Non-goal:** audit events must never block a clinical transaction. If the audit write fails, the
  business transaction fails too (correctness over availability) — but the write path is kept cheap
  enough that this is not a practical risk, and the outbox absorbs downstream consumers.

---

## 12. Phase 1 exit criteria

- [ ] `audit.audit_event` + `audit_access_log` created, partitioned, grants and triggers in place
- [ ] `IAuditContext` wired; interceptor and activity logger enriched
- [ ] Correlation id minted and propagated (HTTP header, log context, background jobs)
- [ ] All 25 file endpoints + record-open + exports emit access events
- [ ] `AUDIT.*` permissions seeded; three auditor presets defined; the "no write verbs" test passes
- [ ] Query API live with patient/encounter/correlation filters
- [ ] Integration tests (Testcontainers — the package is already referenced) covering: immutability
      rejection, correction chain, access logging, and patient-scoped query
