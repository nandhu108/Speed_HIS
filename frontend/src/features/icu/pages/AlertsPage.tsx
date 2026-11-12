import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useAcknowledgeAlert, useIcuAlerts } from '../api'
import { ALERT_SEVERITY_LABELS, OPERATOR_LABELS, PARAMETER_LABELS, type IcuVitalsAlert } from '../types'

export function AlertsPage() {
  const { hasPermission } = useAuth()
  const canAcknowledge = hasPermission('ICU.ALERT.ACKNOWLEDGE')
  const navigate = useNavigate()
  const [onlyUnacknowledged, setOnlyUnacknowledged] = useState(true)
  const { data: alerts = [], isLoading } = useIcuAlerts(onlyUnacknowledged)
  const [acknowledging, setAcknowledging] = useState<IcuVitalsAlert | null>(null)

  return (
    <>
      <PageHeader
        title="Critical Alerts"
        subtitle="Vitals readings that breached an active threshold rule"
        actions={
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input type="checkbox" checked={onlyUnacknowledged} onChange={(e) => setOnlyUnacknowledged(e.target.checked)} />
            Only unacknowledged
          </label>
        }
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Raised</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Parameter</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Value</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Threshold</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Severity</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {alerts.map((a) => (
              <tr key={a.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-xs text-slate-500">{new Date(a.raisedAt).toLocaleString()}</td>
                <td
                  className="cursor-pointer px-4 py-2.5 text-primary-700 hover:underline"
                  onClick={() => navigate(`/ipd/admissions/${a.admissionId}`)}
                >
                  {a.patientName}
                </td>
                <td className="px-4 py-2.5 text-slate-700">{PARAMETER_LABELS[a.parameter]}</td>
                <td className="px-4 py-2.5 font-medium text-slate-800">{a.value}</td>
                <td className="px-4 py-2.5 text-slate-500">{OPERATOR_LABELS[a.operator]} {a.thresholdValue}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={a.severity === 2 ? 'danger' : 'warning'}>{ALERT_SEVERITY_LABELS[a.severity]}</StatusBadge>
                </td>
                <td className="px-4 py-2.5">
                  {a.isAcknowledged ? (
                    <StatusBadge tone="success">Acknowledged</StatusBadge>
                  ) : (
                    <StatusBadge tone="danger">Open</StatusBadge>
                  )}
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canAcknowledge && !a.isAcknowledged && (
                    <Button variant="secondary" onClick={() => setAcknowledging(a)}>
                      Acknowledge
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && alerts.length === 0 && (
              <tr>
                <td colSpan={8} className="px-4 py-10 text-center text-slate-400">
                  No alerts.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {acknowledging && <AcknowledgeModal alert={acknowledging} onClose={() => setAcknowledging(null)} />}
    </>
  )
}

function AcknowledgeModal({ alert, onClose }: { alert: IcuVitalsAlert; onClose: () => void }) {
  const acknowledge = useAcknowledgeAlert()
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await acknowledge.mutateAsync({ id: alert.id, notes: notes || null })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Acknowledge alert — ${alert.patientName}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={acknowledge.isPending}>Acknowledge</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <p className="text-sm text-slate-600">
          {PARAMETER_LABELS[alert.parameter]} was {alert.value} ({OPERATOR_LABELS[alert.operator]} {alert.thresholdValue}).
        </p>
        <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>
    </Modal>
  )
}
