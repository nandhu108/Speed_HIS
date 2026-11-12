import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useUsers } from '../../admin/api'
import { useAssignWorkOrder, useCancelWorkOrder, useCompleteWorkOrder, useRecordSpareUsage, useWorkOrders } from '../api'
import { WORK_ORDER_STATUS_LABELS, WORK_ORDER_TYPE_LABELS, type WorkOrderDto } from '../types'

export function WorkOrdersPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('BIOMED.WORKORDER.MANAGE')
  const { data: workOrders = [], isLoading } = useWorkOrders({})
  const [selected, setSelected] = useState<WorkOrderDto | null>(null)
  const [error, setError] = useState<string | null>(null)

  const assign = useAssignWorkOrder()
  const complete = useCompleteWorkOrder()
  const cancel = useCancelWorkOrder()
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
      <PageHeader title="Work Orders" subtitle="Preventive maintenance and breakdown repair work" />
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">WO No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Equipment</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Scheduled</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Assigned</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              {canManage && <th className="px-4 py-2.5 font-medium text-slate-600">Actions</th>}
            </tr>
          </thead>
          <tbody>
            {workOrders.map((w) => (
              <tr key={w.id} className="border-b border-slate-100 last:border-0 hover:bg-slate-50">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{w.workOrderNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{w.equipmentName}</td>
                <td className="px-4 py-2.5 text-slate-700">{WORK_ORDER_TYPE_LABELS[w.workOrderType]}</td>
                <td className="px-4 py-2.5 text-slate-700">{w.scheduledDate}</td>
                <td className="px-4 py-2.5 text-slate-700">{w.assignedToName ?? '—'}</td>
                <td className="px-4 py-2.5"><StatusBadge tone={w.status === 3 ? 'success' : w.status === 4 ? 'neutral' : 'warning'}>{WORK_ORDER_STATUS_LABELS[w.status]}</StatusBadge></td>
                {canManage && (
                  <td className="px-4 py-2.5">
                    {(w.status === 1 || w.status === 2) && <Button variant="secondary" onClick={() => setSelected(w)}>Manage</Button>}
                  </td>
                )}
              </tr>
            ))}
            {!isLoading && workOrders.length === 0 && (
              <tr><td colSpan={canManage ? 7 : 6} className="px-4 py-10 text-center text-slate-400">No work orders.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {selected && (
        <ManageWorkOrderModal
          workOrder={selected}
          users={users?.items ?? []}
          onClose={() => setSelected(null)}
          onAssign={(assignedToUserId) => run(() => assign.mutateAsync({ id: selected.id, assignedToUserId }))}
          onComplete={(notes) => run(() => complete.mutateAsync({ id: selected.id, notes }))}
          onCancel={(reason) => run(() => cancel.mutateAsync({ id: selected.id, reason }))}
        />
      )}
    </>
  )
}

function ManageWorkOrderModal({
  workOrder, users, onClose, onAssign, onComplete, onCancel,
}: {
  workOrder: WorkOrderDto
  users: { id: number; fullName: string }[]
  onClose: () => void
  onAssign: (userId: number) => void
  onComplete: (notes: string | null) => void
  onCancel: (reason: string) => void
}) {
  const recordSpare = useRecordSpareUsage(workOrder.id)
  const [assignee, setAssignee] = useState('')
  const [notes, setNotes] = useState('')
  const [reason, setReason] = useState('')
  const [batchId, setBatchId] = useState('')
  const [quantity, setQuantity] = useState('')
  const [error, setError] = useState<string | null>(null)

  const addSpare = async () => {
    setError(null)
    try {
      await recordSpare.mutateAsync({ batchId: Number(batchId), quantity: Number(quantity) })
      setBatchId('')
      setQuantity('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal open title={`Manage ${workOrder.workOrderNumber}`} onClose={onClose} footer={<Button variant="secondary" onClick={onClose}>Close</Button>}>
      <div className="space-y-4">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

        <div>
          <h4 className="mb-1 text-xs font-semibold uppercase text-slate-500">Spare usage</h4>
          <table className="mb-2 w-full text-xs">
            <tbody>
              {workOrder.spareUsages.map((s) => (
                <tr key={s.id}><td className="py-0.5">{s.productName}</td><td className="py-0.5 text-right">{s.quantity}</td></tr>
              ))}
            </tbody>
          </table>
          <div className="flex items-end gap-2">
            <Input label="Batch ID" type="number" value={batchId} onChange={(e) => setBatchId(e.target.value)} />
            <Input label="Quantity" type="number" value={quantity} onChange={(e) => setQuantity(e.target.value)} />
            <Button variant="secondary" onClick={addSpare} loading={recordSpare.isPending} disabled={!batchId || !quantity}>Add</Button>
          </div>
        </div>

        <div className="flex items-end gap-2 border-t border-slate-100 pt-3">
          <Select label="Assign to" value={assignee} onChange={(e) => setAssignee(e.target.value)} placeholder="Select user" options={users.map((u) => ({ value: u.id, label: u.fullName }))} />
          <Button variant="secondary" onClick={() => assignee && onAssign(Number(assignee))} disabled={!assignee}>Assign</Button>
        </div>

        <div className="flex items-end gap-2 border-t border-slate-100 pt-3">
          <Input label="Completion notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
          <Button onClick={() => onComplete(notes || null)}>Complete</Button>
        </div>

        <div className="flex items-end gap-2 border-t border-slate-100 pt-3">
          <Input label="Cancel reason" value={reason} onChange={(e) => setReason(e.target.value)} />
          <Button variant="ghost" className="text-red-600" onClick={() => reason && onCancel(reason)} disabled={!reason}>Cancel work order</Button>
        </div>
      </div>
    </Modal>
  )
}
