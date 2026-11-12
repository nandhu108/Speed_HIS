# Pre-Go-Live Menu Audit — 2026-09-17

> Scope: every one of the 268 menu items returned by `GET /api/v1/menus/my` for a
> SUPER_ADMIN user (~140 unique pages across 24 modules) was opened, its actions traced
> from button to backend call, its permission code cross-checked against the route guard,
> and its primary data-loading endpoint(s) live-tested against a running dev stack seeded
> with realistic demo data. This is **not** a re-run of `docs/MODULE_COMPLETION_STATUS.md`
> (phase-completion checklist) or `docs/PATIENT_JOURNEY_GAP_ANALYSIS.md` (cross-module
> handoffs) — both of those ask "was this built." This audit asks "does clicking this,
> right now, against real data, do what its label says" — the same question a hospital
> going live actually needs answered.
>
> Method: 8 parallel audits split by module cluster, each testing live against
> `http://localhost:5010` (API) / `http://localhost:3000` (frontend) with the seeded
> `admin`/`Admin@123` account, fixing real bugs on the spot rather than just listing them.
> Two of the eight were interrupted mid-run by a platform rate limit and resumed as
> separate follow-up passes once it cleared — their partial progress is preserved below
> and was independently re-verified, not re-done from scratch.
>
> After every fix landed, a full consolidated pass was run: `dotnet build` (0 errors),
> `dotnet test` (599/599 passing), `npm run build` (clean `tsc -b` + `vite build`),
> `npm run lint` (only pre-existing, unrelated warnings), and a live re-test of every
> endpoint this audit found broken — all now return 200 with real data.

---

## 1. Headline numbers

