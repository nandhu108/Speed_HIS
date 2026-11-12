import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useAuth } from '../../auth/AuthContext'
import {
  useApproveResult,
  useDepartmentWorklist,
  useEnterResult,
  useLabResult,
  useLabTest,
  useResultQueue,
  useVerifyResult,
} from '../api'
import { PRIORITY_LABELS, type DepartmentWorklistItem } from '../types'

type Tab = 'entry' | 'verify' | 'approve'

export function ResultWorklistPage() {
  const { hasPermission } = useAuth()
  const [tab, setTab] = useState<Tab>('entry')
  const [entering, setEntering] = useState<DepartmentWorklistItem | null>(null)
  const [reviewing, setReviewing] = useState<{ item: DepartmentWorklistItem; mode: 'verify' | 'approve' } | null>(null)
  const [error, setError] = useState<string | null>(null)

  const { data: entryQueue = [], isLoading: loadingEntry } = useDepartmentWorklist()
  const { data: verifyQueue = [], isLoading: loadingVerify } = useResultQueue(4)
  const { data: approveQueue = [], isLoading: loadingApprove } = useResultQueue(5)

  const canEnter = hasPermission('LAB.RESULT.CREATE')
  const canVerify = hasPermission('LAB.RESULT.VERIFY')
  const canApprove = hasPermission('LAB.RESULT.APPROVE')

  const tabs: { key: Tab; label: string; count: number }[] = [
    { key: 'entry', label: 'Entry', count: entryQueue.length },
    { key: 'verify', label: 'Verify', count: verifyQueue.length },
    { key: 'approve', label: 'Approve', count: approveQueue.length },
  ]

  const activeQueue = tab === 'entry' ? entryQueue : tab === 'verify' ? verifyQueue : approveQueue
  const activeLoading = tab === 'entry' ? loadingEntry : tab === 'verify' ? loadingVerify : loadingApprove

  return (
    <>
      <PageHeader title="Result Worklist" subtitle="Enter, verify and approve lab results" />

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="mb-4 flex gap-1 border-b border-slate-200">
        {tabs.map((t) => (
          <button
            key={t.key}
            type="button"
            onClick={() => setTab(t.key)}
            className={`border-b-2 px-4 py-2 text-sm font-medium ${
              tab === t.key ? 'border-primary-600 text-primary-700' : 'border-transparent text-slate-500 hover:text-slate-700'
            }`}
          >
            {t.label} {t.count > 0 && <span className="ml-1 text-xs text-slate-400">({t.count})</span>}
          </button>
        ))}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {activeLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Order No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Test</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Priority</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {activeQueue.map((item) => (
              <tr key={item.labOrderItemId} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{item.orderNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">
                  {item.patientName} ({item.patientUhid}) — {item.patientAgeDisplay} / {item.patientGender}
                </td>
                <td className="px-4 py-2.5 text-slate-700">{item.labTestName}</td>
                <td className="px-4 py-2.5 text-slate-700">{PRIORITY_LABELS[item.priority]}</td>
                <td className="px-4 py-2.5 text-right">
                  {tab === 'entry' && canEnter && (
                    <Button variant="secondary" onClick={() => setEntering(item)}>
                      Enter result
                    </Button>
                  )}
                  {tab === 'verify' && canVerify && (
                    <Button variant="secondary" onClick={() => setReviewing({ item, mode: 'verify' })}>
                      Review
                    </Button>
                  )}
                  {tab === 'approve' && canApprove && (
                    <Button variant="secondary" onClick={() => setReviewing({ item, mode: 'approve' })}>
                      Review
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {!activeLoading && activeQueue.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                  Nothing here.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {entering && <EnterResultModal item={entering} onClose={() => setEntering(null)} onError={setError} />}
      {reviewing && (
        <ReviewResultModal item={reviewing.item} mode={reviewing.mode} onClose={() => setReviewing(null)} onError={setError} />
      )}
    </>
  )
}

function EnterResultModal({
  item,
  onClose,
  onError,
}: {
  item: DepartmentWorklistItem
  onClose: () => void
  onError: (msg: string) => void
}) {
  const { data: test } = useLabTest(item.labTestId)
  const enterResult = useEnterResult()
  const [values, setValues] = useState<Record<number, string>>({})
  const [notes, setNotes] = useState('')
  // Local to the dialog: `onError` renders on the page behind this overlay, so a message sent
  // there while the modal is open is invisible.
  const [formError, setFormError] = useState<string | null>(null)

  const submit = async () => {
    setFormError(null)
    const payloadValues = Object.entries(values)
      .filter(([, v]) => v.trim() !== '')
      .map(([parameterId, value]) => ({ labTestParameterId: Number(parameterId), value }))
    if (payloadValues.length === 0) {
      setFormError('Enter a value for at least one parameter.')
      return
    }
    try {
      await enterResult.mutateAsync({ itemId: item.labOrderItemId, values: payloadValues, notes: notes || null })
      onClose()
    } catch (err) {
      onError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Enter result — ${item.labTestName}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={enterResult.isPending}>
            Save
          </Button>
        </>
      }
    >
      {!test && <Spinner />}
      {test && (
        <div className="space-y-3">
          {formError && (
            <p role="alert" className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">
              {formError}
            </p>
          )}
          <p className="text-sm text-slate-600">
            {item.patientName} ({item.patientUhid}) — {item.patientAgeDisplay} / {item.patientGender}
          </p>
          {test.parameters.map((p) => (
            <div key={p.id} className="flex items-center gap-3">
              <label className="w-40 shrink-0 text-sm font-medium text-slate-700">
                {p.name} {p.unit && <span className="text-xs text-slate-400">({p.unit})</span>}
              </label>
              {p.valueType === 3 ? (
                <select
                  className="flex-1 rounded-md border border-slate-300 bg-panel-solid px-3 py-2 text-sm text-ink outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
                  value={values[p.id] ?? ''}
                  onChange={(e) => setValues({ ...values, [p.id]: e.target.value })}
                >
                  <option value="">Select…</option>
                  {(p.dropdownOptions ?? '').split(',').map((opt) => (
                    <option key={opt} value={opt.trim()}>
                      {opt.trim()}
                    </option>
                  ))}
                </select>
              ) : (
                <input
                  type={p.valueType === 1 ? 'number' : 'text'}
                  step="any"
                  className="flex-1 rounded-md border border-slate-300 bg-panel-solid px-3 py-2 text-sm text-ink outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
                  value={values[p.id] ?? ''}
                  onChange={(e) => setValues({ ...values, [p.id]: e.target.value })}
                />
              )}
            </div>
          ))}
          <label className="block text-sm font-medium text-slate-700">
            Notes
            <textarea
              className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
              rows={2}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            />
          </label>
        </div>
      )}
    </Modal>
  )
}

function ReviewResultModal({
  item,
  mode,
  onClose,
  onError,
}: {
  item: DepartmentWorklistItem
  mode: 'verify' | 'approve'
  onClose: () => void
  onError: (msg: string) => void
}) {
  const { data: result, isLoading } = useLabResult(item.labOrderItemId)
  const verify = useVerifyResult()
  const approve = useApproveResult()
  const pending = mode === 'verify' ? verify.isPending : approve.isPending

  const submit = async () => {
    try {
      if (mode === 'verify') await verify.mutateAsync(item.labOrderItemId)
      else await approve.mutateAsync(item.labOrderItemId)
      onClose()
    } catch (err) {
      onError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`${mode === 'verify' ? 'Verify' : 'Approve'} — ${item.labTestName}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={pending}>
            {mode === 'verify' ? 'Verify' : 'Approve'}
          </Button>
        </>
      }
    >
      {isLoading && <Spinner />}
      {result && (
        <div className="space-y-2">
          <p className="text-sm text-slate-600">
            {item.patientName} ({item.patientUhid}) — {item.patientAgeDisplay} / {item.patientGender}
          </p>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-left">
                <th className="py-1.5 font-medium text-slate-600">Parameter</th>
                <th className="py-1.5 text-right font-medium text-slate-600">Value</th>
                <th className="py-1.5 font-medium text-slate-600">Range</th>
              </tr>
            </thead>
            <tbody>
              {result.values.map((v) => (
                <tr key={v.id} className="border-b border-slate-100 last:border-0">
                  <td className="py-1.5 text-slate-700">{v.parameterName}</td>
                  <td
                    className={`py-1.5 text-right font-medium ${
                      v.flag === 4 ? 'text-red-600' : v.flag === 1 ? 'text-slate-700' : 'text-amber-600'
                    }`}
                  >
                    {v.value} {v.unit}
                  </td>
                  <td className="py-1.5 text-slate-500">
                    {v.normalLow ?? '—'}–{v.normalHigh ?? '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {result.hasCriticalValue && (
            <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">
              This result has a critical value — a critical alert has been raised.
            </p>
          )}
          {result.notes && <p className="text-sm text-slate-600">Notes: {result.notes}</p>}
        </div>
      )}
    </Modal>
  )
}
