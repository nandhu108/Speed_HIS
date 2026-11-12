import { NavLink, Outlet } from 'react-router-dom'
import { usePortalAuth } from '../features/auth/PortalAuthContext'

const links = [
  { to: '/portal', label: 'Home', end: true },
  { to: '/portal/appointments', label: 'Appointments' },
  { to: '/portal/records', label: 'My Records' },
  { to: '/portal/medical-history', label: 'Medical History' },
  { to: '/portal/feedback', label: 'Feedback' },
]

export function PortalLayout() {
  const { patient, logout } = usePortalAuth()

  return (
    <div className="min-h-screen bg-slate-50">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
          <div className="flex items-center gap-6">
            <span className="text-lg font-semibold text-primary-700">Prixo Patient Portal</span>
            <nav className="flex gap-4">
              {links.map((l) => (
                <NavLink
                  key={l.to}
                  to={l.to}
                  end={l.end}
                  className={({ isActive }) => `text-sm font-medium ${isActive ? 'text-primary-700' : 'text-slate-500 hover:text-slate-700'}`}
                >
                  {l.label}
                </NavLink>
              ))}
            </nav>
          </div>
          <div className="flex items-center gap-3 text-sm text-slate-600">
            <span>{patient?.fullName} ({patient?.uhid})</span>
            <button onClick={() => logout()} className="text-red-600 hover:underline">Logout</button>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-5xl px-4 py-6">
        <Outlet />
      </main>
    </div>
  )
}
