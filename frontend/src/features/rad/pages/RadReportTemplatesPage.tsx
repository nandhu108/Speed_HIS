import { useEffect, useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDeleteRadReportTemplate, useRadReportTemplate, useRadReportTemplates, useRadiologyTests, useSaveRadReportTemplate } from '../api'
import type { RadReportTemplateListItem } from '../types'

interface EditorState {
  id?: number
  radiologyTestId: string
  name: string
  findingsTemplate: string
  impressionTemplate: string
  isActive: boolean
}

export function RadReportTemplatesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('RAD.TEMPLATE.MANAGE')
  const { data: templates = [], isLoading } = useRadReportTemplates()
  const { data: tests = [] } = useRadiologyTests()
  const save = useSaveRadReportTemplate()
  const remove = useDeleteRadReportTemplate()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)
  const { data: editingDetail } = useRadReportTemplate(editor?.id ?? null)

  useEffect(() => {
    if (editingDetail) {
      setEditor((prev) => (prev && prev.id === editingDetail.id ? { ...prev, findingsTemplate: editingDetail.findingsTemplate, impressionTemplate: editingDetail.impressionTemplate } : prev))
    }
  }, [editingDetail])

  const openEditor = (template: RadReportTemplateListItem | null) => {
    setError(null)
    setEditor(
      template
        ? { id: template.id, radiologyTestId: String(template.radiologyTestId), name: template.name, findingsTemplate: '', impressionTemplate: '', isActive: template.isActive }
        : { radiologyTestId: '', name: '', findingsTemplate: '', impressionTemplate: '', isActive: true },
    )
  }

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({
        id: editor.id, radiologyTestId: Number(editor.radiologyTestId), name: editor.name,
        findingsTemplate: editor.findingsTemplate, impressionTemplate: editor.impressionTemplate, isActive: editor.isActive,
      })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader
        title="Report Templates"
        subtitle="Canned findings/impression text per radiology test"
        actions={canManage && <Button onClick={() => openEditor(null)}>New template</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Test</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {templates.map((t) => (
              <tr key={t.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-medium text-slate-800">{t.name}</td>
                <td className="px-4 py-2.5 text-slate-700">{t.radiologyTestName}</td>
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
                </td>
              </tr>
            ))}
            {!isLoading && templates.length === 0 && (
              <tr><td colSpan={4} className="px-4 py-10 text-center text-slate-400">No report templates yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit template' : 'New template'}
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
              <Select
                label="Radiology test"
                placeholder="Select…"
                options={tests.map((t) => ({ value: t.id, label: t.name }))}
                value={editor.radiologyTestId}
                onChange={(e) => setEditor({ ...editor, radiologyTestId: e.target.value })}
              />
              <Input label="Template name" value={editor.name} onChange={(e) => setEditor({ ...editor, name: e.target.value })} />
            </div>
            <label className="block text-sm font-medium text-slate-700">
              Findings template
              <textarea
                className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
                rows={4}
                value={editor.findingsTemplate}
                onChange={(e) => setEditor({ ...editor, findingsTemplate: e.target.value })}
              />
            </label>
            <label className="block text-sm font-medium text-slate-700">
              Impression template
              <textarea
                className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
                rows={2}
                value={editor.impressionTemplate}
                onChange={(e) => setEditor({ ...editor, impressionTemplate: e.target.value })}
              />
            </label>
            <label className="flex items-center gap-2 text-sm text-slate-700">
              <input type="checkbox" checked={editor.isActive} onChange={(e) => setEditor({ ...editor, isActive: e.target.checked })} />
              Active
            </label>
          </div>
        </Modal>
      )}
    </>
  )
}
