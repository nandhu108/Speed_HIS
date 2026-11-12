import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { PatientPicker } from '../../appointments/components/PatientPicker'
import { useAuth } from '../../auth/AuthContext'
import { useBillEstimateTemplate, useBillEstimateTemplates, usePackages, useServices } from '../../masters/api'
import type { PatientListItem } from '../../patients/types'
import {
  openBillEstimatePdf,
  useBillEstimate,
  useBillEstimates,
  useCopyBillEstimate,
  useCreateBillEstimate,
  useDeleteBillEstimate,
} from '../api'
import { BILL_ESTIMATE_STATUS_LABELS, type BillEstimateListItem, type BillEstimateStatus, type EstimateLineSourceType } from '../types'

const statusTone: Record<BillEstimateStatus, 'info' | 'success'> = { 1: 'info', 2: 'success' }

export function BillEstimatesPage() {
  const { hasPermission } = useAuth()
  const canCreate = hasPermission('BILLING.ESTIMATE.CREATE')

  const [patient, setPatient] = useState<PatientListItem | null>(null)
  const [page, setPage] = useState(1)
  const [creating, setCreating] = useState(false)
  const [viewingId, setViewingId] = useState<number | null>(null)

  const { data, isLoading } = useBillEstimates({ page, pageSize: 20, patientId: patient?.id })
  const copy = useCopyBillEstimate()
  const remove = useDeleteBillEstimate()

  const columns: Column<BillEstimateListItem>[] = [
    { header: 'Estimate No', cell: (row) => <span className="font-mono text-xs">{row.estimateNumber}</span> },
    { header: 'Date', cell: (row) => row.estimateDate },
    { header: 'Patient', cell: (row) => `${row.patientName} (${row.patientUhid})` },
    {
      header: 'Status',
      cell: (row) => <StatusBadge tone={statusTone[row.status]}>{BILL_ESTIMATE_STATUS_LABELS[row.status]}</StatusBadge>,
    },
    { header: 'Total', className: 'text-right', cell: (row) => row.totalAmount.toFixed(2) },
    {
      header: '',
      className: 'text-right',
      cell: (row) => (
        <div className="flex justify-end gap-1">
          <Button variant="ghost" onClick={() => setViewingId(row.id)}>
            View
          </Button>
          <Button variant="ghost" onClick={() => openBillEstimatePdf(row.id)}>
            Print
          </Button>
          {canCreate && (
            <>
              <Button variant="ghost" onClick={() => copy.mutateAsync(row.id).catch((err) => window.alert(errorMessage(err)))}>
                Copy
              </Button>
              <Button
                variant="ghost"
                className="text-red-600"
                onClick={async () => {
                  if (window.confirm(`Delete estimate '${row.estimateNumber}'?`)) {
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
            </>
          )}
        </div>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title="Bill Estimates"
        subtitle="Pre-admission cost estimates — a counselling tool, not a bill"
        actions={canCreate && <Button onClick={() => setCreating(true)}>New estimate</Button>}
      />

      <div className="mb-4 max-w-md">
        <PatientPicker selected={patient} onSelect={setPatient} />
      </div>

      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        rowKey={(row) => row.id}
        emptyMessage="No estimates yet."
      />

      {creating && <CreateEstimateModal defaultPatient={patient} onClose={() => setCreating(false)} />}
      {viewingId !== null && <ViewEstimateModal id={viewingId} onClose={() => setViewingId(null)} />}
    </>
  )
}

interface EditorLine {
  sourceType: EstimateLineSourceType
  serviceId: number | null
  serviceName: string | null
  packageId: number | null
  packageName: string | null
  quantity: string
}

function CreateEstimateModal({ defaultPatient, onClose }: { defaultPatient: PatientListItem | null; onClose: () => void }) {
  const create = useCreateBillEstimate()
  const { data: templates = [] } = useBillEstimateTemplates()
  const [templateId, setTemplateId] = useState('')
  const { data: templateDetail } = useBillEstimateTemplate(templateId ? Number(templateId) : null)

  const [patient, setPatient] = useState<PatientListItem | null>(defaultPatient)
  const [items, setItems] = useState<EditorLine[]>([])
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)

  const loadTemplate = () => {
    if (!templateDetail) return
    setItems(
      templateDetail.items.map((i) => ({
        sourceType: i.sourceType,
        serviceId: i.serviceId,
        serviceName: i.serviceName,
        packageId: i.packageId,
        packageName: i.packageName,
        quantity: String(i.quantity),
      })),
    )
  }

  const submit = async () => {
    if (!patient) return
    setError(null)
    try {
      await create.mutateAsync({
        patientId: patient.id,
        billEstimateTemplateId: templateId ? Number(templateId) : null,
        items: items.map((i) => ({ sourceType: i.sourceType, serviceId: i.serviceId, packageId: i.packageId, quantity: Number(i.quantity) })),
        notes: notes || null,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="New bill estimate"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={create.isPending} disabled={!patient || items.length === 0}>
            Create
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <PatientPicker selected={patient} onSelect={setPatient} />

        <div className="flex items-end gap-2">
          <Select
            label="Load from template (optional)"
            placeholder="None…"
            value={templateId}
            onChange={(e) => setTemplateId(e.target.value)}
            options={templates.map((t) => ({ value: t.id, label: t.name }))}
          />
          <Button variant="secondary" onClick={loadTemplate} disabled={!templateDetail}>
            Load lines
          </Button>
        </div>

        <div>
          <span className="mb-1 block text-sm font-medium text-slate-700">Lines</span>
          {items.length > 0 && (
            <table className="mb-2 w-full text-sm">
              <tbody>
                {items.map((item, index) => (
                  <tr key={index} className="border-b border-slate-100 last:border-0">
                    <td className="py-1 pr-2">
                      {item.sourceType === 3 ? item.serviceName : item.packageName}
                      <span className="ml-1 text-xs text-slate-400">({item.sourceType === 3 ? 'Service' : 'Package'})</span>
                    </td>
                    <td className="w-24 py-1 pr-2">
                      <Input
                        type="number"
                        min={1}
                        step="0.001"
                        aria-label="Quantity"
                        value={item.quantity}
                        onChange={(e) => {
                          const next = [...items]
                          next[index] = { ...item, quantity: e.target.value }
                          setItems(next)
                        }}
                      />
                    </td>
                    <td className="w-10 py-1 text-right">
                      <Button variant="ghost" className="px-2 py-1 text-xs text-red-600" onClick={() => setItems(items.filter((_, i) => i !== index))}>
                        ✕
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
          <EstimateAddLineForm onAdd={(line) => setItems([...items, line])} />
        </div>

        <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>
    </Modal>
  )
}

function EstimateAddLineForm({ onAdd }: { onAdd: (line: EditorLine) => void }) {
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

function ViewEstimateModal({ id, onClose }: { id: number; onClose: () => void }) {
  const { data: estimate } = useBillEstimate(id)
  if (!estimate) return null

  return (
    <Modal open title={`Estimate ${estimate.estimateNumber}`} onClose={onClose} footer={<Button onClick={onClose}>Close</Button>}>
      <div className="space-y-3 text-sm">
        <p className="text-slate-600">
          {estimate.patientName} ({estimate.patientUhid}) · {estimate.estimateDate}
        </p>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-left">
              <th className="py-1.5 font-medium text-slate-600">Description</th>
              <th className="py-1.5 text-right font-medium text-slate-600">Qty</th>
              <th className="py-1.5 text-right font-medium text-slate-600">Rate</th>
              <th className="py-1.5 text-right font-medium text-slate-600">Total</th>
            </tr>
          </thead>
          <tbody>
            {estimate.items.map((item) => (
              <tr key={item.id} className="border-b border-slate-100 last:border-0">
                <td className="py-1.5 text-slate-700">{item.description}</td>
                <td className="py-1.5 text-right text-slate-700">{item.quantity}</td>
                <td className="py-1.5 text-right text-slate-700">{item.rate.toFixed(2)}</td>
                <td className="py-1.5 text-right font-medium text-slate-800">{item.totalAmount.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
        <p className="text-right text-base font-semibold text-slate-800">Total: {estimate.totalAmount.toFixed(2)}</p>
        {estimate.notes && <p className="text-slate-600">Notes: {estimate.notes}</p>}
      </div>
    </Modal>
  )
}
