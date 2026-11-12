# Prixo HIS — User Stories

Comprehensive, module-by-module user stories for the Prixo Hospital Information System,
covering every workflow across Phases 0–24 as actually implemented. Each story follows:

> **As a** `<role>`, **I want to** `<capability>`, **so that** `<business value>`.
> **Acceptance Criteria** — testable conditions drawn from the real behavior of the system.

**Current as of 2026-08-11.** Stories are written against shipped behavior, not the plan, so
this file is re-checked whenever a batch lands. The 2026-07-30 and 2026-08-08 batches are
folded in: the three-step refund payout (US-BILL-5), advance auto-drawdown (US-BILL-7,
US-IPD-6), OP encounter search (US-BILL-14), bulk tariff maintenance (US-BILL-15), the daily
bed-charge job (US-IPD-6), Trial Balance (US-FIN-8), and the seeded starter indicator set
(US-QUAL-1). Phase-level completion status lives in `docs/MODULE_COMPLETION_STATUS.md`.

Roles referenced map to the seeded role catalogue in `docs/modules/admin.md`
(`SUPER_ADMIN`, `HOSPITAL_ADMIN`, `RECEPTION`, `APPOINTMENT_DESK`, `DOCTOR`, `DOCTOR_PORTAL`,
`NURSE`, `HEAD_NURSE`, `LAB_TECHNICIAN`, `PATHOLOGIST`, `RADIOLOGY_TECHNICIAN`, `RADIOLOGIST`,
`PHARMACIST`, `PHARMACY_PURCHASE`, `PHARMACY_MANAGER`, `STORE_KEEPER`, `BILLING_EXECUTIVE`,
`BILLING_SUPERVISOR`, `CASHIER`, `FINANCE_EXECUTIVE`, `FINANCE_MANAGER`, `MRD_OFFICER`,
`MRD_MANAGER`, `BIOMEDICAL_ENGINEER`, `QUALITY_MANAGER`, `OT_COORDINATOR`, `ICU_INCHARGE`,
`MANAGEMENT`, `INSURANCE_EXECUTIVE`, `INSURANCE_SUPERVISOR`, plus `Patient` and
`Doctor (Portal)` for the two external-facing surfaces).

## Table of Contents

