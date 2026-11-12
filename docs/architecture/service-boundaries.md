# Service Boundaries — Prixo HIS

**Status:** design, 2026-08-13.
**Scope:** the 23 logical domain services. A *logical service* is a bounded context with owned data
and a published contract. **Most of these remain modules inside the monolith** — the boundary is
logical first, physical only where [microservices-strategy.md](microservices-strategy.md) justifies
it.

Legend for **Deployment**: `Core` = inside the monolith · `Extract-N` = extracted at roadmap phase N ·
`Reassess` = candidate, not scheduled.

---

## A. Identity & Access

| | |
|---|---|
| **Owns** | `admin.user`, `role`, `permission`, `user_role`, `role_permission`, `user_permission_override`, `role_preset*`, `user_branch`, `branch`, `designation`, `menu*`, `refresh_token`, `user_session`, `password_history`, `system_setting` |
| **Responsibilities** | Authentication (staff + portal audiences), session lifecycle, permission resolution, branch/department assignment, MFA (future), data-driven menus, settings |
| **Consumers** | Every module (identity), frontend (menus), audit backbone (actor resolution) |
| **Contract (sync)** | `POST /auth/login|refresh|logout`, `GET /menus/my`, `HasPermissionAsync(userId, code)` |
| **Events published** | `UserAuthenticated`, `AuthenticationFailed`, `SessionRevoked`, `PermissionsChanged`, `UserLocked` |
| **Deployment** | **Core.** Score 31 but availability-critical — its outage stops everything. Extract last, if ever. |
| **Boundary work** | Actor-role and department must be resolvable *as at a point in time* for audit (roles change) — add effective-dated role assignment history. |

## B. Patient Identity

| | |
|---|---|
| **Owns** | `patient.patient`, `patient_contact`, `patient_identifier`, `patient_document`, `patient_consent`, `patient_merge`, `sequence_counter` (UHID scope) |
| **Responsibilities** | UHID issue, demographics, identifiers (Aadhaar/ABHA placeholders), duplicate detection, merge, consent capture, patient documents |
| **Consumers** | ~37 entity types across every clinical, revenue, and compliance module |
| **Contract (sync)** | `GET /patients/{id}`, `GET /patients/search`, `POST /patients`, `POST /patients/merge` |
| **Events published** | `PatientRegistered`, `PatientUpdated`, `PatientMerged`, `PatientBlocked`, `PatientUnblocked`, `ConsentCaptured` |
| **Deployment** | **Core → Reassess.** Score 27. Blocked by 37 `PatientId` FKs and a merge that transactionally re-points them. |
| **Boundary work** | Merge must become an event-published operation before any consumer holds a local patient copy. |

## C. Appointment

| | |
|---|---|
| **Owns** | `appointment.doctor_schedule`, `schedule_override`, `unit_schedule`, `appointment` |
| **Responsibilities** | Schedules, slot computation, booking/reschedule/cancel/check-in, token issue, transfer/skip, queue, no-show marking |
| **Consumes** | Patient Identity (patient), Masters (doctor, unit, appointment type), Settings (max advance days) |
| **Events published** | `AppointmentBooked`, `AppointmentRescheduled`, `AppointmentCancelled`, `AppointmentCheckedIn`, `TokenGenerated`, `TokenTransferred`, `AppointmentNoShow` |
| **Deployment** | **Core.** Score 28. |

## D. Encounter / Clinical

| | |
|---|---|
| **Owns** | `opd.op_visit` and children (vitals, complaints, examination, diagnosis, prescription*, clinical_order, visit_referral, follow_up_advice) |
| **Responsibilities** | OP encounter lifecycle, consultation workspace, diagnosis, prescription, orders, referrals/opinions, MHC + Camp variants |
| **Provides** | **`EncounterRef` resolution** across `op_visit` / `ipd.admission` / `er.emergency_visit` — the abstraction introduced in Phase 2 |
| **Events published** | `EncounterCreated`, `EncounterClosed`, `VitalsRecorded`, `DiagnosisRecorded`, `ClinicalNoteSigned`, `PrescriptionCreated`, `ReferralRaised` |
| **Deployment** | **Core.** Score 17. |
| **Boundary note** | `EncounterRef` is a projection, not a table merge. The three roots stay independent. |

## E. EMR

