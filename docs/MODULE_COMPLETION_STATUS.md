# Prixo HIS — Module Completion Status

> Derived from `HIS_DEVELOPMENT_PLAN.md` §23 (Development Checklist), cross-checked against
> `CLAUDE.md`'s project-status paragraph and `docs/modules/*.md`. The plan's own header (§0)
> is stale — it still reads "Phases 0–12 complete" from 2026-07-17. This file supersedes that
> header as of **2026-08-08**.
>
> **Update 2026-08-08:** the two Phase 6 gaps below (IP advance auto-drawdown, daily
> bed-charge refresh job) are now closed — see §3a. A CI workflow (`.github/workflows/ci.yml`)
> was also added, closing the Phase 0 gap once this repo has a GitHub remote to run it on.
>
> **Demo data (2026-08-08):** an opt-in `DemoDataSeeder` now populates every module that
> previously shipped empty — see `docs/demo-data.md`. After it runs, only five deliberate
> placeholder/session tables remain empty database-wide.
>
> Status key: ✅ Complete · 🟡 Partially complete (shipped with a documented gap) · ⏳ Open

---

## 1. Headline status

**All 24 numbered phases in the build order are complete**, plus one module (Loyalty Program)
built outside the original 23-phase numbering and one held module (Insurance/TPA, originally
plan §21 F1) pulled forward and built as Phase 24 at the user's explicit direction. As of
2026-08-08, every previously-open item in §3 is closed except CI actually running (needs a
GitHub remote — the workflow file itself is in place).

```
Phase 0  Project Setup            ✅ (CI workflow added — runs once a GitHub remote exists)
Phase 1  Admin + RBAC             ✅
Phase 2  Masters                  ✅
Phase 3  Patient Registration     ✅
Phase 4  Appointments             ✅
Phase 5  OPD                      ✅
Phase 6  Billing                  ✅ (was 🟡 — both gaps closed 2026-08-08, see §3a)
Phase 7  Laboratory                ✅
Phase 8  Pharmacy                  ✅
Phase 9  IPD                       ✅
Phase 10 Nursing                   ✅
Phase 11 EMR                       ✅
Phase 12 Radiology                 ✅
Phase 13 Stores & Inventory        ✅
Phase 14 Operation Theatre         ✅
Phase 15 ICU                       ✅
Phase 16 Emergency                 ✅
Phase 17 MRD                       ✅
Phase 18 Finance & Accounts        ✅
Phase 19 Biomedical Engineering    ✅
Phase 20 Quality / NABH            ✅ (built last, after 21–23)
Phase 21 Patient Portal            ✅ (built ahead of Phase 20)
Phase 22 Doctor Portal             ✅ (built ahead of Phase 20)
Phase 23 Reports & Dashboards      ✅ (built ahead of Phase 20)
Phase 24 Insurance / TPA           ✅ (held module, built ahead of its §21 slot)
—        Loyalty Program           ✅ (new module, not in the original 24-phase plan)
```

Only two things in the entire plan are genuinely open (§4): CI-on-PR (blocked on a GitHub
remote existing) and two IP-billing automation items that shipped as on-demand actions
instead of background jobs.

**Update 2026-08-19 — ABDM / ABHA milestone M1 (Wave-I integration) built.** The Phase-3
Aadhaar/ABHA capture-only placeholder was turned into a real, config-gated ABHA verify/link/
create flow (two-step OTP), pulled forward from the §22 integration roadmap at the user's
direction. It ships inert until ABDM sandbox credentials are configured — the same
"real schema + defined seam, no-op until configured" posture as the Finance/notification
placeholders. See §6 and `docs/modules/patients.md`. M2 (HIP) / M3 (HIU) remain held.

---

## 2. Phase-by-phase, as user stories (from plan §23)

Each row below is a checklist item ("user story") taken verbatim from the plan's Development
Checklist. All are ✅ unless flagged.

### Phase 0 — Project Setup
- ✅ Repo structure, solution layout, ApiResponse/PagedResult wrappers, exception middleware,
  Serilog, EF Core + snake_case, audit interceptor, health checks, Vite/React/Tailwind stack,
  UI kit v1, Docker Compose
- ✅ CI: build + test + lint on PR — `.github/workflows/ci.yml` added 2026-08-08 (backend:
  `dotnet tool restore` → `restore`/`build`/`test` on `Prixo.slnx`; frontend: `npm ci` →
  `npm run lint` → `npm run build`). Runs on push/PR to `main`; only fires once this repo has
  a GitHub remote to host the Actions runner.
