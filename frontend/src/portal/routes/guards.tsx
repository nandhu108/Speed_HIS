import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { Spinner } from '../../components/ui/Spinner'
import { usePortalAuth } from '../features/auth/PortalAuthContext'

/** Blocks anonymous visitors; same shape as the staff RequireAuth guard but reads the portal-scoped auth context. */
export function RequirePortalAuth() {
  const { status } = usePortalAuth()
  const location = useLocation()

  if (status === 'loading') return <Spinner label="Restoring session…" />
  if (status === 'anonymous') {
    return <Navigate to="/portal/login" replace state={{ from: location.pathname }} />
  }
  return <Outlet />
}
