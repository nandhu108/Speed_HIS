import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDoctors } from '../../masters/api'
import { useOpdQueue, useStartConsult } from '../api'
import { OP_VISIT_STATUSES, VISIT_TYPE_LABELS } from '../types'

function today() {
  return new Date().toISOString().slice(0, 10)
}

export function DoctorQueuePage() {
  const { hasPermission } = useAuth()
  const navigate = useNavigate()
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const [doctorId, setDoctorId] = useState('')
  const date = today()

  const { data: queue = [] } = useOpdQueue(doctorId ? Number(doctorId) : null, date)
  const startConsult = useStartConsult()

  const start = async (visitId: number) => {
    try {
      await startConsult.mutateAsync(visitId)
      navigate(`/opd/visits/${visitId}`)
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader
        title="Doctor Queue"
        subtitle={`Today, ${date} — refreshes automatically`}
        actions={
          <Select
            placeholder="All doctors"
            options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
            value={doctorId}
            onChange={(e) => setDoctorId(e.target.value)}
          />
        }
      />

      <div className="space-y-2">
        {queue.map((entry) => (
          <div
            key={entry.visitId}
            className="flex items-center justify-between rounded-lg border border-slate-200 bg-white px-4 py-3"
          >
            <div className="flex items-center gap-4">
              <span className="text-2xl font-bold text-primary-700">#{entry.tokenNumber}</span>
              <div>
                <p className="text-sm font-medium text-slate-800">
                  {entry.patientName} <span className="font-mono text-xs text-slate-400">({entry.patientUhid})</span>
                </p>
                <p className="text-xs text-slate-400">
                  {entry.doctorName} · {VISIT_TYPE_LABELS[entry.visitType]}
                </p>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <StatusBadge tone={entry.status === OP_VISIT_STATUSES.InConsult ? 'success' : 'info'}>
                {entry.status === OP_VISIT_STATUSES.InConsult ? 'In consult' : 'Waiting'}
              </StatusBadge>
              {hasPermission('OPD.VISIT.MANAGE') && entry.status === OP_VISIT_STATUSES.Waiting && (
                <Button onClick={() => start(entry.visitId)} loading={startConsult.isPending}>
                  Start
                </Button>
              )}
              {entry.status === OP_VISIT_STATUSES.InConsult && (
                <Button variant="secondary" onClick={() => navigate(`/opd/visits/${entry.visitId}`)}>
                  Resume
                </Button>
              )}
            </div>
          </div>
        ))}
        {queue.length === 0 && <p className="text-sm text-slate-400">No one is waiting right now.</p>}
      </div>
    </>
  )
}
