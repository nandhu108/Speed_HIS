export function DateRangeFilter({
  fromDate, toDate, onFromDate, onToDate,
}: {
  fromDate: string; toDate: string; onFromDate: (v: string) => void; onToDate: (v: string) => void
}) {
  return (
    <div className="mb-4 flex items-end gap-2">
      <label className="block">
        <span className="mb-1.5 block text-[10.5px] font-bold tracking-wider text-ink-faint uppercase">From</span>
        <input
          type="date"
          value={fromDate}
          onChange={(e) => onFromDate(e.target.value)}
          className="rounded-shell-sm border border-border-soft bg-panel-2 px-3 py-1.5 text-sm text-ink outline-none focus:border-accent-2 focus:ring-2 focus:ring-accent-2/15"
        />
      </label>
      <label className="block">
        <span className="mb-1.5 block text-[10.5px] font-bold tracking-wider text-ink-faint uppercase">To</span>
        <input
          type="date"
          value={toDate}
          onChange={(e) => onToDate(e.target.value)}
          className="rounded-shell-sm border border-border-soft bg-panel-2 px-3 py-1.5 text-sm text-ink outline-none focus:border-accent-2 focus:ring-2 focus:ring-accent-2/15"
        />
      </label>
    </div>
  )
}

export function defaultDateRange(daysBack: number) {
  const to = new Date()
  const from = new Date()
  from.setDate(from.getDate() - daysBack)
  return { fromDate: from.toISOString().slice(0, 10), toDate: to.toISOString().slice(0, 10) }
}
