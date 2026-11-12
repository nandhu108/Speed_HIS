# Module Spec — OPD (Phase 5)

Status: **complete** (2026-07-10). Owns OP visit registration, the front-desk + doctor
queues, and the consultation workspace — vitals, complaints, history/examination,
diagnosis, prescription (with per-doctor favorites), clinical orders, referrals, and
follow-up advice — plus the visit slip and consultation-summary PDFs.

## Database (schema `opd`)

| Table | Notes |
|---|---|
| `opd.op_visits` | Visit header. Global-unique `visit_number` (`OPD-2026-000123`, per branch+year via `ISequenceService`). `appointment_id` unique-when-not-null — a checked-in appointment can only produce one visit. `consultation_fee` is snapshotted from `Doctor.ConsultationFee`/`FollowUpFee` at registration — see "Billing link" below. |
| `opd.visit_vitals` | One row per visit (unique on `visit_id`). `bmi` computed server-side from height/weight. |
| `opd.visit_complaints` | Many per visit; free-text complaint + duration. |
| `opd.visit_examinations` | One row per visit; history-taking + examination notes. |
| `opd.visit_diagnoses` | Many per visit, FK to the global `master.icd_codes` (Phase 2/3). |
| `opd.prescriptions` / `opd.prescription_items` | One prescription per visit, many drug items (FK `master.drugs`). |
| `opd.prescription_templates` / `opd.prescription_template_items` | Per-doctor saved favorites, applied client-side to prefill a new prescription. |
| `opd.clinical_orders` | Polymorphic order header (`OrderType`: Lab/Radiology/Procedure). No dedicated test master or downstream worklist exists yet — `Description` is free text and `Status` stays `Ordered` until Phase 7/12 exist to progress it. |
| `opd.referrals` | Internal (FK to `master.doctors`) or external (free text) referral out of the visit. |
| `opd.follow_ups` | One row per visit; follow-up date + notes. |

Migration: `Phase5_Opd`.

## New vs. Review detection

At registration, `OpVisitService` checks for a **Completed** visit with the same
patient+doctor where `VisitDate` falls within `Doctor.FollowUpWindowDays` of today,
**inclusive of today** — a same-day repeat visit to the same doctor is a Review, not a
new registration edge case. `ConsultationFee` is set to `Doctor.ConsultationFee` (New) or
`Doctor.FollowUpFee` (Review) accordingly. (This inclusive-of-today boundary was a bug
caught during live testing — the first cut excluded same-day repeats.)

## Token continuity with Appointments

If the visit is registered `AppointmentId`, it reuses that appointment's `TokenNumber`
directly — the patient sees one number from booking through consultation. A walk-in with
no appointment gets a fresh token from a separate per-doctor-per-day sequence
(`OPD_TOKEN`, same atomic-counter mechanism as `APPT_TOKEN` from Phase 4). Completing a
visit that originated from a `CheckedIn` appointment also marks that appointment
`Completed`, closing the loop between the two modules.

## Billing link (draft charge, not a real bill)

The plan calls for the visit to "auto-create a draft consultation charge." Phase 6
(Billing) doesn't exist yet, so there is no `Bill`/`BillItem` entity here — `OpVisit.ConsultationFee`
*is* the draft charge, computed and stored at registration, ready for Billing to read
when it lands. No bill number, no payment, no ledger entry is created by this phase.

**Consultation-charge settings fallback (added 2026-07-27):** the fee is still
`doctor.ConsultationFee`/`FollowUpFee` first — a flat per-doctor field, never a Tariff
lookup (Tariff's branch/patient-category/effective-window resolution only prices
Service-sourced bill lines elsewhere in Billing, not consultation). If that field is 0
(genuinely unconfigured, not a deliberate free consult), `RegisterAsync` falls back to
`Setting` keys `Billing.DefaultConsultationFee`/`DefaultFollowUpFee` (default 0 = no
fallback) — see `docs/modules/admin.md`.

## Consultation workspace

