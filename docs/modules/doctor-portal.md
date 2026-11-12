# Module Spec — Doctor Portal (Phase 22)

Status: **complete** (2026-07-20). Build-order note: same deviation as Phase 21 — the
plan (§12) sequences Phase 20 (Quality/NABH) before this. Quality/NABH has no hard
dependency on the Doctor Portal, so this phase was completed second (after Patient
Portal) at the user's explicit request; Phase 20 remains outstanding. See root
`CLAUDE.md` and `docs/modules/portal.md`.

## Design

Unlike the Patient Portal, the Doctor Portal is **not** a separate auth surface — a
doctor already has a staff `User` account, so it reuses the exact same JWT/RBAC as every
other staff screen ("same SSO" per plan §22). Every `DoctorPortalService` method resolves
"my own doctor profile" from the caller's `UserId` (`Doctor.UserId == currentUser.UserId`)
and then either delegates outright to an existing staff service (`OpVisitService.
GetQueueAsync`, `DischargeService.ApproveDischargeSummaryAsync`) or runs a small
doctor-scoped query using the exact same DTOs the staff screens already return
(`AdmissionListItemDto`, `LabOrderListItemDto`) — no parallel queue engine, discharge
engine, or lab-order engine, and no new database tables/migration. A user with no linked
`Doctor` row gets a clean `DomainException` ("Your account is not linked to a doctor
profile"), not a crash.

## Endpoints (`/api/v1/doctor-portal`)

- `GET dashboard` — today's OP visit count, active-inpatient count (primary or active
  secondary consultant), pending discharge-summary signs, unacknowledged critical lab
  alerts for orders the doctor placed.
- `GET queue?date=` — reuses `OpVisitService.GetQueueAsync(myDoctorId, date)` outright;
  defaults to today.
- `GET inpatients` — admissions where the doctor is `PrimaryConsultantId` or an active
  (non-ended) row in `AdmissionConsultant`, status `Admitted`.
- `GET orders` — the doctor's own `LabOrders` (by `OrderedByDoctorId`), most recent 100.
- `GET discharge-approvals` — `DischargeSummary` rows not yet approved, for admissions
  where the doctor is a consultant.
- `POST discharge-approvals/{admissionId}/approve` — ownership-checked (doctor must be a
  consultant on that admission) wrapper over `DischargeService.ApproveDischargeSummaryAsync`.
- `GET opinion-requests` — "Doctor Opinion Entry" inbox (added 2026-07-27): every OPD Internal
  referral (`opd.referrals`, see `docs/modules/opd.md`) addressed to this doctor, across all
  patients/visits, via `OpVisitService.GetOpinionRequestsForDoctorAsync`. `PUT
  opinion-requests/{referralId}` is an ownership-checked wrapper (referral's
  `ReferredToDoctorId` must match) over the same `OpVisitService.SaveOpinionAsync` the OPD
  Referral tab itself uses — same "thin wrapper, no parallel engine" posture as the discharge
  e-sign action above.

OP consultation itself has no doctor-portal endpoint — clicking a queue entry starts the
visit via the existing `OpVisitService` mutation and opens Phase 5's `ConsultationWorkspace`
unmodified. IP rounds similarly opens Phase 9's `AdmissionDetailPage` unmodified (progress
notes, vitals, doctor orders are already there). Lab-order detail opens Phase 7's existing
`LabOrderDetailPage`.

## Permissions

`DOCTORPORTAL.DASHBOARD.VIEW`, `DOCTORPORTAL.QUEUE.VIEW`, `DOCTORPORTAL.IPROUNDS.VIEW`,
`DOCTORPORTAL.ORDERS.VIEW`, `DOCTORPORTAL.DISCHARGE.VIEW`, `DOCTORPORTAL.OPINION.VIEW` gate
the six read endpoints; the approve action reuses the existing `IPD.DISCHARGE.APPROVESUMMARY`
and the opinion-save action reuses `OPD.VISIT.MANAGE`, rather than new codes, since both are
the identical action the corresponding staff screen already gates. Navigating out
into the reused staff screens (OPD consult, IPD admission detail, Lab order detail) still
needs their own existing permissions (`OPD.VISIT.MANAGE`, `IPD.ADMISSION.VIEW`,
`LAB.ORDER.VIEW`, …) — a doctor's role must carry those too, same as any other user of
those screens.

## Frontend (`src/features/doctorportal`)

- `DoctorDashboardPage` (`/doctor-portal`) — four stat cards, each linking to the
  relevant sub-page.
- `MyQueuePage` (`/doctor-portal/queue`) — today's queue with no doctor picker (unlike
  the staff `DoctorQueuePage`, which front-desk staff use to look at any doctor's queue);
  Start/Resume navigates into the existing OPD workspace.
- `MyInpatientsPage` (`/doctor-portal/inpatients`) — row click opens the existing IPD
  admission detail page.
- `MyOrdersResultsPage` (`/doctor-portal/orders`) — order list (row click opens the
  existing Lab order detail page) plus a patient+test picker that renders EMR's existing
  `ResultsTrendChart`/`useResultsTrend` — the "cumulative lab trends" requirement is
  satisfied by wiring the doctor's own patients into that existing capability, not by
  computing a new aggregate.
- `DischargeApprovalsPage` (`/doctor-portal/discharge-approvals`) — pending list +
  one-click "Approve & e-sign" per admission.
- `OpinionRequestsPage` (`/doctor-portal/opinion-requests`) — pending cross-consult requests
  (inline opinion textbox + save) and a read-only "previously answered" list below.

## Deferred / notes

- **No role auto-provisions a "Doctor" role with these permissions** — same gap Biomed's
  module doc already flagged: no role-to-permission seeding beyond `SUPER_ADMIN`/
  `HOSPITAL_ADMIN` exists anywhere in this codebase. A hospital would need to create a
  "Doctor" role and grant it `DOCTORPORTAL.*` plus whatever staff-screen permissions
  (`OPD.VISIT.MANAGE`, `IPD.ADMISSION.VIEW`, `LAB.ORDER.VIEW`, `EMR.TIMELINE.VIEW`, …) the
  reused screens require — out of scope for this phase to invent.
- **"Cumulative lab trends" is per test, not a dashboard-wide rollup** — matches the
  plan's own wording ("Orders & Results View (cumulative lab trends)") and EMR's existing
  `GetResultsTrendAsync(patientId, testId)` shape; there's no cross-patient/cross-test
  aggregate view.
- **Frontend not exercised in a real browser** — verified via `tsc -b && vite build`
  (clean) and `oxlint` (clean). The backend API surface it talks to *was* exercised live
  (below).

## Live end-to-end verification (2026-07-20)

Ran the full stack (`docker compose up db`, migrations, seeded API) and drove every
endpoint with scripted HTTP calls — 18/18 checks passed, plus one follow-up check:
- A brand-new doctor (fresh `User` + `Doctor` + weekly schedule) sees an all-zero
  dashboard and empty queue/inpatients/orders/discharge-approvals lists — no 500s on an
  empty account.
- Registering a same-day OP visit for that doctor makes it appear in their own
  `GET queue` and increments `todayOpCount` on the next dashboard read.
- **Cross-doctor isolation confirmed**: a second doctor's queue and dashboard show zero
  activity — they never see the first doctor's patient.
- A staff user with no linked `Doctor` row gets a clean `400` ("not linked to a doctor
  profile") from every doctor-portal endpoint, including the discharge-approve action —
  never a `500`.
