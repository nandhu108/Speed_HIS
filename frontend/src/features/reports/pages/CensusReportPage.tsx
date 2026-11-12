import { useEffect, useRef, useState, type UIEvent } from 'react'
import { endpoints } from '../../../api/endpoints'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DateRangeFilter, defaultDateRange } from '../../../components/ui/DateRangeFilter'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { useSimpleMaster, useWards } from '../../masters/api'
import {
  downloadCensusPatientWiseDetails,
  downloadDepartmentWiseCensus,
  useCensusPatientWiseDetails,
  useDepartmentWiseCensus,
  type CensusReportFilter,
} from '../api'
import { CENSUS_SCOPES, type CensusScope } from '../types'

type ReportType = 'opip' | 'op' | 'ip' | 'patient-wise'

const REPORT_TYPES: { value: ReportType; label: string }[] = [
  { value: 'opip', label: 'OPIP Census Department Wise' },
  { value: 'op', label: 'OP Census Department Wise' },
  { value: 'ip', label: 'IP Census Department Wise' },
  { value: 'patient-wise', label: 'Patient Wise Details' },
]

const scopeForReportType: Record<ReportType, CensusScope> = {
  opip: CENSUS_SCOPES.Both,
  op: CENSUS_SCOPES.Op,
  ip: CENSUS_SCOPES.Ip,
  'patient-wise': CENSUS_SCOPES.Both,
}

/**
 * OPIP Census Report — a front-office census blending OP visits and IP admissions,
 * department-wise, split New/Review × Male/Female/Child (the "Child" cutoff is the
 * `Census.ChildAgeThresholdYears` setting, not hardcoded — see Admin → Settings). "Report Type"
 * switches between the combined OPIP table, OP-only, IP-only, and a flat patient-wise listing —
 * one screen covering what the reference tool splits across a dropdown of the same four views.
 * "IP Census with Occupancy Details" isn't built yet — flagged as a follow-up, not silently
 * dropped.
 */
