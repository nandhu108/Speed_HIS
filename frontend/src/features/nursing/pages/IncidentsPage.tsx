import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { PatientPicker } from '../../appointments/components/PatientPicker'
import { useAuth } from '../../auth/AuthContext'
import type { PatientListItem } from '../../patients/types'
import { useCloseIncident, useIncidents, useReportIncident, useReviewIncident } from '../api'
import { INCIDENT_SEVERITY_LABELS, INCIDENT_STATUS_LABELS, type Incident, type IncidentSeverity, type IncidentStatus } from '../types'

const severityTone: Record<IncidentSeverity, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'neutral', 2: 'info', 3: 'warning', 4: 'danger',
}
const statusTone: Record<IncidentStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'warning', 2: 'info', 3: 'success',
}

export function IncidentsPage() {
  const { hasPermission } = useAuth()
  const canCreate = hasPermission('NURSING.INCIDENT.CREATE')
  const canReview = hasPermission('NURSING.INCIDENT.REVIEW')
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState('')
  const { data, isLoading } = useIncidents({ page, pageSize: 20, status: status ? (Number(status) as IncidentStatus) : undefined })
  const [reporting, setReporting] = useState(false)
  const [selected, setSelected] = useState<Incident | null>(null)
  const [error, setError] = useState<string | null>(null)
  const review = useReviewIncident()
  const close = useCloseIncident()

  const doReview = async (incident: Incident) => {
    setError(null)
    const notes = window.prompt('Review notes:')
    if (!notes) return
    try {
      await review.mutateAsync({ id: incident.id, reviewNotes: notes })
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const doClose = async (incident: Incident) => {
    setError(null)
    try {
      await close.mutateAsync({ id: incident.id, reviewNotes: null })
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader
        title="Incidents"
        subtitle="Incident reporting — report, review, close"
        actions={canCreate && <Button onClick={() => setReporting(true)}>Report incident</Button>}
      />

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="mb-4 max-w-xs">
        <Select
          label="Status"
          placeholder="All"
          value={status}
          onChange={(e) => { setStatus(e.target.value); setPage(1) }}
          options={Object.entries(INCIDENT_STATUS_LABELS).map(([value, label]) => ({ value, label }))}
        />
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Location</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Severity</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th className="px-4 py-2.5 font-medium text-slate-600"></th>
            </tr>
          </thead>
          <tbody>
            {(data?.items ?? []).map((i) => (
              <tr key={i.id} className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50" onClick={() => setSelected(i)}>
                <td className="px-4 py-2.5 text-xs text-slate-500">{new Date(i.incidentDate).toLocaleString()}</td>
                <td className="px-4 py-2.5 text-slate-700">{i.incidentType}</td>
                <td className="px-4 py-2.5 text-slate-700">{i.location}</td>
                <td className="px-4 py-2.5"><StatusBadge tone={severityTone[i.severity]}>{INCIDENT_SEVERITY_LABELS[i.severity]}</StatusBadge></td>
                <td className="px-4 py-2.5 text-slate-700">{i.patientName ?? '—'}</td>
                <td className="px-4 py-2.5"><StatusBadge tone={statusTone[i.status]}>{INCIDENT_STATUS_LABELS[i.status]}</StatusBadge></td>
                <td className="px-4 py-2.5 text-right" onClick={(e) => e.stopPropagation()}>
                  {canReview && i.status !== 3 && (
                    <div className="flex justify-end gap-1">
                      {i.status === 1 && <Button variant="ghost" onClick={() => doReview(i)}>Review</Button>}
                      <Button variant="ghost" onClick={() => doClose(i)}>Close</Button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && (data?.items.length ?? 0) === 0 && (
              <tr><td colSpan={7} className="px-4 py-10 text-center text-slate-400">No incidents found.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {data && data.totalPages > 1 && (
        <div className="mt-3 flex items-center justify-end gap-2 text-sm">
          <Button variant="secondary" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Prev</Button>
          <span className="text-slate-500">Page {page} of {data.totalPages}</span>
          <Button variant="secondary" disabled={page >= data.totalPages} onClick={() => setPage((p) => p + 1)}>Next</Button>
        </div>
      )}

      {reporting && <ReportIncidentModal onClose={() => setReporting(false)} />}
      {selected && <IncidentDetailModal incident={selected} onClose={() => setSelected(null)} />}
    </>
  )
}

function IncidentDetailModal({ incident, onClose }: { incident: Incident; onClose: () => void }) {
  return (
    <Modal open title={incident.incidentType} onClose={onClose} footer={<Button variant="secondary" onClick={onClose}>Close</Button>}>
      <div className="space-y-2 text-sm">
        <p><span className="font-medium text-slate-600">When:</span> {new Date(incident.incidentDate).toLocaleString()}</p>
        <p><span className="font-medium text-slate-600">Location:</span> {incident.location}</p>
        <p><span className="font-medium text-slate-600">Severity:</span> {INCIDENT_SEVERITY_LABELS[incident.severity]}</p>
        <p><span className="font-medium text-slate-600">Description:</span> {incident.description}</p>
        {incident.immediateActionTaken && <p><span className="font-medium text-slate-600">Immediate action:</span> {incident.immediateActionTaken}</p>}
        {incident.patientName && <p><span className="font-medium text-slate-600">Patient:</span> {incident.patientName}</p>}
        {incident.admissionNumber && <p><span className="font-medium text-slate-600">Admission:</span> {incident.admissionNumber}</p>}
        <p><span className="font-medium text-slate-600">Reported by:</span> {incident.reportedByName}</p>
        {incident.reviewNotes && <p><span className="font-medium text-slate-600">Review notes:</span> {incident.reviewNotes}</p>}
        {incident.reviewedByName && <p><span className="font-medium text-slate-600">Reviewed by:</span> {incident.reviewedByName}</p>}
        <p><span className="font-medium text-slate-600">Status:</span> {INCIDENT_STATUS_LABELS[incident.status]}</p>
      </div>
    </Modal>
  )
}

function ReportIncidentModal({ onClose }: { onClose: () => void }) {
  const report = useReportIncident()
  const [patient, setPatient] = useState<PatientListItem | null>(null)
  const [incidentType, setIncidentType] = useState('')
  const [location, setLocation] = useState('')
  const [severity, setSeverity] = useState<IncidentSeverity>(2)
  const [description, setDescription] = useState('')
  const [actionTaken, setActionTaken] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    if (!incidentType.trim() || !location.trim() || !description.trim()) {
      setError('Type, location and description are required.')
      return
    }
    try {
      await report.mutateAsync({
        incidentDate: new Date().toISOString(), incidentType, location, severity, description,
        immediateActionTaken: actionTaken || null, patientId: patient?.id ?? null, admissionId: null,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Report incident"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={report.isPending}>Report</Button>
        </>
      }
    >
      <div className="max-h-[65vh] space-y-3 overflow-y-auto">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <div className="grid grid-cols-2 gap-2">
          <Input label="Type (Fall, Medication error…)" value={incidentType} onChange={(e) => setIncidentType(e.target.value)} />
          <Input label="Location" value={location} onChange={(e) => setLocation(e.target.value)} />
        </div>
        <Select
          label="Severity"
          value={severity}
          onChange={(e) => setSeverity(Number(e.target.value) as IncidentSeverity)}
          options={Object.entries(INCIDENT_SEVERITY_LABELS).map(([value, label]) => ({ value, label }))}
        />
        <Input label="Description" value={description} onChange={(e) => setDescription(e.target.value)} />
        <Input label="Immediate action taken" value={actionTaken} onChange={(e) => setActionTaken(e.target.value)} />
        <PatientPicker selected={patient} onSelect={setPatient} />
      </div>
    </Modal>
  )
}
