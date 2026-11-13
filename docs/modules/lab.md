# Module Spec — Laboratory (Phase 7)

Status: **complete** (2026-07-13). Owns the lab test/profile catalog, order creation
(from an OP visit or a direct walk-in), sample collection/receipt/rejection, department
worklists, the entry → verify → approve result pipeline with automatic age/sex-aware
flagging, critical value alerts, the cumulative lab report PDF, and a TAT report.

## Database (schema `lab`)

| Table | Notes |
|---|---|
| `lab.lab_tests` / `lab.lab_test_parameters` / `lab.reference_ranges` | Global catalog. Every test — panel or not — carries at least one `LabTestParameter` row (a non-panel test's single parameter mirrors the test itself), so result entry and reference-range resolution never special-case "does this test have parameters." Each parameter can carry several `ReferenceRange` rows keyed by sex/age bracket. |
| `lab.test_profiles` / `lab.profile_tests` | Orderable bundles (e.g. "Liver Function Test"); ordering a profile expands to one `LabOrderItem` per member test at order time — the item keeps `TestProfileId` purely so the UI can group them back together. |
| `lab.lab_orders` | Branch-scoped order header. Global-unique `order_number` (`LAB-2026-000123`, per branch+year via `ISequenceService`). `OpVisitId`/`ClinicalOrderId` are optional, loose back-references — see "Coupling to OPD" below. `Status` is a rollup recomputed from item statuses (`LabMath.RecomputeOrderStatus`), same pattern as Billing's `RecomputeBillTotals`. |
| `lab.lab_order_items` | One row per ordered test. `Status` is the real per-test pipeline (`Ordered → Collected → Received → ResultEntered → Verified → Approved`, or `Cancelled`) — there's no separate `Rejected` state; a rejected sample bounces every item that used it back to `Ordered`. `BillItemId` is a soft, unenforced link to `billing.bill_items` (see "Billing check" below). |
| `lab.samples` | One specimen draw, covering every order item of the same sample type on that order (one blood draw can serve CBC + LFT + KFT together). Global-unique `sample_number` (`SMP-2026-000123`) is the scannable code — see "Barcode" below. Rejection is recorded directly on this row (`RejectionReason`/`RejectedAt`/`RejectedByUserId`) rather than a separate `sample_rejections` table; `AuditLog` already gives row-diff history for anything more. |
| `lab.lab_results` / `lab.result_values` | One `LabResult` per `LabOrderItem` (no separate status column — `LabOrderItem.Status` is the single source of truth); one `ResultValue` per parameter, with `Unit`/`NormalLow`/`NormalHigh` **snapshotted at entry time** so a historical report still reads correctly if the reference range changes later (same reasoning as Billing's `BillItem` snapshot fields). `LabResult.ApproverSignaturePath` is a placeholder column — see "Placeholders" below. |
| `lab.critical_alerts` | Raised automatically (one row per critical `ResultValue`) when a result is entered with any value flagged `Critical`; tracks acknowledgment separately from the approve step, since acknowledging a critical value and approving the result are different responsibilities. |

Migrations: `Phase7_Lab`, `Phase7_Lab_SignaturePlaceholder` (the second was added right
after the first to fix a placeholder gap caught during doc review — see "Placeholders").

## Coupling to OPD

A lab order is created independently by lab reception — from an OP visit, a direct
walk-in, or with a loose `ClinicalOrderId` back-reference for traceability — rather than
auto-derived from `OpVisit`'s free-text `ClinicalOrder.Description`. The two modules stay
loosely coupled, the same deliberate choice as Billing's OP-visit link: completing every
item on an order that does carry a `ClinicalOrderId` marks that `ClinicalOrder.Status =
Completed` (and creating the order marks it `InProgress`), but nothing requires a
`ClinicalOrder` to exist at all. **There is no "Generate lab order" button on the OPD
consultation workspace yet** — same gap, same reasoning, as Billing's missing
visit-to-bill button.

## Billing check

`LabOrderItem.BillItemId` is a nullable soft link to `billing.bill_items`, meant to be set
by front desk once a test has been billed. **It is not an enforced gate** — nothing in
`LabOrderService` blocks collection, result entry, or approval on it being null. Building
a real gate (or auto-generating bill items from a lab order) would mean `LabOrderService`
depending on `BillingService`, a cross-feature coupling not justified for this phase's
scope; the column exists so a future pass can wire it up without a schema change.

## Reference-range resolution and flagging

`LabMath` (pure, no I/O — mirrors Billing's `BillMath`) does the actual selection and
comparison:

- `ResolveRange` prefers a sex-specific range over sex-agnostic, then an age-bounded range
  over unbounded — same specific-beats-general idiom as Billing's tariff resolution.
- `ResolveFlag` checks critical bounds before normal bounds, so a value outside both is
  `Critical`, not `Low`/`High`.
- `CalculateAgeYears` mirrors the age-display helper duplicated across `OpVisitService`/
  `PatientService` (an established small-duplication pattern in this codebase), but
  returns a number for range matching rather than a display string.

Only `Numeric`-type parameters get auto-flagged; `Text`/`Dropdown` parameters always
record `ResultFlag.Normal` — flagging free text or a dropdown selection as abnormal would
need per-value business rules (e.g. "Positive" being abnormal for a serology test) that
are out of scope for this phase and would need their own design, not a shortcut here.

## Sample rejection → recollect flow

`RejectSampleAsync` sets the sample's `Status = Rejected` and, for every `LabOrderItem`
that used it, clears `SampleId` and resets `Status` back to `Ordered` — the test
re-appears on the collection worklist for a fresh draw. The order's rollup status is
recomputed the same way a normal status change would trigger it.

## Result pipeline

Three explicit steps, each its own permission (`LAB.RESULT.CREATE/VERIFY/APPROVE`) so
separation-of-duties is enforced by permission grants, not application logic: entry
(`EnterResultAsync`, guarded to `Received`/`ResultEntered` so corrections are possible
before verification — re-entering replaces the previous `ResultValue`/`CriticalAlert` rows
wholesale, the same "PUT replaces the list" idiom used elsewhere), verify
(`ResultEntered → Verified`), approve (`Verified → Approved`, which also recomputes the
order rollup and, if everything on the order is now `Approved`/`Cancelled`, marks the
linked `ClinicalOrder` `Completed`).

## Endpoints (`/api/v1/lab`)

- `tests`, `profiles`: standard CRUD (parameters/reference-ranges are replaced wholesale
  on update, not diffed — same reasoning as the entry-replace idiom above)
- `orders`: paged `GET`, `GET {id}`, `POST` (create), `POST {id}/cancel`,
  `POST {id}/items/{itemId}/cancel`, `GET {id}/report` (PDF), `GET orders/tat-report`
- `collection/worklist` (`GET`, items awaiting collection), `collection/samples` (`POST`,
  collect), `collection/samples/awaiting-receipt` (`GET`),
  `collection/samples/{id}/receive|reject` (`POST`)
- `worklist?departmentId=` (`GET`, items awaiting result entry — the "department
  worklist"), `results/queue?status=` (`GET`, the verify/approve queues — `status` must be
  `ResultEntered` or `Verified`), `order-items/{itemId}/result` (`GET`/`POST` enter,
  `POST .../verify`, `POST .../approve`)
- `critical-alerts` (`GET` pending), `critical-alerts/{id}/acknowledge` (`POST`)

## Permissions

`LAB.TEST.{VIEW,MANAGE}`, `LAB.PROFILE.{VIEW,MANAGE}`,
`LAB.ORDER.{VIEW,CREATE,CANCEL}`, `LAB.SAMPLE.{COLLECT,RECEIVE,REJECT}`,
`LAB.RESULT.{CREATE,VERIFY,APPROVE}`, `LAB.ALERT.ACKNOWLEDGE`, `LAB.REPORT.VIEW`.

## Frontend (`src/features/lab`)

- `LabTestsPage` (`/lab/tests`) — CRUD with a nested parameter/reference-range editor
  (add/remove parameters, each with its own age/sex-banded ranges).
- `TestProfilesPage` (`/lab/profiles`) — CRUD with a checkbox test picker.
- `LabOrdersPage` (`/lab/orders`) — list + "New order" (patient picker, test/profile
  multi-select, priority) → navigates to the detail page.
- `LabOrderDetailPage` (`/lab/orders/:id`) — item list with per-item status, cancel
  (whole order or a single unstarted item), a read-only result viewer, and print-report.
- `CollectionWorklistPage` (`/lab/collection`) — items awaiting collection grouped by
  `(order, sample type)` so a multi-select only spans a single physical draw, plus a
  samples-awaiting-receipt list with receive/reject actions.
- `ResultWorklistPage` (`/lab/worklist`) — three tabs (Entry/Verify/Approve) backed by the
  department worklist and the two result-queue statuses; entry renders parameter inputs
  dynamically by `ValueType` (number/text/dropdown).
- `CriticalAlertsPage` (`/lab/critical-alerts`) — pending alerts + acknowledge modal.
- `TatReportPage` (`/lab/tat-report`) — date-range TAT table with an average.

## Placeholders

- `LabResult.ApproverSignaturePath` — digital signature image column, per plan §11
  Phase 7 ("signature image; DSC later"). Nothing writes to it yet; added in a follow-up
  migration (`Phase7_Lab_SignaturePlaceholder`) after the initial pass missed it.
- `IAnalyzerInbound` (`Prixo.Application.Common.Interfaces`) — placeholder for a future
  ASTM/HL7 analyzer listener, per plan §11 ("Analyzer Integration — placeholder — interface
  IAnalyzerInbound defined, held"). Unimplemented and unregistered; result entry is manual.

## OPD order bridge (added 2026-09-16)

Status: **complete**. Closes half of `docs/PATIENT_JOURNEY_GAP_ANALYSIS.md` §4.1: OPD's
consultation workspace could record a `ClinicalOrder` (OrderType Lab) but nothing carried it
into an actual `LabOrder` — reception had to re-key the test from scratch, with only an
optional, easy-to-skip `ClinicalOrderId` back-reference connecting the two if they remembered
to set it. `ConsultationWorkspace`'s Orders tab now has an "Order in Lab" action on any
`Ordered`-status Lab-type `ClinicalOrder`, navigating to `LabOrdersPage` with router state
(`{ opOrder: { opVisitId, clinicalOrderId, patientId, patientName, patientUhid } }`) — the same
prefill mechanism Emergency's STAT-order buttons already use (`docs/modules/emergency.md`), a
second prefill shape (`opOrder` alongside `erOrder`) on the same `CreateOrderModal`. The
patient picker is skipped, `clinicalOrderId` is threaded through to `CreateLabOrderRequest`
automatically — so "creating the order marks the ClinicalOrder InProgress" (see "Coupling to
OPD" below) now actually fires from this path instead of relying on someone remembering to set
the back-reference by hand. **Still not automatic** — staff still have to click the button and
pick the actual tests; nothing auto-generates a `LabOrder` from the free-text description.

## Cross-branch verify/view fix (2026-09-17)

`LabOrderItem` isn't itself branch-scoped — branch isolation only happens because `LabOrder`
(a `BranchEntity`) is a **required** navigation, so EF Core's query-filter cascade through that
required FK is what actually enforces "only this branch's data" whenever a query `Include`s it.
`VerifyResultAsync` and `GetResultByItemIdAsync` were missing that `Include(i => i.LabOrder)` —
confirmed live: `POST /lab/order-items/{id}/result/verify` and the result-view GET succeeded
against another branch's item while `EnterResultAsync`/`ApproveResultAsync` (which already
included `LabOrder`) correctly 404'd on the same id. Fixed by adding the same `Include` those two
already had; no schema change. Radiology had the identical gap — see `docs/modules/radiology.md`.

## Deferred / notes

- **No lab-order button on the OPD workspace** — see "Coupling to OPD" above; **closed
  2026-09-16**, see "OPD order bridge" above.
- **No enforced billing gate** — see "Billing check" above. **Made visible 2026-09-16**: an
  "Unbilled Orders Report" (`LabOrderService.GetUnbilledOrdersReportAsync`, `GET
  /api/v1/lab/orders/unbilled-report`, `UnbilledOrdersReportPage` at `/lab/unbilled-report`)
  lists every item with a result entered (or further along) that still has `BillItemId ==
  null`, closing `docs/PATIENT_JOURNEY_GAP_ANALYSIS.md` §4.3 the advisory way — it changes
  nothing about the gate itself, purely reporting. Radiology got the identical treatment in
  the same pass (`RadOrderItem` gained its own `BillItemId`, previously absent entirely — see
  `docs/modules/radiology.md`).
- **"Barcode" is a scannable sequence-generated code, not barcode rendering/scanning
  hardware integration.** `Sample.SampleNumber` (`SMP-2026-000123`) is unique and could be
  rendered as a barcode image the same way `GenerateQrCode` already does for other
  documents, but no barcode-specific generation or scanner input exists yet.
- **IPD/ER order sources don't exist yet** — those need Phase 9 (IPD) and Phase 16
  (Emergency) respectively; only the OP-visit and direct-walk-in paths are wired up.
- TAT is computed from `LabOrder.OrderDate` (a `DateOnly`, midnight UTC) to
  `LabResult.ApprovedAt` — approximate by less than a day, since order date has no time
  component; documented rather than silently treated as precise.


## Go-live QA fixes (2026-11-13)

From the functional QA pass (`qa-reports/`). Migration `20261113_GoLive_QaFixes`.

- **Billing gate:** OP lab orders (linked to an OP visit, not ER/IP) cannot have a sample collected until the test is on the bill and, by default, the bill is settled (`Billing.DiagnosticsGate`). This replaces the previous advisory-only posture.
- **Inpatient orders:** `LabOrder.AdmissionId` (+ "Order lab test" on the admission page); IP tests charge to the IP bill.
- **Orders:** unknown tests → 404; the same test twice in one order, or while an identical test is still awaiting collection for the patient, is rejected.
- **Results:** numeric parameters must be numbers; negatives are refused where every reference range is non-negative.
- Unbilled report defaults to the last 30 days when no dates are given.
