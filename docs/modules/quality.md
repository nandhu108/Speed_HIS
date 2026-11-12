# Module Spec — Quality / NABH (Phase 20)

Status: **complete**. The last outstanding module in the build order — Phases 21–23 were
deliberately built ahead of it (none depend on Quality/NABH) per the plan's own build-order note.

## New domain (schema `quality`)

| Table | Notes |
|---|---|
| `quality.indicators` | Quality Indicators master (plan: "numerator/denominator definitions"). Global, like the Lab test catalog. `AutoComputeSource` (nullable enum `LabTat`/`BedOccupancy`/`IncidentCount`) drives the explicit "auto-compute" action; null means manual entry only. |
| `quality.indicator_entries` | One monthly data point per indicator/branch/period (`PeriodMonth` normalized to the 1st). `ComputedValue` (Numerator/Denominator) is **computed on read** via `QualityMath.ComputedValue`, not stored — same "compute on read" posture as Biomed's `IsRenewalDue`/`DowntimeMinutes`. Deliberately **not** scaled by 100: the indicator's own Denominator already encodes whether it's a percentage (occupied/totalBeds), a plain average (sum-of-TAT-hours/count) or a raw count (count/1). |
| `quality.audit_templates` / `quality.audit_template_items` | Audit Checklist templates (plan: "templates, scheduled audits, scoring") — ordered questions with a max score each. |
| `quality.audits` / `quality.audit_responses` | A scheduled/conducted run of a template against a branch. `ScorePercent` computed on read (`QualityMath.AuditScorePercent`). |
| `quality.non_conformances` | Findings raised against an NABH standard — distinct from Incident (a patient/staff-safety event); this is an accreditation-gap finding. Open → InProgress → Closed. |
| `quality.risks` | Risk Register. `RiskScore` (Likelihood × Impact, each 1–5) computed on read (`QualityMath.RiskScore`). Identified → Mitigating → Closed. |
| `quality.capas` | CAPA tracker (plan: "action, owner, due, verification"). **Polymorphic** source — `CapaSourceType` (Incident/NonConformance/Audit/Risk) + `SourceId`, no FK — same untyped-association idiom as `BillItem.SourceType`/`SourceId`. Open → InProgress → Verified → Closed. |
| `quality.committees` | Simple master (Quality Assurance Committee, Infection Control Committee, …) — plugs into the existing generic `SimpleMasterService<T>`/`SimpleMasterControllerBase<T>` framework unchanged. |
| `quality.committee_meetings` / `quality.committee_action_items` | Committee Meeting Records (plan: "minutes, action items"). Action items are a shallow child list — deliberately **not** routed through the CAPA tracker, which the plan lists as its own bullet. |
| `quality.nabh_standards` | Simple master for chapter/standard reference codes. Convention: Code is `"<CHAPTER>_<STANDARD>"` (e.g. `AAC_1`) — the chapter is derived on read (`QualityMath.ChapterOf`, split on `_`) rather than a stored column, since the shared simple-master code validator only allows `[A-Z0-9_-]` (no `.`). |
| `quality.evidence` | NABH Evidence Upload (plan: "chapter/standard-wise document repository"). File stored via `IFileStorageService`, same idiom as MRD's `ScannedDocument`/Biomed's `Calibration` certificate. |

Migration: `Phase20_Quality`.

## Merging Incident into the org-wide register — widened, not forked

The plan's "Incident Reporting (org-wide intake — merges nursing incidents)" is implemented by
**widening the existing `Nursing.Incident` entity in place** (`nursing.incidents` table) rather
than forking a parallel `quality.incidents` table — the same "widen, don't fork" posture ICU used
on `IpVitals` and Stores used on `Batch`. Three nullable columns were added: `RootCauseAnalysis`,
`IsSentinelEvent`, `SourceModule`. Nursing's existing `IncidentStatus` state machine
(Report→UnderReview→Closed) and every existing `IncidentService` method are untouched — the only
addition is `IncidentService.RecordRcaAsync(...)`, gated by a new `QUALITY.INCIDENT.RCA`
permission, which sets the two Quality-owned fields without touching Status. The Quality Manager
role is additionally granted Nursing's own `NURSING.INCIDENT.{VIEW,CREATE,REVIEW}` permissions, so
it operates the same screens/endpoint Nursing already built — no parallel incident engine exists
anywhere.

