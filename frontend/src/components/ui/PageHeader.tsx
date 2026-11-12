import { ArrowLeft } from 'lucide-react'
import type { ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'

interface PageHeaderProps {
  title: string
  subtitle?: string
  actions?: ReactNode
  /** Renders a Back button before the title, navigating to this route. Every open screen lives
   * in its own workspace tab (see TabRouterPane) with no real browser history to go back
   * through — `navigator.go()` there is a deliberate no-op — so "back" always means an explicit
   * target route (typically the detail page's list screen) rather than history.back(). Omit on
   * pages that are themselves a list/landing screen, or that already offer their own tab-aware
   * back action (e.g. BillWorkspace's `onBack`, used when embedded in OpBillingSearchPage). */
  backTo?: string
}

export function PageHeader({ title, subtitle, actions, backTo }: PageHeaderProps) {
  const navigate = useNavigate()
  return (
    <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
      <div className="flex items-center gap-3">
        {backTo && (
          <button
            type="button"
            onClick={() => navigate(backTo)}
            title="Back"
            className="flex size-8 shrink-0 items-center justify-center rounded-shell-sm border border-border-soft bg-panel-solid text-ink-muted hover:border-border-strong hover:text-ink"
          >
            <ArrowLeft className="size-4" aria-hidden />
          </button>
        )}
        <div>
          <h1 className="font-display text-xl font-bold tracking-tight text-ink">{title}</h1>
          {subtitle && <p className="mt-0.5 text-sm text-ink-muted">{subtitle}</p>}
        </div>
      </div>
      {actions && <div className="flex items-center gap-2">{actions}</div>}
    </div>
  )
}
