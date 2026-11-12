import { useState } from 'react'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useCashBankCollection, useGstReport, useOutstandingReport, useRevenueReport, useTrialBalance } from '../api'

type Tab = 'cash-bank' | 'revenue' | 'gst' | 'outstanding' | 'trial-balance'

function today() {
  return new Date().toISOString().slice(0, 10)
}

export function FinanceReportsPage() {
  const [tab, setTab] = useState<Tab>('cash-bank')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const range = { fromDate: fromDate || undefined, toDate: toDate || undefined }

  const [asOfDate, setAsOfDate] = useState(today())

  const tabs: { key: Tab; label: string }[] = [
    { key: 'cash-bank', label: 'Cash & Bank Collection' },
    { key: 'revenue', label: 'Revenue' },
    { key: 'gst', label: 'GST / Tax' },
    { key: 'outstanding', label: 'Outstanding' },
    { key: 'trial-balance', label: 'Trial Balance' },
  ]

  return (
    <>
      <PageHeader title="Finance Reports" subtitle="Cash/bank collection, revenue, GST outward register, outstanding bills and trial balance" />

      {tab !== 'outstanding' && tab !== 'trial-balance' && (
        <div className="mb-4 flex flex-wrap items-end gap-2">
          <Input label="From date" type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
          <Input label="To date" type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
        </div>
      )}

      {tab === 'trial-balance' && (
        <div className="mb-4 flex flex-wrap items-end gap-2">
          <Input label="As of date" type="date" value={asOfDate} onChange={(e) => setAsOfDate(e.target.value)} />
        </div>
      )}

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

      {tab === 'cash-bank' && <CashBankTab range={range} />}
      {tab === 'revenue' && <RevenueTab range={range} />}
      {tab === 'gst' && <GstTab range={range} />}
      {tab === 'outstanding' && <OutstandingTab />}
      {tab === 'trial-balance' && <TrialBalanceTab asOfDate={asOfDate} />}
    </>
  )
}

function CashBankTab({ range }: { range: { fromDate?: string; toDate?: string } }) {
  const { data, isLoading } = useCashBankCollection(range)
  if (isLoading || !data) return <Spinner />
  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50 text-left">
            <th className="px-4 py-2.5 font-medium text-slate-600">Payment Mode</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Payments</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Amount</th>
          </tr>
        </thead>
        <tbody>
          {data.byMode.map((r) => (
            <tr key={r.paymentModeId} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-2.5 text-slate-700">{r.paymentModeName}</td>
              <td className="px-4 py-2.5 text-right text-slate-700">{r.paymentCount}</td>
              <td className="px-4 py-2.5 text-right text-slate-700">{r.amount.toFixed(2)}</td>
            </tr>
          ))}
          {data.byMode.length === 0 && (
            <tr><td colSpan={3} className="px-4 py-10 text-center text-slate-400">No collections in this range.</td></tr>
          )}
        </tbody>
        <tfoot>
          <tr className="border-t border-slate-200 font-semibold">
            <td className="px-4 py-2.5">Total</td>
            <td />
            <td className="px-4 py-2.5 text-right">{data.total.toFixed(2)}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  )
}

