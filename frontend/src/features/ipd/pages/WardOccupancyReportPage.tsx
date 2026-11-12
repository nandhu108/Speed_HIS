import { useEffect, useRef, useState, type UIEvent } from 'react'
import { errorMessage } from '../../../api/axios'
import { endpoints } from '../../../api/endpoints'
import { Button } from '../../../components/ui/Button'
import { DateRangeFilter, defaultDateRange } from '../../../components/ui/DateRangeFilter'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { useDoctors, useSimpleMaster, useWards } from '../../masters/api'
import { downloadWardOccupancyReport, useWardOccupancyReport, type WardOccupancyReportFilter } from '../api'
import { BED_STATUS_LABELS, type BedStatus, type IpReportMode } from '../types'

const WARD_FLAG_OPTIONS = [
  { value: 'icu', label: 'ICU' },
  { value: 'observation', label: 'Observation' },
]

/** Bed Occupancy Report — bed-days occupied vs. available per ward over a required date range.
 * "Bed Status" filters the bed's *current* status (a live operational field, not historical), so
 * applying it against a past date range is necessarily approximate. */
export function WardOccupancyReportPage() {
  const [mode, setMode] = useState<IpReportMode>('summary')
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(7))
  const [wardId, setWardId] = useState('')
  const [doctorId, setDoctorId] = useState('')
  const [unitId, setUnitId] = useState('')
  const [wardFlag, setWardFlag] = useState('')
  const [bedStatus, setBedStatus] = useState('')
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

  const { data: wards = [] } = useWards()
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const { data: units = [] } = useSimpleMaster('units', endpoints.masters.units)

  const filter: WardOccupancyReportFilter = {
    mode, fromDate, toDate,
    wardId: wardId ? Number(wardId) : null,
    doctorId: doctorId ? Number(doctorId) : null,
    unitId: unitId ? Number(unitId) : null,
    wardFlag: wardFlag || null,
    bedStatus: bedStatus ? (Number(bedStatus) as BedStatus) : null,
  }
  const rangeComplete = Boolean(fromDate && toDate)
  const { data: result, isFetching, refetch } = useWardOccupancyReport(filter, hasRun && rangeComplete)

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
    if (rangeComplete) await refetch()
  }

  const resetFilters = () => {
    setWardId(''); setDoctorId(''); setUnitId(''); setWardFlag(''); setBedStatus('')
  }

  const download = async (format: 'csv' | 'excel' | 'pdf') => {
    setError(null)
    try {
      await downloadWardOccupancyReport(filter, format)
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
      <PageHeader title="Bed Occupancy Report" subtitle="Bed-days occupied vs. available per ward over a date range, Summary or Detail, exportable" />

      <div className="rounded-lg border border-border-soft bg-panel-solid p-4">
        <div className="mb-4 flex flex-wrap items-end justify-between gap-4">
          <DateRangeFilter
            fromDate={fromDate} toDate={toDate}
            onFromDate={(v) => setRange((r) => ({ ...r, fromDate: v }))} onToDate={(v) => setRange((r) => ({ ...r, toDate: v }))}
          />
          <div className="mb-4 flex gap-1 rounded-md border border-border-soft p-1">
            {(['summary', 'detail'] as const).map((m) => (
              <button
                key={m}
                type="button"
                onClick={() => setMode(m)}
                className={`rounded px-3 py-1 text-sm font-medium capitalize ${mode === m ? 'bg-primary-600 text-white' : 'text-ink-muted hover:bg-panel-2'}`}
              >
                {m}
              </button>
            ))}
          </div>
        </div>

        <div className="mb-4 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          <Select
            label="Ward" placeholder="All" value={wardId} onChange={(e) => setWardId(e.target.value)}
            options={wards.map((w) => ({ value: w.id, label: w.name }))}
          />
          <Select
            label="Doctor" placeholder="All" value={doctorId} onChange={(e) => setDoctorId(e.target.value)}
            options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
          />
          <Select
            label="Unit" placeholder="All" value={unitId} onChange={(e) => setUnitId(e.target.value)}
            options={units.map((u) => ({ value: u.id, label: u.name }))}
          />
          <Select
            label="Flag" placeholder="All" value={wardFlag} onChange={(e) => setWardFlag(e.target.value)}
            options={WARD_FLAG_OPTIONS}
          />
          <Select
            label="Bed Status" placeholder="All" value={bedStatus} onChange={(e) => setBedStatus(e.target.value)}
            options={Object.entries(BED_STATUS_LABELS).map(([value, label]) => ({ value, label }))}
          />
        </div>
        {bedStatus && (
          <p className="mb-4 -mt-2 text-xs text-ink-faint">
            Bed Status reflects each bed's current status, not its status during the selected date range.
          </p>
        )}

        <div className="mb-4 flex flex-wrap items-center gap-2">
          <Button onClick={run} loading={isFetching} disabled={!rangeComplete}>Run</Button>
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

        {!rangeComplete && <p className="mb-3 text-sm text-ink-faint">Select both a from-date and a to-date — bed-days available depends on a bounded range.</p>}
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
              </table>
            </div>

            <div className="mt-3">{pager}</div>
          </>
        )}
        {!hasRun && rangeComplete && <p className="py-10 text-center text-sm text-ink-faint">Pick a mode and filters, then Run.</p>}
      </div>
    </>
  )
}
