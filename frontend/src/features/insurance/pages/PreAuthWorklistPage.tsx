import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { PatientPicker } from '../../appointments/components/PatientPicker'
import { useAuth } from '../../auth/AuthContext'
import { useAdmissions } from '../../ipd/api'
import { useOpVisits } from '../../opd/api'
import type { PatientListItem } from '../../patients/types'
import {
  useApprovedPreAuthRequests,
  useDecideEnhancement,
  useDecidePreAuth,
  usePatientInsurancePolicies,
  usePendingPreAuthRequests,
  usePreAuthEnhancements,
  useRaisePreAuth,
  useRequestEnhancement,
} from '../api'
import {
  ENHANCEMENT_STATUS_LABELS,
  ENHANCEMENT_STATUSES,
  PRE_AUTH_STATUS_LABELS,
  PRE_AUTH_STATUSES,
  TREATMENT_TYPES,
  TREATMENT_TYPE_LABELS,
  type PreAuthEnhancement,
  type PreAuthRequest,
  type TreatmentType,
} from '../types'

const statusTone: Record<number, 'success' | 'warning' | 'danger' | 'info'> = {
  1: 'warning',
  2: 'success',
  3: 'info',
  4: 'danger',
}

const enhancementStatusTone: Record<number, 'success' | 'warning' | 'danger'> = {
  1: 'warning',
  2: 'success',
  3: 'danger',
}

