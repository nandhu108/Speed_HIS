import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDeleteDiscountType, useDiscountTypes, useSaveDiscountType } from '../api'
import type { DiscountType } from '../types'

interface EditorState {
  id?: number
  code: string
  name: string
  maxPercent: string
  approvalThresholdPercent: string
  isActive: boolean
}

export function DiscountTypesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.DISCOUNTTYPE.MANAGE')
  const { data: rows = [], isLoading } = useDiscountTypes()
  const save = useSaveDiscountType()
  const remove = useDeleteDiscountType()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (row: DiscountType | null) => {
    setError(null)
    setEditor(
      row
        ? {
            id: row.id,
            code: row.code,
            name: row.name,
            maxPercent: String(row.maxPercent),
            approvalThresholdPercent: String(row.approvalThresholdPercent),
            isActive: row.isActive,
          }
        : { code: '', name: '', maxPercent: '', approvalThresholdPercent: '', isActive: true },
    )
  }

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({
        id: editor.id,
        code: editor.code,
        name: editor.name,
        maxPercent: Number(editor.maxPercent),
        approvalThresholdPercent: Number(editor.approvalThresholdPercent),
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
        title="Discount Types"
        subtitle="Discount schemes with caps and approval thresholds"
        actions={canManage && <Button onClick={() => openEditor(null)}>New discount type</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Max %</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Approval above %</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{row.code}</td>
                <td className="px-4 py-2.5 font-medium text-slate-700">{row.name}</td>
                <td className="px-4 py-2.5 text-slate-700">{row.maxPercent}%</td>
                <td className="px-4 py-2.5 text-slate-700">{row.approvalThresholdPercent}%</td>
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
                          if (window.confirm(`Delete discount type '${row.name}'?`)) {
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
                  No discount types yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit discount type' : 'New discount type'}
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
            <div className="grid grid-cols-2 gap-3">
              <Input
                label="Max discount %"
                type="number"
                min={0}
                max={100}
                step="0.01"
                value={editor.maxPercent}
                onChange={(e) => setEditor({ ...editor, maxPercent: e.target.value })}
              />
              <Input
                label="Approval above %"
                type="number"
                min={0}
                max={100}
                step="0.01"
                value={editor.approvalThresholdPercent}
                onChange={(e) => setEditor({ ...editor, approvalThresholdPercent: e.target.value })}
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
