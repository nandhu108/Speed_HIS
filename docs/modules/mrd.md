# Module Spec — MRD (Phase 17)

Status: **complete** (2026-07-20). Per the plan, MRD was scheduled after IPD/Nursing/EMR
because it works off completed admissions ("needs discharges to exist" — plan §12); it adds
one new schema (`mrd`) plus one small master (`master.record_document_types`) rather than
widening an existing module.

## New domain (schema `mrd`)

| Table | Notes |
|---|---|
| `mrd.record_files` | The physical file per admission — `AdmissionId` (unique, partial index), `FileNumber` (sequence-generated, `MR-2026-000123`), `Status` (Active/Destroyed), and a `CurrentLocation`/`CurrentCustodianUserId` snapshot kept in sync by every movement. Branch-scoped. |
| `mrd.file_movements` | Append-only location/custodian history — one row per move, written both by the explicit "record movement" action and by `FileRequestService.IssueAsync`/`ReturnAsync`. |
| `mrd.file_requests` | File Issue & Return **and** internal/external/legal Record Requests share this one table (plan's schema list has a single `file_requests`), distinguished by `RequestType`. Lifecycle: Requested → Approved/Rejected → Issued → Returned. `IsOverdue` is computed on read (`Status == Issued && today > ExpectedReturnDate`), not stored. |
| `mrd.scanned_documents` | Document Scanning — same on-disk storage idiom as EMR's `Attachment`/Phase 3's `PatientDocument`, scoped to an admission, typed via `master.record_document_types`. `VirusScanStatus` stays the same placeholder column (see `IVirusScanner`). |
| `mrd.coding_worklist` | One row per admission, created lazily the first time a coder marks a case coded (find-or-create upsert, same idiom as `EmergencyAssessment`). Diagnosis codes themselves live in EMR/IPD/OPD's existing diagnosis records — this table only tracks the MRD coder's completion of that admission. |
| `mrd.file_audits` | Discharge File Audit — one row per admission (upsert). Five boolean checklist flags (summary signed / consents / progress notes / investigation reports / nursing records present) plus free-text `MissingDocumentsNotes`; `IsComplete` is recomputed from the flags on every save by `MrdMath.IsAuditComplete`. |
| `mrd.retention_policies` | Hospital-wide (not branch-scoped) `Code`/`Name`/`RetentionYears`/`Description`. Kept as a small bespoke CRUD service rather than the generic simple-master framework because `RetentionYears` has no slot in `ISimpleMaster`. Seeded with three baseline policies (IP/OP/MLC record). |
| `mrd.destruction_register` | One row per record file flagged for destruction under a policy. Eligibility itself (`MrdMath.EligibleDate` = discharge date + retention years) is computed on read and only turned into a row when MRD deliberately flags a file — that's what starts the Eligible → Approved → Destroyed lifecycle. Destroying an entry also flips `RecordFile.Status` to `Destroyed`. |

New master: `master.record_document_types` (plan §17: "document-type master") — a plain
`Code`/`Name`/`Description` lookup on the existing generic `SimpleMasterService<T>` framework,
seeded with Discharge Summary/Consent Form/Progress Notes/Lab Report/Radiology Report/Referral
Letter/Other.

Migration: `Phase17_Mrd`.

## Pure logic

`MrdMath` (unit-tested, no I/O) holds the three rules the services delegate to:
`IsOverdue(expectedReturnDate, today)`, `EligibleDate(fromDate, retentionYears)`, and
`IsAuditComplete(...)` from the five checklist flags.

## Endpoints (`/api/v1/mrd`)

- `files`: `GET` (board, filterable by status/admissionId/search), `POST` (open a file for an admission)
- `files/{id}`: `GET`
- `files/{id}/movements`: `GET`, `POST` (record a movement)
- `requests`: `GET` (queue, filterable by status/overdueOnly/recordFileId), `POST` (raise)
- `requests/{id}/approve`, `/reject`, `/issue`, `/return`: `POST`
- `mrd/admissions/{admissionId}/documents`: `GET`, `POST` (multipart upload); `/{documentId}/download`: `GET`
- `mrd/admissions/{admissionId}/file-audit`: `GET`, `PUT` (upsert)
- `coding-worklist`: `GET` (filterable by onlyPending); `coding-worklist/{admissionId}/code`: `POST`
- `retention-policies`: `GET`, `POST`, `PUT /{id}`, `DELETE /{id}`
- `destruction-register`: `GET`, `POST` (flag a file); `/eligible?retentionPolicyId=`: `GET`; `/{id}/approve`, `/{id}/destroy`: `POST`
- `reports/coding-stats`, `reports/file-movement`, `reports/incomplete-records`: `GET` (all filterable by fromDate/toDate)
- `document-types` (on `RecordDocumentTypesController`, under Masters' generic framework): `GET`/`POST`/`PUT`/`DELETE`

## Permissions

`MRD.FILE.{VIEW,CREATE,MANAGE,ISSUE}`, `MRD.REQUEST.CREATE`, `MRD.DOCUMENT.{VIEW,UPLOAD}`,
`MRD.CODING.{VIEW,MANAGE}`, `MRD.AUDIT.{VIEW,MANAGE}`, `MRD.RETENTION.{VIEW,MANAGE}`,
`MRD.DESTRUCTION.{VIEW,MANAGE}`, `MRD.REPORT.VIEW`, `MASTER.RECORDDOCUMENTTYPE.{VIEW,MANAGE}`.
`MRD.FILE.ISSUE` (approve/reject/issue/return on a request) is the exact code the plan names
as its permission-naming example (plan §16.3).

## Frontend

- `RecordFilesPage` (`/mrd/files`) — browse/search the file board; row click → `RecordFileDetailPage`.
- `RecordFileDetailPage` (`/mrd/files/:id`) — file snapshot, movement history + "record movement",
  and this file's requests + "raise request" (internal auto-fills the requester as the current
  user; external/legal require a requester name).
- `FileRequestsPage` (`/mrd/requests`) — the approval queue: status/overdue filters,
  approve/reject/issue/return actions.
- `CodingWorklistPage` (`/mrd/coding-worklist`) — discharged admissions with coding status,
  "mark coded" with optional notes.
- `AuditsPage` (`/mrd/audits`) — reuses the incomplete-records report as the work queue
  (deliberately: an audit that's already complete needs no attention), each row links to the
  IPD admission's MRD tab where the checklist is actually filled in.
- `RetentionDestructionPage` (`/mrd/retention`) — tabbed: policy CRUD, and destruction register
  (eligible-files-per-policy lookup → flag → approve → destroy).
- `MrdReportsPage` (`/mrd/reports`) — tabbed: coding stats, file movement, incomplete records,
  all date-range filterable.
- **`AdmissionMrdTab`** — embedded into IPD's `AdmissionDetailPage` as a new "MRD" tab, the same
  cross-feature pattern ICU used for `AdmissionIcuTab`: open/view the record file, upload/list/
  download scanned documents (document-type dropdown reuses Masters' generic `useSimpleMaster`
  hook), and the file-audit checklist — all scoped to the admission already in context, so no
  admission-picker component was needed anywhere in the module.
- Masters gained a "Document Types" screen (`/masters/document-types`) via the existing generic
  `SimpleMasterPage`.

## Deferred / notes

- **No PDF prints this phase** — MLC/discharge-audit-style statutory documents were not asked
  for by name in the plan's MRD bullet list (unlike ER's MLC register); the underlying data and
  screens exist, same "deferred PDF" pattern used when a report layout wasn't specified.
- **No AI ICD-coding suggestion** — plan §21/Table lists "AI ICD Coding" as a future roadmap
  item (plan §22), not part of Phase 17's scope; the coding worklist only tracks a human
  coder's completion.
- **Frontend not exercised in a real browser** — verified via `tsc -b && vite build` (clean)
  and `oxlint` (clean, one pre-existing unrelated warning in `AuthContext.tsx`); no browser tool
  is available in this environment.
