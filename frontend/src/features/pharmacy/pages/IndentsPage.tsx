import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
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
import { useDrugs, useItems, useSimpleMaster } from '../../masters/api'
import { ProductPicker } from '../components/ProductPicker'
import { useCreateIndent, useDecideIndent, useIndent, useIndents, useStores } from '../api'
import { INDENT_STATUS_LABELS, type IndentDetail, type IndentListItem, type IndentStatus } from '../types'
import { compactErrors, hasErrors, requiredSelect, type FieldErrors } from '../../../lib/formValidation'

const statusTone: Record<IndentStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'warning', 2: 'info', 3: 'danger', 4: 'success',
}

export function IndentsPage() {
  const { hasPermission } = useAuth()
  const canCreate = hasPermission('PHARMACY.INDENT.CREATE')
  const canApprove = hasPermission('PHARMACY.INDENT.APPROVE')
  const { data: indents = [], isLoading } = useIndents()
  const [creating, setCreating] = useState(false)
  const [viewingId, setViewingId] = useState<number | null>(null)

  return (
    <>
      <PageHeader
        title="Indents"
        subtitle="A department's request against a store — request, approve/reject, then fulfil with a stock issue"
        actions={canCreate && <Button onClick={() => setCreating(true)}>New indent</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Indent No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Store</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Department</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Items</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {indents.map((i: IndentListItem) => (
              <tr key={i.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{i.indentNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{i.indentDate}</td>
                <td className="px-4 py-2.5 text-slate-700">{i.storeName}</td>
                <td className="px-4 py-2.5 text-slate-700">{i.departmentName}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{i.itemCount}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={statusTone[i.status]}>{INDENT_STATUS_LABELS[i.status]}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  <Button variant="ghost" onClick={() => setViewingId(i.id)}>
                    View
                  </Button>
                </td>
              </tr>
            ))}
            {!isLoading && indents.length === 0 && (
              <tr>
                <td colSpan={7} className="px-4 py-10 text-center text-slate-400">
                  No indents yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <CreateModal onClose={() => setCreating(false)} />}
      {viewingId !== null && <DetailModal id={viewingId} canApprove={canApprove} onClose={() => setViewingId(null)} />}
    </>
  )
}

function DetailModal({ id, canApprove, onClose }: { id: number; canApprove: boolean; onClose: () => void }) {
  const { data: indent, isLoading } = useIndent(id)
  const decide = useDecideIndent()
  const navigate = useNavigate()
  const [approvedQuantities, setApprovedQuantities] = useState<Record<number, string>>({})
  const [rejecting, setRejecting] = useState(false)
  const [rejectionReason, setRejectionReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  if (isLoading || !indent) {
    return (
      <Modal open title="Indent" onClose={onClose} footer={null}>
        <Spinner />
      </Modal>
    )
  }

  const quantityFor = (item: IndentDetail['items'][number]) =>
    approvedQuantities[item.id] ?? String(item.approvedQuantity ?? item.requestedQuantity)

  const approve = async () => {
    setError(null)
    try {
      await decide.mutateAsync({
        id,
        approve: true,
        rejectionReason: null,
        items: indent.items.map((item) => ({ indentItemId: item.id, approvedQuantity: Number(quantityFor(item)) })),
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const reject = async () => {
    setError(null)
    try {
      await decide.mutateAsync({ id, approve: false, rejectionReason, items: null })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const fulfil = () => {
    navigate('/pharmacy/issues', {
      state: {
        prefillIssue: {
          storeId: indent.storeId,
          departmentId: indent.departmentId,
          indentId: indent.id,
          items: indent.items.map((item) => ({ label: item.productName, approvedQuantity: item.approvedQuantity ?? item.requestedQuantity })),
        },
      },
    })
  }

  return (
    <Modal
      open
      title={indent.indentNumber}
      onClose={onClose}
      footer={
        indent.status === 1 && canApprove ? (
          <>
            <Button variant="ghost" className="text-red-600" onClick={() => setRejecting(true)}>
              Reject
            </Button>
            <Button onClick={approve} loading={decide.isPending}>
              Approve
            </Button>
          </>
        ) : indent.status === 2 ? (
          <Button onClick={fulfil}>Fulfil (create issue)</Button>
        ) : (
          <Button variant="secondary" onClick={onClose}>
            Close
          </Button>
        )
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <div className="grid grid-cols-2 gap-2 text-sm">
          <div>
            <span className="text-slate-500">Store</span>
            <p className="font-medium text-slate-800">{indent.storeName}</p>
          </div>
          <div>
            <span className="text-slate-500">Department</span>
            <p className="font-medium text-slate-800">{indent.departmentName}</p>
          </div>
        </div>
        {indent.notes && <p className="text-sm text-slate-600">{indent.notes}</p>}
        {indent.rejectionReason && <p className="text-sm text-red-600">Rejected: {indent.rejectionReason}</p>}

        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-left">
              <th className="py-1.5 font-medium text-slate-600">Product</th>
              <th className="py-1.5 text-right font-medium text-slate-600">Requested</th>
              <th className="py-1.5 text-right font-medium text-slate-600">Approved</th>
            </tr>
          </thead>
          <tbody>
            {indent.items.map((item) => (
              <tr key={item.id} className="border-b border-slate-100 last:border-0">
                <td className="py-1.5 text-slate-700">{item.productName}</td>
                <td className="py-1.5 text-right text-slate-700">{item.requestedQuantity}</td>
                <td className="py-1.5 text-right">
                  {indent.status === 1 && canApprove ? (
                    <input
                      type="number"
                      className="w-24 rounded-md border border-slate-300 px-2 py-1 text-right text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
                      value={quantityFor(item)}
                      onChange={(e) => setApprovedQuantities((prev) => ({ ...prev, [item.id]: e.target.value }))}
                    />
                  ) : (
                    (item.approvedQuantity ?? '—')
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {rejecting && (
          <div className="space-y-2 rounded-md border border-red-200 bg-red-50 p-3">
            <Input label="Rejection reason" value={rejectionReason} onChange={(e) => setRejectionReason(e.target.value)} />
            <Button variant="danger" onClick={reject} loading={decide.isPending} disabled={!rejectionReason.trim()}>
              Confirm rejection
            </Button>
          </div>
        )}
      </div>
    </Modal>
  )
}

function CreateModal({ onClose }: { onClose: () => void }) {
  const create = useCreateIndent()
  const { data: storesResult } = useStores()
  const { data: departments = [] } = useSimpleMaster('departments', endpoints.masters.departments)
  const { data: drugsResult } = useDrugs({ page: 1, pageSize: 500 })
  const { data: itemsResult } = useItems({ page: 1, pageSize: 500 })
  const [storeId, setStoreId] = useState('')
  const [departmentId, setDepartmentId] = useState('')
  const [lines, setLines] = useState<{ drugId: string; itemId: string; qty: string }[]>([{ drugId: '', itemId: '', qty: '' }])
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const items = lines
      .filter((l) => (l.drugId || l.itemId) && l.qty)
      .map((l) => ({ drugId: l.drugId ? Number(l.drugId) : null, itemId: l.itemId ? Number(l.itemId) : null, requestedQuantity: Number(l.qty) }))
    const errors = compactErrors({
      storeId: requiredSelect(storeId, 'Store'),
      departmentId: requiredSelect(departmentId, 'Department'),
      items: items.length === 0 ? 'Add at least one complete line — each needs a drug or item and a quantity.' : undefined,
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await create.mutateAsync({ storeId: Number(storeId), departmentId: Number(departmentId), notes: notes || null, items })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="New indent"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={create.isPending}>
            Raise indent
          </Button>
        </>
      }
    >
      <div className="max-h-[60vh] space-y-3 overflow-y-auto">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        {fieldErrors.items && (
          <p role="alert" className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">
            {fieldErrors.items}
          </p>
        )}
        <div className="grid grid-cols-2 gap-3">
          <Select
            label="Store"
            required
            error={fieldErrors.storeId}
            placeholder="Select…"
            value={storeId}
            onChange={(e) => setStoreId(e.target.value)}
            options={(storesResult ?? []).map((s) => ({ value: s.id, label: s.name }))}
          />
          <Select
            label="Department"
            required
            error={fieldErrors.departmentId}
            placeholder="Select…"
            value={departmentId}
            onChange={(e) => setDepartmentId(e.target.value)}
            options={departments.map((d) => ({ value: d.id, label: d.name }))}
          />
        </div>
        {lines.map((line, idx) => (
          <div key={idx} className="flex items-end gap-2">
            <ProductPicker
              className="flex-1"
              value={line}
              onChange={(v) => setLines(lines.map((l, i) => (i === idx ? { ...l, ...v } : l)))}
              drugs={drugsResult?.items ?? []}
              items={itemsResult?.items ?? []}
            />
            <input
              type="number"
              placeholder="Qty"
              className="w-24 rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
              value={line.qty}
              onChange={(e) => setLines(lines.map((l, i) => (i === idx ? { ...l, qty: e.target.value } : l)))}
            />
          </div>
        ))}
        <Button variant="secondary" onClick={() => setLines([...lines, { drugId: '', itemId: '', qty: '' }])}>
          Add line
        </Button>
        <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>
    </Modal>
  )
}
