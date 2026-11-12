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
import { useAccounts, useCollectReceivable, useCreateReceivable, usePendingCreditBills, useReceivableAging, useReceivables } from '../api'
import { RECEIVABLE_STATUS_LABELS, type PendingCreditBillDto, type ReceivableDto } from '../types'

type Tab = 'receivables' | 'pending' | 'aging'

export function ReceivablesPage() {
  const [tab, setTab] = useState<Tab>('receivables')
  const tabs: { key: Tab; label: string }[] = [
    { key: 'receivables', label: 'Receivables' },
    { key: 'pending', label: 'Pending Credit Bills' },
    { key: 'aging', label: 'Aging' },
  ]

  return (
    <>
      <PageHeader title="Receivables" subtitle="Corporate/credit-billed outstanding, through collection" />
      <div className="mb-4 flex gap-1 border-b border-slate-200">
        {tabs.map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`border-b-2 px-3 py-2 text-sm font-medium ${
              tab === t.key ? 'border-primary-600 text-primary-700' : 'border-transparent text-slate-500 hover:text-slate-700'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>
      {tab === 'receivables' && <ReceivablesTab />}
      {tab === 'pending' && <PendingBillsTab />}
      {tab === 'aging' && <AgingTab />}
    </>
  )
}

function ReceivablesTab() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('FINANCE.RECEIVABLE.MANAGE')
  const { data: receivables = [], isLoading } = useReceivables()
  const [collecting, setCollecting] = useState<ReceivableDto | null>(null)

  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      {isLoading && <Spinner />}
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50 text-left">
            <th className="px-4 py-2.5 font-medium text-slate-600">Corporate</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Bill</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Amount</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Balance</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            {canManage && <th className="px-4 py-2.5 font-medium text-slate-600">Actions</th>}
          </tr>
        </thead>
        <tbody>
          {receivables.map((r) => (
            <tr key={r.id} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-2.5 text-slate-700">{r.corporateName}</td>
              <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{r.billNumber}</td>
              <td className="px-4 py-2.5 text-right text-slate-700">{r.billAmount.toFixed(2)}</td>
              <td className="px-4 py-2.5 text-right font-medium text-slate-800">{r.balanceAmount.toFixed(2)}</td>
              <td className="px-4 py-2.5">
                <StatusBadge tone={r.status === 3 ? 'success' : r.status === 2 ? 'warning' : 'neutral'}>{RECEIVABLE_STATUS_LABELS[r.status]}</StatusBadge>
              </td>
              {canManage && (
                <td className="px-4 py-2.5">
                  {r.status !== 3 && <Button variant="secondary" onClick={() => setCollecting(r)}>Record collection</Button>}
                </td>
              )}
            </tr>
          ))}
          {!isLoading && receivables.length === 0 && (
            <tr>
              <td colSpan={canManage ? 6 : 5} className="px-4 py-10 text-center text-slate-400">No receivables raised yet.</td>
            </tr>
          )}
        </tbody>
      </table>
      {collecting && <CollectModal receivable={collecting} onClose={() => setCollecting(null)} />}
    </div>
  )
}

function PendingBillsTab() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('FINANCE.RECEIVABLE.MANAGE')
  const { data: bills = [], isLoading } = usePendingCreditBills()
  const create = useCreateReceivable()
  const [error, setError] = useState<string | null>(null)

  const raise = async (bill: PendingCreditBillDto) => {
    setError(null)
    try {
      await create.mutateAsync({ billId: bill.billId })
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      {error && <p className="m-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      {isLoading && <Spinner />}
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50 text-left">
            <th className="px-4 py-2.5 font-medium text-slate-600">Bill No</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Corporate</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Bill Date</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Amount</th>
            {canManage && <th className="px-4 py-2.5 font-medium text-slate-600">Actions</th>}
          </tr>
        </thead>
        <tbody>
          {bills.map((b) => (
            <tr key={b.billId} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{b.billNumber}</td>
              <td className="px-4 py-2.5 text-slate-700">{b.corporateName}</td>
              <td className="px-4 py-2.5 text-slate-700">{b.billDate}</td>
              <td className="px-4 py-2.5 text-right text-slate-700">{b.totalAmount.toFixed(2)}</td>
              {canManage && (
                <td className="px-4 py-2.5">
                  <Button variant="secondary" loading={create.isPending} onClick={() => raise(b)}>Raise receivable</Button>
                </td>
              )}
            </tr>
          ))}
          {!isLoading && bills.length === 0 && (
            <tr>
              <td colSpan={canManage ? 5 : 4} className="px-4 py-10 text-center text-slate-400">No credit bills pending a receivable.</td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  )
}

function AgingTab() {
  const [asOf, setAsOf] = useState(new Date().toISOString().slice(0, 10))
  const { data: rows = [], isLoading } = useReceivableAging(asOf)

  return (
    <div>
      <Input label="As of date" type="date" value={asOf} onChange={(e) => setAsOf(e.target.value)} className="mb-3 max-w-xs" />
      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Corporate</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Bucket</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Balance</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r, i) => (
              <tr key={i} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{r.entityName}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.bucket}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.balanceAmount.toFixed(2)}</td>
              </tr>
            ))}
            {!isLoading && rows.length === 0 && (
              <tr>
                <td colSpan={3} className="px-4 py-10 text-center text-slate-400">No outstanding receivables.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function CollectModal({ receivable, onClose }: { receivable: ReceivableDto; onClose: () => void }) {
  const collect = useCollectReceivable()
  const { data: accounts = [] } = useAccounts()
  const [amount, setAmount] = useState(String(receivable.balanceAmount))
  const [cashAccountId, setCashAccountId] = useState('')
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10))
  const [referenceNumber, setReferenceNumber] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await collect.mutateAsync({ id: receivable.id, amount: Number(amount), cashAccountId: Number(cashAccountId), date, referenceNumber: referenceNumber || null })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Record collection — ${receivable.corporateName}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={collect.isPending} disabled={!amount || !cashAccountId}>Record collection</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <p className="text-sm text-slate-600">Outstanding balance: {receivable.balanceAmount.toFixed(2)}</p>
        <Input label="Amount" type="number" value={amount} onChange={(e) => setAmount(e.target.value)} />
        <Select
          label="Collected into account"
          value={cashAccountId}
          onChange={(e) => setCashAccountId(e.target.value)}
          placeholder="Select account"
          options={accounts.filter((a) => a.accountType === 1).map((a) => ({ value: a.id, label: `${a.code} — ${a.name}` }))}
        />
        <Input label="Date" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
        <Input label="Reference number" value={referenceNumber} onChange={(e) => setReferenceNumber(e.target.value)} />
      </div>
    </Modal>
  )
}
