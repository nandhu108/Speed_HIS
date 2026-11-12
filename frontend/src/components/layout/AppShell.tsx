import { useQuery } from '@tanstack/react-query'
import { ChevronDown, ChevronLeft, ChevronRight, KeyRound, LogOut, Moon, Search, Sun, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse } from '../../api/types/api'
import { useAuth } from '../../features/auth/AuthContext'
import { outerNavigate, useOuterPath } from '../../lib/outerHistory'
import { useTheme } from '../../lib/ThemeContext'
import { TabWorkspaceProvider, useTabWorkspace } from '../../lib/tabWorkspace/TabWorkspaceContext'
import { MenuIconFor } from '../ui/menuIcons'
import { TabbedWorkspace } from './TabbedWorkspace'
import { TabStrip } from './TabStrip'

export interface MenuNode {
  id: number
  parentId: number | null
  title: string
  icon: string | null
  path: string | null
  permissionCode: string | null
  sortOrder: number
  isActive: boolean
  children: MenuNode[]
}

export function useMyMenu() {
  return useQuery({
    queryKey: ['menus', 'my'],
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<MenuNode[]>>(endpoints.myMenu)
      return data.data ?? []
    },
    staleTime: 5 * 60_000,
  })
}

function nodeMatches(node: MenuNode, query: string): boolean {
  if (node.title.toLowerCase().includes(query)) return true
  return node.children.some((child) => nodeMatches(child, query))
}

function filterMenu(nodes: MenuNode[], query: string): MenuNode[] {
  if (!query) return nodes
  return nodes
    .filter((node) => nodeMatches(node, query))
    .map((node) => (node.children.length ? { ...node, children: filterMenu(node.children, query) } : node))
}

/** A node's path "owns" a route if the route is that path exactly, or a sub-path of it — never
 * just a same-prefix sibling (e.g. `/appointments` must not claim `/appointments/queue-display`). */
function pathOwnsRoute(nodePath: string | null, pathname: string): boolean {
  if (!nodePath) return false
  if (nodePath === '/') return pathname === '/'
  return pathname === nodePath || pathname.startsWith(`${nodePath}/`)
}

/**
 * Depth-first search for the menu node that owns the current route. Several menu entries can
 * legitimately own the same route as a prefix (e.g. `/appointments` and
 * `/appointments/queue-display` both own `/appointments/queue-display`) — the *longest* matching
 * path wins, since it's the most specific one.
 */
export function findActiveNode(nodes: MenuNode[], pathname: string): { node: MenuNode; group: MenuNode | null } | null {
  function search(list: MenuNode[], parent: MenuNode | null): { node: MenuNode; group: MenuNode | null } | null {
    let best: { node: MenuNode; group: MenuNode | null } | null = null
    let bestLength = -1

    for (const node of list) {
      if (pathOwnsRoute(node.path, pathname) && (node.path?.length ?? 0) > bestLength) {
        best = { node, group: parent }
        bestLength = node.path!.length
      }
      // Recurse to any depth — per-role menu layouts can nest a page arbitrarily deep now
      // (e.g. Front Office > Registration > OP Registration), not just one level.
      const nested = search(node.children, node)
      if (nested && (nested.node.path?.length ?? 0) > bestLength) {
        best = nested
        bestLength = nested.node.path!.length
      }
    }
    return best
  }

  return search(nodes, null)
}

function titleCase(segment: string): string {
  return segment.replace(/-/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase())
}

/**
 * Best-effort label/icon for a workspace tab. `findActiveNode` only matches paths that are
 * *exact menu entries* — a detail page reached by clicking a row (e.g. `/emergency/visits/8`,
 * `/ipd/admissions/42`) has no menu entry of its own, so it falls through to the top-level
 * group that owns its first path segment (e.g. "Emergency"), and only as a last resort to a
 * title-cased guess from the path itself.
 */
export function resolveTabMeta(menu: MenuNode[], path: string): { label: string; icon: string | null } {
  const pathname = path.split('?')[0]!
  const exact = findActiveNode(menu, pathname)
  if (exact) return { label: exact.node.title, icon: exact.node.icon }

  const firstSegment = `/${pathname.split('/').filter(Boolean)[0] ?? ''}`
  const group = menu.find((g) => g.path === firstSegment || g.children.some((c) => c.path?.startsWith(firstSegment)))
  if (group) return { label: group.title, icon: group.icon }

  const lastSegment = pathname.split('/').filter(Boolean).pop()
  return { label: lastSegment ? titleCase(lastSegment) : 'Home', icon: null }
}

/** Does this subtree contain the currently-active page? Drives which groups auto-expand. */
function containsNode(node: MenuNode, activeNodeId: number | null): boolean {
  if (activeNodeId == null) return false
  return node.children.some((child) => child.id === activeNodeId || containsNode(child, activeNodeId))
}

