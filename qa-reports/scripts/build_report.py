import json, os, collections, datetime
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.utils import get_column_letter

OUT = r"d:\Durga Ram Back Up\Prixo_nandhu-speed-multi-seed\qa-reports"
rows = [json.loads(l) for l in open(os.path.join(OUT, "register.jsonl"), encoding="utf-8")]
by_id = {r["id"]: r for r in rows}

# ---------------- curated defects (root causes) ----------------
D = []
def bug(sev, module, title, tcs, impact, steps, expected, actual):
    D.append(dict(sev=sev, module=module, title=title, tcs=tcs, impact=impact, steps=steps, expected=expected, actual=actual))

bug("Critical", "OP Billing", "Concurrent payments are all accepted - money collected twice, bill header shows once", "BILL-054, BILL-100",
    "Cash/receipts recorded that the bill does not reflect; double collection from patient; reconciliation impossible.",
    "Generate OP bill of 500 -> submit 4 'Collect payment 500 cash' requests simultaneously (double-click / two counters).",
    "Exactly one payment accepted; others rejected.", "All 4 accepted: 4 receipts (RCPT-...) for 2,000, bill paidAmount stays 500; 12 bills found with payment rows != paidAmount.")
bug("Critical", "OP Billing", "Refunds can exceed the amount paid; bill paid amount goes negative", "BILL-074, BILL-082",
    "Over-refund / cash leakage.", "Pay 500 -> raise refund 300 -> raise refund 300 -> approve and issue both.",
    "Second refund rejected (total refunds <= paid).", "Both issued; bill paidAmount = -100.")
bug("Critical", "Pharmacy", "Expired stock accepted into inventory and dispensed to a patient", "PH-009, PH-047, PH-048",
    "Patient safety - expired medicine dispensed.", "GRN a batch with expiry 30 days in the past -> post GRN -> POS sale of that drug.",
    "GRN rejected or batch quarantined; sale refused.", "GRN created and posted; sale PSALE-... completed from the expired batch.")
bug("Critical", "Pharmacy", "GST charged on top of MRP - patient billed above MRP", "PH-033",
    "Overcharging; MRP is tax-inclusive by law (Legal Metrology).", "Receive batch with MRP 15 -> sell 10 units.",
    "Total <= 150 (GST contained within MRP).", "Sub-total 150 + GST 18 = 168 charged.")
bug("Critical", "Pharmacy", "Sales returns can exceed quantity sold", "PH-052",
    "Stock inflation and over-refund.", "Sell 10 units -> return 3 -> return 8.",
    "Second return rejected (3 + 8 > 10).", "Both returns posted (11 units refunded against 10 sold).")
bug("Critical", "Pharmacy", "Stock adjustment can drive a batch to negative stock", "PH-064",
    "Inventory corruption.", "Raise adjustment -100000 on a batch holding ~146 units -> approve.",
    "Rejected - on-hand cannot go below zero.", "Approved; batch on-hand became about -99,854 (QA reversed it afterwards).")
bug("Critical", "Pharmacy", "One patient's prescription can be dispensed to a different patient", "PH-045",
    "Wrong-patient medication.", "POS sale for patient Bravo with prescriptionId belonging to patient Alpha.",
    "Rejected - prescription/patient mismatch.", "Sale completed.")
bug("Critical", "IP Management", "A patient already admitted can be admitted again (second bed, second IP number)", "IPD-015",
    "Duplicate inpatient records, double bed charges, census errors.", "Admit patient -> admit same patient again into another vacant bed.",
    "Rejected - patient has an active admission.", "Second admission created.")
bug("Critical", "Discharge / Billing", "Patient can be discharged with an unpaid IP bill; billing clearance ignores the balance", "DIS-005, DIS-026",
    "Revenue loss; final-billing control absent. The Rs 5,000 deposit also stayed un-adjusted on the bill.",
    "Admit (deposit 5000) -> add charges -> request discharge -> give Billing clearance while balance 8,500 -> finalize.",
    "Billing clearance and final discharge refused until the bill is settled.", "Clearance accepted with balance 8,500; IP-2026-000013 discharged with BILL-2026-000097 unpaid (paid 0).")
bug("Critical", "Discharge", "Discharge summary can be e-signed by a non-treating doctor, and in another doctor's name", "DIS-010, DIS-011",
    "Medico-legal - wrong signatory on the discharge summary.", "Doctor B calls summary/approve with approvedByDoctorId = B, then = A (treating).",
    "Only the treating consultant, signing as themselves, can approve.", "Both calls succeeded.")
