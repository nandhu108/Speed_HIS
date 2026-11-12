import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDeleteTestProfile, useLabTests, useSaveTestProfile, useTestProfile, useTestProfiles } from '../api'

export function TestProfilesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('LAB.PROFILE.MANAGE')
  const { data: profiles = [], isLoading } = useTestProfiles()
  const remove = useDeleteTestProfile()

  const [editingId, setEditingId] = useState<number | null>(null)
  const [creating, setCreating] = useState(false)

  return (
    <>
      <PageHeader
        title="Test Profiles"
        subtitle="Bundles of tests ordered together (e.g. Liver Function Test)"
        actions={canManage && <Button onClick={() => setCreating(true)}>New profile</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Tests</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Price</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {profiles.map((p) => (
              <tr key={p.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{p.code}</td>
                <td className="px-4 py-2.5 font-medium text-slate-700">{p.name}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{p.testCount}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{p.price?.toFixed(2) ?? '—'}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={p.isActive ? 'success' : 'neutral'}>{p.isActive ? 'Active' : 'Inactive'}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canManage && (
                    <div className="flex justify-end gap-1">
                      <Button variant="ghost" onClick={() => setEditingId(p.id)}>
                        Edit
                      </Button>
                      <Button
                        variant="ghost"
                        className="text-red-600"
                        onClick={async () => {
                          if (window.confirm(`Delete profile '${p.name}'?`)) {
                            try {
                              await remove.mutateAsync(p.id)
                            } catch (err) {
                              window.alert(errorMessage(err))
                            }
                          }
                        }}
                      >
                        Delete
                      </Button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && profiles.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  No test profiles yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <ProfileEditor id={null} onClose={() => setCreating(false)} />}
      {editingId !== null && <ProfileEditor id={editingId} onClose={() => setEditingId(null)} />}
    </>
  )
}

function ProfileEditor({ id, onClose }: { id: number | null; onClose: () => void }) {
  const { data: existing } = useTestProfile(id)
  const { data: tests = [] } = useLabTests()
  const save = useSaveTestProfile()
  const [error, setError] = useState<string | null>(null)

  const [code, setCode] = useState(existing?.code ?? '')
  const [name, setName] = useState(existing?.name ?? '')
  const [price, setPrice] = useState(existing?.price != null ? String(existing.price) : '')
  const [isActive, setIsActive] = useState(existing?.isActive ?? true)
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set(existing?.tests.map((t) => t.labTestId) ?? []))

  if (existing && code === '' && existing.code !== '') {
    setCode(existing.code)
    setName(existing.name)
    setPrice(existing.price != null ? String(existing.price) : '')
    setIsActive(existing.isActive)
    setSelectedIds(new Set(existing.tests.map((t) => t.labTestId)))
  }

  const toggle = (testId: number) => {
    const next = new Set(selectedIds)
    if (next.has(testId)) next.delete(testId)
    else next.add(testId)
    setSelectedIds(next)
  }

  const submit = async () => {
    setError(null)
    try {
      await save.mutateAsync({
        id: id ?? undefined,
        code,
        name,
        price: price ? Number(price) : null,
        labTestIds: Array.from(selectedIds),
        isActive,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={id ? 'Edit test profile' : 'New test profile'}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={save.isPending} disabled={!code || !name || selectedIds.size === 0}>
            Save
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <div className="grid grid-cols-2 gap-3">
          <Input label="Code" value={code} onChange={(e) => setCode(e.target.value)} />
          <Input label="Name" value={name} onChange={(e) => setName(e.target.value)} />
          <Input label="Price override (optional)" type="number" min={0} value={price} onChange={(e) => setPrice(e.target.value)} />
        </div>
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
          Active
        </label>

        <div>
          <p className="mb-1 text-sm font-medium text-slate-700">Member tests</p>
          <div className="max-h-64 overflow-y-auto rounded-md border border-slate-200 p-2">
            {tests.map((t) => (
              <label key={t.id} className="flex items-center gap-2 px-2 py-1 text-sm text-slate-700 hover:bg-slate-50">
                <input type="checkbox" checked={selectedIds.has(t.id)} onChange={() => toggle(t.id)} />
                {t.name} <span className="font-mono text-xs text-slate-400">({t.code})</span>
              </label>
            ))}
          </div>
        </div>
      </div>
    </Modal>
  )
}
