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
import { useSaveStore, useStores } from '../api'
import { STORE_TYPE_LABELS, type Store, type StoreType } from '../types'

export function StoresPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('PHARMACY.STORE.MANAGE')
  const { data: stores = [], isLoading } = useStores()
  const [editing, setEditing] = useState<Store | null>(null)
  const [creating, setCreating] = useState(false)

  return (
    <>
      <PageHeader
        title="Pharmacy Stores"
        subtitle="Stock-holding locations within this branch (main store, OT store, ward store…)"
        actions={canManage && <Button onClick={() => setCreating(true)}>New store</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Default</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {stores.map((s) => (
              <tr key={s.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{s.code}</td>
                <td className="px-4 py-2.5 font-medium text-slate-700">{s.name}</td>
                <td className="px-4 py-2.5 text-slate-700">{STORE_TYPE_LABELS[s.storeType]}</td>
                <td className="px-4 py-2.5 text-slate-700">{s.isDefault ? 'Yes' : ''}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={s.isActive ? 'success' : 'neutral'}>{s.isActive ? 'Active' : 'Inactive'}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canManage && (
                    <Button variant="ghost" onClick={() => setEditing(s)}>
                      Edit
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && stores.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  No stores yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <StoreEditor store={null} onClose={() => setCreating(false)} />}
      {editing && <StoreEditor store={editing} onClose={() => setEditing(null)} />}
    </>
  )
}

function StoreEditor({ store, onClose }: { store: Store | null; onClose: () => void }) {
  const save = useSaveStore()
  const [code, setCode] = useState(store?.code ?? '')
  const [name, setName] = useState(store?.name ?? '')
  const [storeType, setStoreType] = useState<StoreType>(store?.storeType ?? 1)
  const [isDefault, setIsDefault] = useState(store?.isDefault ?? false)
  const [isActive, setIsActive] = useState(store?.isActive ?? true)
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await save.mutateAsync({ id: store?.id, code, name, storeType, isDefault, isActive })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={store ? 'Edit store' : 'New store'}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={save.isPending} disabled={!code || !name}>
            Save
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Input label="Code" value={code} onChange={(e) => setCode(e.target.value)} />
        <Input label="Name" value={name} onChange={(e) => setName(e.target.value)} />
        <Select
          label="Store type"
          value={storeType}
          onChange={(e) => setStoreType(Number(e.target.value) as StoreType)}
          options={Object.entries(STORE_TYPE_LABELS).map(([value, label]) => ({ value, label }))}
        />
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" checked={isDefault} onChange={(e) => setIsDefault(e.target.checked)} />
          Default store for this branch
        </label>
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
          Active
        </label>
      </div>
    </Modal>
  )
}
