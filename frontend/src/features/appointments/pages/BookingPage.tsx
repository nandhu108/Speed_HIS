import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { compactErrors, hasErrors, requiredSelect, requiredText, type FieldErrors } from '../../../lib/formValidation'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { useAppointmentTypes, useDoctors } from '../../masters/api'
import type { PatientListItem } from '../../patients/types'
import { PatientPicker } from '../components/PatientPicker'
import { useBookAppointment, useSlots } from '../api'
import { BOOKING_TYPES, CONSULTATION_MODE_LABELS, CONSULTATION_MODES, type ConsultationMode } from '../types'

function today() {
  return new Date().toISOString().slice(0, 10)
}

export function BookingPage() {
  const navigate = useNavigate()
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const { data: appointmentTypes = [] } = useAppointmentTypes()
  const [patient, setPatient] = useState<PatientListItem | null>(null)
  const [doctorId, setDoctorId] = useState('')
  const [date, setDate] = useState(today())
  const [appointmentTypeId, setAppointmentTypeId] = useState('')
  const [notes, setNotes] = useState('')
  const [consultationMode, setConsultationMode] = useState<ConsultationMode>(CONSULTATION_MODES.InPerson)
  const [teleconsultLink, setTeleconsultLink] = useState('')
  const [selectedSlot, setSelectedSlot] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const { data: slots, isLoading: slotsLoading } = useSlots(doctorId ? Number(doctorId) : null, date || null)
  const book = useBookAppointment()

  /** Mirrors BookAppointmentRequestValidator, plus the date/slot the booking engine needs. */
  const validate = (): FieldErrors =>
    compactErrors({
      patient: patient ? undefined : 'Select a patient.',
      doctorId: requiredSelect(doctorId, 'Doctor'),
      date: requiredText(date, 'Date'),
      selectedSlot: selectedSlot ? undefined : 'Pick an available slot.',
    })

  const submit = async (bookingType: (typeof BOOKING_TYPES)[keyof typeof BOOKING_TYPES]) => {
    setError(null)
    const errors = validate()
    setFieldErrors(errors)
    if (hasErrors(errors)) {
      setError('Check the highlighted fields below.')
      return
    }
    try {
      const result = await book.mutateAsync({
        patientId: patient!.id,
        doctorId: Number(doctorId),
        appointmentDate: date,
        startTime: selectedSlot!,
        bookingType,
        notes: notes || null,
        appointmentTypeId: appointmentTypeId ? Number(appointmentTypeId) : null,
        consultationMode,
        teleconsultLink: consultationMode === CONSULTATION_MODES.TeleConsult ? teleconsultLink || null : null,
      })
      navigate(`/appointments?highlight=${result.id}`)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader title="Book Appointment" subtitle="Search a patient, pick a doctor and an available slot" />

      <div className="max-w-2xl space-y-4 rounded-lg border border-slate-200 bg-white p-6">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

        <div>
          <PatientPicker selected={patient} onSelect={setPatient} />
          {fieldErrors.patient && (
            <p role="alert" className="mt-1 text-xs font-medium text-crit">
              {fieldErrors.patient}
            </p>
          )}
        </div>

        <div className="grid grid-cols-2 gap-3">
          <Select
            label="Doctor"
            required
            error={fieldErrors.doctorId}
            placeholder="Select a doctor…"
            options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
            value={doctorId}
            onChange={(e) => {
              setDoctorId(e.target.value)
              setSelectedSlot(null)
            }}
          />
          <Input
            label="Date"
            type="date"
            required
            error={fieldErrors.date}
            min={today()}
            value={date}
            onChange={(e) => {
              setDate(e.target.value)
              setSelectedSlot(null)
            }}
          />
        </div>

        {doctorId && date && (
          <div>
            <span className="mb-1 block text-sm font-medium text-slate-700">
              Available slots
              <span className="ml-0.5 text-crit" title="Required">
                *
              </span>
            </span>
            {slotsLoading && <Spinner />}
            {!slotsLoading && (slots?.length ?? 0) === 0 && (
              <p className="text-sm text-slate-400">The doctor has no availability on this date.</p>
            )}
            <div className="flex flex-wrap gap-2">
              {(slots ?? []).map((slot) => (
                <button
                  key={slot.startTime}
                  type="button"
                  disabled={!slot.isAvailable}
                  onClick={() => setSelectedSlot(slot.startTime)}
                  className={`rounded-md border px-3 py-1.5 text-sm ${
                    !slot.isAvailable
                      ? 'cursor-not-allowed border-slate-100 bg-slate-50 text-slate-300 line-through'
                      : selectedSlot === slot.startTime
                        ? 'border-primary-500 bg-primary-50 text-primary-700'
                        : 'border-slate-300 text-slate-700 hover:bg-slate-50'
                  }`}
                >
                  {slot.startTime.slice(0, 5)}
                </button>
              ))}
            </div>
            {fieldErrors.selectedSlot && (
              <p role="alert" className="mt-1 text-xs font-medium text-crit">
                {fieldErrors.selectedSlot}
              </p>
            )}
          </div>
        )}

        <Select
          label="Appointment Type (optional)"
          placeholder="None"
          options={appointmentTypes.filter((t) => t.isActive).map((t) => ({ value: t.id, label: t.name }))}
          value={appointmentTypeId}
          onChange={(e) => setAppointmentTypeId(e.target.value)}
        />

        <Select
          label="Consultation Mode"
          options={Object.entries(CONSULTATION_MODE_LABELS).map(([value, label]) => ({ value, label }))}
          value={consultationMode}
          onChange={(e) => setConsultationMode(Number(e.target.value) as ConsultationMode)}
        />
        {consultationMode === CONSULTATION_MODES.TeleConsult && (
          <Input
            label="Meeting link (optional — can be added later)"
            placeholder="https://…"
            value={teleconsultLink}
            onChange={(e) => setTeleconsultLink(e.target.value)}
          />
        )}

        <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />

        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button
            variant="secondary"
            loading={book.isPending}
            onClick={() => submit(BOOKING_TYPES.WalkIn)}
          >
            Book as walk-in
          </Button>
          <Button
            loading={book.isPending}
            onClick={() => submit(BOOKING_TYPES.Booked)}
          >
            Book appointment
          </Button>
        </div>
      </div>
    </>
  )
}
