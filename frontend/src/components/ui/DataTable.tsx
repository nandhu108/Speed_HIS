import { Search, X } from 'lucide-react'
import { useEffect, useRef, useState, type ReactNode } from 'react'
import type { PagedResult } from '../../api/types/api'
import { Input } from './Input'
import { Spinner } from './Spinner'

export interface Column<T> {
  header: string
  cell: (row: T) => ReactNode
  className?: string
}

interface DataTableProps<T> {
  columns: Column<T>[]
  result: PagedResult<T> | undefined
  isLoading: boolean
  page: number
  onPageChange: (page: number) => void
  onSearch?: (search: string) => void
  searchPlaceholder?: string
  rowKey: (row: T) => string | number
  emptyMessage?: string
  toolbar?: ReactNode
  /** Adds a show/hide Columns picker, same as ReportResultTable's — uses each column's own `header` as its stable key (already unique/used as the React key below). */
  enableColumnPicker?: boolean
  /** Adds a "Rows per page" selector next to Previous/Next, same as ReportResultTable's — this is server-paginated, so changing it re-fetches at the new page size rather than re-slicing already-loaded rows. Omit to keep the plain Previous/Next-only pager. */
  onPageSizeChange?: (pageSize: number) => void
  pageSizeOptions?: number[]
  /** Repeats the pager in the top toolbar bar too, same as ReportResultTable's — useful on tall report tables where the bottom pager is out of sight without scrolling. Opt-in (default false) so the ~30 other DataTable screens keep their existing single-pager-at-bottom layout. */
  showTopPager?: boolean
}

