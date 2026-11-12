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
import { useBranches, useMenuPathOptions, useSaveBranch, type SaveBranchPayload } from '../api'
import type { Branch } from '../types'

const emptyBranch: SaveBranchPayload = {
  code: '',
  name: '',
  address: null,
  city: null,
  state: null,
  pincode: null,
  phone: null,
  email: null,
  gstNumber: null,
  reportHeader: null,
  timeZone: 'Asia/Kolkata',
  isActive: true,
  defaultLandingPath: null,
}

export function BranchesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('ADMIN.BRANCH.MANAGE')
  const { data: branches = [], isLoading } = useBranches()
  const saveBranch = useSaveBranch()
  const pathOptions = useMenuPathOptions()

  const [editor, setEditor] = useState<SaveBranchPayload | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (branch: Branch | null) => {
    setError(null)
    setEditor(branch ? { ...branch } : { ...emptyBranch })
  }

  const submit = async () => {
    if (!editor) return
    setError(null)
    try {
      await saveBranch.mutateAsync(editor)
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const set = (patch: Partial<SaveBranchPayload>) => setEditor((current) => ({ ...current!, ...patch }))

  return (
    <>
      <PageHeader
        title="Branches"
        subtitle="Hospital units — all transactional data is scoped to a branch"
        actions={canManage && <Button onClick={() => openEditor(null)}>New branch</Button>}
      />

      {isLoading && <Spinner />}

      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
        {branches.map((branch) => (
          <div key={branch.id} className="rounded-lg border border-slate-200 bg-white p-4">
            <div className="flex items-start justify-between">
              <div>
                <p className="font-semibold text-slate-800">{branch.name}</p>
                <p className="text-xs text-slate-400">{branch.code}</p>
              </div>
              <StatusBadge tone={branch.isActive ? 'success' : 'neutral'}>
                {branch.isActive ? 'Active' : 'Inactive'}
              </StatusBadge>
            </div>
            <p className="mt-2 text-sm text-slate-500">
              {[branch.address, branch.city, branch.state].filter(Boolean).join(', ') || 'No address on file'}
            </p>
            {branch.phone && <p className="mt-1 text-xs text-slate-400">{branch.phone}</p>}
            {canManage && (
              <div className="mt-3">
                <Button variant="secondary" onClick={() => openEditor(branch)}>
                  Edit
                </Button>
              </div>
            )}
          </div>
        ))}
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit branch' : 'New branch'}
          onClose={() => setEditor(null)}
          footer={
            <>
              <Button variant="secondary" onClick={() => setEditor(null)}>
                Cancel
              </Button>
              <Button onClick={submit} loading={saveBranch.isPending}>
                Save
              </Button>
            </>
          }
        >
          <div className="grid max-h-[60vh] grid-cols-2 gap-3 overflow-y-auto pr-1">
            {error && <p className="col-span-2 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
            <Input label="Code" value={editor.code} onChange={(e) => set({ code: e.target.value.toUpperCase() })} />
            <Input label="Name" value={editor.name} onChange={(e) => set({ name: e.target.value })} />
            <Input
              label="Address"
              className="col-span-2"
              value={editor.address ?? ''}
              onChange={(e) => set({ address: e.target.value || null })}
            />
            <Input label="City" value={editor.city ?? ''} onChange={(e) => set({ city: e.target.value || null })} />
            <Input label="State" value={editor.state ?? ''} onChange={(e) => set({ state: e.target.value || null })} />
            <Input
              label="Pincode"
              value={editor.pincode ?? ''}
              onChange={(e) => set({ pincode: e.target.value || null })}
            />
            <Input label="Phone" value={editor.phone ?? ''} onChange={(e) => set({ phone: e.target.value || null })} />
            <Input label="Email" value={editor.email ?? ''} onChange={(e) => set({ email: e.target.value || null })} />
            <Input
              label="GST number"
              value={editor.gstNumber ?? ''}
              onChange={(e) => set({ gstNumber: e.target.value || null })}
            />
            <Input
              label="Report header"
              className="col-span-2"
              value={editor.reportHeader ?? ''}
              onChange={(e) => set({ reportHeader: e.target.value || null })}
            />
            <Input label="Time zone" value={editor.timeZone} onChange={(e) => set({ timeZone: e.target.value })} />
            <Select
              label="Default landing page"
              placeholder="No override — show the dashboard"
              options={pathOptions}
              value={editor.defaultLandingPath ?? ''}
              onChange={(e) => set({ defaultLandingPath: e.target.value || null })}
            />
            <label className="flex items-end gap-2 pb-2 text-sm text-slate-700">
              <input type="checkbox" checked={editor.isActive} onChange={(e) => set({ isActive: e.target.checked })} />
              Active
            </label>
          </div>
        </Modal>
      )}
    </>
  )
}
