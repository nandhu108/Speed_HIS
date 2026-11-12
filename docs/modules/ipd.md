# Module Spec — IPD (Phase 9)

Status: **complete** (2026-07-13). Owns admission (IP number, bed allocation, consultant
mapping, consent, initial deposit, admission slip), the bed board (status + occupancy
dashboard), ward/room/bed and consultant transfers, the unified doctor order sheet
(medication/lab/radiology/diet/nursing), basic inpatient charting (progress notes, vitals,
intake/output, procedure notes), and the discharge workflow (request → 4-way clearance
checklist → doctor-approved discharge summary → final discharge → discharge slip).

## Database (schema `ipd`)

| Table | Notes |
|---|---|
| `ipd.admissions` | Admission header — the IPD anchor, mirroring OpVisit's role for OPD. `CurrentBedId` is a denormalized pointer to the active `BedAllocation`'s bed; full transfer history lives in `bed_allocations`. `ReferringOpVisitId` is a loose, optional link — nothing auto-creates an admission from an OPD visit. |
| `ipd.admission_consultants` | One row per consultant mapping — both the initial primary+secondary mapping and every consultant transfer (a transfer ends the current row's `EndedAt` and inserts a new one), so full history reads off this one table without a separate "transfer" entity. |
| `ipd.bed_allocations` | One row per bed the admission has occupied. `ReleasedAt` null means current. **Concurrency-safe by construction**: partial unique indexes on `(bed_id) WHERE released_at IS NULL` and `(admission_id) WHERE released_at IS NULL` mean a second concurrent allocation attempt hits the index and fails instead of double-booking — on top of the application-level `Bed.Status != Vacant` guard. `BedChargePerDay` snapshots `RoomType`'s rate at allocation time. |
| `ipd.doctor_orders` / `medication_orders` / `diet_orders` | Unified order-sheet header (polymorphic like OPD's `ClinicalOrder`) plus two structured child tables. Medication and Diet get structured rows; Lab/Radiology/Nursing stay free-text on `Description` — same loose-coupling posture as everywhere else in this codebase (no auto-generated `LabOrder`/`RadiologyOrder`/Pharmacy `Issue` from a placed order). |
| `ipd.progress_notes` / `ip_vitals` / `intake_output_entries` / `procedure_notes` | Basic Phase 9 inpatient charting — repeatable, admission-scoped entries. Phase 10 (Nursing) deepens vitals/I-O with shift-tagging, structured assessment forms and risk scores; this phase only builds the plain entry+list. |
| `ipd.discharge_clearances` | One row per `ClearanceType` (Billing/Pharmacy/Nursing/FinalInvestigation), auto-created when discharge is requested. Final discharge is blocked until every row `IsCleared` — the plan's 4-way checklist. Clearing is manual sign-off; the Billing row's live `Bill.BalanceAmount` is surfaced (not enforced) alongside it — see "Deferred / notes" below. |
| `ipd.discharge_summaries` | One per admission. `IsApproved` (doctor-approved, per plan) gates both the Discharge Summary PDF's "approved by" line and — combined with the checklist — final discharge. |
| `ipd.admission_requests` | Pre-admission "please admit this patient" request → approve/reject workflow (added 2026-07-24, commit `5e9e2b2`; undocumented here until now). Mirrors Stores' indent→approve→issue pattern — gates front desk's direct-admit flow (`AdmissionService.AdmitAsync`) without itself creating the `Admission` (a bed still has to be picked); approving just clears the request, and `ApprovedAdmissionId` is stamped once `AdmitAsync` is actually called against it. `GET`/`POST /ipd/admission-requests`, `POST {id}/approve`, `POST {id}/reject` (`AdmissionRequestsController.cs`); permissions `IPD.ADMISSIONREQUEST.{VIEW,CREATE,DECIDE}`; `AdmissionRequestsPage` (`/ipd/admission-requests`). |

Migration: `Phase9_Ipd`. Cross-cutting additions in this phase's own migration:
`Bed.Status` (Masters' `Bed` entity, Phase 2) for the bed board; `Bill.AdmissionId` +
`BillItemSourceType.BedCharge` (Billing, Phase 6); `Advance.AdmissionId` (Billing, Phase 6 —
the doc comment there predicted this exact addition); `Issue.AdmissionId` (Pharmacy, Phase 8).

## Bed allocation is concurrency-safe, not just guarded

`AdmissionService.AdmitAsync`/`TransferBedAsync` check `Bed.Status == Vacant` before
allocating, but the real safety net is the two partial unique indexes on `BedAllocation`
described above — even if two requests race past the application-level check, only one
`INSERT` can win; the other throws and the caller sees a "bed no longer available" error
rather than silently double-booking. `Bed.Status` itself is derived-only for `Occupied` (set
exclusively by admit/transfer/discharge/cancel) but mutable for the other four values
(Vacant/Reserved/Cleaning/Blocked), which are toggled directly from the bed board
(`BedBoardService.SetStatusAsync` — refuses to touch a currently-occupied bed).

## IP billing reuses the OP billing engine, not a parallel one

`BillingService.GenerateIpBillAsync(admissionId)` creates a `Bill` (`BillType.Ip`,
`AdmissionId` set) and immediately computes its first `BedCharge` line via
`RefreshBedChargesAsync`, which recomputes every `BedCharge` line wholesale from
`BedAllocation` history — one line per allocation segment, quantity in whole days
(`IpdMath.ComputeBedDays`, hotel-style: any part of a day counts as a full day) at that
segment's snapshotted `BedChargePerDay`. Everything else — adding Service/Package lines,
discount threshold-approval, payment collection, refunds, cancellation — is the *exact same
code path* `GenerateOpBillAsync` already uses; nothing IP-specific was duplicated.
`RefreshBedChargesAsync` remains a manual, on-demand action; since 2026-08-08 it also runs on
a daily schedule via `IpBedChargeRefreshJob` (Billing's doc has the details) and, either way,
auto-sweeps the admission's available deposit against the recomputed balance — a stay's
running bill no longer needs a cashier to manually draw the deposit down.

## Every admission/bed/discharge mutation is transactional; charting isn't

Per CLAUDE.md's explicit list ("admission, discharge"), `AdmissionService.AdmitAsync`,
`TransferBedAsync`, `CancelAsync`, and `DischargeService.FinalizeDischargeAsync` all run
inside `IUnitOfWork.ExecuteInTransactionAsync` — each touches `Admission` + `Bed` +
`BedAllocation` (and `Advance`/`PatientConsent` for admit) together and must not partially
apply. `TransferConsultantAsync` is wrapped too for the same multi-row-consistency reason,
even though CLAUDE.md's list doesn't name it explicitly. Requesting a discharge, clearing a
checklist item, saving/approving a discharge summary, placing a doctor order, and every
charting entry (progress note/vitals/I-O/procedure note) are plain single-`SaveChangesAsync`
calls — none of them touch stock, billing, or bed state, so they don't need the guarantee.

## Endpoints (`/api/v1/ipd`)

- `admissions`: paged `GET`, `GET {id}`, `POST` (admit), `POST {id}/cancel`,
  `POST {id}/transfer-bed`, `POST {id}/transfer-consultant`, `POST {id}/consultants`
  (add secondary), `POST {id}/consultants/{consultantId}/end`, `GET {id}/admission-slip` (PDF)
- `admissions/{admissionId}/orders`: `GET?orderType=`, `POST`, `POST {orderId}/cancel`
- `admissions/{admissionId}/progress-notes`, `/vitals`, `/intake-output`, `/procedure-notes`:
  `GET`, `POST` each
- `admissions/{admissionId}/discharge`: `POST request`, `GET clearances`,
  `POST clearances/{clearanceType}`, `GET summary`, `PUT summary`, `POST summary/approve`,
  `POST finalize`, `GET summary-pdf`, `GET slip`
- `bed-board`: `GET?wardId=`, `GET occupancy`, `POST {bedId}/status`
- `billing/bills/generate-ip/{admissionId}`, `billing/bills/{id}/refresh-bed-charges` (owned
  by BillingService, triggered from here)
- `reports/admissions`, `reports/admissions/export`, `reports/ward-occupancy`,
  `reports/ward-occupancy/export` — IP Report and Ward Occupancy Report (below).

## Permissions

`IPD.ADMISSION.{VIEW,CREATE,CANCEL}`, `IPD.BED.{VIEW,MANAGE}`, `IPD.CONSULTANT.MANAGE`,
`IPD.ORDER.{VIEW,CREATE,CANCEL}`, `IPD.CHART.{VIEW,MANAGE}`,
`IPD.DISCHARGE.{REQUEST,CLEAR,APPROVESUMMARY,FINALIZE}`. The IP Report and Ward Occupancy
Report reuse `IPD.ADMISSION.VIEW`/`IPD.BED.VIEW` respectively — no new permission codes.
ER→IP and OP→IP conversion reuse the existing `IPD.ADMISSION.CREATE` that already gates
"Admit patient" — converting is the same action, just pre-filled.

## Frontend (`src/features/ipd`)

- `AdmissionsPage` (`/ipd/admissions`) — list + search, "Admit patient" modal (patient
  picker, primary consultant, vacant-bed picker sourced from the bed board, admission type,
  optional consent capture and initial deposit). The modal itself is a standalone,
  reusable component (`components/AdmitPatientModal.tsx`) — deliberately not tied to
  navigation, so it opens **in place** on whichever page the admit action lives, instead of
  routing away from it. OPD's consultation workspace, an ER visit's Assessment tab (once
  disposition is "Admitted/Converted"), and the Convert-to-IP launchpad below all open it
  directly with an `AdmitFromPrefill` (patient + source `opVisitId`/`emergencyVisitId`) — the
  modal skips the patient search and links the source visit automatically. Cancel/close just
  dismisses the modal and leaves you on the page you started from; only a successful Admit
  navigates, to the new admission's detail page.
- `AdmissionDetailPage` (`/ipd/admissions/:id`) — tabbed: Overview (consultants, bed history,
  transfer/cancel actions, admission slip, IP bill generation, and — when converted from an
  OP/ER visit — the source visit number), Orders (unified order sheet + place-order form with
  medication/diet sub-forms), Progress Notes, Vitals, Intake/Output, Procedures, Discharge
  (request → checklist → summary → approve → finalize → PDFs).
- `BedBoardPage` (`/ipd/bed-board`) — occupancy stat tiles, ward-grouped bed grid with live
  status, click-to-toggle status modal (blocked while occupied). Live/current-moment only —
  see Ward Occupancy Report below for a historical, date-ranged view.
- `IpReportPage` (`/ipd/report`) — standalone IP Report: admission-date-range filter, Summary
  (per-ward admissions/discharges/avg LOS) or Detail (one row per admission, with LOS) toggle,
  CSV/Excel/PDF export.
- `WardOccupancyReportPage` (`/ipd/ward-occupancy-report`) — bed-days occupied vs. available
  per ward over a **required** date range (open-ended ranges can't compute bed-days-available),
  Summary (per-ward occupancy %) or Detail (one row per bed allocation, with days-in-range)
  toggle, CSV/Excel/PDF export. Bed-day overlap math is a pure, unit-tested helper
  (`OccupancyMath.OverlapDays`), same "pure math + typed service" split as `ErMath`/`BillMath`.
- `ConvertToIpPage` (`/ipd/convert`, gated on `IPD.ADMISSION.CREATE`) — a browsable launchpad
  on top of the per-visit "Admit to IP" buttons: recently-completed OP visits and
  ER visits disposed "Admitted (Converted)" that don't yet have a linked admission (an
  `AlreadyAdmitted` flag added to `OpVisitListItemDto`/`EmergencyVisitListItemDto`, computed
  via `Admissions.Any(ReferringOpVisitId/ReferringEmergencyVisitId == visit.Id)`), each with a
  one-click "Convert" that opens the same `AdmitPatientModal` in place on this page.

## Deferred / notes

- **ER→IP conversion was schema-only until now.** `Admission.ReferringEmergencyVisitId`
  existed as a column since Phase 16 but was never read from `AdmitPatientRequest` or set by
  `AdmissionService.AdmitAsync` — the column was dead. Fixed alongside the OP→IP fix (see
  OPD's doc): the request/service/DTOs now thread it through, and an ER visit's Assessment tab
  gets an "Admit to IP" button once its disposition is "Admitted/Converted". Still no separate
  convert endpoint by design (per the original plan §16 framing) — it's the same admission
  flow, just pre-filled and linked.
- **No auto-generated Lab/Radiology orders or Pharmacy issues from a doctor order.** A
  Medication order is structured (drug/dose/route/frequency) and schema-ready for Phase 10's
  MAR, but nothing turns it into a Pharmacy `Issue` yet — `Issue.AdmissionId` (added this
  phase) lets a pharmacist manually link an issue to an admission, closing out the "IP issue
  is schema-ready only" placeholder from Phase 8's doc into "manual but usable."
- **Billing clearance is a manual sign-off, with the live balance now surfaced alongside it
  (fixed 2026-09-17)** — closes `docs/PATIENT_JOURNEY_GAP_ANALYSIS.md` §5.2. `GET clearances`
  now returns `BillBalanceAmount` (looked up from the admission's latest bill) on the Billing
  row only; the Discharge tab shows it next to that checklist item, in red when > 0. Clearing
  is still not blocked on it — a deliberate, user-confirmed choice: hard-gating discharge on a
  zero balance would break legitimate LAMA / approved-pending-insurance / waived-balance
  discharges, so staff still tick it themselves, just with the real number in front of them
  instead of a blind checkbox.
- **Bed-charge refresh now also runs on a daily schedule (2026-08-08)**, via Billing's
  `IpBedChargeRefreshJob` (`Cron.Daily`) — see `docs/modules/billing.md`.
  `RefreshBedChargesAsync` (Overview tab / billing endpoint) remains available as a manual
  "recompute now" action, same as before.
- **The IP advance/deposit is now auto-drawn against the running bill (2026-08-08)** — see
  `BillingService.AutoDrawAdvancesAsync` in `docs/modules/billing.md`. The admission's
  Initial Deposit step is unchanged; only the *consumption* side is new.
- **Nursing-side charting stays basic here.** Vitals and intake/output are plain
  timestamped entries; shift-tagged notes, structured assessment forms, and Morse/Braden risk
  scores are explicitly Phase 10 (Nursing) work, per the plan's "nursing-side entries deepen
  in Phase 10."
- **Consent capture has no scan/file upload in the admission wizard.** `PatientConsent` still
  supports a `DocumentPath`, reachable via the patient's own consent-capture endpoint, but the
  admission flow only captures signed-by name + relationship (text only).
- **No secondary-consultant multi-select in the Admit form.** The admission wizard admits
  with just a primary consultant; secondary consultants are added afterward from the
  Overview tab's "Add secondary" action — a deliberate simplification, not a gap in the API
  (`AdmitPatientRequest.SecondaryConsultantIds` exists and is exercised by the backend).

## Department on IP admission (added 2026-11-12)

- `ipd.admissions` gained a nullable **`department_id`**; `AdmitPatientRequest.DepartmentId` is **mandatory** and must be an active Clinical department (same `DepartmentService.EnsureClinicalAsync` gate as OP registration — see "Department on OP registration" in `opd.md`). The migration backfills existing admissions from the primary consultant's first mapped department.
- `AdmitPatientModal` (shared by Admissions, OP workspace, ER visit and Convert-to-IP) has a required Department select above Primary consultant; consultants narrow to that department's doctors plus unmapped doctors. `AdmissionDetailDto` returns `departmentId`/`departmentName`.
