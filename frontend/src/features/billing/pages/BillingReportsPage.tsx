import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { endpoints } from '../../../api/endpoints'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { defaultDateRange } from '../../../components/ui/DateRangeFilter'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { ReportResultTable } from '../../../components/ui/ReportResultTable'
import { Select } from '../../../components/ui/Select'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useUsers } from '../../admin/api'
import { useAuth } from '../../auth/AuthContext'
import { useCorporates, useDoctors, usePatientCategories, useServices, useSimpleMaster, useWards } from '../../masters/api'
import {
  downloadBillReport,
  downloadBillingReport,
  useBillingReportResult,
  useBills,
  useConcessionSearch,
  type ConcessionSearchQuery,
  type ReportExportFormat,
} from '../api'
import { BILL_STATUS_LABELS, BILL_TYPE_LABELS, BILLING_GENDER_LABELS, type BillListItem, type BillStatus, type ConcessionSearchResult } from '../types'

type Tab =
  | 'bill-report'
  | 'ageing'
  | 'ward-outstanding'
  | 'day-book'
  | 'cancelled'
  | 'concession'
  | 'refund'
  | 'advance'
  | 'tariff-list'
  | 'doctors-payout'
  | 'user-settlement'
  | 'service-breakup'
  | 'ip-detail'
  | 'revenue'
  | 'outstanding'

const TABS: { key: Tab; label: string }[] = [
  { key: 'bill-report', label: 'Bill Report (OP/IP)' },
  { key: 'ageing', label: 'Ageing' },
  { key: 'ward-outstanding', label: 'Ward-wise Outstanding' },
  { key: 'outstanding', label: 'Outstanding' },
  { key: 'day-book', label: 'Day Book' },
  { key: 'cancelled', label: 'Bill Cancelled' },
  { key: 'concession', label: 'Concession' },
  { key: 'refund', label: 'Refund' },
  { key: 'advance', label: 'Advance' },
  { key: 'tariff-list', label: 'Tariff List' },
  { key: 'doctors-payout', label: 'Doctors Payout' },
  { key: 'user-settlement', label: 'User Settlement' },
  { key: 'service-breakup', label: 'Service-wise Breakup' },
  { key: 'ip-detail', label: 'IP Service Billing Detail' },
  { key: 'revenue', label: 'Revenue' },
]

function today() {
  return new Date().toISOString().slice(0, 10)
}

function ExportButtons({ onExport }: { onExport: (format: ReportExportFormat) => Promise<void> }) {
  const [pending, setPending] = useState<ReportExportFormat | null>(null)

  const run = async (format: ReportExportFormat) => {
    setPending(format)
    try {
      await onExport(format)
    } catch (err) {
      window.alert(errorMessage(err))
    } finally {
      setPending(null)
    }
  }

  return (
    <div className="flex gap-1">
      <Button variant="secondary" onClick={() => run('csv')} loading={pending === 'csv'}>
        CSV
      </Button>
      <Button variant="secondary" onClick={() => run('excel')} loading={pending === 'excel'}>
        Excel
      </Button>
      <Button variant="secondary" onClick={() => run('pdf')} loading={pending === 'pdf'}>
        PDF
      </Button>
    </div>
  )
}

