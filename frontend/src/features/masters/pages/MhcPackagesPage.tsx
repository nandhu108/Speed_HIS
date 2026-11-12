import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDeleteMhcPackage, useMhcPackages, useSaveMhcPackage } from '../api'
import type { MhcPackage } from '../types'

interface EditorState {
  id?: number
  code: string
  name: string
  description: string
  price: string
  isActive: boolean
}

export function MhcPackagesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.MHCPACKAGE.MANAGE')
  const { data: rows = [], isLoading } = useMhcPackages()
  const save = useSaveMhcPackage()
  const remove = useDeleteMhcPackage()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (row: MhcPackage | null) => {
    setError(null)
    setEditor(
      row
        ? {
            id: row.id,
            code: row.code,
            name: row.name,
            description: row.description ?? '',
            price: String(row.price),
            isActive: row.isActive,
          }
        : { code: '', name: '', description: '', price: '', isActive: true },
    )
  }

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({
        id: editor.id,
        code: editor.code,
        name: editor.name,
        description: editor.description || null,
        price: Number(editor.price) || 0,
        isActive: editor.isActive,
      })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader
        title="MHC Packages"
        subtitle="Master Health Checkup packages priced at MHC registration"
        actions={canManage && <Button onClick={() => openEditor(null)}>New package</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Description</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Price</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{row.code}</td>
                <td className="px-4 py-2.5 font-medium text-slate-700">{row.name}</td>
                <td className="px-4 py-2.5 text-slate-500">{row.description ?? '—'}</td>
                <td className="px-4 py-2.5 font-mono text-slate-700">{row.price.toFixed(2)}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={row.isActive ? 'success' : 'neutral'}>
                    {row.isActive ? 'Active' : 'Inactive'}
                  </StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canManage && (
                    <div className="flex justify-end gap-1">
                      <Button variant="ghost" onClick={() => openEditor(row)}>
                        Edit
                      </Button>
                      <Button
                        variant="ghost"
                        className="text-red-600"
                        onClick={async () => {
                          if (window.confirm(`Delete package '${row.name}'?`)) {
                            try {
                              await remove.mutateAsync(row.id)
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
            {!isLoading && rows.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  No packages yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit package' : 'New package'}
          onClose={() => setEditor(null)}
          footer={
            <>
              <Button variant="secondary" onClick={() => setEditor(null)}>
                Cancel
              </Button>
              <Button onClick={submit} loading={save.isPending}>
                Save
              </Button>
            </>
          }
        >
          <div className="space-y-3">
            {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
            <div className="grid grid-cols-2 gap-3">
              <Input label="Code" value={editor.code} onChange={(e) => setEditor({ ...editor, code: e.target.value })} />
              <Input label="Name" value={editor.name} onChange={(e) => setEditor({ ...editor, name: e.target.value })} />
            </div>
            <Input
              label="Description"
              value={editor.description}
              onChange={(e) => setEditor({ ...editor, description: e.target.value })}
            />
            <Input
              label="Price"
              type="number"
              value={editor.price}
              onChange={(e) => setEditor({ ...editor, price: e.target.value })}
            />
            <label className="flex items-center gap-2 text-sm text-slate-700">
              <input
                type="checkbox"
                checked={editor.isActive}
                onChange={(e) => setEditor({ ...editor, isActive: e.target.checked })}
              />
              Active
            </label>
          </div>
        </Modal>
      )}
    </>
  )
}
