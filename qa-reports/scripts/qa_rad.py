import sys, os, datetime
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from qa_lib import *

M = "E-Radiology"
st = load_state(); P = st["patients"]; DOC = st["doctors"]["qa.doctor"]
admin = Sess("admin", "Admin@123", 2); doc = Sess("qa.doctor", "Qa@12345", 2); rad = Sess("qa.rad", "Qa@12345", 2)
tech = Sess("qa.radtech", "Qa@12345", 2); nur = Sess("qa.nurse", "Qa@12345", 2); bil = Sess("qa.billing", "Qa@12345", 2)

tests = admin.get("/rad/tests")[1]["data"]
print("rad tests:", len(tests), [(t["id"], t["name"], t.get("modalityName"), t.get("requiresContrast")) for t in tests[:6]])
plain = next(t for t in tests if not t.get("requiresContrast") and ("X-Ray" in t["name"] or "Chest" in t["name"] or True))
contrast = next((t for t in tests if t.get("requiresContrast")), None)
print("plain:", plain["id"], plain["name"], "| contrast:", contrast and (contrast["id"], contrast["name"]))
tmpl = admin.get(f"/rad/templates?radiologyTestId={plain['id']}")[1]["data"]
TODAY = datetime.date.today()
FUT = (datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(days=1)).replace(hour=10, minute=0, second=0, microsecond=0)


def rorder(pk, test_ids, who=None, **kw):
    b = {"patientId": P[pk], "opVisitId": None, "admissionId": None, "emergencyVisitId": None, "clinicalOrderId": None,
         "orderedByDoctorId": DOC, "priority": 1, "clinicalNotes": "QA", "radiologyTestIds": test_ids}
    b.update(kw)
    return (who or doc).post("/rad/orders", b)


def rdetail(oid):
    return admin.get(f"/rad/orders/{oid}")[1]["data"]


def idof(d):
    return d["data"] if not isinstance(d["data"], dict) else d["data"]["id"]


# ---- orders
s, d = rorder("Golf", [plain["id"]])
tc("RAD-001", M, "Doctor places a radiology order", ok(s), f"{s} {msg(d)}", "Order created", severity="Critical")
oid = idof(d); o = rdetail(oid); it = o["items"][0]
print("ORDER RAW:", json.dumps(o)[:500])
tc("RAD-002", M, "Order maps to correct patient, has number, Ordered status and an accession number", o["patientUhid"] == "PRX-2026-000008" and it.get("accessionNumber") and o["status"] == 1,
   f"uhid={o['patientUhid']} acc={it.get('accessionNumber')} status={o['status']}", "Golf/PRX-2026-000008, accession present, Ordered", severity="Critical")
