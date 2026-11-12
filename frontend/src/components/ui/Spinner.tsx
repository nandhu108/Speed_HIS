export function Spinner({ label = 'Loading…' }: { label?: string }) {
  return (
    <div className="flex items-center justify-center gap-3 py-10 text-ink-muted" role="status">
      <span className="size-6 animate-spin rounded-full border-2 border-border-strong border-t-accent-2" aria-hidden />
      <span className="text-sm">{label}</span>
    </div>
  )
}
