import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from qa_lib import *

st = load_state()
admin = Sess("admin", "Admin@123", 2)
PWD = "Qa@12345"

s, d = admin.get("/Roles")
roles = {r["name"]: r["id"] for r in d["data"]}

users = {
    "qa.reception": ["Reception"], "qa.billing": ["OP Billing", "Billing Executive"], "qa.billsup": ["Billing Supervisor"],
    "qa.cashier": ["Cashier"], "qa.nurse": ["Nurse"], "qa.headnurse": ["Head Nurse"], "qa.lab": ["Lab Technician"],
    "qa.patho": ["Pathologist"], "qa.rad": ["Radiologist"], "qa.radtech": ["Radiology Technician"],
    "qa.pharm": ["Pharmacist"], "qa.pharmmgr": ["Pharmacy Manager"], "qa.pharmbuy": ["Pharmacy Purchase"],
    "qa.ot": ["OT Coordinator"], "qa.doctor": ["Doctor", "Doctor Portal"], "qa.doctor2": ["Doctor", "Doctor Portal"],
    "qa.mrd": ["MRD Manager"], "qa.store": ["Store Keeper"], "qa.noperm": [],
}
st.setdefault("users", {})
s, d = admin.get("/Users?Page=1&PageSize=200")
existing = {u["username"]: u["id"] for u in d["data"]["items"]}
for uname, rnames in users.items():
    if uname in existing:
        st["users"][uname] = existing[uname]
        continue
    body = {"username": uname, "password": PWD, "fullName": "QA " + uname.split(".")[1].title(), "email": None,
            "phone": None, "employeeCode": None, "designationId": None, "departmentId": None,
            "defaultBranchId": 2, "roleIds": [roles[r] for r in rnames], "branchIds": [2]}
    s, d = admin.post("/Users", body)
    print("user", uname, s, msg(d) if not ok(s) else "created")
    if ok(s):
        st["users"][uname] = d["data"]

# QA doctor master records
s, d = admin.get("/departments")
depts = {x["code"]: x["id"] for x in d["data"]}
st["depts"] = depts
for uname, dcode, reg in (("qa.doctor", "DEP001", "QA-REG-001"), ("qa.doctor2", "DEP010", "QA-REG-002")):
    s, d = admin.get(f"/Doctors?page=1&pageSize=200")
    have = {x["userId"]: x["id"] for x in d["data"]["items"]}
    uid = st["users"].get(uname)
    if uid in have:
        st.setdefault("doctors", {})[uname] = have[uid]
        continue
    body = {"userId": uid, "registrationNo": reg, "specialtyId": None, "qualification": "MBBS (QA)",
            "consultationFee": 500, "followUpFee": 250, "followUpWindowDays": 7, "defaultSlotMinutes": 15,
            "departmentIds": [depts[dcode]], "isActive": True}
    s, d = admin.post("/Doctors", body)
    print("doctor", uname, s, msg(d) if not ok(s) else "created")
    if ok(s):
        st.setdefault("doctors", {})[uname] = d["data"]

# synthetic patients
s, d = admin.get("/Patients?Page=1&PageSize=100")
have = {p["fullName"] for p in d["data"]["items"]}
st.setdefault("patients", {})
names = ["Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel"]
for i, n in enumerate(names, 1):
    full = f"QATEST {n}"
    row = next((p for p in d["data"]["items"] if p["fullName"].replace("  ", " ").endswith(full)), None)
    if row:
        st["patients"][n] = row["id"]
        continue
    body = patient_body("QATEST", n, gender=1 if i % 2 else 2, dob=f"19{70 + i}-0{i}-15", mobile=f"900000010{i}")
    s, r = admin.post("/Patients", body)
    print("patient", n, s, msg(r) if not ok(s) else r["data"])
    if ok(s):
        st["patients"][n] = r["data"]["id"] if isinstance(r["data"], dict) else r["data"]
save_state(st)
print(json.dumps(st, indent=1)[:1500])
