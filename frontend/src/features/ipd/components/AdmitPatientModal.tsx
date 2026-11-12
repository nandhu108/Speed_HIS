import { useEffect, useState } from 'react'
import { errorMessage } from '../../../api/axios'
import {
  compactErrors,
  hasErrors,
  requiredSelect,
  requiredText,
  type FieldErrors,
} from '../../../lib/formValidation'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { Select } from '../../../components/ui/Select'
import { useToast } from '../../../components/ui/Toast'
import { usePaymentModes } from '../../billing/api'
import { doctorsForDepartment, useClinicalDepartments, useConsentTemplates, useDoctors } from '../../masters/api'
import { PatientPicker } from '../../appointments/components/PatientPicker'
import { usePatients } from '../../patients/api'
import { PatientPhotoCapture } from '../../patients/components/PatientPhotoCapture'
import type { PatientListItem } from '../../patients/types'
import { useAdmitPatient, useBedBoard } from '../api'
import type { AdmissionType } from '../types'

/** Prefill for "Admit to IP" opened from the OP consultation workspace, an ER visit's Assessment tab, or the Convert-to-IP launchpad — a source visit to link, not a full patient record. */
export interface AdmitFromPrefill {
  patientId: number
  patientName: string
  patientUhid: string
  opVisitId?: number
  emergencyVisitId?: number
  admissionRequestId?: number
}

/**
 * The Admit Patient form as a standalone modal — deliberately not tied to navigation, so it can
 * be opened in place on whichever page the "Admit to IP" / "Convert" action lives (Admissions
 * list, OP workspace, ER visit, Convert-to-IP launchpad) without leaving that page. Only a
 * successful admit calls `onAdmitted`; Cancel/close just closes the modal in place.
 */
