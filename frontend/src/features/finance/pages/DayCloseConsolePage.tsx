import { useState } from 'react'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useDayCloseConsole } from '../api'

export function DayCloseConsolePage() {
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10))
  const { data: rows = [], isLoading } = useDayCloseConsole(date)

  return (
    <>
      <PageHeader title="Day Close Console" subtitle="Every branch's day-close status for one date, in one place" />

      <Input label="Date" type="date" value={date} onChange={(e) => setDate(e.target.value)} className="mb-4 max-w-xs" />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Branch</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">System Total</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Declared Total</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Variance</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.branchId} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{r.branchName}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={r.isClosed ? 'success' : 'warning'}>{r.isClosed ? 'Closed' : 'Open'}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.systemTotal !== null ? r.systemTotal.toFixed(2) : '—'}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.declaredTotal !== null ? r.declaredTotal.toFixed(2) : '—'}</td>
                <td className={`px-4 py-2.5 text-right font-medium ${r.variance ? (r.variance < 0 ? 'text-red-600' : 'text-amber-600') : 'text-slate-700'}`}>
                  {r.variance !== null ? r.variance.toFixed(2) : '—'}
                </td>
              </tr>
            ))}
            {!isLoading && rows.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">No branches found.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}
