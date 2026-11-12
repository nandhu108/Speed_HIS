# Module Spec — Stores & Inventory (Phase 13)

Status: **complete** (2026-07-18). Genuinely widens Phase 8's pharmacy stock engine to
carry general (non-drug) items — linen, stationery, consumables, biomedical spares —
rather than duplicating it, per the architecture decision below. Adds `StoreType` to
`Store` (descriptive only, not enforced), a nullable `ItemId` alongside `DrugId` on
`Batch`/`PurchaseRequestItem`/`PurchaseOrderItem`/`GrnItem`, and a new indent
("request → approve → issue") workflow that sits in front of the existing Issue engine.

## Architecture decision: widen, don't duplicate

Investigating Phase 8's actual entity coupling found that only `Batch` and the PR/PO/GRN
line-item tables reference `DrugId` directly — everything downstream (`Issue`,
`StockAdjustment`, `StockTransfer`, `StockAudit`, `StockLedger`) already only references
`BatchId`, with no drug-specific logic. That meant genuinely widening the engine —
nullable `DrugId` + new nullable `ItemId` on the coupled entities, "exactly one of the two
is set" as a service-layer invariant (not a DB check constraint, same posture as Rad's
`RadOrderItem` exactly-one-of pattern) — was a surgical, low-risk change, not the
full-rewrite it might first appear to be. `StockMath`, `StockLedgerWriter`, and every
downstream service needed no changes at all; `PharmacySaleService` (OP POS) stays
drug-only by design — general stores aren't dispensed to patients at the pharmacy counter.

## Database (schema `pharmacy`)

