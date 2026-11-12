import { useState } from 'react'
import { endpoints } from '../../../api/endpoints'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useSimpleMaster } from '../../masters/api'
import { useAuth } from '../../auth/AuthContext'
import { useCloseNonConformance, useMarkNonConformanceInProgress, useNonConformances, useRaiseNonConformance } from '../api'
import { NC_SEVERITY_LABELS, NC_STATUS_LABELS, type NonConformanceSeverity, type NonConformanceStatus } from '../types'

const statusTone: Record<NonConformanceStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'warning', 2: 'info', 3: 'success',
}
const severityTone: Record<NonConformanceSeverity, 'warning' | 'danger'> = { 1: 'warning', 2: 'danger' }

export function NonConformancesPage() {
  const { hasPermission } = useAuth()
  const canCreate = hasPermission('QUALITY.NONCONFORMANCE.CREATE')
  const canManage = hasPermission('QUALITY.NONCONFORMANCE.MANAGE')
  const [page, setPage] = useState(1)
  const { data, isLoading } = useNonConformances({ page, pageSize: 20 })
  const [raising, setRaising] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const markInProgress = useMarkNonConformanceInProgress()
  const close = useCloseNonConformance()

  const run = async (fn: () => Promise<unknown>) => {
    setError(null)
    try { await fn() } catch (err) { setError(errorMessage(err)) }
  }

  return (
    <>
      <PageHeader
        title="Non-Conformances"
        subtitle="Findings raised against an NABH standard"
        actions={canCreate && <Button onClick={() => setRaising(true)}>Raise finding</Button>}
      />
      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Title</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Standard</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Department</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Severity</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              {canManage && <th className="px-4 py-2.5 font-medium text-slate-600">Actions</th>}
            </tr>
          </thead>
          <tbody>
            {(data?.items ?? []).map((n) => (
              <tr key={n.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{n.title}</td>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{n.standardCode ?? '—'}</td>
                <td className="px-4 py-2.5 text-slate-700">{n.departmentName ?? '—'}</td>
                <td className="px-4 py-2.5"><StatusBadge tone={severityTone[n.severity]}>{NC_SEVERITY_LABELS[n.severity]}</StatusBadge></td>
                <td className="px-4 py-2.5"><StatusBadge tone={statusTone[n.status]}>{NC_STATUS_LABELS[n.status]}</StatusBadge></td>
                {canManage && (
                  <td className="px-4 py-2.5">
                    <div className="flex gap-1">
                      {n.status === 1 && <Button variant="ghost" onClick={() => run(() => markInProgress.mutateAsync(n.id))}>In Progress</Button>}
                      {n.status !== 3 && <Button variant="ghost" onClick={() => run(() => close.mutateAsync({ id: n.id, notes: null }))}>Close</Button>}
                    </div>
                  </td>
                )}
              </tr>
            ))}
            {!isLoading && (data?.items.length ?? 0) === 0 && (
              <tr><td colSpan={canManage ? 6 : 5} className="px-4 py-10 text-center text-slate-400">No non-conformances raised.</td></tr>
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

      {raising && <RaiseModal onClose={() => setRaising(false)} />}
    </>
  )
}

function RaiseModal({ onClose }: { onClose: () => void }) {
  const raise = useRaiseNonConformance()
  const { data: standards = [] } = useSimpleMaster('quality-nabh-standards', endpoints.quality.nabhStandards)
  const [standardId, setStandardId] = useState('')
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [severity, setSeverity] = useState<NonConformanceSeverity>(1)
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    if (!title.trim() || !description.trim()) { setError('Title and description are required.'); return }
    try {
      await raise.mutateAsync({ standardId: standardId ? Number(standardId) : null, departmentId: null, title, description, severity })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal open title="Raise non-conformance finding" onClose={onClose} footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={submit} loading={raise.isPending}>Raise</Button></>}>
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Select label="NABH standard" placeholder="None" value={standardId} onChange={(e) => setStandardId(e.target.value)} options={standards.map((s) => ({ value: s.id, label: `${s.code} — ${s.name}` }))} />
        <Input label="Title" value={title} onChange={(e) => setTitle(e.target.value)} />
        <Input label="Description" value={description} onChange={(e) => setDescription(e.target.value)} />
        <Select
          label="Severity"
          value={severity}
          onChange={(e) => setSeverity(Number(e.target.value) as NonConformanceSeverity)}
          options={Object.entries(NC_SEVERITY_LABELS).map(([value, label]) => ({ value, label }))}
        />
      </div>
    </Modal>
  )
}