| | |
|---|---|
| **Owns** | `emr.medical_history`, `surgical_history`, `family_history`, `allergy`, `immunization`, `problem_list_entry`, `clinical_note`, `attachment` |
| **Responsibilities** | Longitudinal record, histories, allergies (feeding OPD prescription cross-check), problem list, timeline aggregation, attachments |
| **Deployment** | **Core → Reassess** as a read/query service (score 21; it owns little and reads much) |
| **Boundary work** | The timeline aggregator issues 8 queries per patient and merges in memory — acceptable for one patient, not for the audit journey. Phase 3 introduces a proper read model. |

## F. IPD

| | |
|---|---|
| **Owns** | `ipd.admission`, `admission_consultant`, `bed_allocation`, `doctor_order`, `medication_order`, `diet_order`, `progress_note`, `ip_vitals`, `intake_output_entry`, `procedure_note`, `discharge_clearance`, `discharge_summary`, `admission_request` |
| **Consumed by** | Billing, Nursing, ICU, OT, ER, MRD, Insurance, Quality, Reports, both portals — **the widest producer in the system** |
| **Events published** | `AdmissionRequested`, `AdmissionCreated`, `BedAllocated`, `BedTransferred`, `ConsultantTransferred`, `MedicationOrdered`, `DischargeRequested`, `ClearanceGranted`, `DischargeSummaryApproved`, `DischargeCompleted` |
| **Deployment** | **Core, permanently.** Score 14 — the load-bearing wall. |

## G. Nursing

| | |
|---|---|
| **Owns** | `nursing.nursing_assessment`, `nursing_note`, `risk_assessment`, `care_plan`, `transfer_note`, `mar_schedule`, `mar_administration`, `shift_handover`, `patient_handover_note`, `incident` |
| **Note** | `nursing.incident` is **the org-wide incident register** (already widened with RCA + sentinel-event fields). The Compliance domain reads it; it is not duplicated. |
| **Events published** | `NursingAssessmentRecorded`, `MedicationAdministered`, `MedicationSkipped`, `MedicationRefused`, `RiskScoreRecorded`, `HandoverCompleted`, `IncidentReported`, `IncidentReviewed`, `RcaRecorded` |
| **Deployment** | **Core.** Score 17 — MAR depends on IPD medication orders. |

## H. Laboratory

| | |
|---|---|
| **Owns** | `lab.lab_test`, `lab_test_parameter`, `reference_range`, `test_profile`, `profile_test`, `lab_order`, `lab_order_item`, `sample`, `lab_result`, `result_value`, `critical_alert` |
| **Events published** | `LabOrderCreated`, `LabOrderCancelled`, `SampleCollected`, `SampleReceived`, `SampleRejected`, `LabResultEntered`, `LabResultVerified`, `LabResultApproved`, `CriticalResultDetected`, `CriticalResultAcknowledged` |
| **Deployment** | **Core → Reassess** (score 31). Clean order/result contract; a natural second-wave candidate once events and tracing exist. |

## I. Radiology

| | |
|---|---|
| **Owns** | `rad.radiology_test`, `rad_report_template`, `rad_order`, `rad_order_item`, `rad_schedule`, `contrast_consent`, `rad_report` |
| **Events published** | `RadiologyOrderCreated`, `RadiologyScheduled`, `ImagesCaptured`, `RadiologyReportEntered`, `RadiologyReportVerified`, `RadiologyReportApproved` |
| **Deployment** | **Core → Reassess** (score 31). Extract as a pair with Lab or not at all. |
| **Integration** | PACS/DICOM remains a placeholder (`AccessionNumber` generated, `PacsStudyUid` never written). |

## J. Pharmacy

| | |
|---|---|
| **Owns** | `pharmacy.store`, `rack`, `batch`, `stock_ledger`, purchase (`purchase_request*`, `purchase_order*`, `grn*`, `purchase_return*`), sales (`pharmacy_sale*`, `sales_return*`), issues (`issue*`, `issue_return*`), `stock_adjustment*`, `stock_transfer*`, `stock_audit*` |
| **Critical constraint** | `stock_ledger` and `batch` are **written by four modules** (Pharmacy, OT implants/consumables, Biomed spares, Stores indent issue) inside their own transactions |
| **Events published** | `StockReceived`, `StockIssued`, `StockAdjusted`, `StockTransferred`, `MedicationDispensed`, `SaleCancelled`, `NearExpiryDetected`, `LowStockDetected` |
| **Deployment** | **Core.** Score 22 — cannot extract until stock mutation is funnelled through one owner API. |

