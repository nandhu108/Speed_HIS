import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Select } from '../../../components/ui/Select'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useToast } from '../../../components/ui/Toast'
import { useRun } from '../../../lib/useRun'
import { errorMessage } from '../../../api/axios'
import { compactErrors, hasErrors, requiredSelect, requiredText, type FieldErrors } from '../../../lib/formValidation'
import { Tabs } from '../../masters/components/Tabs'
import { useDoctors } from '../../masters/api'
import { useAuth } from '../../auth/AuthContext'
import { AdmitPatientModal } from '../../ipd/components/AdmitPatientModal'
import { DrugPicker } from '../components/DrugPicker'
import { IcdPicker } from '../components/IcdPicker'
import { useAllergyCheck } from '../../emr/api'
import {
  openVisitSlip,
  openVisitSummary,
  useAddOrder,
  useAddReferral,
  useCancelOrder,
  useCompleteVisit,
  useDeleteReferral,
  usePrescriptionTemplates,
  useSaveComplaints,
  useSaveDiagnoses,
  useSaveExamination,
  useSaveFollowUp,
  useSavePrescription,
  useSaveReferralOpinion,
  useSaveVitals,
  useStartConsult,
  useWorkspace,
} from '../api'
import {
  OP_VISIT_STATUS_LABELS,
  OP_VISIT_STATUSES,
  ORDER_PRIORITIES,
  ORDER_PRIORITY_LABELS,
  ORDER_STATUS_LABELS,
  ORDER_TYPE_LABELS,
  ORDER_TYPES,
  REFERRAL_DIRECTION_LABELS,
  REFERRAL_DIRECTIONS,
  VISIT_TYPE_LABELS,
  type ClinicalOrderPriority,
  type ClinicalOrderType,
  type ConsultationWorkspace as WorkspaceData,
  type ReferralDirection,
  type SaveComplaint,
  type SaveDiagnosis,
  type SavePrescriptionItem,
  type VisitReferral,
} from '../types'

const tabs = [
  { key: 'overview', label: 'Overview' },
  { key: 'vitals', label: 'Vitals & History' },
  { key: 'diagnosis', label: 'Diagnosis' },
  { key: 'prescription', label: 'Prescription' },
  { key: 'orders', label: 'Orders' },
  { key: 'referral', label: 'Referral & Follow-up' },
] as const
type TabKey = (typeof tabs)[number]['key']

export function ConsultationWorkspace() {
  const run = useRun()
  const { id } = useParams<{ id: string }>()
  const visitId = id ? Number(id) : null
  const navigate = useNavigate()
  const { hasPermission } = useAuth()
  const canManage = hasPermission('OPD.VISIT.MANAGE')
  const canAdmit = hasPermission('IPD.ADMISSION.CREATE')

  const [tab, setTab] = useState<TabKey>('overview')
  const [admitting, setAdmitting] = useState(false)
  const { data: workspace, isLoading } = useWorkspace(visitId)
  const startConsult = useStartConsult()
  const completeVisit = useCompleteVisit()

  if (isLoading || !workspace || visitId === null) {
    return <Spinner label="Loading workspace…" />
  }

  const { visit } = workspace

  return (
    <>
      <PageHeader
        title={`${visit.patientName} — Token #${visit.tokenNumber}`}
        subtitle={`${visit.visitNumber} · ${visit.patientUhid} · ${visit.patientGender} / ${visit.patientAgeDisplay} · ${visit.patientMobile}`}
        actions={
          <div className="flex items-center gap-2">
            <StatusBadge tone={visit.status === OP_VISIT_STATUSES.Completed ? 'neutral' : 'success'}>
              {OP_VISIT_STATUS_LABELS[visit.status]}
            </StatusBadge>
            <Button variant="secondary" onClick={() => openVisitSlip(visit.id)}>
              Print slip
            </Button>
            <Button variant="secondary" onClick={() => openVisitSummary(visit.id)}>
              Print summary
            </Button>
            {canAdmit && (
              <Button variant="secondary" onClick={() => setAdmitting(true)}>
                Admit to IP
              </Button>
            )}
            {canManage && visit.status === OP_VISIT_STATUSES.Waiting && (
              <Button loading={startConsult.isPending} onClick={() => run(() => startConsult.mutateAsync(visit.id))}>
                Start consult
              </Button>
            )}
            {canManage && visit.status === OP_VISIT_STATUSES.InConsult && (
              <Button loading={completeVisit.isPending} onClick={() => run(() => completeVisit.mutateAsync(visit.id))}>
                Complete visit
              </Button>
            )}
          </div>
        }
      />

      <Tabs tabs={tabs} active={tab} onChange={setTab} />

      {tab === 'overview' && <OverviewTab workspace={workspace} />}
      {tab === 'vitals' && <VitalsTab workspace={workspace} canManage={canManage} />}
      {tab === 'diagnosis' && <DiagnosisTab workspace={workspace} canManage={canManage} />}
      {tab === 'prescription' && <PrescriptionTab workspace={workspace} canManage={canManage} />}
      {tab === 'orders' && <OrdersTab workspace={workspace} canManage={canManage} />}
      {tab === 'referral' && <ReferralTab workspace={workspace} canManage={canManage} />}

      {admitting && (
        <AdmitPatientModal
          prefill={{ patientId: visit.patientId, patientName: visit.patientName, patientUhid: visit.patientUhid, opVisitId: visit.id }}
          onClose={() => setAdmitting(false)}
          onAdmitted={(id) => navigate(`/ipd/admissions/${id}`)}
        />
      )}
    </>
  )
}

