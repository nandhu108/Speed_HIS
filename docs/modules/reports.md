# Module Spec — Reports & Dashboards (Phase 23)

Status: **complete** (2026-07-20). Build-order note: same deviation as Phases 21/22 — the
plan (§12) sequences Phase 20 (Quality/NABH) before this. Quality/NABH has no hard
dependency on Reports & Dashboards, so this phase was completed at the user's explicit
request while 20 stays deferred. See root `CLAUDE.md`.

## Design

Every earlier phase already ships its own reports (Lab/Rad TAT, Finance cash-bank/revenue/
GST/outstanding, Biomed downtime/PM-compliance, MRD coding/movement, Billing day-close, IPD
bed-occupancy dashboard). Phase 23 does **not** duplicate any of those — it adds the
cross-cutting layer plan §23 actually calls for: dashboards that aggregate *across* what
those phases built, plus a generic report catalog/executor/export engine. Concretely:

- **New `reporting` schema** (plan §17: "Reporting reads from SQL views / materialized
  views — never raw OLTP joins scattered in code") with three materialized views —
  `mv_revenue_daily`, `mv_daily_collection`, `mv_op_census` — each day-granular so the
  dashboards can chart a trend, not just a single-range total the way Finance's existing
  `GetRevenueReportAsync`/`GetCashBankCollectionAsync` do. Doctor attribution on
  `mv_revenue_daily` mirrors Finance's existing scoping exactly (OP visits only, via
  `Bill.OpVisitId` — no IP-consultant revenue attribution, since Finance's report doesn't do
  that either). Refreshed every 15 minutes by a Hangfire recurring job (`ReportRefreshJob` →
  `IReportViewRefresher`, the one place in the codebase that runs raw SQL, `REFRESH
  MATERIALIZED VIEW CONCURRENTLY`).
- **Five dashboards** (`DashboardService`): Management (revenue/collection/OP+IP census/
  bed occupancy/unacknowledged alerts, composing IPD's existing `BedBoardService` outright
  for occupancy — no parallel calculation), OP, IP (adds ALOS, the one genuinely new IPD
  metric), Billing, Revenue. Revenue's by-department/by-doctor breakdown **is** plan §23's
  "Doctor-wise & Department-wise reports" — not a separate pair of endpoints.