function SidebarNode({
  node,
  activeNodeId,
  forceOpen,
  collapsed,
  onExpandSidebar,
}: {
  node: MenuNode
  activeNodeId: number | null
  forceOpen: boolean
  collapsed: boolean
  onExpandSidebar: () => void
}) {
  // Groups used to all start expanded, which on a full-permission role rendered every module's
  // children at once — several screens' worth of scrolling to reach anything below "Billing".
  // They now start closed and open on their own when they hold the active page.
  const holdsActive = containsNode(node, activeNodeId)
  const [open, setOpen] = useState(holdsActive)
  useEffect(() => {
    if (holdsActive) setOpen(true)
  }, [holdsActive])

  const { openTab, openInActiveTab } = useTabWorkspace()
  const isActive = node.id === activeNodeId
  const hasChildren = node.children.length > 0
  const isOpen = !collapsed && (forceOpen || open)

  const linkClass = `flex items-center gap-2.5 rounded-shell-sm px-3 py-2 text-[13px] font-semibold transition-colors ${
    collapsed ? 'justify-center' : ''
  } ${isActive ? 'bg-panel-2 text-ink shadow-[inset_2px_0_0_0_var(--color-accent-2)]' : 'text-ink-muted hover:bg-panel-2 hover:text-ink'}`

  const handleLinkClick = (path: string) => (e: React.MouseEvent) => {
    // A real <a href> underneath (so shift-click/middle-click "open in new browser
    // window/tab" still works as a plain navigation). A plain left-click navigates the
    // current workspace tab in place; Ctrl/Cmd+click instead opens a new workspace tab.
    if (e.button !== 0 || e.shiftKey || e.altKey) return
    e.preventDefault()
    if (e.metaKey || e.ctrlKey) {
      openTab(path)
    } else {
      openInActiveTab(path)
    }
  }

  const childList = hasChildren && isOpen && (
    <div className="mt-1 ml-4 space-y-0.5 border-l border-border-soft pl-2">
      {node.children.map((child) => (
        <SidebarNode
          key={child.id}
          node={child}
          activeNodeId={activeNodeId}
          forceOpen={forceOpen}
          collapsed={collapsed}
          onExpandSidebar={onExpandSidebar}
        />
      ))}
    </div>
  )

  // A node with both a path AND children (e.g. "Dashboard" once a role nests dashboards
  // under it) needs two independent click targets: the label navigates, a separate chevron
  // toggles the children — neither the pure-link nor pure-group case below covers that.
  if (node.path && hasChildren) {
    const path = node.path
    return (
      <div>
        <div className={`flex items-center gap-0.5 ${collapsed ? 'flex-col' : ''}`}>
          <a href={path} title={collapsed ? node.title : undefined} onClick={handleLinkClick(path)} className={`min-w-0 flex-1 ${linkClass}`}>
            <MenuIconFor name={node.icon} />
            {!collapsed && node.title}
          </a>
          {!collapsed && (
            <button
              type="button"
              title={isOpen ? 'Collapse' : 'Expand'}
              onClick={() => setOpen((value) => !value)}
              className="flex shrink-0 items-center justify-center rounded-shell-sm p-2 text-ink-muted hover:bg-panel-2 hover:text-ink"
            >
              <ChevronDown className={`size-3.5 transition-transform ${isOpen ? '' : '-rotate-90'}`} aria-hidden />
            </button>
          )}
        </div>
        {childList}
      </div>
    )
  }

  if (node.path) {
    const path = node.path
    return (
      <a href={path} title={collapsed ? node.title : undefined} onClick={handleLinkClick(path)} className={linkClass}>
        <MenuIconFor name={node.icon} />
        {!collapsed && node.title}
      </a>
    )
  }

  return (
    <div>
      <button
        type="button"
        title={collapsed ? node.title : undefined}
        onClick={() => {
          if (collapsed) {
            onExpandSidebar()
            setOpen(true)
            return
          }
          setOpen((value) => !value)
        }}
        aria-expanded={hasChildren ? isOpen : undefined}
        // A closed group still shows where you are, so the active page isn't invisible once
        // its parent collapses.
        className={`flex w-full items-center gap-2.5 rounded-shell-sm px-3 py-2 text-[13px] font-semibold transition-colors hover:bg-panel-2 hover:text-ink ${
          collapsed ? 'justify-center' : ''
        } ${holdsActive ? 'text-ink' : 'text-ink-muted'}`}
      >
        <MenuIconFor name={node.icon} />
        {!collapsed && (
          <>
            <span className="flex-1 text-left">{node.title}</span>
            <ChevronDown className={`size-3.5 transition-transform ${isOpen ? '' : '-rotate-90'}`} aria-hidden />
          </>
        )}
      </button>
      {childList}
    </div>
  )
}

