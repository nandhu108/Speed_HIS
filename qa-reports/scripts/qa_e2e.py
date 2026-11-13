import sys, os, datetime
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from qa_lib import *
from decimal import Decimal as D

st = load_state(); DOC = st["doctors"]["qa.doctor"]; DOC2 = st["doctors"]["qa.doctor2"]; GM = st["depts"]["DEP001"]; SURG = st["depts"]["DEP010"]
S = lambda u: Sess(u, "Qa@12345", 2)
admin = Sess("admin", "Admin@123", 2); rec = S("qa.reception"); fo = S("qa.frontoffice"); doc = S("qa.doctor"); lab = S("qa.lab"); patho = S("qa.patho")
radr = S("qa.rad"); tech = S("qa.radtech"); ph = S("qa.pharm"); bil = S("qa.billing"); sup = S("qa.billsup"); nur = S("qa.nurse"); hn = S("qa.headnurse"); ot = S("qa.ot")
RUN = datetime.datetime.now().strftime("%H%M%S"); UTC = datetime.timezone.utc; TODAY = datetime.date.today()
idof = lambda d: d["data"] if not isinstance(d["data"], dict) else d["data"]["id"]
store = [x for x in admin.get("/pharmacy/stores")[1]["data"] if x["storeType"] == 1][0]["id"]


def step(tid, mod, name, s, d, cond=None, sev="High", exp="Step succeeds"):
    good = ok(s) if cond is None else cond
    tc(tid, mod, name, good, f"{s} {msg(d)}", exp, severity=sev)
    return good


# ================= APPOINTMENTS (supporting module) =================
MA = "J-Appointments"
blocks = [{"dayOfWeek": dw, "startTime": "08:00:00", "endTime": "20:00:00", "slotMinutes": 15} for dw in range(0, 7)]
s, d = admin.put(f"/doctors/{DOC}/schedule/blocks", {"blocks": blocks})
step("APT-001", MA, "Configure doctor weekly schedule (setup)", s, d)
D1 = (TODAY + datetime.timedelta(days=1)).isoformat()
s, d = admin.get(f"/Appointments/slots?doctorId={DOC}&date={D1}")
slots = d["data"] if s == 200 else []
free = [x for x in slots if x.get("isAvailable", True)]
step("APT-002", MA, "Slot engine returns 15-minute slots for tomorrow", s, d, cond=s == 200 and len(slots) >= 40, exp=">= 48 slots (08:00-20:00)")
s, d = rec.post("/Patients", patient_body("QATEST", f"Op{RUN}", 2, "1988-08-08", "90000008" + RUN[-2:])); pOP = d["data"]["id"]
slot = free[3]["startTime"] if free else "09:00:00"
book = lambda pid, t, dt=D1: rec.post("/Appointments", {"patientId": pid, "doctorId": DOC, "appointmentDate": dt, "startTime": t, "bookingType": 1, "notes": None, "appointmentTypeId": None, "consultationMode": 1, "teleconsultLink": None})
s, d = book(pOP, slot); ap = idof(d) if ok(s) else None
step("APT-003", MA, "Book appointment in a free slot", s, d, sev="Critical")
s, d = rec.post("/Patients", patient_body("QATEST", f"Op2{RUN}", 1, "1987-07-07", "90000009" + RUN[-2:])); pOP2 = d["data"]["id"]
s, d = book(pOP2, slot)
step("APT-004", MA, "Double-booking the same doctor slot is blocked", s, d, cond=rej(s), sev="Critical", exp="Rejected")
s, d = book(pOP2, slot, (TODAY - datetime.timedelta(days=2)).isoformat())
step("APT-005", MA, "Booking an appointment in the past is blocked", s, d, cond=rej(s), sev="Medium", exp="Rejected")
s, d = book(pOP2, "03:00:00")
step("APT-006", MA, "Booking outside the doctor's schedule (03:00) is blocked", s, d, cond=rej(s), sev="Medium", exp="Rejected")
s, d = book(pOP2, free[5]["startTime"] if len(free) > 5 else "10:00:00"); ap2 = idof(d) if ok(s) else None
s, d = rec.post(f"/Appointments/{ap2}/reschedule", {"newDate": D1, "newStartTime": slot, "reason": "QA"})
step("APT-007", MA, "Reschedule into an already-taken slot blocked", s, d, cond=rej(s), sev="High", exp="Rejected")
s, d = rec.post(f"/Appointments/{ap2}/reschedule", {"newDate": D1, "newStartTime": free[8]["startTime"] if len(free) > 8 else "11:00:00", "reason": "QA patient request"})
step("APT-008", MA, "Reschedule into a free slot", s, d)
print("RESCHEDULE RAW:", json.dumps(d)[:300])
if ok(s) and d.get("data"):
    ap2 = idof(d)
