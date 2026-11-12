# Future Architecture — Prixo HIS

**Status:** approved direction, 2026-08-13. Design document; no code written from it yet.
**Prerequisite reading:** [current-state.md](current-state.md).

---

## 1. The product principle this architecture serves

Prixo HIS evolves from *hospital management software* into a **connected hospital operating
platform**, and ultimately into a **clinical + operational + financial + quality + audit + regulatory
evidence platform**.

The architectural test for every future decision is whether the system can answer, for any patient,
any encounter, any transaction:

> What happened · Who did it · When · Why · Was it approved · What evidence exists · What evidence is
> missing · Which patient was affected · Which department was responsible · Which policy applies ·
> Which requirement applies · Was corrective action taken · Is the CAPA closed · **Can we prove it
> during an audit?**

Today the system can answer the first four for most transactions and none of the rest across a
patient's journey. Closing that is the architecture's purpose.

---

## 2. Target shape (18–24 months)

**Hybrid: a modular monolith with enforced internal boundaries, an event backbone, and a small
number of extracted services that earned extraction.**

```
                    ┌───────────────────────────────────────────┐
                    │  Staff SPA  │  Patient Portal │ Auditor UI │
                    └──────┬──────────────┬──────────────┬───────┘
                           │              │              │
                    ┌──────▼──────────────▼──────────────▼───────┐
                    │        Prixo.Api  (single deployable)      │
                    │  ┌──────────────────────────────────────┐  │
                    │  │ CLINICAL CORE  (stays together)      │  │
                    │  │ Patients · Appointments · OPD · IPD  │  │
                    │  │ Nursing · ICU · Emergency · OT · EMR │  │
                    │  ├──────────────────────────────────────┤  │
                    │  │ REVENUE     Billing · Finance · Ins. │  │
                    │  ├──────────────────────────────────────┤  │
                    │  │ DIAGNOSTIC  Lab · Radiology          │  │
                    │  ├──────────────────────────────────────┤  │
                    │  │ SUPPLY      Pharmacy · Stores        │  │
                    │  ├──────────────────────────────────────┤  │
                    │  │ SUPPORT     MRD · Biomed · Masters   │  │
                    │  ├──────────────────────────────────────┤  │
                    │  │ COMPLIANCE  Quality · Evidence ·     │  │
                    │  │             Regulatory · Safety      │  │
                    │  └──────────────┬───────────────────────┘  │
                    │                 │ publishes domain events  │
                    │        ┌────────▼─────────┐                │
                    │        │ Outbox dispatcher│                │
                    │        └────────┬─────────┘                │
                    └─────────────────┼──────────────────────────┘
                                      │
              ┌───────────────────────┼───────────────────────┐
              │                       │                       │
     ┌────────▼────────┐    ┌─────────▼────────┐   ┌──────────▼────────┐
     │  Notification   │    │ Audit & Evidence │   │ Reporting /       │
     │  Service        │    │ Service          │   │ Analytics         │
     │  (Phase 9)      │    │ (Phase 10)       │   │ (Phase 15)        │
     └─────────────────┘    └──────────────────┘   └───────────────────┘
```

Everything inside the box shares one database and one deployment. Everything outside owns its own
store and is reached by API (sync) or event (async). **Extraction is the exception, justified per
service by the scoring rubric in [microservices-strategy.md](microservices-strategy.md) — never the
default.**

---

## 3. Layer model

| Layer | Today | Target | Change |
|---|---|---|---|
| **User access** | Staff SPA + portal SPA, JWT ×2 audiences | + Auditor experience (read-only), shared clinical component library | Additive |
| **Application** | 144 services over one `IAppDbContext` | Same services, but each module declares the tables it owns; cross-module reads move to explicit contracts | Enforced boundaries, no rewrite |
| **Workflow / business process** | Explicit action endpoints + approval entities per module | + a configurable **rule engine** for hospital policy (assessment due windows, double verification, clearance requirements) | New capability |
| **Data & platform** | 1 PostgreSQL, 23 schemas, 245 tables, 3 MVs | Same DB; new `audit` and compliance tables; partitioned audit; read replica when volume justifies | Additive |
| **Eventing** | *none* | In-process domain events + transactional outbox → dispatcher; broker only when a cross-process consumer exists | **New** |
| **Integration** | Placeholder columns + 3 unimplemented ports | Outbound runtime: HTTP policy, retry/backoff, circuit breaker, DLQ, idempotency — built once for SMS, reused for gateway/HL7 | **New** |
| **Security & access** | RBAC 405 codes, DB-checked, 2 audiences, branch filter | + `AUDIT.*` read-only namespace, MFA, access-reason capture, document access logging, department scoping | Extended |
| **Audit** | Row diffs + activity log, no patient dimension | `audit.audit_event` — patient/encounter/actor-role/department/session/device/reason/approval/correlation, append-only by construction | **Redesigned (additive)** |
| **Observability** | Console logs only | Structured logs + metrics + traces + correlation propagation | **New** |

---

## 4. Architectural principles (binding)