bug("Critical", "OT", "WHO Surgical Safety Checklist stamped complete with failed safety items", "OT-014, OT-031",
    "Wrong-site surgery / retained-item risk.", "Sign-In with siteMarked=false + complete=true; Sign-Out with instrument count NOT correct + complete=true.",
    "Step cannot be completed until every mandatory item is ticked.", "Both steps saved as complete; surgery can proceed/finish.")
bug("Critical", "Doctor / EMR", "Completed consultation's prescription and diagnosis can be silently overwritten", "EMR-035, EMR-036",
    "Clinical record integrity; prescription may already be dispensed.", "Complete consultation -> PUT a new prescription (5000 mg QID) and new diagnosis.",
    "Locked, or changed only via an addendum/correction trail.", "Both saved with 200.")
bug("Critical", "Lab / Radiology / Billing", "Lab and radiology tests are never billed: no price mapping, zero-rate stub lines, processing not gated", "LAB-061, RAD-036, RAD-037, E2E1-12",
    "Major revenue leakage; no 'billing verification' before tests.", "OP visit -> lab APTT + CT Brain done end-to-end -> generate OP bill.",
    "Tests billed at tariff; processing requires billing.", "0/69 lab and 0/54 radiology tests map to a billing service; OP bill shows consultation only; doctor orders appear as 0.00 stub lines.")

bug("High", "OP Billing", "OP bill can be generated for a cancelled visit", "BILL-005", "Billing a visit that did not happen.",
    "Register OP visit -> cancel visit -> generate OP bill.", "Rejected.", "Bill created (e.g. BILL-2026-000070).")
bug("High", "OP Billing", "Day-close 'expected collection' ignores issued cash refunds", "BILL-102, E2E4-07", "Cashier always appears short by refunds paid out; reconciliation wrong.",
    "Pay 1000 cash -> refund 400 cash -> read day-close collection.", "Collection increases by net 600.", "Collection increased by 1000; full pay-and-refund still adds the full amount.")
bug("High", "Doctor / EMR", "No doctor ownership check on OPD consultation endpoints", "EMR-003, EMR-004", "Any doctor can start/write another doctor's consultation.",
    "Doctor B calls start-consult / vitals on Doctor A's visit.", "Rejected (or explicit cover-doctor workflow).", "Accepted.")
bug("High", "Doctor / EMR", "Reception can open the clinical consultation workspace", "EMR-049", "Clinical data (diagnosis, notes, Rx) exposed to front desk.",
    "Reception GET /opd/visits/{id}/workspace.", "403 (billing role is correctly refused).", "200.")
bug("High", "IP Management", "Progress notes can be filed in another doctor's name", "IPD-044", "Authorship of clinical notes not trustworthy.",
    "Doctor B posts a progress note with doctorId = Doctor A.", "Author = caller.", "Saved as Doctor A.")
bug("High", "IP Management", "IP doctor lab/radiology orders never reach Lab/Radiology", "IPD-040", "Inpatient investigations silently not done.",
    "Doctor places Lab order on an admission -> check /lab/orders.", "Lab order created.", "Order-sheet entry only; no lab order (lab orders cannot carry an admissionId).")
bug("High", "IP Billing", "Pharmacy IP issues are not charged to the IP bill", "IPD-063", "Inpatient medicines dispensed free.",
    "Issue 2 units to admission -> generate IP bill.", "Medicine line on IP bill.", "Bed charges only.")
bug("High", "IP Billing (config)", "General Ward and Semi-Private bed charge is 0.00/day", "IPD-062", "IP stays in those wards bill no bed charge.",
    "Admit to General Ward, transfer to Semi-Private -> generate IP bill.", "Room-type tariff applied.", "Rates 0.00 (ICU 7,500 correct).")
bug("High", "IP Management", "Admission with an IP bill already raised can be cancelled", "IPD-073", "Orphaned IP bill / charges.",
    "Admit -> generate IP bill -> cancel admission.", "Refused until bill cancelled/settled.", "Admission cancelled.")
bug("High", "Discharge", "Clearance segregation: one permission clears all four departments; Billing cannot clear Billing", "DIS-006, DIS-025", "Head Nurse signs off Billing and Pharmacy clearances.",
    "Head Nurse clears Pharmacy item; Billing user clears Billing item.", "Each department clears its own item only.", "Head Nurse allowed; Billing user 403.")
