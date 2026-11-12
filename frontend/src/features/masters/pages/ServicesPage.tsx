import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { endpoints } from '../../../api/endpoints'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDeleteService, useSaveService, useServices, useSimpleMaster, useTaxes } from '../api'
import { SimpleMasterSection } from '../components/SimpleMasterSection'
import { Tabs } from '../components/Tabs'
import type { Service } from '../types'

const tabs = [
  { key: 'services', label: 'Services' },
  { key: 'categories', label: 'Categories' },
] as const

interface EditorState {
  id?: number
  code: string
  name: string
  serviceCategoryId: string
  departmentId: string
  isTaxable: boolean
  taxId: string
  defaultRate: string
  isActive: boolean
}

export function ServicesPage() {
  const [tab, setTab] = useState<(typeof tabs)[number]['key']>('services')

  return (
    <>
      <PageHeader title="Services" subtitle="Billable service catalog and its categories" />
      <Tabs tabs={tabs} active={tab} onChange={setTab} />
      {tab === 'services' ? (
        <ServicesTab />
      ) : (
        <SimpleMasterSection
          noun="category"
          queryKey="service-categories"
          endpoint={endpoints.masters.serviceCategories}
          managePermission="MASTER.SERVICE.MANAGE"
        />
      )}
    </>
  )
}

function ServicesTab() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.SERVICE.MANAGE')
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const { data: result, isLoading } = useServices({ page, pageSize: 20, search: search || undefined })
  const { data: categories = [] } = useSimpleMaster('service-categories', endpoints.masters.serviceCategories)
  const { data: departments = [] } = useSimpleMaster('departments', endpoints.masters.departments)
  const { data: taxes = [] } = useTaxes()
  const save = useSaveService()
  const remove = useDeleteService()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (service: Service | null) => {
    setError(null)
    setEditor(
      service
        ? {
            id: service.id,
            code: service.code,
            name: service.name,
            serviceCategoryId: String(service.serviceCategoryId),
            departmentId: service.departmentId ? String(service.departmentId) : '',
            isTaxable: service.isTaxable,
            taxId: service.taxId ? String(service.taxId) : '',
            defaultRate: String(service.defaultRate),
            isActive: service.isActive,
          }
        : {
            code: '',
            name: '',
            serviceCategoryId: '',
            departmentId: '',
            isTaxable: false,
            taxId: '',
            defaultRate: '0',
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
        serviceCategoryId: Number(editor.serviceCategoryId),
        departmentId: editor.departmentId ? Number(editor.departmentId) : null,
        isTaxable: editor.isTaxable,
        taxId: editor.isTaxable && editor.taxId ? Number(editor.taxId) : null,
        defaultRate: Number(editor.defaultRate),
        isActive: editor.isActive,
      })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const columns: Column<Service>[] = [
    { header: 'Code', cell: (row) => <span className="font-mono text-xs text-slate-500">{row.code}</span> },
    { header: 'Name', cell: (row) => <span className="font-medium">{row.name}</span> },
    { header: 'Category', cell: (row) => row.categoryName },
    { header: 'Department', cell: (row) => row.departmentName ?? '—' },
    { header: 'Rate', cell: (row) => row.defaultRate.toLocaleString() },
    { header: 'Tax', cell: (row) => (row.isTaxable ? (row.taxName ?? 'Taxable') : '—') },
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
                if (window.confirm(`Delete service '${row.name}'?`)) {
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
      <DataTable
        columns={columns}
        result={result}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onSearch={setSearch}
        searchPlaceholder="Search services…"
        rowKey={(row) => row.id}
        emptyMessage="No services yet."
        toolbar={canManage && <Button onClick={() => openEditor(null)}>New service</Button>}
      />

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit service' : 'New service'}
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
              <Select
                label="Category"
                placeholder="Select category…"
                options={categories.map((c) => ({ value: c.id, label: c.name }))}
                value={editor.serviceCategoryId}
                onChange={(e) => setEditor({ ...editor, serviceCategoryId: e.target.value })}
              />
              <Select
                label="Department"
                placeholder="— none —"
                options={departments.map((d) => ({ value: d.id, label: d.name }))}
                value={editor.departmentId}
                onChange={(e) => setEditor({ ...editor, departmentId: e.target.value })}
              />
            </div>
            <Input
              label="Default rate"
              type="number"
              min={0}
              step="0.01"
              value={editor.defaultRate}
              onChange={(e) => setEditor({ ...editor, defaultRate: e.target.value })}
            />
            <div className="grid grid-cols-2 items-end gap-3">
              <label className="flex items-center gap-2 pb-2 text-sm text-slate-700">
                <input
                  type="checkbox"
                  checked={editor.isTaxable}
                  onChange={(e) => setEditor({ ...editor, isTaxable: e.target.checked })}
                />
                Taxable
              </label>
              {editor.isTaxable && (
                <Select
                  label="Tax slab"
                  placeholder="Select tax…"
                  options={taxes.map((t) => ({ value: t.id, label: t.name }))}
                  value={editor.taxId}
                  onChange={(e) => setEditor({ ...editor, taxId: e.target.value })}
                />
              )}
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
