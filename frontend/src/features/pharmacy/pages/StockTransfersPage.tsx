import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useBranches } from '../../admin/api'
import { useBatches, useCreateTransfer, useDispatchTransfer, useReceiveTransfer, useStores, useTransfers } from '../api'
import { TRANSFER_STATUS_LABELS, type StockTransferStatus } from '../types'
import { compactErrors, hasErrors, requiredSelect, type FieldErrors } from '../../../lib/formValidation'

const statusTone: Record<StockTransferStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'neutral', 2: 'warning', 3: 'success', 4: 'danger',
}

export function StockTransfersPage() {
  const { hasPermission } = useAuth()
  const canTransfer = hasPermission('PHARMACY.STOCK.TRANSFER')
  const canReceive = hasPermission('PHARMACY.STOCK.RECEIVE')
  const { data: transfers = [], isLoading } = useTransfers()
  const dispatch = useDispatchTransfer()
  const receive = useReceiveTransfer()
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  return (
    <>
      <PageHeader
        title="Stock Transfers"
        subtitle="Move stock between stores (same branch) or branches"
        actions={canTransfer && <Button onClick={() => setCreating(true)}>New transfer</Button>}
      />

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Transfer No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">From</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">To</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {transfers.map((t) => (
              <tr key={t.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{t.transferNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{t.transferDate}</td>
                <td className="px-4 py-2.5 text-slate-700">{t.fromStoreName}</td>
                <td className="px-4 py-2.5 text-slate-700">{t.toStoreName}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={statusTone[t.status]}>{TRANSFER_STATUS_LABELS[t.status]}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canTransfer && t.status === 1 && (
                    <Button
                      variant="secondary"
                      onClick={async () => {
                        try {
                          await dispatch.mutateAsync(t.id)
                        } catch (err) {
                          setError(errorMessage(err))
                        }
                      }}
                    >
                      Dispatch
                    </Button>
                  )}
                  {canReceive && t.status === 2 && (
                    <Button
                      variant="secondary"
                      onClick={async () => {
                        try {
                          await receive.mutateAsync(t.id)
                        } catch (err) {
                          setError(errorMessage(err))
                        }
                      }}
                    >
                      Receive
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && transfers.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  No transfers yet.
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
  const create = useCreateTransfer()
  const { data: storesResult } = useStores()
  const { data: branches = [] } = useBranches()
  const [fromStoreId, setFromStoreId] = useState('')
  const [toBranchId, setToBranchId] = useState('')
  const [toStoreId, setToStoreId] = useState('')
  const { data: batches = [] } = useBatches(undefined, undefined, fromStoreId ? Number(fromStoreId) : undefined, true)
  const [lines, setLines] = useState<{ batchId: string; qty: string }[]>([{ batchId: '', qty: '' }])
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const items = lines.filter((l) => l.batchId && l.qty).map((l) => ({ batchId: Number(l.batchId), quantity: Number(l.qty) }))
    const errors = compactErrors({
      fromStoreId: requiredSelect(fromStoreId, 'From store'),
      toBranchId: requiredSelect(toBranchId, 'To branch'),
      toStoreId: requiredSelect(toStoreId, 'To store'),
      items: items.length === 0 ? 'Add at least one complete line — each needs a batch and a quantity.' : undefined,
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await create.mutateAsync({ fromStoreId: Number(fromStoreId), toBranchId: Number(toBranchId), toStoreId: Number(toStoreId), items })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="New stock transfer"
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
          label="From store"
          required
          error={fieldErrors.fromStoreId}
          placeholder="Select…"
          value={fromStoreId}
          onChange={(e) => setFromStoreId(e.target.value)}
          options={(storesResult ?? []).map((s) => ({ value: s.id, label: s.name }))}
        />
        <div className="grid grid-cols-2 gap-3">
          <Select
            label="To branch"
            required
            error={fieldErrors.toBranchId}
            placeholder="Select…"
            value={toBranchId}
            onChange={(e) => setToBranchId(e.target.value)}
            options={branches.map((b) => ({ value: b.id, label: b.name }))}
          />
          <Select
            label="To store"
            required
            error={fieldErrors.toStoreId}
            placeholder="Select…"
            value={toStoreId}
            onChange={(e) => setToStoreId(e.target.value)}
            options={(storesResult ?? []).filter((s) => String(s.id) !== fromStoreId).map((s) => ({ value: s.id, label: s.name }))}
          />
        </div>
        {lines.map((line, idx) => (
          <div key={idx} className="flex items-end gap-2">
            <Select
              label="Batch"
              className="flex-1"
              placeholder="Select…"
              value={line.batchId}
              onChange={(e) => setLines(lines.map((l, i) => (i === idx ? { ...l, batchId: e.target.value } : l)))}
              options={batches.map((b) => ({ value: b.id, label: `${b.productName} — ${b.batchNumber} (${b.quantityOnHand} on hand)` }))}
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
        <Button variant="secondary" onClick={() => setLines([...lines, { batchId: '', qty: '' }])}>
          Add line
        </Button>
      </div>
    </Modal>
  )
}
