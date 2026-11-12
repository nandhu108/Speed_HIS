import sys, os, datetime, threading
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from qa_lib import *
from decimal import Decimal as D

M = "F-Pharmacy"
st = load_state(); P = st["patients"]
admin = Sess("admin", "Admin@123", 2); mgr = Sess("qa.pharmmgr", "Qa@12345", 2); buy = Sess("qa.pharmbuy", "Qa@12345", 2)
ph = Sess("qa.pharm", "Qa@12345", 2); nur = Sess("qa.nurse", "Qa@12345", 2); bil = Sess("qa.billing", "Qa@12345", 2)
TODAY = datetime.date.today()
CASH = 1


def idof(d):
    return d["data"] if not isinstance(d["data"], dict) else d["data"]["id"]


stores = admin.get("/pharmacy/stores")[1]["data"]
tc("PH-000", M, "Branch has a pharmacy store and at least one vendor configured (go-live readiness)", any(x["storeType"] == 1 and x["code"] != "QA-PHARM" for x in stores),
   f"SPEED_MULTI pharmacy stores (excluding QA-created)={len([x for x in stores if x['storeType'] == 1 and x['code'] != 'QA-PHARM'])}", ">= 1 pharmacy store", "Inspect /pharmacy/stores in branch SPEED_MULTI", severity="High",
   bug="Configuration gap: the Speed Multi branch was seeded with no pharmacy store and no vendor ('Pharmacy deliberately omitted' in SpeedMultiSeeder), so no purchase, GRN, sale or issue is possible until set up.")
if not any(x["storeType"] == 1 for x in stores):
    s_, d_ = admin.post("/pharmacy/stores", {"code": "QA-PHARM", "name": "QA Main Pharmacy", "storeType": 1, "isDefault": True, "isActive": True})
    assert ok(s_), (s_, d_)
    stores = admin.get("/pharmacy/stores")[1]["data"]
store = next(x for x in stores if x["storeType"] == 1)["id"]
drugs = admin.get("/Drugs?Page=1&PageSize=20")[1]["data"]["items"]
DR_A, DR_B, DR_C = drugs[0], drugs[1], drugs[2]
vend = admin.get("/Vendors")[1]["data"]["items"]
if not vend:
    s_, d_ = admin.post("/Vendors", {"code": "QAVEND", "name": "QA Vendor Pvt Ltd", "contactPerson": "QA", "phone": "9000000888", "email": None, "address": None, "gstNumber": None, "drugLicenseNumber": None, "paymentTermsDays": 30, "isActive": True})
    print("vendor create", s_, msg(d_))
    vend = admin.get("/Vendors")[1]["data"]["items"]
VEND = vend[0]["id"]
print("store", store, "drugs", [x["name"] for x in (DR_A, DR_B, DR_C)], "vendor", VEND)


def stock_of(drug_id):
    rows = admin.get(f"/pharmacy/stock/batches?drugId={drug_id}&storeId={store}&onlyInStock=false")[1]["data"]
    return rows


def qty_on_hand(drug_id):
    return sum(D(str(r.get("quantityOnHand", r.get("quantity", 0)))) for r in stock_of(drug_id))


