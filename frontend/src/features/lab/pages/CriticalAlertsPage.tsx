import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useAcknowledgeAlert, usePendingCriticalAlerts } from '../api'
import type { CriticalAlert } from '../types'

export function CriticalAlertsPage() {
  const { data: alerts = [], isLoading } = usePendingCriticalAlerts()
  const [acknowledging, setAcknowledging] = useState<CriticalAlert | null>(null)

  return (
    <>
      <PageHeader title="Critical Alerts" subtitle="Critical lab values awaiting acknowledgement" />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Order No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Parameter</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Value</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Raised</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {alerts.map((a) => (
              <tr key={a.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{a.orderNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.patientName}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.parameterName}</td>
                <td className="px-4 py-2.5 text-right font-medium text-red-600">{a.value}</td>
                <td className="px-4 py-2.5 text-slate-700">{new Date(a.raisedAt).toLocaleString()}</td>
                <td className="px-4 py-2.5 text-right">
                  <Button variant="secondary" onClick={() => setAcknowledging(a)}>
                    Acknowledge
                  </Button>
                </td>
              </tr>
            ))}
            {!isLoading && alerts.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  Nothing pending.
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

function AcknowledgeModal({ alert, onClose }: { alert: CriticalAlert; onClose: () => void }) {
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
      title="Acknowledge critical alert"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={acknowledge.isPending}>
            Acknowledge
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <p className="mb-3 text-sm text-slate-600">
        {alert.orderNumber} · {alert.patientName} · {alert.parameterName}: <strong className="text-red-600">{alert.value}</strong>
      </p>
      <Input label="Notes (optional)" value={notes} onChange={(e) => setNotes(e.target.value)} />
    </Modal>
  )
}
