import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDeletePaymentMode, usePaymentModes, useSavePaymentMode } from '../api'
import type { PaymentMode } from '../types'

interface EditorState {
  id?: number
  code: string
  name: string
  description: string
  requiresReference: boolean
  isActive: boolean
}

export function PaymentModesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('BILLING.PAYMENTMODE.MANAGE')
  const { data: modes = [], isLoading } = usePaymentModes()
  const save = useSavePaymentMode()
  const remove = useDeletePaymentMode()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (mode: PaymentMode | null) => {
    setError(null)
    setEditor(
      mode
        ? {
            id: mode.id,
            code: mode.code,
            name: mode.name,
            description: mode.description ?? '',
            requiresReference: mode.requiresReference,
            isActive: mode.isActive,
          }
        : { code: '', name: '', description: '', requiresReference: false, isActive: true },
    )
  }

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({
        id: editor.id,
        code: editor.code,
        name: editor.name,
        description: editor.description || null,
        requiresReference: editor.requiresReference,
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
        title="Payment Modes"
        subtitle="Cash, card, UPI, cheque… used to collect bill payments and advances"
        actions={canManage && <Button onClick={() => openEditor(null)}>New payment mode</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Requires reference</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {modes.map((mode) => (
              <tr key={mode.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{mode.code}</td>
                <td className="px-4 py-2.5 font-medium text-slate-700">{mode.name}</td>
                <td className="px-4 py-2.5 text-slate-700">{mode.requiresReference ? 'Yes' : 'No'}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={mode.isActive ? 'success' : 'neutral'}>
                    {mode.isActive ? 'Active' : 'Inactive'}
                  </StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canManage && (
                    <div className="flex justify-end gap-1">
                      <Button variant="ghost" onClick={() => openEditor(mode)}>
                        Edit
                      </Button>
                      <Button
                        variant="ghost"
                        className="text-red-600"
                        onClick={async () => {
                          if (window.confirm(`Delete payment mode '${mode.name}'?`)) {
                            try {
                              await remove.mutateAsync(mode.id)
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
            {!isLoading && modes.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                  No payment modes yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit payment mode' : 'New payment mode'}
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
              label="Description"
              value={editor.description}
              onChange={(e) => setEditor({ ...editor, description: e.target.value })}
            />
            <label className="flex items-center gap-2 text-sm text-slate-700">
              <input
                type="checkbox"
                checked={editor.requiresReference}
                onChange={(e) => setEditor({ ...editor, requiresReference: e.target.checked })}
              />
              Requires a reference number at collection time
            </label>
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
