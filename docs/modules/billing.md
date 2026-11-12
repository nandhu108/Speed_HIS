# Module Spec — Billing (Phase 6, extended)

Status: **complete** (2026-07-13), extended 2026-07-22. Owns OP/IP/Emergency billing end
to end — bill generation, tariff-based rate resolution, discount threshold-approval
workflow, payment collection + receipt PDF, refunds with approval, credit billing, bill
cancellation, patient advances, and branch day close. (Note: IP bed-charge auto-feed and
`GenerateIpBillAsync` actually landed with Phase 9/IPD — the "IP billing has no
generation path" line from this doc's original Phase 6 write-up is stale; see
`BillingService.GenerateIpBillAsync`/`RefreshBedChargesAsync`.)

**2026-07-22 "full-fledged billing" extension** (gap-closing against a legacy HIS billing
menu — see the plan file for the full mapping): advance drawdown against a bill, an IP
credit-limit exposure warning, append-only bill notes, post-finalization bill credit
notes, and bill parking. Corporate/Insurance-linked legacy menu items were explicitly
excluded — that's Insurance/TPA, a held module (plan §21).

## Database (schema `billing`)

| Table | Notes |
|---|---|
| `billing.bills` | Bill header. Global-unique `bill_number` (`BILL-2026-000123`, per branch+year via `ISequenceService`). Unique-when-not-null on `op_visit_id` — a visit can only produce one non-cancelled bill. Carries `SubTotal`/`BillDiscountPercent`/`DiscountAmount`/`TaxAmount`/`TotalAmount`/`PaidAmount`/`BalanceAmount` all recomputed server-side by `BillMath`, never trusted from the client. `CorporateId`/`InsuranceCompanyId` are placeholders per plan §6/§21. |
| `billing.bill_items` | Line items; `SourceType` (Consultation/ClinicalOrder/Service/Package). `Description`/`Rate`/`TaxPercent` are **snapshotted at add-time** so a historical bill still reads correctly if the catalog changes later. `ClinicalOrderId` links a stub line back to its OPD order (Phase 5). |
| `billing.bill_payments` | One row per payment collection. Global-unique `receipt_number` (`RCPT-2026-000123`, same per-branch/year sequence mechanism as bill numbers). |
| `billing.refunds` | Refund request against a bill (optionally against one specific payment). `Status`: Pending/Approved/Rejected. |
| `billing.discount_approvals` | Row created only when a requested discount is above the auto-approve threshold (see below) — auto-applied discounts never create a row. |
| `billing.advances` | Patient deposit, global (not visit/bill scoped). `BalanceAmount` tracked but never drawn down here — Phase 9 (IPD) is what consumes deposits. |
| `billing.day_closes` / `billing.day_close_mode_amounts` | One `day_close` per `(branch_id, date)` (unique, partial). Child rows are system vs. declared amount + variance, one per payment mode touched that day. |
| `billing.payment_modes` | Global (not branch-scoped) catalog — Cash/Card/UPI/Cheque seeded by default. `RequiresReference` gates whether a reference number is mandatory at collection time. |

Migration: `Phase6_Billing`.

## Bill generation and tariff rate resolution

`BillingService.GenerateOpBillAsync` guards against a duplicate non-cancelled bill for
the same `OpVisitId`, then adds one `Consultation` line (`Rate = OpVisit.ConsultationFee`,
already snapshotted by Phase 5) plus one zero-rate `ClinicalOrder` stub line per
non-cancelled order on that visit — Phase 7/12 will give those real rates once Lab/
Radiology exist to price them.

Manually adding a `Service` line resolves its rate via `BillMath.ResolveTariffRate`:
prefers a patient-category-specific `Tariff` over a category-agnostic one, then the most
recently effective row within the branch and as-of date, falling back to
`Service.DefaultRate` if nothing matches. The selection itself is a pure function over
plain `TariffCandidate` records — `BillingService` does the EF query, `BillMath` does the
picking — so the rule is unit-testable without a database.

## Discount workflow

An **ad-hoc discount** (no `DiscountTypeId`) always routes to approval — there's no
threshold to compare against, so `BillMath.RequiresApproval` treats it as threshold 0. A
**typed discount** (`DiscountType`) caps the requested percent at `MaxPercent` and
auto-applies immediately if it's at or below that type's own `ApprovalThresholdPercent`;
above it, a `DiscountApproval` row is created and the discount is *not* applied until an
approver decides it. This applies identically at the item level and the bill level — the
only difference is `DiscountApproval.BillItemId` being null for a bill-level request.

Bill-level discounts are modeled as `Bill.BillDiscountPercent` (not a flat stored amount)
so they keep recomputing correctly as items are added/removed/re-discounted:
`BillMath.RecomputeBillTotals` applies the bill-level percent to `(SubTotal minus the sum
of item discounts)`, not to the raw subtotal — a 10% item discount and a 5% bill discount
compose rather than double-count.

## Payments, refunds, day close — all transactional

`CollectPaymentAsync` and `DecideRefundAsync` both run inside
`IUnitOfWork.ExecuteInTransactionAsync`, per CLAUDE.md's non-negotiable list. Both are
also guarded by `DayCloseGuard.EnsureOpenAsync` (bill generation too) — once a branch's
day is closed, no new bill/payment can be recorded for that date. Approving a refund
reduces `Bill.PaidAmount` and recomputes `BalanceAmount`/`Status`, falling back to `Open`
if `PaidAmount` drops to zero (not stuck at `PartiallyPaid`/`Paid`).

`CancelAsync` requires `Bill.PaidAmount == 0` — a bill with any payment on it must be
fully refunded first; there's no separate cancellation-approval step, just a required
reason logged via `IActivityLogger`.

`DayCloseService.CloseDayAsync` groups that date's `BillPayments` by mode into system
amounts, merges in the cashier's declared amounts (rows with no declared amount default to
0, not the system amount — a silent no-input isn't the same as "confirmed correct"),
computes `Variance = Declared − System` per mode, and marks the day closed. Closing again
for an already-closed `(branch, date)` throws.

## Receipt PDF

`QuestPdfService.GenerateBillReceipt` follows the same A5/A6 QuestPDF layout conventions
as the Phase 3/5 documents (registration card, visit slip/summary): header
(branch name/address), a line-item table, a totals block, and — when printed for a
specific payment — a receipt-number/mode/amount footer block. `GET
/api/v1/billing/bills/{id}/receipt?paymentId=` returns the latest payment's receipt by
default, or a specific payment's when `paymentId` is given.

## Full-fledged billing extension (Batch 1)

