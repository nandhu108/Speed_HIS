import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useUsers } from '../../admin/api'
import { useAuth } from '../../auth/AuthContext'
import { useCapas, useCloseCapa, useCompleteCapa, useRaiseCapa, useVerifyCapa } from '../api'
import { CAPA_SOURCE_LABELS, CAPA_STATUS_LABELS, type CapaSourceType, type CapaStatus } from '../types'

const statusTone: Record<CapaStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'warning', 2: 'info', 3: 'info', 4: 'success',
}

export function CapaTrackerPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('QUALITY.CAPA.MANAGE')
  const { data: capas = [], isLoading } = useCapas({})
  const [raising, setRaising] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const complete = useCompleteCapa()
  const verify = useVerifyCapa()
  const close = useCloseCapa()

  const run = async (fn: () => Promise<unknown>) => {
    setError(null)
    try { await fn() } catch (err) { setError(errorMessage(err)) }
  }

  return (
    <>
      <PageHeader title="CAPA Tracker" subtitle="Corrective and Preventive Actions — action, owner, due date, verification" actions={canManage && <Button onClick={() => setRaising(true)}>Raise CAPA</Button>} />
      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Source</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Action plan</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Owner</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Due</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              {canManage && <th className="px-4 py-2.5 font-medium text-slate-600">Actions</th>}
            </tr>
          </thead>
          <tbody>
            {capas.map((c) => (
              <tr key={c.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{CAPA_SOURCE_LABELS[c.sourceType]} #{c.sourceId}</td>
                <td className="px-4 py-2.5 text-slate-700">{c.actionPlan}</td>
                <td className="px-4 py-2.5 text-slate-700">{c.ownerName}</td>
                <td className="px-4 py-2.5 text-slate-700">{c.dueDate}</td>
                <td className="px-4 py-2.5"><StatusBadge tone={statusTone[c.status]}>{CAPA_STATUS_LABELS[c.status]}</StatusBadge></td>
                {canManage && (
                  <td className="px-4 py-2.5">
                    <div className="flex gap-1">
                      {c.status === 1 && <Button variant="ghost" onClick={() => run(() => complete.mutateAsync(c.id))}>Complete</Button>}
                      {c.status === 2 && <Button variant="ghost" onClick={() => run(() => verify.mutateAsync({ id: c.id, verificationNotes: null }))}>Verify</Button>}
                      {c.status === 3 && <Button variant="ghost" onClick={() => run(() => close.mutateAsync(c.id))}>Close</Button>}
                    </div>
                  </td>
                )}
              </tr>
            ))}
            {!isLoading && capas.length === 0 && (
              <tr><td colSpan={canManage ? 6 : 5} className="px-4 py-10 text-center text-slate-400">No CAPA actions raised.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {raising && <RaiseCapaModal onClose={() => setRaising(false)} />}
    </>
  )
}

function RaiseCapaModal({ onClose }: { onClose: () => void }) {
  const raise = useRaiseCapa()
  const { data: users } = useUsers({ page: 1, pageSize: 500 })
  const [sourceType, setSourceType] = useState<CapaSourceType>(1)
  const [sourceId, setSourceId] = useState('')
  const [actionPlan, setActionPlan] = useState('')
  const [ownerUserId, setOwnerUserId] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    if (!sourceId || !actionPlan.trim() || !ownerUserId || !dueDate) { setError('All fields are required.'); return }
    try {
      await raise.mutateAsync({ sourceType, sourceId: Number(sourceId), actionPlan, ownerUserId: Number(ownerUserId), dueDate })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal open title="Raise CAPA" onClose={onClose} footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={submit} loading={raise.isPending}>Raise</Button></>}>
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <div className="grid grid-cols-2 gap-2">
          <Select label="Source type" value={sourceType} onChange={(e) => setSourceType(Number(e.target.value) as CapaSourceType)} options={Object.entries(CAPA_SOURCE_LABELS).map(([value, label]) => ({ value, label }))} />
          <Input label="Source ID" type="number" value={sourceId} onChange={(e) => setSourceId(e.target.value)} />
        </div>
        <Input label="Action plan" value={actionPlan} onChange={(e) => setActionPlan(e.target.value)} />
        <Select label="Owner" placeholder="Select owner" value={ownerUserId} onChange={(e) => setOwnerUserId(e.target.value)} options={(users?.items ?? []).map((u) => ({ value: u.id, label: u.fullName }))} />
        <Input label="Due date" type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
      </div>
    </Modal>
  )
}
