import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import {
  compactErrors,
  hasErrors,
  maxLengthError,
  requiredSelect,
  requiredText,
  type FieldErrors,
} from '../../../lib/formValidation'
import { useAuth } from '../../auth/AuthContext'
import { useAdmissions } from '../../ipd/api'
import { useDoctors } from '../../masters/api'
import { useBookings, useCreateBooking, useTheatres } from '../api'
import { SURGERY_STATUS_LABELS, type BookingListItem, type SurgeryStatus } from '../types'

const statusTone: Record<SurgeryStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info', 2: 'warning', 3: 'success', 4: 'danger', 5: 'neutral',
}

export function BookingsPage() {
  const { hasPermission } = useAuth()
  const canCreate = hasPermission('OT.BOOKING.CREATE')
  const navigate = useNavigate()
  const [status, setStatus] = useState('')
  const { data: bookings = [], isLoading } = useBookings({ status: status ? (Number(status) as SurgeryStatus) : undefined })
  const [creating, setCreating] = useState(false)

  return (
    <>
      <PageHeader
        title="Surgery Bookings"
        subtitle="Theatre/surgeon/anaesthetist-conflict-checked OT scheduling"
        actions={canCreate && <Button onClick={() => setCreating(true)}>New booking</Button>}
      />

      <div className="mb-4 flex gap-3">
        <Select
          label="Status"
          placeholder="All"
          value={status}
          onChange={(e) => setStatus(e.target.value)}
          options={Object.entries(SURGERY_STATUS_LABELS).map(([value, label]) => ({ value, label }))}
        />
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
              <th className="px-4 py-2.5 font-medium text-slate-600">Scheduled</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            </tr>
          </thead>
          <tbody>
            {bookings.map((b: BookingListItem) => (
              <tr
                key={b.id}
                className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50"
                onClick={() => navigate(`/ot/bookings/${b.id}`)}
              >
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{b.bookingNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{b.patientName}</td>
                <td className="px-4 py-2.5 text-slate-700">{b.procedureName}</td>
                <td className="px-4 py-2.5 text-slate-700">{b.theatreName}</td>
                <td className="px-4 py-2.5 text-slate-700">{b.surgeonName}</td>
                <td className="px-4 py-2.5 text-slate-700">{new Date(b.scheduledStart).toLocaleString()}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={statusTone[b.status]}>{SURGERY_STATUS_LABELS[b.status]}</StatusBadge>
                </td>
              </tr>
            ))}
            {!isLoading && bookings.length === 0 && (
              <tr>
                <td colSpan={7} className="px-4 py-10 text-center text-slate-400">
                  No bookings yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <CreateModal onClose={() => setCreating(false)} />}
    </>
  )
}

function CreateModal({ onClose }: { onClose: () => void }) {
  const create = useCreateBooking()
  const navigate = useNavigate()
  const { data: admissionsResult } = useAdmissions({ page: 1, pageSize: 200, status: 1 })
  const { data: theatres = [] } = useTheatres()
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const [admissionId, setAdmissionId] = useState('')
  const [theatreId, setTheatreId] = useState('')
  const [procedureName, setProcedureName] = useState('')
  const [surgeonId, setSurgeonId] = useState('')
  const [anaesthetistId, setAnaesthetistId] = useState('')
  const [scheduledStart, setScheduledStart] = useState('')
  const [scheduledEnd, setScheduledEnd] = useState('')
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      admissionId: requiredSelect(admissionId, 'Admission'),
      theatreId: requiredSelect(theatreId, 'Theatre'),
      procedureName: requiredText(procedureName, 'Procedure') ?? maxLengthError(procedureName, 300, 'Procedure'),
      surgeonId: requiredSelect(surgeonId, 'Surgeon'),
      scheduledStart: requiredText(scheduledStart, 'Scheduled start'),
      // Mirrors `RuleFor(x => x.ScheduledEnd).GreaterThan(x => x.ScheduledStart)` — without this
      // the conflict check runs server-side on an inverted window and rejects with a bare 400.
      scheduledEnd:
        requiredText(scheduledEnd, 'Scheduled end') ??
        (scheduledStart && scheduledEnd && new Date(scheduledEnd) <= new Date(scheduledStart)
          ? 'Scheduled end must be after the start.'
          : undefined),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      const id = await create.mutateAsync({
        admissionId: Number(admissionId),
        theatreId: Number(theatreId),
        procedureName,
        surgeonId: Number(surgeonId),
        anaesthetistId: anaesthetistId ? Number(anaesthetistId) : null,
        scheduledStart: new Date(scheduledStart).toISOString(),
        scheduledEnd: new Date(scheduledEnd).toISOString(),
        notes: notes || null,
      })
      onClose()
      navigate(`/ot/bookings/${id}`)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="New surgery booking"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={create.isPending}>Book</Button>
        </>
      }
    >
      <div className="max-h-[65vh] space-y-3 overflow-y-auto">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Select
          label="Admission"
          required
          error={fieldErrors.admissionId}
          placeholder="Select…"
          value={admissionId}
          onChange={(e) => setAdmissionId(e.target.value)}
          options={(admissionsResult?.items ?? []).map((a) => ({ value: a.id, label: `${a.admissionNumber} — ${a.patientName}` }))}
        />
        <Input
          label="Procedure"
          required
          error={fieldErrors.procedureName}
          value={procedureName}
          onChange={(e) => setProcedureName(e.target.value)}
        />
        <div className="grid grid-cols-2 gap-3">
          <Select
            label="Theatre"
            required
            error={fieldErrors.theatreId}
            placeholder="Select…"
            value={theatreId}
            onChange={(e) => setTheatreId(e.target.value)}
            options={theatres.map((t) => ({ value: t.id, label: t.name }))}
          />
          <Select
            label="Surgeon"
            required
            error={fieldErrors.surgeonId}
            placeholder="Select…"
            value={surgeonId}
            onChange={(e) => setSurgeonId(e.target.value)}
            options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
          />
          <Select
            label="Anaesthetist (optional)"
            placeholder="Not assigned yet"
            value={anaesthetistId}
            onChange={(e) => setAnaesthetistId(e.target.value)}
            options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
          />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <Input
            label="Scheduled start"
            required
            error={fieldErrors.scheduledStart}
            type="datetime-local"
            value={scheduledStart}
            onChange={(e) => setScheduledStart(e.target.value)}
          />
          <Input
            label="Scheduled end"
            required
            error={fieldErrors.scheduledEnd}
            type="datetime-local"
            value={scheduledEnd}
            onChange={(e) => setScheduledEnd(e.target.value)}
          />
        </div>
        <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>
    </Modal>
  )
}
