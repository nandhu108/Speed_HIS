import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import {
  useBillEstimateTemplate,
  useBillEstimateTemplates,
  useDeleteBillEstimateTemplate,
  usePackages,
  useSaveBillEstimateTemplate,
  useServices,
} from '../api'
import type { EstimateLineSourceType } from '../types'

interface EditorLine {
  sourceType: EstimateLineSourceType
  serviceId: number | null
  serviceName: string | null
  packageId: number | null
  packageName: string | null
  quantity: string
}

interface EditorState {
  id?: number
  code: string
  name: string
  description: string
  items: EditorLine[]
  isActive: boolean
}

export function BillEstimateTemplatesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.BILLESTIMATETEMPLATE.MANAGE')
  const { data: templates = [], isLoading } = useBillEstimateTemplates()
  const save = useSaveBillEstimateTemplate()
  const remove = useDeleteBillEstimateTemplate()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [editId, setEditId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  const { data: detail } = useBillEstimateTemplate(editId)
  if (editId !== null && detail && editor === null) {
    setEditor({
      id: detail.id,
      code: detail.code,
      name: detail.name,
      description: detail.description ?? '',
      items: detail.items.map((item) => ({
        sourceType: item.sourceType,
        serviceId: item.serviceId,
        serviceName: item.serviceName,
        packageId: item.packageId,
        packageName: item.packageName,
        quantity: String(item.quantity),
      })),
      isActive: detail.isActive,
    })
  }

  const closeEditor = () => {
    setEditor(null)
    setEditId(null)
  }

  const submit = async () => {
    if (!editor) return
    setError(null)
    try {
      await save.mutateAsync({
        id: editor.id,
        code: editor.code,
        name: editor.name,
        description: editor.description || null,
        items: editor.items.map((item) => ({
          sourceType: item.sourceType,
          serviceId: item.serviceId,
          packageId: item.packageId,
          quantity: Number(item.quantity),
        })),
        isActive: editor.isActive,
      })
      closeEditor()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader
        title="Bill Estimate Templates"
        subtitle="Reusable Service/Package line sets that seed a pre-admission cost estimate"
        actions={
          canManage && (
            <Button
              onClick={() => {
                setError(null)
                setEditId(null)
                setEditor({ code: '', name: '', description: '', items: [], isActive: true })
              }}
            >
              New template
            </Button>
          )
        }
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Lines</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {templates.map((t) => (
              <tr key={t.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{t.code}</td>
                <td className="px-4 py-2.5 font-medium text-slate-700">{t.name}</td>
                <td className="px-4 py-2.5 text-slate-500">{t.itemCount}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={t.isActive ? 'success' : 'neutral'}>
                    {t.isActive ? 'Active' : 'Inactive'}
                  </StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canManage && (
                    <div className="flex justify-end gap-1">
                      <Button
                        variant="ghost"
                        onClick={() => {
                          setError(null)
                          setEditor(null)
                          setEditId(t.id)
                        }}
                      >
                        Edit
                      </Button>
                      <Button
                        variant="ghost"
                        className="text-red-600"
                        onClick={async () => {
                          if (window.confirm(`Delete template '${t.name}'?`)) {
                            try {
                              await remove.mutateAsync(t.id)
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
            {!isLoading && templates.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                  No templates yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit template' : 'New template'}
          onClose={closeEditor}
          footer={
            <>
              <Button variant="secondary" onClick={closeEditor}>
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
            <Input
              label="Description"
              value={editor.description}
              onChange={(e) => setEditor({ ...editor, description: e.target.value })}
            />

            <div>
              <span className="mb-1 block text-sm font-medium text-slate-700">Lines</span>
              {editor.items.length > 0 && (
                <table className="mb-2 w-full text-sm">
                  <tbody>
                    {editor.items.map((item, index) => (
                      <tr key={index} className="border-b border-slate-100 last:border-0">
                        <td className="py-1 pr-2">
                          {item.sourceType === 3 ? item.serviceName : item.packageName}
                          <span className="ml-1 text-xs text-slate-400">
                            ({item.sourceType === 3 ? 'Service' : 'Package'})
                          </span>
                        </td>
                        <td className="w-24 py-1 pr-2">
                          <Input
                            type="number"
                            min={1}
                            step="0.001"
                            aria-label="Quantity"
                            value={item.quantity}
                            onChange={(e) => {
                              const items = [...editor.items]
                              items[index] = { ...item, quantity: e.target.value }
                              setEditor({ ...editor, items })
                            }}
                          />
                        </td>
                        <td className="w-10 py-1 text-right">
                          <Button
                            variant="ghost"
                            className="px-2 py-1 text-xs text-red-600"
                            onClick={() => setEditor({ ...editor, items: editor.items.filter((_, i) => i !== index) })}
                          >
                            ✕
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
              <AddLineForm
                onAdd={(line) => setEditor({ ...editor, items: [...editor.items, line] })}
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

function AddLineForm({ onAdd }: { onAdd: (line: EditorLine) => void }) {
  const { data: servicesResult } = useServices({ page: 1, pageSize: 500 })
  const { data: packages = [] } = usePackages()
  const [sourceType, setSourceType] = useState<'3' | '4'>('3')
  const [refId, setRefId] = useState('')
  const [quantity, setQuantity] = useState('1')

  const add = () => {
    if (!refId) return
    if (sourceType === '3') {
      const service = (servicesResult?.items ?? []).find((s) => s.id === Number(refId))
      if (!service) return
      onAdd({ sourceType: 3, serviceId: service.id, serviceName: service.name, packageId: null, packageName: null, quantity })
    } else {
      const pkg = packages.find((p) => p.id === Number(refId))
      if (!pkg) return
      onAdd({ sourceType: 4, serviceId: null, serviceName: null, packageId: pkg.id, packageName: pkg.name, quantity })
    }
    setRefId('')
    setQuantity('1')
  }

  return (
    <div className="flex items-end gap-2">
      <Select
        label="Type"
        value={sourceType}
        onChange={(e) => {
          setSourceType(e.target.value as '3' | '4')
          setRefId('')
        }}
        options={[
          { value: '3', label: 'Service' },
          { value: '4', label: 'Package' },
        ]}
      />
      {sourceType === '3' ? (
        <Select
          label="Service"
          placeholder="Select…"
          value={refId}
          onChange={(e) => setRefId(e.target.value)}
          options={(servicesResult?.items ?? []).map((s) => ({ value: s.id, label: s.name }))}
        />
      ) : (
        <Select
          label="Package"
          placeholder="Select…"
          value={refId}
          onChange={(e) => setRefId(e.target.value)}
          options={packages.map((p) => ({ value: p.id, label: p.name }))}
        />
      )}
      <Input label="Qty" type="number" min={1} step="0.001" className="w-20" value={quantity} onChange={(e) => setQuantity(e.target.value)} />
      <Button variant="secondary" onClick={add} disabled={!refId}>
        Add
      </Button>
    </div>
  )
}