function OverviewTab({ workspace }: { workspace: WorkspaceData }) {
  return (
    <div className="grid gap-4 md:grid-cols-2">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <p className="mb-2 text-sm font-semibold text-slate-700">Visit details</p>
        <dl className="grid grid-cols-2 gap-y-2 text-sm">
          <dt className="text-slate-400">Doctor</dt>
          <dd>{workspace.visit.doctorName}</dd>
          <dt className="text-slate-400">Type</dt>
          <dd>{VISIT_TYPE_LABELS[workspace.visit.visitType]}</dd>
          <dt className="text-slate-400">Consultation fee</dt>
          <dd>₹{workspace.visit.consultationFee.toLocaleString()}</dd>
          <dt className="text-slate-400">Date</dt>
          <dd>{workspace.visit.visitDate}</dd>
        </dl>
        <p className="mt-3 text-xs text-slate-400">
          The consultation fee is a draft charge for Billing (Phase 6) to pick up — no bill exists yet.
        </p>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <p className="mb-2 text-sm font-semibold text-slate-700">Recent visits</p>
        {workspace.recentVisits.length === 0 && <p className="text-sm text-slate-400">No prior completed visits.</p>}
        <div className="space-y-2">
          {workspace.recentVisits.map((v) => (
            <div key={v.id} className="text-sm">
              <span className="font-medium">{v.visitDate}</span> — {v.doctorName}
              {v.diagnoses.length > 0 && <span className="text-slate-400"> ({v.diagnoses.join(', ')})</span>}
            </div>
          ))}
        </div>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4 md:col-span-2">
        <p className="mb-2 text-sm font-semibold text-slate-700">Recent medications (last 90 days)</p>
        {workspace.recentMedications.length === 0 && <p className="text-sm text-slate-400">None on record.</p>}
        <div className="flex flex-wrap gap-2">
          {workspace.recentMedications.map((m, i) => (
            <StatusBadge key={i} tone="neutral">
              {m.drugName} — {m.dose} {m.frequency}
            </StatusBadge>
          ))}
        </div>
      </div>
    </div>
  )
}