function RevenueTab({ range }: { range: { fromDate?: string; toDate?: string } }) {
  const { data, isLoading } = useRevenueReport(range)
  if (isLoading || !data) return <Spinner />
  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div className="rounded-lg border border-slate-200 bg-white">
        <div className="border-b border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700">By Department</div>
        <table className="w-full text-sm">
          <tbody>
            {data.byDepartment.map((r, i) => (
              <tr key={i} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{r.departmentName}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.amount.toFixed(2)}</td>
              </tr>
            ))}
            {data.byDepartment.length === 0 && (
              <tr><td colSpan={2} className="px-4 py-10 text-center text-slate-400">No revenue in this range.</td></tr>
            )}
          </tbody>
        </table>
      </div>
      <div className="rounded-lg border border-slate-200 bg-white">
        <div className="border-b border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700">By Doctor</div>
        <table className="w-full text-sm">
          <tbody>
            {data.byDoctor.map((r, i) => (
              <tr key={i} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{r.doctorName}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.amount.toFixed(2)}</td>
              </tr>
            ))}
            {data.byDoctor.length === 0 && (
              <tr><td colSpan={2} className="px-4 py-10 text-center text-slate-400">No OPD revenue in this range.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function GstTab({ range }: { range: { fromDate?: string; toDate?: string } }) {
  const { data, isLoading } = useGstReport(range)
  if (isLoading || !data) return <Spinner />
  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-slate-200 bg-white">
        <div className="border-b border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700">Outward Register</div>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Source</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Tax %</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Taxable Amount</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Tax Amount</th>
            </tr>
          </thead>
          <tbody>
            {data.outwardRegister.map((r, i) => (
              <tr key={i} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{r.source}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.taxPercent}%</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.taxableAmount.toFixed(2)}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.taxAmount.toFixed(2)}</td>
              </tr>
            ))}
            {data.outwardRegister.length === 0 && (
              <tr><td colSpan={4} className="px-4 py-10 text-center text-slate-400">No taxable transactions in this range.</td></tr>
            )}
          </tbody>
        </table>
      </div>
      <div className="rounded-lg border border-slate-200 bg-white">
        <div className="border-b border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700">HSN Summary (Pharmacy)</div>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">HSN Code</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Quantity</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Taxable Amount</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Tax Amount</th>
            </tr>
          </thead>
          <tbody>
            {data.hsnSummary.map((r, i) => (
              <tr key={i} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{r.hsnCode}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.quantity}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.taxableAmount.toFixed(2)}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.taxAmount.toFixed(2)}</td>
              </tr>
            ))}
            {data.hsnSummary.length === 0 && (
              <tr><td colSpan={4} className="px-4 py-10 text-center text-slate-400">No HSN-coded pharmacy sales in this range.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function OutstandingTab() {
  const { data, isLoading } = useOutstandingReport()
  if (isLoading || !data) return <Spinner />
  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50 text-left">
            <th className="px-4 py-2.5 font-medium text-slate-600">Bill No</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Date</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Credit</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Total</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Balance</th>
          </tr>
        </thead>
        <tbody>
          {data.bills.map((b) => (
            <tr key={b.billId} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{b.billNumber}</td>
              <td className="px-4 py-2.5 text-slate-700">{b.billDate}</td>
              <td className="px-4 py-2.5 text-slate-700">{b.patientName}</td>
              <td className="px-4 py-2.5">{b.isCreditBill ? <StatusBadge tone="warning">{b.corporateName}</StatusBadge> : '—'}</td>
              <td className="px-4 py-2.5 text-right text-slate-700">{b.totalAmount.toFixed(2)}</td>
              <td className="px-4 py-2.5 text-right font-medium text-slate-800">{b.balanceAmount.toFixed(2)}</td>
            </tr>
          ))}
          {data.bills.length === 0 && (
            <tr><td colSpan={6} className="px-4 py-10 text-center text-slate-400">No outstanding bills.</td></tr>
          )}
        </tbody>
        <tfoot>
          <tr className="border-t border-slate-200 font-semibold">
            <td className="px-4 py-2.5" colSpan={5}>Total outstanding</td>
            <td className="px-4 py-2.5 text-right">{data.totalOutstanding.toFixed(2)}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  )
}

/** Every account's cumulative Debit/Credit totals as of a date — TotalDebit == TotalCredit is the actual "trial balance" proof, so that footer row's balanced/out-of-balance state is shown explicitly rather than left for the user to eyeball. */
function TrialBalanceTab({ asOfDate }: { asOfDate: string }) {
  const { data, isLoading } = useTrialBalance(asOfDate)
  if (isLoading || !data) return <Spinner />
  const balanced = Math.abs(data.totalDebit - data.totalCredit) < 0.005
  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50 text-left">
            <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Account</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Debit</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Credit</th>
          </tr>
        </thead>
        <tbody>
          {data.rows.map((r) => (
            <tr key={r.accountId} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{r.accountCode}</td>
              <td className="px-4 py-2.5 text-slate-700">{r.accountName}</td>
              <td className="px-4 py-2.5 text-slate-500">{r.accountType}</td>
              <td className="px-4 py-2.5 text-right text-slate-700">{r.debitTotal.toFixed(2)}</td>
              <td className="px-4 py-2.5 text-right text-slate-700">{r.creditTotal.toFixed(2)}</td>
            </tr>
          ))}
          {data.rows.length === 0 && (
            <tr><td colSpan={5} className="px-4 py-10 text-center text-slate-400">No ledger activity on or before this date.</td></tr>
          )}
        </tbody>
        <tfoot>
          <tr className="border-t border-slate-200 font-semibold">
            <td className="px-4 py-2.5" colSpan={3}>
              Total {balanced ? <StatusBadge tone="success">Balanced</StatusBadge> : <StatusBadge tone="danger">Out of balance</StatusBadge>}
            </td>
            <td className="px-4 py-2.5 text-right">{data.totalDebit.toFixed(2)}</td>
            <td className="px-4 py-2.5 text-right">{data.totalCredit.toFixed(2)}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  )
}
