import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useCorporates, useDeleteCorporate, useSaveCorporate } from '../api'
import type { Corporate } from '../types'

interface EditorState {
  id?: number
  code: string
  name: string
  contactPerson: string
  phone: string
  email: string
  address: string
  gstNumber: string
  creditLimit: string
  isActive: boolean
}

export function CorporatesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.CORPORATE.MANAGE')
  const { data: rows = [], isLoading } = useCorporates()
  const save = useSaveCorporate()
  const remove = useDeleteCorporate()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (row: Corporate | null) => {
    setError(null)
    setEditor(
      row
        ? {
            id: row.id,
            code: row.code,
            name: row.name,
            contactPerson: row.contactPerson ?? '',
            phone: row.phone ?? '',
            email: row.email ?? '',
            address: row.address ?? '',
            gstNumber: row.gstNumber ?? '',
            creditLimit: String(row.creditLimit),
            isActive: row.isActive,
          }
        : {
            code: '',
            name: '',
            contactPerson: '',
            phone: '',
            email: '',
            address: '',
            gstNumber: '',
            creditLimit: '0',
            isActive: true,
          },
    )
  }

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({
        id: editor.id,
        code: editor.code,
        name: editor.name,
        contactPerson: editor.contactPerson || null,
        phone: editor.phone || null,
        email: editor.email || null,
        address: editor.address || null,
        gstNumber: editor.gstNumber || null,
        creditLimit: Number(editor.creditLimit),
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
        title="Corporates"
        subtitle="Corporate clients with credit billing arrangements"
        actions={canManage && <Button onClick={() => openEditor(null)}>New corporate</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Contact</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Credit limit</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{row.code}</td>
                <td className="px-4 py-2.5 font-medium text-slate-700">{row.name}</td>
                <td className="px-4 py-2.5 text-slate-500">
                  {row.contactPerson ?? '—'}
                  {row.phone && <span className="block text-xs">{row.phone}</span>}
                </td>
                <td className="px-4 py-2.5 text-slate-700">{row.creditLimit.toLocaleString()}</td>
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
                          if (window.confirm(`Delete corporate '${row.name}'?`)) {
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
                  No corporates yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit corporate' : 'New corporate'}
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
            <div className="grid grid-cols-2 gap-3">
              <Input
                label="Contact person"
                value={editor.contactPerson}
                onChange={(e) => setEditor({ ...editor, contactPerson: e.target.value })}
              />
              <Input label="Phone" value={editor.phone} onChange={(e) => setEditor({ ...editor, phone: e.target.value })} />
            </div>
            <Input label="Email" type="email" value={editor.email} onChange={(e) => setEditor({ ...editor, email: e.target.value })} />
            <Input label="Address" value={editor.address} onChange={(e) => setEditor({ ...editor, address: e.target.value })} />
            <div className="grid grid-cols-2 gap-3">
              <Input
                label="GST number"
                value={editor.gstNumber}
                onChange={(e) => setEditor({ ...editor, gstNumber: e.target.value })}
              />
              <Input
                label="Credit limit"
                type="number"
                min={0}
                step="0.01"
                value={editor.creditLimit}
                onChange={(e) => setEditor({ ...editor, creditLimit: e.target.value })}
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
