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
import { useDeleteDrug, useDrugs, useSaveDrug, useSimpleMaster, useTaxes } from '../api'
import { SimpleMasterSection } from '../components/SimpleMasterSection'
import { Tabs } from '../components/Tabs'
import type { Drug } from '../types'

const tabs = [
  { key: 'drugs', label: 'Drugs' },
  { key: 'generics', label: 'Generics' },
  { key: 'manufacturers', label: 'Manufacturers' },
] as const

interface EditorState {
  id?: number
  code: string
  name: string
  genericId: string
  manufacturerId: string
  form: string
  strength: string
  uomId: string
  hsnCode: string
  taxId: string
  schedule: string
  isHighAlert: boolean
  isActive: boolean
}

export function DrugsPage() {
  const [tab, setTab] = useState<(typeof tabs)[number]['key']>('drugs')

  return (
    <>
      <PageHeader title="Drugs" subtitle="Pharmacy drug catalog, generics and manufacturers" />
      <Tabs tabs={tabs} active={tab} onChange={setTab} />
      {tab === 'drugs' && <DrugsTab />}
      {tab === 'generics' && (
        <SimpleMasterSection
          noun="generic"
          queryKey="generics"
          endpoint={endpoints.masters.generics}
          managePermission="MASTER.DRUG.MANAGE"
        />
      )}
      {tab === 'manufacturers' && (
        <SimpleMasterSection
          noun="manufacturer"
          queryKey="manufacturers"
          endpoint={endpoints.masters.manufacturers}
          managePermission="MASTER.DRUG.MANAGE"
        />
      )}
    </>
  )
}

function DrugsTab() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.DRUG.MANAGE')
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const { data: result, isLoading } = useDrugs({ page, pageSize: 20, search: search || undefined })
  const { data: generics = [] } = useSimpleMaster('generics', endpoints.masters.generics)
  const { data: manufacturers = [] } = useSimpleMaster('manufacturers', endpoints.masters.manufacturers)
  const { data: uoms = [] } = useSimpleMaster('uoms', endpoints.masters.uoms)
  const { data: taxes = [] } = useTaxes()
  const save = useSaveDrug()
  const remove = useDeleteDrug()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (drug: Drug | null) => {
    setError(null)
    setEditor(
      drug
        ? {
            id: drug.id,
            code: drug.code,
            name: drug.name,
            genericId: drug.genericId ? String(drug.genericId) : '',
            manufacturerId: drug.manufacturerId ? String(drug.manufacturerId) : '',
            form: drug.form ?? '',
            strength: drug.strength ?? '',
            uomId: String(drug.uomId),
            hsnCode: drug.hsnCode ?? '',
            taxId: drug.taxId ? String(drug.taxId) : '',
            schedule: drug.schedule ?? '',
            isHighAlert: drug.isHighAlert,
            isActive: drug.isActive,
          }
        : {
            code: '',
            name: '',
            genericId: '',
            manufacturerId: '',
            form: '',
            strength: '',
            uomId: '',
            hsnCode: '',
            taxId: '',
            schedule: '',
            isHighAlert: false,
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
        genericId: editor.genericId ? Number(editor.genericId) : null,
        manufacturerId: editor.manufacturerId ? Number(editor.manufacturerId) : null,
        form: editor.form || null,
        strength: editor.strength || null,
        uomId: Number(editor.uomId),
        hsnCode: editor.hsnCode || null,
        taxId: editor.taxId ? Number(editor.taxId) : null,
        schedule: editor.schedule || null,
        isHighAlert: editor.isHighAlert,
        isActive: editor.isActive,
      })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const columns: Column<Drug>[] = [
    { header: 'Code', cell: (row) => <span className="font-mono text-xs text-slate-500">{row.code}</span> },
    {
      header: 'Name',
      cell: (row) => (
        <span className="font-medium">
          {row.name}
          {(row.form || row.strength) && (
            <span className="block text-xs font-normal text-slate-500">
              {[row.form, row.strength].filter(Boolean).join(' · ')}
            </span>
          )}
        </span>
      ),
    },
    { header: 'Generic', cell: (row) => row.genericName ?? '—' },
    { header: 'Manufacturer', cell: (row) => row.manufacturerName ?? '—' },
    { header: 'Schedule', cell: (row) => row.schedule ?? '—' },
    {
      header: 'High-alert',
      cell: (row) => row.isHighAlert && <StatusBadge tone="warning">High-alert</StatusBadge>,
    },
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
                if (window.confirm(`Delete drug '${row.name}'?`)) {
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
        searchPlaceholder="Search drugs…"
        rowKey={(row) => row.id}
        emptyMessage="No drugs yet."
        toolbar={canManage && <Button onClick={() => openEditor(null)}>New drug</Button>}
      />

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit drug' : 'New drug'}
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
                label="Generic"
                placeholder="— none —"
                options={generics.map((g) => ({ value: g.id, label: g.name }))}
                value={editor.genericId}
                onChange={(e) => setEditor({ ...editor, genericId: e.target.value })}
              />
              <Select
                label="Manufacturer"
                placeholder="— none —"
                options={manufacturers.map((mf) => ({ value: mf.id, label: mf.name }))}
                value={editor.manufacturerId}
                onChange={(e) => setEditor({ ...editor, manufacturerId: e.target.value })}
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <Input
                label="Form (tablet, syrup…)"
                value={editor.form}
                onChange={(e) => setEditor({ ...editor, form: e.target.value })}
              />
              <Input
                label="Strength (500mg…)"
                value={editor.strength}
                onChange={(e) => setEditor({ ...editor, strength: e.target.value })}
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <Select
                label="Unit of measure"
                placeholder="Select unit…"
                options={uoms.map((u) => ({ value: u.id, label: u.name }))}
                value={editor.uomId}
                onChange={(e) => setEditor({ ...editor, uomId: e.target.value })}
              />
              <Select
                label="Tax"
                placeholder="— none —"
                options={taxes.map((t) => ({ value: t.id, label: t.name }))}
                value={editor.taxId}
                onChange={(e) => setEditor({ ...editor, taxId: e.target.value })}
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <Input
                label="HSN code"
                value={editor.hsnCode}
                onChange={(e) => setEditor({ ...editor, hsnCode: e.target.value })}
              />
              <Input
                label="Schedule (H, H1, X…)"
                value={editor.schedule}
                onChange={(e) => setEditor({ ...editor, schedule: e.target.value })}
              />
            </div>
            <div className="flex items-center gap-4">
              <label className="flex items-center gap-2 text-sm text-slate-700">
                <input
                  type="checkbox"
                  checked={editor.isHighAlert}
                  onChange={(e) => setEditor({ ...editor, isHighAlert: e.target.checked })}
                />
                High-alert drug (MAR double-verification)
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
          </div>
        </Modal>
      )}
    </>
  )
}