s, d = rorder("Golf", [])
tc("RAD-003", M, "Order with no tests rejected", s in (400, 422), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = rorder("Golf", [999999])
tc("RAD-004", M, "Order with unknown test rejected cleanly (not a 500)", s in (400, 404), f"{s} {msg(d)}", "404/400", severity="Medium")
s, d = rorder("Golf", [plain["id"], plain["id"]])
tc("RAD-005", M, "Same scan twice in one order blocked/de-duplicated", rej(s) or len(rdetail(idof(d))["items"]) == 1, f"{s} {msg(d)}", "Rejected or single line", severity="Medium")
s, d = rorder("Golf", [plain["id"]], who=nur)
tc("RAD-006", M, "Nurse cannot place radiology orders (RBAC)", s == 403, f"{s}", "403", severity="Medium")
s, d = rorder("Golf", [plain["id"]], who=bil)
tc("RAD-007", M, "Billing role cannot place radiology orders (RBAC)", s == 403, f"{s}", "403", severity="Medium")
s, d = bil.get("/rad/orders"); tc("RAD-008", M, "Billing role cannot browse radiology orders/reports (data protection)", s == 403, f"{s}", "403", severity="High")

# ---- scheduling
s, d = tech.get("/rad/scheduling/worklist")
tc("RAD-009", M, "Order appears in scheduling worklist", s == 200 and str(o["orderNumber"]) in json.dumps(d["data"]), f"{s} rows={len(d['data']) if s == 200 and isinstance(d['data'], list) else ''}", "Listed", severity="High")
s, d = tech.post(f"/rad/capture/order-items/{it['id']}")
tc("RAD-010", M, "Image capture BEFORE scheduling is blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
_s, _d = rorder("Golf", [plain["id"]]); _past_item = rdetail(idof(_d))["items"][0]
s, d = tech.post(f"/rad/scheduling/order-items/{_past_item['id']}", {"scheduledAt": (datetime.datetime.now(datetime.timezone.utc) - datetime.timedelta(days=2)).isoformat(), "notes": None})
tc("RAD-011", M, "Scheduling a scan in the PAST rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
_s, _d = rorder("Golf", [plain["id"]]); _naive_item = rdetail(idof(_d))["items"][0]
s, d = tech.post(f"/rad/scheduling/order-items/{_naive_item['id']}", {"scheduledAt": (datetime.datetime.now() + datetime.timedelta(days=3)).replace(microsecond=0).isoformat(), "notes": None})
tc("RAD-056", M, "Scheduled time without a timezone offset is accepted or cleanly rejected (no server crash)", s < 500, f"{s} {msg(d)}", "2xx or 4xx, never 500", severity="Medium",
   bug="POST /rad/scheduling/order-items/{id} with a timezone-less ISO datetime returns HTTP 500 (unhandled) instead of a validation error.")
s, d = tech.post(f"/rad/scheduling/order-items/{it['id']}", {"scheduledAt": FUT.isoformat(), "notes": "QA"})
tc("RAD-012", M, "Schedule the scan for tomorrow 10:00", ok(s), f"{s} {msg(d)}", "Scheduled", severity="High")
# double booking same modality/time
s, d = rorder("Hotel", [plain["id"]]); o2 = rdetail(idof(d)); it2 = o2["items"][0]
s, d = tech.post(f"/rad/scheduling/order-items/{it2['id']}", {"scheduledAt": FUT.isoformat(), "notes": "QA same slot"})
tc("RAD-013", M, "Two patients scheduled in the identical modality slot (double-booking) is blocked/warned", rej(s), f"{s} {msg(d)}", "Slot conflict detected", "Schedule two items on same modality at same time", severity="Medium",
   bug="No modality slot-conflict check: two patients can be scheduled on the same modality at the same minute.")
s, d = tech.post(f"/rad/scheduling/order-items/{it['id']}", {"scheduledAt": (FUT + datetime.timedelta(hours=2)).isoformat(), "notes": "QA reschedule"})
tc("RAD-014", M, "Reschedule an already scheduled scan", ok(s), f"{s} {msg(d)}", "Rescheduled", severity="Medium")
s, d = nur.post(f"/rad/scheduling/order-items/{it['id']}", {"scheduledAt": FUT.isoformat(), "notes": None})
tc("RAD-015", M, "Nurse cannot schedule scans (RBAC)", s == 403, f"{s}", "403", severity="Medium")

# ---- capture, report workflow
s, d = rad.post(f"/rad/order-items/{it['id']}/report", {"templateId": None, "findings": "x", "impression": "y"})
tc("RAD-016", M, "Report entry BEFORE image capture is blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = tech.post(f"/rad/capture/order-items/{it['id']}")
tc("RAD-017", M, "Technician captures image (marks ImageCaptured)", ok(s), f"{s} {msg(d)}", "Captured", severity="High")
not_tested("RAD-018", M, "Image upload / PACS integration", "FEATURE NOT PRESENT: no image-file upload endpoint exists; PACS/DICOM is a placeholder (accession number only). Capture only flips status.", expected="Images uploaded/linked from PACS and viewable in report")
s, d = rad.post(f"/rad/order-items/{it['id']}/report", {"templateId": None, "findings": "", "impression": ""})
tc("RAD-019", M, "Report with empty findings and impression rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = tech.post(f"/rad/order-items/{it['id']}/report", {"templateId": None, "findings": "Technician typed findings", "impression": "x"})
tc("RAD-020", M, "Technician cannot author the radiology report (RBAC)", s == 403, f"{s}", "403", severity="High")
s, d = rad.post(f"/rad/order-items/{it['id']}/report", {"templateId": tmpl[0]["id"] if tmpl else None, "findings": "Lung fields clear. No consolidation (QA).", "impression": "Normal chest radiograph (QA)."})
tc("RAD-021", M, "Radiologist enters report (template prefill id used)", ok(s), f"{s} {msg(d)}", "Saved", severity="Critical")
s, d = rad.post(f"/rad/order-items/{it['id']}/report/approve")
tc("RAD-022", M, "Approve BEFORE verify blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = rad.get(f"/rad/order-items/{it['id']}/report/pdf")
tc("RAD-023", M, "Report PDF NOT released before approval", not (s == 200 and d.get("binary")), f"{s} {d.get('contentType', msg(d))}", "Blocked until approved", severity="Critical")
s, d = rad.post(f"/rad/order-items/{it['id']}/report/verify")
tc("RAD-024", M, "Report verified", ok(s), f"{s} {msg(d)}", "Verified", severity="High")
s, d = rad.post(f"/rad/order-items/{it['id']}/report", {"templateId": None, "findings": "Changed after verify", "impression": "Changed"})
tc("RAD-025", M, "Report edit after verification blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = bil.post(f"/rad/order-items/{it['id']}/report/approve")
tc("RAD-026", M, "Billing role cannot approve radiology report (RBAC)", s == 403, f"{s}", "403", severity="Critical")
s, d = rad.post(f"/rad/order-items/{it['id']}/report/approve")
tc("RAD-027", M, "Radiologist approves report", ok(s), f"{s} {msg(d)}", "Approved", severity="Critical")
s, d = rad.get(f"/rad/order-items/{it['id']}/report/pdf")
tc("RAD-028", M, "Report PDF available after approval", s == 200 and d.get("binary") and d["head"] == b"%PDF-", f"{s} {d.get('contentType', msg(d))}", "Valid PDF", severity="High")
s, d = rad.post(f"/rad/order-items/{it['id']}/report", {"templateId": None, "findings": "Late change", "impression": "Late"})
tc("RAD-029", M, "Report correction AFTER approval blocked or goes through a controlled amendment", rej(s), f"{s} {msg(d)}", "Rejected / amendment workflow", severity="High")
not_tested("RAD-030", M, "Formal report correction/addendum workflow", "FEATURE NOT PRESENT: once approved a report is locked and there is no amend/addendum endpoint; correction requires a new order.", expected="Controlled addendum with history")
s, d = admin.post(f"/rad/orders/{oid}/items/{it['id']}/cancel")
tc("RAD-031", M, "Cancelling an APPROVED scan blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
o = rdetail(oid)
tc("RAD-032", M, "Order status rolls up to Completed", o["status"] == 3, f"status={o['status']} item={o['items'][0]['status']}", "Completed", severity="High")
s, d = doc.get(f"/emr/patients/{P['Golf']}/investigations")
tc("RAD-033", M, "Approved report visible in the patient's EMR", s == 200 and (plain["name"] in json.dumps(d["data"]) or "Radiology" in json.dumps(d["data"]) or "Chest" in json.dumps(d["data"])), f"{s} {json.dumps(d['data'])[:150]}", "Radiology result listed", severity="High", bug="Radiology is not aggregated into the EMR at all: approved reports do not appear in the patient timeline, investigations or procedures views (EMR only aggregates lab results).")
s, d = doc.get(f"/emr/patients/{P['Foxtrot']}/investigations")
tc("RAD-034", M, "Report NOT visible in another patient's EMR", s == 200 and plain["name"] not in json.dumps(d["data"]), f"{s}", "Absent", severity="Critical")

# ---- billing verification
TD = TODAY.isoformat()
unb = admin.get(f"/rad/orders/unbilled-report?fromDate={TD}&toDate={TD}")[1]["data"]
tc("RAD-035", M, "Approved but unbilled scan appears in the Unbilled Orders report", any(o["orderNumber"] == x.get("orderNumber") for x in unb), f"{len(unb)} rows", "Listed", severity="High")
tc("RAD-036", M, "Radiology is gated by billing (scan cannot be scheduled/captured/reported until billed)", False,
   "Scan scheduled, captured, reported and approved with no bill line; nothing blocked it (documented as deliberate/advisory only)",
   "Billing verified before scan", "Order -> never bill -> schedule -> capture -> report", severity="High",
   bug="Revenue leakage: radiology scans are performed without billing verification; radiology tests have no billing service mapping.")
mapped = sum(1 for t in tests if admin.get(f"/rad/tests/{t['id']}")[1]["data"].get("serviceId"))
tc("RAD-037", M, "Radiology tests are mapped to billing services (price source)", mapped > 0, f"{mapped} of {len(tests)} radiology tests have a billing service", ">0", severity="High")

# ---- contrast consent gate
if contrast:
    s, d = rorder("Hotel", [contrast["id"]]); oc = rdetail(idof(d)); ic = oc["items"][0]
    tech.post(f"/rad/scheduling/order-items/{ic['id']}", {"scheduledAt": (FUT + datetime.timedelta(days=1)).isoformat(), "notes": None})
    s, d = tech.post(f"/rad/capture/order-items/{ic['id']}")
    tc("RAD-040", M, "Contrast study cannot be captured without signed contrast consent (safety gate)", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
    s, d = tech.post(f"/rad/order-items/{ic['id']}/contrast-consent", {"signedByName": "", "relationshipToPatient": None})
    tc("RAD-041", M, "Consent without signatory name rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
    s, d = tech.post(f"/rad/order-items/{ic['id']}/contrast-consent", {"signedByName": "QATEST Hotel", "relationshipToPatient": "Self"})
    tc("RAD-042", M, "Record contrast consent", ok(s), f"{s} {msg(d)}", "Saved", severity="High")
    s, d = tech.post(f"/rad/capture/order-items/{ic['id']}")
    tc("RAD-043", M, "Capture succeeds once consent recorded", ok(s), f"{s} {msg(d)}", "Captured", severity="High")
else:
    blocked("RAD-040", M, "Contrast consent gate", "no contrast-requiring radiology test exists in the catalog")

# ---- cancellation
s, d = rorder("Foxtrot", [plain["id"]]); oc2 = idof(d)
s, d = admin.post(f"/rad/orders/{oc2}/cancel", {"reason": ""})
tc("RAD-050", M, "Cancel order without reason rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = admin.post(f"/rad/orders/{oc2}/cancel", {"reason": "QA cancel before scheduling"})
tc("RAD-051", M, "Cancel unscheduled order with reason", ok(s), f"{s} {msg(d)}", "Cancelled", severity="High")
oc2d = rdetail(oc2)
tc("RAD-052", M, "Cancelled order retained with Cancelled status", oc2d["status"] == 4, f"status={oc2d['status']}", "Cancelled(4)", severity="Critical")
s, d = tech.post(f"/rad/scheduling/order-items/{oc2d['items'][0]['id']}", {"scheduledAt": FUT.isoformat(), "notes": None})
tc("RAD-053", M, "Scheduling a CANCELLED order blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = tech.get("/rad/orders/tat-report"); tc("RAD-054", M, "TAT report loads", s == 200, f"{s}", "200", severity="Low")
s, d = rad.get("/rad/reports/queue?status=4"); tc("RAD-055", M, "Radiologist reporting queue loads", s == 200, f"{s}", "200", severity="Medium")
summary()
