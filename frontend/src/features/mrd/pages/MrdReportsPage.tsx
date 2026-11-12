import { useState } from 'react'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useCodingStats, useFileMovementReport, useIncompleteRecords } from '../api'

type Tab = 'coding' | 'movement' | 'incomplete'

export function MrdReportsPage() {
  const [tab, setTab] = useState<Tab>('coding')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const range = { fromDate: fromDate || undefined, toDate: toDate || undefined }

  const tabs: { key: Tab; label: string }[] = [
    { key: 'coding', label: 'Coding Stats' },
    { key: 'movement', label: 'File Movement' },
    { key: 'incomplete', label: 'Incomplete Records' },
  ]

  return (
    <>
      <PageHeader title="MRD Reports" subtitle="Coding completeness, file movement history and incomplete records" />

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

      {tab === 'coding' && <CodingStatsTab range={range} />}
      {tab === 'movement' && <FileMovementTab range={range} />}
      {tab === 'incomplete' && <IncompleteRecordsTab range={range} />}
    </>
  )
}

function CodingStatsTab({ range }: { range: { fromDate?: string; toDate?: string } }) {
  const { data: stats, isLoading } = useCodingStats(range)
  if (isLoading || !stats) return <Spinner />

  return (
    <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
      <Stat label="Total discharged" value={stats.totalDischarged} />
      <Stat label="Coded" value={stats.coded} />
      <Stat label="Pending" value={stats.pending} />
      <Stat label="Coded %" value={`${stats.codedPercent}%`} />
    </div>
  )
}

function Stat({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="text-xs font-medium uppercase tracking-wide text-slate-400">{label}</div>
      <div className="mt-1 text-2xl font-semibold text-slate-800">{value}</div>
    </div>
  )
}

function FileMovementTab({ range }: { range: { fromDate?: string; toDate?: string } }) {
  const { data: rows = [], isLoading } = useFileMovementReport(range)
  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      {isLoading && <Spinner />}
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50 text-left">
            <th className="px-4 py-2.5 font-medium text-slate-600">Moved At</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">File No</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Movement</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Moved By</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r, i) => (
            <tr key={i} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-2.5 text-slate-500">{new Date(r.movedAt).toLocaleString()}</td>
              <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{r.fileNumber}</td>
              <td className="px-4 py-2.5 text-slate-700">{r.patientName}</td>
              <td className="px-4 py-2.5 text-slate-700">{r.fromLocation} → {r.toLocation}</td>
              <td className="px-4 py-2.5 text-slate-700">{r.movedByName}</td>
            </tr>
          ))}
          {!isLoading && rows.length === 0 && (
            <tr>
              <td colSpan={5} className="px-4 py-10 text-center text-slate-400">No movements in this range.</td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  )
}

function IncompleteRecordsTab({ range }: { range: { fromDate?: string; toDate?: string } }) {
  const { data: rows = [], isLoading } = useIncompleteRecords(range)
  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      {isLoading && <Spinner />}
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50 text-left">
            <th className="px-4 py-2.5 font-medium text-slate-600">Admission</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Discharged</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">File</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Audit</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Coding</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r) => (
            <tr key={r.admissionId} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{r.admissionNumber}</td>
              <td className="px-4 py-2.5 text-slate-700">{r.patientName}</td>
              <td className="px-4 py-2.5 text-slate-700">{r.dischargedAt ? new Date(r.dischargedAt).toLocaleDateString() : '—'}</td>
              <td className="px-4 py-2.5"><StatusBadge tone={r.hasRecordFile ? 'success' : 'danger'}>{r.hasRecordFile ? 'Opened' : 'Missing'}</StatusBadge></td>
              <td className="px-4 py-2.5"><StatusBadge tone={r.auditComplete ? 'success' : 'danger'}>{r.auditComplete ? 'Complete' : 'Incomplete'}</StatusBadge></td>
              <td className="px-4 py-2.5"><StatusBadge tone={r.isCoded ? 'success' : 'warning'}>{r.isCoded ? 'Coded' : 'Pending'}</StatusBadge></td>
            </tr>
          ))}
          {!isLoading && rows.length === 0 && (
            <tr>
              <td colSpan={6} className="px-4 py-10 text-center text-slate-400">No incomplete records.</td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  )
}