function VitalsTab({ workspace, canManage }: { workspace: WorkspaceData; canManage: boolean }) {
  const run = useRun()
  const visitId = workspace.visit.id
  const saveVitals = useSaveVitals(visitId)
  const saveComplaints = useSaveComplaints(visitId)
  const saveExamination = useSaveExamination(visitId)

  const [vitals, setVitals] = useState({
    heightCm: workspace.vitals?.heightCm?.toString() ?? '',
    weightKg: workspace.vitals?.weightKg?.toString() ?? '',
    temperatureF: workspace.vitals?.temperatureF?.toString() ?? '',
    pulseRate: workspace.vitals?.pulseRate?.toString() ?? '',
    respiratoryRate: workspace.vitals?.respiratoryRate?.toString() ?? '',
    bloodPressureSystolic: workspace.vitals?.bloodPressureSystolic?.toString() ?? '',
    bloodPressureDiastolic: workspace.vitals?.bloodPressureDiastolic?.toString() ?? '',
    spO2: workspace.vitals?.spO2?.toString() ?? '',
    randomBloodSugar: workspace.vitals?.randomBloodSugar?.toString() ?? '',
  })
  const [complaints, setComplaints] = useState<SaveComplaint[]>(
    workspace.complaints.map((c) => ({ complaint: c.complaint, duration: c.duration })),
  )
  const [history, setHistory] = useState(workspace.examination?.historyNotes ?? '')
  const [examinationNotes, setExaminationNotes] = useState(workspace.examination?.examinationNotes ?? '')

  const num = (v: string) => (v ? Number(v) : null)

  return (
    <div className="grid gap-4 md:grid-cols-2">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <div className="mb-2 flex items-center justify-between">
          <p className="text-sm font-semibold text-slate-700">Vitals</p>
          {canManage && (
            <Button
              loading={saveVitals.isPending}
              onClick={() =>
                run(() =>
                  saveVitals.mutateAsync({
                    heightCm: num(vitals.heightCm),
                    weightKg: num(vitals.weightKg),
                    temperatureF: num(vitals.temperatureF),
                    pulseRate: num(vitals.pulseRate),
                    respiratoryRate: num(vitals.respiratoryRate),
                    bloodPressureSystolic: num(vitals.bloodPressureSystolic),
                    bloodPressureDiastolic: num(vitals.bloodPressureDiastolic),
                    spO2: num(vitals.spO2),
                    randomBloodSugar: num(vitals.randomBloodSugar),
                  }),
                )
              }
            >
              Save vitals
            </Button>
          )}
        </div>
        <div className="grid grid-cols-2 gap-2">
          <Input label="Height (cm)" type="number" value={vitals.heightCm} onChange={(e) => setVitals({ ...vitals, heightCm: e.target.value })} disabled={!canManage} />
          <Input label="Weight (kg)" type="number" value={vitals.weightKg} onChange={(e) => setVitals({ ...vitals, weightKg: e.target.value })} disabled={!canManage} />
          <Input label="Temp (°F)" type="number" value={vitals.temperatureF} onChange={(e) => setVitals({ ...vitals, temperatureF: e.target.value })} disabled={!canManage} />
          <Input label="Pulse" type="number" value={vitals.pulseRate} onChange={(e) => setVitals({ ...vitals, pulseRate: e.target.value })} disabled={!canManage} />
          <Input label="Resp. rate" type="number" value={vitals.respiratoryRate} onChange={(e) => setVitals({ ...vitals, respiratoryRate: e.target.value })} disabled={!canManage} />
          <Input label="SpO2 (%)" type="number" value={vitals.spO2} onChange={(e) => setVitals({ ...vitals, spO2: e.target.value })} disabled={!canManage} />
          <Input label="BP systolic" type="number" value={vitals.bloodPressureSystolic} onChange={(e) => setVitals({ ...vitals, bloodPressureSystolic: e.target.value })} disabled={!canManage} />
          <Input label="BP diastolic" type="number" value={vitals.bloodPressureDiastolic} onChange={(e) => setVitals({ ...vitals, bloodPressureDiastolic: e.target.value })} disabled={!canManage} />
          <Input label="RBS (mg/dL)" type="number" value={vitals.randomBloodSugar} onChange={(e) => setVitals({ ...vitals, randomBloodSugar: e.target.value })} disabled={!canManage} />
        </div>
        {workspace.vitals?.bmi && <p className="mt-2 text-sm text-slate-500">BMI: {workspace.vitals.bmi}</p>}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <div className="mb-2 flex items-center justify-between">
          <p className="text-sm font-semibold text-slate-700">Chief complaints</p>
          {canManage && (
            <div className="flex gap-2">
              <Button variant="secondary" onClick={() => setComplaints([...complaints, { complaint: '', duration: '' }])}>
                Add
              </Button>
              <Button loading={saveComplaints.isPending} onClick={() => run(() => saveComplaints.mutateAsync(complaints.filter((c) => c.complaint.trim())))}>
                Save
              </Button>
            </div>
          )}
        </div>
        <div className="space-y-2">
          {complaints.map((c, i) => (
            <div key={i} className="flex gap-2">
              <Input
                className="flex-1"
                placeholder="Complaint"
                value={c.complaint}
                disabled={!canManage}
                onChange={(e) => setComplaints(complaints.map((row, j) => (j === i ? { ...row, complaint: e.target.value } : row)))}
              />
              <Input
                className="w-28"
                placeholder="Duration"
                value={c.duration ?? ''}
                disabled={!canManage}
                onChange={(e) => setComplaints(complaints.map((row, j) => (j === i ? { ...row, duration: e.target.value } : row)))}
              />
              {canManage && (
                <Button variant="ghost" className="text-red-600" onClick={() => setComplaints(complaints.filter((_, j) => j !== i))}>
                  ✕
                </Button>
              )}
            </div>
          ))}
          {complaints.length === 0 && <p className="text-sm text-slate-400">No complaints recorded.</p>}
        </div>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4 md:col-span-2">
        <div className="mb-2 flex items-center justify-between">
          <p className="text-sm font-semibold text-slate-700">History &amp; examination</p>
          {canManage && (
            <Button
              loading={saveExamination.isPending}
              onClick={() => run(() => saveExamination.mutateAsync({ historyNotes: history || null, examinationNotes: examinationNotes || null }))}
            >
              Save
            </Button>
          )}
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <span className="mb-1 block text-sm font-medium text-slate-700">History taking</span>
            <textarea
              rows={5}
              disabled={!canManage}
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100 disabled:bg-slate-50"
              value={history}
              onChange={(e) => setHistory(e.target.value)}
            />
          </div>
          <div>
            <span className="mb-1 block text-sm font-medium text-slate-700">Examination notes</span>
            <textarea
              rows={5}
              disabled={!canManage}
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100 disabled:bg-slate-50"
              value={examinationNotes}
              onChange={(e) => setExaminationNotes(e.target.value)}
            />
          </div>
        </div>
      </div>
    </div>
  )
}

