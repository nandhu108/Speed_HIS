import { Link } from 'react-router-dom'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useDoctorDashboard } from '../api'

function StatCard({ label, value, to, tone }: { label: string; value: number; to: string; tone?: 'danger' | 'warning' }) {
  const valueColor = tone === 'danger' ? 'text-red-600' : tone === 'warning' ? 'text-amber-600' : 'text-slate-800'
  return (
    <Link to={to} className="rounded-lg border border-slate-200 bg-white p-4 hover:border-primary-300 hover:shadow-sm">
      <p className={`text-3xl font-bold ${valueColor}`}>{value}</p>
      <p className="mt-1 text-sm text-slate-500">{label}</p>
    </Link>
  )
}

export function DoctorDashboardPage() {
  const { data, isLoading } = useDoctorDashboard()
  if (isLoading || !data) return <Spinner />

  return (
    <>
      <PageHeader title="Doctor Dashboard" subtitle="Your day at a glance" />
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label="Today's OP visits" value={data.todayOpCount} to="/doctor-portal/queue" />
        <StatCard label="Active inpatients" value={data.activeInpatients} to="/doctor-portal/inpatients" />
        <StatCard label="Pending discharge signs" value={data.pendingDischargeSigns} to="/doctor-portal/discharge-approvals" tone={data.pendingDischargeSigns > 0 ? 'warning' : undefined} />
        <StatCard label="Unacknowledged critical alerts" value={data.unacknowledgedCriticalAlerts} to="/doctor-portal/orders" tone={data.unacknowledgedCriticalAlerts > 0 ? 'danger' : undefined} />
      </div>
    </>
  )
}
