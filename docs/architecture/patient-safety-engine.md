# Patient Safety Engine — Prixo HIS

**Status:** design, 2026-08-13. **Phase 7** (depends on Phases 1, 2, 6 and the event backbone).

A configurable detection layer that turns clinical events into **safety events** with severity,
ownership, acknowledgment, and escalation — reusing the detections Prixo already performs instead of
re-implementing them.

---

## 1. What already exists (and is reused, not rebuilt)

| Existing detection | Where | Reuse |
|---|---|---|
| Allergy cross-check on OPD prescription | EMR allergy banner + live prescription warning | Emits `SafetyEventDetected(MedicationAllergyConflict)` instead of only warning on screen |
| Critical lab value flagging + alert + acknowledgment | Lab (age/sex-aware H/L/critical, `critical_alert`) | Becomes a safety event source; unacknowledged alerts age into escalations |
| ICU threshold rules on every vitals reading | `icu.critical_alert_rule` → `icu_vitals_alert` | **The prototype for the rule engine** — generalize its shape branch-wide |
| Fall risk (Morse) / bed-sore (Braden) / pain scores with auto-banded results | `nursing.risk_assessment` | High-risk bands raise safety events |
| High-alert medication double verification | Nursing MAR | Missing double-verification becomes a detectable condition |
| WHO surgical safety checklist gating | OT (blocks status transitions) | Missing/incomplete phases feed gaps and safety events |
| Contrast consent gate | Radiology technician worklist | Missing consent becomes a detectable condition |
| Incident reporting → review → close, with RCA + sentinel flag | `nursing.incident` | **The** incident register; safety events can be promoted to incidents, never duplicated into a parallel table |

The engine's job is **unification and escalation**, not new clinical logic.

---

## 2. Model

```
quality.safety_event
  id · branch_id
  event_code                -- sequenced reference (SE-2026-000123)
  patient_id · encounter_type · encounter_id · department_id
  event_type                -- see §3
  severity                  -- Low | Medium | High | Critical
  detected_at
  detected_by               -- System | User
  detected_by_user_id · rule_id
  source_event_id           -- the audit event / domain event that triggered detection
  entity_type · entity_id   -- the clinical object concerned
  description               -- rule-generated, no free-text PHI beyond identifiers

  status                    -- Detected | Acknowledged | ActionTaken | Escalated | Closed | FalsePositive
  acknowledged_by_user_id · acknowledged_at
  action_taken · action_by_user_id · action_at
  escalation_level · escalated_at · escalated_to_role_code
  closed_by_user_id · closed_at · closure_notes

  incident_id               -- → nursing.incident, when promoted
  capa_id                   -- → quality.quality_capa
  correlation_id
```

Safety events are **not** clinical records and never alter clinical data. They are detections about
records.

---

## 3. Detection catalogue

Every one is a **configured rule**, not hard-coded logic. Shipped rule *templates* are disabled by
default; the hospital enables and parameterizes them.

| Detection | Primary trigger | Default severity |
|---|---|---|
| Medication–allergy conflict | `PrescriptionCreated`, `MedicationOrdered` × EMR allergies | Critical |
| High-alert medication ordered/administered without double verification | `MedicationOrdered`, `MedicationAdministered` | High |
| Duplicate medication (same generic, overlapping window) | `PrescriptionCreated`, `MedicationOrdered` | Medium |
| Critical laboratory result | `CriticalResultDetected` | Critical |
| **Delayed critical-result acknowledgement** | elapsed time since `CriticalResultDetected` | Critical |
| Abnormal vital beyond configured band | `VitalsRecorded`, `IcuVitalsRecorded` | High |
| High fall risk without documented precaution | `RiskScoreRecorded` + completeness check | High |
| High pressure-injury (Braden) risk without care plan | `RiskScoreRecorded` | High |
| Missing consent before procedure/surgery/contrast | `SurgeryScheduled`, `ImagesCaptured` + consent check | Critical |
| Missing/incomplete surgical safety checklist phase | `SurgeryStarted`, `SurgeryCompleted` | Critical |
| Delayed result (TAT breach) | Lab/Rad order age vs configured TAT | Medium |
| Missed MAR dose | scheduled dose past window without administration/skip/refusal | High |
| Discharge documentation gap | completeness engine at `DischargeRequested` | High |
| Missing approval (discharge summary, discount, refund, stock adjustment) | approval-gated flows | High |
| Unverified lab result / unapproved radiology report at discharge | discharge-time sweep | High |

