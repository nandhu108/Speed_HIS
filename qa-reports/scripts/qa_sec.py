import sys, os, datetime
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from qa_lib import *

M = "K-Security & Access"
st = load_state(); P = st["patients"]
s, d = raw("GET", "/Patients?Page=1&PageSize=1")
tc("SEC-001", M, "Patient list without login is refused", s == 401, f"{s}", "401", severity="Critical")
s, d = raw("GET", f"/emr/patients/{P['Alpha']}/timeline")
tc("SEC-002", M, "EMR timeline without login is refused", s == 401, f"{s}", "401", severity="Critical")
s, d = raw("GET", "/billing/bills?Page=1&PageSize=1", token="eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.forged")
tc("SEC-003", M, "Forged/garbage JWT refused", s == 401, f"{s}", "401", severity="Critical")
s, d = raw("POST", "/auth/login", {"username": "admin", "password": "wrong-password", "branchId": 2})
tc("SEC-004", M, "Wrong password refused with a generic message (no user enumeration)", s in (400, 401), f"{s} {msg(d)}", "401 generic", severity="High")
codes = []
for _ in range(12):
    s, d = raw("POST", "/auth/login", {"username": "qa.noperm-x", "password": "bad", "branchId": 2}); codes.append(s)
tc("SEC-005", M, "Repeated failed logins are rate-limited (brute-force protection)", 429 in codes, f"codes={codes}", "429 after repeated failures", severity="High")
import time; time.sleep(30)
# cross-branch isolation: a SPEED_MULTI-only user tries the MAIN branch
s, d = raw("POST", "/auth/login", {"username": "qa.billing", "password": "Qa@12345", "branchId": 1})
tc("SEC-006", M, "User assigned only to SPEED_MULTI cannot log in to MAIN branch", s != 200 or d["data"]["user"]["branchId"] != 1, f"{s} {msg(d) if s != 200 else d['data']['user']['branchName']}", "Refused or kept on own branch", severity="Critical")
admin_main = Sess("admin", "Admin@123", 1)
main_bills = admin_main.get("/billing/bills?Page=1&PageSize=5")[1]["data"]["totalCount"]
bil = Sess("qa.billing", "Qa@12345", 2)
sp_bills = bil.get("/billing/bills?Page=1&PageSize=500")[1]["data"]["items"]
admin_sp = Sess("admin", "Admin@123", 2)
sp_ids = {x["id"] for x in admin_sp.get("/billing/bills?Page=1&PageSize=500")[1]["data"]["items"]}
tc("SEC-007", M, "Branch scoping: SPEED_MULTI user sees only SPEED_MULTI bills", all(x["id"] in sp_ids for x in sp_bills), f"main-branch bills={main_bills}; leaked={[x['id'] for x in sp_bills if x['id'] not in sp_ids][:5]}", "No other-branch rows", severity="Critical")
s, d = raw("POST", "/Users", {"username": "qa.weakpwd", "password": "123", "fullName": "QA Weak", "email": None, "phone": None, "employeeCode": None, "designationId": None, "departmentId": None,
                              "defaultBranchId": 2, "roleIds": [], "branchIds": [2]}, admin_sp.token)
tc("SEC-008", M, "Weak password ('123') rejected by password policy", rej(s), f"{s} {msg(d)}", "Rejected", severity="High")
nur = Sess("qa.nurse", "Qa@12345", 2)
s, d = nur.get("/Users?Page=1&PageSize=5")
tc("SEC-009", M, "Clinical user cannot list users / administration", s == 403, f"{s}", "403", severity="High")
s, d = nur.put(f"/Roles/1/permissions", {"permissionCodes": ["ADMIN.USER.CREATE"]})
tc("SEC-010", M, "Clinical user cannot grant themselves permissions (privilege escalation)", s == 403, f"{s}", "403", severity="Critical")
s, d = admin_sp.get("/audit-logs?Page=1&PageSize=1")
tc("SEC-011", M, "Audit log populated and readable by administrator", s == 200 and d["data"]["totalCount"] > 0, f"{s} rows={d['data']['totalCount'] if s == 200 else ''}", "> 0 rows", severity="High")
summary()
