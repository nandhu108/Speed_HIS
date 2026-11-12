# Observability — Prixo HIS

**Status:** design, 2026-08-13. **Phase 16 for the full stack — but correlation IDs land in Phase 1
and tracing must precede any service extraction (Phase 9).**

---

## 1. Current state

| Signal | Reality |
|---|---|
| Logging | Serilog → **console only**, `UseSerilogRequestLogging()` enabled, config-driven sinks available but none configured |
| Correlation | ASP.NET `TraceIdentifier` echoed in `ApiResponse.traceId` and stored on `audit_log.TraceId` — **per-request only, not propagated across a workflow** |
| Metrics | **None.** No OpenTelemetry, no Prometheus, no `System.Diagnostics.Metrics` usage |
| Tracing | **None.** No `ActivitySource`, no distributed tracing |
| Health | `/health` (liveness) and `/health/ready` (PostgreSQL) — correctly separated |
| Background jobs | Hangfire dashboard (dev only, anonymous); no job metrics or alerting |
| Errors | `ExceptionHandlingMiddleware` returns the `ApiResponse` envelope; no error tracker |
| Business monitoring | None — no alert if bills stop being created or the reporting refresh silently fails |

**Consequence:** extracting a service today would be a debugging regression. This is why the roadmap
puts tracing before extraction.

---

## 2. Target signal model

### 2.1 Structured logging

- Serilog with **structured properties**, not string interpolation.
- Enrichers on every log: `CorrelationId`, `RequestId`, `UserId`, `BranchId`, `SessionId`,
  `SourceService`, `Environment`.
- **PHI redaction is a first-class destructuring policy** — patient names, UHIDs, contact details,
  diagnoses and clinical text never reach a sink. Asserted by test.
- Levels: `Information` for business milestones, `Warning` for degraded paths, `Error` for handled
  failures, `Fatal` for startup/shutdown. Request logging stays at `Information` with a sampling rule
  for health checks.
- Sinks: console (dev), file/rolling + a log aggregator (prod). Retention aligned to the audit
  retention policy — **logs are not the audit trail** and must not be relied on as one.

### 2.2 Metrics

RED for every endpoint + USE for resources + business metrics that matter clinically:

| Category | Metrics |
|---|---|
| HTTP | request rate, error rate, duration histogram (p50/p95/p99) per route + status |
| Database | connection pool usage, query duration, transaction duration, deadlocks, long-running transactions |
| Background jobs | run count, failure count, duration, **lag** (scheduled vs actual), Hangfire queue depth |
| Outbox (Phase 8) | pending depth, dispatch latency, retry count, dead-lettered count |
| Audit | audit-write latency, events/sec, partition size, chain-verification failures |
| Business | bills created/hour, payments collected/hour, admissions, OP registrations, lab results approved, **unacknowledged critical alerts**, open high/critical safety events, documentation completeness average |
| Notification (Phase 9) | queued, sent, failed, escalated, delivery latency per channel |

Business metrics matter as much as technical ones: "no bill has been created in 30 minutes during
OPD hours" is a more useful alert than CPU utilization.

### 2.3 Tracing

- OpenTelemetry `ActivitySource` per module; automatic instrumentation for ASP.NET Core, HttpClient,
  Npgsql.
- Trace context propagated via W3C `traceparent`; **`CorrelationId` carried as a baggage item** so a
  business workflow and a technical trace can be joined.
- Spans on: request → service method → transaction → event publish → consumer handling.
- Sampling: 100% for errors and for audit-relevant operations; head-based sampling otherwise.

---

## 3. Correlation model

One id per business thread of work, distinct from the per-request id:

| Id | Scope | Where it lives |
|---|---|---|
| `RequestId` | one HTTP request | ASP.NET `TraceIdentifier`, `ApiResponse.traceId` |
| `CorrelationId` | one business workflow (registration → … → discharge) | `X-Correlation-Id` header, Serilog context, `audit_event.correlation_id`, event envelope |
| `CausationId` | the immediate cause of an event | event envelope |
| `TraceId`/`SpanId` | distributed trace | W3C headers, OTel |
| `AuditEventId` | one recorded fact | `audit.audit_event` |

