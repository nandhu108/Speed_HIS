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
import { useToast } from '../../../components/ui/Toast'
import { useAppointmentForEmergencyVisit, useMapEmergencyToken } from '../../appointments/api'
import { useGenerateEmergencyBill } from '../../billing/api'
import { useAuth } from '../../auth/AuthContext'
import { AdmitPatientModal } from '../../ipd/components/AdmitPatientModal'
import { useDoctors } from '../../masters/api'
import { useBeds, useWards } from '../../masters/api'
import {
  useAccident,
  useAllocateObservation,
  useAssessment,
  useCancelVisit,
  useEmergencyVisit,
  useMlc,
  useObservations,
  useRecordTriage,
  useReleaseObservation,
  useSaveAccident,
  useSaveAssessment,
  useSaveMlc,
} from '../api'
import {
  ARRIVAL_MODE_LABELS,
  DISPOSITION_LABELS,
  EMERGENCY_STATUS_LABELS,
  ESI_LEVEL_LABELS,
  type DispositionType,
  type EmergencyStatus,
  type EsiLevel,
} from '../types'

type Tab = 'overview' | 'assessment' | 'orders' | 'observation' | 'mlc' | 'billing'

const statusTone: Record<EmergencyStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info', 2: 'warning', 3: 'warning', 4: 'success', 5: 'neutral',
}

