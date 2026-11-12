# Database Boundaries — Prixo HIS

**Status:** design, 2026-08-13.
**Today:** one PostgreSQL database, 23 schemas, 245 tables, one `AppDbContext` exposing ~300 `DbSet`s
to all 144 Application services. **Module data isolation is currently zero at the persistence layer.**

**Target:** every table has exactly one owning module. Other modules reach it through a service
contract or an event — never a direct query. The database stays physically single until a service is
actually extracted.

---

## 1. Ownership rules

1. **One owner per table.** The owner is the only module that writes it.
2. **Reads are the hard part.** A read of another module's table is allowed *today* but must be
   registered in §4 so it can be replaced deliberately.
3. **Reference data is published, not shared.** `master.*` is written by Masters and read by all;
   that stays, treated as a published read model.
4. **No new cross-module writes.** Any new feature that needs to write another module's table calls
   that module's service instead. This rule starts immediately, before any migration work.
5. **Physical separation follows extraction, not the other way round.** Schemas stay in one database
   until a service moves out.

---

## 2. Schema → owner map

| Schema | Tables | Owner (target) | Physical target |
|---|---|---|---|
| `admin` | 19 | Identity & Access | stays in core DB |
| `audit` | 2 (+ new) | Audit & Evidence | **own DB at Extract-10** |
| `master` | 16 | Masters (published read model) | stays |
| `patient` | 9 | Patient Identity | stays; reassess |
| `appointment` | 4 | Appointment | stays |
| `opd` | 12 | Encounter/Clinical | stays |
| `ipd` | 13 | IPD | stays |
| `nursing` | 10 | Nursing | stays |
| `icu` | 8 | ICU | stays |
| `er` | 6 | Emergency | stays |
| `ot` | 11 | Operation Theatre | stays |
| `emr` | 8 | EMR | stays |
| `lab` | 11 | Laboratory | stays; reassess |
| `rad` | 6 | Radiology | stays; reassess |
| `pharmacy` | 28 | Pharmacy/Supply | stays |
| `billing` | 17 | Billing | stays |
| `finance` | 6 | Finance | stays |
| `insurance` | 5 | Insurance/TPA | stays; reassess |
| `mrd` | 8 | MRD | stays |
| `biomed` | 9 | Biomedical | stays |
| `quality` | 14 (+ new) | Quality & Compliance | stays |
| `loyalty` | 2 | Loyalty | stays |
| `reporting` | 2 + 3 MVs | Reporting | **own store at Extract-15** |

---

## 3. Contested tables (multiple writers today)

These are the real boundary violations. Each needs an owner API before the writing modules can be
separated.

| Table | Current writers | Future owner | Migration strategy | Risk |
|---|---|---|---|---|
| `pharmacy.stock_ledger` | Pharmacy, OT (implants/consumables), Biomed (spares), Stores (indent issue) | **Pharmacy** | Funnel all mutation through an internal `IStockMutator` port (the `StockLedgerWriter` primitive already exists and is reused by OT/Biomed — formalize it as the only write path, then make it the service contract) | **High** — three modules mutate stock inside their own transactions |
| `pharmacy.batch` | same as above (FEFO allocation) | **Pharmacy** | Same port; allocation becomes `AllocateFefo(request)` returning batch assignments | High |
| `billing.bill_item` | Billing, IPD (bed charges), OT (implant charges), Pharmacy (sale), ER (empty bill) | **Billing** | `IBillCharger.PostChargeAsync(...)` port — one entry point for appending charge lines; already partially expressed by `IAdvanceCollector`/`IFinanceLedgerPoster` idiom | **High** |
| `billing.bill` | Billing, Insurance (`InsuranceCompanyId`) | **Billing** | Insurance calls a `LinkInsurer` action instead of writing the column | Medium |
| `ipd.admission` | IPD, ER (admit-convert), OT (booking anchor) | **IPD** | ER/OT call `AdmissionService`; they already do for most paths | Medium |
| `opd.op_visit` | OPD, MHC flow, Camp flow, Appointments (check-in) | **Encounter/Clinical** | All three registration flows already funnel through `OpVisitService`; formalize | Low |
| `patient.patient` | Patients, Portal (self-registration), ER (unknown-patient) | **Patient Identity** | Both already call `PatientService`; formalize as the only writer | Low |
| `admin.notification_queue` | Appointments (only producer, no consumer) | **Notification (new)** | Superseded by the Notification service's own tables; the old table is drained then retired | Low |
| `finance.ledger_entry` | Finance (via `IFinanceLedgerPoster` hooks from Billing/Insurance) | **Finance** | Already correct — the hook is the contract | Low |
| `reporting.mv_*` | Reporting refresher only | **Reporting** | Already correct | Low |

