"""Properly discharges leftover QA admissions (synthetic patients) through the full, now-enforced workflow."""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from qa_lib import *

st = load_state()
a = Sess("admin", "Admin@123", 2); bil = Sess("qa.billing", "Qa@12345", 2)
docs = {st["doctors"]["qa.doctor"]: Sess("qa.doctor", "Qa@12345", 2), st["doctors"]["qa.doctor2"]: Sess("qa.doctor2", "Qa@12345", 2)}
for x in a.get("/ipd/admissions?Status=1&Page=1&PageSize=200")[1]["data"]["items"]:
    if not x["patientName"].startswith("QATEST"):
        continue
    adm = a.get(f"/ipd/admissions/{x['id']}")[1]["data"]
    doc = docs.get(adm["primaryConsultantId"])
    if doc is None:
        print("skip (consultant not a QA doctor):", adm["admissionNumber"]); continue
    if not adm.get("dischargeRequestedAt"):
        print("request", doc.post(f"/ipd/admissions/{x['id']}/discharge/request", {"requestedByDoctorId": adm["primaryConsultantId"], "dischargeType": 1, "dischargeTypeNotes": "QA cleanup"})[0])
    a.post(f"/billing/bills/generate-ip/{x['id']}")
    for b in a.get(f"/billing/bills?PatientId={adm['patientId']}&Page=1&PageSize=20")[1]["data"]["items"]:
        if b["billType"] == 2 and b["status"] != 4 and b["balanceAmount"] > 0:
            print("pay", b["billNumber"], bil.post(f"/billing/bills/{b['id']}/payments", {"paymentModeId": 1, "amount": b["balanceAmount"], "referenceNumber": None})[0])
    for ct in (1, 2, 3, 4):
        s, d = a.post(f"/ipd/admissions/{x['id']}/discharge/clearances/{ct}", {"remarks": "QA cleanup"})
        if not ok(s): print("clear", ct, s, msg(d))
    summ = a.get(f"/ipd/admissions/{x['id']}/discharge/summary")[1]["data"]
    if not summ or not summ.get("isApproved"):
        doc.put(f"/ipd/admissions/{x['id']}/discharge/summary", {"finalDiagnosis": "QA test admission", "courseInHospital": "QA", "medicationsOnDischarge": "None", "followUpAdvice": "None", "conditionAtDischarge": "Stable"})
        s, d = doc.post(f"/ipd/admissions/{x['id']}/discharge/summary/approve", {"approvedByDoctorId": adm["primaryConsultantId"]})
        if not ok(s): print("approve", s, msg(d))
    s, d = a.post(f"/ipd/admissions/{x['id']}/discharge/finalize")
    print("finalize", adm["admissionNumber"], s, msg(d))
