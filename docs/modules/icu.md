# Module Spec — ICU / Critical Care (Phase 15)

Status: **complete** (2026-07-18). Extends IPD and Nursing wholesale rather than modeling a
parallel "ICU stay" concept — an ICU patient is just an `Admission` whose current bed sits
on a `Ward.IsIcu` ward (that flag was already added to Masters back in Phase 9,
"Phase 15 allocates ICU stays to these"). Adds ventilator and infusion charting, a
clinician-assigned severity assessment, hospital-configurable critical-alert threshold
rules evaluated against vitals, and an ICU census/ventilated-count/severity dashboard.

## Reuse, not a parallel module

Per the plan's "extends IPD + nursing" dependency note, most of Phase 15's checklist needed
zero new backend:

- **ICU Admission / transfer-in, ICU Bed Allocation** — IPD's existing `Admission` +
  `BedAllocation` + bed-transfer flow (Phase 9), filtered by `Ward.IsIcu`. No new entity.
- **ICU Nursing Notes** — Nursing's existing `NursingNote` (Phase 10), just viewed against
  an ICU-ward admission.
- **Doctor Rounds notes** — IPD's existing `ProgressNote` (Phase 9).
- **Medication Chart (ICU MAR view)** — Nursing's existing `MarSchedule`/`MarAdministration`
  (Phase 10).

## Database (schema `icu`)

| Table | Notes |
|---|---|
| `ipd.ip_vitals` (widened) | Four new nullable columns — `GlasgowComaScale`, `MeanArterialPressure`, `CentralVenousPressure`, `UrineOutputMl` — added directly to Phase 9's `IpVitals` rather than a parallel "ICU vitals" entity, so a patient's vitals timeline stays on one table across a ward↔ICU bed transfer. Usable by any ward; only ICU's recording endpoint (below) also evaluates alert rules. |
| `icu.ventilator_sessions` / `ventilator_observations` | One row per continuous ventilation episode (`StoppedAt IS NULL` = currently ventilated — what the dashboard's "ventilated count" counts) plus periodic flowsheet rows carrying both settings and observed pressures together, the same way a real ICU ventilator chart round is documented. |
| `icu.infusion_chart_entries` / `infusion_rate_entries` | Same session + flowsheet split as the ventilator chart. `DrugId` links to the catalog when the infused agent is a Drug; `DrugNameOverride` covers non-catalog fluids. |
| `icu.icu_severity_assessments` | A clinician-assigned acuity level (Mild/Moderate/Severe/Critical) per point in time — not a computed score (no APACHE II/SOFA in this phase), giving the dashboard a severity history per stay. |
| `icu.critical_alert_rules` | Branch-scoped, hospital-configurable vitals thresholds (Parameter/Operator/ThresholdValue/Severity) — CLAUDE.md's "no hardcoded hospital data" applies here exactly as it does to Tariffs. |
| `icu.icu_vitals_alerts` | A rule breach raised against one `IpVitals` reading, with the same acknowledge workflow shape as Lab's `CriticalAlert` (Phase 7) — a distinct entity because the domain (vitals vs. lab result values) is genuinely different, not a forced reuse. |
| `icu.device_readings` | Placeholder table only (plan: "Device Integration (placeholder only — device_reading table + IDeviceInbound interface)") — nothing writes to it yet. |

Migration: `Phase15_Icu`.

## Critical alert evaluation happens where the write happens, not via cross-module calls

No Application feature service anywhere calls into a sibling feature's service (confirmed
absent as a pattern when Phase 14 needed the same kind of composition) — every cross-module
interaction in this codebase reaches directly into the other module's entities/DbSets
instead. `IcuVitalsService.RecordVitalsAsync` therefore writes the exact same `IpVitals` row
IPD's `ClinicalChartService.AddVitalsAsync` does (the ~10 lines are duplicated rather than
calling into IPD's service), then evaluates every active `CriticalAlertRule` for the branch
against the new reading via the pure `IcuMath.ExtractValue`/`IsBreached` helpers, raising an
`IcuVitalsAlert` per breach — all before the transaction that started with the vitals write
returns. IPD's own ward-level vitals endpoint keeps working exactly as before and doesn't
know ICU's alert-rule schema exists.