bug("High", "Radiology / EMR", "Approved radiology reports do not appear in the EMR", "RAD-033", "Doctors cannot see imaging results in the patient record.",
    "Approve a radiology report -> EMR timeline/investigations/procedures.", "Report listed.", "Not present anywhere in EMR.")
bug("High", "Laboratory", "Lab result entry accepts non-numeric and negative values for numeric parameters", "LAB-025, LAB-026", "Garbage results can be verified/approved.",
    "Enter 'abc' and '-5' for APTT.", "Rejected.", "Saved.")
bug("High", "IP / Nursing", "IP orders, vitals and risk scores are not validated", "IPD-032, IPD-033, NUR-002, NUR-003, NUR-004, NUR-008", "Unsafe medication orders and implausible observations recorded.",
    "Medication order without dose / ending before start; vitals SpO2 140, Temp 150F, GCS 20; Morse item 999.", "Rejected.", "All saved (201).")
bug("High", "OT", "Implant usage recorded without a serial number", "OT-027", "Implant traceability/recall impossible.",
    "Record implant usage with serialNumber=''.", "Rejected.", "Recorded.")
bug("High", "OT / Billing", "Completing surgery posts no theatre/surgeon/anaesthesia charge", "E2E3-06", "OT revenue depends on manual keying.",
    "Complete surgery -> generate IP bill.", "OT charge created by OT module.", "Only bed lines; OT hours keyed manually.")
bug("High", "Pharmacy (config)", "Speed Multi branch has no pharmacy store or vendor", "PH-000", "No purchase, GRN, sale or IP issue possible at go-live.",
    "Open /pharmacy/stores in SPEED_MULTI.", ">= 1 pharmacy store.", "None (QA created 'QA Main Pharmacy' and a QA vendor to test).")
bug("High", "Patient Registration", "Exact duplicate patient registered without warning at save", "REG-015", "Duplicate UHIDs for one person (relies on UI pre-check only).",
    "Register a patient with the same name, DOB and mobile as an existing one.", "Blocked or duplicate warning requiring confirmation.", "New UHID created.")
bug("High", "Pharmacy", "No maker-checker on stock adjustments", "PH-062", "One person can raise and approve their own write-off.",
    "Pharmacy Manager raises adjustment -> approves it.", "Self-approval refused.", "Approved.")

bug("Medium", "Patient Registration", "Blocking a patient does not stop OP registration (documented as deliberate)", "REG-027", "Block flag is informational only.",
    "Block patient -> register OP visit.", "Refused.", "Visit registered.")
bug("Medium", "Patient Registration", "Registration accepts invalid gender and future date of birth", "REG-009, REG-010", "Bad demographic data.",
    "Register with gender 0; with DOB 30 days ahead.", "Rejected.", "UHIDs created.")
bug("Medium", "Patient Registration", "Full-name search ('First Last') returns no result", "REG-016", "Front desk cannot find patients by full name.",
    "Search 'QATEST Alpha'.", "Patient found.", "0 results ('Alpha' alone works).")
bug("Medium", "Lab / Radiology", "No duplicate-order prevention", "LAB-005, LAB-006, RAD-005", "Duplicate tests/scans and charges.",
    "Same test twice in one order; same test re-ordered while open; same scan twice.", "Blocked or warned.", "Accepted.")
bug("Medium", "Platform", "Unhandled server errors (HTTP 500) instead of validation messages", "LAB-004, PH-042, RAD-056", "Users see 'unexpected error'.",
    "Lab order with unknown test; POS sale with unknown payment mode; radiology schedule time without timezone.", "4xx with a clear message.", "HTTP 500.")
bug("Medium", "Scheduling", "Past-dated and out-of-hours bookings accepted", "RAD-011, OT-006, APT-005, APT-006, APT-013", "Phantom bookings; queue/token errors.",
    "Schedule scan/OT/appointment in the past; appointment at 03:00 (schedule 08-20); check-in a day early.", "Rejected.", "All accepted.")
bug("Medium", "Radiology", "Modality double-booking allowed and scans cannot be rescheduled once scheduled", "RAD-013, RAD-014", "Clashes; rescheduling needs cancel + re-order.",
    "Schedule two patients at the same modality minute; reschedule a scheduled item.", "Conflict blocked; reschedule allowed.", "Double-booked; reschedule refused.")
