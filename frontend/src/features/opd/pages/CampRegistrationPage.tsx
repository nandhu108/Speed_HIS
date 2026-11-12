import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { endpoints } from '../../../api/endpoints'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { PatientPicker } from '../../appointments/components/PatientPicker'
import { useAuth } from '../../auth/AuthContext'
import { doctorsForDepartment, useClinicalDepartments, useDoctors, useSimpleMaster } from '../../masters/api'
import type { PatientListItem } from '../../patients/types'
import { useCancelVisit, useOpVisits, useRegisterOpVisit, useUpdateVisitCamp } from '../api'
import { OP_VISIT_STATUS_LABELS, VISIT_TYPE_LABELS, type OpVisitListItem, type OpVisitStatus } from '../types'

const statusTone: Record<OpVisitStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info',
  2: 'success',
  3: 'neutral',
  4: 'danger',
}

export function CampRegistrationPage() {
  const { hasPermission } = useAuth()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [campId, setCampId] = useState('')
  const [patient, setPatient] = useState<PatientListItem | null>(null)
  const [departmentId, setDepartmentId] = useState('')
  const [doctorId, setDoctorId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [cancelling, setCancelling] = useState<OpVisitListItem | null>(null)
  const [editingCamp, setEditingCamp] = useState<OpVisitListItem | null>(null)

  const { data: camps = [] } = useSimpleMaster('camps', endpoints.masters.camps)
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const { data: clinicalDepartments = [] } = useClinicalDepartments()
  const departmentName = clinicalDepartments.find((d) => String(d.id) === departmentId)?.name
  const doctorOptions = doctorsForDepartment(doctorsResult?.items ?? [], departmentName)
  const { data, isLoading } = useOpVisits({
    page, pageSize: 20, search: search || undefined,
    campId: campId ? Number(campId) : undefined,
  })
  const register = useRegisterOpVisit()

  const canCreate = hasPermission('OPD.VISIT.CREATE')
  const canManage = hasPermission('OPD.VISIT.MANAGE')

  const submit = async () => {
    if (!patient || !departmentId || !doctorId || !campId) return
    setError(null)
    try {
      const visit = await register.mutateAsync({
        patientId: patient.id, departmentId: Number(departmentId), doctorId: Number(doctorId),
        appointmentId: null, campId: Number(campId),
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
    { header: 'Camp', cell: (row) => row.campName ?? '—' },
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
          {canCreate && row.status === 1 && (
            <Button variant="ghost" onClick={() => setEditingCamp(row)}>
              Edit camp
            </Button>
          )}
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
      <PageHeader title="Camp Registration" subtitle="Register OP visits under a health camp, or find an existing one" />

      <div className="mb-3 max-w-xs">
        <Select
          label="Camp"
          placeholder="Select a camp…"
          options={camps.map((c) => ({ value: c.id, label: c.name }))}
          value={campId}
          onChange={(e) => setCampId(e.target.value)}
        />
      </div>

      {canCreate && campId && (
        <div className="mb-6 max-w-2xl rounded-lg border border-slate-200 bg-white p-4">
          {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
          <div className="flex flex-wrap items-end gap-3">
            <div className="w-72">
              <PatientPicker selected={patient} onSelect={setPatient} />
            </div>
            <Select
              label="Department"
              required
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
              placeholder="Select a doctor…"
              options={doctorOptions.map((d) => ({ value: d.id, label: d.fullName }))}
              value={doctorId}
              onChange={(e) => setDoctorId(e.target.value)}
            />
            <Button disabled={!patient || !departmentId || !doctorId} loading={register.isPending} onClick={submit}>
              Register visit
            </Button>
          </div>
        </div>
      )}

      {campId ? (
        <DataTable
          columns={columns}
          result={data}
          isLoading={isLoading}
          page={page}
          onPageChange={setPage}
          onSearch={setSearch}
          searchPlaceholder="Search visit no, UHID or name…"
          rowKey={(row) => row.id}
          emptyMessage="No visits registered under this camp."
        />
      ) : (
        <p className="py-10 text-center text-sm text-slate-400">Select a camp to register or view visits.</p>
      )}

      {cancelling && <CancelVisitModal visit={cancelling} onClose={() => setCancelling(null)} />}
      {editingCamp && <EditCampModal visit={editingCamp} camps={camps} onClose={() => setEditingCamp(null)} />}
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

function EditCampModal({
  visit, camps, onClose,
}: {
  visit: OpVisitListItem
  camps: { id: number; name: string }[]
  onClose: () => void
}) {
  const update = useUpdateVisitCamp()
  const [campId, setCampId] = useState(String(visit.campId ?? ''))
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await update.mutateAsync({ visitId: visit.id, campId: campId ? Number(campId) : null })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Edit camp — visit #${visit.tokenNumber}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={update.isPending}>
            Save
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <Select
        label="Camp"
        placeholder="Select a camp…"
        options={camps.map((c) => ({ value: c.id, label: c.name }))}
        value={campId}
        onChange={(e) => setCampId(e.target.value)}
      />
    </Modal>
  )
}
