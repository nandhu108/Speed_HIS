"""Cancels leftover QA test transactions (synthetic QATEST patients only) so a re-run starts clean."""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from qa_lib import *

a = Sess("admin", "Admin@123", 2)
n = {"lab": 0, "rad": 0, "adm": 0}
for o in a.get("/lab/orders?Page=1&PageSize=500")[1]["data"]["items"]:
    if o["patientName"].startswith("QATEST"):
        d = a.get(f"/lab/orders/{o['id']}")[1]["data"]
        for it in d["items"]:
            if it["status"] == 1:
                a.post(f"/lab/orders/{o['id']}/items/{it['id']}/cancel"); n["lab"] += 1
for o in a.get("/rad/orders?Page=1&PageSize=500")[1]["data"]["items"]:
    if o["patientName"].startswith("QATEST"):
        d = a.get(f"/rad/orders/{o['id']}")[1]["data"]
        for it in d["items"]:
            if it["status"] in (1, 2):
                a.post(f"/rad/orders/{o['id']}/items/{it['id']}/cancel"); n["rad"] += 1
fo = Sess("qa.frontoffice", "Qa@12345", 2)
for x in a.get("/ipd/admissions?Status=1&Page=1&PageSize=200")[1]["data"]["items"]:
    if x["patientName"].startswith("QATEST"):
        s, d = fo.post(f"/ipd/admissions/{x['id']}/cancel", {"reason": "QA cleanup before re-run"})
        n["adm"] += ok(s)
        if not ok(s):
            print("could not cancel", x["admissionNumber"], msg(d))
print("cleanup:", n)
