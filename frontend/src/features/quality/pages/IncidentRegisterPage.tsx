import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useIncidents, useRecordIncidentRca } from '../../nursing/api'
import { INCIDENT_SEVERITY_LABELS, INCIDENT_STATUS_LABELS, type Incident, type IncidentSeverity, type IncidentStatus } from '../../nursing/types'

const severityTone: Record<IncidentSeverity, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'neutral', 2: 'info', 3: 'warning', 4: 'danger',
}
const statusTone: Record<IncidentStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'warning', 2: 'info', 3: 'success',
}

/**
 * Quality's org-wide incident register (plan §20: "merges nursing incidents"). Reuses Nursing's
 * existing incident list/RCA endpoints outright rather than a parallel entity — see the backend
 * Incident entity's own doc comment.
 */
export function IncidentRegisterPage() {
  const { hasPermission } = useAuth()
  const canRca = hasPermission('QUALITY.INCIDENT.RCA')
  const [page, setPage] = useState(1)
  const [sentinelOnly, setSentinelOnly] = useState(false)
  const { data, isLoading } = useIncidents({ page, pageSize: 20, sentinelOnly: sentinelOnly || undefined })
  const [selected, setSelected] = useState<Incident | null>(null)

  return (
    <>
      <PageHeader title="Incident Register" subtitle="Org-wide incident register with root-cause analysis and sentinel-event tracking" />

      <div className="mb-4">
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" checked={sentinelOnly} onChange={(e) => { setSentinelOnly(e.target.checked); setPage(1) }} />
          Sentinel events only
        </label>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Source</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Severity</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Sentinel</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            </tr>
          </thead>
          <tbody>
            {(data?.items ?? []).map((i) => (
              <tr key={i.id} className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50" onClick={() => setSelected(i)}>
                <td className="px-4 py-2.5 text-xs text-slate-500">{new Date(i.incidentDate).toLocaleString()}</td>
                <td className="px-4 py-2.5 text-slate-700">{i.incidentType}</td>
                <td className="px-4 py-2.5 text-slate-500">{i.sourceModule ?? '—'}</td>
                <td className="px-4 py-2.5"><StatusBadge tone={severityTone[i.severity]}>{INCIDENT_SEVERITY_LABELS[i.severity]}</StatusBadge></td>
                <td className="px-4 py-2.5">{i.isSentinelEvent && <StatusBadge tone="danger">Sentinel</StatusBadge>}</td>
                <td className="px-4 py-2.5"><StatusBadge tone={statusTone[i.status]}>{INCIDENT_STATUS_LABELS[i.status]}</StatusBadge></td>
              </tr>
            ))}
            {!isLoading && (data?.items.length ?? 0) === 0 && (
              <tr><td colSpan={6} className="px-4 py-10 text-center text-slate-400">No incidents found.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {data && data.totalPages > 1 && (
        <div className="mt-3 flex items-center justify-end gap-2 text-sm">
          <Button variant="secondary" disabled={page <= 1} onClick={() => setPage((n) => n - 1)}>Prev</Button>
          <span className="text-slate-500">Page {page} of {data.totalPages}</span>
          <Button variant="secondary" disabled={page >= data.totalPages} onClick={() => setPage((n) => n + 1)}>Next</Button>
        </div>
      )}

      {selected && <IncidentRcaModal incident={selected} canRca={canRca} onClose={() => setSelected(null)} />}
    </>
  )
}

function IncidentRcaModal({ incident, canRca, onClose }: { incident: Incident; canRca: boolean; onClose: () => void }) {
  const recordRca = useRecordIncidentRca()
  const [rootCauseAnalysis, setRootCauseAnalysis] = useState(incident.rootCauseAnalysis ?? '')
  const [isSentinelEvent, setIsSentinelEvent] = useState(incident.isSentinelEvent)
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await recordRca.mutateAsync({ id: incident.id, rootCauseAnalysis, isSentinelEvent })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={incident.incidentType}
      onClose={onClose}
      footer={
        canRca
          ? <><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={submit} loading={recordRca.isPending} disabled={!rootCauseAnalysis.trim()}>Save RCA</Button></>
          : <Button variant="secondary" onClick={onClose}>Close</Button>
      }
    >
      <div className="space-y-3 text-sm">
        <p><span className="font-medium text-slate-600">When:</span> {new Date(incident.incidentDate).toLocaleString()}</p>
        <p><span className="font-medium text-slate-600">Location:</span> {incident.location}</p>
        <p><span className="font-medium text-slate-600">Description:</span> {incident.description}</p>
        <p><span className="font-medium text-slate-600">Reported by:</span> {incident.reportedByName}</p>
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        {canRca ? (
          <>
            <label className="flex items-center gap-2 text-sm text-slate-700">
              <input type="checkbox" checked={isSentinelEvent} onChange={(e) => setIsSentinelEvent(e.target.checked)} />
              Sentinel event
            </label>
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700">Root cause analysis</label>
              <textarea className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" rows={4} value={rootCauseAnalysis} onChange={(e) => setRootCauseAnalysis(e.target.value)} />
            </div>
          </>
        ) : (
          incident.rootCauseAnalysis && <p><span className="font-medium text-slate-600">Root cause analysis:</span> {incident.rootCauseAnalysis}</p>
        )}
      </div>
    </Modal>
  )
}