export function VisitDetailPage() {
  const { id } = useParams()
  const visitId = Number(id)
  const navigate = useNavigate()
  const { hasPermission } = useAuth()
  const { data: visit, isLoading } = useEmergencyVisit(visitId)
  const [tab, setTab] = useState<Tab>('overview')
  const [error, setError] = useState<string | null>(null)

  if (isLoading || !visit) return <Spinner />

  const tabs: { key: Tab; label: string }[] = [
    { key: 'overview', label: 'Overview / Triage' },
    { key: 'assessment', label: 'Assessment / Disposition' },
    { key: 'orders', label: 'Orders' },
    { key: 'observation', label: 'Observation Bed' },
    { key: 'mlc', label: 'MLC / Accident' },
    { key: 'billing', label: 'Billing' },
  ]

  return (
    <>
      <PageHeader
        title={`${visit.visitNumber} — ${visit.patientName}`}
        subtitle={`Token #${visit.tokenNumber} · ${visit.patientUhid} · ${ARRIVAL_MODE_LABELS[visit.arrivalMode]}`}
        backTo="/emergency/queue"
        actions={
          <div className="flex items-center gap-2">
            {visit.isMlc && <StatusBadge tone="danger">MLC</StatusBadge>}
            <StatusBadge tone={statusTone[visit.status]}>{EMERGENCY_STATUS_LABELS[visit.status]}</StatusBadge>
            <Button variant="ghost" onClick={() => navigate('/emergency/queue')}>Back</Button>
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
            className={`border-b-2 px-4 py-2 text-sm font-medium ${tab === t.key ? 'border-primary-600 text-primary-700' : 'border-transparent text-slate-500 hover:text-slate-700'}`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'overview' && (
        <OverviewTab
          visitId={visitId}
          canTriage={hasPermission('EMERGENCY.TRIAGE.RECORD')}
          canManage={hasPermission('EMERGENCY.VISIT.MANAGE')}
          canMapToken={hasPermission('APPOINTMENT.APPOINTMENT.BOOK')}
          setError={setError}
        />
      )}
      {tab === 'assessment' && (
        <AssessmentTab
          visitId={visitId}
          canManage={hasPermission('EMERGENCY.ASSESSMENT.MANAGE')}
          canAdmit={hasPermission('IPD.ADMISSION.CREATE')}
          setError={setError}
        />
      )}
      {tab === 'orders' && <OrdersTab visitId={visitId} />}
      {tab === 'observation' && (
        <ObservationTab visitId={visitId} canManage={hasPermission('EMERGENCY.OBSERVATION.MANAGE')} setError={setError} />
      )}
      {tab === 'mlc' && <MlcTab visitId={visitId} canManage={hasPermission('EMERGENCY.MLC.MANAGE')} setError={setError} />}
      {tab === 'billing' && <BillingTab visitId={visitId} canCreate={hasPermission('BILLING.BILL.CREATEEMERGENCY')} setError={setError} />}
    </>
  )
}

// ── Overview / Triage ────────────────────────────────────────────────────
function OverviewTab({
  visitId, canTriage, canManage, canMapToken, setError,
}: { visitId: number; canTriage: boolean; canManage: boolean; canMapToken: boolean; setError: (e: string | null) => void }) {
  const { data: visit } = useEmergencyVisit(visitId)
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const { data: mappedToken } = useAppointmentForEmergencyVisit(visitId)
  const mapToken = useMapEmergencyToken()
  const recordTriage = useRecordTriage(visitId)
  const cancelVisit = useCancelVisit(visitId)
  const toast = useToast()

  const t = visit?.triage
  const [esiLevel, setEsiLevel] = useState<EsiLevel>(t?.esiLevel ?? 3)
  const [chiefComplaint, setChiefComplaint] = useState(t?.chiefComplaint ?? '')
  const [temperatureF, setTemperatureF] = useState(t?.temperatureF != null ? String(t.temperatureF) : '')
  const [pulseRate, setPulseRate] = useState(t?.pulseRate != null ? String(t.pulseRate) : '')
  const [respiratoryRate, setRespiratoryRate] = useState(t?.respiratoryRate != null ? String(t.respiratoryRate) : '')
  const [bpSystolic, setBpSystolic] = useState(t?.bloodPressureSystolic != null ? String(t.bloodPressureSystolic) : '')
  const [bpDiastolic, setBpDiastolic] = useState(t?.bloodPressureDiastolic != null ? String(t.bloodPressureDiastolic) : '')
  const [spO2, setSpO2] = useState(t?.spO2 != null ? String(t.spO2) : '')
  const [notes, setNotes] = useState(t?.notes ?? '')
  const [assignDoctorId, setAssignDoctorId] = useState(visit?.assignedDoctorId ? String(visit.assignedDoctorId) : '')
  const [cancelling, setCancelling] = useState(false)
  const [cancelReason, setCancelReason] = useState('')

  const saveTriage = async () => {
    if (!chiefComplaint.trim()) return
    setError(null)
    try {
      await recordTriage.mutateAsync({
        esiLevel,
        chiefComplaint,
        temperatureF: temperatureF ? Number(temperatureF) : null,
        pulseRate: pulseRate ? Number(pulseRate) : null,
        respiratoryRate: respiratoryRate ? Number(respiratoryRate) : null,
        bloodPressureSystolic: bpSystolic ? Number(bpSystolic) : null,
        bloodPressureDiastolic: bpDiastolic ? Number(bpDiastolic) : null,
        spO2: spO2 ? Number(spO2) : null,
        notes: notes || null,
        assignDoctorId: assignDoctorId ? Number(assignDoctorId) : null,
      })
      toast.success(t ? 'Triage updated' : 'Triage recorded')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const submitCancel = async () => {
    if (!cancelReason.trim()) return
    setError(null)
    try {
      await cancelVisit.mutateAsync(cancelReason)
      setCancelling(false)
      toast.success('Visit cancelled')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const submitMapToken = async () => {
    setError(null)
    try {
      const appointment = await mapToken.mutateAsync(visitId)
      toast.success(`ER token mapped — token #${appointment.tokenNumber}`)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const disposed = visit?.status === 4 || visit?.status === 5

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div className="space-y-3 rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="text-sm font-semibold text-slate-800">Triage</h3>
        <Select
          label="ESI level"
          value={String(esiLevel)}
          onChange={(e) => setEsiLevel(Number(e.target.value) as EsiLevel)}
          options={Object.entries(ESI_LEVEL_LABELS).map(([value, label]) => ({ value, label }))}
          disabled={!canTriage || disposed}
        />
        <label className="block text-sm font-medium text-slate-700">
          Chief complaint
          <textarea
            className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
            rows={2}
            value={chiefComplaint}
            onChange={(e) => setChiefComplaint(e.target.value)}
            disabled={!canTriage || disposed}
          />
        </label>
        <div className="grid grid-cols-3 gap-3">
          <Input label="Temp (°F)" type="number" value={temperatureF} onChange={(e) => setTemperatureF(e.target.value)} disabled={!canTriage || disposed} />
          <Input label="Pulse" type="number" value={pulseRate} onChange={(e) => setPulseRate(e.target.value)} disabled={!canTriage || disposed} />
          <Input label="RR" type="number" value={respiratoryRate} onChange={(e) => setRespiratoryRate(e.target.value)} disabled={!canTriage || disposed} />
          <Input label="BP Sys" type="number" value={bpSystolic} onChange={(e) => setBpSystolic(e.target.value)} disabled={!canTriage || disposed} />
          <Input label="BP Dia" type="number" value={bpDiastolic} onChange={(e) => setBpDiastolic(e.target.value)} disabled={!canTriage || disposed} />
          <Input label="SpO2 (%)" type="number" value={spO2} onChange={(e) => setSpO2(e.target.value)} disabled={!canTriage || disposed} />
        </div>
        <Select
          label="Assign doctor"
          placeholder="Not assigned"
          value={assignDoctorId}
          onChange={(e) => setAssignDoctorId(e.target.value)}
          options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
          disabled={!canTriage || disposed}
        />
        <label className="block text-sm font-medium text-slate-700">
          Notes
          <textarea
            className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
            rows={2}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            disabled={!canTriage || disposed}
          />
        </label>
        {canTriage && !disposed && (
          <Button onClick={saveTriage} loading={recordTriage.isPending} disabled={!chiefComplaint.trim()}>
            {t ? 'Update triage' : 'Record triage'}
          </Button>
        )}
        {t && (
          <p className="text-xs text-slate-400">Last recorded by {t.triagedByName} at {new Date(t.triagedAt).toLocaleString()}</p>
        )}
      </div>

      <div className="space-y-3 rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="text-sm font-semibold text-slate-800">Visit</h3>
        <dl className="space-y-1 text-sm">
          <div className="flex justify-between"><dt className="text-slate-500">Arrived</dt><dd>{visit && new Date(visit.arrivedAt).toLocaleString()}</dd></div>
          <div className="flex justify-between"><dt className="text-slate-500">Arrival mode</dt><dd>{visit && ARRIVAL_MODE_LABELS[visit.arrivalMode]}</dd></div>
          <div className="flex justify-between"><dt className="text-slate-500">Status</dt><dd>{visit && EMERGENCY_STATUS_LABELS[visit.status]}</dd></div>
        </dl>
        {visit?.cancelReason && (
          <p className="rounded-md bg-slate-50 px-3 py-2 text-sm text-slate-600">Cancelled: {visit.cancelReason}</p>
        )}

        <div className="rounded-md bg-slate-50 px-3 py-2">
          <h4 className="text-xs font-semibold uppercase text-slate-500">OPD Token</h4>
          {mappedToken ? (
            <p className="mt-1 text-sm text-slate-700">
              Token #{mappedToken.tokenNumber} with {mappedToken.doctorName}
            </p>
          ) : (
            <>
              <p className="mt-1 text-xs text-slate-400">
                Give this ER patient a real position in the assigned doctor&apos;s OPD queue.
              </p>
              {canMapToken && (
                <Button
                  variant="secondary"
                  className="mt-2"
                  onClick={submitMapToken}
                  loading={mapToken.isPending}
                  disabled={!visit?.assignedDoctorId || disposed}
                >
                  Map ER token
                </Button>
              )}
              {!visit?.assignedDoctorId && (
                <p className="mt-1 text-xs text-slate-400">Assign a doctor above first.</p>
              )}
            </>
          )}
        </div>

        {canManage && !disposed && visit?.status !== 5 && (
          <Button variant="danger" onClick={() => setCancelling(true)}>Cancel visit</Button>
        )}
      </div>

      {cancelling && (
        <Modal
          open
          title="Cancel ER visit"
          onClose={() => setCancelling(false)}
          footer={
            <>
              <Button variant="secondary" onClick={() => setCancelling(false)}>Back</Button>
              <Button variant="danger" onClick={submitCancel} loading={cancelVisit.isPending} disabled={!cancelReason.trim()}>
                Cancel visit
              </Button>
            </>
          }
        >
          <Input label="Reason" value={cancelReason} onChange={(e) => setCancelReason(e.target.value)} />
        </Modal>
      )}
    </div>
  )
}

// ── Assessment / Disposition ─────────────────────────────────────────────
function AssessmentTab({
  visitId, canManage, canAdmit, setError,
}: { visitId: number; canManage: boolean; canAdmit: boolean; setError: (e: string | null) => void }) {
  const navigate = useNavigate()
  const { data: visit } = useEmergencyVisit(visitId)
  const { data: assessment } = useAssessment(visitId)
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const save = useSaveAssessment(visitId)
  const toast = useToast()
  const [admitting, setAdmitting] = useState(false)

  const [doctorId, setDoctorId] = useState(assessment ? String(assessment.assessedByDoctorId) : '')
  const [history, setHistory] = useState(assessment?.history ?? '')
  const [examination, setExamination] = useState(assessment?.examinationFindings ?? '')
  const [diagnosis, setDiagnosis] = useState(assessment?.provisionalDiagnosis ?? '')
  const [treatment, setTreatment] = useState(assessment?.treatmentGiven ?? '')
  const [disposition, setDisposition] = useState(assessment?.disposition ? String(assessment.disposition) : '')
  const [dispositionNotes, setDispositionNotes] = useState(assessment?.dispositionNotes ?? '')
  const [transferFacility, setTransferFacility] = useState(assessment?.transferredToFacility ?? '')
  const [transferSummary, setTransferSummary] = useState(assessment?.transferSummary ?? '')

  const submit = async () => {
    if (!doctorId) return
    setError(null)
    try {
      await save.mutateAsync({
        assessedByDoctorId: Number(doctorId),
        history: history || null,
        examinationFindings: examination || null,
        provisionalDiagnosis: diagnosis || null,
        treatmentGiven: treatment || null,
        disposition: disposition ? (Number(disposition) as DispositionType) : null,
        dispositionNotes: dispositionNotes || null,
        transferredToFacility: transferFacility || null,
        transferSummary: transferSummary || null,
      })
      toast.success('Assessment saved')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="max-w-2xl space-y-3 rounded-lg border border-slate-200 bg-white p-4">
      <Select
        label="Assessed by"
        placeholder="Select doctor…"
        value={doctorId}
        onChange={(e) => setDoctorId(e.target.value)}
        options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
        disabled={!canManage}
      />
      <label className="block text-sm font-medium text-slate-700">
        History
        <textarea className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100" rows={2} value={history} onChange={(e) => setHistory(e.target.value)} disabled={!canManage} />
      </label>
      <label className="block text-sm font-medium text-slate-700">
        Examination findings
        <textarea className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100" rows={2} value={examination} onChange={(e) => setExamination(e.target.value)} disabled={!canManage} />
      </label>
      <Input label="Provisional diagnosis" value={diagnosis} onChange={(e) => setDiagnosis(e.target.value)} disabled={!canManage} />
      <label className="block text-sm font-medium text-slate-700">
        Treatment given
        <textarea className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100" rows={2} value={treatment} onChange={(e) => setTreatment(e.target.value)} disabled={!canManage} />
      </label>
      <Select
        label="Disposition"
        placeholder="Not decided yet"
        value={disposition}
        onChange={(e) => setDisposition(e.target.value)}
        options={Object.entries(DISPOSITION_LABELS).map(([value, label]) => ({ value, label }))}
        disabled={!canManage}
      />
      {disposition === '3' && (
        <>
          <Input label="Transferred to facility" value={transferFacility} onChange={(e) => setTransferFacility(e.target.value)} disabled={!canManage} />
          <label className="block text-sm font-medium text-slate-700">
            Transfer summary
            <textarea className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100" rows={2} value={transferSummary} onChange={(e) => setTransferSummary(e.target.value)} disabled={!canManage} />
          </label>
        </>
      )}
      <Input label="Disposition notes" value={dispositionNotes} onChange={(e) => setDispositionNotes(e.target.value)} disabled={!canManage} />
      <div className="flex gap-2">
        {canManage && (
          <Button onClick={submit} loading={save.isPending} disabled={!doctorId}>Save assessment</Button>
        )}
        {canAdmit && assessment?.disposition === 2 && visit && (
          <Button variant="secondary" onClick={() => setAdmitting(true)}>
            Admit to IP
          </Button>
        )}
      </div>
      {admitting && visit && (
        <AdmitPatientModal
          prefill={{ patientId: visit.patientId, patientName: visit.patientName, patientUhid: visit.patientUhid, emergencyVisitId: visit.id }}
          onClose={() => setAdmitting(false)}
          onAdmitted={(id) => navigate(`/ipd/admissions/${id}`)}
        />
      )}
    </div>
  )
}

// ── Orders (STAT lab/rad — reuses the existing Lab/Rad order screens) ────
function OrdersTab({ visitId }: { visitId: number }) {
  const navigate = useNavigate()
  const { data: visit } = useEmergencyVisit(visitId)
  if (!visit) return <Spinner />

  const erOrder = {
    emergencyVisitId: visit.id,
    patientId: visit.patientId,
    patientName: visit.patientName,
    patientUhid: visit.patientUhid,
  }

  return (
    <div className="max-w-xl space-y-3 rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="text-sm font-semibold text-slate-800">STAT Orders</h3>
      <p className="text-sm text-slate-500">
        Lab and radiology orders raised here are linked back to this ER visit and default to STAT priority.
      </p>
      <div className="flex gap-3">
        <Button onClick={() => navigate('/lab/orders', { state: { erOrder } })}>Order STAT lab test</Button>
        <Button onClick={() => navigate('/rad/orders', { state: { erOrder } })}>Order STAT radiology</Button>
      </div>
    </div>
  )
}

// ── Observation bed ───────────────────────────────────────────────────────
function ObservationTab({
  visitId, canManage, setError,
}: { visitId: number; canManage: boolean; setError: (e: string | null) => void }) {
  const { data: allocations = [] } = useObservations(visitId)
  const { data: wards = [] } = useWards()
  const { data: beds = [] } = useBeds()
  const allocate = useAllocateObservation(visitId)
  const release = useReleaseObservation(visitId)
  const toast = useToast()
  const [bedId, setBedId] = useState('')

  const observationWardNames = new Set(wards.filter((w) => w.isObservation).map((w) => w.name))
  const availableBeds = beds.filter((b) => observationWardNames.has(b.wardName) && b.isActive)
  const active = allocations.filter((a) => !a.releasedAt)

  const submit = async () => {
    if (!bedId) return
    setError(null)
    try {
      await allocate.mutateAsync(Number(bedId))
      setBedId('')
      toast.success('Observation bed allocated')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="max-w-xl space-y-4 rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="text-sm font-semibold text-slate-800">Observation Bed Allocations</h3>
      {allocations.length === 0 && <p className="text-sm text-slate-400">No observation bed allocated yet.</p>}
      {allocations.map((a) => (
        <div key={a.id} className="flex items-center justify-between rounded-md border border-slate-200 px-3 py-2 text-sm">
          <span>{a.wardName} / {a.roomName} / {a.bedCode}</span>
          <div className="flex items-center gap-2">
            <span className="text-xs text-slate-400">
              {new Date(a.allocatedAt).toLocaleString()}{a.releasedAt ? ` → ${new Date(a.releasedAt).toLocaleString()}` : ''}
            </span>
            {!a.releasedAt && canManage && (
              <Button
                variant="ghost"
                onClick={() => release.mutateAsync(a.id).then(() => toast.success('Observation bed released')).catch((err) => setError(errorMessage(err)))}
              >
                Release
              </Button>
            )}
          </div>
        </div>
      ))}
      {canManage && active.length === 0 && (
        <div className="flex items-end gap-2">
          <Select
            label="Allocate observation bed"
            placeholder="Select bed…"
            value={bedId}
            onChange={(e) => setBedId(e.target.value)}
            options={availableBeds.map((b) => ({ value: b.id, label: `${b.wardName} / ${b.roomName} / ${b.code}` }))}
          />
          <Button onClick={submit} loading={allocate.isPending} disabled={!bedId}>Allocate</Button>
        </div>
      )}
      {availableBeds.length === 0 && (
        <p className="text-xs text-slate-400">No observation-ward beds configured yet — add one under Masters → Locations → Wards.</p>
      )}
    </div>
  )
}

// ── MLC / Accident ────────────────────────────────────────────────────────
function MlcTab({
  visitId, canManage, setError,
}: { visitId: number; canManage: boolean; setError: (e: string | null) => void }) {
  const { data: mlc } = useMlc(visitId)
  const { data: accident } = useAccident(visitId)
  const saveMlc = useSaveMlc(visitId)
  const saveAccident = useSaveAccident(visitId)
  const toast = useToast()

  const [policeStation, setPoliceStation] = useState(mlc?.policeStationName ?? '')
  const [policeInformedAt, setPoliceInformedAt] = useState('')
  const [firNumber, setFirNumber] = useState(mlc?.firNumber ?? '')
  const [broughtByName, setBroughtByName] = useState(mlc?.broughtByName ?? '')
  const [broughtByDesignation, setBroughtByDesignation] = useState(mlc?.broughtByDesignation ?? '')
  const [injuryDescription, setInjuryDescription] = useState(mlc?.injuryDescription ?? '')
  const [mlcNotes, setMlcNotes] = useState(mlc?.notes ?? '')

  const [accidentType, setAccidentType] = useState(accident?.accidentType ?? '')
  const [accidentAt, setAccidentAt] = useState('')
  const [accidentLocation, setAccidentLocation] = useState(accident?.accidentLocation ?? '')
  const [vehicleInvolved, setVehicleInvolved] = useState(accident?.vehicleInvolved ?? false)
  const [accidentNotes, setAccidentNotes] = useState(accident?.notes ?? '')

  const submitMlc = async () => {
    setError(null)
    try {
      await saveMlc.mutateAsync({
        policeStationName: policeStation || null,
        policeInformedAt: policeInformedAt ? new Date(policeInformedAt).toISOString() : null,
        firNumber: firNumber || null,
        broughtByName: broughtByName || null,
        broughtByDesignation: broughtByDesignation || null,
        injuryDescription: injuryDescription || null,
        notes: mlcNotes || null,
      })
      toast.success(mlc ? 'MLC updated' : 'MLC saved')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const submitAccident = async () => {
    if (!accidentType.trim()) return
    setError(null)
    try {
      await saveAccident.mutateAsync({
        accidentType,
        accidentAt: accidentAt ? new Date(accidentAt).toISOString() : null,
        accidentLocation: accidentLocation || null,
        vehicleInvolved,
        notes: accidentNotes || null,
      })
      toast.success(accident ? 'Accident record updated' : 'Accident record saved')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div className="space-y-3 rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="text-sm font-semibold text-slate-800">MLC Record</h3>
        {mlc && <p className="font-mono text-xs text-slate-500">{mlc.mlcNumber}</p>}
        <Input label="Police station" value={policeStation} onChange={(e) => setPoliceStation(e.target.value)} disabled={!canManage} />
        <Input label="Police informed at" type="datetime-local" value={policeInformedAt} onChange={(e) => setPoliceInformedAt(e.target.value)} disabled={!canManage} />
        <Input label="FIR number" value={firNumber} onChange={(e) => setFirNumber(e.target.value)} disabled={!canManage} />
        <Input label="Brought by (name)" value={broughtByName} onChange={(e) => setBroughtByName(e.target.value)} disabled={!canManage} />
        <Input label="Brought by (designation)" value={broughtByDesignation} onChange={(e) => setBroughtByDesignation(e.target.value)} disabled={!canManage} />
        <label className="block text-sm font-medium text-slate-700">
          Injury description
          <textarea className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100" rows={2} value={injuryDescription} onChange={(e) => setInjuryDescription(e.target.value)} disabled={!canManage} />
        </label>
        <Input label="Notes" value={mlcNotes} onChange={(e) => setMlcNotes(e.target.value)} disabled={!canManage} />
        {canManage && <Button onClick={submitMlc} loading={saveMlc.isPending}>{mlc ? 'Update MLC' : 'Save MLC'}</Button>}
      </div>

      <div className="space-y-3 rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="text-sm font-semibold text-slate-800">Accident Case Record</h3>
        <Input label="Accident type" placeholder="e.g. Road Traffic Accident" value={accidentType} onChange={(e) => setAccidentType(e.target.value)} disabled={!canManage} />
        <Input label="Accident at" type="datetime-local" value={accidentAt} onChange={(e) => setAccidentAt(e.target.value)} disabled={!canManage} />
        <Input label="Location" value={accidentLocation} onChange={(e) => setAccidentLocation(e.target.value)} disabled={!canManage} />
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" checked={vehicleInvolved} onChange={(e) => setVehicleInvolved(e.target.checked)} disabled={!canManage} />
          Vehicle involved
        </label>
        <Input label="Notes" value={accidentNotes} onChange={(e) => setAccidentNotes(e.target.value)} disabled={!canManage} />
        {canManage && (
          <Button onClick={submitAccident} loading={saveAccident.isPending} disabled={!accidentType.trim()}>
            {accident ? 'Update accident record' : 'Save accident record'}
          </Button>
        )}
      </div>
    </div>
  )
}

// ── Billing ────────────────────────────────────────────────────────────────
function BillingTab({
  visitId, canCreate, setError,
}: { visitId: number; canCreate: boolean; setError: (e: string | null) => void }) {
  const navigate = useNavigate()
  const generate = useGenerateEmergencyBill()
  const toast = useToast()

  const submit = async () => {
    setError(null)
    try {
      const bill = await generate.mutateAsync(visitId)
      toast.success('Emergency bill generated')
      navigate(`/billing/bills/${bill.id}`)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="max-w-xl space-y-3 rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="text-sm font-semibold text-slate-800">Emergency Bill</h3>
      <p className="text-sm text-slate-500">
        Generates an empty bill linked to this ER visit — line items (consultation, procedures, consumables) are then
        added on the bill screen, same as any OT charge.
      </p>
      {canCreate && (
        <Button onClick={submit} loading={generate.isPending}>Generate emergency bill</Button>
      )}
    </div>
  )
}