export function PreAuthWorklistPage() {
  const { hasPermission } = useAuth()
  const { data: requests = [], isLoading } = usePendingPreAuthRequests()
  const { data: approved = [], isLoading: approvedLoading } = useApprovedPreAuthRequests()
  const [raising, setRaising] = useState(false)
  const [deciding, setDeciding] = useState<{ request: PreAuthRequest; approve: boolean } | null>(null)
  const [enhancing, setEnhancing] = useState<PreAuthRequest | null>(null)

  const canCreate = hasPermission('INSURANCE.PREAUTH.CREATE')
  const canApprove = hasPermission('INSURANCE.PREAUTH.APPROVE')

  return (
    <>
      <PageHeader title="Pre-Authorization Requests" subtitle="Insurance/TPA pre-auth requests awaiting a decision" />

      {canCreate && (
        <div className="mb-4">
          <Button onClick={() => setRaising(true)}>Raise pre-authorization</Button>
        </div>
      )}

      <div className="mb-6 rounded-lg border border-slate-200 bg-white">
        <div className="border-b border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700">Pending Decision</div>
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Pre-Auth No.</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Insurer</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Estimated</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {requests.map((r) => (
              <tr key={r.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{r.preAuthNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">
                  {r.patientName} ({r.patientUhid})
                </td>
                <td className="px-4 py-2.5 text-slate-700">{r.insuranceCompanyName}</td>
                <td className="px-4 py-2.5 text-slate-700">{TREATMENT_TYPE_LABELS[r.treatmentType]}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.estimatedAmount.toFixed(2)}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={statusTone[r.status]}>{PRE_AUTH_STATUS_LABELS[r.status]}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canApprove && r.status === PRE_AUTH_STATUSES.Requested && (
                    <div className="flex justify-end gap-1">
                      <Button variant="secondary" onClick={() => setDeciding({ request: r, approve: true })}>
                        Approve
                      </Button>
                      <Button variant="danger" onClick={() => setDeciding({ request: r, approve: false })}>
                        Reject
                      </Button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && requests.length === 0 && (
              <tr>
                <td colSpan={7} className="px-4 py-10 text-center text-slate-400">
                  Nothing pending.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        <div className="border-b border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700">
          Approved — Enhancements
        </div>
        {approvedLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Pre-Auth No.</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Insurer</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Estimated</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Approved</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {approved.map((r) => (
              <tr key={r.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{r.preAuthNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">
                  {r.patientName} ({r.patientUhid})
                </td>
                <td className="px-4 py-2.5 text-slate-700">{r.insuranceCompanyName}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{r.estimatedAmount.toFixed(2)}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{(r.approvedAmount ?? 0).toFixed(2)}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={statusTone[r.status]}>{PRE_AUTH_STATUS_LABELS[r.status]}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {(canCreate || canApprove) && (
                    <Button variant="ghost" onClick={() => setEnhancing(r)}>
                      Enhancements
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {!approvedLoading && approved.length === 0 && (
              <tr>
                <td colSpan={7} className="px-4 py-10 text-center text-slate-400">
                  No approved pre-authorizations yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {raising && <RaisePreAuthModal onClose={() => setRaising(false)} />}
      {deciding && (
        <DecideModal request={deciding.request} approve={deciding.approve} onClose={() => setDeciding(null)} />
      )}
      {enhancing && (
        <EnhancementsModal
          request={enhancing}
          canCreate={canCreate}
          canApprove={canApprove}
          onClose={() => setEnhancing(null)}
        />
      )}
    </>
  )
}

function RaisePreAuthModal({ onClose }: { onClose: () => void }) {
  const raise = useRaisePreAuth()
  const [patient, setPatient] = useState<PatientListItem | null>(null)
  const [policyId, setPolicyId] = useState('')
  const [treatmentType, setTreatmentType] = useState<TreatmentType>(TREATMENT_TYPES.Op)
  // The encounter link (OpVisitId/AdmissionId) is what BillingService.GetInsuranceStatusAsync
  // matches against to surface the pre-auth banner on BillDetailPage — without it, that
  // already-shipped "Insurance awareness on the bill" feature (docs/modules/billing.md) can
  // never find this pre-auth for any bill. Optional (a pre-auth can genuinely predate the visit
  // being booked), but no longer unreachable.
  const [encounterId, setEncounterId] = useState('')
  const [estimatedAmount, setEstimatedAmount] = useState('')
  const [diagnosis, setDiagnosis] = useState('')
  const [error, setError] = useState<string | null>(null)

  const { data: policies = [] } = usePatientInsurancePolicies(patient?.id ?? null)
  // No `enabled` gate available on these shared hooks — pin to a patientId no real patient has
  // rather than firing an unfiltered fetch of every visit/admission while none is selected yet.
  const { data: opVisits } = useOpVisits({ patientId: patient?.id ?? -1, page: 1, pageSize: 20 })
  const { data: admissions } = useAdmissions({ patientId: patient?.id ?? -1, page: 1, pageSize: 20 })

  const submit = async () => {
    if (!patient || !policyId || !estimatedAmount || !diagnosis.trim()) {
      setError('Patient, policy, estimated amount and diagnosis are required.')
      return
    }
    setError(null)
    try {
      await raise.mutateAsync({
        patientId: patient.id,
        policyId: Number(policyId),
        treatmentType,
        opVisitId: treatmentType === TREATMENT_TYPES.Op && encounterId ? Number(encounterId) : null,
        admissionId: treatmentType === TREATMENT_TYPES.Ip && encounterId ? Number(encounterId) : null,
        estimatedAmount: Number(estimatedAmount),
        diagnosis: diagnosis.trim(),
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Raise pre-authorization"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={raise.isPending}>
            Raise
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
        <div className="grid grid-cols-2 gap-3">
          <Select
            label="Treatment type"
            options={Object.entries(TREATMENT_TYPE_LABELS).map(([value, label]) => ({ value, label }))}
            value={String(treatmentType)}
            onChange={(e) => {
              setTreatmentType(Number(e.target.value) as TreatmentType)
              setEncounterId('')
            }}
          />
          <Input
            label="Estimated amount"
            type="number"
            min={0}
            value={estimatedAmount}
            onChange={(e) => setEstimatedAmount(e.target.value)}
          />
        </div>
        <Select
          label={`${treatmentType === TREATMENT_TYPES.Op ? 'OP visit' : 'Admission'} (optional)`}
          placeholder={patient ? '— none yet / link later —' : 'Select a patient first'}
          value={encounterId}
          onChange={(e) => setEncounterId(e.target.value)}
          options={
            treatmentType === TREATMENT_TYPES.Op
              ? (opVisits?.items ?? []).map((v) => ({ value: v.id, label: `${v.visitNumber} — ${v.visitDate} (${v.doctorName})` }))
              : (admissions?.items ?? []).map((a) => ({ value: a.id, label: `${a.admissionNumber} — ${a.admissionDate} (${a.wardName})` }))
          }
        />
        <p className="text-xs text-slate-500">
          Linking the visit/admission now lets this pre-auth's status surface as a banner on that
          encounter's bill later (Billing's insurance-awareness banner).
        </p>
        <Input label="Diagnosis" value={diagnosis} onChange={(e) => setDiagnosis(e.target.value)} />
      </div>
    </Modal>
  )
}

function DecideModal({
  request,
  approve,
  onClose,
}: {
  request: PreAuthRequest
  approve: boolean
  onClose: () => void
}) {
  const decide = useDecidePreAuth()
  const [approvedAmount, setApprovedAmount] = useState(String(request.estimatedAmount))
  const [rejectionReason, setRejectionReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await decide.mutateAsync({
        id: request.id,
        approve,
        approvedAmount: approve ? Number(approvedAmount) : null,
        rejectionReason: approve ? null : rejectionReason,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={approve ? 'Approve pre-authorization' : 'Reject pre-authorization'}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button
            variant={approve ? 'primary' : 'danger'}
            onClick={submit}
            loading={decide.isPending}
            disabled={!approve && !rejectionReason.trim()}
          >
            {approve ? 'Approve' : 'Reject'}
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <p className="mb-3 text-sm text-slate-600">
        {request.preAuthNumber} · {request.patientName} · estimated {request.estimatedAmount.toFixed(2)}
      </p>
      {approve ? (
        <Input
          label="Approved amount"
          type="number"
          min={0}
          value={approvedAmount}
          onChange={(e) => setApprovedAmount(e.target.value)}
        />
      ) : (
        <Input label="Rejection reason" value={rejectionReason} onChange={(e) => setRejectionReason(e.target.value)} />
      )}
    </Modal>
  )
}

/**
 * Mid-treatment "enhancement" — a request to raise an already-approved pre-auth's amount, with
 * its own request→decide cycle (see docs/modules/insurance.md's Workflow step 3). Lists this
 * pre-auth's enhancement history, lets INSURANCE.PREAUTH.CREATE raise a new one (only when none
 * is currently Requested — same "no piling up pending requests" posture as Billing's discount/
 * refund flows), and lets INSURANCE.PREAUTH.APPROVE decide a pending one inline.
 */
function EnhancementsModal({
  request,
  canCreate,
  canApprove,
  onClose,
}: {
  request: PreAuthRequest
  canCreate: boolean
  canApprove: boolean
  onClose: () => void
}) {
  const { data: enhancements = [], isLoading } = usePreAuthEnhancements(request.id)
  const requestEnhancement = useRequestEnhancement(request.id)
  const decideEnhancement = useDecideEnhancement()

  const [requesting, setRequesting] = useState(false)
  const [amount, setAmount] = useState('')
  const [reason, setReason] = useState('')
  const [deciding, setDeciding] = useState<{ enhancement: PreAuthEnhancement; approve: boolean } | null>(null)
  const [rejectionReason, setRejectionReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const hasPendingRequest = enhancements.some((e) => e.status === ENHANCEMENT_STATUSES.Requested)

  const submitRequest = async () => {
    if (!amount || !reason.trim()) {
      setError('Amount and reason are required.')
      return
    }
    setError(null)
    try {
      await requestEnhancement.mutateAsync({ requestedAmount: Number(amount), reason: reason.trim() })
      setAmount('')
      setReason('')
      setRequesting(false)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const submitDecision = async () => {
    if (!deciding) return
    setError(null)
    try {
      await decideEnhancement.mutateAsync({
        enhancementId: deciding.enhancement.id,
        approve: deciding.approve,
        rejectionReason: deciding.approve ? null : rejectionReason,
      })
      setDeciding(null)
      setRejectionReason('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Enhancements — ${request.preAuthNumber}`}
      onClose={onClose}
      footer={<Button onClick={onClose}>Close</Button>}
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <p className="text-sm text-slate-600">
          {request.patientName} · estimated {request.estimatedAmount.toFixed(2)} · approved{' '}
          {(request.approvedAmount ?? 0).toFixed(2)}
        </p>

        {isLoading && <Spinner />}
        {!isLoading && enhancements.length === 0 && <p className="text-sm text-slate-400">No enhancement requests yet.</p>}
        {enhancements.length > 0 && (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50 text-left">
                <th className="px-2 py-1.5 font-medium text-slate-600">Requested</th>
                <th className="px-2 py-1.5 text-right font-medium text-slate-600">Amount</th>
                <th className="px-2 py-1.5 font-medium text-slate-600">Reason</th>
                <th className="px-2 py-1.5 font-medium text-slate-600">Status</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {enhancements.map((e) => (
                <tr key={e.id} className="border-b border-slate-100 last:border-0">
                  <td className="px-2 py-1.5 text-xs text-slate-500">
                    {e.requestedByUserName} ({new Date(e.requestedAt).toLocaleDateString()})
                  </td>
                  <td className="px-2 py-1.5 text-right text-slate-700">{e.requestedAmount.toFixed(2)}</td>
                  <td className="px-2 py-1.5 text-slate-700">{e.reason}</td>
                  <td className="px-2 py-1.5">
                    <StatusBadge tone={enhancementStatusTone[e.status]}>{ENHANCEMENT_STATUS_LABELS[e.status]}</StatusBadge>
                  </td>
                  <td className="px-2 py-1.5 text-right">
                    {canApprove && e.status === ENHANCEMENT_STATUSES.Requested && (
                      <div className="flex justify-end gap-1">
                        <Button variant="secondary" onClick={() => setDeciding({ enhancement: e, approve: true })}>
                          Approve
                        </Button>
                        <Button variant="danger" onClick={() => setDeciding({ enhancement: e, approve: false })}>
                          Reject
                        </Button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        {deciding && (
          <div className="rounded-md border border-slate-200 bg-slate-50 p-3">
            <p className="mb-2 text-sm text-slate-700">
              {deciding.approve ? 'Approve' : 'Reject'} enhancement of {deciding.enhancement.requestedAmount.toFixed(2)}
              ? {deciding.approve && 'This adds onto the pre-auth\'s approved amount.'}
            </p>
            {!deciding.approve && (
              <Input
                label="Rejection reason"
                value={rejectionReason}
                onChange={(e) => setRejectionReason(e.target.value)}
                className="mb-2"
              />
            )}
            <div className="flex justify-end gap-2">
              <Button variant="secondary" onClick={() => setDeciding(null)}>
                Cancel
              </Button>
              <Button
                variant={deciding.approve ? 'primary' : 'danger'}
                onClick={submitDecision}
                loading={decideEnhancement.isPending}
                disabled={!deciding.approve && !rejectionReason.trim()}
              >
                Confirm
              </Button>
            </div>
          </div>
        )}

        {canCreate && !hasPendingRequest && !requesting && (
          <Button variant="secondary" onClick={() => setRequesting(true)}>
            Request enhancement
          </Button>
        )}
        {canCreate && hasPendingRequest && (
          <p className="text-xs text-slate-500">An enhancement request is already pending decision.</p>
        )}
        {requesting && (
          <div className="rounded-md border border-slate-200 bg-slate-50 p-3">
            <div className="mb-2 grid grid-cols-2 gap-2">
              <Input label="Requested amount" type="number" min={0} value={amount} onChange={(e) => setAmount(e.target.value)} />
              <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="secondary" onClick={() => setRequesting(false)}>
                Cancel
              </Button>
              <Button onClick={submitRequest} loading={requestEnhancement.isPending}>
                Submit
              </Button>
            </div>
          </div>
        )}
      </div>
    </Modal>
  )
}