`GET /opd/visits/{id}/workspace` aggregates everything the single-page clinical screen
needs in one call: the visit header, up to 5 recent **completed** visits with this
patient (each visit's diagnoses joined in), medications prescribed in the last 90 days
(labeled "Recent Medications" on the frontend, not "Active Medications" — there's no drug
stop-date tracking yet, so calling them "active" would overclaim), current vitals,
complaints, examination, diagnoses, prescription, orders, referrals, and follow-up advice.
Each section has its own save endpoint (`PUT` for upserts/replace-all lists, `POST`/`DELETE`
for orders and referrals) so the frontend can save one tab without touching the others.

There is no separate "problem list" table — querying `VisitDiagnosis` rows across a
patient's visit history (surfaced in the workspace's recent-visits section) serves as one,
per the plan's "problem list write-through" requirement.

## Endpoints (`/api/v1/opd`)

- `visits`: paged `GET`, `GET {id}`, `POST` (register), `GET {id}/workspace`
- `visits/{id}/start-consult|complete|cancel` (explicit action endpoints, status-guarded)
- `visits/{id}/vitals|examination|follow-up` (`PUT`, upsert)
- `visits/{id}/complaints|diagnoses` (`PUT`, replace-all list)
- `visits/{id}/prescription` (`PUT`, upsert header + replace-all items)
- `visits/{id}/orders` (`GET`/`POST`), `visits/{id}/orders/{orderId}/cancel`
- `visits/{id}/referrals` (`GET`/`POST`), `visits/{id}/referrals/{referralId}` (`DELETE`),
  `visits/{id}/referrals/{referralId}/opinion` (`PUT` — "Doctor Opinion Entry", see below)
- `visits/{id}/slip`, `visits/{id}/summary` (PDF, not wrapped in `ApiResponse`)
- `queue?doctorId=&date=` — today's Waiting/InConsult visits

`/api/v1/prescription-templates?doctorId=`: standard CRUD.

## Permissions

`OPD.VISIT.VIEW`, `OPD.VISIT.CREATE` (front-desk registration), `OPD.VISIT.MANAGE`
(covers every clinical-entry endpoint plus start-consult/complete/cancel — kept as one
permission rather than one per workspace tab, same granularity choice as
`PATIENT.PATIENT.UPDATE` in Phase 3), `OPD.TEMPLATE.{VIEW,MANAGE}`.

## Frontend (`src/features/opd`)

- `FrontDeskPage` (`/opd`) — register a visit (`PatientPicker` reused from Appointments +
  doctor select) and a searchable visit list with cancel.
- `DoctorQueuePage` (`/opd/queue`) — auto-refreshing (15s) token list with a Start action
  that calls start-consult then opens the workspace.
