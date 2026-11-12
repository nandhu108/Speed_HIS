import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useRecordFiles } from '../api'
import { RECORD_FILE_STATUS_LABELS, type RecordFileListItem } from '../types'

export function RecordFilesPage() {
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const { data: files = [], isLoading } = useRecordFiles({ search: search || undefined })

  return (
    <>
      <PageHeader
        title="Medical Record Files"
        subtitle="The MRD file board — a record file is opened from an admission's IPD detail page"
      />

      <div className="mb-3">
        <Input placeholder="Search by file number, patient name or UHID" value={search} onChange={(e) => setSearch(e.target.value)} className="max-w-sm" />
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">File No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Admission</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Location</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Custodian</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            </tr>
          </thead>
          <tbody>
            {files.map((f: RecordFileListItem) => (
              <tr
                key={f.id}
                className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50"
                onClick={() => navigate(`/mrd/files/${f.id}`)}
              >
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{f.fileNumber}</td>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{f.admissionNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">
                  {f.patientName} <span className="text-slate-400">({f.patientUhid})</span>
                </td>
                <td className="px-4 py-2.5 text-slate-700">{f.currentLocation}</td>
                <td className="px-4 py-2.5 text-slate-700">{f.currentCustodianName ?? '—'}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={f.status === 1 ? 'success' : 'neutral'}>{RECORD_FILE_STATUS_LABELS[f.status]}</StatusBadge>
                </td>
              </tr>
            ))}
            {!isLoading && files.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  No medical record files yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}
