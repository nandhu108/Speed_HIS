import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDeletePatientCategory, usePatientCategories, useSavePatientCategory } from '../api'
import type { PatientCategory } from '../types'

const DEFAULT_COLOR = '#3B82F6'

interface EditorState {
  id?: number
  code: string
  name: string
  description: string
  colorHex: string
  isActive: boolean
}

/** "Patient-type colors" — bespoke master editor (mirrors AppointmentTypesPage's shape) since
 * PatientCategory carries ColorHex, shown as a badge/dot here and next to the category on
 * PatientsListPage. */
export function PatientCategoriesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.PATIENTCATEGORY.MANAGE')
  const { data: rows = [], isLoading } = usePatientCategories()
  const save = useSavePatientCategory()
  const remove = useDeletePatientCategory()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (row: PatientCategory | null) => {
    setError(null)
    setEditor(
      row
        ? {
            id: row.id,
            code: row.code,
            name: row.name,
            description: row.description ?? '',
            colorHex: row.colorHex ?? DEFAULT_COLOR,
            isActive: row.isActive,
          }
        : { code: '', name: '', description: '', colorHex: DEFAULT_COLOR, isActive: true },
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
        colorHex: editor.colorHex || null,
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
        title="Patient Categories"
        subtitle="Billing categories such as General, Corporate, Foreign National — each with a display color for front-desk lists"
        actions={canManage && <Button onClick={() => openEditor(null)}>New patient category</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Description</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Color</th>
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
                  {row.colorHex ? (
                    <span className="inline-flex items-center gap-2">
                      <span className="inline-block size-3.5 rounded-full border border-slate-200" style={{ backgroundColor: row.colorHex }} />
                      <span className="font-mono text-xs text-slate-500">{row.colorHex}</span>
                    </span>
                  ) : (
                    '—'
                  )}
                </td>
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
                          if (window.confirm(`Delete patient category '${row.name}'?`)) {
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
                  No patient categories yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit patient category' : 'New patient category'}
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
            <div>
              <span className="mb-1.5 block text-[10.5px] font-bold tracking-wider text-ink-faint uppercase">Color</span>
              <div className="flex items-center gap-2">
                <input
                  type="color"
                  className="h-9 w-12 cursor-pointer rounded-md border border-slate-300"
                  value={editor.colorHex || DEFAULT_COLOR}
                  onChange={(e) => setEditor({ ...editor, colorHex: e.target.value })}
                />
                <Input
                  value={editor.colorHex}
                  placeholder="#3B82F6"
                  onChange={(e) => setEditor({ ...editor, colorHex: e.target.value })}
                />
              </div>
            </div>
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
