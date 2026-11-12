import { useNavigate } from 'react-router-dom'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useIcuDashboard } from '../api'
import { SEVERITY_LEVEL_LABELS, type IcuSeverityLevel } from '../types'

const severityTone: Record<IcuSeverityLevel, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info', 2: 'warning', 3: 'danger', 4: 'danger',
}

export function IcuDashboardPage() {
  const { data, isLoading } = useIcuDashboard()
  const navigate = useNavigate()

  return (
    <>
      <PageHeader title="ICU Dashboard" subtitle="Census, ventilated count, and severity across every ICU-flagged ward" />

      {isLoading && <Spinner />}
      {data && (
        <>
          <div className="mb-6 grid grid-cols-2 gap-4 sm:grid-cols-3">
            <StatCard label="Census" value={data.census} />
            <StatCard label="Ventilated" value={data.ventilatedCount} />
            <StatCard label="Unacknowledged Alerts" value={data.patients.reduce((sum, p) => sum + p.unacknowledgedAlertCount, 0)} />
          </div>

          <div className="rounded-lg border border-slate-200 bg-white">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50 text-left">
                  <th className="px-4 py-2.5 font-medium text-slate-600">Admission</th>
                  <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
                  <th className="px-4 py-2.5 font-medium text-slate-600">Bed</th>
                  <th className="px-4 py-2.5 font-medium text-slate-600">Ventilated</th>
                  <th className="px-4 py-2.5 font-medium text-slate-600">Severity</th>
                  <th className="px-4 py-2.5 text-right font-medium text-slate-600">Alerts</th>
                </tr>
              </thead>
              <tbody>
                {data.patients.map((p) => (
                  <tr
                    key={p.admissionId}
                    className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50"
                    onClick={() => navigate(`/ipd/admissions/${p.admissionId}`)}
                  >
                    <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{p.admissionNumber}</td>
                    <td className="px-4 py-2.5 text-slate-700">{p.patientName}</td>
                    <td className="px-4 py-2.5 text-slate-700">{p.wardName}/{p.roomName}/{p.bedCode}</td>
                    <td className="px-4 py-2.5">
                      <StatusBadge tone={p.isVentilated ? 'warning' : 'neutral'}>{p.isVentilated ? 'Yes' : 'No'}</StatusBadge>
                    </td>
                    <td className="px-4 py-2.5">
                      {p.latestSeverity ? (
                        <StatusBadge tone={severityTone[p.latestSeverity]}>{SEVERITY_LEVEL_LABELS[p.latestSeverity]}</StatusBadge>
                      ) : (
                        <span className="text-slate-400">Not assessed</span>
                      )}
                    </td>
                    <td className="px-4 py-2.5 text-right">
                      {p.unacknowledgedAlertCount > 0 ? (
                        <StatusBadge tone="danger">{p.unacknowledgedAlertCount}</StatusBadge>
                      ) : (
                        <span className="text-slate-400">0</span>
                      )}
                    </td>
                  </tr>
                ))}
                {data.patients.length === 0 && (
                  <tr>
                    <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                      No patients currently on an ICU-flagged ward.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </>
      )}
    </>
  )
}

function StatCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <p className="text-xs text-slate-500">{label}</p>
      <p className="mt-1 text-2xl font-semibold text-slate-800">{value}</p>
    </div>
  )
}
