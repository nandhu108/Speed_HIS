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
import { useAssignComplaint, useCloseComplaint, useComplaints, useCreateWorkOrderFromComplaint, useRaiseComplaint, useResolveComplaint } from '../api'
import { COMPLAINT_STATUS_LABELS, type BreakdownComplaintDto } from '../types'

const statusTone: Record<number, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'danger', 2: 'warning', 3: 'warning', 4: 'success', 5: 'neutral',
}

export function ComplaintsPage() {
  const { hasPermission } = useAuth()
  const canCreate = hasPermission('BIOMED.COMPLAINT.CREATE')
  const canManage = hasPermission('BIOMED.COMPLAINT.MANAGE')
  const { data: complaints = [], isLoading } = useComplaints({})
  const [raising, setRaising] = useState(false)
  const [selected, setSelected] = useState<BreakdownComplaintDto | null>(null)
  const [error, setError] = useState<string | null>(null)

  const assign = useAssignComplaint()
  const resolve = useResolveComplaint()
  const close = useCloseComplaint()
  const createWorkOrder = useCreateWorkOrderFromComplaint()
  const { data: users } = useUsers({ page: 1, pageSize: 500 })

  const run = async (fn: () => Promise<unknown>) => {
    setError(null)
    try {
      await fn()
      setSelected(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader title="Breakdown Complaints" subtitle="Raise → assign → resolve, with downtime capture" actions={canCreate && <Button onClick={() => setRaising(true)}>Raise complaint</Button>} />
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Complaint No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Equipment</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Description</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Reported</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Downtime</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              {canManage && <th className="px-4 py-2.5 font-medium text-slate-600">Actions</th>}
            </tr>
          </thead>
          <tbody>
            {complaints.map((c) => (
              <tr key={c.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{c.complaintNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{c.equipmentName}</td>
                <td className="px-4 py-2.5 text-slate-700">{c.description}</td>
                <td className="px-4 py-2.5 text-slate-700">{new Date(c.reportedAt).toLocaleString()}</td>
                <td className="px-4 py-2.5 text-slate-700">{c.downtimeMinutes !== null ? `${c.downtimeMinutes} min` : '—'}</td>
                <td className="px-4 py-2.5"><StatusBadge tone={statusTone[c.status]}>{COMPLAINT_STATUS_LABELS[c.status]}</StatusBadge></td>
                {canManage && (
                  <td className="px-4 py-2.5">
                    {c.status !== 5 && <Button variant="secondary" onClick={() => setSelected(c)}>Manage</Button>}
                  </td>
                )}
              </tr>
            ))}
            {!isLoading && complaints.length === 0 && (
              <tr><td colSpan={canManage ? 7 : 6} className="px-4 py-10 text-center text-slate-400">No complaints raised.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {raising && <RaiseModal onClose={() => setRaising(false)} />}
      {selected && (
        <ManageComplaintModal
          complaint={selected}
          users={users?.items ?? []}
          onClose={() => setSelected(null)}
          onAssign={(userId) => run(() => assign.mutateAsync({ id: selected.id, assignedToUserId: userId }))}
          onResolve={(resolution) => run(() => resolve.mutateAsync({ id: selected.id, resolution }))}
          onClose2={() => run(() => close.mutateAsync(selected.id))}
          onCreateWorkOrder={(scheduledDate) => run(() => createWorkOrder.mutateAsync({ breakdownComplaintId: selected.id, scheduledDate, notes: null }))}
        />
      )}
    </>
  )
}

function RaiseModal({ onClose }: { onClose: () => void }) {
  const raise = useRaiseComplaint()
  const [equipmentId, setEquipmentId] = useState('')
  const [description, setDescription] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await raise.mutateAsync({ equipmentId: Number(equipmentId), description })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Raise breakdown complaint"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={raise.isPending} disabled={!equipmentId || !description}>Raise</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Input label="Equipment ID" type="number" value={equipmentId} onChange={(e) => setEquipmentId(e.target.value)} />
        <Input label="Description" value={description} onChange={(e) => setDescription(e.target.value)} />
      </div>
    </Modal>
  )
}

function ManageComplaintModal({
  complaint, users, onClose, onAssign, onResolve, onClose2, onCreateWorkOrder,
}: {
  complaint: BreakdownComplaintDto
  users: { id: number; fullName: string }[]
  onClose: () => void
  onAssign: (userId: number) => void
  onResolve: (resolution: string) => void
  onClose2: () => void
  onCreateWorkOrder: (scheduledDate: string) => void
}) {
  const [assignee, setAssignee] = useState('')
  const [resolution, setResolution] = useState('')
  const [scheduledDate, setScheduledDate] = useState(new Date().toISOString().slice(0, 10))

  return (
    <Modal open title={`Manage ${complaint.complaintNumber}`} onClose={onClose} footer={<Button variant="secondary" onClick={onClose}>Close</Button>}>
      <div className="space-y-4">
        {complaint.status === 1 && (
          <div className="flex items-end gap-2">
            <Select label="Assign to" value={assignee} onChange={(e) => setAssignee(e.target.value)} placeholder="Select user" options={users.map((u) => ({ value: u.id, label: u.fullName }))} />
            <Button variant="secondary" onClick={() => assignee && onAssign(Number(assignee))} disabled={!assignee}>Assign</Button>
          </div>
        )}
        {complaint.status !== 4 && complaint.status !== 5 && (
          <div className="flex items-end gap-2 border-t border-slate-100 pt-3">
            <Input label="Resolution" value={resolution} onChange={(e) => setResolution(e.target.value)} />
            <Button onClick={() => resolution && onResolve(resolution)} disabled={!resolution}>Resolve</Button>
          </div>
        )}
        {complaint.status === 4 && (
          <Button variant="secondary" onClick={onClose2}>Close complaint</Button>
        )}
        <div className="flex items-end gap-2 border-t border-slate-100 pt-3">
          <Input label="Work order scheduled date" type="date" value={scheduledDate} onChange={(e) => setScheduledDate(e.target.value)} />
          <Button variant="secondary" onClick={() => onCreateWorkOrder(scheduledDate)}>Raise work order</Button>
        </div>
      </div>
    </Modal>
  )
}
