import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useDecideDiscount, usePendingDiscountApprovals } from '../api'
import type { DiscountApproval } from '../types'

export function DiscountApprovalsPage() {
  const { data: approvals = [], isLoading } = usePendingDiscountApprovals()
  const [deciding, setDeciding] = useState<{ approval: DiscountApproval; approve: boolean } | null>(null)

  return (
    <>
      <PageHeader title="Discount Approvals" subtitle="Discount requests waiting above the auto-approve threshold" />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Bill</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Line item</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Percent</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Amount</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Reason</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {approvals.map((a) => (
              <tr key={a.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{a.billNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.billItemDescription ?? 'Whole bill'}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{a.requestedPercent}%</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{a.requestedAmount.toFixed(2)}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.reason ?? '—'}</td>
                <td className="px-4 py-2.5 text-right">
                  <div className="flex justify-end gap-1">
                    <Button variant="secondary" onClick={() => setDeciding({ approval: a, approve: true })}>
                      Approve
                    </Button>
                    <Button variant="danger" onClick={() => setDeciding({ approval: a, approve: false })}>
                      Reject
                    </Button>
                  </div>
                </td>
              </tr>
            ))}
            {!isLoading && approvals.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  Nothing pending.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {deciding && (
        <DecideModal approval={deciding.approval} approve={deciding.approve} onClose={() => setDeciding(null)} />
      )}
    </>
  )
}

function DecideModal({ approval, approve, onClose }: { approval: DiscountApproval; approve: boolean; onClose: () => void }) {
  const decide = useDecideDiscount()
  const [rejectionReason, setRejectionReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await decide.mutateAsync({ id: approval.id, approve, rejectionReason: approve ? null : rejectionReason })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={approve ? 'Approve discount' : 'Reject discount'}
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
        {approval.billNumber} · {approval.billItemDescription ?? 'Whole bill'} · {approval.requestedPercent}% ({approval.requestedAmount.toFixed(2)})
      </p>
      {!approve && <Input label="Rejection reason" value={rejectionReason} onChange={(e) => setRejectionReason(e.target.value)} />}
    </Modal>
  )
}
