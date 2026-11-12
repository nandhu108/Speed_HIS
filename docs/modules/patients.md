# Module Spec — Patient Registration (Phase 3)

Status: **complete** (2026-07-10). Owns the single patient identity (UHID) — the spine of
the system — plus everything captured at registration: demographics, guardian/attender
contacts, Aadhaar/ABHA placeholders, photo, consent capture, duplicate detection/merge,
and the printable registration card.

## Database (schema `patient`, plus `master.consent_templates` and `admin.sequence_counters`)

| Table | Notes |
|---|---|
| `patient.patients` | **Global** (no `branch_id`) — one UHID, visible from every branch. `registered_branch_id` is informational only. Unique partial index on `uhid`; plain indexes on `mobile` and `date_of_birth` for search/duplicate prefiltering. `IsBlocked`/`BlockedReason`/`BlockedAt`/`BlockedByUserId` (added 2026-07-24, commit `5e9e2b2`; undocumented until now) gate a patient from new registration/billing action at the front desk — see "Block/unblock" below. |
| `patient.patient_contacts` | Guardian/attender/emergency contact, `contact_type` enum. |
| `patient.patient_identifiers` | Aadhaar/ABHA placeholder — `is_verified` stays false; no UIDAI/ABDM integration yet (held). |
| `patient.patient_documents` | Any uploaded file (ID proof, consent scan, other) — `file_path` is relative to `IFileStorageService`'s root, never a raw filesystem path. |
| `patient.patient_consents` | One row per signed consent, links to `master.consent_templates`; optional scanned-copy `document_path`. |
| `patient.patient_merges` | Append-style audit of every merge: survivor id, merged id, reason. |
| `master.consent_templates` | Reusable consent text (title + body); seeded with General Treatment / Surgical / Photography-Data-Use starters. |
| `admin.sequence_counters` | `(sequence_key, scope_id, year)` unique; backs `ISequenceService`. `scope_id = 0` means org-wide (used by UHID). |

Migration: `Phase3_PatientRegistration`.

## UHID generation

`ISequenceService.NextValueAsync` runs a single `INSERT … ON CONFLICT … DO UPDATE …
RETURNING last_value` statement — atomic and race-safe under concurrent registrations
without app-level locking. UHID format is `{Prefix}-{Year}-{Value:D{Digits}}` (default
`PRX-2026-000123`), prefix/digit-count configurable via `Patient.UhidPrefix` /
`Patient.UhidDigits` settings. UHID scope is `0` (org-wide) per the plan; other future
sequences (Bill No, IP No…) will use `scopeId = branchId`.

## Duplicate detection

`PatientService.CheckDuplicatesAsync` prefilters candidates by exact mobile or date-of-birth
match (indexed, cheap), then scores each in memory:

- Mobile match: +60
- Date-of-birth match: +35
- Name similarity (`TextSimilarity.NameSimilarity`, a dependency-free normalized
  Levenshtein ratio): +30 (≥0.85), +15 (≥0.7), +5 (≥0.5)

Candidates scoring ≥50 are returned, most-likely first (top 5). This runs both as a
pre-save check during registration (`POST /patients/duplicates/check`, non-blocking —
the UI shows a warning the user can override) and for an existing patient
(`GET /patients/{id}/duplicates`, feeds the merge tool).

## Patient merge

`PatientService.MergeAsync` runs inside `IUnitOfWork.ExecuteInTransactionAsync` (first
real consumer of the transaction abstraction): re-points every `PatientContact`,
`PatientIdentifier`, `PatientDocument` and `PatientConsent` row from the merged patient
to the survivor, marks the merged patient `IsMerged` + `MergedIntoPatientId`, soft-deletes
it, and writes a `PatientMerge` audit row — all committed together. Also logged via
`IActivityLogger` (`PATIENT_MERGED`).

## Files (`IFileStorageService` / `LocalFileStorageService`)

Local-disk storage rooted at `FileStorage:RootPath` (default `App_Data/uploads`, gitignored).
Filenames are GUID-prefixed to avoid collisions; paths are sanitized per segment. Swappable
for blob storage later behind the same interface. Photo uploads: JPEG/PNG/WEBP, ≤5 MB.
Other documents: ≤15 MB.