export function BillingReportsPage() {
  const { hasPermission } = useAuth()
  const canViewIp = hasPermission('BILLING.BILL.VIEWIP')
  // Ward-wise Outstanding and IP Service Billing Detail are inherently IP-only reports —
  // hide them entirely for a role that can't see IP bills at all, rather than showing a tab
  // that just 403s.
  const visibleTabs = TABS.filter((t) => canViewIp || (t.key !== 'ward-outstanding' && t.key !== 'ip-detail'))

  // Deep-linkable via a route segment ("/billing/reports/<key>") — the individual
  // per-report menu items each point at their own tab key. A route param (not a "?tab="
  // query string) so this app's tab-workspace treats each report as a genuinely distinct
  // screen — it reuses an already-open tab for a sidebar click sharing the same pathname
  // (see TabWorkspaceContext.openInActiveTab), so two menu items differing only by query
  // would collapse into "already open" after the first click. Derived straight from the
  // route on every render (no local copy to fall out of sync) — a stale/crafted link to a
  // tab this user can't see (e.g. an IP-only report without BILLING.BILL.VIEWIP) falls back
  // to the default tab rather than rendering content whose query would just 403.
  const { tab: tabParam } = useParams<{ tab?: string }>()
  const tab: Tab = visibleTabs.some((t) => t.key === tabParam) ? (tabParam as Tab) : 'bill-report'
  const navigate = useNavigate()
  const setTab = (next: Tab) => navigate(`/billing/reports/${next}`, { replace: true })

  // Reached via one of the 15 individual report menu items (a route segment, not the bare
  // "/billing/reports"): render just that one report, no tab switcher — the sidebar is the
  // navigation between reports on this path, same as every other single-purpose report page
  // in the app. The bare combined page (tabParam undefined) keeps the full tab bar.
  const isStandalone = tabParam !== undefined
  const currentTabLabel = TABS.find((t) => t.key === tab)?.label ?? 'Billing Reports'

  return (
    <>
      <PageHeader
        title={isStandalone ? currentTabLabel : 'Billing Reports'}
        subtitle={isStandalone ? undefined : 'Bill report, ageing, outstanding, concession, refund, advance, tariff and settlement reports'}
      />

      {!isStandalone && (
        <div className="mb-4 flex flex-wrap gap-1 border-b border-slate-200">
          {visibleTabs.map((t) => (
            <button
              key={t.key}
              onClick={() => setTab(t.key)}
              className={`border-b-2 px-3 py-2 text-sm font-medium ${
                tab === t.key ? 'border-primary-600 text-primary-700' : 'border-transparent text-slate-500 hover:text-slate-700'
              }`}
            >
              {t.label}
            </button>
          ))}
        </div>
      )}

      {/* Every tab below owns its own complete filter card (date filters, report-specific
          fields, Reset, Export) — no shared date row here — followed by a separate,
          unwrapped ReportResultTable/DataTable area, matching BillReportTab/ConcessionTab's
          "one filter card, then the table" shape rather than one box holding everything. */}
      {tab === 'bill-report' && <BillReportTab />}
      {tab === 'ageing' && (
        <GenericReportTab reportKey="ageing" dateMode="single" dateLabel="As On Date" billFilters={{ billType: true }} reportTypeToggle />
      )}
      {tab === 'ward-outstanding' && <WardWiseOutstandingTab />}
      {tab === 'day-book' && (
        <GenericReportTab
          reportKey="day-book"
          dateMode="single"
          dateLabel="Date"
          billFilters={{ billType: true, billedByUser: true, corporate: true }}
        />
      )}
      {tab === 'cancelled' && <GenericReportTab reportKey="cancelled-bills" dateMode="range" />}
      {tab === 'concession' && <ConcessionTab />}
      {tab === 'refund' && <GenericReportTab reportKey="refund" dateMode="range" billFilters={{ billType: true, corporate: true }} />}
      {tab === 'advance' && <GenericReportTab reportKey="advance" dateMode="range" />}
      {tab === 'tariff-list' && <GenericReportTab reportKey="tariff-list" dateMode="none" />}
      {tab === 'doctors-payout' && <GenericReportTab reportKey="doctors-payout" dateMode="range" />}
      {tab === 'user-settlement' && <GenericReportTab reportKey="user-settlement" dateMode="range" />}
      {tab === 'service-breakup' && <GenericReportTab reportKey="service-category-breakup" dateMode="range" />}
      {tab === 'ip-detail' && <GenericReportTab reportKey="ip-service-billing-detail" dateMode="range" />}
      {tab === 'revenue' && <RevenueTab />}
      {tab === 'outstanding' && (
        <GenericReportTab
          reportKey="outstanding"
          dateMode="range"
          billFilters={{ billType: true, billedByUser: true, corporate: true }}
          reportTypeToggle
        />
      )}
    </>
  )
}

type DateMode = 'range' | 'single' | 'none'

/**
 * One generic report tab, backed by BillingReportsController's single GetResult dispatcher —
 * see useBillingReportResult. Owns its own plain, unwrapped filter row (date range/single
 * date/no date per report, plus Patient Name/Bill No once the result reveals it has those
 * columns — see BillingReportService.GetReportResultAsync's Flatten column keys) + Reset +
 * Export — no card/border around the filter row, so the table below is the only "card" on the
 * screen. Date changes are draft-until-Search (an explicit fetch is a real network round
 * trip); Patient Name/Bill No filter the already-fetched rows live (no round trip, so no
 * reason to gate them behind a button). Reports with neither typed field (Ward-wise
 * Outstanding, Tariff List, Doctors Payout, User Settlement, Service-wise Breakup) get no
 * filter field at all — the table's own Columns picker plus its sortable/paged view over the
 * full result is enough for those, no redundant "search all columns" box.
 */
interface BillFilterOptions {
  billType?: boolean
  billedByUser?: boolean
  corporate?: boolean
}

