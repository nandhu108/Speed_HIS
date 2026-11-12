import { useRef } from 'react'
import { Router, useRoutes, type Navigator, type To } from 'react-router-dom'
import { appRoutes } from '../../routes/appRoutes'

function toPathString(to: To): string {
  if (typeof to === 'string') return to
  return `${to.pathname ?? ''}${to.search ?? ''}${to.hash ?? ''}`
}

function RouteTree() {
  return useRoutes(appRoutes)
}

/**
 * One open workspace tab's own isolated router — the load-bearing piece that makes "keep every
 * open tab mounted" possible without touching a single existing page. `appRoutes` is the exact
 * same RouteObject[] the top-level createBrowserRouter would otherwise render once; here it's
 * resolved independently per tab via useRoutes(), each fed this tab's own `path` through a
 * custom Navigator instead of the real browser history. Every page's useNavigate()/useParams()/
 * <Link> inside keeps working unmodified — it just resolves to this tab's router, not the
 * global one.
 *
 * This component itself has no opinion on visibility — TabbedWorkspace decides where each tab's
 * instance is currently *shown* by portaling it into a pane slot or a hidden background
 * container, without ever unmounting it (see TabbedWorkspace.tsx). That's what lets a
 * background tab's unsaved state survive switching away and back.
 */
export function TabRouterPane({
  path, onNavigate,
}: { path: string; onNavigate: (path: string, replace: boolean) => void }) {
  const onNavigateRef = useRef(onNavigate)
  onNavigateRef.current = onNavigate

  const navigatorRef = useRef<Navigator | null>(null)
  if (!navigatorRef.current) {
    navigatorRef.current = {
      createHref: (to) => toPathString(to),
      go: () => {
        // No independent back/forward stack per tab — switching tabs isn't a history event,
        // same as real browser tabs not sharing one another's back button.
      },
      // Preserved distinctly (not both collapsed to one call) so the active tab's push/replace
      // choice keeps reflecting correctly in the real browser history — see updateTabLocation.
      push: (to) => onNavigateRef.current(toPathString(to), false),
      replace: (to) => onNavigateRef.current(toPathString(to), true),
    }
  }

  return (
    <Router location={path} navigator={navigatorRef.current}>
      <RouteTree />
    </Router>
  )
}
