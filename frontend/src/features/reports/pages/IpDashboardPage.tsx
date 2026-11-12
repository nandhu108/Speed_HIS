import { useState } from 'react'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useIpDashboard } from '../api'
import { StatTile } from '../components/StatTile'
import { DateRangeFilter, defaultDateRange } from '../../../components/ui/DateRangeFilter'

export function IpDashboardPage() {
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const { data, isLoading } = useIpDashboard({ fromDate, toDate })

  return (
    <>
      <PageHeader title="IP Dashboard" subtitle="Bed occupancy, admissions/discharges and ALOS for your branch" />
      <DateRangeFilter
        fromDate={fromDate} toDate={toDate}
        onFromDate={(v) => setRange((r) => ({ ...r, fromDate: v }))} onToDate={(v) => setRange((r) => ({ ...r, toDate: v }))}
      />
      {isLoading || !data ? <Spinner /> : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <StatTile label="Total Beds" value={data.totalBeds} />
          <StatTile label="Occupied" value={data.occupied} />
          <StatTile label="Vacant" value={data.vacant} />
          <StatTile label="Occupancy Rate" value={`${data.occupancyRatePercent}%`} />
          <StatTile label="Admissions (range)" value={data.admissionsInRange} />
          <StatTile label="Discharges (range)" value={data.dischargesInRange} />
          <StatTile label="Avg. Length of Stay (days)" value={data.averageLengthOfStayDays ?? '—'} />
        </div>
      )}
    </>
  )
}
