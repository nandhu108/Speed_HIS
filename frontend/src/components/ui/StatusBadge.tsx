type Tone = 'success' | 'warning' | 'danger' | 'info' | 'neutral'

const tones: Record<Tone, string> = {
  success: 'bg-ok/10 text-ok',
  warning: 'bg-warn/10 text-warn',
  danger: 'bg-crit/10 text-crit',
  info: 'bg-info/10 text-info',
  neutral: 'bg-panel-2 text-ink-muted',
}

export function StatusBadge({ tone = 'neutral', children }: { tone?: Tone; children: React.ReactNode }) {
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-semibold ${tones[tone]}`}>
      <span className="size-1.5 rounded-full bg-current" aria-hidden />
      {children}
    </span>
  )
}
