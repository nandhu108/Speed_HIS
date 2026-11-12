import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useStartConsult } from '../../opd/api'
import { OP_VISIT_STATUSES, VISIT_TYPE_LABELS } from '../../opd/types'
import { useMyQueue } from '../api'

function today() {
  return new Date().toISOString().slice(0, 10)
}

/** Doctor Portal's own queue (plan §22: "Patient Queue") — same OpVisitService.GetQueueAsync the staff DoctorQueuePage uses, just auto-scoped to the logged-in doctor with no picker. */
export function MyQueuePage() {
  const navigate = useNavigate()
  const date = today()
  const { data: queue = [], isLoading } = useMyQueue(date)
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
      <PageHeader title="My Queue" subtitle={`Today, ${date}`} />
      <div className="space-y-2">
        {isLoading && <Spinner />}
        {queue.map((entry) => (
          <div key={entry.visitId} className="flex items-center justify-between rounded-lg border border-slate-200 bg-white px-4 py-3">
            <div className="flex items-center gap-4">
              <span className="text-2xl font-bold text-primary-700">#{entry.tokenNumber}</span>
              <div>
                <p className="text-sm font-medium text-slate-800">
                  {entry.patientName} <span className="font-mono text-xs text-slate-400">({entry.patientUhid})</span>
                </p>
                <p className="text-xs text-slate-400">{VISIT_TYPE_LABELS[entry.visitType]}</p>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <StatusBadge tone={entry.status === OP_VISIT_STATUSES.InConsult ? 'success' : 'info'}>
                {entry.status === OP_VISIT_STATUSES.InConsult ? 'In consult' : 'Waiting'}
              </StatusBadge>
              {entry.status === OP_VISIT_STATUSES.Waiting && (
                <Button onClick={() => start(entry.visitId)} loading={startConsult.isPending}>Start</Button>
              )}
              {entry.status === OP_VISIT_STATUSES.InConsult && (
                <Button variant="secondary" onClick={() => navigate(`/opd/visits/${entry.visitId}`)}>Resume</Button>
              )}
            </div>
          </div>
        ))}
        {!isLoading && queue.length === 0 && <p className="py-10 text-center text-slate-400">No patients in your queue today.</p>}
      </div>
    </>
  )
}
