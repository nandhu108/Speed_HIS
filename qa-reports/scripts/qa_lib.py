"""QA harness for the Prixo HIS API. Records every executed test case to qa-reports/register.jsonl.
Never PASS without asserting the expected result. Use blocked()/not_tested() with a reason otherwise."""
import json, os, urllib.request, urllib.error, datetime

BASE = "http://localhost:5000/api/v1"
REPORT_DIR = r"d:\Durga Ram Back Up\Prixo_nandhu-speed-multi-seed\qa-reports"
REGISTER = os.path.join(REPORT_DIR, "register.jsonl")
os.makedirs(REPORT_DIR, exist_ok=True)
STATE_FILE = os.path.join(REPORT_DIR, "state.json")


def load_state():
    return json.load(open(STATE_FILE)) if os.path.exists(STATE_FILE) else {}


def save_state(st):
    json.dump(st, open(STATE_FILE, "w"), indent=1)


class Sess:
    def __init__(self, username, password, branch=2):
        self.username = username
        import time
        for attempt in range(8):
            s, d = raw("POST", "/auth/login", {"username": username, "password": password, "branchId": branch})
            if s != 429:
                break
            time.sleep(15)
        if s != 200:
            raise RuntimeError(f"login failed for {username}: {s} {d}")
        self.token = d["data"]["accessToken"]
        self.user = d["data"]["user"]

    def __call__(self, method, path, body=None):
        return raw(method, path, body, self.token)

    def get(self, path):
        return self("GET", path)

    def post(self, path, body=None):
        return self("POST", path, body if body is not None else {})

    def put(self, path, body=None):
        return self("PUT", path, body)

    def delete(self, path):
        return self("DELETE", path)


SERVER_ERRORS = os.path.join(REPORT_DIR, "server_errors.jsonl")


def _log5xx(method, path, code, data):
    with open(SERVER_ERRORS, "a", encoding="utf-8") as f:
        f.write(json.dumps({"method": method, "path": path, "status": code, "trace": (data or {}).get("traceId") if isinstance(data, dict) else None,
                            "body": json.dumps(data)[:200] if data is not None else ""}) + chr(10))


def raw(method, path, body=None, token=None):
    req = urllib.request.Request(BASE + path, method=method,
                                 data=json.dumps(body).encode() if body is not None else None)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req) as r:
            data = r.read()
            ct = r.headers.get("Content-Type", "")
            if "json" in ct:
                return r.status, json.loads(data or b"{}")
            return r.status, {"binary": True, "contentType": ct, "size": len(data), "head": data[:5]}
    except urllib.error.HTTPError as e:
        data = e.read()
        try:
            parsed = json.loads(data)
        except Exception:
            parsed = {"raw": data[:200].decode(errors="ignore")}
        if e.code >= 500:
            _log5xx(method, path, e.code, parsed)
        return e.code, parsed


def msg(d):
    if not isinstance(d, dict):
        return str(d)
    if d.get("binary"):
        return f"<binary {d.get('contentType')} {d.get('size')}B>"
    return d.get("message") or json.dumps(d.get("errors") or d)[:250]


def ok(s):
    return s in (200, 201)


def rej(s):
    """A proper rejection: client error (4xx). A 5xx is an unhandled server error, never a valid rejection."""
    return 400 <= s < 500


_counts = {"PASS": 0, "FAIL": 0, "BLOCKED": 0, "NOT TESTED": 0}


def record(tc_id, module, scenario, pre, steps, expected, actual, status, severity="", bug="", evidence=""):
    row = dict(id=tc_id, module=module, scenario=scenario, preconditions=pre, steps=steps, expected=expected,
               actual=str(actual)[:600], status=status, severity=severity, bug=bug, evidence=evidence,
               ts=datetime.datetime.now().isoformat(timespec="seconds"))
    with open(REGISTER, "a", encoding="utf-8") as f:
        f.write(json.dumps(row, ensure_ascii=False) + "\n")
    _counts[status] = _counts.get(status, 0) + 1
    mark = {"PASS": "PASS", "FAIL": "FAIL", "BLOCKED": "BLKD", "NOT TESTED": "N/T"}[status]
    extra = f"  :: {str(actual)[:170]}" if status != "PASS" else ""
    print(f"[{mark}] {tc_id} {scenario}{extra}")


def tc(tc_id, module, scenario, cond, actual="", expected="", steps="", pre="", severity="Medium", bug="", evidence=""):
    """cond True => PASS, False => FAIL (severity/bug recorded)."""
    if cond:
        record(tc_id, module, scenario, pre, steps, expected, actual or "as expected", "PASS", "", "", evidence)
    else:
        record(tc_id, module, scenario, pre, steps, expected, actual, "FAIL", severity,
               bug or f"Expected: {expected}. Actual: {actual}", evidence)
    return bool(cond)


def blocked(tc_id, module, scenario, reason, steps="", expected="", pre=""):
    record(tc_id, module, scenario, pre, steps, expected, reason, "BLOCKED")


def not_tested(tc_id, module, scenario, reason, steps="", expected="", pre=""):
    record(tc_id, module, scenario, pre, steps, expected, reason, "NOT TESTED")


def summary():
    print("\nSession counts:", _counts)


def patient_body(first="QATEST", last="Patient", gender=1, dob="1985-06-15", mobile="9000000100", **kw):
    body = dict(salutationId=None, firstName=first, middleName=None, lastName=last, gender=gender, dateOfBirth=dob,
                estimatedAge=None, bloodGroup=None, maritalStatusId=None, mobile=mobile, alternateMobile=None,
                whatsAppNumber=None, email=None, address="QA Synthetic Street, Test City", permanentAddress=None,
                isPermanentSameAsPresent=True, stateId=None, cityId=None, areaId=None, pincode=None, countryId=None,
                nationalityId=None, occupation=None, monthlyIncome=None, languageId=None, motherName=None,
                nationalId=None, patientCaseTypeId=None, patientHcTagId=None, speciallyAbledCategoryId=None,
                isVip=False, smsOptIn=False, insuranceRequired=False, remarks=None, patientCategoryId=None,
                referrerId=None, contacts=[], identifiers=[], insurancePolicies=[])
    body.update(kw)
    return body