s, d = rec.post(f"/Appointments/{ap2}/cancel", {"reason": ""})
step("APT-009", MA, "Cancel appointment without reason blocked", s, d, cond=rej(s), sev="Medium", exp="Rejected")
s, d = rec.post(f"/Appointments/{ap2}/cancel", {"reason": "QA patient cancelled"})
step("APT-010", MA, "Cancel appointment with reason", s, d)
s, d = rec.post(f"/Appointments/{ap2}/check-in")
step("APT-011", MA, "Checking in a CANCELLED appointment blocked", s, d, cond=rej(s), sev="High", exp="Rejected")
s, d = book(pOP2, free[5]["startTime"] if len(free) > 5 else "10:00:00")
step("APT-012", MA, "Slot released by a cancelled/rescheduled appointment is re-bookable", s, d, sev="Medium")
s, d = rec.post(f"/Appointments/{ap}/check-in")
step("APT-013", MA, "Check-in BEFORE the appointment date (tomorrow) is blocked", s, d, cond=rej(s), sev="Medium", exp="Rejected (check-in only on the day)")

# ================= SCENARIO 1: OP journey (same-day appointment) =================
M1 = "E2E-1 OP journey"
now = datetime.datetime.now()
today_free = [x for x in admin.get(f"/Appointments/slots?doctorId={DOC}&date={TODAY.isoformat()}")[1]["data"] if x.get("isAvailable", True) and x["startTime"] > now.strftime("%H:%M:%S")]
if not today_free:
    blocked("E2E1-01", M1, "OP journey", "no free slot left today for the QA doctor (run later in the day)")
