import { useRef, useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDeleteIcdCode, useIcdCodes, useImportIcdCsv, useSaveIcdCode } from '../api'
import type { IcdCode, IcdImportResult } from '../types'

interface EditorState {
  id?: number
  code: string
  description: string
  category: string
  isActive: boolean
}

export function IcdCodesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.ICD.MANAGE')
  const canImport = hasPermission('MASTER.ICD.IMPORT')
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const { data: result, isLoading } = useIcdCodes({ page, pageSize: 20, search: search || undefined })
  const save = useSaveIcdCode()
  const remove = useDeleteIcdCode()
  const importCsv = useImportIcdCsv()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [importResult, setImportResult] = useState<IcdImportResult | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const openEditor = (row: IcdCode | null) => {
    setError(null)
    setEditor(
      row
        ? { id: row.id, code: row.code, description: row.description, category: row.category ?? '', isActive: row.isActive }
        : { code: '', description: '', category: '', isActive: true },
    )
  }

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({
        id: editor.id,
        code: editor.code,
        description: editor.description,
        category: editor.category || null,
        isActive: editor.isActive,
      })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const onFilePicked = async (file: File | undefined) => {
    if (!file) return
    try {
      const outcome = await importCsv.mutateAsync(file)
      setImportResult(outcome)
    } catch (err) {
      window.alert(errorMessage(err))
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  const columns: Column<IcdCode>[] = [
    { header: 'Code', cell: (row) => <span className="font-mono text-xs font-medium text-slate-700">{row.code}</span> },
    { header: 'Description', cell: (row) => row.description },
    { header: 'Category', cell: (row) => row.category ?? '—' },
    {
      header: 'Status',
      cell: (row) => (
        <StatusBadge tone={row.isActive ? 'success' : 'neutral'}>{row.isActive ? 'Active' : 'Inactive'}</StatusBadge>
      ),
    },
    {
      header: '',
      className: 'text-right',
      cell: (row) =>
        canManage && (
          <div className="flex justify-end gap-1">
            <Button variant="ghost" onClick={() => openEditor(row)}>
              Edit
            </Button>
            <Button
              variant="ghost"
              className="text-red-600"
              onClick={async () => {
                if (window.confirm(`Delete ICD code '${row.code}'?`)) {
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
        ),
    },
  ]

  return (
    <>
      <PageHeader
        title="ICD-10 Codes"
        subtitle="Diagnosis code catalog with CSV bulk import"
        actions={
          <div className="flex gap-2">
            {canImport && (
              <>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".csv,text/csv"
                  className="hidden"
                  onChange={(e) => onFilePicked(e.target.files?.[0])}
                />
                <Button variant="secondary" loading={importCsv.isPending} onClick={() => fileInputRef.current?.click()}>
                  Import CSV
                </Button>
              </>
            )}
            {canManage && <Button onClick={() => openEditor(null)}>New code</Button>}
          </div>
        }
      />

      <DataTable
        columns={columns}
        result={result}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onSearch={setSearch}
        searchPlaceholder="Search ICD codes…"
        rowKey={(row) => row.id}
        emptyMessage="No ICD codes yet. Import a CSV to get started."
      />

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit ICD code' : 'New ICD code'}
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
            <Input
              label="Description"
              value={editor.description}
              onChange={(e) => setEditor({ ...editor, description: e.target.value })}
            />
            <Input
              label="Category"
              value={editor.category}
              onChange={(e) => setEditor({ ...editor, category: e.target.value })}
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

      {importResult && (
        <Modal
          open
          title="Import complete"
          onClose={() => setImportResult(null)}
          footer={<Button onClick={() => setImportResult(null)}>Close</Button>}
        >
          <div className="space-y-2 text-sm text-slate-700">
            <p>
              Inserted <strong>{importResult.inserted}</strong>, updated <strong>{importResult.updated}</strong>,
              skipped <strong>{importResult.skipped}</strong>.
            </p>
            {importResult.errors.length > 0 && (
              <div className="max-h-48 overflow-y-auto rounded-md bg-red-50 p-3">
                <p className="mb-1 font-medium text-red-700">Errors</p>
                <ul className="list-inside list-disc space-y-0.5 text-red-700">
                  {importResult.errors.map((message, index) => (
                    <li key={index}>{message}</li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        </Modal>
      )}
    </>
  )
}