bug("Medium", "OP Billing", "Discount / concession can be applied without a reason", "BILL-030", "Weak audit of concessions.",
    "Apply 5% bill discount with empty reason.", "Reason mandatory.", "Applied.")
bug("Medium", "OP Billing", "Same UPI transaction reference accepted on two bills", "BILL-053", "Duplicate-payment / fraud risk.",
    "Collect UPI with ref QA-UPI-0001 on bill A and again on bill B.", "Second use rejected or flagged.", "Accepted.")
bug("Medium", "OP Billing", "Corporate credit bill above the corporate credit limit not blocked or flagged", "BILL-120", "Unchecked credit exposure.",
    "Corporate limit 1,000 -> mark 2,000 bill as credit.", "Blocked or warned.", "Marked credit; no limit/exposure shown.")
bug("Medium", "Pharmacy", "GRN accepts MRP below purchase rate and receipts far above the PO", "PH-011, PH-018", "Selling below cost; over-receipt.",
    "GRN with MRP 5 < cost 8; GRN of 9,999 against PO of 100.", "Rejected or warned.", "Accepted.")
bug("Medium", "IP Management (RBAC)", "Doctor role cannot raise the admission recommendation", "IPD-001", "Doctors depend on Front Office to raise the request.",
    "Doctor POST /ipd/admission-requests.", "Allowed.", "403 (role lacks IPD.ADMISSIONREQUEST.CREATE).")
bug("Medium", "OT / Billing", "Implant/consumable usage refused mid-surgery until Billing generates the IP bill", "OT-037", "Theatre staff blocked by billing desk.",
    "Start surgery (no IP bill yet) -> record consumable.", "Recorded; charge queued.", "400 'Generate the IP bill ... before recording usage'.")
bug("Low", "Various", "Minor validation/reporting gaps", "EMR-013, EMR-032, REG-003, LAB-062", "Data quality.",
    "Systolic < diastolic; follow-up date in past; UHID gap after rejected save; unbilled report without dates.", "Rejected / gap-free / defaults.", "Accepted / gap / empty list.")

for i, b in enumerate(D, 1):
    b["id"] = f"BUG-{i:03d}"

# ---------------- workbook ----------------
wb = Workbook()
H = PatternFill("solid", fgColor="1F3864"); HF = Font(bold=True, color="FFFFFF")
FILL = {"PASS": "C6EFCE", "FAIL": "FFC7CE", "BLOCKED": "FFEB9C", "NOT TESTED": "D9D9D9",
        "Critical": "FF9999", "High": "FFCC99", "Medium": "FFF2CC", "Low": "E2EFDA"}
thin = Side(style="thin", color="BFBFBF")


def sheet(ws, headers, data, widths, colour_col=None):
    ws.append(headers)
    for c in ws[1]:
        c.fill = H; c.font = HF; c.alignment = Alignment(wrap_text=True, vertical="top")
    for r in data:
        ws.append(r)
    for row in ws.iter_rows(min_row=2):
        for c in row:
            c.alignment = Alignment(wrap_text=True, vertical="top"); c.border = Border(top=thin, bottom=thin, left=thin, right=thin)
        if colour_col is not None:
            v = row[colour_col].value
            if v in FILL:
                row[colour_col].fill = PatternFill("solid", fgColor=FILL[v])
    for i, w in enumerate(widths, 1):
        ws.column_dimensions[get_column_letter(i)].width = w
    ws.freeze_panes = "A2"
    ws.auto_filter.ref = ws.dimensions


mods = sorted({r["module"] for r in rows})
cnt = collections.Counter((r["module"], r["status"]) for r in rows)
tot = collections.Counter(r["status"] for r in rows)
sevc = collections.Counter(b["sev"] for b in D)
executed = tot["PASS"] + tot["FAIL"]