export function CensusReportPage() {
  const [reportType, setReportType] = useState<ReportType>('opip')
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const [departmentId, setDepartmentId] = useState('')
  const [wardId, setWardId] = useState('')
  const [patientCategoryId, setPatientCategoryId] = useState('')
  const [hasRun, setHasRun] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [hiddenColumns, setHiddenColumns] = useState<Set<string>>(new Set())
  const [columnPickerOpen, setColumnPickerOpen] = useState(false)
  const columnPickerRef = useRef<HTMLDivElement>(null)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(25)
  const pageSizeOptions = [10, 25, 50, 100, 200]
  const tableMaxHeightPx = 520

  const topScrollRef = useRef<HTMLDivElement>(null)
  const bottomScrollRef = useRef<HTMLDivElement>(null)
  const tableResizeObserverRef = useRef<ResizeObserver | null>(null)
  const [contentWidth, setContentWidth] = useState(0)

  const tableRef = (el: HTMLTableElement | null) => {
    tableResizeObserverRef.current?.disconnect()
    tableResizeObserverRef.current = null
    if (!el) return
    const observer = new ResizeObserver(() => setContentWidth(el.scrollWidth))
    observer.observe(el)
    tableResizeObserverRef.current = observer
    setContentWidth(el.scrollWidth)
  }

  const { data: departments = [] } = useSimpleMaster('departments', endpoints.masters.departments)
  const { data: wards = [] } = useWards()
  const { data: patientCategories = [] } = useSimpleMaster('patientcategories', endpoints.masters.patientCategories)

  const filter: CensusReportFilter = {
    fromDate, toDate,
    scope: scopeForReportType[reportType],
    departmentId: departmentId ? Number(departmentId) : null,
    wardId: wardId ? Number(wardId) : null,
    patientCategoryId: patientCategoryId ? Number(patientCategoryId) : null,
  }
  const isPatientWise = reportType === 'patient-wise'
  const departmentWise = useDepartmentWiseCensus(filter, hasRun && !isPatientWise)
  const patientWise = useCensusPatientWiseDetails(filter, hasRun && isPatientWise)
  const { data: result, isFetching, refetch } = isPatientWise ? patientWise : departmentWise

  useEffect(() => {
    setHiddenColumns(new Set())
    setPage(1)
  }, [result?.columns])

  useEffect(() => {
    if (!columnPickerOpen) return
    const onClickOutside = (e: MouseEvent) => {
      if (columnPickerRef.current && !columnPickerRef.current.contains(e.target as Node)) {
        setColumnPickerOpen(false)
      }
    }
    document.addEventListener('mousedown', onClickOutside)
    return () => document.removeEventListener('mousedown', onClickOutside)
  }, [columnPickerOpen])

  const syncScrollFrom = (source: 'top' | 'bottom') => (e: UIEvent<HTMLDivElement>) => {
    const target = source === 'top' ? bottomScrollRef.current : topScrollRef.current
    if (target) target.scrollLeft = e.currentTarget.scrollLeft
  }

  const run = async () => {
    setError(null)
    setHasRun(true)
    await refetch()
  }

  const resetFilters = () => {
    setDepartmentId(''); setWardId(''); setPatientCategoryId('')
  }

  const download = async (format: 'csv' | 'excel' | 'pdf') => {
    setError(null)
    try {
      if (isPatientWise) await downloadCensusPatientWiseDetails(filter, format)
      else await downloadDepartmentWiseCensus(filter, format)
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

  const visibleColumns = result?.columns.filter((c) => !hiddenColumns.has(c.key)) ?? []
  const totalRows = result?.rows.length ?? 0
  const totalPages = Math.max(1, Math.ceil(totalRows / pageSize))
  const pagedRows = result?.rows.slice((page - 1) * pageSize, page * pageSize) ?? []

  // A TOTAL row summing every numeric column across *all* matching rows (not just the current
  // page) — the department-wise views are department counts, so a grand total across every
  // department is expected, same as the reference tool's own bottom row. Doesn't apply to the
  // patient-wise flat listing, which has nothing meaningful to sum.
  const columnTotals: Record<string, number> | null =
    !isPatientWise && result
      ? Object.fromEntries(
          visibleColumns
            .filter((c) => c.key !== 'department')
            .map((c) => [c.key, result.rows.reduce((sum, row) => sum + (Number(row[c.key]) || 0), 0)]),
        )
      : null

  const pager = (
    <div className="flex flex-wrap items-center justify-between gap-4 text-sm text-ink-muted">
      <span>
        Page {page} of {totalPages} · {totalRows} row{totalRows === 1 ? '' : 's'}
      </span>
      <div className="flex items-center gap-3">
        <label className="flex items-center gap-1.5">
          Rows per page
          <select
            value={pageSize}
            onChange={(e) => {
              setPageSize(Number(e.target.value))
              setPage(1)
            }}
            className="rounded-md border border-border-soft bg-panel-solid px-2 py-1 text-ink"
          >
            {pageSizeOptions.map((size) => <option key={size} value={size}>{size}</option>)}
          </select>
        </label>
        <div className="flex gap-1">
          <button
            type="button"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            className="rounded-md border border-border-soft px-3 py-1 hover:border-border-strong disabled:opacity-40"
          >
            Previous
          </button>
          <button
            type="button"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
            className="rounded-md border border-border-soft px-3 py-1 hover:border-border-strong disabled:opacity-40"
          >
            Next
          </button>
        </div>
      </div>
    </div>
  )

  return (
    <>
      <PageHeader title="OPIP Census Report" subtitle="Department-wise New/Review × Gender/Child census across OP and IP, plus a patient-wise listing" />

      <div className="rounded-lg border border-border-soft bg-panel-solid p-4">
        <div className="mb-4 flex flex-wrap items-end justify-between gap-4">
          <DateRangeFilter
            fromDate={fromDate} toDate={toDate}
            onFromDate={(v) => setRange((r) => ({ ...r, fromDate: v }))} onToDate={(v) => setRange((r) => ({ ...r, toDate: v }))}
          />
          <Select
            label="Report Type" value={reportType} onChange={(e) => setReportType(e.target.value as ReportType)}
            options={REPORT_TYPES}
          />
        </div>

        <div className="mb-4 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          <Select
            label="Department" placeholder="All" value={departmentId} onChange={(e) => setDepartmentId(e.target.value)}
            options={departments.map((d) => ({ value: d.id, label: d.name }))}
          />
          <Select
            label="Ward" placeholder="All" value={wardId} onChange={(e) => setWardId(e.target.value)}
            options={wards.map((w) => ({ value: w.id, label: w.name }))}
          />
          <Select
            label="Patient Type" placeholder="All" value={patientCategoryId} onChange={(e) => setPatientCategoryId(e.target.value)}
            options={patientCategories.map((c) => ({ value: c.id, label: c.name }))}
          />
        </div>
        {(reportType === 'op' || reportType === 'patient-wise') && wardId && (
          <p className="mb-4 -mt-2 text-xs text-ink-faint">Ward only applies to IP-sourced rows — OP visits aren't tied to a ward.</p>
        )}

        <div className="mb-4 flex flex-wrap items-center gap-2">
          <Button onClick={run} loading={isFetching}>Run</Button>
          <Button variant="secondary" onClick={resetFilters}>Reset Filters</Button>
          {result && hasRun && (
            <>
              <Button variant="secondary" onClick={() => download('csv')}>Export CSV</Button>
              <Button variant="secondary" onClick={() => download('excel')}>Export Excel</Button>
              <Button variant="secondary" onClick={() => download('pdf')}>Export PDF</Button>
              <div ref={columnPickerRef} className="relative ml-auto">
                <Button variant="secondary" onClick={() => setColumnPickerOpen((o) => !o)}>
                  Columns ({visibleColumns.length}/{result.columns.length})
                </Button>
                {columnPickerOpen && (
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
                        <input
                          type="checkbox"
                          checked={!hiddenColumns.has(c.key)}
                          onChange={() => toggleColumn(c.key)}
                          className="accent-accent-2"
                        />
                        {c.label}
                      </label>
                    ))}
                  </div>
                )}
              </div>
            </>
          )}
        </div>

        {error && <p className="mb-3 rounded-md bg-crit/10 px-3 py-2 text-sm text-crit">{error}</p>}
        {isFetching && <Spinner />}

        {result && hasRun && (
          <>
            <div className="mb-2">{pager}</div>

            <div ref={topScrollRef} onScroll={syncScrollFrom('top')} className="overflow-x-auto overflow-y-hidden" style={{ height: 14 }}>
              <div style={{ width: contentWidth, height: 1 }} />
            </div>
            <div
              ref={bottomScrollRef}
              onScroll={syncScrollFrom('bottom')}
              className="overflow-auto"
              style={{ maxHeight: tableMaxHeightPx }}
            >
              <table ref={tableRef} className="w-full text-sm">
                <thead>
                  <tr className="text-left">
                    {visibleColumns.map((c) => (
                      <th key={c.key} className="sticky top-0 z-10 whitespace-nowrap border-b border-border-soft bg-panel-solid px-3 py-2 font-medium text-ink-muted">
                        {c.label}
                      </th>
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
                    <tr><td colSpan={visibleColumns.length} className="px-3 py-8 text-center text-ink-faint">No rows for this range.</td></tr>
                  )}
                </tbody>
                {columnTotals && result.rows.length > 0 && (
                  <tfoot>
                    <tr>
                      {visibleColumns.map((c) => (
                        <td
                          key={c.key}
                          className="sticky bottom-0 whitespace-nowrap border-t border-border-soft bg-panel-solid px-3 py-2 font-semibold text-ink"
                        >
                          {c.key === 'department' ? 'TOTAL' : (columnTotals[c.key] ?? 0)}
                        </td>
                      ))}
                    </tr>
                  </tfoot>
                )}
              </table>
            </div>

            <div className="mt-3">{pager}</div>
          </>
        )}
        {!hasRun && <p className="py-10 text-center text-sm text-ink-faint">Pick a date range, report type and filters, then Run.</p>}
      </div>
    </>
  )
}
