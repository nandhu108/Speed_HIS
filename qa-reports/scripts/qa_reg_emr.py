import sys, os, re
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from qa_lib import *
from decimal import Decimal as D

st = load_state(); P = st["patients"]
DOC = st["doctors"]["qa.doctor"]; DOC2 = st["doctors"]["qa.doctor2"]; GM = st["depts"]["DEP001"]; SURG = st["depts"]["DEP010"]
admin = Sess("admin", "Admin@123", 2); rec = Sess("qa.reception", "Qa@12345", 2); bil = Sess("qa.billing", "Qa@12345", 2)
doc = Sess("qa.doctor", "Qa@12345", 2); doc2 = Sess("qa.doctor2", "Qa@12345", 2); nur = Sess("qa.nurse", "Qa@12345", 2)

# =============================== A. PATIENT REGISTRATION ===============================
M = "A-Patient Registration"
s, d = rec.post("/Patients", patient_body("QATEST", "Reg", 2, "1990-03-10", "9000000201"))
tc("REG-001", M, "Register a new patient (minimum mandatory data)", ok(s) and re.match(r"^PRX-\d{4}-\d{6}$", d["data"]["uhid"]), f"{s} {d['data'] if ok(s) else msg(d)}", "201 + UHID PRX-YYYY-NNNNNN", severity="Critical")
reg = d["data"] if ok(s) else None
s, d = admin.get("/Patients?Page=1&PageSize=200")
uhids = [p["uhid"] for p in d["data"]["items"]]
tc("REG-002", M, "UHIDs are unique across all patients", len(uhids) == len(set(uhids)), f"{len(uhids)} patients, {len(set(uhids))} unique", "No duplicate UHID", severity="Critical")
nums = sorted(int(u.split("-")[-1]) for u in uhids)
tc("REG-003", M, "UHID sequence is gap-free and increasing", nums == list(range(nums[0], nums[0] + len(nums))), f"{nums[:3]}...{nums[-3:]}", "Contiguous sequence", severity="Low")

base = patient_body("QATEST", "Val", 1, "1990-01-01", "9000000202")
for tid, field, val, label in (("REG-004", "firstName", "", "empty first name"), ("REG-005", "lastName", "", "empty last name"),
                               ("REG-006", "mobile", "", "empty mobile"), ("REG-007", "mobile", "12ab", "non-numeric mobile"),
                               ("REG-008", "mobile", "123", "too-short mobile"), ("REG-009", "gender", 0, "invalid gender"),
                               ("REG-010", "dateOfBirth", (datetime.date.today() + datetime.timedelta(days=30)).isoformat(), "future date of birth"),
                               ("REG-011", "email", "not-an-email", "malformed email"), ("REG-012", "firstName", "A" * 300, "300-char first name")):
    b = dict(base); b[field] = val
    s, d = rec.post("/Patients", b)
    tc(tid, M, f"Validation: {label}", s in (400, 422), f"{s} {msg(d)}", "Rejected with field error", severity="Medium")
b = dict(base); b["dateOfBirth"] = None; b["estimatedAge"] = None
s, d = rec.post("/Patients", b)
tc("REG-013", M, "Neither DOB nor estimated age supplied", s in (400, 422), f"{s} {msg(d)}", "Rejected (age basis required)", severity="Medium")

# duplicate detection
s, d = rec.post("/Patients/duplicates/check", {"firstName": "QATEST", "lastName": "Alpha", "dateOfBirth": "1971-01-15", "mobile": "9000000101", "excludePatientId": None})
tc("REG-014", M, "Duplicate check finds an existing patient (same name/DOB/mobile)", s == 200 and len(d["data"]) >= 1, f"{s} {len(d['data']) if s == 200 else msg(d)} matches", ">= 1 match returned", severity="High")
s, d = rec.post("/Patients", patient_body("QATEST", "Alpha", 1, "1971-01-15", "9000000101"))
tc("REG-015", M, "Registering an exact duplicate (same name, DOB, mobile) is blocked or clearly warned", rej(s) or ("duplicate" in json.dumps(d).lower()), f"{s} {msg(d)} data={json.dumps(d.get('data'))[:150]}",
   "Blocked, or response carries duplicate warning requiring confirmation", severity="High")
