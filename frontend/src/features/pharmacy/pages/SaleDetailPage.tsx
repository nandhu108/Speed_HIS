import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { usePaymentModes } from '../../billing/api'
import { useCancelSale, useCreateSalesReturn, useModifyPayType, usePharmacySale, openSaleInvoice } from '../api'
import { SALE_STATUS_LABELS, type PharmacySaleStatus } from '../types'
import { compactErrors, hasErrors, requiredText, type FieldErrors } from '../../../lib/formValidation'

const statusTone: Record<PharmacySaleStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'success', 2: 'neutral',
}

export function SaleDetailPage() {
  const { id } = useParams()
  const saleId = Number(id)
  const { hasPermission } = useAuth()
  const { data: sale, isLoading } = usePharmacySale(saleId)
  const cancel = useCancelSale()

  const [cancelling, setCancelling] = useState(false)
  const [returning, setReturning] = useState(false)
  const [modifyingPayType, setModifyingPayType] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const canCancel = hasPermission('PHARMACY.SALE.CANCEL')
  const canReturn = hasPermission('PHARMACY.SALESRETURN.CREATE')
  const canModifyPayType = hasPermission('PHARMACY.SALE.MODIFY')

  if (isLoading || !sale) return <Spinner />

  return (
    <>
      <PageHeader
        title={sale.saleNumber}
        subtitle={`${sale.patientName ?? 'Walk-in'} · ${sale.saleDate} · ${sale.storeName}`}
        backTo="/pharmacy/sales"
        actions={
          <div className="flex items-center gap-2">
            <StatusBadge tone={statusTone[sale.status]}>{SALE_STATUS_LABELS[sale.status]}</StatusBadge>
            <Button variant="secondary" onClick={() => openSaleInvoice(saleId)}>
              Print invoice
            </Button>
            {canReturn && sale.status === 1 && (
              <Button variant="secondary" onClick={() => setReturning(true)}>
                Return items
              </Button>
            )}
            {canModifyPayType && sale.status === 1 && (
              <Button variant="secondary" onClick={() => setModifyingPayType(true)}>
                {sale.paymentModeName ? 'Modify pay type' : 'Set pay type'}
              </Button>
            )}
            {canCancel && sale.status === 1 && (
              <Button variant="danger" onClick={() => setCancelling(true)}>
                Cancel sale
              </Button>
            )}
          </div>
        }
      />

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="mb-6 grid grid-cols-2 gap-4 rounded-lg border border-slate-200 bg-white p-4 sm:grid-cols-4">
        <div>
          <p className="text-xs text-slate-500">Sub Total</p>
          <p className="mt-0.5 text-slate-800">{sale.subTotal.toFixed(2)}</p>
        </div>
        <div>
          <p className="text-xs text-slate-500">Discount</p>
          <p className="mt-0.5 text-slate-800">{sale.discountAmount.toFixed(2)}</p>
        </div>
        <div>
          <p className="text-xs text-slate-500">Tax</p>
          <p className="mt-0.5 text-slate-800">{sale.taxAmount.toFixed(2)}</p>
        </div>
        <div>
          <p className="text-xs text-slate-500">Total</p>
          <p className="mt-0.5 font-semibold text-slate-900">{sale.totalAmount.toFixed(2)}</p>
        </div>
        <div>
          <p className="text-xs text-slate-500">Pay Type</p>
          <p className="mt-0.5 text-slate-800">{sale.paymentModeName ?? '—'}</p>
        </div>
      </div>

      {sale.cancelReason && (
        <p className="mb-4 rounded-md bg-slate-50 px-3 py-2 text-sm text-slate-600">Cancelled: {sale.cancelReason}</p>
      )}

      <div className="rounded-lg border border-slate-200 bg-white">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Drug</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Batch</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Expiry</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Qty</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Rate</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Total</th>
            </tr>
          </thead>
          <tbody>
            {sale.items.map((item) => (
              <tr key={item.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{item.drugName}</td>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{item.batchNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{item.expiryDate}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{item.quantity}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{item.rate.toFixed(2)}</td>
                <td className="px-4 py-2.5 text-right font-medium text-slate-800">{item.totalAmount.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {cancelling && (
        <CancelModal
          onClose={() => setCancelling(false)}
          onSubmit={async (reason) => {
            try {
              await cancel.mutateAsync({ id: saleId, reason })
              setCancelling(false)
            } catch (err) {
              setError(errorMessage(err))
            }
          }}
          loading={cancel.isPending}
        />
      )}
      {returning && <ReturnModal saleId={saleId} onClose={() => setReturning(false)} />}
      {modifyingPayType && (
        <PayTypeModal
          saleId={saleId}
          currentPaymentModeId={sale.paymentModeId}
          onClose={() => setModifyingPayType(false)}
          onError={setError}
        />
      )}
    </>
  )
}

function CancelModal({ onClose, onSubmit, loading }: { onClose: () => void; onSubmit: (reason: string) => void; loading: boolean }) {
  const [reason, setReason] = useState('')
  return (
    <Modal
      open
      title="Cancel sale"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button variant="danger" onClick={() => onSubmit(reason)} loading={loading} disabled={!reason.trim()}>
            Cancel sale
          </Button>
        </>
      }
    >
      <p className="mb-3 text-sm text-slate-600">All sold batches will be returned to stock.</p>
      <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
    </Modal>
  )
}

function ReturnModal({ saleId, onClose }: { saleId: number; onClose: () => void }) {
  const { data: sale } = usePharmacySale(saleId)
  const createReturn = useCreateSalesReturn()
  const [quantities, setQuantities] = useState<Record<number, string>>({})
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const items = Object.entries(quantities)
      .filter(([, v]) => v && Number(v) > 0)
      .map(([itemId, v]) => ({ pharmacySaleItemId: Number(itemId), quantity: Number(v) }))
    const errors = compactErrors({
      reason: requiredText(reason, 'Reason'),
      items: items.length === 0 ? 'Enter a return quantity greater than zero on at least one line.' : undefined,
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await createReturn.mutateAsync({ pharmacySaleId: saleId, reason, items })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Return items"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={createReturn.isPending}>
            Post return
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
        {sale?.items.map((item) => (
          <div key={item.id} className="flex items-center justify-between gap-3">
            <span className="text-sm text-slate-700">
              {item.drugName} ({item.batchNumber}) — sold {item.quantity}
            </span>
            <input
              type="number"
              min={0}
              max={item.quantity}
              className="w-24 rounded-md border border-slate-300 px-2 py-1 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
              value={quantities[item.id] ?? ''}
              onChange={(e) => setQuantities({ ...quantities, [item.id]: e.target.value })}
            />
          </div>
        ))}
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

/** "Pharmacy Pay Type Modify" — corrects the payment mode on a completed sale. */
function PayTypeModal({
  saleId, currentPaymentModeId, onClose, onError,
}: { saleId: number; currentPaymentModeId: number | null; onClose: () => void; onError: (e: string | null) => void }) {
  const { data: paymentModes = [] } = usePaymentModes()
  const modifyPayType = useModifyPayType()
  const [paymentModeId, setPaymentModeId] = useState(currentPaymentModeId ? String(currentPaymentModeId) : '')

  const submit = async () => {
    onError(null)
    try {
      await modifyPayType.mutateAsync({ id: saleId, paymentModeId: paymentModeId ? Number(paymentModeId) : null })
      onClose()
    } catch (err) {
      onError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Modify pay type"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button onClick={submit} loading={modifyPayType.isPending}>
            Save
          </Button>
        </>
      }
    >
      <Select
        label="Payment mode"
        placeholder="Not recorded"
        value={paymentModeId}
        onChange={(e) => setPaymentModeId(e.target.value)}
        options={paymentModes.filter((m) => m.isActive).map((m) => ({ value: m.id, label: m.name }))}
      />
    </Modal>
  )
}
