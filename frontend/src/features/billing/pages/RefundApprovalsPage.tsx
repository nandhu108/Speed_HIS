import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useAuth } from '../../auth/AuthContext'
import { useDecideRefund, useIssuableRefunds, useIssueRefund, usePendingRefunds } from '../api'
import type { Refund } from '../types'

/** Either a bill number or "Advance #N" — Refund.billNumber/advanceId are mutually exclusive. */
function refundSubject(r: Refund) {
  return r.billNumber ?? `Advance #${r.advanceId}`
}

export function RefundApprovalsPage() {
  const { hasPermission } = useAuth()
  const canApprove = hasPermission('BILLING.REFUND.APPROVE')
  const canIssue = hasPermission('BILLING.REFUND.ISSUE')
  const { data: pending = [], isLoading: pendingLoading } = usePendingRefunds()
  const { data: issuable = [], isLoading: issuableLoading } = useIssuableRefunds()
  const [deciding, setDeciding] = useState<{ refund: Refund; approve: boolean } | null>(null)
  const [issuing, setIssuing] = useState<Refund | null>(null)

  return (
    <>
      <PageHeader title="Refund Approvals" subtitle="Refund requests waiting for approval or payout — against a bill or an advance's unused deposit" />

      <div className="mb-6 rounded-lg border border-slate-200 bg-white">
        <div className="border-b border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700">Pending Approval</div>
        {pendingLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Bill / Advance</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Amount</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Method</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Reason</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {pending.map((r) => (
              <tr key={r.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{refundSubject(r)}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.amount.toFixed(2)}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.method === 1 ? 'Cash' : 'Credit Note'}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.reason}</td>
                <td className="px-4 py-2.5 text-right">
                  {canApprove && (
                    <div className="flex justify-end gap-1">
                      <Button variant="secondary" onClick={() => setDeciding({ refund: r, approve: true })}>
                        Approve
                      </Button>
                      <Button variant="danger" onClick={() => setDeciding({ refund: r, approve: false })}>
                        Reject
                      </Button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
            {!pendingLoading && pending.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                  Nothing pending.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        <div className="border-b border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700">
          Approved — Awaiting Payout
        </div>
        {issuableLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Bill / Advance</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Amount</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Method</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Approved By</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {issuable.map((r) => (
              <tr key={r.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{refundSubject(r)}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.amount.toFixed(2)}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.method === 1 ? 'Cash' : 'Credit Note'}</td>
                <td className="px-4 py-2.5 text-slate-700">
                  {r.approvedByName ?? '—'}{r.approvedAt ? ` (${new Date(r.approvedAt).toLocaleString()})` : ''}
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canIssue && (
                    <Button variant="secondary" onClick={() => setIssuing(r)}>
                      Issue
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {!issuableLoading && issuable.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                  Nothing awaiting payout.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {deciding && <DecideModal refund={deciding.refund} approve={deciding.approve} onClose={() => setDeciding(null)} />}
      {issuing && <IssueModal refund={issuing} onClose={() => setIssuing(null)} />}
    </>
  )
}

function DecideModal({ refund, approve, onClose }: { refund: Refund; approve: boolean; onClose: () => void }) {
  const decide = useDecideRefund()
  const [rejectionReason, setRejectionReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await decide.mutateAsync({ id: refund.id, approve, rejectionReason: approve ? null : rejectionReason })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={approve ? 'Approve refund' : 'Reject refund'}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button
            variant={approve ? 'primary' : 'danger'}
            onClick={submit}
            loading={decide.isPending}
            disabled={!approve && !rejectionReason.trim()}
          >
            {approve ? 'Approve' : 'Reject'}
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <p className="mb-3 text-sm text-slate-600">
        {refundSubject(refund)} · {refund.amount.toFixed(2)} · {refund.reason}
      </p>
      {approve && (
        <p className="mb-3 text-xs text-slate-500">
          This only records the decision — the {refund.method === 1 ? 'cash' : 'credit note'} still needs to be issued
          separately once approved.
        </p>
      )}
      {!approve && <Input label="Rejection reason" value={rejectionReason} onChange={(e) => setRejectionReason(e.target.value)} />}
    </Modal>
  )
}

function IssueModal({ refund, onClose }: { refund: Refund; onClose: () => void }) {
  const issue = useIssueRefund()
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await issue.mutateAsync(refund.id)
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Issue refund"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={issue.isPending}>
            Confirm issued
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <p className="text-sm text-slate-600">
        Confirm {refund.amount.toFixed(2)} has been physically handed over ({refund.method === 1 ? 'cash' : 'credit note'})
        for {refundSubject(refund)}. This reverses the bill's balance or reduces the advance's remaining deposit.
      </p>
    </Modal>
  )
}
