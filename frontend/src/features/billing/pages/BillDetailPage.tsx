import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import {
  compactErrors,
  hasErrors,
  requiredPositiveAmount,
  requiredSelect,
  requiredText,
  type FieldErrors,
} from '../../../lib/formValidation'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useCorporates, useDiscountTypes, usePackages, useServices } from '../../masters/api'
import {
  openBillReceipt,
  useAddBillItem,
  useAddPendingOrders,
  useAddBillNote,
  useApplyBillDiscount,
  useApplyItemDiscount,
  useAdvancesByPatient,
  useBill,
  useBillNotes,
  useBills,
  useCancelBill,
  useCollectPayment,
  useDrawAdvanceOnBill,
  useMakeCreditBill,
  useParkBill,
  usePaymentModes,
  useRemoveBillItem,
  useRequestCreditNote,
  useRequestRefund,
  useUnparkBill,
} from '../api'
import { BILL_CLAIM_STATUS_LABELS, BILL_PRE_AUTH_STATUS_LABELS, BILL_STATUS_LABELS, type BillItem, type BillStatus } from '../types'

const statusTone: Record<BillStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info',
  2: 'warning',
  3: 'success',
  4: 'neutral',
  5: 'warning',
}

/** Standalone route (`/billing/bills/:id`) â€” used by direct links (Bills list, Advances, etc.). */
export function BillDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  return <BillWorkspace billId={Number(id)} backTo="/billing/bills" onCancelled={() => navigate('/billing/bills')} />
}

/**
 * The actual bill entry screen â€” extracted so OP Billing's multi-window tabs
 * (`OpBillingSearchPage`) can embed it directly without a route change per tab, matching
 * Aosta's "OP BILL PAYMENT I/II/III" tabs where each tab keeps its own bill session.
 * `onBack` (only passed from within a tab) shows a "Back" action that returns to that tab's
 * patient search, mirroring Aosta's own BACK button. `onOpenBill` lets "Previous Bills" switch
 * the current tab to a different bill instead of navigating away; standalone usage falls back
 * to a real route navigation via `onCancelled`/default `useNavigate`.
 */
