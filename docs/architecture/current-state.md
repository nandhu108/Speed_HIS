# PRIXO HIS — Current State Architecture Assessment

**Assessment date:** 2026-08-13
**Method:** direct inspection of the repository at `d:\Prixo` (branch `main`). Every claim below is
sourced from code, not from `CLAUDE.md`, `docs/HIS_DEVELOPMENT_PLAN.md`, or module specs. Where the
documentation and the code disagree, the code is treated as authoritative and the discrepancy is
recorded.

**Status of this document:** discovery output only. No code, schema, or configuration was changed to
produce it.

---

## 1. Executive summary

Prixo HIS is a **well-built modular monolith** — one .NET 10 solution, one PostgreSQL database, one
deployable API, one React SPA. It is not a distributed system and does not pretend to be one. The
module boundaries are real at the *source-tree* level (feature folders mirror across Domain →
Application → API → frontend) but are **not enforced at the data layer**: a single `AppDbContext`
exposes ~300 `DbSet`s over 245 tables in 23 schemas, and any service may query any table.

The engineering conventions are unusually disciplined for a system this size: soft delete and branch
scoping via global query filters, mandatory transactions on financial/stock paths, DTO-only API
boundaries, a 405-code permission registry checked against the database on every request, and an
automatic row-diff audit interceptor. These are the assets a migration should build on, not replace.

Six findings dominate the roadmap:

| # | Finding | Consequence |
|---|---|---|
| F1 | The audit trail has **no patient, encounter, session, reason, or correlation dimension** — `audit.audit_log` can only be queried by user, entity name, entity id, and date | The requested "Patient Audit Journey" **cannot be built on the current audit tables**. This is the single largest gap. |
| F2 | There is **no unified Encounter concept** — `OpVisit`, `Admission`, and `EmergencyVisit` are three parallel roots joined by nullable FKs on orders and bills | Any cross-encounter timeline, completeness engine, or audit scope must either introduce an encounter abstraction or hard-code a 3-way union everywhere |
| F3 | **No event infrastructure exists at all** — no MediatR, no domain events, no outbox dispatcher, no broker | Event-driven architecture is a greenfield addition, not a refactor. Nothing currently depends on eventing, so it can be introduced without breaking anything. |
| F4 | Audit immutability is **convention, not enforcement** — no DB triggers, no revoked grants, and `AuditLog`/`ActivityLog` are mapped as writable EF entities | An "append-only audit" claim is not currently defensible to an auditor |
| F5 | **Notification is an unconsumed outbox** — `admin.notification_queue` has exactly one producer (`AppointmentService`) and zero dispatchers; no SMS/email/push exists anywhere | A Notification Service is new build, not extraction |
| F6 | Observability is **console logging only** — no metrics, no tracing, no OpenTelemetry, no correlation propagation beyond ASP.NET's `TraceIdentifier` | Service extraction would be operating blind. Observability must precede extraction, not follow it. |

**Architectural recommendation up front:** do not extract microservices yet. The measured
service-extraction scores (§12) place every current module in "keep modular monolith" or "prepare
boundary" territory — with two exceptions (Notification, Audit/Evidence) that score as strong
candidates precisely because **they do not exist yet** and can be born standalone. Build the audit,
evidence, and event foundations *inside* the monolith first; extract only after they carry real
traffic.

---

## 2. Current architecture

### 2.1 Physical topology

```
┌──────────────────────────────────────────────────────────────┐
│ browser                                                      │
│   React 19 SPA (Vite 8) — staff app  +  /portal patient app  │
└───────────────┬──────────────────────────────────────────────┘
                │ HTTPS, JWT bearer (2 audiences: staff | portal)
┌───────────────▼──────────────────────────────────────────────┐
│ Prixo.Api — single ASP.NET Core process (docker: api:8080)   │
│  · JWT auth (2 schemes) · session-validation middleware      │
│  · permission handler (DB lookup per request)                │
│  · rate limiter (portal auth only) · health checks           │
│  · Hangfire server IN-PROCESS (3 recurring jobs)             │
└───────────────┬──────────────────────────────────────────────┘
                │ single AppDbContext, single connection string
┌───────────────▼──────────────────────────────────────────────┐
│ PostgreSQL 16 — one database `prixo_his`                     │
│  23 schemas · 245 tables · 3 materialized views (reporting)  │
│  + Hangfire job storage in the SAME database                 │
└──────────────────────────────────────────────────────────────┘
                │
        local disk volume (App_Data/uploads) — all documents
```

There is **no API gateway, no message broker, no cache tier, no object storage, no read replica**.
`docker/docker-compose.yml` defines `db`, `api`, `web`, and an optional `pgadmin`. A production HA
topology is not expressed anywhere in the repository.

### 2.2 Logical layering (backend)

Clean Architecture, references pointing inward only:

| Project | Contents | Notes |
|---|---|---|
| `Prixo.Domain` | ~190 entity classes in 23 folders, enums, `BaseEntity`/`BranchEntity`, `ISoftDelete`/`IBranchScoped`/`IAuditable`/`ISimpleMaster` | Zero dependencies. **No domain events, no aggregate roots, no invariant enforcement in the domain layer** — entities are persistence-shaped data holders; all rules live in Application services. |
| `Prixo.Application` | 144 `*Service.cs` classes across 24 feature folders, DTOs, FluentValidation validators, 22 `Common/Interfaces` ports, `PermissionRegistry` (405 codes), `DefaultRoleDefinitions` | Business logic lives here. Services depend on `IAppDbContext` (the full 300-DbSet surface), not on repositories. |
| `Prixo.Infrastructure` | `AppDbContext`, 23 configuration files, 55 migrations, `AuditSaveChangesInterceptor`, `UnitOfWork`, 6 concrete services (ActivityLogger, LocalFileStorage, QuestPdf, ReportExport, ReportViewRefresher, Sequence), JWT/password identity, Hangfire wiring | |
| `Prixo.Api` | ~30 controller groups, 4 authorization types, 2 middlewares, 2 current-user services, validation filter | Controllers are thin: parse → service → `ApiResponse<T>`. |