function initialsOf(name: string) {
  return name
    .split(' ')
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase()
}

function roleLabel(roles: string[]) {
  if (!roles.length) return 'Staff'
  return roles[0]
    .toLowerCase()
    .split('_')
    .map((w) => w[0]?.toUpperCase() + w.slice(1))
    .join(' ')
}

/** Opens Change Password as a workspace tab rather than a bare outer-URL navigation — needs
 * useTabWorkspace(), which is only reachable from inside AppShell's own JSX output (where
 * TabWorkspaceProvider has already been mounted), not from AppShell's own function body. */
function ChangePasswordMenuItem({ onDone }: { onDone: () => void }) {
  const { openTab } = useTabWorkspace()
  return (
    <button
      type="button"
      onClick={() => {
        onDone()
        openTab('/change-password')
      }}
      className="flex w-full items-center gap-2 px-3 py-2 text-sm text-ink-muted hover:bg-panel-2 hover:text-ink"
    >
      <KeyRound className="size-4" aria-hidden /> Change password
    </button>
  )
}

const SIDEBAR_KEY = 'prixo.sidebar.collapsed'
const MENU_SEARCH_ID = 'prixo-menu-search'

export function AppShell() {
  const { user, logout } = useAuth()
  const { theme, toggleTheme } = useTheme()
  const outerPath = useOuterPath()
  const { data: menu = [] } = useMyMenu()
  const [userMenuOpen, setUserMenuOpen] = useState(false)
  const [menuSearch, setMenuSearch] = useState('')
  // Sticky across reloads — staff who work collapsed had to re-collapse on every refresh.
  const [collapsed, setCollapsed] = useState(() => localStorage.getItem(SIDEBAR_KEY) === '1')
  useEffect(() => {
    localStorage.setItem(SIDEBAR_KEY, collapsed ? '1' : '0')
  }, [collapsed])

  const trimmedSearch = menuSearch.trim().toLowerCase()
  const visibleMenu = filterMenu(menu, trimmedSearch)

  const active = useMemo(() => findActiveNode(menu, outerPath.split('?')[0]!), [menu, outerPath])

  // Ctrl/Cmd+K jumps to the menu search from anywhere — the fastest path between modules for
  // staff who know the screen name but not which group it lives under.
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault()
        setCollapsed(false)
        requestAnimationFrame(() => document.getElementById(MENU_SEARCH_ID)?.focus())
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [])

  const handleLogout = async () => {
    await logout()
    outerNavigate('/login', { replace: true })
  }

  return (
    <TabWorkspaceProvider>
      <div className="relative flex h-dvh overflow-hidden bg-shell-bg">
        <aside
          className={`relative z-10 flex min-h-0 shrink-0 flex-col border-r border-border-soft bg-panel transition-[width] duration-200 ${
            collapsed ? 'w-[68px]' : 'w-64'
          }`}
        >
          <button
            type="button"
            onClick={() => setCollapsed((v) => !v)}
            title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
            className="absolute top-4 -right-3 z-20 flex size-6 items-center justify-center rounded-full border border-border-strong bg-panel-solid text-ink-muted shadow-shell-sm hover:text-ink"
          >
            {collapsed ? <ChevronRight className="size-3" aria-hidden /> : <ChevronLeft className="size-3" aria-hidden />}
          </button>
  
          <div className={`flex h-15 shrink-0 items-center gap-2.5 border-b border-border-soft px-4 ${collapsed ? 'justify-center px-0' : ''}`}>
            <span className="flex size-9 shrink-0 items-center justify-center rounded-shell-sm bg-accent-2 text-sm font-bold text-white shadow-shell-sm">
              P
            </span>
            {!collapsed && (
              <div className="min-w-0">
                <div className="font-display text-[15px] leading-tight font-bold tracking-tight text-ink">Prixo</div>
                <div className="text-[9px] font-semibold tracking-[0.14em] text-ink-faint uppercase">Hospital OS</div>
              </div>
            )}
          </div>
  
          {!collapsed && (
            <div className="border-b border-border-soft p-3">
              <div className="relative">
                <Search className="pointer-events-none absolute top-1/2 left-2.5 size-3.5 -translate-y-1/2 text-ink-faint" aria-hidden />
                <input
                  id={MENU_SEARCH_ID}
                  type="text"
                  value={menuSearch}
                  onChange={(e) => setMenuSearch(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Escape') setMenuSearch('')
                  }}
                  placeholder="Search menu…"
                  aria-label="Search menu"
                  className="w-full rounded-shell-sm border border-border-soft bg-panel-2 py-1.5 pr-14 pl-8 text-sm text-ink outline-none placeholder:text-ink-faint focus:border-accent-2 focus:ring-1 focus:ring-accent-2/30"
                />
                {!menuSearch && (
                  <kbd className="pointer-events-none absolute top-1/2 right-2 -translate-y-1/2 rounded border border-border-soft bg-panel px-1.5 py-0.5 text-[10px] font-semibold text-ink-faint">
                    Ctrl K
                  </kbd>
                )}
                {menuSearch && (
                  <button
                    type="button"
                    onClick={() => setMenuSearch('')}
                    aria-label="Clear menu search"
                    className="absolute top-1/2 right-2 -translate-y-1/2 text-ink-faint hover:text-ink"
                  >
                    <X className="size-3.5" aria-hidden />
                  </button>
                )}
              </div>
            </div>
          )}
  
          <nav className="min-h-0 flex-1 space-y-1 overflow-y-auto p-3">
            {visibleMenu.length === 0 && trimmedSearch && (
              <p className="px-3 py-2 text-sm text-ink-faint">No menu items match "{menuSearch.trim()}".</p>
            )}
            {visibleMenu.map((node) => (
              <SidebarNode
                key={node.id}
                node={node}
                activeNodeId={active?.node.id ?? null}
                forceOpen={!!trimmedSearch}
                collapsed={collapsed}
                onExpandSidebar={() => setCollapsed(false)}
              />
            ))}
          </nav>
  
          <div className="shrink-0 border-t border-border-soft p-2.5">
            <div className={`flex items-center gap-2.5 rounded-shell-sm bg-panel-2 p-2 ${collapsed ? 'justify-center' : ''}`}>
              <span className="flex size-8 shrink-0 items-center justify-center rounded-shell-sm bg-accent-2 text-xs font-bold text-white">
                {user ? initialsOf(user.fullName) : ''}
              </span>
              {!collapsed && (
                <div className="min-w-0 flex-1">
                  <div className="truncate text-xs font-semibold text-ink">{user?.fullName}</div>
                  <div className="text-[10px] text-ink-faint">{user ? roleLabel(user.roles) : ''}</div>
                </div>
              )}
            </div>
          </div>
        </aside>
  
        <div className="relative z-10 flex min-h-0 min-w-0 flex-1 flex-col">
          <header className="flex h-15 shrink-0 items-center gap-4 border-b border-border-soft bg-panel px-6">
            <div className="min-w-0">
              <div className="font-display truncate text-base font-bold text-ink">{active?.node.title ?? 'Prixo HIS'}</div>
              <div className="truncate text-[11px] text-ink-faint">{active?.group?.title ?? user?.branchName}</div>
            </div>
  
            <div className="ml-auto flex items-center gap-2">
              <button
                type="button"
                onClick={toggleTheme}
                title="Toggle theme"
                className="flex size-9 items-center justify-center rounded-shell-sm border border-border-soft bg-panel-2 text-ink-muted hover:text-ink"
              >
                {theme === 'dark' ? <Sun className="size-4" aria-hidden /> : <Moon className="size-4" aria-hidden />}
              </button>
  
              <div className="relative">
                <button
                  type="button"
                  onClick={() => setUserMenuOpen((value) => !value)}
                  className="flex items-center gap-2 rounded-shell-sm px-2 py-1.5 text-sm font-medium text-ink hover:bg-panel-2"
                >
                  <span className="flex size-7 items-center justify-center rounded-full bg-accent-2 text-xs font-semibold text-white">
                    {user ? initialsOf(user.fullName) : ''}
                  </span>
                  <span className="hidden sm:inline">{user?.fullName}</span>
                  <ChevronDown className="size-4 text-ink-faint" aria-hidden />
                </button>
                {userMenuOpen && (
                  <div
                    className="absolute right-0 z-20 mt-1 w-48 rounded-shell-sm border border-border-soft bg-panel-solid py-1 shadow-shell-lg"
                    onMouseLeave={() => setUserMenuOpen(false)}
                  >
                    <ChangePasswordMenuItem onDone={() => setUserMenuOpen(false)} />
                    <button
                      type="button"
                      onClick={handleLogout}
                      className="flex w-full items-center gap-2 px-3 py-2 text-sm text-crit hover:bg-crit/10"
                    >
                      <LogOut className="size-4" aria-hidden /> Sign out
                    </button>
                  </div>
                )}
              </div>
            </div>
          </header>
          <TabStrip />
          <main className="min-h-0 flex-1 overflow-y-auto p-6">
            <TabbedWorkspace />
          </main>
        </div>
      </div>
    </TabWorkspaceProvider>
  )
}
