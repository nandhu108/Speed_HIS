import { useState } from 'react'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useConsumptionReport } from '../api'

function today() {
  return new Date().toISOString().slice(0, 10)
}
function monthAgo() {
  const d = new Date()
  d.setDate(d.getDate() - 30)
  return d.toISOString().slice(0, 10)
}

export function ConsumptionReportPage() {
  const [fromDate, setFromDate] = useState(monthAgo())
  const [toDate, setToDate] = useState(today())
  const [submitted, setSubmitted] = useState({ fromDate: monthAgo(), toDate: today() })
  const { data: rows = [], isLoading } = useConsumptionReport(submitted.fromDate, submitted.toDate, true)

  return (
    <>
      <PageHeader title="Consumption Report" subtitle="Net quantity sold and issued per drug/item, department-wise, over a date range" />

      <div className="mb-6 flex items-end gap-3 rounded-lg border border-slate-200 bg-white p-4">
        <Input label="From" type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
        <Input label="To" type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
        <Button onClick={() => setSubmitted({ fromDate, toDate })}>Run report</Button>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Product</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Department</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Sold</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Issued</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r, i) => (
              <tr key={`${r.drugId ?? 'i'}-${r.itemId ?? 'd'}-${r.departmentName ?? i}`} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{r.productName}</td>
                <td className="px-4 py-2.5 text-slate-500">{r.departmentName ?? '—'}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.quantitySold}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.quantityIssued}</td>
              </tr>
            ))}
            {!isLoading && rows.length === 0 && (
              <tr>
                <td colSpan={4} className="px-4 py-10 text-center text-slate-400">
                  No consumption in this range.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}
