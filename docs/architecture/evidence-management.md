# Evidence Management (Digital Evidence Vault) — Prixo HIS

**Status:** design, 2026-08-13. **Phase 4.**

---

## 1. Position: generalize, do not duplicate

Prixo already stores documents in **five** places, each with its own table and its own file path
column:

| Existing store | Table | Domain |
|---|---|---|
| Quality/NABH evidence | `quality.quality_evidence` | chapter/standard-wise uploads |
| Patient documents | `patient.patient_document`, `patient_consent` | ID proofs, consents |
| EMR attachments | `emr.attachment` | clinical files (virus-scan placeholder) |
| MRD scans | `mrd.scanned_document` | scanned case records |
| Biomed / Insurance | `biomed.calibration` certificates, `insurance.claim_document` | certificates, claim packs |

All write through the same `IFileStorageService` (local disk today).

**The Evidence Vault is not a sixth store.** It is a *registry and contract layer*:
`quality.quality_evidence` is generalized into `quality.evidence`, and the other four remain the
owning stores of their own documents while **registering** themselves in the vault through
`evidence_link`. One index, one access-control point, one access log, one retention model — without
migrating five modules' files.

---

## 2. Model

```
quality.evidence                         -- the registry (generalizes quality_evidence)
  id · branch_id
  evidence_code            -- human reference, sequenced (EV-2026-000123)
  title · description
  evidence_type            -- ClinicalDocument | Consent | Report | Image | LabReport |
                           -- RadiologyReport | DischargeSummary | NursingRecord | OtChecklist |
                           -- IcuRecord | BillingDocument | InsuranceDocument | Policy | Sop |
                           -- TrainingRecord | AuditRecord | CapaEvidence | Certificate | Other
  source_type              -- Uploaded | Generated | Registered
  owner_module             -- QUALITY | PATIENT | EMR | MRD | BIOMED | INSURANCE | ...

  -- storage (for Uploaded/Generated); Registered rows point at the owning table instead
  file_path · file_name · mime_type · size_bytes
  checksum_sha256          -- ● integrity
  version                  -- ● starts at 1
  supersedes_evidence_id   -- previous version, never overwritten

  status                   -- Draft | Available | Verified | Superseded | Expired | Withdrawn
  uploaded_by_user_id · uploaded_at
  verified_by_user_id · verified_at · verification_notes

  retention_policy_id      -- reuses the MRD retention idiom
  retain_until · expiry_date
  is_deleted               -- soft delete only; withdrawal is a status, not a delete

quality.evidence_link                    -- polymorphic linkage (same idiom as QualityCapa.SourceType/SourceId)
  id · evidence_id
  link_type                -- Patient | Encounter | Requirement | Capa | Incident | NonConformance |
                           -- Risk | Audit | Order | Result | Bill | Procedure | Equipment | Committee
  patient_id · encounter_type · encounter_id
  target_type · target_id
  linked_by_user_id · linked_at

quality.evidence_version                 -- immutable version history
  id · evidence_id · version · file_path · checksum_sha256
  created_by_user_id · created_at · change_reason

quality.evidence_access_log              -- projects into audit.audit_access_log
  id · evidence_id · actor_user_id · actor_role_codes · accessed_at
  access_type              -- Viewed | Downloaded | Exported | Verified | Withdrawn
  access_reason · ip_address · session_id · correlation_id
```

Every evidence item therefore carries exactly what the brief requires: evidence id, document id,
patient/encounter linkage, requirement linkage, uploaded by, verified by, created/modified dates,
version, hash, status, retention policy, and access history.

---

## 3. Registration vs upload

| Mode | Used by | Behaviour |
|---|---|---|
| **Uploaded** | Quality, policies, SOPs, training records, external certificates | File written via `IFileStorageService`, checksum computed, row created |
| **Generated** | Discharge summary PDF, lab/radiology report, receipt, registration card | Produced by `IPdfService`; the vault stores the rendered artefact + checksum so the *exact document shown to the patient* is preserved |
| **Registered** | `patient_document`, `patient_consent`, `emr.attachment`, `mrd.scanned_document`, `insurance.claim_document`, `biomed.calibration` | No file copy. The vault holds a pointer (`owner_module`, `target_type/id`) plus checksum and linkage. The owning module remains the source of truth. |

This is the key decision: **no mass file migration, no duplicated storage, one queryable index.**

---

## 4. Integrity

- `checksum_sha256` computed on write for uploaded/generated evidence, and on registration for
  pointed-at files.
- A scheduled verification job re-hashes a sample of evidence and reports mismatches as a
  **Critical** safety/quality event (`EvidenceIntegrityMismatch`) — never silently.
- Versions are additive: a replacement creates version N+1 and marks N `Superseded`. Nothing is
  overwritten.
- Withdrawal sets `status = Withdrawn` with a mandatory reason; the file and its history remain.

---

## 5. Access control

| Permission | Grants |
|---|---|
| `QUALITY.EVIDENCE.VIEW` / `.UPLOAD` / `.VERIFY` / `.WITHDRAW` | Quality team operations |
| `AUDIT.EVIDENCE.VIEW` | Read-only auditor access (all three personas) |
| Owning-module permission | Still required for module-owned evidence — the vault **narrows** access, never widens it |

Rules:
- Patient-linked evidence is subject to the same branch scoping as the patient record.
- **Every access writes `evidence_access_log` and `audit.audit_access_log`** — including auditor reads.
- Auditors can view and export; they cannot upload, verify, withdraw, or delete. There is no code
  path for it.
- Downloads stream through the API with the permission check and the access log in the same handler —
  never a direct file URL.

---

## 6. Retention

Reuses the pattern MRD already implements (`retention_policy` → eligible → approved → destroyed,
with a destruction register):

```
Policy (per evidence_type + record class) → retain_until computed on write
   → Eligible for destruction (computed on read, like MRD's overdue detection)
   → Approval required (permissioned, reasoned)
   → Destruction recorded in the register; the EVIDENCE ROW SURVIVES as a tombstone
     (status = Expired, file removed, checksum + history retained)
```

Audit history of a destroyed document is never destroyed with it.

---

## 7. Where evidence surfaces

| Surface | Uses |
|---|---|
| Patient Audit Journey | `evidence_count` per step; evidence cards inline |
| NABH requirement view | Evidence available / missing per requirement |
| NMC framework | Logbook, assessment, training evidence (framework-level only for now) |
| CAPA | Closure evidence attached to the existing `quality_capa` |
| Incident / RCA | Supporting evidence on `nursing.incident` |
| Audit report | Evidence summary + missing-evidence sections |
| Documentation completeness | "Document present?" checks resolve through the vault |

---

## 8. Non-goals

- Not a DMS with folders, check-in/check-out, or collaborative editing.
- Not a PACS. Radiology images remain a placeholder integration; the vault stores *reports*, not
  DICOM studies.
- Not a replacement for the five owning stores.
- Not a legal e-signature service. Digital signature is a separate concern
  ([future-enhancements.md](future-enhancements.md#y-digital-signature--approval-service)); the vault
  records **whether** a document is signed and by whom, not the cryptography.

---

## 9. Dependencies

- **Phase 1** (audit) for access logging and correlation.
- **Object storage** should land with or before this phase: `LocalFileStorageService` is single-node,
  which blocks horizontal scaling and makes checksums the only integrity guarantee. Same
  `IFileStorageService` contract, new implementation.
- **Virus scanning** (`IVirusScanner`, currently unimplemented) must be wired before the vault accepts
  externally-supplied uploads at scale.
