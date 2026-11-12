import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useAuth } from '../../auth/AuthContext'
import { useDecideCreditNote, usePendingCreditNotes } from '../api'
import type { CreditNote } from '../types'

export function CreditNotesApprovalPage() {
  const { hasPermission } = useAuth()
  const canApprove = hasPermission('BILLING.CREDITNOTE.APPROVE')
  const { data: creditNotes = [], isLoading } = usePendingCreditNotes()
  const [deciding, setDeciding] = useState<{ creditNote: CreditNote; approve: boolean } | null>(null)

  return (
    <>
      <PageHeader title="Credit Note Approvals" subtitle="Bill credit notes waiting for approval" />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Credit Note</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Bill</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Amount</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Reason</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {creditNotes.map((c) => (
              <tr key={c.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{c.creditNoteNumber}</td>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{c.billNumber}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{c.amount.toFixed(2)}</td>
                <td className="px-4 py-2.5 text-slate-700">{c.reason}</td>
                <td className="px-4 py-2.5 text-right">
                  {canApprove && (
                    <div className="flex justify-end gap-1">
                      <Button variant="secondary" onClick={() => setDeciding({ creditNote: c, approve: true })}>
                        Approve
                      </Button>
                      <Button variant="danger" onClick={() => setDeciding({ creditNote: c, approve: false })}>
                        Reject
                      </Button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && creditNotes.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                  Nothing pending.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {deciding && (
        <DecideModal creditNote={deciding.creditNote} approve={deciding.approve} onClose={() => setDeciding(null)} />
      )}
    </>
  )
}

function DecideModal({ creditNote, approve, onClose }: { creditNote: CreditNote; approve: boolean; onClose: () => void }) {
  const decide = useDecideCreditNote()
  const [rejectionReason, setRejectionReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await decide.mutateAsync({ id: creditNote.id, approve, rejectionReason: approve ? null : rejectionReason })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={approve ? 'Approve credit note' : 'Reject credit note'}
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
        {creditNote.billNumber} · {creditNote.amount.toFixed(2)} · {creditNote.reason}
      </p>
      <ul className="mb-3 space-y-1 text-xs text-slate-500">
        {creditNote.items.map((i) => (
          <li key={i.id}>
            {i.billItemDescription}: {i.amount.toFixed(2)}
          </li>
        ))}
      </ul>
      {!approve && <Input label="Rejection reason" value={rejectionReason} onChange={(e) => setRejectionReason(e.target.value)} />}
    </Modal>
  )
}
