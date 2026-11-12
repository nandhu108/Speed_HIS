import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useCreatePmSchedule, useGenerateDueWorkOrders, usePmSchedules } from '../api'

export function PmSchedulesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('BIOMED.PM.MANAGE')
  const [dueOnly, setDueOnly] = useState(false)
  const { data: schedules = [], isLoading } = usePmSchedules(dueOnly)
  const generate = useGenerateDueWorkOrders()
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  const runGenerate = async () => {
    setError(null)
    setMessage(null)
    try {
      const count = await generate.mutateAsync(undefined)
      setMessage(`${count} work order(s) generated.`)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader
        title="Preventive Maintenance Schedules"
        subtitle="Schedule templates that generate work orders when due"
        actions={
          canManage && (
            <div className="flex gap-2">
              <Button variant="secondary" onClick={runGenerate} loading={generate.isPending}>Generate due work orders</Button>
              <Button onClick={() => setCreating(true)}>New schedule</Button>
            </div>
          )
        }
      />

      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      {message && <p className="mb-3 rounded-md bg-green-50 px-3 py-2 text-sm text-green-700">{message}</p>}

      <label className="mb-3 flex items-center gap-2 text-sm text-slate-700">
        <input type="checkbox" checked={dueOnly} onChange={(e) => setDueOnly(e.target.checked)} />
        Only due
      </label>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Equipment</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Frequency (days)</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Next Due</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Last Generated</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            </tr>
          </thead>
          <tbody>
            {schedules.map((s) => (
              <tr key={s.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{s.equipmentName}</td>
                <td className="px-4 py-2.5 text-slate-700">{s.frequencyDays}</td>
                <td className="px-4 py-2.5 text-slate-700">{s.nextDueDate}</td>
                <td className="px-4 py-2.5 text-slate-700">{s.lastGeneratedDate ?? '—'}</td>
                <td className="px-4 py-2.5">{s.isDue && <StatusBadge tone="warning">Due</StatusBadge>}</td>
              </tr>
            ))}
            {!isLoading && schedules.length === 0 && (
              <tr><td colSpan={5} className="px-4 py-10 text-center text-slate-400">No PM schedules defined.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <NewScheduleModal onClose={() => setCreating(false)} />}
    </>
  )
}

function NewScheduleModal({ onClose }: { onClose: () => void }) {
  const create = useCreatePmSchedule()
  const [equipmentId, setEquipmentId] = useState('')
  const [frequencyDays, setFrequencyDays] = useState('90')
  const [nextDueDate, setNextDueDate] = useState('')
  const [checklistNotes, setChecklistNotes] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await create.mutateAsync({ equipmentId: Number(equipmentId), frequencyDays: Number(frequencyDays), nextDueDate, checklistNotes: checklistNotes || null })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="New PM schedule"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={create.isPending} disabled={!equipmentId || !nextDueDate}>Save</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Input label="Equipment ID" type="number" value={equipmentId} onChange={(e) => setEquipmentId(e.target.value)} />
        <Input label="Frequency (days)" type="number" value={frequencyDays} onChange={(e) => setFrequencyDays(e.target.value)} />
        <Input label="Next due date" type="date" value={nextDueDate} onChange={(e) => setNextDueDate(e.target.value)} />
        <Input label="Checklist notes" value={checklistNotes} onChange={(e) => setChecklistNotes(e.target.value)} />
      </div>
    </Modal>
  )
}
