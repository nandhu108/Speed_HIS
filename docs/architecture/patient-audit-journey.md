# Patient Audit Journey — Prixo HIS

**Status:** design, 2026-08-13. **Phase 3** (depends on Phase 1 audit backbone + Phase 2 encounter
abstraction).

The flagship capability of the future platform: an auditor selects a patient and a scope, and sees
the complete, evidenced journey — every action, actor, approval, document, and gap.

---

## 1. Scope selection

```
Patient (by UHID / name+mobile / MR number)
  └─ Scope:  All history  |  Encounter (OP visit)  |  Admission (IP)  |  Emergency visit
             |  Date range  |  Audit engagement (external assessor)
  └─ Filters: module · department · actor · event type · severity · exceptions only
```

Resolution uses the Phase 2 `EncounterRef` projection so one code path covers `OpVisit`,
`Admission`, and `EmergencyVisit` instead of a 3-way union in every query.

---

## 2. The journey model

The timeline merges three sources into one ordered stream:

| Source | Contributes |
|---|---|
| `audit.audit_event` | Who/when/what/why/approval/before-after for every action |
| Business rows (via `EncounterRef`) | Current state and the clinical/financial substance of each step |
| Completeness + safety engines | Expected-but-missing steps, rendered as gaps in place |

**Canonical step sequence** (steps absent for a given patient simply do not appear; steps *expected
but missing* appear in red — that is the point):

```
PATIENT REGISTERED → APPOINTMENT → OPD CONSULTATION → VITALS → DIAGNOSIS → PRESCRIPTION
→ LAB ORDER → SAMPLE COLLECTION → LAB RESULT → RESULT VERIFICATION
→ RADIOLOGY ORDER → RADIOLOGY REPORT → PHARMACY DISPENSING
→ IP ADMISSION → BED ALLOCATION → NURSING ASSESSMENT → MEDICATION ADMINISTRATION
→ DOCTOR NOTES → PROCEDURE / OT → BILLING → INSURANCE / TPA
→ DISCHARGE → DISCHARGE SUMMARY → FOLLOW-UP → PATIENT PORTAL ACCESS
```

### 2.1 Per-step payload

Every timeline entry exposes, where applicable:

| Field | Source |
|---|---|
| Timestamp, module, action | `audit_event` |
| Performed by, **role as at that time**, department, branch | `audit_event.actor_*` |
| Patient, encounter | `audit_event.patient_id` / `encounter_*` |
| Previous value → new value | `before_value` / `after_value` |
| Approval, approver, approved at | `approval_id` / `approver_user_id` |
| Reason / cancellation reason / override reason | `reason` |
| Document + digital signature status | linked evidence / signature fields |
| IP, device, session | `audit_event` context columns |
| Related order / result / bill / payment / document | linkage columns |
| Status | Completed · Pending · Exception · Informational · Approval |

---

## 3. Read model

Live-querying 20+ tables per journey would repeat EMR `TimelineService`'s in-memory-merge pattern at
a scale it was explicitly not designed for. Instead:

```
audit.patient_journey_projection            -- rebuildable read model
  id · patient_id · encounter_type · encounter_id · occurred_at
  step_key            -- canonical step (PATIENT_REGISTERED, LAB_RESULT_VERIFIED, ...)
  module · department_id · branch_id
  actor_user_id · actor_display · actor_role_codes
  title · detail
  status              -- Completed | Pending | Exception | Informational | Approval
  audit_event_id · entity_type · entity_id
  approval_id · reason
  evidence_count · has_signature
  correlation_id
```

- Populated by the event dispatcher (Phase 8) and, before that, by a projection builder that reads
  `audit_event`.
- **Fully rebuildable** from `audit_event` — a `rebuild` command ships with it, so a projection bug is
  never a data loss.
- Indexed on `(patient_id, occurred_at)` and `(encounter_type, encounter_id, occurred_at)`.

---

## 4. API

```
GET  /api/v1/audit/journey/patient/{patientId}?from=&to=&module=&exceptionsOnly=
GET  /api/v1/audit/journey/encounter/{encounterType}/{encounterId}
GET  /api/v1/audit/journey/step/{projectionId}          → full step detail + audit event + evidence
GET  /api/v1/audit/journey/gaps/{encounterType}/{encounterId}
POST /api/v1/audit/journey/report                       → PDF | Excel | CSV
```

All `[HasPermission(AUDIT.JOURNEY.VIEW)]`; report generation additionally requires
`AUDIT.REPORT.GENERATE`. **Every one of these calls writes an `audit_access_log` row** — auditors are
audited.

---

## 5. Timeline UI

New screen: **Audit → Patient Journey**. Reuses `AppShell`/`TabbedWorkspace`; introduces shared
components (`PatientHeader`, `EncounterHeader`, `AuditTimeline`, `EvidenceCard`, `ApprovalCard`,
`GapCard`) into `src/components/` so Quality, NABH, and NMC screens reuse them rather than forking.