**Phase 1 delivers the correlation middleware**: accept or mint `X-Correlation-Id`, push to
`LogContext`, expose on `IAuditContext`, echo in the response, and stamp every audit event. Everything
later depends on this being right, and it is cheap now.

---

## 4. The administrator trace story

Target: an administrator can follow one action end to end.

```
USER ACTION      Dr. Menon approves discharge summary IP-2026-000412
   ↓ API         POST /api/v1/ipd/discharge-summaries/8842/approve   [corr: 7f3a…]
   ↓ AUTHZ       IPD.DISCHARGESUMMARY.APPROVE granted (user 214, roles [DOCTOR])
   ↓ SERVICE     DischargeService.ApproveAsync — span 42 ms
   ↓ DATABASE    tx: discharge_summary UPDATE + audit_event INSERT — 18 ms
   ↓ EVENT       DischargeSummaryApproved published → outbox (id 91233)
   ↓ CONSUMERS   Audit ✓ 4 ms · Completeness ✓ 11 ms · Notification ✓ 260 ms
   ↓ NOTIFICATION SMS to ward · in-app to billing · delivered 09:41:12
   ↓ AUDIT       audit_event 0f21… — actor, role, department, patient, encounter, before/after
```

Every hop is queryable by `CorrelationId`, and the audit event is queryable by `PatientId`.

---

## 5. Health and readiness

| Endpoint | Checks |
|---|---|
| `/health` | Process liveness (existing) |
| `/health/ready` | PostgreSQL (existing) + file storage writable + Hangfire server alive + outbox dispatcher heartbeat (Phase 8) |
| `/health/detail` | Per-dependency detail, permissioned (`ADMIN.HEALTH.VIEW`), never anonymous |

Extracted services (Phase 9+) implement the same three, and the monolith surfaces their status on an
admin service-status screen.

---

## 6. Alerting

| Alert | Condition | Priority |
|---|---|---|
| API error rate | >2% of requests 5xx over 5 min | High |
| Database saturation | pool >85% or p95 query >1 s | High |
| **Unacknowledged critical lab alert** | age > configured window | **Clinical — highest** |
| Open critical safety event unacknowledged | age > window | Clinical — highest |
| Outbox backlog | pending > threshold or oldest > 5 min | High |
| Dead-lettered events | any | High |
| Job failure | no-show / reporting refresh / bed-charge job fails or misses a run | Medium |
| Audit chain verification failure | any mismatch | **Critical (integrity)** |
| Business flatline | zero bills/registrations during configured operating hours | Medium |
| Notification delivery failure rate | >10% per channel over 15 min | Medium |

Clinical alerts route through the Notification service's escalation chain, not to an ops channel
alone — an unacknowledged critical result is a patient-safety event before it is an ops event.

---

## 7. Dashboards

| Dashboard | Audience |
|---|---|
| Service health (RED, dependencies, jobs, outbox) | Ops |
| Database (connections, slow queries, transaction duration, table growth incl. audit partitions) | Ops/DBA |
| Business activity (registrations, visits, admissions, bills, collections, results) | Operations manager |
| Clinical safety (critical alerts, safety events, acknowledgment times) | Quality / clinical leadership |
| Audit health (event volume, access-log volume, chain verification, projection lag) | Quality / compliance |

---

## 8. Sequencing

| Phase | Delivers |
|---|---|
| **1** | Correlation middleware + log enrichment + PHI redaction policy + audit write metrics |
| **6–7** | Business + clinical safety metrics (completeness, safety events, acknowledgment latency) |
| **8** | Outbox metrics, dispatcher heartbeat, dead-letter alerting |
| **Before 9** | **OpenTelemetry tracing must be live** — no extraction without it |
| **16** | Full stack: aggregated logs, metrics backend, dashboards, alert routing, service-status screen |

---

## 9. Constraints

- **Never log PHI.** Redaction is enforced by policy and asserted by test, not left to reviewer
  discipline.
- **Logs are not audit.** The audit trail is `audit.audit_event`; log retention and audit retention
  are set independently.
- Sampling must never drop error traces or audit-relevant operations.
- Instrumentation must not measurably slow clinical paths; metrics are cheap counters/histograms, not
  synchronous writes.
- Dashboards showing patient counts must respect branch scoping and never expose identifiable
  patients.
