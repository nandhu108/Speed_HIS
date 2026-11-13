# Module Spec — EMR / Clinical Records (Phase 11)

Status: **complete** (2026-07-17). Owns the unified Patient Timeline, medical/surgical/
family/allergy/immunization histories, the allergy banner + OPD prescription warning hook,
ad-hoc clinical notes, a manually-curated Active/Resolved problem list (diagnosis registry),
prescription/investigation/procedure history views, a discharge summary archive, a lab
results trend view, and EMR attachments (external reports/scanned documents).

## Database (schema `emr`)

| Table | Notes |
|---|---|
| `emr.medical_histories` / `surgical_histories` / `family_histories` / `immunizations` | Plain, repeatable, patient-scoped entries — same append-only posture as IPD's ProgressNote/Nursing's NursingNote. |
| `emr.allergies` | Drives the allergy banner and the OPD prescription warning. `AllergenName` is free text — no drug/allergen master link — matched against candidate drug names by `EmrMath.MatchesAllergen` (case-insensitive substring, either direction). Deactivating an allergy reuses the inherited `BaseEntity.IsActive` rather than a bespoke flag — declaring a second `IsActive` on `Allergy` collided with the base column (`CS0108`), which is exactly the naming trap CLAUDE.md's mandatory audit columns set up; the fix was to use the one that already exists. |
| `emr.problem_list` | The diagnosis registry (plan §11) — patient-level, manually-curated Active/Resolved, distinct from and not synced with OPD's per-visit `VisitDiagnosis` (which still has no status). `IcdCodeId` is optional; `DiagnosisText` is always snapshotted. |
| `emr.clinical_notes` | Ad-hoc notes; `NoteType` is free text ("specialty templates" per plan — no template master, the frontend owns any per-type prompts). `OpVisitId`/`AdmissionId` are optional loose context links. |
| `emr.attachments` | External reports/scanned documents — distinct from Phase 3's `PatientDocument` (registration-time ID proof/photo/consent scan), same on-disk `IFileStorageService`, different subfolder. `VirusScanStatus` is a placeholder column per plan ("virus-scan hook placeholder"); see `IVirusScanner` — nothing writes anything but `NotScanned` yet, same posture as Lab's `IAnalyzerInbound` and Nursing's `IDeviceInbound`-style placeholders. |

**Every EMR table is patient-scoped (`BaseEntity`, no `BranchId`) — deliberately not
branch-scoped**, unlike most of this codebase's transactional data. A patient's allergies,
histories and problem list must read identically regardless of which branch a doctor is
viewing them from; branch-scoping clinical safety data would let a known penicillin allergy
recorded at one branch silently not warn a doctor at another.

Migration: `Phase11_Emr`. No cross-cutting additions to other modules' tables this
phase — EMR reads existing OPD/IPD/Lab data as-is, without needing new columns there.

## Patient Timeline and history views are pure aggregation, no new storage

`TimelineService` queries OpVisits, Admissions, approved LabResults, Prescriptions, IPD
ProcedureNotes, DischargeSummaries, ClinicalNotes and Attachments — each as its own query,
merged and sorted in memory. At one-patient scale that's simpler and cheap enough that a
single UNIONed SQL query isn't worth the complexity. `GetPrescriptionHistoryAsync`/
`GetProcedureHistoryAsync` use the "materialize via `Include`, then project in memory"
shape and therefore need every accessed navigation property `Include`-d; `GetTimelineAsync`'s
per-source blocks and `GetDischargeSummaryArchiveAsync` instead chain `.Select()` directly
on the `IQueryable` before `.ToListAsync()`, which EF Core translates into a SQL join without
needing `Include` at all. Live testing against Postgres caught exactly this distinction:
`GetInvestigationHistoryAsync` initially materialized `LabOrderItem` rows without
`Include(i => i.LabOrder)` and threw a `NullReferenceException` on `i.LabOrder!.OrderDate` in
the in-memory `.Select()` afterward — fixed by adding the `Include`.

## Results trend

`GetResultsTrendAsync(patientId, testId)` returns one series per `LabTestParameter` under the
given test, each a `(Date, Value, Flag)` point list built from approved `ResultValue` rows.
`ResultValue.Value` is free-text (some results are qualitative, e.g. "Positive"), so
`EmrMath.TryParseTrendValue` parses what it can and silently drops anything that isn't
numeric rather than erroring. The frontend renders this as a small inline SVG line chart
(`ResultsTrendChart`) — 2px line, flag-colored point markers (Critical/High/Low use the
status palette, Normal uses the series color), native tooltips, and a collapsible data table
for accessibility. No reference-range shading — `ResultsTrendPointDto` doesn't carry
Normal/Critical bounds, only the resolved flag; adding the band is a small, self-contained
follow-up if wanted.

