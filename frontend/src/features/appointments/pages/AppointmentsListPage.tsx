import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDoctors } from '../../masters/api'
import {
  useAppointments,
  useCancelAppointment,
  useCheckInAppointment,
  useRecallToken,
  useRescheduleAppointment,
  useSetTeleconsultLink,
  useSkipToken,
  useSlots,
  useTransferToken,
} from '../api'
import {
  APPOINTMENT_STATUS_LABELS,
  APPOINTMENT_STATUSES,
  BOOKING_TYPE_LABELS,
  CONSULTATION_MODES,
  type AppointmentListItem,
  type AppointmentStatus,
} from '../types'

const statusTone: Record<AppointmentStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info',
  2: 'success',
  3: 'neutral',
  4: 'danger',
  5: 'warning',
}

export function AppointmentsListPage() {
  const { hasPermission } = useAuth()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [doctorId, setDoctorId] = useState('')
  const [date, setDate] = useState('')
  const [status, setStatus] = useState('')

  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const { data, isLoading } = useAppointments({
    page,
    pageSize: 20,
    search: search || undefined,
    doctorId: doctorId ? Number(doctorId) : undefined,
    date: date || undefined,
    status: status ? (Number(status) as AppointmentStatus) : undefined,
  })

  const checkIn = useCheckInAppointment()
  const skip = useSkipToken()
  const recall = useRecallToken()
  const [cancelling, setCancelling] = useState<AppointmentListItem | null>(null)
  const [rescheduling, setRescheduling] = useState<AppointmentListItem | null>(null)
  const [transferring, setTransferring] = useState<AppointmentListItem | null>(null)
  const [settingLink, setSettingLink] = useState<AppointmentListItem | null>(null)

  const columns: Column<AppointmentListItem>[] = [
    {
      header: 'Token',
      cell: (row) => (
        <span className="font-mono">
          #{row.tokenNumber}
          {row.skippedAt && <StatusBadge tone="warning">Skipped</StatusBadge>}
          {row.emergencyVisitId && <StatusBadge tone="danger">ER</StatusBadge>}
          {row.consultationMode === CONSULTATION_MODES.TeleConsult && <StatusBadge tone="info">Tele</StatusBadge>}
        </span>
      ),
    },
    { header: 'Date / Time', cell: (row) => `${row.appointmentDate} ${row.startTime.slice(0, 5)}` },
    { header: 'Patient', cell: (row) => `${row.patientName} (${row.patientUhid})` },
    { header: 'Doctor', cell: (row) => row.doctorName },
    { header: 'Type', cell: (row) => BOOKING_TYPE_LABELS[row.bookingType] },
    {
      header: 'Status',
      cell: (row) => <StatusBadge tone={statusTone[row.status]}>{APPOINTMENT_STATUS_LABELS[row.status]}</StatusBadge>,
    },
    {
      header: '',
      className: 'text-right',
      cell: (row) => {
        const isScheduled = row.status === APPOINTMENT_STATUSES.Scheduled
        const isCheckedIn = row.status === APPOINTMENT_STATUSES.CheckedIn
        if (!isScheduled && !isCheckedIn) return null

        return (
          <div className="flex justify-end gap-1">
            {isScheduled && hasPermission('APPOINTMENT.APPOINTMENT.CHECKIN') && (
              <Button
                variant="ghost"
                onClick={async () => {
                  try {
                    await checkIn.mutateAsync(row.id)
                  } catch (err) {
                    window.alert(errorMessage(err))
                  }
                }}
              >
                Check in
              </Button>
            )}
            {isCheckedIn && hasPermission('APPOINTMENT.APPOINTMENT.SKIP') && (
              <Button
                variant="ghost"
                onClick={async () => {
                  try {
                    await (row.skippedAt ? recall : skip).mutateAsync(row.id)
                  } catch (err) {
                    window.alert(errorMessage(err))
                  }
                }}
              >
                {row.skippedAt ? 'Recall' : 'Skip'}
              </Button>
            )}
            {hasPermission('APPOINTMENT.APPOINTMENT.TRANSFER') && (
              <Button variant="ghost" onClick={() => setTransferring(row)}>
                Transfer
              </Button>
            )}
            {row.consultationMode === CONSULTATION_MODES.TeleConsult && hasPermission('APPOINTMENT.APPOINTMENT.BOOK') && (
              <Button variant="ghost" onClick={() => setSettingLink(row)}>
                {row.teleconsultLink ? 'Edit link' : 'Set link'}
              </Button>
            )}
            {isScheduled && hasPermission('APPOINTMENT.APPOINTMENT.RESCHEDULE') && (
              <Button variant="ghost" onClick={() => setRescheduling(row)}>
                Reschedule
              </Button>
            )}
            {isScheduled && hasPermission('APPOINTMENT.APPOINTMENT.CANCEL') && (
              <Button variant="ghost" className="text-red-600" onClick={() => setCancelling(row)}>
                Cancel
              </Button>
            )}
          </div>
        )
      },
    },
  ]

  return (
    <>
      <PageHeader title="Appointments" subtitle="Search, filter and manage bookings" />

      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onSearch={setSearch}
        searchPlaceholder="Search patient…"
        rowKey={(row) => row.id}
        emptyMessage="No appointments found."
        toolbar={
          <div className="flex gap-2">
            <Select
              placeholder="All doctors"
              options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
              value={doctorId}
              onChange={(e) => {
                setDoctorId(e.target.value)
                setPage(1)
              }}
            />
            <Input type="date" value={date} onChange={(e) => { setDate(e.target.value); setPage(1) }} />
            <Select
              placeholder="All statuses"
              options={Object.entries(APPOINTMENT_STATUS_LABELS).map(([value, label]) => ({ value, label }))}
              value={status}
              onChange={(e) => { setStatus(e.target.value); setPage(1) }}
            />
          </div>
        }
      />

      {cancelling && (
        <CancelModal appointment={cancelling} onClose={() => setCancelling(null)} />
      )}
      {rescheduling && (
        <RescheduleModal appointment={rescheduling} onClose={() => setRescheduling(null)} />
      )}
      {transferring && (
        <TransferModal appointment={transferring} onClose={() => setTransferring(null)} />
      )}
      {settingLink && (
        <TeleconsultLinkModal appointment={settingLink} onClose={() => setSettingLink(null)} />
      )}
    </>
  )
}

