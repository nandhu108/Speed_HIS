# Prixo HIS — Architecture Documentation

Architecture and roadmap documents for the evolution of Prixo HIS from a hospital management system
into a **clinical + operational + financial + quality + audit + regulatory evidence platform**.

For module functionality, see [`docs/modules/`](../modules/). For build scope and phase order, see
[`docs/HIS_DEVELOPMENT_PLAN.md`](../HIS_DEVELOPMENT_PLAN.md) — that plan remains the source of truth
for the 24 delivered phases; the documents here describe what comes next.

---

## Start here

| Document | Read it for |
|---|---|
| [current-state.md](current-state.md) | **Assessment of what exists today** — measured module map, dependency graph, database coupling, security findings, scalability risks, 22-item debt register, microservice scoring for 23 candidates |
| [future-architecture.md](future-architecture.md) | The target shape, layer model, and 10 binding architectural principles |
| [migration-roadmap.md](migration-roadmap.md) | Phases 0–16, dependencies, exit criteria, risks |

## Architecture

| Document | Covers |
|---|---|
| [microservices-strategy.md](microservices-strategy.md) | Extraction rubric, strangler-fig ladder, service requirements, non-goals, reversal criteria |
| [service-boundaries.md](service-boundaries.md) | The 23 logical services — owned data, contracts, events, deployment verdict |
| [domain-events.md](domain-events.md) | Event envelope, full catalogue, correlation, outbox mechanics, versioning |
| [database-boundaries.md](database-boundaries.md) | Table ownership, contested tables, cross-module read register, enforcement |

## Audit & evidence

| Document | Covers |
|---|---|
| [audit-architecture.md](audit-architecture.md) | `audit_event` model, immutability, access logging, corrections, auditor personas |
| [patient-audit-journey.md](patient-audit-journey.md) | Journey read model, timeline UI, gap detection, audit report |
| [evidence-management.md](evidence-management.md) | Evidence Vault — registry over five existing document stores |

## Quality & compliance

| Document | Covers |
|---|---|
| [nabh-framework.md](nabh-framework.md) | Versioned regulatory framework, requirement mapping, NABH Audit Mode, readiness scoring |
| [nmc-framework.md](nmc-framework.md) | NMC framework layer; education entities deferred by decision |
| [clinical-document-completeness.md](clinical-document-completeness.md) | Configurable required-document sets and completeness scoring |
| [patient-safety-engine.md](patient-safety-engine.md) | Safety events, rule engine, escalation, promotion to incident |

## Platform

| Document | Covers |
|---|---|
| [security-architecture.md](security-architecture.md) | Current controls, 13 gaps with closing phases, auditor model, privacy |
| [observability.md](observability.md) | Logging, metrics, tracing, correlation, health, alerting |
| [future-enhancements.md](future-enhancements.md) | The 26 candidate modules with honest status — Planned / Partial / Future |

---

## Standing rules for this work

1. **Reuse before creation** — no second patient identity, notification, audit, or document store.
2. **Additive over destructive** — no working module is rewritten to fit the new architecture.
3. **Boundaries before services** — a module owns its tables and publishes its events before anyone
   discusses extracting it.
4. **Audit is written, never edited** — corrections are new events referencing the original.
5. **Regulation is data, never code** — no NABH or NMC requirement content ships; no compliance claim
   appears in code, UI, or reports.
6. **Read-only means read-only by construction** — the `AUDIT.*` namespace contains no write verbs.
7. **No fabricated metrics** — a component without a data source renders "Not measured", never 0%.
8. **No PHI** in logs, URLs, events, or notification payloads.

---

*Assessment and design set produced 2026-08-13. Phase 0 complete; Phase 1 (audit foundation) is next
and not yet started.*
