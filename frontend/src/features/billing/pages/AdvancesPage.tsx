import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { DateRangeFilter, defaultDateRange } from '../../../components/ui/DateRangeFilter'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { PatientPicker } from '../../appointments/components/PatientPicker'
import { useAuth } from '../../auth/AuthContext'
import type { PatientListItem } from '../../patients/types'
import {
  useAdvancesByPatient,
  useCollectAdvance,
  useOpBillingSearch,
  usePaymentModes,
  useRequestAdvanceRefund,
  useTransferAdvance,
  type OpBillingSearchQuery,
} from '../api'
import { BILLING_GENDER_LABELS, type Advance, type OpBillingSearchResult, type RefundMethod } from '../types'

const emptyFilters = { patientName: '', ipNumber: '', packageOnly: false }

/**
 * The legacy "Advance Payment" search screen — an OP encounter search (Patient Name/IP No/
 * OP Multiple Visit Package, reusing OpBillingSearchQuery — the same search this page's
 * OpBillingSearchPage sibling already does) to pick which visit to collect an advance/deposit
 * against, not a plain "type a patient name" picker. Selecting a row opens the collect form
 * below (payment mode/amount/reference/notes) plus that patient's advance history.
 */
export function AdvancesPage() {
  const { hasPermission } = useAuth()
  const canCollect = hasPermission('BILLING.ADVANCE.COLLECT')
  const canRefund = hasPermission('BILLING.REFUND.REQUEST')
  const canTransfer = hasPermission('BILLING.ADVANCE.TRANSFER')

  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(0))
  const [filters, setFilters] = useState(emptyFilters)
  const [query, setQuery] = useState<OpBillingSearchQuery>({ page: 1, pageSize: 10, fromDate, toDate })
  const { data, isLoading } = useOpBillingSearch(query)
  const [selected, setSelected] = useState<OpBillingSearchResult | null>(null)

  const runSearch = () => {
    setSelected(null)
    setQuery({
      page: 1,
      pageSize: 10,
      fromDate,
      toDate,
      patientName: filters.patientName.trim() || undefined,
      ipNumber: filters.ipNumber.trim() || undefined,
      packageOnly: filters.packageOnly || undefined,
    })
  }

  const resetSearch = () => {
    setFilters(emptyFilters)
    setSelected(null)
    const defaults = defaultDateRange(0)
    setRange(defaults)
    setQuery({ page: 1, pageSize: 10, ...defaults })
  }

  const columns: Column<OpBillingSearchResult>[] = [
    { header: 'Encounter Date', cell: (row) => row.encounterDate },
    { header: 'Reg No', cell: (row) => <span className="font-mono text-xs">{row.regNo}</span> },
    { header: 'Patient Name', cell: (row) => row.patientName },
    { header: 'Age / Sex', cell: (row) => `${row.age} / ${BILLING_GENDER_LABELS[row.gender]}` },
    { header: 'Doctor / Dept', cell: (row) => [row.doctorName, row.specialtyName].filter(Boolean).join(' / ') || '—' },
    { header: 'Corporate Name', cell: (row) => row.corporateName ?? '—' },
    {
      header: '',
      className: 'text-right',
      cell: (row) => (
        <Button variant={selected?.opVisitId === row.opVisitId ? 'ghost' : 'primary'} onClick={() => setSelected(row)}>
          {selected?.opVisitId === row.opVisitId ? 'Selected' : 'Collect advance'}
        </Button>
      ),
    },
  ]

  return (
    <>
      <PageHeader title="Advance Payment" subtitle="Search an OP encounter to collect a patient advance/deposit against" />

      <div className="mb-4 rounded-shell border border-border-soft bg-panel p-4 shadow-shell-sm">
        <DateRangeFilter fromDate={fromDate} toDate={toDate} onFromDate={(v) => setRange((r) => ({ ...r, fromDate: v }))} onToDate={(v) => setRange((r) => ({ ...r, toDate: v }))} />
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
          <Input label="Patient Name" value={filters.patientName} onChange={(e) => setFilters((f) => ({ ...f, patientName: e.target.value }))} />
          <Input label="IP No" value={filters.ipNumber} onChange={(e) => setFilters((f) => ({ ...f, ipNumber: e.target.value }))} />
          <label className="flex items-end gap-1.5 pb-2 text-sm text-ink-muted">
            <input type="checkbox" checked={filters.packageOnly} onChange={(e) => setFilters((f) => ({ ...f, packageOnly: e.target.checked }))} />
            OP Multiple Visit Package
          </label>
        </div>
        <div className="mt-3 flex justify-end gap-2">
          <Button variant="secondary" onClick={resetSearch}>Reset</Button>
          <Button onClick={runSearch}>Search</Button>
        </div>
      </div>

      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={query.page ?? 1}
        onPageChange={(page) => setQuery((q) => ({ ...q, page }))}
        onPageSizeChange={(pageSize) => setQuery((q) => ({ ...q, page: 1, pageSize }))}
        rowKey={(row) => row.opVisitId}
        emptyMessage="No encounters found for these filters."
        enableColumnPicker
      />

      {selected && (
        <div className="mt-4">
          <CollectAdvancePanel
            canCollect={canCollect}
            canRefund={canRefund}
            canTransfer={canTransfer}
            patientId={selected.patientId}
            patientName={selected.patientName}
          />
        </div>
      )}
    </>
  )
}

