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
import { useAuth } from '../../auth/AuthContext'
import { useAdmissions } from '../../ipd/api'
import { useDoctors } from '../../masters/api'
import { useBatches } from '../../pharmacy/api'
import {
  useAddTeamMember,
  useBooking,
  useCancelBooking,
  useChecklist,
  useCompleteSurgery,
  useConsumableUsage,
  useImplantUsage,
  useIntraOp,
  useOperativeNote,
  usePostOpNote,
  usePostponeBooking,
  usePreAnaesthesia,
  useRecordConsumableUsage,
  useRecordImplantUsage,
  useRemoveTeamMember,
  useRescheduleBooking,
  useSaveIntraOp,
  useSaveOperativeNote,
  useSavePostOpNote,
  useSavePreAnaesthesia,
  useStartSurgery,
  useUpdateBookingPatient,
  useUpdateSignIn,
  useUpdateSignOut,
  useUpdateTimeOut,
  useVerifyConsent,
} from '../api'
import {
  ANAESTHESIA_TYPE_LABELS,
  SURGERY_STATUS_LABELS,
  TEAM_ROLE_LABELS,
  type AnaesthesiaType,
  type SurgeryStatus,
  type SurgeryTeamRole,
} from '../types'

type Tab = 'overview' | 'checklist' | 'anaesthesia' | 'notes' | 'usage' | 'team'

const statusTone: Record<SurgeryStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info', 2: 'warning', 3: 'success', 4: 'danger', 5: 'neutral',
}

