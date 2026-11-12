import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { useRun } from '../../../lib/useRun'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDrugs, useItems } from '../../masters/api'
import { ProductPicker } from '../components/ProductPicker'
import { useCreatePurchaseRequest, useDecidePurchaseRequest, usePurchaseRequests } from '../api'
import { PURCHASE_REQUEST_STATUS_LABELS, type PurchaseRequestListItem, type PurchaseRequestStatus } from '../types'
import { compactErrors, hasErrors, type FieldErrors } from '../../../lib/formValidation'

const statusTone: Record<PurchaseRequestStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'neutral', 2: 'info', 3: 'success', 4: 'danger', 5: 'neutral',
}

export function PurchaseRequestsPage() {
  const run = useRun()
  const { hasPermission } = useAuth()
  const [page, setPage] = useState(1)
  const [creating, setCreating] = useState(false)
  const { data, isLoading } = usePurchaseRequests({ page, pageSize: 20 })
  const decide = useDecidePurchaseRequest()

  const canCreate = hasPermission('PHARMACY.PURCHASEREQUEST.CREATE')
  const canApprove = hasPermission('PHARMACY.PURCHASEREQUEST.APPROVE')

  const columns: Column<PurchaseRequestListItem>[] = [
    { header: 'Request No', cell: (row) => <span className="font-mono text-xs">{row.requestNumber}</span> },
    { header: 'Date', cell: (row) => row.requestDate },
    { header: 'Items', className: 'text-right', cell: (row) => row.itemCount },
    {
      header: 'Status',
      cell: (row) => <StatusBadge tone={statusTone[row.status]}>{PURCHASE_REQUEST_STATUS_LABELS[row.status]}</StatusBadge>,
    },
    {
      header: '',
      className: 'text-right',
      cell: (row) =>
        canApprove && (row.status === 1 || row.status === 2) ? (
          <div className="flex justify-end gap-1">
            <Button variant="secondary" onClick={() => run(() => decide.mutateAsync({ id: row.id, approve: true }))}>
              Approve
            </Button>
            <Button variant="ghost" className="text-red-600" onClick={() => run(() => decide.mutateAsync({ id: row.id, approve: false }))}>
              Reject
            </Button>
          </div>
        ) : null,
    },
  ]

  return (
    <>
      <PageHeader
        title="Purchase Requests"
        subtitle="Internal request to purchase drugs, before a purchase order is raised"
        actions={canCreate && <Button onClick={() => setCreating(true)}>New request</Button>}
      />

      <DataTable columns={columns} result={data} isLoading={isLoading} page={page} onPageChange={setPage} rowKey={(row) => row.id} />

      {creating && <CreateModal onClose={() => setCreating(false)} />}
    </>
  )
}

function CreateModal({ onClose }: { onClose: () => void }) {
  const create = useCreatePurchaseRequest()
  const { data: drugsResult } = useDrugs({ page: 1, pageSize: 500 })
  const { data: itemsResult } = useItems({ page: 1, pageSize: 500 })
  const [lines, setLines] = useState<{ drugId: string; itemId: string; qty: string }[]>([{ drugId: '', itemId: '', qty: '' }])
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const items = lines
      .filter((l) => (l.drugId || l.itemId) && l.qty)
      .map((l) => ({
        drugId: l.drugId ? Number(l.drugId) : null,
        itemId: l.itemId ? Number(l.itemId) : null,
        requestedQuantity: Number(l.qty),
      }))
    const errors = compactErrors({
      items: items.length === 0 ? 'Add at least one complete line — each needs a drug or item and a quantity.' : undefined,
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await create.mutateAsync({ notes: notes || null, items })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="New purchase request"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={create.isPending}>
            Create
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        {fieldErrors.items && (
          <p role="alert" className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">
            {fieldErrors.items}
          </p>
        )}
        {lines.map((line, idx) => (
          <div key={idx} className="flex items-end gap-2">
            <ProductPicker
              className="flex-1"
              value={line}
              onChange={(v) => setLines(lines.map((l, i) => (i === idx ? { ...l, ...v } : l)))}
              drugs={drugsResult?.items ?? []}
              items={itemsResult?.items ?? []}
            />
            <input
              type="number"
              placeholder="Qty"
              className="w-24 rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
              value={line.qty}
              onChange={(e) => setLines(lines.map((l, i) => (i === idx ? { ...l, qty: e.target.value } : l)))}
            />
          </div>
        ))}
        <Button variant="secondary" onClick={() => setLines([...lines, { drugId: '', itemId: '', qty: '' }])}>
          Add line
        </Button>
        <label className="block text-sm font-medium text-slate-700">
          Notes
          <textarea
            className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
            rows={2}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
          />
        </label>
      </div>
    </Modal>
  )
}
