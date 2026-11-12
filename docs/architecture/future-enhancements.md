# Future Enhancement Modules — Prixo HIS

**Status:** candidate register, 2026-08-13.
**Purpose:** the 26 candidate modules from the enhancement brief, each assessed honestly against what
already exists in the codebase. **Nothing here is claimed as built.**

Status vocabulary:

| Status | Meaning |
|---|---|
| **Planned** | Scheduled in [migration-roadmap.md](migration-roadmap.md) |
| **Partial** | Substantial machinery already exists; the module is a generalization, not new build |
| **Future** | Not scheduled. Design intent recorded; needs a business trigger. |

---

## A. Patient Audit & Journey — **Planned (Phase 3)**
Timeline of every action against a patient/encounter with actor, role, approval, reason, evidence,
and gaps. Blocked on Phase 1 (audit has no patient dimension today).
→ [patient-audit-journey.md](patient-audit-journey.md)

## B. Evidence Management — **Planned (Phase 4) · Partial**
`quality.quality_evidence` exists (NABH chapter-wise uploads); four other document stores exist
(`patient_document`, `emr.attachment`, `mrd.scanned_document`, `insurance.claim_document`). The vault
is a registry + versioning + checksum + access-log + retention layer over them — **not a sixth
store**.
→ [evidence-management.md](evidence-management.md)

## C. NABH Audit Management — **Planned (Phase 5) · Partial**
Exists: `quality_nabh_standard` master, evidence library, audit templates/runs, non-conformance,
CAPA, risk, committees, indicators. Missing: versioned framework, requirement hierarchy, mapping
chain, computed evidence status, NABH Audit Mode screen. **No requirement content ships.**
→ [nabh-framework.md](nabh-framework.md)

## D. NMC / Medical Education Audit — **Framework Planned (Phase 5) · Entities Future**
Per the 2026-08-13 scope decision: build the configurable framework layer now; defer
Resident/Faculty/Rotation/Logbook/Assessment/Competency entities until there is a teaching-institution
requirement. Designing them speculatively would mean inventing regulatory structure.
→ [nmc-framework.md](nmc-framework.md)

## E. Policy & SOP Management — **Future**
Today policies are documents in the evidence library with no lifecycle. Needs: authoring → review →
approval → publication → version → acknowledgment by staff → periodic review → retirement, with each
version retained as evidence. Depends on B. Natural companion to the framework mapping chain (a
requirement maps to a *current* policy version).

## F. Incident Management — **Partial (exists)**
`nursing.incident` is already the org-wide register: report → review → close, widened with
`RootCauseAnalysis` and `IsSentinelEvent`. Gaps: no configurable incident-type taxonomy, no severity
matrix, no anonymous reporting, no automatic ingestion of portal feedback (a deliberate scope line
today). **Do not create a second incident table** — Phase 7 links safety events to this one.

## G. RCA Management — **Partial (exists)**
An RCA action and field exist on `Incident`. Gaps: structured methodology (5-why / fishbone /
contributing factors), RCA team, timeline reconstruction (which Phase 3's journey would supply
directly), and mandatory RCA for sentinel events.

## H. CAPA Management — **Partial (exists)**
`quality.quality_capa` is already polymorphic over Incident / NonConformance / Audit / Risk, with
owner, due date, verification, and notes. Gaps: `Requirement` and `AuditGap` as source types (Phases
5, 7), effectiveness review, overdue escalation via Notification, CAPA dashboard.

## I. Risk Management — **Partial (exists)**
`quality.quality_risk` implements a Likelihood × Impact register. Gaps: risk treatment plans, residual
risk after mitigation, risk owner review cycle, department heat map, linkage to safety events and
incidents.

## J. Patient Safety Management — **Planned (Phase 7)**
Unifies existing detections (allergy cross-check, lab criticals, ICU threshold rules, Morse/Braden
scores, MAR double-verification, WHO checklist, contrast consent) into `safety_event` with
acknowledgment, escalation, and promotion to incident.
→ [patient-safety-engine.md](patient-safety-engine.md)

## K. Infection Control — **Future**
Nothing exists. Would need: surveillance definitions (HAI/CAUTI/CLABSI/SSI/VAP per hospital
definition), case identification worklist from lab isolates + device days, hand-hygiene audit rounds
(the quality audit-template engine could carry these), antibiotic-use tracking (pharmacy data
exists), isolation-status flag on admission, outbreak alerts. Substantial module; strong candidate
after Phase 7 because it consumes safety, lab, and pharmacy events.

## L. Medication Safety — **Future · Partial substrate**
Exists: high-alert flag + double verification (MAR), allergy cross-check, FEFO/expiry control,
reason-coded skip/refuse. Missing: medication error reporting taxonomy, look-alike/sound-alike
register, prescribing-pattern audit, antibiotic stewardship, drug-interaction checking (**would need
a licensed clinical knowledge base — an explicit build-vs-buy decision, not a Prixo table**).