export function BookingDetailPage() {
  const { id } = useParams()
  const bookingId = Number(id)
  const navigate = useNavigate()
  const { hasPermission } = useAuth()
  const { data: booking, isLoading } = useBooking(bookingId)
  const [tab, setTab] = useState<Tab>('overview')
  const [error, setError] = useState<string | null>(null)

  if (isLoading || !booking) return <Spinner />

  const tabs: { key: Tab; label: string }[] = [
    { key: 'overview', label: 'Overview' },
    { key: 'checklist', label: 'WHO Checklist' },
    { key: 'anaesthesia', label: 'Anaesthesia' },
    { key: 'notes', label: 'Operative / Post-Op' },
    { key: 'usage', label: 'Implants / Consumables' },
    { key: 'team', label: 'Team' },
  ]

  return (
    <>
      <PageHeader
        title={`${booking.bookingNumber} — ${booking.procedureName}`}
        subtitle={`${booking.patientName} · ${booking.admissionNumber} · ${booking.theatreName}`}
        backTo="/ot/bookings"
        actions={
          <div className="flex items-center gap-2">
            <StatusBadge tone={statusTone[booking.status]}>{SURGERY_STATUS_LABELS[booking.status]}</StatusBadge>
            <Button variant="ghost" onClick={() => navigate('/ot/bookings')}>Back</Button>
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

      {tab === 'overview' && <OverviewTab bookingId={bookingId} canManage={hasPermission('OT.BOOKING.MANAGE')} canConduct={hasPermission('OT.SURGERY.CONDUCT')} canVerifyConsent={hasPermission('OT.CHECKLIST.MANAGE')} setError={setError} />}
      {tab === 'checklist' && <ChecklistTab bookingId={bookingId} canManage={hasPermission('OT.CHECKLIST.MANAGE')} setError={setError} />}
      {tab === 'anaesthesia' && <AnaesthesiaTab bookingId={bookingId} canManage={hasPermission('OT.ANAESTHESIANOTE.MANAGE')} setError={setError} />}
      {tab === 'notes' && <NotesTab bookingId={bookingId} canManage={hasPermission('OT.OPERATIVENOTE.MANAGE')} setError={setError} />}
      {tab === 'usage' && <UsageTab bookingId={bookingId} canRecord={hasPermission('OT.USAGE.RECORD')} setError={setError} />}
      {tab === 'team' && <TeamTab bookingId={bookingId} canManage={hasPermission('OT.BOOKING.MANAGE')} setError={setError} />}
    </>
  )
}

// ── Overview ─────────────────────────────────────────────────────────────
function OverviewTab({
  bookingId, canManage, canConduct, canVerifyConsent, setError,
}: { bookingId: number; canManage: boolean; canConduct: boolean; canVerifyConsent: boolean; setError: (e: string | null) => void }) {
  const { data: booking } = useBooking(bookingId)
  const reschedule = useRescheduleBooking(bookingId)
  const cancel = useCancelBooking(bookingId)
  const postpone = usePostponeBooking(bookingId)
  const verifyConsent = useVerifyConsent(bookingId)
  const start = useStartSurgery(bookingId)
  const complete = useCompleteSurgery(bookingId)
  const updatePatient = useUpdateBookingPatient(bookingId)
  const [rescheduling, setRescheduling] = useState(false)
  const [cancelling, setCancelling] = useState(false)
  const [postponing, setPostponing] = useState(false)
  const [changingPatient, setChangingPatient] = useState(false)
  const [consentId, setConsentId] = useState('')

  if (!booking) return <Spinner />

  const run = async (fn: () => Promise<unknown>) => {
    setError(null)
    try {
      await fn()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-4 rounded-lg border border-slate-200 bg-white p-4 sm:grid-cols-4">
        <Field label="Patient" value={`${booking.patientName} (${booking.admissionNumber})`} />
        <Field label="Surgeon" value={booking.surgeonName} />
        <Field label="Anaesthetist" value={booking.anaesthetistName ?? '—'} />
        <Field label="Scheduled" value={new Date(booking.scheduledStart).toLocaleString()} />
        <Field label="Duration" value={`${new Date(booking.scheduledStart).toLocaleTimeString()} – ${new Date(booking.scheduledEnd).toLocaleTimeString()}`} />
        <Field label="Actual start" value={booking.actualStart ? new Date(booking.actualStart).toLocaleString() : '—'} />
        <Field label="Actual end" value={booking.actualEnd ? new Date(booking.actualEnd).toLocaleString() : '—'} />
        <Field label="Consent verified" value={booking.consentVerifiedAt ? new Date(booking.consentVerifiedAt).toLocaleString() : 'Not verified'} />
        {booking.statusReason && <Field label="Status reason" value={booking.statusReason} />}
        {booking.notes && <Field label="Notes" value={booking.notes} />}
      </div>

      {booking.status === 1 && (
        <div className="flex flex-wrap items-end gap-2 rounded-lg border border-slate-200 bg-white p-4">
          {canVerifyConsent && !booking.consentVerifiedAt && (
            <div className="flex items-end gap-2">
              <Input label="Patient consent ID" value={consentId} onChange={(e) => setConsentId(e.target.value)} />
              <Button variant="secondary" onClick={() => run(() => verifyConsent.mutateAsync(Number(consentId)))} disabled={!consentId}>
                Verify consent
              </Button>
            </div>
          )}
          {canConduct && (
            <Button onClick={() => run(() => start.mutateAsync(undefined))} loading={start.isPending}>
              Start surgery
            </Button>
          )}
          {canManage && <Button variant="secondary" onClick={() => setRescheduling(true)}>Reschedule</Button>}
          {canManage && <Button variant="secondary" onClick={() => setChangingPatient(true)}>Change patient</Button>}
          {canManage && <Button variant="secondary" onClick={() => setPostponing(true)}>Postpone</Button>}
          {canManage && <Button variant="ghost" className="text-red-600" onClick={() => setCancelling(true)}>Cancel</Button>}
        </div>
      )}
      {booking.status === 2 && canConduct && (
        <div className="rounded-lg border border-slate-200 bg-white p-4">
          <Button onClick={() => run(() => complete.mutateAsync(undefined))} loading={complete.isPending}>
            Complete surgery
          </Button>
        </div>
      )}
      {booking.status === 5 && canManage && (
        <div className="rounded-lg border border-slate-200 bg-white p-4">
          <Button variant="ghost" className="text-red-600" onClick={() => setCancelling(true)}>Cancel</Button>
        </div>
      )}

      {rescheduling && (
        <RescheduleModal
          onClose={() => setRescheduling(false)}
          onSubmit={(start, end) => run(() => reschedule.mutateAsync({ scheduledStart: start, scheduledEnd: end })).then(() => setRescheduling(false))}
          loading={reschedule.isPending}
        />
      )}
      {cancelling && (
        <ReasonModal
          title={`Cancel ${booking.bookingNumber}`}
          onClose={() => setCancelling(false)}
          onSubmit={(reason) => run(() => cancel.mutateAsync(reason)).then(() => setCancelling(false))}
          loading={cancel.isPending}
        />
      )}
      {postponing && (
        <ReasonModal
          title={`Postpone ${booking.bookingNumber}`}
          onClose={() => setPostponing(false)}
          onSubmit={(reason) => run(() => postpone.mutateAsync(reason)).then(() => setPostponing(false))}
          loading={postpone.isPending}
        />
      )}
      {changingPatient && (
        <UpdatePatientModal
          currentAdmissionId={booking.admissionId}
          onClose={() => setChangingPatient(false)}
          onSubmit={(admissionId) => run(() => updatePatient.mutateAsync(admissionId)).then(() => setChangingPatient(false))}
          loading={updatePatient.isPending}
        />
      )}
    </div>
  )
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <span className="text-xs text-slate-500">{label}</span>
      <p className="text-sm font-medium text-slate-800">{value}</p>
    </div>
  )
}

function ReasonModal({ title, onClose, onSubmit, loading }: { title: string; onClose: () => void; onSubmit: (reason: string) => void; loading: boolean }) {
  const [reason, setReason] = useState('')
  return (
    <Modal
      open
      title={title}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Back</Button>
          <Button variant="danger" onClick={() => onSubmit(reason)} loading={loading} disabled={!reason.trim()}>Confirm</Button>
        </>
      }
    >
      <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
    </Modal>
  )
}