### 2.3 Frontend architecture

React 19 + Vite 8 + TypeScript strict + Tailwind 4 + React Router 7 + TanStack Query 5 + Axios +
React Hook Form/Zod. 302 `.ts/.tsx` files. 27 feature folders under `src/features/`, each
self-contained (`pages/`, `api.ts`, `types.ts`), plus a **separately-rooted patient portal**
(`src/portal/` with its own api/layout/routes/lib). Shared surface is deliberately thin: 13 UI
primitives in `src/components/ui/`, 3 layout components (a tabbed workspace shell), 6 `src/lib/`
modules.

> **Doc drift (minor):** `CLAUDE.md` states React 18 + React Router 6 + Bootstrap-excluded Tailwind.
> The code is React 19 + RR7 + Tailwind 4. Worth correcting when the docs are next touched.

There is **no shared clinical component vocabulary** — no patient header, encounter header, or
timeline component exists as a reusable primitive. The EMR timeline UI is local to the EMR feature.
The future audit/quality screens described in §27–28 of the enhancement brief will need these
extracted as shared components.

---

## 3. Current modules

24 Application feature folders map to the 23 named business modules plus `Masters`:

| # | Module | Feature folder | Schema(s) | Tables | Maturity observed in code |
|---|---|---|---|---|---|
| 1 | Administration & RBAC | `Admin` | `admin`, `audit` | 19+2 | Production-shaped: users, roles, 405 permissions, per-user overrides, role presets, menus, branches, sessions, settings, both log tables |
| 2 | Masters | `Masters` | `master` | 16 | Generic CRUD framework over `ISimpleMaster` + bespoke editors (Tariff, Doctor, Locations, ICD) |
| 3 | Patient Registration | `Patients` | `patient` | 9 | UHID sequencing, duplicate detection (`TextSimilarity`), merge (transactional FK re-point), consents, documents, identifiers |
| 4 | Appointments | `Appointments` | `appointment` | 4 | Slot engine, tokens, no-show Hangfire job, notification outbox (write-only) |
| 5 | OPD | `Opd` | `opd` | 12 | `OpVisit` is the OP encounter root; carries MHC + Camp discriminators |
| 6 | Billing | `Billing` | `billing` | 17 | Transactional; discount/refund/credit-note approval workflows; day close; cashier shift |
| 7 | Laboratory | `Lab` | `lab` | 11 | Order → sample → result → verify → approve; auto H/L/critical flagging; critical alerts |
| 8 | Pharmacy | `Pharmacy` | `pharmacy` | 28 | Largest schema. Append-only `stock_ledger`, FEFO, PR/PO/GRN/return, POS, issues, adjustments, transfers, audits |
| 9 | IPD | `Ipd` | `ipd` | 13 | Admission, bed allocation, orders, charting, 4-way discharge clearance, discharge summary |
| 10 | Nursing | `Nursing` | `nursing` | 10 | Assessments, MAR (schedule + administration), risk scores, SBAR handover, `Incident` |
| 11 | EMR | `Emr` | `emr` | 8 | Timeline aggregator (in-memory merge of 8 sources), histories, problem list, attachments |
| 12 | Radiology | `Rad` | `rad` | 6 | Order → schedule → capture → report → verify → approve; PACS columns are placeholders |
| 13 | Stores & Inventory | (inside `Pharmacy`) | `pharmacy` | — | Widened Pharmacy entities (`ItemId` alongside `DrugId`) + Indent workflow. **No separate schema or folder.** |
| 14 | Operation Theatre | `Ot` | `ot` | 11 | Booking with conflict checks, WHO checklist gating status transitions, implants/consumables → stock + bill |
| 15 | ICU | `Icu` | `icu` | 8 | Extends IPD; ventilator/infusion charts, severity, configurable alert rules, `device_reading` placeholder |
| 16 | Emergency | `Er` | `er` | 6 | Triage/ESI, assessment, disposition, MLC/accident registers, observation beds |
| 17 | MRD | `Mrd` | `mrd` | 8 | Physical file + movement log, requests, scanning, coding worklist, file audit, retention/destruction |
| 18 | Finance | `Finance` | `finance` | 6 | CoA, vouchers + balanced lines, ledger projection, AP/AR |
| 19 | Biomedical | `Biomed` | `biomed` | 9 | Equipment, AMC, PM, work orders, breakdown, calibration, spares → Pharmacy stock |
| 20 | Quality/NABH | `Quality` | `quality` | 14 | Indicators, audit templates/runs, non-conformance, **CAPA (polymorphic source)**, risk register, committees, **NABH standard master**, **evidence library** |
| 21 | Patient Portal | `Portal` | `patient`, `billing` | — | Separate JWT audience + own refresh tokens; thin ownership-checked wrappers over staff services |
| 22 | Doctor Portal | `DoctorPortal` | — | 0 | Pure projection over staff services; owns no tables |
| 23 | Reports & Dashboards | `Reports` | `reporting` | 2 + 3 MVs | Report definition catalog, generic executor, CSV/Excel/PDF export, 5 dashboards |
| 24 | Insurance/TPA | `Insurance` | `insurance` | 5 | Policy, pre-auth + enhancement, claim → settlement → Finance hook |
| — | Loyalty | `Loyalty` | `loyalty` | 2 | Card issue/renew/dependents |

**Assets already present that the future roadmap must reuse rather than duplicate:**
`quality.quality_evidence` (evidence library), `quality.quality_nabh_standard` (requirement master),
`quality.quality_capa` (already polymorphic over Incident/NonConformance/Audit/Risk),
`quality.quality_risk`, `nursing.incident` (already widened with RCA + sentinel-event fields),
`ipd.discharge_clearance` (a working 4-way clearance checklist), `ot.surgical_safety_checklist`,
`mrd.file_audit` (a completeness checklist with a computed flag), and EMR's `TimelineService`.

---

## 4. Dependency map

Measured two ways from the source. Both are directional (`A → B` = A depends on B).

### 4.1 Cross-feature *service* dependencies (`using Prixo.Application.Features.X` inside feature Y)

