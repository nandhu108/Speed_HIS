import { useEffect, useRef, useState, type UIEvent } from 'react'
import { errorMessage } from '../../../api/axios'
import { endpoints } from '../../../api/endpoints'
import { Button } from '../../../components/ui/Button'
import { DateRangeFilter, defaultDateRange } from '../../../components/ui/DateRangeFilter'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { useDoctors, useReferrers, useRoomTypes, useSimpleMaster, useWards } from '../../masters/api'
import { downloadIpAdmissionsReport, useIpAdmissionsReport, type IpReportFilter } from '../api'
import { ADMISSION_STATUS_LABELS, type AdmissionStatus, type IpReportMode } from '../types'

/** IP Report — date range + free filters (doctor/ward/department/patient type/referred
 * source/bed type/status/unit/reg no/patient name/IP no), Summary/Detail toggle, a show/hide
 * column picker over whatever columns the current run returned, rows-per-page with an inner
 * scroll + sticky header once the table gets tall, and CSV/Excel/PDF export. */
export function IpReportPage() {
  const [mode, setMode] = useState<IpReportMode>('summary')
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const [doctorId, setDoctorId] = useState('')
  const [wardId, setWardId] = useState('')
  const [departmentId, setDepartmentId] = useState('')
  const [patientCategoryId, setPatientCategoryId] = useState('')
  const [referrerId, setReferrerId] = useState('')
  const [roomTypeId, setRoomTypeId] = useState('')
  const [status, setStatus] = useState('')
  const [unitId, setUnitId] = useState('')
  const [regNo, setRegNo] = useState('')
  const [patientName, setPatientName] = useState('')
  const [ipNo, setIpNo] = useState('')
  const [hasRun, setHasRun] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [hiddenColumns, setHiddenColumns] = useState<Set<string>>(new Set())
  const [columnPickerOpen, setColumnPickerOpen] = useState(false)
  const columnPickerRef = useRef<HTMLDivElement>(null)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(25)
  const pageSizeOptions = [10, 25, 50, 100, 200]
  // Bounds the table body height regardless of "rows per page" — past this it scrolls internally
  // (sticky header) instead of growing the page indefinitely. A no-op when fewer rows fit within it.
  const tableMaxHeightPx = 520

  const topScrollRef = useRef<HTMLDivElement>(null)
  const bottomScrollRef = useRef<HTMLDivElement>(null)
  const tableResizeObserverRef = useRef<ResizeObserver | null>(null)
  const [contentWidth, setContentWidth] = useState(0)

  // Callback ref (not a plain ref + effect) so the ResizeObserver attaches the instant the table
  // mounts and detaches on unmount, rather than depending on a useEffect that runs before a
  // conditionally-rendered table exists. Tracks column-toggling, pagination, and window resize
  // alike — all just show up as the table's scrollWidth changing.
  const tableRef = (el: HTMLTableElement | null) => {
    tableResizeObserverRef.current?.disconnect()
    tableResizeObserverRef.current = null
    if (!el) return
    const observer = new ResizeObserver(() => setContentWidth(el.scrollWidth))
    observer.observe(el)
    tableResizeObserverRef.current = observer
    setContentWidth(el.scrollWidth)
  }

  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const { data: wards = [] } = useWards()
  const { data: departments = [] } = useSimpleMaster('departments', endpoints.masters.departments)
  const { data: patientCategories = [] } = useSimpleMaster('patientcategories', endpoints.masters.patientCategories)
  const { data: referrersResult } = useReferrers({ page: 1, pageSize: 200 })
  const { data: roomTypes = [] } = useRoomTypes()
  const { data: units = [] } = useSimpleMaster('units', endpoints.masters.units)

  const filter: IpReportFilter = {
    mode, fromDate, toDate,
    doctorId: doctorId ? Number(doctorId) : null,
    wardId: wardId ? Number(wardId) : null,
    departmentId: departmentId ? Number(departmentId) : null,
    patientCategoryId: patientCategoryId ? Number(patientCategoryId) : null,
    referrerId: referrerId ? Number(referrerId) : null,
    roomTypeId: roomTypeId ? Number(roomTypeId) : null,
    status: status ? (Number(status) as AdmissionStatus) : null,
    unitId: unitId ? Number(unitId) : null,
    regNo: regNo.trim() || null,
    patientName: patientName.trim() || null,
    ipNo: ipNo.trim() || null,
  }
  const { data: result, isFetching, refetch } = useIpAdmissionsReport(filter, hasRun)

  // A fresh run may return a different column set (Summary vs Detail) — start every run with
  // every column visible rather than carrying over a hidden-column selection that may no longer
  // apply. Also back to page 1, since a stale page number from a longer previous run could land
  // past the end of a shorter new one.
  useEffect(() => {
    setHiddenColumns(new Set())
    setPage(1)
  }, [result?.columns])

  const syncScrollFrom = (source: 'top' | 'bottom') => (e: UIEvent<HTMLDivElement>) => {
    const target = source === 'top' ? bottomScrollRef.current : topScrollRef.current
    if (target) target.scrollLeft = e.currentTarget.scrollLeft
  }

  // The column picker has nothing else to close it — without this it just sits open, overlapping
  // the pager/table beneath it, until the Columns button is clicked again.
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

  const run = async () => {
    setError(null)
    setHasRun(true)
    await refetch()
  }

  const resetFilters = () => {
    setDoctorId(''); setWardId(''); setDepartmentId(''); setPatientCategoryId('')
    setReferrerId(''); setRoomTypeId(''); setStatus(''); setUnitId('')
    setRegNo(''); setPatientName(''); setIpNo('')
  }

  const download = async (format: 'csv' | 'excel' | 'pdf') => {
    setError(null)
    try {
      await downloadIpAdmissionsReport(filter, format)
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
      <PageHeader title="IP Report" subtitle="Admissions — filter, choose Summary or Detail, pick columns, and export" />

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
          <Input label="UHID" value={regNo} onChange={(e) => setRegNo(e.target.value)} />
          <Input label="IP No" value={ipNo} onChange={(e) => setIpNo(e.target.value)} />
          <Input label="Patient Name" value={patientName} onChange={(e) => setPatientName(e.target.value)} />
          <Select
            label="Doctor" placeholder="All" value={doctorId} onChange={(e) => setDoctorId(e.target.value)}
            options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
          />
          <Select
            label="Status" placeholder="All" value={status} onChange={(e) => setStatus(e.target.value)}
            options={Object.entries(ADMISSION_STATUS_LABELS).map(([value, label]) => ({ value, label }))}
          />
          <Select
            label="Ward" placeholder="All" value={wardId} onChange={(e) => setWardId(e.target.value)}
            options={wards.map((w) => ({ value: w.id, label: w.name }))}
          />
          <Select
            label="Bed Type" placeholder="All" value={roomTypeId} onChange={(e) => setRoomTypeId(e.target.value)}
            options={roomTypes.map((r) => ({ value: r.id, label: r.name }))}
          />
          <Select
            label="Department" placeholder="All" value={departmentId} onChange={(e) => setDepartmentId(e.target.value)}
            options={departments.map((d) => ({ value: d.id, label: d.name }))}
          />
          <Select
            label="Department Unit" placeholder="All" value={unitId} onChange={(e) => setUnitId(e.target.value)}
            options={units.map((u) => ({ value: u.id, label: u.name }))}
          />
          <Select
            label="Patient Type" placeholder="All" value={patientCategoryId} onChange={(e) => setPatientCategoryId(e.target.value)}
            options={patientCategories.map((c) => ({ value: c.id, label: c.name }))}
          />
          <Select
            label="Referred Source" placeholder="All" value={referrerId} onChange={(e) => setReferrerId(e.target.value)}
            options={(referrersResult?.items ?? []).map((r) => ({ value: r.id, label: r.name }))}
          />
        </div>

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

            {/* Duplicate scrollbar pinned above the table, synced to the real one below — on a
                wide table the bottom scrollbar is out of sight until you scroll all the way down. */}
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
        {!hasRun && <p className="py-10 text-center text-sm text-ink-faint">Pick a date range and filters, then Run.</p>}
      </div>
    </>
  )
}