function RescheduleModal({ onClose, onSubmit, loading }: { onClose: () => void; onSubmit: (start: string, end: string) => void; loading: boolean }) {
  const [start, setStart] = useState('')
  const [end, setEnd] = useState('')
  return (
    <Modal
      open
      title="Reschedule booking"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Back</Button>
          <Button onClick={() => onSubmit(new Date(start).toISOString(), new Date(end).toISOString())} loading={loading} disabled={!start || !end}>
            Reschedule
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        <Input label="New scheduled start" type="datetime-local" value={start} onChange={(e) => setStart(e.target.value)} />
        <Input label="New scheduled end" type="datetime-local" value={end} onChange={(e) => setEnd(e.target.value)} />
      </div>
    </Modal>
  )
}

/** "OT Patient Update" — corrects a mis-selected admission/patient on a Scheduled booking. */
function UpdatePatientModal({
  currentAdmissionId, onClose, onSubmit, loading,
}: { currentAdmissionId: number; onClose: () => void; onSubmit: (admissionId: number) => void; loading: boolean }) {
  const { data: admissionsResult } = useAdmissions({ page: 1, pageSize: 200, status: 1 })
  const [admissionId, setAdmissionId] = useState('')

  const options = (admissionsResult?.items ?? [])
    .filter((a) => a.id !== currentAdmissionId)
    .map((a) => ({ value: a.id, label: `${a.admissionNumber} — ${a.patientName}` }))

  return (
    <Modal
      open
      title="Change patient / admission"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Back</Button>
          <Button onClick={() => onSubmit(Number(admissionId))} loading={loading} disabled={!admissionId}>
            Update
          </Button>
        </>
      }
    >
      <p className="mb-3 text-sm text-slate-500">
        Re-points this booking to a different admission — use only to correct a mis-selected patient.
      </p>
      <Select
        label="Admission"
        placeholder="Select admission…"
        value={admissionId}
        onChange={(e) => setAdmissionId(e.target.value)}
        options={options}
      />
    </Modal>
  )
}

