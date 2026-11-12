import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { endpoints } from '../../../api/endpoints'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { PatientPicker } from '../../appointments/components/PatientPicker'
import { useAuth } from '../../auth/AuthContext'
import { useBills, usePaymentModes } from '../../billing/api'
import { useSimpleMaster } from '../../masters/api'
import type { PatientListItem } from '../../patients/types'
import {
  usePatientInsurancePolicies,
  usePendingClaims,
  useSettleClaim,
  useSubmitClaim,
  useUpdateClaimStatus,
} from '../api'
import { CLAIM_STATUS_LABELS, CLAIM_STATUSES, type Claim, type ClaimStatus } from '../types'

const statusTone: Record<number, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'neutral',
  2: 'info',
  3: 'warning',
  4: 'warning',
  5: 'success',
  6: 'danger',
  7: 'success',
}

export function ClaimsWorklistPage() {
  const { hasPermission } = useAuth()
  const { data: claims = [], isLoading } = usePendingClaims()
  const [submitting, setSubmitting] = useState(false)
  const [updating, setUpdating] = useState<Claim | null>(null)
  const [settling, setSettling] = useState<Claim | null>(null)

  const canCreate = hasPermission('INSURANCE.CLAIM.CREATE')
  const canManage = hasPermission('INSURANCE.CLAIM.MANAGE')
  const canSettle = hasPermission('INSURANCE.CLAIM.SETTLE')

  return (
    <>
      <PageHeader title="Claims" subtitle="Insurance/TPA claims — submission through settlement" />

      {canCreate && (
        <div className="mb-4">
          <Button onClick={() => setSubmitting(true)}>Submit claim</Button>
        </div>
      )}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Claim No.</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Insurer</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Bill</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Claim Amt</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Bill Paid</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {claims.map((c) => (
              <tr key={c.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{c.claimNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">
                  {c.patientName} ({c.patientUhid})
                </td>
                <td className="px-4 py-2.5 text-slate-700">{c.insuranceCompanyName}</td>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{c.billNumber}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{c.claimAmount.toFixed(2)}</td>
                <td
                  className={`px-4 py-2.5 text-right ${
                    c.billPaidAmount > 0 && c.status !== CLAIM_STATUSES.Settled && c.status !== CLAIM_STATUSES.Denied
                      ? 'font-semibold text-amber-700'
                      : 'text-slate-700'
                  }`}
                  title={c.billPaidAmount > 0 ? 'Cash has already been collected against this bill — confirm with the patient before settling as cashless.' : undefined}
                >
                  {c.billPaidAmount.toFixed(2)}
                </td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={statusTone[c.status]}>{CLAIM_STATUS_LABELS[c.status]}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  <div className="flex justify-end gap-1">
                    {canManage && (c.status === CLAIM_STATUSES.Submitted || c.status === CLAIM_STATUSES.UnderReview) && (
                      <Button variant="secondary" onClick={() => setUpdating(c)}>
                        Update status
                      </Button>
                    )}
                    {canSettle && (c.status === CLAIM_STATUSES.Approved || c.status === CLAIM_STATUSES.PartiallyApproved) && (
                      <Button onClick={() => setSettling(c)}>Settle</Button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
            {!isLoading && claims.length === 0 && (
              <tr>
                <td colSpan={8} className="px-4 py-10 text-center text-slate-400">
                  Nothing pending.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {submitting && <SubmitClaimModal onClose={() => setSubmitting(false)} />}
      {updating && <UpdateStatusModal claim={updating} onClose={() => setUpdating(null)} />}
      {settling && <SettleModal claim={settling} onClose={() => setSettling(null)} />}
    </>
  )
}

function SubmitClaimModal({ onClose }: { onClose: () => void }) {
  const submitClaim = useSubmitClaim()
  const [patient, setPatient] = useState<PatientListItem | null>(null)
  const [policyId, setPolicyId] = useState('')
  const [billId, setBillId] = useState('')
  const [claimAmount, setClaimAmount] = useState('')
  const [error, setError] = useState<string | null>(null)

  const { data: policies = [] } = usePatientInsurancePolicies(patient?.id ?? null)
  const { data: billsResult } = useBills({ page: 1, pageSize: 20, patientId: patient?.id })

  const submit = async () => {
    if (!patient || !policyId || !billId || !claimAmount) {
      setError('Patient, policy, bill and claim amount are required.')
      return
    }
    setError(null)
    try {
      await submitClaim.mutateAsync({
        patientId: patient.id,
        policyId: Number(policyId),
        preAuthRequestId: null,
        billId: Number(billId),
        claimAmount: Number(claimAmount),
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Submit claim"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={submitClaim.isPending}>
            Submit
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <div className="space-y-3">
        <PatientPicker selected={patient} onSelect={setPatient} />
        <Select
          label="Policy"
          placeholder={patient ? '— select policy —' : 'Select a patient first'}
          options={policies.map((p) => ({ value: p.id, label: `${p.insuranceCompanyName} — ${p.policyNumber}` }))}
          value={policyId}
          onChange={(e) => setPolicyId(e.target.value)}
        />
        <Select
          label="Bill"
          placeholder={patient ? '— select bill —' : 'Select a patient first'}
          options={(billsResult?.items ?? []).map((b) => ({ value: b.id, label: `${b.billNumber} — ${b.totalAmount.toFixed(2)}` }))}
          value={billId}
          onChange={(e) => setBillId(e.target.value)}
        />
        <Input
          label="Claim amount"
          type="number"
          min={0}
          value={claimAmount}
          onChange={(e) => setClaimAmount(e.target.value)}
        />
      </div>
    </Modal>
  )
}

function UpdateStatusModal({ claim, onClose }: { claim: Claim; onClose: () => void }) {
  const updateStatus = useUpdateClaimStatus()
  const [status, setStatus] = useState<ClaimStatus>(
    claim.status === CLAIM_STATUSES.Submitted ? CLAIM_STATUSES.UnderReview : CLAIM_STATUSES.Approved,
  )
  const [approvedAmount, setApprovedAmount] = useState(String(claim.claimAmount))
  const [denialReasonId, setDenialReasonId] = useState('')
  const [denialNotes, setDenialNotes] = useState('')
  const [error, setError] = useState<string | null>(null)

  const { data: denialReasons = [] } = useSimpleMaster('claim-denial-reasons', endpoints.masters.claimDenialReasons)

  const nextOptions =
    claim.status === CLAIM_STATUSES.Submitted
      ? [
          { value: CLAIM_STATUSES.UnderReview, label: 'Under Review' },
          { value: CLAIM_STATUSES.Denied, label: 'Denied' },
        ]
      : [
          { value: CLAIM_STATUSES.PartiallyApproved, label: 'Partially Approved' },
          { value: CLAIM_STATUSES.Approved, label: 'Approved' },
          { value: CLAIM_STATUSES.Denied, label: 'Denied' },
        ]

  const submit = async () => {
    setError(null)
    try {
      await updateStatus.mutateAsync({
        id: claim.id,
        status,
        approvedAmount: status === CLAIM_STATUSES.Approved || status === CLAIM_STATUSES.PartiallyApproved
          ? Number(approvedAmount)
          : null,
        denialReasonId: status === CLAIM_STATUSES.Denied && denialReasonId ? Number(denialReasonId) : null,
        denialNotes: status === CLAIM_STATUSES.Denied ? denialNotes || null : null,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Update claim ${claim.claimNumber}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button onClick={submit} loading={updateStatus.isPending}>
            Save
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <div className="space-y-3">
        <Select
          label="New status"
          options={nextOptions.map((o) => ({ value: o.value, label: o.label }))}
          value={status}
          onChange={(e) => setStatus(Number(e.target.value) as ClaimStatus)}
        />
        {(status === CLAIM_STATUSES.Approved || status === CLAIM_STATUSES.PartiallyApproved) && (
          <Input
            label="Approved amount"
            type="number"
            min={0}
            value={approvedAmount}
            onChange={(e) => setApprovedAmount(e.target.value)}
          />
        )}
        {status === CLAIM_STATUSES.Denied && (
          <>
            <Select
              label="Denial reason"
              placeholder="—"
              options={denialReasons.map((r) => ({ value: r.id, label: r.name }))}
              value={denialReasonId}
              onChange={(e) => setDenialReasonId(e.target.value)}
            />
            <Input label="Notes" value={denialNotes} onChange={(e) => setDenialNotes(e.target.value)} />
          </>
        )}
      </div>
    </Modal>
  )
}

function SettleModal({ claim, onClose }: { claim: Claim; onClose: () => void }) {
  const settle = useSettleClaim()
  const [settledAmount, setSettledAmount] = useState(String(claim.approvedAmount ?? claim.claimAmount))
  const [deductionAmount, setDeductionAmount] = useState('')
  const [paymentModeId, setPaymentModeId] = useState('')
  const [referenceNumber, setReferenceNumber] = useState('')
  const [error, setError] = useState<string | null>(null)

  const { data: paymentModes = [] } = usePaymentModes()

  const submit = async () => {
    if (!settledAmount || !paymentModeId) {
      setError('Settled amount and payment mode are required.')
      return
    }
    setError(null)
    try {
      await settle.mutateAsync({
        id: claim.id,
        settledAmount: Number(settledAmount),
        deductionAmount: deductionAmount ? Number(deductionAmount) : null,
        paymentModeId: Number(paymentModeId),
        settlementReferenceNumber: referenceNumber || null,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Settle claim ${claim.claimNumber}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button onClick={submit} loading={settle.isPending}>
            Settle
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <div className="space-y-3">
        <Input
          label="Settled amount"
          type="number"
          min={0}
          value={settledAmount}
          onChange={(e) => setSettledAmount(e.target.value)}
        />
        <Input
          label="Deduction amount"
          type="number"
          min={0}
          value={deductionAmount}
          onChange={(e) => setDeductionAmount(e.target.value)}
        />
        <Select
          label="Payment mode"
          placeholder="—"
          options={paymentModes.map((m) => ({ value: m.id, label: m.name }))}
          value={paymentModeId}
          onChange={(e) => setPaymentModeId(e.target.value)}
        />
        <Input
          label="Settlement reference no."
          value={referenceNumber}
          onChange={(e) => setReferenceNumber(e.target.value)}
        />
      </div>
    </Modal>
  )
}