else:
    s, d = book(pOP, today_free[0]["startTime"], TODAY.isoformat()); apt = idof(d) if ok(s) else None
    step("E2E1-01", M1, "Book same-day appointment", s, d, sev="Critical")
    s, d = rec.post(f"/Appointments/{apt}/check-in"); step("E2E1-02", M1, "Check-in -> token issued", s, d, sev="Critical")
    s, d = rec.post("/opd/visits", {"patientId": pOP, "doctorId": DOC, "departmentId": GM, "appointmentId": apt}); v = d["data"] if ok(s) else {}
    tok = admin.get(f"/Appointments/{apt}")[1]["data"].get("tokenNumber")
    step("E2E1-03", M1, "OP visit from the checked-in appointment keeps the same token", s, d, cond=ok(s) and v.get("tokenNumber") == tok, sev="High", exp=f"token {tok}")
    vid = v.get("id")
    doc.post(f"/opd/visits/{vid}/start-consult")
    doc.put(f"/opd/visits/{vid}/vitals", {"heightCm": 160, "weightKg": 60, "temperatureF": 100.4, "pulseRate": 96, "respiratoryRate": 20, "bloodPressureSystolic": 118, "bloodPressureDiastolic": 76, "spO2": 97, "randomBloodSugar": 110})
    icd = admin.get("/icd-codes?Page=1&PageSize=1")[1]["data"]["items"][0]
    s, d = doc.put(f"/opd/visits/{vid}/diagnoses", {"diagnoses": [{"icdCodeId": icd["id"], "type": 1, "notes": "QA"}]}); step("E2E1-04", M1, "Doctor records diagnosis", s, d)
    s, d = doc.post("/lab/orders", {"patientId": pOP, "opVisitId": vid, "emergencyVisitId": None, "clinicalOrderId": None, "orderedByDoctorId": DOC, "priority": 1, "clinicalNotes": None, "items": [{"labTestId": 7, "testProfileId": None}]})
    lo = idof(d) if ok(s) else None; step("E2E1-05", M1, "Lab order linked to the OP visit", s, d)
    _rs, _rd = doc.post("/rad/orders", {"patientId": pOP, "opVisitId": vid, "admissionId": None, "emergencyVisitId": None, "clinicalOrderId": None, "orderedByDoctorId": DOC, "priority": 1, "clinicalNotes": None, "radiologyTestIds": [14]})
    ro = idof(_rd) if ok(_rs) else None
    s, d = bil.post("/billing/bills/generate-op", {"opVisitId": vid}); b = d["data"] if ok(s) else {}
    step("E2E1-11", M1, "OP bill generated with consultation + pending lab and radiology orders", s, d, sev="Critical")
    lines = [(i["sourceType"], i["description"], i["rate"]) for i in b.get("items", [])]
    tc("E2E1-15", M1, "Lab test and scan done for this visit appear on the OP bill", any(l[0] == 8 for l in lines) and any(l[0] == 9 for l in lines),
       f"bill lines={lines}", "Lab + radiology lines on the bill", severity="Critical")
    priced_tests = [l for l in lines if l[0] in (8, 9) and D(str(l[2])) > 0]
    tc("E2E1-12", M1, "Lab and radiology lines are priced from the tariff", len(priced_tests) >= 2, f"bill lines={lines}",
       "APTT and CT Brain billed at tariff", "Complete the OP journey -> generate OP bill", severity="High",
       bug="DATA: the hospital has not yet mapped lab/radiology tests to priced billing services; lines show 0.00 '(price not configured)' until the price list is entered.")
    s, d = bil.post(f"/billing/bills/{b['id']}/payments", {"paymentModeId": 1, "amount": b["balanceAmount"], "referenceNumber": None}) if b.get("balanceAmount") else (200, {})
    bb = bil.get(f"/billing/bills/{b['id']}")[1]["data"]
    step("E2E1-13", M1, "OP bill settled in full before diagnostics", 200, {}, cond=bb["status"] == 3, sev="Critical", exp="Paid")
    lod = admin.get(f"/lab/orders/{lo}")[1]["data"]; it = lod["items"][0]
    sm = lab.post("/lab/collection/samples", {"labOrderId": lo, "sampleTypeId": admin.get("/lab/tests/7")[1]["data"]["sampleTypeId"], "labOrderItemIds": [it["id"]]})[1]["data"]
    lab.post(f"/lab/collection/samples/{sm['id'] if isinstance(sm, dict) else sm}/receive")
    pid_param = admin.get("/lab/tests/7")[1]["data"]["parameters"][0]["id"]
    lab.post(f"/lab/order-items/{it['id']}/result", {"values": [{"labTestParameterId": pid_param, "value": "29"}], "notes": None})
    patho.post(f"/lab/order-items/{it['id']}/result/verify"); s, d = patho.post(f"/lab/order-items/{it['id']}/result/approve")
    step("E2E1-06", M1, "Lab result collected -> received -> entered -> verified -> approved", s, d, sev="Critical")
    s, d = doc.get(f"/opd/visits/{vid}/workspace")
    step("E2E1-07", M1, "Doctor sees the approved lab result back in the patient's record", s, d, cond="APTT" in json.dumps(doc.get(f"/emr/patients/{pOP}/investigations")[1]["data"]) or "Activated" in json.dumps(doc.get(f"/emr/patients/{pOP}/investigations")[1]["data"]), sev="High")
    rit = admin.get(f"/rad/orders/{ro}")[1]["data"]["items"][0]
    tech.post(f"/rad/scheduling/order-items/{rit['id']}", {"scheduledAt": (datetime.datetime.now(UTC) + datetime.timedelta(hours=1)).isoformat(), "notes": None})
    tech.post(f"/rad/capture/order-items/{rit['id']}")
    radr.post(f"/rad/order-items/{rit['id']}/report", {"templateId": None, "findings": "Normal (QA)", "impression": "Normal (QA)"})
    radr.post(f"/rad/order-items/{rit['id']}/report/verify"); s, d = radr.post(f"/rad/order-items/{rit['id']}/report/approve")
    step("E2E1-08", M1, "Radiology (CT Brain plain) scheduled -> captured -> reported -> approved", s, d, sev="Critical")
    drug = admin.get("/Drugs?Page=1&PageSize=20")[1]["data"]["items"][1]
    s, d = doc.put(f"/opd/visits/{vid}/prescription", {"notes": None, "items": [{"drugId": drug["id"], "dose": "1 unit", "frequency": "OD", "duration": "3 days", "route": "SC", "instructions": None}]})
    step("E2E1-09", M1, "Doctor prescribes", s, d)
    doc.post(f"/opd/visits/{vid}/complete")
    rxid = admin.get(f"/opd/visits/{vid}/workspace")[1]["data"]["prescription"]["id"]
    s, d = ph.post("/pharmacy/sales", {"storeId": store, "patientId": pOP, "prescriptionId": rxid, "paymentModeId": 1, "items": [{"drugId": drug["id"], "quantity": 3, "discountPercent": 0}]})
    step("E2E1-10", M1, "Pharmacy dispenses against the prescription", s, d, sev="Critical")
    tl = json.dumps(doc.get(f"/emr/patients/{pOP}/timeline")[1]["data"])
    tc("E2E1-14", M1, "EMR timeline shows the visit, prescription and lab result", "OPD Visit" in tl and "Prescription" in tl and ("Lab" in tl or "APTT" in tl or "Activated" in tl), tl[:200], "All three present", severity="High")

