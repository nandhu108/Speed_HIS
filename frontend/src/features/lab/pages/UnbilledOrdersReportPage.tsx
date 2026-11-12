import { useState } from 'react'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useUnbilledOrdersReport } from '../api'
import { ITEM_STATUS_LABELS } from '../types'

function today() {
  return new Date().toISOString().slice(0, 10)
}

function monthAgo() {
  const d = new Date()
  d.setDate(d.getDate() - 30)
  return d.toISOString().slice(0, 10)
}

/**
 * LabOrderItem.BillItemId is a soft, unenforced link — a test can be resulted and even
 * approved with no bill line ever attached to it. This report doesn't change that (see the
 * entity's own doc comment); it just makes the gap visible instead of silent, closing
 * docs/PATIENT_JOURNEY_GAP_ANALYSIS.md §4.3 the advisory way.
 */
export function UnbilledOrdersReportPage() {
  const [fromDate, setFromDate] = useState(monthAgo())
  const [toDate, setToDate] = useState(today())
  const [submittedRange, setSubmittedRange] = useState({ fromDate: monthAgo(), toDate: today() })

  const { data: rows = [], isLoading } = useUnbilledOrdersReport(submittedRange.fromDate, submittedRange.toDate, true)

  return (
    <>
      <PageHeader
        title="Unbilled Orders Report"
        subtitle="Tests with a result entered (or further along) that still have no billed line attached — nothing here is blocked, this is a visibility check only."
      />

      <div className="mb-6 flex items-end gap-3 rounded-lg border border-slate-200 bg-white p-4">
        <Input label="From" type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
        <Input label="To" type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
        <Button onClick={() => setSubmittedRange({ fromDate, toDate })}>Run report</Button>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Order No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Order Date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Test</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.labOrderItemId} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{r.orderNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.orderDate}</td>
                <td className="px-4 py-2.5 text-slate-700">
                  {r.patientName} ({r.patientUhid})
                </td>
                <td className="px-4 py-2.5 text-slate-700">{r.labTestName}</td>
                <td className="px-4 py-2.5 text-slate-700">{ITEM_STATUS_LABELS[r.status]}</td>
              </tr>
            ))}
            {!isLoading && rows.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                  Nothing unbilled in this range.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}
