# Security & Privacy Architecture — Prixo HIS

**Status:** design, 2026-08-13. Records the current posture (verified in code), the target, and the
phase in which each gap closes.

---

## 1. Current posture — what is already right

Verified in the codebase, not assumed:

| Control | Implementation |
|---|---|
| Deny by default | `FallbackPolicy = RequireAuthenticatedUser()`; anonymous surface limited to login/refresh/portal-auth/health (+ OpenAPI, Scalar, Hangfire dashboard **in Development only**) |
| Permissions not in the token | `PermissionAuthorizationHandler` resolves `MODULE.RESOURCE.ACTION` against the database **per request** — revocation is immediate. 405 codes from a single registry. |
| Per-user overrides | `admin.user_permission_override` on top of role grants |
| Server-side session control | `SessionValidationMiddleware` rejects revoked/idle-expired sessions; configurable idle timeout; activity write throttled to 1/min |
| Short-lived tokens | 15-minute JWT + rotating **hashed** refresh tokens with reuse detection (`TOKEN_REUSE_DETECTED`) |
| Separate portal auth surface | Distinct JWT audience — a staff token can never satisfy `[PortalAuth]` and vice versa |
| Branch isolation | Global EF query filter on every `IBranchScoped` entity; cross-branch needs `IgnoreQueryFilters()` + `REPORT.CROSSBRANCH.VIEW` (used in 4 places) |
| Soft delete everywhere | Global filter; hard deletes converted by the interceptor |
| Audit stamping | `AuditSaveChangesInterceptor` in the same transaction as the change; token/session/password tables excluded; `PasswordHash` column-excluded |
| Input validation | FluentValidation per request DTO via a global `ValidationFilter`; DTO-only boundaries |
| Safe file writes | Path-segment sanitization + GUID-prefixed filenames — no traversal |
| Portal brute-force limiting | Fixed-window rate limit per IP on portal auth |
| Transactions | `IUnitOfWork` mandatory on billing, payments, stock, GRN, admission, discharge, day close, merge |

---

## 2. Gaps and their closing phase

| # | Gap | Severity | Closes in |
|---|---|---|---|
| S1 | **No document/record access logging** across 25 file-returning endpoints | High | **Phase 1** |
| S2 | No MFA and no MFA-ready schema | High | Phase 1 (schema) / Phase 11 (enforcement) |
| S3 | `IVirusScanner` unimplemented — every attachment stored `NotScanned` | High | Phase 4 (with Evidence Vault) |
| S4 | Rate limiting only on portal auth; staff login unlimited | Medium | Phase 1 |
| S5 | Branch filter **silently disables** when `BranchId` is null (background jobs, seeding, branchless users) | Medium | Phase 2 |
| S6 | No department scoping or patient-ownership restriction — any branch-level VIEW sees every patient | Medium | Phase 2 |
| S7 | No auditor role; no read-only permission model | Medium | **Phase 1** |
| S8 | Files unencrypted on a local volume; no object storage | Medium | Phase 4 |
| S9 | Hangfire dashboard anonymous (dev) and shares the business database | Low/Med | Phase 16 |
| S10 | No idempotency keys on mutating endpoints — a retry can double-post a payment | Medium | Phase 8 |
| S11 | Digital signature is a placeholder path column; no DSC/PKI | Medium | future enhancement |
| S12 | No integration tests despite the stated Definition of Done | Medium | Phase 1 onward |
| S13 | No log redaction policy — an exception carrying a DTO could serialize PHI | Medium | Phase 1 |

---

## 3. Authorization model (target)

Unchanged in shape — `MODULE.RESOURCE.ACTION`, DB-checked, seeded from `PermissionRegistry`. Three
additions:

### 3.1 The `AUDIT.*` read-only namespace

Contains **no write verbs at all**. Read-only by construction, not by omission:

```
AUDIT.JOURNEY.VIEW · AUDIT.EVENT.VIEW · AUDIT.ACCESS.VIEW · AUDIT.EVIDENCE.VIEW
AUDIT.GAP.VIEW · AUDIT.COMPLETENESS.VIEW · AUDIT.READINESS.VIEW · AUDIT.SAFETY.VIEW
AUDIT.REPORT.GENERATE · AUDIT.CROSSBRANCH.VIEW
```

Enforced by a build-failing test: no code in the `AUDIT.` namespace may end in
`CREATE|UPDATE|DELETE|APPROVE|REJECT|CANCEL|MANAGE`, and no auditor role preset may contain a code
from any other namespace's write verbs.

### 3.2 Three auditor personas

| Persona | Scope | Extra controls |
|---|---|---|
| **Internal quality team** | All `AUDIT.*` (cross-branch off by default) + `QUALITY.*` write codes for CAPA/NC work | Their write ability lives entirely in `QUALITY.*`; the audit surface stays read-only |
| **External assessor** | Journey, evidence, gaps, completeness, readiness, report generation. No raw event search, no cross-branch | **Time-boxed account** (validity window on `admin.user`), scoped to a named engagement + date range; every read logged; export watermarked with auditor + timestamp |
| **Hospital management** | Readiness, gaps, reports, cross-branch | No per-record clinical drill-down; dashboards and aggregates only |

