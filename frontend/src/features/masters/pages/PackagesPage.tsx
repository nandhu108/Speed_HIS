import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDeletePackage, usePackage, usePackages, useSavePackage } from '../api'
import { ServicePicker } from '../components/ServicePicker'

interface EditorItem {
  serviceId: number
  serviceCode: string
  serviceName: string
  quantity: string
}

interface EditorState {
  id?: number
  code: string
  name: string
  description: string
  price: string
  validityDays: string
  items: EditorItem[]
  isActive: boolean
}

export function PackagesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.PACKAGE.MANAGE')
  const { data: packages = [], isLoading } = usePackages()
  const save = useSavePackage()
  const remove = useDeletePackage()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [editId, setEditId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  const { data: detail } = usePackage(editId)
  if (editId !== null && detail && editor === null) {
    setEditor({
      id: detail.id,
      code: detail.code,
      name: detail.name,
      description: detail.description ?? '',
      price: String(detail.price),
      validityDays: String(detail.validityDays),
      items: detail.items.map((item) => ({
        serviceId: item.serviceId,
        serviceCode: item.serviceCode,
        serviceName: item.serviceName,
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
    try {
      await save.mutateAsync({
        id: editor.id,
        code: editor.code,
        name: editor.name,
        description: editor.description || null,
        price: Number(editor.price),
        validityDays: Number(editor.validityDays),
        items: editor.items.map((item) => ({ serviceId: item.serviceId, quantity: Number(item.quantity) })),
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
        title="Packages"
        subtitle="Bundled service offerings at a fixed price"
        actions={
          canManage && (
            <Button
              onClick={() => {
                setError(null)
                setEditId(null)
                setEditor({
                  code: '',
                  name: '',
                  description: '',
                  price: '0',
                  validityDays: '365',
                  items: [],
                  isActive: true,
                })
              }}
            >
              New package
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
              <th className="px-4 py-2.5 font-medium text-slate-600">Price</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Validity (days)</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Services</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {packages.map((pkg) => (
              <tr key={pkg.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{pkg.code}</td>
                <td className="px-4 py-2.5 font-medium text-slate-700">{pkg.name}</td>
                <td className="px-4 py-2.5 text-slate-700">{pkg.price.toLocaleString()}</td>
                <td className="px-4 py-2.5 text-slate-500">{pkg.validityDays}</td>
                <td className="px-4 py-2.5 text-slate-500">{pkg.itemCount}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={pkg.isActive ? 'success' : 'neutral'}>
                    {pkg.isActive ? 'Active' : 'Inactive'}
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
                          setEditId(pkg.id)
                        }}
                      >
                        Edit
                      </Button>
                      <Button
                        variant="ghost"
                        className="text-red-600"
                        onClick={async () => {
                          if (window.confirm(`Delete package '${pkg.name}'?`)) {
                            try {
                              await remove.mutateAsync(pkg.id)
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
            {!isLoading && packages.length === 0 && (
              <tr>
                <td colSpan={7} className="px-4 py-10 text-center text-slate-400">
                  No packages yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit package' : 'New package'}
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
            <div className="grid grid-cols-2 gap-3">
              <Input
                label="Price"
                type="number"
                min={0}
                step="0.01"
                value={editor.price}
                onChange={(e) => setEditor({ ...editor, price: e.target.value })}
              />
              <Input
                label="Validity (days)"
                type="number"
                min={1}
                value={editor.validityDays}
                onChange={(e) => setEditor({ ...editor, validityDays: e.target.value })}
              />
            </div>

            <div>
              <span className="mb-1 block text-sm font-medium text-slate-700">Included services</span>
              {editor.items.length > 0 && (
                <table className="mb-2 w-full text-sm">
                  <tbody>
                    {editor.items.map((item, index) => (
                      <tr key={item.serviceId} className="border-b border-slate-100 last:border-0">
                        <td className="py-1 pr-2">
                          <span className="font-mono text-xs text-slate-500">{item.serviceCode}</span>{' '}
                          {item.serviceName}
                        </td>
                        <td className="w-24 py-1 pr-2">
                          <Input
                            type="number"
                            min={1}
                            aria-label={`Quantity for ${item.serviceName}`}
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
                            onClick={() =>
                              setEditor({ ...editor, items: editor.items.filter((_, i) => i !== index) })
                            }
                          >
                            ✕
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
              <ServicePicker
                excludeIds={editor.items.map((item) => item.serviceId)}
                onPick={(service) =>
                  setEditor({
                    ...editor,
                    items: [
                      ...editor.items,
                      {
                        serviceId: service.id,
                        serviceCode: service.code,
                        serviceName: service.name,
                        quantity: '1',
                      },
                    ],
                  })
                }
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
