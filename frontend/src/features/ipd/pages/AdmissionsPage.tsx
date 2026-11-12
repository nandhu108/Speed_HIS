import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { AdmitPatientModal } from '../components/AdmitPatientModal'
import { useAdmissions } from '../api'
import { ADMISSION_STATUS_LABELS, ADMISSION_TYPE_LABELS, type AdmissionStatus } from '../types'

const statusTone: Record<AdmissionStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'success', 2: 'neutral', 3: 'danger',
}

export function AdmissionsPage() {
  const navigate = useNavigate()
  const { hasPermission } = useAuth()
  const canAdmit = hasPermission('IPD.ADMISSION.CREATE')
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const { data, isLoading } = useAdmissions({ page, pageSize: 20, search: search || undefined })
  const [admitting, setAdmitting] = useState(false)

  return (
    <>
      <PageHeader
        title="Admissions"
        subtitle="IP admissions — request, bed allocation, consultant mapping, consent and deposit"
        actions={canAdmit && <Button onClick={() => setAdmitting(true)}>Admit patient</Button>}
      />

      <div className="mb-4 max-w-sm">
        <Input placeholder="Search admission no, UHID, name…" value={search} onChange={(e) => { setSearch(e.target.value); setPage(1) }} />
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Admission No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Consultant</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Bed</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            </tr>
          </thead>
          <tbody>
            {(data?.items ?? []).map((a) => (
              <tr
                key={a.id}
                className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50"
                onClick={() => navigate(`/ipd/admissions/${a.id}`)}
              >
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{a.admissionNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">
                  {a.patientName} <span className="font-mono text-xs text-slate-400">({a.patientUhid})</span>
                </td>
                <td className="px-4 py-2.5 text-slate-700">{ADMISSION_TYPE_LABELS[a.admissionType]}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.primaryConsultantName}</td>
                <td className="px-4 py-2.5 text-slate-700">
                  {a.wardName}/{a.roomName}/{a.bedCode}
                </td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={statusTone[a.status]}>{ADMISSION_STATUS_LABELS[a.status]}</StatusBadge>
                </td>
              </tr>
            ))}
            {!isLoading && (data?.items.length ?? 0) === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  No admissions found.
                </td>
              </tr>
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

      {admitting && (
        <AdmitPatientModal onClose={() => setAdmitting(false)} onAdmitted={(id) => navigate(`/ipd/admissions/${id}`)} />
      )}
    </>
  )
}