export function AdmitPatientModal({
  prefill, onClose, onAdmitted,
}: { prefill?: AdmitFromPrefill; onClose: () => void; onAdmitted: (id: number) => void }) {
  const admit = useAdmitPatient()
  const toast = useToast()
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const { data: clinicalDepartments = [] } = useClinicalDepartments()
  const { data: bedBoard = [] } = useBedBoard()
  const { data: consentTemplates = [] } = useConsentTemplates()
  const { data: paymentModes = [] } = usePaymentModes()
  const vacantBeds = bedBoard.filter((b) => b.status === 1)

  // A prefilled admission (from OP visit / ER visit) already knows the patient — resolve the
  // full PatientListItem by UHID (unique) instead of re-running the search picker.
  const { data: prefillPatients } = usePatients({ page: 1, pageSize: 1, search: prefill?.patientUhid }, { enabled: !!prefill })
  const [patient, setPatient] = useState<PatientListItem | null>(null)
  useEffect(() => {
    if (prefill && !patient && prefillPatients?.items[0]) setPatient(prefillPatients.items[0])
  }, [prefill, patient, prefillPatients])

  const [departmentId, setDepartmentId] = useState('')
  const [primaryConsultantId, setPrimaryConsultantId] = useState('')
  const departmentName = clinicalDepartments.find((d) => String(d.id) === departmentId)?.name
  const consultantOptions = doctorsForDepartment(doctorsResult?.items ?? [], departmentName)
  const [bedId, setBedId] = useState('')
  const [admissionType, setAdmissionType] = useState<AdmissionType>(1)
  const [diagnosis, setDiagnosis] = useState('')
  const [consentTemplateId, setConsentTemplateId] = useState('')
  const [consentSignedBy, setConsentSignedBy] = useState('')
  const [consentRelationship, setConsentRelationship] = useState('')
  const [depositAmount, setDepositAmount] = useState('')
  const [depositPaymentModeId, setDepositPaymentModeId] = useState('')
  const [depositReference, setDepositReference] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  /**
   * Mirrors AdmitPatientRequestValidator plus the two conditional rules AdmissionService
   * enforces itself: a consent template needs a signatory, and a deposit needs a payment
   * mode. Both used to surface only as a server error after the modal was fully filled in.
   */
  const validate = (): FieldErrors =>
    compactErrors({
      patient: patient ? undefined : 'Select a patient.',
      departmentId: requiredSelect(departmentId, 'Department'),
      primaryConsultantId: requiredSelect(primaryConsultantId, 'Primary consultant'),
      bedId: requiredSelect(bedId, 'Bed'),
      consentSignedBy: consentTemplateId ? requiredText(consentSignedBy, 'Signed by') : undefined,
      depositPaymentModeId:
        Number(depositAmount) > 0 ? requiredSelect(depositPaymentModeId, 'Payment mode') : undefined,
      depositAmount: Number(depositAmount) < 0 ? 'Deposit cannot be negative.' : undefined,
    })

  const submit = async () => {
    setError(null)
    const errors = validate()
    setFieldErrors(errors)
    if (hasErrors(errors)) {
      setError('Check the highlighted fields below.')
      return
    }
    try {
      const id = await admit.mutateAsync({
        patientId: patient!.id,
        departmentId: Number(departmentId),
        primaryConsultantId: Number(primaryConsultantId),
        bedId: Number(bedId),
        admissionType,
        provisionalDiagnosis: diagnosis || null,
        referringOpVisitId: prefill?.opVisitId ?? null,
        referringEmergencyVisitId: prefill?.emergencyVisitId ?? null,
        secondaryConsultantIds: [],
        consentTemplateId: consentTemplateId ? Number(consentTemplateId) : null,
        consentSignedByName: consentSignedBy || null,
        consentRelationshipToPatient: consentRelationship || null,
        initialDepositAmount: depositAmount ? Number(depositAmount) : 0,
        depositPaymentModeId: depositPaymentModeId ? Number(depositPaymentModeId) : null,
        depositReferenceNumber: depositReference || null,
        admissionRequestId: prefill?.admissionRequestId ?? null,
      })
      toast.success('Patient admitted')
      onAdmitted(id)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Admit patient"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={admit.isPending}>Admit</Button>
        </>
      }
    >
      <div className="max-h-[65vh] space-y-3 overflow-y-auto">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        {prefill ? (
          <div className="rounded-md border border-primary-200 bg-primary-50 px-3 py-2 text-sm text-primary-800">
            Converting {prefill.opVisitId ? 'OP visit' : 'ER visit'} for{' '}
            <span className="font-medium">{patient?.fullName ?? prefill.patientName}</span>{' '}
            <span className="font-mono text-xs">({prefill.patientUhid})</span> to an IP admission.
          </div>
        ) : (
          <div>
            <PatientPicker selected={patient} onSelect={setPatient} />
            {fieldErrors.patient && (
              <p role="alert" className="mt-1 text-xs font-medium text-crit">
                {fieldErrors.patient}
              </p>
            )}
          </div>
        )}
        {patient && (
          <div className="rounded-md border border-slate-200 p-3">
            <p className="mb-2 text-xs font-semibold uppercase text-slate-500">Patient photo (optional)</p>
            <PatientPhotoCapture patientId={patient.id} personLabel={patient.fullName} compact />
          </div>
        )}
        <Select
          label="Department"
          required
          error={fieldErrors.departmentId}
          placeholder="Select a department…"
          value={departmentId}
          onChange={(e) => {
            setDepartmentId(e.target.value)
            setPrimaryConsultantId('')
          }}
          options={clinicalDepartments.map((d) => ({ value: d.id, label: d.name }))}
        />
        <Select
          label="Primary consultant"
          required
          error={fieldErrors.primaryConsultantId}
          placeholder="Select…"
          value={primaryConsultantId}
          onChange={(e) => setPrimaryConsultantId(e.target.value)}
          options={consultantOptions.map((d) => ({ value: d.id, label: d.fullName }))}
        />
        <Select
          label="Bed (vacant only)"
          required
          error={fieldErrors.bedId}
          placeholder="Select…"
          value={bedId}
          onChange={(e) => setBedId(e.target.value)}
          options={vacantBeds.map((b) => ({ value: b.bedId, label: `${b.wardName}/${b.roomName}/${b.bedCode} (${b.roomTypeName})` }))}
        />
        <Select
          label="Admission type"
          value={admissionType}
          onChange={(e) => setAdmissionType(Number(e.target.value) as AdmissionType)}
          options={[{ value: 1, label: 'Planned' }, { value: 2, label: 'Emergency' }, { value: 3, label: 'Referral' }]}
        />
        <Input label="Provisional diagnosis" value={diagnosis} onChange={(e) => setDiagnosis(e.target.value)} />

        <div className="rounded-md border border-slate-200 p-3">
          <p className="mb-2 text-xs font-semibold uppercase text-slate-500">Admission consent (optional)</p>
          <Select
            label="Consent template"
            placeholder="None"
            value={consentTemplateId}
            onChange={(e) => setConsentTemplateId(e.target.value)}
            options={consentTemplates.map((c) => ({ value: c.id, label: c.title }))}
          />
          {consentTemplateId && (
            <div className="mt-2 grid grid-cols-2 gap-2">
              <Input
                label="Signed by"
                required
                error={fieldErrors.consentSignedBy}
                value={consentSignedBy}
                onChange={(e) => setConsentSignedBy(e.target.value)}
              />
              <Input label="Relationship (if not self)" value={consentRelationship} onChange={(e) => setConsentRelationship(e.target.value)} />
            </div>
          )}
        </div>

        <div className="rounded-md border border-slate-200 p-3">
          <p className="mb-2 text-xs font-semibold uppercase text-slate-500">Initial deposit (optional)</p>
          <div className="grid grid-cols-2 gap-2">
            <Input
              label="Amount"
              type="number"
              min={0}
              error={fieldErrors.depositAmount}
              value={depositAmount}
              onChange={(e) => setDepositAmount(e.target.value)}
            />
            <Select
              label="Payment mode"
              required={Number(depositAmount) > 0}
              error={fieldErrors.depositPaymentModeId}
              placeholder="Select…"
              value={depositPaymentModeId}
              onChange={(e) => setDepositPaymentModeId(e.target.value)}
              options={paymentModes.map((m) => ({ value: m.id, label: m.name }))}
            />
          </div>
          {depositAmount && (
            <div className="mt-2">
              <Input label="Reference number" value={depositReference} onChange={(e) => setDepositReference(e.target.value)} />
            </div>
          )}
        </div>
      </div>
    </Modal>
  )
}