### 3.3 Department scoping (Phase 2)

Adds an optional department predicate alongside branch scoping, configurable per role
(`Scope = Branch | Department | Assigned`), so a ward nurse's default view is her ward rather than
every patient in the branch. Existing behaviour remains the default until a hospital opts in — this
is a tightening, and tightening silently would break workflows.

---

## 4. Privacy architecture

Every sensitive access answers: **who · what · when · why · from where · which patient · which
encounter.**

| Mechanism | Detail |
|---|---|
| Access logging | `audit.audit_access_log` on record opens, document downloads, report exports, list searches over patient data, and **denied attempts** ([audit-architecture.md §5](audit-architecture.md#5-access-logging)) |
| Access reason | Configurable: `Off` / `FlaggedOnly` (VIP, MLC, staff, psychiatric — existing master categories) / `All` |
| Break-glass | Permissioned, mandatory reason, always logged, always notifies the quality owner |
| Minimum necessary | Department scoping + portal ownership checks + branch filter |
| PHI out of logs | Serilog destructuring policy: DTOs carrying patient data are redacted; no patient name/UHID in log messages; a log-scrubbing test asserts key fields never serialize |
| PHI out of URLs | Identifiers only in paths (`/patients/{id}`), never names, DOBs, or diagnoses in query strings |
| PHI out of events | Domain events carry identifiers and state, never clinical narrative |
| Portal isolation | Separate audience, ownership-checked wrappers, approved-only report access — already implemented |
| Retention | Per record type, with an approved and audited destruction path (the MRD pattern, generalized in the Evidence Vault) |

---

## 5. Authentication roadmap

| Item | Target |
|---|---|
| **MFA** | Schema in Phase 1 (`admin.user_mfa`: method, secret ref, enrolled_at, last_used, backup codes hashed), TOTP first, enforced per role (admin, finance, and all auditor personas) in Phase 11. Portal OTP remains gated on SMS integration. |
| Password policy | Existing history table + forced change on first login; add configurable complexity/expiry via `system_setting` |
| Session | Keep the current model; add concurrent-session limits and "sign out everywhere" (the sessions screen already exists) |
| Service-to-service | When services are extracted: caller identity propagated, never a shared superuser credential |
| Secrets | Out of `appsettings*.json` and into environment/secret store for any non-dev deployment; `Jwt:Key` is already validated for length at startup |

---

## 6. API security

| Control | Now | Target |
|---|---|---|
| Rate limiting | Portal auth only | Staff auth (per IP + per user), export endpoints, report generation, portal (unchanged) |
| Idempotency | none | `Idempotency-Key` header on POST/PUT for payments, refunds, stock movements, admissions — stored with the response hash for replay |
| CORS | Explicit allow-list from configuration | Unchanged |
| Error envelope | `ApiResponse<T>` with `traceId`; no stack traces to clients | Unchanged + correlation id echoed |
| Versioning | `/api/v1/` | Unchanged; `/v2` only for breaking changes |
| Upload security | Path sanitization, GUID names | + content-type allow-list, size caps, **virus scan before the file becomes retrievable** |
| Gateway | none (single backend) | Only if/when services are extracted; auth, routing, rate limiting, correlation — never business logic |

---

## 7. Data protection

| Layer | Now | Target |
|---|---|---|
| In transit | HTTPS at the edge | Unchanged; enforce HSTS, TLS ≥1.2 |
| At rest — database | Depends on host | Storage-level encryption; documented as a deployment requirement |
| At rest — files | Plain local disk | Object storage with server-side encryption; same `IFileStorageService` contract |
| Backups | Not expressed in the repository | Documented, tested restore procedure; backup of audit partitions with chain integrity preserved |
| Disaster recovery | Not expressed | RPO/RTO stated per environment; restore drill recorded as quality evidence |
| Key material | `Jwt:Key` from configuration | Secret store; rotation procedure with overlapping validity |

---

## 8. Security testing

| Layer | Requirement |
|---|---|
| Unit | Permission-namespace assertions (auditor has no write verbs); branch-filter behaviour; redaction policy |
| Integration (Testcontainers — package already referenced, suite currently empty) | Audit immutability rejects UPDATE/DELETE; access logging fires on every document endpoint; cross-branch denial; portal token rejected on staff endpoints and vice versa; idempotency replay |
| Regression | Every new endpoint must have a permission test; a controller without `[HasPermission]` fails a convention test |
| Periodic | Dependency scanning in CI; `/security-review` on branches touching auth, audit, or file handling |

---

## 9. Non-negotiables

- No anonymous endpoint beyond login/refresh/portal-auth/health.
- No permission embedded in a JWT.
- No audit log that can be edited or deleted by the application.
- No auditor with write access to clinical, financial, or audit data.
- No PHI in logs, URLs, events, or notification payloads.
- No bypass of the branch filter without an explicit, permissioned, audited path.
- No new module without permission codes, validators, and audit coverage — the existing Definition of
  Done stands, with **integration tests now actually written**.
