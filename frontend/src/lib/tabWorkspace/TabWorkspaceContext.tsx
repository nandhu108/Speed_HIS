import { createContext, useContext, useEffect, useRef, useState, type ReactNode } from 'react'
import { outerNavigate, useOuterPath } from '../outerHistory'

export interface WorkspaceTab {
  id: string
  /** pathname + search this tab currently shows — drifts as the tab navigates internally (e.g. a list → a detail row), independent of every other open tab. */
  path: string
}

interface TabWorkspaceContextValue {
  tabs: WorkspaceTab[]
  activeTabId: string
  splitTabId: string | null
  /** Opens (or activates, if one's already showing this exact path) a brand-new tab — used for
   * explicit "open in a new tab" actions (Ctrl/Cmd+click on a sidebar item, the user menu). */
  openTab: (path: string) => void
  /** Navigates the currently active tab to this path in place (or activates an existing tab
   * already showing it, rather than creating a duplicate) — what a plain sidebar click does. */
  openInActiveTab: (path: string) => void
  closeTab: (id: string) => void
  activateTab: (id: string) => void
  /** Called by a tab's own navigator whenever a page inside it navigates — keeps that tab (and, if it's the active one, the real browser URL — pushing or replacing to match what the page itself asked for) in sync. */
  updateTabLocation: (id: string, path: string, replace: boolean) => void
  toggleSplit: () => void
  /** Opens (or activates) a tab into the split pane, independent of the primary active tab. */
  setSplitTarget: (path: string) => void
}

const TabWorkspaceContext = createContext<TabWorkspaceContextValue | null>(null)

const STORAGE_KEY = 'prixo:workspace-tabs'
let nextId = 1
const makeId = () => `tab-${nextId++}`

interface PersistedShape {
  paths: string[]
  activePath: string
}

/** Open tabs are only meaningful for whoever is currently logged in — call this on login and
 * logout so the next person to use this browser tab doesn't inherit (and can't peek at) the
 * previous account's open screens. */
export function clearPersistedTabs() {
  try {
    sessionStorage.removeItem(STORAGE_KEY)
  } catch {
    // sessionStorage unavailable — nothing to clear.
  }
}

function readPersisted(): PersistedShape | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as PersistedShape) : null
  } catch {
    return null
  }
}

export function TabWorkspaceProvider({ children }: { children: ReactNode }) {
  const currentPath = useOuterPath()

  const [tabs, setTabs] = useState<WorkspaceTab[]>(() => {
    const persisted = readPersisted()
    const paths = persisted?.paths.length ? persisted.paths : [currentPath]
    return paths.map((path) => ({ id: makeId(), path }))
  })
  const [activeTabId, setActiveTabId] = useState<string>(() => {
    const persisted = readPersisted()
    const activeIndex = persisted ? tabs.findIndex((t) => t.path === persisted.activePath) : 0
    return tabs[Math.max(0, activeIndex)]?.id ?? tabs[0].id
  })
  const [splitTabId, setSplitTabId] = useState<string | null>(null)

  // Keep the outer browser URL in sync whenever the active tab's path changes underneath it.
  // Tab-switch/open/close always replace (switching tabs isn't itself a back-button-able event,
  // same as real browser tabs); in-page navigation forwards whatever push/replace the page asked for.
  const syncOuterUrl = (path: string, replace = true) => {
    if (currentPath !== path) {
      outerNavigate(path, { replace })
    }
  }

  // Reconcile once on mount: a bookmarked/pasted/refreshed URL should always show in the active
  // tab, even if the restored tab list's active path is stale. Deliberately runs only once —
  // every subsequent path change is driven by tab navigation, not the outer router.
  const reconciled = useRef(false)
  useEffect(() => {
    if (reconciled.current) return
    reconciled.current = true
    setTabs((prev) => {
      const active = prev.find((t) => t.id === activeTabId)
      if (!active || active.path === currentPath) return prev
      return prev.map((t) => (t.id === activeTabId ? { ...t, path: currentPath } : t))
    })
  }, [activeTabId, currentPath])

  useEffect(() => {
    const active = tabs.find((t) => t.id === activeTabId)
    const persisted: PersistedShape = { paths: tabs.map((t) => t.path), activePath: active?.path ?? tabs[0]?.path ?? '/' }
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(persisted))
    } catch {
      // sessionStorage unavailable (private mode, quota) — tab persistence is a nicety, not required.
    }
  }, [tabs, activeTabId])

  const activateTab = (id: string) => {
    const tab = tabs.find((t) => t.id === id)
    if (tab) syncOuterUrl(tab.path)
    setActiveTabId(id)
  }

  const openTab = (path: string) => {
    const existing = tabs.find((t) => t.path === path)
    if (existing) {
      activateTab(existing.id)
      return
    }
    const tab: WorkspaceTab = { id: makeId(), path }
    setTabs((prev) => [...prev, tab])
    syncOuterUrl(path)
    setActiveTabId(tab.id)
  }

  const openInActiveTab = (path: string) => {
    // Matched by pathname only (not the full path+search) — a tab whose in-page state has
    // drifted to carry a query string (pagination, filters) still counts as "already showing
    // this screen" for a plain sidebar click, so it's reused instead of leaving a stray
    // look-alike tab open next to it.
    const pathname = path.split('?')[0]
    const existing = tabs.find((t) => t.path.split('?')[0] === pathname)
    if (existing) {
      activateTab(existing.id)
      return
    }
    updateTabLocation(activeTabId, path, true)
  }

  const closeTab = (id: string) => {
    if (tabs.length <= 1) return
    const closingIndex = tabs.findIndex((t) => t.id === id)
    const next = tabs.filter((t) => t.id !== id)
    setTabs(next)
    if (splitTabId === id) setSplitTabId(null)
    if (activeTabId === id) {
      const fallback = next[closingIndex - 1] ?? next[0]
      syncOuterUrl(fallback.path)
      setActiveTabId(fallback.id)
    }
  }

  const updateTabLocation = (id: string, path: string, replace: boolean) => {
    setTabs((prev) => prev.map((t) => (t.id === id ? { ...t, path } : t)))
    if (activeTabId === id) syncOuterUrl(path, replace)
  }

  const toggleSplit = () => {
    if (splitTabId) {
      setSplitTabId(null)
      return
    }
    const active = tabs.find((t) => t.id === activeTabId)
    const tab: WorkspaceTab = { id: makeId(), path: active?.path ?? '/' }
    setTabs((prev) => [...prev, tab])
    setSplitTabId(tab.id)
  }

  const setSplitTarget = (path: string) => {
    const existing = tabs.find((t) => t.path === path)
    if (existing) {
      setSplitTabId(existing.id)
      return
    }
    const tab: WorkspaceTab = { id: makeId(), path }
    setTabs((prev) => [...prev, tab])
    setSplitTabId(tab.id)
  }

  const value: TabWorkspaceContextValue = {
    tabs, activeTabId, splitTabId, openTab, openInActiveTab, closeTab, activateTab, updateTabLocation, toggleSplit, setSplitTarget,
  }

  return <TabWorkspaceContext.Provider value={value}>{children}</TabWorkspaceContext.Provider>
}

export function useTabWorkspace(): TabWorkspaceContextValue {
  const ctx = useContext(TabWorkspaceContext)
  if (!ctx) throw new Error('useTabWorkspace must be used within a TabWorkspaceProvider')
  return ctx
}
