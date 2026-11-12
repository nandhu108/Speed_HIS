# Module Spec — Admin + RBAC (Phase 1)

Status: **complete** (2026-07-10). Owns authentication, authorization, user/role/menu/branch
administration, system settings, sessions, and the audit/activity logs every later module writes to.

## Database (schemas `admin`, `audit`)

| Table | Notes |
|---|---|
| `admin.branches` | Global. Unique partial index on `code` (`WHERE is_deleted = false`). |
| `admin.users` | Global. Unique `username` (partial). `must_change_password`, `failed_login_count`, `locked_until`, `photo_path` (upload endpoint deferred), `default_branch_id`. |
| `admin.roles` / `admin.permissions` / `admin.role_permissions` / `admin.user_roles` | Permissions seeded from the code registry only. `roles.is_system` protects SUPER_ADMIN / HOSPITAL_ADMIN. |
| `admin.user_branches` | Which branches a user may sign in to. |
| `admin.designations` | Job titles. |
| `admin.menus` | Self-referencing tree; `permission_code` gates visibility; sidebar is data-driven. |
| `admin.refresh_tokens` | SHA-256 hash only. Rotated per refresh; `replaced_by_token_hash` records lineage. |
| `admin.user_sessions` | One per login (`session_id` GUID also carried in the JWT `sid` claim). |
| `admin.system_settings` | Key/value with group; well-known keys in `SettingKeys`. |
| `admin.password_histories` | Last-N reuse prevention. |
| `audit.audit_logs` | Append-only row diffs (`jsonb`, changed columns only), written by the SaveChanges interceptor in the same transaction. Key/hash/audit-stamp columns excluded. |
| `audit.activity_logs` | Append-only business/security events via `IActivityLogger`. |

Migration: `Phase1_AdminRbac`.

## Auth design

- **Access token**: JWT, 15 min. Claims: `sub`, name, `fullname`, `branch`, `sid`, roles.
  **Permissions are NOT in the JWT** — `[HasPermission]` → dynamic policy → handler checks the
  DB (per-user set cached 60 s, invalidated on role/permission mutations).
- **Refresh token**: opaque 512-bit random, stored SHA-256-hashed, 7 days, rotated on every
  refresh. Reuse of a rotated token revokes the whole session family (theft detection) and logs
  `TOKEN_REUSE_DETECTED`.
- **Session validation middleware**: rejects tokens whose server-side session is revoked or
  idle-expired (`Session.IdleTimeoutMinutes`); updates `last_activity_at` throttled to 1/min.
- **Lockout**: `Lockout.MaxFailedAttempts` failures → locked for `Lockout.Minutes`.
- **Password policy**: length/character rules + history (`Password.*` settings); first login and
  admin resets force a change; changing a password revokes all *other* sessions.
- Fallback authorization policy: every endpoint requires auth except `login`, `refresh`, health.

## Endpoints (`/api/v1`)

- `auth`: `POST login|refresh|logout|change-password`, `GET me|my-branches`
- `users`: paged CRUD + `POST {id}/reset-password` (admin user id 1 cannot be deleted)
- `roles`: CRUD + `GET|PUT {id}/permissions`; `permissions`: catalogue
- `menus`: `GET my` (permission-filtered sidebar), `GET` tree, CRUD
- `branches`, `designations`: CRUD (soft delete, in-use guards)
- `settings`: `GET`, `PUT {key}`
- `abdm/admin`: `GET status`, `POST config`, `POST toggle`, `GET test-connection` (ABDM governance & milestone controls)
- `sessions`: paged `GET`, `POST {sessionId}/revoke` (explicit action endpoint)
- `audit-logs`, `activity-logs`: paged, filterable viewers

## Permissions (registry `Prixo.Application.Common.Security.PermissionRegistry`)

`ADMIN.USER.{VIEW,CREATE,UPDATE,DELETE,RESETPASSWORD}`, `ADMIN.ROLE.{VIEW,CREATE,UPDATE,DELETE,ASSIGNPERMISSIONS}`,
`ADMIN.PERMISSION.VIEW`, `ADMIN.MENU.{VIEW,MANAGE}`, `ADMIN.BRANCH.{VIEW,MANAGE}`,
`ADMIN.DESIGNATION.{VIEW,MANAGE}`, `ADMIN.SETTING.{VIEW,UPDATE}`, `ADMIN.ABDM.MANAGE`, `ADMIN.SESSION.{VIEW,REVOKE}`,
`ADMIN.AUDIT.VIEW`, `ADMIN.ACTIVITY.VIEW`.

