import { useState } from 'react'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useOtRegister } from '../api'
import { SURGERY_STATUS_LABELS, type SurgeryStatus } from '../types'

function today() {
  return new Date().toISOString().slice(0, 10)
}
function monthAgo() {
  const d = new Date()
  d.setDate(d.getDate() - 30)
  return d.toISOString().slice(0, 10)
}

const statusTone: Record<SurgeryStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info', 2: 'warning', 3: 'success', 4: 'danger', 5: 'neutral',
}

export function OtRegisterPage() {
  const [fromDate, setFromDate] = useState(monthAgo())
  const [toDate, setToDate] = useState(today())
  const [submitted, setSubmitted] = useState({ fromDate: monthAgo(), toDate: today() })
  const { data: rows = [], isLoading } = useOtRegister(submitted.fromDate, submitted.toDate, true)

  return (
    <>
      <PageHeader title="OT Register" subtitle="Statutory register — every surgery booked in a date range" />

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
              <th className="px-4 py-2.5 font-medium text-slate-600">Booking No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Procedure</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Theatre</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Surgeon</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Anaesthetist</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Scheduled</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Actual</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.bookingId} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{r.bookingNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.patientName}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.procedureName}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.theatreName}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.surgeonName}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.anaesthetistName ?? '—'}</td>
                <td className="px-4 py-2.5 text-slate-700">{new Date(r.scheduledStart).toLocaleString()}</td>
                <td className="px-4 py-2.5 text-slate-700">
                  {r.actualStart ? new Date(r.actualStart).toLocaleTimeString() : '—'}
                  {r.actualEnd ? ` – ${new Date(r.actualEnd).toLocaleTimeString()}` : ''}
                </td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={statusTone[r.status]}>{SURGERY_STATUS_LABELS[r.status]}</StatusBadge>
                </td>
              </tr>
            ))}
            {!isLoading && rows.length === 0 && (
              <tr>
                <td colSpan={9} className="px-4 py-10 text-center text-slate-400">
                  No surgeries in this range.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}
