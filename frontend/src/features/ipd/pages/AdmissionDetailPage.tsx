import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { compactErrors, hasErrors, requiredPositiveAmount, requiredSelect, requiredText, type FieldErrors } from '../../../lib/formValidation'
import { useAuth } from '../../auth/AuthContext'
import { AdmissionIcuTab } from '../../icu/pages/AdmissionIcuTab'
import { useDoctors, useDrugs } from '../../masters/api'
import { AdmissionMrdTab } from '../../mrd/pages/AdmissionMrdTab'
import { useBatches } from '../../pharmacy/api'
import {
  useAddCarePlan,
  useAddNursingAssessment,
  useAddNursingNote,
  useAddRiskAssessment,
  useAddTransferNote,
  useCarePlans,
  useMar,
  useMarAction,
  useNursingAssessments,
  useNursingNotes,
  useResolveCarePlan,
  useRiskAssessments,
  useTransferNotes,
} from '../../nursing/api'
import {
  CARE_PLAN_STATUS_LABELS,
  MAR_STATUS_LABELS,
  RISK_TYPE_LABELS,
  SHIFT_LABELS,
  type MarStatus,
  type NursingShift,
  type RiskAssessmentType,
} from '../../nursing/types'
import {
  openAdmissionSlip,
  openDischargeSlip,
  openDischargeSummaryPdf,
  useAddIntakeOutput,
  useAddIpVitals,
  useAddProcedureNote,
  useAddProgressNote,
  useAddSecondaryConsultant,
  useAdmission,
  useApproveDischargeSummary,
  useBedBoard,
  useCancelAdmission,
  useCancelDoctorOrder,
  useClearChecklistItem,
  useCreateDoctorOrder,
  useDischargeClearances,
  useDischargeSummary,
  useDoctorOrders,
  useEndSecondaryConsultant,
  useFinalizeDischarge,
  useGenerateIpBill,
  useIntakeOutput,
  useIpVitals,
  useProcedureNotes,
  useProgressNotes,
  useRequestDischarge,
  useTransferBed,
  useTransferConsultant,
  useUpsertDischargeSummary,
} from '../api'
import {
  ADMISSION_STATUS_LABELS,
  ADMISSION_TYPE_LABELS,
  CLEARANCE_TYPE_LABELS,
  DISCHARGE_TYPE_LABELS,
  DOCTOR_ORDER_TYPE_LABELS,
  ORDER_PRIORITY_LABELS,
  ORDER_STATUS_LABELS,
  type AdmissionStatus,
  type ClearanceType,
  type ClinicalOrderPriority,
  type DischargeType,
  type DoctorOrderType,
} from '../types'

type Tab = 'overview' | 'orders' | 'notes' | 'vitals' | 'io' | 'procedures' | 'nursing' | 'icu' | 'mrd' | 'discharge'

const statusTone: Record<AdmissionStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'success', 2: 'neutral', 3: 'danger',
}