# ================= SCENARIO 2: IP journey with settlement =================
M2 = "E2E-2 IP journey"
s, d = rec.post("/Patients", patient_body("QATEST", f"Ipj{RUN}", 1, "1960-01-01", "90000010" + RUN[-2:])); pIP = d["data"]["id"]
v = rec.post("/opd/visits", {"patientId": pIP, "doctorId": DOC, "departmentId": GM, "appointmentId": None})[1]["data"]
doc.post(f"/opd/visits/{v['id']}/start-consult")
bedid = next(b["bedId"] for b in admin.get("/ipd/bed-board")[1]["data"] if b["wardName"] == "Private Ward" and b["status"] == 1)
s, d = fo.post("/ipd/admissions", {"patientId": pIP, "primaryConsultantId": DOC, "departmentId": GM, "bedId": bedid, "admissionType": 3, "provisionalDiagnosis": "QA", "referringOpVisitId": v["id"],
                                   "referringEmergencyVisitId": None, "secondaryConsultantIds": [], "consentTemplateId": None, "consentSignedByName": None, "consentRelationshipToPatient": None,
                                   "initialDepositAmount": 3000, "depositPaymentModeId": 1, "depositReferenceNumber": None})
A = idof(d) if ok(s) else None
step("E2E2-01", M2, "OP consultation converted to IP admission (Private ward, deposit 3000)", s, d, sev="Critical")
a = admin.get(f"/ipd/admissions/{A}")[1]["data"]
step("E2E2-02", M2, "Admission links back to the referring OP visit", 200, {}, cond=a["referringOpVisitId"] == v["id"], sev="Medium", exp="referringOpVisitId set")
doc.post(f"/ipd/admissions/{A}/orders", {"orderedByDoctorId": DOC, "orderType": 4, "description": "Soft diet", "priority": 1, "notes": None, "medication": None, "diet": {"dietType": "Soft", "instructions": None}})
nur.post(f"/nursing/admissions/{A}/notes", {"shift": 2, "notes": "QA evening note"})
s, d = bil.post(f"/billing/bills/generate-ip/{A}"); ib = d["data"] if ok(s) else {}
step("E2E2-03", M2, "IP bill generated (bed charges)", s, d, sev="Critical")
bil.post(f"/billing/bills/{ib['id']}/items", {"sourceType": 3, "serviceId": 22, "packageId": None, "quantity": 1})   # IP registration 500
ib = bil.get(f"/billing/bills/{ib['id']}")[1]["data"]
adv = admin.get(f"/billing/advances/patient/{pIP}")[1]["data"]; advl = adv if isinstance(adv, list) else adv.get("items", [])
if advl and D(str(ib["balanceAmount"])) > 0:
    amt = min(D(str(ib["balanceAmount"])), D(str(advl[0].get("availableAmount", advl[0].get("amount")))))
    s, d = bil.post(f"/billing/bills/{ib['id']}/advances/draw", {"advanceId": advl[0]["id"], "amount": float(amt)})
    step("E2E2-04", M2, "Deposit (advance) drawn against the IP bill", s, d, sev="Critical")
