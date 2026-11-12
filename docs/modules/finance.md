# Module Spec — Finance & Accounts (Phase 18)

Status: **complete** (2026-07-20). Per the plan, Finance was scheduled after Billing/
Pharmacy/Stores have real transaction volume to post from ("needs billing/pharmacy/stores
volume" — plan §12). Adds one new schema (`finance`) and widens `billing.payment_modes`
with a nullable link into it — no other existing module's tables changed.

## New domain (schema `finance`)

| Table | Notes |
|---|---|
| `finance.accounts` | Chart of Accounts. Global (shared across branches, same posture as the Service/Drug catalogs) — `Code`/`Name`/`AccountType` (Asset/Liability/Equity/Income/Expense)/optional `ParentAccountId` for a simple hierarchy. `IsSystemAccount` marks the three accounts the seeder creates and the auto-posting rules look up **by code** (`SystemAccountCodes`), never by Id — protected from rename/delete. |
| `finance.vouchers` / `finance.voucher_lines` | The one posting document (Journal/Payment/Receipt). Posting is immediate — no draft state, since a voucher only gets created once its lines already balance. Correcting a mistake creates a **reversal voucher** (`ReversalOfVoucherId`, debit/credit swapped) rather than editing or deleting posted lines, so the ledger stays append-only. `SourceReference` tags automatic postings (`BILL_PAYMENT:123`, `PAYABLE_SETTLEMENT:45`…) — null for a manually entered voucher. |
| `finance.ledger_entries` | A denormalized, account-indexed 1:1 projection of every posted `VoucherLine`, written by the same call that writes the line — exists as its own table purely so the ledger viewer doesn't have to join through `Voucher` on every read. |
| `finance.payables` | One row per posted GRN, raised **explicitly** by Finance from a pending-GRN worklist (not auto-created the moment a GRN posts — Finance controls when a supplier invoice enters the AP workflow). Paying one posts a real ledger voucher. |
| `finance.receivables` | The AR mirror of Payable — one row per credit `Bill`, raised explicitly from a pending-credit-bill worklist. `CollectedAmount` is Finance's own sub-ledger tracking of what the corporate has settled; it does **not** touch `Bill.PaidAmount` (Billing never collects a corporate bill at the counter — see "No cross-module writes" below). |

Widened an existing entity rather than a parallel structure (same posture as prior phases):
`billing.payment_modes.linked_account_id` — nullable FK into `finance.accounts`; the seeder
links the default CASH mode to the Cash account and CARD/UPI/CHEQUE to a Bank account, so a
fresh dev database posts real ledger entries on the very first bill payment. A branch that
hasn't configured this yet just doesn't get auto-posted — Billing keeps working standalone.

No `gst_registers` table — the GST/Tax report is a live read-only aggregate over
`Bill`/`BillItem` (Billing) and `PharmacySale`/`PharmacySaleItem` (Pharmacy), same "compute on
read, don't duplicate a source of truth" posture MRD used for destruction eligibility and
incomplete records.

Migration: `Phase18_Finance`.

## The posting engine

Every ledger impact in the system — manual voucher entry, Payable/Receivable settlement, or
an automatic billing event — funnels through one method, `VoucherService`'s internal
`PostCoreAsync`, so the double-entry-balance check and the Voucher→VoucherLine→LedgerEntry
write pattern only exist once. `FinanceMath.IsBalanced` (pure, unit-tested) is the balance
check: at least two lines, and total debits equal total credits.

## "Internal event handlers" — billing → ledger auto-posting

The plan calls for "billing/pharmacy/GRN events post to ledger via internal event handlers
(double-entry, transactional)". This phase implements the pattern for one flagship event —
**bill payment collection** — rather than every possible event, to bound the risk of touching
three already-shipped, tested modules' core transactional methods:

- `IFinanceLedgerPoster` (declared in `Application/Common/Interfaces`, implemented by
  `VoucherService`) is the hook — the same posture as `IActivityLogger`/`IPdfService`:
  `BillingService` depends on the **interface**, not on the Finance feature's concrete class.
- `BillingService.CollectPaymentAsync` calls `ledgerPoster.PostBillPaymentAsync(...)` inside
  its existing transaction, right after saving the payment. It resolves the payment mode's
  `LinkedAccountId` and the `PatientRevenue` system account and posts Debit Bank/Cash ·
  Credit Patient Revenue. If either isn't configured yet, it's a **deliberate no-op**, not an
  exception — Finance setup is optional, not a precondition for Billing to keep working.
- Pharmacy GRN posting was **not** wired the same way this phase — the AP entry point is
  instead the explicit Payable-raise action described below, which does post a real ledger
  voucher once Finance chooses to act on it. Wiring GRN-time auto-posting too was judged not
  worth the regression risk to `PurchaseService`'s already-large transactional posting method
  for this phase; the module doc calls it out here as a scoping decision, not an oversight.

## Payables / Receivables

Both follow the same three-step shape: a pending-worklist read (posted GRNs without a
Payable / credit Bills without a Receivable), an explicit "raise" action, then "record
payment"/"record collection" which posts a real two-line voucher via `VoucherService`:

- Paying a Payable: Debit Accounts Payable (reduces the liability) / Credit the chosen
  bank-cash account.
- Collecting a Receivable: Debit the chosen bank-cash account / Credit Accounts Receivable
  (reduces the asset).

`FinanceMath.RecomputePayable`/`RecomputeReceivable` (pure, unit-tested) derive
balance/status from amount-paid-so-far, same idiom as Billing's `BillMath`.
`FinanceMath.AgingBucket` (0-30/31-60/61-90/90+) drives both aging reports.

## No cross-module writes

Consistent with Phases 14–17's "no cross-service DI" posture: no Finance service takes a
constructor dependency on `BillingService`/`PurchaseService`. The one exception —
`BillingService` depending on `IFinanceLedgerPoster` — runs the dependency in the direction
CLAUDE.md's layering already allows (an existing module depending on an interface a later
module implements, exactly like `IActivityLogger`), not the reverse.

