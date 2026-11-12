import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useConsentTemplates, useDeleteConsentTemplate, useSaveConsentTemplate } from '../api'
import type { ConsentTemplate } from '../types'

interface EditorState {
  id?: number
  code: string
  title: string
  bodyText: string
  isActive: boolean
}

export function ConsentTemplatesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.CONSENTTEMPLATE.MANAGE')
  const { data: rows = [], isLoading } = useConsentTemplates()
  const save = useSaveConsentTemplate()
  const remove = useDeleteConsentTemplate()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (row: ConsentTemplate | null) => {
    setError(null)
    setEditor(
      row
        ? { id: row.id, code: row.code, title: row.title, bodyText: row.bodyText, isActive: row.isActive }
        : { code: '', title: '', bodyText: '', isActive: true },
    )
  }

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync(editor)
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader
        title="Consent Templates"
        subtitle="Reusable consent text signed at registration or before a procedure"
        actions={canManage && <Button onClick={() => openEditor(null)}>New template</Button>}
      />

      {isLoading && <Spinner />}

      <div className="grid gap-3 md:grid-cols-2">
        {rows.map((row) => (
          <div key={row.id} className="rounded-lg border border-slate-200 bg-white p-4">
            <div className="flex items-start justify-between">
              <div>
                <p className="font-semibold text-slate-800">{row.title}</p>
                <p className="text-xs text-slate-400">{row.code}</p>
              </div>
              <StatusBadge tone={row.isActive ? 'success' : 'neutral'}>
                {row.isActive ? 'Active' : 'Inactive'}
              </StatusBadge>
            </div>
            <p className="mt-2 line-clamp-3 text-sm text-slate-500">{row.bodyText}</p>
            {canManage && (
              <div className="mt-3 flex gap-1">
                <Button variant="secondary" onClick={() => openEditor(row)}>
                  Edit
                </Button>
                <Button
                  variant="ghost"
                  className="text-red-600"
                  onClick={async () => {
                    if (window.confirm(`Delete template '${row.title}'?`)) {
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
          </div>
        ))}
        {!isLoading && rows.length === 0 && (
          <p className="text-sm text-slate-400">No consent templates yet.</p>
        )}
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
            <Input label="Code" value={editor.code} onChange={(e) => setEditor({ ...editor, code: e.target.value.toUpperCase() })} />
            <Input label="Title" value={editor.title} onChange={(e) => setEditor({ ...editor, title: e.target.value })} />
            <div>
              <span className="mb-1 block text-sm font-medium text-slate-700">Consent text</span>
              <textarea
                rows={6}
                className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
                value={editor.bodyText}
                onChange={(e) => setEditor({ ...editor, bodyText: e.target.value })}
              />
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