ib = bil.get(f"/billing/bills/{ib['id']}")[1]["data"]
if D(str(ib["balanceAmount"])) > 0:
    bil.post(f"/billing/bills/{ib['id']}/payments", {"paymentModeId": 1, "amount": ib["balanceAmount"], "referenceNumber": None})
ib = bil.get(f"/billing/bills/{ib['id']}")[1]["data"]
step("E2E2-05", M2, "IP bill fully settled (balance 0)", 200, {}, cond=D(str(ib["balanceAmount"])) == 0, sev="Critical", exp=f"balance 0 (was {ib['balanceAmount']})")
doc.post(f"/ipd/admissions/{A}/discharge/request", {"requestedByDoctorId": DOC, "dischargeType": 1, "dischargeTypeNotes": None})
for ct, who in ((1, bil), (2, ph), (3, hn), (4, doc)):
    who.post(f"/ipd/admissions/{A}/discharge/clearances/{ct}", {"remarks": "QA"})
doc.put(f"/ipd/admissions/{A}/discharge/summary", {"finalDiagnosis": "QA dx", "courseInHospital": "QA", "medicationsOnDischarge": "QA", "followUpAdvice": "QA", "conditionAtDischarge": "Stable"})
doc.post(f"/doctor-portal/discharge-approvals/{A}/approve")
s, d = admin.post(f"/ipd/admissions/{A}/discharge/finalize")
bstat = next(b["status"] for b in admin.get("/ipd/bed-board")[1]["data"] if b["bedId"] == bedid)
step("E2E2-06", M2, "Discharge completes and bed is released", s, d, cond=ok(s) and bstat in (1, 4), sev="Critical")
s, d = admin.get(f"/emr/patients/{pIP}/discharge-summaries")
step("E2E2-07", M2, "Discharge summary visible in EMR", s, d, cond=s == 200 and len(d["data"]) >= 1, sev="High")

# ================= SCENARIO 3: Surgical journey =================
M3 = "E2E-3 Surgical journey"
s, d = rec.post("/Patients", patient_body("QATEST", f"Surg{RUN}", 1, "1975-05-05", "90000011" + RUN[-2:])); pS = d["data"]["id"]
bedid = next(b["bedId"] for b in admin.get("/ipd/bed-board")[1]["data"] if b["wardName"] == "General Ward" and b["status"] == 1)
s, d = fo.post("/ipd/admissions", {"patientId": pS, "primaryConsultantId": DOC, "departmentId": SURG, "bedId": bedid, "admissionType": 1, "provisionalDiagnosis": "QA appendicitis", "referringOpVisitId": None,
                                   "referringEmergencyVisitId": None, "secondaryConsultantIds": [], "consentTemplateId": 2, "consentSignedByName": "QATEST Self", "consentRelationshipToPatient": "Self",
                                   "initialDepositAmount": 0, "depositPaymentModeId": None, "depositReferenceNumber": None})
