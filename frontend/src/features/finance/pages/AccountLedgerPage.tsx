import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useAccountLedger } from '../api'

export function AccountLedgerPage() {
  const { accountId } = useParams<{ accountId: string }>()
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const { data: ledger, isLoading } = useAccountLedger(Number(accountId), { fromDate: fromDate || undefined, toDate: toDate || undefined })

  if (isLoading || !ledger) return <Spinner />

  return (
    <>
      <PageHeader title={`${ledger.accountCode} — ${ledger.accountName}`} subtitle="Account ledger" backTo="/finance/accounts" />

      <div className="mb-4 flex flex-wrap items-end gap-2">
        <Input label="From date" type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
        <Input label="To date" type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
      </div>

      <div className="mb-4 grid grid-cols-2 gap-4 sm:grid-cols-3">
        <Stat label="Opening balance" value={ledger.openingBalance} />
        <Stat label="Closing balance" value={ledger.closingBalance} />
        <Stat label="Entries" value={ledger.entries.length} />
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Voucher</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Narration</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Debit</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Credit</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Balance</th>
            </tr>
          </thead>
          <tbody>
            {ledger.entries.map((e) => (
              <tr key={e.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-500">{e.entryDate}</td>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{e.voucherNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{e.narration}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{e.debitAmount ? e.debitAmount.toFixed(2) : ''}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{e.creditAmount ? e.creditAmount.toFixed(2) : ''}</td>
                <td className="px-4 py-2.5 text-right font-medium text-slate-800">{e.runningBalance.toFixed(2)}</td>
              </tr>
            ))}
            {ledger.entries.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">No entries in this range.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}

function Stat({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="text-xs font-medium uppercase tracking-wide text-slate-400">{label}</div>
      <div className="mt-1 text-2xl font-semibold text-slate-800">{typeof value === 'number' && value % 1 !== 0 ? value.toFixed(2) : value}</div>
    </div>
  )
}
