import { AlertTriangle, Bed, ReceiptIndianRupee, Users, Wallet } from 'lucide-react'
import { useState } from 'react'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useManagementDashboard, useRevenueDashboard } from '../api'
import { AmountBarList, StatTile } from '../components/StatTile'
import { DateRangeFilter, defaultDateRange } from '../../../components/ui/DateRangeFilter'
import { TrendChart } from '../components/TrendChart'

export function ManagementDashboardPage() {
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const { data, isLoading } = useManagementDashboard({ fromDate, toDate })
  const { data: revenue } = useRevenueDashboard({ fromDate, toDate })

  return (
    <>
      <PageHeader
        title="Dashboard"
        subtitle={data?.isCrossBranch ? 'All branches (you hold cross-branch reporting access)' : 'Your branch'}
      />
      <DateRangeFilter
        fromDate={fromDate} toDate={toDate}
        onFromDate={(v) => setRange((r) => ({ ...r, fromDate: v }))} onToDate={(v) => setRange((r) => ({ ...r, toDate: v }))}
      />
      {isLoading || !data ? (
        <Spinner />
      ) : (
        <div className="flex flex-col gap-5">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
            <StatTile label="Revenue" value={`₹${data.totalRevenue.toLocaleString()}`} icon={Wallet} />
            <StatTile label="Collection" value={`₹${data.totalCollection.toLocaleString()}`} icon={ReceiptIndianRupee} />
            <StatTile label="OP Visits" value={data.opVisitCount} icon={Users} />
            <StatTile label="Active Inpatients" value={data.activeInpatients} icon={Bed} />
            <StatTile label="Bed Occupancy" value={`${data.bedOccupancyPercent}%`} icon={Bed} />
            <StatTile
              label="Unacknowledged Alerts"
              value={data.unacknowledgedAlerts}
              tone={data.unacknowledgedAlerts > 0 ? 'danger' : 'success'}
              icon={AlertTriangle}
            />
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <div className="rounded-shell border border-border-soft bg-panel p-5 shadow-shell-sm">
              <p className="mb-1 text-[14.5px] font-semibold text-ink">Revenue Trend</p>
              <p className="mb-3 text-xs text-ink-muted">{fromDate} to {toDate}</p>
              <TrendChart points={revenue?.dailyTrend ?? []} valuePrefix="₹" />
            </div>
            <div className="rounded-shell border border-border-soft bg-panel p-5 shadow-shell-sm">
              <p className="mb-4 text-[14.5px] font-semibold text-ink">Revenue by Department</p>
              <AmountBarList rows={(revenue?.byDepartment ?? []).map((d) => ({ label: d.departmentName, amount: d.amount }))} />
            </div>
          </div>

          {data.byBranch.length > 0 && (
            <div className="rounded-shell border border-border-soft bg-panel shadow-shell-sm">
              <div className="border-b border-border-soft px-4 py-3 text-[14.5px] font-semibold text-ink">By Branch</div>
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-panel-2 text-left">
                    <th className="px-4 py-3 text-[10.5px] font-bold tracking-wider text-ink-faint uppercase">Branch</th>
                    <th className="px-4 py-3 text-[10.5px] font-bold tracking-wider text-ink-faint uppercase">Revenue</th>
                    <th className="px-4 py-3 text-[10.5px] font-bold tracking-wider text-ink-faint uppercase">Collection</th>
                    <th className="px-4 py-3 text-[10.5px] font-bold tracking-wider text-ink-faint uppercase">OP Visits</th>
                    <th className="px-4 py-3 text-[10.5px] font-bold tracking-wider text-ink-faint uppercase">Active Inpatients</th>
                  </tr>
                </thead>
                <tbody>
                  {data.byBranch.map((b) => (
                    <tr key={b.branchId} className="border-t border-border-soft">
                      <td className="px-4 py-2.5 text-ink">{b.branchName}</td>
                      <td className="font-mono px-4 py-2.5 text-ink-muted">{b.revenue.toLocaleString()}</td>
                      <td className="font-mono px-4 py-2.5 text-ink-muted">{b.collection.toLocaleString()}</td>
                      <td className="font-mono px-4 py-2.5 text-ink-muted">{b.opVisitCount}</td>
                      <td className="font-mono px-4 py-2.5 text-ink-muted">{b.activeInpatients}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          {!data.isCrossBranch && (
            <p className="text-xs text-ink-faint">
              <StatusBadge tone="info">Single-branch view</StatusBadge> — grant REPORT.CROSSBRANCH.VIEW to see every branch here.
            </p>
          )}
        </div>
      )}
    </>
  )
}
