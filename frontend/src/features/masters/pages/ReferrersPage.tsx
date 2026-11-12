import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDeleteReferrer, useDoctors, useReferrers, useSaveReferrer } from '../api'
import { REFERRER_TYPES, type Referrer, type ReferrerType } from '../types'

interface EditorState {
  id?: number
  code: string
  name: string
  type: ReferrerType
  doctorId: string
  phone: string
  organization: string
  isActive: boolean
}

export function ReferrersPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.REFERRER.MANAGE')
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const { data: result, isLoading } = useReferrers({ page, pageSize: 20, search: search || undefined })
  const { data: doctors } = useDoctors({ page: 1, pageSize: 100 })
  const save = useSaveReferrer()
  const remove = useDeleteReferrer()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (referrer: Referrer | null) => {
    setError(null)
    setEditor(
      referrer
        ? {
            id: referrer.id,
            code: referrer.code,
            name: referrer.name,
            type: referrer.type,
            doctorId: referrer.doctorId ? String(referrer.doctorId) : '',
            phone: referrer.phone ?? '',
            organization: referrer.organization ?? '',
            isActive: referrer.isActive,
          }
        : {
            code: '',
            name: '',
            type: REFERRER_TYPES.External,
            doctorId: '',
            phone: '',
            organization: '',
            isActive: true,
          },
    )
  }

  const submit = async () => {
    if (!editor) return
    const isInternal = editor.type === REFERRER_TYPES.Internal
    try {
      await save.mutateAsync({
        id: editor.id,
        code: editor.code,
        name: editor.name,
        type: editor.type,
        doctorId: isInternal && editor.doctorId ? Number(editor.doctorId) : null,
        phone: editor.phone || null,
        organization: editor.organization || null,
        isActive: editor.isActive,
      })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const columns: Column<Referrer>[] = [
    { header: 'Code', cell: (row) => <span className="font-mono text-xs text-slate-500">{row.code}</span> },
    { header: 'Name', cell: (row) => <span className="font-medium">{row.name}</span> },
    {
      header: 'Type',
      cell: (row) => (
        <StatusBadge tone={row.type === REFERRER_TYPES.Internal ? 'info' : 'neutral'}>
          {row.type === REFERRER_TYPES.Internal ? 'Internal' : 'External'}
        </StatusBadge>
      ),
    },
    { header: 'Doctor', cell: (row) => row.doctorName ?? '—' },
    { header: 'Organization', cell: (row) => row.organization ?? '—' },
    { header: 'Phone', cell: (row) => row.phone ?? '—' },
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
                if (window.confirm(`Delete referrer '${row.name}'?`)) {
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
        title="Referrers"
        subtitle="Internal and external referral sources"
        actions={canManage && <Button onClick={() => openEditor(null)}>New referrer</Button>}
      />

      <DataTable
        columns={columns}
        result={result}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onSearch={setSearch}
        searchPlaceholder="Search referrers…"
        rowKey={(row) => row.id}
        emptyMessage="No referrers yet."
      />

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit referrer' : 'New referrer'}
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
            <Select
              label="Type"
              options={[
                { value: REFERRER_TYPES.External, label: 'External' },
                { value: REFERRER_TYPES.Internal, label: 'Internal (staff doctor)' },
              ]}
              value={editor.type}
              onChange={(e) => setEditor({ ...editor, type: Number(e.target.value) as ReferrerType })}
            />
            {editor.type === REFERRER_TYPES.Internal && (
              <Select
                label="Doctor"
                placeholder="Select doctor…"
                options={(doctors?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
                value={editor.doctorId}
                onChange={(e) => setEditor({ ...editor, doctorId: e.target.value })}
              />
            )}
            <div className="grid grid-cols-2 gap-3">
              <Input label="Phone" value={editor.phone} onChange={(e) => setEditor({ ...editor, phone: e.target.value })} />
              <Input
                label="Organization"
                value={editor.organization}
                onChange={(e) => setEditor({ ...editor, organization: e.target.value })}
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
