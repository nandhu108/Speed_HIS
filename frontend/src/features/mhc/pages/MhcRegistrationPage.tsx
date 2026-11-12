import { useState } from 'react'
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
import { useMhcPackages, useSimpleMaster } from '../../masters/api'
import type { PatientListItem } from '../../patients/types'
import { openMhcVisitSlip, useCancelMhcVisit, useMhcVisits, useRegisterMhcVisit } from '../api'
import { MHC_VISIT_STATUS_LABELS, MHC_VISIT_TYPE_LABELS, type MhcVisitListItem, type MhcVisitStatus } from '../types'

const statusTone: Record<MhcVisitStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info',
  2: 'success',
  3: 'neutral',
  4: 'danger',
}

export function MhcRegistrationPage() {
  const { hasPermission } = useAuth()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [patient, setPatient] = useState<PatientListItem | null>(null)
  const [mhcDoctorId, setMhcDoctorId] = useState('')
  const [mhcPackageId, setMhcPackageId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [cancelling, setCancelling] = useState<MhcVisitListItem | null>(null)

  const { data: mhcDoctors = [] } = useSimpleMaster('mhc-doctors', endpoints.masters.mhcDoctors)
  const { data: packages = [] } = useMhcPackages()
  const { data, isLoading } = useMhcVisits({ page, pageSize: 20, search: search || undefined })
  const register = useRegisterMhcVisit()

  const canCreate = hasPermission('MHC.VISIT.CREATE')
  const canManage = hasPermission('MHC.VISIT.MANAGE')

  const submit = async () => {
    if (!patient || !mhcDoctorId || !mhcPackageId) return
    setError(null)
    try {
      await register.mutateAsync({
        patientId: patient.id,
        mhcDoctorId: Number(mhcDoctorId),
        mhcPackageId: Number(mhcPackageId),
      })
      setPatient(null)
      setMhcDoctorId('')
      setMhcPackageId('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const columns: Column<MhcVisitListItem>[] = [
    { header: 'Token', cell: (row) => <span className="font-mono">#{row.tokenNumber}</span> },
    { header: 'Visit No', cell: (row) => <span className="font-mono text-xs">{row.visitNumber}</span> },
    { header: 'Patient', cell: (row) => `${row.patientName} (${row.patientUhid})` },
    { header: 'MHC Doctor', cell: (row) => row.doctorName },
    { header: 'Type', cell: (row) => MHC_VISIT_TYPE_LABELS[row.visitType] },
    {
      header: 'Status',
      cell: (row) => <StatusBadge tone={statusTone[row.status]}>{MHC_VISIT_STATUS_LABELS[row.status]}</StatusBadge>,
    },
    {
      header: '',
      className: 'text-right',
      cell: (row) => (
        <div className="flex justify-end gap-1">
          <Button variant="ghost" onClick={() => openMhcVisitSlip(row.id)}>
            Print slip
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
      <PageHeader title="MHC Registration" subtitle="Register a Master Health Checkup visit or find an existing one" />

      {canCreate && (
        <div className="mb-6 max-w-3xl rounded-lg border border-slate-200 bg-white p-4">
          {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
          <div className="flex flex-wrap items-end gap-3">
            <div className="w-72">
              <PatientPicker selected={patient} onSelect={setPatient} />
            </div>
            <Select
              label="MHC Doctor"
              placeholder="Select an MHC doctor…"
              options={mhcDoctors.map((d) => ({ value: d.id, label: d.name }))}
              value={mhcDoctorId}
              onChange={(e) => setMhcDoctorId(e.target.value)}
            />
            <Select
              label="Package"
              placeholder="Select a package…"
              options={packages.map((p) => ({ value: p.id, label: `${p.name} (₹${p.price.toFixed(2)})` }))}
              value={mhcPackageId}
              onChange={(e) => setMhcPackageId(e.target.value)}
            />
            <Button disabled={!patient || !mhcDoctorId || !mhcPackageId} loading={register.isPending} onClick={submit}>
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
        emptyMessage="No MHC visits found."
      />

      {cancelling && <CancelMhcVisitModal visit={cancelling} onClose={() => setCancelling(null)} />}
    </>
  )
}

function CancelMhcVisitModal({ visit, onClose }: { visit: MhcVisitListItem; onClose: () => void }) {
  const cancel = useCancelMhcVisit()
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
