import sys, os, datetime, time
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from qa_lib import *
from decimal import Decimal as D

MG, MN, MD = "G-IP Management", "G-Nursing", "I-Discharge"
st = load_state(); DOC = st["doctors"]["qa.doctor"]; DOC2 = st["doctors"]["qa.doctor2"]; GM = st["depts"]["DEP001"]
admin = Sess("admin", "Admin@123", 2); rec = Sess("qa.reception", "Qa@12345", 2); doc = Sess("qa.doctor", "Qa@12345", 2); doc2 = Sess("qa.doctor2", "Qa@12345", 2)
nur = Sess("qa.nurse", "Qa@12345", 2); hn = Sess("qa.headnurse", "Qa@12345", 2); bil = Sess("qa.billing", "Qa@12345", 2); ph = Sess("qa.pharm", "Qa@12345", 2)
mgr = Sess("qa.pharmmgr", "Qa@12345", 2); fo = Sess("qa.frontoffice", "Qa@12345", 2)
RUN = datetime.datetime.now().strftime("%H%M%S")
TODAY = datetime.date.today()


def idof(d):
    return d["data"] if not isinstance(d["data"], dict) else d["data"]["id"]


def newpt(tag, g=1):
    s, d = rec.post("/Patients", patient_body("QATEST", f"Ip{tag}{RUN}", g, "1965-04-04", "90000006" + RUN[-2:]))
    assert ok(s), (s, d)
    return d["data"]["id"]


def board():
    return admin.get("/ipd/bed-board")[1]["data"]


def vacant(ward):
    return next(b["bedId"] for b in board() if b["wardName"] == ward and b["status"] == 1)


def bed(bid):
    return next(b for b in board() if b["bedId"] == bid)


def admit(pid, bed_id, who=None, **kw):
    b = {"patientId": pid, "primaryConsultantId": DOC, "departmentId": GM, "bedId": bed_id, "admissionType": 1, "provisionalDiagnosis": "QA provisional",
         "referringOpVisitId": None, "referringEmergencyVisitId": None, "secondaryConsultantIds": [], "consentTemplateId": None, "consentSignedByName": None,
         "consentRelationshipToPatient": None, "initialDepositAmount": 0, "depositPaymentModeId": None, "depositReferenceNumber": None, "admissionRequestId": None}
    b.update(kw)
    return (who or fo).post("/ipd/admissions", b)


def adm(aid):
    return admin.get(f"/ipd/admissions/{aid}")[1]["data"]


# =========================== ADMISSION REQUESTS ===========================
pA = newpt("A")
s, d = doc.post("/ipd/admission-requests", {"patientId": pA, "requestedConsultantId": DOC, "wardId": None, "expectedAdmissionDate": TODAY.isoformat(), "reason": "QA admission advised"})
tc("IPD-001", MG, "Doctor raises admission recommendation (admission request)", ok(s), f"{s} {msg(d)}", "Request Pending", severity="Medium",
   bug="The Doctor role lacks IPD.ADMISSIONREQUEST.CREATE, so the treating doctor cannot raise the admission recommendation; only Front Office can.")
