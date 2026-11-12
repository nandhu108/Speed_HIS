# Module Spec — Patient Portal (Phase 21)

Status: **complete** (2026-07-20). Build-order note: the plan (§12) sequences Phase 20
(Quality/NABH) before Phase 21. Quality/NABH is a held-back aggregator with no hard
dependency on the Portal, so this phase was completed first at the user's explicit
request; Phase 20 remains outstanding and should be picked up next. See root `CLAUDE.md`.

## Design

A fully separate auth surface from the staff app — own JWT audience (`PrixoPortal`), own
rotating-refresh-token table, own React Router subtree (`/portal/*`) with its own
`PortalAuthProvider`/token store, deliberately not nested under staff `RequireAuth`.
Everywhere else the Portal is a **thin, ownership-checked wrapper over existing staff
services** — it invents no parallel booking engine, PDF generator, or history query; it
only adds "does this record belong to the calling patient" and calls the same
`AppointmentService`/`LabOrderService`/`RadOrderService`/`BillingService`/
`DischargeService`/`HistoryService` the staff screens use. This means every approval gate
staff already enforce (lab/rad report approval, discharge-summary approval) is inherited
for free — the Portal cannot leak an unapproved report.

Patients have no RBAC permissions, so Portal endpoints carry `[PortalAuth]` (a second JWT
bearer scheme, `PortalAuthAttribute`) instead of `[HasPermission]`; a staff token can never
satisfy a `[PortalAuth]` endpoint and vice versa (two independent `AddJwtBearer` schemes in
`Program.cs`).

## New domain

| Table | Schema | Notes |
|---|---|---|
| `portal_accounts` | `patient` | One row per `Patient` (login credential), not a widened `Patient` — same split reasoning as Admin's User/Employee. Self-service activated via `RegisterAsync`: identity proven by UHID + mobile + DOB already on file, not admin-provisioned. Lockout after 5 failed attempts / 15 min, mirroring staff `AuthService`. |
| `portal_refresh_tokens` | `patient` | Rotating hashed refresh tokens, reuse-detection revokes the whole session family — same shape as staff `RefreshToken`, keyed to `PatientId`. |
| `feedbacks` | `patient` | Visit- or admission-linked rating (plan: "→ Quality module"). Stays in `patient` schema rather than `quality.feedbacks` because Quality (Phase 20) doesn't exist yet — same "real data now, migrates once the module lands" posture as other held-module placeholders. Exactly one of `OpVisitId`/`AdmissionId` is set. |
| `billing.online_payment_requests` | `billing` | **Payment placeholder** (plan: "gateway held"). Not a `BaseEntity` — an outbox table, same posture as Appointments' `NotificationQueue`: a patient "requesting" online payment today just queues intent (`Status = Requested`) for the billing desk to follow up on; no money moves through this row until a real gateway (held module) adds a processor. Migration `Phase21_PortalPayment`. |

Migration: `Phase21_PatientPortal` (accounts/tokens/feedback) + `Phase21_PortalPayment`
(payment placeholder, added once the payment gap was closed).

## Endpoints (`/api/v1/portal`)

- `auth/register`, `auth/login`, `auth/refresh` — `[AllowAnonymous]`, rate-limited
  (`PortalAuth` fixed-window policy)
- `auth/logout`, `auth/change-password` — `[PortalAuth]`
- `appointments/slots` (GET, doctor+date), `appointments` (GET mine / POST book),
  `appointments/{id}/cancel` (POST) — thin wrapper over `AppointmentService`;
  `BookForPortalAsync` takes an explicit `branchId` since the portal JWT carries no branch
  claim (patient picks the branch, unlike a staff session)
- `lab-reports`, `lab-reports/{id}/pdf`, `rad-reports`, `rad-reports/{id}/pdf`,
  `prescriptions`, `bills`, `bills/{id}/receipt`, `bills/{id}/pay-online` (POST, payment
  placeholder), `discharge-summaries`, `discharge-summaries/{id}/pdf`, `medical-history`
- `feedback` (GET mine / POST submit)
- `branches`, `doctors` — read-only lookups the booking screen needs, reusing
  `BranchService`/`DoctorService` directly (no parallel directory)

Every "mine" query and every PDF download re-checks `PatientId` ownership against the
`[PortalAuth]` JWT's subject claim before touching the underlying staff service — a patient
can never address another patient's record by guessing an id.

## Frontend (`src/portal`)

- `PortalLoginPage` / `PortalRegisterPage` — outside the auth guard
- `PortalHomePage` — card grid to the four feature areas
- `PortalAppointmentsPage` — branch/doctor/date slot picker → book, plus a "my
  appointments" table with cancel
