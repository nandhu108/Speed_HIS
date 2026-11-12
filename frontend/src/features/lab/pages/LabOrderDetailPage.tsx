import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { openLabReport, useCancelLabOrder, useCancelLabOrderItem, useLabOrder, useLabResult } from '../api'
import { ITEM_STATUS_LABELS, LAB_ORDER_STATUS_LABELS, PRIORITY_LABELS, type LabOrderItemStatus, type LabOrderStatus } from '../types'

const orderStatusTone: Record<LabOrderStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info',
  2: 'warning',
  3: 'success',
  4: 'neutral',
}

const itemStatusTone: Record<LabOrderItemStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'neutral',
  2: 'info',
  3: 'info',
  4: 'warning',
  5: 'warning',
  6: 'success',
  7: 'danger',
}

export function LabOrderDetailPage() {
  const { id } = useParams()
  const orderId = Number(id)
  const { hasPermission } = useAuth()
  const { data: order, isLoading } = useLabOrder(orderId)
  const cancelOrder = useCancelLabOrder(orderId)
  const cancelItem = useCancelLabOrderItem(orderId)

  const [cancelling, setCancelling] = useState(false)
  const [viewingItemId, setViewingItemId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  const canCancel = hasPermission('LAB.ORDER.CANCEL')
  const canViewReport = hasPermission('LAB.REPORT.VIEW')

  if (isLoading || !order) {
    return <Spinner />
  }

  const canCancelWhole = order.status === 1

  return (
    <>
      <PageHeader
        title={order.orderNumber}
        subtitle={`${order.patientName} (${order.patientUhid}) · ${order.patientAgeDisplay} / ${order.patientGender} · ${order.orderDate}`}
        backTo="/lab/orders"
        actions={
          <div className="flex items-center gap-2">
            <StatusBadge tone={orderStatusTone[order.status]}>{LAB_ORDER_STATUS_LABELS[order.status]}</StatusBadge>
            {canViewReport && order.items.some((i) => i.status === 6) && (
              <Button variant="secondary" onClick={() => openLabReport(orderId)}>
                Print report
              </Button>
            )}
            {canCancel && canCancelWhole && (
              <Button variant="danger" onClick={() => setCancelling(true)}>
                Cancel order
              </Button>
            )}
          </div>
        }
      />

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="mb-6 grid grid-cols-2 gap-4 rounded-lg border border-slate-200 bg-white p-4 sm:grid-cols-4">
        <div>
          <p className="text-xs text-slate-500">Priority</p>
          <p className="mt-0.5 text-slate-800">{PRIORITY_LABELS[order.priority]}</p>
        </div>
        <div>
          <p className="text-xs text-slate-500">Referring doctor</p>
          <p className="mt-0.5 text-slate-800">{order.orderedByDoctorName ?? '—'}</p>
        </div>
        <div className="col-span-2">
          <p className="text-xs text-slate-500">Clinical notes</p>
          <p className="mt-0.5 text-slate-800">{order.clinicalNotes ?? '—'}</p>
        </div>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        <div className="border-b border-slate-200 px-4 py-3">
          <h2 className="text-sm font-semibold text-slate-800">Tests</h2>
        </div>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Test</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Profile</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Sample</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {order.items.map((item) => (
              <tr key={item.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{item.labTestName}</td>
                <td className="px-4 py-2.5 text-slate-700">{item.testProfileName ?? '—'}</td>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{item.sampleNumber ?? '—'}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={itemStatusTone[item.status]}>{ITEM_STATUS_LABELS[item.status]}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  <div className="flex justify-end gap-1">
                    {item.status >= 4 && (
                      <Button variant="ghost" onClick={() => setViewingItemId(item.id)}>
                        View result
                      </Button>
                    )}
                    {canCancel && item.status === 1 && (
                      <Button
                        variant="ghost"
                        className="text-red-600"
                        onClick={async () => {
                          if (window.confirm(`Cancel test '${item.labTestName}'?`)) {
                            try {
                              await cancelItem.mutateAsync(item.id)
                            } catch (err) {
                              setError(errorMessage(err))
                            }
                          }
                        }}
                      >
                        Cancel
                      </Button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {cancelling && (
        <CancelOrderModal
          onClose={() => setCancelling(false)}
          onSubmit={async (reason) => {
            try {
              await cancelOrder.mutateAsync(reason)
              setCancelling(false)
            } catch (err) {
              setError(errorMessage(err))
            }
          }}
          loading={cancelOrder.isPending}
        />
      )}
      {viewingItemId !== null && <ResultViewModal itemId={viewingItemId} onClose={() => setViewingItemId(null)} />}
    </>
  )
}

function CancelOrderModal({
  onClose,
  onSubmit,
  loading,
}: {
  onClose: () => void
  onSubmit: (reason: string) => void
  loading: boolean
}) {
  const [reason, setReason] = useState('')
  return (
    <Modal
      open
      title="Cancel order"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button variant="danger" onClick={() => onSubmit(reason)} loading={loading} disabled={!reason.trim()}>
            Cancel order
          </Button>
        </>
      }
    >
      <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
    </Modal>
  )
}

function ResultViewModal({ itemId, onClose }: { itemId: number; onClose: () => void }) {
  const { data: result, isLoading } = useLabResult(itemId)
  return (
    <Modal open title="Result" onClose={onClose} footer={<Button onClick={onClose}>Close</Button>}>
      {isLoading && <Spinner />}
      {result && (
        <div className="space-y-2">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-left">
                <th className="py-1.5 font-medium text-slate-600">Parameter</th>
                <th className="py-1.5 text-right font-medium text-slate-600">Value</th>
                <th className="py-1.5 font-medium text-slate-600">Range</th>
              </tr>
            </thead>
            <tbody>
              {result.values.map((v) => (
                <tr key={v.id} className="border-b border-slate-100 last:border-0">
                  <td className="py-1.5 text-slate-700">{v.parameterName}</td>
                  <td
                    className={`py-1.5 text-right font-medium ${
                      v.flag === 4 ? 'text-red-600' : v.flag === 1 ? 'text-slate-700' : 'text-amber-600'
                    }`}
                  >
                    {v.value} {v.unit}
                  </td>
                  <td className="py-1.5 text-slate-500">
                    {v.normalLow ?? '—'}–{v.normalHigh ?? '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {result.notes && <p className="text-sm text-slate-600">Notes: {result.notes}</p>}
          <p className="text-xs text-slate-400">
            Entered {new Date(result.enteredAt).toLocaleString()}
            {result.verifiedAt && ` · Verified ${new Date(result.verifiedAt).toLocaleString()}`}
            {result.approvedAt && ` · Approved ${new Date(result.approvedAt).toLocaleString()}`}
          </p>
        </div>
      )}
    </Modal>
  )
}