function GenericReportTab({
  reportKey, dateMode, dateLabel = 'Date', billFilters, reportTypeToggle = false,
}: {
  reportKey: string
  dateMode: DateMode
  dateLabel?: string
  /** Which of Bill Type/Billed User/Company selects to show — same fields and RBAC gating as BillReportTab's, opt-in per field since not every report backs all three (e.g. Refund has no billed-user concept). Omit entirely for reports with no real backend support for any of them. */
  billFilters?: BillFilterOptions
  /** Adds a Summary/Detail Select wired to the "reportType" extra filter — Summary groups rows (report-specific shape), Detail is the existing per-row listing. Only reports whose backend actually branches on reportType should set this (currently just Ageing). */
  reportTypeToggle?: boolean
}) {
  const { hasPermission } = useAuth()
  const allowedTypeOptions = ALL_BILL_TYPE_OPTIONS.filter((o) => hasPermission(o.permission))
  const { data: usersResult } = useUsers({ page: 1, pageSize: 500 })
  const { data: corporates = [] } = useCorporates()

  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const [singleDate, setSingleDate] = useState(today())
  const [patientName, setPatientName] = useState('')
  const [billNo, setBillNo] = useState('')
  const [regNo, setRegNo] = useState('')
  const [voucherNo, setVoucherNo] = useState('')
  const [billType, setBillType] = useState('')
  const [billedByUserId, setBilledByUserId] = useState('')
  const [corporateId, setCorporateId] = useState('')
  const [reportType, setReportType] = useState<'summary' | 'detail'>('detail')

  const range = dateMode === 'range' ? { fromDate, toDate } : {}
  const dateParam = dateMode === 'single' ? singleDate : undefined
  const effectiveBillType = allowedTypeOptions.length === 1 ? allowedTypeOptions[0].value : billType
  const extra =
    billFilters || reportTypeToggle
      ? {
          billType: billFilters?.billType && effectiveBillType ? (Number(effectiveBillType) as 1 | 2 | 3) : undefined,
          billedByUserId: billFilters?.billedByUser && billedByUserId ? Number(billedByUserId) : undefined,
          corporateId: billFilters?.corporate && corporateId ? Number(corporateId) : undefined,
          reportType: reportTypeToggle ? reportType : undefined,
        }
      : undefined
  const { data, isLoading, isError, error } = useBillingReportResult(reportKey, range, dateParam, extra)

  const hasPatientColumn = data?.columns.some((c) => c.key === 'patientName') ?? false
  const hasBillColumn = data?.columns.some((c) => c.key === 'billNumber') ?? false
  const hasRegNoColumn = data?.columns.some((c) => c.key === 'regNo') ?? false
  const hasVoucherNoColumn = data?.columns.some((c) => c.key === 'voucherNo') ?? false
  const patientTerm = patientName.trim().toLowerCase()
  const billTerm = billNo.trim().toLowerCase()
  const regTerm = regNo.trim().toLowerCase()
  const voucherTerm = voucherNo.trim().toLowerCase()
  const extraFilter =
    patientTerm || billTerm || regTerm || voucherTerm
      ? (row: Record<string, unknown>) => {
          if (patientTerm && !String(row.patientName ?? '').toLowerCase().includes(patientTerm)) return false
          if (billTerm && !String(row.billNumber ?? '').toLowerCase().includes(billTerm)) return false
          if (regTerm && !String(row.regNo ?? '').toLowerCase().includes(regTerm)) return false
          if (voucherTerm && !String(row.voucherNo ?? '').toLowerCase().includes(voucherTerm)) return false
          return true
        }
      : undefined

  const resetFilters = () => {
    setRange(defaultDateRange(30))
    setSingleDate(today())
    setPatientName('')
    setBillNo('')
    setRegNo('')
    setVoucherNo('')
    setBillType('')
    setBilledByUserId('')
    setCorporateId('')
    setReportType('detail')
  }

  return (
    <>
      <div className="mb-4 rounded-shell border border-border-soft bg-panel p-4 shadow-shell-sm">
        <div className="flex flex-wrap items-end gap-3">
          {hasRegNoColumn && (
            <div className="w-40">
              <Input label="Register No" value={regNo} onChange={(e) => setRegNo(e.target.value)} />
            </div>
          )}
          {hasPatientColumn && (
            <div className="w-48">
              <Input label="Patient Name" value={patientName} onChange={(e) => setPatientName(e.target.value)} />
            </div>
          )}
          {hasVoucherNoColumn && (
            <div className="w-40">
              <Input label="Voucher No" value={voucherNo} onChange={(e) => setVoucherNo(e.target.value)} />
            </div>
          )}
          {hasBillColumn && (
            <div className="w-48">
              <Input label="Bill No" value={billNo} onChange={(e) => setBillNo(e.target.value)} />
            </div>
          )}
          {reportTypeToggle && (
            <div className="w-36">
              <Select
                label="Report Type"
                value={reportType}
                onChange={(e) => setReportType(e.target.value as 'summary' | 'detail')}
                options={[
                  { value: 'detail', label: 'Detail' },
                  { value: 'summary', label: 'Summary' },
                ]}
              />
            </div>
          )}
          {billFilters?.billType && allowedTypeOptions.length > 1 && (
            <div className="w-36">
              <Select
                label="Bill Type"
                placeholder="All"
                value={billType}
                onChange={(e) => setBillType(e.target.value)}
                options={allowedTypeOptions.map((o) => ({ value: o.value, label: o.label }))}
              />
            </div>
          )}
          {billFilters?.billedByUser && (
            <div className="w-44">
              <Select
                label="Billed User"
                placeholder="All"
                value={billedByUserId}
                onChange={(e) => setBilledByUserId(e.target.value)}
                options={(usersResult?.items ?? []).map((u) => ({ value: u.id, label: u.fullName }))}
              />
            </div>
          )}
          {billFilters?.corporate && (
            <div className="w-44">
              <Select
                label="Company"
                placeholder="All"
                value={corporateId}
                onChange={(e) => setCorporateId(e.target.value)}
                options={corporates.map((c) => ({ value: c.id, label: c.name }))}
              />
            </div>
          )}
          {dateMode === 'range' && (
            <>
              <div className="w-40">
                <Input label="From Date" type="date" value={fromDate} onChange={(e) => setRange((r) => ({ ...r, fromDate: e.target.value }))} />
              </div>
              <div className="w-40">
                <Input label="To Date" type="date" value={toDate} onChange={(e) => setRange((r) => ({ ...r, toDate: e.target.value }))} />
              </div>
            </>
          )}
          {dateMode === 'single' && (
            <div className="w-48">
              <Input label={dateLabel} type="date" value={singleDate} onChange={(e) => setSingleDate(e.target.value)} />
            </div>
          )}
          <div className="ml-auto flex gap-2">
            <Button variant="secondary" onClick={resetFilters}>Reset</Button>
            <ExportButtons onExport={(format) => downloadBillingReport(reportKey, format, range, dateParam, extra)} />
          </div>
        </div>
      </div>

      <ReportResultTable result={data} isLoading={isLoading} errorMessage={isError ? errorMessage(error) : undefined} extraFilter={extraFilter} />
    </>
  )
}

