# Login credentials (development)

Every credential on this page is a **development/demo seed value that already lives in source
control** (`appsettings.Development.json`, `DbSeeder.cs`, `DemoDataSeeder.cs`). None of it is a
production secret, and none of it may ever be used to stand up a real hospital environment —
`DemoDataSeeder` refuses to run outside `Development` for exactly this reason (see
`docs/demo-data.md`).

There are **two entirely separate auth surfaces**. They do not share accounts, tokens, or JWT
audiences — a staff login will not open the portal and vice versa (see `docs/modules/portal.md`).

---

## 1. Staff (the main HIS app)

`POST /api/v1/auth/login` — `{ "username": ..., "password": ... }`

> ### ⚠ Two different passwords
> `admin` is **`Admin@123`**. Every other account below is **`Prixo@123`**.
> `admin` / `Prixo@123` no longer works (it was reset back to the seed value on 2026-08-17).
>
> **All 30 verified 2026-08-17** — `admin` → 200 with `Admin@123` (and 400 with `Prixo@123`); the
> other 29 → 200 with `Prixo@123`. Forced password change is **off**, so none of these bounce you
> to the change-password screen. Roles below are read from the database, not from memory.

### Every working login on this dev database

| Username | Password | Role(s) | Covers |
|---|---|---|---|
| `admin` | `Admin@123` | `SUPER_ADMIN` | Everything — all permissions, all branches |
| `dr.smith` | `Prixo@123` | `HOSPITAL_ADMIN` | Admin, masters, read access across clinical modules |
| `arjun.sharma` | `Prixo@123` | `RECEPTION` | Patient registration, search, OP billing basics, ER registration |
| `Durgaram` | `Prixo@123` | `APPOINTMENT_DESK` + `BILLING_EXECUTIVE` + `BILLING_SUPERVISOR` | Booking, tokens, queue; bills, discounts, refunds, credit notes incl. approvals |
| `Durgaram_frontoffice` | `Prixo@123` | `FRONT_OFFICE` + `BILLING_SUPERVISOR` | OP/MHC/camp registration, ER registration, IPD front desk, loyalty + insurance intake |
| `durgaop` | `Prixo@123` | `OP_BILLING` | OP billing desk, dashboard |
| `aditya.gupta` | `Prixo@123` | `CASHIER` + `BILLING_SUPERVISOR` | Payment collection, receipts, cashier shift, day close |
| `ramesh.sharma` | `Prixo@123` | `DOCTOR` + `DOCTOR_PORTAL` | OPD consultation workspace, IPD orders, lab/rad ordering, EMR, ICU, OT — and the Doctor Portal (my-queue, my-inpatients, e-sign inbox) |
| `radha.sharma` | `Prixo@123` | `NURSE` | Nursing assessments, MAR, notes, vitals, incidents, IPD charting |
| `kabir.verma` | `Prixo@123` | `HEAD_NURSE` + `NURSE` | The above, plus shift handovers and ward-level nursing |
| `praveen.gupta` | `Prixo@123` | `ICU_INCHARGE` + `HEAD_NURSE` | Ventilator + infusion charts, severity assessment, critical alerts |
| `manoj.verma` | `Prixo@123` | `LAB_TECHNICIAN` | Sample collection, receipt, result entry |
| `ananya.verma` | `Prixo@123` | `PATHOLOGIST` | Result verification, approval, critical alerts |
| `saanvi.verma` | `Prixo@123` | `RADIOLOGY_TECHNICIAN` | Rad scheduling, image capture, contrast-consent gate |
| `meera.verma` | `Prixo@123` | `RADIOLOGIST` | Rad report entry, verify, approve |
| `divya.verma` | `Prixo@123` | `PHARMACIST` | OP sale/POS, dispensing, returns, issues |
| `swati.verma` | `Prixo@123` | `PHARMACY_PURCHASE` | PR → PO → GRN → purchase return |
| `radha.verma` | `Prixo@123` | `PHARMACY_MANAGER` + `PHARMACIST` | Stock adjustments, transfers, stock audits (approvals) |
| `uma.verma` | `Prixo@123` | `STORE_KEEPER` | Indents, non-drug items, general store stock |
| `krishna.gupta` | `Prixo@123` | `FINANCE_EXECUTIVE` | Vouchers, ledger, payables/receivables |
| `rohan.gupta` | `Prixo@123` | `FINANCE_MANAGER` | The above plus approvals and the cross-branch day-close console |
| `kabir.gupta` | `Prixo@123` | `MRD_OFFICER` | File movement, scanning, ICD coding |
| `dhruv.gupta` | `Prixo@123` | `MRD_MANAGER` + `MRD_OFFICER` | The above plus record requests, retention, destruction |
| `ramesh.gupta` | `Prixo@123` | `OT_COORDINATOR` | Surgery booking, safety checklist, team, implant usage |
| `karthik.gupta` | `Prixo@123` | `BIOMEDICAL_ENGINEER` | Equipment, AMC/CMC, PM schedules, work orders |
| `suresh.gupta` | `Prixo@123` | `QUALITY_MANAGER` | Indicators, audits, non-conformance, CAPA, risk, evidence |
| `loyalty.desk` | `Prixo@123` | `LOYALTY_DESK` | Loyalty card issue, renewal, dependents |
| `insurance.exec` | `Prixo@123` | `INSURANCE_EXECUTIVE` | Policies, pre-auth, claim submission |
| `insurance.super` | `Prixo@123` | `INSURANCE_SUPERVISOR` | Claim review and settlement |
| `aditya.sharma` | `Prixo@123` | `MANAGEMENT` | The five dashboards, cross-branch reports |

