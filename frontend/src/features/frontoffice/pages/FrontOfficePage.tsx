import { ArrowRight, Bed, CalendarClock, ClipboardList, Siren, UserPlus } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { compactErrors, hasErrors, requiredSelect, type FieldErrors } from '../../../lib/formValidation'
import { PatientPicker } from '../../appointments/components/PatientPicker'
import { useAppointments, useCheckInAppointment } from '../../appointments/api'
import { APPOINTMENT_STATUS_LABELS, type AppointmentStatus } from '../../appointments/types'
import { useAuth } from '../../auth/AuthContext'
import { useEmergencyQueue } from '../../er/api'
import { useBedOccupancy } from '../../ipd/api'
import { doctorsForDepartment, useClinicalDepartments, useDoctors } from '../../masters/api'
import { useOpVisits, useRegisterOpVisit } from '../../opd/api'
import type { PatientListItem } from '../../patients/types'

const today = () => new Date().toISOString().slice(0, 10)

const appointmentStatusTone: Record<AppointmentStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info', 2: 'success', 3: 'neutral', 4: 'danger', 5: 'warning',
}

export function FrontOfficePage() {
  const { hasPermission } = useAuth()
  const navigate = useNavigate()

  const canViewOp = hasPermission('OPD.VISIT.VIEW')
  const canViewAppt = hasPermission('APPOINTMENT.APPOINTMENT.VIEW')
  const canCheckIn = hasPermission('APPOINTMENT.APPOINTMENT.CHECKIN')
  const canViewEr = hasPermission('EMERGENCY.VISIT.VIEW')
  const canViewBeds = hasPermission('IPD.BED.VIEW')
  const canRegisterVisit = hasPermission('OPD.VISIT.CREATE')
  const canRegisterPatient = hasPermission('PATIENT.PATIENT.CREATE')

  return (
    <>
      <PageHeader title="Front Office" subtitle="Daily reception workspace" />

      <div className="mb-6 grid grid-cols-1 gap-3.5 sm:grid-cols-2 lg:grid-cols-4">
        {canViewOp && <OpWaitingTile onClick={() => navigate('/opd')} />}
        {canViewAppt && <AppointmentsTile onClick={() => navigate('/appointments')} />}
        {canViewEr && <ErQueueTile onClick={() => navigate('/emergency/queue')} />}
        {canViewBeds && <BedOccupancyTile onClick={() => navigate('/ipd/bed-board')} />}
      </div>

      <div className="grid grid-cols-1 gap-5 xl:grid-cols-2">
        {canRegisterVisit && <QuickVisitPanel canRegisterPatient={canRegisterPatient} />}
        {canViewAppt && <TodaysAppointmentsPanel canCheckIn={canCheckIn} />}
      </div>

      <div className="mt-6 flex flex-wrap gap-2.5">
        {canViewOp && <QuickLink label="OPD Front Desk" onClick={() => navigate('/opd')} />}
        {canViewBeds && <QuickLink label="Live Ward Map" onClick={() => navigate('/ipd/bed-board')} />}
        {canViewEr && <QuickLink label="ER Queue" onClick={() => navigate('/emergency/queue')} />}
        {hasPermission('DASHBOARD.OP.VIEW') && <QuickLink label="OP Dashboard" onClick={() => navigate('/reports/op')} />}
        {hasPermission('DASHBOARD.IP.VIEW') && <QuickLink label="IP Dashboard" onClick={() => navigate('/reports/ip')} />}
        {hasPermission('DASHBOARD.BILLING.VIEW') && <QuickLink label="Billing Dashboard" onClick={() => navigate('/reports/billing')} />}
      </div>
    </>
  )
}

function StatCard({
  label, value, icon: Icon, onClick,
}: { label: string; value: string | number; icon: typeof Bed; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="rounded-shell border border-border-soft bg-panel p-4 text-left shadow-shell-sm transition-all hover:-translate-y-0.5 hover:border-border-strong hover:shadow-shell-md"
    >
      <div className="flex items-start justify-between">
        <p className="text-[11.5px] font-semibold text-ink-muted">{label}</p>
        <Icon className="size-4 text-accent-2" aria-hidden />
      </div>
      <p className="font-mono mt-1.5 text-[26px] font-semibold tracking-tight text-ink">{value}</p>
    </button>
  )
}

// Each tile owns its own query, gated by mounting only when the caller holds the matching
// permission — an ungated shared query would fire an unauthorized (403) request otherwise.
function OpWaitingTile({ onClick }: { onClick: () => void }) {
  const { data } = useOpVisits({ page: 1, pageSize: 1, status: 1, date: today() })
  return <StatCard label="OPD Waiting" value={data?.totalCount ?? '—'} icon={ClipboardList} onClick={onClick} />
}

function AppointmentsTile({ onClick }: { onClick: () => void }) {
  const { data } = useAppointments({ page: 1, pageSize: 8, date: today() })
  return <StatCard label="Today's Appointments" value={data?.totalCount ?? '—'} icon={CalendarClock} onClick={onClick} />
}

