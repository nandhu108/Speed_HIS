import { Columns2, X } from 'lucide-react'
import { MenuIconFor } from '../ui/menuIcons'
import { resolveTabMeta, useMyMenu } from './AppShell'
import { useTabWorkspace, type WorkspaceTab } from '../../lib/tabWorkspace/TabWorkspaceContext'

/** The browser-tab-like strip under the header — one entry per open workspace tab, plus the Split toggle. */
export function TabStrip() {
  const { data: menu = [] } = useMyMenu()
  const { tabs, activeTabId, splitTabId, activateTab, closeTab, toggleSplit } = useTabWorkspace()

  return (
    <div className="flex h-11 shrink-0 items-center gap-1.5 border-b border-border-soft bg-panel-2 px-3.5">
      <div className="flex flex-1 items-center gap-1.5 overflow-x-auto [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
        {tabs.map((tab) => (
          <TabChip
            key={tab.id}
            tab={tab}
            meta={resolveTabMeta(menu, tab.path)}
            isActive={tab.id === activeTabId || tab.id === splitTabId}
            isPrimary={tab.id === activeTabId}
            canClose={tabs.length > 1}
            onActivate={() => activateTab(tab.id)}
            onClose={(e) => {
              e.stopPropagation()
              closeTab(tab.id)
            }}
          />
        ))}
      </div>
      <button
        type="button"
        onClick={toggleSplit}
        title="Split view"
        className={`flex shrink-0 items-center gap-1.5 rounded-shell-sm border px-2.5 py-1.5 text-[11.5px] font-semibold transition-colors ${
          splitTabId
            ? 'border-transparent bg-accent-2 text-white'
            : 'border-border-soft bg-panel-2 text-ink-muted hover:text-ink'
        }`}
      >
        <Columns2 className="size-3.5" aria-hidden />
        Split
      </button>
    </div>
  )
}

function TabChip({
  tab, meta, isActive, isPrimary, canClose, onActivate, onClose,
}: {
  tab: WorkspaceTab
  meta: { label: string; icon: string | null }
  isActive: boolean
  isPrimary: boolean
  canClose: boolean
  onActivate: () => void
  onClose: (e: React.MouseEvent) => void
}) {
  // A wrapper div rather than nesting the close control inside the activate <button>: a button
  // inside a button is invalid HTML, and the close target was `tabIndex={-1}` + `opacity-0`,
  // so closing a tab was mouse-hover-only — unreachable by keyboard entirely.
  return (
    <div
      className={`group flex shrink-0 items-center rounded-shell-sm border pr-1 transition-colors ${
        isActive
          ? 'border-border-strong bg-panel-solid text-ink shadow-shell-sm'
          : 'border-transparent text-ink-muted hover:bg-panel hover:text-ink'
      } ${isPrimary && isActive ? 'ring-1 ring-accent-2/40' : ''}`}
    >
      <button
        type="button"
        onClick={onActivate}
        // Middle-click closes, matching browser tabs.
        onAuxClick={(e) => {
          if (e.button === 1 && canClose) onClose(e)
        }}
        // Two tabs can share a label (e.g. two patient detail pages), so the tooltip stays the
        // path — that's what tells them apart.
        title={tab.path}
        aria-current={isActive ? 'page' : undefined}
        className="flex items-center gap-2 py-1.5 pl-3 text-[12px] font-semibold"
      >
        <MenuIconFor name={meta.icon} />
        <span className="whitespace-nowrap">{meta.label}</span>
      </button>
      {canClose && (
        <button
          type="button"
          onClick={onClose}
          aria-label={`Close ${meta.label}`}
          title={`Close ${meta.label}`}
          className={`ml-1 flex size-4.5 shrink-0 items-center justify-center rounded text-ink-faint transition-opacity hover:bg-panel-2 hover:text-ink focus-visible:opacity-100 ${
            isActive ? 'opacity-70' : 'opacity-0 group-hover:opacity-100'
          }`}
        >
          <X className="size-3" aria-hidden />
        </button>
      )}
    </div>
  )
}
