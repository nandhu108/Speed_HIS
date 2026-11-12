import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useApproveMyDischargeSummary, useMyPendingDischargeSummaries } from '../api'

/** Discharge Summary Approval e-sign queue (plan §22) — a thin ownership-checked wrapper over IPD's existing DischargeService.ApproveDischargeSummaryAsync; the summary itself was authored on the IPD admission detail page, not here. */
export function DischargeApprovalsPage() {
  const { data: pending = [], isLoading } = useMyPendingDischargeSummaries()
  const approve = useApproveMyDischargeSummary()
  const [error, setError] = useState<string | null>(null)

  const doApprove = async (admissionId: number) => {
    setError(null)
    try {
      await approve.mutateAsync(admissionId)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader title="Discharge Approvals" subtitle="Summaries awaiting your e-sign for patients you're consulting on" />
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <div className="space-y-3">
        {isLoading && <Spinner />}
        {pending.map((s) => (
          <div key={s.admissionId} className="rounded-lg border border-slate-200 bg-white p-4">
            <div className="mb-2 flex items-center justify-between">
              <p className="text-sm font-medium text-slate-800">
                {s.patientName} <span className="font-mono text-xs text-slate-400">({s.patientUhid})</span>
              </p>
              <span className="font-mono text-xs text-slate-400">{s.admissionNumber}</span>
            </div>
            <p className="mb-3 text-sm text-slate-600">Final diagnosis: {s.finalDiagnosis}</p>
            <Button onClick={() => doApprove(s.admissionId)} loading={approve.isPending}>Approve &amp; e-sign</Button>
          </div>
        ))}
        {!isLoading && pending.length === 0 && <p className="py-10 text-center text-slate-400">Nothing pending your approval.</p>}
      </div>
    </>
  )
}
