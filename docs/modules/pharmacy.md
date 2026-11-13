# Module Spec — Pharmacy (Phase 8)

Status: **complete** (2026-07-13). Owns the multi-store stock engine (Store/Rack/Batch,
backed by an append-only `stock_ledger`), the purchase workflow (Request → Order → GRN →
Return), OP POS sale with FEFO batch allocation and returns, department/emergency stock
issues and returns, and the inventory-control trio — approval-gated adjustments,
store/branch transfers, and count-sheet stock audits — plus near-expiry/low-stock alerts
and a drug consumption report.

## Database (schema `pharmacy`)

| Table | Notes |
|---|---|
| `pharmacy.stores` / `pharmacy.racks` | Branch-scoped stock-holding locations (main pharmacy store, OT store, ward store…) and their storage racks (informational only, not stock-tracked at rack granularity). One default store per branch is seeded (`MAIN`) so day-one GRN/sale flows have somewhere to post. |
| `pharmacy.batches` | One purchased lot of a drug in a store, created/topped-up by posting a GRN or receiving a transfer. `QuantityOnHand` is a denormalized running balance — the ledger is the source of truth it's reconstructable from. |
| `pharmacy.stock_ledger` | **Append-only**, never updated or deleted (plan §7). `MovementType`'s In/Out suffix carries the sign — `Quantity` is always positive. `ReferenceType`/`ReferenceId` (a string discriminator + long, not N nullable FKs) point at whichever document caused the row: `Grn`, `PurchaseReturn`, `PharmacySale`, `SalesReturn`, `Issue`, `IssueReturn`, `StockAdjustment`, or `StockTransfer`. |
| `pharmacy.purchase_requests` / `_items` | Internal request to buy drugs. `Draft → Submitted → Approved/Rejected`; approving converts nothing automatically — a PO referencing it is created separately. |
| `pharmacy.purchase_orders` / `_items` | Raised against a vendor. `Draft → Approved → PartiallyReceived/Received` (rolled up from GRN postings against it) `→ Cancelled`. Approval is a status gate only — no stock effect. |
| `pharmacy.grns` / `_items` | Goods Receipt Note. Supplier-invoice fields (`SupplierInvoiceNumber`/`Date`) are folded directly into the header rather than a separate `SupplierInvoice` entity — a GRN and its invoice are 1:1 in this flow, so a join table would be redundant. `Draft → Posted` — posting is the transactional step that creates/tops up a `Batch` per line and writes a `PurchaseIn` ledger row. |
| `pharmacy.purchase_returns` / `_items` | Return stock to a vendor. Created and posted in one step (no draft review) — by the time someone opens this screen the return decision has already been made. |
| `pharmacy.pharmacy_sales` / `_items` | OP POS sale. `PrescriptionId` is an optional loose link to an OPD prescription (same traceability-only pattern as Lab's `ClinicalOrderId`); `BillItemId` is a soft, unenforced link to `billing.bill_items`. |
| `pharmacy.sales_returns` / `_items` | Return sold items to stock. |
| `pharmacy.issues` / `_items` | Non-sale stock issue. `IssueType`: `Department`/`Emergency` are real, usable flows; `Ip` is schema-ready only (see "IP issue" below). |
| `pharmacy.issue_returns` / `_items` | Return issued items to stock. |
| `pharmacy.stock_adjustments` / `_items` | Reason-coded correction, signed `QuantityChange` per line. **The one true approval-gated workflow this phase** — matches Billing's discount-approval posture. Requesting writes nothing to stock; approving does. |
| `pharmacy.stock_transfers` / `_items` | Store↔store (same branch) or branch↔branch. `Pending → Dispatched` (writes `TransferOut` at the source) `→ Received` (creates/tops up the destination batch, writes `TransferIn` there). |
| `pharmacy.stock_audits` / `_items` | Count sheet. Starting one snapshots every in-stock batch's system quantity for the chosen store; recording counts is free-form until completion, which posts an `AdjustmentIn`/`AdjustmentOut` ledger row per non-zero variance — the same declared-vs-system idiom as Billing's `DayClose`. |

Migration: `Phase8_Pharmacy`. `Drug.ReorderLevel` (used by the low-stock alert) was added
to the Phase 2 `Drug` entity in this phase's own migration, alongside `Vendor`'s existing
placeholder columns (`HsnCode`/`TaxId`/`Schedule`, added back in Phase 2 in anticipation of
this phase).

## FEFO batch allocation

`StockMath.AllocateFefo` (pure, no I/O — mirrors Billing's `BillMath` / Lab's `LabMath`)
takes the caller's requested quantity and a list of `(BatchId, ExpiryDate, Available)`
candidates, sorts by expiry, and greedily allocates until either the request is fully
covered or stock runs out (returning the shortfall as `Remaining` rather than throwing —
the caller, `PharmacySaleService.CreateSaleAsync`, decides that's a `DomainException`). A
sale line can therefore split across multiple batches transparently; each allocation gets
its own `PharmacySaleItem` row and its own `StockLedger` entry.

## Every stock-affecting action is a mandatory transaction

Per CLAUDE.md's explicit list ("pharmacy sale/issue, GRN, stock transfer/adjustment"),
`GrnPage`'s post action, `PharmacySaleService.CreateSaleAsync`/`CancelSaleAsync`,
`IssueService.CreateIssueAsync`, `StockService.DecideAdjustmentAsync`,
`StockService.DispatchTransferAsync`/`ReceiveTransferAsync`, and
`StockService.CompleteAuditAsync` all run inside `IUnitOfWork.ExecuteInTransactionAsync`.
Creating a *draft* document (PR, PO, GRN before posting, a pending adjustment/transfer/
audit before its stock-affecting step) is a plain single-`SaveChangesAsync` call — nothing
about a draft touches stock, so it doesn't need the transactional guarantee.

`StockLedgerWriter` (an internal static helper, same shape as Billing's `DayCloseGuard`)
is the single choke point every service goes through to update a batch's running balance
and append the matching ledger row in the same call — "update the balance" and "log the
movement" can never drift apart. It takes the `Batch` by navigation property rather than
`BatchId` so it works even when the batch was just created and hasn't been saved yet (EF
fixes up the FK at `SaveChanges`); the causing document's own id, by contrast, must already
exist before the call, since `ReferenceId` is a plain `long`, not a FK.

## Consumption report nets returns, not just outbound totals

`StockService.GetConsumptionReportAsync` sums `SaleOut − SaleReturnIn` and
`IssueOut − IssueReturnIn` per drug, not just the outbound movements. This was a real bug
caught during live end-to-end testing: a POS sale that gets fully cancelled writes both a
`SaleOut` row (at sale time) and a `SaleReturnIn` reversal row (at cancel time) — summing
only `SaleOut` would report a cancelled sale as "consumed," which is wrong. Netting the two
gives the true figure (zero, for a fully-cancelled sale).

## Endpoints (`/api/v1/pharmacy`)

- `stores`, `stores/racks`: CRUD
- `purchase-requests`: paged `GET`, `GET {id}`, `POST`, `POST {id}/decide`
- `purchase-orders`: paged `GET`, `GET {id}`, `POST`, `POST {id}/approve`
- `grn`: paged `GET`, `GET {id}`, `POST` (draft), `POST {id}/post` (writes stock)
- `purchase-returns`: `GET`, `GET {id}`, `POST` (creates and posts in one step)
- `sales`: `GET batch-availability?drugId=&storeId=`, paged `GET`, `GET {id}`, `POST`
  (FEFO-allocated), `POST {id}/cancel` (reverses stock), `PUT {id}/pay-type` ("Pharmacy Pay
  Type Modify", added 2026-07-27 — corrects a Completed sale's `PaymentModeId`; no stock/ledger
  effect, so it's a plain permission-gated write, not a transaction), `GET {id}/invoice` (PDF)
- `sales-returns`: `GET`, `POST`
- `issues`: `GET?issueType=`, `GET {id}`, `POST`
- `issue-returns`: `GET`, `POST`
- `stock/batches?drugId=&storeId=&onlyInStock=`, `stock/batches/{id}/ledger`,
  `stock/alerts/near-expiry?withinDays=`, `stock/alerts/low-stock`,
  `stock/consumption-report?fromDate=&toDate=`
- `adjustments`: `GET` (pending), `GET {id}`, `POST`, `POST {id}/decide`
- `transfers`: `GET`, `GET {id}`, `POST`, `POST {id}/dispatch`, `POST {id}/receive`
- `audits`: `GET`, `GET {id}`, `POST` (snapshots system quantities), `POST {id}/counts`,
  `POST {id}/complete` (posts variances)

## Permissions

`PHARMACY.STORE.{VIEW,MANAGE}`, `PHARMACY.PURCHASEREQUEST.{VIEW,CREATE,APPROVE}`,
`PHARMACY.PURCHASEORDER.{VIEW,CREATE,APPROVE}`, `PHARMACY.GRN.{VIEW,CREATE}` (create covers
both drafting and posting), `PHARMACY.PURCHASERETURN.{VIEW,CREATE}`,
`PHARMACY.SALE.{VIEW,CREATE,CANCEL,MODIFY}`, `PHARMACY.SALESRETURN.{VIEW,CREATE}`,
`PHARMACY.ISSUE.{VIEW,CREATE}`, `PHARMACY.ISSUERETURN.CREATE`,
`PHARMACY.STOCK.{VIEW,ADJUST,APPROVE,TRANSFER,RECEIVE,AUDIT}`, `PHARMACY.REPORT.VIEW`.

## Frontend (`src/features/pharmacy`)

- `StoresPage` (`/pharmacy/stores`) — Store CRUD.
- `PosSalePage` (`/pharmacy/sales/new`) — store + required payment mode + optional patient,
  add-to-cart with live batch-availability preview, checkout.
- `SalesListPage`/`SaleDetailPage` (`/pharmacy/sales`, `/pharmacy/sales/:id`) — list,
  detail with cancel, print invoice, a partial-quantity return modal, a "Pay Type" summary
  field, and a "Modify pay type" action (reuses Billing's `usePaymentModes` hook, same master
  the POS Cashier Shift/discount screens already use).
- `PurchaseRequestsPage`/`PurchaseOrdersPage`/`GrnPage`/`PurchaseReturnsPage` — list +
  create modal per stage; GRN's list has an inline "Post" action.
- `StockExplorerPage` (`/pharmacy/stock`) — batch list with drug/store filters; clicking a
  row opens its full stock-ledger history.
- `IssuesPage` (`/pharmacy/issues`) — list + create (Department/Emergency only).
- `StockAdjustmentsPage` (`/pharmacy/adjustments`) — pending queue with approve/reject +
  a create form.
- `StockTransfersPage` (`/pharmacy/transfers`) — list with dispatch/receive actions +
  create form (from-store, to-branch, to-store).
- `StockAuditsPage` (`/pharmacy/audits`) — start an audit for a store, edit counts inline,
  complete to post variances.
- `AlertsPage` (`/pharmacy/alerts`) — tabbed near-expiry / low-stock views.
- `ConsumptionReportPage` (`/pharmacy/consumption-report`) — date-range drug consumption.

## OPD prescription bridge (added 2026-09-16)

Status: **complete**. Closes `docs/PATIENT_JOURNEY_GAP_ANALYSIS.md` §4.4: an OPD
prescription's `PrescriptionId` link on `PharmacySale` was always set manually by the
pharmacist finding the right prescription at the POS screen — nothing carried the doctor's
prescribed list into Pharmacy at all. `ConsultationWorkspace`'s Prescription tab now has a
"Send to Pharmacy" action (enabled once the prescription is saved and has at least one item)
that navigates to `PosSalePage` with router state — the same `{ <name>: {...} }` prefill
convention Lab/Radiology's OPD order bridge just established
(`docs/modules/lab.md`/`docs/modules/radiology.md`): patient info skips `PatientPicker`, and
the cart is pre-populated with each prescribed drug at a placeholder quantity of 1 and 0%
discount — the pharmacist still picks the actual dispensed quantity per line (dose ≠ stock
unit, same reasoning MAR's stock link uses) and still allocates against real batches via the
existing FEFO flow; nothing here auto-decides that part. `prescriptionId` is threaded through
to `CreateSaleRequest` automatically on checkout, closing that traceability link for free.

## Deferred / notes

- **No IP issue generation path.** `IssueType.Ip` exists on the enum and nothing blocks
  creating one manually, but there's no admission or medication order to generate it from
  until Phase 9 (IPD) exists — same posture as Billing's `BillType.Ip`. **Partially closed
  2026-09-16**: Nursing's MAR now writes a direct `StockLedger` row (not an `Issue`) when a
  nurse optionally links a batch while confirming an administered dose — see
  `docs/modules/nursing.md`'s "MAR now optionally decrements Pharmacy stock" note. `IssueType.Ip`
  itself is still never auto-generated from anything; the two mechanisms (a manual `Ip` `Issue`,
  and MAR's own direct ledger write) are separate paths onto the same `StockLedger`.
- **No sales-returns/issue-returns list pages.** Both actions are reachable (sales return
  via the sale detail page's "Return items" button; the `issue-returns` API exists and is
  tested) but there's no dedicated browse screen for either — a small, self-contained
  follow-up if needed, not a re-architecture.
- **`PosSalePage` now collects a payment mode at checkout (fixed 2026-09-17)** — closes
  `docs/PATIENT_JOURNEY_GAP_ANALYSIS.md` §4.5. The screen now has a required "Payment mode"
  `Select` (same `usePaymentModes` hook the "Modify pay type" modal already used) next to the
  Store selector; Checkout is disabled until one is picked, and `CreateSaleRequest.
  PaymentModeId` is threaded through from real selection instead of a hardcoded `null`.
  "Pharmacy Pay Type Modify" stays in place as a correction path for sales made before this
  fix and for the rare case a cashier needs to fix a mis-picked mode after the fact — it is
  no longer the *only* way a sale ever gets a payment mode.
- **Alerts are on-demand queries, not a scheduled job.** The plan calls for "Near Expiry &
  Minimum Stock (Hangfire + dashboard widgets)"; this phase built the query endpoints and
  an `/pharmacy/alerts` page, but no recurring Hangfire job or dashboard widget pushes
  them proactively — a natural follow-up using the same job pattern as Phase 4's
  `NoShowJob`.
- **"GST invoice" is a QuestPDF invoice with per-line tax shown, not a statutory
  e-invoice/IRN integration** — same honest scoping as every other "X invoice/report" PDF
  in this codebase.
- **No barcode scanning on the POS screen** — drug selection is a searchable dropdown, not
  a barcode/name-search combo box; the plan's "barcode/name search" is only half built.
- Purchase Order approval and GRN posting are independent — approving a PO doesn't gate
  whether a GRN can reference it, and a GRN can be created without a PO at all (a direct,
  unplanned purchase). This mirrors how Billing's OP bill generation isn't gated on
  anything upstream either.
- **`StockAdjustmentsController.GetPending`'s permission fixed 2026-09-17** — the pending-
  queue endpoint behind `/pharmacy/adjustments` required `PHARMACY.STOCK.APPROVE`, while the
  menu item, the frontend route guard, and the sibling `GetById` endpoint all gate on
  `PHARMACY.STOCK.ADJUST`. A role holding only `STOCK.ADJUST` (a pure requester, matching this
  workflow's own request/approve separation of duties) could open the page but its one data
  query would 403 immediately — invisible with the seeded `PHARMACY_MANAGER` role, which holds
  both permissions together, but a real break for any narrower custom role. `GetPending` now
  requires `PHARMACY.STOCK.ADJUST`, matching `GetById` and the menu/route gate.
- **GRN ↔ Purchase Order linkage now has a UI path (fixed 2026-09-17).** Previously flagged:
  `PurchaseService.PostGrnAsync` already rolled a PO's status to `PartiallyReceived`/
  `Received` and updated `PurchaseOrderItem.ReceivedQuantity` when `Grn.PurchaseOrderId` was
  set, but `GrnPage`'s "New GRN" modal hardcoded `purchaseOrderId: null` and there was no
  "Receive" action anywhere on `PurchaseOrdersPage` — every UI-created GRN was necessarily
  the "direct, unplanned purchase" path, and the backend capability was reachable only via a
  direct API call. Fixed, no backend change needed (the rollup logic was already correct and
  is unchanged): `PurchaseOrdersPage` now has a "Receive (GRN)" action on any `Approved`/
  `PartiallyReceived` order (`PHARMACY.GRN.CREATE`-gated), which navigates to `GrnPage` with
  the PO id via router state — same prefill idiom Emergency's STAT-order buttons originated
  and the OPD→Lab/Rad/Pharmacy bridges reused. `GrnPage`'s create modal, when opened this
  way, fetches the PO, locks the vendor to the PO's vendor, and seeds one line per
  still-outstanding item (`orderedQuantity − receivedQuantity`) with drug/item, rate, and
  tax prefilled. Batch number, expiry and MRP still can't come from a PO — those are only
  known at physical receipt — so the user still fills those three fields per line from the
  actual delivery before posting. Verified end-to-end live: create PO → approve →
  Receive (GRN) → post → PO rolls to `Received` with the correct `ReceivedQuantity`.


## Go-live QA fixes (2026-11-13)

From the functional QA pass (`qa-reports/`). Migration `20261113_GoLive_QaFixes`.

- **Expired stock:** GRN lines for medicines need an expiry after today; posting re-checks; FEFO allocation, batch availability, IP issues and OT usage skip/refuse expired batches (`StockMath.IsUsable`).
- **MRP is tax-inclusive:** POS line total = MRP × qty − discount; GST is the included portion (`StockMath.ComputeMrpInclusiveLineAmounts`).
- **Negative stock is impossible:** `StockLedgerWriter` refuses any movement that would take a batch below zero; adjustments are also checked at request time.
- **Maker-checker:** the user who raised a stock adjustment cannot approve it.
- **Returns:** sales and issue returns are limited cumulatively to the quantity sold/issued; a sale with returns cannot be cancelled; an IP issue return reduces the charged IP bill line.
- **Prescription dispensing** checks the prescription belongs to the selected patient; unknown payment modes return 404 instead of 500.
- **GRN:** MRP below purchase rate and receipts above the quantity still due on the PO are rejected. Concurrent sales lock candidate batch rows.
- Speed Multi seeder now creates the branch's "Main Pharmacy" store (`SM-PHARM`).
