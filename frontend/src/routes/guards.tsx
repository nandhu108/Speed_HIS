import { Outlet } from 'react-router-dom'
import { useEffect } from 'react'
import type { ReactNode } from 'react'
import { Spinner } from '../components/ui/Spinner'
import { useAuth } from '../features/auth/AuthContext'
import { outerNavigate, useOuterPath } from '../lib/outerHistory'

/**
 * Blocks anonymous users; also forces the change-password flow before anything else. Wraps
 * `children` (not a React Router route-guard shape, since it sits above AppShell — which has no
 * router ancestor at all, see lib/outerHistory.ts) rather than rendering an `<Outlet/>`.
 */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { status, user } = useAuth()
  const outerPath = useOuterPath()
  const needsLogin = status === 'anonymous'
  const needsPasswordChange = status === 'authenticated' && !!user?.mustChangePassword && outerPath !== '/change-password'

  useEffect(() => {
    // Once either redirect fires, RootSwitch swaps away from rendering this component entirely
    // (pathname no longer matches the staff-app branch), so this never gets a chance to loop.
    if (needsLogin) outerNavigate(`/login?from=${encodeURIComponent(outerPath)}`, { replace: true })
    else if (needsPasswordChange) outerNavigate('/change-password', { replace: true })
  }, [needsLogin, needsPasswordChange, outerPath])

  if (status === 'loading' || needsLogin || needsPasswordChange) return <Spinner label="Restoring session…" />
  return <>{children}</>
}

/** Hides a route the user has no permission for. Cosmetic — the API is the gate. Runs inside a workspace tab's own isolated router (see appRoutes.tsx), so <Outlet/> here is completely unrelated to RequireAuth above — a normal, valid React Router context. */
export function RequirePermission({ permission }: { permission: string }) {
  const { hasPermission } = useAuth()
  if (!hasPermission(permission)) {
    return (
      <div className="py-16 text-center">
        <p className="text-lg font-semibold text-slate-700">403 — No permission</p>
        <p className="mt-1 text-sm text-slate-500">
          You do not have access to this page. Contact your administrator.
        </p>
      </div>
    )
  }
  return <Outlet />
}