- **Advance drawdown** — `BillingService.DrawAdvanceAsync` finally closes the gap this doc
  used to flag ("deposits stay separately visible/collectible, not yet consumed by
  billing"): it decrements `Advance.BalanceAmount` and increments `Bill.PaidAmount` in one
  transaction, reusing the exact balance/status recompute `CollectPaymentAsync` already
  does. No `BillPayment`/receipt is written since no cash actually moves.
- **Advance auto-drawdown (2026-08-08)** — `DrawAdvanceAsync` above stayed a manual,
  pick-one-advance action; `BillingService.AutoDrawAdvancesAsync` (private, called from
  `RefreshBedChargesInternalAsync`) now sweeps *every* available advance on the admission
  against the bill's outstanding balance automatically, FIFO by `ReceivedAt`, reusing the
  same balance/status recompute. It runs on IP bill generation (the admission deposit is
  swept the moment the bill exists), on a manual bed-charge refresh, and from the new
  `IpBedChargeRefreshJob` nightly sweep below — so a running IP bill no longer needs a
  cashier to remember to draw the deposit down by hand. `DrawAdvanceAsync` remains for
  drawing a *specific* advance out of order.
- **Daily bed-charge refresh job (2026-08-08)** — `IpBedChargeRefreshJob`
  (`IIpBedChargeRefreshJob`, registered `Cron.Daily` as `"refresh-ip-bed-charges"` in
  `Program.cs`) closes the "no daily Hangfire job auto-refreshing charges" gap this doc used
  to flag. It calls `BillingService.RefreshAllOpenIpBedChargesAsync`, which finds every open
  (`Status` not Cancelled/Paid/Parked) IP bill, recomputes its `BedCharge` lines from
  `BedAllocation` history in its own transaction (one bill's failure can't block the rest —
  `RefreshBedChargesInternalAsync` is idempotent, so a retried job is harmless), and applies
  the advance auto-drawdown above. Runs with no HTTP context, so the per-request checks
  (`RequireBranch`, `EnsureCanAccessBillTypeAsync`) are bypassed by design, same posture as
  `NoShowJob`. `RefreshBedChargesAsync` (the manual endpoint) is unchanged and still available
  for an on-demand recompute (e.g. right before discharge).
- **IP credit limit** — `Admission.CreditLimit` (nullable, set via
  `AdmissionService.SetCreditLimitAsync`) is a front-desk-configured warning threshold,
  never a hard block. `BillingService.GetByIdAsync` computes `CreditExposure = 
  BalanceAmount − sum(available advances)` for any bill on that admission and returns it
  alongside `CreditLimit` on `BillDetailDto`; the frontend renders it as a banner, not a
  gate.
- **Bill notes** — `BillNote` is an append-only remark log (`bills/{id}/notes`,
  `GET`/`POST`) — no edit/delete, matching the audit-table convention elsewhere.
- **Bill credit notes** — `BillCreditNote`/`BillCreditNoteItem` model a post-finalization
  value adjustment against specific lines of a Paid/PartiallyPaid bill: request → approve
  (two-step, same shape as `DiscountApproval`/`Refund`) → `Bill.TotalAmount` decreases and
  `BalanceAmount` recomputes. This is deliberately distinct from `Refund` — a credit note
  corrects what was billed; if the patient ends up overpaid as a result, that excess is
  returned separately through the existing Refund workflow. Approval posts a Journal
  voucher (Debit Patient Revenue / Credit Accounts Receivable) via a new
  `IFinanceLedgerPoster.PostCreditNoteAsync`, the same no-op-until-configured posture as
  `PostBillPaymentAsync`.
- **Bill parking** — `BillStatus.Parked` blocks edits/payments/discounts the same as
  `Paid`/`Cancelled` (`LoadEditableBillAsync`, `CollectPaymentAsync`). `ParkAsync`/
  `UnparkAsync` gate on separate `BILLING.BILL.PARK`/`BILLING.BILL.PARKAPPROVE`
  permissions; unparking recomputes status from `PaidAmount` vs. `TotalAmount` (same
  expression `DecideRefundAsync` uses), not a stored "status before parking" field.

New permissions: `BILLING.ADVANCE.DRAW`, `BILLING.NOTE.MANAGE`,
`BILLING.CREDITNOTE.{VIEW,CREATE,APPROVE}`, `BILLING.BILL.{PARK,PARKAPPROVE}`. New
endpoints all under `bills/{id}`: `advances/draw` (`POST`), `notes` (`GET`/`POST`),
`park`/`unpark` (`POST`), `credit-notes` (`POST`); plus `credit-notes` (`GET` pending) and
`credit-notes/{id}/decide` (`POST`) mirroring `discount-approvals`/`refunds`. New
frontend: park/unpark/draw-advance/request-credit-note actions and a notes panel on
`BillDetailPage`, plus `CreditNotesApprovalPage` and `ParkedBillsPage`.

## Full-fledged billing extension (Batch 2 — masters & config)

- **`Masters.BillEstimateTemplate`/`BillEstimateTemplateItem`** — reusable Service/Package
  line sets (e.g. "Normal Delivery Package Estimate") that seed a `Batch 4` `BillEstimate`.
  Branch-scoped standard CRUD shaped exactly after `PackageService`
  (`BillEstimateTemplateService`, `Features/Masters/Billing/`).
- **`Masters.DoctorPayoutRule`** — doctor fee/commission share per `ServiceCategory`
  (`PayoutType.Percent` or `.Flat`), unique per (branch, doctor, category). Backs the
  Doctors Payout / Performing Doctor reports (Batch 5) and the "Doctor Fee Control Panel"
  legacy item — there's no separate control-panel entity, just this master's CRUD screen.
  Report-only; no ledger posting (payroll/HR is a held module, plan §21).
- **"Nationality & Pricelist Mapping"** closed by reuse, not new schema: seeded a
  `FOREIGN` (`PatientCategory`) row — `Tariff` already resolves rate by
  `PatientCategoryId` (`BillMath.ResolveTariffRate`), so a nationality-tier price is just
  another tariff row keyed to this category, same as `GEN`.
- **"Control Panel - Billing"** closed via the existing generic Admin Settings screen
  (`SystemSetting`, `Group = "Billing"`), not a new entity: `SettingKeys.BillingReceiptFooterNote`
  is a real, wired setting — `BillingService.GenerateReceiptAsync` reads it and passes it
  through to `BillReceiptData.FooterNote`, printed on the receipt PDF footer in place of
  the old hardcoded thank-you line when set.

New permissions: `MASTER.BILLESTIMATETEMPLATE.{VIEW,MANAGE}`,
`MASTER.DOCTORPAYOUTRULE.{VIEW,MANAGE}`. New endpoints: `billestimatetemplates` and
`doctorpayoutrules`, standard CRUD (`GET`/`GET {id}`/`POST`/`PUT {id}`/`DELETE {id}` —
`DoctorPayoutRule` has no detail endpoint, the list DTO carries everything). New frontend:
`BillEstimateTemplatesPage`/`DoctorPayoutRulesPage` under `/masters`.

## Full-fledged billing extension (Batch 3 — cashier shift settlement)

- **`CashierShift`/`CashierShiftModeAmount`** close the legacy "SETTLEMENTS - USER" / "USER
  SETTLEMENT - PHYSICAL CASH/RECEIVE/VIEW" gap `DayClose`'s own doc comment flagged:
  "the plan's finer-grained counter_sessions (multiple simultaneous cashier counters) is
  not modeled." A `CashierShift` sits *alongside* `DayClose`, not instead of it — `DayClose`
  is still the single branch-day gate blocking bill/payment creation; a shift is a
  per-user lens over the same `BillPayment` rows via a new nullable
  `BillPayment.CashierShiftId`, set from the collecting user's currently-open shift in
  `BillingService.CollectPaymentAsync` (null when no shift is open, so day-close keeps
  working standalone).
- Workflow: `Open` (declares an opening float; one `Open` shift per user at a time,
  enforced by a partial unique index) → cashier collects payments all shift → `Close`
  (groups this shift's tagged payments by mode into system amounts, cashier declares
  actuals, variance computed — same shape as `DayCloseService.CloseDayAsync`) → a
  supervisor `Receive`s the physical cash, closing the loop. `GetCollectionAsync` gives a
  live system-total preview for an still-open shift so the cashier knows what to expect
  before declaring.
- New permissions: `BILLING.CASHIERSHIFT.{VIEW,MANAGE,RECEIVE}` (`MANAGE` covers a
  cashier opening/closing their own shift; `RECEIVE` is the supervisor's separate action).
  New endpoints under `billing/cashier-shifts`: `GET` (paged, supervisor view), `GET mine`,
  `POST open`, `GET {id}/collection`, `POST {id}/close`, `POST {id}/receive`. New
  frontend: `CashierShiftsPage`.

## Full-fledged billing extension (Batch 4 — bill estimation)

- **`BillEstimate`/`BillEstimateItem`** — the actual pre-admission cost-estimate document
  (Batch 2's `BillEstimateTemplate` is just the reusable line-set that seeds one). Rate
  resolution reuses `BillMath.ResolveTariffRate` — the exact same rule
  `BillingService.AddManualItemAsync` uses — so an estimate's numbers track what the
  patient would actually be billed at today's tariff. Purely informational: `BillEstimate`
  never auto-converts into a real `Bill`; front desk creates that separately once the
  patient proceeds, matching the codebase's manual-refresh house style (e.g. IPD's
  `RefreshBedChargesAsync`).
- Workflow: `Create` (either from a template's lines, loaded client-side into an editable
  list, or built ad hoc) → `Copy` (duplicates lines into a fresh Draft with a new number —
  "Bill Estimations Copy") → `GeneratePdfAsync` (flips Draft→Printed on first print, never
  blocks further edits) → `Delete`. No approval workflow — an estimate carries no
  financial commitment.
- New permissions: `BILLING.ESTIMATE.{VIEW,CREATE}` (`CREATE` covers create/copy/delete —
  there's no separate approval step to gate). New endpoints under `billing/estimates`:
  standard CRUD plus `POST {id}/copy` and `GET {id}/pdf`. New frontend:
  `BillEstimatesPage` (list + create-from-template-or-ad-hoc editor + view/print/copy).

## Full-fledged billing extension (Batch 5 — reports)

`BillingReportService` — row-level detail reports over live tables (not the day-aggregated
`reporting` materialized views Custom Report Builder's generic executor dispatches
against), same posture as `FinanceReportService`/`MrdReportService`. No new schema; every
query reuses entities this billing extension (or earlier phases) already built.
Deliberately does **not** duplicate reports that already exist elsewhere: plain
"Outstanding Report" is `FinanceReportService.GetOutstandingReportAsync` (linked from
Finance's own reports page); "Collection Report"/"Revenue Report"/"OP-IP Census" are the
Phase 23 materialized-view dashboards. What's genuinely new here is what those don't
cover:

- **Ageing** — the same outstanding-bills shape as Finance's report, bucketed by
  days-outstanding (0-30/31-60/61-90/90+).
- **Ward-wise Outstanding** — outstanding IP-bill balance grouped by the admission's
  current ward (`Admission.CurrentBed.Room.Ward`).
- **Day Book** — one date's bills generated + payments collected, chronological.
- **Bill Cancelled**, **Concession** (approved `DiscountApproval` rows), **Refund**,
  **Advance** — straightforward date-ranged listings over each entity, each already built
  by earlier Billing phases/batches.
- **Tariff List** — flat `Tariff`/`TariffItem` listing (service, rate, patient category,
  effective window) — a pure master-data report, no date range.
- **Doctors Payout / Performing Doctor** — matches each Service-sourced `BillItem` to the
  bill's OP doctor (`OpVisit.Doctor`) or IP consultant (`Admission.PrimaryConsultant`),
  looks up that doctor's Batch 2 `DoctorPayoutRule` for the item's service category, and
  sums the computed payout. Package lines are excluded (no single service category) — a
  documented scope trim, not an oversight.
- **User Settlement Denomination** — Batch 3's `CashierShiftModeAmount` rows, date-ranged
  by shift close time.
- **Service-wise Breakup / Header-wise Collection** — `BillItem` grouped by
  `Service.ServiceCategory` ("header" in the legacy naming).
- **IP Service Billing Detail** — `BillItem` detail restricted to `BillType.Ip`.

`BillQuery` (the existing bills list) gained a `BillType?` filter, so "OP Bill Report" /
"IP Bill Generated Report" / "Bill Statement" / "Bill Enquiry" / "OP-IP Bill Copy Report"
are just `BillsListPage` + `BillDetailPage` + the existing receipt reprint filtered by
type — no new backend needed for those.

**Explicitly not built**: client-branded report variants ("... MAPIMS", "... QB MAPIMS")
from the legacy menu — these are report-template/export-formatting concerns for a specific
client presentation, not a distinct feature; the base report already covers the data.
"Supplementary Bill" needs no new schema either — an IP bill already accepts new line
items any time it's Open/PartiallyPaid.

New permission: `BILLING.REPORT.VIEW` (covers every `BillingReportService` report — no
per-report split, matching `PHARMACY.REPORT.VIEW`'s precedent). New endpoints under
`billing/reports`: `ageing`, `ward-wise-outstanding`, `day-book`, `cancelled-bills`,
`concession`, `refund`, `advance`, `tariff-list`, `doctors-payout`, `user-settlement`,
`service-category-breakup`, `ip-service-billing-detail`. New frontend:
`BillingReportsPage` (tabbed, one tab per report), registered in the Custom Report Builder
catalog as a linked `BILLING_REPORTS` row.

## Full-fledged billing extension (Batch 5 addendum — Bill Report search + CSV/Excel/PDF export)

Answers "what about the OP [report]?" against the legacy `OP BILL REPORT` search screen:
`BillQuery` gained `CorporateId`/`BilledByUserId` filters (alongside the `BillType` filter
added earlier) — `BillingService.FilterBills` is now the one shared filter-building method
`GetPagedAsync` (screen) and the new `GetForReportAsync` (unpaged, for export) both call,
so the two never drift. The **Bill Report** tab on `BillingReportsPage` — the first tab,
covering both "OP BILL REPORT" and "IP BILL GENERATED REPORT" from the legacy menu via the
Bill Type filter — exposes Bill No/Patient/UHID search, Bill Type, Billed User (`Bill.CreatedBy`),
Company (`Corporate`), and a date range, matching the legacy screen's field set.

**Every report now exports CSV/Excel/PDF**, reusing the Custom Report Builder's existing
`ReportExportHelper.ToFileResult` (already built for Phase 23's generic executor) instead
of a bespoke export mechanism:
- `BillsController`'s new `GET bills/export?format=` flattens `GetForReportAsync`'s rows
  into a `ReportResultDto` inline.
- `BillingReportService.GetReportResultAsync(reportKey, range, date)` is a single
  dispatcher — one `switch` flattening any of the 12 Batch-5 reports into the same
  `ReportResultDto` shape — backing `BillingReportsController`'s one shared
  `GET {key}/export?format=` action, so there's one export code path for all twelve
  reports rather than twelve bespoke ones.

Frontend: `downloadBillReport`/`downloadBillingReport` (`features/billing/api.ts`) trigger
a real file download (not just opening a tab) via a temporary anchor element; every tab on
`BillingReportsPage` (and the new Bill Report tab) has CSV/Excel/PDF buttons next to its
date-range filter.

## Full-fledged billing extension (Batch 5 addendum — per-bill-type RBAC scoping)

Answers "I need to set separate [permissions] for OP and IP... so the [Bill Type] filter
will hide": three new permissions, `BILLING.BILL.VIEWOP`/`VIEWIP`/`VIEWEMERGENCY`, scope
*which BillType rows* a role/user's Bills/Reports queries return — on top of, not instead
of, the existing `BILLING.BILL.VIEW` (which still gates the Bills/Reports feature as a
whole). This is checked in-service, not via `[HasPermission]`: `BillingService` and
`BillingReportService` both compute the caller's allowed `BillType` set from
`PermissionService.GetUserPermissionsAsync` (`GetAllowedBillTypesAsync`, duplicated in
each service — same small-duplication posture as `ResolveTariffRate`) and narrow every
query to it. An explicit `BillType` filter outside that set throws `ForbiddenException`
(403, with a real message — not `UnauthorizedAccessException`, which the global handler
maps to a generic 401); no filter at all silently narrows results instead of erroring, so
an OP-only cashier's plain bill search just never surfaces IP bills. `BillingService.GetByIdAsync`
enforces the same check per bill, so opening an IP bill's detail page directly by ID is
also blocked for an OP-only user. `WardWiseOutstanding`/`IpServiceBillingDetail` (inherently
IP-only reports) require `VIEWIP` outright.

Seeded onto every existing bill-viewing role (all three, everywhere `BILLING.BILL.VIEW`
already appears) so current behavior — see every type — doesn't regress; an admin narrows
a specific role or user by *removing* one of these three in the existing Roles/Users
permission UI, same as any other permission (no new admin screen needed — the permission
catalog there is already generic over `PermissionRegistry.All`).

Frontend: `BillingReportsPage`'s Bill Type filter (on the Bill Report tab) checks all
three permissions — holding only one hides the filter entirely (query is pinned to that
type); holding two or three shows a `Select` limited to just those options. The
Ward-wise Outstanding / IP Service Billing Detail tabs are hidden outright without
`VIEWIP`. `BillDetailPage` now shows a real error message (previously an infinite spinner)
if `GetByIdAsync` 403s.

## Full-fledged billing extension (Batch 6 — OP Billing patient search)

Closes the "no visit-to-bill navigation link yet" gap this doc used to flag, matching the
legacy "OP Bill Payment" patient search screen: a cashier now finds a patient by searching
**visits**, not by typing an OPD Visit ID.

- **`BillingService.SearchOpVisitsForBillingAsync`** — a new query over `OpVisit` (not `Bill`,
  since unbilled visits by definition have no `Bill` row yet), filtered by date range, patient
  name, doctor name, reg no (`Patient.Uhid`), mobile, gender, and IP number (matches any
  `Admission.AdmissionNumber` for that visit's patient — a walk-in OP patient can still have a
  past/current IP admission). Branch scoping comes from `OpVisit`'s own global query filter, same
  as every other `OpVisit` query — no manual branch check needed. Each row left-joins the
  patient's non-cancelled `Bill` for that visit (if any) so the frontend can offer "Open" instead
  of "Generate bill".
- **Three legacy checkboxes, each mapped to a real existing concept rather than new schema**:
  `MrdOnly` narrows to visits whose patient has an MRD `RecordFile` (Mrd is admission-scoped, so
  this only ever matches previously-admitted patients — a documented scope trim, not a bug);
  `EmrOnly` narrows to patients with at least one EMR `ClinicalNote`; `PackageOnly` narrows to
  `OpVisit.Category == Mhc` — the "OP Multiple Visit Package" checkbox, which is really asking
  for MHC checkup-package visits, the only multi-visit-package concept this codebase has.
- **No new permission.** The search itself is gated by the existing `BILLING.BILL.VIEW`
  (consistent with `OPD.VISIT.VIEW` gating both the OPD queue and front desk); clicking a row
  either navigates straight to an existing bill or calls the existing `generate-op` endpoint
  (`BILLING.BILL.CREATEOP`) — the same idempotency guard `GenerateOpBillAsync` already had
  (`DomainException` on a duplicate non-cancelled bill for the visit) covers the rare
  double-click/two-cashiers race without new locking.
- **`BillDetailPage` gained a "Previous Bills" panel** — the legacy "Previous Outstanding" and
  "Previous Bill History" tabs collapsed into one panel with an "Outstanding only" checkbox and
  an optional date range, since both are just filtered views of the same patient's other bills.
  No new backend endpoint: it reuses `useBills({ patientId })`, the same query `BillsListPage`
  already had.

New endpoint: `GET bills/op-search` (`OpBillingSearchQuery` → paged `OpBillingSearchResultDto`).
New frontend: `OpBillingSearchPage` (`/billing/op-search`), linked from the Billing menu
alongside `Bills`.

## Menu layout: tabs-in-one-page vs one-menu-item-per-report

The legacy OP billing menu exposed each report (Ageing, Day Book, Concession, Refund, ...)
as its own named menu entry; `BillingReportsPage` bundles all 13 behind one "Billing
Reports" item with an internal tab bar instead. Rather than pick one shape for everyone,
`OP_BILLING` gets the legacy split-item view while every other `BILLING.REPORT.VIEW` role
keeps the single tabbed page, using a new **generic, reusable** Admin/Menu primitive that
lives in `MenuService`/`Menu` (not billing-specific — the next module that wants a
tabs-vs-separate-items choice can reuse it without new schema):

- `Menu.HiddenByDefault` — a menu item seeded with this set to `true` is off for every
  role until that role has an explicit `MenuRoleLayout` override with `Hidden = false`.
  `MenuService.GetMyMenuAsync`/`GetTreeForRoleAsync` fall back to it (instead of
  hardcoded "visible") when no per-role override row exists yet. Nothing else about the
  per-role override machinery changed — the existing "By menu" admin tree's Eye/EyeOff
  toggle (`MenuPermissionTree.tsx`) already opts a role in or out of *any* item, default-
  hidden or not, with zero new UI.
- `DbSeeder` seeds a new "Reports" folder under Billing (matching the legacy menu's own
  separate REPORTS section, rather than flattening everything alongside Bills/Day
  Close/...), moving the existing combined "Billing Reports" item into it and adding 13
  sibling `Menu` rows (`hiddenByDefault: true`), each pointing at
  `/billing/reports/<key>` — the same `BillingReportsPage`, deep-linked to one tab via a
  route **segment**, not a query string. This matters: the app's tab-workspace
  (`TabWorkspaceContext.openInActiveTab`) reuses an already-open tab for a sidebar click
  sharing the same *pathname*, ignoring any query string (deliberately — a list page's
  own `?page=2` drift shouldn't spawn a duplicate tab). Two menu items differing only by
  `?tab=` collapse to "already open" after the first click and silently do nothing on
  every click after — a route segment makes each of the 13 a genuinely distinct pathname,
  exactly like every other individually-linked report page elsewhere in this codebase.
  `BillingReportsPage` reads the tab from `useParams()` (`reports/:tab?`, optional so the
  combined page's own bare `/billing/reports` still works), falling back to the default
  tab if the requested key isn't in that user's `visibleTabs` (e.g. an IP-only report
  without `BILLING.BILL.VIEWIP`). 11 items reuse `BILLING.REPORT.VIEW`; the two inherently
  IP-only ones (Ward-wise Outstanding, IP Service Billing Detail) use
  `BILLING.BILL.VIEWIP` instead, matching the tab bar's own IP filter.
- Landing on one of the 13 individual routes renders standalone — no tab bar, no "Billing
  Reports" title, just that one report's own title and content, the same shape as every
  other single-purpose report page. Only the bare `/billing/reports` (the combined item)
  shows the tab bar; the sidebar's "Reports" folder is the navigation between reports on
  the split-item path, not an in-page switcher.
- `SeedOpBillingReportMenuLayoutAsync` is the only role-specific wiring: it shows all 13
  new items for `OP_BILLING` and hides the combined "Billing Reports" entry for that role
  only. Every other role keeps the combined page, just now one level deeper inside the new
  "Reports" folder instead of flat under Billing — the only change for them.
- An admin can rebuild the same split for any other role (or module) without touching
  code: Admin → Roles → Permissions → "By menu" tab, toggle the eye icon on the items to
  show/hide, exactly as with any other menu item.

## Concession Report → OP Bill Concession Search

The Concession Report tab was a plain date-ranged list (Bill No/Patient/Percent/Amount/
Reason). Rebuilt to match the legacy "OP Bill Concession Search" screen — a genuine search,
not a summary: `BillingReportService.SearchConcessionsAsync`/`ConcessionSearchQuery`
(`GET billing/reports/concession/search`, paged) filters approved `DiscountApproval` rows
by date range plus Reg No (`Patient.Uhid`), Patient Name, "OT No" (`OpVisit.TokenNumber` —
the legacy label for the OPD visit's daily token number) and Bill No, all case-insensitive
partial matches (`EF.Functions.ILike`, same idiom as `BillingService.SearchOpVisitsForBillingAsync`).
Filter-building is split into a private `BuildConcessionQuery` so the paged on-screen search
and the unpaged `GetReportResultAsync("concession", ...)` CSV/Excel/PDF export (date-range
only, no text filters) never drift — same posture as `BillingService.FilterBills`.

Each row now carries the bill/patient/doctor context the legacy grid shows: Bill Date,
Age/Gender (`IpdMath.FormatAge`, same helper `SearchOpVisitsForBillingAsync` uses),
Doctor/Dept (`Bill.OpVisit.Doctor.User`/`.Specialty` — null for IP/Emergency-origin bills,
same nullability as everywhere else Doctor is optional), and Bill Amount. The legacy
screen's single "Requested/Approved User"/"Requested/Approved Date" column is really two
different people/times — `DiscountApproval` has no explicit "requested by" field (it
reuses the audit-stamp `CreatedBy`/`CreatedAt`, same as `BillQuery.BilledByUserId` → `Bill.CreatedBy`
elsewhere) plus `DecidedByUserId`/`DecidedAt` for the approver — both are resolved (one
batched `Users` lookup, not N+1) and shown stacked in one cell rather than dropping the
requester to match the legacy column count.

New frontend: `ConcessionTab` (`BillingReportsPage.tsx`) rewritten as a self-contained
search panel (own date range + Reg No/Patient Name/OT No/Bill No fields + Search/Cancel,
paged `DataTable`) instead of using the tabbed page's shared date-range row — the same
`OpBillingSearchPage`-style search+grid pattern, not the plain `<Table>` every other tab
here still uses.

## Report Search Panel — the standard layout for a legacy "search screen"

Several legacy screens (OP Bill Payment, OP Bill Concession Search, Advance Payment, OP
Report...) share one shape: a dense grid of labeled filter fields plus a date range, a
Search/Go action, and a results grid you act on row by row — not a plain date-ranged list.
This is now the standard pattern for building one of those in Prixo, reusing existing UI
primitives only (no new component library):

- **Filter panel**: one `rounded-shell border ... p-4 shadow-shell-sm backdrop-blur-xl`
  container. `<DateRangeFilter>` (existing two-field from/to control — a legacy screen's
  single combined date-range *badge* is a distinct, heavier component that hasn't been
  built; this is the deliberate, existing substitute) on its own row, then the rest of the
  fields in a responsive grid (`grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4`, more
  columns as more fields are needed) using plain `<Input>`/`<Select>`/checkbox `<label>`s —
  every field labeled, matching the legacy screen's label-above-field style.
- **Actions**: a `Reset` (secondary) and `Search`/`Go` (primary) button pair, right-aligned
  at the bottom of the panel (`mt-3 flex justify-end gap-2`) — not auto-searching on
  keystroke. Filters live in local draft state; only committed into the actual query on
  Search, exactly as `OpBillingSearchPage.PatientSearchPanel` already did before this
  pattern had a name.
- **Results**: a paged `<DataTable columns={...} result={data} isLoading page onPageChange
  rowKey emptyMessage />`, not a hand-rolled `<Table>`/`<thead>`/`<tbody>`.

First applied to `OpBillingSearchPage` (pre-existing), then `ConcessionTab` and
`AdvancesPage` (below) — the reference examples for the next one. This is for a genuine
*search* screen (typed filters, click a row to act on it); a plain flat report with no
search fields beyond a date range uses the different pattern below instead.

## ReportResultTable — the standard shape for a plain flat report

The legacy "OP Report" screen (`OpReportPage.tsx`/`OpdReportsController`, built for OPD
visits) established a second pattern, for a report that's just "pick filters, see a table"
— no per-row action, unlike a Report Search Panel: a generic `{ reportName, columns, rows }`
result (`Prixo.Application.Features.Reports.ReportResultDto`, the exact shape
`ReportExportHelper`'s CSV/Excel/PDF export already used everywhere), rendered by one
shared table component with a show/hide column picker, client-side pagination (the report
query itself isn't server-paged — the whole result loads, then the table pages through
it), a sticky header, and a duplicate top scrollbar synced to the real one so a wide
table's horizontal scrollbar isn't out of sight below the fold.

Billing's 11 plain-list report tabs (Ageing, Ward-wise Outstanding, Day Book, Bill
Cancelled, Refund, Advance, Tariff List, Doctors Payout, User Settlement, Service-wise
Breakup, IP Service Billing Detail) used to each hand-roll their own `<Table>`/typed hook
— no pagination at all (a real gap for a large date range), no column picker, and every
report needed its own bespoke Row/Report TypeScript interface. Rebuilt onto this pattern:

- **`ReportResultTable`** (`components/ui/ReportResultTable.tsx`) — extracted from
  `OpReportPage`'s table/pager/column-picker code, generalized to take any `ReportResult`.
  Presentational only; filters/Run/Reset/Export stay with the caller. Two additions beyond
  what `OpReportPage` had: a **search box** (client-side — the whole date-ranged result is
  already fetched in one shot, same as the pager, so filtering it needs no round trip;
  matches any visible column's stringified value) closing the "every report needs search"
  gap these 11 tabs had (none had any filter beyond a date range before), and a **sticky
  first column** alongside the existing sticky header — a frozen top-left corner cell where
  both meet — so a wide report's leading identifying column (Bill No, Ward, Tariff, ...)
  stays visible while scrolling right. `DataTable` (`BillReportTab`/`ConcessionTab`/every
  admin list page) got the same sticky-header + sticky-first-column treatment for
  consistency, without a column picker (its typed `Column<T>[]` shape doesn't carry a
  stable string key the way `ReportResultColumn` does) — those screens already have their
  own dedicated search fields, so they didn't have the search gap `ReportResultTable` closes.
- **Backend**: `BillingReportsController.GetResult` (`GET billing/reports/{key}`) replaces
  what used to be 11 separate typed actions (`GetAgeing`, `GetRefund`, ...) with one,
  mirroring `{key}/export` — both call the exact same `GetReportResultAsync` dispatcher, so
  the on-screen table and the exported file never drift (same posture as
  `OpdReportsController`'s per-report JSON+export action pairs). The 11 typed DTOs
  (`AgeingReportDto`, `RefundReportDto`, ...) and their service methods are unchanged and
  still used *internally* by the dispatcher's `switch` — only the controller surface
  collapsed, not the underlying report logic.
- **Frontend**: one `useBillingReportResult(key, range, date)` hook (`api.ts`) and one
  `GenericReportTab({ reportKey, range, date })` component (`BillingReportsPage.tsx`)
  replace the 11 typed hooks and 11 typed Tab components. The 11 typed Row/Report
  TypeScript interfaces in `types.ts` are gone with them.
- **Known trade-off**: the generic shape has no per-report totals row — Ageing's "Total
  outstanding," Refund's "Total approved," etc. are gone from the on-screen table. This
  isn't a new regression: the *exported* CSV/Excel/PDF never had a totals row either (same
  `Flatten()` result), so the on-screen view now just matches what export already showed.
  Day Book's two side-by-side tables (Bills Generated / Payments Collected) became one
  table with a "Kind" column distinguishing Bill vs. Payment rows — also already how its
  export was flattened.

`ConcessionTab` and `BillReportTab` are deliberately **not** on this pattern — they're
Report Search Panels (typed filters, per-row actions), not plain flat reports.

**Follow-up fixes after first landing this pattern**, closing gaps a live look at the Ageing
tab surfaced — and then a second pass that changed *where* the filter UI lives:

- **Ageing had no date parameter at all** — every other report either takes a date range or
  (Day Book) a single date; `GetAgeingReportAsync` took neither, always bucketing against
  "now." Matches the legacy "Ageing Report **As On Date**" naming for a reason: it gained an
  optional `asOnDate` param (defaults to today), which is the reference date buckets are
  computed against *and* now also excludes bills generated after it (a bill that didn't
  exist yet as of a past as-on-date shouldn't count as outstanding "as of" that date).
- **Filter card moved out of `ReportResultTable`, into each caller — including its search
  box** — two passes: the first had `ReportResultTable` wrap itself in a card *and* own
  Patient Name/Bill No detection directly, putting the whole filter row + table inside one
  box, plus its own separate "search all columns" box below that. Compared side by side with
  `BillReportTab` (no such extra row — just a filter card, then a Columns button, then the
  table), the second box read as visual clutter and the two screens didn't match. Rebuilt
  again: `ReportResultTable` now has **no filter UI of its own at all** — just the table half
  (column picker + pager + table, unwrapped, no card). It takes an `extraFilter?: (row) =>
  boolean` prop instead; the caller's own filter card supplies whatever counts as a match.
  `GenericReportTab` (`BillingReportsPage.tsx`) owns the real filter card per report — a
  `dateMode: 'range' | 'single' | 'none'` prop drives whether it shows a `DateRangeFilter`, a
  single date input (As On Date / Date), or no date control; Patient Name/Bill No fields
  appear once the loaded result reveals it has those columns; a report with **neither**
  (Ward-wise Outstanding, Tariff List, Doctors Payout, User Settlement, Service-wise Breakup)
  falls back to one plain "Search" field matching any column — still inside the *same* card,
  not a second row below the table. Every report's toolbar above its table is now just the
  Columns button, matching `BillReportTab` exactly. Date changes are draft-until-Search (a
  real network round trip); every text field (Patient Name/Bill No/fallback Search) filters
  the already-fetched rows live (no round trip, no reason to gate them behind a button).
- **`DataTable` gained the same Columns picker** `ReportResultTable` already had (using each
  `Column<T>.header` as its stable key, the same value already used as the React key) —
  opt-in via `enableColumnPicker`, now set on `BillReportTab`, `ConcessionTab`,
  `OpBillingSearchPage`, and `AdvancesPage`. `BillReportTab` also stopped hand-rolling its
  own `<table>`/`<thead>` and moved onto `DataTable` outright, gaining sticky header/column
  and the picker for free instead of a second bespoke implementation.
- **`DataTable` also gained a "Rows per page" selector** (opt-in via `onPageSizeChange` +
  optional `pageSizeOptions`, same default list `ReportResultTable` uses) — unlike
  `ReportResultTable`'s client-side one, this is server-paginated: changing it re-fetches at
  the new page size rather than re-slicing already-loaded rows, since `DataTable`'s
  `PagedResult<T>` only ever holds one page's worth of rows. Wired into `ConcessionTab`,
  `BillReportTab` (which gained its own local `pageSize` state to feed it), `AdvancesPage`,
  and `OpBillingSearchPage`.
- **`OpReportPage` (OPD/Front Office) migrated onto `ReportResultTable`** too — it was the
  *original* source of this whole pattern (sticky header, column picker, synced scrollbar),
  copy-pasted inline before being extracted; now it's a consumer like everything else,
  ~150 lines of duplicated table/pager/scroll-sync code deleted. Its own filter card
  (date range, Summary/Detail toggle, doctor/visit-type/department/etc. filters, Run/Reset/
  Export) is unchanged in content, just restyled onto the same `rounded-shell border
  border-border-soft bg-panel p-4 shadow-shell-sm backdrop-blur-xl` card convention
  Concession/OpBillingSearchPage/AdvancesPage already use (was `rounded-lg`/`bg-panel-solid`,
  a slightly different, older pair of classes doing the same job).
- **Not yet migrated**: several other OPD/IPD/ER/Appointments report pages
  (`IpReportPage`, `ErReportPage`, `MhcReportsPage`, `WardOccupancyReportPage`,
  `ConversionReportPage`, `AdmissionRequestConversionReportPage`,
  `DoctorAppointmentReportPage`, `CensusReportPage`) carry the *exact* same duplicated
  sticky-header/column-picker/scroll-sync code `OpReportPage` used to — confirmed by grep,
  not yet touched. Same migration `OpReportPage` just got, still pending.

## Advance Payment — encounter search instead of a patient picker

`AdvancesPage` used to be "pick one patient via a typeahead `PatientPicker`, then collect
an advance" — no way to browse/filter which encounter you meant, no encounter context
(Corporate, doctor, age/gender) before collecting. Rebuilt as a Report Search Panel over
the *same* OP encounter search `OpBillingSearchPage` already uses
(`useOpBillingSearch`/`SearchOpVisitsForBillingAsync`) — Patient Name/IP No/OP Multiple
Visit Package filters, a results grid (Encounter Date/Reg No/Patient Name/Age-Sex/
Doctor-Dept/Corporate Name), and a "Collect advance" action per row that opens the
existing collect form (payment mode/amount/reference/notes, unchanged) plus that patient's
advance history below. No new backend endpoint — this reuses the OP visit search wholesale
rather than building a parallel one.

The one real backend addition: `OpBillingSearchResultDto` gained `CorporateName` (from the
matched Bill's `Corporate.Name`, null until a bill exists for that visit — same
nullability posture as the existing `BillId`/`BillNumber`/`Status` fields), since the
legacy Advance Payment grid shows it and `OpBillingSearchPage` benefits from it too (both
consume the same DTO). Deliberately scoped to OP encounters only — IP admission deposits
are collected through IPD's own admission flow, not this screen (see `docs/modules/ipd.md`),
so a Bill Type OP/IP/Both filter wasn't added.

## Endpoints (`/api/v1/billing`)

- `bills`: paged `GET`, `GET {id}`, `POST generate-op`, `GET op-search` (OP Billing patient search)
- `bills/{id}/items` (`POST`/`PUT {itemId}`/`DELETE {itemId}`)
- `bills/{id}/items/{itemId}/discount`, `bills/{id}/discount` (`POST`, may return a pending `DiscountApprovalDto`)
- `bills/{id}/payments` (`POST`, collect), `bills/{id}/receipt` (`GET`, PDF)
- `bills/{id}/cancel`, `bills/{id}/credit` (`POST`)
- `bills/{id}/refunds` (`POST`, request)
- `discount-approvals` (`GET` pending), `discount-approvals/{id}/decide` (`POST`)
- `refunds` (`GET` pending), `refunds/{id}/decide` (`POST`)
- `advances` (`POST` collect), `advances/patient/{patientId}` (`GET`)
- `day-close/collection?date=` (`GET`), `day-close?date=` (`GET`), `day-close/close` (`POST`)
- `payment-modes`: standard CRUD

## Permissions

`BILLING.BILL.{VIEW,CREATE,CREATEOP,CREATEIP,CREATEEMERGENCY,MANAGE,CANCEL}`, `BILLING.PAYMENT.COLLECT`,
`BILLING.REFUND.{REQUEST,APPROVE,ISSUE}`, `BILLING.DISCOUNT.APPROVE`,
`BILLING.ADVANCE.{VIEW,COLLECT,TRANSFER}`, `BILLING.DAYCLOSE.{VIEW,CLOSE}`,
`BILLING.PAYMENTMODE.{VIEW,MANAGE}`.

**OP/IP billing RBAC split**: OP bill generation is gated by `BILLING.BILL.CREATEOP`,
IP bill generation by `BILLING.BILL.CREATEIP`, and Emergency bill generation by
`BILLING.BILL.CREATEEMERGENCY`. `OP_BILLING` is the single combined OP-only role for the
legacy OP billing menu: OP bill generation, OP service billing, bill copy/receipt print,
payment collection, advance/pre-payment collection and drawdown, refund request/approval/issue,
post-concession approval, cashier settlement manage/receive/view, OP estimates, OP billing
masters and reports. It deliberately keeps only `BILLING.BILL.VIEWOP`/`CREATEOP`, so
IP/Emergency bill rows and generation stay outside this role. It also deliberately does
**not** hold `OPD.VISIT.VIEW` — no billing endpoint or page needs it, and holding it only
leaked the Front Office/OPD registration menus into this role's sidebar (`Menu.PermissionCode`
reuses the same code for those), which the legacy OP billing menu never had.

Legacy OP menu coverage:

| Legacy group | Current coverage |
|---|---|
| Price List Configure / Pricelist Creation / OP-IP Price Definition / OP Future Date Price Definition | `MASTER.TARIFF.{VIEW,MANAGE}` (`Tariff.EffectiveFrom/EffectiveTo`; export through tariff-list report) |
| Price List Import Export | `MASTER.TARIFF.IMPORT` — real CSV upsert (`TariffService.ImportCsvAsync`, one row per Tariff+Service[+RoomType]) and export (`TariffsController.Export`) in the same column shape, so a round-trip export → edit → re-import works |
| Service Creation | `MASTER.SERVICE.{VIEW,MANAGE}` |
| MHC Package Master | `MASTER.MHCPACKAGE.{VIEW,MANAGE}` |
| Header/Service-wise Concession | `MASTER.DISCOUNTTYPE.{VIEW,MANAGE}` + `BILLING.DISCOUNT.APPROVE` + Concession report |
| Source Names | `MASTER.REFERRER.{VIEW,MANAGE}` |
| Nationality & Pricelist Mapping | `MASTER.NATIONALITY.{VIEW,MANAGE}` + `MASTER.PATIENTCATEGORY.{VIEW,MANAGE}` + tariff patient-category pricing |
| Corporate Service Definition / Corporate Service Entry | `MASTER.CORPORATE.{VIEW,MANAGE}` + bill corporate/credit marking |
| Pre Payment / Advance Payment | `BILLING.ADVANCE.{VIEW,COLLECT,DRAW}` |
| Advance Transfer | `BILLING.ADVANCE.TRANSFER` — moves part/all of an advance's unused balance to a different patient and/or admission; creates a new `Advance` row for the target (append-only, mirrors "a rate revision is a new row") rather than mutating the source's PatientId/AdmissionId in place |
| Refund Request/Approve/Issue - Advance or OP Bill | `Refund.BillId`/`AdvanceId` are mutually exclusive nullable FKs (one `ck_refunds_bill_or_advance` constraint enforces exactly one is set) — a bill refund or an advance refund go through the same request → approve/reject → issue pipeline. `BILLING.REFUND.REQUEST` (`POST /billing/bills/{id}/refunds` or `POST /billing/advances/{id}/refunds`) → `BILLING.REFUND.APPROVE` (decision-only: sets `Approved`/`Rejected`, no balance mutation yet) → `BILLING.REFUND.ISSUE` (`POST /billing/refunds/{id}/issue`, the actual payout — reverses the bill's `PaidAmount`/`BalanceAmount`/`Status` or reduces the advance's `BalanceAmount`, sets `Issued`), mirroring `CashierShiftStatus`'s `Open→Closed→Received` two-step shape. `RefundApprovalsPage` shows both the Pending-approval and Approved-awaiting-payout queues. |
| OP Billing / OP Service Billing / OP Bill Print Copy / OP Refund / OS Payment | `BILLING.BILL.{VIEW,VIEWOP,CREATEOP,MANAGE}` + `BILLING.PAYMENT.COLLECT` + receipt PDF |
| Settlements - User / Physical Cash / Receive / View | `BILLING.CASHIERSHIFT.{MANAGE,VIEW,RECEIVE}` |
| OP Report / OP Bill Report / Bill Details / Bill Statement / OP-IP Bill Copy | Bills list/detail/export filtered by `BILLING.BILL.VIEWOP` |
| Concession / Bill Cancelled / Advance / Collection / Outstanding / Ageing / Refund / Day Book / Revenue reports | `BILLING.REPORT.VIEW` and dashboard/finance reports, with bill rows narrowed to OP by `VIEWOP` |
| Ward-wise Outstanding / IP Billing | IP Billing role/permissions only, not included in OP billing roles |
| IP Price Definition Bedtype Wise | `TariffItem.RoomTypeId` (nullable) — a service can carry both a room-agnostic rate and one or more room-type-specific override rows in the same tariff; `BillMath.ResolveTariffRate` prefers the row matching the most dimensions (patient category + room type), same match-or-null-fallback shape patient category already had |
| IP Future Date Price Definition (room charges) | New `RoomTypeCharge` master (RoomTypeId, Rate, EffectiveFrom, EffectiveTo — same "a rate revision is a new row" shape as Tariff), resolved via `IpdMath.ResolveRoomTypeCharge` at bed allocation/transfer time and snapshotted onto `BedAllocation.BedChargePerDay` as before; `RoomType.BedChargePerDay` stays the flat fallback when no dated row is effective |
| Client Specific Masters / Multi Window | Existing data-driven Admin Menus/Roles screens handle client menu layout; no separate business table is needed. |

## Frontend (`src/features/billing`)

- `BillingDashboardPage` (`/reports/billing`, lives in `features/reports` — the page itself
  is Phase 23's, not new here) is menu-nested under **Billing**, not just
  **Reports & Dashboards** — the Billing sidebar group leads with it, matching the legacy
  menu's shape (dashboard alongside its own transactions/reports rather than off in a
  separate cross-module reports area). Same permission (`DASHBOARD.BILLING.VIEW`),
  same page, just reachable from two menu paths.
- `BillsListPage` (`/billing/bills`) — searchable list + a "Generate OP bill" action
  (takes an OPD visit ID directly — a quick path for when the visit ID is already known;
  `OpBillingSearchPage` is the patient-search-driven path for everyone else).
- `OpBillingSearchPage` (`/billing/op-search`) — the legacy "OP Bill Payment" patient search:
  date range + patient name/doctor name/reg no/mobile/gender/IP number filters plus the
  MRD/EMR/OP-Multiple-Visit-Package checkboxes (see Batch 6 above), a results grid showing
  whether a bill already exists, and a row action that opens or generates it.
- `BillDetailPage` (`/billing/bills/:id`) — line items (add Service/Package, per-item
  discount, remove), bill-level discount, payment collection, refund request, cancel,
  mark-as-credit, print-receipt actions, and a Previous Bills panel (Batch 6), gated per
  action by permission and bill status (editing is blocked once `Paid`/`Cancelled`, matching
  `LoadEditableBillAsync`).
- `DiscountApprovalsPage` / `RefundApprovalsPage` — approval inboxes with an
  approve/reject modal (reject requires a reason, matching the validators).
- `AdvancesPage` — `PatientPicker` (reused from Appointments) + collect form + history.
- `DayClosePage` — today's (or any date's) collection by mode with editable declared
  amounts, or the closed summary with variance if already closed.
- `PaymentModesPage` — standard CRUD master, same shape as Phase 2's `TaxesPage`.

## Insurance awareness on the bill (added 2026-09-16)

Status: **complete**. Closes `docs/PATIENT_JOURNEY_GAP_ANALYSIS.md` §5.1: Billing's payment
collection screen had zero awareness of an in-progress Insurance pre-auth/claim on the same
encounter, and Insurance's claim worklist had zero visibility into whether the bill had
already been paid in cash — either direction could silently double-settle a bill (collect
cash *and* claim cashless, or claim cashless after the balance was already collected). Both
sides now surface the other's state, **advisory only** — same "banner not a gate" posture as
the existing `CreditLimit`/`CreditExposure` fields, not a new blocking rule.

- **Bill side**: `BillDetailDto.InsuranceStatus` (`BillingService.GetInsuranceStatusAsync`) —
  a not-yet-resolved `Claim` against this exact bill (`Status` not `Settled`/`Denied`)
  outranks a not-yet-resolved `PreAuthRequest` against the same `OpVisitId`/`AdmissionId`
  (`Status` not `Rejected`); reached via `db.Claims`/`db.PreAuthRequests` directly (same
  "reach into the other module's DbSets, no cross-service DI" posture as OT/Biomed reaching
  into Pharmacy). `null` when neither exists. `BillDetailPage` renders it as an amber banner
  next to the credit-limit one.
- **Claim side**: `ClaimDto` gained `BillPaidAmount`/`BillBalanceAmount` (the bill's *current*
  totals, not snapshotted at claim submission) — `ClaimsWorklistPage` shows a "Bill Paid"
  column, highlighted when cash has already been collected against a claim that isn't yet
  `Settled`/`Denied`.

Neither side blocks anything — `CollectPaymentAsync` and `ClaimService.SubmitAsync`/
`RecordSettlementAsync` are all unchanged; this is purely a visibility fix so staff can catch
the conflict themselves before it happens, the same trust-the-user posture the rest of this
codebase's advisory banners take.

## Online Payment Requests — staff worklist (added 2026-09-16)

Status: **complete**. Closes a gap flagged in `docs/PATIENT_JOURNEY_GAP_ANALYSIS.md` §8.2:
the Patient Portal's "Pay Online" action (`docs/modules/portal.md`) writes a
`billing.online_payment_requests` row and tells the patient "our billing team will contact
you," but until now there was no staff-side screen to read that queue at all — only the
Portal's own write endpoint existed. `OnlinePaymentRequestService` is a thin worklist over
the same table: list (optionally filtered by `OnlinePaymentRequestStatus`, `Requested` rows
sorted first), `Complete` (staff followed up and collected payment the normal way on the
bill), `Cancel` (mistake/duplicate/patient paid another way) — both terminal, one-way, same
"already actioned, cannot re-action" guard as `MarService.ActionAsync`. **It never moves
money itself** — the actual payment is still collected through the existing
`CollectPaymentAsync` flow on the bill; this only closes the outbox row once that's done.

Endpoints: `GET /api/v1/billing/online-payment-requests?status=`,
`POST .../{id}/complete`, `POST .../{id}/cancel`.

Permissions: `BILLING.ONLINEPAYMENT.{VIEW,MANAGE}` — granted to `BILLING_EXECUTIVE`
(and by extension `BILLING_SUPERVISOR`, which extends it) and `OP_BILLING`.

Frontend: `OnlinePaymentRequestsPage` (`/billing/online-payment-requests`) — a flat worklist
(requested/patient/bill/amount/status) with Mark Completed / Cancel row actions and a
click-through to the bill to actually collect payment.

## Billing Reports fix — Ward-wise Outstanding / Service-wise Breakup 500s (fixed 2026-09-17)

Status: **fixed**. Pre-go-live QA audit found both `GET /api/v1/billing/reports/ward-wise-outstanding`
(summary shape, `reportType` unset or `summary`) and `GET /api/v1/billing/reports/service-category-breakup`
threw a 500 on every call — not a permission or data issue, a genuine EF Core query-translation
failure (`InvalidOperationException: ... could not be translated`). Both queries did a `GroupBy`
immediately after a `Select` that reached a scalar through a 3–4 level join chain
(`Bill → Admission → Bed → Room → Ward` and `BillItem → Bill`/`BillItem → Service → ServiceCategory`
respectively) — EF Core 10 flattens the projection away and then can't translate the grouped
`Sum`/`Count` over the resulting `TransparentIdentifier`. `BillingReportService.GetWardWiseOutstandingAsync`
and `GetServiceCategoryBreakupReportAsync` now materialize the flat rows with `ToListAsync` first and
group in memory, same posture `GetAgeingReportAsync`'s bucket-summary branch already used. No DTO,
route, or frontend change — `BillingReportsPage`'s "Ward-wise Outstanding" and "Service-wise Breakup"
tabs (and their CSV/Excel/PDF exports, which share the same dispatcher) now return real data instead
of erroring on every load.

## Pre-go-live QA fix — Day Close / Cashier Shift declared-amount pre-fill (fixed 2026-09-17)

Status: **fixed**. `DayClosePage` and `CashierShiftsPage`'s close-shift modal both pre-filled
the "Declared amount" input with that mode's *system* amount (`value={declared[id] ?? String(m.amount)}`)
and, worse, fell back to the same system amount when computing the submitted payload
(`Number(declared[id] ?? m.amount)`) — so a cashier who clicked "Close day"/"Close shift"
without typing anything into a single field got a silent 0-variance close on every mode.
This directly defeated the reconciliation intent `DayCloseService.CloseDayAsync`/
`CashierShiftService.CloseAsync` were built around (see "Payments, refunds, day close" above:
"rows with no declared amount default to 0... a silent no-input isn't the same as a confirmed
correct") — the backend's own 0-default for an omitted mode was unreachable because the
frontend always submitted one row per mode, populated with the system figure whenever the
cashier hadn't actually counted it. Fixed: both inputs now start **empty** (system amount shown
only as a `placeholder`, not a value) and an untouched field submits `0`, matching the backend's
documented default and forcing a genuine mismatch to surface instead of being masked. No backend
change — `CloseDayRequest`/`CloseCashierShiftRequest` and their services were already correct.

## Pre-go-live QA fix — "Mark as credit" never set a Corporate (fixed 2026-09-17)

Status: **fixed**. `MakeCreditBillAsync` (`POST bills/{id}/credit`) always accepted an optional
`CorporateId` and `BillDetailPage`'s "Mark as credit" button was its only caller — but the
button hardcoded `{ corporateId: null }` with no picker, so **no bill marked credit through the
UI could ever carry a Corporate**. Two knock-on effects: it silently broke the "Corporate Service
Entry" legacy mapping this doc's own Permissions table claims ("bill corporate/credit marking"),
and it made every such bill permanently invisible to Finance's Receivables worklist, which
requires `Bill.CorporateId != null` to list a credit bill as pending (`ReceivableService.
GetPendingBillsAsync` — see the Pending Credit Bills 500 fix in `docs/modules/finance.md`; that
fix only stopped the *crash* on a null-CorporateId row, it didn't address why every credit bill
had a null CorporateId to begin with). Fixed: "Mark as credit" now opens a small modal
(`MakeCreditBillModal`) with an optional Corporate `<Select>` (sourced from the existing
`useCorporates` master) before calling the same unchanged endpoint — a personal (no-corporate)
credit bill is still a valid choice, it's just an explicit one now instead of the only possible
outcome. No backend change.

## Pre-go-live QA fix — Credit Note Approve/Reject shown to view-only users (fixed 2026-09-17)

Status: **fixed**. `CreditNotesApprovalPage` rendered its Approve/Reject buttons for anyone
holding `BILLING.CREDITNOTE.VIEW` (the page's own route gate) with no `BILLING.CREDITNOTE.APPROVE`
check, unlike `RefundApprovalsPage`'s `canApprove` gating for the equivalent action — so a
view-only credit-note user saw buttons that would always 403 on click (`CreditNotesController.
Decide` is correctly gated server-side; nothing was ever approved without permission, this was a
misleading affordance, not a security hole per CLAUDE.md's "frontend checks are cosmetic, the API
is the gate"). Now gated by `canApprove` the same way `RefundApprovalsPage` already does.

## Deferred / notes

- **OPD's `ConsultationWorkspace` still doesn't have a "Generate bill" button** — that
  cross-module wiring is a separate, smaller follow-up. What *is* now closed (Batch 6) is the
  bigger gap: front desk/cashiers no longer need to know an OPD Visit ID at all, since
  `OpBillingSearchPage` finds the visit by patient details first.
- **Advances are now drawn down against a bill** via `DrawAdvanceAsync` (manual) and, since
  2026-08-08, automatically via `AutoDrawAdvancesAsync`/`IpBedChargeRefreshJob` (the
  "full-fledged billing" extension above) — no longer a standalone deferred item.
- **Bed charges now refresh on a daily schedule**, not manual-only — see
  `IpBedChargeRefreshJob` in the extension note above. `RefreshBedChargesAsync` (manual) is
  unaffected.
- **IP billing generation** (`GenerateIpBillAsync`, bed-charge auto-feed) landed with
  Phase 9/IPD, not in this phase — see the extension note at the top of this doc.
- **Rate revision is Phase 2's `Tariff.EffectiveFrom/EffectiveTo` versioning**, not a new
  concept introduced here — this phase adds the resolution query (`BillMath.ResolveTariffRate`)
  that actually picks a rate from among the versions at bill time.
- Bill generation itself is a single insert, not wrapped in an explicit transaction (payment
  collection and refund approval are, per CLAUDE.md's transactional-boundary list — bill
  generation only ever writes one aggregate root in one `SaveChangesAsync`).
