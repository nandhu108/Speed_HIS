import { useState } from 'react'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useDoctors } from '../../masters/api'
import { useQueueDisplay } from '../api'
import { APPOINTMENT_STATUS_LABELS, APPOINTMENT_STATUSES, CONSULTATION_MODES } from '../types'

function today() {
  return new Date().toISOString().slice(0, 10)
}

/**
 * Auto-refreshing token queue. Runs under the normal authenticated shell (this
 * codebase has no anonymous API surface beyond login/refresh/health) — a true
 * unattended kiosk would need a dedicated low-privilege display session, which is
 * a future addition, not part of this phase.
 */
export function QueueDisplayPage() {
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const [doctorId, setDoctorId] = useState('')
  const date = today()

  const { data: queue = [] } = useQueueDisplay(doctorId ? Number(doctorId) : null, null, date)

  return (
    <>
      <PageHeader
        title="Queue Display"
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

      <div className="grid gap-3 md:grid-cols-2 lg:grid-cols-3">
        {queue.map((entry) => (
          <div
            key={entry.appointmentId}
            className={`flex items-center justify-between rounded-lg border bg-white p-5 ${
              entry.skippedAt ? 'border-amber-300' : 'border-slate-200'
            }`}
          >
            <div>
              <p className="text-3xl font-bold text-primary-700">#{entry.tokenNumber}</p>
              <p className="mt-1 text-sm font-medium text-slate-800">{entry.patientName}</p>
              <p className="text-xs text-slate-400">{entry.doctorName}</p>
            </div>
            <div className="text-right">
              <div className="flex items-center justify-end gap-1">
                {entry.emergencyVisitId && <StatusBadge tone="danger">ER</StatusBadge>}
                {entry.skippedAt && <StatusBadge tone="warning">Skipped</StatusBadge>}
                {entry.consultationMode === CONSULTATION_MODES.TeleConsult && <StatusBadge tone="info">Tele</StatusBadge>}
                <StatusBadge tone={entry.status === APPOINTMENT_STATUSES.CheckedIn ? 'success' : 'info'}>
                  {APPOINTMENT_STATUS_LABELS[entry.status]}
                </StatusBadge>
              </div>
              <p className="mt-1 text-xs text-slate-400">{entry.startTime.slice(0, 5)}</p>
            </div>
          </div>
        ))}
        {queue.length === 0 && <p className="text-sm text-slate-400">No one is waiting right now.</p>}
      </div>
    </>
  )
}
