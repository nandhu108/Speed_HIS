import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useAuth } from '../../auth/AuthContext'
import { useScheduleItem, useSchedulingWorklist } from '../api'
import { PRIORITY_LABELS, type SchedulingWorklistItem } from '../types'

export function SchedulingWorklistPage() {
  const { hasPermission } = useAuth()
  const canSchedule = hasPermission('RAD.SCHEDULE.MANAGE')
  const { data: worklist = [], isLoading } = useSchedulingWorklist()
  const [scheduling, setScheduling] = useState<SchedulingWorklistItem | null>(null)
  const [error, setError] = useState<string | null>(null)

  return (
    <>
      <PageHeader title="Scheduling Worklist" subtitle="Schedule an appointment slot per modality for ordered tests" />

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Order No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Test</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Modality</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Priority</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {worklist.map((item) => (
              <tr key={item.radOrderItemId} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{item.orderNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">
                  {item.patientName} ({item.patientUhid})
                </td>
                <td className="px-4 py-2.5 text-slate-700">
                  {item.radiologyTestName}
                  {item.requiresContrast && <span className="ml-1 text-xs text-amber-600">(contrast)</span>}
                </td>
                <td className="px-4 py-2.5 text-slate-700">{item.modalityName}</td>
                <td className="px-4 py-2.5 text-slate-700">{PRIORITY_LABELS[item.priority]}</td>
                <td className="px-4 py-2.5 text-right">
                  {canSchedule && (
                    <Button variant="secondary" onClick={() => setScheduling(item)}>
                      Schedule
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && worklist.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  Nothing awaiting scheduling.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {scheduling && <ScheduleModal item={scheduling} onClose={() => setScheduling(null)} onError={setError} />}
    </>
  )
}

function ScheduleModal({
  item,
  onClose,
  onError,
}: {
  item: SchedulingWorklistItem
  onClose: () => void
  onError: (msg: string) => void
}) {
  const schedule = useScheduleItem()
  const [scheduledAt, setScheduledAt] = useState('')
  const [notes, setNotes] = useState('')

  const submit = async () => {
    if (!scheduledAt) return
    try {
      await schedule.mutateAsync({ itemId: item.radOrderItemId, scheduledAt: new Date(scheduledAt).toISOString(), notes: notes || null })
      onClose()
    } catch (err) {
      onError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Schedule — ${item.radiologyTestName}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={schedule.isPending} disabled={!scheduledAt}>
            Schedule
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        <p className="text-sm text-slate-600">
          {item.patientName} ({item.patientUhid}) — {item.modalityName}
        </p>
        <Input label="Scheduled at" type="datetime-local" value={scheduledAt} onChange={(e) => setScheduledAt(e.target.value)} />
        <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>
    </Modal>
  )
}