## K. Inventory / Stores

| | |
|---|---|
| **Owns** | `pharmacy.indent`, `indent_item`; shares the Pharmacy stock engine via `ItemId` (non-drug) alongside `DrugId` |
| **Deployment** | **Core, merged with Pharmacy.** It is not a separate service — it is the same stock engine with a different catalogue. Treat J+K as one bounded context, "Supply". |

## L. Billing

| | |
|---|---|
| **Owns** | `billing.bill`, `bill_item`, `bill_payment`, `refund`, `discount_approval`, `advance`, `day_close*`, `bill_note`, `bill_credit_note*`, `cashier_shift*`, `bill_estimate*`, `payment_mode`, `online_payment_request` |
| **Critical constraint** | `bill_item` is appended by IPD (bed charges), OT (implants), Pharmacy (sale), ER (empty bill); `bill.insurance_company_id` written by Insurance |
| **Events published** | `BillCreated`, `BillFinalized`, `BillCancelled`, `PaymentReceived`, `RefundRequested`, `RefundApproved`, `DiscountRequested`, `DiscountApproved`, `CreditNoteIssued`, `DayClosed` |
| **Deployment** | **Core, permanently.** Score 16 — 7-module fan-in. |

## M. Finance

| | |
|---|---|
| **Owns** | `finance.account`, `voucher`, `voucher_line`, `ledger_entry`, `payable`, `receivable` |
| **Consumes** | Billing (payments via `IFinanceLedgerPoster`), Insurance (claim settlement), Pharmacy (GRN payables) |
| **Events consumed** | `PaymentReceived`, `ClaimSettled`, `GrnPosted` → ledger postings (currently a deliberate no-op until accounts are configured) |
| **Deployment** | **Core.** Score 25 — prepare boundary. |

## N. Insurance / TPA

| | |
|---|---|
| **Owns** | `insurance.patient_insurance_policy`, `pre_auth_request`, `pre_auth_enhancement`, `claim`, `claim_document` |
| **Events published** | `PolicyCaptured`, `PreAuthRequested`, `PreAuthApproved`, `PreAuthRejected`, `EnhancementRequested`, `ClaimSubmitted`, `ClaimUnderReview`, `ClaimApproved`, `ClaimRejected`, `ClaimSettled` |
| **Deployment** | **Core → Reassess** (score 28). External-facing, so isolation eventually pays. |

## O. Operation Theatre

| | |
|---|---|
| **Owns** | `ot.theatre`, `equipment`, `surgery_booking`, `surgery_team_member`, `surgical_safety_checklist`, `pre_anaesthesia_assessment`, `intra_op_anaesthesia_record`, `operative_note`, `post_op_note`, `implant_usage`, `consumable_usage` |
| **Events published** | `SurgeryScheduled`, `SurgeryRescheduled`, `SurgeryCancelled`, `ChecklistPhaseCompleted`, `SurgeryStarted`, `SurgeryCompleted`, `ImplantUsed` |
| **Deployment** | **Core.** Score 18 — completion writes OT + Pharmacy + Billing in one transaction. |

## P. ICU

| | |
|---|---|
| **Owns** | `icu.ventilator_session`, `ventilator_observation`, `infusion_chart_entry`, `infusion_rate_entry`, `icu_severity_assessment`, `critical_alert_rule`, `icu_vitals_alert`, `device_reading` |
| **Note** | An ICU patient **is** an `ipd.admission` on an `Ward.IsIcu` ward. There is no ICU stay entity and none should be created. |
| **Events published** | `VentilatorSessionStarted/Ended`, `IcuVitalsRecorded`, `IcuAlertRaised`, `SeverityAssessed` |
| **Deployment** | **Core, permanently.** Score 14. |

## Q. Emergency

| | |
|---|---|
| **Owns** | `er.emergency_visit`, `triage`, `emergency_assessment`, `mlc_record`, `accident_case_record`, `observation_allocation` |
| **Events published** | `EmergencyVisitRegistered`, `TriageCompleted`, `EmergencyAssessed`, `DispositionRecorded`, `MlcRegistered`, `AdmitConverted` |
| **Deployment** | **Core.** Score 17 — a workflow over Patients/Lab/Rad/IPD/Billing. |

## R. MRD

