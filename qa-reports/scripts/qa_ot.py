import sys, os, datetime
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from qa_lib import *
from decimal import Decimal as D

M = "H-OT Management"
st = load_state(); DOC = st["doctors"]["qa.doctor"]; DOC2 = st["doctors"]["qa.doctor2"]; GM = st["depts"]["DEP001"]
admin = Sess("admin", "Admin@123", 2); rec = Sess("qa.reception", "Qa@12345", 2); fo = Sess("qa.frontoffice", "Qa@12345", 2)
ot = Sess("qa.ot", "Qa@12345", 2); doc = Sess("qa.doctor", "Qa@12345", 2); nur = Sess("qa.nurse", "Qa@12345", 2); bil = Sess("qa.billing", "Qa@12345", 2)
RUN = datetime.datetime.now().strftime("%H%M%S")
UTC = datetime.timezone.utc
T0 = (datetime.datetime.now(UTC) + datetime.timedelta(days=10 + int(RUN) % 300)).replace(hour=4, minute=0, second=0, microsecond=0)


def idof(d):
    return d["data"] if not isinstance(d["data"], dict) else d["data"]["id"]


def iso(t):
    return t.isoformat()


def vacant(ward):
    return next(b["bedId"] for b in admin.get("/ipd/bed-board")[1]["data"] if b["wardName"] == ward and b["status"] == 1)


def admit_new(tag):
    s, d = rec.post("/Patients", patient_body("QATEST", f"Ot{tag}{RUN}", 1, "1970-07-07", "90000007" + RUN[-2:]))
    pid = d["data"]["id"]
    s, d = fo.post("/ipd/admissions", {"patientId": pid, "primaryConsultantId": DOC, "departmentId": st["depts"]["DEP010"], "bedId": vacant("General Ward"), "admissionType": 1,
                                       "provisionalDiagnosis": "QA hernia", "referringOpVisitId": None, "referringEmergencyVisitId": None, "secondaryConsultantIds": [],
                                       "consentTemplateId": 2, "consentSignedByName": "QATEST Self", "consentRelationshipToPatient": "Self",
                                       "initialDepositAmount": 0, "depositPaymentModeId": None, "depositReferenceNumber": None})
    assert ok(s), (s, d)
    return pid, idof(d)


theatres = admin.get("/ot/theatres")[1]["data"]
if len(theatres) < 2:
    admin.post("/ot/theatres", {"code": "QA-OT2", "name": "QA Theatre 2", "isActive": True})
    theatres = admin.get("/ot/theatres")[1]["data"]
TH1, TH2 = theatres[0]["id"], theatres[1]["id"]
pid, A = admit_new("A")
consent_id = admin.get(f"/ipd/admissions/{A}")[1]["data"].get("patientConsentId") or (admin.get(f"/Patients/{pid}/consents")[1]["data"] or [{}])[0].get("id")


def book(adm, th, start, hours=2, surgeon=DOC, anaes=DOC2, who=None):
    return (who or ot).post("/ot/bookings", {"admissionId": adm, "theatreId": th, "procedureName": "Inguinal hernia repair (QA)", "surgeonId": surgeon, "anaesthetistId": anaes,
                                            "scheduledStart": iso(start), "scheduledEnd": iso(start + datetime.timedelta(hours=hours)), "notes": None})


