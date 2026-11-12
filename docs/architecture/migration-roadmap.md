# Migration Roadmap — Prixo HIS

**Status:** approved direction, 2026-08-13.
**Rule:** every phase ships a user-visible capability. There are **no pure-refactor phases** — the
architecture improves as a by-product of delivering something the hospital can use.

Phase numbering follows the brief. Effort is relative (S/M/L/XL), not a date commitment.

---

## Phase 0 — Architecture discovery ✅ complete

**Delivered:** [current-state.md](current-state.md) — measured module map, dependency graph, database
coupling, contested tables, security findings, scalability risks, 22-item debt register,
microservice scoring for all 23 candidates.

**Key outcomes:** F1 (audit has no patient dimension) is the critical path; the clinical core scores
14–18 and stays together; Notification and Audit score 41–43 *because they don't exist yet*.

---

## Phase 1 — Audit foundation `L` **← next**

**Why first:** nothing else in the roadmap can be built on today's audit tables.

| Deliverable | Detail |
|---|---|
| `audit.audit_event` | Full model, monthly partitions, indexes ([audit-architecture.md §3](audit-architecture.md#3-the-auditaudit_event-model)) |
| `audit.audit_access_log` | Record/document/export/denied access |
| Immutability | DB grants (no UPDATE/DELETE), triggers, read-only EF mapping |
| `IAuditContext` | Ambient patient/encounter/reason/correlation, mirroring `ICurrentUser` |
| Enriched producers | Interceptor + `IActivityLogger` gain structured subject + context |
| Correlation middleware | `X-Correlation-Id` accept/mint/propagate/echo; Serilog enrichment; PHI redaction policy |
| Access logging | All 25 file endpoints, record opens, report exports, cross-branch reads |
| `AUDIT.*` permissions | Read-only namespace + three auditor presets + build-failing "no write verbs" test |
| Audit query API | Patient / encounter / correlation / actor / module / date filters |
| MFA schema | `admin.user_mfa` (schema only; enforcement Phase 11) |
| Staff auth rate limiting | Extends the existing portal policy |
| **Integration test harness** | Testcontainers (package already referenced, suite empty) — immutability, access logging, patient-scoped query |

**Exit criteria:** an auditor can query every recorded action for a given patient, including who read
their record; no application path can modify an audit row.

---

## Phase 2 — Encounter abstraction `M`

`EncounterRef` projection over `OpVisit` / `Admission` / `EmergencyVisit`; department scoping option;
fix the branch-filter-disabled-when-null gap (S5); architecture test asserting module entity
ownership.

**Ships:** consistent encounter selection across audit, quality, and reporting screens.
**Exit:** one code path resolves any encounter; no new feature writes a 3-way union.

---

## Phase 3 — Patient audit journey `L`

Journey projection + timeline API + **Audit → Patient Journey** screen + shared clinical components
(`PatientHeader`, `EncounterHeader`, `AuditTimeline`, `EvidenceCard`, `ApprovalCard`, `GapCard`) +
audit report export (PDF/Excel/CSV).

**Ships:** the flagship capability. **Exit:** an auditor reconstructs a full patient journey with
actors, roles, approvals, reasons, and access history — and exports it.

---

## Phase 4 — Evidence vault `M`

Generalize `quality.quality_evidence` → `quality.evidence` + `evidence_link` + versions + checksums +
access log + retention. Register (not migrate) patient documents, EMR attachments, MRD scans, claim
documents, Biomed certificates. **Wire `IVirusScanner`. Move to object storage.**

**Exit:** one queryable evidence index across five existing stores; integrity verifiable; every
access logged.

---

## Phase 5 — Quality / NABH + NMC framework `L`

`regulatory_framework` (versioned) + `regulatory_requirement` + `requirement_mapping` +
`requirement_evidence_status`; NABH Audit Mode screen; NMC framework-level dashboard; CAPA gains
`Requirement` as a source type.

**Ships empty of regulatory content.** **Exit:** a hospital loads its own framework version, maps
requirements to policies/processes/HIS workflows, and sees evidence available vs gap — with the
framework version stamped on every report.

---

## Phase 6 — Clinical documentation completeness `M`

`documentation_requirement` + items + evaluator; panels in IPD/discharge/MRD; department- and
doctor-wise dashboard; generalizes `mrd.file_audit`.

**Exit:** completeness % per encounter, drillable to the missing item, configurable per hospital,
advisory by default.

---

## Phase 7 — Patient safety engine + audit gap detection `L`

`safety_event` + `safety_rule` + shared rule evaluator (also serving completeness and gap detection);
`audit_gap` register; Patient Safety Dashboard; promotion path to the **existing** `nursing.incident`
→ RCA → CAPA.

**Exit:** configurable detection with acknowledgment, escalation, and closure — no parallel incident
register.

---

## Phase 8 — Domain event architecture `L`

`audit.outbox_message` + dispatcher (Hangfire) + the event catalogue from
[domain-events.md](domain-events.md), published in-transaction, consumed in-process by Audit,
Completeness, Safety, Quality. Idempotency keys on mutating endpoints (S10).

**Exit:** every domain event in the catalogue is published and consumed in-process, with dispatch
metrics and dead-letter alerting. **No broker.**

---

## Phase 9 — Notification service `M` — *first extraction*

Templates, channels (SMS/email/push/in-app), escalation policies, delivery attempts, retry/DLQ.
Consumes `CriticalResultDetected`, `IcuAlertRaised`, `SafetyEventDetected`, `AuditGapDetected`,
appointment reminders, `PmDue`, `CalibrationDue`, `NearExpiryDetected`. Drains and retires
`admin.notification_queue`.

**Gate:** OpenTelemetry tracing must be live before this phase starts.
**Exit:** one notification system; no module sends its own messages.

---

## Phase 10 — Audit service extraction `L`

Move the audit store to its own database with its own retention; add hash chaining + a verification
job; the monolith publishes, the service ingests and serves queries.

**Trigger (not automatic):** audit volume, retention divergence, or an external-assessor access
requirement that justifies isolation. If none applies, **stay in-core** — that is a valid outcome.

---

## Phase 11 — Patient identity hardening `M`

MFA enforcement for admin/finance/auditor roles; patient identity contract formalized (all writes via
`PatientService`, merge published as an event); duplicate/merge improvements.
**Extraction is not scheduled** — score 27, blocked by 37 `PatientId` FKs.

---

## Phases 12–14 — Reassessment gates, not commitments

| Phase | Candidate | Precondition |
|---|---|---|
| 12 | Laboratory service | Re-score after Phases 1–10; needs an operational trigger (scale, isolation, ownership) |
| 13 | Pharmacy / Inventory | **Blocked** until `stock_ledger`/`batch` mutation is funnelled through one owner port |
| 14 | Billing / Finance | **Blocked** — 4 modules append `bill_item`; score 16. Likely never extracted. |

Each requires a written trigger and a fresh score. Absent that, they stay in-core.

---

## Phase 15 — Analytics / data platform `L`

Event-fed datasets, warehouse or dedicated reporting store, subscription delivery (currently an
outbox with no dispatcher), audit-readiness and compliance datasets. Removes reporting load from the
primary database.

---

## Phase 16 — Full observability `M`

OpenTelemetry metrics + traces, log aggregation, dashboards, alert routing (clinical alerts through
Notification's escalation chain), service-status screen, Hangfire hardening.

*Tracing is pulled forward as a gate on Phase 9; this phase completes the stack.*

---

## Dependency graph

```
0 ──▶ 1 ──┬──▶ 2 ──▶ 3 ──▶ 4 ──▶ 5
          │              └──▶ 6 ──▶ 7
          └──▶ (correlation + tracing) ──▶ 8 ──▶ 9 ──▶ 10
                                                └──▶ 15 ──▶ 16
                                          11 ──▶ 12/13/14 (gated, unscheduled)
```

Hard prerequisites: **1 before everything** · 2 before 3, 6, 7 · 4 before 5 · 6 before 7 ·
8 before 9 · tracing before 9 · 9 before 10.

---

## Cross-phase standards

Every phase, without exception:

- Explicit named migration (`YYYYMMDD_<Module>_<Change>`); never `EnsureCreated`, never auto-migrate
- Permission codes in `PermissionRegistry`, `[HasPermission]` on every endpoint
- FluentValidation per request DTO; DTO-only boundaries; thin controllers
- Unit **and integration** tests green (the integration suite starts being real in Phase 1)
- Audit coverage verified for new write paths
- UI permission-gated
- Module spec updated under `docs/modules/`; architecture docs updated here
- No regulatory content, no compliance claims, no fabricated metrics

---

## Risks to the plan

| Risk | Mitigation |
|---|---|
| Phase 1 expands audit write volume before partitioning proves out | Partition and index at creation; load-test with production-scale row counts before rollout |
| Encounter abstraction leaks into every module | Delivered as an additive projection; no source table is restructured |
| Framework phases stall waiting for hospital content | Framework ships empty and is useful with manual evidence from day one |
| Readiness score becomes a vanity metric | Every component drillable; unmeasurable components render "Not measured" |
| Extraction pressure arrives before observability | Phase 9 is explicitly gated on tracing |
| Roadmap competes with feature delivery | Each phase is a shippable capability, not a refactor |
