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
import { useSimpleMaster } from '../../masters/api'
import { endpoints } from '../../../api/endpoints'
import { useDeleteRadiologyTest, useRadiologyTests, useSaveRadiologyTest } from '../api'
import type { RadiologyTestListItem } from '../types'

interface EditorState {
  id?: number
  code: string
  name: string
  modalityId: string
  bodyPart: string
  requiresContrast: boolean
  prepInstructions: string
  tatHours: string
  isActive: boolean
}

export function RadiologyTestsPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('RAD.TEST.MANAGE')
  const { data: tests = [], isLoading } = useRadiologyTests()
  const { data: modalities = [] } = useSimpleMaster('modalities', endpoints.masters.modalities)
  const save = useSaveRadiologyTest()
  const remove = useDeleteRadiologyTest()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (test: RadiologyTestListItem | null) => {
    setError(null)
    setEditor(
      test
        ? {
            id: test.id, code: test.code, name: test.name, modalityId: String(test.modalityId), bodyPart: test.bodyPart,
            requiresContrast: test.requiresContrast, prepInstructions: '', tatHours: String(test.tatHours), isActive: test.isActive,
          }
        : { code: '', name: '', modalityId: '', bodyPart: '', requiresContrast: false, prepInstructions: '', tatHours: '24', isActive: true },
    )
  }

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({
        id: editor.id, code: editor.code, name: editor.name, modalityId: Number(editor.modalityId), bodyPart: editor.bodyPart,
        requiresContrast: editor.requiresContrast, prepInstructions: editor.prepInstructions || null, serviceId: null,
        tatHours: Number(editor.tatHours), isActive: editor.isActive,
      })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader
        title="Radiology Tests"
        subtitle="Orderable radiology test catalog"
        actions={canManage && <Button onClick={() => openEditor(null)}>New test</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Modality</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Body Part</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Contrast</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">TAT (hrs)</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {tests.map((t) => (
              <tr key={t.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{t.code}</td>
                <td className="px-4 py-2.5 font-medium text-slate-800">{t.name}</td>
                <td className="px-4 py-2.5 text-slate-700">{t.modalityName}</td>
                <td className="px-4 py-2.5 text-slate-700">{t.bodyPart}</td>
                <td className="px-4 py-2.5">{t.requiresContrast && <StatusBadge tone="warning">Contrast</StatusBadge>}</td>
                <td className="px-4 py-2.5 text-slate-700">{t.tatHours}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={t.isActive ? 'success' : 'neutral'}>{t.isActive ? 'Active' : 'Inactive'}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canManage && (
                    <div className="flex justify-end gap-1">
                      <Button variant="ghost" onClick={() => openEditor(t)}>Edit</Button>
                      <Button
                        variant="ghost"
                        className="text-red-600"
                        onClick={async () => {
                          if (window.confirm(`Delete radiology test '${t.name}'?`)) {
                            try {
                              await remove.mutateAsync(t.id)
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
            {!isLoading && tests.length === 0 && (
              <tr><td colSpan={8} className="px-4 py-10 text-center text-slate-400">No radiology tests yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit radiology test' : 'New radiology test'}
          onClose={() => setEditor(null)}
          footer={
            <>
              <Button variant="secondary" onClick={() => setEditor(null)}>Cancel</Button>
              <Button onClick={submit} loading={save.isPending}>Save</Button>
            </>
          }
        >
          <div className="space-y-3">
            {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
            <div className="grid grid-cols-2 gap-3">
              <Input label="Code" value={editor.code} onChange={(e) => setEditor({ ...editor, code: e.target.value })} />
              <Input label="Name" value={editor.name} onChange={(e) => setEditor({ ...editor, name: e.target.value })} />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <Select
                label="Modality"
                placeholder="Select…"
                options={modalities.map((m) => ({ value: m.id, label: m.name }))}
                value={editor.modalityId}
                onChange={(e) => setEditor({ ...editor, modalityId: e.target.value })}
              />
              <Input label="Body part" value={editor.bodyPart} onChange={(e) => setEditor({ ...editor, bodyPart: e.target.value })} />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <Input label="TAT hours" type="number" value={editor.tatHours} onChange={(e) => setEditor({ ...editor, tatHours: e.target.value })} />
              <Input label="Prep instructions" value={editor.prepInstructions} onChange={(e) => setEditor({ ...editor, prepInstructions: e.target.value })} />
            </div>
            <div className="flex items-center gap-4">
              <label className="flex items-center gap-2 text-sm text-slate-700">
                <input type="checkbox" checked={editor.requiresContrast} onChange={(e) => setEditor({ ...editor, requiresContrast: e.target.checked })} />
                Requires contrast
              </label>
              <label className="flex items-center gap-2 text-sm text-slate-700">
                <input type="checkbox" checked={editor.isActive} onChange={(e) => setEditor({ ...editor, isActive: e.target.checked })} />
                Active
              </label>
            </div>
          </div>
        </Modal>
      )}
    </>
  )
}