- ✅ API container health — `docker/api.Dockerfile` fixed 2026-08-08: the `HEALTHCHECK` now probes
  the real `/health` endpoint via `curl` instead of `dotnet --version`, which needed the absent SDK
  and always exited 155 (the container could never report healthy, breaking any
  `depends_on: service_healthy`). Also drops to the runtime image's built-in non-root `app` user
  via `COPY --chown` instead of `adduser`, which doesn't exist in that image and exited 127.

### Phase 1 — Admin + RBAC
- ✅ Login/logout, JWT + refresh rotation, lockout, password policy, Users/Roles/Permissions/
  Menus CRUD, branch setup, designations, system settings, session management, audit +
  activity logs, seed data

### Phase 2 — Masters
- ✅ Generic master CRUD framework, ~20 catalogue masters (departments, doctors, services,
  tariffs, packages, bed hierarchy, vendor/item/drug masters, tax/discount/referral, ICD-10
  import, corporate + insurance placeholders, UoM/sample-type/modality)

### Phase 3 — Patient Registration
- ✅ **ABDM / ABHA milestone M1** (built 2026-08-19, Wave-I integration pulled forward):
  ABHA verify/link/create via the two-step OTP handshake — `abha_profiles` +
  `abdm_verification_sessions` schema, `IAbdmGateway` seam + config-gated `AbdmGatewayClient`,
  `PATIENT.ABHA.VIEW`/`.MANAGE` permissions, front-desk verify UI. Inert until ABDM sandbox
  credentials are set; M2 (HIP)/M3 (HIU) held. See `docs/modules/patients.md`.
- ✅ Registration wizard, UHID generation, patient search, guardian/contacts, Aadhaar/ABHA +
  photo placeholders, duplicate detection + merge, consent capture, registration-card PDF

### Phase 4 — Appointments
- ✅ Doctor schedule + overrides/leave, slot engine, booking/walk-in/reschedule/cancel, token
  generation + queue display, no-show auto-marking job + reminder outbox placeholder

### Phase 5 — OPD
- ✅ Visit registration + token + slip, new/review auto-detection + billing link, front-desk/
  doctor queues, full consultation workspace (vitals/complaints/history/exam/diagnosis/
  prescription/orders/referral/follow-up)

### Phase 6 — Billing ✅
- ✅ OP bill + receipt (transactional), payment modes + refunds + approval, discount workflow
  with thresholds, credit billing + corporate placeholder, rate revision + cancellation
  workflow, daily collection + day close, unit tests
- ✅ IP advances/deposits + running-bill auto-feed — **closed 2026-08-08.**
  `BillingService.AutoDrawAdvancesAsync` sweeps every available advance on the admission
  against the bill's balance FIFO by `ReceivedAt`, using a new pure/unit-tested
  `BillMath.PlanAdvanceSweep`. Runs automatically inside `RefreshBedChargesInternalAsync` —
  i.e. on IP bill generation, on a manual refresh, and from the new nightly job below.
  `DrawAdvanceAsync` (manual, pick-one-advance) is unchanged and still available.
- ✅ Bed-charge daily job + interim/final bill — **closed 2026-08-08.** `GenerateIpBillAsync`/
  `RefreshBedChargesAsync` still cover interim/final bill generation on demand; a new
  `IpBedChargeRefreshJob` (`IIpBedChargeRefreshJob`, Hangfire `Cron.Daily`, id
  `"refresh-ip-bed-charges"`, registered in `Program.cs`) now recomputes every open IP bill's
  bed-charge lines once a day via `BillingService.RefreshAllOpenIpBedChargesAsync`, each bill
  in its own transaction so one admission's bad state can't block the rest. See
  `docs/modules/billing.md` and `docs/modules/ipd.md` for the full write-up.

### Phase 7 — Laboratory
- ✅ Test/parameter/reference-range/profile masters, orders, barcode + collection +
  acknowledgement + rejection, department worklists + result entry with H/L/critical
  auto-flagging, validate→verify→approve pipeline, critical alerts, cumulative report PDF,
  TAT report, analyzer-interface placeholder

### Phase 8 — Pharmacy
- ✅ Deep drug masters + racks, PR→PO→GRN→return (transactional), OP POS sale with FEFO + GST
  invoice, IP/emergency/department issues, sales + IP returns with append-only stock ledger,
  approval-gated adjustments + transfers + stock audit, near-expiry/min-stock alerts +
  consumption report

