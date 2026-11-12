import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from qa_lib import *

M = "D-Laboratory"
st = load_state(); P = st["patients"]; DOC = st["doctors"]["qa.doctor"]; GM = st["depts"]["DEP001"]
admin = Sess("admin", "Admin@123", 2); rec = Sess("qa.reception", "Qa@12345", 2); doc = Sess("qa.doctor", "Qa@12345", 2)
lab = Sess("qa.lab", "Qa@12345", 2); patho = Sess("qa.patho", "Qa@12345", 2); nur = Sess("qa.nurse", "Qa@12345", 2); bil = Sess("qa.billing", "Qa@12345", 2)
APTT = 7   # normal 25-35s, critical high 90 (adult, any sex)


def order(pk, tests, who=None, visit=None, **kw):
    body = {"patientId": P[pk], "opVisitId": visit, "emergencyVisitId": None, "clinicalOrderId": None, "orderedByDoctorId": DOC,
            "priority": 1, "clinicalNotes": "QA", "items": [{"labTestId": t, "testProfileId": None} for t in tests]}
    body.update(kw)
    return (who or doc).post("/lab/orders", body)


def detail(oid):
    return admin.get(f"/lab/orders/{oid}")[1]["data"]


def sample_for(oid, item_ids, sample_type, who=None):
    return (who or lab).post("/lab/collection/samples", {"labOrderId": oid, "sampleTypeId": sample_type, "labOrderItemIds": item_ids})


# ---- catalog facts
det = admin.get(f"/lab/tests/{APTT}")[1]["data"]
SAMPLE = det["sampleTypeId"]; PARAM = det["parameters"][0]["id"]

# ---- 1. order creation
s, d = order("Delta", [APTT])
tc("LAB-001", M, "Doctor places a lab order (APTT)", ok(s), f"{s} {msg(d)}", "Order created with LAB-YYYY-NNNNNN number", severity="Critical")
oid = d["data"] if ok(s) and not isinstance(d["data"], dict) else d["data"]["id"]
o = detail(oid)
tc("LAB-002", M, "Order has correct patient mapping, number and Ordered status", o["patientUhid"] == "PRX-2026-000005" and o["orderNumber"].startswith("LAB-") and o["status"] == 1,
   f"uhid={o['patientUhid']} no={o['orderNumber']} status={o['status']}", "Delta/PRX-2026-000005, LAB-, Ordered", severity="Critical")
