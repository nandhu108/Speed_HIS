import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { endpoints } from '../../../api/endpoints'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useAreas, useDeleteArea, useSaveArea, useSimpleMaster } from '../api'
import type { Area } from '../types'

interface EditorState {
  id?: number
  code: string
  name: string
  cityId: string
  isActive: boolean
}

const m = endpoints.masters

export function AreasPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.AREA.MANAGE')
  const [cityFilter, setCityFilter] = useState('')
  const { data: cities = [] } = useSimpleMaster('cities', m.cities)
  const { data: rows = [], isLoading } = useAreas(cityFilter ? Number(cityFilter) : undefined)
  const save = useSaveArea()
  const remove = useDeleteArea()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const cityOptions = cities.map((c) => ({ value: c.id, label: c.name }))

  const openEditor = (row: Area | null) => {
    setError(null)
    setEditor(
      row
        ? { id: row.id, code: row.code, name: row.name, cityId: String(row.cityId), isActive: row.isActive }
        : { code: '', name: '', cityId: cityFilter, isActive: true },
    )
  }

  const submit = async () => {
    if (!editor) return
    if (!editor.cityId) {
      setError('Select a city.')
      return
    }
    try {
      await save.mutateAsync({
        id: editor.id,
        code: editor.code,
        name: editor.name,
        cityId: Number(editor.cityId),
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
        title="Areas"
        subtitle="Areas within a City — used for patient address mapping"
        actions={canManage && <Button onClick={() => openEditor(null)}>New area</Button>}
      />

      <div className="mb-3 max-w-xs">
        <Select
          label="Filter by city"
          placeholder="All cities"
          options={cityOptions}
          value={cityFilter}
          onChange={(e) => setCityFilter(e.target.value)}
        />
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">City</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{row.code}</td>
                <td className="px-4 py-2.5 font-medium text-slate-700">{row.name}</td>
                <td className="px-4 py-2.5 text-slate-500">{row.cityName}</td>
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
                          if (window.confirm(`Delete area '${row.name}'?`)) {
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
                  No areas yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit area' : 'New area'}
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
            <Select
              label="City"
              placeholder="Select city…"
              options={cityOptions}
              value={editor.cityId}
              onChange={(e) => setEditor({ ...editor, cityId: e.target.value })}
            />
            <div className="grid grid-cols-2 gap-3">
              <Input label="Code" value={editor.code} onChange={(e) => setEditor({ ...editor, code: e.target.value })} />
              <Input label="Name" value={editor.name} onChange={(e) => setEditor({ ...editor, name: e.target.value })} />
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
