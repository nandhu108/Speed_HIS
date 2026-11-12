import { useEffect, useMemo, useRef, useState } from 'react'
import { Spinner } from './Spinner'

// Column keys ending in "Number"/"No" (billNumber, regNo, tokenNo, ipNumber, ...) are
// identifier codes, not prose — render them monospace like BillReportTab's DataTable does for
// its own Bill No column, instead of the same proportional font as every other column.
const ID_COLUMN_KEY = /(Number|No)$/

// A column where every non-empty value across the whole result parses as a number (amount/
// qty/count columns) reads better right-aligned with tabular figures, same as BillReportTab's
// Total/Paid/Balance columns — computed from the data rather than a hardcoded key list since
// these generic reports have report-specific column sets.
function isNumericColumn(key: string, rows: Record<string, unknown>[]): boolean {
  let sawValue = false
  for (const row of rows) {
    const v = row[key]
    if (v === null || v === undefined || v === '') continue
    sawValue = true
    if (typeof v === 'number') continue
    if (typeof v !== 'string' || v.trim() === '' || Number.isNaN(Number(v))) return false
  }
  return sawValue
}

export interface ReportResultColumn {
  key: string
  label: string
}

/** Mirrors backend Prixo.Application.Features.Reports.ReportResultDto — the generic (columns,
 * rows) shape every plain-list report (OP Report, Billing Reports, ...) exports through and can
 * now also render on-screen through, via this one component instead of a bespoke table each. */
export interface ReportResult {
  reportName: string
  columns: ReportResultColumn[]
  rows: Record<string, unknown>[]
}

interface ReportResultTableProps {
  result: ReportResult | undefined
  isLoading: boolean
  /** Surfaces a failed fetch as an error message instead of leaving the spinner running forever — without this, a query that settles into an error state (network down, 500, permission denied) never satisfies the old `isLoading || !result` check, since `result` stays undefined on error too. */
  errorMessage?: string
  emptyMessage?: string
  /** Bounds the table body height regardless of "rows per page" — past this it scrolls internally (sticky header) instead of growing the page indefinitely. */
  maxHeightPx?: number
  /** Caller-supplied predicate — the caller's own filter card (Patient Name/Bill No, a fallback "Search" field for reports with neither, ...) decides what counts as a match; this component has no search box of its own, matching BillReportTab's DataTable, which doesn't either. Client-side; the whole result is already fetched in one shot. */
  extraFilter?: (row: Record<string, unknown>) => boolean
}

/**
 * The table half of a report screen — sticky header + sticky first column (frozen corner
 * where both meet), a show/hide column picker, and client-side pagination. Deliberately
 * unwrapped (no card/border around the toolbar, no search box) — the filter card above it
 * (date range, report-specific fields incl. search, Export) is the caller's own, matching
 * BillReportTab/ConcessionTab's shape: one filter card, then a separate table area below it,
 * not everything nested in one box. Extracted from OpReportPage's original inline
 * implementation (same sticky-header/column-picker/synced-scrollbar approach), generalized
 * to any ReportResult so any report screen can reuse it instead of re-implementing it.
 */
