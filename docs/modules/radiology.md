# Module Spec — Radiology / RIS (Phase 12)

Status: **complete** (2026-07-17). Owns the radiology test/report-template masters, order
creation (from an OPD visit, an IPD admission, or directly), modality-based appointment
scheduling, the technician image-capture worklist, contrast consent capture, the structured
report entry → verify → approve pipeline, the Radiology PDF Report, the TAT report, and the
PACS/DICOM placeholder columns. Built as a close structural mirror of Lab (Phase 7) — same
order/order-item shape, same order-status-recomputed-from-items idiom, same entry→verify→approve
pipeline — with parameters/reference-ranges/critical-alerts dropped (radiology results are
narrative, not per-parameter numeric values) and modality scheduling + contrast consent added
in their place.

## Database

| Table | Notes |
|---|---|
| `master.radiology_tests` | Global orderable test catalog — same posture as Lab's `LabTest`. `ModalityId` reuses the `Modality` master already seeded in Phase 2 (X-Ray/CT/MRI/USG/...); no new modality data needed. |
| `rad.report_templates` | Canned Findings/Impression text per test (plan §12 "structured templates + rich text") — "rich text" is a plain long-text field here, same posture as every other narrative field in this codebase (discharge summary sections, clinical notes). |
| `rad.rad_orders` / `rad_order_items` | Mirrors Lab's `lab_orders`/`lab_order_items` exactly: `RadOrder` is branch-scoped directly, `PatientId`/`OpVisitId`/`AdmissionId`/`ClinicalOrderId` are all optional traceability links (plan's "from OPD/IPD/ER/direct" — ER doesn't exist yet). `RadOrderItem.AccessionNumber` is generated per item (PACS placeholder — see below); `Status` drives the full pipeline (Ordered → Scheduled → ImageCaptured → ReportEntered → Verified → Approved, or Cancelled), and the order's own `Status` is recomputed from item statuses by `RadMath.RecomputeOrderStatus`. |
| `rad.rad_schedules` | One row per scheduled `RadOrderItem` — the "Appointment/Slot Scheduling per modality" step. `ModalityId` is denormalized from `RadiologyTest.ModalityId` at scheduling time so the technician worklist filters by modality without an extra join (same snapshot idiom as `BedAllocation.BedChargePerDay`). |
| `rad.contrast_consents` | A dedicated, order-item-scoped consent record — deliberately **not** reusing the shared `PatientConsent` flow (which only links to a patient, not a specific ordered test), per the plan's explicit `contrast_consents` table. Same shape as `PatientConsent` otherwise (SignedByName/RelationshipToPatient/SignedAt/DocumentPath). |
| `rad.rad_reports` | One per `RadOrderItem` — entry→verify→approve mirrors Lab's `LabResult` exactly (the pipeline stage lives on `RadOrderItem.Status`, this row just holds who/when plus `Findings`/`Impression`). `ApproverSignaturePath` is a placeholder column, same posture as Lab. |

Migration: `Phase12_Radiology`. Cross-cutting additions: `SequenceKeys.RadOrderNumber` /
`RadAccessionNumber`, `ActivityEvents.RadOrderCancelled` / `RadReportApproved`,
`SettingKeys.RadiologyPacsViewerUrl` (seeded empty — see PACS/DICOM below).

## PACS/DICOM stays a placeholder, exactly as scoped

