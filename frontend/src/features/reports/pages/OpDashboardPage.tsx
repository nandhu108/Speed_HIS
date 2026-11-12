import { useState } from 'react'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useOpDashboard } from '../api'
import { AmountBarList, StatTile } from '../components/StatTile'
import { DateRangeFilter, defaultDateRange } from '../../../components/ui/DateRangeFilter'
import { TrendChart } from '../components/TrendChart'

export function OpDashboardPage() {
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const { data, isLoading } = useOpDashboard({ fromDate, toDate })

  return (
    <>
      <PageHeader title="OP Dashboard" subtitle="OPD visit census for your branch" />
      <DateRangeFilter
        fromDate={fromDate} toDate={toDate}
        onFromDate={(v) => setRange((r) => ({ ...r, fromDate: v }))} onToDate={(v) => setRange((r) => ({ ...r, toDate: v }))}
      />
      {isLoading || !data ? <Spinner /> : (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <StatTile label="Total Visits" value={data.totalVisits} />
            <StatTile label="New" value={data.newCount} />
            <StatTile label="Review" value={data.reviewCount} />
          </div>

          <div className="mt-6 grid grid-cols-1 gap-4 lg:grid-cols-2">
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <h3 className="mb-2 text-sm font-semibold text-slate-700">Daily Visits</h3>
              <TrendChart points={data.dailyTrend} />
            </div>
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <h3 className="mb-2 text-sm font-semibold text-slate-700">By Doctor</h3>
              <AmountBarList rows={data.byDoctor.map((d) => ({ label: d.doctorName, amount: d.visitCount }))} />
            </div>
          </div>
        </>
      )}
    </>
  )
}