AS = idof(d); step("E2E3-01", M3, "Surgical admission (General Surgery) with surgical consent", s, d, sev="Critical")
cons = admin.get(f"/ipd/admissions/{AS}")[1]["data"].get("patientConsentId") or (admin.get(f"/Patients/{pS}/consents")[1]["data"] or [{}])[0].get("id")
th = admin.get("/ot/theatres")[1]["data"][0]["id"]
t0 = (datetime.datetime.now(UTC) + datetime.timedelta(days=400 + int(RUN) % 200)).replace(hour=3, minute=0, second=0, microsecond=0)
s, d = ot.post("/ot/bookings", {"admissionId": AS, "theatreId": th, "procedureName": "Lap appendicectomy (QA)", "surgeonId": DOC, "anaesthetistId": DOC2, "scheduledStart": t0.isoformat(), "scheduledEnd": (t0 + datetime.timedelta(hours=2)).isoformat(), "notes": None})
BK = idof(d) if ok(s) else None; step("E2E3-02", M3, "OT booked", s, d, sev="Critical")
ot.put(f"/ot/bookings/{BK}/checklist/sign-in", {"patientIdentityConfirmed": True, "siteMarked": True, "anaesthesiaSafetyCheckComplete": True, "pulseOximeterFunctioning": True, "knownAllergy": False, "difficultAirwayRisk": False, "riskOfBloodLoss": False, "complete": True})
ot.post(f"/ot/bookings/{BK}/verify-consent", {"patientConsentId": cons})
doc.put(f"/ot/bookings/{BK}/pre-anaesthesia", {"assessedByDoctorId": DOC2, "asaGrade": "I", "airwayAssessment": "MP1", "comorbidities": "None", "investigations": "Normal", "anaesthesiaPlan": "GA", "notes": None})
s, d = doc.post(f"/ot/bookings/{BK}/start"); step("E2E3-03", M3, "Pre-op checklist + consent verified -> surgery started", s, d, sev="Critical")
ot.put(f"/ot/bookings/{BK}/checklist/time-out", {"teamIntroduced": True, "patientSiteProcedureConfirmed": True, "criticalEventsReviewed": True, "antibioticProphylaxisGiven": True, "imagingDisplayed": True, "complete": True})
ot.put(f"/ot/bookings/{BK}/checklist/sign-out", {"procedureNameRecorded": True, "instrumentSpongeNeedleCountCorrect": True, "specimenLabeled": True, "equipmentProblemsAddressed": True, "recoveryConcernsReviewed": True, "complete": True})
doc.put(f"/ot/bookings/{BK}/operative-note", {"surgeonId": DOC, "procedurePerformed": "Lap appendicectomy", "findings": "Inflamed appendix", "complications": "None", "startedAt": datetime.datetime.now(UTC).isoformat(), "endedAt": datetime.datetime.now(UTC).isoformat(), "notes": None})
s, d = doc.post(f"/ot/bookings/{BK}/complete"); step("E2E3-04", M3, "Surgery completed with operative note", s, d, sev="Critical")
s, d = doc.put(f"/ot/bookings/{BK}/post-op-note", {"recoveryNotes": "Stable", "vitalsSummary": "Normal", "complications": "None", "shiftedAt": datetime.datetime.now(UTC).isoformat(), "notes": None})
step("E2E3-05", M3, "Post-operative documentation", s, d)
s, d = bil.post(f"/billing/bills/generate-ip/{AS}"); sb = d["data"] if ok(s) else {}
bil.post(f"/billing/bills/{sb['id']}/items", {"sourceType": 3, "serviceId": 29, "packageId": None, "quantity": 2})   # OT 2 hours
sb = bil.get(f"/billing/bills/{sb['id']}")[1]["data"]
tc("E2E3-06", M3, "OT charges reach the IP bill automatically when surgery is completed", any(i["sourceType"] == 6 for i in sb["items"]) or any("OT" in i["description"] and i["sourceType"] != 3 for i in sb["items"]),
   f"lines={[(i['sourceType'], i['description'], i['rate']) for i in sb['items']]}", "OT/theatre charge line created by the OT module", "Complete surgery -> generate IP bill", severity="High",
   bug="Completing a surgery posts no theatre/surgeon/anaesthesia charge; OT time must be keyed manually as a service line (only implant/consumable usage auto-posts).")
bil.post(f"/billing/bills/{sb['id']}/payments", {"paymentModeId": 1, "amount": sb["balanceAmount"], "referenceNumber": None})
doc.post(f"/ipd/admissions/{AS}/discharge/request", {"requestedByDoctorId": DOC, "dischargeType": 1, "dischargeTypeNotes": None})
for ct, who in ((1, bil), (2, ph), (3, hn), (4, doc)):
    who.post(f"/ipd/admissions/{AS}/discharge/clearances/{ct}", {"remarks": "QA"})
