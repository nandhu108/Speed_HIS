# Microservices Strategy — Prixo HIS

**Status:** approved direction, 2026-08-13.
**Position:** Prixo HIS remains a modular monolith. Services are extracted only where a measured
score and an operational reason both justify it.

---

## 1. Why not 23 microservices

A hospital information system has properties that punish premature distribution:

- **Atomicity is a safety property.** Bed allocation, stock decrement, and bill posting currently
  commit together. Distributing them converts a database guarantee into an eventual-consistency
  problem in a domain where "the bed shows free but the patient is in it" is a clinical incident.
- **Availability is coupled anyway.** Registration, orders, results, and billing are used in one
  continuous workflow at a counter. Splitting them multiplies failure modes without letting any part
  usefully continue alone.
- **One team.** Service-per-team is the strongest microservice justification; it does not apply here
  yet.
- **Single-hospital load.** No module currently exhibits a scaling profile that a well-indexed
  PostgreSQL instance and a horizontally-scaled stateless API cannot serve.

The monolith is not the problem. **Missing capability (audit, evidence, events, notification,
observability) is the problem**, and none of it requires distribution.

---

## 2. The migration ladder

```
MODULAR MONOLITH                ← today (boundaries by convention)
        ↓
DOMAIN BOUNDARIES               ← Phases 1–7: each module owns its tables; contracts replace
        ↓                          cross-module table reads
EVENT-DRIVEN COMMUNICATION      ← Phase 8: transactional outbox, in-process dispatch
        ↓
EXTRACT HIGH-VALUE SERVICES     ← Phases 9–10: Notification, then Audit & Evidence
        ↓
HYBRID ARCHITECTURE             ← monolith + 2–3 services; the steady state for years
        ↓
MICROSERVICES WHERE JUSTIFIED   ← only on evidence: scale, failure isolation, or team ownership
```

Strangler Fig at every rung: the new boundary is built beside the old path, callers are redirected
one at a time, and the old path is deleted only when nothing calls it.

---

## 3. Extraction scoring rubric

Nine dimensions, 1–5 each (5 = most favourable to extraction). "Integration complexity" and
"transaction complexity" are scored inverted, so 5 means *low* complexity.

| Dimension | 1 | 5 |
|---|---|---|
| Business independence | inseparable from another module's workflow | standalone business capability |
| Database independence | writes tables other modules also write | owns every table it writes |
| Deployment independence | must ship with the core | can ship on its own cadence |
| Scaling requirement | scales with the core | distinct, spiky, or heavy load profile |
| Team ownership | no distinct owner | a team could own it end-to-end |
| Failure isolation | its outage stops patient care | its outage degrades gracefully |
| Integration complexity (inv.) | many synchronous callers | few, asynchronous |
| Data ownership | data is co-owned | clear single owner |
| Transaction complexity (inv.) | participates in multi-module ACID transactions | no cross-module transactions |

**0–15** keep in modular monolith · **16–25** prepare boundary · **26+** strong candidate.