| Table | Notes |
|---|---|
| `pharmacy.stores.store_type` | New column, `StoreType` enum (`Pharmacy`/`General`/`Linen`/`Stationery`/`Consumables`/`BiomedicalSpares`). Purely descriptive — nothing stops a `General` store from also holding a drug batch. Backfilled to `Pharmacy` for every pre-Phase-13 store. |
| `pharmacy.batches` | `DrugId` is now nullable; new nullable `ItemId`. `ExpiryDate`/`Mrp` are now nullable too — general items (a bed sheet, a ream of paper) often have neither. Two unique indexes now exist — `(DrugId, StoreId, BatchNumber)` filtered `WHERE drug_id IS NOT NULL` and `(ItemId, StoreId, BatchNumber)` filtered `WHERE item_id IS NOT NULL` — so a drug batch and a general-item batch can never collide on the same key even though they share one table. |
| `pharmacy.purchase_request_items` / `purchase_order_items` / `grn_items` | Same nullable-`DrugId` + new-`ItemId` widening. `GrnItem.ExpiryDate`/`Mrp` are nullable for the same reason as `Batch`. |
| `pharmacy.indents` / `indent_items` | New. `Indent`: `StoreId`, `DepartmentId`, `IndentNumber` (`IND-{year}-{seq}`), `Status` (`Requested → Approved/Rejected`, then `Issued` once an Issue references it), `RequestedByUserId`/`DecidedByUserId`/`DecidedAt`/`RejectionReason`. `IndentItem`: exactly-one-of `DrugId`/`ItemId`, `RequestedQuantity`, nullable `ApprovedQuantity` (defaults to the requested quantity if the approver doesn't trim a line). |
| `pharmacy.issues.indent_id` | New nullable FK — traceability only, nothing requires an issue to come from an indent. |

Migration: `Phase13_StoresInventory`.

## Indent → approve → issue

`IndentService` owns the request/decide lifecycle; it never touches stock itself.
Approving (`DecideAsync`) sets each line's `ApprovedQuantity` — the approver can trim any
line below what was requested, or leave it to default to the requested quantity.
Fulfilling is just a normal `IssueService.CreateIssueAsync` call with `IndentId` set:
`IssueService` then validates, per product line on the indent, that the sum of issued
quantities across the batches on that issue doesn't exceed the line's approved quantity,
and flips the indent to `Issued` once the issue is created. This mirrors the existing
"header workflow gates a downstream stock-affecting action" shape used by PO→GRN — the
indent is the gate, the issue is what actually moves stock.

## Consumption report is now department-wise

`StockService.GetConsumptionReportAsync` groups by `(DrugId, ItemId, DepartmentName)`
instead of just the product, resolving `DepartmentName` from the `Issue` an `IssueOut`/
`IssueReturnIn` ledger row references (`null` for OP pharmacy sale rows, which have no
department). This was the plan's explicit "Consumption Report (department-wise)"
requirement; low-stock alerts were widened the same way, unioning drug and item reorder
checks into one `LowStockProductDto` list.

## Endpoints (`/api/v1/pharmacy`)

- `stores`: `SaveStoreRequest` now carries `StoreType`.
- `purchase-requests` / `purchase-orders` / `grn`: item DTOs/requests now carry nullable
  `DrugId`+`ItemId` instead of a required `DrugId`; `GrnItem`'s `ExpiryDate`/`Mrp` are
  optional when `ItemId` is set.
- `stock/batches?drugId=&itemId=&storeId=&onlyInStock=`, `stock/alerts/low-stock` (now
  `LowStockProductDto[]`), `stock/consumption-report` (now `ProductConsumptionRowDto[]`,
  department-wise) — all widened, same routes.
- `issues`: `CreateIssueRequest` now carries `IndentId`.
- `indents` (new): paged-free `GET?status=`, `GET {id}`, `POST`, `POST {id}/decide`
  (`{ approve, rejectionReason, items: [{ indentItemId, approvedQuantity }] }`).

## Permissions

New: `PHARMACY.INDENT.{VIEW,CREATE,APPROVE}`. Everything else reuses Phase 8's existing
`PHARMACY.*` codes — no permission was renamed or removed.

## Frontend (`src/features/pharmacy`)

- `ProductPicker` (`components/ProductPicker.tsx`) — shared Drug/Item type toggle + Select,
  used by the Purchase Request, Purchase Order, GRN, and Indent create forms so each line
  can point at either master.
- `StoresPage` — Store form now has a `StoreType` field; list shows a Type column.
- `StockExplorerPage` — a second filter (Item, alongside Drug) mutually clears the other;
  batch rows/ledger modal show the resolved product name regardless of which master it's
  from; expiry/MRP render `—` when null.
- `IndentsPage` (`/pharmacy/indents`, new) — list with status badges, a create modal
  (store/department/lines via `ProductPicker`), and a detail modal that lets an approver
  trim each line's quantity before approving, reject with a reason, or — once approved —
  jump to `IssuesPage` with the store/department/indent pre-filled via router state to
  fulfil it as a stock issue.
- `IssuesPage` — reads an optional `location.state.prefillIssue` (set by `IndentsPage`'s
  "Fulfil" button) to auto-open the create modal with the indent's store/department/id
  already filled in; the create payload now always sends `admissionId`/`indentId`.
- `ConsumptionReportPage` — new Department column; `AlertsPage`'s low-stock tab and every
  batch-picker dropdown across Adjustments/Transfers/Audits/Purchase Returns now show
  `productName` (drug or item) instead of an assumed `drugName`.

## Deferred / notes

- **No product-level UOM/rack picker on GRN lines for general items** — a general-item GRN
  line uses the same batch-number/qty/rate fields as a drug line; there's no
  item-category-specific form variation.
- **`PharmacySaleService` (OP POS) intentionally stays drug-only** — general stores aren't
  sold to patients at the pharmacy counter, so `SaleLineRequest`/`BatchAvailabilityDto`
  were left untouched (only the nullable-`Batch.ExpiryDate`/`Mrp` compile fallout was
  fixed, with a null-forgiving `.Value` since a drug batch is always fully populated by
  `CreateGrnItemRequestValidator`'s drug-required rules).
- **Indent quantity validation is per-product, not per-batch** — `IssueService` sums the
  issued quantity across whichever batches an issuer picks for a product and checks that
  against the indent line's approved quantity; it doesn't require the issuer to use a
  single batch or split proportionally.