## Seed (idempotent, runs at startup in Development)

Permission catalogue, `MAIN` branch, system roles (see the full catalogue below), user
`admin`/`Admin@123` (`Seed:AdminPassword`, forced change), admin menu tree, setting defaults.

## Seeded role catalogue

Plan §8 always specified a full seeded-role list ("Reception, Appointment Desk, Doctor, Nurse,
Head Nurse, Lab Technician, Pathologist, ..."), but through Phase 19 the seeder only ever created
`SUPER_ADMIN` and `HOSPITAL_ADMIN` — every later phase's module spec (starting with
`docs/modules/biomed.md`) explicitly noted this as a deferred gap: *"no role-to-permission seeding
beyond SUPER_ADMIN/HOSPITAL_ADMIN exists anywhere in this codebase yet."* Phase 20 finally closes
that gap in `DbSeeder.SeedRolesAsync`/`SeedDepartmentRolesAsync` — pure additive seed data against
the existing Role/Permission/RolePermission tables (Phase 1's schema, unchanged). An admin can
still hand-tune any of these afterwards from **Admin → Roles & Permissions**.

`HOSPITAL_ADMIN`'s module whitelist was also widened at the same time to include `OT`, `ICU`,
`EMERGENCY`, `MRD`, `FINANCE`, `BIOMED` and `QUALITY` — these were simply never added as each of
those phases landed. It stays branch-scoped operational admin: `REPORT.CROSSBRANCH.VIEW` and
`DOCTORPORTAL.*` are deliberately **not** included.

### Deviations from the plan's literal role-name list

- **No `Patient` role row** — the Patient Portal (Phase 21) uses a wholly separate `PortalAccount`
  entity/JWT audience with no Role/Permission concept at all (ownership checks instead of RBAC);
  there is nothing to seed.
- **`DOCTOR_PORTAL` is its own role, not folded into `DOCTOR`** — matches the plan listing it as a
  separate line item, and lets an admin grant/revoke portal access to a specific doctor's `User`
  independently of their clinical `DOCTOR` role (a doctor gets both roles via the existing
  multi-role `UserRoles` assignment).
- **HOD/supervisor tiers added beyond the plan's literal list, only where a real approval-gated
  permission already exists to justify the split** — mirrors how the plan itself already splits
  Billing Executive/Supervisor and Lab Technician/Pathologist this way: `PHARMACY_MANAGER` (holds
  the purchase/stock/indent approval permissions `PHARMACIST`/`PHARMACY_PURCHASE` don't),
  `FINANCE_MANAGER` split from `FINANCE_EXECUTIVE` (voucher reversal, chart of accounts, the
  cross-branch day-close console), `MRD_MANAGER` split from `MRD_OFFICER` (file issue/return
  approval, retention/destruction, discharge audits). **No HOD split invented for
  Biomedical/OT/Quality** — none of those modules has an approval-gated action a second tier could
  hold (Biomed's flow is assign/resolve/close, not approve; OT's gate is the WHO checklist, not a
  hierarchy; Quality Manager already *is* the oversight tier) — an empty-permission-delta role
  would be an unjustified abstraction.

### Role → permission bundle

| Role code | Name | Permission bundle |
|---|---|---|
| `SUPER_ADMIN` | Super Administrator | Every permission in the registry |
| `HOSPITAL_ADMIN` | Hospital Administrator | Admin/user-management basics + every permission across MASTER/PATIENT/APPOINTMENT/OPD/BILLING/LAB/PHARMACY/IPD/NURSING/EMR/RAD/OT/ICU/EMERGENCY/MRD/FINANCE/BIOMED/QUALITY |
| `RECEPTION` | Reception | Patient registration, appointments, OPD front-desk registration, OP billing, ER registration |
| `APPOINTMENT_DESK` | Appointment Desk | Patient view/create, scheduling and appointment booking only |
| `DOCTOR` | Doctor | Clinical consultation across OPD/IPD/Lab/Rad orders, OT, ICU, Emergency, EMR |
| `DOCTOR_PORTAL` | Doctor Portal | `DOCTORPORTAL.*` only — grant alongside `DOCTOR`, not instead of it |
| `NURSE` | Nurse | Ward nursing, MAR, handovers, incident reporting, ICU/ER frontline charting |
| `HEAD_NURSE` | Head Nurse (HOD) | `NURSE` + incident review, bed management, discharge clearance |
| `LAB_TECHNICIAN` | Lab Technician | Sample collection and result entry |
| `PATHOLOGIST` | Pathologist (HOD) | Result verification/approval, lab catalog ownership |
| `RADIOLOGY_TECHNICIAN` | Radiology Technician | Scheduling and image-capture worklist |
| `RADIOLOGIST` | Radiologist (HOD) | Report verification/approval, radiology catalog ownership |
| `PHARMACIST` | Pharmacist | Dispensing, OP POS sale |
| `PHARMACY_PURCHASE` | Pharmacy Purchase | Procurement (PR/PO/GRN) |
| `PHARMACY_MANAGER` | Pharmacy Manager (HOD) | `PHARMACIST` + `PHARMACY_PURCHASE` + all approve/adjust/transfer permissions |
| `STORE_KEEPER` | Store Keeper | General (non-drug) stores issue/indent |
| `BILLING_EXECUTIVE` | Billing Executive | Bill generation, payment collection |
| `BILLING_SUPERVISOR` | Billing Supervisor (HOD) | `BILLING_EXECUTIVE` + cancel, discount/refund approval, day close |
| `CASHIER` | Cashier | Payment collection at the counter only |
| `FINANCE_EXECUTIVE` | Finance/Accounts Executive | Vouchers, payables/receivables, ledger |
| `FINANCE_MANAGER` | Finance/Accounts Manager (HOD) | `FINANCE_EXECUTIVE` + voucher reversal, chart of accounts, cross-branch day close |
| `MRD_OFFICER` | MRD Officer | File management, document scanning, ICD coding worklist |
| `MRD_MANAGER` | MRD Manager (HOD) | `MRD_OFFICER` + file issue/return, retention/destruction, discharge audits |
| `BIOMEDICAL_ENGINEER` | Biomedical Engineer | Equipment, contracts, PM, complaints, calibration (single tier — no approval gate exists) |
| `QUALITY_MANAGER` | Quality Manager | Indicators, audits, incident RCA (+ Nursing's own incident view/create/review), non-conformance, CAPA, risk, committees, NABH evidence |
| `OT_COORDINATOR` | OT Coordinator | Theatre scheduling, WHO checklist gate |
| `ICU_INCHARGE` | ICU Incharge (HOD-equivalent) | Critical-alert rules, ICU charting/dashboard, bed management |
| `MANAGEMENT` | Management | Read-only cross-branch dashboards |

Exact permission codes per role are in `DbSeeder.SeedDepartmentRolesAsync`
(`Prixo.Infrastructure/Persistence/DbSeeder.cs`) — this table is the summary, that method is the
source of truth.

## Frontend (`src/features/auth`, `src/features/admin`)

- `AuthProvider`: access token in memory, refresh token in `localStorage`; axios interceptors add
  the bearer and do single-flight 401 → refresh → replay.
- Guards: `RequireAuth` (+ forced change-password redirect), `RequirePermission` (cosmetic — the
  API is the gate). Sidebar from `GET /menus/my`.
- Pages: Users (roles/branches mapping, reset password), Roles + permission matrix, Menus,
  Branches, Designations, Settings (grouped inline editing), Sessions (force sign-out),
  Audit Log (diff viewer), Activity Log. Shared `DataTable` (server pagination + search).

## "Front Office control panel" / "Consultation-charge settings" (added 2026-07-27)

Status: **complete**. Both are new `SettingKeys` in the existing generic key-value store —
no new table, no new frontend page. The already-existing `SettingsPage` groups rows by
`Group` and renders any new group automatically, so a "Front Office" or "Billing" group
just appears once seeded:

- `FrontOffice.MaxAdvanceBookingDays` (default 90, 0 = unlimited) — enforced only in
  `AppointmentService.BookAsync` (the staff "Book Appointment" action). Reschedule, ER
  Token Mapping, and Patient Portal booking all call `BookInternalAsync` directly and are
  deliberately unaffected — this is a front-desk control, not a system-wide booking rule.
- `Billing.DefaultConsultationFee` / `Billing.DefaultFollowUpFee` (default 0 = no fallback)
  — consumed in `OpVisitService.RegisterAsync` only when a doctor's own
  `ConsultationFee`/`FollowUpFee` is 0 (not configured). A doctor with a real fee, even a
  deliberately low one, is never overridden — see `docs/modules/opd.md`'s "Billing link"
  section. `DoctorsPage` already fully exposes per-doctor fee editing, so this settings
  group only adds the hospital-wide fallback that didn't exist before, not a duplicate
  fee editor.

## Deferred

User photo upload endpoint (needs file storage), Toast component (inline alerts used),
CI pipeline + branch protection (needs GitHub remote).