---

## 4. Cross-module read register

Reads that must eventually become contracts or replicated read models. Derived from the measured
dependency map ([current-state.md §4](current-state.md#4-dependency-map)).

| Reader | Reads (owner) | Replacement strategy |
|---|---|---|
| Billing | Opd, Ipd, Er, Emr, Mrd, Patients, Masters | Charge context passed in by the caller; patient/encounter via `EncounterRef` |
| Insurance | Billing, Ipd, Opd, Patients | Bill snapshot API; `EncounterRef` |
| Emr | Opd, Ipd, Masters, Patients | Timeline read model fed by events (Phase 3) |
| Quality | Ipd (occupancy), Lab (TAT), Masters | Indicator auto-compute moves to event-fed datasets |
| Nursing | Ipd (medication orders), Patients | MAR generation stays in-core; contract at extraction time only |
| Icu | Ipd | Same — ICU is IPD by design |
| Mrd | Ipd | Discharge events |
| Ot | Ipd, Billing, Pharmacy, Patients | Charge + stock ports (above) |
| Er | Ipd | `AdmissionService` |
| Finance | Billing, Pharmacy | Event-fed postings (already hook-based) |
| Reports | Ipd, Admin, everything | Event-fed datasets / MVs |
| Portal, DoctorPortal | Appointments, Billing, Emr, Ipd, Lab, Rad | **Keep delegating to owning services** — already correct |
| Lab, Rad | Opd, Patients | `EncounterRef` + patient snapshot on the order |

---

## 5. New tables introduced by the roadmap

All additive. No existing table is dropped or restructured.

| Phase | Schema | Table | Purpose |
|---|---|---|---|
| 1 | `audit` | `audit_event` | The audit backbone (partitioned monthly) |
| 1 | `audit` | `audit_access_log` | Record/document read access |
| 1 | `audit` | `audit_event_correction` | Corrective events referencing `original_event_id` |
| 2 | `opd`/view | `encounter_ref` (view or projection table) | Unified encounter reference over the three roots |
| 3 | `audit` | `patient_journey_projection` | Read model for the journey timeline (rebuildable) |
| 4 | `quality` | `evidence`, `evidence_link`, `evidence_version`, `evidence_access_log` | Evidence Vault (generalizes `quality_evidence`) |
| 5 | `quality` | `regulatory_framework`, `regulatory_requirement`, `requirement_mapping`, `requirement_evidence_status` | Versioned regulatory frameworks (NABH/NMC/hospital-defined) |
| 6 | `quality` | `documentation_requirement`, `documentation_requirement_item` | Configurable required-document sets per encounter type |
| 7 | `quality` | `safety_event`, `safety_rule`, `audit_gap` | Safety engine + gap register |
| 8 | `audit` | `outbox_message` | Transactional outbox |
| 9 | (new svc) | notification templates/queue/attempts/escalations | Notification service store |

Conventions unchanged: `BaseEntity`/`BranchEntity`, soft delete (**except** audit tables, which are
append-only and carry no `IsDeleted`), partial unique indexes `WHERE is_deleted = false`, snake_case,
`numeric(18,2)` money, `timestamptz`, FKs with `ON DELETE RESTRICT`.

---

## 6. Enforcement

| Mechanism | Phase | What it does |
|---|---|---|
| **Ownership manifest** | 1 | A per-module list of owned tables, checked into `docs/architecture/` and reviewed on every PR that adds a table |
| **Architecture test** | 2 | A unit test asserting each `Features/<Module>` folder only references its declared `Domain.Entities.*` namespaces; violations require an explicit waiver entry |
| **`IAppDbContext` narrowing** | 7+ | Introduce per-module context interfaces (`IBillingDbContext`, …) exposing only owned + explicitly-permitted `DbSet`s; migrate module by module. **Large, low-urgency, do not front-load.** |
| **Schema grants** | at extraction | The extracted service's DB user loses rights on tables it does not own |

---

## 7. Risks

| Risk | Mitigation |
|---|---|
| Narrowing `IAppDbContext` is an XL refactor touching 144 services | Do it module-by-module, last; the manifest + architecture test deliver most of the benefit at a fraction of the cost |
| Stock/bill ports change hot transactional paths | Port extraction is behaviour-preserving (same code, one entry point); covered by the existing 541 unit tests plus new integration tests |
| Audit partitioning changes query plans | Introduce partitioning at table creation (Phase 1), not retrofitted |
| Read models drift from source of truth | Projections are rebuildable from events; a rebuild command ships with each projection |
| Extraction copies data and creates two truths | Only Notification (owns nothing shared) and Audit (append-only, single writer) are scheduled — neither duplicates business state |