# 1 Summary
ws = wb.active; ws.title = "Summary"
ws.append(["Prixo HIS - Functional QA Summary"]); ws["A1"].font = Font(bold=True, size=14)
meta = [("Executed on", datetime.date.today().isoformat()), ("Environment", "Local dev, branch SPEED_MULTI, fresh DB (prixo-fresh), API http://localhost:5000"),
        ("Method", "Executed through the live REST API with role-specific synthetic test users; UI not driven (no browser available) - layout/UI messages NOT TESTED"),
        ("Test data", "Synthetic only: patients named 'QATEST ...', users 'qa.*' (password Qa@12345)"),
        ("Total test cases", len(rows)), ("Executed (PASS+FAIL)", executed), ("Passed", tot["PASS"]), ("Failed", tot["FAIL"]),
        ("Blocked", tot["BLOCKED"]), ("Not tested (feature absent)", tot["NOT TESTED"]), ("Distinct defects (root causes)", len(D)),
        ("Critical defects", sevc["Critical"]), ("High defects", sevc["High"]), ("Medium defects", sevc["Medium"]), ("Low defects", sevc["Low"]),
        ("Readiness verdict", "NOT READY for go-live - 13 Critical defects in billing integrity, medication safety, discharge/finance controls and OT safety checklist")]
for k, v in meta:
    ws.append([k, v])
ws.append([]); ws.append(["Module", "Total", "PASS", "FAIL", "BLOCKED", "NOT TESTED", "Pass rate (executed)"])
for c in ws[ws.max_row]:
    c.fill = H; c.font = HF
for m in mods:
    p_, f_, b_, n_ = cnt[(m, "PASS")], cnt[(m, "FAIL")], cnt[(m, "BLOCKED")], cnt[(m, "NOT TESTED")]
    ws.append([m, p_ + f_ + b_ + n_, p_, f_, b_, n_, f"{100 * p_ / max(1, p_ + f_):.0f}%"])
ws.column_dimensions["A"].width = 34; ws.column_dimensions["B"].width = 110
for col in "CDEFG":
    ws.column_dimensions[col].width = 14