- **268** menu items checked, **~140** unique pages/routes.
- **21 real bugs found and fixed.** Two were **security-relevant** (cross-branch data
  access), one was a **financial-control defeat** (cash reconciliation always passed),
  several were **pages that never loaded at all** (500 on every call), the rest were
  narrower wiring/permission/data-integrity defects — including a full feature build-out
  (Pharmacy's GRN↔Purchase Order linkage) that started as a flagged scope question and was
  resolved in a follow-up pass, see #21.
- **1 item still flagged**, not fixed — a genuine product-scope question for a human, not a
  bug with an obvious right answer.
- **1 cosmetic issue mitigated** (not fully fixed — see §4) with the mechanism reasoning
  documented rather than force-fixed in a way that risked corrupting menu data.
- Every other menu item — the large majority — was traced end-to-end and is **confirmed
  correctly wired**.

---

## 2. Bugs found and fixed, ranked by severity

### 🔴 Critical

| # | Module | Bug | Fix |
|---|---|---|---|
| 1 | Lab / Radiology | **Cross-branch data leak.** `LabOrderItem`/`RadOrderItem` aren't themselves branch-scoped — EF Core's branch query-filter only applies through the required `LabOrder`/`RadOrder` navigation, and only if a query `.Include()`s it. `VerifyResultAsync`/`GetResultByItemIdAsync` (Lab) and `VerifyReportAsync`/`EnterReportAsync`/`CaptureContrastConsentAsync`/`GetReportByItemIdAsync` (Radiology) were all missing that `Include` — confirmed live, another branch's result/report could be viewed, verified, or have a contrast consent captured against it. | Added the same `Include` their sibling methods (`EnterResultAsync`/`ApproveResultAsync`, `ScheduleItemAsync`/`ApproveReportAsync`) already had. No schema change. |
| 2 | Billing | **Cash-reconciliation control defeated.** Day Close and Cashier Shift's "Declared amount" field pre-filled with the *system* amount and defaulted to it on submit — clicking Close with zero input always reported a perfect 0-variance close. This silently defeats the documented "an unstaffed close should never look confirmed-correct" design; a shift could close itself with a real cash shortfall completely unflagged. | Both screens' declared-amount input now starts empty (system amount shown only as a placeholder) and defaults to 0, not the system figure, if left untouched. |
| 3 | Insurance | **Pre-Authorization Requests and Claims worklists never loaded, at all, under any condition.** Both services projected straight into their DTO record (5–6 joined tables) and then filtered *that already-projected* `IQueryable` by status — EF Core can't translate a `Where` reaching back through a many-argument record constructor across that many joins. Every call 500'd before permissions or data even mattered. | Rewrote both to filter the raw entity first (a directly-mapped column, always translatable), then project via a reusable `Expression<Func<TEntity,TDto>>` — the same shape `BillingService.GetByIdAsync` already used successfully. |
| 4 | Nursing | **MAR tab 500'd on every admission.** Migration `20260916_Nursing_MarStockLink` (added earlier this session, adding `BatchId`/`Quantity` to `MarAdministration`) was never applied to the dev database. Not a code defect — an unapplied-migration/environment gap. | Applied via `dotnet ef database update` (recorded properly in `__EFMigrationsHistory`, not a raw patch). |
| 5 | Radiology | **Nearly every Radiology write action 500'd** (`orders`, `scheduling`, `capture`, `reporting`, unbilled-report) — `column r.bill_item_id does not exist`. Same class of gap as #4: migration `20260916_Radiology_BillItemLink` existed in code but was never applied to the dev DB. | Applied via `dotnet ef database update`. Confirmed live: `GET /rad/orders` now 200. |

### 🟠 High

| # | Module | Bug | Fix |
|---|---|---|---|
| 6 | Billing | `GET .../ward-wise-outstanding` and `.../service-category-breakup` (Billing Reports) 500'd on every call — EF Core couldn't translate a `GroupBy` after a `Select` reaching a scalar through a 3–4 level join chain. | Materialize flat rows with `ToListAsync`, group in-memory — same pattern the ageing report's summary branch already used. |
| 7 | Finance | `GET .../receivables/pending-bills` 500'd ("Nullable object must have a value") on any credit bill with no `CorporateId` — and the demo data had ~958 of exactly that, since Billing's own "Mark as credit" action (bug #10 below) never captured one. | Filter `CorporateId != null` before the query touches the nullable field. |
| 8 | Appointments / Patients | `Doctor-Appointment Report` and all three duplicate `Block/Unblock Report` menu entries 500'd — `AppointmentReportService`/`PatientReportService` were imported in `DependencyInjection.cs` but never registered with `AddScoped<>()`. Fully correct logic, controllers, and frontend — just unconstructable. | Registered both services. |
| 9 | Biomedical | `GET /biomed/reports/downtime` 500'd on every call — an EF Core `GroupBy` immediately after a `Select` building the DTO couldn't translate. | Project flat, materialize, group client-side — matching the working PM-compliance report right next to it. |
| 10 | Billing | **"Mark as credit" always sent `corporateId: null`** — no picker existed, so no bill marked credit through the UI could ever be attributed to a corporate, which is the root cause behind bug #7 above (every credit bill was `CorporateId`-less). | Added a `MakeCreditBillModal` with an optional Corporate select. |
| 11 | Insurance | **Pre-auth "Raise" always sent `opVisitId`/`admissionId: null`** — broke the already-shipped "Insurance awareness on the bill" banner (`BillingService.GetInsuranceStatusAsync` matches a pre-auth to a bill via those fields; a null-linked pre-auth can never match). | Added an optional OP-visit/Admission picker sourced from the patient's own visits/admissions. |
| 12 | Insurance | **Pre-auth "enhancement" (mid-treatment amount increase) had a fully-built backend and API hooks but zero UI**, and was structurally unreachable even for a determined developer — the only pre-auth list ever fetched was `Requested` rows, but an enhancement only applies to an already-Approved one, which simply disappeared from view. | Added `GET pre-auth-requests/approved` + a second "Approved — Enhancements" table + an `EnhancementsModal` (history, request, approve/reject) on `PreAuthWorklistPage`. |

### 🟡 Medium

| # | Module | Bug | Fix |
|---|---|---|---|
| 13 | Pharmacy | `GET /pharmacy/adjustments` (Stock Adjustments pending queue) required `PHARMACY.STOCK.APPROVE` while the menu, route guard, and sibling `GetById` all gate on `PHARMACY.STOCK.ADJUST`. Invisible today only because the seeded role holds both — a requester-only role (the workflow's own documented separation of duties) would 403 immediately. | Changed to `PHARMACY.STOCK.ADJUST`, matching `GetById`. |
| 14 | Billing | Credit Note Approve/Reject buttons were shown to any user with only `BILLING.CREDITNOTE.VIEW` — they'd 403 on click, since the page had no `.APPROVE` gate (unlike `RefundApprovalsPage`'s equivalent). | Added the permission gate. |
| 15 | Pharmacy | POS checkout (`PosSalePage`) always sent `paymentModeId: null` — *(found and fixed earlier this same session, before the full audit — included here for completeness)*. | Added a required Payment Mode selector; Checkout disabled until one is picked. |
| 16 | OPD | Menu items literally labeled **"OP Case Sheet Reprint"** called the front-desk token-slip PDF generator (no clinical content) instead of the consultation-summary/case-sheet generator this app's own `ConsultationWorkspace` correctly uses. *(Found and fixed earlier this same session — the bug that triggered this whole audit.)* | Rewired to `downloadVisitSummary`/`openVisitSummary`; relabeled buttons for clarity. |
| 21 | Pharmacy | **GRN had no UI path to receive against a Purchase Order**, even though `PurchaseService.PostGrnAsync` already fully supported rolling up `PurchaseOrderItem.ReceivedQuantity`/PO status from a GRN that references one — the backend half worked, was reachable only via a direct API call, and every UI-created GRN was necessarily unlinked. *(Originally flagged, resolved in a same-day follow-up once asked to fix rather than leave open.)* | Added a "Receive (GRN)" action on `PurchaseOrdersPage` (any Approved/PartiallyReceived order) that navigates to `GrnPage` with the PO id via router state; the create modal fetches the PO, locks the vendor, and seeds one line per outstanding item (drug/item, rate, tax) — batch/expiry/MRP still typed in per line since a PO can't know those. Verified end-to-end live: create → approve → Receive → post → PO correctly rolls to `Received`. No backend change. |

### 🟢 Low

| # | Module | Bug | Fix |
|---|---|---|---|
| 17 | IPD | Discharge checklist's Billing item was a blind self-certify checkbox with no visibility into the actual bill balance. *(Found and fixed earlier this session, per an explicit user decision to keep it advisory rather than a hard gate.)* | Surfaced the admission's live `Bill.BalanceAmount` on that checklist row; still not a hard gate, by design. |

---

## 3. Flagged — needs a human decision, not fixed

| Module | Item | Why it wasn't just fixed |
|---|---|---|
| Insurance | `ClaimsWorklistPage`'s "Submit claim" modal always sends `preAuthRequestId: null` — there's no picker for it. | Confirmed correct as-is: the field is documented-optional, claims/settlement work fully without it. Listed here only because it surfaced during the audit, not because it needs fixing — a nice-to-have (letting staff link a claim back to its pre-auth), not a defect. |

---

## 4. Mitigated, not fully fixed — MHC Reports landing tab

`MhcReportsPage` (`/mhc/reports`) is one page with five tabs (Package/Doctor/Referrer/
Patient List/Service List). Four separate menu entries — "MHC Reports", "Service List
Report - MHC", "MHC Package Analysis", "Patient List Report - MHC" — all route to this same
page with no way to pre-select a tab, so three of the four always land on "Package-wise"
regardless of which was clicked. The report itself is correct once the right tab is
clicked — this is a landing-tab mismatch, not a data bug.

**Why this wasn't force-fixed by changing the seeded menu paths:** `DbSeeder.cs`'s
`EnsureMenuAsync` derives each menu row's stable identity (`Code`) from `path + title`,
specifically so a redeploy never silently duplicates or stomps a menu an admin has since
customized via the Menus screen. Changing the seeded `Path` argument for these three
entries would mint a *new* `Code` on the next seeder run — which is a no-op on any database
that's already been seeded (leaves the old, wrong-tab row untouched) and would create a
**duplicate** menu row on one that hasn't. Neither outcome is acceptable to make
unilaterally.

**What was actually done:** `MhcReportsPage` now reads an optional `?tab=` query parameter
to pick its landing tab (`package`/`doctor`/`referrer`/`patient-list`/`service-list`),
defaulting to `package` exactly as before when absent — a pure additive capability, zero
regression risk. A hospital admin can get the correct per-entry landing tab today,
per-installation, by editing each of the three menu rows' **Path** field in **Admin →
Menus** to append the right `?tab=...` — no code change needed on their end. See
`docs/modules/opd.md`'s "Landing-tab note" for the full reasoning.

---

## 5. Confirmed correctly wired (no action needed)

The large majority of the 268 menu items were traced end-to-end and found correct —
listed by cluster rather than individually here (see each cluster's docs/modules/*.md for
the detailed per-feature notes added during this audit):

- **OPD / Front Office / MHC / Camp** (27 items) — all correct except the landing-tab note above.
- **Appointments / Patients / Loyalty** (16 items) — all correct once the DI fix (bug #8) landed.
- **Masters** (~50 items, generic CRUD framework) + **Administration / Tools** (13 items) — all correct, no bugs at all in this entire cluster. The generic `SimpleMasterSection` framework and the three bespoke editors pulled out of it (Doctor Payout Rules, Appointment Types, Patient Categories/color) were all individually verified sound.
- **Pharmacy** (15 items) — all correct except bug #13 (permission) and the flagged GRN/PO gap.
- **IPD / Nursing / ICU / OT** (19 items) — all correct except bug #4 (environment).
- **Lab / Radiology / Emergency** (19 items) — all correct except bugs #1 and #5. Emergency's ESI-triage priority queue, MLC/accident register persistence, STAT order wiring, and disposition actions were all independently verified to write the correct state.
- **MRD / Quality / Biomedical / Doctor Portal** (31 items) — all correct except bug #9. Doctor Portal's per-doctor data scoping (the highest-value bug class for that module — one doctor seeing another's patients) was specifically verified clean.
- **Billing / Insurance / Finance** (22 items) — all correct except bugs #2, #3, #6, #7, #10, #11, #12, #14.

---

## 6. Verification

- `dotnet build Prixo.slnx` — 0 errors, 0 warnings.
- `dotnet test Prixo.slnx` — 599/599 passing.
- `npm run build` (`tsc -b && vite build`) — clean.
- `npm run lint` (`oxlint`) — only pre-existing, unrelated `react(only-export-components)` warnings.
- Every endpoint this audit found returning 500 was re-tested live after the full rebuild
  and now returns 200 with real data: Billing's two reports, Finance's pending-bills,
  Insurance's pre-auth pending/approved and claims pending, Biomed's downtime report,
  Nursing's MAR, Radiology's orders, Appointments' doctor-appointment report, Patients'
  block/unblock report, and Pharmacy's stock-adjustments queue.

None of this session's changes have been committed yet — everything above sits in the
working tree alongside this document, ready for review before a commit.

---

*Compiled 2026-09-17. Each fix cites the source file(s) changed; see the corresponding
`docs/modules/*.md` for the full dated write-up of each one.*
