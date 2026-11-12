import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import {
  useApproveDestruction,
  useDeleteRetentionPolicy,
  useDestroyRecordFile,
  useDestructionRegister,
  useEligibleFiles,
  useFlagForDestruction,
  useRetentionPolicies,
  useSaveRetentionPolicy,
} from '../api'
import { DESTRUCTION_STATUS_LABELS, type DestructionRegisterEntryDto, type RetentionPolicyDto } from '../types'

type Tab = 'policies' | 'destruction'

export function RetentionDestructionPage() {
  const [tab, setTab] = useState<Tab>('policies')
  const tabs: { key: Tab; label: string }[] = [
    { key: 'policies', label: 'Retention Policies' },
    { key: 'destruction', label: 'Destruction Register' },
  ]

  return (
    <>
      <PageHeader title="Record Retention & Destruction" subtitle="Retention windows per record type and the destruction register" />
      <div className="mb-4 flex gap-1 border-b border-slate-200">
        {tabs.map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`border-b-2 px-3 py-2 text-sm font-medium ${
              tab === t.key ? 'border-primary-600 text-primary-700' : 'border-transparent text-slate-500 hover:text-slate-700'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>
      {tab === 'policies' && <PoliciesTab />}
      {tab === 'destruction' && <DestructionTab />}
    </>
  )
}

function PoliciesTab() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MRD.RETENTION.MANAGE')
  const { data: policies = [], isLoading } = useRetentionPolicies()
  const remove = useDeleteRetentionPolicy()
  const [editing, setEditing] = useState<RetentionPolicyDto | 'new' | null>(null)
  const [error, setError] = useState<string | null>(null)

  return (
    <div>
      {canManage && (
        <div className="mb-3 flex justify-end">
          <Button onClick={() => setEditing('new')}>New policy</Button>
        </div>
      )}
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Retention (years)</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Description</th>
              {canManage && <th className="px-4 py-2.5 font-medium text-slate-600">Actions</th>}
            </tr>
          </thead>
          <tbody>
            {policies.map((p) => (
              <tr key={p.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{p.code}</td>
                <td className="px-4 py-2.5 text-slate-700">{p.name}</td>
                <td className="px-4 py-2.5 text-slate-700">{p.retentionYears}</td>
                <td className="px-4 py-2.5 text-slate-500">{p.description ?? '—'}</td>
                {canManage && (
                  <td className="space-x-2 px-4 py-2.5">
                    <Button variant="ghost" onClick={() => setEditing(p)}>Edit</Button>
                    <Button
                      variant="ghost"
                      className="text-red-600"
                      onClick={async () => {
                        setError(null)
                        try {
                          await remove.mutateAsync(p.id)
                        } catch (err) {
                          setError(errorMessage(err))
                        }
                      }}
                    >
                      Delete
                    </Button>
                  </td>
                )}
              </tr>
            ))}
            {!isLoading && policies.length === 0 && (
              <tr>
                <td colSpan={canManage ? 5 : 4} className="px-4 py-10 text-center text-slate-400">
                  No retention policies defined.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
      {editing && <PolicyModal policy={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />}
    </div>
  )
}

function PolicyModal({ policy, onClose }: { policy: RetentionPolicyDto | null; onClose: () => void }) {
  const save = useSaveRetentionPolicy()
  const [code, setCode] = useState(policy?.code ?? '')
  const [name, setName] = useState(policy?.name ?? '')
  const [years, setYears] = useState(String(policy?.retentionYears ?? 5))
  const [description, setDescription] = useState(policy?.description ?? '')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await save.mutateAsync({ id: policy?.id, code, name, retentionYears: Number(years), description: description || null })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={policy ? 'Edit retention policy' : 'New retention policy'}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={save.isPending} disabled={!code || !name}>Save</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Input label="Code" value={code} onChange={(e) => setCode(e.target.value.toUpperCase())} />
        <Input label="Name" value={name} onChange={(e) => setName(e.target.value)} />
        <Input label="Retention years" type="number" value={years} onChange={(e) => setYears(e.target.value)} />
        <Input label="Description" value={description} onChange={(e) => setDescription(e.target.value)} />
      </div>
    </Modal>
  )
}

function DestructionTab() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MRD.DESTRUCTION.MANAGE')
  const { data: policies = [] } = useRetentionPolicies()
  const [policyId, setPolicyId] = useState('')
  const { data: eligible = [] } = useEligibleFiles(policyId ? Number(policyId) : null)
  const { data: entries = [], isLoading } = useDestructionRegister()

  const flag = useFlagForDestruction()
  const approve = useApproveDestruction()
  const [destroying, setDestroying] = useState<DestructionRegisterEntryDto | null>(null)
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
    <div className="space-y-4">
      {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      {canManage && (
        <div className="rounded-lg border border-slate-200 bg-white p-4">
          <h3 className="mb-2 text-sm font-semibold text-slate-700">Files eligible for destruction</h3>
          <Select
            label="Retention policy"
            value={policyId}
            onChange={(e) => setPolicyId(e.target.value)}
            placeholder="Select a policy"
            options={policies.map((p) => ({ value: p.id, label: `${p.name} (${p.retentionYears}y)` }))}
            className="mb-3 max-w-sm"
          />
          <table className="w-full text-xs">
            <tbody>
              {eligible.map((f) => (
                <tr key={f.recordFileId} className="border-t border-slate-100">
                  <td className="py-1.5 font-mono text-slate-500">{f.fileNumber}</td>
                  <td className="py-1.5">{f.patientName}</td>
                  <td className="py-1.5 text-slate-500">Eligible since {f.eligibleDate}</td>
                  <td className="py-1.5">
                    <Button
                      variant="secondary"
                      onClick={() => run(() => flag.mutateAsync({ recordFileId: f.recordFileId, retentionPolicyId: f.retentionPolicyId }))}
                    >
                      Flag for destruction
                    </Button>
                  </td>
                </tr>
              ))}
              {policyId && eligible.length === 0 && (
                <tr>
                  <td colSpan={4} className="py-4 text-center text-slate-400">No files eligible under this policy.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">File No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Policy</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Eligible Date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              {canManage && <th className="px-4 py-2.5 font-medium text-slate-600">Actions</th>}
            </tr>
          </thead>
          <tbody>
            {entries.map((e) => (
              <tr key={e.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{e.fileNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{e.retentionPolicyName}</td>
                <td className="px-4 py-2.5 text-slate-700">{e.eligibleDate}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={e.status === 3 ? 'danger' : e.status === 2 ? 'warning' : 'neutral'}>
                    {DESTRUCTION_STATUS_LABELS[e.status]}
                  </StatusBadge>
                </td>
                {canManage && (
                  <td className="space-x-2 px-4 py-2.5">
                    {e.status === 1 && <Button variant="secondary" onClick={() => run(() => approve.mutateAsync(e.id))}>Approve</Button>}
                    {e.status === 2 && <Button variant="secondary" className="text-red-600" onClick={() => setDestroying(e)}>Destroy</Button>}
                  </td>
                )}
              </tr>
            ))}
            {!isLoading && entries.length === 0 && (
              <tr>
                <td colSpan={canManage ? 5 : 4} className="px-4 py-10 text-center text-slate-400">
                  No destruction register entries.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {destroying && <DestroyModal entry={destroying} onClose={() => setDestroying(null)} />}
    </div>
  )
}

function DestroyModal({ entry, onClose }: { entry: DestructionRegisterEntryDto; onClose: () => void }) {
  const destroy = useDestroyRecordFile()
  const [method, setMethod] = useState('')
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await destroy.mutateAsync({ id: entry.id, destructionMethod: method, notes: notes || null })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Destroy ${entry.fileNumber}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={destroy.isPending} disabled={!method}>Confirm destruction</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <p className="text-sm text-slate-600">This is irreversible. The record file will be marked destroyed.</p>
        <Input label="Destruction method" value={method} onChange={(e) => setMethod(e.target.value)} placeholder="e.g. Shredding" />
        <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>
    </Modal>
  )
}
