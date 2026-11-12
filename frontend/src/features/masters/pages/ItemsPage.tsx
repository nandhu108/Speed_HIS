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
import { useDeleteItem, useItems, useSaveItem, useSimpleMaster, useTaxes } from '../api'
import { SimpleMasterSection } from '../components/SimpleMasterSection'
import { Tabs } from '../components/Tabs'
import type { Item } from '../types'

const tabs = [
  { key: 'items', label: 'Items' },
  { key: 'categories', label: 'Categories' },
] as const

interface EditorState {
  id?: number
  code: string
  name: string
  itemCategoryId: string
  uomId: string
  hsnCode: string
  taxId: string
  reorderLevel: string
  isActive: boolean
}

export function ItemsPage() {
  const [tab, setTab] = useState<(typeof tabs)[number]['key']>('items')

  return (
    <>
      <PageHeader title="Items" subtitle="General inventory item catalog and categories" />
      <Tabs tabs={tabs} active={tab} onChange={setTab} />
      {tab === 'items' ? (
        <ItemsTab />
      ) : (
        <SimpleMasterSection
          noun="category"
          queryKey="item-categories"
          endpoint={endpoints.masters.itemCategories}
          managePermission="MASTER.ITEM.MANAGE"
        />
      )}
    </>
  )
}

function ItemsTab() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.ITEM.MANAGE')
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const { data: result, isLoading } = useItems({ page, pageSize: 20, search: search || undefined })
  const { data: categories = [] } = useSimpleMaster('item-categories', endpoints.masters.itemCategories)
  const { data: uoms = [] } = useSimpleMaster('uoms', endpoints.masters.uoms)
  const { data: taxes = [] } = useTaxes()
  const save = useSaveItem()
  const remove = useDeleteItem()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (item: Item | null) => {
    setError(null)
    setEditor(
      item
        ? {
            id: item.id,
            code: item.code,
            name: item.name,
            itemCategoryId: String(item.itemCategoryId),
            uomId: String(item.uomId),
            hsnCode: item.hsnCode ?? '',
            taxId: item.taxId ? String(item.taxId) : '',
            reorderLevel: String(item.reorderLevel),
            isActive: item.isActive,
          }
        : { code: '', name: '', itemCategoryId: '', uomId: '', hsnCode: '', taxId: '', reorderLevel: '0', isActive: true },
    )
  }

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({
        id: editor.id,
        code: editor.code,
        name: editor.name,
        itemCategoryId: Number(editor.itemCategoryId),
        uomId: Number(editor.uomId),
        hsnCode: editor.hsnCode || null,
        taxId: editor.taxId ? Number(editor.taxId) : null,
        reorderLevel: Number(editor.reorderLevel),
        isActive: editor.isActive,
      })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const columns: Column<Item>[] = [
    { header: 'Code', cell: (row) => <span className="font-mono text-xs text-slate-500">{row.code}</span> },
    { header: 'Name', cell: (row) => <span className="font-medium">{row.name}</span> },
    { header: 'Category', cell: (row) => row.categoryName },
    { header: 'UoM', cell: (row) => row.uomName },
    { header: 'Tax', cell: (row) => row.taxName ?? '—' },
    { header: 'Reorder level', cell: (row) => row.reorderLevel },
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
                if (window.confirm(`Delete item '${row.name}'?`)) {
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
        searchPlaceholder="Search items…"
        rowKey={(row) => row.id}
        emptyMessage="No items yet."
        toolbar={canManage && <Button onClick={() => openEditor(null)}>New item</Button>}
      />

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit item' : 'New item'}
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
                value={editor.itemCategoryId}
                onChange={(e) => setEditor({ ...editor, itemCategoryId: e.target.value })}
              />
              <Select
                label="Unit of measure"
                placeholder="Select unit…"
                options={uoms.map((u) => ({ value: u.id, label: u.name }))}
                value={editor.uomId}
                onChange={(e) => setEditor({ ...editor, uomId: e.target.value })}
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <Input
                label="HSN code"
                value={editor.hsnCode}
                onChange={(e) => setEditor({ ...editor, hsnCode: e.target.value })}
              />
              <Select
                label="Tax"
                placeholder="— none —"
                options={taxes.map((t) => ({ value: t.id, label: t.name }))}
                value={editor.taxId}
                onChange={(e) => setEditor({ ...editor, taxId: e.target.value })}
              />
            </div>
            <Input
              label="Reorder level"
              type="number"
              min={0}
              value={editor.reorderLevel}
              onChange={(e) => setEditor({ ...editor, reorderLevel: e.target.value })}
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
