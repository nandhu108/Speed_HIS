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
import { useAuth } from '../../auth/AuthContext'
import { PatientPicker } from '../../appointments/components/PatientPicker'
import { useDoctors, useWards } from '../../masters/api'
import type { PatientListItem } from '../../patients/types'
import { AdmitPatientModal } from '../components/AdmitPatientModal'
import {
  useAdmissionRequests,
  useApproveAdmissionRequest,
  useCreateAdmissionRequest,
  useRejectAdmissionRequest,
} from '../api'
import { ADMISSION_REQUEST_STATUS_LABELS, type AdmissionRequestItem, type AdmissionRequestStatus } from '../types'

const statusTone: Record<AdmissionRequestStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'warning', 2: 'success', 3: 'danger',
}

export function AdmissionRequestsPage() {
  const { hasPermission } = useAuth()
  const navigate = useNavigate()
  const canCreate = hasPermission('IPD.ADMISSIONREQUEST.CREATE')
  const canDecide = hasPermission('IPD.ADMISSIONREQUEST.DECIDE')
  const canAdmit = hasPermission('IPD.ADMISSION.CREATE')

  const [page, setPage] = useState(1)
  const { data, isLoading } = useAdmissionRequests({ page, pageSize: 20 })
  const approve = useApproveAdmissionRequest()

  const [creating, setCreating] = useState(false)
  const [rejecting, setRejecting] = useState<AdmissionRequestItem | null>(null)
  const [admitting, setAdmitting] = useState<AdmissionRequestItem | null>(null)

  return (
    <>
      <PageHeader
        title="Admission Requests"
        subtitle="Pre-admission requests — approve or reject before the patient is actually admitted"
        actions={canCreate && <Button onClick={() => setCreating(true)}>New request</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Consultant</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Ward</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Expected date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Requested by</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {(data?.items ?? []).map((r) => (
              <tr key={r.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">
                  {r.patientName} <span className="font-mono text-xs text-slate-400">({r.patientUhid})</span>
                </td>
                <td className="px-4 py-2.5 text-slate-700">{r.requestedConsultantName ?? '—'}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.wardName ?? '—'}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.expectedAdmissionDate}</td>
                <td className="px-4 py-2.5 text-slate-500">{r.requestedByName}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={statusTone[r.status]}>{ADMISSION_REQUEST_STATUS_LABELS[r.status]}</StatusBadge>
                  {r.status === 3 && r.rejectionReason && (
                    <p className="mt-1 text-xs text-slate-400">{r.rejectionReason}</p>
                  )}
                </td>
                <td className="px-4 py-2.5 text-right">
                  <div className="flex justify-end gap-1">
                    {canDecide && r.status === 1 && (
                      <>
                        <Button
                          variant="ghost"
                          onClick={async () => {
                            try {
                              await approve.mutateAsync(r.id)
                            } catch (err) {
                              window.alert(errorMessage(err))
                            }
                          }}
                        >
                          Approve
                        </Button>
                        <Button variant="ghost" className="text-red-600" onClick={() => setRejecting(r)}>
                          Reject
                        </Button>
                      </>
                    )}
                    {canAdmit && r.status === 2 && !r.approvedAdmissionId && (
                      <Button variant="ghost" onClick={() => setAdmitting(r)}>
                        Admit
                      </Button>
                    )}
                    {r.approvedAdmissionId && (
                      <Button variant="ghost" onClick={() => navigate(`/ipd/admissions/${r.approvedAdmissionId}`)}>
                        View admission
                      </Button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
            {!isLoading && (data?.items.length ?? 0) === 0 && (
              <tr>
                <td colSpan={7} className="px-4 py-10 text-center text-slate-400">
                  No admission requests found.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {data && data.totalPages > 1 && (
        <div className="mt-3 flex items-center justify-end gap-2 text-sm">
          <Button variant="secondary" disabled={page <= 1} onClick={() => setPage((p2) => p2 - 1)}>Prev</Button>
          <span className="text-slate-500">Page {page} of {data.totalPages}</span>
          <Button variant="secondary" disabled={page >= data.totalPages} onClick={() => setPage((p2) => p2 + 1)}>Next</Button>
        </div>
      )}

      {creating && <NewRequestModal onClose={() => setCreating(false)} />}
      {rejecting && <RejectRequestModal request={rejecting} onClose={() => setRejecting(null)} />}
      {admitting && (
        <AdmitPatientModal
          prefill={{
            patientId: admitting.patientId,
            patientName: admitting.patientName,
            patientUhid: admitting.patientUhid,
            admissionRequestId: admitting.id,
          }}
          onClose={() => setAdmitting(null)}
          onAdmitted={(id) => navigate(`/ipd/admissions/${id}`)}
        />
      )}
    </>
  )
}

function NewRequestModal({ onClose }: { onClose: () => void }) {
  const create = useCreateAdmissionRequest()
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const { data: wards = [] } = useWards()

  const [patient, setPatient] = useState<PatientListItem | null>(null)
  const [consultantId, setConsultantId] = useState('')
  const [wardId, setWardId] = useState('')
  const [expectedDate, setExpectedDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    if (!patient) {
      setError('Select a patient.')
      return
    }
    setError(null)
    try {
      await create.mutateAsync({
        patientId: patient.id,
        requestedConsultantId: consultantId ? Number(consultantId) : null,
        wardId: wardId ? Number(wardId) : null,
        expectedAdmissionDate: expectedDate,
        reason: reason || null,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="New admission request"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={create.isPending}>Raise request</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <PatientPicker selected={patient} onSelect={setPatient} />
        <Select
          label="Requested consultant"
          placeholder="None"
          value={consultantId}
          onChange={(e) => setConsultantId(e.target.value)}
          options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
        />
        <Select
          label="Ward"
          placeholder="None"
          value={wardId}
          onChange={(e) => setWardId(e.target.value)}
          options={wards.map((w) => ({ value: w.id, label: w.name }))}
        />
        <Input
          label="Expected admission date"
          type="date"
          value={expectedDate}
          onChange={(e) => setExpectedDate(e.target.value)}
        />
        <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
      </div>
    </Modal>
  )
}

function RejectRequestModal({ request, onClose }: { request: AdmissionRequestItem; onClose: () => void }) {
  const reject = useRejectAdmissionRequest()
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    if (!reason.trim()) {
      setError('Reason is required.')
      return
    }
    try {
      await reject.mutateAsync({ id: request.id, reason })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Reject admission request"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button variant="danger" onClick={submit} loading={reject.isPending}>Reject</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <p className="text-sm text-slate-600">
          Rejecting the request for <span className="font-medium">{request.patientName}</span>.
        </p>
        <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
      </div>
    </Modal>
  )
}