## Endpoints (`/api/v1/finance`)

- `accounts`: `GET`, `POST`, `PUT {id}`, `DELETE {id}`
- `vouchers`: `GET` (filterable by type/status/date range), `GET {id}`, `POST` (post), `{id}/cancel`: `POST`
- `ledger/{accountId}?fromDate&toDate`: `GET`
- `payables`: `GET`, `POST` (raise from a GRN); `payables/pending-grns`: `GET`; `payables/aging?asOf=`: `GET`; `payables/{id}/pay`: `POST`
- `receivables`: `GET`, `POST` (raise from a bill); `receivables/pending-bills`: `GET`; `receivables/aging?asOf=`: `GET`; `receivables/{id}/collect`: `POST`
- `reports/cash-bank-collection`, `reports/revenue`, `reports/gst`, `reports/outstanding`: `GET` (date-range filterable)
- `reports/day-close-console?date=`: `GET` — the plan's "centralized Day Close (all counters)". Cross-branch: reuses IPD/Billing's existing `DayClose`/`DayCloseModeAmount` entities via `IgnoreQueryFilters()` rather than a new table; gated by **both** `FINANCE.DAYCLOSECONSOLE.VIEW` and the new general-purpose `REPORT.CROSSBRANCH.VIEW` (the exact permission code CLAUDE.md's non-negotiables section already names for cross-branch reads — this is its first real use).

## Permissions

`FINANCE.ACCOUNT.{VIEW,MANAGE}`, `FINANCE.VOUCHER.{VIEW,CREATE,CANCEL}`,
`FINANCE.LEDGER.VIEW`, `FINANCE.PAYABLE.{VIEW,MANAGE}`, `FINANCE.RECEIVABLE.{VIEW,MANAGE}`,
`FINANCE.REPORT.VIEW`, `FINANCE.DAYCLOSECONSOLE.VIEW`, `REPORT.CROSSBRANCH.VIEW`.

## Frontend (`src/features/finance`)

- `VouchersPage` (`/finance/vouchers`) — list (expandable rows show the debit/credit lines),
  a "New voucher" modal with dynamic add/remove lines and a live balanced/unbalanced banner,
  and cancel-with-reason (posts the reversal).
- `AccountsPage` (`/finance/accounts`) — CoA CRUD; row click opens `AccountLedgerPage`
  (`/finance/ledger/:accountId`) — opening/closing balance, running balance per entry,
  date-range filter.
- `PayablesPage` / `ReceivablesPage` — tabbed: the worklist (pending GRNs / pending credit
  bills) with a "raise" action, the list with "record payment"/"record collection" (bank/cash
  account picker sourced from Asset-type accounts), and an aging tab.
- `DayCloseConsolePage` (`/finance/day-close-console`) — every branch's status for one date.
- `FinanceReportsPage` (`/finance/reports`) — tabbed: cash/bank collection, revenue
  (department/doctor), GST outward register + HSN summary, outstanding bills, trial balance.
- Masters/Billing gained no new screens; `PaymentModesPage` doesn't yet expose the new
  `linkedAccountId` field in the UI (see Deferred).

## Deferred / notes

- **`PaymentModesPage` doesn't expose `linkedAccountId` in the UI yet** — the seeder wires
  the four default modes to Cash/Bank automatically, so auto-posting works out of the box in
  a fresh dev database; adding a new payment mode and linking it to a custom account still
  needs a direct API call or a future small UI addition, not a re-architecture.
- **GRN/pharmacy-sale ledger auto-posting is deferred** — see "internal event handlers"
  above; the AP side is covered through the explicit Payable workflow instead.
- **Trial Balance added (`FinanceReportService.GetTrialBalanceAsync`, `/finance/reports/trial-balance`)** —
  every account's cumulative Debit/Credit totals from `LedgerEntry` as of a given date (raw
  sums, not netted to one side, so TotalDebit == TotalCredit is the literal proof the ledger
  balances), accounts with no ledger activity omitted. Still **no balance sheet / P&L
  statement screens** — the plan's Phase 18 bullet list asks for
  vouchers/ledger/payables/receivables/GST/revenue/outstanding, not full financial statements;
  the ledger viewer and reports here remain the "P&L inputs" the plan's reports table (§17)
  describes, not a finished P&L.
