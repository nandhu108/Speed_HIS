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
import { useDeleteDepartment, useDepartments, useSaveDepartment } from '../api'
import { DEPARTMENT_TYPE_LABELS, type Department, type DepartmentType } from '../types'

interface EditorState {
  id?: number
  code: string
  name: string
  description: string
  type: DepartmentType
  isActive: boolean
}

type TypeFilter = 'all' | `${DepartmentType}`

/**
 * Departments master — bespoke (not the generic SimpleMasterPage) since each department is
 * Clinical or Non-Clinical. Only Clinical departments are offered when registering an OP visit
 * or admitting an IP patient.
 */
export function DepartmentsPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.DEPARTMENT.MANAGE')
  const { data: rows = [], isLoading } = useDepartments()
  const save = useSaveDepartment()
  const remove = useDeleteDepartment()

  const [typeFilter, setTypeFilter] = useState<TypeFilter>('all')
  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const visible = rows.filter((r) => typeFilter === 'all' || String(r.type) === typeFilter)

  const openEditor = (row: Department | null) => {
    setError(null)
    setEditor(
      row
        ? { id: row.id, code: row.code, name: row.name, description: row.description ?? '', type: row.type, isActive: row.isActive }
        : { code: '', name: '', description: '', type: 1, isActive: true },
    )
  }

  const submit = async () => {
    if (!editor) return
    setError(null)
    try {
      await save.mutateAsync({
        id: editor.id,
        code: editor.code,
        name: editor.name,
        description: editor.description || null,
        type: editor.type,
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
        title="Departments"
        subtitle="Clinical and non-clinical hospital departments — OP and IP registration use clinical departments only"
        actions={canManage && <Button onClick={() => openEditor(null)}>New department</Button>}
      />

      <div className="mb-3 flex items-center gap-2">
        <Select
          label="Type"
          value={typeFilter}
          onChange={(e) => setTypeFilter(e.target.value as TypeFilter)}
          options={[
            { value: 'all', label: 'All types' },
            { value: '1', label: DEPARTMENT_TYPE_LABELS[1] },
            { value: '2', label: DEPARTMENT_TYPE_LABELS[2] },
          ]}
          className="w-44"
        />
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Description</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {visible.map((row) => (
              <tr key={row.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{row.code}</td>
                <td className="px-4 py-2.5 font-medium text-slate-700">{row.name}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={row.type === 1 ? 'info' : 'neutral'}>{DEPARTMENT_TYPE_LABELS[row.type]}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-slate-500">{row.description ?? '—'}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={row.isActive ? 'success' : 'neutral'}>{row.isActive ? 'Active' : 'Inactive'}</StatusBadge>
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
                          if (window.confirm(`Delete department '${row.name}'?`)) {
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
            {!isLoading && visible.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  No departments found.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit department' : 'New department'}
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
            <Select
              label="Department type"
              required
              value={String(editor.type)}
              onChange={(e) => setEditor({ ...editor, type: Number(e.target.value) as DepartmentType })}
              options={[
                { value: '1', label: DEPARTMENT_TYPE_LABELS[1] },
                { value: '2', label: DEPARTMENT_TYPE_LABELS[2] },
              ]}
            />
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
