# Clinical Documentation Completeness Engine — Prixo HIS

**Status:** design, 2026-08-13. **Phase 6.**

Answers, for any encounter: *which documents are required, which exist, which are missing, and what
is the completeness percentage* — computed from real rows, never estimated.

---

## 1. Reuse first

Two working completeness mechanisms already exist and are generalized rather than replaced:

| Existing | What it does | Role here |
|---|---|---|
| `mrd.file_audit` | Discharge file checklist (summary signed / consents / notes / reports present) with a **computed completeness flag** | The prototype. Its checks become configured rules; the MRD screen keeps working. |
| `ipd.discharge_clearance` | 4-way clearance (billing / pharmacy / lab-rad / nursing) gating discharge | Stays as the operational gate; the engine reads it as evidence rather than duplicating it. |

Also reused as *sources of truth* (never re-implemented): `ot.surgical_safety_checklist`,
`nursing.nursing_assessment`, `nursing.mar_administration`, `ipd.progress_note`,
`ipd.discharge_summary.is_approved`, `patient.patient_consent`, `lab.lab_result` approval state,
`rad.rad_report` approval state.

---

## 2. Model

```
quality.documentation_requirement            -- a required-document SET, per scope
  id · branch_id
  code · name · description
  encounter_type            -- OpVisit | Admission | EmergencyVisit | Surgery | IcuStay(=Admission on ICU ward)
  applies_when              -- jsonb predicate: department, ward type, admission type,
                            -- patient category, LOS threshold, procedure performed, ...
  framework_requirement_id  -- optional link to a regulatory requirement
  is_active · effective_from · effective_to

quality.documentation_requirement_item       -- the individual required documents
  id · documentation_requirement_id
  item_code                 -- ADMISSION_ASSESSMENT | DOCTOR_ASSESSMENT | NURSING_ASSESSMENT |
                            -- VITALS | MEDICATION_ORDERS | MAR | INVESTIGATION_REPORTS |
                            -- PROGRESS_NOTES | PROCEDURE_NOTES | CONSENT | PRE_OP_CHECKLIST |
                            -- POST_OP_NOTE | DISCHARGE_SUMMARY | CONSULTANT_APPROVAL | ...
  title · description
  source_kind               -- EntityExists | EntityApproved | EntityCount | EvidenceLinked | Manual
  source_module · source_entity · source_predicate(jsonb)
  minimum_count             -- e.g. progress notes ≥ 1 per day of stay
  due_within                -- interval from encounter start (e.g. nursing assessment ≤ 24 h)
  weight                    -- contribution to the percentage (default 1)
  is_mandatory              -- mandatory items gate "complete"; optional items only affect %
  severity_if_missing       -- Low | Medium | High | Critical
  responsible_role_code · responsible_department_id
```

Nothing here is hard-coded: **every required document is a configured row**, so one hospital's
requirements never become another's.

---

## 3. Evaluation

Computed **on read**, cached per encounter, recomputed on relevant events — matching the codebase's
existing "computed on read" idiom (MRD overdue detection, AMC renewal-due, Biomed PM due).

```
for each requirement set matching (encounter_type, applies_when):
    for each item:
        satisfied = evaluate(source_kind, source_module/entity/predicate, minimum_count)
        overdue   = due_within is set AND now > encounter_start + due_within AND NOT satisfied

completeness % = Σ weight(satisfied) / Σ weight(all items)   → rounded, never rounded up to 100
status         = Complete            (all mandatory satisfied)
               | Incomplete          (mandatory outstanding, none overdue)
               | Overdue              (mandatory outstanding past due_within)
```

**Trigger events** (Phase 8): `EncounterCreated`, `AdmissionCreated`, `NursingAssessmentRecorded`,
`ProgressNoteRecorded`, `MedicationAdministered`, `LabResultApproved`, `RadiologyReportApproved`,
`ChecklistPhaseCompleted`, `OperativeNoteRecorded`, `DischargeSummaryApproved`, `DischargeCompleted`,
plus a daily sweep for `due_within` expiry on open encounters.