function ErQueueTile({ onClick }: { onClick: () => void }) {
  const { data } = useEmergencyQueue({})
  return <StatCard label="ER Queue" value={data?.length ?? '—'} icon={Siren} onClick={onClick} />
}

function BedOccupancyTile({ onClick }: { onClick: () => void }) {
  const { data } = useBedOccupancy()
  return <StatCard label="Bed Occupancy" value={data ? `${data.occupied}/${data.totalBeds}` : '—'} icon={Bed} onClick={onClick} />
}

function QuickLink({ label, onClick }: { label: string; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="flex items-center gap-1.5 rounded-shell-sm border border-border-soft bg-panel-2 px-3.5 py-2 text-[12.5px] font-semibold text-ink-muted hover:border-border-strong hover:text-ink"
    >
      {label}
      <ArrowRight className="size-3.5" aria-hidden />
    </button>
  )
}

function QuickVisitPanel({ canRegisterPatient }: { canRegisterPatient: boolean }) {
  const navigate = useNavigate()
  const [patient, setPatient] = useState<PatientListItem | null>(null)
  const [departmentId, setDepartmentId] = useState('')
  const [doctorId, setDoctorId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const { data: clinicalDepartments = [] } = useClinicalDepartments()
  const departmentName = clinicalDepartments.find((d) => String(d.id) === departmentId)?.name
  const doctorOptions = doctorsForDepartment(doctorsResult?.items ?? [], departmentName)
  const register = useRegisterOpVisit()

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      patient: patient ? undefined : 'Select a patient.',
      departmentId: requiredSelect(departmentId, 'Department'),
      doctorId: requiredSelect(doctorId, 'Doctor'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      const visit = await register.mutateAsync({
        patientId: patient!.id, departmentId: Number(departmentId), doctorId: Number(doctorId), appointmentId: null,
      })
      navigate(`/opd/visits/${visit.id}`)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="rounded-shell border border-border-soft bg-panel p-5 shadow-shell-sm">
      <div className="mb-4 flex items-center justify-between">
        <p className="text-[14.5px] font-semibold text-ink">Register OPD Visit</p>
        {canRegisterPatient && (
          <button
            type="button"
            onClick={() => navigate('/patients/new')}
            className="flex items-center gap-1 text-[12px] font-semibold text-accent-2 hover:underline"
          >
            <UserPlus className="size-3.5" aria-hidden />
            New patient
          </button>
        )}
      </div>
      {error && <p className="mb-3 rounded-shell-sm bg-crit/10 px-3 py-2 text-sm text-crit">{error}</p>}
      <div className="flex flex-col gap-3">
        <div>
          <PatientPicker selected={patient} onSelect={setPatient} />
          {fieldErrors.patient && (
            <p role="alert" className="mt-1 text-xs font-medium text-crit">
              {fieldErrors.patient}
            </p>
          )}
        </div>
        <Select
          label="Department"
          required
          error={fieldErrors.departmentId}
          placeholder="Select a department…"
          options={clinicalDepartments.map((d) => ({ value: d.id, label: d.name }))}
          value={departmentId}
          onChange={(e) => {
            setDepartmentId(e.target.value)
            setDoctorId('')
          }}
        />
        <Select
          label="Doctor"
          required
          error={fieldErrors.doctorId}
          placeholder="Select a doctor…"
          options={doctorOptions.map((d) => ({ value: d.id, label: d.fullName }))}
          value={doctorId}
          onChange={(e) => setDoctorId(e.target.value)}
        />
        <Button loading={register.isPending} onClick={submit} className="justify-center">
          Register visit
        </Button>
      </div>
    </div>
  )
}

function TodaysAppointmentsPanel({ canCheckIn }: { canCheckIn: boolean }) {
  const { data } = useAppointments({ page: 1, pageSize: 8, date: today() })
  const checkIn = useCheckInAppointment()
  const appointments = data?.items ?? []

  return (
    <div className="rounded-shell border border-border-soft bg-panel p-5 shadow-shell-sm">
      <p className="mb-4 text-[14.5px] font-semibold text-ink">Today's Appointments</p>
      <div className="flex flex-col gap-2">
        {appointments.length === 0 && <p className="py-6 text-center text-sm text-ink-faint">No appointments today.</p>}
        {appointments.map((a) => (
          <div key={a.id} className="flex items-center gap-3 rounded-shell-sm border border-border-soft bg-panel-2 px-3 py-2.5">
            <span className="font-mono text-xs text-ink-faint">#{a.tokenNumber}</span>
            <div className="min-w-0 flex-1">
              <p className="truncate text-[13px] font-medium text-ink">{a.patientName}</p>
              <p className="truncate text-[11px] text-ink-faint">{a.doctorName} · {a.startTime}</p>
            </div>
            <StatusBadge tone={appointmentStatusTone[a.status]}>{APPOINTMENT_STATUS_LABELS[a.status]}</StatusBadge>
            {canCheckIn && a.status === 1 && (
              <Button variant="secondary" onClick={() => checkIn.mutate(a.id)} loading={checkIn.isPending}>
                Check in
              </Button>
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