/**
 * "Revenue Report" — the legacy screen's Bill Type/Company/Ordering Doctor/Service/Headers
 * (Service Category)/User Department/Paid-Credit filters, each a real backend filter (see
 * BillingReportService.GetRevenueReportAsync), plus the legacy "Report Type" Summary/Detail
 * toggle. Deliberately not a "Matrix"/"Header Group"/column-pivot builder like the legacy
 * screen's — no report in this app dynamically pivots columns/groups (the Custom Report
 * Builder's own executor is fixed-query specifically to avoid building raw SQL from a report
 * code), so this stays the same fixed (columns, rows) shape as every other Billing report,
 * just with a wider filter set and two possible column sets depending on Report Type.
 */
function RevenueTab() {
  const { hasPermission } = useAuth()
  const allowedTypeOptions = ALL_BILL_TYPE_OPTIONS.filter((o) => hasPermission(o.permission))
  const { data: corporates = [] } = useCorporates()
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 500 })
  const { data: servicesResult } = useServices({ page: 1, pageSize: 500 })
  const { data: serviceCategories = [] } = useSimpleMaster('service-categories', endpoints.masters.serviceCategories)
  const { data: departments = [] } = useSimpleMaster('departments', endpoints.masters.departments)

  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const [reportType, setReportType] = useState<'summary' | 'detail'>('summary')
  const [billType, setBillType] = useState('')
  const [corporateId, setCorporateId] = useState('')
  const [doctorId, setDoctorId] = useState('')
  const [serviceId, setServiceId] = useState('')
  const [serviceCategoryId, setServiceCategoryId] = useState('')
  const [departmentId, setDepartmentId] = useState('')
  const [isCredit, setIsCredit] = useState('')

  const range = { fromDate, toDate }
  const effectiveBillType = allowedTypeOptions.length === 1 ? allowedTypeOptions[0].value : billType
  const extra = {
    reportType,
    billType: effectiveBillType ? (Number(effectiveBillType) as 1 | 2 | 3) : undefined,
    corporateId: corporateId ? Number(corporateId) : undefined,
    doctorId: doctorId ? Number(doctorId) : undefined,
    serviceId: serviceId ? Number(serviceId) : undefined,
    serviceCategoryId: serviceCategoryId ? Number(serviceCategoryId) : undefined,
    departmentId: departmentId ? Number(departmentId) : undefined,
    isCredit: isCredit ? isCredit === 'credit' : undefined,
  }
  const { data, isLoading, isError, error } = useBillingReportResult('revenue', range, undefined, extra)

  const resetFilters = () => {
    setRange(defaultDateRange(30))
    setReportType('summary')
    setBillType('')
    setCorporateId('')
    setDoctorId('')
    setServiceId('')
    setServiceCategoryId('')
    setDepartmentId('')
    setIsCredit('')
  }

  return (
    <>
      <div className="mb-4 rounded-shell border border-border-soft bg-panel p-4 shadow-shell-sm">
        <div className="flex flex-wrap items-end gap-3">
          <div className="w-36">
            <Select
              label="Report Type"
              value={reportType}
              onChange={(e) => setReportType(e.target.value as 'summary' | 'detail')}
              options={[
                { value: 'summary', label: 'Summary' },
                { value: 'detail', label: 'Detail' },
              ]}
            />
          </div>
          {allowedTypeOptions.length > 1 && (
            <div className="w-36">
              <Select
                label="Bill Type"
                placeholder="All"
                value={billType}
                onChange={(e) => setBillType(e.target.value)}
                options={allowedTypeOptions.map((o) => ({ value: o.value, label: o.label }))}
              />
            </div>
          )}
          <div className="w-44">
            <Select
              label="Company"
              placeholder="All"
              value={corporateId}
              onChange={(e) => setCorporateId(e.target.value)}
              options={corporates.map((c) => ({ value: c.id, label: c.name }))}
            />
          </div>
          <div className="w-44">
            <Select
              label="Ordering Doctor"
              placeholder="All"
              value={doctorId}
              onChange={(e) => setDoctorId(e.target.value)}
              options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
            />
          </div>
          <div className="w-44">
            <Select
              label="Service"
              placeholder="All"
              value={serviceId}
              onChange={(e) => setServiceId(e.target.value)}
              options={(servicesResult?.items ?? []).map((s) => ({ value: s.id, label: s.name }))}
            />
          </div>
          <div className="w-40">
            <Select
              label="Headers"
              placeholder="All"
              value={serviceCategoryId}
              onChange={(e) => setServiceCategoryId(e.target.value)}
              options={serviceCategories.map((c) => ({ value: c.id, label: c.name }))}
            />
          </div>
          <div className="w-40">
            <Select
              label="User Department"
              placeholder="All"
              value={departmentId}
              onChange={(e) => setDepartmentId(e.target.value)}
              options={departments.map((d) => ({ value: d.id, label: d.name }))}
            />
          </div>
          <div className="w-36">
            <Select
              label="Paid/Credit"
              placeholder="All"
              value={isCredit}
              onChange={(e) => setIsCredit(e.target.value)}
              options={[
                { value: 'paid', label: 'Paid' },
                { value: 'credit', label: 'Credit' },
              ]}
            />
          </div>
          <div className="w-40">
            <Input label="From Date" type="date" value={fromDate} onChange={(e) => setRange((r) => ({ ...r, fromDate: e.target.value }))} />
          </div>
          <div className="w-40">
            <Input label="To Date" type="date" value={toDate} onChange={(e) => setRange((r) => ({ ...r, toDate: e.target.value }))} />
          </div>
          <div className="ml-auto flex gap-2">
            <Button variant="secondary" onClick={resetFilters}>Reset</Button>
            <ExportButtons onExport={(format) => downloadBillingReport('revenue', format, range, undefined, extra)} />
          </div>
        </div>
      </div>

      <ReportResultTable result={data} isLoading={isLoading} errorMessage={isError ? errorMessage(error) : undefined} />
    </>
  )
}

