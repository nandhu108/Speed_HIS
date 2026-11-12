import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { useAuth } from '../../auth/AuthContext'
import { useSimpleMaster } from '../../masters/api'
import { endpoints } from '../../../api/endpoints'
import { useBatches, useCreateIssue, useIssues, useStores } from '../api'
import { ISSUE_TYPE_LABELS, type IssueListItem } from '../types'
import { compactErrors, hasErrors, requiredSelect, type FieldErrors } from '../../../lib/formValidation'

interface IssuePrefill {
  storeId: number
  departmentId: number
  indentId: number
  items: { label: string; approvedQuantity: number }[]
}

export function IssuesPage() {
  const { hasPermission } = useAuth()
  const canCreate = hasPermission('PHARMACY.ISSUE.CREATE')
  const { data: issues = [], isLoading } = useIssues()
  const location = useLocation()
  const navigate = useNavigate()
  const prefill = (location.state as { prefillIssue?: IssuePrefill } | null)?.prefillIssue ?? null
  const [creating, setCreating] = useState(prefill !== null)

  const closeCreate = () => {
    setCreating(false)
    if (prefill) navigate(location.pathname, { replace: true, state: null })
  }

  return (
    <>
      <PageHeader
        title="Stock Issues"
        subtitle="Department and emergency issues (IP issue needs Phase 9 admissions to generate from)"
        actions={canCreate && <Button onClick={() => setCreating(true)}>New issue</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Issue No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Department</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Items</th>
            </tr>
          </thead>
          <tbody>
            {issues.map((i: IssueListItem) => (
              <tr key={i.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{i.issueNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{i.issueDate}</td>
                <td className="px-4 py-2.5 text-slate-700">{ISSUE_TYPE_LABELS[i.issueType]}</td>
                <td className="px-4 py-2.5 text-slate-700">{i.departmentName ?? '—'}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{i.itemCount}</td>
              </tr>
            ))}
            {!isLoading && issues.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                  No issues yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <CreateModal onClose={closeCreate} prefill={prefill} />}
    </>
  )
}

function CreateModal({ onClose, prefill }: { onClose: () => void; prefill: IssuePrefill | null }) {
  const create = useCreateIssue()
  const { data: storesResult } = useStores()
  const { data: departments = [] } = useSimpleMaster('departments', endpoints.masters.departments)
  const [storeId, setStoreId] = useState(prefill ? String(prefill.storeId) : '')
  const [issueType, setIssueType] = useState<'1' | '2'>('1')
  const [departmentId, setDepartmentId] = useState(prefill ? String(prefill.departmentId) : '')
  const { data: batches = [] } = useBatches(undefined, undefined, storeId ? Number(storeId) : undefined, true)
  const [lines, setLines] = useState<{ batchId: string; qty: string }[]>([{ batchId: '', qty: '' }])
  const [notes, setNotes] = useState(prefill ? `Fulfils indent — approved: ${prefill.items.map((i) => `${i.label} x${i.approvedQuantity}`).join(', ')}` : '')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const items = lines.filter((l) => l.batchId && l.qty).map((l) => ({ batchId: Number(l.batchId), quantity: Number(l.qty) }))
    const errors = compactErrors({
      storeId: requiredSelect(storeId, 'Store'),
      items: items.length === 0 ? 'Add at least one complete line — each needs a batch and a quantity.' : undefined,
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await create.mutateAsync({
        storeId: Number(storeId),
        issueType: Number(issueType) as 1 | 2,
        departmentId: departmentId ? Number(departmentId) : null,
        patientId: null,
        admissionId: null,
        indentId: prefill?.indentId ?? null,
        notes: notes || null,
        items,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={prefill ? `Fulfil indent (#${prefill.indentId})` : 'New stock issue'}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={create.isPending}>
            Create
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
            label="Type"
            value={issueType}
            onChange={(e) => setIssueType(e.target.value as '1' | '2')}
            options={[
              { value: '1', label: 'Department' },
              { value: '2', label: 'Emergency' },
            ]}
          />
          {issueType === '1' && (
            <Select
              label="Department"
              placeholder="Select…"
              value={departmentId}
              onChange={(e) => setDepartmentId(e.target.value)}
              options={departments.map((d) => ({ value: d.id, label: d.name }))}
            />
          )}
        </div>
        {lines.map((line, idx) => (
          <div key={idx} className="flex items-end gap-2">
            <Select
              label="Batch"
              className="flex-1"
              placeholder="Select…"
              value={line.batchId}
              onChange={(e) => setLines(lines.map((l, i) => (i === idx ? { ...l, batchId: e.target.value } : l)))}
              options={batches.map((b) => ({ value: b.id, label: `${b.productName} — ${b.batchNumber} (${b.quantityOnHand} on hand)` }))}
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
        <Button variant="secondary" onClick={() => setLines([...lines, { batchId: '', qty: '' }])}>
          Add line
        </Button>
        <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>
    </Modal>
  )
}
