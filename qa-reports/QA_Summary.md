# Prixo HIS — Functional QA Summary

> ## Update 2026-11-13 — defects fixed and re-tested
>
> | | Baseline (2026-11-12) | Re-test after fixes (2026-11-13) |
> |---|---|---|
> | Test cases | 541 | 543 |
> | Passed | 450 | **529** |
> | Failed | 79 | **3** (all waiting on hospital price data) |
> | Blocked | 1 | 0 |
> | Server errors (HTTP 500) | 3 kinds | **0** |
>
> **43 of 45 defects are fixed and verified.** All 13 Critical defects are fixed in code. 11 of them pass outright. The other 2 (BUG-013 lab/radiology billing, BUG-021 bed charges) are code-complete but stay open until the hospital enters its **price list and General/Semi-Private bed rates**. See `GO_LIVE_CHECKLIST.md` §A.
>
> Additional regressions found and fixed during the re-test:
> - A scheduled scan could never be cancelled (e.g. a no-show).
> - Any timestamp sent with a local +05:30 offset crashed with HTTP 500.
>
> **Verdict:** ready for go-live from the software side once the hospital data in `GO_LIVE_CHECKLIST.md` is entered, the production setup there is done, and each department has walked its screens once. The automated QA ran at API level and did not exercise the UI.
>
> Per-defect re-test status: workbook sheet "6 Defect Register", column *Re-test 2026-11-13*. The sections below are the original baseline findings.

**Date:** 2026-11-12 · **Build:** local dev (fresh database `prixo-fresh`), branch **SPEED_MULTI** · **Full detail:** `Prixo_HIS_QA_Report.xlsx`

## Verdict (baseline, before fixes)

**Not ready for go-live.** The core clinical workflows exist and mostly behave correctly: registration, consultation, the lab and radiology pipelines, admission, OT and discharge. But 13 Critical defects remain in money handling, medication safety, discharge controls and the OT safety checklist. These must be fixed before real patients are handled.

## How it was tested

- **Method:** every case was executed against the running application through the same REST API the screens use.
- **Users:** 19 synthetic role-specific users (`qa.*`, for example front office, billing, supervisor, nurse, lab tech, pathologist, radiologist, pharmacist, OT coordinator and doctors), so permission checks are real.
- **Data:** synthetic only. Patients are named `QATEST …`; no real patient data was used or changed.
- **Setup created for testing:** doctor schedule, pharmacy store and vendor, a second theatre, a taxable service and a corporate.
- **Not covered:** screen layout and UI-level messages were **not tested**, because no browser was available in this session. These need a separate UI pass.
- **Verdict rule:** a case was marked PASS only when the expected result was checked. Server crashes (HTTP 500) were never counted as a "correct rejection".

## Numbers

| | Count |
|---|---|
| Modules inspected | 26 menu modules; 15 functional areas tested |
| Test cases | **541** |
| Executed (PASS + FAIL) | 529 |
| Passed | **450** (85% of executed) |
| Failed | **79** |
| Blocked | 1 (ICU transfer: both ICU beds held by test admissions with discharge in progress) |
| Not tested | 11 (feature not present), plus all UI-only checks |
| Distinct defects (root causes) | **45**: 13 Critical · 18 High · 13 Medium · 1 Low |

| Module | Pass | Fail | Not tested / blocked |
|---|---|---|---|
| Patient registration | 31 | 6 | 2 |
| Doctor / EMR | 42 | 7 | 0 |
| OP billing | 73 | 9 | 1 |
| Laboratory | 54 | 7 | 0 |
| Radiology | 38 | 8 | 2 |
| Pharmacy | 48 | 12 | 1 |
| IP management | 41 | 9 | 1 |
| Nursing | 16 | 4 | 0 |
| OT | 34 | 5 | 3 |
| Discharge | 18 | 6 | 2 |
| Appointments | 10 | 3 | 0 |
| End-to-end journeys (4) | 34 | 3 | 0 |
| Security and access | 11 | 0 | 0 |

## Critical defects (fix before go-live)

1. **BUG-001 — Double collection.** Four simultaneous payments on a ₹500 bill were all accepted, giving four receipts for ₹2,000. The bill still shows ₹500 paid, and 12 bills have payment rows that don't match their paid amount.
2. **BUG-002 — Over-refund.** Refunds can exceed the amount paid. Two ₹300 refunds on a ₹500 payment left the bill's paid amount at **−₹100**.
3. **BUG-003 — Expired medicine dispensed.** A batch that expired 30 days earlier was received, posted to stock and sold to a patient.
4. **BUG-004 — GST added on top of MRP.** 10 × MRP ₹15 was billed as ₹168 instead of at most ₹150.
5. **BUG-005 — Over-return.** Sales returns can exceed the quantity sold: 11 units were returned against 10 sold.
6. **BUG-006 — Negative stock.** A stock adjustment can drive stock negative (−99,854 units).
7. **BUG-007 — Wrong-patient dispensing.** Patient A's prescription can be dispensed to patient B.
8. **BUG-008 — Duplicate admission.** A patient who is already admitted can be admitted again into a second bed.
9. **BUG-009 — Discharge with unpaid bill.** A patient was discharged with an unpaid IP bill of ₹8,500. The billing clearance ignores the balance, and the ₹5,000 deposit was never adjusted against the bill.
10. **BUG-010 — Wrong discharge signatory.** A discharge summary can be e-signed by a non-treating doctor, including in the treating doctor's name.
11. **BUG-011 — WHO checklist not enforced.** Sign-In was saved as complete with the **site not marked**, and Sign-Out was saved as complete with an **incorrect instrument and sponge count**.
12. **BUG-012 — Completed records editable.** A completed consultation's prescription and diagnosis can be silently overwritten.
13. **BUG-013 — Lab and radiology never billed.** None of the 69 lab tests or 54 radiology tests is mapped to a price. The OP bill for a full visit with APTT and a CT brain showed the consultation only, and tests are processed without any billing check.

