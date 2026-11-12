import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { useAuth } from '../../auth/AuthContext'
import { useVendors } from '../../masters/api'
import { useBatches, useCreatePurchaseReturn, usePurchaseReturns, useStores } from '../api'
import { compactErrors, hasErrors, requiredSelect, requiredText, type FieldErrors } from '../../../lib/formValidation'

export function PurchaseReturnsPage() {
  const { hasPermission } = useAuth()
  const canCreate = hasPermission('PHARMACY.PURCHASERETURN.CREATE')
  const { data: returns = [], isLoading } = usePurchaseReturns()
  const [creating, setCreating] = useState(false)

  return (
    <>
      <PageHeader
        title="Purchase Returns"
        subtitle="Return stock to a vendor (damaged, expired, wrong item…)"
        actions={canCreate && <Button onClick={() => setCreating(true)}>New return</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Return No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Vendor</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Total</th>
            </tr>
          </thead>
          <tbody>
            {returns.map((r) => (
              <tr key={r.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{r.returnNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.returnDate}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.vendorName}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.totalAmount.toFixed(2)}</td>
              </tr>
            ))}
            {!isLoading && returns.length === 0 && (
              <tr>
                <td colSpan={4} className="px-4 py-10 text-center text-slate-400">
                  No purchase returns yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <CreateModal onClose={() => setCreating(false)} />}
    </>
  )
}

function CreateModal({ onClose }: { onClose: () => void }) {
  const create = useCreatePurchaseReturn()
  const { data: vendorsResult } = useVendors({ page: 1, pageSize: 200 })
  const { data: storesResult } = useStores()
  const [vendorId, setVendorId] = useState('')
  const [storeId, setStoreId] = useState('')
  const { data: batches = [] } = useBatches(undefined, undefined, storeId ? Number(storeId) : undefined, true)
  const [lines, setLines] = useState<{ batchId: string; qty: string; rate: string }[]>([{ batchId: '', qty: '', rate: '' }])
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const items = lines
      .filter((l) => l.batchId && l.qty && l.rate)
      .map((l) => ({ batchId: Number(l.batchId), quantity: Number(l.qty), rate: Number(l.rate) }))
    const errors = compactErrors({
      vendorId: requiredSelect(vendorId, 'Vendor'),
      reason: requiredText(reason, 'Reason'),
      items: items.length === 0 ? 'Add at least one complete line — each needs a batch, a quantity and a rate.' : undefined,
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await create.mutateAsync({ grnId: null, vendorId: Number(vendorId), reason, items })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="New purchase return"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={create.isPending}>
            Post return
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
        <div className="grid grid-cols-2 gap-3">
          <Select
            label="Vendor"
            required
            error={fieldErrors.vendorId}
            placeholder="Select…"
            value={vendorId}
            onChange={(e) => setVendorId(e.target.value)}
            options={(vendorsResult?.items ?? []).map((v) => ({ value: v.id, label: v.name }))}
          />
          <Select
            label="Store"
            placeholder="Select…"
            value={storeId}
            onChange={(e) => setStoreId(e.target.value)}
            options={(storesResult ?? []).map((s) => ({ value: s.id, label: s.name }))}
          />
        </div>
        {lines.map((line, idx) => (
          <div key={idx} className="grid grid-cols-3 gap-2">
            <Select
              label="Batch"
              placeholder="Select…"
              value={line.batchId}
              onChange={(e) => setLines(lines.map((l, i) => (i === idx ? { ...l, batchId: e.target.value } : l)))}
              options={batches.map((b) => ({ value: b.id, label: `${b.productName} — ${b.batchNumber} (${b.quantityOnHand} on hand)` }))}
            />
            <Input label="Quantity" type="number" value={line.qty} onChange={(e) => setLines(lines.map((l, i) => (i === idx ? { ...l, qty: e.target.value } : l)))} />
            <Input label="Rate" type="number" value={line.rate} onChange={(e) => setLines(lines.map((l, i) => (i === idx ? { ...l, rate: e.target.value } : l)))} />
          </div>
        ))}
        <Button variant="secondary" onClick={() => setLines([...lines, { batchId: '', qty: '', rate: '' }])}>
          Add line
        </Button>
        <Input
          label="Reason"
          required
          error={fieldErrors.reason}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
        />
      </div>
    </Modal>
  )
}