item = o["items"][0]
s, d = order("Delta", [])
tc("LAB-003", M, "Order with no tests rejected", s in (400, 422), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = order("Delta", [999999])
tc("LAB-004", M, "Order with unknown test rejected", s in (400, 404), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = order("Delta", [APTT, APTT])
tc("LAB-005", M, "Same test twice in ONE order is blocked (duplicate prevention)", rej(s) or len(detail(d["data"] if not isinstance(d["data"], dict) else d["data"]["id"])["items"]) == 1,
   f"{s} {msg(d)}", "Rejected or de-duplicated to one line", severity="Medium")
dup_open_id = None
s, d = order("Delta", [APTT])
tc("LAB-006", M, "Same test re-ordered for the same patient while an identical order is still open is warned/blocked", rej(s), f"{s} {msg(d)}", "Duplicate open order prevented or warned",
   "Order APTT twice for the same patient", severity="Medium", bug="No duplicate-test protection: identical open orders for the same patient and test are accepted silently.")
s, d = order("Delta", [APTT], who=nur)
tc("LAB-007", M, "Nurse role cannot place lab orders (RBAC)", s == 403, f"{s}", "403", severity="Medium")
s, d = order("Delta", [APTT], who=bil)
tc("LAB-008", M, "Billing role cannot place lab orders (RBAC)", s == 403, f"{s}", "403", severity="Medium")

# ---- 3. collection
wl = lab.get("/lab/collection/worklist")
tc("LAB-010", M, "Order appears in the sample-collection worklist", wl[0] == 200 and oid in [x.get("labOrderId", x.get("orderId", x.get("id"))) for x in (wl[1]["data"] if isinstance(wl[1]["data"], list) else [])] or oid in json.dumps(wl[1]["data"]), f"{wl[0]} rows={len(wl[1]['data']) if wl[0]==200 and isinstance(wl[1]['data'], list) else ''}", "Listed", severity="High")
s, d = sample_for(oid, [item["id"]], 999999)
tc("LAB-011", M, "Collect with wrong/unknown sample type rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = sample_for(oid, [999999], SAMPLE)
tc("LAB-012", M, "Collect an item that is not on this order rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
# result before collection
s, d = lab.post(f"/lab/order-items/{item['id']}/result", {"values": [{"labTestParameterId": PARAM, "value": "30"}], "notes": None})
tc("LAB-013", M, "Result entry BEFORE sample collection/receipt is blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = sample_for(oid, [item["id"]], SAMPLE)
tc("LAB-014", M, "Collect sample (item -> Collected, sample identifier generated)", ok(s), f"{s} {msg(d)} -> {json.dumps(d.get('data'))[:160]}", "Sample created with number/barcode", severity="Critical")
sample = d["data"] if ok(s) else None
sid = sample["id"] if isinstance(sample, dict) else sample
barcode = (sample or {}).get("sampleNumber") or (sample or {}).get("barcode") if isinstance(sample, dict) else None
tc("LAB-015", M, "Sample has a unique, scannable identifier (barcode/sample number)", bool(barcode), f"identifier={barcode}", "Sample number/barcode present", severity="High")
s, d = sample_for(oid, [item["id"]], SAMPLE)
tc("LAB-016", M, "Collecting the same item twice is blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = lab.post(f"/lab/order-items/{item['id']}/result", {"values": [{"labTestParameterId": PARAM, "value": "30"}], "notes": None})
tc("LAB-017", M, "Result entry after collection but BEFORE lab receipt is blocked", rej(s), f"{s} {msg(d)}", "Rejected (sample not received)", severity="High")

# rejection and recollect
s, d = lab.post(f"/lab/collection/samples/{sid}/reject", {"reason": ""})
tc("LAB-018", M, "Sample rejection without a reason rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = lab.post(f"/lab/collection/samples/{sid}/reject", {"reason": "Haemolysed (QA)"})
tc("LAB-019", M, "Reject sample with reason", ok(s), f"{s} {msg(d)}", "Rejected", severity="High")
o = detail(oid)
tc("LAB-020", M, "Rejected sample sends the test back to Ordered for recollection", o["items"][0]["status"] == 1, f"item status={o['items'][0]['status']}", "Ordered (1)", severity="High")
s, d = lab.post(f"/lab/collection/samples/{sid}/receive")
tc("LAB-021", M, "A rejected sample cannot then be received", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = sample_for(oid, [item["id"]], SAMPLE)
tc("LAB-022", M, "Recollect sample after rejection", ok(s), f"{s} {msg(d)}", "New sample", severity="High")
sid2 = d["data"]["id"] if ok(s) and isinstance(d["data"], dict) else (d["data"] if ok(s) else None)
s, d = nur.post(f"/lab/collection/samples/{sid2}/receive")
tc("LAB-023", M, "Nurse cannot receive samples in lab (RBAC)", s == 403, f"{s}", "403", severity="Medium")
s, d = lab.post(f"/lab/collection/samples/{sid2}/receive")
tc("LAB-024", M, "Lab receives the sample", ok(s), f"{s} {msg(d)}", "Received", severity="High")

# ---- 4. result entry & flags
def enter(value, who=None, notes=None):
    return (who or lab).post(f"/lab/order-items/{item['id']}/result", {"values": [{"labTestParameterId": PARAM, "value": value}], "notes": notes})

s, d = enter("abc")
tc("LAB-025", M, "Non-numeric value for a numeric parameter rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = enter("-5")
tc("LAB-026", M, "Negative APTT (-5 s) rejected as impossible", rej(s), f"{s} {msg(d)}", "Rejected or flagged", severity="Medium")
s, d = enter("")
tc("LAB-027", M, "Empty result value rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = enter("50")
tc("LAB-028", M, "Enter an abnormal result (50 s, range 25-35)", ok(s), f"{s} {msg(d)}", "Saved", severity="High")
unb0 = admin.get("/lab/orders/unbilled-report")[1]["data"]
TD = datetime.date.today().isoformat()
unb = admin.get(f"/lab/orders/unbilled-report?fromDate={TD}&toDate={TD}")[1]["data"]
tc("LAB-062", M, "Unbilled report with no date range returns the current unbilled items (default window)", len(unb0) > 0, f"no-date call returned {len(unb0)} rows while date-ranged call returned {len(unb)}", "Same unbilled items returned by default", severity="Low")
tc("LAB-009", M, "Item with a result entered but NO bill link is reported as unbilled", any(oid == x.get("labOrderId", x.get("orderId")) or o["orderNumber"] == x.get("orderNumber") for x in unb) if isinstance(unb, list) else False,
   f"{len(unb) if isinstance(unb, list) else unb} unbilled rows", "Listed in Unbilled Orders report", severity="High")
tc("LAB-061", M, "Laboratory processing is gated by billing (test cannot be processed until billed)", False,
   "Sample collected, received, result entered and approved with NO bill line for the test; nothing blocked it (documented as deliberate/advisory only)",
   "Test must be billed/paid before sample collection or result release", "Place lab order -> never bill -> collect -> receive -> result", severity="High",
   bug="Revenue leakage: lab tests are processed without billing. Lab tests have no billing-service mapping (0 of 69) and OP-bill lines for doctor orders are zero-rate stubs.")
r = lab.get(f"/lab/order-items/{item['id']}/result")[1]["data"]
flag = json.dumps(r)
print("RESULT RAW:", flag[:500])
vals = r.get("values") or []
f0 = vals[0].get("flag") if vals else None
tc("LAB-029", M, "50 s auto-flagged HIGH (not critical)", f0 == 3, f"flag={f0}", "High (3)", severity="Critical")
s, d = enter("30")
r = lab.get(f"/lab/order-items/{item['id']}/result")[1]["data"]
f0 = (r.get("values") or [{}])[0].get("flag")
tc("LAB-030", M, "Result modified before verification: 30 s now NORMAL (re-flagged)", ok(s) and f0 == 1, f"{s} flag={f0}", "Normal (1)", severity="High")
s, d = enter("95")
r = lab.get(f"/lab/order-items/{item['id']}/result")[1]["data"]
f0 = (r.get("values") or [{}])[0].get("flag")
alerts = patho.get("/lab/critical-alerts")[1]["data"]
mine = [a for a in alerts if a.get("labOrderId") == oid or a.get("orderNumber") == o["orderNumber"] or oid == a.get("orderId")] if isinstance(alerts, list) else []
tc("LAB-031", M, "95 s flagged CRITICAL and a critical alert is raised", f0 == 4 and len(mine) >= 1, f"flag={f0} alerts_for_order={len(mine)} total_alerts={len(alerts) if isinstance(alerts, list) else ''}", "Critical flag + alert", severity="Critical")
if mine:
    s, d = patho.post(f"/lab/critical-alerts/{mine[0]['id']}/acknowledge", {"notes": "QA informed doctor"})
    tc("LAB-032", M, "Acknowledge critical alert", ok(s), f"{s} {msg(d)}", "Acknowledged", severity="High")
    s, d = patho.post(f"/lab/critical-alerts/{mine[0]['id']}/acknowledge", {"notes": "again"})
    tc("LAB-033", M, "Acknowledging an already acknowledged alert blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Low")
enter("30")

# ---- 5. verify / approve
s, d = patho.post(f"/lab/order-items/{item['id']}/result/approve")
tc("LAB-034", M, "Approve BEFORE verify is blocked (workflow order)", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = nur.post(f"/lab/order-items/{item['id']}/result/verify")
tc("LAB-035", M, "Nurse cannot verify results (RBAC)", s == 403, f"{s}", "403", severity="High")
s, d = lab.post(f"/lab/order-items/{item['id']}/result/verify")
tc("LAB-036", M, "Lab technician cannot verify results (verification is pathologist-only)", s == 403, f"{s} {msg(d)}", "403", severity="High")
s, d = patho.post(f"/lab/order-items/{item['id']}/result/verify")
tc("LAB-060", M, "Pathologist verifies result", ok(s), f"{s} {msg(d)}", "Verified", severity="High")
s, d = enter("31")
tc("LAB-037", M, "Result CANNOT be changed after verification without a controlled path", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = lab.get(f"/lab/orders/{oid}/report")
tc("LAB-038", M, "Report is NOT released before approval", not (s == 200 and d.get("binary")), f"{s} {d.get('contentType', msg(d))}", "Blocked until approved", severity="Critical")
s, d = bil.post(f"/lab/order-items/{item['id']}/result/approve")
tc("LAB-039", M, "Billing role cannot approve results (RBAC)", s == 403, f"{s}", "403", severity="Critical")
s, d = lab.post(f"/lab/order-items/{item['id']}/result/approve")
tc("LAB-040", M, "Technician cannot give final approval (pathologist-only)", s == 403, f"{s} {msg(d)}", "403", severity="High")
s, d = patho.post(f"/lab/order-items/{item['id']}/result/approve")
tc("LAB-041", M, "Pathologist approves result", ok(s), f"{s} {msg(d)}", "Approved", severity="Critical")
o = detail(oid)
tc("LAB-042", M, "Order status rolls up to Approved/Completed", o["status"] == 3, f"order status={o['status']} item={o['items'][0]['status']}", "Order completed", severity="High")
s, d = patho.post(f"/lab/order-items/{item['id']}/result/approve")
tc("LAB-043", M, "Approving twice blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = lab.get(f"/lab/orders/{oid}/report")
tc("LAB-044", M, "Report PDF available after approval", s == 200 and d.get("binary") and d["head"] == b"%PDF-", f"{s} {d.get('contentType')}", "Valid PDF", severity="High")
s, d = admin.post(f"/lab/orders/{oid}/items/{item['id']}/cancel")
tc("LAB-045", M, "Cancelling an APPROVED test is blocked (result already released)", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = admin.post(f"/lab/orders/{oid}/cancel", {"reason": "QA cancel approved order"})
tc("LAB-046", M, "Cancelling a fully approved order is blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")

# ---- 6. EMR availability, patient mapping
s, d = doc.get(f"/emr/patients/{P['Delta']}/investigations")
tc("LAB-047", M, "Approved result is visible in the patient's EMR investigations", s == 200 and "Activated Partial" in json.dumps(d["data"]) or "APTT" in json.dumps(d["data"]), f"{s} {json.dumps(d['data'])[:160]}", "APTT present", severity="High")
s, d = doc.get(f"/emr/patients/{P['Golf']}/investigations")
tc("LAB-048", M, "Result does NOT appear in another patient's EMR (patient mapping)", s == 200 and "APTT" not in json.dumps(d["data"]) and "Activated Partial" not in json.dumps(d["data"]), f"{s}", "Absent", severity="Critical")
s, d = doc.get(f"/emr/patients/{P['Delta']}/results-trend?testId={APTT}")
tc("LAB-049", M, "Results trend for the test loads", s == 200, f"{s}", "200", severity="Low")

# ---- 7. order cancellation paths
s, d = order("Echo", [APTT]); oid3 = d["data"] if not isinstance(d["data"], dict) else d["data"]["id"]
s, d = admin.post(f"/lab/orders/{oid3}/cancel", {"reason": ""})
tc("LAB-050", M, "Cancel order without reason rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = admin.post(f"/lab/orders/{oid3}/cancel", {"reason": "QA cancel before collection"})
tc("LAB-051", M, "Cancel order before collection", ok(s), f"{s} {msg(d)}", "Cancelled", severity="High")
o3 = detail(oid3)
tc("LAB-052", M, "Cancelled order record retained with Cancelled status", o3["status"] == 4, f"status={o3['status']}", "Cancelled", severity="Critical")
s, d = sample_for(oid3, [o3["items"][0]["id"]], SAMPLE)
tc("LAB-053", M, "Sample collection on a CANCELLED order blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
# cancel after collection
s, d = order("Echo", [APTT]); oid4 = d["data"] if not isinstance(d["data"], dict) else d["data"]["id"]; it4 = detail(oid4)["items"][0]
sample_for(oid4, [it4["id"]], SAMPLE)
s, d = admin.post(f"/lab/orders/{oid4}/cancel", {"reason": "QA cancel after sample collected"})
# profile / panel ordering
profs = admin.get("/lab/profiles")[1]["data"]
if profs:
    s, d = doc.post("/lab/orders", {"patientId": P["Foxtrot"], "opVisitId": None, "emergencyVisitId": None, "clinicalOrderId": None, "orderedByDoctorId": DOC, "priority": 1, "clinicalNotes": None,
                                    "items": [{"labTestId": None, "testProfileId": profs[0]["id"]}]})
    tc("LAB-055", M, "Order a test profile (expands to its tests)", ok(s), f"{s} {msg(d)}", "Order with profile tests", severity="Medium")
# reports and RBAC
s, d = lab.get("/lab/orders/tat-report"); tc("LAB-056", M, "TAT report loads", s == 200, f"{s}", "200", severity="Low")
s, d = bil.get("/lab/orders"); tc("LAB-057", M, "Billing role cannot browse lab orders/results (data protection)", s == 403, f"{s}", "403", severity="High")
s, d = rec.get("/lab/orders"); tc("LAB-058", M, "Reception role cannot browse lab orders/results (data protection)", s == 403, f"{s}", "403", severity="High")
s, d = lab.get(f"/lab/orders/{oid}/report")
tc("LAB-059", M, "Report content belongs to the right patient (header check on order detail)", detail(oid)["patientName"].endswith("Delta"), detail(oid)["patientName"], "QATEST Delta", severity="Critical")
summary()