function CollectAdvancePanel({
  canCollect, canRefund, canTransfer, patientId, patientName,
}: { canCollect: boolean; canRefund: boolean; canTransfer: boolean; patientId: number; patientName: string }) {
  const [paymentModeId, setPaymentModeId] = useState('')
  const [amount, setAmount] = useState('')
  const [reference, setReference] = useState('')
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)

  const { data: modes = [] } = usePaymentModes()
  const collect = useCollectAdvance()
  const { data: advances = [], isLoading } = useAdvancesByPatient(patientId)
  const [refunding, setRefunding] = useState<Advance | null>(null)
  const [transferring, setTransferring] = useState<Advance | null>(null)

  const selectedMode = modes.find((m) => m.id === Number(paymentModeId))

  const submit = async () => {
    if (!paymentModeId || !amount) return
    setError(null)
    try {
      await collect.mutateAsync({
        patientId,
        amount: Number(amount),
        paymentModeId: Number(paymentModeId),
        referenceNumber: reference || null,
        notes: notes || null,
      })
      setAmount('')
      setReference('')
      setNotes('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      {canCollect && (
        <div className="mb-6 max-w-2xl rounded-lg border border-slate-200 bg-white p-4">
          <h2 className="mb-3 text-sm font-semibold text-slate-800">Collect advance — {patientName}</h2>
          {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
          <div className="grid grid-cols-2 gap-3">
            <Select
              label="Payment mode"
              placeholder="Select…"
              value={paymentModeId}
              onChange={(e) => setPaymentModeId(e.target.value)}
              options={modes.filter((m) => m.isActive).map((m) => ({ value: m.id, label: m.name }))}
            />
            <Input label="Amount" type="number" min={0} step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} />
            {selectedMode?.requiresReference && (
              <Input label="Reference number" value={reference} onChange={(e) => setReference(e.target.value)} />
            )}
            <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
          </div>
          <div className="mt-3 flex justify-end">
            <Button onClick={submit} loading={collect.isPending} disabled={!paymentModeId || !amount}>
              Collect advance
            </Button>
          </div>
        </div>
      )}

      <div className="rounded-lg border border-slate-200 bg-white">
        <div className="border-b border-slate-200 px-4 py-3">
          <h2 className="text-sm font-semibold text-slate-800">Advances for {patientName}</h2>
        </div>
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Received</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Mode</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Amount</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Balance</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Notes</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {advances.map((a) => (
              <tr key={a.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{new Date(a.receivedAt).toLocaleString()}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.paymentModeName}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{a.amount.toFixed(2)}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{a.balanceAmount.toFixed(2)}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.notes ?? '—'}</td>
                <td className="px-4 py-2.5 text-right">
                  {a.balanceAmount > 0 && (canRefund || canTransfer) && (
                    <div className="flex justify-end gap-1">
                      {canRefund && (
                        <Button variant="secondary" onClick={() => setRefunding(a)}>
                          Refund
                        </Button>
                      )}
                      {canTransfer && (
                        <Button variant="secondary" onClick={() => setTransferring(a)}>
                          Transfer
                        </Button>
                      )}
                    </div>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && advances.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  No advances for this patient.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {refunding && <RequestAdvanceRefundModal advance={refunding} onClose={() => setRefunding(null)} />}
      {transferring && <TransferAdvanceModal advance={transferring} onClose={() => setTransferring(null)} />}
    </>
  )
}

function RequestAdvanceRefundModal({ advance, onClose }: { advance: Advance; onClose: () => void }) {
  const requestRefund = useRequestAdvanceRefund(advance.id)
  const [amount, setAmount] = useState('')
  const [reason, setReason] = useState('')
  const [method, setMethod] = useState<'1' | '2'>('1')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await requestRefund.mutateAsync({ amount: Number(amount), reason, method: Number(method) as RefundMethod })
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
        <Input
          label={`Amount (balance: ${advance.balanceAmount.toFixed(2)})`}
          type="number"
          min={0}
          max={advance.balanceAmount}
          step="0.01"
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
        />
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

function TransferAdvanceModal({ advance, onClose }: { advance: Advance; onClose: () => void }) {
  const transfer = useTransferAdvance(advance.id)
  const [target, setTarget] = useState<PatientListItem | null>(null)
  const [targetAdmissionId, setTargetAdmissionId] = useState('')
  const [amount, setAmount] = useState('')
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    if (!target) return
    setError(null)
    try {
      await transfer.mutateAsync({
        targetPatientId: target.id,
        targetAdmissionId: targetAdmissionId ? Number(targetAdmissionId) : null,
        amount: Number(amount),
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
      title="Transfer advance"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={transfer.isPending} disabled={!target || !amount}>
            Transfer
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <p className="text-xs text-slate-500">
          Moves part or all of this advance's unused balance ({advance.balanceAmount.toFixed(2)}) to a different patient
          and/or admission. Creates a new advance for the target; the original balance is reduced by the same amount.
        </p>
        <PatientPicker selected={target} onSelect={setTarget} />
        <Input
          label="Target admission ID (optional, for an IP deposit)"
          type="number"
          min={0}
          value={targetAdmissionId}
          onChange={(e) => setTargetAdmissionId(e.target.value)}
        />
        <Input
          label={`Amount (balance: ${advance.balanceAmount.toFixed(2)})`}
          type="number"
          min={0}
          max={advance.balanceAmount}
          step="0.01"
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
        />
        <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>
    </Modal>
  )
}