function DiagnosisTab({ workspace, canManage }: { workspace: WorkspaceData; canManage: boolean }) {
  const run = useRun()
  const visitId = workspace.visit.id
  const saveDiagnoses = useSaveDiagnoses(visitId)
  const [diagnoses, setDiagnoses] = useState<(SaveDiagnosis & { code: string; description: string })[]>(
    workspace.diagnoses.map((d) => ({ icdCodeId: d.icdCodeId, type: d.type, notes: d.notes, code: d.icdCode, description: d.icdDescription })),
  )

  const save = () =>
    run(() => saveDiagnoses.mutateAsync(diagnoses.map(({ icdCodeId, type, notes }) => ({ icdCodeId, type, notes }))));

  return (
    <div className="max-w-2xl rounded-lg border border-slate-200 bg-white p-4">
      <div className="mb-3 flex items-center justify-between">
        <p className="text-sm font-semibold text-slate-700">Diagnosis</p>
        {canManage && (
          <Button loading={saveDiagnoses.isPending} onClick={save}>
            Save
          </Button>
        )}
      </div>

      {canManage && (
        <div className="mb-3">
          <IcdPicker
            onSelect={(icd) =>
              setDiagnoses([...diagnoses, { icdCodeId: icd.id, type: 1, notes: null, code: icd.code, description: icd.description }])
            }
          />
        </div>
      )}

      <div className="space-y-2">
        {diagnoses.map((d, i) => (
          <div key={i} className="flex items-center gap-2 rounded-md border border-slate-200 p-2">
            <div className="flex-1">
              <span className="font-mono text-xs text-primary-700">{d.code}</span> {d.description}
            </div>
            <Select
              options={[
                { value: 1, label: 'Provisional' },
                { value: 2, label: 'Final' },
              ]}
              value={d.type}
              disabled={!canManage}
              onChange={(e) => setDiagnoses(diagnoses.map((row, j) => (j === i ? { ...row, type: Number(e.target.value) as 1 | 2 } : row)))}
              className="w-32"
            />
            {canManage && (
              <Button variant="ghost" className="text-red-600" onClick={() => setDiagnoses(diagnoses.filter((_, j) => j !== i))}>
                Remove
              </Button>
            )}
          </div>
        ))}
        {diagnoses.length === 0 && <p className="text-sm text-slate-400">No diagnoses recorded.</p>}
      </div>
    </div>
  )
}

