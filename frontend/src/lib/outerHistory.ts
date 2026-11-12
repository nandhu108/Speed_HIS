import { useEffect, useState } from 'react'

/**
 * A minimal, react-router-free navigation primitive for everything *outside* a workspace tab's
 * own isolated router (see lib/tabWorkspace/) — the top-level login/portal/staff-app switch,
 * the staff auth gate, and the tab workspace's "keep the real address bar in sync with the
 * active tab" logic all need to read/drive the actual browser URL, but none of them can sit
 * inside a React Router context: doing so would make `AppShell`'s descendants (which each mount
 * their own `<Router>` per tab) violate React Router's "no nested <Router>" invariant, since
 * nesting is a same-branch-anywhere-below rule, not a proximity one.
 *
 * `pushState`/`replaceState` don't fire `popstate` on their own, so `outerNavigate` dispatches a
 * synthetic event every listener here also listens for.
 */
const NAVIGATE_EVENT = 'prixo:outernav'

export function getOuterPath(): string {
  return `${window.location.pathname}${window.location.search}`
}

export function outerNavigate(path: string, options?: { replace?: boolean }) {
  if (options?.replace) window.history.replaceState(null, '', path)
  else window.history.pushState(null, '', path)
  window.dispatchEvent(new Event(NAVIGATE_EVENT))
}

export function useOuterPath(): string {
  const [path, setPath] = useState(getOuterPath)
  useEffect(() => {
    const onChange = () => setPath(getOuterPath())
    window.addEventListener('popstate', onChange)
    window.addEventListener(NAVIGATE_EVENT, onChange)
    return () => {
      window.removeEventListener('popstate', onChange)
      window.removeEventListener(NAVIGATE_EVENT, onChange)
    }
  }, [])
  return path
}