export function AdmissionDetailPage() {
  const { id } = useParams()
  const admissionId = Number(id)
  const navigate = useNavigate()
  const { hasPermission } = useAuth()
  const { data: admission, isLoading } = useAdmission(admissionId)
  const [tab, setTab] = useState<Tab>('overview')
  const [error, setError] = useState<string | null>(null)

  if (isLoading || !admission) return <Spinner />

  const tabs: { key: Tab; label: string }[] = [
    { key: 'overview', label: 'Overview' },
    { key: 'orders', label: 'Orders' },
    { key: 'notes', label: 'Progress Notes' },
    { key: 'vitals', label: 'Vitals' },
    { key: 'io', label: 'Intake/Output' },
    { key: 'procedures', label: 'Procedures' },
    { key: 'nursing', label: 'Nursing' },
    { key: 'icu', label: 'ICU' },
    { key: 'mrd', label: 'MRD' },
    { key: 'discharge', label: 'Discharge' },
  ]

  return (
    <>
      <PageHeader
        title={`${admission.admissionNumber} — ${admission.patientName}`}
        subtitle={`${admission.patientUhid} · ${admission.patientGender} / ${admission.patientAgeDisplay} · ${admission.wardName}/${admission.roomName}/${admission.bedCode}`}
        backTo="/ipd/admissions"
        actions={
          <div className="flex items-center gap-2">
            <StatusBadge tone={statusTone[admission.status]}>{ADMISSION_STATUS_LABELS[admission.status]}</StatusBadge>
            <Button variant="secondary" onClick={() => openAdmissionSlip(admission.id)}>Admission slip</Button>
            <Button variant="ghost" onClick={() => navigate('/ipd/admissions')}>Back</Button>
          </div>
        }
      />

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="mb-4 flex flex-wrap gap-1 border-b border-slate-200">
        {tabs.map((t) => (
          <button
            key={t.key}
            type="button"
            onClick={() => setTab(t.key)}
            className={`border-b-2 px-4 py-2 text-sm font-medium ${
              tab === t.key ? 'border-primary-600 text-primary-700' : 'border-transparent text-slate-500 hover:text-slate-700'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'overview' && (
        <OverviewTab
          admissionId={admissionId}
          canManage={hasPermission('IPD.BED.MANAGE') || hasPermission('IPD.CONSULTANT.MANAGE')}
          canGenerateIpBill={hasPermission('BILLING.BILL.CREATEIP')}
          setError={setError}
        />
      )}
      {tab === 'orders' && <OrdersTab admissionId={admissionId} setError={setError} />}
      {tab === 'notes' && <ProgressNotesTab admissionId={admissionId} setError={setError} />}
      {tab === 'vitals' && <VitalsTab admissionId={admissionId} setError={setError} />}
      {tab === 'io' && <IoTab admissionId={admissionId} setError={setError} />}
      {tab === 'procedures' && <ProceduresTab admissionId={admissionId} setError={setError} />}
      {tab === 'nursing' && <NursingTab admissionId={admissionId} setError={setError} />}
      {tab === 'icu' && <AdmissionIcuTab admissionId={admissionId} setError={setError} />}
      {tab === 'mrd' && <AdmissionMrdTab admissionId={admissionId} setError={setError} />}
      {tab === 'discharge' && <DischargeTab admissionId={admissionId} admittedStatus={admission.status} setError={setError} />}
    </>
  )
}

// ── Overview ─────────────────────────────────────────────────────────────
function OverviewTab({
  admissionId, canManage, canGenerateIpBill, setError,
}: { admissionId: number; canManage: boolean; canGenerateIpBill: boolean; setError: (e: string | null) => void }) {
  const { data: admission } = useAdmission(admissionId)
  const cancelAdmission = useCancelAdmission()
  const generateIpBill = useGenerateIpBill()
  const [transferringBed, setTransferringBed] = useState(false)
  const [transferringConsultant, setTransferringConsultant] = useState(false)
  const [addingConsultant, setAddingConsultant] = useState(false)
  const [cancelling, setCancelling] = useState(false)
  const endConsultant = useEndSecondaryConsultant()

  if (!admission) return null

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <div className="rounded-lg border border-slate-200 bg-white p-4">
          <h3 className="mb-2 text-sm font-semibold text-slate-700">Admission</h3>
          <dl className="space-y-1 text-sm">
            <Row label="Type">{ADMISSION_TYPE_LABELS[admission.admissionType]}</Row>
            <Row label="Diagnosis">{admission.provisionalDiagnosis ?? '—'}</Row>
            <Row label="Admitted">{new Date(admission.admissionDate).toLocaleString()}</Row>
            {admission.referringOpVisitNumber && <Row label="Converted from">OP visit {admission.referringOpVisitNumber}</Row>}
            {admission.referringEmergencyVisitNumber && <Row label="Converted from">ER visit {admission.referringEmergencyVisitNumber}</Row>}
            {admission.cancelReason && <Row label="Cancel reason">{admission.cancelReason}</Row>}
          </dl>
          {admission.status === 1 && canManage && (
            <div className="mt-3 flex gap-2">
              <Button variant="secondary" onClick={() => setCancelling(true)}>Cancel admission</Button>
            </div>
          )}
        </div>

        <div className="rounded-lg border border-slate-200 bg-white p-4">
          <h3 className="mb-2 text-sm font-semibold text-slate-700">IP Billing</h3>
          <p className="mb-2 text-xs text-slate-500">Generate the IP bill, then refresh bed charges as the stay continues.</p>
          {canGenerateIpBill && (
            <div className="flex gap-2">
              <Button
                variant="secondary"
                loading={generateIpBill.isPending}
                onClick={async () => {
                  setError(null)
                  try {
                    await generateIpBill.mutateAsync(admissionId)
                  } catch (err) {
                    setError(errorMessage(err))
                  }
                }}
              >
                Generate / view IP bill
              </Button>
            </div>
          )}
        </div>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <div className="mb-2 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-slate-700">Consultants</h3>
          {admission.status === 1 && canManage && (
            <div className="flex gap-2">
              <Button variant="secondary" onClick={() => setTransferringConsultant(true)}>Transfer primary</Button>
              <Button variant="secondary" onClick={() => setAddingConsultant(true)}>Add secondary</Button>
            </div>
          )}
        </div>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-left text-slate-500">
              <th className="py-1.5 font-medium">Doctor</th>
              <th className="py-1.5 font-medium">Role</th>
              <th className="py-1.5 font-medium">From</th>
              <th className="py-1.5 font-medium">To</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {admission.consultants.map((c) => (
              <tr key={c.id} className="border-b border-slate-100 last:border-0">
                <td className="py-1.5">{c.doctorName}</td>
                <td className="py-1.5">{c.role === 1 ? 'Primary' : 'Secondary'}</td>
                <td className="py-1.5">{new Date(c.assignedAt).toLocaleDateString()}</td>
                <td className="py-1.5">{c.endedAt ? new Date(c.endedAt).toLocaleDateString() : '—'}</td>
                <td className="py-1.5 text-right">
                  {c.role === 2 && !c.endedAt && admission.status === 1 && canManage && (
                    <Button
                      variant="ghost"
                      onClick={async () => {
                        try {
                          await endConsultant.mutateAsync({ id: admissionId, consultantId: c.id })
                        } catch (err) {
                          setError(errorMessage(err))
                        }
                      }}
                    >
                      End
                    </Button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <div className="mb-2 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-slate-700">Bed history</h3>
          {admission.status === 1 && canManage && <Button variant="secondary" onClick={() => setTransferringBed(true)}>Transfer bed</Button>}
        </div>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-left text-slate-500">
              <th className="py-1.5 font-medium">Bed</th>
              <th className="py-1.5 font-medium">Charge/day</th>
              <th className="py-1.5 font-medium">From</th>
              <th className="py-1.5 font-medium">To</th>
            </tr>
          </thead>
          <tbody>
            {admission.bedAllocations.map((b) => (
              <tr key={b.id} className="border-b border-slate-100 last:border-0">
                <td className="py-1.5">{b.wardName}/{b.roomName}/{b.bedCode}</td>
                <td className="py-1.5">{b.bedChargePerDay.toFixed(2)}</td>
                <td className="py-1.5">{new Date(b.allocatedAt).toLocaleString()}</td>
                <td className="py-1.5">{b.releasedAt ? new Date(b.releasedAt).toLocaleString() : 'Current'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {transferringBed && (
        <TransferBedModal
          admissionId={admissionId}
          onClose={() => setTransferringBed(false)}
          setError={setError}
        />
      )}
      {transferringConsultant && (
        <TransferConsultantModal admissionId={admissionId} onClose={() => setTransferringConsultant(false)} setError={setError} />
      )}
      {addingConsultant && <AddConsultantModal admissionId={admissionId} onClose={() => setAddingConsultant(false)} setError={setError} />}
      {cancelling && (
        <CancelModal
          onClose={() => setCancelling(false)}
          onSubmit={async (reason) => {
            try {
              await cancelAdmission.mutateAsync({ id: admissionId, reason })
              setCancelling(false)
            } catch (err) {
              setError(errorMessage(err))
            }
          }}
          loading={cancelAdmission.isPending}
        />
      )}
    </div>
  )
}

function Row({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex justify-between gap-4">
      <dt className="text-slate-500">{label}</dt>
      <dd className="text-right text-slate-700">{children}</dd>
    </div>
  )
}

function TransferBedModal({ admissionId, onClose, setError }: { admissionId: number; onClose: () => void; setError: (e: string | null) => void }) {
  const transfer = useTransferBed()
  const { data: bedBoard = [] } = useBedBoard()
  const vacantBeds = bedBoard.filter((b) => b.status === 1)
  const [bedId, setBedId] = useState('')
  const [reason, setReason] = useState('')

  return (
    <Modal
      open
      title="Transfer bed"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button
            loading={transfer.isPending}
            disabled={!bedId}
            onClick={async () => {
              try {
                await transfer.mutateAsync({ id: admissionId, newBedId: Number(bedId), reason: reason || null })
                onClose()
              } catch (err) {
                setError(errorMessage(err))
              }
            }}
          >
            Transfer
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        <Select
          label="New bed (vacant only)"
          placeholder="Select…"
          value={bedId}
          onChange={(e) => setBedId(e.target.value)}
          options={vacantBeds.map((b) => ({ value: b.bedId, label: `${b.wardName}/${b.roomName}/${b.bedCode}` }))}
        />
        <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
      </div>
    </Modal>
  )
}

function TransferConsultantModal({ admissionId, onClose, setError }: { admissionId: number; onClose: () => void; setError: (e: string | null) => void }) {
  const transfer = useTransferConsultant()
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const [doctorId, setDoctorId] = useState('')
  const [notes, setNotes] = useState('')

  return (
    <Modal
      open
      title="Transfer primary consultant"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button
            loading={transfer.isPending}
            disabled={!doctorId}
            onClick={async () => {
              try {
                await transfer.mutateAsync({ id: admissionId, newPrimaryConsultantId: Number(doctorId), notes: notes || null })
                onClose()
              } catch (err) {
                setError(errorMessage(err))
              }
            }}
          >
            Transfer
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        <Select
          label="New primary consultant"
          placeholder="Select…"
          value={doctorId}
          onChange={(e) => setDoctorId(e.target.value)}
          options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
        />
        <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>
    </Modal>
  )
}

function AddConsultantModal({ admissionId, onClose, setError }: { admissionId: number; onClose: () => void; setError: (e: string | null) => void }) {
  const add = useAddSecondaryConsultant()
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const [doctorId, setDoctorId] = useState('')
  const [notes, setNotes] = useState('')

  return (
    <Modal
      open
      title="Add secondary consultant"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button
            loading={add.isPending}
            disabled={!doctorId}
            onClick={async () => {
              try {
                await add.mutateAsync({ id: admissionId, doctorId: Number(doctorId), notes: notes || null })
                onClose()
              } catch (err) {
                setError(errorMessage(err))
              }
            }}
          >
            Add
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        <Select
          label="Doctor"
          placeholder="Select…"
          value={doctorId}
          onChange={(e) => setDoctorId(e.target.value)}
          options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
        />
        <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>
    </Modal>
  )
}

function CancelModal({ onClose, onSubmit, loading }: { onClose: () => void; onSubmit: (reason: string) => void; loading: boolean }) {
  const [reason, setReason] = useState('')
  return (
    <Modal
      open
      title="Cancel admission"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Back</Button>
          <Button variant="danger" disabled={!reason.trim()} loading={loading} onClick={() => onSubmit(reason)}>Cancel admission</Button>
        </>
      }
    >
      <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
    </Modal>
  )
}

// ── Orders ───────────────────────────────────────────────────────────────
function OrdersTab({ admissionId, setError }: { admissionId: number; setError: (e: string | null) => void }) {
  const { data: orders = [], isLoading } = useDoctorOrders(admissionId)
  const cancel = useCancelDoctorOrder(admissionId)
  const [creating, setCreating] = useState(false)

  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      <div className="flex items-center justify-between border-b border-slate-200 p-3">
        <h3 className="text-sm font-semibold text-slate-700">Doctor orders</h3>
        <Button onClick={() => setCreating(true)}>New order</Button>
      </div>
      {isLoading && <Spinner />}
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50 text-left">
            <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Description</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">By</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Priority</th>
            <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {orders.map((o) => (
            <tr key={o.id} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-2.5">{DOCTOR_ORDER_TYPE_LABELS[o.orderType]}</td>
              <td className="px-4 py-2.5">
                {o.description}
                {o.medication && (
                  <p className="text-xs text-slate-400">{o.medication.drugName} — {o.medication.dose}, {o.medication.route}, {o.medication.frequency}</p>
                )}
                {o.diet && <p className="text-xs text-slate-400">{o.diet.dietType}</p>}
              </td>
              <td className="px-4 py-2.5">{o.orderedByDoctorName}</td>
              <td className="px-4 py-2.5">{ORDER_PRIORITY_LABELS[o.priority]}</td>
              <td className="px-4 py-2.5">
                <StatusBadge tone={o.status === 4 ? 'danger' : o.status === 3 ? 'success' : 'info'}>{ORDER_STATUS_LABELS[o.status]}</StatusBadge>
              </td>
              <td className="px-4 py-2.5 text-right">
                {o.status !== 3 && o.status !== 4 && (
                  <Button
                    variant="ghost"
                    onClick={async () => {
                      const reason = window.prompt('Cancellation reason?')
                      if (!reason) return
                      try {
                        await cancel.mutateAsync({ orderId: o.id, reason })
                      } catch (err) {
                        setError(errorMessage(err))
                      }
                    }}
                  >
                    Cancel
                  </Button>
                )}
              </td>
            </tr>
          ))}
          {!isLoading && orders.length === 0 && (
            <tr>
              <td colSpan={6} className="px-4 py-10 text-center text-slate-400">No orders yet.</td>
            </tr>
          )}
        </tbody>
      </table>
      {creating && <CreateOrderModal admissionId={admissionId} onClose={() => setCreating(false)} setError={setError} />}
    </div>
  )
}

function CreateOrderModal({ admissionId, onClose, setError }: { admissionId: number; onClose: () => void; setError: (e: string | null) => void }) {
  const create = useCreateDoctorOrder(admissionId)
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const { data: drugsResult } = useDrugs({ page: 1, pageSize: 500 })
  const [orderedByDoctorId, setOrderedByDoctorId] = useState('')
  const [orderType, setOrderType] = useState<DoctorOrderType>(1)
  const [description, setDescription] = useState('')
  const [priority, setPriority] = useState<ClinicalOrderPriority>(1)
  const [notes, setNotes] = useState('')
  const [drugId, setDrugId] = useState('')
  const [dose, setDose] = useState('')
  const [route, setRoute] = useState('')
  const [frequency, setFrequency] = useState('')
  const [startDate, setStartDate] = useState(new Date().toISOString().slice(0, 10))
  const [dietType, setDietType] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      orderedByDoctorId: requiredSelect(orderedByDoctorId, 'Ordered by'),
      description: requiredText(description, 'Description'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await create.mutateAsync({
        orderedByDoctorId: Number(orderedByDoctorId),
        orderType,
        description,
        priority,
        notes: notes || null,
        medication: orderType === 1 ? { drugId: Number(drugId), dose, route, frequency, startDate, endDate: null, instructions: null } : null,
        diet: orderType === 4 ? { dietType, instructions: null } : null,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="New doctor order"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={create.isPending}>Place order</Button>
        </>
      }
    >
      <div className="max-h-[65vh] space-y-3 overflow-y-auto">
        <Select
          label="Ordered by"
          required
          error={fieldErrors.orderedByDoctorId}
          placeholder="Select…"
          value={orderedByDoctorId}
          onChange={(e) => setOrderedByDoctorId(e.target.value)}
          options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
        />
        <Select
          label="Order type"
          value={orderType}
          onChange={(e) => setOrderType(Number(e.target.value) as DoctorOrderType)}
          options={[
            { value: 1, label: 'Medication' }, { value: 2, label: 'Lab' }, { value: 3, label: 'Radiology' },
            { value: 4, label: 'Diet' }, { value: 5, label: 'Nursing' },
          ]}
        />
        <Input
          label="Description"
          required
          error={fieldErrors.description}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />
        <Select
          label="Priority"
          value={priority}
          onChange={(e) => setPriority(Number(e.target.value) as ClinicalOrderPriority)}
          options={[{ value: 1, label: 'Routine' }, { value: 2, label: 'Urgent' }, { value: 3, label: 'Stat' }]}
        />

        {orderType === 1 && (
          <div className="rounded-md border border-slate-200 p-3">
            <p className="mb-2 text-xs font-semibold uppercase text-slate-500">Medication</p>
            <Select
              label="Drug"
              placeholder="Select…"
              value={drugId}
              onChange={(e) => setDrugId(e.target.value)}
              options={(drugsResult?.items ?? []).map((d) => ({ value: d.id, label: d.name }))}
            />
            <div className="mt-2 grid grid-cols-3 gap-2">
              <Input label="Dose" value={dose} onChange={(e) => setDose(e.target.value)} />
              <Input label="Route" value={route} onChange={(e) => setRoute(e.target.value)} />
              <Input label="Frequency" value={frequency} onChange={(e) => setFrequency(e.target.value)} />
            </div>
            <div className="mt-2">
              <Input label="Start date" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
            </div>
          </div>
        )}
        {orderType === 4 && (
          <div className="rounded-md border border-slate-200 p-3">
            <p className="mb-2 text-xs font-semibold uppercase text-slate-500">Diet</p>
            <Input label="Diet type" value={dietType} onChange={(e) => setDietType(e.target.value)} />
          </div>
        )}

        <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>
    </Modal>
  )
}

// ── Progress notes ───────────────────────────────────────────────────────
function ProgressNotesTab({ admissionId, setError }: { admissionId: number; setError: (e: string | null) => void }) {
  const { data: notes = [], isLoading } = useProgressNotes(admissionId)
  const add = useAddProgressNote(admissionId)
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const [doctorId, setDoctorId] = useState('')
  const [text, setText] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      doctorId: requiredSelect(doctorId, 'Doctor'),
      text: requiredText(text, 'Note'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await add.mutateAsync({ doctorId: Number(doctorId), notes: text })
      setText('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Add progress note</h3>
        <div className="flex flex-wrap items-end gap-2">
          <Select
            label="Doctor"
            required
            error={fieldErrors.doctorId}
            placeholder="Select…"
            value={doctorId}
            onChange={(e) => setDoctorId(e.target.value)}
            options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
          />
          <div className="min-w-[280px] flex-1">
            <Input
              label="Note"
              required
              error={fieldErrors.text}
              value={text}
              onChange={(e) => setText(e.target.value)}
            />
          </div>
          <Button loading={add.isPending} onClick={submit}>Add</Button>
        </div>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        {isLoading && <Spinner />}
        <ul className="space-y-3">
          {notes.map((n) => (
            <li key={n.id} className="border-b border-slate-100 pb-2 last:border-0">
              <p className="text-xs text-slate-400">{new Date(n.noteAt).toLocaleString()} — {n.doctorName}</p>
              <p className="text-sm text-slate-700">{n.notes}</p>
            </li>
          ))}
          {!isLoading && notes.length === 0 && <p className="py-6 text-center text-slate-400">No progress notes yet.</p>}
        </ul>
      </div>
    </div>
  )
}

// ── Vitals ───────────────────────────────────────────────────────────────
function VitalsTab({ admissionId, setError }: { admissionId: number; setError: (e: string | null) => void }) {
  const { data: vitals = [], isLoading } = useIpVitals(admissionId)
  const add = useAddIpVitals(admissionId)
  const [form, setForm] = useState({ temperatureF: '', pulseRate: '', respiratoryRate: '', bloodPressureSystolic: '', bloodPressureDiastolic: '', spO2: '', randomBloodSugar: '' })

  const submit = async () => {
    setError(null)
    try {
      await add.mutateAsync({
        temperatureF: form.temperatureF ? Number(form.temperatureF) : null,
        pulseRate: form.pulseRate ? Number(form.pulseRate) : null,
        respiratoryRate: form.respiratoryRate ? Number(form.respiratoryRate) : null,
        bloodPressureSystolic: form.bloodPressureSystolic ? Number(form.bloodPressureSystolic) : null,
        bloodPressureDiastolic: form.bloodPressureDiastolic ? Number(form.bloodPressureDiastolic) : null,
        spO2: form.spO2 ? Number(form.spO2) : null,
        randomBloodSugar: form.randomBloodSugar ? Number(form.randomBloodSugar) : null,
      })
      setForm({ temperatureF: '', pulseRate: '', respiratoryRate: '', bloodPressureSystolic: '', bloodPressureDiastolic: '', spO2: '', randomBloodSugar: '' })
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Record vitals</h3>
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
          <Input label="Temp (°F)" type="number" value={form.temperatureF} onChange={(e) => setForm({ ...form, temperatureF: e.target.value })} />
          <Input label="Pulse" type="number" value={form.pulseRate} onChange={(e) => setForm({ ...form, pulseRate: e.target.value })} />
          <Input label="Resp. rate" type="number" value={form.respiratoryRate} onChange={(e) => setForm({ ...form, respiratoryRate: e.target.value })} />
          <Input label="SpO2 (%)" type="number" value={form.spO2} onChange={(e) => setForm({ ...form, spO2: e.target.value })} />
          <Input label="BP systolic" type="number" value={form.bloodPressureSystolic} onChange={(e) => setForm({ ...form, bloodPressureSystolic: e.target.value })} />
          <Input label="BP diastolic" type="number" value={form.bloodPressureDiastolic} onChange={(e) => setForm({ ...form, bloodPressureDiastolic: e.target.value })} />
          <Input label="RBS" type="number" value={form.randomBloodSugar} onChange={(e) => setForm({ ...form, randomBloodSugar: e.target.value })} />
        </div>
        <Button className="mt-3" loading={add.isPending} onClick={submit}>Record</Button>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Time</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Temp</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Pulse</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">RR</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">BP</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">SpO2</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">RBS</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">By</th>
            </tr>
          </thead>
          <tbody>
            {vitals.map((v) => (
              <tr key={v.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-xs text-slate-500">{new Date(v.recordedAt).toLocaleString()}</td>
                <td className="px-4 py-2.5">{v.temperatureF ?? '—'}</td>
                <td className="px-4 py-2.5">{v.pulseRate ?? '—'}</td>
                <td className="px-4 py-2.5">{v.respiratoryRate ?? '—'}</td>
                <td className="px-4 py-2.5">{v.bloodPressureSystolic && v.bloodPressureDiastolic ? `${v.bloodPressureSystolic}/${v.bloodPressureDiastolic}` : '—'}</td>
                <td className="px-4 py-2.5">{v.spO2 ?? '—'}</td>
                <td className="px-4 py-2.5">{v.randomBloodSugar ?? '—'}</td>
                <td className="px-4 py-2.5 text-xs text-slate-500">{v.recordedByName}</td>
              </tr>
            ))}
            {!isLoading && vitals.length === 0 && (
              <tr><td colSpan={8} className="px-4 py-10 text-center text-slate-400">No vitals recorded yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

// ── Intake/Output ────────────────────────────────────────────────────────
function IoTab({ admissionId, setError }: { admissionId: number; setError: (e: string | null) => void }) {
  const { data: entries = [], isLoading } = useIntakeOutput(admissionId)
  const add = useAddIntakeOutput(admissionId)
  const [isIntake, setIsIntake] = useState(true)
  const [category, setCategory] = useState('')
  const [quantity, setQuantity] = useState('')
  const [notes, setNotes] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      category: requiredText(category, 'Category'),
      quantity: requiredPositiveAmount(quantity, 'Quantity'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await add.mutateAsync({ isIntake, category, quantityMl: Number(quantity), notes: notes || null })
      setCategory('')
      setQuantity('')
      setNotes('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const totalIntake = entries.filter((e) => e.isIntake).reduce((sum, e) => sum + e.quantityMl, 0)
  const totalOutput = entries.filter((e) => !e.isIntake).reduce((sum, e) => sum + e.quantityMl, 0)

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Record intake/output</h3>
        <div className="flex flex-wrap items-end gap-2">
          <Select
            label="Type"
            value={isIntake ? 'in' : 'out'}
            onChange={(e) => setIsIntake(e.target.value === 'in')}
            options={[{ value: 'in', label: 'Intake' }, { value: 'out', label: 'Output' }]}
          />
          <Input
            label="Category"
            required
            error={fieldErrors.category}
            value={category}
            onChange={(e) => setCategory(e.target.value)}
            placeholder="Oral
            /
            IV
            /
            Urine…"
          />
          <Input
            label="Quantity (mL)"
            required
            error={fieldErrors.quantity}
            type="number"
            value={quantity}
            onChange={(e) => setQuantity(e.target.value)}
          />
          <div className="min-w-[200px] flex-1">
            <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
          </div>
          <Button loading={add.isPending} onClick={submit}>Record</Button>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="rounded-lg border border-slate-200 bg-white p-3 text-center">
          <p className="text-2xl font-semibold text-slate-900">{totalIntake} mL</p>
          <p className="text-xs text-slate-500">Total intake</p>
        </div>
        <div className="rounded-lg border border-slate-200 bg-white p-3 text-center">
          <p className="text-2xl font-semibold text-slate-900">{totalOutput} mL</p>
          <p className="text-xs text-slate-500">Total output</p>
        </div>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Time</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Category</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Qty (mL)</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Notes</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((e) => (
              <tr key={e.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-xs text-slate-500">{new Date(e.recordedAt).toLocaleString()}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={e.isIntake ? 'info' : 'warning'}>{e.isIntake ? 'Intake' : 'Output'}</StatusBadge>
                </td>
                <td className="px-4 py-2.5">{e.category}</td>
                <td className="px-4 py-2.5">{e.quantityMl}</td>
                <td className="px-4 py-2.5 text-slate-500">{e.notes ?? '—'}</td>
              </tr>
            ))}
            {!isLoading && entries.length === 0 && (
              <tr><td colSpan={5} className="px-4 py-10 text-center text-slate-400">No entries yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

// ── Procedures ───────────────────────────────────────────────────────────
function ProceduresTab({ admissionId, setError }: { admissionId: number; setError: (e: string | null) => void }) {
  const { data: procedures = [], isLoading } = useProcedureNotes(admissionId)
  const add = useAddProcedureNote(admissionId)
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const [name, setName] = useState('')
  const [doctorId, setDoctorId] = useState('')
  const [findings, setFindings] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      name: requiredText(name, 'Procedure name'),
      doctorId: requiredSelect(doctorId, 'Performed by'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await add.mutateAsync({
        procedureName: name, performedByDoctorId: Number(doctorId), performedAt: new Date().toISOString(),
        findings: findings || null, notes: null,
      })
      setName('')
      setFindings('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Record procedure</h3>
        <div className="grid gap-2 sm:grid-cols-3">
          <Input
            label="Procedure name"
            required
            error={fieldErrors.name}
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
          <Select
            label="Performed by"
            required
            error={fieldErrors.doctorId}
            placeholder="Select…"
            value={doctorId}
            onChange={(e) => setDoctorId(e.target.value)}
            options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
          />
          <Input label="Findings" value={findings} onChange={(e) => setFindings(e.target.value)} />
        </div>
        <Button className="mt-3" loading={add.isPending} onClick={submit}>Record</Button>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <ul className="divide-y divide-slate-100">
          {procedures.map((p) => (
            <li key={p.id} className="p-4">
              <p className="text-sm font-medium text-slate-700">{p.procedureName}</p>
              <p className="text-xs text-slate-400">{new Date(p.performedAt).toLocaleString()} — {p.performedByDoctorName}</p>
              {p.findings && <p className="mt-1 text-sm text-slate-600">{p.findings}</p>}
            </li>
          ))}
          {!isLoading && procedures.length === 0 && <li className="py-10 text-center text-slate-400">No procedures recorded yet.</li>}
        </ul>
      </div>
    </div>
  )
}

// ── Nursing ──────────────────────────────────────────────────────────────
type NursingSubTab = 'notes' | 'mar' | 'risk' | 'careplans' | 'assessments' | 'transfers'

function NursingTab({ admissionId, setError }: { admissionId: number; setError: (e: string | null) => void }) {
  const [sub, setSub] = useState<NursingSubTab>('notes')
  const subTabs: { key: NursingSubTab; label: string }[] = [
    { key: 'notes', label: 'Shift Notes' },
    { key: 'mar', label: 'MAR' },
    { key: 'risk', label: 'Risk Scores' },
    { key: 'careplans', label: 'Care Plans' },
    { key: 'assessments', label: 'Assessments' },
    { key: 'transfers', label: 'Transfer Notes' },
  ]

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-1 border-b border-slate-100">
        {subTabs.map((t) => (
          <button
            key={t.key}
            type="button"
            onClick={() => setSub(t.key)}
            className={`rounded-t px-3 py-1.5 text-xs font-medium ${
              sub === t.key ? 'bg-primary-50 text-primary-700' : 'text-slate-500 hover:text-slate-700'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>
      {sub === 'notes' && <NursingNotesSubTab admissionId={admissionId} setError={setError} />}
      {sub === 'mar' && <MarSubTab admissionId={admissionId} setError={setError} />}
      {sub === 'risk' && <RiskAssessmentsSubTab admissionId={admissionId} setError={setError} />}
      {sub === 'careplans' && <CarePlansSubTab admissionId={admissionId} setError={setError} />}
      {sub === 'assessments' && <NursingAssessmentsSubTab admissionId={admissionId} setError={setError} />}
      {sub === 'transfers' && <TransferNotesSubTab admissionId={admissionId} setError={setError} />}
    </div>
  )
}

function NursingNotesSubTab({ admissionId, setError }: { admissionId: number; setError: (e: string | null) => void }) {
  const { data: notes = [], isLoading } = useNursingNotes(admissionId)
  const add = useAddNursingNote(admissionId)
  const [shift, setShift] = useState('1')
  const [text, setText] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      text: requiredText(text, 'Note'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await add.mutateAsync({ shift: Number(shift) as NursingShift, notes: text })
      setText('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Add shift note</h3>
        <div className="flex flex-wrap items-end gap-2">
          <Select
            label="Shift"
            value={shift}
            onChange={(e) => setShift(e.target.value)}
            options={Object.entries(SHIFT_LABELS).map(([value, label]) => ({ value, label }))}
          />
          <div className="min-w-[280px] flex-1">
            <Input
              label="Note"
              required
              error={fieldErrors.text}
              value={text}
              onChange={(e) => setText(e.target.value)}
            />
          </div>
          <Button loading={add.isPending} onClick={submit}>Add</Button>
        </div>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        {isLoading && <Spinner />}
        <ul className="space-y-3">
          {notes.map((n) => (
            <li key={n.id} className="border-b border-slate-100 pb-2 last:border-0">
              <p className="text-xs text-slate-400">{new Date(n.noteAt).toLocaleString()} — {SHIFT_LABELS[n.shift]} — {n.recordedByName}</p>
              <p className="text-sm text-slate-700">{n.notes}</p>
            </li>
          ))}
          {!isLoading && notes.length === 0 && <p className="py-6 text-center text-slate-400">No shift notes yet.</p>}
        </ul>
      </div>
    </div>
  )
}

function MarSubTab({ admissionId, setError }: { admissionId: number; setError: (e: string | null) => void }) {
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10))
  const { data: schedule = [], isLoading } = useMar(admissionId, date)
  const action = useMarAction(admissionId, date)
  const [administerTarget, setAdministerTarget] = useState<{ id: number; drugId: number; drugName: string; isHighAlert: boolean } | null>(null)

  const act = async (id: number, marStatus: MarStatus) => {
    setError(null)
    const reason = window.prompt(marStatus === 3 ? 'Reason for skipping this dose?' : 'Reason for refusal?')
    if (!reason) return
    try {
      await action.mutateAsync({ id, payload: { action: marStatus, reason, notes: null, verifiedByUserId: null, batchId: null, quantity: null } })
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const administer = async (payload: { verifiedByUserId: number | null; batchId: number | null; quantity: number | null }) => {
    if (!administerTarget) return
    setError(null)
    try {
      await action.mutateAsync({
        id: administerTarget.id,
        payload: { action: 2, reason: null, notes: null, ...payload },
      })
      setAdministerTarget(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="space-y-4">
      <Input label="Date" type="date" value={date} onChange={(e) => setDate(e.target.value)} className="max-w-xs" />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Due</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Drug</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Dose / Route</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th className="px-4 py-2.5 font-medium text-slate-600"></th>
            </tr>
          </thead>
          <tbody>
            {schedule.map((m) => (
              <tr key={m.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-xs text-slate-500">
                  {new Date(m.dueAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </td>
                <td className="px-4 py-2.5">
                  {m.drugName}
                  {m.isHighAlert && (
                    <span className="ml-2 inline-block">
                      <StatusBadge tone="danger">High-alert</StatusBadge>
                    </span>
                  )}
                </td>
                <td className="px-4 py-2.5 text-slate-600">{m.dose} · {m.route}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={m.status === 1 ? 'neutral' : m.status === 2 ? 'success' : 'warning'}>
                    {MAR_STATUS_LABELS[m.status]}
                  </StatusBadge>
                  {m.administration && (
                    <p className="mt-1 text-xs text-slate-400">
                      {m.administration.actionByName}
                      {m.administration.verifiedByName ? ` · verified by ${m.administration.verifiedByName}` : ''}
                      {m.administration.reason ? ` · ${m.administration.reason}` : ''}
                      {m.administration.action === 2 && (
                        m.administration.stockDeducted
                          ? ` · stock: ${m.administration.quantity} from batch ${m.administration.batchNumber}`
                          : ' · stock not tracked'
                      )}
                    </p>
                  )}
                </td>
                <td className="px-4 py-2.5 text-right">
                  {m.status === 1 && (
                    <div className="flex justify-end gap-1">
                      <Button
                        variant="secondary"
                        onClick={() => setAdministerTarget({ id: m.id, drugId: m.drugId, drugName: m.drugName, isHighAlert: m.isHighAlert })}
                      >
                        Administer
                      </Button>
                      <Button variant="ghost" onClick={() => act(m.id, 3)}>Skip</Button>
                      <Button variant="ghost" className="text-red-600" onClick={() => act(m.id, 4)}>Refuse</Button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && schedule.length === 0 && (
              <tr><td colSpan={5} className="px-4 py-10 text-center text-slate-400">No doses scheduled for this date.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {administerTarget && (
        <AdministerDoseModal
          drugName={administerTarget.drugName}
          drugId={administerTarget.drugId}
          isHighAlert={administerTarget.isHighAlert}
          onClose={() => setAdministerTarget(null)}
          onSubmit={administer}
          loading={action.isPending}
        />
      )}
    </div>
  )
}

/**
 * Batch/quantity are optional — a nurse can always confirm the dose with no stock effect,
 * since clinical charting must never be blocked by an inventory problem. Picking a batch
 * decrements Pharmacy stock in the same transaction (closes the "MAR never touches
 * inventory" gap).
 */
function AdministerDoseModal({
  drugName, drugId, isHighAlert, onClose, onSubmit, loading,
}: {
  drugName: string
  drugId: number
  isHighAlert: boolean
  onClose: () => void
  onSubmit: (payload: { verifiedByUserId: number | null; batchId: number | null; quantity: number | null }) => void
  loading: boolean
}) {
  const { data: batches = [] } = useBatches(drugId, undefined, undefined, true)
  const [batchId, setBatchId] = useState('')
  const [quantity, setQuantity] = useState('1')
  const [verifiedByUserId, setVerifiedByUserId] = useState('')

  const batchOptions = batches.map((b) => ({ value: b.id, label: `${b.batchNumber} — ${b.quantityOnHand} on hand` }))
  const canSubmit = !isHighAlert || verifiedByUserId.trim() !== ''

  return (
    <Modal
      open
      title={`Administer — ${drugName}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Back</Button>
          <Button
            onClick={() =>
              onSubmit({
                verifiedByUserId: verifiedByUserId ? Number(verifiedByUserId) : null,
                batchId: batchId ? Number(batchId) : null,
                quantity: batchId ? Number(quantity) : null,
              })
            }
            loading={loading}
            disabled={!canSubmit}
          >
            Confirm administered
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {isHighAlert && (
          <Input
            label="Verifying nurse's user ID (double-verification required for this high-alert drug)"
            type="number"
            value={verifiedByUserId}
            onChange={(e) => setVerifiedByUserId(e.target.value)}
          />
        )}
        <Select
          label="Deduct from stock batch (optional)"
          placeholder="Don't track stock for this dose"
          value={batchId}
          onChange={(e) => setBatchId(e.target.value)}
          options={batchOptions}
        />
        {batchId && <Input label="Quantity" type="number" value={quantity} onChange={(e) => setQuantity(e.target.value)} />}
      </div>
    </Modal>
  )
}

const RISK_ITEMS: Record<RiskAssessmentType, { key: string; label: string; options: { value: number; label: string }[] }[]> = {
  1: [{ key: 'score', label: 'Pain score (0-10 NRS)', options: Array.from({ length: 11 }, (_, i) => ({ value: i, label: String(i) })) }],
  2: [
    { key: 'historyOfFalling', label: 'History of falling', options: [{ value: 0, label: 'No' }, { value: 25, label: 'Yes' }] },
    { key: 'secondaryDiagnosis', label: 'Secondary diagnosis', options: [{ value: 0, label: 'No' }, { value: 15, label: 'Yes' }] },
    {
      key: 'ambulatoryAid', label: 'Ambulatory aid',
      options: [{ value: 0, label: 'None / bedrest / nurse assist' }, { value: 15, label: 'Crutches / cane / walker' }, { value: 30, label: 'Furniture' }],
    },
    { key: 'ivHeparinLock', label: 'IV / Heparin lock', options: [{ value: 0, label: 'No' }, { value: 20, label: 'Yes' }] },
    {
      key: 'gait', label: 'Gait',
      options: [{ value: 0, label: 'Normal / bedrest / wheelchair' }, { value: 10, label: 'Weak' }, { value: 20, label: 'Impaired' }],
    },
    {
      key: 'mentalStatus', label: 'Mental status',
      options: [{ value: 0, label: 'Oriented to own ability' }, { value: 15, label: 'Overestimates / forgets limitations' }],
    },
  ],
  3: [
    { key: 'sensoryPerception', label: 'Sensory perception', options: [1, 2, 3, 4].map((v) => ({ value: v, label: String(v) })) },
    { key: 'moisture', label: 'Moisture', options: [1, 2, 3, 4].map((v) => ({ value: v, label: String(v) })) },
    { key: 'activity', label: 'Activity', options: [1, 2, 3, 4].map((v) => ({ value: v, label: String(v) })) },
    { key: 'mobility', label: 'Mobility', options: [1, 2, 3, 4].map((v) => ({ value: v, label: String(v) })) },
    { key: 'nutrition', label: 'Nutrition', options: [1, 2, 3, 4].map((v) => ({ value: v, label: String(v) })) },
    { key: 'frictionShear', label: 'Friction & shear', options: [1, 2, 3].map((v) => ({ value: v, label: String(v) })) },
  ],
}

function RiskAssessmentsSubTab({ admissionId, setError }: { admissionId: number; setError: (e: string | null) => void }) {
  const [type, setType] = useState<RiskAssessmentType>(2)
  const { data: assessments = [], isLoading } = useRiskAssessments(admissionId, type)
  const add = useAddRiskAssessment(admissionId)
  const items = RISK_ITEMS[type]
  const [scores, setScores] = useState<Record<string, number>>({})

  const submit = async () => {
    setError(null)
    if (items.some((i) => scores[i.key] === undefined)) {
      setError('Score every item before saving.')
      return
    }
    try {
      await add.mutateAsync({ assessmentType: type, itemScores: scores })
      setScores({})
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const riskTone = (level: string) => (level === 'High' || level === 'Severe' ? 'danger' : level === 'Moderate' ? 'warning' : 'success')

  return (
    <div className="space-y-4">
      <Select
        label="Scale"
        value={String(type)}
        onChange={(e) => { setType(Number(e.target.value) as RiskAssessmentType); setScores({}) }}
        options={Object.entries(RISK_TYPE_LABELS).map(([value, label]) => ({ value, label }))}
        className="max-w-xs"
      />

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">New {RISK_TYPE_LABELS[type]} assessment</h3>
        <div className="grid gap-2 sm:grid-cols-2">
          {items.map((item) => (
            <Select
              key={item.key}
              label={item.label}
              placeholder="Select…"
              value={scores[item.key] ?? ''}
              onChange={(e) => setScores((s) => ({ ...s, [item.key]: Number(e.target.value) }))}
              options={item.options.map((o) => ({ value: o.value, label: o.label }))}
            />
          ))}
        </div>
        <Button className="mt-3" loading={add.isPending} onClick={submit}>Save assessment</Button>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        {isLoading && <Spinner />}
        <ul className="space-y-2">
          {assessments.map((a) => (
            <li key={a.id} className="flex items-center justify-between border-b border-slate-100 pb-2 last:border-0">
              <div>
                <p className="text-xs text-slate-400">{new Date(a.assessedAt).toLocaleString()} — {a.assessedByName}</p>
                <p className="text-sm text-slate-700">Score: {a.totalScore}</p>
              </div>
              <StatusBadge tone={riskTone(a.riskLevel)}>{a.riskLevel}</StatusBadge>
            </li>
          ))}
          {!isLoading && assessments.length === 0 && <p className="py-6 text-center text-slate-400">No assessments recorded yet.</p>}
        </ul>
      </div>
    </div>
  )
}

function CarePlansSubTab({ admissionId, setError }: { admissionId: number; setError: (e: string | null) => void }) {
  const { data: plans = [], isLoading } = useCarePlans(admissionId)
  const add = useAddCarePlan(admissionId)
  const resolve = useResolveCarePlan(admissionId)
  const [problem, setProblem] = useState('')
  const [goal, setGoal] = useState('')
  const [interventions, setInterventions] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      problem: requiredText(problem, 'Problem'),
      goal: requiredText(goal, 'Goal'),
      interventions: requiredText(interventions, 'Interventions'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await add.mutateAsync({ problemStatement: problem, goal, interventions })
      setProblem('')
      setGoal('')
      setInterventions('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const resolvePlan = async (carePlanId: number) => {
    setError(null)
    const evaluationNotes = window.prompt('Evaluation notes (optional):')
    try {
      await resolve.mutateAsync({ carePlanId, evaluationNotes: evaluationNotes || null })
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">New care plan</h3>
        <div className="grid gap-2 sm:grid-cols-3">
          <Input
            label="Problem"
            required
            error={fieldErrors.problem}
            value={problem}
            onChange={(e) => setProblem(e.target.value)}
          />
          <Input
            label="Goal"
            required
            error={fieldErrors.goal}
            value={goal}
            onChange={(e) => setGoal(e.target.value)}
          />
          <Input
            label="Interventions"
            required
            error={fieldErrors.interventions}
            value={interventions}
            onChange={(e) => setInterventions(e.target.value)}
          />
        </div>
        <Button className="mt-3" loading={add.isPending} onClick={submit}>Create</Button>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <ul className="divide-y divide-slate-100">
          {plans.map((c) => (
            <li key={c.id} className="p-4">
              <div className="flex items-start justify-between gap-2">
                <div>
                  <p className="text-sm font-medium text-slate-700">{c.problemStatement}</p>
                  <p className="text-xs text-slate-500">Goal: {c.goal}</p>
                  <p className="text-xs text-slate-500">Interventions: {c.interventions}</p>
                  <p className="mt-1 text-xs text-slate-400">{new Date(c.createdAt).toLocaleString()} — {c.createdByName}</p>
                  {c.evaluationNotes && <p className="mt-1 text-xs text-slate-500">Evaluation: {c.evaluationNotes}</p>}
                </div>
                <div className="flex flex-col items-end gap-1">
                  <StatusBadge tone={c.status === 1 ? 'info' : 'neutral'}>{CARE_PLAN_STATUS_LABELS[c.status]}</StatusBadge>
                  {c.status === 1 && <Button variant="ghost" onClick={() => resolvePlan(c.id)}>Resolve</Button>}
                </div>
              </div>
            </li>
          ))}
          {!isLoading && plans.length === 0 && <li className="py-10 text-center text-slate-400">No care plans yet.</li>}
        </ul>
      </div>
    </div>
  )
}

function NursingAssessmentsSubTab({ admissionId, setError }: { admissionId: number; setError: (e: string | null) => void }) {
  const { data: assessments = [], isLoading } = useNursingAssessments(admissionId)
  const add = useAddNursingAssessment(admissionId)
  const [templateCode, setTemplateCode] = useState('ADMISSION')
  const [notes, setNotes] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      notes: requiredText(notes, 'Findings'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await add.mutateAsync({ templateCode, dataJson: JSON.stringify({ notes }) })
      setNotes('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const renderData = (json: string) => {
    try {
      const parsed = JSON.parse(json) as Record<string, unknown>
      return Object.entries(parsed).map(([k, v]) => `${k}: ${String(v)}`).join(' · ')
    } catch {
      return json
    }
  }

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">New assessment</h3>
        <div className="flex flex-wrap items-end gap-2">
          <Select
            label="Template"
            value={templateCode}
            onChange={(e) => setTemplateCode(e.target.value)}
            options={[
              { value: 'ADMISSION', label: 'Admission Assessment' },
              { value: 'SKIN', label: 'Skin Assessment' },
              { value: 'NUTRITION', label: 'Nutrition Screening' },
              { value: 'PSYCHOSOCIAL', label: 'Psychosocial Assessment' },
            ]}
          />
          <div className="min-w-[280px] flex-1">
            <Input
              label="Findings"
              required
              error={fieldErrors.notes}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            />
          </div>
          <Button loading={add.isPending} onClick={submit}>Add</Button>
        </div>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        {isLoading && <Spinner />}
        <ul className="space-y-3">
          {assessments.map((a) => (
            <li key={a.id} className="border-b border-slate-100 pb-2 last:border-0">
              <p className="text-xs text-slate-400">{new Date(a.assessedAt).toLocaleString()} — {a.templateCode} — {a.assessedByName}</p>
              <p className="text-sm text-slate-700">{renderData(a.dataJson)}</p>
            </li>
          ))}
          {!isLoading && assessments.length === 0 && <p className="py-6 text-center text-slate-400">No assessments recorded yet.</p>}
        </ul>
      </div>
    </div>
  )
}

function TransferNotesSubTab({ admissionId, setError }: { admissionId: number; setError: (e: string | null) => void }) {
  const { data: notes = [], isLoading } = useTransferNotes(admissionId)
  const add = useAddTransferNote(admissionId)
  const [text, setText] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      text: requiredText(text, 'Handover notes'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await add.mutateAsync({ bedAllocationId: null, handoverNotes: text, receivedByUserId: null })
      setText('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Add transfer note</h3>
        <div className="flex flex-wrap items-end gap-2">
          <div className="min-w-[280px] flex-1">
            <Input
              label="Handover notes"
              required
              error={fieldErrors.text}
              value={text}
              onChange={(e) => setText(e.target.value)}
            />
          </div>
          <Button loading={add.isPending} onClick={submit}>Add</Button>
        </div>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        {isLoading && <Spinner />}
        <ul className="space-y-3">
          {notes.map((n) => (
            <li key={n.id} className="border-b border-slate-100 pb-2 last:border-0">
              <p className="text-xs text-slate-400">
                {new Date(n.transferredAt).toLocaleString()} — {n.transferredByName}{n.receivedByName ? ` → ${n.receivedByName}` : ''}
              </p>
              <p className="text-sm text-slate-700">{n.handoverNotes}</p>
            </li>
          ))}
          {!isLoading && notes.length === 0 && <p className="py-6 text-center text-slate-400">No transfer notes yet.</p>}
        </ul>
      </div>
    </div>
  )
}

// ── Discharge ────────────────────────────────────────────────────────────
function DischargeTab({ admissionId, admittedStatus, setError }: { admissionId: number; admittedStatus: AdmissionStatus; setError: (e: string | null) => void }) {
  const { data: admission } = useAdmission(admissionId)
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const requestDischarge = useRequestDischarge(admissionId)
  const { data: clearances = [] } = useDischargeClearances(admissionId)
  const clearItem = useClearChecklistItem(admissionId)
  const { data: summary } = useDischargeSummary(admissionId)
  const upsertSummary = useUpsertDischargeSummary(admissionId)
  const approveSummary = useApproveDischargeSummary(admissionId)
  const finalize = useFinalizeDischarge(admissionId)

  const [requestedByDoctorId, setRequestedByDoctorId] = useState('')
  const [dischargeType, setDischargeType] = useState<DischargeType>(1)
  const [dischargeNotes, setDischargeNotes] = useState('')

  const [finalDiagnosis, setFinalDiagnosis] = useState(summary?.finalDiagnosis ?? '')
  const [course, setCourse] = useState(summary?.courseInHospital ?? '')
  const [meds, setMeds] = useState(summary?.medicationsOnDischarge ?? '')
  const [followUp, setFollowUp] = useState(summary?.followUpAdvice ?? '')
  const [condition, setCondition] = useState(summary?.conditionAtDischarge ?? '')
  const [approverDoctorId, setApproverDoctorId] = useState('')

  if (!admission) return null

  if (admission.status === 2) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <p className="mb-3 text-sm text-slate-600">
          Patient discharged on {admission.dischargedAt && new Date(admission.dischargedAt).toLocaleString()} ({admission.dischargeType && DISCHARGE_TYPE_LABELS[admission.dischargeType]}).
        </p>
        <div className="flex gap-2">
          <Button variant="secondary" onClick={() => openDischargeSummaryPdf(admissionId)}>Discharge summary PDF</Button>
          <Button variant="secondary" onClick={() => openDischargeSlip(admissionId)}>Discharge slip</Button>
        </div>
      </div>
    )
  }

  if (admittedStatus !== 1) {
    return <p className="py-10 text-center text-slate-400">Discharge is not applicable for this admission's status.</p>
  }

  if (!admission.dischargeRequestedAt) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Request discharge</h3>
        <div className="grid gap-2 sm:grid-cols-3">
          <Select
            label="Requested by"
            placeholder="Select…"
            value={requestedByDoctorId}
            onChange={(e) => setRequestedByDoctorId(e.target.value)}
            options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
          />
          <Select
            label="Discharge type"
            value={dischargeType}
            onChange={(e) => setDischargeType(Number(e.target.value) as DischargeType)}
            options={Object.entries(DISCHARGE_TYPE_LABELS).map(([v, label]) => ({ value: Number(v), label }))}
          />
          <Input label="Notes" value={dischargeNotes} onChange={(e) => setDischargeNotes(e.target.value)} />
        </div>
        <Button
          className="mt-3"
          loading={requestDischarge.isPending}
          disabled={!requestedByDoctorId}
          onClick={async () => {
            setError(null)
            try {
              await requestDischarge.mutateAsync({ requestedByDoctorId: Number(requestedByDoctorId), dischargeType, dischargeTypeNotes: dischargeNotes || null })
            } catch (err) {
              setError(errorMessage(err))
            }
          }}
        >
          Request discharge
        </Button>
      </div>
    )
  }

  const allCleared = clearances.length > 0 && clearances.every((c) => c.isCleared)

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Clearance checklist</h3>
        <p className="mb-2 text-xs text-slate-500">Settle the IP bill from the Overview tab before clearing Billing.</p>
        <ul className="space-y-2">
          {clearances.map((c) => (
            <li key={c.id} className="flex items-center justify-between rounded-md border border-slate-100 px-3 py-2">
              <div>
                <p className="text-sm font-medium text-slate-700">{CLEARANCE_TYPE_LABELS[c.clearanceType]}</p>
                {c.isCleared && <p className="text-xs text-slate-400">Cleared by {c.clearedByName} on {c.clearedAt && new Date(c.clearedAt).toLocaleString()}</p>}
                {!c.isCleared && c.billBalanceAmount != null && (
                  <p className={`text-xs ${c.billBalanceAmount > 0 ? 'font-medium text-red-600' : 'text-slate-400'}`}>
                    {c.billBalanceAmount > 0
                      ? `Outstanding bill balance: ${c.billBalanceAmount.toFixed(2)}`
                      : 'Bill balance: 0.00 (fully settled)'}
                  </p>
                )}
              </div>
              {c.isCleared ? (
                <StatusBadge tone="success">Cleared</StatusBadge>
              ) : (
                <Button
                  onClick={async () => {
                    try {
                      await clearItem.mutateAsync({ clearanceType: c.clearanceType as ClearanceType, remarks: null })
                    } catch (err) {
                      setError(errorMessage(err))
                    }
                  }}
                >
                  Clear
                </Button>
              )}
            </li>
          ))}
        </ul>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Discharge summary</h3>
        {summary?.isApproved ? (
          <p className="text-sm text-slate-600">Approved by {summary.approvedByName} on {summary.approvedAt && new Date(summary.approvedAt).toLocaleString()}.</p>
        ) : (
          <div className="space-y-2">
            <Input label="Final diagnosis" value={finalDiagnosis} onChange={(e) => setFinalDiagnosis(e.target.value)} />
            <Input label="Course in hospital" value={course} onChange={(e) => setCourse(e.target.value)} />
            <Input label="Medications on discharge" value={meds} onChange={(e) => setMeds(e.target.value)} />
            <Input label="Follow-up advice" value={followUp} onChange={(e) => setFollowUp(e.target.value)} />
            <Input label="Condition at discharge" value={condition} onChange={(e) => setCondition(e.target.value)} />
            <div className="flex gap-2">
              <Button
                variant="secondary"
                loading={upsertSummary.isPending}
                disabled={!finalDiagnosis.trim()}
                onClick={async () => {
                  try {
                    await upsertSummary.mutateAsync({
                      finalDiagnosis, courseInHospital: course || null, medicationsOnDischarge: meds || null,
                      followUpAdvice: followUp || null, conditionAtDischarge: condition || null,
                    })
                  } catch (err) {
                    setError(errorMessage(err))
                  }
                }}
              >
                Save summary
              </Button>
              {summary && (
                <>
                  <Select
                    placeholder="Approving doctor…"
                    value={approverDoctorId}
                    onChange={(e) => setApproverDoctorId(e.target.value)}
                    options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
                  />
                  <Button
                    loading={approveSummary.isPending}
                    disabled={!approverDoctorId}
                    onClick={async () => {
                      try {
                        await approveSummary.mutateAsync(Number(approverDoctorId))
                      } catch (err) {
                        setError(errorMessage(err))
                      }
                    }}
                  >
                    Approve
                  </Button>
                </>
              )}
            </div>
          </div>
        )}
      </div>

      <Button
        variant="danger"
        disabled={!allCleared || !summary?.isApproved}
        loading={finalize.isPending}
        onClick={async () => {
          try {
            await finalize.mutateAsync(undefined)
          } catch (err) {
            setError(errorMessage(err))
          }
        }}
      >
        Finalize discharge
      </Button>
    </div>
  )
}