function PrescriptionTab({ workspace, canManage }: { workspace: WorkspaceData; canManage: boolean }) {
  const run = useRun()
  const navigate = useNavigate()
  const visitId = workspace.visit.id
  const doctorId = workspace.visit.doctorId
  const savePrescription = useSavePrescription(visitId)
  const { data: templates = [] } = usePrescriptionTemplates(doctorId)
  const [notes, setNotes] = useState(workspace.prescription?.notes ?? '')
  const [items, setItems] = useState<(SavePrescriptionItem & { drugName: string })[]>(
    workspace.prescription?.items.map((i) => ({
      drugId: i.drugId, drugName: i.drugName, dose: i.dose, frequency: i.frequency, duration: i.duration, route: i.route, instructions: i.instructions,
    })) ?? [],
  )

  const updateItem = (index: number, patch: Partial<SavePrescriptionItem>) =>
    setItems(items.map((row, i) => (i === index ? { ...row, ...patch } : row)))

  const { data: allergyMatches = [] } = useAllergyCheck(workspace.visit.patientId, items.map((i) => i.drugName))
  const matchedDrugNames = new Set(allergyMatches.map((m) => m.drugName))

  const applyTemplate = (templateId: string) => {
    const template = templates.find((t) => t.id === Number(templateId))
    if (!template) return
    setItems([
      ...items,
      ...template.items.map((i) => ({
        drugId: i.drugId, drugName: i.drugName, dose: i.dose, frequency: i.frequency, duration: i.duration, route: i.route, instructions: i.instructions,
      })),
    ])
  }

  return (
    <div className="max-w-3xl rounded-lg border border-slate-200 bg-white p-4">
      <div className="mb-3 flex items-center justify-between">
        <p className="text-sm font-semibold text-slate-700">Prescription</p>
        <div className="flex items-center gap-2">
          {canManage && templates.length > 0 && (
            <Select placeholder="Apply favorite…" options={templates.map((t) => ({ value: t.id, label: t.name }))} value="" onChange={(e) => applyTemplate(e.target.value)} />
          )}
          {canManage && (
            <Button
              loading={savePrescription.isPending}
              onClick={() => run(() => savePrescription.mutateAsync({ notes: notes || null, items: items.map(({ drugName: _drugName, ...rest }) => rest) }))}
            >
              Save
            </Button>
          )}
          {canManage && workspace.prescription?.id != null && workspace.prescription.items.length > 0 && (
            <Button
              variant="secondary"
              onClick={() =>
                navigate('/pharmacy/sales/new', {
                  state: {
                    prescription: {
                      prescriptionId: workspace.prescription!.id,
                      patientId: workspace.visit.patientId,
                      patientName: workspace.visit.patientName,
                      patientUhid: workspace.visit.patientUhid,
                      items: workspace.prescription!.items.map((i) => ({ drugId: i.drugId, drugName: i.drugName })),
                    },
                  },
                })
              }
            >
              Send to Pharmacy
            </Button>
          )}
        </div>
      </div>

      {canManage && (
        <div className="mb-3">
          <DrugPicker onSelect={(drug) => setItems([...items, { drugId: drug.id, drugName: drug.name, dose: '', frequency: '', duration: '', route: null, instructions: null }])} />
        </div>
      )}

      {allergyMatches.length > 0 && (
        <div className="mb-3 rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-800">
          <p className="font-semibold">⚠ Allergy warning</p>
          <ul className="mt-1 list-inside list-disc">
            {allergyMatches.map((m) => (
              <li key={`${m.allergyId}-${m.drugName}`}>
                {m.drugName} may conflict with a recorded {m.allergenName} allergy{m.reaction ? ` (${m.reaction})` : ''}.
              </li>
            ))}
          </ul>
        </div>
      )}

      <div className="space-y-2">
        {items.map((item, i) => (
          <div key={i} className="grid grid-cols-6 items-end gap-2 rounded-md border border-slate-200 p-2">
            <span className="col-span-2 flex items-center gap-2 text-sm font-medium">
              {item.drugName}
              {matchedDrugNames.has(item.drugName) && <StatusBadge tone="danger">Allergy risk</StatusBadge>}
            </span>
            <Input placeholder="Dose" value={item.dose} disabled={!canManage} onChange={(e) => updateItem(i, { dose: e.target.value })} />
            <Input placeholder="Frequency" value={item.frequency} disabled={!canManage} onChange={(e) => updateItem(i, { frequency: e.target.value })} />
            <Input placeholder="Duration" value={item.duration} disabled={!canManage} onChange={(e) => updateItem(i, { duration: e.target.value })} />
            {canManage && (
              <Button variant="ghost" className="text-red-600" onClick={() => setItems(items.filter((_, j) => j !== i))}>
                Remove
              </Button>
            )}
          </div>
        ))}
        {items.length === 0 && <p className="text-sm text-slate-400">No items prescribed.</p>}
      </div>

      <div className="mt-3">
        <Input label="Notes" value={notes} disabled={!canManage} onChange={(e) => setNotes(e.target.value)} />
      </div>
    </div>
  )
}

