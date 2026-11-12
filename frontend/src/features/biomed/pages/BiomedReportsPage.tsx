import { useState } from 'react'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useDowntimeReport, usePmComplianceReport } from '../api'

type Tab = 'downtime' | 'pm-compliance'

export function BiomedReportsPage() {
  const [tab, setTab] = useState<Tab>('downtime')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const range = { fromDate: fromDate || undefined, toDate: toDate || undefined }

  const tabs: { key: Tab; label: string }[] = [
    { key: 'downtime', label: 'Equipment Downtime' },
    { key: 'pm-compliance', label: 'PM Compliance' },
  ]

  return (
    <>
      <PageHeader title="Biomedical Reports" subtitle="Equipment downtime and preventive-maintenance compliance" />

      <div className="mb-4 flex flex-wrap items-end gap-2">
        <Input label="From date" type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
        <Input label="To date" type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
      </div>

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

      {tab === 'downtime' && <DowntimeTab range={range} />}
      {tab === 'pm-compliance' && <PmComplianceTab range={range} />}
    </>
  )
}

function DowntimeTab({ range }: { range: { fromDate?: string; toDate?: string } }) {
  const { data: rows = [], isLoading } = useDowntimeReport(range)
  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      {isLoading && <Spinner />}
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50 text-left">
            <th className="px-4 py-2.5 font-medium text-slate-600">Equipment</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Complaints</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Total Downtime (min)</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r) => (
            <tr key={r.equipmentId} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-2.5 text-slate-700">{r.equipmentName}</td>
              <td className="px-4 py-2.5 text-right text-slate-700">{r.complaintCount}</td>
              <td className="px-4 py-2.5 text-right font-medium text-slate-800">{r.totalDowntimeMinutes}</td>
            </tr>
          ))}
          {!isLoading && rows.length === 0 && <tr><td colSpan={3} className="px-4 py-10 text-center text-slate-400">No resolved complaints in this range.</td></tr>}
        </tbody>
      </table>
    </div>
  )
}

function PmComplianceTab({ range }: { range: { fromDate?: string; toDate?: string } }) {
  const { data: rows = [], isLoading } = usePmComplianceReport(range)
  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      {isLoading && <Spinner />}
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50 text-left">
            <th className="px-4 py-2.5 font-medium text-slate-600">Equipment</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Completed</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">On Time</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Compliance %</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r) => (
            <tr key={r.equipmentId} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-2.5 text-slate-700">{r.equipmentName}</td>
              <td className="px-4 py-2.5 text-right text-slate-700">{r.totalCompleted}</td>
              <td className="px-4 py-2.5 text-right text-slate-700">{r.completedOnTime}</td>
              <td className="px-4 py-2.5 text-right font-medium text-slate-800">{r.compliancePercent}%</td>
            </tr>
          ))}
          {!isLoading && rows.length === 0 && <tr><td colSpan={4} className="px-4 py-10 text-center text-slate-400">No completed PM work orders in this range.</td></tr>}
        </tbody>
      </table>
    </div>
  )
}
