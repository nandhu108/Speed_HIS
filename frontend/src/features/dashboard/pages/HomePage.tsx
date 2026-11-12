import { HeartPulse } from 'lucide-react'
import { ManagementDashboardPage } from '../../reports/pages/ManagementDashboardPage'
import { useAuth } from '../../auth/AuthContext'

// user.defaultLandingPath only redirects the very first navigation right after login
// (see LoginPage.tsx) — "/" itself always shows the real Dashboard, so the sidebar's
// "Dashboard" link keeps working as a normal link instead of bouncing back to the
// configured landing page every time it's clicked.
export function HomePage() {
  const { user, hasPermission } = useAuth()

  if (hasPermission('DASHBOARD.MANAGEMENT.VIEW')) {
    return <ManagementDashboardPage />
  }

  return (
    <div className="flex flex-col items-center justify-center gap-4 py-20 text-center">
      <span className="flex size-14 items-center justify-center rounded-shell bg-accent-2 shadow-shell-md">
        <HeartPulse className="size-7 text-white" aria-hidden />
      </span>
      <div>
        <h1 className="font-display text-xl font-bold text-ink">Welcome, {user?.fullName}</h1>
        <p className="mt-1 text-sm text-ink-muted">Use the sidebar to get to your work — search it if you're not sure where something lives.</p>
      </div>
    </div>
  )
}
