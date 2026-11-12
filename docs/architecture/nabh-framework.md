# NABH Framework (Regulatory Evidence Architecture) — Prixo HIS

**Status:** design, 2026-08-13. **Phase 5.**

---

## 0. What this is, and what it is not

**This is an evidence-management system, not a compliance certification.**

Software does not make a hospital NABH-accredited. What software can do is let a hospital map its own
applicable requirements to its own policies, processes, HIS workflows, and patient evidence — and
show, at any moment, what evidence exists and what is missing.

**Binding language rules** (enforced in code review, UI copy, and reports):

| Never write | Write instead |
|---|---|
| "NABH compliant" / "NABH certified" | "NABH-aligned workflow" |
| "Meets standard X" | "Evidence available" / "Evidence gap" |
| "Compliance score 92%" | "Evidence available for 92% of mapped requirements" |
| Any requirement text shipped in the product | "Hospital-defined requirement" loaded by the hospital |

**No NABH requirement content ships with Prixo.** The framework ships empty. The hospital loads the
edition and requirements it is actually assessed against, and every audit report records the
framework name and **version** used.

---

## 1. Reuse first

The `quality` schema already contains most of the machinery:

| Existing | Role in this design |
|---|---|
| `quality.quality_nabh_standard` | Chapter/standard reference master (`Code = CHAPTER_STANDARD`) — becomes the **seed source** for requirements under a hospital-loaded framework version |
| `quality.quality_evidence` | Generalized into the Evidence Vault ([evidence-management.md](evidence-management.md)) |
| `quality.quality_capa` | Already polymorphic (Incident / NonConformance / Audit / Risk) — gains `Requirement` as a source type |
| `quality.quality_non_conformance` | Findings against a standard — gains a requirement link |
| `quality.quality_risk` | Risk register, already Likelihood × Impact |
| `quality.quality_audit*` | Checklist templates + scored runs |
| `quality.quality_indicator*` | Monthly numerator/denominator with auto-compute |
| `nursing.incident` | **The** org-wide incident register (already carries RCA + sentinel flags) |
| `quality.quality_committee*` | Committee meetings and action items |

Nothing above is duplicated. The new tables are the *framework* layer that sits above them.

---

## 2. Regulatory framework model (versioned)

```
quality.regulatory_framework
  id · branch_id(nullable = org-wide)
  code                    -- NABH | NMC | HOSPITAL | <hospital-defined>
  name                    -- as entered by the hospital
  version                 -- ● hospital-entered, free text (e.g. "5th Edition")
  effective_from · effective_to
  status                  -- Draft | Active | Superseded | Withdrawn
  source_reference        -- ● where the hospital obtained it (document/URL/citation)
  notes
  activated_by_user_id · activated_at

quality.regulatory_requirement
  id · framework_id
  requirement_code        -- hospital-entered (e.g. chapter/standard/objective element ref)
  parent_requirement_id   -- hierarchy: chapter → standard → objective element
  title · description     -- ● hospital-entered; never shipped
  applicable_department_ids  -- bigint[]
  evidence_type_required  -- Document | Record | PatientEvidence | Policy | Training |
                          -- AuditRun | Indicator | Process | Observation
  evidence_required_count · is_mandatory
  responsible_role_code · reviewer_role_code
  risk_level              -- Low | Medium | High
  review_frequency        -- Monthly | Quarterly | HalfYearly | Annual | AdHoc
  status                  -- Active | Retired
  supersedes_requirement_id   -- history preserved across versions
```

**Versioning rule:** activating a new framework version never edits the old one. Old requirements
remain `Retired` but queryable, and every audit report states which framework version it was run
against. `FrameworkVersionActivated` is a domain event.

---

## 3. The mapping chain

The brief's chain, expressed as data:

```
NABH requirement → hospital policy → hospital process → HIS workflow → patient evidence
→ document evidence → audit event → responsible department → corrective action → CAPA
→ closure evidence
```

```
quality.requirement_mapping
  id · requirement_id · branch_id
  policy_evidence_id      -- → quality.evidence (the hospital's policy/SOP document)
  process_description     -- hospital's own words
  his_workflow_ref        -- structured pointer to what the HIS already does:
                          --   module + entity + action  (e.g. IPD / DischargeSummary / Approved)
  evidence_source         -- Manual | HisAutomatic | Hybrid
  audit_event_type        -- the domain event that constitutes automatic evidence
  documentation_requirement_id  -- → completeness engine rule, when applicable
  responsible_department_id · owner_user_id · reviewer_user_id
  status                  -- Mapped | PartiallyMapped | Unmapped
  last_reviewed_at · next_review_due
```

**The important consequence:** where `evidence_source = HisAutomatic`, evidence is *computed* from
the audit backbone rather than uploaded. Example mapping shapes (illustrative, hospital-authored):

