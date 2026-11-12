import { Link } from 'react-router-dom'
import { usePortalAuth } from '../features/auth/PortalAuthContext'

export function PortalHomePage() {
  const { patient } = usePortalAuth()

  const cards = [
    { to: '/portal/appointments', title: 'Appointments', desc: 'Book a new appointment or view/cancel upcoming ones' },
    { to: '/portal/records', title: 'My Records', desc: 'Lab reports, radiology reports, prescriptions, bills, discharge summaries' },
    { to: '/portal/medical-history', title: 'Medical History', desc: 'Conditions, surgeries, allergies and immunizations on file' },
    { to: '/portal/feedback', title: 'Feedback', desc: 'Rate a visit or admission' },
  ]

  return (
    <>
      <h1 className="mb-1 text-xl font-semibold text-slate-800">Welcome, {patient?.fullName}</h1>
      <p className="mb-6 text-sm text-slate-500">UHID: {patient?.uhid}</p>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        {cards.map((c) => (
          <Link key={c.to} to={c.to} className="rounded-lg border border-slate-200 bg-white p-4 hover:border-primary-300 hover:shadow-sm">
            <h2 className="mb-1 font-semibold text-slate-800">{c.title}</h2>
            <p className="text-sm text-slate-500">{c.desc}</p>
          </Link>
        ))}
      </div>
    </>
  )
}