Full scored table for all 23 candidates: [current-state.md §12](current-state.md#12-microservice-candidate-analysis).

Summary of verdicts:

| Verdict | Candidates |
|---|---|
| **Strong (26+)** | Notification 43 · Audit & Evidence 41 · Reporting 34 · Quality 34 · Biomed 32 · Lab 31 · Radiology 31 · Identity 31 · MRD 29 · Appointments 28 · Insurance 28 · Patient Identity 27 |
| **Prepare boundary (16–25)** | Finance 25 · Pharmacy/Inventory 22 · EMR 21 · OT 18 |
| **Keep (0–15)** | IPD 14 · ICU 14 |
| **Keep, low score** | Billing 16 · OPD/Encounter 17 · Nursing 17 · Emergency 17 |

> A high score is *permission to consider*, not an instruction. Biomed scores 32 and will not be
> extracted, because there is no scaling, ownership, or failure-isolation reason to pay the price.

---

## 4. Extraction order and rationale

| Order | Service | Score | Reason it goes here |
|---|---|---|---|
| 1 | **Notification** | 43 | It does not exist yet. Building it standalone costs nothing extra, and it has zero legacy coupling: one dead outbox table and no dispatcher. Naturally async, degrades gracefully, distinct scaling (bursty), obvious owner. |
| 2 | **Audit & Evidence** | 41 | Built in-monolith first (Phases 1–4) so the model stabilises against real traffic; extracted when audit volume, retention, and immutability requirements justify separate storage. Write-heavy, read-rare, different retention profile from OLTP. |
| 3 | **Reporting / Analytics** | 34 | Only after a data pipeline exists. Extraction without a pipeline just moves the query load, it does not remove it from the primary. |
| 4+ | Reassess | — | Patient Identity, Lab, Radiology, Identity & Access are the plausible next wave. **None is scheduled.** Each requires a fresh score after Phases 1–10, plus a stated operational trigger. |

---

## 5. Requirements every extracted service must satisfy

Non-negotiable checklist. A service that cannot tick every box is not extracted.

| Requirement | Standard for Prixo |
|---|---|
| **Clear ownership** | One named owner; the service's tables appear under exactly one owner in [database-boundaries.md](database-boundaries.md) |
| **Database boundary** | Own schema at minimum, own database preferred. No other service reads its tables directly. |
| **API contract** | REST over `/api/v1/`, `ApiResponse<T>` envelope, OpenAPI published, versioned path |
| **Event contract** | Versioned event schemas per [domain-events.md](domain-events.md); additive changes only within a major version |
| **Authentication** | Same JWT issuer; service-to-service calls carry the caller's identity, never a shared superuser |
| **Authorization** | `MODULE.RESOURCE.ACTION` codes checked at the service, not only at the caller |
| **Audit events** | Emits to the audit backbone with `correlationId`, `patientId`/`encounterId` where applicable |
| **Monitoring** | RED metrics (rate/errors/duration) + business metrics; dashboards before cutover |
| **Error handling** | No silent failure; typed errors; poison messages to a dead-letter queue with alerting |
| **Retry strategy** | Exponential backoff with jitter and a cap; retries only on idempotent operations |
| **Idempotency** | Every mutating endpoint and every consumer accepts an idempotency key / dedupes on event id |
| **Versioning** | Path-versioned API; event `schemaVersion`; N-1 compatibility during rollout |
| **Health checks** | `/health` (liveness) and `/health/ready` (dependencies) — matching the existing API convention |

---

## 6. Blockers that must clear before *any* extraction

1. **`IAppDbContext` exposes all ~300 DbSets.** Until modules declare owned tables, no boundary is
   real. (Phases 1–7 introduce ownership; enforcement is by review + an architecture test.)
2. **No observability.** Extraction without tracing is a debugging regression. (Phase 16 — but
   correlation IDs land in Phase 1 and tracing must precede Phase 9.)
3. **No idempotency anywhere.** Distributed retries are unsafe until mutating endpoints accept keys.
4. **No integration tests.** The suite is 541 unit tests plus an empty integration project. A
   contract-test harness must exist before a network boundary is introduced.
5. **Bidirectional entity coupling** (`Billing ↔ Ipd`, `Pharmacy ↔ Billing`, `Patients ↔ Insurance`)
   must be broken by contract or event on at least one side.

---

## 7. Explicit non-goals

- **No Kafka.** The outbox + in-process dispatch pattern covers Phases 8–10. If a broker becomes
  necessary, evaluate PostgreSQL `LISTEN/NOTIFY`, then a single-node broker sized to real volume.
  Technology follows measured need.
- **No API gateway while there is one backend service.** Revisit at Phase 9. When introduced, it does
  authentication, routing, rate limiting, versioning, correlation, and logging — **never business
  logic**.
- **No splitting of the clinical core** (OPD/IPD/Nursing/ICU/ER/OT).
- **No service mesh, no Kubernetes-by-default, no CQRS-everywhere, no per-service polyglot
  persistence.**
- **No "extract to prove we can".** Every extraction needs a written trigger: a scaling metric, an
  isolation incident, or a team boundary.

---

## 8. Reversal criteria

An extraction is reverted (merged back into the monolith) if, six months after cutover:

- it has caused more incidents than it prevented;
- its independent deploy cadence was never used;
- every change to it still required a coordinated monolith change; or
- its data had to be replicated back into the monolith to make queries work.

Reversal is a normal outcome, not a failure.