## Reuse, not a parallel module

- `QualityIndicatorService.AutoComputeAsync` reuses `LabOrderService.GetTatReportAsync` (averages
  `TurnaroundHours`) and `BedBoardService.GetOccupancyDashboardAsync` (occupied/total) directly —
  cross-feature service injection, the same pattern `DoctorPortalService` uses on
  `OpVisitService`/`DischargeService`.
- `QualityCommitteeService`/`QualityNabhStandardService` extend the generic `SimpleMasterService<T>`
  exactly like `UomService`/`SampleTypeService`; their controllers extend
  `SimpleMasterControllerBase<T>` exactly like `DepartmentsController`.
- No new "assign owner" picker — reuses Admin's existing `useUsers` list, same as Biomed.

## Endpoints (`/api/v1/quality`)

- `indicators`: `GET` (search), `POST`, `PUT {id}`; `indicators/{id}/entries`: `GET`, `POST`;
  `indicators/{id}/entries/auto-compute`: `POST`
- `audit-templates`: `GET`, `POST`; `audits`: `GET` (paged, status filter), `POST` (schedule),
  `{id}/score`, `{id}/complete`: `POST`
- `non-conformances`: `GET` (paged), `POST` (raise), `{id}/in-progress`, `{id}/close`: `POST`
- `capas`: `GET` (filterable status/sourceType), `POST` (raise), `{id}/complete`, `{id}/verify`,
  `{id}/close`: `POST`
- `risks`: `GET` (filterable status), `POST`, `{id}/status`: `POST`
- `committees`: generic simple-master `GET`/`POST`/`PUT {id}`/`DELETE {id}`
- `committee-meetings`: `GET` (filterable committeeId), `POST`;
  `{meetingId}/action-items/{actionItemId}/complete`: `POST`
- `nabh-standards`: generic simple-master `GET`/`POST`/`PUT {id}`/`DELETE {id}`
- `evidence`: `GET` (filterable standardId), `POST` (multipart upload), `{id}/file`: `GET` (download)
- `dashboards/kpi`, `dashboards/compliance`: `GET`
- Plus one addition to the existing Nursing controller: `nursing/incidents/{id}/rca`: `POST`

## Permissions

`QUALITY.INDICATOR.{VIEW,MANAGE}`, `QUALITY.INDICATORENTRY.RECORD`,
`QUALITY.AUDITTEMPLATE.{VIEW,MANAGE}`, `QUALITY.AUDIT.{VIEW,MANAGE}`, `QUALITY.INCIDENT.RCA`,
`QUALITY.NONCONFORMANCE.{VIEW,CREATE,MANAGE}`, `QUALITY.CAPA.{VIEW,MANAGE}`,
`QUALITY.RISK.{VIEW,MANAGE}`, `QUALITY.COMMITTEE.{VIEW,MANAGE}` (also covers the NABH standards
master — a small reference table administered alongside committees, not worth its own code),
`QUALITY.EVIDENCE.{VIEW,UPLOAD}`, `QUALITY.DASHBOARD.VIEW`.

## Frontend (`src/features/quality`)

- `IndicatorsPage` (`/quality/indicators`) — list + create; row click opens an entries modal
  (record manually or auto-compute for a period).
- `AuditsPage` (`/quality/audits`) — template creation, schedule audit, paged run list; row click
  opens a scoring modal.
