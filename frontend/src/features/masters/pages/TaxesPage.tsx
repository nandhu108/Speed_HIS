import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDeleteTax, useSaveTax, useTaxes } from '../api'
import type { Tax } from '../types'

interface EditorState {
  id?: number
  code: string
  name: string
  ratePercent: string
  isActive: boolean
}

export function TaxesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.TAX.MANAGE')
  const { data: taxes = [], isLoading } = useTaxes()
  const save = useSaveTax()
  const remove = useDeleteTax()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (tax: Tax | null) => {
    setError(null)
    setEditor(
      tax
        ? { id: tax.id, code: tax.code, name: tax.name, ratePercent: String(tax.ratePercent), isActive: tax.isActive }
        : { code: '', name: '', ratePercent: '', isActive: true },
    )
  }

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({
        id: editor.id,
        code: editor.code,
        name: editor.name,
        ratePercent: Number(editor.ratePercent),
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
        title="Taxes"
        subtitle="GST slabs applied to services, items and drugs"
        actions={canManage && <Button onClick={() => openEditor(null)}>New tax</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Rate %</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {taxes.map((tax) => (
              <tr key={tax.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{tax.code}</td>
                <td className="px-4 py-2.5 font-medium text-slate-700">{tax.name}</td>
                <td className="px-4 py-2.5 text-slate-700">{tax.ratePercent}%</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={tax.isActive ? 'success' : 'neutral'}>
                    {tax.isActive ? 'Active' : 'Inactive'}
                  </StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canManage && (
                    <div className="flex justify-end gap-1">
                      <Button variant="ghost" onClick={() => openEditor(tax)}>
                        Edit
                      </Button>
                      <Button
                        variant="ghost"
                        className="text-red-600"
                        onClick={async () => {
                          if (window.confirm(`Delete tax '${tax.name}'?`)) {
                            try {
                              await remove.mutateAsync(tax.id)
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
            {!isLoading && taxes.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                  No taxes yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit tax' : 'New tax'}
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
              label="Rate %"
              type="number"
              min={0}
              max={100}
              step="0.01"
              value={editor.ratePercent}
              onChange={(e) => setEditor({ ...editor, ratePercent: e.target.value })}
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