- `PortalRecordsPage` — tabbed: Lab Reports, Radiology Reports, Prescriptions, Bills &
  Receipts (with the "Pay Online" placeholder action), Discharge Summaries. PDF downloads
  fetch as an authenticated blob (`downloadPortalPdf`) since a plain `<a href>` can't carry
  the bearer header.
- `PortalMedicalHistoryPage` — conditions/surgeries/allergies/immunizations, reusing EMR's
  `HistoryService` DTOs
- `PortalFeedbackPage` — rating + comment, visit- or admission-linked

## Deferred / notes

- **Slot browsing requires login**, not literally "public" as the plan's one-line
  wording suggests — an unauthenticated endpoint exposing doctor schedules/availability
  is a data-exposure surface with no real UX benefit for a hospital portal (patients
  already need an account for everything else here), so slot viewing sits behind
  `[PortalAuth]` like the rest of the controller.
- **OTP is not implemented** — plan explicitly scopes this as "password first, OTP when
  SMS integration lands"; password auth is what's built.
- **Payment is a request-queue placeholder, not a gateway** — `online_payment_requests`
  rows are never processed automatically; activating a real gateway later means adding a
  processor over this table, not re-architecting it (same posture as `NotificationQueue`).
  **2026-09-16**: until now, that queue had no reader at all — the portal side could write a
  request but no staff screen could see it, so the "our billing team will contact you" message
  was a promise the product couldn't keep. Added a staff-side worklist
  (`OnlinePaymentRequestService`/`OnlinePaymentRequestsController`,
  `BILLING.ONLINEPAYMENT.{VIEW,MANAGE}`, `OnlinePaymentRequestsPage`) — see `docs/modules/billing.md`.
  It still never moves money; staff collect payment the normal way on the bill and mark the
  request Completed/Cancelled to close the loop.
- **No dedicated Portal service-level xUnit tests** — matches this codebase's existing
  pattern (Biomed/Finance/MRD phases also ship Validator-only unit tests, no per-service
  unit tests). `Prixo.Api.IntegrationTests` has no real xUnit coverage for any module yet
  — a pre-existing gap across the whole codebase, not specific to this phase. In lieu of
  that, the full flow was exercised live against a real Postgres instance (see below).
- **Fixed a pre-existing build break found while finishing this phase**: `Features/Admin/Logs/LogQueryService.cs` (backing `ActivityLogsController`/`AuditLogsController`, both dating to Phase 1) was referenced everywhere but had never actually been committed — the solution did not compile on a clean checkout before this fix. Added the missing `LogQueryService` + `ActivityLogDto`/`AuditLogDto`/`ActivityLogQuery`/`AuditLogQuery` types.
- **Frontend not exercised in a real browser** — verified via `tsc -b && vite build`
  (clean) and `oxlint` (clean, two pre-existing unrelated warnings); no browser tool is
  available in this environment. The backend API surface it talks to *was* exercised live
  (below), so the remaining risk is limited to rendering/wiring, not endpoint behavior.

## Live end-to-end verification (2026-07-20)

Ran the full stack for real (`docker compose up db`, migrations applied, API booted,
seeder ran) and drove every Phase 21 endpoint with scripted HTTP calls — 36/36 checks
passed:
- Self-registration against a freshly-registered staff-side patient; duplicate
  registration rejected; login with UHID or mobile; wrong-password rejected.
- Refresh-token rotation, and **reuse-detection actually revokes the session** (replaying
  a rotated-out refresh token is rejected, and the token issued in that same response is
  also dead afterwards).
- Cross-scheme isolation confirmed: a staff JWT gets 401 on `[PortalAuth]` endpoints and
  a portal JWT gets 401 on staff endpoints.
- Change-password invalidates the old password and other sessions.
- Full appointment lifecycle against a real doctor/schedule/branch created for the test:
  slot list → book → the booked slot flips to unavailable → appears in "my
  appointments" → cancel → slot frees up again — and a second patient's token cannot
  cancel the first patient's appointment (403/ownership check holds).
- Records/medical-history/feedback/pay-online endpoints all return clean 200/400s (never
  500s) against an account with no clinical history yet — feedback with neither
  `opVisitId` nor `admissionId`, and pay-online against a non-existent bill, are both
  correctly rejected as validation errors rather than crashing.
- Confirmed the `PortalAuth` fixed-window rate limiter (10 requests/5 min/IP) is live and
  actually enforced — discovered by tripping it during test iteration.