### Phase 9 — IPD
- ✅ Admission (IP number, consent, deposit, slip), concurrency-safe bed board + allocation,
  bed/ward/room/consultant transfers with charge cutover, unified doctor order sheet +
  medication orders, progress notes/vitals/I-O/procedure notes, diet-order placeholder,
  discharge request + 4-way clearance, discharge summary + final discharge + slip, bed
  occupancy dashboard

### Phase 10 — Nursing
- ✅ Admission nursing assessment, MAR (schedule generation, administer/skip/refuse,
  high-alert double-verification), shift-tagged notes + vitals + I/O, Pain/Morse/Braden risk
  scores, care plans + SBAR handover, incident reporting + transfer notes

### Phase 11 — EMR
- ✅ Patient timeline aggregation, medical/surgical/family/allergy/immunization histories,
  allergy banner + live prescription cross-check, clinical notes + problem list,
  prescription/investigation/procedure history + results trend chart, attachments +
  consent repository

### Phase 12 — Radiology
- ✅ Test + modality masters, orders + modality scheduling + technician worklist, capture
  status + templated reporting, verify→approve pipeline + PDF report, enforced contrast-
  consent gate, TAT report, PACS/DICOM placeholders

### Phase 13 — Stores & Inventory
- ✅ Store-type + item/category masters, indent→approve→issue, general-store PR/PO/GRN,
  returns/transfers/adjustments/verification counts, consumption + reorder alerts

### Phase 14 — Operation Theatre
- ✅ Theatre/equipment master + conflict-checked booking calendar, team assignment, WHO
  pre-op checklist + consent verification, anaesthesia/surgery/post-op notes, implant +
  consumable capture → stock + billing, OT billing + register report

### Phase 15 — ICU
- ✅ ICU stay + bed allocation, hourly vitals flowsheet + ventilator + infusion charts, ICU
  MAR view + rounds notes, critical alert rules + dashboard, device-integration placeholder

### Phase 16 — Emergency
- ✅ Rapid registration + unknown-patient flow, ESI triage + ER tracker, assessment sheet +
  STAT lab/radiology orders, MLC + accident registers, observation ward + admission
  conversion + transfer-out, emergency billing

### Phase 17 — MRD
- ✅ Record file creation/tracking/issue-return + overdue alerts, document scanning + type
  master, coding worklist + completeness tracking, discharge file audit + missing-doc
  tracking, retention policies + destruction register + record requests, MRD reports

### Phase 18 — Finance & Accounts
- ✅ Chart of accounts + auto-posting rules, journal/payment/receipt vouchers, ledger +
  payables + receivables aging, GST/revenue/outstanding reports, central day-close console

### Phase 19 — Biomedical Engineering
- ✅ Equipment registry + location tracking, AMC/CMC + warranty renewal alerts, PM schedules
  → auto work orders, breakdown complaints + downtime capture, calibration tracking +
  service history + spare usage, downtime + PM-compliance reports

### Phase 20 — Quality / NABH
- ✅ Indicator definitions + auto-pull, KPI dashboard + audit checklists/runs, unified
  incident intake + RCA + non-conformance, CAPA tracker + risk register, committee meeting
  records + NABH evidence library, compliance dashboard
- ✅ Starter indicator seed — added 2026-08-08. `DbSeeder.SeedQualityIndicatorsAsync` seeds 30
  commonly-tracked indicators (`QI-01`…`QI-30`), idempotent by `Code`, `TargetValue` left null,
  `AutoComputeSource` wired only for Bed Occupancy and Lab TAT. Closes "`quality.indicators` ships
  empty, so a hospital must hand-type every indicator before recording any data." See
  `docs/modules/quality.md`.
- *(Deliberately built last, after Phases 21–23, per plan note — none of those three hard-depend on it)*

### Phase 21 — Patient Portal
- ✅ Portal auth (separate JWT audience) + self-registration, appointment booking/cancel,
  approved-only lab/radiology reports + prescriptions + bills + discharge summaries,
  portal-safe medical history + feedback, online-payment placeholder

### Phase 22 — Doctor Portal
- ✅ Doctor layout + dashboard, queue + consult (reused) + IP rounds view, orders/results
  with trends, discharge-summary e-sign inbox

