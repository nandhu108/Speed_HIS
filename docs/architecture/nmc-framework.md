# NMC / Medical Education Framework — Prixo HIS

**Status:** design, 2026-08-13. **Phase 5 (framework layer only).**
**Scope decision (user, 2026-08-13):** build the **configurable framework layer** now; the medical
education entities (Resident, Faculty, Rotation, Duty roster, Logbook, Assessment, Competency) are
recorded as a **future enhancement**, not built in this roadmap — see
[future-enhancements.md](future-enhancements.md#t-medical-education--resident-logbook).

---

## 0. Position

Regulatory requirements for medical education differ by institution, by course, and by regulatory
version, and they change. Prixo therefore ships **no NMC requirement content** — exactly the same
posture as [nabh-framework.md](nabh-framework.md).

What Prixo provides is a framework in which an institution maps *its own applicable requirements* to
its own departments, people, activities, and evidence.

**Language rules are identical to NABH:** "NMC-aligned workflow", "evidence available", "evidence
gap", "institution-defined requirement". Never "NMC compliant".

---

## 1. What is built now (Phase 5)

The regulatory framework model from [nabh-framework.md §2](nabh-framework.md#2-regulatory-framework-model-versioned)
is **framework-agnostic**. `regulatory_framework.code = 'NMC'` is a first-class case, not a special
one:

- `regulatory_framework` — code `NMC`, institution-entered name/version/effective dates/source
- `regulatory_requirement` — institution-entered hierarchy, applicable departments, evidence type,
  responsible/reviewer roles, review frequency, versioning with `supersedes_requirement_id`
- `requirement_mapping` — policy → process → HIS workflow → evidence source → owner/reviewer
- `requirement_evidence_status` — computed evidence availability per requirement and period
- Evidence Vault linkage — any document, record, or HIS-generated artefact as evidence
- CAPA linkage — `quality_capa` gains `Requirement` as a source type
- Audit trail — every mapping, evidence upload, verification, and status change is an audit event

**This alone lets an institution run an NMC evidence exercise today**, with manual and
document-based evidence, before any education-specific entity exists.

### 1.1 Evidence sources already available in Prixo

An institution can map NMC-type requirements to data the HIS *already* produces, without new tables:

| Evidence need (institution-defined) | Existing Prixo source |
|---|---|
| Clinical exposure / case load by department | OPD visits, IPD admissions, ER visits by department and doctor |
| Procedures performed | `ipd.procedure_note`, `ot.operative_note`, OT register report |
| Case records / documentation | EMR timeline, discharge summaries, MRD coding worklist |
| Supervision and approval | Discharge summary approval, lab/radiology verification chains, doctor opinion entries |
| Departmental activity | Doctor-appointment report, OP/IP census, doctor-wise revenue and volume reports |
| Committee/teaching activity | `quality.quality_committee_meeting` + action items |
| Incident learning | `nursing.incident` + RCA, CAPA |

The mapping layer points at these; the audit backbone evidences them.

---

## 2. What is deferred (future enhancement)

The person- and training-centric model:

```
Resident · Faculty · Student
   ↓
Department → Rotation → Duty roster
   ↓
Patient / Case → Clinical activity → Procedure
   ↓
Supervisor → Assessment → Logbook entry → Approval → Audit evidence
```

Deferred entities (design sketch only, **not scheduled**):

```
education.trainee            -- links to admin.user + designation; course, batch, year
education.faculty            -- links to master.doctor / admin.user; supervisory scope
education.rotation           -- trainee × department × period
education.duty_roster        -- trainee × shift × unit (would reuse the Unit Scheduling idiom)
education.logbook_entry      -- trainee × patient/encounter × activity type × date
                             -- (patient linkage via EncounterRef, subject to the same
                             --  branch + privacy rules as any clinical read)
education.assessment         -- trainee × competency × assessor × score × date
education.competency         -- institution-defined competency master
education.attendance         -- trainee × date × session
```

**Why deferred:** these entities model an institution's academic operations, which the current
deployment context does not require. Building them speculatively would mean inventing regulatory
structure — precisely what the execution rules forbid. The framework layer is the honest,
non-speculative half, and it is genuinely useful on its own.

---

## 3. NMC Evidence Dashboard (framework-level, Phase 5)

```
┌ Framework: NMC (institution-loaded)  Version: <entered>  Period: 2026-27 ────────┐
│ Requirements │ Evidence │ Gaps │ Departments │ Readiness │ Report                │
├─────────────────────────────────────────────────────────────────────────────────┤
│ Department            Requirements  Evidence available  Missing  Pending review  │
│ General Medicine            18              14             4            2        │
│ Surgery                     16              16             0            0        │
│ Paediatrics                 14               9             5            3        │
├─────────────────────────────────────────────────────────────────────────────────┤
│ Requirement <code> (institution-entered title)                                   │
│  Department: General Medicine   Evidence type: Record   Owner: HOD               │
│  Evidence: 12 of 16 items      Reviewer: Dean's office   Status: Partial         │
│  [View evidence]  [Attach]  [Raise CAPA]  [Audit history]                        │
└─────────────────────────────────────────────────────────────────────────────────┘
```

Dashboard sections per the brief — required evidence, available evidence, missing evidence, pending
approval, faculty verification, resident verification, department status, audit readiness — with the
last four rendered **"Not measured"** until the education entities exist. No fabricated numbers.

---

## 4. Permissions

Reuses the framework namespace; no separate NMC permission tree:

```
QUALITY.FRAMEWORK.VIEW / .MANAGE
QUALITY.REQUIREMENT.VIEW / .MANAGE / .MAP
QUALITY.EVIDENCE.VIEW / .UPLOAD / .VERIFY
QUALITY.READINESS.VIEW
AUDIT.*   -- read-only auditor personas, unchanged
```

When the education module is eventually built it introduces `EDUCATION.*` codes; nothing in this
phase pre-empts them.

---

## 5. Explicit prohibitions

- No NMC requirement text, curriculum content, competency lists, or numeric quotas ship with Prixo.
- No hard-coded regulatory version anywhere in business logic.
- No claim of NMC compliance, recognition, or approval in code, UI, or reports.
- No inference of academic adequacy from evidence counts.
- Every report names the framework and version it was produced against.
- Trainee-linked patient data, when the education module lands, is subject to the same branch
  scoping, access logging, and privacy rules as any other clinical read — a logbook entry does not
  create a new patient-data access path.