```
┌─ UHID 000123 · Ramesh K · 47 y · M ──────────────── [OP-2026-004412 ▾] [Export ▾] ─┐
│ Journey  ·  Gaps (3)  ·  Evidence (11)  ·  Access history  ·  Completeness 92%     │
├───────────────────────────────────────────────────────────────────────────────────┤
│ 09:02  ● Patient Registration    Reception · A. Nair (Front Office)          ✓     │
│        UHID created · consent captured                        [evidence 2] [audit] │
│ 09:18  ● Appointment Check-in    Front Office · A. Nair                      ✓     │
│        Token OP-14 generated                                             [audit]   │
│ 09:42  ● OPD Consultation        Dr. S. Menon (Physician)                    ✓     │
│        Diagnosis recorded — E11.9                                        [audit]   │
│ 09:51  ● Prescription            Dr. S. Menon                                ✓     │
│        4 items · signed                                       [document] [audit]   │
│ 10:05  ● Lab Order               Dr. S. Menon                                ✓     │
│        CBC, HbA1c                                                        [audit]   │
│ 10:21  ● Sample Collection       Laboratory · J. Thomas                      ✓     │
│ 11:02  ● Lab Result Entered      Laboratory · R. Das                         ✓     │
│ 11:10  ● Result Verified         Dr. P. Iyer (Pathologist)                   ✓     │
│ 11:14  ▲ Critical Result         HbA1c 13.2 — alert raised                   ⚠     │
│        Acknowledged 11:31 by Dr. S. Menon                                [audit]   │
│ 12:15  ● Pharmacy Dispensing     Pharmacist · K. Suresh                      ✓     │
│ 12:40  ◆ Discount Approval       Approved by Billing Manager · ₹450          ⬥     │
│        Reason: senior citizen concession                      [approval] [audit]   │
│ ──  ✕ MISSING · Nursing assessment not recorded within 24 h of admission     ✕     │
│        Severity HIGH · Assigned: Nursing Administration       [gap] [raise CAPA]    │
└───────────────────────────────────────────────────────────────────────────────────┘
```

**Status colours** (accessible pairings — colour is never the only signal; each carries an icon and a
text label):

| Colour | Meaning | Icon |
|---|---|---|
| **Green** | Completed | ✓ |
| **Yellow** | Pending / in progress | ⏳ |
| **Red** | Exception · missing · rejected | ✕ |
| **Blue** | Informational | ● |
| **Purple** | Approval / authorization | ⬥ |

Each row expands to the full audit detail (before/after, IP, device, session, reason, approver) and
links to related order/result/bill/document.

---

## 6. Audit gap detection

Gaps are computed, never hand-maintained. Two sources:

1. **Documentation completeness engine** — required-document sets per encounter type
   ([clinical-document-completeness.md](clinical-document-completeness.md)).
2. **Rule engine** — configurable hospital policy rules
   ([patient-safety-engine.md](patient-safety-engine.md#5-rule-engine)).

```
quality.audit_gap
  id · branch_id · patient_id · encounter_type · encounter_id
  gap_type          -- MissingConsent | MissingSignature | MissingNursingAssessment |
                    -- MissingMedicationAdministration | MissingDischargeApproval |
                    -- UnverifiedLabResult | UnapprovedRadiologyReport | MissingConsultantNote |
                    -- MissingPreOpChecklist | MissingPostOpNote | MissingBillingClearance |
                    -- MissingPharmacyClearance | MissingInvestigationClearance | ...
  rule_id · severity(Low|Medium|High|Critical)
  department_id · required_evidence · description
  detected_at · detected_by(System|User) · status(Open|InProgress|Resolved|Waived)
  assigned_to_role · assigned_to_user_id
  resolved_at · resolution_notes · waiver_reason · waived_by_user_id
  capa_id           -- links to the EXISTING quality.quality_capa
```

Presented exactly as the brief specifies:

```
AUDIT GAP DETECTED
Patient:   UHID-000123          Encounter: IP-2026-000412
Issue:     Discharge summary exists but final consultant approval is missing.
Severity:  HIGH                 Department: IPD
Required evidence: Consultant approval
Status:    OPEN                 Assigned to: Medical Administration
```

Gap types are **configuration**, not code: each is a rule row with a scope, a condition, a severity,
and an owning department. Publishing `AuditGapDetected` lets Notification escalate and Quality raise
a CAPA against the existing polymorphic `QualityCapa.SourceType/SourceId`.

---

## 7. Audit report

`POST /api/v1/audit/journey/report` produces a single document via the existing QuestPDF /
ClosedXML / CSV export path (`IReportExportService`, `ReportResultDto`):

```
Hospital · Branch · Audit type · Audit period · Auditor · Generated at
Regulatory framework + VERSION used (when the audit is framework-scoped)
Patient · UHID · Encounter(s)
──────────────────────────────────────────────
1. Journey timeline (chronological, with actors, roles, approvals)
2. Evidence summary          (available, verified, version, hash)
3. Missing evidence
4. Documentation gaps        (with severity + owner)
5. Approvals                 (what, who, when, reason)
6. Exceptions                (cancellations, overrides, corrections, break-glass)
7. Incidents                 (from the existing nursing.incident register)
8. CAPA                      (open/closed, from quality_capa)
9. Risk                      (from quality_risk)
10. Access history           (who read this record during the audit period)
11. Final audit status       (auditor-entered narrative + system-computed counts)
```

Formats: **PDF** (evidence pack), **Excel** (working papers), **CSV** (data extract). Generation is
itself an audited action; the report records its own `correlationId` so a produced report can be
traced back to the exact query that made it.

---

## 8. What this design deliberately does not do

- **Does not create a second timeline.** EMR's clinical `TimelineService` stays the clinician-facing
  view; the audit journey is the evidence-facing view and reads the audit backbone.
- **Does not let auditors change anything.** No write endpoint exists in the `AUDIT.*` namespace;
  raising a CAPA from a gap is a `QUALITY.*` action performed by quality staff, not the auditor.
- **Does not infer clinical judgment.** A gap means "the record does not contain X", never "care was
  inadequate".
- **Does not fabricate history.** Events before Phase 1 lack the new dimensions; the journey renders
  them from `audit_log`/`activity_log` with an explicit "limited detail (pre-upgrade)" marker rather
  than silently implying full fidelity.