dup_created = ok(s)

# search
s, d = rec.get("/Patients?Page=1&PageSize=10&Search=QATEST%20Alpha")
tc("REG-016", M, "Search patient by name", s == 200 and any("Alpha" in p["fullName"] for p in d["data"]["items"]), f"{s} {d['data']['totalCount'] if s == 200 else ''}", "Alpha found", severity="Medium")
s, d = rec.get("/Patients?Page=1&PageSize=10&Search=PRX-2026-000002")
tc("REG-017", M, "Search patient by UHID", s == 200 and d["data"]["totalCount"] >= 1 and d["data"]["items"][0]["uhid"] == "PRX-2026-000002", f"{s}", "Exact UHID match", severity="High")
s, d = rec.get("/Patients?Page=1&PageSize=10&Search=9000000101")
tc("REG-018", M, "Search patient by mobile", s == 200 and d["data"]["totalCount"] >= 1, f"{s} {d['data']['totalCount'] if s == 200 else ''}", "Alpha found", severity="Medium")
s, d = rec.get("/Patients?Page=1&PageSize=10&Search=zzzz-no-such")
tc("REG-019", M, "Search with no match returns empty result (not error)", s == 200 and d["data"]["totalCount"] == 0, f"{s}", "Empty list", severity="Low")

# modify
if reg:
    s, d = rec.get(f"/Patients/{reg['id']}")
    full = d["data"]
    s, d = rec.get(f"/Patients/{reg['id']}")
    upd = patient_body("QATEST", "RegEdited", 2, "1990-03-10", "9000000201", address="Updated QA Address")
    upd["isActive"] = True
    s, d = rec.put(f"/Patients/{reg['id']}", upd)
    tc("REG-020", M, "Modify patient demographics", ok(s), f"{s} {msg(d)}", "Updated", severity="High")
    s, d = rec.get(f"/Patients/{reg['id']}")
    tc("REG-021", M, "Modified data persisted; UHID unchanged", s == 200 and d["data"]["lastName"] == "RegEdited" and d["data"]["uhid"] == reg["uhid"], f"last={d['data'].get('lastName')} uhid={d['data'].get('uhid')}", "New value stored, same UHID", severity="Critical")
    s, d = admin.get("/audit-logs?Page=1&PageSize=20&EntityName=Patient")
    tc("REG-022", M, "Patient edit is audited", s == 200 and d["data"]["totalCount"] >= 1, f"{s} rows={d['data']['totalCount'] if s == 200 else ''}", "Audit rows exist", severity="High")
    s, d = nur.put(f"/Patients/{reg['id']}", upd)
    tc("REG-023", M, "Nurse role cannot modify patient demographics (RBAC)", s == 403, f"{s}", "403", severity="High")
    s, d = nur.post("/Patients", patient_body("QATEST", "Nope", 1, "1990-01-01", "9000000203"))
    tc("REG-024", M, "Nurse role cannot register patients (RBAC)", s == 403, f"{s}", "403", severity="High")

    # block/unblock
    s, d = rec.post(f"/Patients/{reg['id']}/block", {"reason": ""})
    tc("REG-025", M, "Block patient without reason rejected", s in (400, 403, 422), f"{s} {msg(d)}", "Rejected", severity="Medium")
    s, d = admin.post(f"/Patients/{reg['id']}/block", {"reason": "QA block test"})
    tc("REG-026", M, "Block patient with reason", ok(s), f"{s} {msg(d)}", "Blocked", severity="High")
    s, d = rec.post("/opd/visits", {"patientId": reg["id"], "doctorId": DOC, "departmentId": GM, "appointmentId": None})
    tc("REG-027", M, "OP visit for a BLOCKED patient is refused", rej(s), f"{s} {msg(d)}", "Rejected", "Block patient -> register OP visit", severity="Critical")
    s, d = admin.post(f"/Patients/{reg['id']}/unblock")
    tc("REG-028", M, "Unblock patient", ok(s), f"{s} {msg(d)}", "Unblocked", severity="High")
    s, d = rec.post("/opd/visits", {"patientId": reg["id"], "doctorId": DOC, "departmentId": GM, "appointmentId": None})
    tc("REG-029", M, "OP visit works again after unblock", ok(s), f"{s} {msg(d)}", "Registered", severity="High")
    reg_visit = d["data"]["id"] if ok(s) else None
    s, d = admin.get("/patients/reports/block-unblock?fromDate=2026-01-01&toDate=2026-12-31")
    tc("REG-030", M, "Block/Unblock report shows the event", s == 200 and len(json.dumps(d)) > 200, f"{s}", "Report lists block+unblock", severity="Medium")
    s, d = rec.get(f"/Patients/{reg['id']}/card")
    tc("REG-031", M, "Registration card PDF", s == 200 and d.get("binary") and d["head"] == b"%PDF-", f"{s} {d.get('contentType')}", "Valid PDF", severity="Medium")
    s, d = rec.get(f"/opd/visits/{reg_visit}/slip") if reg_visit else (0, {})
    tc("REG-032", M, "OP visit slip PDF (token/receipt)", s == 200 and d.get("binary") and d["head"] == b"%PDF-", f"{s} {d.get('contentType')}", "Valid PDF", severity="Medium")
    s, d = rec.get(f"/opd/visits?PatientId={reg['id']}&Page=1&PageSize=10")
    tc("REG-033", M, "Registration/visit history for the patient", s == 200 and d["data"]["totalCount"] >= 1, f"{s} visits={d['data']['totalCount'] if s == 200 else ''}", "Visit listed", severity="Medium")