Per plan §12 ("placeholder only: `accession_number` generated, `pacs_study_uid` column,
viewer-URL config key"): `RadOrderItem.AccessionNumber` is the only one of the three that's
actually populated, generated per item the same way Lab generates sample barcodes
(`ISequenceService`, branch+year scoped, `ACC-2026-000123`). `PacsStudyUid` is a nullable
column nothing ever writes to. `SettingKeys.RadiologyPacsViewerUrl` is seeded as an empty
string under the Settings UI's new "Radiology" group — a real PACS integration phase would
resolve `{PacsViewerUrl}?study={PacsStudyUid}` or similar; nothing here builds that resolution.

## Contrast consent is an enforced safety gate, not just a checkbox

`RadOrderService.MarkImageCapturedAsync` refuses to progress a contrast-requiring test past
Scheduled unless a `ContrastConsent` row already exists for that order item — verified live
against Postgres: marking a CT-with-contrast item captured without consent returns a 400
("Contrast consent must be captured before image capture for this test"); capturing consent
first, then retrying, succeeds. A non-contrast test (e.g. plain X-Ray) has no such gate.

## Reporting pipeline and order-status rollup

`EnterReportAsync` accepts an optional `TemplateId` — the frontend fetches the template's
`FindingsTemplate`/`ImpressionTemplate` text and prefills the form, but the *submitted* text
is always what's saved (editing after prefill is expected, not blocked). `VerifyReportAsync`/
`ApproveReportAsync` mirror Lab's result pipeline exactly, including marking the traceability
`ClinicalOrder` (if any) `Completed` once the whole order rolls up to `RadOrderStatus.Completed`.
The Radiology PDF Report (`GenerateReportPdfAsync`) refuses to generate before the report is
`Approved`, same posture as Lab's report generation.

## Endpoints

- `rad/tests`: `GET`, `GET {id}`, `POST`, `PUT {id}`, `DELETE {id}` (global master CRUD)
- `rad/templates?radiologyTestId=`: `GET`, `GET {id}`, `POST`, `PUT {id}`, `DELETE {id}`
- `rad/orders`: paged `GET`, `GET {id}`, `POST` (create), `POST {id}/cancel`,
  `POST {id}/items/{itemId}/cancel`, `GET tat-report`
- `rad/scheduling/worklist?modalityId=`: `GET`; `POST scheduling/order-items/{itemId}` (schedule)
- `rad/capture/worklist?modalityId=`: `GET`; `POST capture/order-items/{itemId}` (mark captured)
- `rad/order-items/{itemId}/contrast-consent`: `POST`
- `rad/worklist` (entry queue), `rad/reports/queue?status=` (verify/approve queues): `GET`
- `rad/order-items/{itemId}/report`: `GET`, `POST` (enter), `POST report/verify`,
  `POST report/approve`, `GET report/pdf`

## Permissions

`RAD.TEST.{VIEW,MANAGE}`, `RAD.TEMPLATE.{VIEW,MANAGE}`, `RAD.ORDER.{VIEW,CREATE,CANCEL}`,
`RAD.SCHEDULE.MANAGE` (worklist + scheduling action, one permission — mirrors Lab's
`LabSampleCollect`), `RAD.CAPTURE.MANAGE` (worklist + mark-captured action),
`RAD.CONTRASTCONSENT.CAPTURE`, `RAD.REPORT.{CREATE,VERIFY,APPROVE,VIEW}`.

## Frontend (`src/features/rad`)

- `RadiologyTestsPage` (`/rad/tests`), `RadReportTemplatesPage` (`/rad/templates`) — master
  CRUD, mirroring Lab's `LabTestsPage`/`TestProfilesPage` list+modal pattern.
- `RadOrdersPage` (`/rad/orders`) — list + "New order" modal (patient picker, referring
  doctor, priority, multi-select test checklist showing modality/body-part/contrast flags).
- `RadOrderDetailPage` (`/rad/orders/:id`) — per-item status, accession number, contrast
  consent badge, cancel/print/view-report actions.
- `SchedulingWorklistPage` (`/rad/scheduling`) — items awaiting scheduling, schedule modal
  (datetime picker).
- `CaptureWorklistPage` (`/rad/capture`) — items awaiting image capture; a contrast-requiring
  item without consent shows a "Capture consent" action and disables "Mark captured" until
  consent exists (mirrors the backend gate).
- `ReportingWorklistPage` (`/rad/reporting`) — entry/verify/approve tabs, same shape as Lab's
  `ResultWorklistPage`; the entry modal's template picker prefills Findings/Impression from
  the selected template's detail (fetched on selection, not from the list payload).
- `TatReportPage` (`/rad/tat-report`) — date-range TAT report, mirrors Lab's.
- `UnbilledOrdersReportPage` (`/rad/unbilled-report`) — see "Billing link (added 2026-09-16)" below.

## OPD order bridge (added 2026-09-16)

Status: **complete**. Identical treatment to Lab's own "OPD order bridge" (see
`docs/modules/lab.md`) — `ConsultationWorkspace`'s Orders tab now has an "Order in Radiology"
action on any `Ordered`-status Radiology-type `ClinicalOrder`, navigating to `RadOrdersPage`
with the same `{ opOrder: {...} }` router-state prefill shape Lab uses, reusing the identical
`erOrder`/`opOrder` dual-prefill pattern already on `CreateOrderModal`. Closes half of
`docs/PATIENT_JOURNEY_GAP_ANALYSIS.md` §4.1.