function OrdersTab({ workspace, canManage }: { workspace: WorkspaceData; canManage: boolean }) {
  const run = useRun()
  const navigate = useNavigate()
  const visitId = workspace.visit.id
  const addOrder = useAddOrder(visitId)
  const cancelOrder = useCancelOrder(visitId)
  const [orderType, setOrderType] = useState<ClinicalOrderType>(ORDER_TYPES.Lab)
  const [description, setDescription] = useState('')
  const [priority, setPriority] = useState<ClinicalOrderPriority>(ORDER_PRIORITIES.Routine)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const toast = useToast()

  const submit = async () => {
    const errors = compactErrors({ description: requiredText(description, 'Description') })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    // Nothing in this workspace had a catch, and there is no global mutation error handler, so a
    // rejected order used to vanish as an unhandled rejection with the field still populated.
    try {
      await addOrder.mutateAsync({ orderType, description, priority, notes: null })
      setDescription('')
    } catch (err) {
      toast.error(errorMessage(err))
    }
  }

  return (
    <div className="max-w-2xl rounded-lg border border-slate-200 bg-white p-4">
      <p className="mb-3 text-sm font-semibold text-slate-700">Lab / Radiology / Procedure orders</p>

      {canManage && (
        <div className="mb-4 flex flex-wrap items-end gap-2 border-b border-slate-100 pb-4">
          <Select label="Type" options={Object.entries(ORDER_TYPE_LABELS).map(([v, l]) => ({ value: v, label: l }))} value={orderType} onChange={(e) => setOrderType(Number(e.target.value) as ClinicalOrderType)} />
          <Input
            label="Description"
            required
            error={fieldErrors.description}
            className="flex-1"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
          <Select label="Priority" options={Object.entries(ORDER_PRIORITY_LABELS).map(([v, l]) => ({ value: v, label: l }))} value={priority} onChange={(e) => setPriority(Number(e.target.value) as ClinicalOrderPriority)} />
          <Button loading={addOrder.isPending} onClick={submit}>
            Add order
          </Button>
        </div>
      )}

      <div className="space-y-2">
        {workspace.orders.map((order) => (
          <div key={order.id} className="flex items-center justify-between rounded-md border border-slate-200 px-3 py-2">
            <div>
              <StatusBadge tone="info">{ORDER_TYPE_LABELS[order.orderType]}</StatusBadge>{' '}
              <span className="text-sm font-medium">{order.description}</span>
              <span className="ml-2 text-xs text-slate-400">{ORDER_PRIORITY_LABELS[order.priority]}</span>
            </div>
            <div className="flex items-center gap-2">
              <StatusBadge tone={order.status === 4 ? 'danger' : order.status === 3 ? 'success' : 'neutral'}>
                {ORDER_STATUS_LABELS[order.status]}
              </StatusBadge>
              {canManage && order.status === 1 && (order.orderType === ORDER_TYPES.Lab || order.orderType === ORDER_TYPES.Radiology) && (
                <Button
                  variant="secondary"
                  onClick={() =>
                    navigate(order.orderType === ORDER_TYPES.Lab ? '/lab/orders' : '/rad/orders', {
                      state: {
                        opOrder: {
                          opVisitId: visitId,
                          clinicalOrderId: order.id,
                          patientId: workspace.visit.patientId,
                          patientName: workspace.visit.patientName,
                          patientUhid: workspace.visit.patientUhid,
                        },
                      },
                    })
                  }
                >
                  Order in {order.orderType === ORDER_TYPES.Lab ? 'Lab' : 'Radiology'}
                </Button>
              )}
              {canManage && order.status === 1 && (
                <Button variant="ghost" className="text-red-600" onClick={() => run(() => cancelOrder.mutateAsync(order.id))}>
                  Cancel
                </Button>
              )}
            </div>
          </div>
        ))}
        {workspace.orders.length === 0 && <p className="text-sm text-slate-400">No orders placed.</p>}
      </div>
    </div>
  )
}

