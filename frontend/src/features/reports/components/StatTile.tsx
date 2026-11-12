import type { LucideIcon } from 'lucide-react'

const toneColor: Record<string, string> = {
  danger: 'text-crit',
  warning: 'text-warn',
  success: 'text-ok',
}

export function StatTile({
  label,
  value,
  tone,
  icon: Icon,
}: {
  label: string
  value: string | number
  tone?: 'danger' | 'warning' | 'success'
  icon?: LucideIcon
}) {
  const valueColor = tone ? toneColor[tone] : 'text-ink'
  return (
    <div className="rounded-shell border border-border-soft bg-panel p-4 shadow-shell-sm">
      <div className="flex items-start justify-between gap-2">
        <p className="text-[11.5px] font-semibold text-ink-muted">{label}</p>
        {Icon && <Icon className={`size-4 ${valueColor}`} aria-hidden />}
      </div>
      <p className={`font-mono mt-1.5 text-[26px] font-semibold tracking-tight ${valueColor}`}>{value}</p>
    </div>
  )
}

export function AmountBarList({ rows }: { rows: { label: string; amount: number }[] }) {
  const max = Math.max(1, ...rows.map((r) => r.amount))
  return (
    <div className="space-y-3">
      {rows.map((r) => (
        <div key={r.label}>
          <div className="mb-1 flex justify-between text-[11.5px]">
            <span className="text-ink-muted">{r.label}</span>
            <span className="font-mono font-medium text-ink">{r.amount.toLocaleString()}</span>
          </div>
          <div className="h-1.5 w-full overflow-hidden rounded-full bg-panel-2">
            <div
              className="h-full rounded-full bg-accent-2"
              style={{ width: `${(r.amount / max) * 100}%` }}
            />
          </div>
        </div>
      ))}
      {rows.length === 0 && <p className="py-4 text-center text-sm text-ink-faint">No data.</p>}
    </div>
  )
}