/**
 * "Ward-wise Outstanding" — IP-only, gated by BILLING.BILL.VIEWIP same as before. Report Type
 * toggles between the original per-ward summary (Ward, Bills, Balance) and a per-admission
 * Detail view (see BillingReportService.GetWardWiseOutstandingDetailAsync) — Detail's
 * "Room Charges"/"Service Amount" is a real BillItem.SourceType split, not the legacy screen's
 * Investigation/Pharmacy/Other-Service breakdown (this schema has no seeded category taxonomy
 * for that, and IP pharmacy issues never post to the Bill at all). Paid/Credit now also
 * surfaces overpaid admissions (negative balance) that the report silently excluded before —
 * legacy "CR/DR" is just the sign of our own BalanceAmount.
 */
function WardWiseOutstandingTab() {
  const { data: corporates = [] } = useCorporates()
  const { data: wards = [] } = useWards()
  const { data: patientCategories = [] } = usePatientCategories()
  const { data: departments = [] } = useSimpleMaster('departments', endpoints.masters.departments)

  const [reportType, setReportType] = useState<'summary' | 'detail'>('summary')
  const [ipNumber, setIpNumber] = useState('')
  const [wardId, setWardId] = useState('')
  const [corporateId, setCorporateId] = useState('')
  const [patientCategoryId, setPatientCategoryId] = useState('')
  const [departmentId, setDepartmentId] = useState('')
  const [isCredit, setIsCredit] = useState('')

  const extra = {
    reportType,
    ipNumber: ipNumber.trim() || undefined,
    wardId: wardId ? Number(wardId) : undefined,
    corporateId: corporateId ? Number(corporateId) : undefined,
    patientCategoryId: patientCategoryId ? Number(patientCategoryId) : undefined,
    departmentId: departmentId ? Number(departmentId) : undefined,
    isCredit: isCredit ? isCredit === 'credit' : undefined,
  }
  const { data, isLoading, isError, error } = useBillingReportResult('ward-wise-outstanding', {}, undefined, extra)

  const resetFilters = () => {
    setReportType('summary')
    setIpNumber('')
    setWardId('')
    setCorporateId('')
    setPatientCategoryId('')
    setDepartmentId('')
    setIsCredit('')
  }

  return (
    <>
      <div className="mb-4 rounded-shell border border-border-soft bg-panel p-4 shadow-shell-sm">
        <div className="flex flex-wrap items-end gap-3">
          <div className="w-36">
            <Select
              label="Report Type"
              value={reportType}
              onChange={(e) => setReportType(e.target.value as 'summary' | 'detail')}
              options={[
                { value: 'summary', label: 'Summary' },
                { value: 'detail', label: 'Detail' },
              ]}
            />
          </div>
          <div className="w-40">
            <Input label="IP Number" value={ipNumber} onChange={(e) => setIpNumber(e.target.value)} />
          </div>
          <div className="w-40">
            <Select
              label="Ward"
              placeholder="All"
              value={wardId}
              onChange={(e) => setWardId(e.target.value)}
              options={wards.map((w) => ({ value: w.id, label: w.name }))}
            />
          </div>
          <div className="w-44">
            <Select
              label="Corporate"
              placeholder="All"
              value={corporateId}
              onChange={(e) => setCorporateId(e.target.value)}
              options={corporates.map((c) => ({ value: c.id, label: c.name }))}
            />
          </div>
          <div className="w-40">
            <Select
              label="Patient Type"
              placeholder="All"
              value={patientCategoryId}
              onChange={(e) => setPatientCategoryId(e.target.value)}
              options={patientCategories.map((c) => ({ value: c.id, label: c.name }))}
            />
          </div>
          <div className="w-40">
            <Select
              label="Department"
              placeholder="All"
              value={departmentId}
              onChange={(e) => setDepartmentId(e.target.value)}
              options={departments.map((d) => ({ value: d.id, label: d.name }))}
            />
          </div>
          <div className="w-32">
            <Select
              label="CR/DR"
              placeholder="All"
              value={isCredit}
              onChange={(e) => setIsCredit(e.target.value)}
              options={[
                { value: 'debit', label: 'Debit' },
                { value: 'credit', label: 'Credit' },
              ]}
            />
          </div>
          <div className="ml-auto flex gap-2">
            <Button variant="secondary" onClick={resetFilters}>Reset</Button>
            <ExportButtons onExport={(format) => downloadBillingReport('ward-wise-outstanding', format, {}, undefined, extra)} />
          </div>
        </div>
      </div>

      <ReportResultTable result={data} isLoading={isLoading} errorMessage={isError ? errorMessage(error) : undefined} />
    </>
  )
}