| Hospital-defined requirement (example) | HIS workflow ref | Automatic evidence |
|---|---|---|
| "Every admitted patient has a nursing assessment within the defined window" | Nursing / NursingAssessment / Recorded | Completeness rule + `NursingAssessmentRecorded` events |
| "Critical results are communicated and acknowledged" | Lab / CriticalAlert / Acknowledged | `CriticalResultDetected` → `CriticalResultAcknowledged` pairs with elapsed time |
| "Surgical safety checklist completed for every surgery" | OT / SurgicalSafetyChecklist / PhaseCompleted | Existing checklist gating + events |
| "Discharge summary approved by the treating consultant" | IPD / DischargeSummary / Approved | `DischargeSummaryApproved` events + gap detection |

These are **examples of mapping shape**, not shipped requirement content.

---

## 4. Evidence status per requirement

Computed, never hand-set:

```
quality.requirement_evidence_status        -- materialized per (requirement, branch, period)
  requirement_id · branch_id · period_start · period_end
  evidence_required · evidence_available · evidence_missing
  automatic_evidence_count · manual_evidence_count
  last_evidence_at
  status          -- EvidenceAvailable | PartialEvidence | EvidenceGap | NotApplicable
  gap_count · open_capa_count
  computed_at
```

Status vocabulary is deliberately evidential: **Evidence available · Partial evidence · Evidence
gap · Not applicable**. Never "compliant".

---

## 5. NABH Audit Mode

New screen: **Quality → Audit → NABH Audit Mode**.

```
┌ Framework: [Hospital-loaded framework ▾]  Version: 5th Edition  Period: Q2 2026 ─┐
│ Chapter view │ Requirement view │ Evidence gaps │ CAPA │ Readiness │ Report      │
├──────────────────────────────────────────────────────────────────────────────────┤
│ CHAPTER (hospital-entered)          Requirements  Evidence   Gaps   CAPA  Status  │
│ ▸ AAC                                     24         21        3      2   Partial │
│ ▸ COP                                     31         31        0      0   Available│
│ ▸ MOM                                     18         12        6      4   Gap     │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Requirement AAC_1.3  (hospital-entered title)                                     │
│  Mapped policy: QP-014 v3        Process: (hospital description)                  │
│  HIS workflow: IPD / DischargeSummary / Approved      Evidence source: Automatic   │
│  Evidence: 148 of 152 encounters   ✕ 4 encounters lack consultant approval         │
│  Owner: Medical Administration    Reviewer: Quality Manager                        │
│  Risk: High    Next review: 2026-09-30    Open CAPA: 1                             │
│  [View patient evidence]  [Raise CAPA]  [Attach document]  [Audit history]         │
└──────────────────────────────────────────────────────────────────────────────────┘
```

Every requirement drills through to the **actual patients/encounters** constituting or missing the
evidence — which is where this stops being a document tracker and becomes an evidence platform.

---

## 6. Audit readiness score

Computed only from real rows. **No defaults, no seeded percentages, no estimation.**

```
Overall readiness = weighted mean of the components, weights hospital-configurable
  Clinical documentation completeness  = complete required documents / required documents
  Quality evidence                     = requirements with evidence / mapped requirements
  Patient safety                       = 1 − (open high/critical safety events / encounters)
  CAPA closure                         = CAPAs closed on time / CAPAs due
  Policy evidence                      = requirements with a current mapped policy / requirements
  Training evidence                    = (only when the training module exists — otherwise "Not measured")
```

Rules:
- Any component without a data source renders **"Not measured"** — never 0%, never a placeholder.
- Every percentage is drillable to the underlying rows.
- Scores are stamped with the framework version and computation timestamp.
- No score is displayed as a pass/fail threshold or as accreditation likelihood.

---

## 7. Permissions

```
QUALITY.FRAMEWORK.VIEW / .MANAGE          -- load, version, activate frameworks
QUALITY.REQUIREMENT.VIEW / .MANAGE        -- requirement CRUD within a framework
QUALITY.REQUIREMENT.MAP                   -- create/maintain mappings
QUALITY.EVIDENCE.VIEW / .UPLOAD / .VERIFY / .WITHDRAW
QUALITY.READINESS.VIEW
AUDIT.READINESS.VIEW / AUDIT.EVIDENCE.VIEW / AUDIT.GAP.VIEW   -- read-only auditor personas
```

Auditors (internal, external, management) get the `AUDIT.*` read-only set. Only quality staff hold
the `QUALITY.*` write verbs.

---

## 8. Explicit prohibitions

- Do not ship NABH requirement text, chapter definitions, objective elements, or scoring rules.
- Do not hard-code a NABH edition or version anywhere in business logic.
- Do not display or export any statement asserting compliance, accreditation, or certification.
- Do not infer that missing evidence means non-compliance — the system reports **evidence state**.
- Do not compute a readiness score from any data the hospital has not actually entered.
- Every audit report must name the framework and version it was produced against.
