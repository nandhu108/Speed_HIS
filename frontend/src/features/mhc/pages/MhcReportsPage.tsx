import { useEffect, useRef, useState, type UIEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { endpoints } from '../../../api/endpoints'
import { Button } from '../../../components/ui/Button'
import { DateRangeFilter, defaultDateRange } from '../../../components/ui/DateRangeFilter'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { BILL_TYPE_LABELS, type BillType } from '../../billing/types'
import { useServices, useSimpleMaster } from '../../masters/api'
import { Tabs } from '../../masters/components/Tabs'
import {
  downloadMhcDoctorWiseReport,
  downloadMhcPackageWiseReport,
  downloadMhcPatientListReport,
  downloadMhcReferrerWiseReport,
  downloadMhcServiceListReport,
  useMhcDoctorWiseReport,
  useMhcPackageWiseReport,
  useMhcPatientListReport,
  useMhcReferrerWiseReport,
  useMhcServiceListReport,
  type MhcReportFilter,
  type MhcServiceListReportFilter,
} from '../api'
import { MHC_VISIT_TYPE_LABELS, MHC_VISIT_TYPES, type MhcReportResult, type MhcVisitType } from '../types'

const tabs = [
  { key: 'package', label: 'Package-wise' },
  { key: 'doctor', label: 'Doctor-wise' },
  { key: 'referrer', label: 'Referrer-wise' },
  { key: 'patient-list', label: 'Patient List' },
  { key: 'service-list', label: 'Service List' },
] as const
type TabKey = (typeof tabs)[number]['key']

/** "MHC Reports" — package/panel-doctor/referrer breakdowns over MHC registration data
 * (`opd.op_visits` where Category == Mhc, reusing OpReportService's engine), plus a Service List
 * report over MHC visits' billed Service line items (a different query shape entirely — see
 * OpReportService.GetMhcServiceListReportAsync). "Department" isn't offered as a filter here:
 * MHC visits carry MhcDoctorId, not DoctorId/DoctorDepartments, so there's no department link to
 * filter on (unlike the OP/IP reports). */
const isTabKey = (value: string | null): value is TabKey => tabs.some((t) => t.key === value)

export function MhcReportsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const requestedTab = searchParams.get('tab')
  const [tab, setTab] = useState<TabKey>(isTabKey(requestedTab) ? requestedTab : 'package')

  const changeTab = (next: TabKey) => {
    setTab(next)
    setSearchParams((prev) => {
      const params = new URLSearchParams(prev)
      params.set('tab', next)
      return params
    }, { replace: true })
  }

  return (
    <>
      <PageHeader title="MHC Reports" subtitle="Package-wise, doctor-wise, referrer-wise and patient-list MHC registration views, plus a billed-service listing" />
      <Tabs tabs={tabs} active={tab} onChange={changeTab} />
      {tab === 'package' && (
        <MhcReportTab
          useReport={useMhcPackageWiseReport}
          download={downloadMhcPackageWiseReport}
          emptyMessage="No MHC registrations for this range."
        />
      )}
      {tab === 'doctor' && (
        <MhcReportTab
          useReport={useMhcDoctorWiseReport}
          download={downloadMhcDoctorWiseReport}
          emptyMessage="No MHC registrations for this range."
        />
      )}
      {tab === 'referrer' && (
        <MhcReportTab
          useReport={useMhcReferrerWiseReport}
          download={downloadMhcReferrerWiseReport}
          emptyMessage="No MHC registrations for this range."
        />
      )}
      {tab === 'patient-list' && (
        <MhcReportTab
          useReport={useMhcPatientListReport}
          download={downloadMhcPatientListReport}
          emptyMessage="No MHC registrations for this range."
        />
      )}
      {tab === 'service-list' && <MhcServiceListReportTab />}
    </>
  )
}

