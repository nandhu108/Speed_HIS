# Prixo HIS — Hospital Information System
## Master Development Plan

> **Document status:** Master plan v1.0 — Phases 0–12 complete; **Phase 12 (Radiology/RIS) complete** — 2026-07-17
> **Stack:** React 18 + Vite + TypeScript + Tailwind CSS | .NET 10 Web API | PostgreSQL 16 | EF Core 10 | Docker
> **Rule:** This document is the single source of truth for build order. No module is started before its phase.

---

## Table of Contents

1. [Project Vision](#1-project-vision)
2. [Technology Stack Decision](#2-technology-stack-decision)
3. [Why React + .NET + PostgreSQL + EF Core](#3-why-react--net--postgresql--ef-core)
4. [Folder Structure](#4-folder-structure)
5. [Backend Architecture](#5-backend-architecture)
6. [Frontend Architecture](#6-frontend-architecture)
7. [Database Design Strategy](#7-database-design-strategy)
8. [Authentication and RBAC Plan](#8-authentication-and-rbac-plan)
9. [Audit Log Strategy](#9-audit-log-strategy)
10. [Multi-branch / Multi-hospital-unit Strategy](#10-multi-branch--multi-hospital-unit-strategy)
11. [Module-wise Development Phases](#11-module-wise-development-phases)
12. [Exact Build Order](#12-exact-build-order)
13. [API Planning](#13-api-planning)
14. [Database Entity Planning](#14-database-entity-planning)
15. [UI Page Planning](#15-ui-page-planning)
16. [Role and Permission Planning](#16-role-and-permission-planning)
17. [Reports Planning](#17-reports-planning)
18. [Deployment Planning](#18-deployment-planning)
19. [Docker Setup Plan](#19-docker-setup-plan)
20. [Testing Plan](#20-testing-plan)
21. [Future Modules Plan](#21-future-modules-plan)
22. [AI and Integration Modules Future Roadmap](#22-ai-and-integration-modules-future-roadmap)
23. [Development Checklist](#23-development-checklist)

---

## 1. Project Vision

Prixo HIS is a **modular, multi-branch Hospital Information System** covering the complete patient journey — registration → appointment → OPD/IPD/Emergency → clinical documentation → diagnostics (Lab/Radiology) → pharmacy → billing → discharge → medical records — plus the supporting back-office (stores, finance, biomedical, quality) and self-service portals for patients and doctors.

### Goals

- **Single patient identity (UHID)** across all branches and all modules.
- **Clinical safety first:** ordered workflows (order → result → verify → approve), critical value alerts, medication administration records, audit trails on everything.
- **Financial integrity:** every chargeable event flows into billing automatically; transactions are atomic; day-close reconciles cash.
- **NABH-ready:** audit logs, consent capture, incident reporting, quality indicators, and MRD compliance are built into the core, not bolted on.
- **Extensible foundation:** Blood Bank, Dialysis, Insurance/TPA, HR & Payroll, integrations (ABDM, HL7 FHIR, PACS), mobile apps, and AI modules plug in later without re-architecture.

### Non-goals for the initial build

- No third-party integrations (SMS/WhatsApp/payment gateway/PACS/analyzers) — **placeholder interfaces only**.
- No mobile apps — the web frontend is responsive; portals cover self-service.
- No AI features — data model is designed so AI modules can consume it later.

### Guiding principles

1. **Foundation before features** — Phase 0/1/2 are completed and hardened before any clinical module.
2. **Masters drive everything** — no hardcoded hospital data, ever. Every dropdown is a master table.
3. **Every write is audited** — who, what, when, before/after values.
4. **Every money/stock movement is a database transaction.**
5. **Soft delete by default** on business data; hard delete never exposed via API.
6. **Placeholder ≠ skipped** — placeholder features get real DB columns and disabled UI, so enabling them later is configuration, not migration.

---

## 2. Technology Stack Decision

| Layer | Choice | Version | Notes |
|---|---|---|---|
| Frontend framework | React | 18.x | SPA |
| Build tool | Vite | 5.x+ | Fast HMR, native TS |
| Language (FE) | TypeScript | 5.x | `strict: true` |
| CSS framework | **Tailwind CSS** | 3.x/4.x | See justification below |
| Headless components | Radix UI / Headless UI | latest | Accessible primitives styled with Tailwind |
| Routing | React Router | 6.x | Route-based code splitting |
| Server state | TanStack Query | 5.x | Caching, invalidation, optimistic updates |
| HTTP client | Axios | 1.x | Interceptors for JWT + refresh |
| Forms | React Hook Form + Zod | latest | Schema validation shared conventions with backend DTOs |
| Tables | TanStack Table | 8.x | Server-side pagination everywhere |
| Charts | Recharts | latest | Dashboards |
| Backend | .NET Web API | **.NET 10 (LTS)** | C# 14 |
| ORM | Entity Framework Core | 10.x | Code First + Npgsql provider |
| Database | PostgreSQL | 16.x | Single DB, schema-per-domain |
| Auth | JWT + Refresh Tokens | — | `Microsoft.AspNetCore.Authentication.JwtBearer` |
| Validation (BE) | FluentValidation | 11.x | Per-DTO validators |
| Mapping | Mapster (or manual mapping) | latest | Lightweight, fast |
| Logging | Serilog | latest | Console + file sinks; structured JSON |
| API docs | OpenAPI (built-in .NET 10) + Scalar UI | — | .NET 10 ships `Microsoft.AspNetCore.OpenApi` |
| Background jobs | Hangfire (PostgreSQL storage) | latest | Alerts, TAT recalculation, day-close jobs |
| PDF generation | QuestPDF | latest | Reports, bills, lab reports |
| Containerization | Docker + Docker Compose | — | Dev + prod profiles |
| Reverse proxy (prod) | Nginx | — | TLS termination, static frontend |

### Tailwind CSS vs Bootstrap — decision: **Tailwind CSS**

| Criterion | Tailwind | Bootstrap | Winner |
|---|---|---|---|
| Data-dense hospital screens (queues, worklists, charts) | Utility-level control over spacing/density | Fixed component metrics fight you | Tailwind |
| Custom design system (hospital branding, print layouts) | First-class via `tailwind.config` tokens | Requires Sass overrides | Tailwind |
| Bundle size | JIT purges to only used classes | Ships full CSS + JS | Tailwind |
| React fit | Pure CSS utilities; pair with Radix/Headless UI | Bootstrap JS conflicts with React state; react-bootstrap lags | Tailwind |
| Consistency across a large team | Enforced by design tokens + component library we build once | Easy to drift into custom CSS | Tailwind |
| Speed for CRUD-heavy screens | Fast once base components exist | Faster on day 1 only | Tie |

**Verdict:** Tailwind. A HIS is 80% dense tables, forms, and status-driven workflows — we build a small internal component library once (Phase 0/1: `Button`, `Input`, `Select`, `DataTable`, `Modal`, `StatusBadge`, `PageHeader`, `FormSection`) and every later module reuses it. Bootstrap's advantage (prebuilt components) disappears after week one; its disadvantages (override wars, generic look, JS conflicts) last forever.

---

## 3. Why React + .NET + PostgreSQL + EF Core

### React + Vite + TypeScript
- **Ecosystem depth** for exactly what a HIS needs: TanStack Query (server-state caching for queues/worklists that must stay fresh), TanStack Table (server-paginated data grids), React Hook Form (100+ complex forms).
- **TypeScript end-to-end safety:** DTO types generated/mirrored from the API keep 23 modules in sync.
- **Vite:** sub-second HMR keeps a large codebase productive; simple env handling per environment.
- **Hiring & longevity:** React is the safest long-term frontend bet for a multi-year hospital product.

### .NET 10 Web API
- **LTS until late 2028** — right support window for a hospital deployment.
- **Performance:** among the fastest mainstream web stacks; matters for high-frequency endpoints (token queues, bed boards, worklists).
- **Enterprise-grade primitives built in:** DI, configuration, health checks, OpenAPI, rate limiting, output caching, `TimeProvider` for testable time.
- **C# 14 + strong typing** suits domain-heavy code (billing rules, stock ledgers, clinical workflows) far better than dynamic stacks.
- **Clean Architecture is idiomatic** in .NET; the community patterns (MediatR-style handlers or service classes, FluentValidation, EF interceptors) are mature.

### PostgreSQL
- **Zero license cost** at hospital scale (vs SQL Server per-core licensing).
- **Feature fit:** `jsonb` for flexible clinical payloads (assessment forms, vitals templates), table partitioning for audit logs, row-level constraints, `tsvector` full-text search for patient/drug search, materialized views for dashboards.
- **Rock-solid transactional integrity** — essential for billing, stock, and admission flows.
- **Ecosystem:** first-class EF Core provider (Npgsql), pgAdmin, mature backup tooling (pgBackRest, `pg_dump`).

### EF Core 10 (Code First)
- **Migrations as code** — every schema change is reviewed, versioned, and repeatable across dev/staging/prod and future hospital branches.
- **Global query filters** — soft delete (`IsDeleted`) and branch scoping (`BranchId`) enforced in one place, impossible to forget in 500 queries.
- **SaveChanges interceptors** — audit columns and audit log rows populated automatically for every entity.
- **LINQ + compiled queries** for complex reporting; **raw SQL escape hatch** (`FromSqlRaw`, views) where reports need it.
- **Transactions & concurrency:** `ExecutionStrategy`, `xmin` optimistic concurrency tokens on stock/billing rows.

---

## 4. Folder Structure

### Repository root

```
prixo-his/
├── backend/                         # .NET 10 solution
├── frontend/                        # React + Vite app
├── docs/                            # This plan + per-module specs
│   ├── HIS_DEVELOPMENT_PLAN.md
│   ├── api-conventions.md
│   └── modules/                     # One spec per module as it's built
├── docker/
│   ├── docker-compose.yml           # dev: db + api + web + pgadmin
│   ├── docker-compose.prod.yml
│   ├── api.Dockerfile
│   ├── web.Dockerfile
│   └── nginx/
│       └── nginx.conf
├── scripts/                         # DB seed, backup, migration helpers
├── .editorconfig
├── .gitignore
└── README.md
```

### Backend — Clean Architecture (`backend/`)

```
backend/
├── Prixo.sln
├── src/
│   ├── Prixo.Domain/                        # ── No dependencies ──
│   │   ├── Common/
│   │   │   ├── BaseEntity.cs                # Id, audit columns, IsActive, IsDeleted
│   │   │   ├── BranchEntity.cs              # BaseEntity + BranchId
│   │   │   └── ValueObjects/                # Money, DateRange, PersonName...
│   │   ├── Entities/
│   │   │   ├── Admin/                       # User, Role, Permission, Menu...
│   │   │   ├── Masters/                     # Department, Service, Tariff, Bed...
│   │   │   ├── Patients/                    # Patient, PatientContact, Consent...
│   │   │   ├── Appointments/
│   │   │   ├── Opd/
│   │   │   ├── Ipd/
│   │   │   ├── Emergency/
│   │   │   ├── Emr/
│   │   │   ├── Nursing/
│   │   │   ├── Billing/
│   │   │   ├── Lab/
│   │   │   ├── Radiology/
│   │   │   ├── Pharmacy/
│   │   │   ├── Inventory/
│   │   │   ├── Ot/
│   │   │   ├── Icu/
│   │   │   ├── Mrd/
│   │   │   ├── Finance/
│   │   │   ├── Biomedical/
│   │   │   └── Quality/
│   │   ├── Enums/                           # Gender, VisitType, BillStatus...
│   │   └── Exceptions/                      # DomainException, etc.
│   │
│   ├── Prixo.Application/                   # ── Depends on Domain only ──
│   │   ├── Common/
│   │   │   ├── Interfaces/                  # IAppDbContext, ICurrentUser, IDateTime,
│   │   │   │                                #   IRepository<T>, IUnitOfWork, IPdfService,
│   │   │   │                                #   INotificationService (placeholder impl)
│   │   │   ├── Models/                      # ApiResponse<T>, PagedResult<T>, PagedQuery
│   │   │   ├── Behaviors/                   # Validation, logging, transaction pipeline
│   │   │   └── Mappings/
│   │   ├── Features/                        # One folder per module, service-per-feature
│   │   │   ├── Admin/
│   │   │   │   ├── Auth/       (LoginService, DTOs, Validators)
│   │   │   │   ├── Users/      (UserService, UserDto, CreateUserRequest...)
│   │   │   │   ├── Roles/
│   │   │   │   └── Menus/
│   │   │   ├── Masters/
│   │   │   ├── Patients/
│   │   │   └── ... (mirror of Domain/Entities)
│   │   └── DependencyInjection.cs
│   │
│   ├── Prixo.Infrastructure/                # ── Depends on Application ──
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/              # IEntityTypeConfiguration per entity
│   │   │   ├── Interceptors/                # AuditSaveChangesInterceptor
│   │   │   ├── Migrations/
│   │   │   ├── Repositories/                # Generic + specialized repositories
│   │   │   └── Seed/                        # Roles, permissions, menus, demo masters
│   │   ├── Identity/                        # JwtTokenService, PasswordHasher, RefreshTokenService
│   │   ├── Services/                        # PdfService, FileStorageService, SequenceService (UHID/IP No)
│   │   ├── BackgroundJobs/                  # Hangfire jobs
│   │   └── DependencyInjection.cs
│   │
│   └── Prixo.Api/                           # ── Composition root ──
│       ├── Controllers/                     # Thin: parse → service → ApiResponse
│       │   ├── Admin/
│       │   ├── Masters/
│       │   └── ... (per module)
│       ├── Middleware/                      # ExceptionHandling, AuditContext, RequestLogging
│       ├── Filters/                         # PermissionAuthorizeAttribute
│       ├── Extensions/
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Program.cs
│
└── tests/
    ├── Prixo.Domain.Tests/
    ├── Prixo.Application.Tests/             # Service tests w/ in-memory or Testcontainers
    └── Prixo.Api.IntegrationTests/          # WebApplicationFactory + Testcontainers-PostgreSQL
```

### Frontend (`frontend/`)

```
frontend/
├── index.html
├── vite.config.ts
├── tailwind.config.ts
├── tsconfig.json
├── .env.development / .env.production
└── src/
    ├── main.tsx
    ├── App.tsx                          # Providers: QueryClient, Router, Auth, Toast
    ├── api/
    │   ├── axios.ts                     # Base instance + JWT/refresh interceptors
    │   ├── endpoints.ts                 # Central URL constants
    │   └── types/                       # DTO types mirroring backend
    ├── auth/
    │   ├── AuthProvider.tsx             # Token storage, user, permissions
    │   ├── RequireAuth.tsx              # Route guard
    │   └── RequirePermission.tsx        # Component/route-level permission guard
    ├── components/                      # Shared UI kit (built Phase 0–1)
    │   ├── ui/                          # Button, Input, Select, Modal, Tabs, Badge...
    │   ├── data-table/                  # Server-paginated DataTable
    │   ├── forms/                       # FormField, DatePicker, SearchSelect (master lookups)
    │   └── layout/                      # AppShell, Sidebar (permission-driven menu), Topbar
    ├── hooks/                           # useDebounce, usePagination, usePermissions...
    ├── lib/                             # formatters, validators, print utils
    ├── features/                        # One folder per module — mirrors backend
    │   ├── admin/       (users, roles, permissions, menus, audit-log)
    │   ├── masters/
    │   ├── patients/
    │   ├── appointments/
    │   ├── opd/
    │   ├── billing/
    │   ├── lab/
    │   ├── pharmacy/
    │   ├── ipd/
    │   ├── nursing/
    │   ├── emr/
    │   ├── radiology/
    │   ├── inventory/
    │   ├── ot/
    │   ├── icu/
    │   ├── emergency/
    │   ├── mrd/
    │   ├── finance/
    │   ├── biomedical/
    │   ├── quality/
    │   ├── patient-portal/
    │   ├── doctor-portal/
    │   └── dashboards/
    │       └── (each feature: pages/, components/, api.ts, hooks.ts, types.ts)
    └── routes/
        └── index.tsx                    # Lazy route tree grouped by module
```

**Convention:** every feature folder is self-contained (`pages`, `components`, `api.ts` with TanStack Query hooks, `types.ts`). Cross-feature imports only via `components/` and `lib/`.

---

## 5. Backend Architecture

### Layering (Clean Architecture)

```
   ┌──────────────────────────────────────────────┐
   │ Prixo.Api          Controllers, Middleware    │  HTTP concerns only
   ├──────────────────────────────────────────────┤
   │ Prixo.Infrastructure  EF Core, JWT, PDF, Jobs │  Implements Application interfaces
   ├──────────────────────────────────────────────┤
   │ Prixo.Application   Services, DTOs, Validators│  Use cases, business orchestration
   ├──────────────────────────────────────────────┤
   │ Prixo.Domain        Entities, Enums, Rules    │  Zero dependencies
   └──────────────────────────────────────────────┘
```

**Dependency rule:** references point inward only. Domain knows nothing about EF Core; Application defines interfaces (`IAppDbContext`, `IRepository<T>`, `ICurrentUser`) that Infrastructure implements.

### Patterns in use

- **Repository + Unit of Work — selectively.** Generic `IRepository<T>` for masters/CRUD. Complex modules (Billing, Pharmacy stock, IPD) get dedicated repositories with intention-revealing methods (`GetOpenBillWithItemsAsync`, `GetBatchesFefoAsync`). Simple queries in services may use `IAppDbContext` directly — no ceremony for ceremony's sake.
- **Service layer:** one service per feature (`PatientService`, `LabOrderService`). Controllers never touch DbContext.
- **DTO in / DTO out:** entities never cross the API boundary. FluentValidation validator per request DTO, executed automatically.
- **Result & response wrapper:** every endpoint returns `ApiResponse<T>`:

```json
{
  "success": true,
  "message": "Patient registered",
  "data": { ... },
  "errors": null,
  "traceId": "0HN..."
}
```

Paged lists return `ApiResponse<PagedResult<T>>` with `items, page, pageSize, totalCount, totalPages`.

- **Global exception middleware:** maps `NotFoundException → 404`, `ValidationException → 422`, `ForbiddenException → 403`, `DomainException → 400` (message shown), anything else → 500 with traceId (details logged, never leaked).
- **Transactions:** an explicit `IUnitOfWork.ExecuteInTransactionAsync(...)` wraps multi-table flows — **mandatory** for: billing (bill + items + receipt + ledger), pharmacy sale/issue (bill + batch stock decrement + stock ledger), GRN, admission (admission + bed status + deposit), discharge (clearances + bill close + bed release), stock transfer/adjustment, payments/refunds, day close.
- **Concurrency:** PostgreSQL `xmin` as optimistic concurrency token on `Bed`, `StockBatch`, `Bill`, `Appointment` slots. Bed allocation and batch decrement additionally use `SELECT ... FOR UPDATE` (via `FromSqlRaw`) or serialized retry.
- **Sequences:** `SequenceService` generates UHID, IP No, Bill No, Sample No, GRN No per **branch + year** using PostgreSQL sequences/`ON CONFLICT` counters — atomic, gap-tolerant, prefix-configurable (e.g., `PRX-2026-000123`).
- **Background jobs (Hangfire):** near-expiry drug scan (daily), minimum-stock alert, appointment no-show marking, TAT metric rollups, audit-log partition maintenance, report cache refresh.
- **Health & observability:** `/health` (liveness) + `/health/ready` (DB check); Serilog structured logs with `TraceId`, `UserId`, `BranchId` enrichment.

---

## 6. Frontend Architecture

### State strategy

| Kind of state | Tool |
|---|---|
| Server data (patients, queues, bills) | TanStack Query — query keys namespaced per module: `['opd','queue',{doctorId,date}]` |
| Auth/session (user, permissions, branch) | React Context (`AuthProvider`) + localStorage (refresh token in httpOnly cookie preferred; see §8) |
| Form state | React Hook Form + Zod resolvers |
| UI state (modals, filters) | Local component state / URL search params |

**No Redux.** TanStack Query + Context covers a HIS cleanly.

### Data-fetching conventions

- Every feature exposes typed hooks: `usePatients(query)`, `useCreatePatient()`.
- **Mutations invalidate precisely:** registering a patient invalidates `['patients','list']`; completing a consult invalidates the doctor queue.
- **Polling for live boards:** token queue, bed board, lab worklist use `refetchInterval` (10–30 s). (SignalR is a later optimization — the hook API doesn't change.)
- **Optimistic updates** only for low-risk toggles (mark sample collected); never for billing.

### Routing & guarding

```
/login
/app                              ← AppShell (sidebar from menu API)
  /dashboard
  /admin/users /admin/roles /admin/menus /admin/audit-log ...
  /masters/*                      ← generic master CRUD framework
  /patients /patients/new /patients/:id
  /appointments /appointments/schedule
  /opd/desk /opd/queue /opd/consult/:visitId
  /ipd/admissions /ipd/bed-board /ipd/patient/:admissionId/*
  /emergency/*
  /billing/op /billing/ip/:admissionId /billing/day-close
  /lab/orders /lab/collection /lab/worklist /lab/validation
  /radiology/*
  /pharmacy/pos /pharmacy/purchase/* /pharmacy/stock/*
  /inventory/*  /ot/*  /icu/*  /nursing/*  /mrd/*  /finance/*
  /biomedical/*  /quality/*  /reports/*
/portal                           ← Patient portal (separate layout & auth audience)
/doctor                           ← Doctor portal layout
```

- `RequireAuth` guards `/app`; `RequirePermission("PHARMACY.SALE.CREATE")` guards routes **and** buttons.
- **Menu is data-driven:** sidebar renders from `/api/menus/my` (permission-filtered on the server). New modules appear by seeding menu rows — no frontend release needed for menu changes.
- Route-level `React.lazy` per module keeps the initial bundle small.

### UI kit (built once, Phase 0–1)

`AppShell`, `PageHeader` (title + breadcrumb + actions), `DataTable` (server pagination/sort/filter/export), `SearchSelect` (debounced master lookup — used hundreds of times), `PatientBanner` (UHID, name, age/sex, allergies — pinned on every clinical screen), `StatusBadge`, `ConfirmDialog`, `PrintLayout` (registration card, bills, reports), `StatCard`, `FormSection`.

---

## 7. Database Design Strategy

### Conventions

- **Naming:** `snake_case` tables/columns via Npgsql naming convention; PostgreSQL schemas per domain: `admin`, `master`, `patient`, `opd`, `ipd`, `emr`, `billing`, `lab`, `rad`, `pharmacy`, `inventory`, `ot`, `icu`, `nursing`, `mrd`, `finance`, `biomed`, `quality`, `audit`.
- **Primary keys:** `bigint identity` for high-volume transactional tables; business identifiers (UHID, BillNo) are separate unique columns — never the PK.
- **Mandatory audit columns on every business table:**

```sql
id            bigint / identity PK
created_at    timestamptz NOT NULL
created_by    bigint NOT NULL REFERENCES admin.users(id)
updated_at    timestamptz NULL
updated_by    bigint NULL
is_active     boolean NOT NULL DEFAULT true
is_deleted    boolean NOT NULL DEFAULT false
```

Branch-scoped tables add `branch_id bigint NOT NULL REFERENCES admin.branches(id)`.

- **Soft delete:** global EF query filter `!IsDeleted`; unique indexes are **partial**: `UNIQUE (branch_id, code) WHERE is_deleted = false`.
- **Foreign keys always**, `ON DELETE RESTRICT` (business data must never cascade away).
- **Check constraints:** non-negative stock/amounts, valid date ranges (`discharge_at >= admitted_at`), enum value guards.
- **Money:** `numeric(18,2)`; quantities `numeric(18,3)`; rates `numeric(18,4)`.
- **Time:** `timestamptz` everywhere; app operates in hospital-local timezone, stores UTC.

### Indexing plan

- FK columns: always indexed.
- Hot search paths: `patient(uhid)`, `patient(mobile)`, trigram/`tsvector` index on patient name; `bill(bill_no)`, `lab_sample(barcode)`.
- Worklist queries: composite indexes such as `appointment(branch_id, doctor_id, appointment_date, status)`, `lab_order_item(status, ordered_at)`, `bed(branch_id, ward_id, status)`.
- `audit.audit_logs` and `audit.activity_logs`: **monthly range partitions** on `created_at`; index `(entity_name, entity_id)` and `(user_id, created_at)`.
- Dashboards read from **materialized views** (`mv_daily_collection`, `mv_bed_occupancy`) refreshed by Hangfire — heavy aggregates never hit OLTP tables directly.

### Migration discipline

- One migration per feature PR, named `YYYYMMDD_<Module>_<Change>`.
- Migrations applied by CI/CD (`dotnet ef database update` or migration bundle) — **never** `EnsureCreated`, never auto-migrate on app start in production.
- Seed data (roles, permissions, menus, units, sample masters) is idempotent code in `Infrastructure/Persistence/Seed`, keyed by stable codes.

---

## 8. Authentication and RBAC Plan

### Authentication flow

1. `POST /api/auth/login` (username/password + branch selection) → validates password (**ASP.NET Identity `PasswordHasher` / PBKDF2**; account lockout after N failures).
2. Issues **access token** (JWT, 15 min) + **refresh token** (opaque 256-bit random, 7 days, stored **hashed** in `admin.refresh_tokens` with device info).
3. Access token claims: `sub` (userId), `branch`, `name`, `roles`, `sid` (sessionId). **Permissions are NOT in the JWT** (too many); they're fetched at login (`/api/auth/me`) and cached server-side per role version.
4. `POST /api/auth/refresh` → **rotation**: old refresh token revoked, new pair issued; reuse of a revoked token revokes the whole session family (theft detection).
5. `POST /api/auth/logout` → revokes refresh token + audit event.
6. Frontend: access token in memory; Axios interceptor auto-refreshes on 401 and replays the failed request; refresh token in httpOnly secure cookie (fallback: localStorage for LAN deployments without TLS — configurable).

### Password & session policy (configurable via System Configuration)

- Min length 8, complexity rules, expiry days, history (no reuse of last 5), forced change on first login.
- Session management: active-sessions list per user, admin force-logout, max concurrent sessions, idle timeout.

### RBAC model — permission-based (roles are just permission bundles)

```
users ──< user_roles >── roles ──< role_permissions >── permissions
users ──< user_branches >── branches          (a user may work at multiple branches)
menus ── permission_code                      (menu visibility = permission)
```

- **Permission code format:** `MODULE.RESOURCE.ACTION` — e.g. `PATIENT.REGISTRATION.CREATE`, `BILLING.DISCOUNT.APPROVE`, `LAB.RESULT.VERIFY`, `PHARMACY.STOCK.ADJUST`.
- Permissions are **seeded from code** (a static registry class) so backend checks, seed data, and frontend constants can't drift.
- **API enforcement:** `[HasPermission("LAB.RESULT.APPROVE")]` attribute → policy-based authorization handler → checks user's effective permission set (cached, invalidated when roles change).
- **Frontend enforcement:** `usePermissions()` + `<RequirePermission>` hides menus, routes, and buttons — cosmetic only; the API is the real gate.
- **Seeded roles:** Super Admin, Hospital Admin, Reception, Appointment Desk, Doctor, Nurse, Head Nurse, Lab Technician, Pathologist, Radiology Technician, Radiologist, Pharmacist, Pharmacy Purchase, Store Keeper, Billing Executive, Billing Supervisor, Cashier, Finance/Accounts, MRD Officer, Biomedical Engineer, Quality Manager, OT Coordinator, ICU Incharge, Management (read-only dashboards), Patient (portal), Doctor-Portal.
- **Data-level scoping:** every request carries `BranchId`; doctors see their own queues; portal users see only their own records (enforced in services, not just UI).

---

## 9. Audit Log Strategy

Two complementary logs, both in schema `audit`, both **partitioned monthly**:

### 1. Entity audit log (automatic — EF Core `SaveChangesInterceptor`)

Captures every `INSERT/UPDATE/DELETE` on business entities:

| Column | Content |
|---|---|
| `id, created_at` | when |
| `user_id, branch_id, ip_address, trace_id` | who/where (from `ICurrentUser` populated by middleware) |
| `entity_name, entity_id` | what |
| `action` | Created / Updated / Deleted (soft) |
| `old_values, new_values` | `jsonb` — **changed columns only**; sensitive fields (password hashes) excluded |

- Interceptor also stamps `CreatedAt/CreatedBy/UpdatedAt/UpdatedBy` and converts hard deletes to soft deletes for `ISoftDelete` entities.
- Read UI: Admin → Audit Log with filters (user, entity, date, action) + per-record "History" tab on critical screens (bill, patient, prescription).

### 2. Activity log (explicit — business events)

Service-level `IActivityLogger.LogAsync(...)` for events that matter beyond row diffs: login/logout/failed login, password change, permission change, bill cancellation + reason, discount approval, patient merge, record file issue, result approval, stock adjustment reason, day close, report export/print of clinical documents.

### Rules

- Audit writes happen **inside the same transaction** as the business change — a bill can't exist without its audit row.
- Audit tables are **append-only**: no UPDATE/DELETE grants for the app role.
- Retention: 7 years (NABH/legal); old partitions compressed/archived by job.
- Every create/update/delete/login/logout is audited — this satisfies the security rule set and NABH evidence requirements.

---

## 10. Multi-branch / Multi-hospital-unit Strategy

**Model: single database, shared schema, `branch_id` discriminator column.** (Simplest to operate, easy cross-branch reporting, one migration path. Database-per-tenant is overkill for branches of one hospital group and would multiply ops cost.)

- `admin.branches` — hospital units (code, name, address, GST no, logo, report header, timezone, sequence prefixes).
- **Branch-scoped:** all transactional data (visits, admissions, bills, stock, orders) + operational masters (wards, beds, rooms, OT rooms, tariffs — tariffs can differ per branch).
- **Global (not branch-scoped):** patient identity (UHID is org-wide — one patient, visible history across branches), users, roles/permissions, ICD codes, drug/generic masters, service catalog (branch tariff maps service → branch price).
- **Enforcement:** EF **global query filter** `e.BranchId == _currentUser.BranchId` on `BranchEntity`; `ICurrentUser.BranchId` comes from the JWT. Cross-branch queries (management dashboards, patient history) use explicit `IgnoreQueryFilters()` guarded by `REPORT.CROSSBRANCH.VIEW` permission.
- **Branch switch:** users mapped to multiple branches pick one at login (or switch → new token issued). All sequences (UHID excepted) are per-branch.
- **Stock is strictly per branch + store location;** inter-branch stock transfer is a first-class documented flow (transfer out → in-transit → receive).

---

## 11. Module-wise Development Phases

> Held for later (see §21): Blood Bank, Dialysis, Physiotherapy, Dental, Insurance/TPA, HR & Payroll, Facility/Maintenance, Ambulance, Kitchen/Dietary, Infection Control, Mobile Apps, Communication, Integrations, AI Modules.
> Items marked **(placeholder)** get schema + disabled UI now, real implementation later.

### Phase 0 — Initial Project Setup (Foundation)

- Repository structure (monorepo as in §4), `.editorconfig`, branch strategy (trunk + feature branches, PR reviews)
- Backend: .NET 10 solution — Domain / Application / Infrastructure / Api projects wired
- `BaseEntity`, `BranchEntity`, `ICurrentUser`, `ApiResponse<T>`, `PagedResult<T>`
- Global exception handling middleware + ProblemDetails mapping
- Serilog structured logging (console + rolling file), request logging with TraceId
- EF Core 10 + Npgsql + initial `AppDbContext` + first migration; snake_case convention
- Audit `SaveChangesInterceptor` (stamps audit columns; full audit rows land in Phase 1)
- Health checks `/health`, `/health/ready`; OpenAPI + Scalar UI
- Frontend: Vite + React + TS strict + Tailwind + Router + TanStack Query + Axios instance
- UI kit v1: AppShell, Button, Input, Select, Modal, DataTable, PageHeader, Toast
- PostgreSQL 16 via Docker Compose (+ pgAdmin); environment configuration (.env / appsettings per env, user-secrets in dev)
- CI skeleton: build + test + lint on PR

### Phase 1 — System Administration + RBAC

- Login / Logout, JWT + refresh token rotation, `/auth/me`
- User Management (CRUD, activate/deactivate, reset password, branch mapping, photo)
- Role Management, Permission Management (registry-seeded, role-permission matrix UI)
- Menu Management (tree, icons, permission mapping, ordering) + permission-driven sidebar
- **Branch / Hospital Unit Setup** (multi-branch foundation — must exist before masters)
- Designation Setup; System Configuration (key-value settings UI: password policy, sequence prefixes, hospital header)
- Password Policy enforcement + first-login change + lockout
- Session Management (active sessions, force logout, idle timeout)
- Audit Log (entity audit + viewer UI), Activity Log (login/logout etc.)
- Data Backup Settings (config + documented `pg_dump` job), Notification Settings **(placeholder)**

### Phase 2 — Master Management

**Status:** Complete (2026-07-10). Generic master framework, all catalogue entities, APIs, FE screens, permissions, and menu entries shipped. Bulk CSV import/export for generic masters deferred (ICD-10 CSV import implemented).

Generic master framework (one reusable CRUD pattern: list + search + form + import/export) covering:

- Patient Category, Doctor Master (linked to User, specialties, consultation fees, schedule defaults, registration no), Employee Master (lightweight — full HR later)
- Department, Specialty, Designation
- Service Master (code, department, category, taxable), Tariff Master (per branch + patient category price lists, effective dates), Package Master (services bundle, price, validity)
- Building, Floor, Room Type, Ward, Room, Bed (hierarchy: building → floor → ward → room → bed)
- Vendor Master, Item Master (stores), Drug Master (with Generic, Manufacturer — full pharmacy masters deepen in Phase 8)
- Tax Master (GST slabs), Discount Master (types, limits, approval thresholds), Referral Master (internal/external referrers)
- Diagnosis / ICD-10 Master (import from CSV), Corporate Master **(schema now, billing use later)**, Insurance/TPA Master **(placeholder — held module)**
- Unit of Measure, Sample Type, Modality (needed by later phases; created here)

### Phase 3 — Patient Registration

- New Patient Registration (demographics, contact, guardian/attender, category, referral)
- **UHID Generation** (org-wide, configurable format `PRX-YYYY-NNNNNN`)
- Existing Patient Search (UHID / name / mobile / DOB — indexed, debounced)
- Aadhaar / ABHA **(placeholder fields + verify-button disabled)**; Patient Photo Capture **(placeholder: file upload now, webcam later)**
- Duplicate Detection (name+DOB+mobile fuzzy match warning at save) & Patient Merge (survivor record, all FKs re-pointed in transaction, merge audit)
- Consent Capture (consent templates master, signed consent records, file attachment)
- Registration Card Printing (PDF, barcode/QR of UHID)

### Phase 4 — Appointment Management

- Doctor Schedule (weekly template + date overrides, slot duration, leave/block)
- Appointment Booking (slot picker), Walk-in Appointment, Reschedule, Cancel (reason-coded)
- Doctor Availability view; Token Generation (per doctor per session)
- Queue Display (public screen route, auto-refresh)
- No-show tracking (auto-mark job); Appointment Reminder **(placeholder: outbox table `notification_queue`, no sender)**
- Online Appointment — deferred to Patient Portal phase (reuses this engine)

### Phase 5 — OPD Module

- OPD Front Desk: OP Visit Registration (from appointment or direct), doctor/department selection, consultation type, visit type New/Review (auto-detected by revisit window), token, OP visit slip print, **OP billing link** (visit auto-creates draft consultation charge)
- Patient Queue (front desk + doctor views, statuses: waiting → in-consult → done)
- Doctor Consultation screen (single-page clinical workspace with PatientBanner):
  - Patient Clinical Summary (past visits, active meds, allergies)
  - Vitals, Chief Complaints, History Taking, Examination Notes
  - Diagnosis + ICD Coding (SearchSelect over ICD master)
  - Prescription (drug search, dose/frequency/duration/instructions, favorites/templates per doctor)
  - Lab Orders, Radiology Orders, Procedure Orders (order → lands in respective module worklists)
  - Referral (internal doctor / external), Follow-up Advice (next visit date → pre-books slot optionally)
- Visit summary print

### Phase 6 — Billing Module

- **OP Billing:** consultation/service/lab/radiology/procedure/package bills; auto-pull of ordered items; multi-item bill; receipt print
- Payments: cash/card/UPI (modes master); Receipt & Refund with approval
- **IP Billing:** Advance/deposit collection, running bill (auto-feed from bed charges job, orders, pharmacy issues, OT, procedures, doctor visit charges, nursing charges, consumables), Interim Bill, Final Bill, Discharge Bill
- Billing Controls: Discount (line/bill level, threshold-based approval workflow), Credit Billing, Corporate Billing **(placeholder — corporate master exists, settlement later)**, Insurance Billing **(placeholder — held)**, Package mapping, Rate Revision (new tariff versions with effective dates), Bill Cancellation (reason + approval + audit), Refund Approval
- Daily Collection report; **Day Close** (cashier-wise declaration vs system, locks the day, transaction)
- All money flows in DB transactions; bill numbering per branch/year

### Phase 7 — Laboratory / LIS

- Masters: Lab Test (department, method, specimen, units, reference ranges by age/sex, critical limits), Test Profile (panels), Sample Type
- Lab Order (from OPD/IPD/Emergency or direct walk-in with billing check)
- Barcode Generation (sample no), Sample Collection, Acknowledgement (receipt in lab), Rejection (reason-coded, recollect flow)
- Worklist (department-wise, status-driven), Result Entry (numeric/text/dropdown, auto-flag H/L vs reference range)
- Result Validation → Verification → Approval (role-gated three-step; configurable to two)
- Critical Value Alert (in-app notification to ordering doctor + acknowledgment log)
- Lab Report PDF (letterhead, cumulative view), Digital Signature **(placeholder: signature image; DSC later)**
- Lab TAT Report (order→collect→result→approve timestamps); Analyzer Integration **(placeholder — interface `IAnalyzerInbound` defined, held)**
- WhatsApp/Email report sharing **(placeholder — notification outbox)**

### Phase 8 — Pharmacy

- Masters: Drug (deep: generic, form, strength, schedule H/H1/X flag, HSN, GST), Generic, Manufacturer, Supplier, Rack/Location; Batch (created via GRN)
- Purchase: Purchase Request → Purchase Order (approval) → **GRN** (batch, expiry, MRP, purchase rate, free qty, tax → stock in, transaction) → Purchase Return → Supplier Invoice tracking
- Sales/Issue: **OP Pharmacy Sale (POS screen: barcode/name search, FEFO batch suggestion, prescription link, GST invoice)**, IP Medicine Issue (against medication orders), Emergency Issue, Department Issue, Medicine Return (IP), Sales Return — all transactional with stock ledger entries
- Inventory: Batch & Expiry tracking, Stock Adjustment (reason + approval), Stock Transfer (store↔store, branch↔branch), Stock Audit (count sheets, variance posting)
- Alerts: Near Expiry, Minimum Stock (Hangfire + dashboard widgets)
- Drug Consumption Report; every stock movement writes an immutable `stock_ledger` row (running balance reconstructable)

### Phase 9 — IPD Module

- Admission: Request → IP Number Generation → Bed Allocation (ward/room/bed picker from live bed board) → Consultant Mapping (primary + secondary) → Admission Consent → Initial Deposit (billing link) → Admission Slip
- Ward/Bed Management: Bed Status board (vacant/occupied/reserved/cleaning/blocked), Bed/Ward/Room Transfer (with bed-charge cutover timestamping), Consultant Transfer, Attender Details, Bed Occupancy Dashboard
- Inpatient Clinical: Daily Progress Notes, Doctor Orders (unified order sheet: medication/lab/radiology/diet **(diet placeholder)**/nursing instructions), Vitals Chart, Intake/Output Chart, Procedure Notes — nursing-side entries deepen in Phase 10
- Discharge: Request → clearance checklist (**Billing, Pharmacy (medicine return), Nursing, Final Investigation**) → Discharge Summary (structured: diagnosis, course, meds on discharge, follow-up; doctor-approved) → Final Discharge (bed release, transaction) → Discharge Slip
- LAMA / DAMA / death / referral discharge types; Death summary variant

### Phase 10 — Nursing Module

- Nursing Assessment (admission assessment, structured `jsonb` form templates)
- Nursing Notes (shift-tagged), Vitals Entry (feeds charts), Intake/Output Chart
- **Medication Administration Record (MAR):** due-time schedule generated from medication orders; administer/skip/refuse with reason; double-verification flag for high-alert drugs
- Pain / Fall Risk (Morse) / Bed Sore (Braden) assessments with score auto-calc
- Nursing Care Plan; Shift Handover (SBAR-style, per ward)
- Incident Reporting (links to Quality module later), Patient Transfer Notes

### Phase 11 — EMR / Clinical Records

- **Patient Timeline** (unified chronological view: visits, admissions, orders, results, prescriptions, procedures, documents — aggregation over existing data)
- Histories: Medical, Surgical, Family, Allergy (**drives allergy banner + prescription warning**), Immunization
- Clinical Notes (ad-hoc, specialty templates), Diagnosis registry (problem list: active/resolved)
- Prescription / Investigation / Procedure History views; Discharge Summaries archive
- Attachments (external reports, scanned documents — file storage service, virus-scan hook placeholder)
- Consent Documents repository

### Phase 12 — Radiology / RIS

- Masters: Radiology Test (modality, body part, contrast flag, prep instructions), Modality
- Radiology Order (from OPD/IPD/ER/direct) → Appointment/Slot Scheduling per modality → Technician Worklist → Image Capture Status → Radiologist Reporting (structured templates + rich text) → Verification → Approval → **Radiology PDF Report**
- Contrast Consent capture; Radiology TAT Report
- PACS / DICOM **(placeholder only: `accession_number` generated, `pacs_study_uid` column, viewer-URL config key)**

### Phase 13 — Stores & Inventory (General)

- Reuses pharmacy inventory engine with store-type separation (general store, linen, stationery, consumables, biomedical spares)
- Item & Category Masters, Vendor (shared), Purchase Request → PO → GRN
- Stock Issue / Department Issue (indent → approve → issue), Stock Return, Stock Transfer, Stock Adjustment, Stock Verification (periodic count)
- Consumption Report (department-wise), Reorder Level Alert

### Phase 14 — Operation Theatre

- OT Master (theatres, equipment), OT Booking & Surgery Scheduling (calendar, conflict check with surgeon/anaesthetist/theatre)
- Surgeon & Anaesthetist Assignment, team roles
- Pre-operative Checklist (WHO surgical safety checklist template), Consent Verification
- Anaesthesia Notes (pre-anaesthesia assessment, intra-op record), Surgery Notes (operative notes template)
- Implant Usage (batch/serial tracked → billing), Consumable Usage (→ stock decrement + billing)
- OT Billing (surgeon fee, anaesthesia, theatre time slabs, implants, consumables → IP bill)
- Post-operative Notes / recovery; OT Register (statutory register report)

### Phase 15 — ICU / Critical Care

- ICU Admission / transfer-in, ICU Bed Allocation (ICU-flagged wards)
- Vitals Monitoring (high-frequency vitals grid, hourly), Ventilator Chart (settings/observations), Infusion Chart (rate, titration)
- ICU Nursing Notes, Doctor Rounds notes, Medication Chart (ICU MAR view)
- Critical Alert (threshold rules on vitals → in-app alert), ICU Dashboard (census, ventilated count, severity)
- Device Integration **(placeholder only — `device_reading` table + `IDeviceInbound` interface)**

### Phase 16 — Emergency / Casualty

- Emergency Registration (rapid, minimal fields, unknown-patient support with later merge)
- **Triage** (ESI/color levels, triage notes, auto-priority in queue), Emergency Token
- Emergency Doctor Assessment (fast clinical sheet), Emergency Lab/Radiology Orders (STAT priority through existing engines)
- **MLC Register** (medico-legal case: police station, informed time, injury details — statutory report), Accident Case Register
- Emergency Billing (post-facto friendly), Observation Ward (short-stay beds)
- Admission Conversion (ER → IPD carrying clinical data), Referral / Transfer Out (with transfer summary)

### Phase 17 — MRD

- Medical Record File Creation (physical file no per admission), File Tracking (location/custodian), File Issue & Return (request → approve → issue → return, overdue alerts)
- Document Scanning (upload against admission, document-type master)
- ICD Coding (coder worklist for discharged cases, coding completeness)
- Discharge File Audit (checklist: summary signed, consents present...), Missing Document Tracking
- Record Retention (policy per record type, destruction register), Record Request (internal/external/legal)
- MRD Reports (coding stats, file movement, incomplete records)

### Phase 18 — Finance & Accounts

- Cash & Bank Collection views (from billing), centralized **Day Close** (all counters)
- Chart of Accounts + Ledger; Journal / Payment / Receipt Vouchers
- Payables (supplier invoices from GRN → payment), Receivables (credit/corporate outstanding)
- GST / Tax Report (outward register from bills, HSN summary), Revenue Report (department/doctor-wise), Outstanding Report
- Posting rules: billing/pharmacy/GRN events post to ledger via internal event handlers (double-entry, transactional)

### Phase 19 — Biomedical Engineering

- Equipment Master (serial, model, criticality, department/location), Equipment Location tracking
- AMC / CMC contracts (vendor, dates, renewal alerts), Warranty Tracking
- Preventive Maintenance (schedule templates → auto work orders), Breakdown Complaint (raise → assign → resolve, downtime capture)
- Calibration (due tracking, certificates), Service History, Spare Usage (links Stores)
- Equipment Downtime Report, PM compliance report

### Phase 20 — Quality / NABH

- Quality Indicators master (numerator/denominator definitions) + monthly data entry / auto-pull where computable (TAT, occupancy, incidents)
- KPI Monitoring dashboard, Audit Checklist (templates, scheduled audits, scoring)
- Incident Reporting (org-wide intake — merges nursing incidents; severity, RCA), Non-conformance, **CAPA** tracking (action, owner, due, verification)
- Risk Register, Committee Meeting Records (minutes, action items)
- NABH Evidence Upload (chapter/standard-wise document repository), Compliance Dashboard

### Phase 21 — Patient Portal

- Patient Login (mobile + OTP **(placeholder: password first, OTP when SMS integration lands)**; separate JWT audience)
- Appointment Booking (public slot view → book/cancel), My Appointments
- Lab Reports & Radiology Reports (approved-only PDFs), Prescriptions, Bills & receipts, Discharge Summaries, Medical History (portal-safe subset)
- Payment **(placeholder — gateway held)**, Feedback (visit-linked ratings → Quality module)

### Phase 22 — Doctor Portal

- Doctor Login (same SSO, doctor layout), personal Dashboard (today's OP count, IP census, pending signs, critical alerts)
- Patient Queue, OP Consultation (reuses Phase 5 workspace), IP Rounds (my inpatients, quick progress notes + orders)
- Orders & Results View (cumulative lab trends), Prescription, Discharge Summary Approval (e-sign queue)

### Phase 23 — Reports & Dashboards

- Management Dashboard (revenue, census, OP/IP mix, alerts) — cross-branch with permission
- OP / IP / Billing / Revenue dashboards; Doctor-wise & Department-wise reports
- Patient Census, Bed Occupancy, Lab TAT, Radiology TAT, Pharmacy Stock, Daily Collection, Audit Report
- **Custom Report Builder** (v1: saved parameterized queries over curated reporting views, column picker, export CSV/PDF/Excel)
- Report subscription **(placeholder: schedule + outbox)**

---

## 12. Exact Build Order

```
Phase 0  Project Setup            ← everything depends on it
Phase 1  Admin + RBAC             ← auth, users, permissions, menus, audit
Phase 2  Masters                  ← all reference data
Phase 3  Patient Registration     ← UHID: the spine of the system
Phase 4  Appointments             ← needs doctors + patients
Phase 5  OPD                      ← needs appointments; creates orders
Phase 6  Billing                  ← OPD generates charges; deposits needed before IPD
Phase 7  Laboratory               ← consumes OPD orders; feeds billing
Phase 8  Pharmacy                 ← consumes prescriptions; feeds billing; stock engine
Phase 9  IPD                      ← needs billing (deposits), pharmacy (issues), lab
Phase 10 Nursing                  ← needs IPD (MAR needs medication orders)
Phase 11 EMR                      ← aggregates everything built so far
Phase 12 Radiology                ← same order pattern as lab
Phase 13 Stores & Inventory       ← generalizes pharmacy stock engine
Phase 14 Operation Theatre        ← needs IPD + stock + billing
Phase 15 ICU                      ← extends IPD + nursing
Phase 16 Emergency                ← reuses OPD/IPD/lab/billing engines (fast because of reuse)
Phase 17 MRD                      ← needs discharges to exist
Phase 18 Finance & Accounts       ← needs billing/pharmacy/stores volume
Phase 19 Biomedical               ← independent; slots here
Phase 20 Quality / NABH           ← aggregates incidents/indicators from all modules
Phase 21 Patient Portal           ← exposes existing data safely
Phase 22 Doctor Portal            ← recomposes existing features
Phase 23 Reports & Dashboards     ← final layer over everything (widgets shipped incrementally earlier)
```

**Dependency rules that must not be violated:**
- Nothing before Phase 1 auth is merged to main unauthenticated.
- No module screen ships without its permissions seeded and enforced.
- Billing (6) before IPD (9): admission requires deposits.
- Pharmacy (8) before IPD (9): IP medicine issue is part of IPD flow.
- Emergency (16) intentionally after IPD/Lab/Billing so it reuses their engines — ER is a workflow over existing services, not new plumbing.
- Dashboards: each phase delivers its own KPIs; Phase 23 composes them.

---

## 13. API Planning

### Conventions

- Base: `/api/v1/...`; versioning via URL segment.
- REST resource naming, kebab/plural: `/api/v1/patients`, `/api/v1/lab/orders`.
- Verbs: `GET` list/detail, `POST` create + workflow actions (`/bills/{id}/cancel`), `PUT` full update, `PATCH` rare, `DELETE` = soft delete.
- Lists: `?page=1&pageSize=20&sort=name&order=asc&search=...&filters...` → `PagedResult<T>`.
- Workflow state changes are **explicit action endpoints** (never generic status PATCH): `/lab/results/{id}/verify`, `/admissions/{id}/discharge`.
- Every endpoint: `[Authorize]` + `[HasPermission]`; anonymous only `POST /auth/login`, `POST /auth/refresh`, portal registration, `/health`.
- Idempotency keys on payment/receipt POSTs.

### Representative endpoint map (per module — full list lives in `docs/modules/`)

| Module | Key endpoints |
|---|---|
| Auth | `POST /auth/login` `POST /auth/refresh` `POST /auth/logout` `GET /auth/me` `POST /auth/change-password` |
| Admin | `/users` `/roles` `/roles/{id}/permissions` `/permissions` `/menus` `/menus/my` `/branches` `/settings` `/audit-logs` `/activity-logs` `/sessions` |
| Masters | `/masters/departments` `/masters/services` `/masters/tariffs` `/masters/packages` `/masters/wards` `/masters/beds` `/masters/icd` `/masters/doctors` ... (uniform CRUD contract) |
| Patients | `GET /patients?search=` `POST /patients` `GET /patients/{id}` `PUT /patients/{id}` `POST /patients/{id}/merge` `GET /patients/{id}/duplicates` `POST /patients/{id}/consents` `GET /patients/{id}/card` (PDF) |
| Appointments | `/doctors/{id}/schedule` `/appointments/slots?doctorId&date` `POST /appointments` `POST /appointments/{id}/reschedule` `POST /appointments/{id}/cancel` `POST /appointments/{id}/check-in` `GET /queue/display?deptId` |
| OPD | `POST /opd/visits` `GET /opd/queue?doctorId` `GET /opd/visits/{id}/workspace` `POST /opd/visits/{id}/vitals` `POST /opd/visits/{id}/diagnosis` `POST /opd/visits/{id}/prescription` `POST /opd/visits/{id}/orders` `POST /opd/visits/{id}/complete` |
| Billing | `POST /billing/op-bills` `GET /billing/op-bills/{id}` `POST /billing/op-bills/{id}/pay` `POST /billing/bills/{id}/cancel` `POST /billing/refunds` `POST /billing/ip/{admissionId}/advances` `GET /billing/ip/{admissionId}/running-bill` `POST /billing/ip/{admissionId}/interim-bill` `POST /billing/ip/{admissionId}/final-bill` `POST /billing/discounts/{id}/approve` `POST /billing/day-close` |
| Lab | `POST /lab/orders` `GET /lab/collection-list` `POST /lab/samples/{id}/collect` `POST /lab/samples/{id}/reject` `GET /lab/worklist?deptId` `POST /lab/results` `POST /lab/results/{id}/verify` `POST /lab/results/{id}/approve` `GET /lab/reports/{orderId}/pdf` `GET /lab/tat-report` |
| Pharmacy | `POST /pharmacy/purchase-orders` `POST /pharmacy/grn` `POST /pharmacy/sales` `POST /pharmacy/ip-issues` `POST /pharmacy/returns` `GET /pharmacy/stock?drugId` `GET /pharmacy/stock/near-expiry` `POST /pharmacy/stock/adjustments` `POST /pharmacy/stock/transfers` |
| IPD | `POST /ipd/admissions` `GET /ipd/bed-board` `POST /ipd/admissions/{id}/transfer-bed` `POST /ipd/admissions/{id}/orders` `POST /ipd/admissions/{id}/progress-notes` `GET /ipd/admissions/{id}/clearances` `POST /ipd/admissions/{id}/discharge-summary` `POST /ipd/admissions/{id}/discharge` |
| Nursing | `POST /nursing/assessments` `GET /nursing/mar?admissionId&date` `POST /nursing/mar/{id}/administer` `POST /nursing/handovers` `POST /nursing/incidents` |
| Radiology | `POST /rad/orders` `GET /rad/worklist?modality` `POST /rad/reports` `POST /rad/reports/{id}/approve` `GET /rad/reports/{id}/pdf` |
| EMR | `GET /emr/patients/{id}/timeline` `GET/POST /emr/patients/{id}/histories` `GET /emr/patients/{id}/results-trend?testId` `POST /emr/patients/{id}/attachments` |
| OT | `POST /ot/bookings` `GET /ot/schedule?date` `POST /ot/surgeries/{id}/checklist` `POST /ot/surgeries/{id}/notes` `POST /ot/surgeries/{id}/consumables` |
| Finance | `/finance/vouchers` `/finance/ledger?accountId` `/finance/payables` `/finance/receivables` `/finance/reports/gst` `/finance/day-close` |
| Portal | `/portal/auth/*` `/portal/appointments` `/portal/reports` `/portal/bills` `/portal/feedback` (separate JWT audience, own rate limits) |
| Reports | `GET /reports/{code}?params...` `GET /dashboards/management` `GET /dashboards/op` ... `POST /reports/custom` |

---

## 14. Database Entity Planning

All entities carry the mandatory audit columns (§7). Branch-scoped unless marked (G) = global. ~200 tables at completion; the key ones:

**admin:** `branches`(G) `users`(G) `roles`(G) `permissions`(G) `user_roles` `role_permissions` `user_branches` `menus`(G) `refresh_tokens` `user_sessions` `system_settings` `designations` `password_history` `notification_queue`

**audit:** `audit_logs` (partitioned) `activity_logs` (partitioned)

**master:** `departments` `specialties` `patient_categories` `doctors` `doctor_departments` `employees` `services` `service_categories` `tariffs` `tariff_items` `packages` `package_items` `buildings` `floors` `ward_types` `wards` `room_types` `rooms` `beds` `vendors` `items` `item_categories` `drugs` `generics` `manufacturers` `taxes` `discount_types` `referrers` `icd_codes`(G) `uoms` `sample_types` `modalities` `corporates` `consent_templates`

**patient:** `patients`(G) `patient_contacts` `patient_guardians` `patient_identifiers` (Aadhaar/ABHA placeholder) `patient_consents` `patient_merges` `patient_documents`

**appointment:** `doctor_schedules` `schedule_overrides` `appointments` `appointment_tokens` `no_show_logs`

**opd:** `op_visits` `visit_vitals` `visit_complaints` `visit_examinations` `visit_diagnoses` `prescriptions` `prescription_items` `clinical_orders` (polymorphic order header: lab/rad/procedure) `referrals` `follow_ups`

**billing:** `bills` `bill_items` `bill_payments` `payment_modes` `receipts` `refunds` `advances` `discount_approvals` `bill_cancellations` `credit_notes` `day_closes` `counter_sessions`

**lab:** `lab_tests` `lab_test_parameters` `reference_ranges` `test_profiles` `profile_tests` `lab_orders` `lab_order_items` `samples` `sample_rejections` `lab_results` `result_values` `result_approvals` `critical_alerts`

**rad:** `rad_tests` `rad_orders` `rad_order_items` `rad_schedules` `rad_reports` `report_templates` `contrast_consents`

**pharmacy/inventory (shared engine):** `stores` `stock_batches` `stock_ledger` (append-only) `purchase_requests` `purchase_orders` `po_items` `grns` `grn_items` `purchase_returns` `supplier_invoices` `pharmacy_sales` `sale_items` `ip_issues` `issue_items` `stock_returns` `stock_transfers` `stock_adjustments` `stock_audits` `indents` `indent_items` `racks`

**ipd:** `admissions` `admission_consultants` `bed_allocations` (history) `bed_transfers` `admission_consents` `doctor_orders` `medication_orders` `progress_notes` `procedure_notes` `discharge_requests` `discharge_clearances` `discharge_summaries` `bed_charge_lines`

**nursing:** `nursing_assessments` `nursing_notes` `vitals` `mar_schedules` `mar_administrations` `intake_output` `risk_assessments` `care_plans` `shift_handovers` `incidents` `transfer_notes`

**emr:** `medical_histories` `surgical_histories` `family_histories` `allergies` `immunizations` `problem_list` `clinical_notes` `attachments`

**emergency:** `er_visits` `triage_records` `mlc_registers` `accident_registers` `observation_stays` `transfer_outs`

**ot:** `ot_rooms` `ot_bookings` `surgeries` `surgery_team` `pre_op_checklists` `anaesthesia_records` `surgery_notes` `implant_usages` `ot_consumables` `post_op_notes`

**icu:** `icu_stays` `icu_vitals` `ventilator_charts` `infusion_charts` `icu_rounds` `critical_alert_rules` `device_readings` (placeholder)

**mrd:** `record_files` `file_movements` `file_requests` `scanned_documents` `coding_worklist` `file_audits` `retention_policies` `destruction_register`

**finance:** `accounts` (CoA) `vouchers` `voucher_lines` `ledger_entries` `payables` `receivables` `gst_registers`

**biomed:** `equipment` `equipment_locations` `amc_contracts` `pm_schedules` `work_orders` `breakdown_complaints` `calibrations` `service_histories` `spare_usages`

**quality:** `quality_indicators` `indicator_values` `audit_checklists` `audit_runs` `quality_incidents` `non_conformances` `capas` `risk_register` `committee_meetings` `nabh_evidences` `feedbacks`

---

## 15. UI Page Planning

~140 pages at completion. Per module (list → detail → workflow pattern throughout):

| Module | Pages |
|---|---|
| Auth | Login, Change Password, My Sessions |
| Admin | Users list/form, Roles + permission matrix, Menu tree editor, Branches, Settings, Audit Log viewer, Activity Log |
| Masters | Master hub + ~25 generic CRUD pages (single reusable template) + Tariff editor (grid) + Package builder |
| Patients | Registration form (wizard: demographics → contact → guardian → consent → print), Patient search, Patient 360 view, Merge tool |
| Appointments | Schedule designer (doctor weekly grid), Booking calendar, Front-desk day view, Public queue display (kiosk route) |
| OPD | Front desk (visit create + queue), Doctor queue, **Consultation workspace** (tabbed single page — the most important screen in the system), Visit slip print |
| Billing | OP billing POS, Bill detail/receipt, Refunds, IP billing workspace (advances, running bill, interim/final), Discount approvals inbox, Day close, Collection report |
| Lab | Order entry, Collection worklist, Department worklist, Result entry grid, Validation/approval queues, Report viewer, TAT report |
| Pharmacy | POS sale, GRN entry, PO management, Stock explorer (batch drill-down), Transfers, Adjustments, Expiry dashboard, IP issue worklist |
| IPD | Admission form, **Bed board** (visual ward map), Inpatient chart (tabs: orders/notes/vitals/MAR/bills/documents), Discharge workspace (clearance checklist + summary editor) |
| Nursing | Ward worklist, MAR screen, Assessment forms, I/O chart, Handover board, Incident form |
| EMR | Patient timeline, Histories editor, Results trend (charts), Documents |
| Radiology | Order entry, Modality worklist, Reporting editor (templates), Approval queue, TAT report |
| Inventory | Indent workflow, GRN, Stock explorer, Verification count sheets |
| OT | OT calendar, Booking form, Surgery workspace (checklist → anaesthesia → notes → consumables), OT register |
| ICU | ICU dashboard, Flowsheet (hour-grid vitals), Ventilator/Infusion charts |
| Emergency | Rapid registration, Triage board (color-coded), ER tracker, MLC register |
| MRD | File tracking board, Issue/return desk, Coding worklist, Audit checklist |
| Finance | Voucher entry, Ledger viewer, Payables/Receivables aging, GST report, Day-close console |
| Biomedical | Equipment registry, PM calendar, Complaint board, Downtime report |
| Quality | Indicator dashboard, Incident intake + RCA workspace, CAPA tracker, Evidence library |
| Patient Portal | Login/registration, Home, Book appointment, My reports, My bills, Feedback |
| Doctor Portal | Dashboard, Queue, Consult, IP rounds, Sign-off inbox |
| Dashboards | Management, OP, IP, Billing, Revenue + report catalog with parameter panels |

---

## 16. Role and Permission Planning

### Permission naming: `MODULE.RESOURCE.ACTION`

Actions vocabulary: `VIEW, CREATE, UPDATE, DELETE, APPROVE, VERIFY, CANCEL, PRINT, EXPORT, ADJUST, TRANSFER, CLOSE, ADMIN`.

Examples: `PATIENT.RECORD.VIEW`, `PATIENT.MERGE.CREATE`, `BILLING.BILL.CANCEL`, `BILLING.DISCOUNT.APPROVE`, `BILLING.DAYCLOSE.CLOSE`, `LAB.RESULT.CREATE/VERIFY/APPROVE`, `PHARMACY.STOCK.ADJUST`, `IPD.DISCHARGE.APPROVE`, `MRD.FILE.ISSUE`, `REPORT.CROSSBRANCH.VIEW`.

### Role → permission matrix (seed; editable in UI)

| Role | Scope summary |
|---|---|
| Super Admin | Everything incl. branches, settings, permission admin |
| Hospital Admin | All operational modules of own branch; no permission admin |
| Reception | Patient registration, search, appointments, OP visit create, card print |
| Billing Executive | OP/IP billing create, receipts; **no** cancel/discount-approve |
| Billing Supervisor | + discount approve, bill cancel, refund approve, day close |
| Doctor | Own queue, consultation, orders, prescriptions, results view, discharge summary |
| Nurse | Vitals, MAR, nursing notes, assessments, I/O, handover, incident create |
| Head Nurse | + care plan approve, ward reports, transfer notes approve |
| Lab Technician | Sample collect/ack/reject, result entry |
| Pathologist | + result verify/approve, critical alert ack, report sign |
| Radiology Technician | Worklist, capture status |
| Radiologist | Reporting, verify/approve |
| Pharmacist | Sales, IP issue, returns, stock view |
| Pharmacy Purchase | PR/PO/GRN, supplier invoices |
| Store Keeper | Indents, issues, GRN (general stores), verification |
| MRD Officer | File tracking, coding, audits, record requests |
| Finance | Vouchers, ledger, payables/receivables, GST reports |
| Biomedical Engineer | Equipment, PM, complaints, calibration |
| Quality Manager | Indicators, incidents (all), CAPA, NABH evidence |
| OT Coordinator | OT booking, scheduling, register |
| ICU Incharge | ICU dashboards, charts, alert rules |
| Management | Read-only dashboards + reports, cross-branch view |
| Patient (portal) | Own records only — enforced server-side |

Rules: separation of duties is enforced by permission design (result *entry* vs *approval*; bill *create* vs *cancel*), the same user may hold both only if roles grant it explicitly; approval endpoints re-check the approver differs from the creator where policy demands (discounts, adjustments).

---

## 17. Reports Planning

### Architecture

- Reporting reads from **SQL views / materialized views** (`reporting` schema) — never raw OLTP joins scattered in code.
- `report_definitions` table: code, name, parameters (jsonb), permission, SQL view/proc, output formats.
- Generic executor endpoint + typed endpoints for complex reports; QuestPDF for print-grade PDFs; CSV/Excel export universal.
- Every phase ships its reports (§11); Phase 23 adds cross-cutting dashboards + custom builder.

### Report catalog (initial build)

| Area | Reports |
|---|---|
| Registration | Daily registrations, demographics profile, referral-source analysis |
| OPD | OP census (dept/doctor/day), new-vs-review ratio, queue wait time |
| IPD | Bed occupancy %, ALOS, admissions/discharges register, ward census, transfer log |
| Emergency | ER census, triage mix, MLC register, conversion rate |
| Billing | Daily collection (counter/user/mode), bill register, discount register, cancelled bills, refund register, credit outstanding, day-close summary |
| Lab | TAT (by test/dept), workload, rejection rate, critical-value log, revenue by test |
| Radiology | TAT, modality utilization, report pending list |
| Pharmacy | Sales register, stock ledger, near-expiry, dead stock, consumption (dept/drug), GST purchase & sales, margin analysis |
| Stores | Consumption, reorder list, stock verification variance |
| OT | OT register, utilization, surgeon-wise volume, cancellation reasons |
| Finance | Revenue (dept/doctor), GST reports, receivables aging, P&L inputs, outstanding |
| MRD | Incomplete records, coding productivity, file movement |
| Biomedical | Downtime, PM compliance, AMC expiry |
| Quality | Indicator trends, incident analysis, CAPA status |
| Admin | Audit report, user activity, login history |

---

## 18. Deployment Planning

### Environments

| Env | Purpose | Infra |
|---|---|---|
| dev | Developer machines | Docker Compose (full stack) |
| staging | UAT with hospital users | Single VM/server, Compose prod profile, anonymized-ish seed data |
| production | Live | On-prem hospital server (typical for HIS; LAN latency + data locality) or cloud VM; Nginx TLS |

### Pipeline (GitHub Actions / GitLab CI)

1. PR → build backend + frontend, run unit/integration tests, lint.
2. Merge to `main` → build Docker images tagged `sha` + `latest`, push registry.
3. Deploy to staging automatically; production deploy is manual approval.
4. **Migrations:** EF migration bundle executed as a pre-deploy step (not app-start); backward-compatible migrations required (expand → migrate → contract).

### Production topology

```
[Client browsers / kiosk displays]
        │ HTTPS
     [Nginx]  ── serves frontend static build
        │      └─ /api → reverse proxy
   [Prixo.Api container(s)]  (scale horizontally later; stateless)
        │
   [PostgreSQL 16]  ── streaming replica (optional) 
        │
   [pgBackRest / pg_dump nightly + WAL archiving]  → offsite/NAS copy
```

### Operations

- Backups: nightly full + WAL continuous; **restore drill monthly** (documented runbook).
- Monitoring: health endpoints + Uptime check; Serilog files shipped to Seq/Grafana Loki (optional).
- Secrets: environment variables / mounted secrets, never in images or git.
- DR target: RPO ≤ 15 min (WAL), RTO ≤ 4 h.

---

## 19. Docker Setup Plan

### `docker/docker-compose.yml` (development)

```yaml
services:
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: prixo_his
      POSTGRES_USER: prixo
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    ports: ["5432:5432"]
    volumes: [pgdata:/var/lib/postgresql/data]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U prixo -d prixo_his"]
      interval: 5s
      retries: 10

  api:
    build: { context: ../backend, dockerfile: ../docker/api.Dockerfile }
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__Default: Host=db;Database=prixo_his;Username=prixo;Password=${DB_PASSWORD}
      Jwt__Key: ${JWT_KEY}
    ports: ["5000:8080"]
    depends_on: { db: { condition: service_healthy } }

  web:
    build: { context: ../frontend, dockerfile: ../docker/web.Dockerfile }
    ports: ["3000:80"]
    depends_on: [api]

  pgadmin:
    image: dpage/pgadmin4
    ports: ["5050:80"]
    environment:
      PGADMIN_DEFAULT_EMAIL: admin@prixo.local
      PGADMIN_DEFAULT_PASSWORD: ${PGADMIN_PASSWORD}

volumes:
  pgdata:
```

### Dockerfiles

- **api.Dockerfile:** multi-stage — `mcr.microsoft.com/dotnet/sdk:10.0` build/publish → `mcr.microsoft.com/dotnet/aspnet:10.0` runtime, non-root user, `HEALTHCHECK` on `/health`.
- **web.Dockerfile:** `node:22-alpine` build (`vite build`) → `nginx:alpine` serving `dist/` with SPA fallback + `/api` proxy config.
- **Prod compose** adds: nginx TLS front, Hangfire dashboard auth, resource limits, `restart: unless-stopped`, log rotation, backup sidecar (pgBackRest), no pgAdmin.
- Migration bundle runs as one-shot service before `api` in prod profile.

---

## 20. Testing Plan

| Layer | Tooling | Scope & targets |
|---|---|---|
| Domain unit tests | xUnit + FluentAssertions | Business rules: bill math, tax, FEFO batch pick, bed-charge cutover, UHID format, MAR schedule generation. **≥90% on Domain** |
| Application/service tests | xUnit + NSubstitute + EF InMemory/SQLite where safe | Service flows, validation, permission guards |
| Integration tests | **Testcontainers-PostgreSQL** + `WebApplicationFactory` | Real DB: migrations apply cleanly; transactional flows (sale decrements stock atomically; discharge blocked without clearances; refresh-token rotation; concurrent bed allocation → one winner) |
| API contract | Integration suite asserts `ApiResponse` envelope + auth on **every** endpoint (reflection test: no controller action without `[HasPermission]` unless allow-listed) |
| Frontend unit | Vitest + React Testing Library | UI kit components, hooks, form validation |
| Frontend E2E | Playwright | Critical journeys per phase: login→register patient→book→OP visit→bill→lab result→pharmacy sale→admit→discharge |
| Performance | k6 (targeted) | Queue/worklist endpoints under load; 200 concurrent users baseline |
| Security checks | ZAP baseline scan in CI, dependency audit (`dotnet list package --vulnerable`, `npm audit`) | Each release |
| UAT | Scripted scenarios per phase with hospital users on staging | Phase exit criterion |

**Definition of Done (every feature):** migrations + seed, permissions seeded & enforced, validators, unit + integration tests green, audit verified, UI permission-gated, docs updated in `docs/modules/`, demo on staging.

---

## 21. Future Modules Plan

Held modules, in recommended post-launch order (each slots into existing architecture — new schema + feature folders, no re-architecture):

| # | Module | Depends on | Notes |
|---|---|---|---|
| F1 | **Insurance / TPA** | Billing, Corporate master | Highest revenue impact. Pre-auth → enhancement → claim → settlement → denial tracking; placeholders already in billing schema |
| F2 | **Communication** | notification_queue (exists) | SMS/WhatsApp/Email/Push senders behind `INotificationChannel`; activates all placeholder reminders/alerts |
| F3 | **HR & Payroll** | Employee master (exists) | Attendance, biometric integration, shifts, leave, payroll, doctor payout (payout rules read billing data) |
| F4 | **Blood Bank** | Lab, Inventory patterns | Donor → screening → components → cross-match → issue; statutory registers |
| F5 | **Dialysis** | IPD/OPD, Billing, Stores | Session scheduling, machine assignment, session records |
| F6 | **Kitchen / Dietary** | IPD diet orders (placeholder exists) | Diet master, meal plans, production sheets, delivery tracking |
| F7 | **Infection Control** | Nursing incidents, Lab | HAI surveillance, isolation tracking, antibiotic stewardship, BMW tracking |
| F8 | **Physiotherapy** | OPD orders, Billing | Assessment, treatment plans, session tracking |
| F9 | **Dental** | OPD, Billing | Dental charting UI, procedures |
| F10 | **Facility / Maintenance** | Biomedical patterns | Complaints, housekeeping, laundry, waste |
| F11 | **Ambulance** | Billing | Trip management, GPS placeholder, logs |
| F12 | **Mobile Apps** | Portals' APIs | React Native/Flutter consuming existing portal + staff APIs |

---

## 22. AI and Integration Modules Future Roadmap

### Integrations (Wave I — after launch stabilizes)

| Integration | Approach |
|---|---|
| **ABDM / ABHA** (India) | **M1 built 2026-08-19** at the user's direction (ahead of Wave-I): ABHA verify/link/create via the two-step OTP handshake — `abha_profiles` + `abdm_verification_sessions` schema, `IAbdmGateway` seam + config-gated `AbdmGatewayClient` (no-op until `Abdm` sandbox credentials are supplied), `PATIENT.ABHA.VIEW`/`.MANAGE` permissions, and the front-desk verify UI (replacing the disabled placeholder button). See the "ABDM / ABHA" section of `docs/modules/patients.md`. **Held:** live gateway wiring (needs sandbox client cert + ABDM RSA public key for OTP encryption), HFR/HPR registration, and M2 (HIP care-context linking) / M3 (HIU record fetch) |
| **HL7 FHIR** | FHIR R4 facade project (`Prixo.Fhir`) mapping Patient, Encounter, Observation, DiagnosticReport, MedicationRequest |
| **LIS Analyzer** | ASTM/HL7 listener service → `IAnalyzerInbound` (interface exists); bidirectional worklist |
| **PACS / DICOM** | DICOM Modality Worklist (accession numbers already generated); web viewer (OHIF) embedding via `pacs_study_uid` |
| **Payment Gateway** | Razorpay/PayU adapter for portal payments + POS UPI |
| **WhatsApp (WATI/Meta)** / SMS / Email / IVR | Channels behind `INotificationChannel`; templates in DB |
| **Biometric / RFID / Barcode-QR** | Device adapters; barcode already core (samples, UHID cards) |
| **Queue display system** | Existing kiosk routes → Android TV boxes |

### AI Modules (Wave II — needs 6–12 months of clean data)

Foundation first: an **AI gateway service** (`Prixo.Ai`) that brokers between HIS data and LLM/ML providers, with PHI redaction, human-in-the-loop confirmation on all clinical suggestions, and full audit of prompts/outputs.

| AI module | Data it consumes | Value |
|---|---|---|
| AI Discharge Summary | Progress notes, orders, results | Draft summary → doctor edits/approves |
| AI ICD Coding | Diagnosis text, clinical notes | Suggest codes to MRD coder |
| AI Prescription / Drug Interaction Checker | Prescriptions + drug KB | Interaction/dose alerts at prescribe time |
| AI Patient Triage | Triage vitals + complaints | ESI level suggestion in ER |
| AI Lab Critical Alert | Result streams | Pattern-based early warning beyond static limits |
| AI Clinical Assistant | EMR timeline | Summarize patient history for consult |
| AI Bed Occupancy / OT Schedule Optimization | Census & OT history | Forecasting + slot suggestions |
| AI Revenue Leakage Detection | Orders vs bills | Flag unbilled services |
| AI Feedback Analysis | Portal feedback | Sentiment + theme dashboards for Quality |
| AI Support Ticket Assistant | Helpdesk/complaints | Auto-categorize/route |

Order of adoption: start with **non-clinical, high-trust wins** (revenue leakage, feedback analysis, ICD suggestion) → then doctor-supervised drafting (discharge summary) → then decision support (interactions, triage) with clinical governance sign-off.

---

## 23. Development Checklist

### Phase 0 — Project Setup
- [x] Repo structure + .editorconfig + branch protection *(branch protection pending — apply when GitHub remote is added)*
- [x] .NET 10 solution: Domain / Application / Infrastructure / Api
- [x] BaseEntity + audit columns + soft-delete filter
- [x] ApiResponse<T> + PagedResult<T> wrappers
- [x] Global exception middleware + ProblemDetails
- [x] Serilog structured logging + request logging
- [x] EF Core 10 + Npgsql + first migration + snake_case
- [x] Audit SaveChanges interceptor (column stamping)
- [x] Health checks + OpenAPI/Scalar
- [x] Vite + React + TS strict + Tailwind + Router + TanStack Query + Axios
- [x] UI kit v1 (AppShell, DataTable, Modal, form controls) *(Toast deferred — inline alerts for now)*
- [x] Docker Compose (db + api + web + pgadmin) + env configuration
- [ ] CI: build + test + lint on PR *(pending — needs GitHub remote)*

### Phase 1 — Admin + RBAC
- [x] Login/logout + JWT + refresh rotation + lockout
- [x] Password hashing + password policy + first-login change + history
- [x] Users CRUD + branch mapping + photo *(PhotoPath stored; upload endpoint arrives with file storage)*
- [x] Roles + permission registry + role-permission matrix UI
- [x] Menu management + permission-driven sidebar
- [x] Branch / hospital unit setup
- [x] Designations + system configuration (settings UI)
- [x] Session management (list, force logout, idle timeout)
- [x] Entity audit log + viewer; activity log (login/logout/security events)
- [x] Backup settings doc + notification settings placeholder
- [x] Seed: super admin, roles, permissions, menus

### Phase 2 — Masters
- [x] Generic master CRUD framework (BE + FE template) *(bulk import/export per master deferred; ICD CSV import shipped)*
- [x] Departments, specialties, patient categories *(designations remain Phase 1)*
- [x] Doctor master (+user link, fees, schedule defaults) & employee master (lightweight)
- [x] Service, tariff (per branch/category, effective-dated), package masters *(tariff scoped to current branch context)*
- [x] Building → floor → ward → room → bed hierarchy
- [x] Vendor, item, drug/generic/manufacturer masters
- [x] Tax, discount, referral masters
- [x] ICD-10 import + search
- [x] Corporate master (schema) + insurance/TPA placeholder
- [x] UoM, sample types, modalities
- [x] EF migration `Phase2_Masters` + permissions/menus seeded for all master screens
- [x] Unit tests: simple-master validators, ICD CSV parse, master DTO validators

### Phase 3 — Patient Registration
- [x] Registration wizard + validation
- [x] UHID generation (atomic, configurable format) *(PostgreSQL upsert-counter `SequenceService`, org-wide scope 0)*
- [x] Patient search (indexed: UHID/name/mobile/DOB)
- [x] Guardian/attender, contacts
- [x] Aadhaar/ABHA placeholder fields + photo upload placeholder *(photo upload is fully functional, not just a placeholder; ABHA is no longer just a placeholder — ABDM milestone M1, verify/link/create via OTP, was built 2026-08-19 — see §22 and `docs/modules/patients.md`. Aadhaar verification (UIDAI) remains a placeholder)*
- [x] Duplicate detection + patient merge (transactional) *(prefilter by mobile/DOB + Levenshtein name scoring; merge re-points contacts/identifiers/documents/consents via IUnitOfWork)*
- [x] Consent templates + consent capture *(templates under Masters; capture supports an optional scanned-copy upload)*
- [x] Registration card PDF with barcode/QR *(QuestPDF + QRCoder; barcode omitted, QR alone satisfies UHID scanning)*
- [x] Unit tests: fuzzy name-matching, patient validators

### Phase 4 — Appointments
- [x] Doctor schedule templates + overrides + leave block
- [x] Slot engine + booking + walk-in
- [x] Reschedule/cancel with reason codes
- [x] Token generation + queue display route *(per-doctor-per-day atomic counter reusing SequenceService; queue display runs under the normal authenticated shell, not a true anonymous kiosk route — see docs/modules/appointments.md)*
- [x] Doctor availability view
- [x] No-show auto-marking job + reminder outbox placeholder *(first Hangfire job in the codebase; hourly recurring job; notification_queue outbox has no sender, by design)*
- [x] Unit tests: schedule overlap validation, booking/cancel/reschedule validators

### Phase 5 — OPD
- [x] OP visit registration (+from appointment) + token + visit slip *(reuses the appointment's token end to end; walk-ins get a fresh per-doctor-per-day token)*
- [x] New/review auto-detection + billing link (draft charge) *(ConsultationFee snapshotted at registration — Phase 6 Billing turns it into a real bill line; no Bill entity built yet)*
- [x] Queues: front desk + doctor
- [x] Consultation workspace: summary, vitals, complaints, history, examination
- [x] Diagnosis + ICD coding; problem list write-through *(no separate problem-list table — querying diagnoses across a patient's visits serves as one)*
- [x] Prescription with doctor favorites/templates
- [x] Lab/radiology/procedure orders → downstream worklists *(order headers created with Status=Ordered; no worklist consumes them yet — Phase 7/12)*
- [x] Referral + follow-up advice (+optional pre-book) *(follow-up date/notes captured; booking the slot is a manual bridge to the Phase 4 Appointments UI, not auto-triggered)*
- [x] Unit tests: vitals range validation, referral internal/external branching, prescription template validators

### Phase 6 — Billing
- [x] OP bill (multi-source items) + receipt print — transactional *(auto-pulls the consultation fee and non-cancelled clinical orders from the OP visit; payment collection and refund approval run inside `IUnitOfWork.ExecuteInTransactionAsync`)*
- [x] Payment modes + refunds + refund approval
- [ ] IP advances/deposits + running bill auto-feed *(Phase 9 added `Advance.AdmissionId` so a deposit is now tagged to a stay and `AdmissionService.AdmitAsync` collects the initial deposit this way; drawing an advance's balance down automatically against the IP bill's `PaidAmount` is still not built — deposits stay separately visible/collectible, not yet consumed by billing)*
- [ ] Bed-charge daily job + interim + final bill *(Phase 9 added `BillingService.GenerateIpBillAsync`/`RefreshBedChargesAsync` — an IP bill can be generated and its bed-charge lines recomputed from `BedAllocation` history on demand, which covers "interim + final bill"; there is still no daily Hangfire job auto-refreshing charges, same honest on-demand scoping as Pharmacy's alerts)*
- [x] Discount workflow with threshold approvals *(ad-hoc discounts, i.e. no `DiscountType`, always route to approval; a `DiscountType` auto-applies up to its own threshold and routes above it)*
- [x] Credit billing + corporate placeholder
- [x] Rate revision (tariff versioning) + bill cancellation workflow *(tariff versioning itself shipped in Phase 2 — `Tariff.EffectiveFrom/EffectiveTo`; this phase adds the resolution query and reason-coded cancellation, guarded so a bill with any payment must be refunded first)*
- [x] Daily collection + day close (locking, transactional)
- [x] Unit tests: bill/item amount math, tariff rate resolution, discount threshold branching, request validators

### Phase 7 — Laboratory
- [x] Lab test/parameter/reference-range/profile masters *(panel tests carry one row per result parameter even when not a panel, so result entry/reference-range resolution never special-cases the single-parameter path)*
- [x] Orders (OPD/IPD/ER/walk-in + billing check) *(OP visit link + direct walk-in built; IPD/ER need those phases to exist. "Billing check" is a soft link only — `LabOrderItem.BillItemId` set by front desk once billed, not an enforced gate — see docs/modules/lab.md)*
- [x] Barcode + collection + acknowledgement + rejection flows *("Barcode" is a scannable sequence-generated code (`SMP-2026-000123`), not barcode image rendering/scanner hardware integration*
- [x] Department worklists + result entry (auto H/L flags) *(critical flagging too, not just H/L — see ResultFlag.Critical)*
- [x] Validate → verify → approve pipeline (role-gated) *(three-step, per-item: ResultEntered → Verified → Approved, each its own permission)*
- [x] Critical value alerts + acknowledgment log
- [x] Lab report PDF (cumulative) + signature placeholder *(`LabResult.ApproverSignaturePath` column exists, nothing writes to it yet)*
- [x] TAT report + analyzer interface placeholder *(`IAnalyzerInbound` interface defined, unimplemented and unregistered — result entry is manual)*

### Phase 8 — Pharmacy
- [x] Deep drug masters (schedule flags, HSN, GST) + racks *(Schedule/HsnCode/TaxId fields already existed as Phase 2 placeholders; this phase adds ReorderLevel plus the Store/Rack masters)*
- [x] PR → PO → GRN (batch/expiry/MRP) → purchase return — transactional *(GRN posting and purchase return are `IUnitOfWork.ExecuteInTransactionAsync`; PR/PO approval is a status gate only, no stock effect)*
- [x] OP POS sale with FEFO + GST invoice *("GST invoice" is a QuestPDF invoice showing per-line tax, not a statutory e-invoice/IRN integration)*
- [x] IP issue against medication orders + emergency/department issues *(Department + Emergency issue were already real, usable flows; Phase 9 added `Issue.AdmissionId` so a pharmacist can now manually link an issue to an admission — still not auto-generated from a `MedicationOrder`, that needs Phase 10's MAR to exist)*
- [x] Returns (sales + IP) + stock ledger (append-only) *("IP" here reads as "issue" — sales returns and issue returns are both built; append-only `stock_ledger` backs every movement*
- [x] Adjustments (approval) + transfers + stock audit *(adjustment is the one true approval-gated workflow this phase; transfer is dispatch/receive; audit posts variances as adjustment ledger rows — same declared-vs-system idiom as Billing's DayClose)*
- [x] Near-expiry & min-stock alerts + consumption report *(alerts are on-demand queries, not a scheduled Hangfire job/dashboard widget yet — see docs/modules/pharmacy.md)*

### Phase 9 — IPD
- [x] Admission flow + IP number + consent + deposit + slip *(consent reuses the shared PatientConsent capture, text-only — no scan/file upload in the admission wizard itself; deposit reuses Advance with a new AdmissionId tag)*
- [x] Bed board + allocation (concurrency-safe) *(application-level `Bed.Status == Vacant` check backed by partial unique indexes on `BedAllocation(bed_id)`/`(admission_id)` WHERE released_at IS NULL — a losing concurrent allocation hits the index, not a silent double-book)*
- [x] Bed/ward/room/consultant transfers with charge cutover *("charge cutover" = each BedAllocation segment's own AllocatedAt/ReleasedAt window and snapshotted BedChargePerDay; bed-charge billing is computed on demand via RefreshBedChargesAsync, not a background job)*
- [x] Unified doctor order sheet + medication orders *(Lab/Radiology/Nursing order types stay free-text on Description — no auto-generated LabOrder/RadiologyOrder, same loose-coupling posture as OPD's ClinicalOrder)*
- [x] Progress notes, vitals chart, I/O chart, procedure notes *(basic, repeatable, admission-scoped entries — Phase 10 Nursing deepens vitals/I-O with shift-tagging and risk scores)*
- [x] Diet orders placeholder *(structured DietType + Instructions capture, no diet-item master or kitchen workflow)*
- [x] Discharge request + 4-way clearance checklist *(4 ClearanceType rows auto-created on request; Billing clearance is manual sign-off, not an automatic balance check)*
- [x] Discharge summary (structured, approved) + final discharge + slip *(IsApproved + ApprovedByDoctorId/At gate both the PDF's signature line and final discharge)*
- [x] Bed occupancy dashboard *(total/occupied/vacant/reserved/cleaning/blocked + per-ward occupancy rate)*

### Phase 10 — Nursing
- [x] Admission nursing assessment (template forms) *(`TemplateCode` + jsonb `DataJson`, schema-ready for real structured templates per code — the shipped UI only captures a single free-text "findings" field wrapped as JSON; a genuine form builder is future work)*
- [x] MAR: schedule generation + administer/skip/refuse + high-alert double check *(due-time schedule lazily generated per admission/date from `MedicationOrder.Frequency` via a small fixed lookup — OD/BD/TID/QID/Q6H/Q8H/Q12H/HS/STAT/SOS; `Drug.IsHighAlert` gates a double-verification requirement enforced server-side, including rejecting self-verification — live-tested end to end)*
- [x] Nursing notes + vitals + I/O *(shift-tagged `NursingNote` is new this phase; vitals/I-O reuse Phase 9's `IpVitals`/`IntakeOutputEntry` tables and endpoints — nothing new was needed there, they already "feed charts")*
- [x] Pain/fall/bedsore risk scores *(Pain NRS, Morse Fall Scale, Braden Bed Sore Scale — raw per-item scores submitted as jsonb, `TotalScore`/`RiskLevel` banding computed server-side by `NursingMath`, unit-tested)*
- [x] Care plans + shift handover (SBAR) *(`ShiftHandover` is ward + date + shift, one per slot via a partial unique index, with per-patient SBAR `PatientHandoverNote` rows; `HandoverToUserId` exists on the API but isn't set from the UI yet — see docs/modules/nursing.md)*
- [x] Incident reporting + transfer notes *(Incident: Reported → UnderReview → Closed, patient/admission both optional so staff/visitor/general incidents are covered; no RCA/CAPA fields — deliberately shallow, Phase 20 Quality/NABH merges this into the org-wide register later)*

### Phase 11 — EMR
- [x] Patient timeline aggregation *(pure aggregation over existing OpVisit/Admission/LabResult/Prescription/ProcedureNote/DischargeSummary/ClinicalNote/Attachment data, no new storage — merged and sorted in memory at one-patient scale)*
- [x] Medical/surgical/family/allergy/immunization histories *(plain, repeatable, patient-scoped entries; every `emr` table is deliberately global — not branch-scoped — so a patient's histories/allergies read identically from any branch)*
- [x] Allergy banner + prescription warning hook *(real backend cross-check — `HistoryService.CheckDrugAllergiesAsync` matches candidate drug names against active drug allergies by substring, exposed at `GET /emr/patients/{id}/allergy-check` and called live from OPD's PrescriptionTab; advisory only, doesn't block saving)*
- [x] Clinical notes + problem list *(`NoteType` is free text — no specialty-template master; problem list is a manually-curated Active/Resolved registry, distinct from and not synced with OPD's per-visit VisitDiagnosis)*
- [x] Prescription/investigation/procedure history views + results trends *(dedicated read views beyond the unified timeline; results trend parses ResultValue's free-text Value via `EmrMath.TryParseTrendValue`, silently skipping non-numeric results, rendered as a small inline SVG line chart with flag-colored points)*
- [x] Attachments + consent repository *("Consent Documents repository" reuses Phase 3's existing PatientConsent capture + list (`ConsentsTab`) as-is — no new backend needed; Attachments is a genuinely new `emr.attachments` table for external reports/scanned documents, with a `VirusScanStatus` placeholder column per plan's "virus-scan hook placeholder" — see `IVirusScanner`)*

### Phase 12 — Radiology
- [x] Test + modality masters *(`master.radiology_tests` is new; Modality itself was already seeded in Phase 2 — no new modality data needed)*
- [x] Orders + modality scheduling + technician worklist *(RadOrder/RadOrderItem mirror Lab's LabOrder/LabOrderItem shape exactly; RadSchedule is one row per scheduled item with ModalityId denormalized for the technician worklist filter)*
- [x] Capture status + reporting editor (templates) *(image capture recorded directly on RadOrderItem — no separate "sample" entity, unlike Lab; report entry supports an optional RadReportTemplate that prefills Findings/Impression, which the radiologist then edits)*
- [x] Verify → approve + PDF report *(entry→verify→approve pipeline mirrors Lab's LabResult pipeline; PDF generation refuses to run before the report is Approved)*
- [x] Contrast consent + TAT report + PACS/DICOM placeholders *(contrast consent is an enforced gate — MarkImageCapturedAsync rejects a contrast-requiring test with no ContrastConsent row, verified live; TAT report mirrors Lab's; PACS/DICOM is schema-only — AccessionNumber generated, PacsStudyUid column unused, RadiologyPacsViewerUrl setting seeded empty)*

### Phase 13 — Stores & Inventory
- [x] Store types + item/category masters
- [x] Indent → approve → issue workflow
- [x] PR/PO/GRN for general stores
- [x] Returns, transfers, adjustments, verification counts
- [x] Consumption + reorder alerts

### Phase 14 — OT
- [x] OT master + booking calendar + conflict checks
- [x] Team assignment (surgeon/anaesthetist/nurses)
- [x] Pre-op checklist + consent verification
- [x] Anaesthesia record + surgery notes + post-op notes
- [x] Implant + consumable capture → stock + billing
- [x] OT billing components + OT register report

### Phase 15 — ICU
- [x] ICU stay management + bed allocation
- [x] Hourly vitals flowsheet + ventilator + infusion charts
- [x] ICU MAR view + rounds notes
- [x] Critical alert rules + ICU dashboard
- [x] Device integration placeholder tables

### Phase 16 — Emergency
- [x] Rapid registration + unknown patient flow
- [x] Triage (levels, board) + ER tracker
- [x] ER assessment sheet + STAT lab/radiology orders
- [x] MLC + accident registers (statutory prints)
- [x] Observation ward + admission conversion + transfer out
- [x] Emergency billing flow

### Phase 17 — MRD
- [x] Record file creation + tracking + issue/return + overdue alerts
- [x] Document scanning/upload + type master
- [x] Coding worklist + completeness tracking
- [x] Discharge file audit + missing docs tracking
- [x] Retention policies + destruction register + record requests
- [x] MRD reports

### Phase 18 — Finance
- [x] Chart of accounts + auto-posting rules from billing/pharmacy/GRN
- [x] Journal/payment/receipt vouchers
- [x] Ledger + payables + receivables aging
- [x] GST reports + revenue + outstanding reports
- [x] Central day close console

### Phase 19 — Biomedical
- [x] Equipment registry + location tracking
- [x] AMC/CMC + warranty + renewal alerts
- [x] PM schedules → auto work orders
- [x] Breakdown complaints + downtime capture
- [x] Calibration tracking + service history + spare usage
- [x] Downtime + PM compliance reports

### Phase 20 — Quality / NABH
- [x] Indicator definitions + data entry/auto-pull
- [x] KPI dashboard + audit checklists/runs
- [x] Unified incident intake + RCA + non-conformance
- [x] CAPA tracker + risk register
- [x] Committee meeting records + NABH evidence library
- [x] Compliance dashboard

*(Built last, after Phases 21–23 — see `docs/modules/quality.md`.)*

### Phase 21 — Patient Portal
- [x] Portal auth (separate audience) + registration
- [x] Appointment booking/cancel
- [x] Reports (lab/radiology approved only), prescriptions, bills, summaries
- [x] Medical history (safe subset) + feedback
- [x] Payment placeholder

*(Completed ahead of Phase 20/Quality-NABH — see `docs/modules/portal.md` for the build-order note.)*

### Phase 22 — Doctor Portal
- [x] Doctor layout + dashboard
- [x] Queue + consult (reuse) + IP rounds view
- [x] Orders/results with trends
- [x] Discharge summary sign-off inbox

*(Also completed ahead of Phase 20/Quality-NABH — see `docs/modules/doctor-portal.md`.)*

### Phase 23 — Reports & Dashboards
- [x] Reporting schema + materialized views + refresh jobs
- [x] Management/OP/IP/Billing/Revenue dashboards
- [x] Full report catalog (§17) with export — executable rows for the new cross-cutting
      views, linked rows for reports earlier phases already built (see `docs/modules/reports.md`)
- [x] Custom report builder v1
- [x] Cross-branch reporting permission + report subscription placeholder

*(Also completed ahead of Phase 20/Quality-NABH — see `docs/modules/reports.md`.)*

### Cross-cutting (every phase)
- [ ] Permissions seeded + `[HasPermission]` on every endpoint
- [ ] Menus seeded
- [ ] FluentValidation on every request DTO
- [ ] Transactions on all money/stock/admission flows
- [ ] Audit verified (entity + activity)
- [ ] Unit + integration tests green; E2E journey updated
- [ ] Module spec written in `docs/modules/`
- [ ] UAT sign-off on staging

---

*End of master plan. Per-module detailed specs (`docs/modules/*.md`) are written just-in-time as each phase begins, using this document as the contract.*