- `ConsultationWorkspace` (`/opd/visits/:id`) — the tabbed clinical screen: Overview,
  Vitals & History, Diagnosis (`IcdPicker`), Prescription (`DrugPicker` + apply-favorite +
  "Send to Pharmacy" once saved — see `docs/modules/pharmacy.md`'s "OPD prescription bridge"),
  Orders ("Order in Lab"/"Order in Radiology" — see the Lab/Radiology "OPD order bridge"
  sections), Referral & Follow-up. Header actions: print slip/summary, start-consult/complete,
  "Admit to IP" (opens IPD's `AdmitPatientModal` in place, patient and this visit pre-linked
  — no navigation away from the workspace unless the admit actually succeeds; see IPD's doc
  for the conversion wiring).
- `PrescriptionTemplatesPage` (`/opd/templates`) — per-doctor favorites CRUD.
- `OpReportPage` (`/opd/report`) — standalone OP Report: date-range filter, Summary
  (day/doctor visit + fee totals) or Detail (one row per visit) toggle, CSV/Excel/PDF export.
  Distinct from the day/doctor-only `OP_CENSUS` row in Reports & Dashboards' Custom Report
  Builder catalog (Phase 23) — this one adds fee totals and a patient-level detail mode.
- `DrugPicker`/`IcdPicker` (`src/features/opd/components`) — debounced catalog search,
  reused across the workspace and the templates page.
- `OpRegistrationUtilityPage` (`/opd/registration-utility`) — one search screen shared by
  three legacy menu entries ("OP Registration Cancel" / "OP Case Sheet Reprint-Direct" /
  "OP Casesheet Reprint-Preview"): find a visit, then cancel it or reprint its case sheet
  (the consultation summary — same PDF as the workspace's "Print summary" button, not the
  front-desk token slip).

## Deferred / notes

- **"OP Case Sheet Reprint" menu entries were reprinting the wrong document (fixed
  2026-09-17).** `OpRegistrationUtilityPage`'s reprint buttons called `downloadVisitSlip`/
  `openVisitSlip` — the small front-desk token/QR receipt, with no clinical content — instead
  of the consultation summary that "case sheet" actually refers to elsewhere in this app (the
  workspace's "Print summary" button). Reported by a user as "not working": clicking either
  menu entry did navigate and did produce a PDF, so nothing errored — it just wasn't the
  document the label promised. Now wired to `downloadVisitSummary`/`openVisitSummary`
  (diagnosis/prescription/advice/follow-up), and the buttons are relabeled "Case sheet
  (direct)"/"Case sheet (preview)" for clarity. No backend change — `GET .../summary` already
  existed and was already used correctly by the workspace.
- **Clinical orders don't automatically reach a worklist** — Lab (Phase 7) and Radiology
  (Phase 12) each have their own independent order-creation flow; a `ClinicalOrder` stays
  free text on `OpVisit` until someone re-keys it there. **Bridged, not automated, 2026-09-16**:
  the Orders tab now has an "Order in Lab"/"Order in Radiology" one-click action on any
  `Ordered` Lab/Radiology `ClinicalOrder`, prefilling the Lab/Rad order-creation screen
  (patient + `ClinicalOrderId` carried through) so the only remaining manual step is actually
  picking the test(s) — see `docs/modules/lab.md`/`docs/modules/radiology.md`'s "OPD order
  bridge" sections and `docs/PATIENT_JOURNEY_GAP_ANALYSIS.md` §4.1. Nothing auto-generates a
  real order from the free-text description alone.
- Follow-up advice does **not** auto-book an appointment. The workspace shows a note
  pointing staff to Appointments → Book Appointment for that patient/doctor/date — a
  deliberate two-click bridge rather than tight coupling between the two modules.
- No allergy banner — `Patient` has no allergy history yet (Phase 11 EMR: "Histories:
  Medical, Surgical, Family, Allergy — drives allergy banner + prescription warning").
  The workspace's "recent medications" list is informational only, not a safety check.
- **OP→IP conversion was schema-ready but never actually wired until now.**
  `AdmitPatientRequest.ReferringOpVisitId` was accepted by the backend from Phase 9 onward,
  but the frontend's Admit Patient modal always sent `null`, and there was no entry point from
  the workspace itself — front desk had to re-search the patient from scratch on the IPD
  Admissions page. Fixed: the workspace's "Admit to IP" button opens the Admit form in place
  with the patient and visit pre-filled, and the modal now actually threads
  `referringOpVisitId` through on submit.

## MHC Registration (front-office extension, added 2026-07-27)

Status: **complete**. Not a numbered phase — MHC (Master Health Checkup) wasn't in the
original plan; it was added on top of OPD's visit engine after a front-office menu gap
review. Scope is deliberately narrow: registration (New/Review, mirroring OPD) + list +
cancel + slip PDF. No consultation workspace — MHC checkups are a priced package, not a
doctor consultation, so there's nothing to chart yet.

**Schema — widened `opd.op_visits` in place, no `MhcVisit` table.** Same reasoning as
Emergency reusing Lab/Radiology, and Stores & Inventory widening Pharmacy's batch tables
with nullable FKs instead of parallel plumbing:

- `doctor_id` became nullable; a new `category` column (`VisitCategory`: `Opd`=1, `Mhc`=2)
  discriminates which flow a row belongs to.
- `mhc_doctor_id` (FK `master.mhc_doctors`) and `mhc_package_id` (FK `master.mhc_packages`,
  new table) are set only for `Category == Mhc`; `ConsultationFee` is reused as-is for the
  package price snapshot.
- No DB check constraint ties `Category` to which FK is populated — enforced in
  `OpVisitService`/`RegisterMhcAsync` only, same app-level-only pattern as the existing
  appointment-linkage guard.
- `master.mhc_doctors` (previously just a Code/Name/Description tagging list) gained
  `follow_up_window_days` (default 14), mirroring `Doctor.FollowUpWindowDays`.
- `master.mhc_packages` is new: Code/Name/Description + `Price` — a bespoke master (like
  `Area`) since `Price` doesn't fit the flat `ISimpleMaster` CRUD framework.

Migration: `Opd_MhcRegistration`.

**New/Review detection deliberately deviates from OPD's rule.** OPD gates on
`Status == Completed`; MHC visits never reach `Completed` in this pass (there's no
workspace to complete them from), so gating on Completed would mean a repeat MHC visit
could never be detected as Review. Instead, the signal is *any non-cancelled prior MHC
visit* for the same patient+MhcDoctor within `MhcDoctor.FollowUpWindowDays` — this also
keeps `Cancel` meaningful, since a Completed-gated visit could never be cancelled under
the existing status guard (`Completed`/`Cancelled` are both terminal).

**Endpoints (`/api/v1/mhc/visits`)** — a narrow surface over the same `OpVisitService`
used by OPD, not a new service: paged `GET`, `GET {id}`, `POST` (register), `POST
{id}/cancel`, `GET {id}/slip` (PDF). No workspace/start-consult/complete/summary routes —
those stay OPD-only. `GET /api/v1/opd/queue` explicitly filters to `Category == Opd` so
MHC registrations never leak into the doctor consultation queue.

`/api/v1/mhc-packages`: bespoke CRUD (like `/api/v1/areas`), not the generic
`SimpleMasterControllerBase`.

**Permissions**: `MHC.VISIT.VIEW`, `MHC.VISIT.CREATE` (front-desk registration),
`MHC.VISIT.MANAGE` (cancel — the only non-clinical status change, no workspace exists to
gate here), `MASTER.MHCPACKAGE.{VIEW,MANAGE}`. Granted to the `RECEPTION` role alongside
`OPD.VISIT.*`.

**Frontend**: `src/features/mhc` — `MhcRegistrationPage` (`/mhc`), mirroring OPD's
`FrontDeskPage` (PatientPicker + MHC doctor select + package select + register form, plus
a searchable list with cancel and print-slip). `src/features/masters/pages/MhcPackagesPage`
(`/masters/mhc-packages`) mirrors `AreasPage`'s bespoke-master pattern.

### MHC Reports (added 2026-07-27)

Status: **complete**. Three package/panel-doctor/referrer breakdowns over the same
`opd.op_visits` rows, added to `OpReportService` (Category == Mhc instead of Category ==
Opd) rather than a parallel service — same engine, same `ReportResultDto`/
`ReportExportHelper` export path as OP Report, just gated on `MHC.VISIT.VIEW` instead of
`OPD.VISIT.VIEW` at the controller:

- **Package-wise** — New/Review counts + total revenue grouped by `MhcPackage`.
- **Doctor-wise** — same shape grouped by `MhcDoctor` (the panel-doctor roster, not the
  regular Doctor master).
- **Referrer-wise** — grouped by `Patient.ReferrerId` → `Referrer` (a global patient
  attribute, not an MHC-specific link); visits with no referrer show as "Walk-in / Direct".

Endpoints: `GET /api/v1/mhc/reports/package-wise|doctor-wise|referrer-wise` + `.../export`
each, on a new `MhcReportsController`.

Frontend: `MhcReportsPage` (`/mhc/reports`) — one page, five tabs now (Package/Doctor/
Referrer/Patient List/Service List, the last two added later — see the OP reports batch
below), each a thin date-range + Run + CSV/Excel/PDF export panel (mirrors
`CampRegistrationReportPage`'s pattern).

**Landing-tab note (found in the pre-go-live QA pass, 2026-09-17).** `DbSeeder.cs` seeds
four separate menu entries at this one route ("MHC Reports", "Service List Report - MHC",
"MHC Package Analysis", "Patient List Report - MHC") so each shows up where the legacy menu
expects it, but all four always land on the same default tab ("Package-wise") since a bare
path carries no tab selection — clicking "Service List Report - MHC" doesn't open Service
List by default. The report itself is correct once the right tab is clicked, so this is
cosmetic, not a data bug. `MhcReportsPage` now reads an optional `?tab=` query param
(`package`/`doctor`/`referrer`/`patient-list`/`service-list`) to pick its landing tab —
deliberately **not** wired up by changing the seeded `Path` values, since `EnsureMenuAsync`'s
`Code` is derived from `path + title` specifically so a redeploy never stomps an admin's own
edits via the Menus screen (`docs/modules/admin.md`), and changing the seed's `Path` argument
for an already-seeded row is a no-op there while creating a fresh duplicate row on a
never-seeded one — not something to change without an explicit menu-data-migration decision.
A hospital admin can get the distinguishing landing tab today, per-installation, by editing
each of these three menu rows' Path in Admin → Menus to append e.g. `?tab=service-list`.

## Camp Registration (front-office extension, added 2026-07-27)

Status: **complete**. Unlike MHC (a distinct visit type with its own doctor roster and
package pricing), a health camp is just an **outreach-event tag** on an ordinary OP
visit — same doctor roster, same fee logic, same New/Review detection. So this reuses
`OpVisitService`/`OpVisitsController`/the OP Report engine almost entirely, rather than
building a parallel flow like MHC did. The `Camp` master
(`backend/src/Prixo.Domain/Entities/Masters/FrontOfficeMasters.cs`) previously had nothing
referencing it anywhere in the backend.

**Schema** — one more nullable FK on `OpVisit`, independent of `Category`:
- `CampId` (FK `master.camps`, `Restrict`) — orthogonal to `Category`; camp tagging does
  not participate in the Opd/Mhc discriminator or in New/Review detection (still keyed on
  patient+doctor exactly as before). Migration: `Opd_CampRegistration`.

**Bundled bug fix**: `OpReportService.GetVisitsReportAsync` never filtered `Category`, so
an MHC visit (`DoctorId == null`) falling inside a queried date range threw a
`NullReferenceException` on `v.Doctor!.User!.FullName`. Fixed by filtering the report's
base query to `Category == VisitCategory.Opd` — the same fix already applied to
`GetQueueAsync` when MHC was built, just missed in this second query at the time.

**Endpoints** — no new controller; extends the existing OPD surface:
- `POST /api/v1/opd/visits` — `RegisterOpVisitRequest` gained a trailing optional
  `CampId` (defaulted, so every existing caller is unaffected).
- `GET /api/v1/opd/visits?campId=` — new optional list filter.
- `PUT /api/v1/opd/visits/{id}/camp` — "Camp Registration Modify": corrects a still-
  `Waiting` visit's `CampId`, status-guarded like `StartConsultAsync`/`CancelAsync`.
  Deliberately gated on `OPD.VISIT.CREATE` (front desk's own permission), **not**
  `OPD.VISIT.MANAGE` — that permission is intentionally coarse (also covers vitals/
  diagnoses/prescription/orders) and granting it to Reception just to unlock this
  correction action would over-grant clinical access.
- `GET /api/v1/opd/reports/visits?campId=` / `.../export?campId=` — "Camp Registration
  Report": the same OP Report engine with an added Camp filter and a `camp` column on
  Detail rows. Not a separate report/service.

**Permissions**: no new permission constants — reuses `OPD.VISIT.{VIEW,CREATE}`. The
`RECEPTION` role gained `MASTER.CAMP.VIEW` (it had `VisitView`/`VisitCreate` but no way to
populate a Camp dropdown until now).

**Frontend** (`src/features/opd`, no new feature folder — Camp visits use the real
`Doctor`/`DoctorName` fields unchanged, unlike MHC's repurposed provider fields):
- `CampRegistrationPage` (`/opd/camp-registration`) — mirrors `FrontDeskPage`: a required
  Camp select that both filters the visit list and sets the default `campId` on new
  registrations, plus an "Edit camp" row action (only while `Waiting`) calling the modify
  endpoint.
- `CampRegistrationReportPage` (`/opd/camp-registration/report`) — a Camp-scoped entry
  point into the same `useOpVisitsReport`/`downloadOpVisitsReport` hooks `OpReportPage`
  already uses, not a duplicate report UI.

## Doctor Opinion Entry (cross-consult, added 2026-07-27)

Status: **complete**. `VisitReferral` (`opd.referrals`, added in Phase 5 for the Referral tab)
already captured an Internal referral's request — direction, referred-to doctor, reason — but
had no way to record the receiving doctor's response. This widens the same row rather than
adding a parallel "consult" entity: `OpinionNotes` (nullable) + `OpinionEnteredAt` (nullable,
null = still pending). Only meaningful when `Direction == Internal` — an External referral
goes to an outside facility, so there's nothing in-system to capture a response against.

`OpVisitService.SaveOpinionAsync` is a plain permission-gated write (`OPD.VISIT.MANAGE`, same
as every other referral action) with no caller-identity check — any authorized staff member can
transcribe a verbal opinion directly on the visit's Referral tab. `GetOpinionRequestsForDoctorAsync`
is the cross-visit query behind the Doctor Portal's own inbox (see
`docs/modules/doctor-portal.md`) — pending requests first, then oldest-first within each group.

Endpoints: `PUT /api/v1/opd/visits/{id}/referrals/{referralId}/opinion`
(`SaveReferralOpinionRequest{ OpinionNotes }`).

Permissions: reuses `OPD.VISIT.MANAGE` — no new OPD permission.

Frontend: `ConsultationWorkspace`'s Referral tab shows an inline "Enter opinion" action on any
Internal referral without one yet, and the recorded opinion once entered — no new tab, no modal.

## OP TAT / Cancel / Conversion / Referral Reports (added 2026-07-27)

Status: **complete**. Four more ad-hoc reports added as methods on `OpReportService`
alongside `GetVisitsReportAsync` — no new service classes, no schema changes; every one is
a plain `IAppDbContext` query producing the same generic `ReportResultDto`, exported via
the shared `ReportExportHelper` (CSV/Excel/PDF) exactly like OP Report and Camp Report:

- **OP TAT Report** — registration→consult-start and consult-start→completion turnaround in
  minutes, computed from timestamps the consultation workspace already stamps (`CreatedAt`,
  `InConsultAt`, `CompletedAt`) — no new columns, only `Completed` visits with both
  timestamps set are included. Optional doctor filter.
- **OP Cancel Report** — every `Category == Opd` visit with `Status == Cancelled`, with
  `CancelledAt`/`CancelReason`.
- **OP to IP Conversion Report** — completed OPD visits vs. how many produced an admission,
  grouped by day. Reuses the existing, already-wired `Admission.ReferringOpVisitId` loose
  link (no new schema) — a visit counts as "converted" if any admission references it.
- **Referral Report** — every Internal/External referral raised from an OPD visit
  (`opd.referrals`), with the cross-consult opinion status ("Doctor Opinion Entry" above:
  Pending/Completed) for Internal rows. Optional direction filter.

Endpoints: `GET /api/v1/opd/reports/tat|cancelled|conversion|referrals` + `.../export` each,
all reusing `OPD.VISIT.VIEW` — no new permissions.

Frontend: `OpTatReportPage`, `OpCancelReportPage`, `ConversionReportPage`,
`ReferralReportPage` — each a thin date-range (+ doctor/direction filter where relevant) +
Run + CSV/Excel/PDF export panel, same shape as `CampRegistrationReportPage`.

## Registration gap-fill: Booking Type, Class, Session, MLC tag, advance collection (added 2026-07-29, Phase 24)

Status: **complete**. Four additive fields/actions on OP visit registration, none wired into
the slot/queue/token engine:

- **`BookingType`** (`AppointmentBookingType` enum, reused from Appointments) — an explicit
  registration-time field alongside the existing `AppointmentId`-presence inference. Both
  signals can coexist; nothing currently reconciles them if they disagree, since `BookingType`
  is descriptive metadata, not a state machine input.
- **`VisitClassId`/`SessionId`** — two new purely-descriptive masters (`master.visit_classes`
  e.g. Regular/Express/Premium, `master.visit_sessions` e.g. Morning/Afternoon/Evening),
  same additive, unwired posture as the existing `AppointmentType` tag.
- **`IsMlcCase`** (bool) — a plain tag marking this OP episode as medico-legal. Deliberately
  **not** wired to Emergency's `MlcRecord`/`MlcType` — a general-OP medico-legal case (e.g. an
  assault victim registered through the front desk rather than ER) gets a flag here; the
  statutory MLC register stays Emergency's own workflow (`docs/modules/emergency.md`). **This
  stayed genuinely unreachable until 2026-09-16** — the flag was captured at registration but
  no screen anywhere let staff query which OP visits carried it, so it was effectively
  write-only. `OpReportService.GetVisitsReportAsync` (the OP Report) gained an `IsMlcCase`
  filter plus an "MLC" (Yes/No) detail column — `OpReportPage` has an "MLC cases only"
  checkbox alongside its other filters. Closes `docs/PATIENT_JOURNEY_GAP_ANALYSIS.md` §3.2 the
  way that entry recommends: a plain report over the existing flag, **not** a merge into
  Emergency's statutory register (the two stay intentionally separate concepts).
- **Advance collection at registration** — `RegisterOpVisitRequest` gained optional
  `AdvanceAmount`/`PaymentModeId`/`AdvanceReferenceNumber`. Reuses Billing's existing
  `AdvanceService.CollectAsync` (Phase 6) through a new `IAdvanceCollector` interface
  (`Application/Common/Interfaces`) — same posture as `IFinanceLedgerPoster`: declared where
  the caller (OPD, an earlier phase) lives, implemented by `AdvanceService` (a later phase),
  registered in DI as `services.AddScoped<IAdvanceCollector>(sp => sp.GetRequiredService<AdvanceService>())`.
  `OpVisitService.RegisterAsync` now wraps the visit insert + advance collection in
  `IUnitOfWork.ExecuteInTransactionAsync` (previously a plain `SaveChangesAsync`, since there
  was only ever one write) so a payment-mode failure can't leave an orphaned visit.

Endpoints: `RegisterOpVisitRequest`/`OpVisitDetailDto` both widened — no new routes.

Permissions: no new permissions; the existing `OPD.VISIT.CREATE` gate covers all of the above
since they're just additional fields on the same registration call.

Frontend: `FrontDeskPage`'s registration panel gained Booking Type/Class/Session selects, an
MLC checkbox, and an Advance Amount + Payment Mode + Reference input row.

## Department on OP registration (added 2026-11-12)

- `master.departments` gained a **`type`** column (`DepartmentType`: Clinical = 1, NonClinical = 2; default Clinical). The Department master moved out of the generic `ISimpleMaster` CRUD framework into a bespoke `DepartmentService`/`DepartmentsController` (same route + permissions, same reasoning as Patient Category's `ColorHex`); `GET /departments?type=1` returns clinical departments only. The Speed Multi seeder marks its workbook "Diagnostic" units (Laboratory, Radiology) NonClinical.
- `opd.op_visits` gained a nullable **`department_id`**. `RegisterOpVisitRequest.DepartmentId` is **mandatory** (validator + service): the department must exist, be active and be Clinical (`DepartmentService.EnsureClinicalAsync`). Nullable in the DB only for MHC rows and pre-existing visits; the migration backfills existing visits from the doctor's first mapped department.
- The Camp CSV bulk import calls `RegisterAsync` directly, so it falls back to the doctor's first mapped department (and fails the row if there is none).
- All three registration forms (OPD Front Desk, Front Office quick panel, Camp Registration) show a required Department select; the Doctor list narrows to that department's doctors plus any doctor with no department mapping.