## Registration card PDF

`IPdfService` / `QuestPdfService` (QuestPDF, Community license) renders an A6 card with
demographics, the patient's photo if uploaded, and a QR code (QRCoder) encoding the UHID —
satisfies the plan's "barcode/QR" requirement via QR alone. `GET /patients/{id}/card`
returns the PDF directly (not wrapped in `ApiResponse`, consistent with other binary
endpoints).

## Endpoints (`/api/v1/patients`)

- `GET` (paged, search across UHID/name/mobile/DOB), `GET {id}`, `POST` (register), `PUT {id}` (update)
- `POST duplicates/check`, `GET {id}/duplicates`
- `POST {id}/merge`
- `POST {id}/photo`, `GET {id}/photo`
- `POST {id}/documents`, `GET {id}/documents`, `GET {id}/documents/{documentId}/download`
- `GET {id}/consents`, `POST {id}/consents` (multipart, optional scan)
- `GET {id}/card`
- `POST {id}/block`, `POST {id}/unblock`

Consent templates: `/api/v1/consent-templates` (standard master CRUD).

## Permissions

`PATIENT.PATIENT.{VIEW,CREATE,UPDATE,MERGE,BLOCK}`, `MASTER.CONSENTTEMPLATE.{VIEW,MANAGE}`.
`UPDATE` covers demographics, contacts, identifiers, photo, documents and consent capture;
`MERGE` is separate given its irreversible, cross-record blast radius; `BLOCK` gates both
`block` and `unblock` (a single permission, same reasoning as OPD's one `VISIT.MANAGE`).

## Block/unblock (added 2026-07-24, commit `5e9e2b2`; undocumented until now)

`PatientService.BlockAsync`/`UnblockAsync` set/clear `IsBlocked` + `BlockedReason` +
`BlockedAt` + `BlockedByUserId`, logged via `IActivityLogger`
(`PATIENT_BLOCKED`/`PATIENT_UNBLOCKED`). `IsBlocked` is returned on both the list and
detail DTOs and surfaced as a badge/action in `PatientsListPage` and `PatientDetailPage`.
Blocking does **not** itself prevent registration/billing actions elsewhere in the system
— it's a visible flag front-desk staff act on, not an enforced gate (no other module
currently checks `IsBlocked` before proceeding). This is a deliberate posture, not an
oversight — a hard block on a flagged patient risks delaying care in a way a billing/data
flag should never force; see `docs/PATIENT_JOURNEY_GAP_ANALYSIS.md` §2.2/§10.

**2026-09-16**: the flag itself was real everywhere from day one, but the shared
`PatientPicker` component — reused by Appointments, OPD, MHC, Camp, Emergency, Billing, IPD's
Admit modal, Lab, Radiology, Pharmacy POS, Loyalty, Insurance, and Nursing Incidents — never
surfaced it, so a blocked patient looked identical to any other in every one of those search
dropdowns. Fixed in the one shared component: a "Blocked" badge on each matching row, and a
red warning line once a blocked patient is actually selected — still advisory, still no
module blocks anything on it, just no longer invisible at the exact moment staff pick a
patient to act on.

### Block/Unblock Report (added 2026-07-27)

Status: **complete**. `PatientReportService.GetBlockUnblockReportAsync` queries the existing
`admin.activity_logs` table (already written by `BlockAsync`/`UnblockAsync` above) filtered
to `PATIENT_BLOCKED`/`PATIENT_UNBLOCKED` in a date range — no new history table. The
activity log's `Description` already embeds the patient UHID and reason as free text (e.g.
`Patient 'UH-2026-000123' blocked: repeated no-shows`), so it's carried through as-is rather
than parsed into structured columns; the only thing genuinely missing before this was the
export layer (`ReportResultDto`/`ReportExportHelper`, same CSV/Excel/PDF path as every other
ad-hoc report).

Endpoints: `GET /api/v1/patients/reports/block-unblock?fromDate=&toDate=` + `.../export`,
reusing `PATIENT.PATIENT.BLOCK` — the same permission that already gates the block/unblock
actions themselves.