## M. Mortality Review — **Future**
Nothing exists. Would need: death registration on admission/ER disposition, mortality case worklist,
structured review (preventability, contributing factors), committee linkage, mortality indicators
(the indicator engine already exists), and evidence linkage. Small module with high audit value.

## N. Morbidity / M&M Meeting — **Future · Partial substrate**
`quality.quality_committee_meeting` + action items already model committee records. M&M needs case
selection (from mortality review and incidents), presentation records, learning points, and action
tracking — mostly a specialization of the existing committee model.

## O. Patient Grievance Management — **Future · Partial substrate**
Portal feedback lands in `patient.feedback` today but does not feed quality workflows. Needs:
grievance intake (portal, front office, written), categorization, severity, owner, SLA, resolution,
patient communication log, and escalation. The deliberate non-ingestion of feedback into
incidents/NC is a known scope line — this module closes it.

## P. Consent Management — **Partial (exists)**
`patient.patient_consent` + `master.consent_template` + `rad.contrast_consent` + OT consent
verification exist. Gaps: a consent register per procedure type, expiry/re-consent rules, withdrawal,
digital signature capture, and completeness enforcement (Phase 6 covers the "is consent present"
check).

## Q. Clinical Documentation Completeness — **Planned (Phase 6) · Partial**
`mrd.file_audit` already computes discharge-file completeness. Phase 6 generalizes it into a
configurable, per-encounter-type engine.
→ [clinical-document-completeness.md](clinical-document-completeness.md)

## R. Credentialing & Privileging — **Future**
Nothing exists beyond `master.doctor` and designations. Would need: qualification/registration
records with expiry, privilege list per specialty, granted privileges per doctor with review dates,
verification evidence, and — the valuable part — **enforcement**: warn or block when a doctor
performs a procedure outside granted privileges (would hook OT booking and order entry). High audit
value; touches clinical workflow, so needs care.

## S. Staff Training & Competency — **Future**
Nothing exists. Needs training catalogue, attendance, assessment, competency matrix per role,
expiry/refresher tracking, and evidence linkage. Feeds the "Training evidence" component of the audit
readiness score, which currently renders **"Not measured"** by design.

## T. Medical Education / Resident Logbook — **Future (deferred by decision)**
Entity sketch in [nmc-framework.md §2](nmc-framework.md#2-what-is-deferred-future-enhancement).
Deferred 2026-08-13; framework layer built instead.

## U. Quality Committee Management — **Partial (exists)**
`quality_committee`, `quality_committee_meeting`, `quality_committee_action_item` exist. Gaps:
scheduled meeting calendar, quorum/attendance, agenda from open CAPA/NC/risk, minutes as versioned
evidence, action-item escalation.

## V. Audit Evidence Vault — **Planned (Phase 4)** — same as B.

## W. Compliance Dashboard — **Planned (Phase 5)**
Framework-wise and department-wise evidence status, gaps, CAPA, readiness. Every number drillable;
unmeasurable components render "Not measured". No compliance claims.

## X. Regulatory Change Management — **Future**
The versioned `regulatory_framework` model (Phase 5) is the foundation: old requirements are retained
as `Retired`, new versions activate, and reports stamp the version used. The *management* layer —
change notification, impact analysis (which mappings/evidence are affected), transition tracking,
sign-off — is a later addition once a hospital has run one version change.

## Y. Digital Signature / Approval Service — **Future · Placeholder exists**
`LabResult.ApproverSignaturePath` and `RadReport.ApproverSignaturePath` are placeholder columns that
nothing writes. Approval *records* (who/when/reason) are already strong across ~18 workflows. A real
signature service means: signature image capture, then DSC/PKI with certificate management,
timestamping, and verifiable document sealing — a build-vs-buy decision, likely regulated. The audit
event model already carries `has_signature` so the platform is ready to record it.

## Z. Notification & Escalation Service — **Planned (Phase 9)**
`admin.notification_queue` exists with one producer and no dispatcher; no SMS/email/push exists
anywhere. Built standalone (extraction score 43) and consumed by every module through events.
→ [service-boundaries.md §U](service-boundaries.md#u-notification)

---

## Summary

| Status | Count | Modules |
|---|---|---|
| **Planned** (in roadmap) | 7 | A · B/V · C · Q · J · W · Z |
| **Partial** (generalize existing) | 8 | F · G · H · I · N · P · U · (L substrate) |
| **Framework now, entities later** | 1 | D/T |
| **Future** (needs a trigger) | 10 | E · K · L · M · O · R · S · X · Y · (D entities) |

**Two observations worth acting on:**

1. **Eight of the 26 already have substantial machinery.** The honest work there is generalization
   and linkage, not new modules — and building parallels would violate the "no duplicate systems"
   rule.
2. **The highest-audit-value *unbuilt* modules are Infection Control (K), Mortality Review (M), and
   Credentialing & Privileging (R).** All three are natural Phase 7+ additions because they consume
   the safety, evidence, and event infrastructure the roadmap builds first. None should be started
   before that foundation exists.
