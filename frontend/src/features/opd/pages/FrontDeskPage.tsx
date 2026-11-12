import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { compactErrors, hasErrors, requiredSelect, type FieldErrors } from '../../../lib/formValidation'
import { endpoints } from '../../../api/endpoints'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { PatientPicker } from '../../appointments/components/PatientPicker'
import { useAuth } from '../../auth/AuthContext'
import { usePaymentModes } from '../../billing/api'
import { doctorsForDepartment, useClinicalDepartments, useDoctors, useSimpleMaster } from '../../masters/api'
import type { PatientListItem } from '../../patients/types'
import { useCancelVisit, useOpVisits, useRegisterOpVisit } from '../api'
import {
  BOOKING_TYPES,
  BOOKING_TYPE_LABELS,
  OP_VISIT_STATUS_LABELS,
  VISIT_TYPE_LABELS,
  type BookingType,
  type OpVisitListItem,
  type OpVisitStatus,
} from '../types'

const statusTone: Record<OpVisitStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info',
  2: 'success',
  3: 'neutral',
  4: 'danger',
}

export function FrontDeskPage() {
  const { hasPermission } = useAuth()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [patient, setPatient] = useState<PatientListItem | null>(null)
  const [departmentId, setDepartmentId] = useState('')
  const [doctorId, setDoctorId] = useState('')
  const [bookingType, setBookingType] = useState<BookingType>(BOOKING_TYPES.WalkIn)
  const [visitClassId, setVisitClassId] = useState('')
  const [sessionId, setSessionId] = useState('')
  const [isMlcCase, setIsMlcCase] = useState(false)
  const [advanceAmount, setAdvanceAmount] = useState('')
  const [paymentModeId, setPaymentModeId] = useState('')
  const [advanceReferenceNumber, setAdvanceReferenceNumber] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [cancelling, setCancelling] = useState<OpVisitListItem | null>(null)

  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const { data: clinicalDepartments = [] } = useClinicalDepartments()
  const departmentName = clinicalDepartments.find((d) => String(d.id) === departmentId)?.name
  const doctorOptions = doctorsForDepartment(doctorsResult?.items ?? [], departmentName)
  const { data: visitClasses = [] } = useSimpleMaster('visit-classes', endpoints.masters.visitClasses)
  const { data: visitSessions = [] } = useSimpleMaster('visit-sessions', endpoints.masters.visitSessions)
  const { data: paymentModes = [] } = usePaymentModes()
  const { data, isLoading } = useOpVisits({ page, pageSize: 20, search: search || undefined })
  const register = useRegisterOpVisit()

  const canCreate = hasPermission('OPD.VISIT.CREATE')
  const canManage = hasPermission('OPD.VISIT.MANAGE')

  /** Mirrors RegisterOpVisitRequestValidator, including its conditional payment-mode rule. */
  const validate = (): FieldErrors =>
    compactErrors({
      patient: patient ? undefined : 'Select a patient.',
      departmentId: requiredSelect(departmentId, 'Department'),
      doctorId: requiredSelect(doctorId, 'Doctor'),
      advanceAmount: advanceAmount && Number(advanceAmount) < 0 ? 'Advance cannot be negative.' : undefined,
      paymentModeId:
        Number(advanceAmount) > 0 ? requiredSelect(paymentModeId, 'Payment mode') : undefined,
    })

  const submit = async () => {
    setError(null)
    const errors = validate()
    setFieldErrors(errors)
    if (hasErrors(errors)) {
      setError('Check the highlighted fields below.')
      return
    }
    try {
      const visit = await register.mutateAsync({
        patientId: patient!.id,
        departmentId: Number(departmentId),
        doctorId: Number(doctorId),
        appointmentId: null,
        bookingType,
        visitClassId: visitClassId ? Number(visitClassId) : null,
        sessionId: sessionId ? Number(sessionId) : null,
        isMlcCase,
        advanceAmount: advanceAmount ? Number(advanceAmount) : null,
        paymentModeId: paymentModeId ? Number(paymentModeId) : null,
        advanceReferenceNumber: advanceReferenceNumber || null,
      })
      navigate(`/opd/visits/${visit.id}`)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const columns: Column<OpVisitListItem>[] = [
    { header: 'Token', cell: (row) => <span className="font-mono">#{row.tokenNumber}</span> },
    { header: 'Visit No', cell: (row) => <span className="font-mono text-xs">{row.visitNumber}</span> },
    { header: 'Patient', cell: (row) => `${row.patientName} (${row.patientUhid})` },
    { header: 'Doctor', cell: (row) => row.doctorName },
    { header: 'Type', cell: (row) => VISIT_TYPE_LABELS[row.visitType] },
    {
      header: 'Status',
      cell: (row) => <StatusBadge tone={statusTone[row.status]}>{OP_VISIT_STATUS_LABELS[row.status]}</StatusBadge>,
    },
    {
      header: '',
      className: 'text-right',
      cell: (row) => (
        <div className="flex justify-end gap-1">
          <Button variant="ghost" onClick={() => navigate(`/opd/visits/${row.id}`)}>
            Open
          </Button>
          {canManage && row.status !== 3 && row.status !== 4 && (
            <Button variant="ghost" className="text-red-600" onClick={() => setCancelling(row)}>
              Cancel
            </Button>
          )}
        </div>
      ),
    },
  ]

  return (
    <>
      <PageHeader title="OPD Front Desk" subtitle="Register a visit or find an existing one" />

      {canCreate && (
        <div className="mb-6 max-w-3xl rounded-lg border border-slate-200 bg-white p-4">
          {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
          <div className="flex flex-wrap items-end gap-3">
            <div className="w-72">
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
            <Select
              label="Booking type"
              options={Object.entries(BOOKING_TYPE_LABELS).map(([value, label]) => ({ value, label }))}
              value={String(bookingType)}
              onChange={(e) => setBookingType(Number(e.target.value) as BookingType)}
              className="w-32"
            />
            <Select
              label="Class"
              placeholder="—"
              options={visitClasses.map((c) => ({ value: c.id, label: c.name }))}
              value={visitClassId}
              onChange={(e) => setVisitClassId(e.target.value)}
              className="w-32"
            />
            <Select
              label="Session"
              placeholder="—"
              options={visitSessions.map((s) => ({ value: s.id, label: s.name }))}
              value={sessionId}
              onChange={(e) => setSessionId(e.target.value)}
              className="w-32"
            />
            <label className="flex items-center gap-1.5 pb-2 text-sm text-slate-600">
              <input type="checkbox" checked={isMlcCase} onChange={(e) => setIsMlcCase(e.target.checked)} />
              MLC
            </label>
          </div>
          <div className="mt-3 flex flex-wrap items-end gap-3">
            <Input
              label="Advance amount"
              type="number"
              min={0}
              error={fieldErrors.advanceAmount}
              value={advanceAmount}
              onChange={(e) => setAdvanceAmount(e.target.value)}
              className="w-40"
            />
            <Select
              label="Payment mode"
              required={Number(advanceAmount) > 0}
              error={fieldErrors.paymentModeId}
              placeholder="—"
              options={paymentModes.map((m) => ({ value: m.id, label: m.name }))}
              value={paymentModeId}
              onChange={(e) => setPaymentModeId(e.target.value)}
              className="w-40"
            />
            <Input
              label="Reference no."
              value={advanceReferenceNumber}
              onChange={(e) => setAdvanceReferenceNumber(e.target.value)}
              className="w-40"
            />
            <Button loading={register.isPending} onClick={submit}>
              Register visit
            </Button>
          </div>
        </div>
      )}

      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onSearch={setSearch}
        searchPlaceholder="Search visit no, UHID or name…"
        rowKey={(row) => row.id}
        emptyMessage="No visits found."
      />

      {cancelling && <CancelVisitModal visit={cancelling} onClose={() => setCancelling(null)} />}
    </>
  )
}

function CancelVisitModal({ visit, onClose }: { visit: OpVisitListItem; onClose: () => void }) {
  const cancel = useCancelVisit()
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await cancel.mutateAsync({ visitId: visit.id, reason })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Cancel visit #${visit.tokenNumber}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button variant="danger" onClick={submit} loading={cancel.isPending} disabled={!reason.trim()}>
            Cancel visit
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
    </Modal>
  )
}
