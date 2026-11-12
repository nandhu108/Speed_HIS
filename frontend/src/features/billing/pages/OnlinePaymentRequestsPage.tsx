import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useCancelOnlinePaymentRequest, useCompleteOnlinePaymentRequest, useOnlinePaymentRequests } from '../api'
import { ONLINE_PAYMENT_REQUEST_STATUS_LABELS } from '../types'

/**
 * The Patient Portal's "Pay Online" button queues a request here and tells the patient "our
 * billing team will contact you" — this page is that follow-up worklist. It never collects
 * payment itself: click through to the bill and collect it the normal way, then come back
 * and mark this request Completed (or Cancelled, if it was a mistake/duplicate) to close the loop.
 */
export function OnlinePaymentRequestsPage() {
  const navigate = useNavigate()
  const { hasPermission } = useAuth()
  const canManage = hasPermission('BILLING.ONLINEPAYMENT.MANAGE')
  const { data: pending = [], isLoading } = useOnlinePaymentRequests(0)
  const complete = useCompleteOnlinePaymentRequest()
  const cancel = useCancelOnlinePaymentRequest()
  const [error, setError] = useState<string | null>(null)

  const act = async (id: number, fn: typeof complete) => {
    setError(null)
    try {
      await fn.mutateAsync(id)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader
        title="Online Payment Requests"
        subtitle="Patients who clicked 'Pay Online' in the Patient Portal — no payment gateway is wired up yet, so these need a human follow-up and a normal payment collection on the bill."
      />

      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Requested</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Bill</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Amount</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {pending.map((r) => (
              <tr key={r.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-xs text-slate-500">{new Date(r.requestedAt).toLocaleString()}</td>
                <td className="px-4 py-2.5">
                  <button className="font-mono text-xs text-brand-600 hover:underline" onClick={() => navigate(`/billing/bills/${r.billId}`)}>
                    {r.billNumber}
                  </button>
                </td>
                <td className="px-4 py-2.5 text-slate-700">{r.patientName}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.amount.toFixed(2)}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone="warning">{ONLINE_PAYMENT_REQUEST_STATUS_LABELS[r.status]}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canManage && (
                    <div className="flex justify-end gap-1">
                      <Button variant="secondary" onClick={() => act(r.id, complete)} loading={complete.isPending}>
                        Mark completed
                      </Button>
                      <Button variant="ghost" className="text-red-600" onClick={() => act(r.id, cancel)} loading={cancel.isPending}>
                        Cancel
                      </Button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && pending.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  No pending online payment requests.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}
