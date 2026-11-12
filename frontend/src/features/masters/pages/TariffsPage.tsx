import { useRef, useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { endpoints } from '../../../api/endpoints'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import {
  downloadTariffExport,
  useDeleteTariff,
  useImportTariffCsv,
  useRoomTypes,
  useSaveTariff,
  useSimpleMaster,
  useTariff,
  useTariffs,
} from '../api'
import { ServicePicker } from '../components/ServicePicker'
import type { TariffImportResult, TariffListItem } from '../types'

interface EditorItem {
  serviceId: number
  serviceCode: string
  serviceName: string
  roomTypeId: string
  rate: string
}

interface EditorState {
  id?: number
  code: string
  name: string
  patientCategoryId: string
  effectiveFrom: string
  effectiveTo: string
  items: EditorItem[]
  isActive: boolean
}

export function TariffsPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.TARIFF.MANAGE')
  const canImport = hasPermission('MASTER.TARIFF.IMPORT')
  const { data: tariffs = [], isLoading } = useTariffs()
  const { data: patientCategories = [] } = useSimpleMaster('patient-categories', endpoints.masters.patientCategories)
  const { data: roomTypes = [] } = useRoomTypes()
  const save = useSaveTariff()
  const remove = useDeleteTariff()
  const importCsv = useImportTariffCsv()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [editId, setEditId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [importResult, setImportResult] = useState<TariffImportResult | null>(null)
  const [exporting, setExporting] = useState(false)

  const { data: detail } = useTariff(editId)
  if (editId !== null && detail && editor === null) {
    setEditor({
      id: detail.id,
      code: detail.code,
      name: detail.name,
      patientCategoryId: detail.patientCategoryId ? String(detail.patientCategoryId) : '',
      effectiveFrom: detail.effectiveFrom,
      effectiveTo: detail.effectiveTo ?? '',
      items: detail.items.map((item) => ({
        serviceId: item.serviceId,
        serviceCode: item.serviceCode,
        serviceName: item.serviceName,
        roomTypeId: item.roomTypeId ? String(item.roomTypeId) : '',
        rate: String(item.rate),
      })),
      isActive: detail.isActive,
    })
  }

  const closeEditor = () => {
    setEditor(null)
    setEditId(null)
  }

  const onFilePicked = async (file: File | undefined) => {
    if (!file) return
    try {
      const outcome = await importCsv.mutateAsync(file)
      setImportResult(outcome)
    } catch (err) {
      window.alert(errorMessage(err))
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  const runExport = async (format: 'csv' | 'excel' | 'pdf') => {
    setExporting(true)
    try {
      await downloadTariffExport(format)
    } catch (err) {
      window.alert(errorMessage(err))
    } finally {
      setExporting(false)
    }
  }

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({
        id: editor.id,
        code: editor.code,
        name: editor.name,
        patientCategoryId: editor.patientCategoryId ? Number(editor.patientCategoryId) : null,
        effectiveFrom: editor.effectiveFrom,
        effectiveTo: editor.effectiveTo || null,
        items: editor.items.map((item) => ({
          serviceId: item.serviceId,
          rate: Number(item.rate),
          roomTypeId: item.roomTypeId ? Number(item.roomTypeId) : null,
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
        title="Tariffs"
        subtitle="Rate plans mapping services to prices per patient category and room type"
        actions={
          <div className="flex gap-2">
            <Button variant="secondary" loading={exporting} onClick={() => runExport('csv')}>
              Export CSV
            </Button>
            <Button variant="secondary" loading={exporting} onClick={() => runExport('excel')}>
              Export Excel
            </Button>
            {canImport && (
              <>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".csv,text/csv"
                  className="hidden"
                  onChange={(e) => onFilePicked(e.target.files?.[0])}
                />
                <Button variant="secondary" loading={importCsv.isPending} onClick={() => fileInputRef.current?.click()}>
                  Import CSV
                </Button>
              </>
            )}
            {canManage && (
              <Button
                onClick={() => {
                  setError(null)
                  setEditId(null)
                  setEditor({
                    code: '',
                    name: '',
                    patientCategoryId: '',
                    effectiveFrom: new Date().toISOString().slice(0, 10),
                    effectiveTo: '',
                    items: [],
                    isActive: true,
                  })
                }}
              >
                New tariff
              </Button>
            )}
          </div>
        }
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient category</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Effective</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Services</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {tariffs.map((tariff: TariffListItem) => (
              <tr key={tariff.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{tariff.code}</td>
                <td className="px-4 py-2.5 font-medium text-slate-700">{tariff.name}</td>
                <td className="px-4 py-2.5 text-slate-500">{tariff.patientCategoryName ?? 'All'}</td>
                <td className="px-4 py-2.5 text-slate-500">
                  {tariff.effectiveFrom}
                  {tariff.effectiveTo ? ` → ${tariff.effectiveTo}` : ' onwards'}
                </td>
                <td className="px-4 py-2.5 text-slate-500">{tariff.itemCount}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={tariff.isActive ? 'success' : 'neutral'}>
                    {tariff.isActive ? 'Active' : 'Inactive'}
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
                          setEditId(tariff.id)
                        }}
                      >
                        Edit
                      </Button>
                      <Button
                        variant="ghost"
                        className="text-red-600"
                        onClick={async () => {
                          if (window.confirm(`Delete tariff '${tariff.name}'?`)) {
                            try {
                              await remove.mutateAsync(tariff.id)
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
            {!isLoading && tariffs.length === 0 && (
              <tr>
                <td colSpan={7} className="px-4 py-10 text-center text-slate-400">
                  No tariffs yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit tariff' : 'New tariff'}
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
            <Select
              label="Patient category"
              placeholder="All categories"
              options={patientCategories.map((c) => ({ value: c.id, label: c.name }))}
              value={editor.patientCategoryId}
              onChange={(e) => setEditor({ ...editor, patientCategoryId: e.target.value })}
            />
            <div className="grid grid-cols-2 gap-3">
              <Input
                label="Effective from"
                type="date"
                value={editor.effectiveFrom}
                onChange={(e) => setEditor({ ...editor, effectiveFrom: e.target.value })}
              />
              <Input
                label="Effective to (optional)"
                type="date"
                value={editor.effectiveTo}
                onChange={(e) => setEditor({ ...editor, effectiveTo: e.target.value })}
              />
            </div>

            <div>
              <span className="mb-1 block text-sm font-medium text-slate-700">Service rates</span>
              <p className="mb-1.5 text-xs text-slate-500">
                Leave room type blank to price a service the same everywhere. Add the same service again to set a
                different rate for a specific room type — it only applies while the patient is in that room.
              </p>
              {editor.items.length > 0 && (
                <table className="mb-2 w-full text-sm">
                  <tbody>
                    {editor.items.map((item, index) => (
                      <tr key={index} className="border-b border-slate-100 last:border-0">
                        <td className="py-1 pr-2">
                          <span className="font-mono text-xs text-slate-500">{item.serviceCode}</span>{' '}
                          {item.serviceName}
                        </td>
                        <td className="w-36 py-1 pr-2">
                          <Select
                            placeholder="All room types"
                            aria-label={`Room type for ${item.serviceName}`}
                            options={roomTypes.map((r) => ({ value: r.id, label: r.name }))}
                            value={item.roomTypeId}
                            onChange={(e) => {
                              const items = [...editor.items]
                              items[index] = { ...item, roomTypeId: e.target.value }
                              setEditor({ ...editor, items })
                            }}
                          />
                        </td>
                        <td className="w-28 py-1 pr-2">
                          <Input
                            type="number"
                            min={0}
                            step="0.01"
                            aria-label={`Rate for ${item.serviceName}`}
                            value={item.rate}
                            onChange={(e) => {
                              const items = [...editor.items]
                              items[index] = { ...item, rate: e.target.value }
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
                // Only excludes a service once it already has a room-agnostic (blank room
                // type) row — re-picking it after that adds a second row to set a room-type-
                // specific override, rather than being treated as a duplicate.
                excludeIds={editor.items.filter((item) => !item.roomTypeId).map((item) => item.serviceId)}
                onPick={(service) =>
                  setEditor({
                    ...editor,
                    items: [
                      ...editor.items,
                      {
                        serviceId: service.id,
                        serviceCode: service.code,
                        serviceName: service.name,
                        roomTypeId: '',
                        rate: String(service.defaultRate),
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

      {importResult && (
        <Modal
          open
          title="Import complete"
          onClose={() => setImportResult(null)}
          footer={<Button onClick={() => setImportResult(null)}>Close</Button>}
        >
          <div className="space-y-2 text-sm text-slate-700">
            <p>
              <strong>{importResult.tariffsInserted}</strong> tariffs created,{' '}
              <strong>{importResult.tariffsUpdated}</strong> updated, <strong>{importResult.itemsUpserted}</strong> rate
              rows applied.
            </p>
            {importResult.errors.length > 0 && (
              <div className="max-h-48 overflow-y-auto rounded-md bg-red-50 p-3">
                <p className="mb-1 font-medium text-red-700">Errors</p>
                <ul className="list-inside list-disc space-y-0.5 text-red-700">
                  {importResult.errors.map((message, index) => (
                    <li key={index}>{message}</li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        </Modal>
      )}
    </>
  )
}
