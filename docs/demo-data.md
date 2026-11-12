# Demo / dummy data seeding

`DemoDataSeeder` (`backend/src/Prixo.Infrastructure/Persistence/DemoDataSeeder.cs`) fills the
modules that ship empty, so a fresh-ish dev database can be demoed, screenshotted, or walked
through in UAT rehearsal without hand-entering data on 40 screens first.

It is split across three files, one partial class:

| File | Purpose |
|---|---|
| `DemoDataSeeder.cs` | The original sections — modules that ship with **no** rows (loyalty, insurance, quality, biomed, MRD…). |
| `DemoDataSeeder.Catalogs.cs` | **Catalog depth** — catalogs that had rows but far too few to demo, plus two catalog defects that broke reporting. |
| `DemoDataSeeder.History.cs` | **Six months of operational history** — visits, admissions, bills, payments and their Finance postings, spread day by day up to today. |

## It is deliberately not part of `DbSeeder`

`DbSeeder` seeds genuine reference data (permissions, roles, menus, GST slabs, states) and is
safe to run anywhere. `DemoDataSeeder` invents **patients' insurance claims, quality incidents,
audit findings and portal logins** — none of which may ever reach a real hospital database. So:

- it is a separate class, registered separately (`AddScoped<DemoDataSeeder>()`);
- it **throws** if `IHostEnvironment` is anything other than Development;
- it is **opt-in even in Development**, behind `Seed:DemoData`.

```jsonc
// backend/src/Prixo.Api/appsettings.Development.json
"Seed": {
  "AdminPassword": "Admin@123",
  "DemoData": true          // ← set false (or remove) to stop demo seeding
}
```

`Program.cs` runs it immediately after `DbSeeder`, inside the existing `IsDevelopment()` block.

## Re-running and resetting

The empty-module sections are guarded *"if this table already has rows, skip it"*, so:

- re-running is safe and never duplicates;
- **to reset one module, delete its rows and restart the API** — that section alone re-seeds;
- the `Random` is fixed-seeded (`20260808`), so a rebuilt environment reproduces the same data.

The two newer files guard differently, because the tables they touch already hold rows that must
be left alone:

- **Catalogs** guard *per row, keyed by the natural code* (service code, ICD code, item code…).
  Re-running only ever adds what is missing, and never edits a row an admin has changed. The one
  exception is `BackfillServiceDepartmentsAsync`, which fills a **null** `DepartmentId` and never
  reassigns a department someone already chose.
- **History** guards *per branch-day*: a branch-day that already has bills is skipped entirely.
  That is what preserves the pre-existing 2026-07-23 data, and what makes an interrupted run
  resume rather than duplicate. Verified: a second run logs
  `Demo history: nothing to add — the window already has data on every day`.

One subtlety worth knowing if you edit the history seeder: the admission gate is derived from
`(branchId, date)` via `AdmissionGate`, **not** from `_random`. A re-rolled coin flip would make
each re-run pile more admissions onto the branch-days it happened to skip last time, since only
chosen dates leave a row behind to guard against. The OP loop needs no such trick because it
writes a bill on every day it touches.

## Why it writes through `AppDbContext`, not the services

The Application services enforce workflow guards — day-close windows, approval
separation-of-duties, sequence generation — that exist to protect real operational data and
would reject the back-dated, already-in-a-terminal-state rows demo data needs (a claim that is
already `Settled`, a CAPA already `Verified`, an audit run from four months ago).

The trade-off is that the seeder owns consistency itself. Where that matters it does the work:
pharmacy sales/issue returns and biomedical spare usage post their own `stock_ledger` rows and
batch increments/decrements, mirroring `StockLedgerWriter`, so stock reports still reconcile.
Verified after seeding: 14 demo ledger rows, **0 batches with negative stock**.

## What gets seeded