- **Frontend not exercised in a real browser** — verified via `tsc -b && vite build` (clean)
  and `oxlint` (clean, one pre-existing unrelated warning); no browser tool is available in
  this environment.

## Pre-go-live QA fix — Pending Credit Bills 500 (2026-09-17)

`GET /api/v1/finance/receivables/pending-bills` (the Receivables page's "Pending Credit Bills"
tab) threw "Nullable object must have a value" on every call once the branch had even one
credit bill with no `CorporateId` — and it always did, since Billing's "Mark as credit" button
lets a bill be flagged credit without picking a corporate (a legitimate state: ~958 of the demo
dataset's bills are exactly this). `ReceivableService.GetPendingBillsAsync` was projecting
`b.CorporateId!.Value` without first filtering out null-corporate bills, even though
`CreateAsync` right below it already refuses to raise a receivable against one ("Only a credit
bill with a corporate assigned..."). Fixed by adding `b.CorporateId != null` to the query's
`Where` — a null-corporate credit bill isn't a receivable candidate at all, so it's correctly
excluded rather than crashing the whole list.

## Fixed while here

Phase 17 (MRD)'s eight Application services (`RecordFileService`, `FileRequestService`,
`ScannedDocumentService`, `CodingWorklistService`, `FileAuditService`, `RetentionPolicyService`,
`DestructionRegisterService`, `MrdReportService`) and `RecordDocumentTypeService` were never
registered in `Prixo.Application`'s `AddApplication()` — every MRD controller would have
failed to resolve at runtime. Fixed as part of this phase's DI wiring; call this out
explicitly since it's a Phase 17 gap, not a Phase 18 one.