## The allergy "prescription warning" is a real backend cross-check, not a UI guess

`HistoryService.CheckDrugAllergiesAsync(patientId, drugNames)` cross-references candidate
drug names against the patient's *active* `AllergenType.Drug` allergies via
`EmrMath.MatchesAllergen`, returning which names matched which allergy record (and why —
allergen name, reaction, severity). It's exposed at `GET /emr/patients/{id}/allergy-check`
and wired into OPD's `PrescriptionTab` (`ConsultationWorkspace.tsx`): every drug added to a
prescription is checked live, a red banner lists any conflicts, and the matched line item
gets an "Allergy risk" badge. This is advisory only — nothing blocks saving the
prescription, matching the plan's "prescription warning" wording (not "prescription block").

## Endpoints (`/api/v1/emr/patients/{patientId}`)

- `medical-history`, `surgical-history`, `family-history`, `immunizations`: `GET`, `POST` each
- `allergies?activeOnly=`: `GET`, `POST`; `POST allergies/{id}/deactivate`;
  `GET allergy-check?drugNames=` (advisory cross-check, repeatable query param)
- `problem-list?status=`: `GET`, `POST`; `POST problem-list/{id}/resolve`
- `clinical-notes`: `GET`, `POST`
- `attachments`: `GET`, `POST` (multipart upload); `GET attachments/{id}/download`
- `timeline`: `GET` (unified chronological view)
- `prescriptions`, `investigations`, `procedures`, `discharge-summaries`: `GET` (dedicated history views)
- `results-trend?testId=`: `GET`

## Permissions

`EMR.TIMELINE.VIEW` (covers the timeline and every read-only history view — prescriptions,
investigations, procedures, discharge summaries, results trend), `EMR.HISTORY.{VIEW,MANAGE}`
(medical/surgical/family/allergy/immunization), `EMR.PROBLEMLIST.{VIEW,MANAGE}`,
`EMR.NOTE.{VIEW,MANAGE}`, `EMR.ATTACHMENT.{VIEW,MANAGE}`.

## Frontend

- Everything lives as an "EMR" tab inside `src/features/patients/pages/PatientDetailPage.tsx`
  (`/patients/:id`) — no separate top-level route, matching how Nursing's admission-scoped
  charting lives inside IPD's `AdmissionDetailPage` rather than its own page. Sub-tabbed:
  Timeline, Histories (medical/surgical/family/immunizations stacked as four mini-sections),
  Allergies (active allergies shown as a red banner above the full list; deactivate action),
  Problem List, Clinical Notes, Attachments (upload/download, mirrors Phase 3's
  `DocumentsTab` pattern), Records (prescription/investigation/procedure/discharge-summary
  history views), Results Trend (test picker + `ResultsTrendChart`).
- `src/features/emr/components/ResultsTrendChart.tsx` — self-contained inline SVG line chart,
  no charting library dependency (none existed in the project); light-mode only, matching the
  rest of this app (no dark-mode theming exists anywhere else either).
- OPD's `ConsultationWorkspace.tsx` `PrescriptionTab` calls `useAllergyCheck` on every
  drug-name change and renders the warning banner/badge described above.

## Deferred / notes

- **No reference-range band on the results trend chart.** The DTO carries only the resolved
  flag, not the Normal/Critical bounds — a self-contained follow-up if the shaded band is
  wanted later.
- **No specialty-template picker for clinical notes.** `NoteType` is a free-text field the
  user types into; a real specialty-template system (structured prompts per type) is future
  work, not a Phase 11 deliverable.
- **Problem list isn't auto-populated from OPD diagnoses.** It's a manually-curated registry;
  nothing copies a `VisitDiagnosis` into it automatically.
- **Attachment upload has no virus scan.** `VirusScanStatus` stays `NotScanned` for every
  upload — see `IVirusScanner`, an unimplemented placeholder interface only.


## Go-live QA fixes (2026-11-13)

From the functional QA pass (`qa-reports/`). Migration `20261113_GoLive_QaFixes`.

- Radiology reports are aggregated: timeline event `RadiologyReport` (9) and "Radiology — …" rows in investigations.