const billStatusTone: Record<BillStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info',
  2: 'warning',
  3: 'success',
  4: 'neutral',
  5: 'warning',
}

const ALL_BILL_TYPE_OPTIONS = [
  { value: '1', label: 'OP', permission: 'BILLING.BILL.VIEWOP' },
  { value: '2', label: 'IP', permission: 'BILLING.BILL.VIEWIP' },
  { value: '3', label: 'Emergency', permission: 'BILLING.BILL.VIEWEMERGENCY' },
] as const

function BillReportTab() {
  const { hasPermission } = useAuth()
  // RBAC-driven visibility: a role/user holding only one of VIEWOP/VIEWIP/VIEWEMERGENCY
  // never sees the Bill Type filter at all — there's nothing to choose, so the query is
  // pinned to that one type. Holding two or three shows a Select limited to those options.
  const allowedTypeOptions = ALL_BILL_TYPE_OPTIONS.filter((o) => hasPermission(o.permission))

  const [search, setSearch] = useState('')
  const [billType, setBillType] = useState('')
  const [corporateId, setCorporateId] = useState('')
  const [billedByUserId, setBilledByUserId] = useState('')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)

  const { data: usersResult } = useUsers({ page: 1, pageSize: 500 })
  const { data: corporates = [] } = useCorporates()

  const effectiveBillType = allowedTypeOptions.length === 1 ? allowedTypeOptions[0].value : billType

  const query = {
    page,
    pageSize,
    search: search || undefined,
    billType: effectiveBillType ? (Number(effectiveBillType) as 1 | 2 | 3) : undefined,
    corporateId: corporateId ? Number(corporateId) : undefined,
    billedByUserId: billedByUserId ? Number(billedByUserId) : undefined,
    fromDate: fromDate || undefined,
    toDate: toDate || undefined,
  }
  const { data, isLoading } = useBills(query)

  const reset = () => {
    setSearch('')
    setBillType('')
    setCorporateId('')
    setBilledByUserId('')
    setFromDate('')
    setToDate('')
    setPage(1)
  }

  const columns: Column<BillListItem>[] = [
    { header: 'Bill No', cell: (row) => <span className="font-mono text-xs">{row.billNumber}</span> },
    { header: 'Date', cell: (row) => row.billDate },
    { header: 'Patient', cell: (row) => `${row.patientName} (${row.patientUhid})` },
    { header: 'Type', cell: (row) => BILL_TYPE_LABELS[row.billType] },
    { header: 'Status', cell: (row) => <StatusBadge tone={billStatusTone[row.status]}>{BILL_STATUS_LABELS[row.status]}</StatusBadge> },
    { header: 'Total', className: 'text-right', cell: (row) => row.totalAmount.toFixed(2) },
    { header: 'Paid', className: 'text-right', cell: (row) => row.paidAmount.toFixed(2) },
    { header: 'Balance', className: 'text-right', cell: (row) => row.balanceAmount.toFixed(2) },
  ]

  return (
    <>
      <div className="mb-4 rounded-shell border border-border-soft bg-panel p-4 shadow-shell-sm">
        <div className="flex flex-wrap items-end gap-3">
          <div className="w-56">
            <Input label="Bill No / Patient / UHID" value={search} onChange={(e) => setSearch(e.target.value)} />
          </div>
          {allowedTypeOptions.length > 1 && (
            <div className="w-36">
              <Select
                label="Bill Type"
                placeholder="All"
                value={billType}
                onChange={(e) => setBillType(e.target.value)}
                options={allowedTypeOptions.map((o) => ({ value: o.value, label: o.label }))}
              />
            </div>
          )}
          <div className="w-44">
            <Select
              label="Billed User"
              placeholder="All"
              value={billedByUserId}
              onChange={(e) => setBilledByUserId(e.target.value)}
              options={(usersResult?.items ?? []).map((u) => ({ value: u.id, label: u.fullName }))}
            />
          </div>
          <div className="w-44">
            <Select
              label="Company"
              placeholder="All"
              value={corporateId}
              onChange={(e) => setCorporateId(e.target.value)}
              options={corporates.map((c) => ({ value: c.id, label: c.name }))}
            />
          </div>
          <div className="w-40">
            <Input label="From date" type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
          </div>
          <div className="w-40">
            <Input label="To date" type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
          </div>
          <div className="ml-auto flex gap-2">
            <Button onClick={() => setPage(1)}>Go</Button>
            <Button variant="secondary" onClick={reset}>Reset</Button>
            <ExportButtons onExport={(format) => downloadBillReport(query, format)} />
          </div>
        </div>
      </div>

      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onPageSizeChange={(size) => {
          setPageSize(size)
          setPage(1)
        }}
        rowKey={(row) => row.id}
        emptyMessage="No bills match this search."
        enableColumnPicker
        showTopPager
      />
    </>
  )
}

