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
import { useBatches, useCreateAdjustment, useDecideAdjustment, usePendingAdjustments, useStores } from '../api'
import { ADJUSTMENT_STATUS_LABELS, type StockAdjustmentListItem, type StockAdjustmentStatus } from '../types'
import { compactErrors, hasErrors, requiredSelect, requiredText, type FieldErrors } from '../../../lib/formValidation'

const statusTone: Record<StockAdjustmentStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'warning', 2: 'success', 3: 'danger',
}

export function StockAdjustmentsPage() {
  const { hasPermission } = useAuth()
  const canRequest = hasPermission('PHARMACY.STOCK.ADJUST')
  const canApprove = hasPermission('PHARMACY.STOCK.APPROVE')
  const { data: adjustments = [], isLoading } = usePendingAdjustments()
  const decide = useDecideAdjustment()
  const [creating, setCreating] = useState(false)
  const [rejecting, setRejecting] = useState<StockAdjustmentListItem | null>(null)
  const [error, setError] = useState<string | null>(null)

  return (
    <>
      <PageHeader
        title="Stock Adjustments"
        subtitle="Reason-coded stock corrections — requires approval before stock is touched"
        actions={canRequest && <Button onClick={() => setCreating(true)}>New adjustment</Button>}
      />

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Adjustment No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Store</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {adjustments.map((a) => (
              <tr key={a.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{a.adjustmentNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.adjustmentDate}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.storeName}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={statusTone[a.status]}>{ADJUSTMENT_STATUS_LABELS[a.status]}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canApprove && (
                    <div className="flex justify-end gap-1">
                      <Button
                        variant="secondary"
                        onClick={async () => {
                          try {
                            await decide.mutateAsync({ id: a.id, approve: true, rejectionReason: null })
                          } catch (err) {
                            setError(errorMessage(err))
                          }
                        }}
                      >
                        Approve
                      </Button>
                      <Button variant="ghost" className="text-red-600" onClick={() => setRejecting(a)}>
                        Reject
                      </Button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && adjustments.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                  Nothing pending.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <CreateModal onClose={() => setCreating(false)} />}
      {rejecting && (
        <RejectModal
          adjustment={rejecting}
          onClose={() => setRejecting(null)}
          onSubmit={async (reason) => {
            try {
              await decide.mutateAsync({ id: rejecting.id, approve: false, rejectionReason: reason })
              setRejecting(null)
            } catch (err) {
              setError(errorMessage(err))
            }
          }}
          loading={decide.isPending}
        />
      )}
    </>
  )
}

function RejectModal({
  adjustment,
  onClose,
  onSubmit,
  loading,
}: {
  adjustment: StockAdjustmentListItem
  onClose: () => void
  onSubmit: (reason: string) => void
  loading: boolean
}) {
  const [reason, setReason] = useState('')
  return (
    <Modal
      open
      title={`Reject ${adjustment.adjustmentNumber}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button variant="danger" onClick={() => onSubmit(reason)} loading={loading} disabled={!reason.trim()}>
            Reject
          </Button>
        </>
      }
    >
      <Input label="Rejection reason" value={reason} onChange={(e) => setReason(e.target.value)} />
    </Modal>
  )
}

function CreateModal({ onClose }: { onClose: () => void }) {
  const create = useCreateAdjustment()
  const { data: storesResult } = useStores()
  const [storeId, setStoreId] = useState('')
  const { data: batches = [] } = useBatches(undefined, undefined, storeId ? Number(storeId) : undefined, false)
  const [lines, setLines] = useState<{ batchId: string; change: string }[]>([{ batchId: '', change: '' }])
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const items = lines
      .filter((l) => l.batchId && l.change && Number(l.change) !== 0)
      .map((l) => ({ batchId: Number(l.batchId), quantityChange: Number(l.change) }))
    const errors = compactErrors({
      storeId: requiredSelect(storeId, 'Store'),
      reason: requiredText(reason, 'Reason'),
      items: items.length === 0 ? 'Add at least one complete line — each needs a batch and a non-zero quantity change.' : undefined,
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await create.mutateAsync({ storeId: Number(storeId), reason, items })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="New stock adjustment"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={create.isPending}>
            Request
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
          label="Store"
          required
          error={fieldErrors.storeId}
          placeholder="Select…"
          value={storeId}
          onChange={(e) => setStoreId(e.target.value)}
          options={(storesResult ?? []).map((s) => ({ value: s.id, label: s.name }))}
        />
        {lines.map((line, idx) => (
          <div key={idx} className="flex items-end gap-2">
            <Select
              label="Batch"
              className="flex-1"
              placeholder="Select…"
              value={line.batchId}
              onChange={(e) => setLines(lines.map((l, i) => (i === idx ? { ...l, batchId: e.target.value } : l)))}
              options={batches.map((b) => ({ value: b.id, label: `${b.productName} — ${b.batchNumber} (on hand: ${b.quantityOnHand})` }))}
            />
            <div className="w-32">
              <Input
                label="Change (± qty)"
                type="number"
                value={line.change}
                onChange={(e) => setLines(lines.map((l, i) => (i === idx ? { ...l, change: e.target.value } : l)))}
              />
            </div>
          </div>
        ))}
        <Button variant="secondary" onClick={() => setLines([...lines, { batchId: '', change: '' }])}>
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
