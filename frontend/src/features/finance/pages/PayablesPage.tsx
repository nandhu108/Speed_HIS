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
import { useAccounts, useCreatePayable, usePayableAging, usePayables, usePayPayable, usePendingGrns } from '../api'
import { PAYABLE_STATUS_LABELS, type PayableDto, type PendingGrnDto } from '../types'

type Tab = 'payables' | 'pending' | 'aging'

export function PayablesPage() {
  const [tab, setTab] = useState<Tab>('payables')
  const tabs: { key: Tab; label: string }[] = [
    { key: 'payables', label: 'Payables' },
    { key: 'pending', label: 'Pending GRNs' },
    { key: 'aging', label: 'Aging' },
  ]

  return (
    <>
      <PageHeader title="Payables" subtitle="Supplier invoices from GRN, through payment" />
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
      {tab === 'payables' && <PayablesTab />}
      {tab === 'pending' && <PendingGrnsTab />}
      {tab === 'aging' && <AgingTab />}
    </>
  )
}

function PayablesTab() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('FINANCE.PAYABLE.MANAGE')
  const { data: payables = [], isLoading } = usePayables()
  const [paying, setPaying] = useState<PayableDto | null>(null)

  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      {isLoading && <Spinner />}
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50 text-left">
            <th className="px-4 py-2.5 font-medium text-slate-600">Vendor</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">GRN</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Invoice</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Amount</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Balance</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            {canManage && <th className="px-4 py-2.5 font-medium text-slate-600">Actions</th>}
          </tr>
        </thead>
        <tbody>
          {payables.map((p) => (
            <tr key={p.id} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-2.5 text-slate-700">{p.vendorName}</td>
              <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{p.grnNumber}</td>
              <td className="px-4 py-2.5 text-slate-700">{p.invoiceNumber}</td>
              <td className="px-4 py-2.5 text-right text-slate-700">{p.invoiceAmount.toFixed(2)}</td>
              <td className="px-4 py-2.5 text-right font-medium text-slate-800">{p.balanceAmount.toFixed(2)}</td>
              <td className="px-4 py-2.5">
                <StatusBadge tone={p.status === 3 ? 'success' : p.status === 2 ? 'warning' : 'neutral'}>{PAYABLE_STATUS_LABELS[p.status]}</StatusBadge>
              </td>
              {canManage && (
                <td className="px-4 py-2.5">
                  {p.status !== 3 && <Button variant="secondary" onClick={() => setPaying(p)}>Record payment</Button>}
                </td>
              )}
            </tr>
          ))}
          {!isLoading && payables.length === 0 && (
            <tr>
              <td colSpan={canManage ? 7 : 6} className="px-4 py-10 text-center text-slate-400">No payables raised yet.</td>
            </tr>
          )}
        </tbody>
      </table>
      {paying && <PayModal payable={paying} onClose={() => setPaying(null)} />}
    </div>
  )
}

function PendingGrnsTab() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('FINANCE.PAYABLE.MANAGE')
  const { data: grns = [], isLoading } = usePendingGrns()
  const create = useCreatePayable()
  const [error, setError] = useState<string | null>(null)

  const raise = async (grn: PendingGrnDto) => {
    setError(null)
    try {
      await create.mutateAsync({ grnId: grn.grnId, dueDate: null })
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
            <th className="px-4 py-2.5 font-medium text-slate-600">GRN No</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Vendor</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Received</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Supplier Invoice</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Amount</th>
            {canManage && <th className="px-4 py-2.5 font-medium text-slate-600">Actions</th>}
          </tr>
        </thead>
        <tbody>
          {grns.map((g) => (
            <tr key={g.grnId} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{g.grnNumber}</td>
              <td className="px-4 py-2.5 text-slate-700">{g.vendorName}</td>
              <td className="px-4 py-2.5 text-slate-700">{g.receivedDate}</td>
              <td className="px-4 py-2.5 text-slate-700">{g.supplierInvoiceNumber ?? '—'}</td>
              <td className="px-4 py-2.5 text-right text-slate-700">{g.totalAmount.toFixed(2)}</td>
              {canManage && (
                <td className="px-4 py-2.5">
                  <Button variant="secondary" loading={create.isPending} onClick={() => raise(g)}>Raise payable</Button>
                </td>
              )}
            </tr>
          ))}
          {!isLoading && grns.length === 0 && (
            <tr>
              <td colSpan={canManage ? 6 : 5} className="px-4 py-10 text-center text-slate-400">No posted GRNs pending a payable.</td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  )
}

function AgingTab() {
  const [asOf, setAsOf] = useState(new Date().toISOString().slice(0, 10))
  const { data: rows = [], isLoading } = usePayableAging(asOf)

  return (
    <div>
      <Input label="As of date" type="date" value={asOf} onChange={(e) => setAsOf(e.target.value)} className="mb-3 max-w-xs" />
      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Vendor</th>
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
                <td colSpan={3} className="px-4 py-10 text-center text-slate-400">No outstanding payables.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function PayModal({ payable, onClose }: { payable: PayableDto; onClose: () => void }) {
  const pay = usePayPayable()
  const { data: accounts = [] } = useAccounts()
  const [amount, setAmount] = useState(String(payable.balanceAmount))
  const [cashAccountId, setCashAccountId] = useState('')
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10))
  const [referenceNumber, setReferenceNumber] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await pay.mutateAsync({ id: payable.id, amount: Number(amount), cashAccountId: Number(cashAccountId), date, referenceNumber: referenceNumber || null })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Record payment — ${payable.vendorName}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={pay.isPending} disabled={!amount || !cashAccountId}>Record payment</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <p className="text-sm text-slate-600">Outstanding balance: {payable.balanceAmount.toFixed(2)}</p>
        <Input label="Amount" type="number" value={amount} onChange={(e) => setAmount(e.target.value)} />
        <Select
          label="Paid from account"
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
