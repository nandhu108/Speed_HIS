# Module Spec — Emergency / Casualty (Phase 16)

Status: **complete** (2026-07-18). A workflow layered over Patients, Lab, Radiology, IPD and
Billing rather than new plumbing — per the plan, Emergency was scheduled after those four so
it could reuse their engines instead of re-implementing them ("ER is a workflow over existing
services, not new plumbing").

## New domain (schema `er`)

| Table | Notes |
|---|---|
| `er.emergency_visits` | The ER encounter header — `PatientId`, `AssignedDoctorId` (nullable), `VisitNumber`, `TokenNumber` (int, day-reset), `ArrivedAt`, `ArrivalMode`, `IsMlc`, `Status` (Registered→Triaged→UnderTreatment→Disposed, or Cancelled). |
| `er.triages` | One row per visit (re-triage updates in place — unique index on `EmergencyVisitId`). ESI level 1–5, chief complaint, inline vitals. Drives the queue's auto-priority sort. |
| `er.emergency_assessments` | The "fast clinical sheet" — find-or-create upsert, one row per visit. History/examination/diagnosis/treatment plus `Disposition` (Discharged/AdmittedConverted/TransferredOut/Lama/BroughtDead/Observation); `TransferredToFacility`/`TransferSummary` folded onto the same row rather than a separate table, populated only for transfer-out. |
| `er.mlc_records` | Statutory MLC register row: police station, informed time, FIR number, brought-by details, injury description. `MlcNumber` is sequence-generated on first save. |
| `er.accident_case_records` | Statutory accident register row — kept separate from `MlcRecord` because a visit can be an accident without being MLC and vice versa. |
| `er.observation_allocations` | Short-stay bed allocation, mirrors IPD's `BedAllocation` shape but keyed to `EmergencyVisitId`. A bed can only have one active (`ReleasedAt IS NULL`) allocation — enforced by a partial unique index. |

Widened existing entities rather than parallel structures (same posture as Phase 13's Batch
and Phase 15's `IpVitals`):

- `master.wards.is_observation` — marks short-stay observation wards; ER's
  `ObservationAllocation` allocates its beds from these, the same way ICU allocates from
  `Ward.IsIcu` wards.
- `ipd.admissions.referring_emergency_visit_id` — loose, optional traceability link
  alongside the existing `ReferringOpVisitId`.
- `lab.lab_orders.emergency_visit_id`, `rad.rad_orders.emergency_visit_id` — loose links
  alongside `OpVisitId`/`AdmissionId`.
- `billing.bills.emergency_visit_id` + `BillType.Emergency = 3` — same unique-active-bill
  index pattern as `AdmissionId`.

Migration: `Phase16_Emergency`.

## Reuse, not a parallel module

Per the plan's "reuses OPD/IPD/lab/billing engines" framing, most of the checklist needed
zero new backend logic once the entities above existed:

- **Rapid registration + unknown-patient flow** — Patient Registration's existing
  `POST /api/v1/patients` already only requires first/last name, gender, and mobile; "unknown
  patient with later merge" is a frontend usage pattern (placeholder values, then Phase 3's
  existing duplicate-detection/merge flow later), not a new capability.
- **STAT Lab/Radiology Orders** — `CreateLabOrderRequest`/`CreateRadOrderRequest` were widened
  with an `EmergencyVisitId` field (they already accepted a bare `PatientId` with nullable
  visit links); an ER STAT order is just a normal lab/rad order with `Priority=Stat` and
  `EmergencyVisitId` set. No new order-creation logic in either module.
- **Admission Conversion (ER → IPD)** — front desk runs the existing IPD admission flow
  (Phase 9) and sets `Admission.ReferringEmergencyVisitId`; there is no separate "convert"
  endpoint.
- **Referral / Transfer Out** — `EmergencyAssessment.Disposition = TransferredOut` with the
  facility/summary fields on the same row; no separate transfer-out entity or endpoint.
- **Emergency Billing** — one new `BillingService.GenerateEmergencyBillAsync` method creates
  an **empty** bill (`BillType.Emergency`, no auto-priced lines — "post-facto friendly" per
  the plan) against the visit; all line items are then added through the pre-existing generic
  `AddManualItemAsync` path, exactly like an OT charge.

## No cross-service DI

Consistent with Phases 14–15: no ER service takes a constructor dependency on
`LabOrderService`, `RadOrderService`, or `BillingService`. `BillingService` itself grew the
new `GenerateEmergencyBillAsync` method (it already owns `Bill` writes); ER's own services
(`EmergencyVisitService`, `EmergencyAssessmentService`, `MlcService`) only touch `er.*` tables
plus read-only lookups (Patient exists, Doctor exists, Bed not already allocated).

## Queue priority

`ErMath.QueuePriority(EsiLevel?)` is pure and unit-tested: untriaged visits sort first
(priority 0, on the theory that an unseen patient is the most urgent unknown), then ESI 1
(Resuscitation) through ESI 5 (Non-Urgent) in order. `EmergencyVisitService.GetQueueAsync`
fetches candidate rows via a correlated-subquery join (avoiding the "custom C# method inside
a still-`IQueryable` LINQ expression" SQL-translation trap hit in earlier phases), materializes
with `ToListAsync`, then sorts client-side by `QueuePriority` then `ArrivedAt`.

## Endpoints (`/api/v1/emergency`)

- `visits`: `GET` (queue, filterable by status/date range), `POST` (register)
- `visits/{id}`: `GET`
- `visits/{id}/triage`: `POST` (find-or-create upsert; re-triage updates the same row)
- `visits/{id}/cancel`: `POST`
- `visits/{id}/assessment`: `GET`, `PUT` (upsert; setting a disposition marks the visit Disposed)
- `visits/{id}/observations`: `GET`, `POST` (allocate); `observations/{allocationId}/release`: `POST`
- `visits/{id}/mlc`: `GET`, `PUT` (upsert)
- `visits/{id}/accident`: `GET`, `PUT` (upsert)
- `billing/bills/generate-emergency/{emergencyVisitId}`: `POST` (on `BillsController`, requires `BILLING.BILL.CREATEEMERGENCY`)

STAT lab/radiology orders go through the **existing** `lab/orders` and `rad/orders` endpoints
with `emergencyVisitId` set — no new Lab/Rad endpoints.

## Permissions

`EMERGENCY.VISIT.{VIEW,CREATE,MANAGE}`, `EMERGENCY.TRIAGE.RECORD`,
`EMERGENCY.ASSESSMENT.MANAGE`, `EMERGENCY.OBSERVATION.MANAGE`, `EMERGENCY.MLC.MANAGE`.
Billing's emergency-bill endpoint uses `BILLING.BILL.CREATEEMERGENCY` — no new Billing
permission was needed.

## Frontend

- `QueuePage` (`/emergency/queue`) — the ER tracker: active visits sorted by triage priority,
  token/ESI/doctor/status/MLC columns, a "Register ER visit" modal (patient search + arrival
  mode + MLC flag), 15s auto-refresh.
- `VisitDetailPage` (`/emergency/visits/:id`) — tabbed workspace:
  - **Overview / Triage** — record/update triage (ESI, chief complaint, vitals, doctor
    assignment), cancel visit.
  - **Assessment / Disposition** — the fast clinical sheet; a `TransferredOut` disposition
    reveals the facility/summary fields (matches the backend's conditional validator rule).
  - **Orders** — "Order STAT lab test" / "Order STAT radiology" buttons that navigate to the
    existing Lab/Rad order pages with the patient and `emergencyVisitId` prefilled via router
    state (`CreateOrderModal` on both pages recognizes an `erOrder` prefill, skips the patient
    picker, and defaults priority to STAT) — the "reuse existing engines" design decision made
    concrete on the frontend too, not just the API.
  - **Observation Bed** — allocate/release a bed from wards flagged `IsObservation` in
    Masters → Locations.
  - **MLC / Accident** — the two statutory registers, side by side.
  - **Billing** — generate the empty emergency bill, then hand off to the existing bill detail
    page for line items/payment.
- Masters → Locations → Wards gained an "Observation ward" checkbox alongside the existing
  ICU checkbox (`Ward.IsObservation`, previously schema-only).

## ER Token Mapping (added 2026-07-27, see `docs/modules/appointments.md`)

Once a visit has an `AssignedDoctorId` (set by triage or assessment), `VisitDetailPage`'s
Overview tab exposes a "Map ER token" action that mints a normal walk-in OPD token for that
doctor via `AppointmentService.MapEmergencyTokenAsync` — the same booking engine as any other
walk-in, giving the ER patient a real slot in that doctor's OPD token board instead of a
second, parallel numbering scheme. This lives entirely in the Appointments module (endpoint,
service method, `Appointment.EmergencyVisitId` FK); nothing was added to `er.*`.

## Deferred / notes

- **No dedicated color-coded triage board widget** — ESI level is shown via a color-toned
  badge in the queue table; there's no separate kanban-style board view.
- **No PDF prints for MLC/accident registers this phase** — the plan calls them out as
  "statutory report"/"statutory prints"; the data model and screens exist, but a
  QuestPDF-rendered register print was not built (the queue/visit screens are considered the
  MLC register for now, same "deferred PDF" pattern used elsewhere when a report format wasn't
  specified precisely enough to commit to a layout).
- **Frontend not exercised in a real browser** — verified via `tsc -b && vite build` (clean)
  and `oxlint` (clean, one pre-existing unrelated warning); no browser tool is available in
  this environment, so the golden-path click-through was not performed live.