1. [Admin + RBAC](#1-admin--rbac)
2. [Patient Registration](#2-patient-registration)
3. [Appointments](#3-appointments)
4. [OPD (Outpatient)](#4-opd-outpatient)
5. [Billing](#5-billing)
6. [Laboratory](#6-laboratory)
7. [Pharmacy](#7-pharmacy)
8. [IPD (Inpatient)](#8-ipd-inpatient)
9. [Nursing](#9-nursing)
10. [EMR / Clinical Records](#10-emr--clinical-records)
11. [Radiology / RIS](#11-radiology--ris)
12. [Stores & Inventory](#12-stores--inventory)
13. [Operation Theatre](#13-operation-theatre)
14. [ICU / Critical Care](#14-icu--critical-care)
15. [Emergency / Casualty](#15-emergency--casualty)
16. [MRD (Medical Records)](#16-mrd-medical-records)
17. [Finance & Accounts](#17-finance--accounts)
18. [Biomedical Engineering](#18-biomedical-engineering)
19. [Quality / NABH](#19-quality--nabh)
20. [Patient Portal](#20-patient-portal)
21. [Doctor Portal](#21-doctor-portal)
22. [Reports & Dashboards](#22-reports--dashboards)
23. [Insurance / TPA](#23-insurance--tpa)

---

## 1. Admin + RBAC

*Epic: as the security and configuration backbone, every other module depends on this one for
authentication, permission checks, branch scoping, menus, and audit trails.*

### US-ADM-1: Secure sign-in with forced first-login password change
**As a** staff user, **I want to** log in with my username/password and be forced to set a new
password on my very first login, **so that** the shared seeded credential (`admin`/`Admin@123`)
is never used long-term.
**Acceptance Criteria**
- Login issues a 15-minute JWT access token plus a rotating refresh token (7-day expiry, opaque
  512-bit, stored only as a SHA-256 hash).
- A user with `must_change_password = true` is redirected to a forced change-password screen
  before reaching any other page.
- Password must satisfy the configured length/character policy and not repeat any of the last N
  passwords in history.
- Changing a password revokes every *other* active session for that user.

### US-ADM-2: Resilient session handling across a work shift
**As a** staff user, **I want** my session to silently refresh while I'm active and to expire if
I walk away, **so that** I don't lose in-progress work but an unattended terminal isn't a security
hole.
**Acceptance Criteria**
- Axios interceptor performs a single-flight 401 → refresh → replay so a token expiring mid-request
  doesn't surface an error to the user.
- Refresh token rotates on every use; replaying an already-rotated (stolen/reused) token revokes
  the entire session family and logs a `TOKEN_REUSE_DETECTED` security event.
- A session idle beyond `Session.IdleTimeoutMinutes` is rejected by the session-validation
  middleware on the next request.
- Repeated failed logins beyond `Lockout.MaxFailedAttempts` locks the account for
  `Lockout.Minutes`.

### US-ADM-3: Manage users, their roles, and branch access
**As a** hospital administrator, **I want to** create/update/deactivate users, assign them one or
more roles, and control which branches they can sign into, **so that** access matches each
employee's actual job and work location.
**Acceptance Criteria**
- User CRUD is paged/searchable; the seed admin (user id 1) cannot be deleted.
- A user can hold multiple roles simultaneously (e.g., `DOCTOR` + `DOCTOR_PORTAL`).
- `POST /users/{id}/reset-password` forces a password change on next login.
- `user_branches` controls the branch picker shown at login; a user can't act on a branch they
  aren't mapped to.

### US-ADM-4: Define roles and fine-grained permissions
**As a** hospital administrator, **I want to** create custom roles and toggle individual
`MODULE.RESOURCE.ACTION` permissions on a permission matrix, **so that** I can implement
separation of duties (e.g., a Billing Executive who can collect payments but not approve
refunds) without waiting for a code change.
**Acceptance Criteria**
- Every protected endpoint requires an explicit `[HasPermission]` code; there are no
  anonymous endpoints besides login/refresh/portal-auth/health.
- Permissions are never embedded in the JWT — every check re-queries the DB (60s cache,
  invalidated immediately on role/permission mutation).
- `roles.is_system` protects `SUPER_ADMIN`/`HOSPITAL_ADMIN` from accidental deletion.
- Frontend permission checks (`RequirePermission`) hide UI but are cosmetic only — the API is
  the real gate, verified by attempting a gated action with a token lacking the permission.

### US-ADM-5: Configure the sidebar menu data-driven
**As a** hospital administrator, **I want to** manage a self-referencing menu tree gated by
permission codes, **so that** each role's sidebar only shows what they're allowed to use.
**Acceptance Criteria**
- `GET /menus/my` returns only nodes whose `permission_code` the caller holds.
- Menu items are CRUD-manageable (title, icon, path, sort order, parent) from Admin → Menus.

### US-ADM-6: Manage branches, designations, and system settings
**As a** hospital administrator, **I want to** register hospital branches, define job
designations, and edit grouped system settings inline, **so that** the platform reflects our
actual organizational structure without hardcoded values.
**Acceptance Criteria**
- Branch/designation CRUD is soft-delete with in-use guards (can't delete a branch with
  dependent data).
- Settings are grouped (e.g., "Billing", "Password", "Lockout") and edited inline via
  `PUT /settings/{key}`.
- No hospital-specific data (rates, thresholds, hospital name) is hardcoded — every value comes
  from a settings row or a master table.

### US-ADM-7: Force sign-out a compromised or stale session
**As a** hospital administrator, **I want to** view active sessions and revoke any of them,
**so that** I can immediately cut off access if a device is lost or an employee leaves.
**Acceptance Criteria**
- `GET /sessions` is paged; `POST /sessions/{sessionId}/revoke` is an explicit action endpoint.
- A revoked session's next request (even with a still-valid-looking JWT) is rejected by the
  session-validation middleware.

### US-ADM-8: Investigate who changed what and when
**As a** hospital administrator or auditor, **I want to** view an append-only audit trail of
every data change and every security/business event, **so that** I can investigate incidents and
satisfy compliance review.
**Acceptance Criteria**
- Every entity change writes a row-diff (changed columns only, JSON) in the same transaction as
  the change, via a `SaveChanges` interceptor — audit rows are never editable or deletable.
- Business events (login/logout, cancellations, approvals, merges) are separately logged via
  `IActivityLogger`.
- Both logs are paged and filterable from Admin → Audit Log / Activity Log.

---

## 2. Patient Registration

*Epic: the single patient identity (UHID) that every clinical and financial module keys off.*

### US-PAT-1: Register a new patient and get a UHID
**As a** front-desk receptionist, **I want to** register a new patient's demographics and
guardian/attender details and receive a unique hospital ID, **so that** the patient has one
identity usable at any branch, forever.
**Acceptance Criteria**
- UHID is generated atomically (`PRX-2026-000123` format) even under concurrent registrations —
  no app-level locking, no possibility of a duplicate.
- The patient record is global (no branch scoping) — visible and usable from every branch.
- The wizard captures demographics, contact & address, guardian & identifiers (Aadhaar/ABHA
  placeholders) across four steps, then a review step before submit.

### US-PAT-2: Get warned about a likely duplicate before registering twice
**As a** front-desk receptionist, **I want to** be shown likely-duplicate existing patients
(scored by mobile/DOB/name-similarity) before I finish registering someone new, **so that** we
don't create two UHIDs for the same person.
**Acceptance Criteria**
- Candidates are prefiltered by exact mobile or DOB match, then scored (mobile +60, DOB +35,
  name-similarity up to +30); only candidates scoring ≥50 are shown, top 5, most-likely first.
- The duplicate warning is non-blocking — staff can proceed anyway if the match is a false
  positive.

### US-PAT-3: Merge two patient records that turned out to be the same person
**As a** front-desk receptionist or supervisor, **I want to** merge a duplicate UHID into the
correct one, **so that** the patient's full history (visits, bills, lab results, prescriptions)
lives under a single identity going forward.
**Acceptance Criteria**
- Merge runs inside one transaction: every contact/identifier/document/consent row is re-pointed
  to the survivor, the merged patient is flagged `IsMerged`/`MergedIntoPatientId` and
  soft-deleted, and an append-only `PatientMerge` audit row is written.
- The merge is logged via `IActivityLogger` (`PATIENT_MERGED`) and is irreversible — gated by its
  own `PATIENT.PATIENT.MERGE` permission, separate from ordinary `UPDATE`.

### US-PAT-4: Capture a patient photo and identity/consent documents
**As a** front-desk receptionist, **I want to** upload a patient's photo, ID proof, and signed
consent forms, **so that** the record is complete and consent is provable later.
**Acceptance Criteria**
- Photo upload accepts JPEG/PNG/WEBP up to 5 MB; other documents up to 15 MB.
- Consent capture links to a reusable `master.consent_templates` entry and optionally attaches a
  scanned copy.
- Files are stored via `IFileStorageService` with GUID-prefixed, sanitized filenames — never a
  raw client-supplied path.

### US-PAT-5: Print a registration card with a scannable QR code
**As a** front-desk receptionist, **I want to** print an A6 patient card carrying demographics,
photo, and a QR code encoding the UHID, **so that** the patient can present it at any future
visit for fast lookup.
**Acceptance Criteria**
- `GET /patients/{id}/card` returns a ready-to-print PDF directly.
- The QR code decodes to the UHID.

### US-PAT-6: Search and browse the patient master
**As any** clinical or front-desk staff member, **I want to** search patients by UHID, name,
mobile, or date of birth, **so that** I can quickly find the right record during a busy shift.
**Acceptance Criteria**
- `GET /patients` is paged and searches across UHID/name/mobile/DOB in one field.
- Selecting a patient opens a detail page with tabs for Overview/Contacts/Documents/Consents.

### US-PAT-7: Capture full front-office demographics, structured address, and insurance policy at registration
**As a** front-desk receptionist, **I want to** pick Salutation/Marital Status/Case Type/Patient
Tag/Specially-Abled Category from dropdowns, select City/State/Area/Country/Nationality instead
of typing them, capture WhatsApp number/Mother's Name/National ID/Monthly Income/VIP/SMS-opt-in,
and record an insurance policy when required, **so that** the registration form matches what our
front office actually needs without falling back to free text or a separate module.
**Acceptance Criteria**
- Salutation, Marital Status, Patient Case Type, Patient Tag, and Specially-Abled Category are
  all master-driven dropdowns (`master.*` tables under Masters), never hardcoded lists.
- City/State/Area cascade (choosing a City filters the Area list to that city); Country and
  Nationality are separate dropdowns backed by the same country list (residence vs. citizenship).
- Checking "Insurance required" reveals a repeatable policy sub-form (insurer/TPA, policy number,
  validity dates); at least one policy is required before the record can save with the flag on.
- A generalized "Other" identity-document type (e.g. PAN, Passport, Voter ID) can be captured
  alongside the existing Aadhaar/ABHA placeholders, picking the specific document type from its
  own master.
- None of this is a new screen — it's the same `PatientRegistrationWizard`/edit-modal fields as
  every other demographic, and existing patients with the old free-text City/State/Nationality
  values were migrated to the new dropdowns automatically on upgrade (best-effort name match).

---

## 3. Appointments

*Epic: doctor availability, slot booking, and the token queue that feeds OPD.*

### US-APPT-1: Define a doctor's weekly working hours
**As an** appointment-desk staff member or administrator, **I want to** set a doctor's recurring
weekly schedule (per branch, per day, with slot length), **so that** bookable slots exist without
manual daily setup.
**Acceptance Criteria**
- A doctor can have multiple blocks on the same day (e.g., morning + evening sessions).
- Schedules are branch-scoped — a doctor's hours can differ across branches they work at.

### US-APPT-2: Mark a doctor unavailable or give them special hours on a specific date
**As an** appointment-desk staff member, **I want to** record a one-off leave day or special hours
for a doctor, **so that** the slot grid reflects reality on holidays or half-days without
disturbing the recurring weekly template.
**Acceptance Criteria**
- A date-specific override, when present, wins outright over the weekly template for that date
  (unavailable → zero slots; available → its own hours replace the day's blocks).
- Only one override per doctor per date is allowed.

### US-APPT-3: Book an appointment into an available slot
**As an** appointment-desk staff member, **I want to** search for a patient, pick a doctor and
date, and book into one of the computed available slots, **so that** the patient gets a
guaranteed time and a token number.
**Acceptance Criteria**
- The slot grid excludes any slot already taken by a non-cancelled appointment.
- Booking re-validates availability immediately before insert to minimize (though not fully
  eliminate) double-booking races.
- A token number is generated atomically per doctor-per-day (`APPT_TOKEN` sequence).
- A walk-in can be booked the same way but flagged `BookingType = WalkIn`.

### US-APPT-4: Reschedule or cancel an appointment
**As an** appointment-desk staff member, **I want to** reschedule a booking to a new slot or
cancel it with a reason, **so that** patient changes of plan are reflected without losing the
original booking's history.
**Acceptance Criteria**
- Rescheduling cancels the old appointment (`CancelReason = "Rescheduled: {reason}"`) and creates
  a new one linked via `RescheduledFromId` — both run in one transaction.
- Cancelling requires a reason and is logged.

### US-APPT-5: Check a patient in and see the live queue
**As a** front-desk staff member, **I want to** check a patient in on arrival and see an
auto-refreshing token queue, **so that** the doctor and waiting patients know who's next.
**Acceptance Criteria**
- Check-in transitions the appointment to `CheckedIn`.
- `GET /queue/display` auto-refreshes every 15 seconds and can be filtered by doctor or
  department.

### US-APPT-6: Automatically flag patients who never showed up
**As an** operations manager, **I want** appointments whose slot has passed with no check-in to
be automatically marked No-Show, **so that** the doctor's schedule and reporting reflect reality
without manual cleanup.
**Acceptance Criteria**
- An hourly Hangfire background job scans every branch (bypassing branch scoping, since it has no
  HTTP/user context) and marks overdue `Scheduled` appointments `NoShow`.

---

## 4. OPD (Outpatient)

*Epic: visit registration through the full doctor consultation workspace.*

### US-OPD-1: Register a walk-in or appointment-linked OP visit
**As a** front-desk staff member, **I want to** register a patient's OP visit — either converting
a checked-in appointment or registering a direct walk-in — **so that** the visit enters the
doctor's queue with the correct fee and token.
**Acceptance Criteria**
- The system auto-detects New vs. Review: a same-day-or-later-within-`FollowUpWindowDays` repeat
  visit to the same doctor is priced as a Review (lower fee); otherwise New.
- A visit from a checked-in appointment reuses that appointment's token number; a walk-in gets a
  fresh token from a separate per-doctor-per-day counter.
- Completing a visit that originated from an appointment also marks that appointment `Completed`.

### US-OPD-2: Work through the doctor's queue for the day
**As a** doctor, **I want to** see my auto-refreshing queue of waiting patients for today and
start the next consultation with one click, **so that** I move through my OP list efficiently.
**Acceptance Criteria**
- The queue lists today's `Waiting`/`InConsult` visits, refreshing every 15 seconds.
- Starting a visit transitions it to `InConsult` and opens the consultation workspace.

### US-OPD-3: Record vitals, history, and examination findings
**As a** doctor (or a nurse taking vitals ahead of the doctor), **I want to** capture height,
weight, vitals (with auto-computed BMI), complaints, and examination notes in the consultation
workspace, **so that** the clinical record is complete before diagnosis and prescription.
**Acceptance Criteria**
- Each workspace section (vitals, complaints, examination) saves independently without touching
  other tabs.
- BMI is computed server-side from height/weight, never trusted from the client.

### US-OPD-4: Record diagnosis and issue a prescription with drug-safety awareness
**As a** doctor, **I want to** record ICD-coded diagnoses and write a prescription — optionally
starting from my saved favorite templates — with a live allergy cross-check, **so that** I
prescribe efficiently and am warned before a documented drug allergy is missed.
**Acceptance Criteria**
- Diagnoses link to the global `master.icd_codes` catalog.
- Applying a saved prescription template prefills line items, which remain fully editable.
- Every drug added is checked against the patient's active drug allergies in real time; a match
  shows a red warning banner and an "Allergy risk" badge on that line — advisory only, never
  blocking save.

### US-OPD-5: Place clinical orders and refer the patient onward
**As a** doctor, **I want to** place lab/radiology/procedure orders and record an internal or
external referral with follow-up advice, **so that** the next steps of care are documented and
actionable.
**Acceptance Criteria**
- Orders are polymorphic (`OrderType`: Lab/Radiology/Procedure) and stay `Ordered` until a
  downstream module (Lab/Radiology) independently creates its own order referencing this one.
- An internal referral links to another doctor in the master catalog; external referrals accept
  free text.
- Follow-up advice records a target date and notes (does not auto-book — staff bridges to
  Appointments manually).

### US-OPD-6: Print the visit slip and consultation summary
**As a** front-desk or clinical staff member, **I want to** print a visit slip at registration and
a consultation summary at the end of the visit, **so that** the patient has a physical record to
carry.
**Acceptance Criteria**
- Both are generated as PDFs on demand (`GET .../slip`, `GET .../summary`).

### US-OPD-7: Admit a patient to IPD directly from the consultation
**As a** doctor, **I want to** click "Admit to IP" from the consultation workspace and have the
admission form pre-filled with the patient and this visit, **so that** I don't have to re-search
the patient from scratch in a different module.
**Acceptance Criteria**
- The Admit Patient modal opens in place (no navigation away) with `referringOpVisitId` set.
- Only a successful admission navigates away, to the new admission's detail page; cancelling
  leaves the doctor on the workspace.

### US-OPD-8: Tag a visit's booking type, class, session and MLC status, and collect an advance at registration
**As a** front-desk staff member, **I want to** record whether a visit was booked or a walk-in,
tag it with a Class and Session, flag it as a medico-legal case, and optionally collect an
advance payment right at registration, **so that** front-office reporting reflects how the visit
actually came in and cash collected at the counter is recorded without a separate trip to
Billing.
**Acceptance Criteria**
- Booking Type, Class, and Session are all selectable at registration (Class/Session from
  master-driven lists) without affecting queue order, token numbering, or fee calculation — purely
  descriptive tags.
- The MLC checkbox marks this OP episode as medico-legal; it does not create or touch a
  statutory MLC register row (that stays Emergency's own workflow) — it's an informational flag.
- Entering an advance amount requires a payment mode; on save, the visit is registered and the
  advance is collected as a single atomic operation — if the payment mode is invalid the whole
  registration fails rather than leaving an orphaned visit.
- The collected advance appears wherever patient advances are already visible (Billing's advance
  list), not in a parallel OPD-only ledger.

---

## 5. Billing

*Epic: OP/IP/Emergency billing, discount governance, payments, refunds, credit, advances, and
day close.*

### US-BILL-1: Generate an OP bill from a visit with correct tariff pricing
**As a** billing executive, **I want to** generate a bill for a completed OP visit that
automatically includes the consultation fee and stub lines for any clinical orders, **so that** I
don't have to manually re-key the consultation charge.
**Acceptance Criteria**
- Generation is blocked if a non-cancelled bill already exists for that visit.
- Manually adding a Service line resolves its rate via tariff lookup: patient-category-specific
  tariff wins over category-agnostic, most-recently-effective wins, falling back to the service's
  default rate.

### US-BILL-2: Apply an item- or bill-level discount within policy
**As a** billing executive, **I want to** apply a discount to a line item or the whole bill, with
automatic routing to a supervisor when it exceeds policy, **so that** discounting stays governed
without slowing down routine, in-policy discounts.
**Acceptance Criteria**
- An ad-hoc discount (no discount type selected) always requires approval.
- A typed discount auto-applies immediately if at or below its type's approval threshold;
  above it, a pending `DiscountApproval` is created and the discount is not applied until decided.
- Item-level and bill-level discounts compose correctly (bill % applies to subtotal minus item
  discounts, not the raw subtotal).

### US-BILL-3: Approve or reject a pending discount request
**As a** billing supervisor, **I want to** review and decide pending discount requests, **so that**
above-threshold discounts get a second set of eyes before they reduce revenue.
**Acceptance Criteria**
- The approvals inbox lists every pending `DiscountApproval`; rejecting requires a reason.
- Approving applies the discount to the bill immediately.

### US-BILL-4: Collect a payment and print a receipt
**As a** billing executive or cashier, **I want to** collect a payment against a bill (cash, card,
UPI, or cheque) and print a receipt, **so that** the patient has proof of payment and the bill
balance updates immediately.
**Acceptance Criteria**
- Payment collection runs in a transaction and is blocked once the branch's day is closed for that
  date.
- A reference number is mandatory for payment modes flagged `RequiresReference`.
- The receipt PDF prints the branch header, line items, totals, and this payment's mode/amount.

### US-BILL-5: Request, approve, and separately pay out a refund
**As a** billing executive, cashier, and billing supervisor, **I want** a refund to move through
three distinct steps — request, approve/reject, then physically issue — **so that** the
supervisor's decision and the cashier's cash handout are never the same click, and an approved
refund that hasn't been paid out yet is visibly still owed to the patient.
**Acceptance Criteria**
- Three permissions gate three steps: `BILLING.REFUND.REQUEST` → `BILLING.REFUND.APPROVE` →
  `BILLING.REFUND.ISSUE`.
- **Approving moves no money** — it only sets `Approved`/`ApprovedByUserId`/`ApprovedAt` (or
  `Rejected` plus a mandatory reason). Balances are untouched at this step.
- **Issuing is where the money moves**: a bill refund reduces `PaidAmount`, recomputes
  `BalanceAmount`/`Status` (correctly falling back to `Open` if paid drops to zero); an advance
  refund reduces the advance's `BalanceAmount`. `IssuedByUserId`/`IssuedAt` are stamped separately
  from the approval stamps.
- Issuing anything not in `Approved` status is rejected with a clear message naming the current
  status — an unapproved or already-issued refund can't be paid out twice.
- A refund targets **either** a bill **or** an advance, never both (DB check constraint
  `ck_refunds_bill_or_advance`); both kinds run the same request → approve → issue pipeline.
- `RefundApprovalsPage` shows two queues — pending-approval and approved-awaiting-payout — so
  approved-but-unpaid refunds can't quietly fall off the radar.
- The refund report totals only `Issued` refunds, not approved-but-unpaid ones.

### US-BILL-6: Cancel a bill that was raised in error
**As a** billing executive, **I want to** cancel a bill with a reason, **so that** mistaken bill
generations don't pollute financial reporting.
**Acceptance Criteria**
- Cancellation is blocked unless `PaidAmount == 0` — any payment must be fully refunded first.
- The reason is logged via the activity log.

### US-BILL-7: Collect an advance and have it drawn down automatically
**As a** billing executive, **I want to** collect an advance deposit against a patient's encounter
and have it consumed against their bill without anyone remembering to click "draw", **so that**
deposits taken at admission actually reduce what the patient owes at settlement instead of sitting
as a separately-visible balance the cashier has to reconcile by hand.
**Acceptance Criteria**
- The Advance Payment screen finds the encounter by **searching OP visits** (patient name, IP no,
  OP-multiple-visit-package filters, with encounter date / reg no / age-sex / doctor-dept /
  corporate name in the results grid) rather than a bare patient typeahead — the cashier sees
  which encounter they're collecting against before taking money. It reuses the same encounter
  search as US-BILL-14, not a parallel one.
- **Auto-drawdown**: every time an IP bill's bed charges are refreshed — on bill generation, on a
  manual refresh, and from the nightly job (US-IPD-6) — the admission's undrawn advances are swept
  against the bill's outstanding balance **FIFO by `ReceivedAt`** (oldest deposit consumed first),
  stopping as soon as the balance is covered.
- A sweep decrements each advance's balance, increases the bill's `PaidAmount`, and recomputes
  `BalanceAmount`/`Status` (`Paid` when fully covered, else `PartiallyPaid`) in the same
  transaction. No `BillPayment` row or receipt is written — no new cash moved.
- A bill with a zero balance, or an admission with no undrawn advances, is a no-op — nothing is
  logged and no partial state is written.
- Each sweep writes an `AdvanceAutoDrawn` activity-log entry naming the amount and bill number, so
  the patient's balance never changes without an audit trail explaining it.
- The manual `DrawAdvanceAsync` action still exists for an out-of-order draw against one specific
  advance — auto-drawdown supplements it, it doesn't replace it.
- The FIFO sweep decision is pure, unit-tested logic (`BillMath.PlanAdvanceSweep`) covering
  empty/zero-balance/ordering/partial-coverage cases — no database needed to verify the rule.
- An IP bill shows a credit-exposure warning banner (balance minus available advances against the
  configured, non-blocking credit limit) — informational only, never a hard stop.

### US-BILL-8: Close the day's collections by payment mode
**As a** billing supervisor, **I want to** reconcile the day's system-recorded collections against
what the cashier physically counted, per payment mode, and close the day, **so that** variances
are caught daily rather than discovered later.
**Acceptance Criteria**
- Once a branch/date is closed, no new bill or payment can be recorded against that date.
- Variance = declared − system, computed per mode; an unfilled declared amount defaults to 0, not
  silently to "matches system."
- Closing an already-closed day throws rather than silently double-closing.

### US-BILL-9: Correct a finalized bill without editing history
**As a** billing supervisor, **I want to** issue a credit note against specific lines of a paid
bill (with approval) instead of editing the original bill, **so that** the original billed amount
stays intact for audit while the patient's balance still corrects.
**Acceptance Criteria**
- A credit note goes through the same two-step request → approve flow as a discount/refund.
- Approving decreases `TotalAmount` and recomputes `BalanceAmount`; any resulting overpayment is
  returned separately via the ordinary Refund workflow, not folded into the credit note.
- Append-only bill notes can be added at any time as a running remark log (no edit/delete).

### US-BILL-10: Park a bill mid-workflow and resume it later
**As a** billing executive, **I want to** park an incomplete bill and unpark it later, **so that**
an interrupted billing session (patient stepped away, price still being confirmed) doesn't block
the counter or get lost.
**Acceptance Criteria**
- A parked bill blocks edits/payments/discounts the same as a paid/cancelled bill until unparked.
- Unparking recomputes status from paid-vs-total amount, not a stale pre-parking flag.

### US-BILL-11: Run a cashier shift with opening float and supervised cash handover
**As a** cashier, **I want to** open a shift with a declared opening float, collect payments during
my shift, then close it and hand the physical cash to a supervisor, **so that** cash accountability
is tracked per person, not just per branch-day.
**Acceptance Criteria**
- Only one open shift per user at a time (enforced by a unique index).
- Closing a shift groups that shift's tagged payments by mode into system totals; the cashier
  declares actuals and a variance is computed, mirroring day close's shape.
- A supervisor's "Receive" action closes the physical handover loop.

### US-BILL-12: Produce a pre-admission cost estimate for a patient
**As a** billing executive, **I want to** build a cost estimate from a reusable package template
or ad hoc, print it, and copy it into a fresh draft, **so that** patients get transparent expected
costs before committing to a procedure.
**Acceptance Criteria**
- Estimate line rates resolve through the exact same tariff logic as a real bill.
- An estimate never auto-converts into a real bill — front desk creates the real bill separately
  once the patient proceeds.

### US-BILL-13: Run detailed billing operational and revenue reports
**As a** billing supervisor or finance user, **I want to** run ageing, ward-wise outstanding, day
book, cancelled-bill, concession, refund, advance, tariff list, doctor payout, cashier
settlement, and service-category breakup reports — each exportable to CSV/Excel/PDF, **so that** I
can analyze billing operations without manual spreadsheet work.
**Acceptance Criteria**
- Every report tab has a date-range filter (where applicable) and CSV/Excel/PDF export buttons
  that trigger a real file download.
- A role scoped only to OP bills (via `BILLING.BILL.VIEWOP` without `VIEWIP`) never sees IP or
  Emergency bill rows in any of these reports, and gets a clear 403 (not a silent empty result)
  if they try to filter directly to a type they don't hold.
- Every flat report renders through one shared `ReportResultTable`, so column behavior is
  identical everywhere: a **Columns picker** to show/hide columns and a **Rows per page**
  selector, rather than a fixed hand-rolled table per report.
- The **Ageing** report takes an as-of date (it previously had no date parameter at all, silently
  ageing against "now" only).
- On-screen search results and the exported file are built from one shared filter-builder per
  report, so an export can never disagree with what the user just looked at.

### US-BILL-14: Find a patient's OP encounter and open or raise their bill
**As a** cashier or billing executive, **I want to** find the visit by patient details — name,
reg no, mobile, doctor, gender, IP number, date range — instead of typing an OPD Visit ID,
**so that** I can bill a patient who is standing at the counter without asking anyone for an
internal ID.
**Acceptance Criteria**
- The search runs over **visits, not bills**, so a not-yet-billed visit still appears (an unbilled
  visit has no bill row to search).
- Each result row shows whether a bill already exists, and the row action either opens that bill or
  generates a new one — the same duplicate guard applies, so two cashiers double-clicking can't
  produce two bills for one visit.
- Three legacy checkboxes map onto existing concepts, not new schema: **MRD only** (patient has an
  MRD record file), **EMR only** (patient has at least one clinical note), and **OP multiple visit
  package** (MHC checkup-package visits).
- Branch scoping comes from the visit's own query filter — a user never sees another branch's
  encounters.
- The **Concession** report is likewise a real search (reg no / patient name / OT no / bill no on
  top of the date range), not a flat list, and shows both the requester and the approver of each
  concession rather than collapsing them into one "user" column.

### US-BILL-15: Maintain the tariff catalog in bulk and revise bed charges over time
**As a** hospital administrator, **I want to** export the whole tariff list, edit it in a
spreadsheet, and import it back, and to record dated bed-charge revisions per room type,
**so that** an annual price revision across hundreds of services isn't hundreds of screen edits.
**Acceptance Criteria**
- Export emits CSV/Excel in the exact column shape import expects back
  (`TariffCode,TariffName,PatientCategoryCode,EffectiveFrom,EffectiveTo,ServiceCode,RoomTypeCode,Rate`),
  so a round-trip needs no manual reshaping.
- Import is an **upsert**: an unknown tariff code is created, a known one has its header updated
  once, and each service/room-type rate row is inserted or updated in place.
- A row referencing an unknown service, room type, or patient-category code is **skipped with a
  line-numbered error** and the rest of the file still imports — one bad row never aborts the
  batch. The response reports tariffs created / updated / rate rows applied plus every error.
- Import is separately permissioned (`TariffImport`) from ordinary tariff viewing/editing.
- A room type carries dated bed-charge revisions (rate + effective from/to, validated so
  effective-to is never before effective-from). Changing the current rate never rewrites history:
  an admission's bed charge stays at the rate snapshotted onto the bed allocation at allocation
  time.

---

## 6. Laboratory

*Epic: test catalog, order-to-result pipeline with automatic flagging, and critical-value safety.*

### US-LAB-1: Maintain the lab test and profile catalog
**As a** pathologist, **I want to** define lab tests with their parameters and age/sex-banded
reference ranges, and bundle related tests into orderable profiles, **so that** ordering and
result entry are consistent and correctly interpreted.
**Acceptance Criteria**
- Every test — panel or not — has at least one parameter row, so result entry never special-cases
  a single-parameter test.
- A profile, once ordered, expands into one order item per member test.

### US-LAB-2: Order lab tests for a patient
**As a** lab receptionist or doctor, **I want to** create a lab order (from an OP visit or a direct
walk-in) selecting one or more tests/profiles, **so that** the sample-collection and result
pipeline begins.
**Acceptance Criteria**
- The order number is globally unique, sequence-generated per branch/year.
- If created with a loose `ClinicalOrderId` reference, completing every item marks that clinical
  order `Completed`.

### US-LAB-3: Collect a specimen and route it through receipt/rejection
**As a** lab technician, **I want to** collect a specimen covering every same-type test on an
order in one draw, receive it in the lab, and reject it with a reason if unsuitable, **so that**
sample quality issues are caught before they corrupt a result.
**Acceptance Criteria**
- One `Sample` row can cover multiple order items sharing the same sample type on one order.
- Rejecting a sample bounces every order item that used it back to `Ordered` status, so it
  reappears on the collection worklist for a fresh draw — no separate rejection table needed.

### US-LAB-4: Enter, verify, and approve a result through separation of duties
**As a** lab technician (entry), then a pathologist (verify/approve), **I want to** each perform
my own distinct step of the result pipeline, **so that** no single person can enter and
self-certify a result.
**Acceptance Criteria**
- Entry, verify, and approve are each gated by their own permission code.
- Numeric parameter values are auto-flagged Normal/High/Low/Critical using sex-specific-over-
  agnostic and age-bounded-over-unbounded range resolution; text/dropdown parameters are always
  recorded Normal (no free-text auto-flagging).
- Re-entering a result before verification replaces the previous values wholesale.
- Approving the final item on an order marks the linked clinical order `Completed`.

### US-LAB-5: Get alerted immediately on a critical result
**As a** pathologist or the ordering doctor, **I want to** be notified the moment a critical value
is entered and be able to acknowledge it, **so that** a life-threatening result is never missed
in the normal result-review queue.
**Acceptance Criteria**
- A `CriticalAlert` row is raised automatically for every result value flagged Critical, at entry
  time — before verification or approval.
- Acknowledgment is tracked separately from approval, since they're different responsibilities.

### US-LAB-6: Work a department's collection and result worklists
**As a** lab technician, **I want to** see everything awaiting collection or result entry for my
department, **so that** I can work through my queue systematically rather than searching order by
order.
**Acceptance Criteria**
- The collection worklist groups items by (order, sample type) so a multi-select never spans more
  than one physical draw.
- The department worklist filters by department for entry; separate verify/approve queues filter
  by result status.

### US-LAB-7: Print a cumulative lab report and monitor turnaround time
**As a** pathologist or lab manager, **I want to** print an approved report for a patient and run
a turnaround-time report across a date range, **so that** patients get a proper report and the
lab can monitor its own performance.
**Acceptance Criteria**
- The report PDF only reflects approved results.
- TAT is computed from order date to result-approval time and averaged over the selected range.

### US-LAB-8: Cancel an order or a single unstarted item
**As a** lab receptionist, **I want to** cancel an entire order or just one item on it (if not yet
started), **so that** mistaken or no-longer-needed orders don't clutter active worklists.
**Acceptance Criteria**
- Cancelling recomputes the order's rollup status from its remaining items.

---

## 7. Pharmacy

*Epic: multi-store stock engine, purchase-to-GRN, POS sale with FEFO, issues, and inventory
control.*

### US-PHARM-1: Receive purchased stock into a store via GRN
**As a** pharmacy purchase officer, **I want to** raise a purchase request, convert it to a
purchase order, and post a Goods Receipt Note against the vendor's invoice, **so that** newly
purchased drugs become available stock with full batch/expiry tracking.
**Acceptance Criteria**
- Posting a GRN (transactional) creates or tops up a `Batch` per line and writes a `PurchaseIn`
  entry to the append-only stock ledger — the ledger is never updated or deleted, only appended.
- PR approval and PO approval are pure status gates with no stock effect; only GRN posting moves
  stock.
- A GRN can be created and posted without a prior PO (a direct, unplanned purchase).

### US-PHARM-2: Sell drugs over the counter with automatic FEFO batch allocation
**As a** pharmacist, **I want to** ring up an OP sale where the system automatically allocates
stock from the earliest-expiring batches first, **so that** near-expiry stock is used up before
it's wasted, without me manually picking batches.
**Acceptance Criteria**
- A single sale line can transparently split across multiple batches if one batch can't cover the
  full quantity; each allocation gets its own sale-item row and ledger entry.
- If total available stock is insufficient, the sale is rejected rather than partially fulfilled.
- Cancelling a sale reverses stock via a `SaleReturnIn` ledger row (transactional), and the
  consumption report correctly nets it back out — a fully cancelled sale reports as zero consumed,
  not as a sale.
- A GST invoice PDF is generated per sale.

### US-PHARM-3: Return sold or issued items back to stock
**As a** pharmacist, **I want to** process a partial-quantity sales return or an issue return,
**so that** returned stock becomes available again and the audit trail is preserved.

### US-PHARM-4: Issue drugs to a department or for emergency use
**As a** pharmacist, **I want to** issue drugs to a hospital department or for an emergency case
(with an optional link to an approved indent), **so that** ward-level and emergency drug use is
tracked outside the patient-facing POS flow.
**Acceptance Criteria**
- An issue against an indent validates that the issued quantity doesn't exceed the indent line's
  approved quantity.

### US-PHARM-5: Request → approve → issue a department indent
**As a** ward staff member, **I want to** submit an indent requesting drugs/items for my
department, have it approved (with quantities possibly trimmed), then have pharmacy fulfil it as
a stock issue, **so that** ward-level drug requisitioning is governed rather than ad hoc.
**Acceptance Criteria**
- An approver can trim any line's quantity below what was requested.
- The indent flips to `Issued` once a fulfilling issue is created against it.

### US-PHARM-6: Correct stock discrepancies through an approval gate
**As a** pharmacist, **I want to** request a reason-coded stock adjustment and have a manager
approve it before it affects the ledger, **so that** stock corrections are deliberate and
reviewable, not silent.
**Acceptance Criteria**
- Requesting an adjustment writes nothing to stock; only approval posts the ledger movement.

### US-PHARM-7: Transfer stock between stores or branches
**As a** pharmacist or store keeper, **I want to** dispatch stock from one store and receive it at
the destination (same branch or a different one), **so that** stock can be rebalanced across
locations.
**Acceptance Criteria**
- Dispatch writes a `TransferOut` ledger entry at the source; receipt creates/tops up the
  destination batch and writes `TransferIn` — two distinct transactional steps, `Pending →
  Dispatched → Received`.

### US-PHARM-8: Run a physical stock audit (count sheet) and post variances
**As a** pharmacy manager, **I want to** snapshot a store's system quantities, record physical
counts, and complete the audit to automatically post the variance to stock, **so that** the
ledger reconciles with what's physically on the shelf.
**Acceptance Criteria**
- Completing an audit posts an `AdjustmentIn`/`AdjustmentOut` per non-zero variance line —
  identical declared-vs-system posture to Billing's day close.

### US-PHARM-9: Get proactively warned about near-expiry and low stock
**As a** pharmacy manager, **I want to** view a list of batches nearing expiry and drugs/items
below their reorder level, **so that** I can act before stock expires or runs out.
**Acceptance Criteria**
- Alerts cover both drugs and Phase 13's general (non-drug) items in one unified low-stock list.

### US-PHARM-10: Run a department-wise drug consumption report
**As a** pharmacy manager, **I want to** see net consumption (sales + issues, minus their
respective returns) per drug, broken down by department, **so that** I can plan procurement
accurately.
**Acceptance Criteria**
- Figures net out returns against outbound movements so a fully-reversed transaction never
  inflates the consumption total.

---

## 8. IPD (Inpatient)

*Epic: admission through discharge — beds, orders, charting, and the 4-way discharge clearance.*

### US-IPD-1: Admit a patient with bed allocation and consent
**As a** front-desk or ward staff member, **I want to** admit a patient — picking a primary
consultant, a vacant bed, admission type, and capturing consent and an initial deposit — **so
that** the inpatient stay is properly anchored from day one.
**Acceptance Criteria**
- Admission, its consultant mapping, its bed allocation, and the deposit/consent all commit in
  one transaction.
- Bed allocation is protected against double-booking by both an application check and a partial
  unique DB index — a losing concurrent request gets a clear "bed no longer available" error, it
  never silently double-books.
- Admitting from an OP visit or an Emergency visit pre-fills the patient and links the source
  visit automatically via the same modal, opened in place.

### US-IPD-2: Transfer a patient's bed or consultant mid-stay
**As a** ward staff member or doctor, **I want to** move a patient to a different ward/room/bed or
change their assigned consultant, **so that** clinical reality (e.g., ICU step-down, consultant
handover) is reflected without losing history.
**Acceptance Criteria**
- Every bed the admission has occupied stays in `bed_allocations` history; a transfer ends the
  current row and inserts a new one.
- A consultant transfer ends the prior mapping row and inserts a new one — full consultant history
  reads off one table.

### US-IPD-3: See live bed occupancy across the ward
**As a** ward manager, **I want to** view a ward-grouped bed grid with live status and toggle a
bed's status (vacant/reserved/cleaning/blocked) with one click, **so that** I can manage bed
turnover in real time.
**Acceptance Criteria**
- A currently-occupied bed cannot have its status toggled directly — only admit/transfer/discharge
  changes an occupied bed.
- An occupancy dashboard shows aggregate stats alongside the grid.

### US-IPD-4: Place a unified doctor order (medication, lab, radiology, diet, nursing)
**As a** doctor, **I want to** place any type of inpatient order from one unified order sheet,
**so that** the care team has a single place to see everything ordered for this admission.
**Acceptance Criteria**
- Medication and diet orders are structured (drug/dose/route/frequency, or diet type/instructions);
  lab/radiology/nursing orders stay free text for this phase.
- A structured medication order is what Nursing's MAR later generates a due-time schedule from.

### US-IPD-5: Chart basic inpatient clinical entries
**As a** doctor or nurse, **I want to** record progress notes, vitals, intake/output entries, and
procedure notes against the admission, **so that** the inpatient clinical course is documented
chronologically.
**Acceptance Criteria**
- Each entry type is a simple repeatable, timestamped list — no draft/approval workflow, since it
  doesn't affect stock/billing/bed state.

### US-IPD-6: Generate and keep an IP bill current with bed charges
**As a** billing executive, **I want to** generate the IP bill for an admission and refresh its bed
charges to reflect the full stay so far, **so that** the bill accurately bills for every day (or
part-day) occupied at the room's rate.
**Acceptance Criteria**
- Bed-charge lines recompute wholesale from bed-allocation history — one line per segment, in
  whole days (any part of a day counts as a full day), at that segment's rate snapshotted when the
  bed was allocated.
- Every other billing action (services, discounts, payments, refunds, cancellation) reuses the
  exact same OP billing code path — nothing IP-specific was duplicated.
- A **nightly job recomputes every still-open IP bill's bed charges** without anyone clicking, so
  a running bill reflects the stay to date each morning. The manual "recompute now" action remains
  for when staff need it immediately (e.g. right before discharge).
- The job skips bills that are `Cancelled`, `Paid`, or `Parked` — only genuinely open IP bills are
  touched.
- Each bill is refreshed in **its own transaction**, so one admission's bad state can't block
  every other bill in the branch; the recompute is idempotent, so a retried run is harmless.
- The same pass auto-sweeps the admission's deposits against the recomputed balance (US-BILL-7),
  so bed charges and deposit drawdown never drift apart by a day.
- A run that touched at least one bill writes a `BedChargesAutoRefreshed` activity-log entry with
  the count; a run with nothing to do logs nothing.

### US-IPD-7: Run the 4-way discharge clearance and produce a discharge summary
**As a** ward staff member, doctor, and department heads, **I want to** request discharge, clear
Billing/Pharmacy/Nursing/Final-Investigation checkpoints, have the doctor write and approve a
discharge summary, then finalize the discharge, **so that** no patient leaves with an outstanding
clinical or administrative loose end.
**Acceptance Criteria**
- Requesting discharge auto-creates all four clearance rows.
- Final discharge is blocked until every clearance row is individually marked cleared.
- The discharge summary must be doctor-approved before it can be finalized or before the PDF shows
  an "approved by" line.
- Finalizing discharge (transactional) releases the bed and closes the admission.
- Admission slip, discharge summary, and discharge slip are all printable as PDFs.

### US-IPD-8: Run IP and ward-occupancy reports
**As a** ward manager or hospital administrator, **I want to** run an admission-date-range IP
report (summary or per-admission detail with length of stay) and a historical ward-occupancy
report (bed-days occupied vs. available), **so that** I can analyze capacity utilization over
time, not just the live snapshot.
**Acceptance Criteria**
- Ward occupancy requires a date range (open-ended ranges can't compute bed-days-available) and
  exports CSV/Excel/PDF.

---

## 9. Nursing

*Epic: structured bedside charting, risk scoring, the Medication Administration Record, shift
handovers, and incident reporting.*

### US-NURS-1: Complete structured nursing assessments and shift notes
**As a** nurse, **I want to** record a structured admission-scoped assessment and file shift-
tagged (Morning/Evening/Night) nursing notes, **so that** a nurse's clinical observations are
captured with a bedside, shift-aware lens distinct from the doctor's progress notes.

### US-NURS-2: Score a patient's Pain, Fall Risk (Morse), and Bed Sore (Braden) risk
**As a** nurse, **I want to** enter the standard scale items for pain, fall risk, or bed-sore risk
and get an automatically computed total score and risk band, **so that** risk-level scoring is
consistent and not left to manual arithmetic.
**Acceptance Criteria**
- Total score and risk band are computed server-side from the submitted per-item scores, not
  trusted from the client.

### US-NURS-3: Maintain a nursing care plan and file a transfer note
**As a** nurse, **I want to** record a problem/goal/interventions care plan and resolve it once
addressed, and file a handover note alongside a ward/bed transfer, **so that** ongoing nursing
concerns and transfer context are documented.
**Acceptance Criteria**
- A resolved care plan cannot be re-opened.
- A transfer note can exist even without an accompanying bed move (e.g., consultant-only change).

### US-NURS-4: Administer medications against a generated MAR schedule with double-verification
**As a** nurse, **I want to** see today's due-time medication schedule generated from the doctor's
orders and record each dose as administered/skipped/refused, with a second nurse's verification
required for high-alert drugs, **so that** medication administration is safe, timely, and
tamper-evident.
**Acceptance Criteria**
- The due-time schedule is generated lazily and idempotently the first time it's requested for a
  given admission/date, mapping common frequency codes (OD/BD/TID/QID/Q6H/Q8H/Q12H/HS/STAT) to
  fixed clock times.
- Skip/Refuse always requires a reason.
- Administering a high-alert-flagged dose is refused unless a second, different user is supplied
  as verifier — a nurse cannot self-verify.
- An already-actioned schedule row cannot be re-actioned.

### US-NURS-5: Hand over the ward at shift change with per-patient SBAR
**As a** nurse, **I want to** create one ward-level, per-shift handover record summarizing every
current patient's Situation/Background/Assessment/Recommendation, **so that** the incoming shift
gets a structured, complete briefing rather than a verbal-only handoff.
**Acceptance Criteria**
- Only one handover per ward/date/shift is allowed.
- Creation pulls the ward's currently admitted patients to prefill per-patient SBAR rows.

### US-NURS-6: Report, review, and close a patient/staff safety incident
**As a** nurse, **I want to** report an incident (with or without a specific patient involved),
have it reviewed, and see it closed out, **so that** safety events are captured and tracked to
resolution.
**Acceptance Criteria**
- Lifecycle is Reported → UnderReview → Closed; patient/admission linkage is optional (staff/
  visitor/general incidents are supported).

---

## 10. EMR / Clinical Records

*Epic: the unified longitudinal patient record — timeline, histories, allergies, problem list,
notes, and attachments.*

### US-EMR-1: View a patient's unified chronological timeline
**As a** doctor, **I want to** see one merged, chronological view of a patient's visits,
admissions, approved lab results, prescriptions, procedures, discharge summaries, notes, and
attachments, **so that** I understand the patient's full history without hopping between modules.

### US-EMR-2: Maintain medical, surgical, family, and immunization histories
**As a** doctor, **I want to** record and review a patient's ongoing medical/surgical/family
history and immunization record, **so that** this context is available at every future
consultation regardless of which branch sees the patient.
**Acceptance Criteria**
- Every EMR table is patient-scoped, not branch-scoped — a history recorded at one branch is
  visible from every branch, since clinical safety data must never be silently hidden by
  location.

### US-EMR-3: Record allergies and be warned automatically when prescribing a conflicting drug
**As a** doctor, **I want to** record a patient's known allergies and have any future prescription
automatically cross-checked against them, **so that** a documented allergy is never missed at
the point of prescribing.
**Acceptance Criteria**
- The allergy banner surfaces every active allergy; deactivating one uses the standard active-flag
  mechanism.
- The prescription cross-check is a real backend call (not a UI-only guess), matched by
  case-insensitive substring against candidate drug names, wired live into OPD's prescription tab.
- The check is advisory only — it never blocks saving a prescription.

### US-EMR-4: Curate an active/resolved problem list
**As a** doctor, **I want to** maintain a manually-curated list of the patient's active and
resolved diagnoses, **so that** there's a persistent clinical problem summary distinct from any
single visit's diagnosis entries.

### US-EMR-5: Write ad-hoc clinical notes and upload external reports
**As a** doctor, **I want to** write a free-form clinical note (optionally linked to a visit or
admission) and upload external reports or scanned documents, **so that** clinical context that
doesn't fit a structured form still lives in the patient's record.

### US-EMR-6: Browse prescription, investigation, procedure, and discharge-summary history
**As a** doctor, **I want to** open dedicated history views listing every past prescription,
investigation, procedure, and discharge summary for a patient, **so that** I can review a specific
category of history without scrolling the full timeline.

### US-EMR-7: View a lab parameter's trend over time
**As a** doctor, **I want to** pick a patient and a lab test and see a trend chart of that test's
parameter values (with flag coloring) across every approved result, **so that** I can spot a
worsening or improving trend at a glance rather than comparing individual reports.
**Acceptance Criteria**
- Only numeric values plot on the chart; qualitative results are parsed where possible and
  silently excluded from the graph otherwise, not erroring.

---

## 11. Radiology / RIS

*Epic: the radiology mirror of Lab's pipeline, plus modality scheduling and a contrast-consent
safety gate.*

### US-RAD-1: Maintain the radiology test catalog and report templates
**As a** radiologist, **I want to** define orderable radiology tests (linked to a modality) and
reusable Findings/Impression report templates, **so that** ordering and reporting are consistent.

### US-RAD-2: Order a radiology test for a patient
**As a** doctor or radiology receptionist, **I want to** create a radiology order from an OPD
visit, an IPD admission, or directly, **so that** imaging is requested with full traceability back
to its source.
**Acceptance Criteria**
- Each order item gets a generated accession number (PACS placeholder) immediately.

### US-RAD-3: Schedule the imaging appointment by modality
**As a** radiology technician, **I want to** see items awaiting scheduling filtered by modality
and assign them a date/time, **so that** the imaging suite's day is organized by machine type.

### US-RAD-4: Capture images only after contrast consent is on file
**As a** radiology technician, **I want to** be blocked from marking a contrast-requiring study as
image-captured until contrast consent has been recorded, **so that** a patient is never imaged
with contrast media without documented consent.
**Acceptance Criteria**
- The backend rejects the image-capture action with a clear error for a contrast test lacking a
  consent row; a non-contrast test has no such gate.
- The frontend disables the "Mark captured" action and surfaces a "Capture consent" action instead,
  mirroring the backend rule.

### US-RAD-5: Enter, verify, and approve a radiology report
**As a** radiology technician (entry) and radiologist (verify/approve), **I want to** each perform
my step of the report pipeline, optionally starting from a template, **so that** reporting
separation of duties matches Lab's safety posture.
**Acceptance Criteria**
- Selecting a template prefills Findings/Impression text, but the submitted (possibly edited) text
  is always what's saved.
- The PDF report generator refuses to run before the report is Approved.

### US-RAD-6: Monitor radiology turnaround time
**As a** radiology manager, **I want to** run a date-range TAT report, **so that** I can monitor
department performance the same way Lab does.

### US-RAD-7: Cancel a radiology order or a single item
**As a** radiology receptionist, **I want to** cancel an order or an individual unstarted item,
**so that** unnecessary orders don't clutter the technician's worklist.

---

## 12. Stores & Inventory

*Epic: widening Pharmacy's stock engine to carry general (non-drug) items via a department indent
workflow.*

### US-STORES-1: Classify a store by what it holds
**As a** store keeper, **I want to** tag each store with a type (Pharmacy/General/Linen/
Stationery/Consumables/BiomedicalSpares), **so that** stores are organized by purpose even though
a general store and a drug store share the same underlying engine.

### US-STORES-2: Manage general (non-drug) items through the same purchase and stock pipeline
**As a** store keeper, **I want to** purchase, receive, and track batches of linen, stationery,
consumables, or biomedical spares using the same PR→PO→GRN pipeline drugs use, **so that** the
hospital has one inventory system, not two parallel ones.
**Acceptance Criteria**
- Every batch/PR/PO/GRN line item points at exactly one of a Drug or a general Item, never both
  and never neither.
- Expiry date and MRP are optional on general items (a bed sheet has neither) but still required
  for drugs.

### US-STORES-3: Route a department's supply request through approval before it's issued
**As a** ward staff member, **I want to** submit an indent for supplies my department needs and
have it approved (possibly with trimmed quantities) before being fulfilled as a stock issue,
**so that** general-item requisitioning is governed the same way pharmaceutical issuing is.
**Acceptance Criteria**
- Approving an indent doesn't move stock by itself; only the resulting issue does, and the issued
  quantity per product can't exceed what was approved.

### US-STORES-4: Run a department-wise consumption report across drugs and items
**As a** store keeper or pharmacy manager, **I want to** see consumption broken down by department
and by product (drug or general item), **so that** procurement planning covers the whole
inventory, not just pharmaceuticals.

---

## 13. Operation Theatre

*Epic: conflict-checked surgery booking, the WHO Surgical Safety Checklist, and implant/consumable
usage that posts to both stock and the patient's bill.*

### US-OT-1: Book a surgery against an admission without double-booking resources
**As an** OT coordinator, **I want to** book a surgery (theatre, surgeon, anaesthetist, time
window) anchored to an existing IPD admission, and be blocked if it conflicts with another
booking, **so that** theatre time, surgeons, and anaesthetists are never accidentally double-
booked.
**Acceptance Criteria**
- A new or rescheduled booking is rejected if its time window overlaps any existing non-cancelled/
  non-postponed booking sharing the same theatre, surgeon, *or* anaesthetist — the specific
  colliding resource is named in the error.
- Every surgery is anchored to an admission — there is no OP/day-care booking path.

### US-OT-2: Assign a full surgical team
**As an** OT coordinator, **I want to** add team members beyond the primary surgeon/anaesthetist
(assistant surgeon, scrub/circulating nurses), **so that** the full team on a case is documented.

### US-OT-3: Complete the WHO Surgical Safety Checklist and gate the case through it
**As an** OT team member, **I want to** complete Sign In / Time Out / Sign Out phases and have the
system block the case from starting or completing without them, **so that** the WHO checklist is
enforced, not just documented as a formality.
**Acceptance Criteria**
- Starting the case (`Scheduled → InProgress`) requires consent verification and the Sign In phase
  complete.
- Completing the case (`InProgress → Completed`) requires the Sign Out phase complete.

### US-OT-4: Record pre-anaesthesia, intra-op, operative, and post-op clinical notes
**As a** doctor or anaesthetist, **I want to** fill in the standard clinical note templates for
each phase of a surgical case, **so that** the perioperative record is complete.
**Acceptance Criteria**
- Each note type is a find-or-create ("Save" upserts) 1:1-per-booking record.

### US-OT-5: Record implant and consumable usage that decrements stock and bills the patient
**As an** OT nurse, **I want to** record which implants (batch/serial-tracked) and consumables
were used during a case, **so that** stock is decremented and the correct charge posts to the
patient's IP bill automatically, in one action.
**Acceptance Criteria**
- Stock decrement and bill-item posting happen inside one transaction — they succeed or fail
  together.
- The charged rate snapshots the batch's MRP (or purchase rate if MRP isn't set) at usage time.

### US-OT-6: Bill theatre time, surgeon fees, and anaesthetist fees
**As a** billing executive, **I want to** add theatre/surgeon/anaesthetist charges to the IP bill
using the hospital's own configured service rates, **so that** these are billed at governed,
tariff-resolved rates rather than being hardcoded.

### US-OT-7: Run the OT register report
**As an** OT coordinator, **I want to** run a date-range OT register listing every case, **so
that** I can produce the statutory theatre record.

---

## 14. ICU / Critical Care

*Epic: wholesale extension of IPD/Nursing for critical-care wards — vitals widened, ventilator/
infusion charting, severity, and configurable critical-alert rules.*

### US-ICU-1: Chart ICU-specific vitals and get evaluated against critical-alert rules live
**As an** ICU nurse, **I want to** record a patient's vitals (including GCS/MAP/CVP/urine output)
and have them automatically checked against the branch's configured critical-alert thresholds,
**so that** a dangerous reading raises an alert the moment it's charted, not on a later review.
**Acceptance Criteria**
- Every active threshold rule for the branch is evaluated against the new reading before the
  recording transaction returns, raising one alert per breach.
- The same underlying vitals table serves every ward, so a patient's vitals timeline stays
  continuous across a ward↔ICU bed transfer.

### US-ICU-2: Configure the hospital's own critical-alert thresholds
**As an** ICU in-charge, **I want to** define which vitals parameter/operator/threshold
combinations count as a critical alert, at what severity, **so that** alerting reflects our own
clinical protocols rather than a hardcoded rule set.

### US-ICU-3: Chart a ventilator session with periodic observations
**As an** ICU nurse or respiratory therapist, **I want to** start a ventilator session, record
periodic settings/pressure observations, and stop the session when the patient is weaned, **so
that** the ventilation record is a proper flowsheet, not a single note.
**Acceptance Criteria**
- Observations can only be charted against a currently-running (not-yet-stopped) session.
- The dashboard's "ventilated count" is exactly the count of sessions with no stop time.

### US-ICU-4: Chart an infusion with titration history
**As an** ICU nurse, **I want to** start an infusion (catalog drug or a named non-catalog fluid),
record rate changes over time, and stop it, **so that** the titration history is a complete
flowsheet.

### US-ICU-5: Record a clinician-assigned severity level over time
**As an** ICU doctor, **I want to** assign a Mild/Moderate/Severe/Critical acuity level at points
in time during the stay, **so that** the severity trend for this admission is visible on the
dashboard.

### US-ICU-6: See the ICU census and act on unacknowledged alerts
**As an** ICU in-charge, **I want to** see a live census (occupancy, ventilated count, per-patient
severity/alert summary) and acknowledge open critical alerts, **so that** I have situational
awareness of the whole unit at a glance.

---

## 15. Emergency / Casualty

*Epic: a workflow layered over Patients/Lab/Radiology/IPD/Billing for rapid ER intake, triage-
driven queueing, statutory registers, and post-facto billing.*

### US-ER-1: Rapidly register a patient, including an unknown/unconscious arrival
**As an** ER front-desk staff member, **I want to** register an ER visit with minimal required
fields (even placeholder identity for an unknown patient), **so that** treatment isn't delayed by
paperwork; the record can be reconciled to a real identity later via the existing duplicate/merge
tools.

### US-ER-2: Triage a patient and drive the queue by acuity, not arrival order
**As a** triage nurse, **I want to** assign an ESI level (1–5) with chief complaint and vitals,
**so that** the sickest patients are seen first regardless of check-in order.
**Acceptance Criteria**
- The queue sorts untriaged visits first (an unseen patient is the most urgent unknown), then ESI
  1 through 5, then arrival time within the same priority.
- Re-triaging a patient updates the same triage row in place.

### US-ER-3: Complete the ER clinical assessment and record a disposition
**As an** ER doctor, **I want to** record history/examination/diagnosis/treatment and set a
disposition (Discharged / Admitted-Converted / Transferred Out / LAMA / Brought Dead /
Observation), **so that** every ER visit resolves to a clear clinical outcome.
**Acceptance Criteria**
- Selecting `TransferredOut` reveals and requires the receiving facility and transfer summary.
- Setting any disposition marks the visit `Disposed`.

### US-ER-4: Order STAT lab and radiology tests without leaving the ER workflow
**As an** ER doctor, **I want to** place a STAT-priority lab or radiology order directly linked to
this ER visit, **so that** urgent results come back fast and stay traceable to the ER encounter.
**Acceptance Criteria**
- This reuses the existing Lab/Radiology order-creation endpoints with `EmergencyVisitId` set and
  `Priority = Stat` — no separate ER-specific ordering engine.

### US-ER-5: Maintain the statutory MLC and accident registers
**As an** ER staff member, **I want to** record Medico-Legal Case details (police station,
informed time, FIR number, brought-by details) and accident case details, **so that** statutory
reporting obligations are met.
**Acceptance Criteria**
- A visit can be an accident without being an MLC and vice versa — the two registers are
  independent.

### US-ER-6: Allocate a short-stay observation bed
**As an** ER staff member, **I want to** allocate a bed from a ward flagged for observation use
and release it when the patient moves on, **so that** short-stay ER patients are bed-tracked
without being formally admitted.
**Acceptance Criteria**
- A bed can only have one active observation allocation at a time (enforced by a unique index).

### US-ER-7: Convert an ER visit to an IPD admission or bill it after the fact
**As an** ER doctor or billing executive, **I want to** convert a disposed "Admitted" ER visit
directly into an IPD admission, and generate an empty, post-facto-friendly ER bill afterward,
**so that** admission and billing aren't blocked on paperwork completed at the point of crisis
care.
**Acceptance Criteria**
- Conversion reuses the standard IPD admission flow, pre-filled and linked via
  `ReferringEmergencyVisitId` — no separate "convert" endpoint.
- The ER bill starts empty; charges are added afterward through the same manual-item path used
  elsewhere in Billing.

---

## 16. MRD (Medical Records)

*Epic: the physical file's lifecycle, request/issue/return, document scanning, coding, discharge
audit, and retention/destruction.*

### US-MRD-1: Open and track a physical record file per admission
**As an** MRD officer, **I want to** open a physical file for a discharged admission and see its
current location/custodian at a glance, **so that** I always know where a paper file is.
**Acceptance Criteria**
- Every location/custodian change writes an append-only movement row and updates the file's
  current-location snapshot in the same action.

### US-MRD-2: Process file issue/return and internal/external/legal record requests
**As an** MRD officer, **I want to** approve or reject a request for a file (whether it's an
internal issue, an external party's request, or a legal request), then issue and later receive it
back, **so that** every hand-off of a physical file is tracked and overdue files are visible.
**Acceptance Criteria**
- Lifecycle: Requested → Approved/Rejected → Issued → Returned.
- Overdue status is computed on read (issued and past expected return date), not stored, so it's
  always current.

### US-MRD-3: Scan and attach documents against an admission
**As an** MRD officer, **I want to** upload scanned documents typed by a document-type master
(discharge summary, consent form, lab report, etc.), **so that** the digital record is complete
alongside the physical file.

### US-MRD-4: Work the discharged-case ICD coding worklist
**As an** MRD coder, **I want to** see discharged admissions pending ICD coding and mark them
coded with optional notes, **so that** coding completion is tracked across the hospital's case
mix.

### US-MRD-5: Run the discharge file completeness audit
**As an** MRD officer, **I want to** check off whether the discharge summary is signed and
consents/progress notes/investigation reports/nursing records are all present, **so that**
incomplete discharge files are caught and followed up.
**Acceptance Criteria**
- Completeness is recomputed automatically from the five checklist flags on every save — never a
  manually-set "done" toggle that can drift from the actual checklist state.

### US-MRD-6: Apply retention policy and process record destruction
**As an** MRD manager, **I want to** define retention policies per record type, see which files
are eligible for destruction, flag them, approve, and destroy them, **so that** records are
retained exactly as long as policy requires and no longer.
**Acceptance Criteria**
- Eligibility (discharge date + retention years) is computed on read, distinct from actually being
  flagged into the destruction register — flagging is a deliberate MRD action, not automatic.
- Destroying a register entry flips the underlying record file's status to Destroyed.

### US-MRD-7: Run MRD operational reports
**As an** MRD manager, **I want to** run coding-stats, file-movement-history, and incomplete-
records reports over a date range, **so that** I can monitor department throughput and
compliance.

---

## 17. Finance & Accounts

*Epic: Chart of Accounts, the single posting engine, payables/receivables, and financial
reporting — including auto-posting from Billing.*

### US-FIN-1: Maintain the Chart of Accounts
**As a** finance manager, **I want to** define accounts (Asset/Liability/Equity/Income/Expense)
with an optional parent for hierarchy, **so that** every posting has a governed destination.
**Acceptance Criteria**
- System accounts used by auto-posting rules are protected from rename/delete (looked up by
  stable code, not by database id).

### US-FIN-2: Post a manual journal, payment, or receipt voucher
**As a** finance executive, **I want to** enter a voucher with two or more debit/credit lines that
must balance, **so that** every financial event the system doesn't auto-post can still be
recorded correctly.
**Acceptance Criteria**
- Posting is rejected unless total debits equal total credits across at least two lines.
- Posting is immediate — there's no draft state, since a voucher is only submitted once it already
  balances.

### US-FIN-3: Correct a posting mistake without ever editing history
**As a** finance manager, **I want to** cancel a posted voucher and have a reversal voucher created
automatically (debits/credits swapped), **so that** the ledger remains a true, append-only history
even when mistakes happen.

### US-FIN-4: View an account's running ledger
**As a** finance executive, **I want to** open any account and see its running balance over a
chosen date range, **so that** I can trace exactly what's been posted to it.

### US-FIN-5: Raise and settle payables against posted GRNs
**As a** finance executive, **I want to** see a worklist of posted GRNs without a payable yet,
explicitly raise a payable, and record its payment, **so that** vendor invoices become AP entries
under Finance's own control, not automatically the instant a GRN posts.
**Acceptance Criteria**
- Paying a payable posts a real two-line voucher (Debit Accounts Payable / Credit the chosen
  bank/cash account).

### US-FIN-6: Raise and collect receivables against credit bills
**As a** finance executive, **I want to** see a worklist of credit bills without a receivable yet,
raise one, and record collections against it, **so that** corporate/credit billing has a proper
AR trail separate from what Billing itself tracks.
**Acceptance Criteria**
- Collecting a receivable posts a real voucher and updates Finance's own collected-amount
  sub-ledger — it does not touch the underlying Bill's paid amount, since the counter never
  collects a credit bill directly.

### US-FIN-7: See bill payments auto-post to the ledger
**As a** finance manager, **I want** every bill payment collected at the counter to automatically
post a balanced Debit-Bank/Credit-Revenue voucher, **so that** day-to-day cash collection doesn't
require manual journal entry once configured.
**Acceptance Criteria**
- Auto-posting fires from inside Billing's own payment-collection transaction.
- If the payment mode or the revenue account isn't configured yet, posting is a silent no-op — it
  never blocks the payment itself.

### US-FIN-8: Run core financial reports and the cross-branch day-close console
**As a** finance manager, **I want to** run cash/bank collection, revenue (by department/doctor),
GST outward register + HSN summary, outstanding-bills, and trial-balance reports, plus see every
branch's day-close status for one date in a single console, **so that** I have financial oversight
across the whole hospital group.
**Acceptance Criteria**
- The cross-branch console requires holding `REPORT.CROSSBRANCH.VIEW` in addition to the
  Finance-specific permission — a branch-scoped user never sees another branch's data.
- **Trial Balance** lists every account's cumulative debit and credit totals as of a chosen date
  (cumulative-to-date, not period activity), so `TotalDebit == TotalCredit` across the whole result
  is the literal proof the ledger balances.
- Debits and credits are shown as **raw sums, not netted to one side** — netting would hide the
  very imbalance the report exists to expose.
- Accounts with no ledger activity at all are omitted, matching how a real trial balance only lists
  posted-to accounts.
- Trial Balance is branch-scoped like every other Finance report; it is **not** a balance sheet or
  P&L — those remain deliberately unbuilt (plan §18 asks for ledger/vouchers/payables/receivables
  and their reports, not full financial statements).

### US-FIN-9: Run aging analysis on payables and receivables
**As a** finance manager, **I want to** see payables and receivables bucketed by days outstanding
(0-30/31-60/61-90/90+), **so that** I can prioritize collection and payment follow-up.

---

## 18. Biomedical Engineering

*Epic: the equipment registry through contracts, PM schedules, breakdowns, calibration, and
spare-parts usage that reuses Pharmacy's stock engine.*

### US-BIOMED-1: Maintain the equipment registry with location tracking
**As a** biomedical engineer, **I want to** register hospital equipment with criticality,
department, and location, and log every physical move, **so that** I always know where a piece of
critical equipment currently is.
**Acceptance Criteria**
- Every recorded move updates the equipment's current-location snapshot and appends to the
  movement log — the same append-only-plus-snapshot pattern used elsewhere (e.g., MRD's file
  movements).

### US-BIOMED-2: Track AMC/CMC contracts and renewal due dates
**As a** biomedical engineer, **I want to** record annual/comprehensive maintenance contracts
against equipment and see which are due for renewal, **so that** contract lapses are caught
before they happen.
**Acceptance Criteria**
- Renewal-due status is computed on read, always current, never a stale stored flag.

### US-BIOMED-3: Generate preventive-maintenance work orders when due
**As a** biomedical engineer, **I want to** define PM schedule templates and click "Generate due
work orders" to create work orders for everything currently due, **so that** preventive
maintenance happens on schedule via a deliberate action I control.
**Acceptance Criteria**
- Completing a PM work order rolls the schedule's next-due date forward by its frequency
  automatically.

### US-BIOMED-4: Log, assign, resolve, and close an equipment breakdown complaint
**As a** ward staff member or biomedical engineer, **I want to** raise a breakdown complaint,
assign it to an engineer, resolve it, and close it, **so that** equipment downtime is tracked from
report to repair.
**Acceptance Criteria**
- Raising a complaint automatically flips the equipment to Under-Maintenance; resolving flips it
  back to In-Service.
- Downtime minutes are computed automatically from reported-time to resolved-time — never
  manually entered.

### US-BIOMED-5: Track calibration due dates with certificate upload
**As a** biomedical engineer, **I want to** record calibration events and upload the certificate,
**so that** compliance documentation is centrally available and due dates are visible.

### US-BIOMED-6: Record spare-parts usage against a repair
**As a** biomedical engineer, **I want to** record which spare part batch was used to complete a
work order, **so that** the spare decrements from Pharmacy's stock engine — the same one drugs
use — with no parallel inventory system and no patient billing side-effect.

### US-BIOMED-7: Run downtime and PM-compliance reports
**As a** biomedical engineering manager, **I want to** run date-range downtime and PM-compliance
reports, **so that** I can measure equipment reliability and maintenance program adherence.

---

## 19. Quality / NABH

*Epic: quality indicators, audit checklists, the org-wide incident register (merged from
Nursing), non-conformances, CAPA, risk register, committees, and NABH evidence.*

### US-QUAL-1: Track a quality indicator's monthly numerator/denominator
**As a** quality manager, **I want to** define a quality indicator and record (or auto-compute)
its numerator/denominator each month, **so that** hospital performance against NABH-style metrics
is tracked over time.
**Acceptance Criteria**
- Indicators wired to an auto-compute source (Lab TAT, bed occupancy, incident count) can be
  computed for a period with one click, reusing the exact same report/dashboard queries those
  modules already built rather than a duplicated calculation.
- The computed rate is always derived on read from numerator/denominator, never stored stale.
- A fresh install starts with **30 seeded starter indicators** (`QI-01`…`QI-30`) spanning the
  categories most hospital quality programs track — bed occupancy, ALOS, HAI rates by type
  (SSI/CAUTI/CLABSI/VAP), falls, pressure ulcers, medication errors, ADRs, hand hygiene, lab and
  radiology TAT, discharge-summary completion, CAPA closure — so the module is usable on day one
  instead of requiring a quality manager to type every indicator in by hand first.
- The seed is **idempotent by `Code`**: re-running it never overwrites an indicator the hospital has
  since renamed/retargeted, and never re-adds one they deliberately deleted.
- Every seeded row has a **null `TargetValue`** on purpose — the acceptable threshold is a Quality
  Committee decision, not something the seed invents. The set is a reviewable starting point, not a
  verbatim reproduction of any NABH edition's Objective Element wording, numbering, or mandated
  targets; wording and applicability must be checked against the hospital's own NABH manual before
  it is used as assessor evidence.
- Only the two rows with a genuine 1:1 engine behind them get `AutoComputeSource` wired (Bed
  Occupancy Rate, Average Laboratory TAT). Indicators that merely *sound* auto-computable — Patient
  Falls, Medication Errors, Needle-Stick Injuries — are left manual, because the available
  `IncidentCount` source returns the hospital's **total** incident count regardless of category and
  would silently record the wrong number in what is meant to be a compliance record.

### US-QUAL-2: Run scheduled audit checklists with scoring
**As a** quality manager, **I want to** define an audit checklist template with weighted
questions, schedule a run against a branch, and score it, **so that** internal audits produce a
comparable percentage score over time.

### US-QUAL-3: Review the org-wide incident register, including sentinel events
**As a** quality manager, **I want to** see every incident reported anywhere in the hospital
(originally captured by Nursing) in one register, filter to sentinel events, and record a root-
cause analysis, **so that** patient-safety events get organizational-level oversight without a
second, parallel incident-reporting system existing.
**Acceptance Criteria**
- The register reuses Nursing's existing incident data and workflow outright — recording an RCA
  adds RCA-specific fields without altering the incident's existing status lifecycle.

### US-QUAL-4: Raise, track, and close a non-conformance against an NABH standard
**As a** quality manager, **I want to** raise a finding against a specific NABH standard and track
it Open → InProgress → Closed, **so that** accreditation gaps (distinct from patient-safety
incidents) are formally tracked to resolution.

### US-QUAL-5: Track a CAPA regardless of what triggered it
**As a** quality manager, **I want to** raise a Corrective and Preventive Action from an incident,
non-conformance, audit finding, or risk — with an owner, due date, and verification step — **so
that** every category of quality issue routes into the same accountable action-tracking system.
**Acceptance Criteria**
- CAPA lifecycle is Open → InProgress → Verified → Closed, with the polymorphic source visible on
  every CAPA record.

### US-QUAL-6: Maintain a Likelihood × Impact risk register
**As a** quality manager, **I want to** log identified risks scored by likelihood and impact and
track them through mitigation to closure, **so that** organizational risk is visible and
prioritized, not just tracked ad hoc.
**Acceptance Criteria**
- The risk score is always computed on read from the two 1–5 factors.

### US-QUAL-7: Record committee meetings with action items
**As a** quality committee member, **I want to** record meeting minutes and track action items to
completion, **so that** committee decisions have an accountable follow-up trail.

### US-QUAL-8: Build an NABH evidence library and monitor a compliance dashboard
**As a** quality manager, **I want to** upload evidence documents organized by chapter/standard
and view a compliance dashboard (open findings, overdue CAPAs, sentinel-event count, average
audit score, evidence coverage) plus a 12-month indicator-trend KPI view, **so that** accreditation
readiness is visible at a glance rather than reconstructed at survey time.

---

## 20. Patient Portal

*Epic: a fully separate, self-service, ownership-checked patient-facing surface reusing every
existing staff engine underneath.*

### US-PORTAL-1: Self-register a portal account against my existing hospital record
**As a** patient, **I want to** create a portal login by proving my identity with my UHID, mobile
number, and date of birth, **so that** I get self-service access without a staff member
provisioning it for me.
**Acceptance Criteria**
- Registration is rejected if the UHID/mobile/DOB combination doesn't match an existing patient
  record, or if that patient already has a portal account.
- Repeated failed logins lock the account (5 attempts / 15 minutes), mirroring staff login.

### US-PORTAL-2: Log in securely with the same session-safety guarantees staff get
**As a** patient, **I want** my portal session to use rotating refresh tokens with theft
detection, **so that** my medical data is protected to the same standard as staff accounts.
**Acceptance Criteria**
- A portal JWT is issued under a separate audience from staff tokens — a staff token gets 401 on
  every portal endpoint and vice versa, verified end to end.
- Replaying a rotated-out refresh token revokes the whole session.

### US-PORTAL-3: Browse available slots and book/cancel my own appointment
**As a** patient, **I want to** pick a branch, doctor, and date, see available slots, and book or
cancel my own appointment, **so that** I can self-schedule without calling the hospital.
**Acceptance Criteria**
- The booking flow reuses the exact same slot engine and appointment service staff use — no
  parallel booking logic.
- A patient can never cancel another patient's appointment (ownership-checked on every action).

### US-PORTAL-4: View and download my own approved reports, prescriptions, and bills
**As a** patient, **I want to** view my lab reports, radiology reports, prescriptions, bills and
receipts, and discharge summaries, and download each as a PDF, **so that** I have self-service
access to my own records.
**Acceptance Criteria**
- Every query and PDF download re-checks that the record actually belongs to the calling patient
  before returning anything — never addressable by guessing another patient's record id.
- Only reports that have cleared staff approval (lab/radiology verify→approve, discharge-summary
  approval) are ever visible — the portal inherits every staff-side approval gate for free rather
  than re-implementing it.

### US-PORTAL-5: Queue an online payment request
**As a** patient, **I want to** request to pay a bill online, **so that** the billing desk is
notified of my payment intent even before a real payment gateway is wired up.
**Acceptance Criteria**
- The request is queued (outbox pattern); no money actually moves, and this is communicated
  clearly rather than pretending the payment happened.

### US-PORTAL-6: Submit feedback tied to a specific visit or admission
**As a** patient, **I want to** submit a rating and comment linked to a specific visit or
admission, **so that** my feedback has clinical context rather than being anonymous and
untraceable.
**Acceptance Criteria**
- Exactly one of visit or admission must be linked — feedback with neither is rejected as invalid,
  not silently accepted.

---

## 21. Doctor Portal

*Epic: a doctor-scoped lens over existing staff screens, reusing the same staff SSO — no new
clinical engine.*

### US-DOCPORTAL-1: See my day at a glance on a personal dashboard
**As a** doctor, **I want to** see today's OP visit count, my active-inpatient count, pending
discharge-summary signatures, and unacknowledged critical lab alerts for orders I placed, **so
that** I know what needs my attention right now without navigating five different screens.
**Acceptance Criteria**
- A staff user with no linked doctor profile gets a clean, explicit error — never a crash — on
  every doctor-portal endpoint.

### US-DOCPORTAL-2: Work my own OP queue
**As a** doctor, **I want to** see only my own today's queue (no doctor picker, unlike the
front-desk queue screen), **so that** I go straight into my own patients.
**Acceptance Criteria**
- Starting/resuming a visit opens the exact same consultation workspace staff use — no parallel
  UI.

### US-DOCPORTAL-3: Review my current inpatients
**As a** doctor, **I want to** see every admission where I'm the primary or an active secondary
consultant, **so that** I can jump straight into ward rounds documentation.
**Acceptance Criteria**
- Clicking a patient opens the existing IPD admission detail page unmodified.

### US-DOCPORTAL-4: Review my orders and a patient's cumulative lab trend
**As a** doctor, **I want to** see the lab orders I've placed and pull up a cumulative trend chart
for a specific patient/test, **so that** I can track how a value is moving over multiple visits.
**Acceptance Criteria**
- The trend view reuses EMR's existing per-test trend chart and query — no new aggregation was
  built.

### US-DOCPORTAL-5: E-sign discharge summaries from a dedicated inbox
**As a** doctor, **I want to** see a queue of my pending discharge-summary approvals and approve
each with one click, **so that** I can clear discharge sign-offs efficiently instead of hunting
through each admission individually.
**Acceptance Criteria**
- Approval is ownership-checked — a doctor can only approve summaries for admissions where they
  are an actual consultant.

---

## 22. Reports & Dashboards

*Epic: cross-cutting dashboards over day-granular materialized views, plus a generic Custom
Report Builder that links out to every module's existing reports rather than duplicating them.*

### US-RPT-1: View the Management Dashboard, cross-branch-aware
**As a** management-level user, **I want to** see revenue, collection, OP+IP census, bed
occupancy, and unacknowledged-alert summaries — automatically scoped to just my branch unless I
hold cross-branch view rights, **so that** I get the right altitude of view for my role without a
separate cross-branch-only screen to remember.
**Acceptance Criteria**
- A user without `REPORT.CROSSBRANCH.VIEW` always sees `isCrossBranch: false` and only their own
  branch's figures — never another branch's data leaking through.
- A user without permission for a specific dashboard gets a clean 403, not a 500 or an empty
  screen.

### US-RPT-2: View OP, IP, Billing, and Revenue dashboards
**As a** department manager, **I want to** see OP-specific, IP-specific (including Average Length
of Stay), Billing-specific, and Revenue (with doctor-wise and department-wise breakdowns)
dashboards, **so that** I have a purpose-built view for my area of responsibility.
**Acceptance Criteria**
- Every dashboard chart draws from day-granular materialized views refreshed every 15 minutes, not
  a live heavy join on transactional tables.

### US-RPT-3: Run and export a catalog report
**As a** any authorized user, **I want to** browse a permission-filtered report catalog, run a
report inline with a date filter, and export it as CSV, Excel, or PDF, **so that** I can get data
out of the system in the format I need without asking IT for a custom query.
**Acceptance Criteria**
- Catalog entries are either "executable" (run inline through the generic executor) or "linked"
  (navigate to that module's own dedicated, fully-featured report page) — I only see catalog rows
  for permissions I actually hold.
- Every export produces genuinely valid output (real CSV, a valid .xlsx, a valid PDF) built from
  real query data, not a stub.

### US-RPT-4: Subscribe to a report on a recurring schedule
**As a** report consumer, **I want to** create, list, and remove my own report subscriptions
(report + cron schedule + recipient email), **so that** the intent is captured now, ready for a
mailer to fulfill once that capability exists.
**Acceptance Criteria**
- Subscriptions are scoped to the requesting user's own rows only.
- The feature is clearly a queued intent — nothing is silently claimed to have been emailed.

### US-RPT-5: Trust that materialized views stay fresh without manual intervention
**As a** hospital administrator, **I want** the underlying reporting views to refresh
automatically on a schedule, **so that** dashboards never require a manual "refresh now" step to
show recent activity.
**Acceptance Criteria**
- A recurring background job refreshes all three materialized views every 15 minutes using a
  concurrent refresh (no read-blocking).

---

## 23. Insurance / TPA

*Epic: pre-authorization through claim settlement against an insurer/TPA — originally a held
module, built at explicit request following the plan's own intended shape (pre-auth →
enhancement → claim → settlement → denial tracking), finally giving `Bill.InsuranceCompanyId`
(inert since Billing/Phase 6) a real writer.*

### US-INS-1: Raise a pre-authorization request against a patient's policy
**As an** insurance desk executive, **I want to** raise a pre-authorization request for a
patient's OP visit or admission against one of their insurance policies, with an estimated
amount and diagnosis, **so that** treatment can proceed with the insurer's cost approval already
in motion.
**Acceptance Criteria**
- The policy picker is scoped to the selected patient's own policies only (captured earlier at
  Patient Registration — see US-PAT-7).
- An OP-treatment request requires the OP visit; an IP-treatment request requires the admission —
  the form won't submit without the matching link.
- The request gets a sequential Pre-Auth number (`PA-2026-000123`) and starts in `Requested`.

### US-INS-2: Approve or reject a pending pre-authorization
**As an** insurance supervisor, **I want to** approve a pre-auth with an approved amount (which
may be less than requested) or reject it with a reason, **so that** front-office and clinical
staff know whether treatment is financially covered before it proceeds.
**Acceptance Criteria**
- Approving with an amount below the estimate auto-flags the request `PartiallyApproved` rather
  than `Approved` — no separate manual flag to remember.
- A request already decided cannot be decided again — the action is rejected outright, not
  silently overwritten.
- Only a user holding the approve permission (not the create permission) can decide a request —
  the same requester-cannot-self-approve separation Billing's discount workflow already uses.

### US-INS-3: Request and approve a mid-treatment enhancement on an already-approved pre-auth
**As an** insurance desk executive, **I want to** request an additional amount against a pre-auth
that's already approved, when treatment costs more than first estimated, **so that** the
insurer's cover keeps pace with an evolving case without opening a whole new pre-auth.
**Acceptance Criteria**
- An enhancement can only be requested against a pre-auth that's already Approved or
  PartiallyApproved — not against one still pending or rejected.
- Approving an enhancement adds its amount onto the parent pre-auth's approved total and flips
  the parent back to `Approved`.

### US-INS-4: Submit a claim against a finalized bill
**As an** insurance desk executive, **I want to** submit a claim against a patient's finalized
bill, optionally linked to the pre-auth that covered the treatment, **so that** the hospital can
recover the treatment cost from the insurer.
**Acceptance Criteria**
- A cancelled bill cannot be claimed against.
- Submitting a claim stamps the bill's insurer field from the policy — the first time this
  Billing-side placeholder field is ever actually populated.
- The claim gets a sequential Claim number (`CLM-2026-000123`) and starts in `Submitted`.

### US-INS-5: Move a claim through review to an approved, partially-approved, or denied outcome
**As an** insurance supervisor, **I want to** move a submitted claim to Under Review and then to
Approved/Partially Approved (with an approved amount) or Denied (with a reason code), **so that**
the claim's outcome is tracked accurately, one step at a time.
**Acceptance Criteria**
- Status can only move one step forward at a time (Submitted→UnderReview→{PartiallyApproved,
  Approved,Denied}) — no skipping straight from Submitted to Approved.
- A denial requires picking a reason from the Claim Denial Reason master, with optional free-text
  notes.
- Supporting documents (pre-auth form, bill copy, discharge summary, policy copy, ...) can be
  attached to the claim at any point, typed via the Claim Document Type master.

### US-INS-6: Settle an approved claim and see it reflected in the ledger
**As an** insurance supervisor, **I want to** record a claim's settlement — amount received, any
deduction the insurer applied, payment mode, and their reference number — **so that** the
recovery is closed out and the money received is properly booked.
**Acceptance Criteria**
- Only a claim that's Approved or Partially Approved can be settled; settling flips it to
  `Settled` and is final.
- Settlement posts a real ledger voucher (Debit the payment mode's bank/cash account, Credit
  Patient Revenue) through the same `IFinanceLedgerPoster` hook Billing's own payment collection
  uses — if the branch hasn't configured Finance accounts yet, the posting is a silent no-op, not
  a blocking error.
- The whole settlement (row update + ledger posting) happens in one transaction — a failure
  partway through leaves nothing half-applied.
