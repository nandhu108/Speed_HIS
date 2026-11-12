import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { AppShell } from '../components/layout/AppShell'
import { useOuterPath } from '../lib/outerHistory'
import { PortalAuthProvider } from '../portal/features/auth/PortalAuthContext'
import { PortalLayout } from '../portal/layout/PortalLayout'
import { PortalAppointmentsPage } from '../portal/pages/PortalAppointmentsPage'
import { PortalFeedbackPage } from '../portal/pages/PortalFeedbackPage'
import { PortalHomePage } from '../portal/pages/PortalHomePage'
import { PortalLoginPage } from '../portal/pages/PortalLoginPage'
import { PortalMedicalHistoryPage } from '../portal/pages/PortalMedicalHistoryPage'
import { PortalRecordsPage } from '../portal/pages/PortalRecordsPage'
import { PortalRegisterPage } from '../portal/pages/PortalRegisterPage'
import { RequirePortalAuth } from '../portal/routes/guards'
import { LoginPage } from '../features/auth/pages/LoginPage'
import { RequireAuth } from './guards'

/**
 * The top-level "which mode is the app in" switch — deliberately *not* a React Router
 * `<Router>`/`<RouterProvider>`, because `AppShell` and everything below it (see
 * lib/tabWorkspace/) needs to be completely free of any ambient router context: each open
 * workspace tab mounts its own independent `<Router>`, and React Router refuses to render a
 * `<Router>` anywhere inside another one's tree, no matter how many plain components sit in
 * between. Reads the real browser URL via lib/outerHistory.ts instead.
 *
 * The Patient Portal doesn't need any of that (no tabs there), so it keeps a normal, ordinary
 * `<BrowserRouter>` of its own — entirely fine since it's never an ancestor of anything that
 * also tries to mount a router.
 */
export function RootSwitch() {
  const pathname = useOuterPath().split('?')[0]!

  if (pathname === '/login') return <LoginPage />

  if (pathname.startsWith('/portal')) {
    return (
      <PortalAuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/portal/login" element={<PortalLoginPage />} />
            <Route path="/portal/register" element={<PortalRegisterPage />} />
            <Route element={<RequirePortalAuth />}>
              <Route element={<PortalLayout />}>
                <Route path="/portal" element={<PortalHomePage />} />
                <Route path="/portal/appointments" element={<PortalAppointmentsPage />} />
                <Route path="/portal/records" element={<PortalRecordsPage />} />
                <Route path="/portal/medical-history" element={<PortalMedicalHistoryPage />} />
                <Route path="/portal/feedback" element={<PortalFeedbackPage />} />
              </Route>
            </Route>
          </Routes>
        </BrowserRouter>
      </PortalAuthProvider>
    )
  }

  return (
    <RequireAuth>
      <AppShell />
    </RequireAuth>
  )
}
