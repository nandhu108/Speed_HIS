import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { useAuth } from '../../auth/AuthContext'
import { useDoctors } from '../../masters/api'
import { DrugPicker } from '../components/DrugPicker'
import { useDeletePrescriptionTemplate, usePrescriptionTemplates, useSavePrescriptionTemplate } from '../api'
import type { SaveTemplateItem } from '../types'

interface EditorState {
  id?: number
  doctorId: string
  name: string
  items: (SaveTemplateItem & { drugName: string })[]
}

export function PrescriptionTemplatesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('OPD.TEMPLATE.MANAGE')
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const [doctorId, setDoctorId] = useState('')
  const { data: templates = [], isLoading } = usePrescriptionTemplates(doctorId ? Number(doctorId) : null)
  const save = useSavePrescriptionTemplate()
  const remove = useDeletePrescriptionTemplate()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    if (!editor) return
    setError(null)
    try {
      await save.mutateAsync({
        id: editor.id,
        doctorId: Number(editor.doctorId),
        name: editor.name,
        items: editor.items.map(({ drugName: _drugName, ...rest }) => rest),
      })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader
        title="Prescription Templates"
        subtitle="Per-doctor favorites to prefill a new prescription"
        actions={
          <div className="flex gap-2">
            <Select
              placeholder="Select doctor…"
              options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
              value={doctorId}
              onChange={(e) => setDoctorId(e.target.value)}
            />
            {canManage && doctorId && (
              <Button onClick={() => setEditor({ doctorId, name: '', items: [] })}>New template</Button>
            )}
          </div>
        }
      />

      {isLoading && <Spinner />}
      {!doctorId && <p className="text-sm text-slate-400">Select a doctor to view their templates.</p>}

      <div className="grid gap-3 md:grid-cols-2">
        {templates.map((t) => (
          <div key={t.id} className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="font-semibold text-slate-800">{t.name}</p>
            <ul className="mt-2 space-y-1 text-sm text-slate-600">
              {t.items.map((i) => (
                <li key={i.id}>
                  {i.drugName} — {i.dose}, {i.frequency}, {i.duration}
                </li>
              ))}
            </ul>
            {canManage && (
              <div className="mt-3 flex gap-1">
                <Button
                  variant="secondary"
                  onClick={() =>
                    setEditor({
                      id: t.id,
                      doctorId: String(t.doctorId),
                      name: t.name,
                      items: t.items.map((i) => ({
                        drugId: i.drugId, drugName: i.drugName, dose: i.dose, frequency: i.frequency, duration: i.duration, route: i.route, instructions: i.instructions,
                      })),
                    })
                  }
                >
                  Edit
                </Button>
                <Button
                  variant="ghost"
                  className="text-red-600"
                  onClick={async () => {
                    if (window.confirm(`Delete template '${t.name}'?`)) {
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
          </div>
        ))}
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit template' : 'New template'}
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
            <Input label="Name" value={editor.name} onChange={(e) => setEditor({ ...editor, name: e.target.value })} />
            <DrugPicker
              onSelect={(drug) =>
                setEditor({
                  ...editor,
                  items: [...editor.items, { drugId: drug.id, drugName: drug.name, dose: '', frequency: '', duration: '', route: null, instructions: null }],
                })
              }
            />
            <div className="space-y-2">
              {editor.items.map((item, i) => (
                <div key={i} className="grid grid-cols-5 items-end gap-2 rounded-md border border-slate-200 p-2">
                  <span className="col-span-2 text-sm font-medium">{item.drugName}</span>
                  <Input
                    placeholder="Dose"
                    value={item.dose}
                    onChange={(e) => setEditor({ ...editor, items: editor.items.map((row, j) => (j === i ? { ...row, dose: e.target.value } : row)) })}
                  />
                  <Input
                    placeholder="Frequency"
                    value={item.frequency}
                    onChange={(e) => setEditor({ ...editor, items: editor.items.map((row, j) => (j === i ? { ...row, frequency: e.target.value } : row)) })}
                  />
                  <Input
                    placeholder="Duration"
                    value={item.duration}
                    onChange={(e) => setEditor({ ...editor, items: editor.items.map((row, j) => (j === i ? { ...row, duration: e.target.value } : row)) })}
                  />
                </div>
              ))}
              {editor.items.length === 0 && <p className="text-sm text-slate-400">Search a drug above to add it.</p>}
            </div>
          </div>
        </Modal>
      )}
    </>
  )
}
