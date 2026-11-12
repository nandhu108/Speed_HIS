import { GripVertical, X } from 'lucide-react'
import { useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { findActiveNode, resolveTabMeta, useMyMenu, type MenuNode } from './AppShell'
import { TabRouterPane } from '../../lib/tabWorkspace/TabRouterPane'
import { useTabWorkspace } from '../../lib/tabWorkspace/TabWorkspaceContext'

function flattenMenu(nodes: MenuNode[]): { path: string; title: string }[] {
  return nodes.flatMap((node) => [
    ...(node.path ? [{ path: node.path, title: node.title }] : []),
    ...flattenMenu(node.children),
  ])
}

function PaneHeader({
  label, menu, currentPath, onPick, onClose,
}: {
  label: string
  menu: MenuNode[]
  currentPath: string | undefined
  onPick?: (path: string) => void
  onClose?: () => void
}) {
  // The <select> can only ever highlight an *exact* menu entry (its options are leaf paths);
  // the displayed text uses the same tab-label fallback chain as the tab strip so a detail page
  // (no menu entry of its own) still shows something better than blank.
  const exactPath = currentPath ? findActiveNode(menu, currentPath.split('?')[0]!)?.node.path : undefined
  const meta = currentPath ? resolveTabMeta(menu, currentPath) : null

  return (
    <div className="flex h-9 shrink-0 items-center gap-2 border-b border-border-soft bg-panel px-3">
      <span className="text-[10.5px] font-bold tracking-wider text-ink-faint uppercase">{label}</span>
      {onPick ? (
        <select
          value={exactPath ?? ''}
          onChange={(e) => onPick(e.target.value)}
          className="ml-auto rounded-shell-sm border border-border-soft bg-panel-solid px-2 py-1 text-[11.5px] font-semibold text-ink outline-none"
        >
          {flattenMenu(menu).map((o) => (
            <option key={o.path} value={o.path}>{o.title}</option>
          ))}
        </select>
      ) : (
        <span className="ml-auto truncate text-[11.5px] font-semibold text-ink-muted">{meta?.label}</span>
      )}
      {onClose && (
        <button type="button" onClick={onClose} title="Close split" className="text-ink-faint hover:text-ink">
          <X className="size-3.5" aria-hidden />
        </button>
      )}
    </div>
  )
}

/**
 * Mounts every open tab's isolated router exactly once (see TabRouterPane) and *portals* each
 * one into whichever slot currently applies to it — the single active pane, Pane A/Pane B in
 * split mode, or an off-screen background container for every other open tab. Portaling (rather
 * than conditionally rendering per slot) is what keeps a tab's component instance — and its
 * unsaved state — alive as it moves between "active," "backgrounded" and "split," since React
 * portals preserve the component identity of their children regardless of which DOM node they
 * currently render into; only the physical slot ref changes.
 */
export function TabbedWorkspace() {
  const { data: menu = [] } = useMyMenu()
  const { tabs, activeTabId, splitTabId, updateTabLocation, setSplitTarget, toggleSplit } = useTabWorkspace()
  const [splitPercent, setSplitPercent] = useState(50)
  const containerRef = useRef<HTMLDivElement>(null)

  const [paneAEl, setPaneAEl] = useState<HTMLDivElement | null>(null)
  const [paneBEl, setPaneBEl] = useState<HTMLDivElement | null>(null)
  const [backgroundEl, setBackgroundEl] = useState<HTMLDivElement | null>(null)

  const onDragStart = (e: React.MouseEvent) => {
    e.preventDefault()
    const container = containerRef.current
    if (!container) return
    const move = (ev: MouseEvent) => {
      const rect = container.getBoundingClientRect()
      const pct = Math.max(20, Math.min(80, ((ev.clientX - rect.left) / rect.width) * 100))
      setSplitPercent(pct)
    }
    const up = () => {
      window.removeEventListener('mousemove', move)
      window.removeEventListener('mouseup', up)
    }
    window.addEventListener('mousemove', move)
    window.addEventListener('mouseup', up)
  }

  const activeTab = tabs.find((t) => t.id === activeTabId)
  const splitTab = splitTabId ? tabs.find((t) => t.id === splitTabId) : null

  return (
    <div className="flex h-full min-h-0 flex-col">
      <div ref={containerRef} className="flex min-h-0 flex-1">
        <div
          style={{ flex: splitTabId ? `0 0 ${splitPercent}%` : '1 1 100%' }}
          className="flex min-w-0 flex-col border-border-soft [&:not(:last-child)]:border-r"
        >
          {splitTabId && <PaneHeader label="Pane A" menu={menu} currentPath={activeTab?.path} />}
          <div ref={setPaneAEl} className="min-h-0 flex-1 overflow-y-auto" />
        </div>

        {splitTabId && (
          <>
            <div onMouseDown={onDragStart} title="Drag to resize" className="relative w-2.5 shrink-0 cursor-col-resize bg-panel">
              <div className="absolute top-1/2 left-1/2 flex h-11 w-1.5 -translate-x-1/2 -translate-y-1/2 items-center justify-center rounded-shell-sm bg-accent-2 opacity-80">
                <GripVertical className="size-3 text-white" aria-hidden />
              </div>
            </div>
            <div style={{ flex: `0 0 ${100 - splitPercent}%` }} className="flex min-w-0 flex-col">
              <PaneHeader label="Pane B" menu={menu} currentPath={splitTab?.path} onPick={setSplitTarget} onClose={toggleSplit} />
              <div ref={setPaneBEl} className="min-h-0 flex-1 overflow-y-auto" />
            </div>
          </>
        )}
      </div>

      <div ref={setBackgroundEl} style={{ display: 'none' }} />

      {tabs.map((tab) => {
        const target = tab.id === activeTabId ? paneAEl : tab.id === splitTabId ? paneBEl : backgroundEl
        if (!target) return null
        return createPortal(
          <TabRouterPane path={tab.path} onNavigate={(path, replace) => updateTabLocation(tab.id, path, replace)} />,
          target,
          tab.id,
        )
      })}
    </div>
  )
}
