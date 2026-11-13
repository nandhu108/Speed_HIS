# Module Spec — Appointments (Phase 4)

Status: **complete** (2026-07-10). Owns doctor availability (weekly template + date
overrides/leave), the slot engine, booking/reschedule/cancel/check-in, per-doctor-per-day
token generation, the queue display, and no-show auto-marking — the first background job
(Hangfire) in the codebase.

## Database (schema `appointment`, plus `admin.notification_queue`)

| Table | Notes |
|---|---|
| `appointment.doctor_schedules` | Weekly recurring block: doctor + branch + day-of-week + start/end time + slot minutes. A doctor can have several blocks per day (morning/evening sessions). Branch-scoped — a doctor's hours can differ per branch they work at. |
| `appointment.schedule_overrides` | Date-specific exception: full-day leave (`is_available=false`) or special hours for that date (replaces the weekly template for that day only). Unique per `(doctor_id, date)`. |
| `appointment.appointments` | Branch-scoped booking. Token number, status (`Scheduled/CheckedIn/Completed/Cancelled/NoShow`), booking type (`Booked/WalkIn`), and a `rescheduled_from_id` self-FK for lineage. Token is denormalized here rather than a separate `appointment_tokens` table — no other consumer needed one this phase. |
| `admin.notification_queue` | Generic outbox placeholder (not appointment-specific — future modules reuse it). One row is enqueued as an SMS-reminder placeholder per booking; nothing dispatches it yet (Communication module is held). |

Migration: `Phase4_Appointments`.

## Slot engine