**Where these came from.** `admin` is the only account any seeder creates — `DbSeeder` writes
exactly one `admin.users` row, and `DemoDataSeeder` writes portal accounts only, never staff users.
The other 29 are one representative per role, given a known password on 2026-08-13 because none of
the ~1,186 in-app-created accounts had a recoverable one; `loyalty.desk`, `insurance.exec` and
`insurance.super` were created outright, for roles that had no user at all.

> Several of these carry **more roles than a single job needs** (`radha.verma` is PHARMACIST *and*
> PHARMACY_MANAGER, `praveen.gupta` is ICU_INCHARGE *and* HEAD_NURSE, `aditya.gupta` is CASHIER
> *and* BILLING_SUPERVISOR). Convenient for clicking through a flow end to end, but it means these
> accounts are **not** a valid way to test that a permission boundary actually holds — a single-role
> account is. Regenerate the mapping with the query below rather than assuming.

**EMERGENCY has no dedicated role by design** — `EMERGENCY.*` permissions are held by
RECEPTION and FRONT_OFFICE (registration/triage) and DOCTOR, NURSE, HEAD_NURSE (assessment,
disposition). Use `Durgaram_frontoffice` to register and `ramesh.sharma` to assess.
**IPD and EMR** are likewise covered by DOCTOR / NURSE / FRONT_OFFICE rather than a dedicated role.

Verify the current mapping any time with:

```sql
select split_part(p.code,'.',1) as module, string_agg(distinct r.code, ', ' order by r.code)
from admin.permissions p
join admin.role_permissions rp on rp.permission_id = p.id and rp.is_deleted = false
join admin.roles r on r.id = rp.role_id and r.is_deleted = false
where p.is_deleted = false and r.code <> 'SUPER_ADMIN'
group by 1 order by 1;
```

### The other ~1,150 staff users still have no recorded password

The accounts above are one representative per role. The rest of the `*.sharma` / `*.verma` /
`*.gupta` series were **created through the app's own Users screen**
(`UserService.CreateAsync`, which hashes a caller-supplied password) in an earlier session —
their passwords were chosen at creation time and are stored only as hashes. They are not
derivable from the repository and were left untouched.

To use one, reset it: sign in as `admin` → **Admin › Users › Reset password**
(`POST /api/v1/users/{id}/reset-password`, permission `ADMIN.USER.RESETPASSWORD`).

### Password policy

Enforced by `PasswordPolicyService` on create, reset, and change. Every rule is a row in
`admin.system_settings`, so **all of it is switchable at Admin › Settings with no code change**:

| Setting key | Default | This dev DB |
|---|---|---|
| `Password.MinLength` | 8 | 8 |
| `Password.RequireUppercase` / `RequireLowercase` / `RequireDigit` / `RequireSpecial` | all `true` | all `true` |
| `Password.ExpiryDays` | 0 — never | 0 |
| `Password.HistoryCount` | 5 | 5 |
| `Password.ForceChangeOnFirstLogin` | `true` | **`false`** ← turned off 2026-08-17 |

### Turning off the forced password change

`Password.ForceChangeOnFirstLogin` controls whether a new or admin-reset account is flagged
"must change password at first login". With it `false`, nobody ever lands on the change-password
screen: `UserService.CreateAsync` doesn't set the flag, and `ResetPasswordAsync` **vetoes** it even
when the Users screen explicitly asks for it.

It ships `true`, because for a real hospital it is the control that stops a handed-out password
living forever. It is `false` here, which is the right call for a dev/demo box where the password
is in source control anyway.

Change it from **Admin › Settings**, or:

```bash
curl -X PUT http://localhost:5000/api/v1/settings/Password.ForceChangeOnFirstLogin \
  -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
  -d '{"value":"false"}'
```

> **Do not change it with raw SQL.** `SettingService` caches every setting in memory for **5
> minutes** and only evicts on the API path above, so a direct `UPDATE admin.system_settings`
> appears to do nothing until the cache expires. This bit me while testing.

For a **fresh** database the flag has to be off before `admin` is even created, because
`SeedAdminUserAsync` runs before the settings are seeded. That is what
`"Seed": { "ForcePasswordChange": false }` in `appsettings.Development.json` is for — it defaults
to `true` when absent, so production is unaffected.

If you want to drop the complexity rules too (e.g. to use a short password), set
`Password.MinLength` to `1` and the four `Require*` keys to `false` the same way. They are
deliberately left at their defaults here.

Changing a password revokes every *other* session for that user. Admin user id 1 cannot be
deleted.

---

## 2. Patient Portal

`POST /api/v1/portal/auth/login` — `{ "uhidOrMobile": ..., "password": ... }`

| Accounts | Password | Notes |
|---|---|---|
| The 25 demo portal accounts | `Portal@123` | `DemoDataSeeder.SeedPatientExperienceAsync` (`hasher.Hash("Portal@123")`). All flagged `MustChangePassword`. |

Log in with **either** the patient's UHID **or** their mobile number — one field accepts both.
On this dev database the 25 portal patients are **`PRX-2026-000517` … `PRX-2026-000541`**.
Both forms verified 2026-08-17 → 200 (`PRX-2026-000517` by UHID, `9820353224` by mobile).

Portal accounts key off `patient_id` and have no username of their own, so this is how you list
the usable logins:

```sql
select p.uhid, p.mobile
from patient.portal_accounts pa
join patient.patients p on p.id = pa.patient_id
where pa.is_deleted = false
order by p.uhid;
```

Only seeded when `Seed:DemoData` is `true` (it is, in `appsettings.Development.json`).

OTP login is a **placeholder** — the plan scopes it as "when SMS integration lands". Password is
the only working portal login path.

---

## When `admin` / `Admin@123` is rejected

> **Resolved on this dev database (2026-08-17)** — `admin` is back to `Admin@123` with the forced
> change off, so it should just work. `Prixo@123` also still works for the 29 per-module accounts.
> Keep reading only if it ever breaks again on this or another database.

To check where you stand:

```sql
select username, must_change_password, password_changed_at from admin.users where username = 'admin';
```

`Seed:AdminPassword` only applies **the first time the row is created**. The seeder is idempotent
and will not rewrite an existing `admin` row, so once someone completes the forced first-login
change, the seed value is dead and the new password exists only as a hash. Confirm with:

```sql
select username, must_change_password, password_changed_at from admin.users where username = 'admin';
```

A non-null `password_changed_at` with `must_change_password = false` means it was changed and
`Admin@123` will no longer work.

Three ways out, cheapest first:

1. **Another account with `ADMIN.USER.RESETPASSWORD`** — sign in as that user and reset `admin`
   from the Users screen.
2. **Overwrite the hash in the dev DB.** Any account whose password you *do* know can donate its
   hash — no hashing tool required:
   ```sql
   -- copies a known-good password hash onto admin, and re-arms the forced change
   update admin.users
      set password_hash = (select password_hash from admin.users where username = '<account-you-know>'),
          must_change_password = true
    where username = 'admin';
   ```
3. **Rebuild the database.** Destroys all data, including the demo set:
   ```bash
   docker compose -f docker/docker-compose.yml down -v      # -v drops the volume
   docker compose -f docker/docker-compose.yml up -d db
   dotnet ef database update --project src/Prixo.Infrastructure --startup-project src/Prixo.Api
   # restart the API — DbSeeder + DemoDataSeeder run at startup in Development
   ```
   The seeder then recreates `admin` at whatever `Seed:AdminPassword` currently says.

Note that seeders **only run at API startup**. Editing a seed value does nothing to an
already-running instance.

---

## Re-checking this page

Credentials drift — someone changes a password, a seeder runs, a role gets rebuilt. Don't trust
this page for more than a session or two without re-running the check. With the API up on `:5000`:

```bash
# admin uses Admin@123; every other account uses Prixo@123.
USERS="dr.smith arjun.sharma Durgaram Durgaram_frontoffice ramesh.sharma radha.sharma \
kabir.verma manoj.verma ananya.verma saanvi.verma meera.verma divya.verma swati.verma radha.verma \
uma.verma durgaop aditya.gupta krishna.gupta rohan.gupta kabir.gupta dhruv.gupta karthik.gupta \
suresh.gupta ramesh.gupta praveen.gupta aditya.sharma loyalty.desk insurance.exec insurance.super"

check() {  # check <username> <password>
  code=$(curl -s -o /dev/null -w "%{http_code}" -X POST http://localhost:5000/api/v1/auth/login \
    -H "Content-Type: application/json" -d "{\"username\":\"$1\",\"password\":\"$2\"}")
  echo "$1 -> $code"
}

check admin Admin@123
for u in $USERS; do check "$u" Prixo@123; done
```

Anything other than `200` means that account's password was changed after this page was written —
reset it from **Admin › Users** as the admin, then correct this page.

To regenerate the role column from the database (the authoritative source — the table above was
built from this, and an earlier version of this page under-reported several multi-role accounts):

```sql
select u.username, string_agg(r.code, ' + ' order by r.code) as roles
from admin.users u
left join admin.user_roles ur on ur.user_id = u.id and ur.is_deleted = false
left join admin.roles r on r.id = ur.role_id and r.is_deleted = false
where u.is_deleted = false and u.username in (
  'admin','dr.smith','arjun.sharma','Durgaram','Durgaram_frontoffice','ramesh.sharma',
  'radha.sharma','kabir.verma','manoj.verma','ananya.verma','saanvi.verma','meera.verma',
  'divya.verma','swati.verma','radha.verma','uma.verma','durgaop','aditya.gupta','krishna.gupta',
  'rohan.gupta','kabir.gupta','dhruv.gupta','karthik.gupta','suresh.gupta','ramesh.gupta',
  'praveen.gupta','aditya.sharma','loyalty.desk','insurance.exec','insurance.super')
group by u.username order by u.username;
```

Note the port: the **containerized** API publishes `:5000` (`docker ps`). A local
`dotnet run` cannot bind there while the container holds it, so a local instance will be on
another port — check which one you are actually talking to before concluding a login is broken.

> File:line references on this page (`DbSeeder.cs:151`, `DbSeeder.cs:1158-1164`) were correct on
> 2026-08-17 but shift whenever those files are edited. Prefer grepping for the literal
> (`grep -rn 'Admin@123' backend/src`) over trusting a line number.

## Related

- `docs/demo-data.md` — what `DemoDataSeeder` fills in, and how to reset one module at a time
- `docs/modules/admin.md` — auth, RBAC, password policy, session handling
- `docs/modules/portal.md` — the separate portal JWT audience and its ownership checks