function ReferralTab({ workspace, canManage }: { workspace: WorkspaceData; canManage: boolean }) {
  const run = useRun()
  const visitId = workspace.visit.id
  const addReferral = useAddReferral(visitId)
  const deleteReferral = useDeleteReferral(visitId)
  const saveFollowUp = useSaveFollowUp(visitId)
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })

  const [direction, setDirection] = useState<ReferralDirection>(REFERRAL_DIRECTIONS.Internal)
  const [referredToDoctorId, setReferredToDoctorId] = useState('')
  const [externalDetails, setExternalDetails] = useState('')
  const [reason, setReason] = useState('')
  const [referralErrors, setReferralErrors] = useState<FieldErrors>({})
  const referralToast = useToast()

  const [followUpDate, setFollowUpDate] = useState(workspace.followUp?.followUpDate ?? '')
  const [followUpNotes, setFollowUpNotes] = useState(workspace.followUp?.notes ?? '')

  useEffect(() => {
    setFollowUpDate(workspace.followUp?.followUpDate ?? '')
    setFollowUpNotes(workspace.followUp?.notes ?? '')
  }, [workspace.followUp]);

  const submitReferral = async () => {
    // Mirrors AddVisitReferralRequestValidator: Internal needs a doctor, External needs the
    // destination text. Neither was checked here, so an Internal referral with no doctor picked
    // posted `Number('')` — a 0 that clears the server's NotNull() rule and then breaks on the FK.
    const errors = compactErrors({
      referredToDoctorId:
        direction === REFERRAL_DIRECTIONS.Internal ? requiredSelect(referredToDoctorId, 'Referred to') : undefined,
      externalDetails:
        direction === REFERRAL_DIRECTIONS.External ? requiredText(externalDetails, 'Referred to (external)') : undefined,
      reason: requiredText(reason, 'Reason'),
    })
    setReferralErrors(errors)
    if (hasErrors(errors)) return
    try {
      await addReferral.mutateAsync({
        direction,
        referredToDoctorId: direction === REFERRAL_DIRECTIONS.Internal ? Number(referredToDoctorId) : null,
        externalDetails: direction === REFERRAL_DIRECTIONS.External ? externalDetails : null,
        reason,
      })
      setReason('')
      setExternalDetails('')
      setReferralErrors({})
    } catch (err) {
      referralToast.error(errorMessage(err))
    }
  }

  return (
    <div className="grid gap-4 md:grid-cols-2">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <p className="mb-3 text-sm font-semibold text-slate-700">Referral</p>
        {canManage && (
          <div className="mb-3 space-y-2 border-b border-slate-100 pb-3">
            <Select
              label="Direction"
              options={Object.entries(REFERRAL_DIRECTION_LABELS).map(([v, l]) => ({ value: v, label: l }))}
              value={direction}
              onChange={(e) => setDirection(Number(e.target.value) as ReferralDirection)}
            />
            {direction === REFERRAL_DIRECTIONS.Internal ? (
              <Select
                label="Referred to"
                required
                error={referralErrors.referredToDoctorId}
                placeholder="Select doctor…"
                options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
                value={referredToDoctorId}
                onChange={(e) => setReferredToDoctorId(e.target.value)}
              />
            ) : (
              <Input
                label="Referred to (external)"
                required
                error={referralErrors.externalDetails}
                value={externalDetails}
                onChange={(e) => setExternalDetails(e.target.value)}
              />
            )}
            <Input
              label="Reason"
              required
              error={referralErrors.reason}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
            />
            <Button loading={addReferral.isPending} onClick={submitReferral}>
              Add referral
            </Button>
          </div>
        )}
        <div className="space-y-2">
          {workspace.referrals.map((r) => (
            <ReferralItem key={r.id} visitId={visitId} referral={r} canManage={canManage} onDelete={() => run(() => deleteReferral.mutateAsync(r.id))} />
          ))}
          {workspace.referrals.length === 0 && <p className="text-sm text-slate-400">No referrals.</p>}
        </div>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <div className="mb-3 flex items-center justify-between">
          <p className="text-sm font-semibold text-slate-700">Follow-up advice</p>
          {canManage && (
            <Button
              loading={saveFollowUp.isPending}
              disabled={!followUpDate}
              onClick={() => run(() => saveFollowUp.mutateAsync({ followUpDate, notes: followUpNotes || null }))}
            >
              Save
            </Button>
          )}
        </div>
        <Input label="Follow-up date" type="date" value={followUpDate} disabled={!canManage} onChange={(e) => setFollowUpDate(e.target.value)} />
        <div className="mt-2">
          <Input label="Notes" value={followUpNotes} disabled={!canManage} onChange={(e) => setFollowUpNotes(e.target.value)} />
        </div>
        <p className="mt-2 text-xs text-slate-400">
          To actually book the follow-up slot, use Appointments → Book Appointment with this patient and doctor.
        </p>
      </div>
    </div>
  )
}

