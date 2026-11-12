import { Fragment, useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useAccounts, useCancelVoucher, usePostVoucher, useVouchers } from '../api'
import { VOUCHER_STATUS_LABELS, VOUCHER_TYPE_LABELS, type PostVoucherLinePayload, type VoucherDto, type VoucherType } from '../types'

export function VouchersPage() {
  const { hasPermission } = useAuth()
  const canCreate = hasPermission('FINANCE.VOUCHER.CREATE')
  const canCancel = hasPermission('FINANCE.VOUCHER.CANCEL')
  const { data: vouchers = [], isLoading } = useVouchers({})
  const [creating, setCreating] = useState(false)
  const [expanded, setExpanded] = useState<number | null>(null)
  const [cancelling, setCancelling] = useState<VoucherDto | null>(null)

  return (
    <>
      <PageHeader
        title="Vouchers"
        subtitle="Journal, Payment and Receipt vouchers — every ledger posting is one of these"
        actions={canCreate && <Button onClick={() => setCreating(true)}>New voucher</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Voucher No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Narration</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Posted By</th>
              {canCancel && <th className="px-4 py-2.5 font-medium text-slate-600">Actions</th>}
            </tr>
          </thead>
          <tbody>
            {vouchers.map((v) => (
              <Fragment key={v.id}>
                <tr
                  key={v.id}
                  className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50"
                  onClick={() => setExpanded(expanded === v.id ? null : v.id)}
                >
                  <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{v.voucherNumber}</td>
                  <td className="px-4 py-2.5 text-slate-700">{VOUCHER_TYPE_LABELS[v.voucherType]}</td>
                  <td className="px-4 py-2.5 text-slate-700">{v.voucherDate}</td>
                  <td className="px-4 py-2.5 text-slate-700">{v.narration}</td>
                  <td className="px-4 py-2.5">
                    <StatusBadge tone={v.status === 1 ? 'success' : 'neutral'}>{VOUCHER_STATUS_LABELS[v.status]}</StatusBadge>
                  </td>
                  <td className="px-4 py-2.5 text-slate-700">{v.postedByName}</td>
                  {canCancel && (
                    <td className="px-4 py-2.5" onClick={(e) => e.stopPropagation()}>
                      {v.status === 1 && !v.sourceReference?.startsWith('REVERSAL') && (
                        <Button variant="ghost" className="text-red-600" onClick={() => setCancelling(v)}>Cancel</Button>
                      )}
                    </td>
                  )}
                </tr>
                {expanded === v.id && (
                  <tr>
                    <td colSpan={canCancel ? 7 : 6} className="bg-slate-50 px-4 py-3">
                      <table className="w-full text-xs">
                        <thead>
                          <tr className="text-left text-slate-500">
                            <th className="pb-1">Account</th>
                            <th className="pb-1 text-right">Debit</th>
                            <th className="pb-1 text-right">Credit</th>
                          </tr>
                        </thead>
                        <tbody>
                          {v.lines.map((l, i) => (
                            <tr key={i}>
                              <td className="py-0.5">{l.accountCode} — {l.accountName}</td>
                              <td className="py-0.5 text-right">{l.debitAmount ? l.debitAmount.toFixed(2) : ''}</td>
                              <td className="py-0.5 text-right">{l.creditAmount ? l.creditAmount.toFixed(2) : ''}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </td>
                  </tr>
                )}
              </Fragment>
            ))}
            {!isLoading && vouchers.length === 0 && (
              <tr>
                <td colSpan={canCancel ? 7 : 6} className="px-4 py-10 text-center text-slate-400">No vouchers posted yet.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <NewVoucherModal onClose={() => setCreating(false)} />}
      {cancelling && <CancelVoucherModal voucher={cancelling} onClose={() => setCancelling(null)} />}
    </>
  )
}

function NewVoucherModal({ onClose }: { onClose: () => void }) {
  const post = usePostVoucher()
  const { data: accounts = [] } = useAccounts()
  const [voucherType, setVoucherType] = useState<VoucherType>(1)
  const [voucherDate, setVoucherDate] = useState(new Date().toISOString().slice(0, 10))
  const [narration, setNarration] = useState('')
  const [lines, setLines] = useState<PostVoucherLinePayload[]>([
    { accountId: 0, debitAmount: 0, creditAmount: 0, narration: null },
    { accountId: 0, debitAmount: 0, creditAmount: 0, narration: null },
  ])
  const [error, setError] = useState<string | null>(null)

  const totalDebit = lines.reduce((sum, l) => sum + (l.debitAmount || 0), 0)
  const totalCredit = lines.reduce((sum, l) => sum + (l.creditAmount || 0), 0)
  const balanced = totalDebit === totalCredit && totalDebit > 0

  const updateLine = (index: number, patch: Partial<PostVoucherLinePayload>) =>
    setLines(lines.map((l, i) => (i === index ? { ...l, ...patch } : l)))

  const submit = async () => {
    setError(null)
    try {
      await post.mutateAsync({ voucherType, voucherDate, narration, lines: lines.filter((l) => l.accountId > 0) })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="New voucher"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={post.isPending} disabled={!balanced || !narration}>Post voucher</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <div className="grid grid-cols-2 gap-2">
          <Select
            label="Type"
            value={String(voucherType)}
            onChange={(e) => setVoucherType(Number(e.target.value) as VoucherType)}
            options={[{ value: 1, label: 'Journal' }, { value: 2, label: 'Payment' }, { value: 3, label: 'Receipt' }]}
          />
          <Input label="Date" type="date" value={voucherDate} onChange={(e) => setVoucherDate(e.target.value)} />
        </div>
        <Input label="Narration" value={narration} onChange={(e) => setNarration(e.target.value)} />

        <div className="space-y-2">
          {lines.map((line, i) => (
            <div key={i} className="grid grid-cols-[1fr_100px_100px_28px] items-end gap-2">
              <Select
                label={i === 0 ? 'Account' : undefined}
                value={line.accountId || ''}
                onChange={(e) => updateLine(i, { accountId: Number(e.target.value) })}
                placeholder="Select"
                options={accounts.map((a) => ({ value: a.id, label: `${a.code} — ${a.name}` }))}
              />
              <Input
                label={i === 0 ? 'Debit' : undefined}
                type="number"
                value={line.debitAmount || ''}
                onChange={(e) => updateLine(i, { debitAmount: Number(e.target.value), creditAmount: 0 })}
              />
              <Input
                label={i === 0 ? 'Credit' : undefined}
                type="number"
                value={line.creditAmount || ''}
                onChange={(e) => updateLine(i, { creditAmount: Number(e.target.value), debitAmount: 0 })}
              />
              <Button
                variant="ghost"
                className="text-red-600"
                disabled={lines.length <= 2}
                onClick={() => setLines(lines.filter((_, idx) => idx !== i))}
              >
                ×
              </Button>
            </div>
          ))}
          <Button variant="secondary" onClick={() => setLines([...lines, { accountId: 0, debitAmount: 0, creditAmount: 0, narration: null }])}>
            Add line
          </Button>
        </div>

        <div className={`rounded-md px-3 py-2 text-sm ${balanced ? 'bg-green-50 text-green-700' : 'bg-amber-50 text-amber-700'}`}>
          Debit total {totalDebit.toFixed(2)} · Credit total {totalCredit.toFixed(2)}
          {!balanced && ' — must be equal and greater than zero'}
        </div>
      </div>
    </Modal>
  )
}

function CancelVoucherModal({ voucher, onClose }: { voucher: VoucherDto; onClose: () => void }) {
  const cancel = useCancelVoucher()
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await cancel.mutateAsync({ id: voucher.id, reason })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Cancel ${voucher.voucherNumber}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Back</Button>
          <Button onClick={submit} loading={cancel.isPending} disabled={!reason}>Cancel voucher</Button>
        </>
      }
    >
      {error && <p className="mb-2 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <p className="mb-2 text-sm text-slate-600">This posts a reversal voucher — the original entries stay in the ledger.</p>
      <textarea
        className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
        rows={3}
        placeholder="Reason"
        value={reason}
        onChange={(e) => setReason(e.target.value)}
      />
    </Modal>
  )
}