### Phase 23 — Reports & Dashboards
- ✅ `reporting` schema + materialized views + 15-min refresh job, Management/OP/IP/Billing/
  Revenue dashboards, full report catalog with CSV/Excel/PDF export, custom report builder
  v1, cross-branch permission + report-subscription placeholder

### Phase 24 — Insurance / TPA (built ahead of its held slot)
- ✅ `PatientInsurancePolicy` capture (feeds Patient Registration's "Insurance Required" flag)
- ✅ Pre-authorization request → approve/reject, with mid-treatment "enhancement" sub-workflow
- ✅ Claim submission against a finalized bill → review → settlement
- ✅ Settlement posts to Finance via `IFinanceLedgerPoster.PostClaimSettlementAsync`
- ✅ `Bill.InsuranceCompanyId` (a Phase-6 placeholder) finally gets a real writer

---

## 3. Formerly-open items — status as of 2026-08-08

| Item | Phase | Status | Detail |
|---|---|---|---|
| CI: build + test + lint on PR | 0 | ✅ workflow added, ⏳ won't fire until a remote exists | `.github/workflows/ci.yml` — backend build+test on `Prixo.slnx`, frontend lint+build. Nothing to do on the code side; needs `git remote add origin ...` + push |
| IP advances auto-drawdown against running bill | 6 | ✅ closed | `BillingService.AutoDrawAdvancesAsync` + `BillMath.PlanAdvanceSweep` (6 new unit tests in `BillMathTests.cs`) |
| Daily bed-charge refresh job | 6 | ✅ closed | `IpBedChargeRefreshJob` (Hangfire `Cron.Daily`), calling `BillingService.RefreshAllOpenIpBedChargesAsync` |
| Cross-cutting checklist (§23 "every phase") | all | — | The plan lists 8 recurring DoD items (permissions seeded, menus seeded, validators, transactions, audit, tests, module spec, UAT sign-off) as an unchecked template — these are verified per-module in `docs/modules/*.md` rather than ticked in the master plan itself |

**ABDM/ABHA M1 (2026-08-19) vs the cross-cutting checklist:** permissions seeded + `[HasPermission]` on all 4 endpoints ✅; menus — none needed (surfaced inline on the existing Patient Detail page, like the documents/consents panels) ✅ by design; FluentValidation on all 3 request DTOs ✅ (+11 validator unit tests in `Patients/AbdmValidatorTests.cs`); transactions — link/unlink wrapped in `IUnitOfWork` (exceeds the money/stock/admission requirement) ✅; audit — entity audit automatic via the `SaveChanges` interceptor, activity events `PATIENT_ABHA_LINKED`/`_UNLINKED` wired ✅ (the link/unlink activity path is unexercised live because the gateway is config-gated); tests — validator units green (594 total), **no service-flow integration test** (would need a fake `IAbdmGateway` + Testcontainers; the happy path can't run without ABDM creds) ⏳; module spec written in `docs/modules/patients.md` ✅; UAT sign-off on staging — user-owned, staging N/A here ⏳.

### 3a. What actually changed (2026-08-08 batch)

- `backend/src/Prixo.Application/Features/Billing/BillMath.cs` — added `AdvanceBalance`,
  `AdvanceDraw` records and the pure `PlanAdvanceSweep` FIFO-sweep function.
- `backend/src/Prixo.Application/Features/Billing/BillingService.cs` — added
  `AutoDrawAdvancesAsync` (private, called from `RefreshBedChargesInternalAsync`) and
  `RefreshAllOpenIpBedChargesAsync` (public, the job's entry point).
- `backend/src/Prixo.Application/Features/Billing/IpBedChargeRefreshJob.cs` (new) +
  `Common/Interfaces/IIpBedChargeRefreshJob.cs` (new) — the Hangfire job, following the
  existing `NoShowJob`/`ReportRefreshJob` pattern exactly.
- `backend/src/Prixo.Application/Common/Interfaces/IActivityLogger.cs` — added
  `AdvanceAutoDrawn` and `BedChargesAutoRefreshed` activity-log event constants.
- `backend/src/Prixo.Application/DependencyInjection.cs` + `backend/src/Prixo.Api/Program.cs`
  — registered and scheduled the new job (`"refresh-ip-bed-charges"`, daily).
- `backend/tests/Prixo.Application.Tests/Billing/BillMathTests.cs` — 6 new tests for
  `PlanAdvanceSweep` (empty/zero-balance/FIFO-order/partial-coverage cases).
- `.github/workflows/ci.yml` (new) — GitHub Actions CI.
- `docker/api.Dockerfile` — `HEALTHCHECK` now curls `/health` (was `dotnet --version`, which
  always exited 155 without the SDK); non-root user switched to the runtime image's built-in
  `app` via `COPY --chown` (`adduser` doesn't exist there and exited 127).
- `backend/src/Prixo.Infrastructure/Persistence/DbSeeder.cs` — added
  `SeedQualityIndicatorsAsync` (30 starter quality indicators, idempotent by `Code`).
- `docs/modules/billing.md` and `docs/modules/ipd.md` — updated in place, following this
  repo's "Deferred / notes" + dated-extension-note convention.

**Verified:** full solution builds clean (`dotnet build Prixo.slnx`, 0 errors, 0 warnings —
re-verified 2026-08-11); all tests pass — 583 in `Prixo.Application.Tests` (including the 6 new
`PlanAdvanceSweep` cases) plus the 1 placeholder test each in `Prixo.Domain.Tests` and
`Prixo.Api.IntegrationTests`, 585 total; the dev API was restarted and the new
`refresh-ip-bed-charges` recurring job confirmed present in Hangfire's PostgreSQL storage
(`hangfire.set` / `recurring-jobs`) alongside `mark-no-shows` and `refresh-reporting-views`,
with no startup errors. Not separately integration-tested against live admission/advance data
(the dev DB currently has no open IP bills to exercise) — this repo has no Testcontainers
integration-test harness built yet (`Prixo.Api.IntegrationTests` is still a stub), consistent
with the codebase's existing "pure-logic unit tests only" pattern for Billing math.

---

## 4. Modules built beyond the original 24-phase plan

These extended an existing phase's engine rather than opening a new numbered phase — each is
documented inline in the relevant `docs/modules/*.md` file (see `CLAUDE.md` for the full list).
All are ✅ complete:

- **ABDM / ABHA milestone M1** (2026-08-19) — first Wave-I integration pulled forward from §22:
  ABHA verify/link/create via OTP; `IAbdmGateway` seam + config-gated client, no-op until ABDM
  sandbox credentials are supplied; upserts the paired `PatientIdentifier(Abha)` so existing
  reports keep resolving the number. M2/M3 held. See `docs/modules/patients.md`.
- **Loyalty Program** — new module: card category/type/member-limit masters, branch-priced
  card price matrix, registration/renewal/upgrade/dependents, deactivate action
- **Appointments extensions** — Unit Scheduling, Appointment Types, Token Management
  (Skip/Transfer/ER mapping), Tele-consulting tag, Doctors On Duty view, Doctor-Appointment
  Report, Front Office max-advance-booking-days setting
- **OPD/Patients extensions** — MHC Registration, Camp Registration, Doctor Opinion Entry,
  a batch of OP/MHC reports, registration gap-fill (Salutation/Marital Status/WhatsApp/
  Mother's Name/National ID/VIP flag, City/State/Nationality FK conversion), patient-type
  colors, Block/Unblock report
- **Pharmacy** — Pay Type Modify (post-hoc payment-mode correction)
- **OT** — Patient Update (re-point a Scheduled booking's admission/patient)
- **Admin** — Front Office control panel + Consultation-charge settings (as `SettingKeys`,
  not new screens)

---

## 5. Modules still deliberately held (not started)

Per plan §21, everything except Insurance/TPA (now Phase 24) remains held:
Blood Bank, Dialysis, Physiotherapy, Dental, HR & Payroll, Facility/Maintenance, Ambulance,
Kitchen/Dietary, Infection Control, Mobile Apps, Communication, and all AI modules (§22).

**Integrations (§22)** are held *except* **ABDM / ABHA milestone M1**, built 2026-08-19 (see §4)
— the rest remain held: ABDM M2/M3, HL7 FHIR, LIS analyzer, PACS/DICOM, payment gateway, and
WhatsApp/SMS/email. Each still has its schema/interface seam in place (`IAnalyzerInbound`,
`pacs_study_uid`, `NotificationQueue`, `OnlinePaymentRequest`) so activation is a connector,
not a re-architecture.

---

*Generated from `docs/HIS_DEVELOPMENT_PLAN.md` §23 and `CLAUDE.md`, 2026-08-08; updated 2026-08-19 (ABDM/ABHA M1).*