| Area | Rows |
|---|---|
| Front-office masters | Salutations, marital statuses, ID document types, visit classes/sessions, case types, HC tags, cities, areas, camps, MHC doctors + packages, units + unit-doctor links + unit schedules, appointment types, referrers |
| Billing masters | Discount types, room-type charges, doctor payout rules, bill-estimate templates, pharmacy racks, OT equipment |
| Loyalty | Card categories, relation tags, card types, branch price matrix, 18 cards (incl. deactivated + duplicate-issued), 42 dependents |
| Insurance / TPA | Claim doc types + denial reasons, 38 policies, 40 pre-auths (all statuses), 8 enhancements, 18 claims (all statuses incl. settled/denied), 54 claim documents |
| Quality / NABH | 10 chapter standards, 6 committees, 4 audit templates, 24 audit runs + 100 responses, 8 non-conformances, 19 CAPAs (some deliberately overdue), 8 risks, 24 committee meetings + action items, 30 evidence records, **341 indicator entries = 12 months of history for all 31 indicators** |
| Biomedical | 17 equipment, 17 AMC/CMC contracts, 17 PM schedules (some already due), 13 breakdown complaints, 29 work orders, 17 calibrations, 8 service-history entries, 2 spare usages |
| MRD | 60 coding worklist entries, 40 file audits (1 in 4 deliberately incomplete), 90 scanned documents, destruction register |
| Billing workflow | 20 estimates, 12 credit notes (all 3 statuses), 7 cashier shifts (open/closed/received, with cash variances), 25 bill notes |
| Pharmacy | 10 sales returns, 2 issue returns — each with matching stock-ledger movements |
| Clinical extras | 15 admission requests, 180 intake/output entries, 25 procedure notes, 30 OPD referrals (some with cross-consult opinions pending), 24 schedule overrides |
| Patient experience | 25 portal accounts, 60 visit-linked feedback records, 40 patient documents |
| Finance | 20 payables across all aging buckets |

### Catalog depth (`DemoDataSeeder.Catalogs.cs`)

Catalogs that had rows, but too few to demo — every one of these is a dropdown somebody has to
pick from:

| Catalog | Before | After |
|---|---|---|
| Services (`master.services`) | 50 | **173** — specialty consultations + procedures across 25 departments |
| Tariffs / tariff items | 6 tariffs, 300 items, **branch 1 only** | 18 tariffs, 3,114 items — **every branch** |
| Chart of Accounts | 9 | **70** |
| General-store items | 3 items, 1 category | **131 items, 13 categories** |
| Drugs / generics / manufacturers | 81 / 40 / 10 | **203 / 155 / 28** |
| Lab tests / profiles | 17 / 1 | **86 / 17** (every numeric parameter gets an adult reference range, so H/L and critical flagging fires) |
| Radiology tests / report templates | 20 / 1 | **74 / 74** (one starter template per test) |
| ICD-10 codes | 61 | **177** |
| Packages | 4 (one branch) | **58** (18 per branch) |
| Prescription templates | 1 | **17** |
| ICU critical-alert rules | 2 | **50** (full threshold set per branch) |
| Nationalities | 2 | **45** |

### Operational history (`DemoDataSeeder.History.cs`)

182 days ending **today**, per branch: OP visits (New/Review mix, weekday-weighted, ~25% growth
across the window, with a few cancellations and no-shows), their bills with tariff-priced lines,
payments across the payment modes, and a matching Finance receipt voucher + ledger entries for
each payment. Plus a steady trickle of admissions with realistic lengths of stay, bed allocations
that respect bed capacity, and IP bills dated at discharge.

| | Before | After |
|---|---|---|
| OP visits | 1,269 | **10,487** |
| Bills | 1,686 | **10,889** |
| Payments / vouchers / ledger rows | 628 / 628 / 1,256 | **8,877 / 8,877 / 17,754** |
| Admissions | 187 | **552** |
| Distinct days carrying bills | **4** | **183** |

Statuses are spread across each enum on purpose, and rates trend gently in the improving
direction over time, so filters, tabs and dashboard trend lines all have something to show
rather than looking like noise.

**Portal demo logins:** the 25 seeded portal accounts share the password `Portal@123` and are
all flagged `MustChangePassword`.

## Deliberately left empty

| Table | Why |
|---|---|
| `admin.notification_queue`, `billing.online_payment_requests`, `reporting.report_subscriptions` | Outbox placeholders with no sender wired. Fake rows would imply queued work nothing will ever process. |
| `icu.device_readings` | `IDeviceInbound` placeholder — no device integration exists. |
| `patient.portal_refresh_tokens` | Live session state; a fabricated token is meaningless. |
| `master.loyalty_programs` | The legacy, deliberately-unused `LoyaltyProgram` master — the loyalty batch uses `LoyaltyCardType` instead (see `docs/modules/loyalty.md`). |

After seeding, these five are the **only** empty tables in the database.

## Why the reports were empty (fixed 2026-08-17)

Three separate defects, only one of which was about volume:

**1. All operational data sat on one day.** 1,676 of the 1,686 bills and 1,245 of the 1,269 OP
visits were dated **2026-07-23** — 4 distinct billed days in the entire database. Every dashboard
tile scoped to "today", "this month" or "last 7 days" therefore rendered zero, and every trend
line was a single vertical spike. `DemoDataSeeder.History.cs` now generates 182 days of history
ending today.

**2. Not one bill line carried a `ServiceId`.** All 1,251 pre-existing `Service` lines had
`service_id IS NULL`. `reporting.mv_revenue_daily` derives department-wise revenue through
`bill_items → services.department_id`, so department revenue collapsed into a single unlabelled
bucket **no matter what date range you asked for**. Every generated line now sets a real
`ServiceId`, and the consultation line prefers a service in one of the doctor's own departments so
department-wise and doctor-wise revenue agree with each other.

**3. 16 of the 50 services had no `DepartmentId`.** Every consultation, room, OT, nursing and
procedure charge was department-less, so even a correctly-linked bill line could only ever resolve
Laboratory or Radiology. `BackfillServiceDepartmentsAsync` assigns those by service code.

Department-wise revenue now resolves **25+ departments**. One `(unattributed)` bucket of ₹1,206,700
remains, and is expected: it is exactly the pre-existing 2026-07-23 bills, deliberately left alone.

### The pre-existing spike was not re-dated

Re-dating those 1,686 bills would mean re-dating the 1,256 `finance.ledger_entries` and 628
vouchers already posted against them in lockstep — any miss turns cosmetic sparseness into a real
Finance inconsistency. The new history is purely additive and the spike survives as one busy day.

### Finance stays reconciled

Payments post through the same double-entry shape as the live code path
(`VoucherService.PostBillPaymentAsync`): debit the payment mode's linked bank/cash account, credit
Patient Revenue, one `LedgerEntry` per `VoucherLine`, tagged `BILL_PAYMENT:{id}`. Verified after
seeding: **8,877 payments ↔ 8,877 vouchers, 17,754 ledger rows = 17,754 voucher lines, 0
unbalanced vouchers, 0 beds holding two active allocations.**

Payments total ₹30,955,615 against ₹30,955,115 of receipt-voucher debits. The ₹500 difference is
**pre-existing**: `BILL-2026-000001` (2026-07-13) has a payment but no ledger posting, from before
the Finance hook was configured. Every generated payment reconciles.

Everything the earlier sections added spans months and trends correctly:

| Data | Range |
|---|---|
| Quality indicator entries | 2025-09 → 2026-07 (12 months) |
| Quality audits | 2026-03 → 2026-08 |
| Insurance claims | 2026-02 → 2026-07 |
| Biomedical work orders | 2026-02 → 2026-08 |
| Finance payables | 2026-04 → 2026-07 |
| Patient feedback | 2026-02 → 2026-08 |
| Loyalty cards | 2025-01 → 2026-07 |
| **OP visits / bills / payments / admissions** | **2026-02-16 → today (183 billed days)** |

## Still thin, and deliberately so

The catalog pass covered dropdown sources. What remains under ~5 rows is almost all *transactional*
history in modules the seeder only sampled — nursing notes and handovers, ICU ventilator/infusion
charts, pharmacy stock transfers/adjustments/audits, OT implant and consumable usage, EMR
histories. Those need workflow-shaped generation (a stock transfer has to move real batches and
write ledger rows), which is a larger, separate piece of work than filling a dropdown.

A few small ones are simply correct: `master.visit_sessions` (Morning/Afternoon/Evening),
`billing.payment_modes` (Cash/Card/UPI/Cheque), `mrd.retention_policies` (three record types),
`master.buildings` (one per branch). Adding payment modes with zero transactions behind them would
put empty rows in the collection-by-mode report, which reads worse than four populated ones.

## Reporting views

The three `reporting` materialized views are refreshed by the `refresh-reporting-views` Hangfire
job every 15 minutes. **The history seeder now refreshes them itself** at the end of a run that
added anything, so the dashboards have data the moment the API finishes starting rather than
looking empty for up to 15 minutes and reading as "the seed did nothing". A refresh failure is
logged and swallowed — it must never abort the seed, since the job will retry anyway.

To refresh by hand:

```sql
REFRESH MATERIALIZED VIEW CONCURRENTLY reporting.mv_revenue_daily;
REFRESH MATERIALIZED VIEW CONCURRENTLY reporting.mv_daily_collection;
REFRESH MATERIALIZED VIEW CONCURRENTLY reporting.mv_op_census;
```
