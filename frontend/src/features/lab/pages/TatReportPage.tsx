import { useState } from 'react'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useTatReport } from '../api'

function today() {
  return new Date().toISOString().slice(0, 10)
}

function monthAgo() {
  const d = new Date()
  d.setDate(d.getDate() - 30)
  return d.toISOString().slice(0, 10)
}

export function TatReportPage() {
  const [fromDate, setFromDate] = useState(monthAgo())
  const [toDate, setToDate] = useState(today())
  const [submittedRange, setSubmittedRange] = useState({ fromDate: monthAgo(), toDate: today() })

  const { data: rows = [], isLoading } = useTatReport(submittedRange.fromDate, submittedRange.toDate, true)

  const avgHours = rows.length > 0
    ? rows.reduce((sum, r) => sum + (r.turnaroundHours ?? 0), 0) / rows.filter((r) => r.turnaroundHours != null).length
    : null

  return (
    <>
      <PageHeader title="TAT Report" subtitle="Turnaround time from order to approval for completed tests" />

      <div className="mb-6 flex items-end gap-3 rounded-lg border border-slate-200 bg-white p-4">
        <Input label="From" type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
        <Input label="To" type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
        <Button onClick={() => setSubmittedRange({ fromDate, toDate })}>Run report</Button>
        {avgHours !== null && (
          <span className="ml-4 text-sm text-slate-600">
            Average TAT: <strong>{avgHours.toFixed(1)} hrs</strong> across {rows.length} tests
          </span>
        )}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Order No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Test</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Order Date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Collected</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Received</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Approved</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">TAT (hrs)</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.labOrderItemId} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{r.orderNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.labTestName}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.orderDate}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.collectedAt ? new Date(r.collectedAt).toLocaleString() : '—'}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.receivedAt ? new Date(r.receivedAt).toLocaleString() : '—'}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.approvedAt ? new Date(r.approvedAt).toLocaleString() : '—'}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.turnaroundHours?.toFixed(1) ?? '—'}</td>
              </tr>
            ))}
            {!isLoading && rows.length === 0 && (
              <tr>
                <td colSpan={7} className="px-4 py-10 text-center text-slate-400">
                  No completed tests in this range.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}