export function BillWorkspace({
  billId,
  onBack,
  backTo,
  onCancelled,
  onOpenBill,
}: {
  billId: number
  onBack?: () => void
  /** Standalone-route usage's equivalent of `onBack` â€” a Back button in the PageHeader that
   * navigates to this route, since a tab has no history to go back through (see PageHeader). */
  backTo?: string
  onCancelled?: () => void
  onOpenBill?: (billId: number) => void
}) {
  const navigate = useNavigate()
  const { hasPermission } = useAuth()
  const { data: bill, isLoading, isError, error: loadError } = useBill(billId)

  const [addingItem, setAddingItem] = useState(false)
  const [discountingItem, setDiscountingItem] = useState<BillItem | null>(null)
  const [discountingBill, setDiscountingBill] = useState(false)
  const [collecting, setCollecting] = useState(false)
  const [cancelling, setCancelling] = useState(false)
  const [refunding, setRefunding] = useState(false)
  const [drawingAdvance, setDrawingAdvance] = useState(false)
  const [parking, setParking] = useState(false)
  const [requestingCreditNote, setRequestingCreditNote] = useState(false)
  const [makingCredit, setMakingCredit] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const removeItem = useRemoveBillItem(billId)
  const unpark = useUnparkBill(billId)
  const addPendingOrders = useAddPendingOrders(billId)

  const canManage = hasPermission('BILLING.BILL.MANAGE')
  const canCollect = hasPermission('BILLING.PAYMENT.COLLECT')
  const canCancel = hasPermission('BILLING.BILL.CANCEL')
  const canRefund = hasPermission('BILLING.REFUND.REQUEST')
  const canDrawAdvance = hasPermission('BILLING.ADVANCE.DRAW')
  const canPark = hasPermission('BILLING.BILL.PARK')
  const canUnpark = hasPermission('BILLING.BILL.PARKAPPROVE')
  const canRequestCreditNote = hasPermission('BILLING.CREDITNOTE.CREATE')

  if (isError) {
    return <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{errorMessage(loadError)}</p>
  }
  if (isLoading || !bill) {
    return <Spinner />
  }

  const editable = bill.status === 1 || bill.status === 2

  return (
    <>
      <PageHeader
        title={bill.billNumber}
        subtitle={`${bill.patientName} (${bill.patientUhid}) Â· ${bill.billDate}`}
        backTo={backTo}
        actions={
          <div className="flex flex-wrap items-center gap-2">
            {onBack && (
              <Button variant="secondary" onClick={onBack}>
                Back
              </Button>
            )}
            <StatusBadge tone={statusTone[bill.status]}>{BILL_STATUS_LABELS[bill.status]}</StatusBadge>
            <Button variant="secondary" onClick={() => openBillReceipt(billId)}>
              Print receipt
            </Button>
            {canManage && !bill.isCreditBill && bill.status === 1 && (
              <Button variant="secondary" onClick={() => setMakingCredit(true)}>
                Mark as credit
              </Button>
            )}
            {canCollect && bill.balanceAmount > 0 && bill.status !== 4 && bill.status !== 5 && (
              <Button onClick={() => setCollecting(true)}>Collect payment</Button>
            )}
            {canDrawAdvance && bill.balanceAmount > 0 && bill.status !== 4 && bill.status !== 5 && (
              <Button variant="secondary" onClick={() => setDrawingAdvance(true)}>
                Draw advance
              </Button>
            )}
            {canRefund && bill.paidAmount > 0 && (
              <Button variant="secondary" onClick={() => setRefunding(true)}>
                Request refund
              </Button>
            )}
            {canRequestCreditNote && (bill.status === 2 || bill.status === 3) && (
              <Button variant="secondary" onClick={() => setRequestingCreditNote(true)}>
                Request credit note
              </Button>
            )}
            {canPark && (bill.status === 1 || bill.status === 2) && (
              <Button variant="secondary" onClick={() => setParking(true)}>
                Park
              </Button>
            )}
            {canUnpark && bill.status === 5 && (
              <Button
                variant="secondary"
                onClick={async () => {
                  try {
                    await unpark.mutateAsync(undefined)
                  } catch (err) {
                    window.alert(errorMessage(err))
                  }
                }}
              >
                Unpark
              </Button>
            )}
            {canCancel && bill.status === 1 && (
              <Button variant="danger" onClick={() => setCancelling(true)}>
                Cancel
              </Button>
            )}
          </div>
        }
      />

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      {bill.creditLimit != null && (
        <p
          className={`mb-4 rounded-md px-3 py-2 text-sm ${
            (bill.creditExposure ?? 0) > bill.creditLimit
              ? 'bg-red-50 text-red-700'
              : 'bg-amber-50 text-amber-700'
          }`}
        >
          IP credit limit {bill.creditLimit.toFixed(2)} Â· current exposure {(bill.creditExposure ?? 0).toFixed(2)}
          {(bill.creditExposure ?? 0) > bill.creditLimit ? ' â€” exceeds the configured limit.' : ''}
        </p>
      )}

      {bill.insuranceStatus && (
        <p className="mb-4 rounded-md bg-amber-50 px-3 py-2 text-sm text-amber-700">
          {bill.insuranceStatus.claimId != null ? (
            <>
              Insurance claim {bill.insuranceStatus.claimNumber} is {BILL_CLAIM_STATUS_LABELS[bill.insuranceStatus.claimStatus!]} for
              this bill â€” confirm with the patient before collecting cash, so the bill isn't settled twice.
            </>
          ) : (
            <>
              Insurance pre-authorization {bill.insuranceStatus.preAuthNumber} is{' '}
              {BILL_PRE_AUTH_STATUS_LABELS[bill.insuranceStatus.preAuthStatus!]}
              {bill.insuranceStatus.preAuthApprovedAmount != null ? ` (approved ${bill.insuranceStatus.preAuthApprovedAmount.toFixed(2)})` : ''} for
              this encounter â€” confirm whether this should be billed cashless before collecting cash.
            </>
          )}
        </p>
      )}

      <div className="mb-6 grid grid-cols-2 gap-4 rounded-lg border border-slate-200 bg-white p-4 sm:grid-cols-5">
        <Summary label="Sub Total" value={bill.subTotal} />
        <Summary label="Discount" value={bill.discountAmount} />
        <Summary label="Tax" value={bill.taxAmount} />
        <Summary label="Total" value={bill.totalAmount} emphasize />
        <Summary label="Balance" value={bill.balanceAmount} emphasize={bill.balanceAmount > 0} />
      </div>

      <div className="mb-6 rounded-lg border border-slate-200 bg-white">
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
          <h2 className="text-sm font-semibold text-slate-800">Line Items</h2>
          {canManage && bill.status !== 4 && bill.status !== 5 && (
            <div className="flex gap-2">
              <Button
                variant="secondary"
                loading={addPendingOrders.isPending}
                onClick={async () => {
                  setError(null)
                  try {
                    await addPendingOrders.mutateAsync(undefined)
                  } catch (err) {
                    setError(errorMessage(err))
                  }
                }}
              >
                Add pending lab/radiology orders
              </Button>
            </div>
          )}
          {canManage && editable && (
            <div className="flex gap-2">
              <Button variant="secondary" onClick={() => setDiscountingBill(true)}>
                Bill-level discount
              </Button>
              <Button variant="secondary" onClick={() => setAddingItem(true)}>
                Add item
              </Button>
            </div>
          )}
        </div>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Description</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Qty</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Rate</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Disc.</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Tax</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Total</th>
              {canManage && editable && <th />}
            </tr>
          </thead>
          <tbody>
            {bill.items.map((item) => (
              <tr key={item.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{item.description}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{item.quantity}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{item.rate.toFixed(2)}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">
                  {item.discountAmount > 0 ? `${item.discountPercent}% (${item.discountAmount.toFixed(2)})` : 'â€”'}
                </td>
                <td className="px-4 py-2.5 text-right text-slate-700">{item.taxAmount.toFixed(2)}</td>
                <td className="px-4 py-2.5 text-right font-medium text-slate-800">{item.totalAmount.toFixed(2)}</td>
                {canManage && editable && (
                  <td className="px-4 py-2.5 text-right">
                    <div className="flex justify-end gap-1">
                      <Button variant="ghost" onClick={() => setDiscountingItem(item)}>
                        Discount
                      </Button>
                      {item.sourceType !== 1 && (
                        <Button
                          variant="ghost"
                          className="text-red-600"
                          onClick={async () => {
                            if (window.confirm('Remove this line item?')) {
                              try {
                                await removeItem.mutateAsync(item.id)
                              } catch (err) {
                                setError(errorMessage(err))
                              }
                            }
                          }}
                        >
                          Remove
                        </Button>
                      )}
                    </div>
                  </td>
                )}
              </tr>
            ))}
            {bill.items.length === 0 && (
              <tr>
                <td colSpan={7} className="px-4 py-8 text-center text-slate-400">
                  No line items.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        <div className="border-b border-slate-200 px-4 py-3">
          <h2 className="text-sm font-semibold text-slate-800">Payments</h2>
        </div>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Receipt No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Mode</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Reference</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Amount</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Paid At</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {bill.payments.map((payment) => (
              <tr key={payment.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{payment.receiptNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{payment.paymentModeName}</td>
                <td className="px-4 py-2.5 text-slate-700">{payment.referenceNumber ?? 'â€”'}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{payment.amount.toFixed(2)}</td>
                <td className="px-4 py-2.5 text-slate-700">{new Date(payment.paidAt).toLocaleString()}</td>
                <td className="px-4 py-2.5 text-right">
                  <Button variant="ghost" onClick={() => openBillReceipt(billId, payment.id)}>
                    Receipt
                  </Button>
                </td>
              </tr>
            ))}
            {bill.payments.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-slate-400">
                  No payments yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <BillNotesSection billId={billId} canManage={canManage || canCollect} />

      <PreviousBillsSection patientId={bill.patientId} currentBillId={billId} onOpenBill={onOpenBill} />

      {addingItem && <AddItemModal billId={billId} onClose={() => setAddingItem(false)} />}
      {discountingItem && (
        <ItemDiscountModal billId={billId} item={discountingItem} onClose={() => setDiscountingItem(null)} />
      )}
      {discountingBill && <BillDiscountModal billId={billId} onClose={() => setDiscountingBill(false)} />}
      {collecting && (
        <CollectPaymentModal billId={billId} balance={bill.balanceAmount} onClose={() => setCollecting(false)} />
      )}
      {cancelling && (
        <CancelBillModal
          billId={billId}
          onClose={() => setCancelling(false)}
          onDone={() => (onCancelled ? onCancelled() : navigate('/billing/bills'))}
        />
      )}
      {refunding && (
        <RequestRefundModal billId={billId} maxAmount={bill.paidAmount} payments={bill.payments} onClose={() => setRefunding(false)} />
      )}
      {drawingAdvance && (
        <DrawAdvanceModal
          billId={billId}
          patientId={bill.patientId}
          balance={bill.balanceAmount}
          onClose={() => setDrawingAdvance(false)}
        />
      )}
      {parking && <ParkBillModal billId={billId} onClose={() => setParking(false)} />}
      {requestingCreditNote && (
        <RequestCreditNoteModal billId={billId} items={bill.items} onClose={() => setRequestingCreditNote(false)} />
      )}
      {makingCredit && <MakeCreditBillModal billId={billId} onClose={() => setMakingCredit(false)} />}
    </>
  )
}

function BillNotesSection({ billId, canManage }: { billId: number; canManage: boolean }) {
  const { data: notes = [] } = useBillNotes(billId)
  const addNote = useAddBillNote(billId)
  const [note, setNote] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    if (!note.trim()) return
    setError(null)
    try {
      await addNote.mutateAsync({ note })
      setNote('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="mt-6 rounded-lg border border-slate-200 bg-white">
      <div className="border-b border-slate-200 px-4 py-3">
        <h2 className="text-sm font-semibold text-slate-800">Notes</h2>
      </div>
      <div className="space-y-2 px-4 py-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        {canManage && (
          <div className="flex gap-2">
            <Input value={note} onChange={(e) => setNote(e.target.value)} placeholder="Add a noteâ€¦" className="flex-1" />
            <Button onClick={submit} loading={addNote.isPending} disabled={!note.trim()}>
              Add
            </Button>
          </div>
        )}
        {notes.length === 0 && <p className="text-sm text-slate-400">No notes yet.</p>}
        {notes.map((n) => (
          <div key={n.id} className="rounded-md bg-slate-50 px-3 py-2 text-sm">
            <p className="text-slate-700">{n.note}</p>
            <p className="mt-0.5 text-xs text-slate-400">{new Date(n.createdAt).toLocaleString()}</p>
          </div>
        ))}
      </div>
    </div>
  )
}

/** Covers the legacy "Previous Outstanding" and "Previous Bill History" tabs in one panel â€”
 * both are just filtered views of the same patient's other bills, so this reuses useBills
 * (already patient-scoped) rather than a second backend query. */
function PreviousBillsSection({
  patientId,
  currentBillId,
  onOpenBill,
}: {
  patientId: number
  currentBillId: number
  onOpenBill?: (billId: number) => void
}) {
  const navigate = useNavigate()
  const [outstandingOnly, setOutstandingOnly] = useState(false)
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const { data } = useBills({ patientId, pageSize: 50, fromDate: fromDate || undefined, toDate: toDate || undefined })
  const rows = (data?.items ?? []).filter((b) => b.id !== currentBillId && (!outstandingOnly || b.balanceAmount > 0))

  return (
    <div className="mt-6 rounded-lg border border-slate-200 bg-white">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-200 px-4 py-3">
        <h2 className="text-sm font-semibold text-slate-800">Previous Bills</h2>
        <div className="flex flex-wrap items-center gap-3 text-sm">
          <label className="flex items-center gap-1.5 text-slate-600">
            <input type="checkbox" checked={outstandingOnly} onChange={(e) => setOutstandingOnly(e.target.checked)} />
            Outstanding only
          </label>
          <input
            type="date"
            value={fromDate}
            onChange={(e) => setFromDate(e.target.value)}
            className="rounded-md border border-slate-200 px-2 py-1 text-xs"
          />
          <span className="text-slate-400">to</span>
          <input
            type="date"
            value={toDate}
            onChange={(e) => setToDate(e.target.value)}
            className="rounded-md border border-slate-200 px-2 py-1 text-xs"
          />
        </div>
      </div>
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50 text-left">
            <th className="px-4 py-2.5 font-medium text-slate-600">Bill No</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Date</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Total</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Paid</th>
            <th className="px-4 py-2.5 text-right font-medium text-slate-600">Balance</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {rows.map((b) => (
            <tr key={b.id} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{b.billNumber}</td>
              <td className="px-4 py-2.5 text-slate-700">{b.billDate}</td>
              <td className="px-4 py-2.5">
                <StatusBadge tone={statusTone[b.status]}>{BILL_STATUS_LABELS[b.status]}</StatusBadge>
              </td>
              <td className="px-4 py-2.5 text-right text-slate-700">{b.totalAmount.toFixed(2)}</td>
              <td className="px-4 py-2.5 text-right text-slate-700">{b.paidAmount.toFixed(2)}</td>
              <td className="px-4 py-2.5 text-right font-medium text-slate-800">{b.balanceAmount.toFixed(2)}</td>
              <td className="px-4 py-2.5 text-right">
                <Button variant="ghost" onClick={() => (onOpenBill ? onOpenBill(b.id) : navigate(`/billing/bills/${b.id}`))}>
                  Open
                </Button>
              </td>
            </tr>
          ))}
          {rows.length === 0 && (
            <tr>
              <td colSpan={7} className="px-4 py-8 text-center text-slate-400">
                No previous bills{outstandingOnly ? ' outstanding' : ''}.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  )
}

function DrawAdvanceModal({
  billId,
  patientId,
  balance,
  onClose,
}: {
  billId: number
  patientId: number
  balance: number
  onClose: () => void
}) {
  const draw = useDrawAdvanceOnBill(billId)
  const { data: advances = [] } = useAdvancesByPatient(patientId)
  const available = advances.filter((a) => a.balanceAmount > 0)
  const [advanceId, setAdvanceId] = useState('')
  const [amount, setAmount] = useState('')
  const [error, setError] = useState<string | null>(null)

  const selected = available.find((a) => a.id === Number(advanceId))
  const maxAmount = Math.min(balance, selected?.balanceAmount ?? 0)

  const submit = async () => {
    setError(null)
    try {
      await draw.mutateAsync({ advanceId: Number(advanceId), amount: Number(amount) })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Draw advance against this bill"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={draw.isPending} disabled={!advanceId || !amount}>
            Draw
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        {available.length === 0 && <p className="text-sm text-slate-500">This patient has no available advance balance.</p>}
        <Select
          label="Advance"
          placeholder="Selectâ€¦"
          value={advanceId}
          onChange={(e) => setAdvanceId(e.target.value)}
          options={available.map((a) => ({ value: a.id, label: `Balance ${a.balanceAmount.toFixed(2)} (received ${new Date(a.receivedAt).toLocaleDateString()})` }))}
        />
        <Input
          label={`Amount (max ${maxAmount.toFixed(2)})`}
          type="number"
          min={0}
          max={maxAmount}
          step="0.01"
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
        />
      </div>
    </Modal>
  )
}

function ParkBillModal({ billId, onClose }: { billId: number; onClose: () => void }) {
  const park = useParkBill(billId)
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await park.mutateAsync({ reason })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Park bill"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={park.isPending} disabled={!reason.trim()}>
            Park
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
    </Modal>
  )
}

/**
 * `MakeCreditBillAsync` accepts an optional Corporate to attribute the credit bill to â€” the
 * backend fully supports it (`Bill.CorporateId`), but this used to be the only way to set it and
 * the button called it with a hardcoded `corporateId: null`, so a bill marked credit through the
 * UI could never carry a corporate. That silently broke the "Corporate Service Entry" legacy
 * mapping (this doc's Permissions table: "bill corporate/credit marking") and made the bill
 * permanently invisible to Finance's Receivables worklist, which requires a non-null
 * `CorporateId` (`ReceivableService.GetPendingBillsAsync`) â€” see the dated fix note in
 * docs/modules/billing.md. Corporate stays optional (a plain personal credit bill is still valid),
 * but it's now a real choice instead of a silent no-op.
 */
function MakeCreditBillModal({ billId, onClose }: { billId: number; onClose: () => void }) {
  const makeCredit = useMakeCreditBill(billId)
  const { data: corporates = [] } = useCorporates()
  const [corporateId, setCorporateId] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await makeCredit.mutateAsync({ corporateId: corporateId ? Number(corporateId) : null })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Mark as credit bill"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={makeCredit.isPending}>
            Mark as credit
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <Select
        label="Corporate (optional)"
        placeholder="â€” None (personal credit) â€”"
        value={corporateId}
        onChange={(e) => setCorporateId(e.target.value)}
        options={corporates.map((c) => ({ value: c.id, label: c.name }))}
      />
      <p className="mt-2 text-xs text-slate-500">
        Attributing this bill to a corporate lets Finance raise it as a Receivable once billed.
      </p>
    </Modal>
  )
}

function RequestCreditNoteModal({
  billId,
  items,
  onClose,
}: {
  billId: number
  items: BillItem[]
  onClose: () => void
}) {
  const requestCreditNote = useRequestCreditNote(billId)
  const [amounts, setAmounts] = useState<Record<number, string>>({})
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const lines = Object.entries(amounts)
    .map(([billItemId, amount]) => ({ billItemId: Number(billItemId), amount: Number(amount) }))
    .filter((l) => l.amount > 0)

  const submit = async () => {
    setError(null)
    try {
      await requestCreditNote.mutateAsync({ reason, items: lines })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Request credit note"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={requestCreditNote.isPending} disabled={!reason.trim() || lines.length === 0}>
            Request
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <p className="text-xs text-slate-500">Enter the amount to reverse for each line that needs correcting.</p>
        <div className="space-y-2">
          {items.map((item) => (
            <div key={item.id} className="flex items-center gap-2">
              <span className="flex-1 text-sm text-slate-700">
                {item.description} <span className="text-slate-400">(billed {item.totalAmount.toFixed(2)})</span>
              </span>
              <Input
                type="number"
                min={0}
                max={item.totalAmount}
                step="0.01"
                className="w-32"
                value={amounts[item.id] ?? ''}
                onChange={(e) => setAmounts((prev) => ({ ...prev, [item.id]: e.target.value }))}
              />
            </div>
          ))}
        </div>
        <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
      </div>
    </Modal>
  )
}

function Summary({ label, value, emphasize }: { label: string; value: number; emphasize?: boolean }) {
  return (
    <div>
      <p className="text-xs text-slate-500">{label}</p>
      <p className={`mt-0.5 text-lg ${emphasize ? 'font-semibold text-slate-900' : 'text-slate-700'}`}>
        {value.toFixed(2)}
      </p>
    </div>
  )
}

function AddItemModal({ billId, onClose }: { billId: number; onClose: () => void }) {
  const addItem = useAddBillItem(billId)
  const { data: servicesResult } = useServices({ page: 1, pageSize: 500 })
  const { data: packages = [] } = usePackages()
  const [sourceType, setSourceType] = useState<'3' | '4'>('3')
  const [refId, setRefId] = useState('')
  const [quantity, setQuantity] = useState('1')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    if (!refId) return
    setError(null)
    try {
      await addItem.mutateAsync({
        sourceType: sourceType === '3' ? 3 : 4,
        serviceId: sourceType === '3' ? Number(refId) : null,
        packageId: sourceType === '4' ? Number(refId) : null,
        quantity: Number(quantity),
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Add line item"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={addItem.isPending} disabled={!refId}>
            Add
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
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
            placeholder="Select a serviceâ€¦"
            value={refId}
            onChange={(e) => setRefId(e.target.value)}
            options={(servicesResult?.items ?? []).map((s) => ({ value: s.id, label: s.name }))}
          />
        ) : (
          <Select
            label="Package"
            placeholder="Select a packageâ€¦"
            value={refId}
            onChange={(e) => setRefId(e.target.value)}
            options={packages.map((p) => ({ value: p.id, label: p.name }))}
          />
        )}
        <Input label="Quantity" type="number" min={1} step="1" value={quantity} onChange={(e) => setQuantity(e.target.value)} />
      </div>
    </Modal>
  )
}

function DiscountFields({
  discountTypeId,
  setDiscountTypeId,
  percent,
  setPercent,
  reason,
  setReason,
}: {
  discountTypeId: string
  setDiscountTypeId: (v: string) => void
  percent: string
  setPercent: (v: string) => void
  reason: string
  setReason: (v: string) => void
}) {
  const { data: discountTypes = [] } = useDiscountTypes()
  return (
    <div className="space-y-3">
      <Select
        label="Discount type (optional)"
        placeholder="Ad-hoc discountâ€¦"
        value={discountTypeId}
        onChange={(e) => setDiscountTypeId(e.target.value)}
        options={discountTypes.filter((t) => t.isActive).map((t) => ({ value: t.id, label: `${t.name} (max ${t.maxPercent}%)` }))}
      />
      <Input label="Percent" type="number" min={0} max={100} step="0.01" value={percent} onChange={(e) => setPercent(e.target.value)} />
      <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
      <p className="text-xs text-slate-500">
        Ad-hoc discounts (no type selected) always require approval. A discount type auto-applies up to its approval
        threshold; above that it also routes for approval.
      </p>
    </div>
  )
}

function ItemDiscountModal({ billId, item, onClose }: { billId: number; item: BillItem; onClose: () => void }) {
  const applyDiscount = useApplyItemDiscount(billId)
  const [discountTypeId, setDiscountTypeId] = useState('')
  const [percent, setPercent] = useState(String(item.discountPercent || ''))
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      const res = await applyDiscount.mutateAsync({
        itemId: item.id,
        discountTypeId: discountTypeId ? Number(discountTypeId) : null,
        percent: Number(percent || 0),
        reason: reason || null,
      })
      setResult(res.data?.data ? 'Discount routed for approval.' : 'Discount applied.')
      setTimeout(onClose, 900)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Discount â€” ${item.description}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={applyDiscount.isPending}>
            Apply
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      {result && <p className="mb-3 rounded-md bg-emerald-50 px-3 py-2 text-sm text-emerald-700">{result}</p>}
      <DiscountFields
        discountTypeId={discountTypeId}
        setDiscountTypeId={setDiscountTypeId}
        percent={percent}
        setPercent={setPercent}
        reason={reason}
        setReason={setReason}
      />
    </Modal>
  )
}

function BillDiscountModal({ billId, onClose }: { billId: number; onClose: () => void }) {
  const applyDiscount = useApplyBillDiscount(billId)
  const [discountTypeId, setDiscountTypeId] = useState('')
  const [percent, setPercent] = useState('')
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      const res = await applyDiscount.mutateAsync({
        discountTypeId: discountTypeId ? Number(discountTypeId) : null,
        percent: Number(percent || 0),
        reason: reason || null,
      })
      setResult(res.data?.data ? 'Discount routed for approval.' : 'Discount applied.')
      setTimeout(onClose, 900)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Bill-level discount"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={applyDiscount.isPending}>
            Apply
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      {result && <p className="mb-3 rounded-md bg-emerald-50 px-3 py-2 text-sm text-emerald-700">{result}</p>}
      <DiscountFields
        discountTypeId={discountTypeId}
        setDiscountTypeId={setDiscountTypeId}
        percent={percent}
        setPercent={setPercent}
        reason={reason}
        setReason={setReason}
      />
    </Modal>
  )
}

function CollectPaymentModal({ billId, balance, onClose }: { billId: number; balance: number; onClose: () => void }) {
  const collect = useCollectPayment(billId)
  const { data: modes = [] } = usePaymentModes()
  const [paymentModeId, setPaymentModeId] = useState('')
  const [amount, setAmount] = useState(String(balance))
  const [reference, setReference] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const selectedMode = modes.find((m) => m.id === Number(paymentModeId))

  /**
   * Mirrors CollectPaymentRequestValidator plus the two rules BillingService enforces on top
   * of it: a payment cannot exceed the outstanding balance, and a mode flagged
   * RequiresReference needs a reference number.
   */
  const validate = (): FieldErrors =>
    compactErrors({
      paymentModeId: requiredSelect(paymentModeId, 'Payment mode'),
      amount:
        requiredPositiveAmount(amount, 'Amount') ??
        (Number(amount) > balance ? 'Amount exceeds the outstanding balance.' : undefined),
      reference: selectedMode?.requiresReference ? requiredText(reference, 'Reference number') : undefined,
    })

  const submit = async () => {
    setError(null)
    const errors = validate()
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await collect.mutateAsync({
        paymentModeId: Number(paymentModeId),
        amount: Number(amount),
        referenceNumber: reference || null,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Collect payment"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={collect.isPending}>
            Collect
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Select
          label="Payment mode"
          required
          error={fieldErrors.paymentModeId}
          placeholder="Selectâ€¦"
          value={paymentModeId}
          onChange={(e) => setPaymentModeId(e.target.value)}
          options={modes.filter((m) => m.isActive).map((m) => ({ value: m.id, label: m.name }))}
        />
        <Input
          label={`Amount (balance: ${balance.toFixed(2)})`}
          type="number"
          required
          error={fieldErrors.amount}
          min={0}
          max={balance}
          step="0.01"
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
        />
        {selectedMode?.requiresReference && (
          <Input
            label="Reference number"
            required
            error={fieldErrors.reference}
            value={reference}
            onChange={(e) => setReference(e.target.value)}
          />
        )}
      </div>
    </Modal>
  )
}

function CancelBillModal({ billId, onClose, onDone }: { billId: number; onClose: () => void; onDone: () => void }) {
  const cancel = useCancelBill(billId)
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await cancel.mutateAsync({ reason })
      onDone()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Cancel bill"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button variant="danger" onClick={submit} loading={cancel.isPending} disabled={!reason.trim()}>
            Cancel bill
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
    </Modal>
  )
}

function RequestRefundModal({
  billId,
  maxAmount,
  payments,
  onClose,
}: {
  billId: number
  maxAmount: number
  payments: { id: number; receiptNumber: string; amount: number }[]
  onClose: () => void
}) {
  const requestRefund = useRequestRefund(billId)
  const [billPaymentId, setBillPaymentId] = useState('')
  const [amount, setAmount] = useState('')
  const [reason, setReason] = useState('')
  const [method, setMethod] = useState<'1' | '2'>('1')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await requestRefund.mutateAsync({
        billPaymentId: billPaymentId ? Number(billPaymentId) : null,
        amount: Number(amount),
        reason,
        method: method === '1' ? 1 : 2,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Request refund"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={requestRefund.isPending} disabled={!amount || !reason.trim()}>
            Request
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Select
          label="Against payment (optional)"
          placeholder="Any payment on this billâ€¦"
          value={billPaymentId}
          onChange={(e) => setBillPaymentId(e.target.value)}
          options={payments.map((p) => ({ value: p.id, label: `${p.receiptNumber} (${p.amount.toFixed(2)})` }))}
        />
        <Input label={`Amount (paid total: ${maxAmount.toFixed(2)})`} type="number" min={0} max={maxAmount} step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} />
        <Select
          label="Method"
          value={method}
          onChange={(e) => setMethod(e.target.value as '1' | '2')}
          options={[
            { value: '1', label: 'Cash' },
            { value: '2', label: 'Credit Note' },
          ]}
        />
        <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
      </div>
    </Modal>
  )
}