export function ReportResultTable({ result, isLoading, errorMessage, emptyMessage = 'No rows for this range.', maxHeightPx = 520, extraFilter }: ReportResultTableProps) {
  const [hiddenColumns, setHiddenColumns] = useState<Set<string>>(new Set())
  const [columnPickerOpen, setColumnPickerOpen] = useState(false)
  const columnPickerRef = useRef<HTMLDivElement>(null)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(25)
  const pageSizeOptions = [10, 25, 50, 100, 200]

  // A fresh result may carry a different column set — start every new result with every
  // column visible and no stale page from a previous run.
  useEffect(() => {
    setHiddenColumns(new Set())
    setPage(1)
  }, [result?.columns])

  useEffect(() => {
    setPage(1)
  }, [extraFilter])

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

  const toggleColumn = (key: string) =>
    setHiddenColumns((prev) => {
      const next = new Set(prev)
      if (next.has(key)) next.delete(key)
      else next.add(key)
      return next
    })

  const numericColumns = useMemo(
    () => new Set((result?.columns ?? []).filter((c) => isNumericColumn(c.key, result?.rows ?? [])).map((c) => c.key)),
    [result],
  )

  if (isLoading) return <Spinner />
  if (errorMessage) return <p className="rounded-shell-sm bg-crit/10 px-3 py-2 text-sm text-crit">{errorMessage}</p>
  if (!result) return <Spinner />

  const visibleColumns = result.columns.filter((c) => !hiddenColumns.has(c.key))

  const searchedRows = extraFilter ? result.rows.filter(extraFilter) : result.rows

  const totalRows = searchedRows.length
  const totalPages = Math.max(1, Math.ceil(totalRows / pageSize))
  const pagedRows = searchedRows.slice((page - 1) * pageSize, page * pageSize)

  const pager = (
    <div className="flex flex-wrap items-center justify-between gap-4 text-sm text-ink-muted">
      <span>
        Page {page} of {totalPages} · {totalRows} row{totalRows === 1 ? '' : 's'}
        {result.rows.length !== totalRows ? ` (of ${result.rows.length})` : ''}
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
            className="rounded-shell-sm border border-border-soft px-3 py-1 hover:border-border-strong disabled:opacity-40"
          >
            Previous
          </button>
          <button
            type="button"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
            className="rounded-shell-sm border border-border-soft px-3 py-1 hover:border-border-strong disabled:opacity-40"
          >
            Next
          </button>
        </div>
      </div>
    </div>
  )

  const columnPicker = (
    <div ref={columnPickerRef} className="relative">
      <button
        type="button"
        onClick={() => setColumnPickerOpen((o) => !o)}
        className="rounded-md border border-border-soft bg-panel-solid px-3 py-1.5 text-sm font-medium text-ink hover:border-border-strong"
      >
        Columns ({visibleColumns.length}/{result.columns.length})
      </button>
      {columnPickerOpen && (
        <div className="absolute right-0 z-30 mt-1 max-h-80 w-64 overflow-y-auto rounded-md border border-border-soft bg-panel-solid p-2 shadow-shell-md">
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
  )

  return (
    <div className="rounded-shell border border-border-soft bg-panel shadow-shell-sm">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border-soft px-4 py-3">
        {pager}
        {columnPicker}
      </div>

      <div className="overflow-auto" style={{ maxHeight: maxHeightPx }}>
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-panel-2 text-left">
              {visibleColumns.map((c, i) => (
                <th
                  key={c.key}
                  className={`sticky top-0 z-10 whitespace-nowrap bg-panel-2 px-4 py-3 text-[10.5px] font-bold tracking-wider text-ink-faint uppercase ${
                    i === 0 ? 'left-0 z-20' : ''
                  } ${numericColumns.has(c.key) ? 'text-right' : ''}`}
                >
                  {c.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {pagedRows.map((row, r) => (
              <tr key={r} className="border-t border-border-soft hover:bg-panel-2/60">
                {visibleColumns.map((c, i) => (
                  <td
                    key={c.key}
                    className={`px-4 py-2.5 whitespace-nowrap text-ink ${i === 0 ? 'sticky left-0 z-[5] bg-panel' : ''} ${
                      numericColumns.has(c.key) ? 'text-right tabular-nums' : ''
                    }`}
                  >
                    {ID_COLUMN_KEY.test(c.key) ? (
                      <span className="font-mono text-xs">{String(row[c.key] ?? '')}</span>
                    ) : (
                      String(row[c.key] ?? '')
                    )}
                  </td>
                ))}
              </tr>
            ))}
            {totalRows === 0 && (
              <tr>
                <td colSpan={visibleColumns.length} className="px-4 py-10 text-center text-ink-faint">
                  {emptyMessage}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3 border-t border-border-soft px-4 py-2.5 text-sm text-ink-muted">
        {pager}
      </div>
    </div>
  )
}