const emptyConcessionFilters = { regNo: '', patientName: '', tokenNo: '', billNo: '' }

/**
 * The legacy "OP Bill Concession Search" screen — a genuine search (Reg No/Patient Name/OT
 * No/Bill No), not a plain date-ranged list. "OT No" is the OPD visit's daily token number
 * (OpVisit.TokenNumber). "Requested/Approved User"/"Requested/Approved Date" is one legacy
 * column each, but a concession really has two distinct people/times — both are shown,
 * stacked, in one cell rather than dropping the requester.
 */
function ConcessionTab() {
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const [filters, setFilters] = useState(emptyConcessionFilters)
  const [query, setQuery] = useState<ConcessionSearchQuery>({ page: 1, pageSize: 10, fromDate, toDate })
  const { data, isLoading } = useConcessionSearch(query)

  const runSearch = () => {
    setQuery({
      page: 1,
      pageSize: 10,
      fromDate,
      toDate,
      regNo: filters.regNo.trim() || undefined,
      patientName: filters.patientName.trim() || undefined,
      tokenNo: filters.tokenNo.trim() ? Number(filters.tokenNo.trim()) : undefined,
      billNo: filters.billNo.trim() || undefined,
    })
  }

  const resetSearch = () => {
    setFilters(emptyConcessionFilters)
    const defaults = defaultDateRange(30)
    setRange(defaults)
    setQuery({ page: 1, pageSize: 10, ...defaults })
  }

  const columns: Column<ConcessionSearchResult>[] = [
    { header: 'Bill No', cell: (row) => <span className="font-mono text-xs">{row.billNumber}</span> },
    { header: 'Bill Date', cell: (row) => row.billDate },
    { header: 'Reg No', cell: (row) => <span className="font-mono text-xs">{row.regNo}</span> },
    { header: 'Patient Name', cell: (row) => row.patientName },
    { header: 'Age / Gender', cell: (row) => `${row.age} / ${BILLING_GENDER_LABELS[row.gender]}` },
    { header: 'Doctor / Dept', cell: (row) => [row.doctorName, row.specialtyName].filter(Boolean).join(' / ') || '—' },
    { header: 'Bill Amount', className: 'text-right', cell: (row) => row.billAmount.toFixed(2) },
    { header: 'Concession', className: 'text-right', cell: (row) => `${row.requestedPercent}% / ${row.requestedAmount.toFixed(2)}` },
    { header: 'Reason', cell: (row) => row.reason ?? '—' },
    {
      header: 'Requested / Approved',
      cell: (row) => (
        <div className="text-xs">
          <div>Requested: {row.requestedByName} ({new Date(row.requestedAt).toLocaleString()})</div>
          <div className="text-slate-500">
            Approved: {row.approvedByName ?? '—'}{row.approvedAt ? ` (${new Date(row.approvedAt).toLocaleString()})` : ''}
          </div>
        </div>
      ),
    },
  ]

  return (
    <>
      <div className="mb-4 rounded-shell border border-border-soft bg-panel p-4 shadow-shell-sm">
        <div className="flex flex-wrap items-end gap-3">
          <div className="w-36">
            <Input label="Reg No" value={filters.regNo} onChange={(e) => setFilters((f) => ({ ...f, regNo: e.target.value }))} />
          </div>
          <div className="w-48">
            <Input label="Patient Name" value={filters.patientName} onChange={(e) => setFilters((f) => ({ ...f, patientName: e.target.value }))} />
          </div>
          <div className="w-32">
            <Input label="OT No" value={filters.tokenNo} onChange={(e) => setFilters((f) => ({ ...f, tokenNo: e.target.value }))} />
          </div>
          <div className="w-40">
            <Input label="Bill No" value={filters.billNo} onChange={(e) => setFilters((f) => ({ ...f, billNo: e.target.value }))} />
          </div>
          <div className="w-40">
            <Input label="From Date" type="date" value={fromDate} onChange={(e) => setRange((r) => ({ ...r, fromDate: e.target.value }))} />
          </div>
          <div className="w-40">
            <Input label="To Date" type="date" value={toDate} onChange={(e) => setRange((r) => ({ ...r, toDate: e.target.value }))} />
          </div>
          <div className="ml-auto flex gap-2">
            <Button variant="secondary" onClick={resetSearch}>Cancel</Button>
            <Button onClick={runSearch}>Search</Button>
            <ExportButtons onExport={(format) => downloadBillingReport('concession', format, { fromDate, toDate })} />
          </div>
        </div>
      </div>

      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={query.page ?? 1}
        onPageChange={(page) => setQuery((q) => ({ ...q, page }))}
        onPageSizeChange={(pageSize) => setQuery((q) => ({ ...q, page: 1, pageSize }))}
        rowKey={(row) => row.billId}
        emptyMessage="No approved concessions found for these filters."
        enableColumnPicker
        showTopPager
      />
    </>
  )
}
