import { useState } from 'react'
import { errorMessage } from '../../api/axios'
import { Button } from '../../components/ui/Button'
import { PageHeader } from '../../components/ui/PageHeader'
import { Select } from '../../components/ui/Select'
import { Spinner } from '../../components/ui/Spinner'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useBookAppointment, useBranches, useCancelAppointment, useDoctors, useMyAppointments, useSlots } from '../features/appointments/api'
import { APPOINTMENT_STATUS_LABELS } from '../features/appointments/types'

export function PortalAppointmentsPage() {
  const { data: appointments, isLoading } = useMyAppointments()
  const { data: branches = [] } = useBranches()
  const { data: doctorsResult } = useDoctors()
  const doctors = doctorsResult?.items ?? []

  const [branchId, setBranchId] = useState('')
  const [doctorId, setDoctorId] = useState('')
  const [date, setDate] = useState('')
  const { data: slots = [], isLoading: slotsLoading } = useSlots(doctorId ? Number(doctorId) : null, date || null)

  const book = useBookAppointment()
  const cancel = useCancelAppointment()
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  const bookSlot = async (startTime: string) => {
    setError(null)
    setMessage(null)
    try {
      await book.mutateAsync({ branchId: Number(branchId), doctorId: Number(doctorId), appointmentDate: date, startTime, notes: null })
      setMessage('Appointment booked!')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const cancelAppointment = async (id: number) => {
    setError(null)
    try {
      await cancel.mutateAsync({ id, reason: 'Cancelled by patient' })
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader title="Appointments" subtitle="Book a new appointment or manage your upcoming ones" />
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      {message && <p className="mb-3 rounded-md bg-green-50 px-3 py-2 text-sm text-green-700">{message}</p>}

      <div className="mb-6 rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-3 text-sm font-semibold text-slate-700">Book a new appointment</h3>
        <div className="mb-3 grid grid-cols-1 gap-2 sm:grid-cols-3">
          <Select label="Branch" value={branchId} onChange={(e) => setBranchId(e.target.value)} placeholder="Select branch" options={branches.map((b) => ({ value: b.id, label: b.name }))} />
          <Select label="Doctor" value={doctorId} onChange={(e) => setDoctorId(e.target.value)} placeholder="Select doctor"
            options={doctors.map((d) => ({ value: d.id, label: `${d.fullName}${d.specialtyName ? ` (${d.specialtyName})` : ''}` }))} />
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">Date</span>
            <input type="date" value={date} onChange={(e) => setDate(e.target.value)} className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
          </label>
        </div>

        {branchId && doctorId && date && (
          <div>
            {slotsLoading && <Spinner />}
            <div className="flex flex-wrap gap-2">
              {slots.map((s) => (
                <button
                  key={s.startTime}
                  disabled={!s.isAvailable || book.isPending}
                  onClick={() => bookSlot(s.startTime)}
                  className={`rounded-md border px-3 py-1.5 text-sm ${
                    s.isAvailable ? 'border-primary-300 text-primary-700 hover:bg-primary-50' : 'cursor-not-allowed border-slate-200 text-slate-300'
                  }`}
                >
                  {s.startTime}
                </button>
              ))}
              {!slotsLoading && slots.length === 0 && <p className="text-sm text-slate-400">No slots for this date.</p>}
            </div>
          </div>
        )}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Time</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Doctor</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Token</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Actions</th>
            </tr>
          </thead>
          <tbody>
            {(appointments?.items ?? []).map((a) => (
              <tr key={a.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{a.appointmentDate}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.startTime}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.doctorName}</td>
                <td className="px-4 py-2.5 text-slate-700">#{a.tokenNumber}</td>
                <td className="px-4 py-2.5"><StatusBadge tone={a.status === 4 ? 'neutral' : a.status === 3 ? 'success' : 'info'}>{APPOINTMENT_STATUS_LABELS[a.status]}</StatusBadge></td>
                <td className="px-4 py-2.5">
                  {a.status === 1 && <Button variant="ghost" className="text-red-600" onClick={() => cancelAppointment(a.id)}>Cancel</Button>}
                </td>
              </tr>
            ))}
            {!isLoading && (appointments?.items ?? []).length === 0 && (
              <tr><td colSpan={6} className="px-4 py-10 text-center text-slate-400">No appointments yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}