Each unsatisfied **mandatory** item past its due window produces an `audit_gap` row and an
`AuditGapDetected` event ([patient-audit-journey.md §6](patient-audit-journey.md#6-audit-gap-detection)).

---

## 4. Worked example — an IP admission

Illustrative configuration (a hospital would author its own):

| Item | Source | Due within | Mandatory | Severity |
|---|---|---|---|---|
| Admission assessment | `EntityExists` — IPD admission record | 4 h | ✓ | High |
| Doctor assessment | `EntityExists` — first progress note by consultant | 24 h | ✓ | High |
| Nursing assessment | `EntityExists` — `nursing.nursing_assessment` | 24 h | ✓ | High |
| Vitals | `EntityCount ≥ 2/day` — `ipd.ip_vitals` | rolling | ✓ | Medium |
| Medication orders | `EntityExists` — `ipd.medication_order` | — | ✓ | Medium |
| MAR | `EntityCount` — `nursing.mar_administration` ≥ scheduled doses | rolling | ✓ | High |
| Investigation reports | `EntityApproved` — all lab/rad orders approved | before discharge | ✓ | Medium |
| Progress notes | `EntityCount ≥ 1/day` — `ipd.progress_note` | rolling | ✓ | Medium |
| Procedure notes | `EntityExists` if a procedure was performed | 24 h after procedure | conditional | High |
| Consent | `EvidenceLinked` — `patient.patient_consent` | before procedure | ✓ | Critical |
| Discharge summary | `EntityExists` — `ipd.discharge_summary` | at discharge | ✓ | High |
| Consultant approval | `EntityApproved` — `discharge_summary.is_approved` | at discharge | ✓ | Critical |

```
DOCUMENTATION COMPLETENESS — IP-2026-000412            92%
Missing:
  ✕ Consultant approval on discharge summary     CRITICAL   IPD / Medical Administration
  ✕ Progress note for 2026-08-11                 MEDIUM     IPD / Dr. S. Menon
```

---

## 5. Surfaces

| Surface | Shows |
|---|---|
| IPD admission detail | A "Documentation" panel with % and the missing list, for the ward team to fix *before* discharge |
| Discharge workflow | Completeness state alongside the existing 4-way clearance — advisory by default, blocking only if the hospital configures it so |
| Patient Audit Journey | Missing items rendered inline as red steps |
| MRD | Generalizes the existing incomplete-records report |
| Quality / NABH | Feeds "Clinical documentation" in the audit readiness score |
| Clinical Documentation Dashboard | Department-wise and doctor-wise completeness, trend over time |

---

## 6. Design constraints

- **Never blocks care by default.** Completeness is advisory; blocking behaviour (e.g. preventing
  final discharge) is opt-in per requirement set, configured by the hospital.
- **Never invents requirements.** An empty configuration means no completeness measurement, and the
  UI says "Not configured" rather than showing 0% or 100%.
- **Never re-implements a check that an owning module already performs.** It reads
  `discharge_clearance`, `surgical_safety_checklist`, and approval flags; it does not duplicate them.
- **Percentages are drillable.** Every number links to the satisfied/unsatisfied item list and from
  there to the underlying rows.
- **Doctor-attributed gaps are handled carefully.** Completeness is a documentation measure, not a
  clinical performance judgment; doctor-wise views are permissioned separately
  (`QUALITY.COMPLETENESS.BYDOCTOR.VIEW`).

---

## 7. Permissions

```
QUALITY.DOCREQUIREMENT.VIEW / .MANAGE        -- configure requirement sets
QUALITY.COMPLETENESS.VIEW                    -- see completeness for an encounter
QUALITY.COMPLETENESS.BYDOCTOR.VIEW           -- doctor-attributed views
AUDIT.COMPLETENESS.VIEW                      -- read-only auditor personas
```