---

## 4. Lifecycle

```
DETECTED ──(rule fires, event published)──▶ notify owning role
   │
   ├─ ACKNOWLEDGED   (who, when — always recorded)
   │      │
   │      ├─ ACTION TAKEN  (what was done)
   │      │        └─▶ CLOSED  (with closure notes)
   │      └─ FALSE POSITIVE (with reason; feeds rule tuning)
   │
   └─ not acknowledged within the configured window
          └─▶ ESCALATED  (level 1 → 2 → 3, role-based, each escalation is an event)
                  └─▶ may be PROMOTED to nursing.incident (existing register)
                          └─▶ RCA → CAPA (existing quality tables)
```

No safety event closes itself. No escalation is silent — every transition is an audit event and a
notification.

---

## 5. Rule engine

Shared with the completeness engine and the audit-gap engine — **one evaluator, three consumers**.

```
quality.safety_rule
  id · branch_id · code · name · description
  rule_type            -- Safety | Completeness | AuditGap
  trigger_event_types  -- text[] of domain event types
  scope                -- jsonb: encounter type, ward type, department, patient category, age band
  condition            -- jsonb expression tree over a whitelisted, typed field set
  time_window          -- interval, for "within X" and "not within X" rules
  severity · owner_role_code · owner_department_id
  escalation_policy_id
  is_enabled · effective_from · effective_to
  created_by · approved_by     -- rule changes are approval-gated and audited
```

Condition expressions are **declarative and whitelisted** — no scripting, no dynamic code execution.
The evaluator supports comparison, range, existence, count, and elapsed-time predicates over a typed
field catalogue exposed per trigger event. Rule changes are versioned and audited: a safety event
records the `rule_id` **and rule version** that produced it, so historical detections remain
explicable after a rule is edited.

Example rule shapes (the brief's examples, expressed as configuration):

| Policy | Rule |
|---|---|
| "If a patient is admitted, a nursing assessment is required within a configured duration" | trigger `AdmissionCreated`, condition `NOT exists(NursingAssessment)`, window `24h`, severity High |
| "If a high-alert medication is ordered, double verification is required" | trigger `MedicationOrdered`, condition `drug.isHighAlert AND NOT doubleVerified`, severity High |
| "If a critical lab result is approved, notification is required" | trigger `CriticalResultDetected` → Notification consumer |
| "If discharge is requested, the clearance workflow must be complete" | trigger `DischargeRequested`, condition `clearance.pending > 0`, severity High |
| "If a requirement needs consent, check evidence" | trigger `SurgeryScheduled`, condition `NOT evidenceLinked(Consent)`, severity Critical |

---

## 6. Surfaces

| Surface | Content |
|---|---|
| **Patient Safety Dashboard** | Open events by severity/department, mean acknowledgment time, escalations, top rules firing, false-positive rate |
| Clinical workspace / ward board | Inline safety banner for the current patient (never modal-blocking care) |
| Doctor Portal | Unacknowledged critical alerts (already a dashboard tile — extended to safety events) |
| Nursing station | Missed doses, risk-without-precaution, handover-relevant events |
| Quality module | Promotion to incident, RCA, CAPA |
| Patient Audit Journey | Safety events inline on the timeline |

---

## 7. Constraints

- **Advisory, never obstructive.** The engine warns, records, and escalates; it does not block
  clinical action. Existing hard gates (WHO checklist, contrast consent) remain where they are.
- **No clinical decision support claims.** These are policy and documentation detections, not
  diagnostic or therapeutic recommendations. No dosing advice, no interaction database, no scoring of
  clinician judgment.
- **Tunable, or it will be ignored.** False-positive tracking is first-class; a rule with a high
  false-positive rate is surfaced to the quality team for retuning.
- **No duplicate incident register.** Promotion writes to `nursing.incident`; the safety event links
  to it.
- **No PHI in notifications.** Alerts carry patient identifiers and a reason code; the clinician
  opens the record in Prixo to see detail — and that read is access-logged.

---

## 8. Permissions

```
QUALITY.SAFETYRULE.VIEW / .MANAGE / .APPROVE
QUALITY.SAFETYEVENT.VIEW / .ACKNOWLEDGE / .ACTION / .CLOSE / .PROMOTE
QUALITY.SAFETY.DASHBOARD.VIEW
AUDIT.SAFETY.VIEW        -- read-only auditor personas
```