```
Appointments → Admin, Reports
Billing      → Admin, Ipd, Reports
Biomed       → Pharmacy
DoctorPortal → Ipd, Lab, Opd
Er           → Ipd, Reports
Icu          → Ipd
Insurance    → Patients
Ipd          → Reports
Masters      → Reports
Opd          → Admin, Ipd, Reports
Ot           → Billing, Pharmacy
Patients     → Admin, Reports
Portal       → Admin, Appointments, Billing, Emr, Ipd, Lab, Rad
Quality      → Ipd, Lab, Masters
Reports      → Admin, Ipd
```

### 4.2 Cross-module *entity* dependencies (`using Prixo.Domain.Entities.X` inside feature Y)

```
Appointments → Admin, Er, Masters, Patients
Billing      → Emr, Er, Ipd, Masters, Mrd, Opd, Patients      ← widest fan-in consumer
Biomed       → Pharmacy
Emr          → Ipd, Masters, Opd, Patients
Er           → Masters, Patients
Finance      → Billing, Pharmacy
Icu          → Ipd, Masters
Insurance    → Billing, Ipd, Opd, Patients
Ipd          → Billing, Masters, Patients
Lab          → Opd, Patients
Loyalty      → Masters, Patients
Mrd          → Ipd
Nursing      → Ipd, Masters, Patients
Opd          → Appointments, Masters, Patients
Ot           → Billing, Ipd, Masters, Patients, Pharmacy
Patients     → Insurance
Pharmacy     → Billing, Masters
Portal       → Billing, Patients
Rad          → Patients
Quality, Reports, Admin, Masters → (self-contained)
```

### 4.3 Readings

- **`Patients` and `Masters` are universal upstream dependencies** — nearly every module reads them.
  This is correct and is exactly why Patient Identity and a reference-data service are the natural
  first extractions on identity grounds, and simultaneously the *riskiest* on availability grounds.