// ── WHO checklist ─────────────────────────────────────────────────────────
function ChecklistTab({ bookingId, canManage, setError }: { bookingId: number; canManage: boolean; setError: (e: string | null) => void }) {
  const { data: checklist, isLoading } = useChecklist(bookingId)
  const updateSignIn = useUpdateSignIn(bookingId)
  const updateTimeOut = useUpdateTimeOut(bookingId)
  const updateSignOut = useUpdateSignOut(bookingId)

  if (isLoading || !checklist) return <Spinner />

  const run = async (fn: () => Promise<unknown>) => {
    setError(null)
    try {
      await fn()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
      <ChecklistPhase
        title="Sign In — before induction of anaesthesia"
        completedAt={checklist.signInCompletedAt}
        canManage={canManage}
        items={[
          ['patientIdentityConfirmed', 'Patient identity, site, procedure & consent confirmed', checklist.patientIdentityConfirmed],
          ['siteMarked', 'Site marked / not applicable', checklist.siteMarked],
          ['anaesthesiaSafetyCheckComplete', 'Anaesthesia safety check complete', checklist.anaesthesiaSafetyCheckComplete],
          ['pulseOximeterFunctioning', 'Pulse oximeter on patient and functioning', checklist.pulseOximeterFunctioning],
          ['knownAllergy', 'Known allergy?', checklist.knownAllergy],
          ['difficultAirwayRisk', 'Difficult airway/aspiration risk?', checklist.difficultAirwayRisk],
          ['riskOfBloodLoss', 'Risk of >500ml blood loss?', checklist.riskOfBloodLoss],
        ]}
        onSubmit={(values, complete) =>
          run(() =>
            updateSignIn.mutateAsync({
              patientIdentityConfirmed: values.patientIdentityConfirmed,
              siteMarked: values.siteMarked,
              anaesthesiaSafetyCheckComplete: values.anaesthesiaSafetyCheckComplete,
              pulseOximeterFunctioning: values.pulseOximeterFunctioning,
              knownAllergy: values.knownAllergy,
              difficultAirwayRisk: values.difficultAirwayRisk,
              riskOfBloodLoss: values.riskOfBloodLoss,
              complete,
            }),
          )
        }
        loading={updateSignIn.isPending}
      />
      <ChecklistPhase
        title="Time Out — before skin incision"
        completedAt={checklist.timeOutCompletedAt}
        canManage={canManage}
        items={[
          ['teamIntroduced', 'All team members introduced by name and role', checklist.teamIntroduced],
          ['patientSiteProcedureConfirmed', 'Patient/site/procedure verbally confirmed', checklist.patientSiteProcedureConfirmed],
          ['criticalEventsReviewed', 'Anticipated critical events reviewed', checklist.criticalEventsReviewed],
          ['antibioticProphylaxisGiven', 'Antibiotic prophylaxis given in last 60 min', checklist.antibioticProphylaxisGiven],
          ['imagingDisplayed', 'Essential imaging displayed', checklist.imagingDisplayed],
        ]}
        onSubmit={(values, complete) =>
          run(() =>
            updateTimeOut.mutateAsync({
              teamIntroduced: values.teamIntroduced,
              patientSiteProcedureConfirmed: values.patientSiteProcedureConfirmed,
              criticalEventsReviewed: values.criticalEventsReviewed,
              antibioticProphylaxisGiven: values.antibioticProphylaxisGiven,
              imagingDisplayed: values.imagingDisplayed,
              complete,
            }),
          )
        }
        loading={updateTimeOut.isPending}
      />
      <ChecklistPhase
        title="Sign Out — before leaving the OR"
        completedAt={checklist.signOutCompletedAt}
        canManage={canManage}
        items={[
          ['procedureNameRecorded', 'Name of procedure recorded', checklist.procedureNameRecorded],
          ['instrumentSpongeNeedleCountCorrect', 'Instrument/sponge/needle counts correct', checklist.instrumentSpongeNeedleCountCorrect],
          ['specimenLabeled', 'Specimen labeled', checklist.specimenLabeled],
          ['equipmentProblemsAddressed', 'Equipment problems addressed', checklist.equipmentProblemsAddressed],
          ['recoveryConcernsReviewed', 'Key concerns for recovery reviewed', checklist.recoveryConcernsReviewed],
        ]}
        onSubmit={(values, complete) =>
          run(() =>
            updateSignOut.mutateAsync({
              procedureNameRecorded: values.procedureNameRecorded,
              instrumentSpongeNeedleCountCorrect: values.instrumentSpongeNeedleCountCorrect,
              specimenLabeled: values.specimenLabeled,
              equipmentProblemsAddressed: values.equipmentProblemsAddressed,
              recoveryConcernsReviewed: values.recoveryConcernsReviewed,
              complete,
            }),
          )
        }
        loading={updateSignOut.isPending}
      />
    </div>
  )
}

function ChecklistPhase({
  title, completedAt, canManage, items, onSubmit, loading,
}: {
  title: string
  completedAt: string | null
  canManage: boolean
  items: [string, string, boolean][]
  onSubmit: (values: Record<string, boolean>, complete: boolean) => void
  loading: boolean
}) {
  const [values, setValues] = useState<Record<string, boolean>>(() => Object.fromEntries(items.map(([key, , value]) => [key, value])))

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="mb-3 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-800">{title}</h3>
        {completedAt && <StatusBadge tone="success">Completed</StatusBadge>}
      </div>
      <div className="space-y-2">
        {items.map(([key, label]) => (
          <label key={key} className="flex items-start gap-2 text-sm text-slate-700">
            <input
              type="checkbox"
              className="mt-0.5"
              checked={values[key]}
              disabled={!canManage}
              onChange={(e) => setValues((prev) => ({ ...prev, [key]: e.target.checked }))}
            />
            {label}
          </label>
        ))}
      </div>
      {canManage && !completedAt && (
        <div className="mt-3 flex gap-2">
          <Button variant="secondary" onClick={() => onSubmit(values, false)} loading={loading}>Save</Button>
          <Button onClick={() => onSubmit(values, true)} loading={loading}>Mark complete</Button>
        </div>
      )}
    </div>
  )
}