# 2 Module checklist
CHECK = [
 ("A Patient Registration", ["New registration", "REG-001"], ["UHID generation & uniqueness", "REG-001,REG-002,REG-003"], ["Duplicate detection", "REG-014,REG-015"], ["Mandatory field validation", "REG-004..REG-013"],
  ["Department selection (mandatory, clinical only)", "IPD-011, E2E1-03"], ["Doctor/unit selection", "REG-029, E2E1-03"], ["Consultation charge", "BILL-001, EMR-040"], ["Registration payment & receipt", "REG-041 (feature absent)"],
  ["Patient search", "REG-016..REG-019"], ["Modification", "REG-020..REG-023"], ["Registration cancellation", "REG-040 (feature absent)"], ["Block/merge/history", "REG-025..REG-037"]),
 ("B Doctor Dashboard / EMR", ["Doctor login & queue", "EMR-001, EMR-002"], ["Vitals", "EMR-007..EMR-013"], ["Complaints/notes", "EMR-014, EMR-015"], ["Diagnosis", "EMR-016, EMR-017"],
  ["Investigation/lab/radiology/procedure orders", "EMR-023..EMR-028"], ["Prescription + allergy check", "EMR-018..EMR-022"], ["Completion", "EMR-033, EMR-034"], ["Previous consultations", "EMR-040..EMR-045"],
  ["Modification/correction", "EMR-035..EMR-038"], ["Access control", "EMR-003, EMR-004, EMR-046..EMR-049"]),
 ("C OP Billing", ["UHID search / pending orders", "BILL-090, BILL-091, E2E1-12"], ["Consultation / service billing", "BILL-001..BILL-015"], ["Discount & concession", "BILL-020..BILL-032, BILL-121"],
  ["Referral", "BILL-122 (no billing referral)"], ["Cash/UPI/multi-mode/partial/full", "BILL-040..BILL-053"], ["Duplicate payment prevention", "BILL-047, BILL-054"], ["Receipt", "BILL-050, BILL-051"],
  ["Refund", "BILL-070..BILL-082"], ["Cancellation", "BILL-060..BILL-069"], ["Reconciliation / shift / day close", "BILL-100..BILL-113"]),
 ("D Laboratory", ["Order reception", "LAB-001..LAB-008"], ["Sample collection / identification / barcode", "LAB-010..LAB-016"], ["Rejection & recollect", "LAB-018..LAB-024"],
  ["Result entry/modification/flags", "LAB-025..LAB-033"], ["Verification & approval", "LAB-034..LAB-043, LAB-060"], ["Report / PDF / EMR", "LAB-044, LAB-047..LAB-049"], ["Cancellation", "LAB-045, LAB-046, LAB-050..LAB-053"],
  ["Billing linkage", "LAB-009, LAB-061, LAB-062"]),
 ("E Radiology", ["Order", "RAD-001..RAD-008"], ["Scheduling / rescheduling", "RAD-009..RAD-015, RAD-056"], ["Scan/capture & contrast consent", "RAD-017, RAD-040..RAD-043"], ["Image upload/PACS", "RAD-018 (absent)"],
  ["Report / verify / approve / PDF", "RAD-016..RAD-029"], ["Correction", "RAD-029, RAD-030 (absent)"], ["EMR availability", "RAD-033, RAD-034"], ["Billing verification", "RAD-035..RAD-037"], ["Cancellation", "RAD-031, RAD-050..RAD-053"]),
 ("F Pharmacy", ["Purchase PR/PO/GRN", "PH-001..PH-018"], ["Batch / expiry", "PH-009, PH-019, PH-020, PH-047, PH-048"], ["Dispensing / FEFO / billing", "PH-030..PH-043"], ["Prescription & partial dispensing", "PH-044, PH-045"],
  ["Alternative medicine", "PH-046 (absent)"], ["Returns / cancellation", "PH-050..PH-059"], ["Adjustments / purchase return", "PH-060..PH-071"], ["Concurrency / ledger / alerts", "PH-080..PH-092"]),
 ("G IP Management + Nursing", ["Admission recommendation/approval", "IPD-001..IPD-008"], ["IP number / bed / wards / ICU", "IPD-010..IPD-023"], ["Doctor orders", "IPD-030..IPD-044"],
  ["Nursing / vitals / MAR", "NUR-001..NUR-020"], ["Pharmacy issue", "IPD-045"], ["Transfers", "IPD-050..IPD-055"], ["IP billing", "IPD-060..IPD-064"], ["Cancellation", "IPD-070..IPD-073"]),
 ("H OT Management", ["Booking & conflicts", "OT-001..OT-007"], ["Minor/Major, emergency, approval", "OT-008..OT-010 (absent)"], ["Team", "OT-011, OT-012"], ["Checklist & consent gates", "OT-013..OT-018, OT-024, OT-030, OT-031"],
  ["Anaesthesia / notes", "OT-019, OT-023, OT-032, OT-034"], ["Start/completion times", "OT-020, OT-021, OT-033"], ["Consumables & implants", "OT-025..OT-029, OT-037"], ["Cancel / reschedule / postpone", "OT-022, OT-035, OT-041..OT-045"]),
 ("I Discharge", ["Recommendation", "DIS-001, DIS-002"], ["Clearances", "DIS-003..DIS-006, DIS-025"], ["Final billing / settlement", "DIS-005, DIS-026"], ["Summary contents & approval", "DIS-007..DIS-014, DIS-019, DIS-020"],
  ["Completion / bed release / PDFs", "DIS-015..DIS-018, DIS-021"], ["Post-discharge locks", "DIS-022, DIS-023"], ["Reversal", "DIS-024 (absent)"]),
]
data = []
for mod, *items in CHECK:
    for it, ref in items:
        ids = []
        for part in ref.replace(" ", "").split(","):
            part = part.split("(")[0]
            if ".." in part:
                a, b = part.split("..")
                pre = a.rsplit("-", 1)[0]
                lo, hi = int(a.rsplit("-", 1)[1]), int(b.rsplit("-", 1)[1])
                ids += [k for k in by_id if k.startswith(pre + "-") and k.rsplit("-", 1)[1].isdigit() and lo <= int(k.rsplit("-", 1)[1]) <= hi]
            elif part in by_id:
                ids.append(part)
        sts = collections.Counter(by_id[i]["status"] for i in ids)
        overall = "FAIL" if sts["FAIL"] else ("NOT TESTED" if sts["NOT TESTED"] and not sts["PASS"] else ("PASS" if sts["PASS"] else ("BLOCKED" if sts["BLOCKED"] else "NOT TESTED")))
        data.append([mod, it, ref, overall, sts["PASS"], sts["FAIL"], sts["BLOCKED"] + sts["NOT TESTED"]])
sheet(wb.create_sheet("1 Module Checklist"), ["Module", "Checklist item", "Test cases", "Result", "Pass", "Fail", "Blocked/NT"], data, [26, 40, 40, 13, 7, 7, 11], colour_col=3)