- **`Billing` is the widest consumer** (7 modules' entities) and **`Ipd` the widest producer**
  (consumed by Billing, Emr, Er, Icu, Insurance, Mrd, Nursing, Ot, Reports, Quality, DoctorPortal,
  Portal). IPD is the load-bearing wall of the monolith.
- **A dependency cycle exists at the entity level:** `Billing ↔ Ipd`, `Patients ↔ Insurance`,
  `Pharmacy ↔ Billing`. These are compile-time-legal because all entities live in one project, but
  they are genuine bidirectional couplings that must be broken (by event or by contract) before
  either side can be extracted.
- **`Quality` is nearly self-contained** (reads Ipd/Lab/Masters only for indicator auto-compute).
  This is the best-isolated large module in the system and the natural home for the new
  audit/evidence capabilities.
- **Portals are pure façades** — `DoctorPortal` owns zero tables; `Portal` owns 3. Neither is an
  extraction candidate; both are presentation adapters.

---

## 5. Database coupling

### 5.1 The central coupling fact

`Prixo.Application.Common.Interfaces.IAppDbContext` (366 lines) exposes **every** `DbSet` in the
system. Every one of the 144 services receives this interface. There is no repository layer, no
per-module context, no schema-scoped connection. **Module data isolation is therefore zero at the
persistence layer** — it exists only as a code-review convention.

Consequences:
- Cross-module reads are invisible in the dependency graph unless the service happens to `using` the
  entity type. §4.2 is therefore a *lower bound* on real coupling.
- Any module can join across schema boundaries in a single LINQ query, and many do (Billing joins
  OPD/IPD/ER; Insurance joins Billing/IPD/OPD; Quality joins Lab/IPD).
- A future service split cannot begin until the read paths are inventoried per table.

### 5.2 Foreign-key coupling across schema boundaries

Cross-schema FKs are pervasive and intentional under the current design. The heaviest are:

| From (schema.table) | To | Nature |
|---|---|---|
| ~37 entity types carrying `PatientId` | `patient.patient` | universal identity reference |
| `lab.lab_order`, `rad.rad_order`, `billing.bill` | `opd.op_visit` **and** `ipd.admission` **and** `er.emergency_visit` (all nullable) | the "no unified encounter" pattern (F2) |
| `billing.bill_item` | `master.service`, plus untyped `SourceType`/`SourceId` | polymorphic, no FK |
| `ot.implant_usage`, `ot.consumable_usage`, `biomed.spare_usage` | `pharmacy.batch`, `pharmacy.stock_ledger` | stock decrement inside another module's transaction |
| `finance.ledger_entry` | `billing.bill_payment` (via poster hook) | denormalized projection |
| everything branch-scoped | `admin.branch` | multi-branch discriminator |
| every audited row | `admin.user` via `CreatedBy`/`UpdatedBy` (no FK constraint) | audit stamping |

### 5.3 Shared tables (multiple owners write them)

| Table | Written by | Risk if split |
|---|---|---|
| `pharmacy.stock_ledger`, `pharmacy.batch` | Pharmacy, OT (implants/consumables), Biomed (spares), Stores (indent issue) | **High** — three modules mutate stock inside their own transactions |
| `billing.bill`, `billing.bill_item` | Billing, IPD (bed charges), OT (implant charges), ER (empty ER bill), Pharmacy (sale→bill), Insurance (claim link) | **High** — bill lines are appended by four modules |
| `opd.op_visit` | OPD, MHC flow, Camp flow, Appointments (check-in) | Medium — one table, three registration workflows |
| `ipd.admission` | IPD, ER (admit-convert), OT (booking anchor), Billing, MRD, Nursing, ICU | **High** — the load-bearing entity |
| `patient.patient` | Patients, Portal (self-registration), ER (unknown-patient rapid registration) | Medium |
| `audit.audit_log` | the interceptor only (all modules implicitly) | Low — single writer, but see §8 |
| `admin.notification_queue` | Appointments only | Low — currently near-dead |
| `master.*` | Masters (writes), everyone (reads) | Low — read-mostly reference data |

### 5.4 Transactions crossing module boundaries

`IUnitOfWork.ExecuteInTransactionAsync` wraps these multi-module flows in a **single local ACID
transaction**. Each is a place where a service split would require a saga or a compensating action:

| Flow | Modules inside one transaction |
|---|---|
| OT implant/consumable usage | OT + Pharmacy (stock ledger) + Billing (bill item) |
| Pharmacy POS sale | Pharmacy + Billing + stock ledger |
| IPD admission | IPD (admission + bed allocation) + Billing (initial deposit/advance) |
| IPD discharge | IPD + Billing (clearance) + Pharmacy (clearance) + Lab/Rad (clearance) |
| Emergency admit-convert | ER + IPD + Billing |
| Bill payment collection | Billing + Finance (`IFinanceLedgerPoster`, no-op until configured) |
| Insurance claim settlement | Insurance + Finance |
| Patient merge | Patients + every table holding `PatientId` (FK re-point) |
| Day close / cashier shift | Billing across all collection sources |
| Biomed work-order completion | Biomed + Pharmacy (spare consumption) |

`UnitOfWork` uses a plain `BeginTransaction`/`Commit` with **no retrying execution strategy** — a
deliberate, documented choice, but it means transient PostgreSQL failures surface to the user and
there is no idempotency key anywhere in the system to make a retry safe.

---

## 6. Shared services (the existing platform layer)

The ports in `Prixo.Application/Common/Interfaces/` are the de-facto platform contracts. They are
the right seams for future extraction because Application code already depends on the interface, not
the implementation:

| Port | Implementation | Maturity |
|---|---|---|
| `IAppDbContext` | `AppDbContext` | **Anti-seam** — exposes everything; the thing to decompose |
| `IUnitOfWork` | `UnitOfWork` | Real, no retry strategy |
| `ICurrentUser` / `IPortalCurrentUser` | claims-based, falls back to system user id 1 | Real |
| `IActivityLogger` | `ActivityLogger` (~120 event constants) | Real, but events are strings with a free-text description and **no structured subject** |
| `ISequenceService` | `SequenceService` (atomic per key/scope/year counters) | Real, 40+ registered keys |
| `IFileStorageService` | `LocalFileStorageService` (local disk, path-sanitized) | Real but **single-node**; no object storage, no encryption at rest, no access log |
| `IPdfService` | `QuestPdfService` (890 lines, all report/document layouts) | Real, monolithic |
| `IReportExportService` | CSV/Excel (ClosedXML)/PDF | Real |
| `IReportViewRefresher` | 3 `REFRESH MATERIALIZED VIEW CONCURRENTLY` statements | Real; the only raw SQL in the codebase |
| `IJwtTokenService`, `IPasswordHasherService` | real | Real |
| `IFinanceLedgerPoster` | no-op until a branch configures accounts | **Deliberate no-op** |
| `IAdvanceCollector` | hook | Deliberate hook |
| `IVirusScanner` | **no implementation registered** | **Placeholder** — every attachment is stored `NotScanned` |
| `IAnalyzerInbound` (lab ASTM/HL7) | **no implementation** | **Placeholder** |
| `IDeviceInbound` (ICU devices) | **no implementation** | **Placeholder** |
| `INoShowJob`, `IReportRefreshJob`, `IIpBedChargeRefreshJob` | Hangfire recurring | Real |

**No shared service exists for:** notification delivery, event publication, outbox dispatch,
document access logging, digital signature, rule evaluation, or caching.

---

## 7. Critical workflows (state machines already in code)

These are the transactional, approval-gated flows an auditor would ask about. All are implemented as
explicit action endpoints (never a status PATCH), which is a significant asset for audit
reconstruction:

| Workflow | States / gates | Approval captured? | Reason captured? |
|---|---|---|---|
| Bill discount | request → approve/reject (threshold-driven) | Yes (`DiscountApproval`) | Yes |
| Refund | request → approve → issue / reject | Yes (`Refund`) | Yes |
| Credit note | request → approve/reject | Yes | Yes |
| Bill cancellation | reason-coded cancel | Actor only | Yes |
| Day close / cashier shift | transactional close, mode-wise amounts | Actor only | n/a |
| Lab result | entry → verify → approve; critical alert → acknowledge | Yes (verifier + approver + timestamps) | Rejection reason on samples |
| Radiology report | entry → verify → approve | Yes | — |
| Stock adjustment | request → approve/reject | Yes | Yes |
| Stock transfer | dispatch → receive | Both actors | — |
| Indent | request → approve → issue | Yes | Yes |
| IPD admission request | create → approve/reject | Yes | Yes |
| IPD discharge | request → 4-way clearance (billing/pharmacy/lab-rad/nursing) → summary → **doctor approval** → final discharge | Yes (`DischargeSummary.IsApproved`) | — |
| OT surgery | Scheduled → InProgress → Completed, **gated by WHO checklist phases + consent verification** | Checklist actors | Cancellation reason |
| Insurance pre-auth | request → approve/reject, + enhancement sub-flow | Yes | Yes |
| Insurance claim | submit → review → approve/partial/deny → settle | Yes | Denial reason master |
| MRD file request | request → approve/reject → issue → return | Yes | Yes |
| Quality CAPA | open → complete → verify | Yes (`VerifiedByUserId`) | Verification notes |
| Patient merge | transactional FK re-point | Actor | Recorded in `PatientMerge` |

**Gap:** every one of these records *who* and *when* on the entity itself, and the row-diff lands in
`audit.audit_log` — but the audit row cannot be tied back to the patient or encounter without
re-joining through the entity, and the *reason* text lives on the business row, not the audit row.

---

## 8. Audit boundaries

### 8.1 What exists

Two independent trails, both in schema `audit`:

**`audit.audit_log`** — written by `AuditSaveChangesInterceptor` for every `IAuditable` insert/update
(and soft delete, recorded as `Deleted`), in the same transaction as the change. Changed columns
only, `jsonb` old/new values.

```
Id, UserId, BranchId, EntityName, EntityId, Action, OldValues, NewValues,
TraceId, IpAddress, CreatedAt
```

**`audit.activity_log`** — written explicitly through `IActivityLogger` for ~120 business/security
event types (login, cancellations, approvals, merges, discharges, settlements…).

```
Id, UserId, Username, BranchId, EventType, Description, IpAddress, UserAgent, CreatedAt
```

Sound decisions already made: `RefreshToken`, `UserSession`, `PasswordHistory` are excluded from the
diff log; `PasswordHash` is column-excluded; audit-column churn (`CreatedAt`/`UpdatedBy`…) is
excluded so diffs stay business-relevant; insert diffs are flushed after key generation inside the
same transaction; failures discard pending audit state.

### 8.2 What is missing (this is F1, the roadmap's critical path)

| Missing dimension | Impact on the requested Patient Audit Journey |
|---|---|
| `PatientId` | **Cannot filter the audit trail by patient at all.** |
| `EncounterId` / encounter type | Cannot scope to a visit or admission |
| `CorrelationId` | Cannot chain registration → order → result → bill as one causal thread |
| `SessionId`, `DeviceId`, `UserAgent` (on `audit_log`) | Partial "from where" answer only (`IpAddress` exists) |
| `ActorRole`, `DepartmentId` | Cannot answer "which role/department did this" without a point-in-time role lookup — and role assignments change |
| `Reason` | Reasons are captured on business rows, not on the audit event |
| `ApprovalId` | Approval and the approved change are separate rows with no link |
| `EventType` semantics on `audit_log` | Only Created/Updated/Deleted — no clinical meaning |
| Structured subject on `activity_log` | `Description` is free text; the patient/entity is embedded in prose, not in columns |
| Document/PDF **access** logging | 25 file-returning endpoints; **no read is logged anywhere**. "Who opened this record" is unanswerable today. |
| Query surface | `AuditLogQuery` filters on user / entity name / action / date / text-search over entity name+id only |

### 8.3 Immutability posture

- No database triggers, rules, or revoked grants protect `audit_log` / `activity_log` — grep of all
  55 migrations finds no `TRIGGER`, `RULE`, or `REVOKE`.
- Both are mapped as ordinary EF entities on a writable `DbContext`. No API exposes update/delete,
  but the application account has full DML rights on them.
- Neither table is hash-chained, signed, or exported to append-only storage.
- No retention policy, archival, or partitioning exists on either table — they grow unbounded in the
  transactional database and are already written on **every** insert/update in the system.

**Verdict:** append-only by *convention and discipline*, not by *construction*. That is defensible
internally and not defensible to an external auditor. Correcting this is cheap and should be Phase 1.

---

## 9. Security boundaries

### 9.1 Strengths (verified in code)

- **Deny-by-default:** `FallbackPolicy = RequireAuthenticatedUser()`. Anonymous surface is limited to
  login/refresh/portal-auth/health, plus OpenAPI + Scalar + the Hangfire dashboard **in Development
  only**.
- **Permissions are not in the JWT.** `PermissionAuthorizationHandler` resolves `MODULE.RESOURCE.ACTION`
  against the database per request, so a revocation takes effect immediately. 405 codes, seeded from
  a single registry, with per-user overrides on top of roles.
- **Server-side session control:** `SessionValidationMiddleware` rejects tokens whose session was
  revoked or idle-expired (configurable idle timeout), throttling activity writes to 1/min.
- **Two JWT audiences.** A staff token can never satisfy a `[PortalAuth]` endpoint or vice versa —
  the patient portal is a genuinely separate auth surface, not a role inside the staff model.
- **Branch isolation is structural**, not per-query: a global EF filter restricts every
  `IBranchScoped` read to the caller's branch; cross-branch requires explicit `IgnoreQueryFilters()`
  plus `REPORT.CROSSBRANCH.VIEW` (used in exactly 4 places).
- 15-minute access tokens with rotating **hashed** refresh tokens and reuse detection
  (`TOKEN_REUSE_DETECTED` activity event).
- Path-sanitized file storage (segment sanitization + GUID-prefixed filenames) — no path traversal.
- DTO-only boundaries with a FluentValidation filter on every request.

### 9.2 Weaknesses

| # | Weakness | Severity | Evidence |
|---|---|---|---|
| S1 | **No document access logging** on 25 file/PDF endpoints | High (privacy) | No `IActivityLogger` call on any download path |
| S2 | **No MFA and no MFA-ready schema** — zero references to MFA/TOTP anywhere; portal OTP is a documented placeholder | High | grep: no matches |
| S3 | **No virus scanning** — `IVirusScanner` has no registered implementation; every attachment stores as `NotScanned` | High | `DependencyInjection.cs` never registers it |
| S4 | **Rate limiting covers only portal auth** — staff login, and every other endpoint, is unlimited | Medium | `Program.cs` — one `PortalAuth` policy |
| S5 | **Branch filter silently disables when `BranchId` is null** — background jobs, seeding, and any user without a branch read **all branches** | Medium | `AppDbContext.OnModelCreating`: `noBranchContext OR branchMatches` |
| S6 | **No department-level or patient-ownership isolation** — any user with `MODULE.RESOURCE.VIEW` sees every patient in the branch | Medium | No such predicate exists in any service |
| S7 | **No auditor role and no read-only permission model** — permissions are per-module verbs; nothing prevents an "auditor" role from also being granted write codes | Medium | `DefaultRoleDefinitions.cs` has no Auditor |
| S8 | Files stored unencrypted on a local volume; no object storage, no per-file access control beyond endpoint permission | Medium | `LocalFileStorageService` |
| S9 | Hangfire dashboard is `AllowAnonymous` (Development only) and Hangfire shares the business database | Low/Medium | `Program.cs` |
| S10 | No idempotency keys on any mutating endpoint — a client retry can double-post a payment | Medium | no `Idempotency` anywhere |
| S11 | Digital signature is a placeholder path column on `LabResult`/`RadReport`; nothing writes it, no DSC/PKI | Medium (regulatory) | entity comments |
| S12 | **No integration tests exist** despite the stated Definition of Done — `Prixo.Api.IntegrationTests` contains one empty test, though the Testcontainers package is referenced. 541 unit tests exist in `Prixo.Application.Tests`. | Medium | `tests/Prixo.Api.IntegrationTests/UnitTest1.cs` |

Serilog request logging is enabled; no evidence of PHI being logged deliberately, but there is **no
redaction policy or sink filter**, so an exception carrying a DTO could serialize patient data into
logs. This should be pinned down before any log shipping is introduced.

---

## 10. Integration boundaries

**The stated "HL7 / FHIR / DICOM interoperability" capability does not exist in code.** What exists:

| Integration | Reality |
|---|---|
| HL7 / FHIR | Zero implementation. One comment on `IAnalyzerInbound` mentioning ASTM/HL7 as a future listener. |
| DICOM / PACS | Placeholder columns only (`RadOrder.AccessionNumber` — generated; `PacsStudyUid` — never written) + a `PACS viewer URL` setting seeded empty |
| ABHA / ABDM | Placeholder identifier types on `PatientIdentifier` |
| SMS / Email / Push | **None.** `admin.notification_queue` has one producer, zero dispatchers |
| Payment gateway | `billing.online_payment_requests` outbox — queues intent, no gateway |
| Lab analyzers | `IAnalyzerInbound` interface, no implementation |
| ICU devices | `IDeviceInbound` interface + `icu.device_reading` table, no implementation |
| Report subscriptions | `reporting.report_subscriptions` outbox, no dispatcher |
| Finance posting | `IFinanceLedgerPoster` — real interface, deliberate no-op until accounts configured |

This is a **consistent and honest placeholder posture** (schema now, activation later) and it is
exactly the right foundation — but the current architecture has **no outbound integration runtime at
all**: no HTTP client policy, no retry/backoff, no circuit breaker, no dead-letter handling, no
outbox dispatcher. The first real integration (SMS, most likely) will have to build that runtime.

---

## 11. Scalability risks

| # | Risk | Detail |
|---|---|---|
| R1 | **Single database, single writer** | 245 tables, all OLTP + reporting on one PostgreSQL instance, plus Hangfire job storage in the same DB. No read replica, no connection pooler configured. |
| R2 | **Reporting competes with transactions** | 3 materialized views refresh every 15 min via `REFRESH … CONCURRENTLY` on the primary; all other reports query live OLTP tables |
| R3 | **Audit write amplification** | Every insert/update writes an extra `audit_log` row inside the same transaction; insert diffs force a second `SaveChanges` (and sometimes an extra transaction). No partitioning, no archival, unbounded growth. |
| R4 | **In-process Hangfire** | Background jobs share the API process, its memory, and its DB pool. Scaling the API horizontally would run recurring jobs on every replica unless configured otherwise. |
| R5 | **Single-node file storage** | `LocalFileStorageService` writes to a local volume — the API cannot be horizontally scaled without shared/blob storage |
| R6 | **In-memory aggregation** | EMR `TimelineService` issues 8 separate queries per patient and merges/sorts in memory (documented as acceptable at one-patient scale — it will not hold for an audit journey spanning encounters + audit events) |
| R7 | **Per-request permission lookup** | `HasPermissionAsync` hits the database on every authorized call; no cache layer exists |
| R8 | **No caching tier at all** | No Redis, no in-memory cache for masters/settings — reference data is re-read constantly |
| R9 | **Bed-board and stock contention** | Concurrency-safe bed allocation and FEFO stock allocation are the two hottest lock paths; no queueing or partitioning strategy |
| R10 | **No retrying execution strategy** | Transient failures on long financial transactions fail outright, with no idempotency to retry safely |

None of these is urgent at single-hospital scale. R3 (audit growth) and R5 (file storage) are the two
that become blocking soonest — R3 because the roadmap will *increase* audit volume substantially.

---

## 12. Microservice candidate analysis

Scored per the brief's rubric (1–5 each, 9 dimensions, max 45). Score reflects *today's* code, not a
target state. Classification: **0–15** keep in modular monolith · **16–25** prepare boundary ·
**26+** strong candidate.

| Candidate | Bus.Indep | DB Indep | Deploy Indep | Scaling | Team Own | Fail Isol | Integ Cx (inv) | Data Own | Txn Cx (inv) | **Total** | Verdict |
|---|---|---|---|---|---|---|---|---|---|---|---|
| **Notification** (new) | 5 | 5 | 5 | 4 | 4 | 5 | 5 | 5 | 5 | **43** | **Strong — build standalone** |
| **Audit & Evidence** (new) | 5 | 5 | 4 | 5 | 4 | 4 | 4 | 5 | 5 | **41** | **Strong — build in-monolith, extract at Phase 10** |
| **Reporting & Analytics** | 4 | 3 | 4 | 5 | 3 | 4 | 4 | 2 | 5 | **34** | Strong — but needs a data pipeline first |
| **Identity & Access** | 4 | 3 | 4 | 3 | 4 | 2 | 3 | 4 | 4 | **31** | Strong on paper; **availability-critical** — extract late |
| **Patient Identity** | 4 | 2 | 3 | 3 | 4 | 2 | 2 | 4 | 3 | **27** | Prepare boundary; extraction blocked by 37 `PatientId` FKs + merge transaction |
| **Laboratory** | 4 | 3 | 3 | 3 | 4 | 3 | 3 | 4 | 4 | **31** | Prepare boundary — clean order/result contract already |
| **Radiology** | 4 | 3 | 3 | 3 | 4 | 3 | 3 | 4 | 4 | **31** | Same shape as Lab; extract as a pair |
| **Biomedical** | 5 | 4 | 4 | 2 | 3 | 4 | 3 | 4 | 3 | **32** | Genuinely independent; low scaling need — extract only for team-ownership reasons |
| **Quality & Compliance** | 5 | 4 | 4 | 2 | 4 | 4 | 3 | 4 | 4 | **34** | Well-isolated; natural home for audit/NABH/NMC |
| **Insurance / TPA** | 4 | 4 | 3 | 2 | 3 | 3 | 2 | 4 | 3 | **28** | Prepare boundary — external-facing, so isolation pays |
| **Pharmacy / Inventory** | 3 | 2 | 2 | 3 | 4 | 2 | 2 | 3 | 1 | **22** | Prepare boundary — 4 modules write its stock ledger |
| **Finance** | 3 | 3 | 3 | 2 | 3 | 3 | 3 | 3 | 2 | **25** | Prepare boundary |
| **Appointments** | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 3 | 4 | **28** | Prepare boundary |
| **MRD** | 4 | 4 | 3 | 1 | 2 | 4 | 3 | 4 | 4 | **29** | Isolated but low value to extract |
| **Billing** | 2 | 1 | 2 | 3 | 3 | 1 | 1 | 2 | 1 | **16** | **Keep** — 7-module fan-in, 4 writers on `bill_item` |
| **Encounter / Clinical (OPD)** | 2 | 1 | 2 | 3 | 3 | 1 | 1 | 2 | 2 | **17** | **Keep** — no encounter abstraction exists yet |
| **IPD** | 2 | 1 | 1 | 2 | 3 | 1 | 1 | 2 | 1 | **14** | **Keep** — load-bearing wall |
| **Nursing** | 2 | 1 | 1 | 2 | 3 | 1 | 2 | 2 | 3 | **17** | **Keep** — inseparable from IPD today |
| **ICU** | 1 | 1 | 1 | 2 | 2 | 1 | 2 | 1 | 3 | **14** | **Keep** — is IPD with extra columns |
| **OT** | 3 | 2 | 2 | 1 | 3 | 2 | 1 | 3 | 1 | **18** | **Keep** — 3-module transaction on completion |
| **Emergency** | 2 | 2 | 2 | 2 | 3 | 1 | 1 | 2 | 2 | **17** | **Keep** — a workflow over IPD/Lab/Billing |
| **EMR** | 2 | 1 | 2 | 3 | 3 | 2 | 2 | 1 | 5 | **21** | Prepare boundary — read-only aggregator, could become a query service |
| **Patient Portal / Doctor Portal** | 1 | 5 | 3 | 3 | 2 | 2 | 1 | 5 | 5 | **27** | **Not a service** — they are façades; extracting them yields a BFF, which is a valid but separate decision |

### Reading

- **Nothing in the current clinical core scores as a microservice.** The three highest business-value
  extractions (Notification, Audit/Evidence, Reporting) are precisely the capabilities that are
  missing or thin today — which is convenient: they can be built with a service-shaped boundary from
  day one instead of being carved out later.
- The clinical core (OPD/IPD/Nursing/ICU/ER/OT) scores 14–18 across the board. **It should remain a
  single deployable unit for the foreseeable future.** Splitting it would convert local transactions
  into distributed sagas across bed allocation, order sheets, and billing — for a hospital, that
  trades a real safety property for an architectural preference.
- Lab, Radiology, Quality, and Biomed are the realistic *second wave*, and only after events and
  observability exist.

---

## 13. Technical debt register

| # | Item | Type | Effort | Priority |
|---|---|---|---|---|
| D1 | Audit event model lacks patient/encounter/correlation/reason/session (F1) | Architecture | L | **P0** |
| D2 | Audit tables not immutable at DB level (F4) | Security | S | **P0** |
| D3 | No document/PDF access logging (S1) | Privacy | M | **P0** |
| D4 | No unified Encounter abstraction (F2) | Architecture | L | **P1** |
| D5 | No integration tests despite stated DoD (S12) | Quality | M | **P1** |
| D6 | Notification outbox has no dispatcher (F5) | Functional | M | **P1** |
| D7 | No observability beyond console logs (F6) | Ops | M | **P1** |
| D8 | `IVirusScanner` unimplemented; uploads unscanned (S3) | Security | S | **P1** |
| D9 | No MFA / MFA-ready schema (S2) | Security | M | **P1** |
| D10 | `IAppDbContext` exposes all 300 DbSets — zero module data isolation | Architecture | XL | P2 |
| D11 | Local-disk file storage blocks horizontal scaling (R5) | Ops | M | P2 |
| D12 | No idempotency keys on mutating endpoints (S10) | Reliability | M | P2 |
| D13 | Audit table growth unmanaged — no partitioning/archival (R3) | Ops | M | P2 |
| D14 | Rate limiting only on portal auth (S4) | Security | S | P2 |
| D15 | Branch filter silently disabled when `BranchId` is null (S5) | Security | S | P2 |
| D16 | No permission/master caching — DB hit per authorized request (R7) | Perf | S | P2 |
| D17 | `UnitOfWork` has no retrying execution strategy (R10) | Reliability | S | P3 |
| D18 | Hangfire in-process + shares business DB (R4) | Ops | M | P3 |
| D19 | `QuestPdfService` is an 890-line monolith of every document layout | Maintainability | M | P3 |
| D20 | `CLAUDE.md` frontend stack description is stale (React 18/RR6 vs actual 19/RR7) | Docs | XS | P3 |
| D21 | 17 stray `*.log` / `*.txt` seeding artifacts committed at repo root | Hygiene | XS | P3 |
| D22 | `DemoDataSeeder` (2104 lines) ships in the API image, gated only by env + config flag | Risk | S | P3 |

---

## 14. Recommended migration strategy

### 14.1 Principle

**Modular monolith → enforced domain boundaries → event-driven communication → extract only what
scores 26+ and has an operational reason.** Strangler Fig throughout: nothing is rewritten; new
capability is built behind the boundary it will eventually own, and existing callers are redirected
one at a time.

The single most important sequencing decision: **the audit/evidence foundation comes before any
service extraction.** It is where the business value is (audit journey, NABH/NMC evidence, safety
engine), it is the capability whose absence is most acute, and its event stream is the same substrate
that a future service split needs.

### 14.2 Recommended order

| Stage | What | Why here |
|---|---|---|
| **0** | This assessment | done |
| **1** | **Audit foundation** — new `audit.audit_event` model carrying patient/encounter/actor-role/department/session/device/reason/approval/correlation/source; make audit tables genuinely append-only (trigger + revoked grants); log document access; add a correlation id to the request pipeline | Unblocks everything in §5–§13 of the brief. Additive: the existing interceptor keeps working. |
| **2** | **Encounter abstraction** — a thin `EncounterRef` (type + id + patient + branch + department + open/close) resolvable from `OpVisit`/`Admission`/`EmergencyVisit`, without merging those tables | Removes the 3-way union from every future feature (D4) |
| **3** | **Patient Audit Journey** — read model + timeline API + auditor UI, sourced from audit events + business rows | First visible deliverable of the new platform principle |
| **4** | **Evidence Vault** — generalize `quality.quality_evidence` (hash, version, retention, access history, polymorphic linkage to patient/encounter/requirement) rather than building a second document store | Reuse, per the brief's "no duplicate systems" rule |
| **5** | **Regulatory framework model** — versioned `RegulatoryFramework` + `Requirement` (NABH, NMC, hospital-defined), mapping requirements → evidence → owner → CAPA, reusing the existing CAPA/risk/non-conformance tables | Configurable, no hard-coded regulation, no compliance claims |
| **6** | **Clinical documentation completeness engine** — configurable required-document sets per encounter type, computed against real rows | Feeds audit readiness scoring with real numbers |
| **7** | **Patient safety engine + audit gap detection** — a rule evaluator over encounter state + audit events | Depends on 1, 2, 6 |
| **8** | **Domain events (in-process first)** — an outbox table + dispatcher inside the monolith; publish the event vocabulary; audit/notification/analytics consume it | Prove the vocabulary before adding a broker. **No broker until a consumer needs cross-process delivery.** |
| **9** | **Notification service** — the first real extraction, because it is new code with zero legacy coupling and a natural async boundary | Score 43 |
| **10** | **Audit & evidence service** — extract once volume justifies separate storage/retention | Score 41 |
| **11+** | Observability → then reassess. Patient Identity, Lab, Radiology only after events and tracing are load-tested | Do not extract blind |

### 14.3 Non-negotiables during migration

- Existing functionality keeps working at every step; no module is rewritten to accommodate the new
  architecture.
- No schema change without an explicit, named migration (existing convention).
- New tables follow the existing conventions exactly: `BaseEntity`/`BranchEntity`, soft delete,
  partial unique indexes, snake_case, schema-per-domain, `numeric(18,2)` money.
- Every new endpoint gets a `MODULE.RESOURCE.ACTION` code in `PermissionRegistry` and a
  `[HasPermission]` attribute.
- The auditor role is **read-only by construction** — a separate permission namespace (`AUDIT.*`)
  with no write verbs, not a role that merely omits them.
- No compliance claims in code, UI, or copy. Vocabulary: "NABH-aligned workflow", "evidence
  available", "evidence gap", "mapped requirement", "hospital-defined requirement".
- No invented NABH or NMC requirement content. The framework ships empty; the hospital loads its
  applicable version, and every audit report records the framework version used.

### 14.4 Explicit non-goals

- Do **not** split the clinical core (OPD/IPD/Nursing/ICU/ER/OT). Score 14–18; the transactions that
  hold it together are patient-safety-relevant.
- Do **not** introduce Kafka. If §8's outbox proves insufficient, PostgreSQL `LISTEN/NOTIFY` or a
  lightweight broker sized to actual volume comes first.
- Do **not** introduce an API gateway while there is one backend service. Revisit at Phase 9.
- Do **not** build a second patient identity, notification, audit, or document store.

---

## 15. Risk analysis of the proposed direction

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Audit volume growth (new event model on top of existing row diffs) | High | High | Partition by month from day one; define retention before go-live; consider writing events async via outbox rather than in-transaction |
| Encounter abstraction leaks into every module | Medium | High | Introduce as an additive resolver/view, never as a rewrite of `OpVisit`/`Admission`/`EmergencyVisit` |
| "Audit readiness score" becomes a vanity metric | Medium | High | Compute only from real rows; every percentage must be drillable to the underlying evidence; never seed defaults |
| Regulatory content drifts / is invented | Medium | **Severe** | Framework ships empty; content is hospital-loaded and version-stamped; every report names the version |
| Auditor role gains write access by permission drift | Medium | High | Separate `AUDIT.*` namespace with no write verbs; assert in tests that the auditor preset contains no write code |
| Event vocabulary churn before extraction | High | Medium | Version events from the first commit; in-process only until stable |
| Extraction attempted before observability | Medium | High | Phase order enforces tracing/metrics before Phase 9 |
| Migration work stalls feature delivery | Medium | Medium | Every phase produces a shipped, user-visible capability — no pure-refactor phases |

---

## 16. Open questions for the product owner

1. **Which NABH edition/version** is the target hospital assessed against, and who supplies the
   requirement list? (The framework will be configurable and ship empty either way.)
2. **NMC scope** — is Prixo deployed at a teaching institution today, or is Medical Education a
   forward-looking capability? This decides whether Residents/Faculty/Rotations need real entities or
   a configurable framework only.
3. **Auditor identity** — internal quality team, external assessor, or both? External access changes
   the security model (time-boxed accounts, separate audience, export-only).
4. **Retention** — how long must audit events and evidence be retained, and does anything need to
   leave the transactional database?
5. **Access-reason prompting** — should break-glass/reason-for-access be enforced hospital-wide, or
   only for flagged (VIP/MLC/psychiatric) records?
6. **Deployment reality** — single hospital, single server today? That determines whether R1/R4/R5
   are urgent or theoretical.

---

## 17. Documents pending approval

This assessment is Phase 0. The remaining architecture documents named in the brief are **not yet
written** and should be produced only after the direction above is approved:

`future-architecture.md` · `microservices-strategy.md` · `service-boundaries.md` ·
`domain-events.md` · `database-boundaries.md` · `audit-architecture.md` ·
`patient-audit-journey.md` · `nabh-framework.md` · `nmc-framework.md` ·
`evidence-management.md` · `patient-safety-engine.md` · `clinical-document-completeness.md` ·
`migration-roadmap.md` · `security-architecture.md` · `observability.md`