// ── Anaesthesia ──────────────────────────────────────────────────────────
function AnaesthesiaTab({ bookingId, canManage, setError }: { bookingId: number; canManage: boolean; setError: (e: string | null) => void }) {
  const { data: preAnaesthesia } = usePreAnaesthesia(bookingId)
  const { data: intraOp } = useIntraOp(bookingId)
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const savePreAnaesthesia = useSavePreAnaesthesia(bookingId)
  const saveIntraOp = useSaveIntraOp(bookingId)

  const [doctorId, setDoctorId] = useState(preAnaesthesia ? String(preAnaesthesia.assessedByDoctorId) : '')
  const [asaGrade, setAsaGrade] = useState(preAnaesthesia?.asaGrade ?? '')
  const [airway, setAirway] = useState(preAnaesthesia?.airwayAssessment ?? '')
  const [comorbidities, setComorbidities] = useState(preAnaesthesia?.comorbidities ?? '')
  const [plan, setPlan] = useState(preAnaesthesia?.anaesthesiaPlan ?? '')

  const [anaesthesiaType, setAnaesthesiaType] = useState<string>(intraOp ? String(intraOp.anaesthesiaType) : '1')
  const [administeredById, setAdministeredById] = useState(intraOp ? String(intraOp.administeredByDoctorId) : '')
  const [startedAt, setStartedAt] = useState('')
  const [intraOpNotes, setIntraOpNotes] = useState(intraOp?.notes ?? '')

  const run = async (fn: () => Promise<unknown>) => {
    setError(null)
    try {
      await fn()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div className="space-y-3 rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="text-sm font-semibold text-slate-800">Pre-Anaesthesia Assessment</h3>
        <Select
          label="Assessed by"
          placeholder="Select…"
          value={doctorId}
          onChange={(e) => setDoctorId(e.target.value)}
          options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
        />
        <Input label="ASA grade" placeholder="e.g. ASA II" value={asaGrade} onChange={(e) => setAsaGrade(e.target.value)} />
        <Input label="Airway assessment" value={airway} onChange={(e) => setAirway(e.target.value)} />
        <Input label="Comorbidities" value={comorbidities} onChange={(e) => setComorbidities(e.target.value)} />
        <Input label="Anaesthesia plan" value={plan} onChange={(e) => setPlan(e.target.value)} />
        {canManage && (
          <Button
            onClick={() =>
              run(() =>
                savePreAnaesthesia.mutateAsync({
                  assessedByDoctorId: Number(doctorId),
                  asaGrade,
                  airwayAssessment: airway || null,
                  comorbidities: comorbidities || null,
                  investigations: null,
                  anaesthesiaPlan: plan || null,
                  notes: null,
                }),
              )
            }
            loading={savePreAnaesthesia.isPending}
            disabled={!doctorId || !asaGrade}
          >
            Save
          </Button>
        )}
        {preAnaesthesia && <p className="text-xs text-slate-500">Last saved by {preAnaesthesia.assessedByDoctorName}</p>}
      </div>

      <div className="space-y-3 rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="text-sm font-semibold text-slate-800">Intra-Op Anaesthesia Record</h3>
        <Select
          label="Type"
          value={anaesthesiaType}
          onChange={(e) => setAnaesthesiaType(e.target.value)}
          options={Object.entries(ANAESTHESIA_TYPE_LABELS).map(([value, label]) => ({ value, label }))}
        />
        <Select
          label="Administered by"
          placeholder="Select…"
          value={administeredById}
          onChange={(e) => setAdministeredById(e.target.value)}
          options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
        />
        <Input label="Started at" type="datetime-local" value={startedAt} onChange={(e) => setStartedAt(e.target.value)} />
        <Input label="Notes" value={intraOpNotes} onChange={(e) => setIntraOpNotes(e.target.value)} />
        {canManage && (
          <Button
            onClick={() =>
              run(() =>
                saveIntraOp.mutateAsync({
                  anaesthesiaType: Number(anaesthesiaType) as AnaesthesiaType,
                  administeredByDoctorId: Number(administeredById),
                  startedAt: startedAt ? new Date(startedAt).toISOString() : new Date().toISOString(),
                  endedAt: null,
                  notes: intraOpNotes || null,
                }),
              )
            }
            loading={saveIntraOp.isPending}
            disabled={!administeredById}
          >
            Save
          </Button>
        )}
        {intraOp && <p className="text-xs text-slate-500">Administered by {intraOp.administeredByDoctorName}</p>}
      </div>
    </div>
  )
}

// ── Operative / post-op notes ────────────────────────────────────────────
function NotesTab({ bookingId, canManage, setError }: { bookingId: number; canManage: boolean; setError: (e: string | null) => void }) {
  const { data: operativeNote } = useOperativeNote(bookingId)
  const { data: postOpNote } = usePostOpNote(bookingId)
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const saveOperative = useSaveOperativeNote(bookingId)
  const savePostOp = useSavePostOpNote(bookingId)

  const [surgeonId, setSurgeonId] = useState(operativeNote ? String(operativeNote.surgeonId) : '')
  const [procedurePerformed, setProcedurePerformed] = useState(operativeNote?.procedurePerformed ?? '')
  const [findings, setFindings] = useState(operativeNote?.findings ?? '')
  const [complications, setComplications] = useState(operativeNote?.complications ?? '')
  const [startedAt, setStartedAt] = useState('')

  const [recoveryNotes, setRecoveryNotes] = useState(postOpNote?.recoveryNotes ?? '')
  const [vitalsSummary, setVitalsSummary] = useState(postOpNote?.vitalsSummary ?? '')

  const run = async (fn: () => Promise<unknown>) => {
    setError(null)
    try {
      await fn()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div className="space-y-3 rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="text-sm font-semibold text-slate-800">Operative Note</h3>
        <Select
          label="Surgeon"
          placeholder="Select…"
          value={surgeonId}
          onChange={(e) => setSurgeonId(e.target.value)}
          options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
        />
        <Input label="Procedure performed" value={procedurePerformed} onChange={(e) => setProcedurePerformed(e.target.value)} />
        <Input label="Findings" value={findings} onChange={(e) => setFindings(e.target.value)} />
        <Input label="Complications" value={complications} onChange={(e) => setComplications(e.target.value)} />
        <Input label="Started at" type="datetime-local" value={startedAt} onChange={(e) => setStartedAt(e.target.value)} />
        {canManage && (
          <Button
            onClick={() =>
              run(() =>
                saveOperative.mutateAsync({
                  surgeonId: Number(surgeonId),
                  procedurePerformed,
                  findings: findings || null,
                  complications: complications || null,
                  startedAt: startedAt ? new Date(startedAt).toISOString() : new Date().toISOString(),
                  endedAt: null,
                  notes: null,
                }),
              )
            }
            loading={saveOperative.isPending}
            disabled={!surgeonId || !procedurePerformed}
          >
            Save
          </Button>
        )}
      </div>

      <div className="space-y-3 rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="text-sm font-semibold text-slate-800">Post-Op / Recovery Note</h3>
        <Input label="Recovery notes" value={recoveryNotes} onChange={(e) => setRecoveryNotes(e.target.value)} />
        <Input label="Vitals summary" value={vitalsSummary} onChange={(e) => setVitalsSummary(e.target.value)} />
        {canManage && (
          <Button
            onClick={() =>
              run(() =>
                savePostOp.mutateAsync({
                  recoveryNotes: recoveryNotes || null,
                  vitalsSummary: vitalsSummary || null,
                  complications: null,
                  shiftedAt: new Date().toISOString(),
                  notes: null,
                }),
              )
            }
            loading={savePostOp.isPending}
          >
            Save
          </Button>
        )}
      </div>
    </div>
  )
}

// ── Implant / consumable usage ───────────────────────────────────────────
function UsageTab({ bookingId, canRecord, setError }: { bookingId: number; canRecord: boolean; setError: (e: string | null) => void }) {
  const { data: implants = [] } = useImplantUsage(bookingId)
  const { data: consumables = [] } = useConsumableUsage(bookingId)
  const { data: batches = [] } = useBatches(undefined, undefined, undefined, true)
  const recordImplant = useRecordImplantUsage(bookingId)
  const recordConsumable = useRecordConsumableUsage(bookingId)

  const [implantBatchId, setImplantBatchId] = useState('')
  const [serialNumber, setSerialNumber] = useState('')
  const [implantQty, setImplantQty] = useState('1')
  const [consumableBatchId, setConsumableBatchId] = useState('')
  const [consumableQty, setConsumableQty] = useState('1')

  const run = async (fn: () => Promise<unknown>) => {
    setError(null)
    try {
      await fn()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const batchOptions = batches.map((b) => ({ value: b.id, label: `${b.productName} — ${b.batchNumber} (${b.quantityOnHand} on hand)` }))

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div className="space-y-3 rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="text-sm font-semibold text-slate-800">Implants used</h3>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-left">
              <th className="py-1.5 font-medium text-slate-600">Item</th>
              <th className="py-1.5 font-medium text-slate-600">Serial</th>
              <th className="py-1.5 text-right font-medium text-slate-600">Qty</th>
              <th className="py-1.5 text-right font-medium text-slate-600">Rate</th>
            </tr>
          </thead>
          <tbody>
            {implants.map((u) => (
              <tr key={u.id} className="border-b border-slate-100 last:border-0">
                <td className="py-1.5 text-slate-700">{u.productName}</td>
                <td className="py-1.5 font-mono text-xs text-slate-500">{u.serialNumber ?? '—'}</td>
                <td className="py-1.5 text-right text-slate-700">{u.quantity}</td>
                <td className="py-1.5 text-right text-slate-700">{u.rate.toFixed(2)}</td>
              </tr>
            ))}
            {implants.length === 0 && (
              <tr>
                <td colSpan={4} className="py-4 text-center text-slate-400">None recorded.</td>
              </tr>
            )}
          </tbody>
        </table>
        {canRecord && (
          <div className="space-y-2 border-t border-slate-100 pt-3">
            <Select label="Batch" placeholder="Select…" value={implantBatchId} onChange={(e) => setImplantBatchId(e.target.value)} options={batchOptions} />
            <div className="flex gap-2">
              <Input label="Serial no" value={serialNumber} onChange={(e) => setSerialNumber(e.target.value)} />
              <Input label="Qty" type="number" value={implantQty} onChange={(e) => setImplantQty(e.target.value)} />
            </div>
            <Button
              onClick={() =>
                run(() =>
                  recordImplant.mutateAsync({ batchId: Number(implantBatchId), serialNumber: serialNumber || null, quantity: Number(implantQty) }),
                ).then(() => {
                  setImplantBatchId('')
                  setSerialNumber('')
                  setImplantQty('1')
                })
              }
              loading={recordImplant.isPending}
              disabled={!implantBatchId || !implantQty}
            >
              Record implant usage
            </Button>
          </div>
        )}
      </div>

      <div className="space-y-3 rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="text-sm font-semibold text-slate-800">Consumables used</h3>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-left">
              <th className="py-1.5 font-medium text-slate-600">Item</th>
              <th className="py-1.5 text-right font-medium text-slate-600">Qty</th>
              <th className="py-1.5 text-right font-medium text-slate-600">Rate</th>
            </tr>
          </thead>
          <tbody>
            {consumables.map((u) => (
              <tr key={u.id} className="border-b border-slate-100 last:border-0">
                <td className="py-1.5 text-slate-700">{u.productName}</td>
                <td className="py-1.5 text-right text-slate-700">{u.quantity}</td>
                <td className="py-1.5 text-right text-slate-700">{u.rate.toFixed(2)}</td>
              </tr>
            ))}
            {consumables.length === 0 && (
              <tr>
                <td colSpan={3} className="py-4 text-center text-slate-400">None recorded.</td>
              </tr>
            )}
          </tbody>
        </table>
        {canRecord && (
          <div className="space-y-2 border-t border-slate-100 pt-3">
            <Select label="Batch" placeholder="Select…" value={consumableBatchId} onChange={(e) => setConsumableBatchId(e.target.value)} options={batchOptions} />
            <Input label="Qty" type="number" value={consumableQty} onChange={(e) => setConsumableQty(e.target.value)} />
            <Button
              onClick={() =>
                run(() => recordConsumable.mutateAsync({ batchId: Number(consumableBatchId), quantity: Number(consumableQty) })).then(() => {
                  setConsumableBatchId('')
                  setConsumableQty('1')
                })
              }
              loading={recordConsumable.isPending}
              disabled={!consumableBatchId || !consumableQty}
            >
              Record consumable usage
            </Button>
          </div>
        )}
      </div>
    </div>
  )
}

// ── Team ─────────────────────────────────────────────────────────────────
function TeamTab({ bookingId, canManage, setError }: { bookingId: number; canManage: boolean; setError: (e: string | null) => void }) {
  const { data: booking } = useBooking(bookingId)
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const addMember = useAddTeamMember(bookingId)
  const removeMember = useRemoveTeamMember(bookingId)
  const [role, setRole] = useState<string>('1')
  const [doctorId, setDoctorId] = useState('')
  const [staffName, setStaffName] = useState('')

  if (!booking) return <Spinner />

  const run = async (fn: () => Promise<unknown>) => {
    setError(null)
    try {
      await fn()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 text-left">
            <th className="py-1.5 font-medium text-slate-600">Role</th>
            <th className="py-1.5 font-medium text-slate-600">Name</th>
            {canManage && <th />}
          </tr>
        </thead>
        <tbody>
          {booking.teamMembers.map((m) => (
            <tr key={m.id} className="border-b border-slate-100 last:border-0">
              <td className="py-1.5 text-slate-700">{TEAM_ROLE_LABELS[m.role]}</td>
              <td className="py-1.5 text-slate-700">{m.doctorName ?? m.staffName}</td>
              {canManage && (
                <td className="py-1.5 text-right">
                  <Button variant="ghost" className="text-red-600" onClick={() => run(() => removeMember.mutateAsync(m.id))}>
                    Remove
                  </Button>
                </td>
              )}
            </tr>
          ))}
          {booking.teamMembers.length === 0 && (
            <tr>
              <td colSpan={3} className="py-4 text-center text-slate-400">No additional team members.</td>
            </tr>
          )}
        </tbody>
      </table>
      {canManage && (
        <div className="mt-3 flex flex-wrap items-end gap-2 border-t border-slate-100 pt-3">
          <Select
            label="Role"
            value={role}
            onChange={(e) => setRole(e.target.value)}
            options={Object.entries(TEAM_ROLE_LABELS).map(([value, label]) => ({ value, label }))}
          />
          <Select
            label="Doctor (optional)"
            placeholder="Not a doctor"
            value={doctorId}
            onChange={(e) => setDoctorId(e.target.value)}
            options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
          />
          <Input label="Staff name (if not a doctor)" value={staffName} onChange={(e) => setStaffName(e.target.value)} />
          <Button
            onClick={() =>
              run(() =>
                addMember.mutateAsync({ role: Number(role) as SurgeryTeamRole, doctorId: doctorId ? Number(doctorId) : null, staffName: staffName || null }),
              ).then(() => {
                setDoctorId('')
                setStaffName('')
              })
            }
            loading={addMember.isPending}
            disabled={!doctorId && !staffName}
          >
            Add
          </Button>
        </div>
      )}
    </div>
  )
}