function ColumnPicker({
  result, hiddenColumns, setHiddenColumns, toggleColumn,
}: {
  result: MhcReportResult
  hiddenColumns: Set<string>
  setHiddenColumns: (next: Set<string>) => void
  toggleColumn: (key: string) => void
}) {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    const onClickOutside = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', onClickOutside)
    return () => document.removeEventListener('mousedown', onClickOutside)
  }, [open])

  const visibleCount = result.columns.filter((c) => !hiddenColumns.has(c.key)).length

  return (
    <div ref={ref} className="relative ml-auto">
      <Button variant="secondary" onClick={() => setOpen((o) => !o)}>
        Columns ({visibleCount}/{result.columns.length})
      </Button>
      {open && (
        <div className="absolute right-0 z-20 mt-1 max-h-80 w-64 overflow-y-auto rounded-md border border-border-soft bg-panel-solid p-2 shadow-shell-md">
          <div className="mb-1 flex justify-between border-b border-border-soft pb-1">
            <button type="button" className="text-xs font-medium text-accent-2" onClick={() => setHiddenColumns(new Set())}>
              Show all
            </button>
            <button
              type="button"
              className="text-xs font-medium text-ink-faint"
              onClick={() => setHiddenColumns(new Set(result.columns.map((c) => c.key)))}
            >
              Hide all
            </button>
          </div>
          {result.columns.map((c) => (
            <label key={c.key} className="flex items-center gap-2 rounded px-1 py-1 text-sm text-ink hover:bg-panel-2">
              <input type="checkbox" checked={!hiddenColumns.has(c.key)} onChange={() => toggleColumn(c.key)} className="accent-accent-2" />
              {c.label}
            </label>
          ))}
        </div>
      )}
    </div>
  )
}