- **Management Dashboard is the one cross-branch-aware read** (plan: "cross-branch with
  permission"): it self-adapts to whether the caller holds the existing
  `REPORT.CROSSBRANCH.VIEW` permission (reused from Finance's day-close console, not a new
  code) rather than exposing a separate cross-branch-only endpoint.
- **Custom Report Builder v1** (plan §17): `reporting.report_definitions` is the catalog —
  each row is either "executable" (`ViewName` set, runs through the generic executor against
  one of the three materialized views) or "linked" (`LinkPath` set, points the picker at a
  dedicated report page an earlier phase already built — Lab/Rad TAT, Finance/Biomed/MRD
  reports, Billing day-close, the audit log). The "generic executor" contract lives at the
  HTTP layer (one `POST /reports/{code}/execute` for any executable code); internally it
  dispatches to one of three small typed queries rather than building raw SQL from a report
  code or date range, so there's no SQL-injection surface.
- **Export**: CSV is plain text (no library). Excel uses ClosedXML (new dependency, MIT
  licensed) behind `IReportExportService`. PDF reuses the existing `IPdfService`/QuestPDF via
  one new generic `GenerateTabularReport` method — a column-headers-plus-rows layout shared
  by every report, not a bespoke template per report.
- **Report subscription placeholder** (plan: "schedule + outbox"): `reporting.
  report_subscriptions` — real schema (report code, cron expression, recipient email), same
  posture as Appointments' `NotificationQueue`: rows are stored, nothing ever dispatches them
  since no mailer exists in this codebase.

## Endpoints

- `GET /api/v1/dashboards/{management,op,ip,billing,revenue}` — each takes optional
  `fromDate`/`toDate`.
- `GET /api/v1/reports/catalog` — permission-filtered (a user only sees catalog rows whose
  `PermissionCode` they hold).
- `POST /api/v1/reports/{code}/execute`, `GET /api/v1/reports/{code}/export?format=csv|excel|pdf`.
- `GET/POST /api/v1/reports/subscriptions`, `DELETE /api/v1/reports/subscriptions/{id}` — a
  user's own subscriptions only.

## Permissions

`DASHBOARD.{MANAGEMENT,OP,IP,BILLING,REVENUE}.VIEW`, `REPORT.CATALOG.VIEW`,
`REPORT.SUBSCRIPTION.MANAGE`. None of these were added to `HOSPITAL_ADMIN`'s curated
permission list (same precedent as Biomed/Doctor Portal — only `SUPER_ADMIN` holds every
permission automatically); a hospital creates a role and grants these explicitly.

## Frontend (`src/features/reports`)

- `ManagementDashboardPage`, `OpDashboardPage`, `IpDashboardPage`, `BillingDashboardPage`,
  `RevenueDashboardPage` (`/reports/management|op|ip|billing|revenue`) — stat tiles, a
  `TrendChart` (single-series line, same shape as EMR's existing `ResultsTrendChart`: thin
  2px line, native-tooltip points, table-view fallback), and `AmountBarList` breakdowns.
- `CustomReportsPage` (`/reports/custom`) — catalog grouped by category; a "linked" entry
  navigates to its existing dedicated page, an "executable" entry runs inline with a date
  filter and CSV/Excel/PDF export buttons.
- `ReportSubscriptionsPage` (`/reports/subscriptions`) — create/list/remove my own
  subscription rows.

## Deferred / notes

- **Not every report in plan §17's full catalog table is "executable" through the Custom
  Report Builder** — only the three genuinely new cross-cutting reports (Revenue Daily,
  Daily Collection, OP Census) are. Everything else already has its own dedicated,
  fully-featured page from an earlier phase; the catalog links to those rather than
  rebuilding them as generic-executor rows, which would have been strictly worse (losing
  each page's bespoke filters/actions) for no benefit.
- **Patient Census and department-level OP census were narrowed in scope.** `mv_op_census`
  tracks day/doctor/visit-type, not day/department — a doctor's clinical department is a
  many-to-many (`DoctorDepartment`), so a single "primary department" attribution would be
  arbitrary; department-level financial reporting is already covered by
  `mv_revenue_daily`/the Revenue Dashboard via `Service.DepartmentId`, which has one
  unambiguous department per line item. A standalone "new registrations per day" Patient
  Census view was considered but cut for time — the Management Dashboard's OP/IP counts and
  IPD's bed occupancy cover the census need this phase actually asked for.
- **No role auto-provisions these permissions** — see Permissions above; same gap flagged in
  Biomed's and Doctor Portal's module docs.
- **Frontend not exercised in a real browser** — verified via `tsc -b && vite build` (clean)
  and `oxlint` (clean, one new warning matching two pre-existing identical ones — a helper
  function exported alongside a component in the same file, not addressed anywhere else in
  this codebase either). The backend API surface it talks to *was* exercised live (below).

## Live end-to-end verification (2026-07-20)

Ran the full stack for real (`docker compose up db`, migration applied, materialized views
confirmed present and populated from real OP-visit data, API booted, seeder ran) and drove
the whole surface with scripted HTTP calls:
- All five dashboards return real, correctly-scoped data (confirmed `mv_op_census` picked up
  an actual OP visit registered during the test).
- Custom Report Builder: executed `OP_CENSUS`/`REVENUE_DAILY`/`DAILY_COLLECTION` and
  exported all three formats — CSV, a valid `.xlsx` (verified ZIP magic bytes), and a valid
  PDF (verified `%PDF` header) all produced real bytes from real data.
- **Found and fixed a real bug during this testing**: `ColumnsJson` deserialization used
  `System.Text.Json`'s default (case-sensitive) options against lower-camelCase JSON keys,
  silently returning all-null `ReportColumnDto`s. Fixed by deserializing with
  `PropertyNameCaseInsensitive = true`; re-verified the catalog and execute/export endpoints
  return correct column metadata afterward.
- **Cross-branch permission gating verified with a purpose-built low-privilege role**: a
  user holding only `DASHBOARD.MANAGEMENT.VIEW` (no `REPORT.CROSSBRANCH.VIEW`) gets
  `isCrossBranch: false` and exactly their own branch's row — not silently leaking other
  branches. The same user gets a clean `403` on `/dashboards/op` (a permission they don't
  hold), not a 500.
- Report subscription create/list placeholder confirmed working (values persist; nothing
  claims to send anything).