/** "Doctor Opinion Entry" — an Internal referral's cross-consult response, entered inline here
 * (any authorized staff/doctor) or via the referred-to doctor's own Doctor Portal inbox. */
function ReferralItem({
  visitId, referral, canManage, onDelete,
}: { visitId: number; referral: VisitReferral; canManage: boolean; onDelete: () => void }) {
  const saveOpinion = useSaveReferralOpinion(visitId)
  const [entering, setEntering] = useState(false)
  const [opinionNotes, setOpinionNotes] = useState(referral.opinionNotes ?? '')

  const isInternal = referral.direction === REFERRAL_DIRECTIONS.Internal

  const submit = async () => {
    if (!opinionNotes.trim()) return
    await saveOpinion.mutateAsync({ referralId: referral.id, opinionNotes })
    setEntering(false)
  }

  return (
    <div className="rounded-md border border-slate-200 px-3 py-2 text-sm">
      <div className="flex items-center justify-between">
        <div>
          <StatusBadge tone="info">{REFERRAL_DIRECTION_LABELS[referral.direction]}</StatusBadge>{' '}
          {referral.referredToDoctorName ?? referral.externalDetails} — {referral.reason}
        </div>
        {canManage && (
          <Button variant="ghost" className="text-red-600" onClick={onDelete}>
            Remove
          </Button>
        )}
      </div>

      {isInternal && referral.opinionEnteredAt && (
        <p className="mt-2 rounded-md bg-slate-50 px-3 py-2 text-slate-600">
          <span className="font-medium text-slate-700">Opinion:</span> {referral.opinionNotes}
        </p>
      )}

      {isInternal && !referral.opinionEnteredAt && canManage && (
        entering ? (
          <div className="mt-2 space-y-2">
            <Input label="Opinion" value={opinionNotes} onChange={(e) => setOpinionNotes(e.target.value)} />
            <div className="flex gap-2">
              <Button loading={saveOpinion.isPending} disabled={!opinionNotes.trim()} onClick={submit}>
                Save opinion
              </Button>
              <Button variant="secondary" onClick={() => setEntering(false)}>
                Cancel
              </Button>
            </div>
          </div>
        ) : (
          <Button variant="ghost" className="mt-2" onClick={() => setEntering(true)}>
            Enter opinion
          </Button>
        )
      )}
    </div>
  )
}