`AppointmentService.BuildAvailabilityWindowsAsync(doctor, date)`: a `ScheduleOverride` for
that exact date wins outright (unavailable → no slots; available → its own hours/slot
length replace the weekly template for that day). Otherwise every `DoctorSchedule` block
matching `date.DayOfWeek` applies. Each window is sliced into fixed-length slots; slots
already taken by a non-cancelled appointment are marked unavailable. Booking re-validates
against a fresh existence check right before insert (open race window, consistent with
the rest of the codebase's approach — no `SELECT … FOR UPDATE` yet anywhere).

## Token generation

Reuses `ISequenceService` (built in Phase 3 for UHID) with `sequenceKey = "APPT_TOKEN"`,
`scopeId = doctorId`, and the date encoded as an int (`20260710`) in place of the "year"
bucket — an atomic per-doctor-per-day counter with no schema changes needed.

## Reschedule

`RescheduleAsync` runs inside `IUnitOfWork.ExecuteInTransactionAsync`: cancels the old
appointment (`CancelReason = "Rescheduled: {reason}"`) and books a replacement with
`RescheduledFromId` pointing at the original — same pattern as Phase 3's patient merge
lineage tracking.

## No-show auto-marking (Hangfire)

First background job in the codebase. `NoShowJob` (Application layer, `INoShowJob`)
scans for `Scheduled` appointments whose slot has passed and marks them `NoShow`; it runs
with no HTTP context, so `AppDbContext.CurrentBranchId` is naturally null and the branch
query filter disables itself for the sweep — every branch is covered in one pass without
`IgnoreQueryFilters()`. Registered as an hourly recurring job (`Cron.Hourly`) in
`Program.cs` via `RecurringJob.AddOrUpdate<INoShowJob>`. Storage is `Hangfire.PostgreSql`
against the same connection string; the dashboard (`/hangfire`) is mapped in Development
only, same posture as Scalar.

## Endpoints

- `GET/PUT /api/v1/doctors/{doctorId}/schedule[/blocks]`, `POST/DELETE …/overrides[/{id}]`
- `GET /api/v1/appointments/slots?doctorId=&date=`
- `GET /api/v1/appointments` (paged, filters: doctorId/patientId/date/status/search),
  `GET /api/v1/appointments/{id}`, `POST /api/v1/appointments` (book)
- `POST /api/v1/appointments/{id}/reschedule|cancel|check-in`
- `GET /api/v1/queue/display?doctorId=&deptId=&date=`

## Permissions

`APPOINTMENT.SCHEDULE.{VIEW,MANAGE}`, `APPOINTMENT.APPOINTMENT.{VIEW,BOOK,RESCHEDULE,CANCEL,CHECKIN}`,
`APPOINTMENT.QUEUE.VIEW`.

## Frontend (`src/features/appointments`)

- `DoctorSchedulePage` — doctor picker, weekly-block editor (add/remove per day, replace-all
  save), leave/override list with an add form.
- `BookingPage` — `PatientPicker` (debounced UHID/name/mobile search — a first, narrowly
  scoped instance of the shared `SearchSelect` pattern the plan calls for later), doctor +
  date pickers, a slot-button grid, separate "book" vs "book as walk-in" actions.
- `AppointmentsListPage` — filtered/searchable list with reschedule (reuses the slot-grid
  UI) and cancel modals, and a check-in action.
- `QueueDisplayPage` — auto-refreshing (`refetchInterval: 15s`) token board.

## Unit Scheduling (front-office extension, added 2026-07-27)

Status: **complete**. Two gaps from a legacy front-office menu audit, both against the
existing `Unit` master (`master.units` — previously a bare Code/Name/Description tag with
zero references anywhere in the backend):

- **`master.unit_doctors`** ("Link Unit & Doctors") — a bare join
  `{ UnitId, DoctorId }`, mirrors `DoctorDepartment`'s shape exactly, unique per
  `(UnitId, DoctorId)`. Unlike `DoctorDepartment` (diffed inline on the Doctor save
  request), this is its own full-replace sub-resource (`PUT /units/{unitId}/doctors`) —
  the legacy item frames it as a screen reached from the Unit side, not the Doctor form.
- **`appointment.unit_schedules`** ("Schedule Unit - Day Wise") — mirrors
  `DoctorSchedule` (day-of-week + start/end time, branch-scoped, multiple blocks/day
  allowed) minus `SlotMinutes`. **Purely descriptive** — nothing links `Unit` to
  `Appointment`/booking, so this is not consumed by the slot engine described above; it
  exists to give the Unit master real substance, same posture as Camp/MHC when those
  landed on top of previously-inert masters.

Endpoints: `GET/PUT /api/v1/units/{unitId}/doctors` (replace-all), `GET/PUT
/api/v1/units/{unitId}/schedule[/blocks]` (replace-all, no overrides sub-resource — units
don't need date-specific leave exceptions in this pass).

Permissions: Unit-Doctor link reuses `MASTER.UNIT.{VIEW,MANAGE}` (a Unit-scoped
sub-resource, not a new module). Unit Schedule gets its own
`APPOINTMENT.UNITSCHEDULE.{VIEW,MANAGE}` pair, mirroring `APPOINTMENT.SCHEDULE.*`.

Frontend: `UnitSchedulingPage` (`/appointments/unit-schedule`) combines both — a Unit
picker, a linked-doctors checkbox list (mirrors `DoctorsPage`'s department-checkbox
pattern), and a weekly-block editor (mirrors `DoctorSchedulePage` minus slot-minutes and
overrides) — one page rather than two near-empty ones, since both are "configure this
Unit" concerns reached the same way.

## Appointment Types ("Appointment Masters", added 2026-07-27)

Status: **complete**. `AppointmentBookingType` (`Booked`/`WalkIn`) stays a fixed 2-value
enum — it is not what this adds. `AppointmentType` (`master.appointment_types`) is a new,
hospital-configurable categorization tag (Code/Name/Description/`DefaultSlotMinutes`,
bespoke — not `ISimpleMaster` — same reasoning as `DoctorPayoutRule`), wired onto
`Appointment` as one more **optional, additive, unwired** nullable `AppointmentTypeId` FK
— the same posture as `OpVisit.CampId`. It plays no role in slot resolution (the "Slot
engine" section above is unchanged); it's carried through booking → reschedule →
detail purely as a reporting/categorization tag.

Endpoints: `/api/v1/appointment-types` (standard bespoke CRUD). `POST /api/v1/appointments`
(`BookAppointmentRequest`) gained a trailing optional `AppointmentTypeId` — existing
callers omitting it are unaffected.

Permissions: `MASTER.APPOINTMENTTYPE.{VIEW,MANAGE}`.

Frontend: `AppointmentTypesPage` under Masters (mirrors `DoctorPayoutRulesPage`'s
flat-plus-extra-field editor). `BookingPage` gained an optional "Appointment Type" select
alongside the existing Doctor/Date fields.

## Token Management ("Skip Token" / "Token Transfer" / "ER Token Mapping", added 2026-07-27)

Status: **complete**. Three legacy front-office token-board gaps, all additive columns on
`Appointment` — no new tables, no change to the slot engine or token-generation idiom
described above.

- **Skip Token** — a checked-in patient not present when called. `Appointment.SkippedAt`
  (nullable) + `SkipCount` record it; `Status` stays `CheckedIn` (the token isn't cancelled
  or removed). `GetQueueAsync`'s ordering changed from `OrderBy(DoctorId).ThenBy(TokenNumber)`
  to `OrderBy(DoctorId).ThenBy(SkippedAt.HasValue).ThenBy(SkippedAt).ThenBy(TokenNumber)` —
  every non-skipped row has `SkippedAt == null` so that group still sorts by token number;
  skipped rows sort after, oldest-skip-first, so a skipped token rejoins near the front of
  the "just skipped" group rather than being lost at the very end. "Recall" just clears
  `SkippedAt`, restoring normal rotation; `SkipCount` is left as a running counter (no reset)
  so repeated skip/recall cycles are visible on the appointment detail.
- **Token Transfer** — reassigns a `Scheduled`/`CheckedIn` token to a different doctor the
  same day. Mints a fresh token number from the new doctor's own per-day
  `ISequenceService` counter (same `AppointmentToken` sequence key, new `scopeId`); recomputes
  `EndTime` from the new doctor's slot length at that `StartTime` (falls back to
  `Doctor.DefaultSlotMinutes` exactly like booking does). `TransferredFromDoctorId`/
  `TransferredAt` record the single last hop, same posture as `RescheduledFromId` — repeated
  transfers overwrite the pointer rather than chaining a history table. Guards against the new
  doctor already having a booking at that exact time (same application-level check
  `BookInternalAsync` uses) and logs `APPOINTMENT_TRANSFERRED` via `IActivityLogger` (transfer
  is judged significant enough to log explicitly, the same call made for cancellation; plain
  skip/recall/check-in rely on the automatic row-diff audit log instead).
- **ER Token Mapping** — once an Emergency visit (`er.emergency_visits`, Phase 16) has an
  `AssignedDoctorId`, `AppointmentService.MapEmergencyTokenAsync` mints a normal `WalkIn`
  token for that doctor through the *same* `BookInternalAsync` engine any other walk-in uses —
  not a parallel queue-merge — so the ER patient gets a real position in that doctor's OPD
  token board/queue display without a second token-numbering scheme. `Appointment` gained a
  loose, optional `EmergencyVisitId` FK (same posture as Lab/Rad/Billing's own
  `EmergencyVisitId` columns), unique while active (`status <> 4`) — one live token per ER
  visit, mirroring Billing's unique-active-emergency-bill index. Endpoint lives on
  `AppointmentsController`, not the Emergency controller, and reuses `AppointmentBook` — no
  new Emergency permission — the same reuse posture as Billing's
  `generate-emergency/{emergencyVisitId}` reusing `BILLING.BILL.CREATEEMERGENCY`.

Endpoints: `POST /api/v1/appointments/{id}/skip`, `/recall`, `/transfer`
(`TransferTokenRequest{ NewDoctorId, Reason? }`, returns the appointment with its new token
number — same shape as reschedule's response), `POST
/api/v1/appointments/emergency/{emergencyVisitId}/map-token`. `GET /api/v1/appointments` grew
an `EmergencyVisitId` filter so a caller (the ER visit page) can look up whether a token was
already mapped for a visit.

Permissions: `APPOINTMENT.APPOINTMENT.SKIP` (covers both skip and recall),
`APPOINTMENT.APPOINTMENT.TRANSFER`. ER mapping reuses `APPOINTMENT.APPOINTMENT.BOOK`.

Frontend: `AppointmentsListPage` gained Skip/Recall and Transfer row actions (Transfer opens a
doctor-picker modal) plus "Skipped"/"ER" token badges; `QueueDisplayPage` shows the same
badges and outlines a skipped card in amber. The ER `VisitDetailPage`'s Overview tab gained a
"Map ER token" card in the Visit panel — disabled until a doctor is assigned, shows the mapped
token/doctor once one exists (backed by the new `EmergencyVisitId` list filter).

## Tele-consulting (added 2026-07-27)

Status: **complete**. `ConsultationMode` (`InPerson`/`TeleConsult`) is a new, additive, unwired
tag on `Appointment` — same posture as `AppointmentTypeId`: it plays no role in slot
resolution. `TeleconsultLink` is a plain external meeting-URL string staff paste in manually;
there is no video-provider integration (Communication is a held module per plan §21, so this
deliberately stops at "carry a link," the same "placeholders get real schema" posture CLAUDE.md
calls for). The link is often generated after the slot is booked, so it's also settable/editable
independently of booking via its own action, not just at `BookAppointmentRequest` time.

Endpoints: `BookAppointmentRequest` gained trailing optional `ConsultationMode` (default
`InPerson`) and `TeleconsultLink` fields — existing callers (including Patient Portal booking)
omitting them are unaffected. `PUT /api/v1/appointments/{id}/teleconsult-link`
(`SaveTeleconsultLinkRequest{ TeleconsultLink }`) sets/clears the link; rejects if the
appointment isn't `TeleConsult`. Reschedule carries the mode/link over to the replacement
appointment, same as it already does for `AppointmentTypeId`.

Permissions: reuses `APPOINTMENT.APPOINTMENT.BOOK` for the link action — no new permission.

Frontend: `BookingPage` gained a Consultation Mode select plus a conditional meeting-link input;
`AppointmentsListPage` shows a "Tele" token badge and a "Set link"/"Edit link" row action;
`QueueDisplayPage` shows the same badge on the token board.

## Doctors On Duty (added 2026-07-27)

Status: **complete**. Not a new duty-roster entity — `AppointmentService.GetDoctorsOnDutyAsync`
is a pure read that reuses the exact same `BuildAvailabilityWindowsAsync` the slot engine and
booking already call, run across every active doctor (optionally filtered by department) for a
date instead of one doctor at a time. A doctor with at least one computed window is "on duty";
one with none (full-day leave, or simply no schedule block for that weekday) is not, and the
leave override's `Reason` is surfaced when available.

Endpoints: `GET /api/v1/doctors-on-duty?date=&departmentId=` (defaults to today), on its own
small controller (`DoctorsOnDutyController`) mirroring `QueueController`'s "single-purpose read
endpoint pulled out of AppointmentsController" shape.

Permissions: reuses `APPOINTMENT.SCHEDULE.VIEW` — no new permission, since it's a projection of
the same DoctorSchedule/ScheduleOverride data that permission already gates.

Frontend: `DoctorsOnDutyPage` (`/appointments/doctors-on-duty`) — date + department filters, an
on-duty grid (name, specialty/departments, computed hour windows) and a "not on duty today"
list with the leave reason where recorded.

## Doctor-Appointment Report (added 2026-07-27)

Status: **complete**. A new, standalone `AppointmentReportService`/`AppointmentReportsController`
pair (no engine to extend here, unlike OPD's `OpReportService`) — per-doctor Scheduled/
Checked-in/Completed/Cancelled/No-show counts over a date range, sourced entirely from
`Appointment.Status`. Same generic `ReportResultDto`/`ReportExportHelper` export path as every
other ad-hoc report in this codebase.

Endpoints: `GET /api/v1/appointments/reports/doctor-appointment?fromDate=&toDate=&doctorId=` +
`.../export`, reusing `APPOINTMENT.APPOINTMENT.VIEW` — no new permission.

Frontend: `DoctorAppointmentReportPage` (`/appointments/reports/doctor-appointment`) — optional
doctor filter + date range + Run + CSV/Excel/PDF export.

**Bug fixed 2026-09-17**: `AppointmentReportService` was never registered in the DI container
(`DependencyInjection.cs` imported the namespace but had no `AddScoped<AppointmentReportService>()`
line), so both `GET .../doctor-appointment` and its `/export` sibling threw a 500
(`Unable to resolve service for type 'AppointmentReportService'`) on every call — the menu item
and page rendered fine, but Run always failed. Same class of bug as the `MhcPackageService`
DI gap noted in `loyalty.md`. Fixed by adding the missing registration alongside
`AppointmentService`'s.

## "Front Office control panel" — max advance booking window (added 2026-07-27)

Status: **complete**. Not a new screen — a new `Setting` (`FrontOffice.MaxAdvanceBookingDays`,
default 90, 0 = unlimited; see `docs/modules/admin.md`) enforced only in
`AppointmentService.BookAsync`, the staff "Book Appointment" action. `RescheduleAsync`,
`MapEmergencyTokenAsync` (ER Token Mapping), and Patient Portal booking all call
`BookInternalAsync` directly and are deliberately exempt — this limits how far ahead
front-desk staff manually book, not every path that can produce an appointment.

## Deferred / notes

- **Queue display is not a true anonymous kiosk route.** The plan calls it a "public
  screen route"; this codebase has no anonymous API surface beyond
  login/refresh/health/Hangfire-dev-dashboard, so it runs under the normal authenticated
  shell instead of inventing a new security posture. A real unattended kiosk would need a
  dedicated low-privilege display session — noted as a gap, not silently built around.
- No separate `appointment_tokens` or `no_show_logs` tables — token number lives on
  `Appointment` directly, and no-show marking is covered by `Appointment.Status` +
  `NoShowMarkedAt` + one `IActivityLogger` entry per sweep rather than a per-row log table.
- Appointment double-booking prevention is application-level (check-then-insert), not a DB
  constraint — matches the rest of the codebase's current concurrency posture.


## Go-live QA fixes (2026-11-13)

From the functional QA pass (`qa-reports/`). Migration `20261113_GoLive_QaFixes`.

- No past-date bookings; a *Booked* appointment must fall inside the doctor's schedule (walk-in/ER tokens exempt); check-in only on the appointment day (IST); blocked patients cannot book.
