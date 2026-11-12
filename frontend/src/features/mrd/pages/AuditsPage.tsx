import { useNavigate } from 'react-router-dom'
import { Button } from '../../../components/ui/Button'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useIncompleteRecords } from '../api'

/** Discharge file audit worklist — reuses the incomplete-records report as the work queue; the checklist itself is filled in from the admission's "MRD" tab in IPD. */
export function AuditsPage() {
  const navigate = useNavigate()
  const { data: rows = [], isLoading } = useIncompleteRecords({})

  return (
    <>
      <PageHeader
        title="Discharge File Audits"
        subtitle="Discharged admissions with an incomplete record — open the admission to complete the checklist"
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Admission</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Discharged</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Record File</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Audit</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Coding</th>
              <th className="px-4 py-2.5 font-medium text-slate-600"></th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.admissionId} className="border-b border-slate-100 last:border-0 hover:bg-slate-50">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{r.admissionNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.patientName}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.dischargedAt ? new Date(r.dischargedAt).toLocaleDateString() : '—'}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={r.hasRecordFile ? 'success' : 'danger'}>{r.hasRecordFile ? 'Opened' : 'Missing'}</StatusBadge>
                </td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={r.auditComplete ? 'success' : 'danger'}>{r.auditComplete ? 'Complete' : r.hasFileAudit ? 'Incomplete' : 'Not started'}</StatusBadge>
                </td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={r.isCoded ? 'success' : 'warning'}>{r.isCoded ? 'Coded' : 'Pending'}</StatusBadge>
                </td>
                <td className="px-4 py-2.5">
                  <Button variant="secondary" onClick={() => navigate(`/ipd/admissions/${r.admissionId}`)}>Open admission</Button>
                </td>
              </tr>
            ))}
            {!isLoading && rows.length === 0 && (
              <tr>
                <td colSpan={7} className="px-4 py-10 text-center text-slate-400">
                  No incomplete records — every discharged admission has a file, a complete audit and coding.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}