function ReportTable({
  result, hiddenColumns, emptyMessage = 'No rows.',
}: {
  result: MhcReportResult
  hiddenColumns: Set<string>
  emptyMessage?: string
}) {
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(25)
  const pageSizeOptions = [10, 25, 50, 100, 200]
  const tableMaxHeightPx = 520
  const topScrollRef = useRef<HTMLDivElement>(null)
  const bottomScrollRef = useRef<HTMLDivElement>(null)
  const tableResizeObserverRef = useRef<ResizeObserver | null>(null)
  const [contentWidth, setContentWidth] = useState(0)

  useEffect(() => setPage(1), [result])

  const tableRef = (el: HTMLTableElement | null) => {
    tableResizeObserverRef.current?.disconnect()
    tableResizeObserverRef.current = null
    if (!el) return
    const observer = new ResizeObserver(() => setContentWidth(el.scrollWidth))
    observer.observe(el)
    tableResizeObserverRef.current = observer
    setContentWidth(el.scrollWidth)
  }
  const syncScrollFrom = (source: 'top' | 'bottom') => (e: UIEvent<HTMLDivElement>) => {
    const target = source === 'top' ? bottomScrollRef.current : topScrollRef.current
    if (target) target.scrollLeft = e.currentTarget.scrollLeft
  }

  const visibleColumns = result.columns.filter((c) => !hiddenColumns.has(c.key))
  const totalRows = result.rows.length
  const totalPages = Math.max(1, Math.ceil(totalRows / pageSize))
  const pagedRows = result.rows.slice((page - 1) * pageSize, page * pageSize)

  const pager = (
    <div className="flex flex-wrap items-center justify-between gap-4 text-sm text-ink-muted">
      <span>Page {page} of {totalPages} · {totalRows} row{totalRows === 1 ? '' : 's'}</span>
      <div className="flex items-center gap-3">
        <label className="flex items-center gap-1.5">
          Rows per page
          <select
            value={pageSize}
            onChange={(e) => { setPageSize(Number(e.target.value)); setPage(1) }}
            className="rounded-md border border-border-soft bg-panel-solid px-2 py-1 text-ink"
          >
            {pageSizeOptions.map((size) => <option key={size} value={size}>{size}</option>)}
          </select>
        </label>
        <div className="flex gap-1">
          <button type="button" disabled={page <= 1} onClick={() => setPage((p) => p - 1)} className="rounded-md border border-border-soft px-3 py-1 hover:border-border-strong disabled:opacity-40">Previous</button>
          <button type="button" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)} className="rounded-md border border-border-soft px-3 py-1 hover:border-border-strong disabled:opacity-40">Next</button>
        </div>
      </div>
    </div>
  )

  return (
    <>
      <div className="mb-2">{pager}</div>
      <div ref={topScrollRef} onScroll={syncScrollFrom('top')} className="overflow-x-auto overflow-y-hidden" style={{ height: 14 }}>
        <div style={{ width: contentWidth, height: 1 }} />
      </div>
      <div ref={bottomScrollRef} onScroll={syncScrollFrom('bottom')} className="overflow-auto" style={{ maxHeight: tableMaxHeightPx }}>
        <table ref={tableRef} className="w-full text-sm">
          <thead>
            <tr className="text-left">
              {visibleColumns.map((c) => (
                <th key={c.key} className="sticky top-0 z-10 whitespace-nowrap border-b border-border-soft bg-panel-solid px-3 py-2 font-medium text-ink-muted">{c.label}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {pagedRows.map((row, i) => (
              <tr key={i} className="border-b border-border-soft last:border-0">
                {visibleColumns.map((c) => <td key={c.key} className="px-3 py-2 whitespace-nowrap text-ink">{String(row[c.key] ?? '')}</td>)}
              </tr>
            ))}
            {result.rows.length === 0 && (
              <tr><td colSpan={visibleColumns.length} className="px-3 py-8 text-center text-ink-faint">{emptyMessage}</td></tr>
            )}
          </tbody>
        </table>
      </div>
      <div className="mt-3">{pager}</div>
    </>
  )
}

function MhcReportTab({
  useReport, download, emptyMessage,
}: {
  useReport: (filter: MhcReportFilter, enabled: boolean) => { data?: MhcReportResult; isFetching: boolean; refetch: () => Promise<unknown> }
  download: (filter: MhcReportFilter, format: 'csv' | 'excel' | 'pdf') => Promise<void>
  emptyMessage: string
}) {
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const [mhcDoctorId, setMhcDoctorId] = useState('')
  const [visitType, setVisitType] = useState('')
  const [patientCategoryId, setPatientCategoryId] = useState('')
  const [regNo, setRegNo] = useState('')
  const [patientName, setPatientName] = useState('')
  const [hasRun, setHasRun] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [hiddenColumns, setHiddenColumns] = useState<Set<string>>(new Set())

  const { data: mhcDoctors = [] } = useSimpleMaster('mhcdoctors', endpoints.masters.mhcDoctors)
  const { data: patientCategories = [] } = useSimpleMaster('patientcategories', endpoints.masters.patientCategories)

  const filter: MhcReportFilter = {
    fromDate, toDate,
    mhcDoctorId: mhcDoctorId ? Number(mhcDoctorId) : null,
    visitType: visitType ? (Number(visitType) as MhcVisitType) : null,
    patientCategoryId: patientCategoryId ? Number(patientCategoryId) : null,
    regNo: regNo.trim() || null,
    patientName: patientName.trim() || null,
  }
  const { data: result, isFetching, refetch } = useReport(filter, hasRun)

  useEffect(() => setHiddenColumns(new Set()), [result?.columns])

  const run = async () => {
    setError(null)
    setHasRun(true)
    await refetch()
  }
  const resetFilters = () => {
    setMhcDoctorId(''); setVisitType(''); setPatientCategoryId(''); setRegNo(''); setPatientName('')
  }
  const doDownload = async (format: 'csv' | 'excel' | 'pdf') => {
    setError(null)
    try {
      await download(filter, format)
    } catch (err) {
      setError(errorMessage(err))
    }
  }
  const toggleColumn = (key: string) =>
    setHiddenColumns((prev) => {
      const next = new Set(prev)
      if (next.has(key)) next.delete(key)
      else next.add(key)
      return next
    })

  return (
    <div className="rounded-lg border border-border-soft bg-panel-solid p-4">
      <div className="mb-4">
        <DateRangeFilter
          fromDate={fromDate} toDate={toDate}
          onFromDate={(v) => setRange((r) => ({ ...r, fromDate: v }))} onToDate={(v) => setRange((r) => ({ ...r, toDate: v }))}
        />
      </div>

      <div className="mb-4 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
        <Input label="UHID" value={regNo} onChange={(e) => setRegNo(e.target.value)} />
        <Input label="Patient Name" value={patientName} onChange={(e) => setPatientName(e.target.value)} />
        <Select
          label="Doctor" placeholder="All" value={mhcDoctorId} onChange={(e) => setMhcDoctorId(e.target.value)}
          options={mhcDoctors.map((d) => ({ value: d.id, label: d.name }))}
        />
        <Select
          label="Visit Type" placeholder="All" value={visitType} onChange={(e) => setVisitType(e.target.value)}
          options={Object.values(MHC_VISIT_TYPES).map((v) => ({ value: v, label: MHC_VISIT_TYPE_LABELS[v] }))}
        />
        <Select
          label="Patient Type" placeholder="All" value={patientCategoryId} onChange={(e) => setPatientCategoryId(e.target.value)}
          options={patientCategories.map((c) => ({ value: c.id, label: c.name }))}
        />
      </div>

      <div className="mb-4 flex flex-wrap items-center gap-2">
        <Button onClick={run} loading={isFetching}>Run</Button>
        <Button variant="secondary" onClick={resetFilters}>Reset Filters</Button>
        {result && hasRun && (
          <>
            <Button variant="secondary" onClick={() => doDownload('csv')}>Export CSV</Button>
            <Button variant="secondary" onClick={() => doDownload('excel')}>Export Excel</Button>
            <Button variant="secondary" onClick={() => doDownload('pdf')}>Export PDF</Button>
            <ColumnPicker result={result} hiddenColumns={hiddenColumns} setHiddenColumns={setHiddenColumns} toggleColumn={toggleColumn} />
          </>
        )}
      </div>

      {error && <p className="mb-3 rounded-md bg-crit/10 px-3 py-2 text-sm text-crit">{error}</p>}
      {isFetching && <Spinner />}

      {result && hasRun && <ReportTable result={result} hiddenColumns={hiddenColumns} emptyMessage={emptyMessage} />}
      {!hasRun && <p className="py-10 text-center text-sm text-ink-faint">Pick filters, then Run.</p>}
    </div>
  )
}

function MhcServiceListReportTab() {
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const [serviceId, setServiceId] = useState('')
  const [billType, setBillType] = useState('')
  const [regNo, setRegNo] = useState('')
  const [hasRun, setHasRun] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [hiddenColumns, setHiddenColumns] = useState<Set<string>>(new Set())

  const { data: servicesResult } = useServices({ page: 1, pageSize: 200 })

  const filter: MhcServiceListReportFilter = {
    fromDate, toDate,
    serviceId: serviceId ? Number(serviceId) : null,
    billType: billType ? (Number(billType) as BillType) : null,
    regNo: regNo.trim() || null,
  }
  const { data: result, isFetching, refetch } = useMhcServiceListReport(filter, hasRun && !!serviceId)

  useEffect(() => setHiddenColumns(new Set()), [result?.columns])

  const run = async () => {
    setError(null)
    setHasRun(true)
    if (serviceId) await refetch()
  }
  const resetFilters = () => { setServiceId(''); setBillType(''); setRegNo('') }
  const download = async (format: 'csv' | 'excel' | 'pdf') => {
    setError(null)
    try {
      await downloadMhcServiceListReport(filter, format)
    } catch (err) {
      setError(errorMessage(err))
    }
  }
  const toggleColumn = (key: string) =>
    setHiddenColumns((prev) => {
      const next = new Set(prev)
      if (next.has(key)) next.delete(key)
      else next.add(key)
      return next
    })

  return (
    <div className="rounded-lg border border-border-soft bg-panel-solid p-4">
      <div className="mb-4">
        <DateRangeFilter
          fromDate={fromDate} toDate={toDate}
          onFromDate={(v) => setRange((r) => ({ ...r, fromDate: v }))} onToDate={(v) => setRange((r) => ({ ...r, toDate: v }))}
        />
      </div>

      <div className="mb-4 grid grid-cols-2 gap-3 sm:grid-cols-3">
        <Input label="UHID" value={regNo} onChange={(e) => setRegNo(e.target.value)} />
        <Select
          label="Bill Type" placeholder="All" value={billType} onChange={(e) => setBillType(e.target.value)}
          options={Object.entries(BILL_TYPE_LABELS).map(([value, label]) => ({ value, label }))}
        />
        <Select
          label="Service *" placeholder="Select a service" value={serviceId} onChange={(e) => setServiceId(e.target.value)}
          options={(servicesResult?.items ?? []).map((s) => ({ value: s.id, label: s.name }))}
        />
      </div>
      {!serviceId && <p className="mb-4 -mt-2 text-xs text-ink-faint">Service is required — this report lists who was billed for one specific service.</p>}

      <div className="mb-4 flex flex-wrap items-center gap-2">
        <Button onClick={run} loading={isFetching} disabled={!serviceId}>Run</Button>
        <Button variant="secondary" onClick={resetFilters}>Reset Filters</Button>
        {result && hasRun && (
          <>
            <Button variant="secondary" onClick={() => download('csv')}>Export CSV</Button>
            <Button variant="secondary" onClick={() => download('excel')}>Export Excel</Button>
            <Button variant="secondary" onClick={() => download('pdf')}>Export PDF</Button>
            <ColumnPicker result={result} hiddenColumns={hiddenColumns} setHiddenColumns={setHiddenColumns} toggleColumn={toggleColumn} />
          </>
        )}
      </div>

      {error && <p className="mb-3 rounded-md bg-crit/10 px-3 py-2 text-sm text-crit">{error}</p>}
      {isFetching && <Spinner />}

      {result && hasRun && <ReportTable result={result} hiddenColumns={hiddenColumns} />}
      {!hasRun && <p className="py-10 text-center text-sm text-ink-faint">Pick a Service, then Run.</p>}
    </div>
  )
}