s, d = fo.post("/ipd/admission-requests", {"patientId": pA, "requestedConsultantId": DOC, "wardId": None, "expectedAdmissionDate": TODAY.isoformat(), "reason": "QA admission advised by Dr QA"})
tc("IPD-008", MG, "Front Office raises admission request on doctor's advice", ok(s), f"{s} {msg(d)}", "Request Pending", severity="High")
req = idof(d) if ok(s) else None
s, d = admit(pA, vacant("General Ward"), admissionRequestId=req)
tc("IPD-002", MG, "Admitting against a NOT-yet-approved request is blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = nur.post(f"/ipd/admission-requests/{req}/approve")
tc("IPD-003", MG, "Nurse cannot approve admission requests (RBAC)", s == 403, f"{s}", "403", severity="Medium")
s, d = fo.post(f"/ipd/admission-requests/{req}/approve")
tc("IPD-004", MG, "Admission request approved", ok(s), f"{s} {msg(d)}", "Approved", severity="High")
pR = newpt("R")
s, d = fo.post("/ipd/admission-requests", {"patientId": pR, "requestedConsultantId": DOC, "wardId": None, "expectedAdmissionDate": TODAY.isoformat(), "reason": "QA reject path"})
rq2 = idof(d)
s, d = fo.post(f"/ipd/admission-requests/{rq2}/reject", {"reason": ""})
tc("IPD-005", MG, "Reject admission request without reason blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
fo.post(f"/ipd/admission-requests/{rq2}/reject", {"reason": "QA bed not required"})
s, d = admit(pR, vacant("General Ward"), admissionRequestId=rq2)
tc("IPD-006", MG, "Admitting against a REJECTED request is blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = admit(pR, vacant("General Ward"), admissionRequestId=req)
tc("IPD-007", MG, "Admitting patient B against patient A's approved request is blocked", rej(s), f"{s} {msg(d)}", "Rejected (patient mismatch)", severity="Critical")

# =========================== ADMISSION ===========================
gw = vacant("General Ward")
s, d = admit(pA, gw, admissionRequestId=req, initialDepositAmount=5000, depositPaymentModeId=1)
tc("IPD-010", MG, "Admit patient to General Ward with Rs 5000 deposit", ok(s), f"{s} {msg(d)}", "Admitted", severity="Critical")
A = idof(d); a = adm(A)
import re
tc("IPD-011", MG, "IP number generated in IP-YYYY-NNNNNN format and department captured", re.match(r"^IP-\d{4}-\d{6}$", a["admissionNumber"]) and a["departmentName"] == "General Medicine",
   f"{a['admissionNumber']} dept={a['departmentName']}", "IP-2026-NNNNNN, General Medicine", severity="High")
tc("IPD-012", MG, "Allocated bed becomes Occupied and shows this patient", bed(gw)["status"] == 2 and bed(gw)["admissionId"] == A, f"bed status={bed(gw)['status']} adm={bed(gw)['admissionId']}", "Occupied by this admission", severity="Critical")
adv = admin.get(f"/billing/advances/patient/{pA}")[1]["data"]
tc("IPD-013", MG, "Initial deposit recorded as a patient advance linked to the admission", any(D(str(x.get("amount", 0))) == 5000 for x in (adv if isinstance(adv, list) else adv.get("items", []))), json.dumps(adv)[:200], "Advance 5000", severity="Critical")
s, d = admin.get(f"/ipd/admissions/{A}/admission-slip")
tc("IPD-014", MG, "Admission slip PDF", s == 200 and d.get("binary") and d["head"] == b"%PDF-", f"{s}", "PDF", severity="Medium")
s, d = admit(pA, vacant("General Ward"))
tc("IPD-015", MG, "Second admission for a patient who is already admitted is blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical",
   bug="A patient with an active admission can be admitted again into another bed (two concurrent IP numbers and two occupied beds for one patient).")
if ok(s):
    admin.post(f"/ipd/admissions/{idof(d)}/cancel", {"reason": "QA cleanup duplicate admission"})
pB = newpt("B", 2)
s, d = admit(pB, gw)
tc("IPD-016", MG, "Admitting into an OCCUPIED bed is blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
blk = vacant("General Ward")
admin.post(f"/ipd/bed-board/{blk}/status", {"status": 5, "reason": "QA maintenance"})
s, d = admit(pB, blk)
tc("IPD-017", MG, "Admitting into a BLOCKED (maintenance) bed is blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
admin.post(f"/ipd/bed-board/{blk}/status", {"status": 1, "reason": "QA restore"})
s, d = admit(pB, vacant("General Ward"), initialDepositAmount=1000, depositPaymentModeId=None)
tc("IPD-018", MG, "Deposit without payment mode rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = admit(pB, vacant("General Ward"), consentTemplateId=1, consentSignedByName=None)
tc("IPD-019", MG, "Consent template selected without signatory rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = admit(pB, vacant("General Ward"), who=nur)
tc("IPD-020", MG, "Nurse cannot admit patients (RBAC)", s == 403, f"{s}", "403", severity="Medium")
pv = vacant("Private Ward")
s, d = admit(pB, pv, consentTemplateId=1, consentSignedByName="QATEST Guardian", consentRelationshipToPatient="Son")
tc("IPD-021", MG, "Special ward (Private) allocation with signed consent", ok(s) and bed(pv)["status"] == 2, f"{s} {msg(d)}", "Admitted to Private Ward", severity="High")
B = idof(d) if ok(s) else None
pC = newpt("C")
icu = vacant("ICU")
s, d = admit(pC, icu)
tc("IPD-022", MG, "ICU bed allocation", ok(s), f"{s} {msg(d)}", "Admitted to ICU", severity="High")
C = idof(d) if ok(s) else None
s, d = admin.get("/icu/dashboard")
tc("IPD-023", MG, "ICU admission appears on ICU dashboard census", s == 200 and "IP-" in json.dumps(d["data"]), f"{s} {json.dumps(d.get('data'))[:150]}", "Listed", severity="Medium")

# =========================== DOCTOR ORDERS / CHARTING ===========================
drugs = admin.get("/Drugs?Page=1&PageSize=200")[1]["data"]["items"]
hi = next(x for x in drugs if x.get("isHighAlert")); norm = next(x for x in drugs if not x.get("isHighAlert"))
med = lambda drug: {"orderedByDoctorId": DOC, "orderType": 1, "description": drug["name"], "priority": 1, "notes": None,
                    "medication": {"drugId": drug["id"], "dose": "1 unit", "route": "IV", "frequency": "BD", "startDate": TODAY.isoformat(), "endDate": (TODAY + datetime.timedelta(days=2)).isoformat(), "instructions": None}, "diet": None}
s, d = doc.post(f"/ipd/admissions/{A}/orders", med(norm)); tc("IPD-030", MG, "Doctor medication order", ok(s), f"{s} {msg(d)}", "Created", severity="Critical")
s, d = doc.post(f"/ipd/admissions/{A}/orders", med(hi)); tc("IPD-031", MG, "Doctor medication order for a HIGH-ALERT drug", ok(s), f"{s} {msg(d)}", "Created", severity="High")
bad = med(norm); bad["medication"]["dose"] = ""
s, d = doc.post(f"/ipd/admissions/{A}/orders", bad); tc("IPD-032", MG, "Medication order without dose rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
bad = med(norm); bad["medication"]["endDate"] = (TODAY - datetime.timedelta(days=5)).isoformat()
s, d = doc.post(f"/ipd/admissions/{A}/orders", bad); tc("IPD-033", MG, "Medication order ending before it starts rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
for t, lab_ in ((2, "Lab"), (3, "Radiology"), (5, "Nursing")):
    s, d = doc.post(f"/ipd/admissions/{A}/orders", {"orderedByDoctorId": DOC, "orderType": t, "description": f"QA {lab_} order", "priority": 1, "notes": None, "medication": None, "diet": None})
    tc(f"IPD-03{3 + t}", MG, f"Doctor {lab_} order", ok(s), f"{s} {msg(d)}", "Created", severity="High")
s, d = doc.post(f"/ipd/admissions/{A}/orders", {"orderedByDoctorId": DOC, "orderType": 4, "description": "Diet", "priority": 1, "notes": None, "medication": None, "diet": {"dietType": "Diabetic", "instructions": None}})
tc("IPD-039", MG, "Diet order", ok(s), f"{s} {msg(d)}", "Created", severity="Medium")
lab_orders = admin.get(f"/lab/orders?PatientId={pA}&Page=1&PageSize=5")[1]["data"]["totalCount"]
tc("IPD-040", MG, "IP doctor's Lab order reaches the Laboratory module", lab_orders >= 1, f"lab orders for patient={lab_orders}", ">= 1 lab order created", "Doctor raises Lab order on admission -> check /lab/orders", severity="High",
   bug="IP doctor lab/radiology orders are free-text order-sheet entries only; they do not create Lab/Radiology orders (CreateLabOrderRequest has no admissionId), so the lab never receives them.")
orders = doc.get(f"/ipd/admissions/{A}/orders")[1]["data"]
lastnurse = [o for o in orders if o["orderType"] == 5][0]
s, d = doc.post(f"/ipd/admissions/{A}/orders/{lastnurse['id']}/cancel", {"reason": ""})
tc("IPD-041", MG, "Cancel doctor order without reason rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = doc.post(f"/ipd/admissions/{A}/orders/{lastnurse['id']}/cancel", {"reason": "QA no longer needed"})
tc("IPD-042", MG, "Cancel doctor order with reason", ok(s), f"{s} {msg(d)}", "Cancelled", severity="Medium")

vit = {"temperatureF": 99.1, "pulseRate": 88, "respiratoryRate": 18, "bloodPressureSystolic": 130, "bloodPressureDiastolic": 85, "spO2": 97, "randomBloodSugar": 140, "glasgowComaScale": None, "meanArterialPressure": None, "centralVenousPressure": None, "urineOutputMl": None}
s, d = nur.post(f"/ipd/admissions/{A}/vitals", vit); tc("NUR-001", MN, "Nurse records IP vitals", ok(s), f"{s} {msg(d)}", "Saved", severity="High")
for tid, f, v_ in (("NUR-002", "spO2", 140), ("NUR-003", "temperatureF", 150), ("NUR-004", "glasgowComaScale", 20)):
    b = dict(vit); b[f] = v_
    s, d = nur.post(f"/ipd/admissions/{A}/vitals", b); tc(tid, MN, f"Vitals validation: {f}={v_} rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = nur.post(f"/nursing/admissions/{A}/notes", {"shift": 1, "notes": "QA morning note"}); tc("NUR-005", MN, "Shift nursing note", ok(s), f"{s} {msg(d)}", "Saved", severity="High")
s, d = nur.post(f"/nursing/admissions/{A}/notes", {"shift": 1, "notes": ""}); tc("NUR-006", MN, "Empty nursing note rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Low")
s, d = nur.post(f"/nursing/admissions/{A}/risk-assessments", {"assessmentType": 2, "itemScores": {"historyOfFalling": 25, "secondaryDiagnosis": 15, "ambulatoryAid": 15, "ivTherapy": 20, "gait": 10, "mentalStatus": 0}})
ra = nur.get(f"/nursing/admissions/{A}/risk-assessments?type=2")[1]["data"]
last = ra[0] if ra else {}
tc("NUR-007", MN, "Morse fall score 85 auto-banded HIGH (>=45)", ok(s) and last.get("totalScore") == 85 and last.get("riskLevel") == "High", f"{s} total={last.get('totalScore')} level={last.get('riskLevel')}", "85 / High", severity="High")
s, d = nur.post(f"/nursing/admissions/{A}/risk-assessments", {"assessmentType": 2, "itemScores": {"historyOfFalling": 999}})
tc("NUR-008", MN, "Morse item score outside the scale (999) rejected", rej(s), f"{s} {msg(d)}", "Rejected (Morse max 125)", severity="Medium")
s, d = nur.post(f"/nursing/admissions/{A}/risk-assessments", {"assessmentType": 3, "itemScores": {"sensory": 2, "moisture": 2, "activity": 2, "mobility": 2, "nutrition": 2, "friction": 1}})
ra = nur.get(f"/nursing/admissions/{A}/risk-assessments?type=3")[1]["data"]; last = ra[0] if ra else {}
tc("NUR-009", MN, "Braden score 11 auto-banded HIGH (10-12)", ok(s) and last.get("totalScore") == 11 and last.get("riskLevel") == "High", f"{s} total={last.get('totalScore')} level={last.get('riskLevel')}", "11 / High", severity="High")
s, d = nur.post(f"/nursing/admissions/{A}/care-plans", {"problemStatement": "Risk of falls", "goal": "No falls", "interventions": "Bed rails, assist ambulation"})
tc("NUR-010", MN, "Nursing care plan", ok(s), f"{s} {msg(d)}", "Saved", severity="Medium")
s, d = nur.post(f"/ipd/admissions/{A}/intake-output", {"isIntake": True, "category": "IV", "quantityMl": 500, "notes": None}); tc("NUR-011", MN, "Intake/output charting", ok(s), f"{s} {msg(d)}", "Saved", severity="Medium")
s, d = nur.post(f"/ipd/admissions/{A}/intake-output", {"isIntake": True, "category": "IV", "quantityMl": -100, "notes": None}); tc("NUR-012", MN, "Negative intake volume rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = doc.post(f"/ipd/admissions/{A}/progress-notes", {"doctorId": DOC, "notes": "QA progress"}); tc("IPD-043", MG, "Doctor progress note", ok(s), f"{s} {msg(d)}", "Saved", severity="High")
s, d = doc2.post(f"/ipd/admissions/{A}/progress-notes", {"doctorId": DOC, "notes": "Written by doctor2 as doctor1"})
tc("IPD-044", MG, "A doctor cannot sign a progress note in ANOTHER doctor's name", rej(s), f"{s} {msg(d)}", "Rejected (author must be the caller)", severity="High",
   bug="Progress notes take doctorId from the request body; any doctor can file a note attributed to a different doctor.")

# MAR
mar = nur.get(f"/nursing/mar?admissionId={A}&date={TODAY.isoformat()}")[1]["data"]
tc("NUR-013", MN, "MAR schedule auto-generated from medication orders", isinstance(mar, list) and len(mar) >= 1, f"{len(mar) if isinstance(mar, list) else mar} doses today", ">= 1 due dose", severity="Critical")
if isinstance(mar, list) and mar:
    hi_dose = next((m for m in mar if m.get("isHighAlert") or hi["name"] in json.dumps(m)), None)
    n_dose = next((m for m in mar if m is not hi_dose), None)
    if n_dose:
        s, d = nur.post(f"/nursing/mar/{n_dose['id']}/administer", {"action": 3, "reason": "", "notes": None, "verifiedByUserId": None, "batchId": None, "quantity": None})
        tc("NUR-014", MN, "Skipping a dose without reason rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
        s, d = nur.post(f"/nursing/mar/{n_dose['id']}/administer", {"action": 2, "reason": None, "notes": "QA given", "verifiedByUserId": None, "batchId": None, "quantity": None})
        tc("NUR-015", MN, "Administer a routine dose", ok(s), f"{s} {msg(d)}", "Administered", severity="Critical")
        s, d = nur.post(f"/nursing/mar/{n_dose['id']}/administer", {"action": 2, "reason": None, "notes": "again", "verifiedByUserId": None, "batchId": None, "quantity": None})
        tc("NUR-016", MN, "Administering the same dose twice blocked (double-dosing)", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
    if hi_dose:
        s, d = nur.post(f"/nursing/mar/{hi_dose['id']}/administer", {"action": 2, "reason": None, "notes": None, "verifiedByUserId": None, "batchId": None, "quantity": None})
        tc("NUR-017", MN, "High-alert drug cannot be administered without a second verifier", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
        s, d = nur.post(f"/nursing/mar/{hi_dose['id']}/administer", {"action": 2, "reason": None, "notes": None, "verifiedByUserId": nur.user["id"], "batchId": None, "quantity": None})
        tc("NUR-018", MN, "High-alert drug: nurse cannot be her/his own second verifier", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
        s, d = nur.post(f"/nursing/mar/{hi_dose['id']}/administer", {"action": 2, "reason": None, "notes": None, "verifiedByUserId": hn.user["id"], "batchId": None, "quantity": None})
        tc("NUR-019", MN, "High-alert drug administered with independent second verifier", ok(s), f"{s} {msg(d)}", "Administered", severity="Critical")
    else:
        blocked("NUR-017", MN, "High-alert double verification", "no high-alert dose found in today's MAR")

# pharmacy issue to IP
store = [x for x in admin.get("/pharmacy/stores")[1]["data"] if x["storeType"] == 1][0]["id"]
batch = next((b for b in admin.get(f"/pharmacy/stock/batches?storeId={store}&onlyInStock=true")[1]["data"] if b["batchNumber"] == "QA-B-1"), None)
if batch:
    q0 = batch["quantityOnHand"]
    s, d = ph.post("/pharmacy/issues", {"storeId": store, "issueType": 3, "departmentId": None, "patientId": pA, "admissionId": A, "indentId": None, "notes": "QA IP issue", "items": [{"batchId": batch["id"], "quantity": 2}]})
    q1 = next(b for b in admin.get(f"/pharmacy/stock/batches?storeId={store}&onlyInStock=false")[1]["data"] if b["id"] == batch["id"])["quantityOnHand"]
    tc("IPD-045", MG, "Pharmacy issues medicine to the IP patient; stock reduced by 2", ok(s) and q0 - q1 == 2, f"{s} {msg(d)} stock {q0}->{q1}", "Issued, -2", severity="Critical")
else:
    blocked("IPD-045", MG, "Pharmacy issue to IP", "no QA-B-1 stock batch available (pharmacy suite not run)")

# =========================== TRANSFERS ===========================
sp = vacant("Semi-Private")
s, d = admin.post(f"/ipd/admissions/{A}/transfer-bed", {"newBedId": gw, "reason": "same"})
tc("IPD-050", MG, "Transfer to the SAME bed blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Low")
s, d = admin.post(f"/ipd/admissions/{A}/transfer-bed", {"newBedId": pv, "reason": "occupied"})
tc("IPD-051", MG, "Transfer into an OCCUPIED bed blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = admin.post(f"/ipd/admissions/{A}/transfer-bed", {"newBedId": sp, "reason": "QA ward transfer to Semi-Private"})
a = adm(A)
tc("IPD-052", MG, "Ward transfer General -> Semi-Private: new bed Occupied, old bed released, history kept", ok(s) and bed(sp)["status"] == 2 and bed(gw)["status"] != 2 and len(a["bedAllocations"]) == 2,
   f"{s} new={bed(sp)['status']} old={bed(gw)['status']} allocations={len(a['bedAllocations'])}", "Occupied / released / 2 allocations", severity="Critical")
tc("IPD-053", MG, "Released bed goes to Cleaning (or Vacant) - not left Occupied", bed(gw)["status"] in (1, 4), f"old bed status={bed(gw)['status']}", "Cleaning(4) or Vacant(1)", severity="High")
s, d = nur.post(f"/nursing/admissions/{A}/transfer-notes", {"bedAllocationId": a["bedAllocations"][0]["id"], "handoverNotes": "QA handover", "receivedByUserId": hn.user["id"]})
tc("NUR-020", MN, "Nursing transfer (handover) note", ok(s), f"{s} {msg(d)}", "Saved", severity="Medium")
icu2 = next((b["bedId"] for b in board() if b["wardName"] == "ICU" and b["status"] == 1), None)
if icu2:
    s, d = admin.post(f"/ipd/admissions/{A}/transfer-bed", {"newBedId": icu2, "reason": "QA deterioration - ICU transfer"})
    tc("IPD-054", MG, "ICU transfer", ok(s) and bed(icu2)["status"] == 2, f"{s} {msg(d)}", "Moved to ICU", severity="High")
else:
    blocked("IPD-054", MG, "ICU transfer", "no vacant ICU bed")
s, d = admin.post(f"/ipd/admissions/{A}/transfer-consultant", {"newPrimaryConsultantId": DOC2, "notes": "QA"})
tc("IPD-055", MG, "Consultant transfer", ok(s) and adm(A)["primaryConsultantId"] == DOC2, f"{s} {msg(d)}", "Primary consultant changed", severity="Medium")
admin.post(f"/ipd/admissions/{A}/transfer-consultant", {"newPrimaryConsultantId": DOC, "notes": "QA back"})

# =========================== IP BILLING ===========================
s, d = bil.post(f"/billing/bills/generate-ip/{A}")
tc("IPD-060", MG, "Generate IP bill", ok(s), f"{s} {msg(d)}", "IP bill", severity="Critical")
ib = d["data"] if ok(s) else {}
print("IP BILL RAW:", json.dumps(ib)[:900])
bedlines = [i for i in ib.get("items", []) if i["sourceType"] == 5]
tc("IPD-061", MG, "IP bill carries bed charges for every bed occupied (General, Semi-Private, ICU)", len(bedlines) >= 2, f"{len(bedlines)} bed-charge lines: {[(i['description'], i['quantity'], i['rate']) for i in bedlines]}", ">= 1 line per ward stayed", severity="High")
tc("IPD-062", MG, "Bed charge rate > 0 (room-type tariff configured)", all(D(str(i["rate"])) > 0 for i in bedlines) and bedlines, f"{[(i['description'], i['rate']) for i in bedlines]}", "Non-zero rates", severity="High", bug="Configuration gap: General Ward and Semi-Private room types bill 0.00/day in the Speed Multi branch, so those IP stays carry no bed charge.")
issued = [i for i in ib.get("items", []) if "Actrapid" in i["description"] or i["sourceType"] not in (1, 2, 3, 4, 5, 6)]
tc("IPD-063", MG, "Medicines issued by pharmacy to the IP patient appear on the IP bill", len(issued) >= 1, f"items={[(i['sourceType'], i['description']) for i in ib.get('items', [])]}", "Issued medicine billed", severity="High",
   bug="Pharmacy IP issues are not posted to the IP bill; medicines given to inpatients are not charged.")
s, d = bil.post(f"/billing/bills/generate-ip/{A}")
tc("IPD-064", MG, "Generating a second IP bill for the same admission is prevented", rej(s) or (ok(s) and idof(d) == ib.get("id")), f"{s} {msg(d)}", "Rejected or same bill returned", severity="High")

# =========================== ADMISSION CANCELLATION ===========================
pD = newpt("D")
dbed = vacant("General Ward")
s, d = admit(pD, dbed); Dd = idof(d)
s, d = fo.post(f"/ipd/admissions/{Dd}/cancel", {"reason": ""})
tc("IPD-070", MG, "Cancel admission without reason rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = fo.post(f"/ipd/admissions/{Dd}/cancel", {"reason": "QA admitted in error"})
tc("IPD-071", MG, "Cancel admission -> status Cancelled and bed released", ok(s) and adm(Dd)["status"] == 3 and bed(dbed)["status"] != 2, f"{s} status={adm(Dd)['status']} bed={bed(dbed)['status']}", "Cancelled / bed free", severity="Critical")
s, d = doc.post(f"/ipd/admissions/{Dd}/progress-notes", {"doctorId": DOC, "notes": "after cancel"})
tc("IPD-072", MG, "Clinical documentation on a CANCELLED admission blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
if C:
    bil.post(f"/billing/bills/generate-ip/{C}")
    s, d = admin.post(f"/ipd/admissions/{C}/cancel", {"reason": "QA cancel with an IP bill already raised"})
    tc("IPD-073", MG, "Cancelling an admission that already has an IP bill is controlled (bill must be cancelled/settled first)", rej(s), f"{s} {msg(d)}", "Rejected or bill auto-cancelled", severity="High")

# =========================== DISCHARGE ===========================
s, d = admin.post(f"/ipd/admissions/{A}/discharge/finalize")
tc("DIS-001", MD, "Final discharge without a discharge request blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = doc.post(f"/ipd/admissions/{A}/discharge/request", {"requestedByDoctorId": DOC, "dischargeType": 1, "dischargeTypeNotes": None})
tc("DIS-002", MD, "Doctor recommends discharge (Normal)", ok(s), f"{s} {msg(d)}", "Discharge requested", severity="High")
cl = admin.get(f"/ipd/admissions/{A}/discharge/clearances")[1]["data"]
tc("DIS-003", MD, "4-way clearance checklist created (Billing/Pharmacy/Nursing/Final investigation)", isinstance(cl, list) and len(cl) == 4, f"{len(cl) if isinstance(cl, list) else cl}", "4 items", severity="High")
s, d = admin.post(f"/ipd/admissions/{A}/discharge/finalize")
tc("DIS-004", MD, "Final discharge with clearances pending blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
_ipbill = bil.get(f"/billing/bills?PatientId={pA}&Page=1&PageSize=5")[1]["data"]["items"][0]
bil.post(f"/billing/bills/{_ipbill['id']}/items", {"sourceType": 3, "serviceId": 26, "packageId": None, "quantity": 10})   # nurse charges 850 x 10
ipb = bil.get(f"/billing/bills?PatientId={pA}&Page=1&PageSize=5")[1]["data"]["items"]
bal = D(str(ipb[0]["balanceAmount"])) if ipb else None
s, d = hn.post(f"/ipd/admissions/{A}/discharge/clearances/1", {"remarks": "QA billing clear while balance outstanding"})
tc("DIS-005", MD, "Billing clearance is refused while the IP bill has an unpaid balance", rej(s), f"{s} {msg(d)} balance={bal}", "Rejected until settled", severity="Critical",
   bug="Billing clearance for discharge can be given while the IP bill still has an outstanding balance; nothing ties the clearance to settlement.")
s, d = hn.post(f"/ipd/admissions/{A}/discharge/clearances/2", {"remarks": "head nurse clearing PHARMACY"})
tc("DIS-006", MD, "Pharmacy clearance can only be given by pharmacy (per-department clearance rights)", s == 403, f"{s} {msg(d)}", "403 for a nurse clearing the pharmacy item", severity="High",
   bug="Discharge clearances use one permission (IPD.DISCHARGE.CLEAR): a Head Nurse can sign off the Billing and Pharmacy clearances; billing/pharmacy roles cannot give their own clearance.")
s, d = bil.post(f"/ipd/admissions/{A}/discharge/clearances/1", {"remarks": "billing clearing billing"})
tc("DIS-025", MD, "Billing role can give the Billing clearance", ok(s), f"{s} {msg(d)}", "Billing clears its own item", severity="High")
for ct, who in ((2, hn), (3, hn), (4, hn), (1, hn)):
    who.post(f"/ipd/admissions/{A}/discharge/clearances/{ct}", {"remarks": "QA clear"})
cl = admin.get(f"/ipd/admissions/{A}/discharge/clearances")[1]["data"]
print("CLEARANCES:", json.dumps(cl)[:400])
s, d = admin.post(f"/ipd/admissions/{A}/discharge/finalize")
tc("DIS-007", MD, "Final discharge without an approved discharge summary blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
summ = {"finalDiagnosis": "QA Community acquired pneumonia", "courseInHospital": "Treated with IV antibiotics (QA)", "medicationsOnDischarge": "Tab Amoxicillin 500 mg TDS x 5 days", "followUpAdvice": "Review in OPD after 7 days", "conditionAtDischarge": "Stable"}
s, d = doc.put(f"/ipd/admissions/{A}/discharge/summary", summ)
tc("DIS-008", MD, "Doctor writes discharge summary (diagnosis, treatment, medications, follow-up)", ok(s), f"{s} {msg(d)}", "Saved", severity="Critical")
s, d = doc.put(f"/ipd/admissions/{A}/discharge/summary", dict(summ, finalDiagnosis=""))
tc("DIS-009", MD, "Discharge summary without final diagnosis rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
doc.put(f"/ipd/admissions/{A}/discharge/summary", summ)
dp = doc.get("/doctor-portal/discharge-approvals")[1]["data"]
tc("DIS-012", MD, "Pending summary appears in treating doctor's Doctor-Portal e-sign inbox", adm(A)["admissionNumber"] in json.dumps(dp) or ('"admissionId": %d' % A) in json.dumps(dp), f"{json.dumps(dp)[:150]}", "Listed", severity="Medium")
s, d = doc2.post(f"/ipd/admissions/{A}/discharge/summary/approve", {"approvedByDoctorId": DOC2})
tc("DIS-010", MD, "A doctor who is not the treating consultant cannot approve the discharge summary", rej(s), f"{s} {msg(d)}", "Rejected", severity="High", bug="Discharge summary can be approved by any doctor, not only the treating consultant.")
s, d = doc2.post(f"/ipd/admissions/{A}/discharge/summary/approve", {"approvedByDoctorId": DOC})
tc("DIS-011", MD, "Doctor B cannot approve the summary in Doctor A's name", rej(s), f"{s} {msg(d)}", "Rejected (approver = caller)", severity="Critical",
   bug="Discharge-summary approval takes approvedByDoctorId from the request; another doctor can e-sign on the treating consultant's behalf.")
s, d = doc.post(f"/doctor-portal/discharge-approvals/{A}/approve")
if rej(s):
    s, d = doc.post(f"/ipd/admissions/{A}/discharge/summary/approve", {"approvedByDoctorId": DOC})
tc("DIS-013", MD, "Treating doctor approves (e-signs) discharge summary", ok(s), f"{s} {msg(d)}", "Approved", severity="Critical")
s, d = doc.put(f"/ipd/admissions/{A}/discharge/summary", dict(summ, finalDiagnosis="Changed after approval"))
tc("DIS-014", MD, "Approved discharge summary cannot be edited", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
b4 = admin.get(f"/ipd/admissions/{A}")[1]["data"]
bed_before = b4["currentBedId"]
s, d = admin.post(f"/ipd/admissions/{A}/discharge/finalize")
a = adm(A)
tc("DIS-015", MD, "Final discharge completes: status Discharged with time stamp", ok(s) and a["status"] == 2 and a["dischargedAt"], f"{s} {msg(d)} status={a['status']}", "Discharged", severity="Critical")
tc("DIS-016", MD, "Bed released on discharge", bed(bed_before)["status"] in (1, 4), f"bed status={bed(bed_before)['status']}", "Cleaning/Vacant", severity="Critical")
s, d = admin.get(f"/ipd/admissions/{A}/discharge/summary-pdf")
tc("DIS-017", MD, "Discharge summary PDF", s == 200 and d.get("binary") and d["head"] == b"%PDF-", f"{s}", "PDF", severity="High")
s, d = admin.get(f"/ipd/admissions/{A}/discharge/slip")
tc("DIS-018", MD, "Discharge slip PDF", s == 200 and d.get("binary") and d["head"] == b"%PDF-", f"{s}", "PDF", severity="Medium")
sm = admin.get(f"/ipd/admissions/{A}/discharge/summary")[1]["data"]
tc("DIS-019", MD, "Summary holds diagnosis, treatment, medications and follow-up as entered", sm.get("finalDiagnosis") == summ["finalDiagnosis"] and sm.get("medicationsOnDischarge") == summ["medicationsOnDischarge"] and sm.get("followUpAdvice") == summ["followUpAdvice"],
   json.dumps(sm)[:200], "All fields present", severity="High")
not_tested("DIS-020", MD, "Investigation results in discharge summary", "Summary is free text; no automatic pull of lab/radiology results into the summary exists (results must be typed).", expected="Investigation results auto-included")
s, d = admin.get(f"/emr/patients/{pA}/discharge-summaries")
tc("DIS-021", MD, "Discharge summary archived in EMR", s == 200 and len(d["data"]) >= 1, f"{s} {len(d['data']) if s == 200 else ''}", ">= 1", severity="High")
s, d = doc.post(f"/ipd/admissions/{A}/progress-notes", {"doctorId": DOC, "notes": "after discharge"})
tc("DIS-022", MD, "Clinical documentation after discharge blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = admin.post(f"/ipd/admissions/{A}/cancel", {"reason": "QA cancel after discharge"})
tc("DIS-023", MD, "Cancelling a DISCHARGED admission blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
not_tested("DIS-024", MD, "Discharge cancellation / reversal", "FEATURE NOT PRESENT: no endpoint to reverse a finalised discharge (e.g. discharge done in error).", expected="Controlled reversal with authorisation + audit")
# QA cleanup: release the Private-ward patient so reruns have beds
if B:
    fo.post(f"/ipd/admissions/{B}/cancel", {"reason": "QA cleanup end of IP suite"})
summary()