## Main integration gaps (High)

- **Inpatient orders and charges:**
  - IP doctor lab/radiology orders are free text and never reach the lab or radiology.
  - Medicines issued by pharmacy to inpatients are not charged on the IP bill.
  - General and Semi-Private beds bill ₹0/day (configuration); ICU is correct at ₹7,500.
- **EMR:** approved radiology reports do not appear in the patient's EMR.
- **Day close:** cash refunds are not netted, so the cashier will always appear short.
- **OT charges:** completing a surgery posts no theatre charge, and implant usage needs no serial number.
- **Pharmacy setup:** the Speed Multi branch has **no pharmacy store or vendor configured**, so pharmacy is unusable until it is set up.

## Access-control findings

- **Ownership and attribution:**
  - Any doctor can open and write to another doctor's OP consultation.
  - A progress note can be filed in another doctor's name.
- **Reception:** can open the clinical consultation workspace. The billing role is correctly refused.
- **Discharge clearances:** one permission covers all four clearances (a Head Nurse can sign off Billing and Pharmacy), and the Billing role cannot give its own clearance.
- **No maker-checker:** a pharmacy manager can approve their own stock adjustment.

## What worked well

- **Login and access security** passed all 11 checks: anonymous and forged-token access refused, brute-force rate limiting, branch isolation, password policy, privilege-escalation block and audit log.
- **Discounts:** the approval thresholds are exact, and approval and rejection both behave correctly.
- **Payments:** overpayment is blocked, the UPI reference is required, payment on paid, parked or cancelled bills is blocked, refunds require approval and cannot be issued twice, and a paid bill can't be cancelled without a refund.
- **Laboratory:**
  - The pipeline order is enforced: collect, receive, enter, verify, then approve. Technician and pathologist rights are separated.
  - Results are flagged High and Critical correctly, critical alerts are raised and acknowledged, and reports are released only after approval.
- **Radiology:**
  - The contrast-consent gate works.
  - Reports are locked once verified.
- **Pharmacy:** stock is consumed FEFO (first-expiry, first-out), the stock ledger is kept, cancellations restore stock exactly once, and a concurrent oversell is prevented.
- **Bed management:** occupied and blocked beds are protected, and transfers keep their history.
- **Nursing:**
  - Morse and Braden risk scores band correctly.
  - The MAR blocks double dosing and requires an independent second verifier for high-alert drugs.
- **OT:** theatre and surgeon conflicts are blocked, and surgery can't start before Sign-In and consent verification.
- **Discharge:** the summary is locked after approval, and the bed is released.

## Features expected but not present (Not tested)

Registration cancellation · registration fee and receipt at registration · billing-side referral · radiology image upload and PACS · radiology report addendum · alternative/generic medicine suggestion · Minor/Major OT type · emergency OT booking · OT approval step · discharge reversal · automatic investigation results in the discharge summary.

## Files in this folder

- `Prixo_HIS_QA_Report.xlsx` contains the seven deliverables as sheets:
  - Summary
  - Module Checklist
  - Test Case Register (541 cases)
  - End-to-End Report
  - Negative & Cancellation
  - Integration
  - Defect Register (45 bugs, with steps to reproduce)
  - Not Tested & Blocked
- `register.jsonl` is the raw result of every executed case, with timestamps.
- `defects.json` holds the curated defect list.
- `server_errors.jsonl` logs every HTTP 500 seen during testing.
- `scripts/` holds the test suites, so the run can be repeated after fixes: run `qa_setup.py` once, then each `qa_*.py`, then `build_report.py`.

## Test data left behind (synthetic)

- **Accounts:** users `qa.*`, all with password `Qa@12345`.
- **Patients:** `QATEST …` with UHIDs from PRX-2026-000002 upward.
- **Configuration:** "QA Main Pharmacy" store, "QA Vendor Pvt Ltd", theatre "QA-OT2", service "QA-TAXED" and corporate "QACORP".
- **Transactions:** bills, orders and so on created by the run.
- **Still open:** two QA admissions (IP-2026-000003 and IP-2026-000008) are stuck in "discharge in progress", which can't be cancelled, and hold the two ICU beds.
