import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { useRun } from '../../../lib/useRun'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDrugs, useItems, useVendors } from '../../masters/api'
import { ProductPicker } from '../components/ProductPicker'
import { useApprovePurchaseOrder, useCreatePurchaseOrder, usePurchaseOrders } from '../api'
import { PURCHASE_ORDER_STATUS_LABELS, type PurchaseOrderListItem, type PurchaseOrderStatus } from '../types'
import { compactErrors, hasErrors, requiredSelect, type FieldErrors } from '../../../lib/formValidation'

const statusTone: Record<PurchaseOrderStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'neutral', 2: 'info', 3: 'warning', 4: 'success', 5: 'danger',
}

export function PurchaseOrdersPage() {
  const run = useRun()
  const navigate = useNavigate()
  const { hasPermission } = useAuth()
  const [page, setPage] = useState(1)
  const [creating, setCreating] = useState(false)
  const { data, isLoading } = usePurchaseOrders({ page, pageSize: 20 })
  const approve = useApprovePurchaseOrder()

  const canCreate = hasPermission('PHARMACY.PURCHASEORDER.CREATE')
  const canApprove = hasPermission('PHARMACY.PURCHASEORDER.APPROVE')
  const canReceive = hasPermission('PHARMACY.GRN.CREATE')

  const columns: Column<PurchaseOrderListItem>[] = [
    { header: 'PO No', cell: (row) => <span className="font-mono text-xs">{row.poNumber}</span> },
    { header: 'Date', cell: (row) => row.orderDate },
    { header: 'Vendor', cell: (row) => row.vendorName },
    { header: 'Total', className: 'text-right', cell: (row) => row.totalAmount.toFixed(2) },
    {
      header: 'Status',
      cell: (row) => <StatusBadge tone={statusTone[row.status]}>{PURCHASE_ORDER_STATUS_LABELS[row.status]}</StatusBadge>,
    },
    {
      header: '',
      className: 'text-right',
      cell: (row) => (
        <div className="flex justify-end gap-1">
          {canApprove && row.status === 1 && (
            <Button variant="secondary" onClick={() => run(() => approve.mutateAsync(row.id))}>
              Approve
            </Button>
          )}
          {canReceive && (row.status === 2 || row.status === 3) && (
            <Button
              variant="secondary"
              onClick={() => navigate('/pharmacy/grn', { state: { grn: { purchaseOrderId: row.id } } })}
            >
              Receive (GRN)
            </Button>
          )}
        </div>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title="Purchase Orders"
        subtitle="Orders raised against a vendor, received into stock via GRN"
        actions={canCreate && <Button onClick={() => setCreating(true)}>New order</Button>}
      />

      <DataTable columns={columns} result={data} isLoading={isLoading} page={page} onPageChange={setPage} rowKey={(row) => row.id} />

      {creating && <CreateModal onClose={() => setCreating(false)} />}
    </>
  )
}

function CreateModal({ onClose }: { onClose: () => void }) {
  const create = useCreatePurchaseOrder()
  const { data: vendorsResult } = useVendors({ page: 1, pageSize: 200 })
  const { data: drugsResult } = useDrugs({ page: 1, pageSize: 500 })
  const { data: itemsResult } = useItems({ page: 1, pageSize: 500 })
  const [vendorId, setVendorId] = useState('')
  const [lines, setLines] = useState<{ drugId: string; itemId: string; qty: string; rate: string; tax: string }[]>([
    { drugId: '', itemId: '', qty: '', rate: '', tax: '0' },
  ])
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const items = lines
      .filter((l) => (l.drugId || l.itemId) && l.qty && l.rate)
      .map((l) => ({
        drugId: l.drugId ? Number(l.drugId) : null,
        itemId: l.itemId ? Number(l.itemId) : null,
        orderedQuantity: Number(l.qty),
        rate: Number(l.rate),
        taxPercent: Number(l.tax || 0),
      }))
    const errors = compactErrors({
      vendorId: requiredSelect(vendorId, 'Vendor'),
      items: items.length === 0 ? 'Add at least one complete line — each needs a drug or item, a quantity and a rate.' : undefined,
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await create.mutateAsync({ vendorId: Number(vendorId), purchaseRequestId: null, notes: notes || null, items })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="New purchase order"
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
      <div className="max-h-[60vh] space-y-3 overflow-y-auto">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        {fieldErrors.items && (
          <p role="alert" className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">
            {fieldErrors.items}
          </p>
        )}
        <Select
          label="Vendor"
          required
          error={fieldErrors.vendorId}
          placeholder="Select…"
          value={vendorId}
          onChange={(e) => setVendorId(e.target.value)}
          options={(vendorsResult?.items ?? []).map((v) => ({ value: v.id, label: v.name }))}
        />
        {lines.map((line, idx) => (
          <div key={idx} className="space-y-2 rounded-md border border-slate-200 p-3">
            <ProductPicker
              value={line}
              onChange={(v) => setLines(lines.map((l, i) => (i === idx ? { ...l, ...v } : l)))}
              drugs={drugsResult?.items ?? []}
              items={itemsResult?.items ?? []}
            />
            <div className="grid grid-cols-3 gap-2">
              <Input label="Qty" type="number" value={line.qty} onChange={(e) => setLines(lines.map((l, i) => (i === idx ? { ...l, qty: e.target.value } : l)))} />
              <Input label="Rate" type="number" value={line.rate} onChange={(e) => setLines(lines.map((l, i) => (i === idx ? { ...l, rate: e.target.value } : l)))} />
              <Input label="Tax %" type="number" value={line.tax} onChange={(e) => setLines(lines.map((l, i) => (i === idx ? { ...l, tax: e.target.value } : l)))} />
            </div>
          </div>
        ))}
        <Button variant="secondary" onClick={() => setLines([...lines, { drugId: '', itemId: '', qty: '', rate: '', tax: '0' }])}>
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