| | |
|---|---|
| **Owns** | `mrd.record_file`, `file_movement`, `file_request`, `scanned_document`, `coding_worklist_entry`, `file_audit`, `retention_policy`, `destruction_register_entry` |
| **Reuse note** | `file_audit` already implements a discharge-file completeness checklist with a computed flag — the Clinical Documentation Completeness engine generalizes it rather than replacing it |
| **Events published** | `RecordFileCreated`, `FileIssued`, `FileReturned`, `DocumentScanned`, `CaseCoded`, `RecordDestroyed` |
| **Deployment** | **Core.** Score 29 but negligible extraction value. |

## S. Biomedical

| | |
|---|---|
| **Owns** | `biomed.biomed_equipment`, `equipment_location_log`, `amc_contract`, `pm_schedule`, `work_order`, `breakdown_complaint`, `calibration`, `service_history_entry`, `spare_usage` |
| **Events published** | `EquipmentRegistered`, `EquipmentMoved`, `PmDue`, `WorkOrderRaised/Completed`, `BreakdownReported/Resolved`, `CalibrationDue/Recorded` |
| **Deployment** | **Core.** Score 32, but no scaling/ownership/isolation reason to extract. |

## T. Quality & Compliance

| | |
|---|---|
| **Owns today** | `quality.quality_indicator*`, `quality_audit*`, `quality_non_conformance`, `quality_risk`, `quality_capa`, `quality_committee*`, `quality_nabh_standard`, `quality_evidence` |
| **Gains (Phases 4–7)** | `regulatory_framework`, `regulatory_requirement`, `requirement_mapping`, generalized `evidence` + `evidence_link`, `documentation_requirement`, `safety_event`, `audit_gap` |
| **Reads** | `nursing.incident` (org-wide register — not duplicated), Lab TAT, IPD occupancy, audit events |
| **Events published** | `NonConformanceRaised`, `CapaCreated`, `CapaClosed`, `RiskRegistered`, `AuditGapDetected`, `SafetyEventDetected`, `EvidenceUploaded`, `RequirementMapped` |
| **Deployment** | **Core.** Score 34 — the best-isolated large module and the home of the new compliance capability. |

## U. Notification

| | |
|---|---|
| **Owns** | notification templates, delivery queue (supersedes `admin.notification_queue`), delivery attempts, escalation policies, channel config |
| **Responsibilities** | SMS · Email · Push · In-app · critical alerts · escalation chains · reminders. **One system. No module sends its own messages.** |
| **Consumes (events)** | `CriticalResultDetected`, `IcuAlertRaised`, `AppointmentBooked`, `AppointmentReminderDue`, `DischargeApproved`, `SafetyEventDetected`, `CapaDueSoon`, `AuditGapDetected` |
| **Contract** | Async event consumption primarily; `POST /notifications/send` for direct, permissioned sends |
| **Deployment** | **Extract-9.** Score 43. Built standalone from the start. |

## V. Audit & Evidence

| | |
|---|---|
| **Owns** | `audit.audit_event`, `audit_access_log`, existing `audit_log` + `activity_log` (retained), Evidence Vault tables |
| **Responsibilities** | Immutable audit events, patient journey read model, access history, approval history, before/after values, evidence storage + verification, audit reports |
| **Consumes** | Every domain event, plus the persistence interceptor's row diffs |
| **Contract** | Read-only query API under `AUDIT.*` permissions; write path is event ingestion only — **no external actor can author or edit an audit event** |
| **Deployment** | **Core (Phases 1–4) → Extract-10.** Score 41. |

## W. Reporting & Analytics

| | |
|---|---|
| **Owns** | `reporting.report_definition`, `report_subscription`, materialized views (`mv_revenue_daily`, `mv_daily_collection`, `mv_op_census`) |
| **Gains** | Compliance/quality/audit-readiness datasets |
| **Deployment** | **Core → Extract-15**, and only behind a real data pipeline. |

---

## Cross-cutting rules

1. **A service never reads another service's tables.** Inside the monolith this is enforced by review
   and an architecture test that asserts each feature folder only references its declared entity
   namespaces. See [database-boundaries.md](database-boundaries.md) for the per-table owner map.
2. **Masters is shared reference data, not a service.** It is read by everyone and written by one
   module; treat it as a published read model, cached, never forked.
3. **Portals are façades.** `Portal` and `DoctorPortal` own almost no data and must keep delegating to
   the owning service rather than querying around it.
4. **Reporting reads events and views, never live OLTP tables of other services** (target state).
5. **Every boundary crossing carries `correlationId` and, where a patient is involved, `patientId` +
   `encounterRef`.**