# ===== stock-in chain =====
before_A = qty_on_hand(DR_A["id"])
s, d = buy.post("/pharmacy/purchase-requests", {"notes": "QA stock-in", "items": [{"drugId": DR_A["id"], "itemId": None, "requestedQuantity": 100}, {"drugId": DR_B["id"], "itemId": None, "requestedQuantity": 50}]})
tc("PH-001", M, "Create purchase request", ok(s), f"{s} {msg(d)}", "PR created", severity="High")
pr = idof(d) if ok(s) else None
s, d = buy.post("/pharmacy/purchase-requests", {"notes": "bad", "items": [{"drugId": DR_A["id"], "itemId": None, "requestedQuantity": 0}]})
tc("PH-002", M, "Purchase request with zero quantity rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = buy.post(f"/pharmacy/purchase-requests/{pr}/decide", {"approve": True})
tc("PH-003", M, "Requester (purchase role) cannot approve own PR (maker-checker/RBAC)", s == 403, f"{s} {msg(d)}", "403", severity="High")
s, d = mgr.post(f"/pharmacy/purchase-requests/{pr}/decide", {"approve": True})
tc("PH-004", M, "Pharmacy manager approves PR", ok(s), f"{s} {msg(d)}", "Approved", severity="High")
s, d = buy.post("/pharmacy/purchase-orders", {"vendorId": VEND, "purchaseRequestId": pr, "notes": None, "items": [
    {"drugId": DR_A["id"], "itemId": None, "orderedQuantity": 100, "rate": 8, "taxPercent": 12}, {"drugId": DR_B["id"], "itemId": None, "orderedQuantity": 50, "rate": 20, "taxPercent": 12}]})
tc("PH-005", M, "Create purchase order from approved PR", ok(s), f"{s} {msg(d)}", "PO created", severity="High")
po = idof(d) if ok(s) else None
pod = next(x for x in admin.get("/pharmacy/purchase-orders?Page=1&PageSize=100")[1]["data"]["items"] if x["id"] == po)
exp_total = D(100) * D(8) * D("1.12") + D(50) * D(20) * D("1.12")
tot = D(str(pod.get("totalAmount", pod.get("netAmount", 0))))
tc("PH-006", M, "PO total = sum(qty x rate) + 12% GST", abs(tot - exp_total) < D("0.01"), f"po total={tot} expected={exp_total}", "1,904.00", severity="Critical")
s, d = mgr.post(f"/pharmacy/purchase-orders/{po}/approve")
tc("PH-007", M, "Approve purchase order", ok(s), f"{s} {msg(d)}", "Approved", severity="High")
s, d = mgr.post(f"/pharmacy/purchase-orders/{po}/approve")
tc("PH-008", M, "Approving PO twice blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")

EXP_OK = (TODAY + datetime.timedelta(days=365)).isoformat()
EXP_NEAR = (TODAY + datetime.timedelta(days=20)).isoformat()
EXP_PAST = (TODAY - datetime.timedelta(days=30)).isoformat()
grn_body = lambda lines: {"purchaseOrderId": po, "vendorId": VEND, "storeId": store, "supplierInvoiceNumber": "QA-INV-001", "supplierInvoiceDate": TODAY.isoformat(), "items": lines}
s, d = mgr.post("/pharmacy/grn", grn_body([{"drugId": DR_A["id"], "itemId": None, "batchNumber": "QA-EXPIRED", "expiryDate": EXP_PAST, "mrp": 15, "purchaseRate": 8, "receivedQuantity": 10, "freeQuantity": 0, "taxPercent": 12}]))
tc("PH-009", M, "GRN with an already-EXPIRED batch rejected (expiry validation)", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = mgr.post("/pharmacy/grn", grn_body([{"drugId": DR_A["id"], "itemId": None, "batchNumber": "QA-NEG", "expiryDate": EXP_OK, "mrp": 15, "purchaseRate": 8, "receivedQuantity": -5, "freeQuantity": 0, "taxPercent": 12}]))
tc("PH-010", M, "GRN with negative received quantity rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = mgr.post("/pharmacy/grn", grn_body([{"drugId": DR_A["id"], "itemId": None, "batchNumber": "QA-MRPLOW", "expiryDate": EXP_OK, "mrp": 5, "purchaseRate": 8, "receivedQuantity": 10, "freeQuantity": 0, "taxPercent": 12}]))
tc("PH-011", M, "GRN where MRP (5) is below purchase rate (8) is flagged/rejected", rej(s), f"{s} {msg(d)}", "Rejected or warned (selling below cost)", severity="Medium")
s, d = mgr.post("/pharmacy/grn", grn_body([{"drugId": DR_A["id"], "itemId": None, "batchNumber": "QA-A-NEAR", "expiryDate": EXP_NEAR, "mrp": 15, "purchaseRate": 8, "receivedQuantity": 30, "freeQuantity": 0, "taxPercent": 12},
                                          {"drugId": DR_A["id"], "itemId": None, "batchNumber": "QA-A-LONG", "expiryDate": EXP_OK, "mrp": 15, "purchaseRate": 8, "receivedQuantity": 70, "freeQuantity": 5, "taxPercent": 12},
                                          {"drugId": DR_B["id"], "itemId": None, "batchNumber": "QA-B-1", "expiryDate": EXP_OK, "mrp": 40, "purchaseRate": 20, "receivedQuantity": 50, "freeQuantity": 0, "taxPercent": 12}]))
tc("PH-012", M, "Create GRN against PO (3 batches)", ok(s), f"{s} {msg(d)}", "GRN draft created", severity="Critical")
grn = idof(d) if ok(s) else None
mid_A = qty_on_hand(DR_A["id"])
tc("PH-013", M, "Stock is NOT increased before the GRN is posted", mid_A == before_A, f"before={before_A} after_draft={mid_A}", "Unchanged until posted", severity="Critical")
s, d = nur.post(f"/pharmacy/grn/{grn}/post")
tc("PH-014", M, "Nurse cannot post a GRN (RBAC)", s == 403, f"{s}", "403", severity="High")
s, d = mgr.post(f"/pharmacy/grn/{grn}/post")
tc("PH-015", M, "Post GRN", ok(s), f"{s} {msg(d)}", "Posted", severity="Critical")
after_A = qty_on_hand(DR_A["id"])
tc("PH-016", M, "Posting GRN increases stock by received + free qty (100 + 5 free = 105)", after_A - before_A == 105, f"delta={after_A - before_A}", "105", severity="Critical")
s, d = mgr.post(f"/pharmacy/grn/{grn}/post")
tc("PH-017", M, "Posting the same GRN twice blocked (no double stock-in)", rej(s) and qty_on_hand(DR_A["id"]) == after_A, f"{s} {msg(d)} stock={qty_on_hand(DR_A['id'])}", "Rejected; stock unchanged", severity="Critical")
s, d = mgr.post("/pharmacy/grn", grn_body([{"drugId": DR_A["id"], "itemId": None, "batchNumber": "QA-OVER", "expiryDate": EXP_OK, "mrp": 15, "purchaseRate": 8, "receivedQuantity": 9999, "freeQuantity": 0, "taxPercent": 12}]))
tc("PH-018", M, "GRN receiving far more than the PO ordered (9999 vs 100) is blocked/flagged", rej(s), f"{s} {msg(d)}", "Over-receipt rejected or flagged", severity="Medium")

rows = stock_of(DR_A["id"])
print("BATCH ROW SAMPLE:", json.dumps(rows[0])[:300] if rows else None)
nearb = next((r for r in rows if r.get("batchNumber") == "QA-A-NEAR"), None)
longb = next((r for r in rows if r.get("batchNumber") == "QA-A-LONG"), None)
tc("PH-019", M, "Batch number, expiry date and quantity stored per batch", bool(nearb and longb and nearb["expiryDate"] == EXP_NEAR), f"near={nearb and (nearb['batchNumber'], nearb['expiryDate'])}", "Batches visible with expiry", severity="High")
al = admin.get("/pharmacy/stock/alerts/near-expiry?withinDays=30")[1]["data"]
tc("PH-020", M, "Near-expiry alert lists the batch expiring in 20 days", "QA-A-NEAR" in json.dumps(al), f"{len(al) if isinstance(al, list) else ''} alerts", "Listed", severity="High")

# ===== sales =====
def sale(pk, lines, who=None, mode=CASH, rx=None):
    return (who or ph).post("/pharmacy/sales", {"storeId": store, "patientId": P[pk], "prescriptionId": rx, "paymentModeId": mode,
                                                "items": [{"drugId": d_, "quantity": q, "discountPercent": 0} for d_, q in lines]})

_pre = {r["batchNumber"]: D(str(r["quantityOnHand"])) for r in stock_of(DR_A["id"])}
s, d = sale("Alpha", [(DR_A["id"], 10)])
tc("PH-030", M, "OP pharmacy sale of 10 units", ok(s), f"{s} {msg(d)}", "Sale created", severity="Critical")
sid = idof(d) if ok(s) else None
sd = admin.get(f"/pharmacy/sales/{sid}")[1]["data"] if sid else {}
print("SALE RAW:", json.dumps(sd)[:700])
tc("PH-031", M, "Sale deducts exactly 10 from stock", qty_on_hand(DR_A["id"]) == sum(_pre.values()) - 10, f"stock={qty_on_hand(DR_A['id'])} expected={after_A - 10}", "Deducted", severity="Critical")
rows = stock_of(DR_A["id"]); nearb = next(r for r in rows if r.get("batchNumber") == "QA-A-NEAR"); longb = next(r for r in rows if r.get("batchNumber") == "QA-A-LONG")
qn = D(str(nearb.get("quantityOnHand", nearb.get("quantity")))); ql = D(str(longb.get("quantityOnHand", longb.get("quantity"))))
tc("PH-032", M, "FEFO: nearest-expiry batch consumed first (near batch -10, long-expiry batch untouched)", _pre["QA-A-NEAR"] - qn == 10 and _pre["QA-A-LONG"] == ql,
   f"near {_pre['QA-A-NEAR']}->{qn}, long {_pre['QA-A-LONG']}->{ql}", "near -10, long unchanged", severity="High")
items = sd.get("items", [])
mrp_cap = sum(D(str(i["quantity"])) * D(str(i["rate"])) for i in items)
tc("PH-033", M, "Patient is never charged above MRP (MRP is tax-inclusive)", D(str(sd.get("totalAmount", 0))) <= mrp_cap,
   f"10 x MRP 15 = {mrp_cap}; charged total={sd.get('totalAmount')} (sub={sd.get('subTotal')} + tax={sd.get('taxAmount')})", "Total <= 150 (GST contained within MRP)", "Sell 10 units of a batch with MRP 15", severity="Critical",
   bug="GST is added on top of MRP: 10 units at MRP 15 billed as 168 instead of <= 150. MRP is the tax-inclusive maximum retail price; patients are overcharged by the GST amount.")
s, d = admin.get(f"/pharmacy/sales/{sid}/invoice")
tc("PH-034", M, "GST invoice PDF generated", s == 200 and d.get("binary") and d["head"] == b"%PDF-", f"{s} {d.get('contentType')}", "Valid PDF", severity="High")
s, d = sale("Alpha", [(DR_A["id"], 999999)])
tc("PH-035", M, "Selling more than available stock rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
for q, tid in ((0, "PH-036"), (-3, "PH-037")):
    s, d = sale("Alpha", [(DR_A["id"], q)])
    tc(tid, M, f"Sale quantity {q} rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")
s, d = sale("Alpha", [(999999, 1)])
tc("PH-038", M, "Sale of unknown drug rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = ph.post("/pharmacy/sales", {"storeId": store, "patientId": P["Alpha"], "prescriptionId": None, "paymentModeId": CASH, "items": []})
tc("PH-039", M, "Sale with no lines rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = sale("Alpha", [(DR_A["id"], 1)], who=nur)
tc("PH-040", M, "Nurse cannot make pharmacy sales (RBAC)", s == 403, f"{s}", "403", severity="High")
s, d = sale("Alpha", [(DR_A["id"], 1)], who=bil)
tc("PH-041", M, "Billing role cannot make pharmacy sales (RBAC)", s == 403, f"{s}", "403", severity="High")
s, d = ph.post("/pharmacy/sales", {"storeId": store, "patientId": P["Alpha"], "prescriptionId": None, "paymentModeId": 999, "items": [{"drugId": DR_A["id"], "quantity": 1, "discountPercent": 0}]})
tc("PH-042", M, "Sale with unknown payment mode rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = ph.post("/pharmacy/sales", {"storeId": store, "patientId": P["Alpha"], "prescriptionId": None, "paymentModeId": CASH, "items": [{"drugId": DR_A["id"], "quantity": 1, "discountPercent": 150}]})
tc("PH-043", M, "Sale line discount above 100% rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")

# prescription link (prescription from consultation)
rx = admin.get(f"/emr/patients/{P['Alpha']}/prescriptions")[1]["data"]
print("RX SAMPLE:", json.dumps(rx)[:300])
rxid = None
if isinstance(rx, list) and rx and rx[0].get("visitId"):
    rxid = (admin.get(f"/opd/visits/{rx[0]['visitId']}/workspace")[1]["data"].get("prescription") or {}).get("id")
if rxid:
    s, d = sale("Alpha", [(DR_A["id"], 2)], rx=rxid)
    tc("PH-044", M, "Partial dispensing against a prescription (2 of prescribed course)", ok(s), f"{s} {msg(d)}", "Dispensed, linked to prescription", severity="High")
    s, d = sale("Bravo", [(DR_A["id"], 1)], rx=rxid)
    tc("PH-045", M, "Dispensing another patient's prescription to a different patient blocked", rej(s), f"{s} {msg(d)}", "Rejected (patient mismatch)", severity="Critical")
else:
    blocked("PH-044", M, "Prescription-linked dispensing", "no prescription id could be read from EMR prescriptions list")
DR_X = drugs[6]
s_, d_ = mgr.post("/pharmacy/grn", {"purchaseOrderId": None, "vendorId": VEND, "storeId": store, "supplierInvoiceNumber": "QA-EXP-SALE", "supplierInvoiceDate": TODAY.isoformat(),
    "items": [{"drugId": DR_X["id"], "itemId": None, "batchNumber": "QA-X-EXPIRED", "expiryDate": EXP_PAST, "mrp": 10, "purchaseRate": 5, "receivedQuantity": 20, "freeQuantity": 0, "taxPercent": 12}]})
posted = ok(s_) and ok(mgr.post(f"/pharmacy/grn/{idof(d_)}/post")[0])
tc("PH-047", M, "An expired batch cannot be posted into saleable stock", not posted, f"expired batch GRN created={ok(s_)} posted={posted}", "Rejected at GRN create or post", severity="Critical")
s_, d_ = sale("Hotel", [(DR_X["id"], 2)])
tc("PH-048", M, "Expired medicine cannot be sold (only stock of the drug is expired)", rej(s_), f"{s_} {msg(d_)}", "Sale rejected - no unexpired stock", "Post GRN of an already-expired batch -> sell that drug", severity="Critical",
   bug="Expired-medicine prevention missing: a batch expired 30 days ago was accepted, posted to stock and dispensed to a patient.")
not_tested("PH-046", M, "Alternative / generic substitute suggestion", "FEATURE NOT PRESENT: no substitute/alternative-medicine endpoint; only batch-availability by drug exists.", expected="Alternatives (same generic) offered when out of stock")

# ===== returns & cancellation =====
sale_items = sd.get("items", [])
sitem = sale_items[0]["id"] if sale_items else None
before_ret = qty_on_hand(DR_A["id"])
if sitem:
    s, d = ph.post("/pharmacy/sales-returns", {"pharmacySaleId": sid, "reason": "QA return", "items": [{"pharmacySaleItemId": sitem, "quantity": 3}]})
    tc("PH-050", M, "Return 3 of 10 sold units", ok(s), f"{s} {msg(d)}", "Return recorded", severity="Critical")
    tc("PH-051", M, "Return restores exactly 3 units to stock", qty_on_hand(DR_A["id"]) == before_ret + 3, f"stock={qty_on_hand(DR_A['id'])} expected={before_ret + 3}", "+3", severity="Critical")
    s, d = ph.post("/pharmacy/sales-returns", {"pharmacySaleId": sid, "reason": "QA return too many", "items": [{"pharmacySaleItemId": sitem, "quantity": 8}]})
    tc("PH-052", M, "Returning more than sold in total (3 + 8 > 10) rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical", bug="Cumulative sales-return quantity is not checked against quantity sold: 11 units returned/refunded against 10 sold (stock and cash both inflated).")
    s, d = ph.post("/pharmacy/sales-returns", {"pharmacySaleId": sid, "reason": "", "items": [{"pharmacySaleItemId": sitem, "quantity": 1}]})
    tc("PH-053", M, "Return without reason rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
    s, d = ph.post("/pharmacy/sales-returns", {"pharmacySaleId": sid, "reason": "QA", "items": [{"pharmacySaleItemId": sitem, "quantity": -1}]})
    tc("PH-054", M, "Negative return quantity rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
# cancellation
s, d = sale("Charlie", [(DR_B["id"], 5)]); cid = idof(d) if ok(s) else None
b_before = qty_on_hand(DR_B["id"])
s, d = ph.post(f"/pharmacy/sales/{cid}/cancel", {"reason": ""})
tc("PH-055", M, "Cancel sale without reason rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Medium")
s, d = ph.post(f"/pharmacy/sales/{cid}/cancel", {"reason": "QA cancel"})
tc("PH-056", M, "Cancel sale restores the 5 units to stock", ok(s) and qty_on_hand(DR_B["id"]) == b_before + 5, f"{s} stock={qty_on_hand(DR_B['id'])} expected={b_before + 5} {msg(d) if not ok(s) else ''}", "+5", severity="Critical")
s, d = ph.post(f"/pharmacy/sales/{cid}/cancel", {"reason": "again"})
tc("PH-057", M, "Cancelling an already cancelled sale blocked (no double stock restore)", rej(s) and qty_on_hand(DR_B["id"]) == b_before + 5, f"{s} {msg(d)} stock={qty_on_hand(DR_B['id'])}", "Rejected", severity="Critical")
cd = admin.get(f"/pharmacy/sales/{cid}")[1]["data"]
if cd.get("items"):
    s, d = ph.post("/pharmacy/sales-returns", {"pharmacySaleId": cid, "reason": "QA", "items": [{"pharmacySaleItemId": cd["items"][0]["id"], "quantity": 1}]})
    tc("PH-058", M, "Return against a CANCELLED sale blocked", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
s, d = ph.put(f"/pharmacy/sales/{sid}/pay-type", {"paymentModeId": 3})
tc("PH-059", M, "Pay-type modify after sale", ok(s), f"{s} {msg(d)}", "Payment mode corrected", severity="Low")

# ===== adjustments =====
bid = longb["batchId"] if "batchId" in longb else longb["id"]
s, d = mgr.post("/pharmacy/adjustments", {"storeId": store, "reason": "QA breakage", "items": [{"batchId": bid, "quantityChange": -4}]})
tc("PH-060", M, "Pharmacy manager raises stock adjustment (-4 breakage)", ok(s), f"{s} {msg(d)}", "Pending approval", severity="High")
adj = idof(d) if ok(s) else None
q0 = qty_on_hand(DR_A["id"])
tc("PH-061", M, "Adjustment does not change stock until approved", qty_on_hand(DR_A["id"]) == q0, "stock unchanged pre-approval", "Unchanged", severity="High")
s, d = mgr.post(f"/pharmacy/adjustments/{adj}/decide", {"approve": True, "rejectionReason": None})
self_approved = ok(s)
tc("PH-062", M, "Requester cannot approve their OWN stock adjustment (maker-checker)", rej(s), f"{s} {msg(d)}", "403", severity="High")
if not self_approved:
    s, d = admin.post(f"/pharmacy/adjustments/{adj}/decide", {"approve": True, "rejectionReason": None})
tc("PH-063", M, "Approved adjustment reduces stock by exactly 4", (self_approved or ok(s)) and qty_on_hand(DR_A["id"]) == q0 - 4, f"{s} stock={qty_on_hand(DR_A['id'])} expected={q0 - 4}", "-4", severity="Critical")
s, d = mgr.post("/pharmacy/adjustments", {"storeId": store, "reason": "QA impossible", "items": [{"batchId": bid, "quantityChange": -100000}]})
a2 = idof(d) if ok(s) else None
if a2:
    s2, d2 = admin.post(f"/pharmacy/adjustments/{a2}/decide", {"approve": True, "rejectionReason": None})
    neg = next(r for r in stock_of(DR_A["id"]) if r["batchNumber"] == "QA-A-LONG")["quantityOnHand"]
    tc("PH-064", M, "Adjustment driving a batch below zero cannot be approved", rej(s2), f"{s2} {msg(d2)}; batch QA-A-LONG now {neg}", "Rejected", severity="Critical",
       bug="No negative-stock guard on stock adjustments: approving a -100000 adjustment left the batch at a negative on-hand quantity.")
    if ok(s2):   # QA cleanup: reverse on the synthetic batch so later tests are not polluted
        s3, d3 = mgr.post("/pharmacy/adjustments", {"storeId": store, "reason": "QA cleanup - reverse PH-064 test adjustment", "items": [{"batchId": bid, "quantityChange": 100000}]})
        if ok(s3):
            admin.post(f"/pharmacy/adjustments/{idof(d3)}/decide", {"approve": True, "rejectionReason": None})
else:
    tc("PH-064", M, "Adjustment driving a batch below zero rejected at request", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")

# ===== purchase return =====
q1 = qty_on_hand(DR_B["id"]); bbatch = next(r for r in stock_of(DR_B["id"]) if r.get("batchNumber") == "QA-B-1"); bbid = bbatch.get("batchId", bbatch.get("id"))
s, d = mgr.post("/pharmacy/purchase-returns", {"grnId": grn, "vendorId": VEND, "reason": "QA damaged", "items": [{"batchId": bbid, "quantity": 5, "rate": 20}]})
tc("PH-070", M, "Purchase return of 5 units reduces stock by 5", ok(s) and qty_on_hand(DR_B["id"]) == q1 - 5, f"{s} stock={qty_on_hand(DR_B['id'])} expected={q1 - 5} {msg(d) if not ok(s) else ''}", "-5", severity="High")
s, d = mgr.post("/pharmacy/purchase-returns", {"grnId": grn, "vendorId": VEND, "reason": "QA too many", "items": [{"batchId": bbid, "quantity": 5000, "rate": 20}]})
tc("PH-071", M, "Purchase return above available stock rejected", rej(s), f"{s} {msg(d)}", "Rejected", severity="Critical")

# ===== concurrency: oversell =====
s, d = mgr.post("/pharmacy/grn", {"purchaseOrderId": None, "vendorId": VEND, "storeId": store, "supplierInvoiceNumber": "QA-INV-RACE", "supplierInvoiceDate": TODAY.isoformat(),
    "items": [{"drugId": DR_C["id"], "itemId": None, "batchNumber": "QA-C-RACE", "expiryDate": EXP_OK, "mrp": 10, "purchaseRate": 5, "receivedQuantity": 5, "freeQuantity": 0, "taxPercent": 12}]})
if ok(s):
    mgr.post(f"/pharmacy/grn/{idof(d)}/post")
    c0 = qty_on_hand(DR_C["id"])
    res = []
    def w():
        res.append(sale("Delta", [(DR_C["id"], 3)])[0])
    ts = [threading.Thread(target=w) for _ in range(4)]
    [t.start() for t in ts]; [t.join() for t in ts]
    c1 = qty_on_hand(DR_C["id"])
    tc("PH-080", M, "4 simultaneous sales of 3 units: total sold never exceeds stock, stock never negative", c1 >= 0 and (c0 - c1) == 3 * sum(1 for x in res if ok(x)) and 3 * sum(1 for x in res if ok(x)) <= c0,
       f"statuses={res} stock {c0}->{c1}", "successes x 3 <= starting stock; stock >= 0", severity="Critical")
else:
    blocked("PH-080", M, "Oversell race", f"could not seed race batch: {s} {msg(d)}")

# ===== reports =====
tc("PH-090", M, "Low-stock alert endpoint loads", admin.get("/pharmacy/stock/alerts/low-stock")[0] == 200, "200", "200", severity="Low")
tc("PH-091", M, "Drug consumption report loads", admin.get(f"/pharmacy/stock/consumption-report?fromDate={TODAY}&toDate={TODAY}")[0] == 200, "200", "200", severity="Low")
nb_id = next(r for r in stock_of(DR_A["id"]) if r.get("batchNumber") == "QA-A-NEAR")["id"]
s, d = admin.get(f"/pharmacy/stock/batches/{nb_id}/ledger")
tc("PH-092", M, "Append-only stock ledger shows movements for the batch", s == 200 and len(d["data"]) >= 2, f"{s} ledger rows={len(d['data']) if s == 200 else ''}", ">=2 rows (GRN in, sale out)", severity="High")
summary()
