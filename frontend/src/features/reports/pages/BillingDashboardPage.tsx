import { useState } from 'react'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useBillingDashboard } from '../api'
import { AmountBarList, StatTile } from '../components/StatTile'
import { DateRangeFilter, defaultDateRange } from '../../../components/ui/DateRangeFilter'
import { TrendChart } from '../components/TrendChart'

export function BillingDashboardPage() {
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const { data, isLoading } = useBillingDashboard({ fromDate, toDate })

  return (
    <>
      <PageHeader title="Billing Dashboard" subtitle="Collection by payment mode for your branch" />
      <DateRangeFilter
        fromDate={fromDate} toDate={toDate}
        onFromDate={(v) => setRange((r) => ({ ...r, fromDate: v }))} onToDate={(v) => setRange((r) => ({ ...r, toDate: v }))}
      />
      {isLoading || !data ? <Spinner /> : (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <StatTile label="Total Collection" value={data.totalCollection.toLocaleString()} />
            <StatTile label="Outstanding" value={data.outstandingAmount.toLocaleString()} tone={data.outstandingAmount > 0 ? 'warning' : 'success'} />
          </div>

          <div className="mt-6 grid grid-cols-1 gap-4 lg:grid-cols-2">
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <h3 className="mb-2 text-sm font-semibold text-slate-700">Daily Collection</h3>
              <TrendChart points={data.dailyTrend} />
            </div>
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <h3 className="mb-2 text-sm font-semibold text-slate-700">By Payment Mode</h3>
              <AmountBarList rows={data.byMode.map((m) => ({ label: `${m.paymentModeName} (${m.txnCount})`, amount: m.amount }))} />
            </div>
          </div>
        </>
      )}
    </>
  )
}
