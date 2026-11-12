import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDeleteSimpleMaster, useSaveSimpleMaster, useSimpleMaster } from '../api'
import type { SimpleMaster } from '../types'

export interface SimpleMasterSectionProps {
  /** Singular noun used in buttons, modal titles and confirm prompts, e.g. "department". */
  noun: string
  queryKey: string
  endpoint: string
  managePermission: string
}

interface EditorState {
  id?: number
  code: string
  name: string
  description: string
  isActive: boolean
}

/** Toolbar + table + editor modal for any code/name/description lookup master. */
export function SimpleMasterSection({ noun, queryKey, endpoint, managePermission }: SimpleMasterSectionProps) {
  const { hasPermission } = useAuth()
  const canManage = hasPermission(managePermission)
  const { data: rows = [], isLoading } = useSimpleMaster(queryKey, endpoint)
  const save = useSaveSimpleMaster(queryKey, endpoint)
  const remove = useDeleteSimpleMaster(queryKey, endpoint)

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (row: SimpleMaster | null) => {
    setError(null)
    setEditor(
      row
        ? { id: row.id, code: row.code, name: row.name, description: row.description ?? '', isActive: row.isActive }
        : { code: '', name: '', description: '', isActive: true },
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
        isActive: editor.isActive,
      })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      {canManage && (
        <div className="flex justify-end border-b border-slate-200 px-4 py-3">
          <Button onClick={() => openEditor(null)}>New {noun}</Button>
        </div>
      )}
      {isLoading && <Spinner />}
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50 text-left">
            <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Description</th>
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
                        if (window.confirm(`Delete ${noun} '${row.name}'?`)) {
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
              <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                No records yet.
              </td>
            </tr>
          )}
        </tbody>
      </table>

      {editor && (
        <Modal
          open
          title={editor.id ? `Edit ${noun}` : `New ${noun}`}
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
            <Input label="Code" value={editor.code} onChange={(e) => setEditor({ ...editor, code: e.target.value })} />
            <Input label="Name" value={editor.name} onChange={(e) => setEditor({ ...editor, name: e.target.value })} />
            <Input
              label="Description"
              value={editor.description}
              onChange={(e) => setEditor({ ...editor, description: e.target.value })}
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
    </div>
  )
}