`IcuMath` (pure, no I/O) is unit-tested directly — same "BillMath/OtMath" posture as every
other module's pure-logic class.

## Ventilator/infusion charts gate against a live session

`IcuChartService.AddVentilatorObservationAsync`/`AddInfusionRateAsync` both reject charting
against a session/entry that's already `StoppedAt`-set ("Cannot chart against a stopped
ventilator session") — the flowsheet only accepts rows while its parent session is running,
same "can't act on a closed document" posture used throughout the codebase (e.g. OT's
checklist gates, Billing's cancelled-bill guard).

## Endpoints (`/api/v1/icu`)

- `alert-rules`: `GET`, `POST`, `PUT {id}`
- `admissions/{id}/vitals`: `POST` (ICU-specific — records vitals *and* evaluates rules;
  reads still go through IPD's existing `GET /ipd/admissions/{id}/vitals`)
- `alerts`: `GET?onlyUnacknowledged=`, `alerts/{id}/acknowledge`: `POST`
- `admissions/{id}/ventilator-sessions`: `GET`, `POST`;
  `ventilator-sessions/{id}/stop`: `POST`; `ventilator-sessions/{id}/observations`: `POST`
- `admissions/{id}/infusions`: `GET`, `POST`; `infusions/{id}/stop`: `POST`;
  `infusions/{id}/rates`: `POST`
- `admissions/{id}/severity`: `GET`, `POST`
- `dashboard`: `GET` (census, ventilated count, per-patient severity/alert summary)

## Permissions

`ICU.RULE.{VIEW,MANAGE}`, `ICU.VITALS.RECORD`, `ICU.ALERT.{VIEW,ACKNOWLEDGE}`,
`ICU.CHART.{VIEW,MANAGE}` (ventilator/infusion/severity), `ICU.DASHBOARD.VIEW`.

## Frontend

- `IcuDashboardPage` (`/icu/dashboard`) — census/ventilated/unacknowledged-alert stat tiles
  plus a per-patient table (ward/bed, ventilated Y/N, latest severity, open alert count),
  each row linking straight to that admission's detail page.
- `AlertsPage` (`/icu/alerts`) — breach list with an unacknowledged-only toggle and an
  acknowledge-with-notes modal.
- `AlertRulesPage` (`/icu/alert-rules`) — threshold rule CRUD.
- `AdmissionIcuTab` (`src/features/icu/pages/AdmissionIcuTab.tsx`) — **not** a standalone
  route; it's a new "ICU" tab embedded directly into IPD's existing `AdmissionDetailPage`
  (one import, one render line, kept in its own file so that already-1500-line page didn't
  grow further) — ICU vitals recording, the ventilator card (start/stop/chart an
  observation), the infusion card (start/stop/titrate), and the severity card, all scoped to
  the admission being viewed.

## Deferred / notes

- **No calendar/flowsheet-grid vitals view** — the "high-frequency vitals grid, hourly" plan
  wording is served by IPD's existing chronological vitals table (now carrying the four ICU
  fields), not a dedicated dense hourly grid widget.
- **No APACHE II/SOFA computed severity score** — `IcuSeverityLevel` is a plain clinician
  call, not derived from sub-scores, an intentional scope cut flagged in the entity's own
  doc comment rather than inventing a scoring system the plan didn't ask for.
- **Device Integration is schema + interface only** — `DeviceReading`/`IDeviceInbound` exist
  so a future device-integration phase has something to implement against, exactly like Lab's
  `IAnalyzerInbound` (Phase 7) and EMR's `IVirusScanner` (Phase 11); nothing calls either yet.
- **Ward-level vitals entry doesn't expose the four new ICU fields in its UI** — the fields
  exist on the shared `IpVitals` row and are visible in the data either way, but only the
  ICU tab's form has inputs for them; the general ward `VitalsTab` UI is unchanged.
