import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useMyOpinionRequests, useSaveMyOpinion } from '../api'
import type { OpinionRequest } from '../types'

/** "Doctor Opinion Entry" inbox (plan §22 extension) — a thin ownership-checked wrapper over
 * OpVisitService.SaveOpinionAsync; the referral itself was raised on the referring doctor's OPD
 * consultation workspace, not here. */
export function OpinionRequestsPage() {
  const { data: requests = [], isLoading } = useMyOpinionRequests()
  const [error, setError] = useState<string | null>(null)

  const pending = requests.filter((r) => !r.opinionEnteredAt)
  const completed = requests.filter((r) => r.opinionEnteredAt)

  return (
    <>
      <PageHeader title="Opinion Requests" subtitle="Cross-consults from other doctors awaiting your opinion" />
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      {isLoading && <Spinner />}

      <div className="space-y-3">
        {pending.map((r) => (
          <OpinionRequestCard key={r.referralId} request={r} onError={setError} />
        ))}
        {!isLoading && pending.length === 0 && <p className="py-6 text-center text-slate-400">No pending opinion requests.</p>}
      </div>

      {completed.length > 0 && (
        <div className="mt-8">
          <p className="mb-3 text-sm font-semibold text-slate-700">Previously answered</p>
          <div className="space-y-2">
            {completed.map((r) => (
              <div key={r.referralId} className="rounded-lg border border-slate-200 bg-white p-4 text-sm">
                <div className="mb-1 flex items-center justify-between">
                  <p className="font-medium text-slate-800">
                    {r.patientName} <span className="font-mono text-xs text-slate-400">({r.patientUhid})</span>
                  </p>
                  <span className="text-xs text-slate-400">{new Date(r.opinionEnteredAt!).toLocaleString()}</span>
                </div>
                <p className="text-slate-500">From {r.referringDoctorName} — {r.reason}</p>
                <p className="mt-2 rounded-md bg-slate-50 px-3 py-2 text-slate-600">{r.opinionNotes}</p>
              </div>
            ))}
          </div>
        </div>
      )}
    </>
  )
}

function OpinionRequestCard({ request, onError }: { request: OpinionRequest; onError: (e: string | null) => void }) {
  const save = useSaveMyOpinion()
  const [opinionNotes, setOpinionNotes] = useState('')

  const submit = async () => {
    if (!opinionNotes.trim()) return
    onError(null)
    try {
      await save.mutateAsync({ referralId: request.referralId, opinionNotes })
    } catch (err) {
      onError(errorMessage(err))
    }
  }

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="mb-2 flex items-center justify-between">
        <p className="text-sm font-medium text-slate-800">
          {request.patientName} <span className="font-mono text-xs text-slate-400">({request.patientUhid})</span>
        </p>
        <span className="font-mono text-xs text-slate-400">{request.visitNumber} · {request.visitDate}</span>
      </div>
      <p className="mb-3 text-sm text-slate-600">From {request.referringDoctorName} — {request.reason}</p>
      <Input label="Your opinion" value={opinionNotes} onChange={(e) => setOpinionNotes(e.target.value)} />
      <Button className="mt-3" loading={save.isPending} disabled={!opinionNotes.trim()} onClick={submit}>
        Save opinion
      </Button>
    </div>
  )
}
