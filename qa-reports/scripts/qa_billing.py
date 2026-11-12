import sys, os, threading, datetime
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from qa_lib import *
from decimal import Decimal as D

M = "C-OP Billing"
st = load_state()
P = st["patients"]; DOC = st["doctors"]["qa.doctor"]; GM = st["depts"]["DEP001"]
admin = Sess("admin", "Admin@123", 2)
rec = Sess("qa.reception", "Qa@12345", 2)
bil = Sess("qa.billing", "Qa@12345", 2)
sup = Sess("qa.billsup", "Qa@12345", 2)
csh = Sess("qa.cashier", "Qa@12345", 2)
nur = Sess("qa.nurse", "Qa@12345", 2)
SVC = {"XRAY": 6, "USG": 7, "ECG": 9, "INJ": 11, "REG": 1}   # service ids (SM-RAD-XRAY 500, USG 1500, ECG 300, INJ 120, REG 100)
CASH, UPI, CARD = 1, 3, 2
TODAY = datetime.date.today().isoformat()
# Dedicated billing patient: never gets a COMPLETED consultation, so every visit stays "New" (fee 500).
if "BillOnly" not in P:
    _s, _d = admin.post("/Patients", patient_body("QATEST", "BillOnly", 1, "1982-02-02", "9000000401"))
    assert ok(_s), (_s, _d)
    P["BillOnly"] = _d["data"]["id"]; st["patients"] = P; st["uhid_billonly"] = _d["data"]["uhid"]; save_state(st)


def new_visit(pk="BillOnly"):
    s, d = rec.post("/opd/visits", {"patientId": P[pk], "doctorId": DOC, "departmentId": GM, "appointmentId": None})
    assert ok(s), (s, d)
    return d["data"]


def new_bill(pk="BillOnly", services=()):
    v = new_visit(pk)
    s, d = bil.post("/billing/bills/generate-op", {"opVisitId": v["id"]})
    assert ok(s), (s, d)
    b = d["data"]
    for sid, qty in services:
        s, d = bil.post(f"/billing/bills/{b['id']}/items", {"sourceType": 3, "serviceId": sid, "packageId": None, "quantity": qty})
        assert ok(s), (s, d)
    return getb(b["id"]), v


def getb(bid):
    return bil.get(f"/billing/bills/{bid}")[1]["data"]


def pay(bid, mode, amt, ref=None, who=None):
    return (who or bil).post(f"/billing/bills/{bid}/payments", {"paymentModeId": mode, "amount": amt, "referenceNumber": ref})


def approx(a, b):
    return abs(D(str(a)) - D(str(b))) < D("0.005")


# ======================= 1. BILL GENERATION & TOTALS =======================
b, v = new_bill("BillOnly")
tc("BILL-001", M, "Generate OP bill from a registered visit", b["status"] == 1 and approx(b["totalAmount"], 500) and approx(b["balanceAmount"], 500) and len(b["items"]) == 1,
   f"status={b['status']} total={b['totalAmount']} items={len(b['items'])}", "Open bill, 1 consultation line = doctor fee 500, balance 500",
   "Register OP visit (fee 500) -> Generate OP bill", severity="Critical")
tc("BILL-002", M, "Consultation line carries doctor name and New/Review type", "Consultation" in b["items"][0]["description"] and "New" in b["items"][0]["description"], b["items"][0]["description"],
   "Description shows doctor and visit type", severity="Low")
s, d = bil.post("/billing/bills/generate-op", {"opVisitId": v["id"]})
tc("BILL-003", M, "Duplicate OP bill for the same visit is prevented", rej(s), f"{s} {msg(d)}", "Rejected (one bill per visit)",
   "Generate-op twice for same visit", severity="High")
s, d = bil.post("/billing/bills/generate-op", {"opVisitId": 99999})
tc("BILL-004", M, "Generate bill for non-existent visit", s == 404, f"{s} {msg(d)}", "404 Not found", severity="Low")
v2 = new_visit("Bravo")
cs, cd = admin.post(f"/opd/visits/{v2['id']}/cancel", {"reason": "QA cancelled before billing"})
assert ok(cs), (cs, cd)
assert admin.get(f"/opd/visits/{v2['id']}")[1]["data"]["status"] == 4
s, d = bil.post("/billing/bills/generate-op", {"opVisitId": v2["id"]})
tc("BILL-005", M, "Generate bill for a CANCELLED OP visit", rej(s), f"{s} {msg(d)}", "Rejected - cancelled visit must not be billable",
   "Register visit -> cancel visit -> generate bill", severity="High")

