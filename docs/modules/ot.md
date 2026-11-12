# Module Spec — Operation Theatre (Phase 14)

Status: **complete** (2026-07-18). Every surgery booking is anchored to an IPD admission
(per the plan's "needs IPD + stock + billing" dependency — there's no OP/day-care path).
Owns theatre/equipment masters, conflict-checked booking + scheduling, surgeon/anaesthetist
assignment + team roles, the WHO Surgical Safety Checklist's three phases, consent
verification, pre-anaesthesia/intra-op/operative/post-op clinical note templates,
batch/serial-tracked implant and consumable usage (stock decrement + IP bill charge in one
transaction), and the OT register report.

## Database (schema `ot`)

| Table | Notes |
|---|---|
| `ot.theatres` / `ot.equipment` | Branch-scoped, same shape as Pharmacy's Store/Rack. One default theatre (`OT1`) is seeded per branch so day-one booking has somewhere to book against. |
| `ot.surgery_bookings` | `SurgeonId`/`AnaesthetistId` are direct FKs (not team rows) — exactly what the conflict check queries against. `PatientConsentId` mirrors Admission's own "set once the shared PatientConsent flow is signed" pattern, not a dedicated OT consent entity. `BookingNumber` is `OT-{year}-{seq}`. |
| `ot.surgery_team_members` | Roles beyond the booking's own Surgeon/Anaesthetist (assistant surgeon, scrub/circulating nurse…). `DoctorId` for a doctor team member, `StaffName` free text for non-doctor staff not in the Doctor master. |
| `ot.surgical_safety_checklists` | One row per booking (created empty alongside it), holding the WHO checklist's three fixed phases — Sign In / Time Out / Sign Out — as discrete boolean checkboxes plus a completion stamp per phase. |
| `ot.pre_anaesthesia_assessments` / `ot.intra_op_anaesthesia_records` | 1:1-per-booking, find-or-create ("Save" upserts). `AsaGrade` is free text (`"ASA II-E"`) rather than an enum — the emergency modifier doesn't fit a plain grade. |
| `ot.operative_notes` / `ot.post_op_notes` | 1:1-per-booking, same upsert pattern. Post-op only records the clinical handover — actual bed/ward transfer is IPD's existing BedTransfer flow (Phase 9), not duplicated here. |
| `ot.implant_usages` / `ot.consumable_usages` | Batch/serial-tracked (implants) or plain batch-tracked (consumables) product usage. `Rate` snapshots the batch's `Mrp` (falling back to `PurchaseRate` for items without one — Phase 13's nullable-Mrp general items) at usage time. `BillItemId` links to the BillItem it posted. |

Migration: `Phase14_Ot`. `BillItemSourceType` gained a new `OtCharge = 6` member for the
lines implant/consumable usage posts.

## Architecture: composes into Billing and Pharmacy directly, no new cross-service calls

`OtUsageService` (implant/consumable usage) reaches directly into `db.Bills`/`db.BillItems`
and reuses `BillMath.ComputeItemAmounts`/`RecomputeBillTotals` (both public statics already
designed for this) rather than adding new methods to `BillingService` — matching the
codebase's existing convention of a service touching another module's DbSets/pure-math
helpers directly (`PurchaseService` → `Masters.Vendor`, `IssueService` → `Ipd.Admission`)
rather than injecting one feature's service into another's constructor, which has no
precedent anywhere else in this codebase. Stock decrement reuses `StockLedgerWriter` from
Pharmacy (`internal` but assembly-visible) with the usage row itself as the ledger's
reference document — the same "document is its own reference" idiom as GRN/Sale/Adjustment.

Theatre time / surgeon fee / anaesthesia fee charges are **not** a new billing code path —
they're posted through Billing's existing `AddManualItemAsync` (Service-sourced bill lines,
already resolving tariff rates) once the hospital creates the corresponding `Service` rows
(e.g. "Theatre Charge", "Surgeon Fee") via the Masters → Services screen that's existed
since Phase 2. This was a deliberate choice over hardcoding those three services into the
seeder — CLAUDE.md's "no hardcoded hospital data" applies to pricing-bearing catalog rows
the same way it applies to Drug/Item/Vendor, which also aren't seeded.

## Every stock/billing-affecting action is a mandatory transaction

Per CLAUDE.md's transaction list ("pharmacy sale/issue" + "billing" both apply here),
`OtUsageService.RecordImplantUsageAsync`/`RecordConsumableUsageAsync` run inside
`IUnitOfWork.ExecuteInTransactionAsync` — the stock decrement and the bill-item post must
succeed or fail together. Booking creation, checklist updates, and note saves are plain
`SaveChangesAsync` calls (no stock/money effect, same reasoning as Pharmacy's draft
documents not needing the transactional guarantee).

