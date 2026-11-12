import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useUsers } from '../../admin/api'
import { useAuth } from '../../auth/AuthContext'
import { useCreateRisk, useRisks, useSetRiskStatus } from '../api'
import { RISK_STATUS_LABELS, type RiskStatus } from '../types'

const statusTone: Record<RiskStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'warning', 2: 'info', 3: 'success',
}
const scoreTone = (score: number): 'success' | 'warning' | 'danger' => (score >= 15 ? 'danger' : score >= 8 ? 'warning' : 'success')

export function RiskRegisterPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('QUALITY.RISK.MANAGE')
  const { data: risks = [], isLoading } = useRisks()
  const [creating, setCreating] = useState(false)
  const setStatus = useSetRiskStatus()
  const [error, setError] = useState<string | null>(null)

  return (
    <>
      <PageHeader title="Risk Register" subtitle="Likelihood x Impact risk matrix, mitigation and review" actions={canManage && <Button onClick={() => setCreating(true)}>New risk</Button>} />
      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Title</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Category</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Score</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Owner</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              {canManage && <th className="px-4 py-2.5 font-medium text-slate-600">Actions</th>}
            </tr>
          </thead>
          <tbody>
            {risks.map((r) => (
              <tr key={r.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{r.title}</td>
                <td className="px-4 py-2.5 text-slate-700">{r.category ?? '—'}</td>
                <td className="px-4 py-2.5"><StatusBadge tone={scoreTone(r.riskScore)}>{r.riskScore} ({r.likelihood}x{r.impact})</StatusBadge></td>
                <td className="px-4 py-2.5 text-slate-700">{r.ownerName}</td>
                <td className="px-4 py-2.5"><StatusBadge tone={statusTone[r.status]}>{RISK_STATUS_LABELS[r.status]}</StatusBadge></td>
                {canManage && (
                  <td className="px-4 py-2.5">
                    {r.status !== 3 && (
                      <div className="flex gap-1">
                        {r.status === 1 && (
                          <Button variant="ghost" onClick={async () => { try { await setStatus.mutateAsync({ id: r.id, status: 2 }) } catch (err) { setError(errorMessage(err)) } }}>Mitigating</Button>
                        )}
                        <Button variant="ghost" onClick={async () => { try { await setStatus.mutateAsync({ id: r.id, status: 3 }) } catch (err) { setError(errorMessage(err)) } }}>Close</Button>
                      </div>
                    )}
                  </td>
                )}
              </tr>
            ))}
            {!isLoading && risks.length === 0 && (
              <tr><td colSpan={canManage ? 6 : 5} className="px-4 py-10 text-center text-slate-400">No risks recorded.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <CreateRiskModal onClose={() => setCreating(false)} />}
    </>
  )
}

function CreateRiskModal({ onClose }: { onClose: () => void }) {
  const create = useCreateRisk()
  const { data: users } = useUsers({ page: 1, pageSize: 500 })
  const [title, setTitle] = useState('')
  const [category, setCategory] = useState('')
  const [description, setDescription] = useState('')
  const [likelihood, setLikelihood] = useState('3')
  const [impact, setImpact] = useState('3')
  const [mitigationPlan, setMitigationPlan] = useState('')
  const [ownerUserId, setOwnerUserId] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    if (!title.trim() || !description.trim() || !ownerUserId) { setError('Title, description and owner are required.'); return }
    try {
      await create.mutateAsync({
        title, category: category || null, description, likelihood: Number(likelihood), impact: Number(impact),
        mitigationPlan: mitigationPlan || null, ownerUserId: Number(ownerUserId),
        identifiedDate: new Date().toISOString().slice(0, 10), reviewDate: null,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal open title="New risk" onClose={onClose} footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={submit} loading={create.isPending}>Create</Button></>}>
      <div className="max-h-[65vh] space-y-3 overflow-y-auto">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Input label="Title" value={title} onChange={(e) => setTitle(e.target.value)} />
        <Input label="Category" value={category} onChange={(e) => setCategory(e.target.value)} />
        <Input label="Description" value={description} onChange={(e) => setDescription(e.target.value)} />
        <div className="grid grid-cols-2 gap-2">
          <Select label="Likelihood (1-5)" value={likelihood} onChange={(e) => setLikelihood(e.target.value)} options={[1, 2, 3, 4, 5].map((n) => ({ value: n, label: String(n) }))} />
          <Select label="Impact (1-5)" value={impact} onChange={(e) => setImpact(e.target.value)} options={[1, 2, 3, 4, 5].map((n) => ({ value: n, label: String(n) }))} />
        </div>
        <Input label="Mitigation plan" value={mitigationPlan} onChange={(e) => setMitigationPlan(e.target.value)} />
        <Select label="Owner" placeholder="Select owner" value={ownerUserId} onChange={(e) => setOwnerUserId(e.target.value)} options={(users?.items ?? []).map((u) => ({ value: u.id, label: u.fullName }))} />
      </div>
    </Modal>
  )
}
