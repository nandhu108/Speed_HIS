import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDeleteDesignation, useDesignations, useSaveDesignation } from '../api'
import type { Designation } from '../types'

interface EditorState {
  id?: number
  name: string
  description: string
  isActive: boolean
}

export function DesignationsPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('ADMIN.DESIGNATION.MANAGE')
  const { data: designations = [], isLoading } = useDesignations()
  const saveDesignation = useSaveDesignation()
  const deleteDesignation = useDeleteDesignation()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (designation: Designation | null) => {
    setError(null)
    setEditor(
      designation
        ? { id: designation.id, name: designation.name, description: designation.description ?? '', isActive: designation.isActive }
        : { name: '', description: '', isActive: true },
    )
  }

  const submit = async () => {
    if (!editor) return
    try {
      await saveDesignation.mutateAsync({
        id: editor.id,
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
    <>
      <PageHeader
        title="Designations"
        subtitle="Job titles assigned to users"
        actions={canManage && <Button onClick={() => openEditor(null)}>New designation</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Description</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Users</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {designations.map((designation) => (
              <tr key={designation.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-medium text-slate-700">{designation.name}</td>
                <td className="px-4 py-2.5 text-slate-500">{designation.description ?? '—'}</td>
                <td className="px-4 py-2.5 text-slate-500">{designation.userCount}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={designation.isActive ? 'success' : 'neutral'}>
                    {designation.isActive ? 'Active' : 'Inactive'}
                  </StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canManage && (
                    <div className="flex justify-end gap-1">
                      <Button variant="ghost" onClick={() => openEditor(designation)}>
                        Edit
                      </Button>
                      <Button
                        variant="ghost"
                        className="text-red-600"
                        onClick={async () => {
                          if (window.confirm(`Delete designation '${designation.name}'?`)) {
                            try {
                              await deleteDesignation.mutateAsync(designation.id)
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
            {!isLoading && designations.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                  No designations yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit designation' : 'New designation'}
          onClose={() => setEditor(null)}
          footer={
            <>
              <Button variant="secondary" onClick={() => setEditor(null)}>
                Cancel
              </Button>
              <Button onClick={submit} loading={saveDesignation.isPending}>
                Save
              </Button>
            </>
          }
        >
          <div className="space-y-3">
            {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
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
    </>
  )
}
