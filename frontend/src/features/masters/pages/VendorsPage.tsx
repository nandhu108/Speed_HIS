import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDeleteVendor, useSaveVendor, useVendors } from '../api'
import type { Vendor } from '../types'

interface EditorState {
  id?: number
  code: string
  name: string
  contactPerson: string
  phone: string
  email: string
  address: string
  gstNumber: string
  isActive: boolean
}

export function VendorsPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.VENDOR.MANAGE')
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const { data: result, isLoading } = useVendors({ page, pageSize: 20, search: search || undefined })
  const save = useSaveVendor()
  const remove = useDeleteVendor()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (vendor: Vendor | null) => {
    setError(null)
    setEditor(
      vendor
        ? {
            id: vendor.id,
            code: vendor.code,
            name: vendor.name,
            contactPerson: vendor.contactPerson ?? '',
            phone: vendor.phone ?? '',
            email: vendor.email ?? '',
            address: vendor.address ?? '',
            gstNumber: vendor.gstNumber ?? '',
            isActive: vendor.isActive,
          }
        : { code: '', name: '', contactPerson: '', phone: '', email: '', address: '', gstNumber: '', isActive: true },
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
        isActive: editor.isActive,
      })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const columns: Column<Vendor>[] = [
    { header: 'Code', cell: (row) => <span className="font-mono text-xs text-slate-500">{row.code}</span> },
    { header: 'Name', cell: (row) => <span className="font-medium">{row.name}</span> },
    {
      header: 'Contact',
      cell: (row) => (
        <span className="text-slate-500">
          {row.contactPerson ?? '—'}
          {row.phone && <span className="block text-xs">{row.phone}</span>}
        </span>
      ),
    },
    { header: 'GST', cell: (row) => row.gstNumber ?? '—' },
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
                if (window.confirm(`Delete vendor '${row.name}'?`)) {
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
        title="Vendors"
        subtitle="Suppliers for pharmacy and general inventory"
        actions={canManage && <Button onClick={() => openEditor(null)}>New vendor</Button>}
      />

      <DataTable
        columns={columns}
        result={result}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onSearch={setSearch}
        searchPlaceholder="Search vendors…"
        rowKey={(row) => row.id}
        emptyMessage="No vendors yet."
      />

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit vendor' : 'New vendor'}
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
            <Input
              label="GST number"
              value={editor.gstNumber}
              onChange={(e) => setEditor({ ...editor, gstNumber: e.target.value })}
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