1. **Reuse before creation.** No second patient identity, notification, audit, or document store. New
   capability extends the existing table where one exists (`quality.quality_evidence`,
   `quality.quality_capa`, `nursing.incident`, `ipd.discharge_clearance`).
2. **Additive over destructive.** No working module is rewritten to accommodate the new architecture.
   The existing `AuditSaveChangesInterceptor` keeps running unchanged while the richer event model is
   added alongside it.
3. **Boundaries before services.** A module must own its tables and publish its events *inside* the
   monolith before anyone discusses extracting it.
4. **Events are facts, not commands.** `LabResultApproved`, not `NotifyDoctor`. Consumers decide what
   to do.
5. **Local transactions stay local.** If a flow currently commits atomically across modules
   (OT→Pharmacy→Billing, IPD discharge, patient merge), it stays in one deployable until a saga is
   genuinely warranted.
6. **Audit is written, never edited.** Corrections are new events referencing `OriginalEventId`.
7. **Regulation is data, never code.** No NABH or NMC requirement text ships in the product. No
   compliance claim appears in code, UI, or copy.
8. **Read-only means read-only by construction.** The auditor permission namespace contains no write
   verbs — it is not a role that happens to omit them.
9. **Every table stays multi-branch aware.** `BranchId` on transactional data; cross-branch access
   stays explicit, permissioned, and audited.
10. **No PHI in logs or URLs.** Patient context travels in the audit event and the request body, not
    in log messages or query strings.

---

## 5. What changes structurally

### 5.1 The Encounter abstraction (Phase 2)

Today `OpVisit`, `Admission`, and `EmergencyVisit` are three independent roots. Rather than merge
them — which would touch every module — introduce a **resolver-backed reference**:

```
EncounterRef  (type: OpVisit | Admission | EmergencyVisit, id, patientId, branchId,
               departmentId, startedAt, endedAt, status)
```

surfaced as a read projection (view or service) that every new cross-cutting feature consumes. The
three source tables are untouched. Audit events, evidence links, completeness rules, and safety
events all key on `(EncounterType, EncounterId)` — a shape the codebase already uses successfully for
`BillItem.SourceType/SourceId` and `QualityCapa.SourceType/SourceId`.

### 5.2 The audit backbone (Phase 1)

A new `audit.audit_event` table becomes the spine of the platform: every clinical, financial,
document, and access action lands there with full patient/encounter/actor/reason/approval/correlation
context, append-only by database construction. See [audit-architecture.md](audit-architecture.md).

### 5.3 The compliance domain (Phases 4–6)

Evidence Vault, versioned Regulatory Framework, requirement mapping, documentation completeness, and
CAPA linkage form a coherent compliance domain inside the `quality` schema — reusing the CAPA, risk,
non-conformance, incident, and NABH-standard tables that already exist rather than creating parallels.

### 5.4 The event backbone (Phase 8)

Transactional outbox inside the monolith. Events are published in the same transaction as the
business change, dispatched asynchronously, consumed first by Audit, then Notification, then
Analytics. No broker until a cross-process consumer exists. See
[domain-events.md](domain-events.md).

---

## 6. What deliberately does not change

- The clinical core stays one deployable (scores 14–18 on the extraction rubric).
- Clean Architecture layering, `ApiResponse<T>`, `PagedResult<T>`, DTO-only boundaries, FluentValidation
  per request, thin controllers.
- `BaseEntity`/`BranchEntity`, soft delete, partial unique indexes, snake_case, schema-per-domain,
  `numeric(18,2)` money, `timestamptz`.
- Permission codes seeded from `PermissionRegistry`, checked against the database per request.
- Explicit named migrations; never `EnsureCreated`, never auto-migrate.
- React + TanStack Query + Tailwind; no Redux, no Bootstrap.

---

## 7. Related documents

| Document | Covers |
|---|---|
| [microservices-strategy.md](microservices-strategy.md) | Extraction rubric, strangler fig, non-goals |
| [service-boundaries.md](service-boundaries.md) | The 23 logical services, ownership, contracts |
| [domain-events.md](domain-events.md) | Event catalog, envelope, versioning, outbox |
| [database-boundaries.md](database-boundaries.md) | Table ownership and migration strategy |
| [audit-architecture.md](audit-architecture.md) | AuditEvent, immutability, access logging, auditor role |
| [patient-audit-journey.md](patient-audit-journey.md) | Journey timeline, gap detection, audit report |
| [evidence-management.md](evidence-management.md) | Evidence Vault |
| [nabh-framework.md](nabh-framework.md) | Regulatory framework, NABH evidence mapping |
| [nmc-framework.md](nmc-framework.md) | NMC / medical education framework |
| [clinical-document-completeness.md](clinical-document-completeness.md) | Documentation completeness engine |
| [patient-safety-engine.md](patient-safety-engine.md) | Safety detection and escalation |
| [security-architecture.md](security-architecture.md) | RBAC, auditor personas, privacy, MFA |
| [observability.md](observability.md) | Logging, metrics, tracing, correlation |
| [migration-roadmap.md](migration-roadmap.md) | Phase plan, deliverables, exit criteria |
| [future-enhancements.md](future-enhancements.md) | The 26 candidate modules, with honest status |
