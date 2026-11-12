# Module Spec — Biomedical Engineering (Phase 19)

Status: **complete** (2026-07-20). Per the plan, Biomedical is independent of the other
held-back dependency chains ("independent; slots here" — plan §12) — it's a self-contained
asset/maintenance-management module that only reaches into Pharmacy for spare-parts stock.

## New domain (schema `biomed`)

| Table | Notes |
|---|---|
| `biomed.equipment` | Equipment Master (plan: "serial, model, criticality, department/location"). C# type is `BiomedEquipment` — plain `Equipment` collided with OT's existing theatre-equipment master (`Prixo.Domain.Entities.Ot.Equipment`); the table/DB name is still `equipment`, scoped by the `biomed` schema, so there's no real naming problem, only a C#-namespace one. `CurrentLocation` is a snapshot kept in sync by `equipment_location_logs`. |
| `biomed.equipment_location_logs` | Location tracking — an append-only movement log, the same shape as MRD's `FileMovement`. |
| `biomed.amc_contracts` | AMC/CMC contracts. `IsRenewalDue` is computed on read (`BiomedMath.IsRenewalDue`), not stored — same "compute eligibility on read" posture MRD used for destruction/retention. |
| `biomed.pm_schedules` | Preventive-maintenance schedule templates — `FrequencyDays` + `NextDueDate`. `GenerateDueAsync` is an **explicit action** ("Generate due work orders" button), not a background job, matching Billing's honest scoping call on bed-charge auto-refresh. |
| `biomed.work_orders` | The unit of maintenance work, raised either from a due `PmSchedule` or explicitly from a `BreakdownComplaint`. Completing one auto-writes a `ServiceHistoryEntry` and, for a PM work order, rolls the schedule's `NextDueDate` forward by `FrequencyDays`. |
| `biomed.breakdown_complaints` | Raise → assign → resolve → close. `DowntimeMinutes` is computed at resolve time from `ReportedAt`→now (`BiomedMath.DowntimeMinutes`), never user-entered. Raising a complaint flips the equipment to `UnderMaintenance`; resolving flips it back to `InService`. |
| `biomed.calibrations` | Due tracking + certificate upload, same on-disk storage idiom (`IFileStorageService`) as MRD's `ScannedDocument`. |
| `biomed.service_histories` | One row per completed work order (auto) plus ad-hoc vendor-visit entries (manual). |
| `biomed.spare_usages` | "Spare Usage (links Stores)" — decrements a Pharmacy `Batch` via the existing internal `StockLedgerWriter` primitive, the exact same one OT's implant/consumable usage uses. No parallel stock-tracking invented; no billing side (unlike OT, a biomedical repair isn't charged to a patient). |

Migration: `Phase19_Biomed`.

## Reuse, not a parallel module

- Spare-parts consumption calls `Prixo.Application.Features.Pharmacy`'s internal
  `StockLedgerWriter.Write(...)` and `ProductNaming.Of(...)` directly — both are `internal`
  (assembly-scoped, so callable from the Biomed feature without a public API change), same
  posture as `OtUsageService`.
- No new "assign user" picker component — reuses Admin's existing `useUsers` list.

## Endpoints (`/api/v1/biomed`)

- `equipment`: `GET` (filterable status/criticality/search), `GET {id}`, `POST`, `PUT {id}`
- `equipment/{id}/locations`: `GET`, `POST` (record a movement)
- `equipment/{id}/service-history`: `GET`, `POST` (ad-hoc log entry)
- `contracts`: `GET` (filterable equipmentId/renewalDueOnly), `POST`
- `pm-schedules`: `GET` (filterable dueOnly), `POST`; `pm-schedules/generate-due`: `POST`
- `work-orders`: `GET` (filterable status/type/equipmentId), `GET {id}`, `POST /from-complaint`,
  `{id}/assign`, `{id}/complete`, `{id}/cancel`, `{id}/spare-usage`: `POST`
- `complaints`: `GET` (filterable status/equipmentId), `POST` (raise), `{id}/assign`,
  `{id}/resolve`, `{id}/close`: `POST`
- `calibrations`: `GET` (filterable equipmentId/dueOnly), `POST` (multipart, optional
  certificate file); `{id}/certificate`: `GET` (download)
- `reports/downtime`, `reports/pm-compliance`: `GET` (date-range filterable). AMC expiry is
  served by `contracts?renewalDueOnly=true`, not duplicated as a separate report endpoint.

## Permissions

`BIOMED.EQUIPMENT.{VIEW,MANAGE}`, `BIOMED.CONTRACT.{VIEW,MANAGE}`, `BIOMED.PM.{VIEW,MANAGE}`,
`BIOMED.WORKORDER.{VIEW,MANAGE}`, `BIOMED.COMPLAINT.{VIEW,CREATE,MANAGE}`,
`BIOMED.CALIBRATION.{VIEW,MANAGE}`, `BIOMED.REPORT.VIEW`.

## Frontend (`src/features/biomed`)

- `EquipmentListPage` (`/biomed/equipment`) — searchable registry; row click →
  `EquipmentDetailPage` (`/biomed/equipment/:id`) — location history + move action, AMC
  contracts, calibrations (with certificate upload), and service history, all as cards on
  one page (kept flat rather than tabbed, to move quickly through a self-contained module).
- `AmcContractsPage` (`/biomed/contracts`) — cross-equipment list + renewal-due filter; new
  contracts are added from the equipment detail page, not here.
- `PmSchedulesPage` (`/biomed/pm-schedules`) — list + due filter + "Generate due work orders".
- `WorkOrdersPage` (`/biomed/work-orders`) — list + a manage modal (assign via Admin's user
  list, record spare usage against a batch, complete, cancel).
- `ComplaintsPage` (`/biomed/complaints`) — raise + a manage modal (assign, resolve, close,
  or raise a formal work order against the complaint).
- `CalibrationsPage` (`/biomed/calibrations`) — cross-equipment list + due filter + certificate
  download; new calibrations are recorded from the equipment detail page.
- `BiomedReportsPage` (`/biomed/reports`) — tabbed: downtime, PM compliance.

## Deferred / notes

- **2026-09-17 fix** — `GET /biomed/reports/downtime` 500'd unconditionally (every call, with or
  without a date range): the query grouped `BreakdownComplaint` by an anonymous key that included
  `Equipment!.Name`, then built the DTO in the `Select` with `g.Sum(c => c.DowntimeMinutes ?? 0)`
  over the group — EF Core 10 can't translate that combination to SQL and throws
  `InvalidOperationException` instead of falling back, which the global handler turned into a
  bare 500. Rewritten to match `GetPmComplianceReportAsync`'s existing posture just below it in
  the same file: project the flat columns needed, materialize with `ToListAsync`, then `GroupBy`
  client-side. No behavior change for a working query, just makes this one actually return data.
- **No PM-schedule background job** — see `pm_schedules` note above; "generate due work
  orders" is a manual button, consistent with the rest of the codebase's honest scoping
  around Hangfire usage.
- **Assign-to is any user, not a role-filtered "Biomedical Engineer" picker** — a `BIOMEDICAL_ENGINEER`
  role now exists (seeded in Phase 20, see `docs/modules/admin.md`), but the picker itself still
  lists every user rather than filtering by role assignment; that filtering was out of scope here
  and remains a possible follow-up.
- **Frontend not exercised in a real browser** — verified via `tsc -b && vite build` (clean)
  and `oxlint` (clean, one pre-existing unrelated warning); no browser tool is available in
  this environment.