# 3 Test case register
reg = [[r["id"], r["module"], r["scenario"], r["preconditions"] or "Synthetic data; role-specific QA user; SPEED_MULTI branch", r["steps"] or r["scenario"], r["expected"], r["actual"], r["status"],
        r["severity"] if r["status"] == "FAIL" else "", r["bug"] if r["status"] == "FAIL" else "", f"register.jsonl @ {r['ts']}"] for r in rows]
sheet(wb.create_sheet("2 Test Case Register"), ["Test Case ID", "Module", "Test Scenario", "Preconditions", "Test Steps", "Expected Result", "Actual Result", "Status", "Severity", "Bug Description", "Evidence"],
      reg, [11, 20, 46, 26, 36, 30, 46, 11, 10, 46, 22], colour_col=7)

# 4 E2E
e2e = [[r["id"], r["module"], r["scenario"], r["expected"], r["actual"], r["status"]] for r in rows if r["module"].startswith("E2E")]
sheet(wb.create_sheet("3 End-to-End Report"), ["Step ID", "Journey", "Step", "Expected", "Actual", "Status"], e2e, [10, 24, 60, 30, 60, 11], colour_col=5)

# 5 Negative & cancellation
kw = ("cancel", "reject", "refund", "return", "revers", "blocked", "without", "invalid", "unknown", "negative", "past", "duplicate", "twice", "again", "above", "beyond", "more than", "cannot")
neg = [[r["id"], r["module"], r["scenario"], r["expected"], r["actual"], r["status"], r["severity"] if r["status"] == "FAIL" else ""] for r in rows if any(k in r["scenario"].lower() for k in kw)]
sheet(wb.create_sheet("4 Negative & Cancellation"), ["Test Case ID", "Module", "Scenario", "Expected", "Actual", "Status", "Severity"], neg, [11, 20, 60, 30, 55, 11, 10], colour_col=5)

# 6 Integration
integ_ids = ["BILL-001", "BILL-013", "LAB-009", "LAB-047", "LAB-061", "RAD-033", "RAD-035", "RAD-036", "RAD-037", "EMR-019", "EMR-030", "EMR-041", "EMR-043", "PH-044", "PH-045",
             "IPD-013", "IPD-023", "IPD-040", "IPD-045", "IPD-060", "IPD-061", "IPD-062", "IPD-063", "OT-025", "OT-029", "OT-037", "DIS-005", "DIS-012", "DIS-021", "DIS-026",
             "BILL-100", "BILL-101", "BILL-102", "E2E1-03", "E2E1-07", "E2E1-12", "E2E1-14", "E2E2-02", "E2E2-04", "E2E2-07", "E2E3-06", "E2E4-07"]
integ = [[i, by_id[i]["module"], by_id[i]["scenario"], by_id[i]["expected"], by_id[i]["actual"], by_id[i]["status"]] for i in integ_ids if i in by_id]
sheet(wb.create_sheet("5 Integration"), ["Test Case ID", "Module", "Integration point", "Expected", "Actual", "Status"], integ, [11, 20, 60, 34, 60, 11], colour_col=5)

# 7 Defects
dd = [[b["id"], b["sev"], b["module"], b["title"], b["tcs"], b["impact"], b["steps"], b["expected"], b["actual"], "Open"] for b in D]
sheet(wb.create_sheet("6 Defect Register"), ["Bug ID", "Severity", "Module", "Title", "Failed test cases", "Impact", "Steps to reproduce", "Expected", "Actual", "Status"],
      dd, [9, 10, 20, 50, 22, 40, 50, 34, 50, 8], colour_col=1)

# 8 Not tested / blocked
nt = [[r["id"], r["module"], r["scenario"], r["status"], r["actual"]] for r in rows if r["status"] in ("NOT TESTED", "BLOCKED")]
nt.append(["UI-ALL", "All modules", "Screen layout, field-level UI validation, dropdown behaviour, print previews", "NOT TESTED", "No browser tool in this session - every case was executed via the API the UI calls."])
sheet(wb.create_sheet("7 Not Tested & Blocked"), ["ID", "Module", "Scenario", "Status", "Reason"], nt, [11, 22, 55, 13, 90], colour_col=3)

path = os.path.join(OUT, "Prixo_HIS_QA_Report.xlsx")
wb.save(path)
json.dump(D, open(os.path.join(OUT, "defects.json"), "w"), indent=1)
print("saved", path, "| cases", len(rows), dict(tot), "| defects", len(D), dict(sevc))