doc.put(f"/ipd/admissions/{AS}/discharge/summary", {"finalDiagnosis": "Acute appendicitis", "courseInHospital": "Lap appendicectomy", "medicationsOnDischarge": "Analgesics", "followUpAdvice": "Suture removal day 7", "conditionAtDischarge": "Stable"})
doc.post(f"/doctor-portal/discharge-approvals/{AS}/approve")
s, d = admin.post(f"/ipd/admissions/{AS}/discharge/finalize"); step("E2E3-07", M3, "Surgical patient billed, settled and discharged", s, d, sev="Critical")

# ================= SCENARIO 4: Cancellation journey =================
M4 = "E2E-4 Cancellation journey"
s, d = rec.post("/Patients", patient_body("QATEST", f"Can{RUN}", 2, "1992-02-02", "90000012" + RUN[-2:])); pC = d["data"]["id"]
v = rec.post("/opd/visits", {"patientId": pC, "doctorId": DOC, "departmentId": GM, "appointmentId": None})[1]["data"]
lo = idof(doc.post("/lab/orders", {"patientId": pC, "opVisitId": v["id"], "emergencyVisitId": None, "clinicalOrderId": None, "orderedByDoctorId": DOC, "priority": 1, "clinicalNotes": None, "items": [{"labTestId": 7, "testProfileId": None}]})[1])
b = bil.post("/billing/bills/generate-op", {"opVisitId": v["id"]})[1]["data"]
bil.post(f"/billing/bills/{b['id']}/items", {"sourceType": 3, "serviceId": 6, "packageId": None, "quantity": 1})
b = bil.get(f"/billing/bills/{b['id']}")[1]["data"]
day0 = bil.get(f"/billing/day-close/collection?date={TODAY}")[1]["data"]["total"]
bil.post(f"/billing/bills/{b['id']}/payments", {"paymentModeId": 1, "amount": b["balanceAmount"], "referenceNumber": None})
b = bil.get(f"/billing/bills/{b['id']}")[1]["data"]; pay = b["payments"][0]
step("E2E4-01", M4, "Patient registered, consulted, lab ordered, billed and paid", 200, {}, cond=b["status"] == 3, sev="Critical", exp="Paid")
s, d = bil.post(f"/billing/bills/{b['id']}/cancel", {"reason": "QA patient left"})
step("E2E4-02", M4, "Paid bill cannot be cancelled before refund", s, d, cond=rej(s), sev="Critical", exp="Rejected")
s, d = bil.post(f"/billing/bills/{b['id']}/refunds", {"billPaymentId": pay["id"], "amount": pay["amount"], "reason": "QA full refund - patient left", "method": 1})
r = idof(d); sup.post(f"/billing/refunds/{r}/decide", {"approve": True, "rejectionReason": None}); s, d = sup.post(f"/billing/refunds/{r}/issue")
step("E2E4-03", M4, "Full refund requested, approved by supervisor and issued", s, d, sev="Critical")
s, d = bil.post(f"/billing/bills/{b['id']}/cancel", {"reason": "QA patient left - refunded"})
step("E2E4-04", M4, "Bill cancelled after full refund", s, d, sev="Critical")
s, d = admin.post(f"/lab/orders/{lo}/cancel", {"reason": "QA patient left"}); step("E2E4-05", M4, "Pending lab order cancelled", s, d, sev="High")
s, d = admin.post(f"/opd/visits/{v['id']}/cancel", {"reason": "QA patient left"}); step("E2E4-06", M4, "OP visit cancelled", s, d, sev="High")
day1 = bil.get(f"/billing/day-close/collection?date={TODAY}")[1]["data"]["total"]
tc("E2E4-07", M4, "Net day-close collection after pay-and-full-refund is unchanged", D(str(day1)) == D(str(day0)), f"before={day0} after={day1}", "Net zero", severity="High",
   bug="Day-close collection counts the payment but not the full refund, so a fully reversed transaction still inflates expected cash.")
s, d = admin.get(f"/activity-logs?Page=1&PageSize=50&Search=refund")
step("E2E4-08", M4, "Refund/cancel events present in activity log", s, d, cond=s == 200 and d["data"]["totalCount"] >= 1, sev="Medium")
s, d = admin.get("/audit-logs?Page=1&PageSize=5&EntityName=Bill")
step("E2E4-09", M4, "Row-level audit trail present for bills", s, d, cond=s == 200 and d["data"]["totalCount"] > 0, sev="High")
summary()