- `IncidentRegisterPage` (`/quality/incidents`) — org-wide register reusing Nursing's own
  `useIncidents`/`useRecordIncidentRca` hooks outright (no parallel data fetching), with a
  sentinel-only filter and an RCA modal.
- `NonConformancesPage` (`/quality/non-conformances`), `CapaTrackerPage` (`/quality/capas`),
  `RiskRegisterPage` (`/quality/risks`) — raise + status-transition actions.
- `CommitteeMeetingsPage` (`/quality/committee-meetings`) — minutes + action items;
  `CommitteesPage` (`/quality/committees`) and `NabhStandardsPage` (`/quality/nabh-standards`) are
  one-line instances of the existing generic `SimpleMasterPage` component (`features/masters`),
  the same way `UomsPage`/`SampleTypesPage` are.
- `NabhEvidencePage` (`/quality/evidence`) — upload (multipart) + download.
- `QualityDashboardPage` (`/quality/dashboard`) — tabbed Compliance (open NC/CAPA/risk counts,
  overdue CAPAs, YTD sentinel events, average audit score, evidence-by-chapter) / KPI (12-month
  indicator trend vs target).

## Quality Indicators starter seed (2026-08-08)

`quality.indicators` shipped empty since Phase 20 — a hospital had to type every indicator in
by hand via the Indicators screen before it could record any monthly data. `DbSeeder.SeedQualityIndicatorsAsync`
now idempotently seeds 30 rows (`QI-01`…`QI-30`) covering the categories most Indian hospital
quality/NABH programs track: Facility & Operations, Clinical Care, Patient Experience, Human
Resources, Emergency Care, Surgical Care, Patient Safety, Medication Safety, Staff Safety,
Infection Control, Diagnostics, Medical Records, Quality Management.

**This is explicitly not a verbatim reproduction of any NABH edition's Objective Element
text, numbering, or mandated target values** — it's the widely-recognized indicator set
(bed occupancy, ALOS, HAI rates by type, falls, medication errors, TAT, discharge-summary
completion, etc.) offered as a reviewable starting point. A Quality Manager should treat it
as a checklist to review, rename, retarget, or delete via the existing Indicators screen —
same idempotent-by-`Code` posture as `SeedRetentionPoliciesAsync`, so re-running the seeder
never overwrites or re-adds a row an admin has already edited or removed. `TargetValue` is
left `null` on every seeded row on purpose: the acceptable threshold is a hospital
policy/Quality Committee decision, not something the seed should invent. Before using this
list as NABH assessor evidence, verify wording, applicability, and targets against the
hospital's official NABH 6th Edition manual.

Only two rows get `AutoComputeSource` wired, and only because Prixo already has a matching
engine whose output maps 1:1 onto the indicator's stated numerator/denominator:
`QI-01` (Bed Occupancy Rate → `BedOccupancy`) and `QI-24` (Average Laboratory Turnaround
Time → `LabTat`). Every other row — including the ones that *sound* like they could reuse
`IncidentCount` (Patient Falls, Medication Errors, Needle-Stick Injuries) — is left manual on
purpose: `IncidentCount` currently returns the hospital's *total* incident count for the
period regardless of category, so wiring it to a category-specific indicator would silently
record the wrong number in what's meant to be a compliance record. Widening `IncidentCount`
to filter by category (the `Incident` entity already carries one) is a natural follow-up if
per-category auto-compute becomes worth building.

## Deferred / notes

- **No background job for auto-compute** — same explicit-action posture as Biomed's PM "generate
  due work orders" button; a user must click "Auto-compute for this period."
- **Frontend not exercised in a real browser** — verified via `tsc -b && vite build` (clean) and
  `oxlint` (clean, only 3 pre-existing warnings unrelated to this module); no browser tool is
  available in this environment.
- See `docs/modules/admin.md` for the full seeded-role catalogue (Part B of this phase's work),
  including the Quality Manager role's permission bundle.