function CancelModal({ appointment, onClose }: { appointment: AppointmentListItem; onClose: () => void }) {
  const cancel = useCancelAppointment()
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await cancel.mutateAsync({ id: appointment.id, reason })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Cancel appointment #${appointment.tokenNumber}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button variant="danger" onClick={submit} loading={cancel.isPending} disabled={!reason.trim()}>
            Cancel appointment
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
    </Modal>
  )
}

function RescheduleModal({ appointment, onClose }: { appointment: AppointmentListItem; onClose: () => void }) {
  const reschedule = useRescheduleAppointment()
  const [date, setDate] = useState(appointment.appointmentDate)
  const [selectedSlot, setSelectedSlot] = useState<string | null>(null)
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const { data: slots, isLoading } = useSlots(appointment.doctorId, date)

  const submit = async () => {
    if (!selectedSlot || !reason.trim()) return
    setError(null)
    try {
      await reschedule.mutateAsync({ id: appointment.id, newDate: date, newStartTime: selectedSlot, reason })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Reschedule appointment #${appointment.tokenNumber}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button onClick={submit} loading={reschedule.isPending} disabled={!selectedSlot || !reason.trim()}>
            Reschedule
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <Input
        label="New date"
        type="date"
        value={date}
        onChange={(e) => {
          setDate(e.target.value)
          setSelectedSlot(null)
        }}
      />
      <div className="mt-3">
        <span className="mb-1 block text-sm font-medium text-slate-700">New slot</span>
        {isLoading && <Spinner />}
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
      </div>
      <div className="mt-3">
        <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
      </div>
    </Modal>
  )
}

function TransferModal({ appointment, onClose }: { appointment: AppointmentListItem; onClose: () => void }) {
  const transfer = useTransferToken()
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const [newDoctorId, setNewDoctorId] = useState('')
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const otherDoctors = (doctorsResult?.items ?? []).filter((d) => d.id !== appointment.doctorId)

  const submit = async () => {
    if (!newDoctorId) return
    setError(null)
    try {
      await transfer.mutateAsync({ id: appointment.id, newDoctorId: Number(newDoctorId), reason: reason || null })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Transfer token #${appointment.tokenNumber}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button onClick={submit} loading={transfer.isPending} disabled={!newDoctorId}>
            Transfer
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <p className="mb-3 text-sm text-slate-500">
        Currently with <strong>{appointment.doctorName}</strong>. A new token number is minted for the doctor you
        transfer to.
      </p>
      <Select
        label="Transfer to doctor"
        placeholder="Select doctor…"
        value={newDoctorId}
        onChange={(e) => setNewDoctorId(e.target.value)}
        options={otherDoctors.map((d) => ({ value: d.id, label: d.fullName }))}
      />
      <div className="mt-3">
        <Input label="Reason (optional)" value={reason} onChange={(e) => setReason(e.target.value)} />
      </div>
    </Modal>
  )
}

function TeleconsultLinkModal({ appointment, onClose }: { appointment: AppointmentListItem; onClose: () => void }) {
  const setLink = useSetTeleconsultLink()
  const [teleconsultLink, setTeleconsultLink] = useState(appointment.teleconsultLink ?? '')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await setLink.mutateAsync({ id: appointment.id, teleconsultLink: teleconsultLink || null })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Meeting link — token #${appointment.tokenNumber}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button onClick={submit} loading={setLink.isPending}>
            Save
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <Input
        label="Meeting link"
        placeholder="https://…"
        value={teleconsultLink}
        onChange={(e) => setTeleconsultLink(e.target.value)}
      />
    </Modal>
  )
}