# merge
s1, d1 = rec.post("/Patients", patient_body("QATEST", "MergeKeep", 1, "1980-05-05", "9000000301"))
s2, d2 = rec.post("/Patients", patient_body("QATEST", "MergeDrop", 1, "1980-05-05", "9000000302"))
if ok(s1) and ok(s2):
    keep, drop = d1["data"]["id"], d2["data"]["id"]
    s, d = admin.post(f"/Patients/{keep}/merge", {"mergedPatientId": drop, "reason": "QA merge test"})
    tc("REG-034", M, "Merge duplicate patient records", ok(s), f"{s} {msg(d)}", "Merged", severity="High")
    s, d = admin.get(f"/Patients/{drop}")
    s2, d2 = admin.get("/Patients?Page=1&PageSize=200&Search=MergeDrop")
    tc("REG-035", M, "Merged-away record is soft-deleted (documented): not retrievable, not listed", s == 404 and d2["data"]["totalCount"] == 0, f"get={s} list={d2['data']['totalCount']}", "Hidden from lists (soft delete); survivor keeps history", severity="High")
    s, d = admin.post(f"/Patients/{keep}/merge", {"mergedPatientId": keep, "reason": "self"})
    tc("REG-036", M, "Merging a patient into itself rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
    s, d = rec.post(f"/Patients/{keep}/merge", {"mergedPatientId": drop, "reason": "no rights"})
    tc("REG-037", M, "Reception cannot merge (RBAC)", s == 403, f"{s}", "403", severity="Medium")

not_tested("REG-040", M, "Registration cancellation", "FEATURE NOT PRESENT: the API has no cancel/delete endpoint for a patient registration (only Block). A mistaken registration can only be blocked or merged.", expected="Controlled cancellation with reason + audit")
not_tested("REG-041", M, "Registration payment and receipt", "FEATURE NOT PRESENT as a registration step: registration is free; the 'Registration Fee' (SM-REG, 100) must be added manually on the OP bill (see BILL-013).", expected="Registration fee billed and receipted at registration")

# =============================== B. DOCTOR / EMR (OPD consultation) ===============================
M = "B-Doctor / EMR"
s, d = admin.get("/Drugs?Page=1&PageSize=5&Search=Paracetamol"); drug = d["data"]["items"][0] if d["data"]["items"] else admin.get("/Drugs?Page=1&PageSize=1")[1]["data"]["items"][0]
s, d = admin.get("/icd-codes?Page=1&PageSize=1&Search=fever"); icd = d["data"]["items"][0] if d["data"]["items"] else admin.get("/icd-codes?Page=1&PageSize=1")[1]["data"]["items"][0]

pid = P["Alpha"]
vid = rec.post("/opd/visits", {"patientId": pid, "doctorId": DOC, "departmentId": GM, "appointmentId": None})[1]["data"]["id"]
s, d = doc.get(f"/doctor-portal/queue")
tc("EMR-001", M, "Doctor login + Doctor Portal queue lists today's patients for this doctor", s == 200 and any(x.get("visitId") == vid or x.get("id") == vid for x in (d["data"] if isinstance(d["data"], list) else d["data"].get("items", []))),
   f"{s} entries={len(d['data']) if s == 200 and isinstance(d['data'], list) else d.get('data')}", "New visit visible in own queue", severity="High")
s, d = doc2.get("/doctor-portal/queue")
other = [x for x in (d["data"] if s == 200 and isinstance(d["data"], list) else []) if x.get("visitId") == vid or x.get("id") == vid]
tc("EMR-002", M, "Another doctor does NOT see this doctor's patient in their queue", s == 200 and not other, f"{s} leaked={len(other)}", "Not visible", severity="Critical")
own_vid = rec.post("/opd/visits", {"patientId": P["Bravo"], "doctorId": DOC, "departmentId": GM, "appointmentId": None})[1]["data"]["id"]
s, d = doc2.post(f"/opd/visits/{own_vid}/start-consult")
tc("EMR-003", M, "Doctor B cannot start consultation on Doctor A's visit", rej(s), f"{s} {msg(d)}", "Rejected (ownership)", "Doctor B calls start-consult on Doctor A's visit", severity="High",
   bug="No ownership check on OPD consultation endpoints: any user holding the consult permission can start/write another doctor's visit (Doctor Portal checks ownership, the OPD endpoints do not).")
s, d = doc2.put(f"/opd/visits/{own_vid}/vitals", {"heightCm": 170, "weightKg": 70, "temperatureF": 98.6, "pulseRate": 72, "respiratoryRate": 16, "bloodPressureSystolic": 120, "bloodPressureDiastolic": 80, "spO2": 98, "randomBloodSugar": 100})
tc("EMR-004", M, "Doctor B cannot write vitals into Doctor A's visit", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
admin.post(f"/opd/visits/{own_vid}/cancel", {"reason": "QA cleanup"})
s, d = doc.post(f"/opd/visits/{vid}/start-consult")
tc("EMR-005", M, "Start consultation -> InConsult", ok(s), f"{s} {msg(d)}", "Status InConsult", severity="High")
s, d = doc.get(f"/opd/visits/{vid}")
tc("EMR-006", M, "Visit status reflects InConsult", s == 200 and d["data"]["status"] == 2, f"status={d['data'].get('status')}", "2", severity="Medium")

vit = {"heightCm": 170, "weightKg": 70, "temperatureF": 98.6, "pulseRate": 72, "respiratoryRate": 16, "bloodPressureSystolic": 120, "bloodPressureDiastolic": 80, "spO2": 98, "randomBloodSugar": 100}
s, d = doc.put(f"/opd/visits/{vid}/vitals", vit)
tc("EMR-007", M, "Save vitals", ok(s), f"{s} {msg(d)}", "Saved", severity="High")
s, d = doc.get(f"/opd/visits/{vid}/workspace"); w = d["data"]
tc("EMR-008", M, "BMI auto-calculated (70 / 1.7^2 = 24.2)", bool(w["vitals"] and abs(float(w["vitals"]["bmi"]) - 24.2) < 0.05), f"bmi={w['vitals'] and w['vitals']['bmi']}", "24.2", severity="Medium")
for tid, f, v_, lab in (("EMR-009", "temperatureF", 200, "temperature 200F"), ("EMR-010", "spO2", 150, "SpO2 150%"), ("EMR-011", "pulseRate", -5, "negative pulse"), ("EMR-012", "heightCm", 0, "zero height")):
    b = dict(vit); b[f] = v_
    s, d = doc.put(f"/opd/visits/{vid}/vitals", b)
    tc(tid, M, f"Vitals validation: {lab}", s in (400, 422), f"{s} {msg(d)}", "Rejected", severity="Medium")
b = dict(vit); b["bloodPressureSystolic"] = 70; b["bloodPressureDiastolic"] = 120
s, d = doc.put(f"/opd/visits/{vid}/vitals", b)
tc("EMR-013", M, "Vitals: systolic (70) lower than diastolic (120) is physiologically invalid", rej(s), f"{s} {msg(d)}", "Rejected or flagged", severity="Low")
doc.put(f"/opd/visits/{vid}/vitals", vit)

s, d = doc.put(f"/opd/visits/{vid}/complaints", {"complaints": [{"complaint": "Fever", "duration": "3 days"}, {"complaint": "Cough", "duration": "2 days"}]})
tc("EMR-014", M, "Save chief complaints", ok(s), f"{s} {msg(d)}", "Saved", severity="High")
s, d = doc.put(f"/opd/visits/{vid}/examination", {"historyNotes": "QA history", "examinationNotes": "QA exam"})
tc("EMR-015", M, "Save history and examination notes", ok(s), f"{s} {msg(d)}", "Saved", severity="High")
s, d = doc.put(f"/opd/visits/{vid}/diagnoses", {"diagnoses": [{"icdCodeId": icd["id"], "type": 1, "notes": "QA dx"}]})
tc("EMR-016", M, "Save diagnosis (ICD-10)", ok(s), f"{s} {msg(d)}", "Saved", severity="Critical")
s, d = doc.put(f"/opd/visits/{vid}/diagnoses", {"diagnoses": [{"icdCodeId": 999999, "type": 1, "notes": ""}]})
tc("EMR-017", M, "Diagnosis with unknown ICD code rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
doc.put(f"/opd/visits/{vid}/diagnoses", {"diagnoses": [{"icdCodeId": icd["id"], "type": 1, "notes": "QA dx"}]})

# allergy cross-check
s, d = admin.post(f"/emr/patients/{pid}/allergies", {"allergenType": 1, "allergenName": drug["name"].split()[0], "reaction": "Rash (QA)", "severity": 2})
tc("EMR-018", M, "Record a drug allergy for the patient", ok(s), f"{s} {msg(d)}", "Saved", severity="High")
s, d = doc.get(f"/emr/patients/{pid}/allergy-check?drugNames={drug['name'].split()[0]}")
tc("EMR-019", M, "Allergy check flags the allergic drug", s == 200 and len(json.dumps(d["data"])) > 5 and "[]" not in json.dumps(d["data"])[:3], f"{s} {json.dumps(d['data'])[:200]}", "Warning returned", severity="Critical")
s, d = doc.put(f"/opd/visits/{vid}/prescription", {"notes": "QA rx", "items": [{"drugId": drug["id"], "dose": "500 mg", "frequency": "TDS", "duration": "5 days", "route": "Oral", "instructions": "after food"}]})
tc("EMR-020", M, "Prescribe a medicine (allergy conflict: warn but allow with doctor override)", ok(s), f"{s} {msg(d)}", "Saved (EMR warns; doctor decides)", severity="Medium")
s, d = doc.put(f"/opd/visits/{vid}/prescription", {"notes": None, "items": [{"drugId": drug["id"], "dose": "", "frequency": "TDS", "duration": "5 days", "route": None, "instructions": None}]})
tc("EMR-021", M, "Prescription line without dose rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = doc.put(f"/opd/visits/{vid}/prescription", {"notes": None, "items": [{"drugId": 999999, "dose": "1", "frequency": "OD", "duration": "1 day", "route": None, "instructions": None}]})
tc("EMR-022", M, "Prescription with unknown drug rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
doc.put(f"/opd/visits/{vid}/prescription", {"notes": "QA rx", "items": [{"drugId": drug["id"], "dose": "500 mg", "frequency": "TDS", "duration": "5 days", "route": "Oral", "instructions": "after food"}]})

# orders
s, d = doc.post(f"/opd/visits/{vid}/orders", {"orderType": 1, "description": "CBC (QA)", "priority": 1, "notes": None})
lab_order = d["data"] if ok(s) else None
tc("EMR-023", M, "Raise a laboratory investigation order", ok(s), f"{s} {msg(d)}", "Order created", severity="High")
s, d = doc.post(f"/opd/visits/{vid}/orders", {"orderType": 2, "description": "X-Ray Chest (QA)", "priority": 2, "notes": None})
rad_order = d["data"] if ok(s) else None
tc("EMR-024", M, "Raise a radiology order", ok(s), f"{s} {msg(d)}", "Order created", severity="High")
s, d = doc.post(f"/opd/visits/{vid}/orders", {"orderType": 3, "description": "Dressing (QA)", "priority": 1, "notes": None})
tc("EMR-025", M, "Raise a procedure order", ok(s), f"{s} {msg(d)}", "Order created", severity="Medium")
s, d = doc.post(f"/opd/visits/{vid}/orders", {"orderType": 1, "description": "", "priority": 1, "notes": None})
tc("EMR-026", M, "Order with empty description rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = doc.get(f"/opd/visits/{vid}/orders"); orders = d["data"]
if orders:
    s, d = doc.post(f"/opd/visits/{vid}/orders/{orders[-1]['id']}/cancel")
    tc("EMR-027", M, "Cancel a clinical order", ok(s), f"{s} {msg(d)}", "Order cancelled", severity="Medium")
    s, d = doc.post(f"/opd/visits/{vid}/orders/{orders[-1]['id']}/cancel")
    tc("EMR-028", M, "Cancelling an already cancelled order blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Low")

# referral & follow-up
s, d = doc.post(f"/opd/visits/{vid}/referrals", {"direction": 1, "referredToDoctorId": DOC2, "externalDetails": None, "reason": "QA cross-consult"})
ref_id = d["data"] if ok(s) else None
tc("EMR-029", M, "Internal referral to another doctor", ok(s), f"{s} {msg(d)}", "Referral created", severity="Medium")
s, d = doc2.get("/doctor-portal/opinion-requests")
tc("EMR-030", M, "Referred-to doctor sees the opinion request in their inbox", s == 200 and len(json.dumps(d["data"])) > 10, f"{s} {len(d['data']) if s == 200 and isinstance(d['data'], list) else ''}", "Request visible", severity="Medium")
s, d = doc.put(f"/opd/visits/{vid}/follow-up", {"followUpDate": (datetime.date.today() + datetime.timedelta(days=7)).isoformat(), "notes": "Review in a week"})
tc("EMR-031", M, "Follow-up advice saved", ok(s), f"{s} {msg(d)}", "Saved", severity="Low")
s, d = doc.put(f"/opd/visits/{vid}/follow-up", {"followUpDate": (datetime.date.today() - datetime.timedelta(days=3)).isoformat(), "notes": "past"})
tc("EMR-032", M, "Follow-up date in the past rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Low")

# complete & post-completion controls
s, d = doc.post(f"/opd/visits/{vid}/complete")
tc("EMR-033", M, "Complete consultation", ok(s), f"{s} {msg(d)}", "Status Completed", severity="Critical")
s, d = doc.get(f"/opd/visits/{vid}"); tc("EMR-034", M, "Status is Completed with completed time stamp", s == 200 and d["data"]["status"] == 3 and d["data"]["completedAt"], f"status={d['data'].get('status')} completedAt={d['data'].get('completedAt')}", "Completed + timestamp", severity="High")
s, d = doc.put(f"/opd/visits/{vid}/diagnoses", {"diagnoses": [{"icdCodeId": icd["id"], "type": 2, "notes": "QA post-complete edit"}]})
tc("EMR-035", M, "Clinical record edit AFTER completion is blocked or goes through a controlled correction", rej(s), f"{s} {msg(d)}", "Rejected (record locked) or addendum workflow", "Complete consult -> try to change diagnosis", severity="High",
   bug="A completed consultation's diagnosis can be silently overwritten; no addendum/correction trail beyond the generic audit log.")
s, d = doc.put(f"/opd/visits/{vid}/prescription", {"notes": "tampered", "items": [{"drugId": drug["id"], "dose": "5000 mg", "frequency": "QID", "duration": "30 days", "route": "Oral", "instructions": None}]})
tc("EMR-036", M, "Prescription edit AFTER completion is blocked or controlled", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = admin.post(f"/opd/visits/{vid}/cancel", {"reason": "QA cancel completed consultation"})
tc("EMR-037", M, "Cancelling a COMPLETED consultation is blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = doc.post(f"/opd/visits/{vid}/start-consult")
tc("EMR-038", M, "Re-starting a completed consultation is blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = doc.get(f"/opd/visits/{vid}/summary"); tc("EMR-039", M, "Visit summary PDF", s == 200 and d.get("binary") and d["head"] == b"%PDF-", f"{s} {d.get('contentType')}", "Valid PDF", severity="Medium")

# history, review logic, EMR timeline
vid2 = rec.post("/opd/visits", {"patientId": pid, "doctorId": DOC, "departmentId": GM, "appointmentId": None})[1]["data"]
tc("EMR-040", M, "Return visit within follow-up window is auto-typed Review with follow-up fee (250)", vid2["visitType"] == 2 and D(str(vid2["consultationFee"])) == D(250), f"type={vid2['visitType']} fee={vid2['consultationFee']}", "Review, fee 250", severity="High")
s, d = doc.post(f"/opd/visits/{vid2['id']}/start-consult"); s, d = doc.get(f"/opd/visits/{vid2['id']}/workspace")
tc("EMR-041", M, "Previous consultation accessible from the new visit workspace", s == 200 and len(d["data"]["recentVisits"]) >= 1, f"{s} recentVisits={len(d['data']['recentVisits']) if s == 200 else ''}", "Prior visit with diagnosis shown", severity="High")
tc("EMR-042", M, "Recent medications shown for prescribing context", s == 200 and len(d["data"]["recentMedications"]) >= 1, f"meds={len(d['data']['recentMedications']) if s == 200 else ''}", ">= 1", severity="Medium")
s, d = doc.get(f"/emr/patients/{pid}/timeline"); tl = json.dumps(d["data"]) if s == 200 else ""
tc("EMR-043", M, "EMR timeline shows the completed OP consultation", s == 200 and "OPD Visit" in tl and "Completed" in tl, f"{s} len={len(tl)}", "Visit present in timeline", severity="High")
s, d = doc.get(f"/emr/patients/{pid}/prescriptions"); tc("EMR-044", M, "Prescription history view", s == 200 and len(json.dumps(d["data"])) > 20, f"{s}", "Rx listed", severity="Medium")
s, d = doc.get(f"/emr/patients/{pid}/problem-list"); tc("EMR-045", M, "Problem list loads", s == 200, f"{s}", "200", severity="Low")
s, d = nur.get(f"/emr/patients/{pid}/timeline"); tc("EMR-046", M, "Nurse can read EMR timeline (clinical role)", s == 200, f"{s}", "200 (nurse has clinical read)", severity="Low")
s, d = bil.get(f"/emr/patients/{pid}/timeline")
tc("EMR-047", M, "Billing role has NO access to clinical EMR timeline (data protection)", s == 403, f"{s}", "403", severity="High")
s, d = bil.get(f"/opd/visits/{vid}/workspace")
tc("EMR-048", M, "Billing role cannot open the clinical consultation workspace", s == 403, f"{s}", "403", severity="High")
s, d = rec.get(f"/opd/visits/{vid}/workspace")
tc("EMR-049", M, "Reception cannot open the clinical consultation workspace", s == 403, f"{s}", "403", severity="High")
summary()