/** Server-paginated table used by every admin list page and every Report Search Panel. */
export function DataTable<T>({
  columns,
  result,
  isLoading,
  page,
  onPageChange,
  onSearch,
  searchPlaceholder = 'Search…',
  rowKey,
  emptyMessage = 'No records found.',
  toolbar,
  enableColumnPicker = false,
  onPageSizeChange,
  pageSizeOptions = [10, 25, 50, 100, 200],
  showTopPager = false,
}: DataTableProps<T>) {
  const [searchDraft, setSearchDraft] = useState('')
  const [hiddenColumns, setHiddenColumns] = useState<Set<string>>(new Set())
  const [columnPickerOpen, setColumnPickerOpen] = useState(false)
  const columnPickerRef = useRef<HTMLDivElement>(null)
  const searchRef = useRef<HTMLInputElement>(null)

  /* Search used to fire only on Enter, with no button and no hint — typing a name and waiting
     looked like a broken screen. It now searches as you type, debounced so a 6-character
     surname is one request rather than six. Enter still works and just skips the wait. */
  const lastSubmitted = useRef('')
  useEffect(() => {
    if (!onSearch) return
    const next = searchDraft.trim()
    if (next === lastSubmitted.current) return
    const timer = setTimeout(() => {
      lastSubmitted.current = next
      onPageChange(1)
      onSearch(next)
    }, 350)
    return () => clearTimeout(timer)
    // `onSearch`/`onPageChange` are typically inline arrows and would re-fire the timer every
    // render if listed; the draft value is the only real trigger.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchDraft])

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

  const toggleColumn = (header: string) =>
    setHiddenColumns((prev) => {
      const next = new Set(prev)
      if (next.has(header)) next.delete(header)
      else next.add(header)
      return next
    })

  const visibleColumns = columns.filter((c) => !hiddenColumns.has(c.header))

  // Always render the count — "how many did that search find?" was previously invisible on any
  // single-page result, which is most master/list screens.
  const pager = result && result.totalCount > 0 && (
    <div className="flex flex-wrap items-center gap-4 text-sm text-ink-muted">
      <span className="tabular">
        {result.totalPages > 1
          ? `Showing ${(result.page - 1) * result.pageSize + 1}–${Math.min(result.page * result.pageSize, result.totalCount)} of ${result.totalCount}`
          : `${result.totalCount} record${result.totalCount === 1 ? '' : 's'}`}
      </span>
      <div className="flex items-center gap-3">
        {onPageSizeChange && (
          <label className="flex items-center gap-1.5">
            Rows per page
            <select
              value={result.pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              className="rounded-md border border-border-soft bg-panel-solid px-2 py-1 text-ink"
            >
              {pageSizeOptions.map((size) => <option key={size} value={size}>{size}</option>)}
            </select>
          </label>
        )}
        {result.totalPages > 1 && (
          <div className="flex items-center gap-1">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => onPageChange(page - 1)}
              className="rounded-shell-sm border border-border-soft px-3 py-1 hover:border-border-strong disabled:cursor-not-allowed disabled:opacity-40"
            >
              Previous
            </button>
            <span className="tabular px-1 text-xs text-ink-faint">
              {result.page} / {result.totalPages}
            </span>
            <button
              type="button"
              disabled={page >= result.totalPages}
              onClick={() => onPageChange(page + 1)}
              className="rounded-shell-sm border border-border-soft px-3 py-1 hover:border-border-strong disabled:cursor-not-allowed disabled:opacity-40"
            >
              Next
            </button>
          </div>
        )}
      </div>
    </div>
  )

  return (
    <div className="rounded-shell border border-border-soft bg-panel shadow-shell-sm">
      {(onSearch || toolbar || enableColumnPicker || (showTopPager && pager)) && (
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border-soft px-4 py-3">
          {onSearch ? (
            <form
              className="relative w-72"
              role="search"
              onSubmit={(event) => {
                event.preventDefault()
                lastSubmitted.current = searchDraft.trim()
                onPageChange(1)
                onSearch(searchDraft.trim())
              }}
            >
              <Search
                className="pointer-events-none absolute top-1/2 left-3 z-10 size-4 -translate-y-1/2 text-ink-faint"
                aria-hidden
              />
              <Input
                ref={searchRef}
                type="search"
                aria-label={searchPlaceholder}
                placeholder={searchPlaceholder}
                value={searchDraft}
                onChange={(event) => setSearchDraft(event.target.value)}
                onKeyDown={(event) => event.key === 'Escape' && searchDraft && setSearchDraft('')}
                inputClassName="pl-9 pr-9 [&::-webkit-search-cancel-button]:hidden"
              />
              {searchDraft && (
                <button
                  type="button"
                  aria-label="Clear search"
                  onClick={() => {
                    setSearchDraft('')
                    searchRef.current?.focus()
                  }}
                  className="absolute top-1/2 right-2 -translate-y-1/2 rounded p-1 text-ink-faint hover:bg-panel-2 hover:text-ink"
                >
                  <X className="size-3.5" aria-hidden />
                </button>
              )}
            </form>
          ) : showTopPager && pager ? (
            pager
          ) : (
            <span />
          )}
          <div className="flex items-center gap-3">
            {toolbar}
            {enableColumnPicker && (
              <div ref={columnPickerRef} className="relative">
                <button
                  type="button"
                  onClick={() => setColumnPickerOpen((o) => !o)}
                  className="rounded-md border border-border-soft bg-panel-solid px-3 py-1.5 text-sm font-medium text-ink hover:border-border-strong"
                >
                  Columns ({visibleColumns.length}/{columns.length})
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
                        onClick={() => setHiddenColumns(new Set(columns.map((c) => c.header)))}
                      >
                        Hide all
                      </button>
                    </div>
                    {columns.map((c) => (
                      <label key={c.header} className="flex items-center gap-2 rounded px-1 py-1 text-sm text-ink hover:bg-panel-2">
                        <input
                          type="checkbox"
                          checked={!hiddenColumns.has(c.header)}
                          onChange={() => toggleColumn(c.header)}
                          className="accent-accent-2"
                        />
                        {c.header || '(unlabeled)'}
                      </label>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      )}

      {/* Viewport-relative instead of a hard 520px: on a 1080p+ ward/reception monitor that
          fixed cap showed ~10 rows and left half the screen empty, while still being tall
          enough to force a nested scrollbar on a laptop. */}
      <div className="max-h-[clamp(20rem,62vh,54rem)] overflow-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-panel-2 text-left">
              {visibleColumns.map((column, i) => (
                <th
                  key={column.header}
                  className={`sticky top-0 z-10 border-b border-border-soft bg-panel-2 px-4 py-3 text-[11px] font-bold tracking-wide whitespace-nowrap text-ink-muted uppercase ${
                    i === 0 ? 'left-0 z-20' : ''
                  } ${column.className ?? ''}`}
                >
                  {column.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {isLoading && !result && (
              <tr>
                <td colSpan={visibleColumns.length}>
                  <Spinner />
                </td>
              </tr>
            )}
            {!isLoading && result?.items.length === 0 && (
              <tr>
                <td colSpan={visibleColumns.length} className="px-4 py-10 text-center text-ink-faint">
                  {emptyMessage}
                </td>
              </tr>
            )}
            {result?.items.map((row) => (
              /* Rows stay on screen while the next page loads (dimmed) rather than being
                 replaced by a spinner — paging no longer collapses the table to a blank box
                 and jumps the scroll position. */
              <tr
                key={rowKey(row)}
                className={`border-t border-border-soft transition-colors hover:bg-panel-2/60 ${
                  isLoading ? 'opacity-50' : ''
                }`}
              >
                {visibleColumns.map((column, i) => (
                  <td
                    key={column.header}
                    className={`px-4 py-2.5 whitespace-nowrap text-ink ${i === 0 ? 'sticky left-0 z-[5] bg-panel' : ''} ${column.className ?? ''}`}
                  >
                    {column.cell(row)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {pager && <div className="flex flex-wrap items-center justify-between gap-3 border-t border-border-soft px-4 py-2.5">{pager}</div>}
    </div>
  )
}