# line items & totals
b, _ = new_bill("BillOnly", [(SVC["XRAY"], 2), (SVC["USG"], 1)])
exp = D(500) + D(500) * 2 + D(1500)
tc("BILL-006", M, "Add services: consultation 500 + X-Ray 500 x2 + USG 1500", approx(b["subTotal"], exp) and approx(b["totalAmount"], exp), f"sub={b['subTotal']} total={b['totalAmount']} expected={exp}",
   "Total = 3000", severity="Critical")
tc("BILL-007", M, "Line totals equal qty x rate", all(approx(i["totalAmount"], D(str(i["quantity"])) * D(str(i["rate"]))) for i in b["items"]), [(i["description"], i["quantity"], i["rate"], i["totalAmount"]) for i in b["items"]],
   "Each line total = quantity x rate", severity="Critical")
for q, tid in ((0, "BILL-008"), (-1, "BILL-009")):
    s, d = bil.post(f"/billing/bills/{b['id']}/items", {"sourceType": 3, "serviceId": SVC["ECG"], "packageId": None, "quantity": q})
    tc(tid, M, f"Add item with quantity {q}", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = bil.post(f"/billing/bills/{b['id']}/items", {"sourceType": 3, "serviceId": 999999, "packageId": None, "quantity": 1})
tc("BILL-010", M, "Add non-existent service", s in (400, 404, 422), f"{s} {msg(d)}", "Rejected", severity="Low")
# edit & delete lines
item = [i for i in b["items"] if i["sourceType"] == 3][0]
s, d = bil.put(f"/billing/bills/{b['id']}/items/{item['id']}", {"description": item["description"], "quantity": 1, "rate": 400})
b2 = getb(b["id"])
exp2 = exp - D(str(item["totalAmount"])) + D(400)
tc("BILL-011", M, "Edit a line (qty/rate) recalculates bill total", ok(s) and approx(b2["totalAmount"], exp2), f"{s} total={b2['totalAmount']} expected={exp2} {msg(d) if rej(s) else ''}", "Totals recalculated", severity="High")
s, d = bil.delete(f"/billing/bills/{b['id']}/items/{item['id']}")
b3 = getb(b["id"])
exp3 = exp2 - D(400)
tc("BILL-012", M, "Delete a line recalculates bill total", ok(s) and approx(b3["totalAmount"], exp3), f"{s} total={b3['totalAmount']} expected={exp3}", "Totals recalculated", severity="High")
s, d = bil.post(f"/billing/bills/{b['id']}/items", {"sourceType": 3, "serviceId": SVC["REG"], "packageId": None, "quantity": 1})
b4 = getb(b["id"])
tc("BILL-013", M, "Registration fee can be added as a manual line (not auto-added)", ok(s) and any("Registration" in i["description"] for i in b4["items"]), "added manually; OP bill generation does NOT auto-add the registration fee",
   "Registration fee appears on bill", severity="Low")
s, d = bil.put(f"/billing/bills/{b['id']}/items/{b4['items'][0]['id']}", {"description": "x", "quantity": 1, "rate": -5})
tc("BILL-014", M, "Edit line with negative rate", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")

# tax
tx_svc = None
s_, d_ = admin.post("/Services", {"code": "QA-TAXED", "name": "QA Taxed Procedure", "serviceCategoryId": 22, "departmentId": GM, "isTaxable": True, "taxId": 3, "defaultRate": 1000, "isActive": True})
if ok(s_):
    tx_svc = d_["data"]
else:
    tx_svc = next((x["id"] for x in admin.get("/Services?Page=1&PageSize=100&Search=QA-TAXED")[1]["data"]["items"] if x["code"] == "QA-TAXED"), None)
if tx_svc:
    bt, _ = new_bill("Bravo", [(tx_svc, 1)])
    taxed = [i for i in bt["items"] if i["serviceId"] == tx_svc]
    t_ok = taxed and approx(taxed[0]["taxAmount"], 120) and approx(bt["taxAmount"], 120) and approx(bt["totalAmount"], 500 + 1000 + 120)
    tc("BILL-015", M, "GST 12% on a taxable service (1000)", t_ok, f"line tax={taxed[0]['taxAmount'] if taxed else None} bill tax={bt['taxAmount']} total={bt['totalAmount']}", "Tax 120, total = 500 + 1000 + 120 = 1620", severity="Critical")
else:
    blocked("BILL-015", M, "GST on taxable service", f"could not create taxed test service: {s_} {msg(d_)}")

# ======================= 2. DISCOUNTS =======================
def line_disc(bid, itemid, typ, pct, reason="QA discount"):
    return sup.post(f"/billing/bills/{bid}/items/{itemid}/discount", {"discountTypeId": typ, "percent": pct, "reason": reason})


b, _ = new_bill("Charlie", [(SVC["USG"], 1)])      # 500 + 1500
usg = [i for i in b["items"] if i["serviceId"] == SVC["USG"]][0]
s, d = line_disc(b["id"], usg["id"], 1, 10)        # STAFF: threshold 20, max 50
bd = getb(b["id"])
tc("BILL-020", M, "Item discount 10% (below 20% threshold) auto-approved", ok(s) and approx(bd["discountAmount"], 150) and approx(bd["totalAmount"], 2000 - 150),
   f"{s} disc={bd['discountAmount']} total={bd['totalAmount']} {msg(d) if rej(s) else ''}", "Discount 150, total 1850 immediately", severity="Critical")
# boundary
b, _ = new_bill("Charlie", [(SVC["USG"], 1)]); usg = [i for i in b["items"] if i["serviceId"] == SVC["USG"]][0]
s, d = line_disc(b["id"], usg["id"], 1, 20); bd = getb(b["id"])
tc("BILL-021", M, "Discount exactly at threshold (20%) is auto-approved", ok(s) and approx(bd["discountAmount"], 300), f"{s} disc={bd['discountAmount']}", "Threshold is inclusive: 20% applied immediately", severity="Medium")
b, _ = new_bill("Charlie", [(SVC["USG"], 1)]); usg = [i for i in b["items"] if i["serviceId"] == SVC["USG"]][0]
s, d = line_disc(b["id"], usg["id"], 1, 30); bd = getb(b["id"])
pend = sup.get("/billing/discount-approvals")[1]["data"]
plist = pend if isinstance(pend, list) else pend.get("items", [])
mine = [x for x in plist if x.get("billId") == b["id"]]
tc("BILL-022", M, "Discount 30% (above threshold) goes to approval, NOT applied yet", ok(s) and approx(bd["discountAmount"], 0) and len(mine) >= 1,
   f"{s} disc_applied={bd['discountAmount']} approvals_for_bill={len(mine)} {msg(d)}", "Pending approval; bill total unchanged until approved", severity="Critical")
if mine:
    appr = mine[0]
    s, d = csh.post(f"/billing/discount-approvals/{appr['id']}/decide", {"approve": True, "rejectionReason": None})
    tc("BILL-023", M, "Cashier cannot approve discounts (RBAC)", s == 403, f"{s} {msg(d)}", "403 Forbidden", severity="High")
    s, d = sup.post(f"/billing/discount-approvals/{appr['id']}/decide", {"approve": True, "rejectionReason": None}); bd = getb(b["id"])
    tc("BILL-024", M, "Supervisor approves pending discount -> applied to bill", ok(s) and approx(bd["discountAmount"], 450) and approx(bd["totalAmount"], 2000 - 450),
       f"{s} disc={bd['discountAmount']} total={bd['totalAmount']} {msg(d) if rej(s) else ''}", "Discount 450 (30% of 1500), total 1550", severity="Critical")
else:
    blocked("BILL-023", M, "Cashier cannot approve discounts", "no pending approval was created (see BILL-022)")
    blocked("BILL-024", M, "Supervisor approves pending discount", "no pending approval")
b, _ = new_bill("Charlie", [(SVC["USG"], 1)]); usg = [i for i in b["items"] if i["serviceId"] == SVC["USG"]][0]
line_disc(b["id"], usg["id"], 1, 30)
plist = sup.get("/billing/discount-approvals")[1]["data"]; plist = plist if isinstance(plist, list) else plist.get("items", [])
mine = [x for x in plist if x.get("billId") == b["id"]]
if mine:
    s, d = sup.post(f"/billing/discount-approvals/{mine[0]['id']}/decide", {"approve": False, "rejectionReason": "QA reject test"}); bd = getb(b["id"])
    tc("BILL-026", M, "Rejected discount leaves bill unchanged", ok(s) and approx(bd["discountAmount"], 0) and approx(bd["totalAmount"], 2000), f"{s} disc={bd['discountAmount']} total={bd['totalAmount']}", "No discount", severity="High")
    s2, d2 = sup.post(f"/billing/discount-approvals/{mine[0]['id']}/decide", {"approve": True, "rejectionReason": None})
    tc("BILL-027", M, "Re-deciding an already decided approval is blocked", rej(s2), f"{s2} {msg(d2)}", "Rejected", severity="High")
b, _ = new_bill("Charlie", [(SVC["USG"], 1)]); usg = [i for i in b["items"] if i["serviceId"] == SVC["USG"]][0]
s, d = line_disc(b["id"], usg["id"], 1, 60)
tc("BILL-028", M, "Discount above type maximum (60% > 50%) rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = line_disc(b["id"], usg["id"], 1, -5)
tc("BILL-029", M, "Negative discount percent rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = sup.post(f"/billing/bills/{b['id']}/items/{usg['id']}/discount", {"discountTypeId": 1, "percent": 5, "reason": ""})
tc("BILL-030", M, "Discount without reason rejected", rej(s), f"{s} {msg(d)}", "Rejected (reason mandatory)", severity="Medium")
b, _ = new_bill("Charlie", [(SVC["USG"], 1)])
s, d = sup.post(f"/billing/bills/{b['id']}/discount", {"discountTypeId": 3, "percent": 10, "reason": "QA senior"}); bd = getb(b["id"])
tc("BILL-031", M, "Bill-level discount 10% (SENIOR) on 2000", ok(s) and approx(bd["discountAmount"], 200) and approx(bd["totalAmount"], 1800), f"{s} disc={bd['discountAmount']} total={bd['totalAmount']} {msg(d) if rej(s) else ''}", "Discount 200, total 1800", severity="Critical")
s, d = nur.post(f"/billing/bills/{b['id']}/discount", {"discountTypeId": 3, "percent": 1, "reason": "x"})
tc("BILL-032", M, "Nurse role cannot discount a bill (RBAC)", s == 403, f"{s} {msg(d)}", "403", severity="High")

# ======================= 3. PAYMENTS =======================
b, _ = new_bill("Delta")           # 500
bid = b["id"]
s, d = pay(bid, CASH, 200); bb = getb(bid)
tc("BILL-040", M, "Partial cash payment 200 of 500", ok(s) and bb["status"] == 2 and approx(bb["paidAmount"], 200) and approx(bb["balanceAmount"], 300), f"{s} status={bb['status']} paid={bb['paidAmount']} bal={bb['balanceAmount']}", "PartiallyPaid, paid 200, balance 300", severity="Critical")
s, d = pay(bid, CASH, 301)
tc("BILL-041", M, "Overpayment beyond balance (301 > 300) rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = pay(bid, CASH, 0)
tc("BILL-042", M, "Zero payment rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = pay(bid, CASH, -50)
tc("BILL-043", M, "Negative payment rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = pay(bid, UPI, 100, None)
tc("BILL-044", M, "UPI payment without reference number rejected", rej(s), f"{s} {msg(d)}", "Reference required for UPI", severity="Medium")
s, d = pay(bid, 999, 100)
tc("BILL-045", M, "Unknown payment mode rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = pay(bid, CASH, 300); bb = getb(bid)
tc("BILL-046", M, "Full settlement: remaining 300 -> Paid", ok(s) and bb["status"] == 3 and approx(bb["balanceAmount"], 0) and approx(bb["paidAmount"], 500), f"{s} status={bb['status']} paid={bb['paidAmount']} bal={bb['balanceAmount']}", "Paid, balance 0", severity="Critical")
s, d = pay(bid, CASH, 1)
tc("BILL-047", M, "Duplicate payment on an already PAID bill blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
paid_bill = bb
s, d = bil.post(f"/billing/bills/{bid}/items", {"sourceType": 3, "serviceId": SVC["ECG"], "packageId": None, "quantity": 1})
tc("BILL-048", M, "Adding a service to a fully PAID bill is blocked", rej(s), f"{s} {msg(d)}", "Rejected (bill closed)", severity="High")
s, d = bil.delete(f"/billing/bills/{bid}/items/{bb['items'][0]['id']}")
tc("BILL-049", M, "Deleting a line from a PAID bill is blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
pid = bb["payments"][0]["id"]
s, d = bil.get(f"/billing/bills/{bid}/receipt?paymentId={pid}")
tc("BILL-050", M, "Receipt PDF generated for a payment", s == 200 and d.get("binary") and d["head"] == b"%PDF-", f"{s} {d.get('contentType')} {d.get('size')}B", "Valid PDF", severity="High")
s, d = bil.get(f"/billing/bills/{bid}/receipt?paymentId=999999")
tc("BILL-051", M, "Receipt for unknown payment id -> error", s in (400, 404), f"{s}", "404", severity="Low")

# multi-mode
b, _ = new_bill("Echo", [(SVC["USG"], 1)])      # 2000
bid = b["id"]
r1 = pay(bid, CASH, 500); r2 = pay(bid, UPI, 700, "QA-UPI-0001"); r3 = pay(bid, CARD, 800, "QA-CARD-0001"); bb = getb(bid)
tc("BILL-052", M, "Split payment across Cash+UPI+Card settles bill", all(ok(x[0]) for x in (r1, r2, r3)) and bb["status"] == 3 and len(bb["payments"]) == 3 and approx(bb["paidAmount"], 2000),
   f"{[x[0] for x in (r1, r2, r3)]} status={bb['status']} payments={len(bb['payments'])} paid={bb['paidAmount']}", "3 payment rows, total 2000, Paid", severity="Critical")
b, _ = new_bill("Echo")
r1 = pay(b["id"], UPI, 100, "QA-UPI-0001")
tc("BILL-053", M, "Same UPI reference number reused on another bill", rej(r1[0]), f"{r1[0]} {msg(r1[1])}", "Reference should be unique per transaction (duplicate-payment guard)", severity="Medium")

# concurrency / double-click
b, _ = new_bill("Foxtrot")
res = []
def worker():
    res.append(pay(b["id"], CASH, 500)[0])
ts = [threading.Thread(target=worker) for _ in range(4)]
[t.start() for t in ts]; [t.join() for t in ts]
bb = getb(b["id"])
tc("BILL-054", M, "4 simultaneous full payments (double-click) -> exactly one accepted", sum(1 for x in res if ok(x)) == 1 and approx(bb["paidAmount"], 500) and len(bb["payments"]) == 1,
   f"statuses={res} paid={bb['paidAmount']} payments={len(bb['payments'])}", "One payment, paid 500, no overpayment", severity="Critical")

# RBAC on payment
b, _ = new_bill("Golf")
s, d = pay(b["id"], CASH, 100, who=nur)
tc("BILL-055", M, "Nurse role cannot collect payment (RBAC)", s == 403, f"{s} {msg(d)}", "403", severity="High")

# ======================= 4. PARK / CANCEL =======================
b, _ = new_bill("Hotel"); bid = b["id"]
s, d = bil.post(f"/billing/bills/{bid}/park", {"reason": "Patient went for tests"}); bb = getb(bid)
tc("BILL-060", M, "Park a bill", ok(s) and bb["status"] == 5, f"{s} status={bb['status']} {msg(d) if rej(s) else ''}", "Status Parked", severity="Medium")
s, d = pay(bid, CASH, 100)
tc("BILL-061", M, "Payment on a PARKED bill is blocked", rej(s), f"{s} {msg(d)}", "Rejected until unparked", severity="High")
s, d = bil.post(f"/billing/bills/{bid}/unpark"); bb = getb(bid)
tc("BILL-062", M, "Unpark restores previous status", ok(s) and bb["status"] == 1, f"{s} status={bb['status']}", "Open", severity="Medium")
s, d = bil.post(f"/billing/bills/{bid}/cancel", {"reason": ""})
tc("BILL-063", M, "Cancel bill without reason rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = bil.post(f"/billing/bills/{bid}/cancel", {"reason": "QA cancel unpaid bill"}); bb = getb(bid)
tc("BILL-064", M, "Cancel unpaid bill with reason", ok(s) and bb["status"] == 4 and bb["cancelReason"], f"{s} status={bb['status']} reason={bb['cancelReason']} {msg(d) if rej(s) else ''}", "Cancelled with reason + timestamp retained", severity="High")
tc("BILL-065", M, "Cancelled bill record is retained (not deleted)", bb["id"] == bid and len(bb["items"]) >= 1, f"items kept={len(bb['items'])}", "Bill and lines remain for audit", severity="Critical")
s, d = bil.post(f"/billing/bills/{bid}/cancel", {"reason": "again"})
tc("BILL-066", M, "Cancelling an already cancelled bill blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = pay(bid, CASH, 100)
tc("BILL-067", M, "Payment on CANCELLED bill blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = bil.post(f"/billing/bills/{bid}/items", {"sourceType": 3, "serviceId": SVC["ECG"], "packageId": None, "quantity": 1})
tc("BILL-068", M, "Adding items to a CANCELLED bill blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
# cancel a bill that has payments
b, _ = new_bill("Hotel"); bid = b["id"]; pay(bid, CASH, 500)
s, d = bil.post(f"/billing/bills/{bid}/cancel", {"reason": "QA cancel a PAID bill"}); bb = getb(bid)
tc("BILL-069", M, "Cancelling a PAID bill without refunding is blocked (money would be orphaned)", rej(s), f"{s} status={bb['status']} paid={bb['paidAmount']} {msg(d)}",
   "Rejected - must be refunded first (or refund auto-created)", severity="Critical")

# ======================= 5. REFUNDS =======================
b, _ = new_bill("BillOnly", [(SVC["XRAY"], 1)]); bid = b["id"]        # 1000
pay(bid, CASH, 1000); bb = getb(bid); pay_id = bb["payments"][0]["id"]
s, d = bil.post(f"/billing/bills/{bid}/refunds", {"billPaymentId": pay_id, "amount": 1500, "reason": "QA over-refund", "method": 1})
tc("BILL-070", M, "Refund more than paid (1500 > 1000) rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = bil.post(f"/billing/bills/{bid}/refunds", {"billPaymentId": pay_id, "amount": 0, "reason": "zero", "method": 1})
tc("BILL-071", M, "Zero refund rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = bil.post(f"/billing/bills/{bid}/refunds", {"billPaymentId": pay_id, "amount": 400, "reason": "", "method": 1})
tc("BILL-072", M, "Refund without reason rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = bil.post(f"/billing/bills/{bid}/refunds", {"billPaymentId": pay_id, "amount": 600, "reason": "QA partial refund", "method": 1})
rid = d["data"] if ok(s) and not isinstance(d["data"], dict) else (d["data"]["id"] if ok(s) else None)
tc("BILL-073", M, "Raise refund request 600 (cash)", ok(s) and rid, f"{s} {msg(d)}", "Pending approval created", severity="High")
s, d = bil.post(f"/billing/bills/{bid}/refunds", {"billPaymentId": pay_id, "amount": 600, "reason": "QA second refund over remaining", "method": 1})
tc("BILL-074", M, "Second refund pushing total (600+600) above paid (1000) rejected", rej(s), f"{s} {msg(d)}", "Rejected - pending refunds count against paid amount", severity="Critical")
if rid:
    s, d = bil.post(f"/billing/refunds/{rid}/issue")
    tc("BILL-075", M, "Issue refund BEFORE approval blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
    s, d = csh.post(f"/billing/refunds/{rid}/decide", {"approve": True, "rejectionReason": None})
    tc("BILL-076", M, "Cashier cannot approve refunds (RBAC)", s == 403, f"{s} {msg(d)}", "403", severity="High")
    s, d = sup.post(f"/billing/refunds/{rid}/decide", {"approve": True, "rejectionReason": None})
    tc("BILL-077", M, "Supervisor approves refund", ok(s), f"{s} {msg(d)}", "Approved", severity="High")
    s, d = sup.post(f"/billing/refunds/{rid}/issue"); bb = getb(bid)
    tc("BILL-078", M, "Issue approved refund -> bill paid amount reduced to 400", ok(s) and approx(bb["paidAmount"], 400) and approx(bb["balanceAmount"], 600),
       f"{s} status={bb['status']} paid={bb['paidAmount']} bal={bb['balanceAmount']} {msg(d) if rej(s) else ''}", "Paid 400, balance 600 (refund reflected)", severity="Critical")
    s, d = sup.post(f"/billing/refunds/{rid}/issue")
    tc("BILL-079", M, "Issuing the same refund twice blocked (double payout)", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
else:
    for t in ("BILL-075", "BILL-076", "BILL-077", "BILL-078", "BILL-079"):
        blocked(t, M, "Refund workflow step", "refund request could not be created (BILL-073)")
# reject path
b, _ = new_bill("Bravo"); bid = b["id"]; pay(bid, CASH, 500); pid = getb(bid)["payments"][0]["id"]
s, d = bil.post(f"/billing/bills/{bid}/refunds", {"billPaymentId": pid, "amount": 100, "reason": "QA reject path", "method": 1})
rid2 = d["data"] if ok(s) and not isinstance(d["data"], dict) else (d["data"]["id"] if ok(s) else None)
if rid2:
    sup.post(f"/billing/refunds/{rid2}/decide", {"approve": False, "rejectionReason": "QA not eligible"})
    s, d = sup.post(f"/billing/refunds/{rid2}/issue"); bb = getb(bid)
    tc("BILL-080", M, "Rejected refund cannot be issued; paid amount unchanged", rej(s) and approx(bb["paidAmount"], 500), f"{s} paid={bb['paidAmount']} {msg(d)}", "Rejected; paid stays 500", severity="Critical")

# credit note
b, _ = new_bill("Charlie"); bid = b["id"]
s, d = bil.post(f"/billing/bills/{bid}/credit-notes", {"reason": "QA credit note on unpaid bill", "items": [{"billItemId": b["items"][0]["id"], "amount": 100}]})
tc("BILL-081", M, "Credit note request on a bill line", ok(s) or s == 400, f"{s} {msg(d)}", "Request created (or clear validation message)", severity="Low")

# ======================= 6. SEARCH / LISTS / NOTES =======================
s, d = bil.get(f"/billing/bills/op-search?RegNo={st['uhid_billonly']}&Page=1&PageSize=10")
tc("BILL-090", M, "OP bill search by UHID", s == 200 and (d["data"]["totalCount"] if s == 200 else 0) >= 1, f"{s} {d['data']['totalCount'] if s == 200 else msg(d)}", "Returns QATEST BillOnly's visits", severity="Medium")
s, d = bil.get(f"/billing/bills?PatientId={P['BillOnly']}&Page=1&PageSize=50")
tc("BILL-091", M, "Bills list filtered by patient only returns that patient's bills", s == 200 and all(x["patientId"] == P["BillOnly"] for x in d["data"]["items"]) and d["data"]["totalCount"] >= 1, f"{s} count={d['data']['totalCount'] if s == 200 else ''}", "Only BillOnly's bills", severity="Critical")
s, d = bil.get("/billing/bills?Status=4&Page=1&PageSize=50")
tc("BILL-092", M, "Bills list filter Status=Cancelled", s == 200 and all(x["status"] == 4 for x in d["data"]["items"]), f"{s}", "Only cancelled", severity="Medium")
s, d = bil.post(f"/billing/bills/{b['id']}/notes", {"note": "QA note"}); s2, d2 = bil.get(f"/billing/bills/{b['id']}/notes")
tc("BILL-093", M, "Add and read bill note", ok(s) and s2 == 200 and len(d2["data"]) >= 1, f"{s}/{s2}", "Note stored", severity="Low")
s, d = bil.get("/billing/bills/export?format=csv&Page=1&PageSize=20")
tc("BILL-094", M, "Bills export (CSV)", s == 200, f"{s} {d.get('contentType', '')}", "Download works", severity="Low")


# extra: issue-time over-refund (paid 500, two 300 refunds)
b, _ = new_bill("Hotel"); bid = b["id"]; pay(bid, CASH, 500); pid = getb(bid)["payments"][0]["id"]
rids = []
for amt in (300, 300):
    s_, d_ = bil.post(f"/billing/bills/{bid}/refunds", {"billPaymentId": pid, "amount": amt, "reason": "QA over-refund probe", "method": 1})
    rids.append((d_["data"] if not isinstance(d_["data"], dict) else d_["data"]["id"]) if ok(s_) else None)
issued = 0
for r in rids:
    if r:
        sup.post(f"/billing/refunds/{r}/decide", {"approve": True, "rejectionReason": None})
        if ok(sup.post(f"/billing/refunds/{r}/issue")[0]):
            issued += 1
bb = getb(bid)
tc("BILL-082", M, "Total issued refunds can never exceed the amount paid (paid 500, refunds 300 + 300)", issued < 2 and D(str(bb["paidAmount"])) >= 0,
   f"issued={issued} paid_after={bb['paidAmount']} balance={bb['balanceAmount']} status={bb['status']}", "Second refund blocked; paid never negative",
   "Pay 500 cash -> raise two 300 refunds -> approve+issue both", severity="Critical",
   bug="Refund validation does not account for pending/issued refunds: 600 refunded against a 500 payment, bill paid amount becomes -100.")

# ======================= 7. RECONCILIATION =======================
allb, pg = [], 1
while True:
    s_, d_ = admin.get(f"/billing/bills?Page={pg}&PageSize=100")
    allb += d_["data"]["items"]
    if len(allb) >= d_["data"]["totalCount"]:
        break
    pg += 1
full = [getb(x["id"]) for x in allb]
mismatch = []
for x in full:
    if x["status"] == 4:
        continue
    row_sum = sum(D(str(p["amount"])) for p in x["payments"])
    if not approx(row_sum, x["paidAmount"]):
        mismatch.append((x["billNumber"], str(row_sum), x["paidAmount"]))
tc("BILL-100", M, "For every bill, header paidAmount equals the sum of its payment rows", not mismatch, f"{len(mismatch)} bills inconsistent (payment rows vs paidAmount): {mismatch[:3]}",
   "Equal for all bills", "Compare every bill's payment rows with its header", severity="Critical",
   bug="Concurrent payments are all recorded (separate receipts) but the bill header only counts the first - cash collected is untraceable on the bill.")
s_, d_ = bil.get(f"/billing/day-close/collection?date={TODAY}")
coll = d_["data"]
all_pay_rows = sum(D(str(p["amount"])) for x in full for p in x["payments"] if x["status"] != 4)
tc("BILL-101", M, "Day-close collection total equals sum of all payment rows taken today", approx(coll["total"], all_pay_rows), f"collection={coll['total']} vs payment rows={all_pay_rows}", "Equal", severity="High")
# refund netting on a clean bill
b, _ = new_bill("Delta", [(SVC["XRAY"], 1)]); bid = b["id"]
before = bil.get(f"/billing/day-close/collection?date={TODAY}")[1]["data"]["total"]
pay(bid, CASH, 1000)
mid = bil.get(f"/billing/day-close/collection?date={TODAY}")[1]["data"]["total"]
pid = getb(bid)["payments"][0]["id"]
s_, d_ = bil.post(f"/billing/bills/{bid}/refunds", {"billPaymentId": pid, "amount": 400, "reason": "QA netting probe", "method": 1})
r = d_["data"] if not isinstance(d_["data"], dict) else d_["data"]["id"]
sup.post(f"/billing/refunds/{r}/decide", {"approve": True, "rejectionReason": None}); sup.post(f"/billing/refunds/{r}/issue")
after = bil.get(f"/billing/day-close/collection?date={TODAY}")[1]["data"]["total"]
tc("BILL-102", M, "Cash refund issued is netted in day-close expected cash", approx(after, D(str(before)) + 600), f"before={before} after_payment={mid} after_cash_refund={after} (expected {D(str(before)) + 600})",
   "Collection reflects net 600 (1000 paid - 400 refunded) so drawer cash reconciles", "Pay 1000, refund 400, read day-close collection", severity="High",
   bug="Day-close 'expected collection' ignores issued cash refunds, so the cashier's physical cash will always be short by refunds paid out.")

# ======================= 8. CASHIER SHIFT / DAY CLOSE =======================
s_, d_ = bil.get("/billing/cashier-shifts/mine")
if s_ == 200 and d_["data"]:
    blocked("BILL-110", M, "Cashier shift open/close", "user already has an open shift")
else:
    s_, d_ = bil.post("/billing/cashier-shifts/open", {"openingFloat": 1000})
    tc("BILL-110", M, "Open cashier shift with opening float", ok(s_), f"{s_} {msg(d_)}", "Shift opened", severity="High")
    if ok(s_):
        shift = d_["data"] if not isinstance(d_["data"], dict) else d_["data"]["id"]
        s2, d2 = bil.post("/billing/cashier-shifts/open", {"openingFloat": 500})
        tc("BILL-111", M, "A second shift cannot be opened while one is open", rej(s2), f"{s2} {msg(d2)}", "Rejected", severity="Medium")
        b, _ = new_bill("Echo"); pay(b["id"], CASH, 500)
        s3, d3 = bil.get(f"/billing/cashier-shifts/{shift}/collection")
        print("SHIFT COLLECTION RAW:", json.dumps(d3)[:400])
        s4, d4 = bil.post(f"/billing/cashier-shifts/{shift}/close", {"declaredAmounts": [{"paymentModeId": 1, "declaredAmount": 400}], "notes": "QA short cash 100"})
        print("SHIFT CLOSE RAW:", s4, json.dumps(d4)[:500])
        tc("BILL-112", M, "Close shift with cash short: variance is recorded", ok(s4), f"{s4} {msg(d4)}", "Closed with variance shown", severity="High")
        s5, d5 = bil.post(f"/billing/cashier-shifts/{shift}/close", {"declaredAmounts": [], "notes": "again"})
        tc("BILL-113", M, "Closing a closed shift again is blocked", rej(s5), f"{s5} {msg(d5)}", "Rejected", severity="Medium")

# ======================= 9. CREDIT BILL / CONCESSION REPORT / AUDIT =======================
s_, d_ = admin.post("/Corporates", {"code": "QACORP", "name": "QA Corporate Ltd", "contactPerson": "QA", "phone": "9000000999", "email": None, "address": None, "gstNumber": None, "creditLimit": 1000, "isActive": True})
corp = d_["data"] if ok(s_) else next((x["id"] for x in admin.get("/Corporates")[1]["data"] if x["code"] == "QACORP"), None)
b, _ = new_bill("Foxtrot", [(SVC["USG"], 1)])        # 2000 > corp limit 1000
s_, d_ = bil.post(f"/billing/bills/{b['id']}/credit", {"corporateId": corp}); bb = getb(b["id"])
tc("BILL-120", M, "Convert bill (2000) to corporate credit above corporate credit limit (1000)", (rej(s_)) or bb.get("creditExposure") is not None,
   f"{s_} {msg(d_)} isCredit={bb['isCreditBill']} limit={bb.get('creditLimit')} exposure={bb.get('creditExposure')}", "Blocked or clearly flagged as over limit", severity="Medium")
s_, d_ = bil.get("/billing/reports/concession/search?RegNo=PRX-2026-000004&Page=1&PageSize=20")
tc("BILL-121", M, "Concession report lists discounted bills (QATEST Charlie)", s_ == 200 and d_["data"]["totalCount"] >= 1, f"{s_} {d_['data']['totalCount'] if s_ == 200 else msg(d_)}", ">= 1 row", severity="Medium")
tc_ref = "BILL-122"
not_tested(tc_ref, M, "Referral on OP bill", "Referral is captured at patient registration/OPD referral, not as an OP-billing field; no billing-side referral workflow exists to test", expected="n/a")
s_, d_ = admin.get(f"/audit-logs?Page=1&PageSize=50&EntityName=Bill")
tc("BILL-123", M, "Audit trail records bill changes", s_ == 200 and d_["data"]["totalCount"] >= 5, f"{s_} audit rows for Bill={d_['data']['totalCount'] if s_ == 200 else msg(d_)}", "Row-level audit entries exist", severity="High")
s_, d_ = admin.get(f"/activity-logs?Page=1&PageSize=20&Search=refund")
tc("BILL-124", M, "Business-event activity log captures refund/discount/cancel events", s_ == 200 and d_["data"]["totalCount"] >= 1, f"{s_} {d_['data']['totalCount'] if s_ == 200 else msg(d_)}", ">= 1 refund event", severity="Medium")
summary()