## Checklist gates the booking's clinical status transitions

`BookingService` mirrors IPD's DischargeClearance posture — a checklist phase must be
complete before the next status transition:

- **Scheduled → InProgress** (`StartAsync`) requires `ConsentVerifiedAt` set (via
  `VerifyConsentAsync`, which links an already-captured `PatientConsent`) **and** the
  checklist's Sign In phase completed.
- **InProgress → Completed** (`CompleteAsync`) requires the checklist's Sign Out phase
  completed.

## Conflict check

`OtMath.Overlaps` (pure, half-open-interval test — a booking ending exactly when another
starts does not conflict) backs `BookingService.EnsureNoConflictAsync`, which rejects a new
or rescheduled booking if its window overlaps an existing non-Cancelled/non-Postponed
booking sharing the same Theatre, Surgeon, **or** Anaesthetist — whichever collides first
is named in the error.

## Endpoints (`/api/v1/ot`)

- `theatres`: `GET`, `POST`, `PUT {id}`; `theatres/equipment`: `GET?theatreId=`, `POST`, `PUT {id}`
- `bookings`: `GET?status=&theatreId=&fromDate=&toDate=`, `GET {id}`, `POST`,
  `POST {id}/reschedule`, `POST {id}/update-patient` ("OT Patient Update", added 2026-07-27 —
  re-points a Scheduled booking's `AdmissionId`/`PatientId` to a different admission, for
  correcting a mis-selected patient at booking time; same `Scheduled`-only gate as reschedule),
  `POST {id}/cancel`, `POST {id}/postpone`,
  `POST {id}/verify-consent`, `POST {id}/start`, `POST {id}/complete`,
  `POST {id}/team`, `DELETE {id}/team/{memberId}`, `GET bookings/register?fromDate=&toDate=`
- `bookings/{id}/checklist`: `GET`, `PUT checklist/sign-in`, `PUT checklist/time-out`,
  `PUT checklist/sign-out`
- `bookings/{id}/pre-anaesthesia`, `/intra-op`, `/operative-note`, `/post-op-note`:
  `GET`, `PUT` (upsert) each
- `bookings/{id}/usage/implants`, `/consumables`: `GET`, `POST` each

## Permissions

`OT.THEATRE.{VIEW,MANAGE}`, `OT.BOOKING.{VIEW,CREATE,MANAGE}` (MANAGE covers
reschedule/cancel/postpone/team), `OT.SURGERY.CONDUCT` (Start/Complete — the
checklist/consent-gated transitions), `OT.CHECKLIST.MANAGE` (checklist + consent
verification), `OT.ANAESTHESIANOTE.MANAGE`, `OT.OPERATIVENOTE.MANAGE`, `OT.USAGE.RECORD`,
`OT.REGISTER.VIEW`.

## Frontend (`src/features/ot`)

- `TheatresPage` (`/ot/theatres`) — tabbed Theatres/Equipment CRUD, same shape as Pharmacy's
  StoresPage.
- `BookingsPage` (`/ot/bookings`) — status-filtered list + create modal (admission, theatre,
  surgeon/anaesthetist, schedule window); a successful create navigates straight to the
  detail page.
- `BookingDetailPage` (`/ot/bookings/:id`) — six tabs: Overview (status actions — verify
  consent, start, complete, reschedule, change patient/admission, cancel, postpone; "Change
  patient" reuses the same admission picker `BookingsPage`'s create modal already uses), WHO Checklist (all three phases
  side by side, each independently completable), Anaesthesia (pre-anaesthesia + intra-op
  side by side), Operative/Post-Op notes, Implants/Consumables (usage tables + record forms
  reusing Pharmacy's `useBatches` hook), Team.
- `OtRegisterPage` (`/ot/register`) — date-range statutory register, same layout as
  Pharmacy's ConsumptionReportPage.

## Deferred / notes

- **No calendar/day-grid scheduling view** — bookings render as a filterable list, not a
  theatre-by-hour calendar grid; the plan's "calendar" wording is only half built (the
  conflict-check backend is real, the visual calendar isn't).
- **No implant/consumable reservation before surgery** — usage is recorded live during the
  case (`InProgress` gate on `OtUsageService`), not pre-picked at booking time.
- **Post-op note doesn't trigger a bed transfer** — `ShiftedAt` is a timestamp on the note
  only; moving the patient's actual bed allocation is a separate, existing IPD action.
- **No OT-specific PDF** (booking slip, register export) — the register is a screen/API
  report only, unlike Radiology/Lab's PDF reports; a natural follow-up using the existing
  `IPdfService` pattern.