s, d = book(A, TH1, T0)
tc("OT-001", M, "Book OT for an admitted patient (theatre, surgeon, anaesthetist, slot)", ok(s), f"{s} {msg(d)}", "Booking Scheduled", severity="Critical")
B1 = idof(d) if ok(s) else None
pid2, A2 = admit_new("B")
s, d = book(A2, TH1, T0 + datetime.timedelta(hours=1), surgeon=st["doctors"]["qa.doctor2"], anaes=DOC)
tc("OT-002", M, "Overlapping booking in the SAME theatre blocked", rej(s), f"{s} {msg(d)}", "Rejected (theatre conflict)", severity="Critical")
s, d = book(A2, TH2, T0 + datetime.timedelta(hours=1), surgeon=DOC, anaes=None)
tc("OT-003", M, "Same surgeon double-booked in another theatre blocked", rej(s), f"{s} {msg(d)}", "Rejected (surgeon conflict)", severity="Critical")
s, d = book(A2, TH2, T0 + datetime.timedelta(hours=1), surgeon=st["doctors"]["qa.doctor"] if False else DOC2, anaes=DOC2)
tc("OT-004", M, "Surgeon and anaesthetist being the same person rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = ot.post("/ot/bookings", {"admissionId": A2, "theatreId": TH2, "procedureName": "QA", "surgeonId": DOC2, "anaesthetistId": None, "scheduledStart": iso(T0 + datetime.timedelta(days=1)), "scheduledEnd": iso(T0 + datetime.timedelta(days=1, hours=-1)), "notes": None})
tc("OT-005", M, "End time before start time rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
_ps = (datetime.datetime.now(UTC) - datetime.timedelta(days=30 + int(RUN) % 300)).replace(hour=1, minute=0, second=0, microsecond=0)
s, d = book(A2, TH2, _ps, surgeon=DOC2, anaes=None)
tc("OT-006", M, "Booking a slot in the past rejected", rej(s) and "already booked" not in msg(d), f"{s} {msg(d)}", "Rejected because the slot is in the past", severity="Medium", bug="OT bookings can be created for a slot in the past.")
if ok(s):
    ot.post(f"/ot/bookings/{idof(d)}/cancel", {"reason": "QA cleanup past booking"})
B2 = None
s, d = book(A2, TH2, T0 + datetime.timedelta(days=1), surgeon=DOC2, anaes=None)
B2 = idof(d) if ok(s) else None
s, d = book(A2, TH1, T0, who=nur)
tc("OT-007", M, "Nurse cannot book OT (RBAC)", s == 403, f"{s}", "403", severity="Medium")
not_tested("OT-008", M, "Minor vs Major OT booking type", "FEATURE NOT PRESENT: booking has no Minor/Major classification field.", expected="Booking categorised Minor/Major")
not_tested("OT-009", M, "Emergency OT booking", "FEATURE NOT PRESENT: no emergency/priority flag on OT bookings; an emergency case is booked like any other slot.", expected="Emergency flag that can override/bump elective slots")
not_tested("OT-010", M, "OT approval step", "FEATURE NOT PRESENT: bookings are Scheduled directly; there is no approve/confirm workflow.", expected="Booking approval by OT in-charge")

# team
s, d = ot.post(f"/ot/bookings/{B1}/team", {"role": 3, "doctorId": None, "staffName": "QA Scrub Nurse"})
tc("OT-011", M, "Add scrub nurse to surgical team", ok(s), f"{s} {msg(d)}", "Added", severity="Medium")
s, d = ot.post(f"/ot/bookings/{B1}/team", {"role": 1, "doctorId": DOC2, "staffName": None})
tc("OT-012", M, "Add assistant surgeon", ok(s), f"{s} {msg(d)}", "Added", severity="Medium")

# gates
s, d = doc.post(f"/ot/bookings/{B1}/start")
tc("OT-013", M, "Surgery cannot start before WHO Sign-In and consent verification", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
signin = {"patientIdentityConfirmed": True, "siteMarked": True, "anaesthesiaSafetyCheckComplete": True, "pulseOximeterFunctioning": True, "knownAllergy": False, "difficultAirwayRisk": False, "riskOfBloodLoss": False, "complete": True}
s, d = ot.put(f"/ot/bookings/{B1}/checklist/sign-in", dict(signin, siteMarked=False))
_ck = admin.get(f"/ot/bookings/{B1}/checklist")[1]["data"]
tc("OT-014", M, "Sign-In cannot be marked complete with an unticked mandatory item (site not marked)", rej(s) or _ck.get("signInCompletedAt") is None, f"{s} {msg(d)}", "Rejected", severity="Critical", bug="WHO Sign-In accepts complete=true with the site NOT marked; the step is stamped complete and surgery can proceed.")
s, d = ot.put(f"/ot/bookings/{B1}/checklist/sign-in", signin)
tc("OT-015", M, "Complete WHO Sign-In", ok(s), f"{s} {msg(d)}", "Saved", severity="High")
s, d = doc.post(f"/ot/bookings/{B1}/start")
tc("OT-016", M, "Surgery cannot start until consent is verified", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = ot.post(f"/ot/bookings/{B1}/verify-consent", {"patientConsentId": 999999})
tc("OT-017", M, "Verifying a non-existent consent rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = ot.post(f"/ot/bookings/{B1}/verify-consent", {"patientConsentId": consent_id})
tc("OT-018", M, "Verify the patient's signed surgical consent", ok(s), f"{s} {msg(d)} consent={consent_id}", "Verified", severity="Critical")
s, d = doc.put(f"/ot/bookings/{B1}/pre-anaesthesia", {"assessedByDoctorId": DOC2, "asaGrade": "II", "airwayAssessment": "Mallampati 2", "comorbidities": "None", "investigations": "Normal", "anaesthesiaPlan": "Spinal", "notes": None})
tc("OT-019", M, "Pre-anaesthesia assessment recorded", ok(s), f"{s} {msg(d)}", "Saved", severity="High")
s, d = doc.post(f"/ot/bookings/{B1}/start")
tc("OT-020", M, "Start surgery after Sign-In + consent (start time captured)", ok(s), f"{s} {msg(d)}", "InProgress", severity="Critical")
bk = admin.get(f"/ot/bookings/{B1}")[1]["data"]
tc("OT-021", M, "Booking InProgress with actual start time", bk["status"] == 2 and bk.get("actualStart"), f"status={bk['status']} start={bk.get('actualStart')}", "InProgress + start", severity="High")
s, d = ot.post(f"/ot/bookings/{B1}/reschedule", {"scheduledStart": iso(T0 + datetime.timedelta(days=3)), "scheduledEnd": iso(T0 + datetime.timedelta(days=3, hours=2))})
tc("OT-022", M, "Rescheduling a surgery already InProgress blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = doc.put(f"/ot/bookings/{B1}/intra-op", {"anaesthesiaType": 2, "administeredByDoctorId": DOC2, "startedAt": iso(datetime.datetime.now(UTC)), "endedAt": None, "notes": "QA"})
tc("OT-023", M, "Intra-op anaesthesia record", ok(s), f"{s} {msg(d)}", "Saved", severity="High")
s, d = ot.put(f"/ot/bookings/{B1}/checklist/time-out", {"teamIntroduced": True, "patientSiteProcedureConfirmed": True, "criticalEventsReviewed": True, "antibioticProphylaxisGiven": True, "imagingDisplayed": True, "complete": True})
tc("OT-024", M, "WHO Time-Out completed", ok(s), f"{s} {msg(d)}", "Saved", severity="High")

# consumables & implants
store = [x for x in admin.get("/pharmacy/stores")[1]["data"] if x["storeType"] == 1][0]["id"]
batch = next(b for b in admin.get(f"/pharmacy/stock/batches?storeId={store}&onlyInStock=true")[1]["data"] if b["batchNumber"] == "QA-B-1")
q0 = batch["quantityOnHand"]
s, d = ot.post(f"/ot/bookings/{B1}/usage/consumables", {"batchId": batch["id"], "quantity": 2})
tc("OT-037", M, "Implants/consumables can be recorded during surgery without depending on the billing desk", ok(s), f"{s} {msg(d)}", "Usage recorded (bill line created/queued automatically)",
   "Start surgery for an admission with no IP bill yet -> record consumable", severity="Medium",
   bug="Consumable/implant usage is refused mid-surgery until Billing has generated the IP bill ('Generate the IP bill for this admission before recording usage').")
bil.post(f"/billing/bills/generate-ip/{A}")
s, d = ot.post(f"/ot/bookings/{B1}/usage/consumables", {"batchId": batch["id"], "quantity": 2})
q1 = next(b for b in admin.get(f"/pharmacy/stock/batches?storeId={store}&onlyInStock=false")[1]["data"] if b["id"] == batch["id"])["quantityOnHand"]
tc("OT-025", M, "Consumable usage decrements stock", ok(s) and q0 - q1 == 2, f"{s} {msg(d)} stock {q0}->{q1}", "-2", severity="Critical")
s, d = ot.post(f"/ot/bookings/{B1}/usage/consumables", {"batchId": batch["id"], "quantity": 999999})
tc("OT-026", M, "Consumable usage above stock rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = ot.post(f"/ot/bookings/{B1}/usage/implants", {"batchId": batch["id"], "serialNumber": "", "quantity": 1})
tc("OT-027", M, "Implant usage without serial number rejected (traceability)", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = ot.post(f"/ot/bookings/{B1}/usage/implants", {"batchId": batch["id"], "serialNumber": f"QA-SN-{RUN}", "quantity": 1})
tc("OT-028", M, "Implant usage with serial number recorded", ok(s), f"{s} {msg(d)}", "Saved", severity="High")
ipb = [b for b in bil.get(f"/billing/bills?PatientId={pid}&Page=1&PageSize=5")[1]["data"]["items"] if b["billType"] == 2]
lines = bil.get(f"/billing/bills/{ipb[0]['id']}")[1]["data"]["items"] if ipb else []
otl = [i for i in lines if i["sourceType"] == 6]
tc("OT-029", M, "Consumable and implant charges posted to the patient's IP bill", len(otl) >= 2 and all(D(str(i["rate"])) > 0 for i in otl), f"OT charge lines={[(i['description'], i['quantity'], i['rate']) for i in otl]}", ">= 2 priced lines", severity="Critical")

# complete
s, d = doc.post(f"/ot/bookings/{B1}/complete")
tc("OT-030", M, "Surgery cannot be completed before WHO Sign-Out", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = ot.put(f"/ot/bookings/{B1}/checklist/sign-out", {"procedureNameRecorded": True, "instrumentSpongeNeedleCountCorrect": False, "specimenLabeled": True, "equipmentProblemsAddressed": True, "recoveryConcernsReviewed": True, "complete": True})
_ck = admin.get(f"/ot/bookings/{B1}/checklist")[1]["data"]
tc("OT-031", M, "Sign-Out cannot complete when instrument/sponge count is NOT correct", rej(s) or _ck.get("signOutCompletedAt") is None, f"{s} {msg(d)}", "Rejected", severity="Critical", bug="WHO Sign-Out accepts complete=true with an INCORRECT instrument/sponge/needle count (retained-item risk).")
ot.put(f"/ot/bookings/{B1}/checklist/sign-out", {"procedureNameRecorded": True, "instrumentSpongeNeedleCountCorrect": True, "specimenLabeled": True, "equipmentProblemsAddressed": True, "recoveryConcernsReviewed": True, "complete": True})
s, d = doc.put(f"/ot/bookings/{B1}/operative-note", {"surgeonId": DOC, "procedurePerformed": "Lichtenstein mesh repair (QA)", "findings": "Indirect sac", "complications": "None", "startedAt": iso(datetime.datetime.now(UTC) - datetime.timedelta(hours=1)), "endedAt": iso(datetime.datetime.now(UTC)), "notes": None})
tc("OT-032", M, "Operation notes recorded by surgeon", ok(s), f"{s} {msg(d)}", "Saved", severity="Critical")
s, d = doc.post(f"/ot/bookings/{B1}/complete")
bk = admin.get(f"/ot/bookings/{B1}")[1]["data"]
tc("OT-033", M, "Complete surgery: status Completed with end time", ok(s) and bk["status"] == 3 and bk.get("actualEnd"), f"{s} {msg(d)} status={bk['status']} end={bk.get('actualEnd')}", "Completed + end time", severity="Critical")
s, d = doc.put(f"/ot/bookings/{B1}/post-op-note", {"recoveryNotes": "Stable", "vitalsSummary": "Normal", "complications": "None", "shiftedAt": iso(datetime.datetime.now(UTC)), "notes": None})
tc("OT-034", M, "Post-operative note recorded", ok(s), f"{s} {msg(d)}", "Saved", severity="High")
s, d = ot.post(f"/ot/bookings/{B1}/cancel", {"reason": "QA cancel completed"})
tc("OT-035", M, "Cancelling a COMPLETED surgery blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = admin.get(f"/ot/bookings/register?fromDate={(T0 - datetime.timedelta(days=1)).date()}&toDate={(T0 + datetime.timedelta(days=5)).date()}")
tc("OT-036", M, "OT register lists the case", s == 200 and "QA" in json.dumps(d["data"]), f"{s}", "Listed", severity="Medium")

# cancel / reschedule / postpone on B2
if B2:
    s, d = ot.post(f"/ot/bookings/{B2}/reschedule", {"scheduledStart": iso(T0 + datetime.timedelta(days=4)), "scheduledEnd": iso(T0 + datetime.timedelta(days=4, hours=2))})
    tc("OT-041", M, "Reschedule a scheduled surgery", ok(s), f"{s} {msg(d)}", "Rescheduled", severity="High")
    s, d = ot.post(f"/ot/bookings/{B2}/postpone", {"reason": ""})
    tc("OT-042", M, "Postpone without reason rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
    s, d = ot.post(f"/ot/bookings/{B2}/cancel", {"reason": ""})
    tc("OT-043", M, "Cancel booking without reason rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
    s, d = ot.post(f"/ot/bookings/{B2}/cancel", {"reason": "QA patient unfit"})
    tc("OT-044", M, "Cancel scheduled booking with reason (record retained)", ok(s) and admin.get(f"/ot/bookings/{B2}")[1]["data"]["status"] == 4, f"{s} {msg(d)}", "Cancelled (4)", severity="High")
    s, d = book(A2, TH2, T0 + datetime.timedelta(days=1), surgeon=DOC2, anaes=None)
    tc("OT-045", M, "Cancelled slot becomes bookable again", ok(s), f"{s} {msg(d)}", "Booked", severity="Medium")
    if ok(s):
        ot.post(f"/ot/bookings/{idof(d)}/cancel", {"reason": "QA cleanup"})
# clean up admissions
fo.post(f"/ipd/admissions/{A2}/cancel", {"reason": "QA cleanup OT suite"})
fo.post(f"/ipd/admissions/{A}/cancel", {"reason": "QA cleanup OT suite"})
summary()
