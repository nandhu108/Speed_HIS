import { useState } from 'react'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useRevenueDashboard } from '../api'
import { AmountBarList, StatTile } from '../components/StatTile'
import { DateRangeFilter, defaultDateRange } from '../../../components/ui/DateRangeFilter'
import { TrendChart } from '../components/TrendChart'

/** Revenue Dashboard — also satisfies plan §23's "Doctor-wise & Department-wise reports" via the two breakdowns below. */
export function RevenueDashboardPage() {
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const { data, isLoading } = useRevenueDashboard({ fromDate, toDate })

  return (
    <>
      <PageHeader title="Revenue Dashboard" subtitle="Billed revenue by department and doctor for your branch" />
      <DateRangeFilter
        fromDate={fromDate} toDate={toDate}
        onFromDate={(v) => setRange((r) => ({ ...r, fromDate: v }))} onToDate={(v) => setRange((r) => ({ ...r, toDate: v }))}
      />
      {isLoading || !data ? <Spinner /> : (
        <>
          <StatTile label="Total Revenue" value={data.totalRevenue.toLocaleString()} />

          <div className="mt-4 rounded-lg border border-slate-200 bg-white p-4">
            <h3 className="mb-2 text-sm font-semibold text-slate-700">Daily Revenue</h3>
            <TrendChart points={data.dailyTrend} />
          </div>

          <div className="mt-6 grid grid-cols-1 gap-4 lg:grid-cols-2">
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <h3 className="mb-2 text-sm font-semibold text-slate-700">By Department</h3>
              <AmountBarList rows={data.byDepartment.map((d) => ({ label: d.departmentName, amount: d.amount }))} />
            </div>
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <h3 className="mb-2 text-sm font-semibold text-slate-700">By Doctor</h3>
              <AmountBarList rows={data.byDoctor.map((d) => ({ label: d.doctorName, amount: d.amount }))} />
            </div>
          </div>
        </>
      )}
    </>
  )
}