Frontend: `BlockUnblockReportPage` (`/patients/reports/block-unblock`) — date range + Run +
CSV/Excel/PDF export, same shape as every other ad-hoc report page.

**Bug fixed 2026-09-17**: `PatientReportService` was never registered in the DI container
(`DependencyInjection.cs` had `PatientService` and the ABDM service but no
`AddScoped<PatientReportService>()`), so `GET .../reports/block-unblock` and its `/export`
sibling threw a 500 (`Unable to resolve service for type 'PatientReportService'`) on every
call — same class of bug as the `AppointmentReportService` DI gap fixed the same day
(`appointments.md`) and the `MhcPackageService` DI gap noted below. Fixed by adding the
missing registration alongside `PatientService`'s.

### Patient-type colors (added 2026-07-27)

Status: **complete**. `master.patient_categories` (Phase 2, previously a plain Code/Name/
Description `ISimpleMaster`) gained `ColorHex` ("#RRGGBB") and became bespoke — same
reasoning as `DoctorPayoutRule`/`AppointmentType` (an extra field the generic
`ISimpleMaster` CRUD framework can't carry). `PatientCategoryService`/
`PatientCategoriesController` moved out of the generic `SimpleMasterService<T>`/
`SimpleMasterControllerBase<T>` framework into their own bespoke pair
(`Features/Masters/PatientCategories`), though the route (`/api/v1/patientcategories`)
and response shape are still a superset of the old generic `SimpleMasterDto` — every other
screen that reads the patient-category dropdown via the generic `useSimpleMaster` hook
(`PatientRegistrationWizard`, `PatientDetailPage`'s edit modal) keeps working unchanged.

`PatientListItemDto`/`PatientDetailDto` both gained `PatientCategoryColorHex` alongside
the existing `PatientCategoryName`.

Endpoints: same CRUD shape as before, now on `SavePatientCategoryRequest{ Code, Name,
Description?, ColorHex?, IsActive }` / `PatientCategoryDto`.

Permissions: unchanged (`MASTER.PATIENTCATEGORY.{VIEW,MANAGE}`).

Frontend: `PatientCategoriesPage` under Masters is now a bespoke editor (mirrors
`AppointmentTypesPage`'s shape) with a color swatch + hex input instead of the generic
table. `PatientsListPage` shows a small color dot next to the category name.

### Registration gap-fill: demographics, address masters, insurance policies (added 2026-07-29, Phase 24)

Status: **complete**. A legacy front-office form audit found `Patient` missing a batch of
fields that the Phase 2 masters had already been built for but never wired up, plus a
handful that didn't exist anywhere. All additive — no existing field renamed or removed
except `City`/`State`/`Nationality`, which converted from free text to FK.

- **Newly wired FKs onto `Patient`** (masters existed since Phase 2, previously orphaned):
  `LanguageId`, `PatientCaseTypeId`, `PatientHcTagId` ("Patient Tag"),
  `SpeciallyAbledCategoryId`.
- **New masters** (`master` schema, generic `ISimpleMaster` framework): `Salutation`,
  `MaritalStatus`, `IdentityDocumentType` (generalizes `PatientIdentifier.Type` beyond the
  hardcoded Aadhaar/Abha enum via a new `Type = Other` + `IdentityDocumentTypeId`).
- **City/State/Nationality → FK conversion**: `Patient.City`/`State`/`Nationality` (plain
  strings) replaced by `StateId`/`CityId`/`AreaId` (FKs to the existing `State`/`City`/`Area`
  masters) and `NationalityId` (FK to the existing `Nationality` master). `CountryId` (country
  of residence, distinct from citizenship) reuses the same `Nationality` master rather than
  a duplicate table. Migration `Phase24_PatientOpdInsuranceFields` backfills the new FK
  columns from the old free-text values by case-insensitive trimmed name match against the
  master tables *before* dropping the string columns — a one-time best-effort cutover;
  unmatched rows are left null, not treated as an error.
- **New scalar fields**: `MotherName`, `NationalId`, `WhatsAppNumber`, `MonthlyIncome`,
  `IsVip`, `SmsOptIn` (default true), `Remarks`, `PermanentAddress` +
  `IsPermanentSameAsPresent` (the existing `Address` field is now explicitly the *present*
  address).
- **Insurance policy capture**: `InsuranceRequired` (bool) is a plain flag — checking it in
  the UI reveals a repeatable policy sub-form that writes `insurance.patient_insurance_policies`
  rows (`InsuranceCompanyId` FK to the existing `master.insurance_companies`, `PolicyNumber`,
  validity dates, `IsPrimary`). No claims/TPA logic lives here — see `docs/modules/insurance.md`
  (Phase 24) for the full pre-auth/claim/settlement workflow this feeds.

Deliberately **not** added here: MLC and a full VIP workflow stay plain informational tags
on OPD's side (`docs/modules/opd.md`) rather than duplicating Emergency's `MlcRecord` or
building a loyalty-style VIP program.

DTOs: `RegisterPatientRequest`/`UpdatePatientRequest`/`PatientDetailDto` all widened with the
above fields; `SavePatientIdentifierRequest` gained `IdentityDocumentTypeId`; new
`SavePatientInsurancePolicyRequest`/`PatientInsurancePolicyDto`.

Permissions: `MASTER.SALUTATION.{VIEW,MANAGE}`, `MASTER.MARITALSTATUS.{VIEW,MANAGE}`,
`MASTER.IDENTITYDOCUMENTTYPE.{VIEW,MANAGE}`, `INSURANCE.POLICY.{VIEW,MANAGE}` (policy
capture only — the workflow permissions live in `insurance.md`).

Frontend: `PatientRegistrationWizard` and `PatientDetailPage`'s edit modal both gained the
new selects (Salutation/Marital Status/Language/Case Type/Patient Tag/Specially Abled,
cascading Country→State→City→Area, generalized ID-document-type picker, and the
insurance-policy sub-form gated behind "Insurance required").

Note: Religion was wired the same way in the initial pass but removed shortly after at the
user's request — `Patient.ReligionId` isn't part of the registration form. `master.religions`
itself is untouched (a pre-existing Phase 2 master, still managed under Masters).

## Frontend (`src/features/patients`)

- `PatientsListPage` — search (UHID/name/mobile/DOB) + `DataTable`.
- `PatientRegistrationWizard` — 4-step (Demographics → Contact & Address → Guardian & IDs →
  Review), DOB-or-estimated-age toggle, duplicate check runs on submit and shows
  `DuplicateWarningModal` (warn, not block) before registering.
- `PatientDetailPage` — tabs (Overview/Contacts/Documents/Consents), inline photo upload
  with an authenticated blob-fetch preview (`usePatientPhotoUrl` — `<img>` can't carry a
  bearer token, so bytes are fetched via axios and swapped in as an object URL), edit
  modal (single form covering demographics + contacts + identifiers), print-card action,
  and a merge tool that surfaces the same fuzzy-match candidates for one-click merge.
- `docs/modules` sibling: `ConsentTemplatesPage` under Masters (title + body text editor).

## Deferred / notes

- Aadhaar/ABHA are capture-only; the verify button is present but disabled (UIDAI/ABDM
  integration is a held module).
- Barcode (Code128) was not added alongside the QR — the plan lists "barcode/QR" as
  alternatives and QR alone covers UHID scanning.
- No `pg_trgm`/trigram index yet — duplicate detection prefilters by exact mobile/DOB
  match rather than fuzzy name search across the whole table; acceptable at current scale,
  worth revisiting if name-only duplicate search becomes a requirement.

## ABDM (Ayushman Bharat Digital Mission) — Full M1, M2 & M3 Integration

Expanded in 2026-10 to provide complete end-to-end ABDM integration covering **Milestone M1, Milestone M2, and Milestone M3**, with complete administrative enable/disable governance in Admin (`/admin/abdm` and `/admin/settings`).

### Governance & Admin Controls
- **System Settings (`admin.system_settings`)**:
  - `Abdm.Enabled` (Master switch: enable/disable all ABDM services)
  - `Abdm.M1Enabled` (Milestone 1: ABHA creation, verification & QR scan)
  - `Abdm.M2Enabled` (Milestone 2: HIP care context linking & FHIR data sharing)
  - `Abdm.M3Enabled` (Milestone 3: HIU consent requests & external health records fetch)
  - `Abdm.BaseUrl`, `Abdm.ClientId`, `Abdm.ClientSecret`, `Abdm.HipId`, `Abdm.HiuId`
- **Admin Console (`/admin/abdm`)**:
  - Interactive status dashboard, one-click toggle switches for master & milestones M1/M2/M3.
  - Gateway connectivity health probe test.
  - Live ecosystem statistics: Total ABHA linked, care contexts linked, health data transferred, consent requests, and external records viewed.
  - Live data transfer log viewer.

### Milestone 1 (M1): ABHA Creation & Verification
- **Methods**: Aadhaar OTP (Full KYC), Mobile OTP, ABHA ID/Address OTP, and Counter **QR Code Scan & Share**.
- **Schema**: `patient.abha_profiles` (ABHA number, address, status, verification method, KYC verified, demographic snapshot) and `patient.abdm_verification_sessions`.
- Syncs with `patient.patient_identifiers` (Type = Abha, `is_verified = true`).
- API: `POST /api/v1/patients/{patientId}/abha/verify/initiate`, `POST verify/complete`, `POST scan-and-share`, `POST unlink`.

### Milestone 2 (M2): Health Information Provider (HIP)
- **Care Context Linking**:
  - Discovers patient clinical encounters: OPD Visits (`OPD-xxxx`), IP Admissions (`IPD-xxxx`), Diagnostic Lab Reports (`LAB-xxxx`), Prescriptions, Discharge Summaries.
  - Maps to `patient.abdm_care_contexts` (`patient_reference`, `abha_number`, `reference_number`, `context_type`, `display`, `is_linked`, `linked_at`).
  - Notifies ABDM Gateway of linked care contexts.
- **FHIR R4 Data Sharing**:
  - Generates NRCeS FHIR R4 compliant Document Bundles with `Composition`, `Patient`, `Practitioner`, `Encounter`, `Condition`, `MedicationRequest`, `Observation`, and `DiagnosticReport`.
  - Records transfers in `patient.abdm_data_transfer_logs`.
- API: `GET /api/v1/abdm/hip/care-contexts/{patientId}`, `POST care-contexts/{patientId}/sync`, `POST care-contexts/{patientId}/link`, `POST share-data`, `GET transfers`.

### Milestone 3 (M3): Health Information User (HIU)
- **Consent Requests**:
  - Clinicians initiate consent requests with Purpose (`CAET`, `BTG`, `PUBHLTH`), date range, expiry, and requested HI Types (`OPConsultation`, `Prescription`, `DiagnosticReport`, `DischargeSummary`).
  - Tracks lifecycle: `Requested` → `Granted` (with `consent_artifact_id`), `Denied`, `Revoked`, `Expired`.
  - Stored in `patient.abdm_consent_requests`.
- **Longitudinal External Health Records**:
  - Pulls encrypted FHIR records from other hospitals across India (AIIMS, Apollo, Fortis, Dr. Lal PathLabs, etc.), decrypts, and extracts clinical summaries.
  - Stored in `patient.abdm_external_health_records`.
  - Surfaced on patient workspace and doctor views with full clinical summary and NRCeS FHIR viewer.
- API: `POST /api/v1/abdm/hiu/consent-requests`, `GET consent-requests/patient/{patientId}`, `POST consent-requests/{id}/check-status`, `POST consent-requests/{id}/fetch-records`, `GET records/{patientId}`.

### Frontend
- **Admin Dashboard**: `/admin/abdm` (permission `ADMIN.ABDM.MANAGE` or `ADMIN.SETTING.VIEW`).
- **Patient Detail Workspace**: Dedicated `ABDM (M1/M2/M3)` tab with sub-tabs for M1 (ABHA Profile & QR Scan), M2 (HIP Care Contexts), and M3 (Longitudinal Health Records).
