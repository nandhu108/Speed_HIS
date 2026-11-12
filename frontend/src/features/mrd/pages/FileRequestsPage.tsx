import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useApproveFileRequest, useFileRequests, useIssueFileRequest, useRejectFileRequest, useReturnFileRequest } from '../api'
import { FILE_REQUEST_STATUS_LABELS, FILE_REQUEST_TYPE_LABELS, type FileRequestDto, type FileRequestStatus } from '../types'

const statusTone: Record<FileRequestStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info', 2: 'warning', 3: 'danger', 4: 'warning', 5: 'success',
}

export function FileRequestsPage() {
  const { hasPermission } = useAuth()
  const canIssue = hasPermission('MRD.FILE.ISSUE')
  const [status, setStatus] = useState<string>('')
  const [overdueOnly, setOverdueOnly] = useState(false)
  const { data: requests = [], isLoading } = useFileRequests({
    status: status ? (Number(status) as FileRequestStatus) : undefined,
    overdueOnly,
  })

  const approve = useApproveFileRequest()
  const issue = useIssueFileRequest()
  const returnFile = useReturnFileRequest()
  const [rejecting, setRejecting] = useState<FileRequestDto | null>(null)
  const [error, setError] = useState<string | null>(null)

  const run = async (fn: () => Promise<unknown>) => {
    setError(null)
    try {
      await fn()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader title="File Requests" subtitle="Issue &amp; return workflow — internal, external and legal record requests" />

      <div className="mb-3 flex flex-wrap items-end gap-2">
        <Select
          label="Status"
          value={status}
          onChange={(e) => setStatus(e.target.value)}
          placeholder="All"
          options={Object.entries(FILE_REQUEST_STATUS_LABELS).map(([value, label]) => ({ value, label }))}
          className="w-48"
        />
        <label className="flex items-center gap-2 pb-2 text-sm text-slate-700">
          <input type="checkbox" checked={overdueOnly} onChange={(e) => setOverdueOnly(e.target.checked)} />
          Overdue only
        </label>
      </div>

      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">File No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Requester</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Purpose</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Expected Return</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              {canIssue && <th className="px-4 py-2.5 font-medium text-slate-600">Actions</th>}
            </tr>
          </thead>
          <tbody>
            {requests.map((r) => (
              <tr key={r.id} className="border-b border-slate-100 last:border-0 hover:bg-slate-50">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{r.fileNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{FILE_REQUEST_TYPE_LABELS[r.requestType]}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.requestedByName ?? r.requesterName ?? '—'}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.purpose}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.expectedReturnDate ?? '—'}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={r.isOverdue ? 'danger' : statusTone[r.status]}>
                    {r.isOverdue ? 'Overdue' : FILE_REQUEST_STATUS_LABELS[r.status]}
                  </StatusBadge>
                </td>
                {canIssue && (
                  <td className="space-x-2 px-4 py-2.5">
                    {r.status === 1 && (
                      <>
                        <Button variant="secondary" onClick={() => run(() => approve.mutateAsync(r.id))}>Approve</Button>
                        <Button variant="ghost" className="text-red-600" onClick={() => setRejecting(r)}>Reject</Button>
                      </>
                    )}
                    {r.status === 2 && (
                      <Button variant="secondary" onClick={() => run(() => issue.mutateAsync(r.id))}>Issue</Button>
                    )}
                    {r.status === 4 && (
                      <Button variant="secondary" onClick={() => run(() => returnFile.mutateAsync(r.id))}>Return</Button>
                    )}
                  </td>
                )}
              </tr>
            ))}
            {!isLoading && requests.length === 0 && (
              <tr>
                <td colSpan={canIssue ? 7 : 6} className="px-4 py-10 text-center text-slate-400">
                  No file requests found.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {rejecting && <RejectModal request={rejecting} onClose={() => setRejecting(null)} />}
    </>
  )
}

function RejectModal({ request, onClose }: { request: FileRequestDto; onClose: () => void }) {
  const reject = useRejectFileRequest()
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
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
      title={`Reject request for ${request.fileNumber}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={reject.isPending} disabled={!reason}>Reject</Button>
        </>
      }
    >
      {error && <p className="mb-2 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <textarea
        className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
        rows={3}
        placeholder="Reason for rejection"
        value={reason}
        onChange={(e) => setReason(e.target.value)}
      />
    </Modal>
  )
}