## Billing link (added 2026-09-16)

Status: **complete**. Unlike Lab (which has always carried a soft `BillItemId` on
`LabOrderItem`), Radiology had **no billing link at all** — a genuine gap, not just an
unenforced one. `RadOrderItem` gained the identical soft, unenforced `BillItemId` (no FK, no
gate on scheduling/capture/report entry/approval — same posture as Lab's), plus an "Unbilled
Orders Report" (`RadOrderService.GetUnbilledOrdersReportAsync`, `GET
/api/v1/rad/orders/unbilled-report`) mirroring Lab's exactly, closing
`docs/PATIENT_JOURNEY_GAP_ANALYSIS.md` §4.3's Radiology half. Advisory only — nothing here
blocks anything, it's purely a visibility report.

## Cross-branch verify/enter/consent fix (2026-09-17)

Same gap as Lab's identical fix (see `docs/modules/lab.md`): `RadOrderItem` isn't itself
branch-scoped, so branch isolation only happens when a query `Include`s the required `RadOrder`
navigation (a `BranchEntity`), letting EF Core's query-filter cascade apply. `VerifyReportAsync`,
`EnterReportAsync`, `CaptureContrastConsentAsync` and `GetReportByItemIdAsync` were missing that
`Include` — confirmed live against another branch's item, where these four wrongly succeeded (or
returned a status-mismatch error proving the row was found) while `ScheduleItemAsync`/
`MarkImageCapturedAsync`/`ApproveReportAsync` (which already included `RadOrder`) correctly
404'd. Fixed by adding the same `Include` those three already had; no schema change.

## Known environment gap (2026-09-17): dev DB behind on migrations

Live-testing found `rad.rad_order_items.bill_item_id` (added by this session's own
`20260916101721_Radiology_BillItemLink` migration — see "Billing link" above) missing from the
running dev database; `dotnet ef database update` has not been run since `20260819...AbdmAbha`.
Every endpoint that loads a full `RadOrderItem` entity (scheduling, capture, contrast consent,
report entry/verify/approve, order detail/cancel, the unbilled-orders report) 500s with
`column r.bill_item_id does not exist`; only the narrow-`Select()` list/worklist queries and the
TAT report (which never touch that column) still return 200. This is a migration-application gap,
not a code defect — the entity, migration and query code are all correct — but it currently
breaks nearly every Radiology write action end-to-end until `dotnet ef database update` runs.

## Deferred / notes

- **No radiologist team/roster or per-modality capacity model.** Scheduling is a single
  datetime per item with no slot-conflict checking — unlike Appointments' doctor slot engine,
  nothing here prevents double-booking a modality at the same time.
- **PACS/DICOM integration is schema-only**, per plan §12 — see above. No viewer, no image
  storage, no DICOM listener.
- **No radiologist-specific reporting queue split by modality/sub-specialty.** The
  entry/verify/approve queues are global, not filtered by who's qualified to report which
  modality — same simplification Lab made for its department worklist (department filter
  exists there; radiology has no equivalent split beyond the modality picker on the
  scheduling/capture worklists).


## Go-live QA fixes (2026-11-13)

From the functional QA pass (`qa-reports/`). Migration `20261113_GoLive_QaFixes`.

- **Billing gate** at scheduling for OP orders (same rule as Lab).
- **Scheduling:** not in the past; a modality slot conflict (±15 min) is refused; a Scheduled scan can be rescheduled. Same-scan duplicates are refused.
- Approved reports now appear in the EMR (timeline type `RadiologyReport`, investigations list).
- Timestamps sent with a local offset (+05:30) are normalised to UTC globally (previously HTTP 500).
