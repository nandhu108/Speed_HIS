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
import { openRadReportPdf, useCancelRadOrder, useCancelRadOrderItem, useRadOrder, useRadReport } from '../api'
import { ITEM_STATUS_LABELS, PRIORITY_LABELS, RAD_ORDER_STATUS_LABELS, type RadOrderItemStatus, type RadOrderStatus } from '../types'

const orderStatusTone: Record<RadOrderStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info',
  2: 'warning',
  3: 'success',
  4: 'neutral',
}

const itemStatusTone: Record<RadOrderItemStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'neutral',
  2: 'info',
  3: 'info',
  4: 'warning',
  5: 'warning',
  6: 'success',
  7: 'danger',
}

export function RadOrderDetailPage() {
  const { id } = useParams()
  const orderId = Number(id)
  const { hasPermission } = useAuth()
  const { data: order, isLoading } = useRadOrder(orderId)
  const cancelOrder = useCancelRadOrder(orderId)
  const cancelItem = useCancelRadOrderItem(orderId)

  const [cancelling, setCancelling] = useState(false)
  const [viewingItemId, setViewingItemId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  const canCancel = hasPermission('RAD.ORDER.CANCEL')
  const canViewReport = hasPermission('RAD.REPORT.VIEW')

  if (isLoading || !order) {
    return <Spinner />
  }

  const canCancelWhole = order.status === 1

  return (
    <>
      <PageHeader
        title={order.orderNumber}
        subtitle={`${order.patientName} (${order.patientUhid}) · ${order.patientAgeDisplay} / ${order.patientGender} · ${order.orderDate}`}
        backTo="/rad/orders"
        actions={
          <div className="flex items-center gap-2">
            <StatusBadge tone={orderStatusTone[order.status]}>{RAD_ORDER_STATUS_LABELS[order.status]}</StatusBadge>
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
              <th className="px-4 py-2.5 font-medium text-slate-600">Accession No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Scheduled</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {order.items.map((item) => (
              <tr key={item.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">
                  {item.radiologyTestName} <span className="text-xs text-slate-400">({item.modalityName})</span>
                  {item.requiresContrast && (
                    <span className="ml-2">
                      <StatusBadge tone={item.hasContrastConsent ? 'success' : 'warning'}>
                        {item.hasContrastConsent ? 'Contrast consented' : 'Contrast consent pending'}
                      </StatusBadge>
                    </span>
                  )}
                </td>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{item.accessionNumber}</td>
                <td className="px-4 py-2.5 text-slate-500">{item.scheduledAt ? new Date(item.scheduledAt).toLocaleString() : '—'}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={itemStatusTone[item.status]}>{ITEM_STATUS_LABELS[item.status]}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  <div className="flex justify-end gap-1">
                    {item.status >= 4 && (
                      <Button variant="ghost" onClick={() => setViewingItemId(item.id)}>
                        View report
                      </Button>
                    )}
                    {canViewReport && item.status === 6 && (
                      <Button variant="ghost" onClick={() => openRadReportPdf(item.id)}>
                        Print
                      </Button>
                    )}
                    {canCancel && item.status === 1 && (
                      <Button
                        variant="ghost"
                        className="text-red-600"
                        onClick={async () => {
                          if (window.confirm(`Cancel test '${item.radiologyTestName}'?`)) {
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
      {viewingItemId !== null && <ReportViewModal itemId={viewingItemId} onClose={() => setViewingItemId(null)} />}
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

function ReportViewModal({ itemId, onClose }: { itemId: number; onClose: () => void }) {
  const { data: report, isLoading } = useRadReport(itemId)
  return (
    <Modal open title="Radiology report" onClose={onClose} footer={<Button onClick={onClose}>Close</Button>}>
      {isLoading && <Spinner />}
      {report && (
        <div className="space-y-3 text-sm">
          <div>
            <p className="mb-1 text-xs font-semibold uppercase text-slate-500">Findings</p>
            <p className="text-slate-700">{report.findings}</p>
          </div>
          <div>
            <p className="mb-1 text-xs font-semibold uppercase text-slate-500">Impression</p>
            <p className="text-slate-700">{report.impression}</p>
          </div>
          <p className="text-xs text-slate-400">
            Entered {new Date(report.enteredAt).toLocaleString()} by {report.enteredByName}
            {report.verifiedAt && ` · Verified ${new Date(report.verifiedAt).toLocaleString()} by ${report.verifiedByName}`}
            {report.approvedAt && ` · Approved ${new Date(report.approvedAt).toLocaleString()} by ${report.approvedByName}`}
          </p>
        </div>
      )}
    </Modal>
  )
}
