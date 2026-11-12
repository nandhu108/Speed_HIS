import { useQueries } from '@tanstack/react-query'
import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, errorMessage } from '../../../api/axios'
import { endpoints } from '../../../api/endpoints'
import type { ApiResponse } from '../../../api/types/api'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { compactErrors, hasErrors, requiredSelect, requiredText, type FieldErrors } from '../../../lib/formValidation'
import { Tabs } from '../../masters/components/Tabs'
import { useAreas, useConsentTemplates, useSimpleMaster, useReferrers } from '../../masters/api'
import { useAuth } from '../../auth/AuthContext'
import {
  downloadAttachment,
  useAddAllergy,
  useAddClinicalNote,
  useAddFamilyHistory,
  useAddImmunization,
  useAddMedicalHistory,
  useAddProblem,
  useAddSurgicalHistory,
  useAllergies,
  useAttachments,
  useClinicalNotes,
  useDeactivateAllergy,
  useDischargeSummaryArchive,
  useFamilyHistory,
  useImmunizations,
  useInvestigationHistory,
  useMedicalHistory,
  usePrescriptionHistory,
  useProblemList,
  useProcedureHistory,
  useResolveProblem,
  useResultsTrend,
  useSurgicalHistory,
  useTimeline,
  useUploadAttachment,
} from '../../emr/api'
import { ResultsTrendChart } from '../../emr/components/ResultsTrendChart'
import {
  ALLERGEN_TYPE_LABELS,
  ALLERGY_SEVERITY_LABELS,
  PROBLEM_STATUS_LABELS,
  TIMELINE_EVENT_LABELS,
  type AllergenType,
  type AllergyEntry,
  type AllergySeverity,
} from '../../emr/types'
import { openLabReport, useLabTests } from '../../lab/api'
import { useWorkspace } from '../../opd/api'
import {
  DIAGNOSIS_TYPE_LABELS,
  OP_VISIT_STATUS_LABELS,
  ORDER_PRIORITY_LABELS,
  ORDER_STATUS_LABELS,
  ORDER_TYPE_LABELS,
  VISIT_TYPE_LABELS,
  type ConsultationWorkspace,
  type VisitVitals,
} from '../../opd/types'
import {
  downloadPatientDocument,
  openRegistrationCard,
  useAddPatientDocument,
  useBlockPatient,
  useCaptureConsent,
  useMergePatient,
  usePatient,
  usePatientConsents,
  usePatientDocuments,
  usePatientDuplicates,
  usePatientPhotoUrl,
  useUnblockPatient,
  useUpdatePatient,
  useAbhaProfile,
  useInitiateAbha,
  useCompleteAbha,
  useUnlinkAbha,
} from '../api'
import { PatientPhotoCapture } from '../components/PatientPhotoCapture'
import {
  CONTACT_TYPE_LABELS,
  CONTACT_TYPES,
  DOCUMENT_TYPE_LABELS,
  DOCUMENT_TYPES,
  GENDER_LABELS,
  IDENTIFIER_TYPE_LABELS,
  IDENTIFIER_TYPES,
  ABDM_METHODS,
  ABDM_METHOD_LABELS,
  ABHA_STATUS_LABELS,
  type AbdmMethod,
  type ContactType,
  type DocumentType,
  type Gender,
  type IdentifierType,
  type PatientDetail,
  type SavePatientContact,
  type SavePatientIdentifier,
  type SavePatientInsurancePolicy,
  type UpdatePatientPayload,
} from '../types'
import { PatientAbdmWorkspace } from '../../abdm/components/PatientAbdmWorkspace'

const tabs = [
  { key: 'overview', label: 'Overview' },
  { key: 'contacts', label: 'Contacts' },
  { key: 'documents', label: 'Documents' },
  { key: 'consents', label: 'Consents' },
  { key: 'emr', label: 'EMR' },
  { key: 'abdm', label: 'ABDM (M1/M2/M3)' },
] as const
type TabKey = (typeof tabs)[number]['key']

function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

export function PatientDetailPage() {
  const { id } = useParams<{ id: string }>()
  const patientId = id ? Number(id) : null
  const { hasPermission } = useAuth()
  const canUpdate = hasPermission('PATIENT.PATIENT.UPDATE')
  const canMerge = hasPermission('PATIENT.PATIENT.MERGE')
  const canBlock = hasPermission('PATIENT.PATIENT.BLOCK')
  const canManageAbha = hasPermission('PATIENT.ABHA.MANAGE')
  const canViewAbha = hasPermission('PATIENT.ABHA.VIEW')

  const [tab, setTab] = useState<TabKey>('overview')
  const [editing, setEditing] = useState(false)
  const [merging, setMerging] = useState(false)
  const [blocking, setBlocking] = useState(false)

  const { data: patient, isLoading } = usePatient(patientId)
  const [photoUrl, refreshPhoto] = usePatientPhotoUrl(patientId, !!patient?.photoPath)
  const unblockPatient = useUnblockPatient()

  if (isLoading || !patient) {
    return <Spinner label="Loading patient…" />
  }

  const fullName = [patient.firstName, patient.middleName, patient.lastName].filter(Boolean).join(' ')

  return (
    <>
      <PageHeader
        title={fullName}
        subtitle={`UHID ${patient.uhid} · ${patient.registeredBranchName}`}
        backTo="/patients"
        actions={
          <div className="flex gap-2">
            <Button variant="secondary" onClick={() => openRegistrationCard(patient.id)}>
              Print card
            </Button>
            {canMerge && (
              <Button variant="secondary" onClick={() => setMerging(true)}>
                Merge
              </Button>
            )}
            {canBlock && !patient.isBlocked && (
              <Button variant="secondary" className="text-red-600" onClick={() => setBlocking(true)}>
                Block
              </Button>
            )}
            {canBlock && patient.isBlocked && (
              <Button
                variant="secondary"
                loading={unblockPatient.isPending}
                onClick={async () => {
                  if (!window.confirm('Unblock this patient?')) return
                  try {
                    await unblockPatient.mutateAsync(patient.id)
                  } catch (err) {
                    window.alert(errorMessage(err))
                  }
                }}
              >
                Unblock
              </Button>
            )}
            {canUpdate && <Button onClick={() => setEditing(true)}>Edit</Button>}
          </div>
        }
      />

      {patient.isBlocked && (
        <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-2.5 text-sm text-red-700">
          <span className="font-semibold">Blocked</span>
          {patient.blockedReason && <span> — {patient.blockedReason}</span>}
          {patient.blockedByUserName && (
            <span className="text-red-500">
              {' '}
              (by {patient.blockedByUserName}
              {patient.blockedAt ? `, ${new Date(patient.blockedAt).toLocaleString()}` : ''})
            </span>
          )}
        </div>
      )}

      <Tabs tabs={tabs} active={tab} onChange={setTab} />

      {tab === 'overview' && (
        <div className="grid gap-4 md:grid-cols-3">
          <div className="rounded-lg border border-slate-200 bg-white p-4 md:col-span-2">
            <dl className="grid grid-cols-2 gap-x-4 gap-y-3 text-sm">
              <Field label="Gender / Age" value={`${GENDER_LABELS[patient.gender]} / ${patient.ageDisplay}`} />
              <Field label="Blood group" value={patient.bloodGroup ?? '—'} />
              <Field label="Mobile" value={patient.mobile} />
              <Field label="Alternate mobile" value={patient.alternateMobile ?? '—'} />
              <Field label="Email" value={patient.email ?? '—'} />
              <Field label="Nationality" value={patient.nationalityName ?? '—'} />
              <Field label="Occupation" value={patient.occupation ?? '—'} />
              <Field label="Patient category" value={patient.patientCategoryName ?? '—'} />
              <Field label="Referrer" value={patient.referrerName ?? '—'} />
              <Field
                label="Address"
                value={
                  [patient.address, patient.cityName, patient.stateName, patient.pincode].filter(Boolean).join(', ') ||
                  '—'
                }
              />
              <Field
                label="Status"
                value={
                  <div className="flex gap-1.5">
                    <StatusBadge tone={patient.isActive ? 'success' : 'neutral'}>{patient.isActive ? 'Active' : 'Inactive'}</StatusBadge>
                    {patient.isBlocked && <StatusBadge tone="danger">Blocked</StatusBadge>}
                  </div>
                }
              />
            </dl>

            <div className="mt-4 border-t border-slate-100 pt-4">
              <p className="mb-2 text-sm font-semibold text-slate-700">Aadhaar / ABHA</p>
              {patient.identifiers.length === 0 && <p className="text-sm text-slate-400">None captured.</p>}
              <div className="flex flex-wrap gap-2">
                {patient.identifiers.map((identifier) => (
                  <StatusBadge key={identifier.id} tone={identifier.isVerified ? 'success' : 'neutral'}>
                    {IDENTIFIER_TYPE_LABELS[identifier.type]}: {identifier.value}
                    {!identifier.isVerified && ' (unverified)'}
                  </StatusBadge>
                ))}
              </div>
            </div>

            {(canViewAbha || canManageAbha) && (
              <AbhaPanel patientId={patient.id} canManage={canManageAbha} />
            )}
          </div>

          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="mb-2 text-sm font-semibold text-slate-700">Photo</p>
            {canUpdate ? (
              <PatientPhotoCapture patientId={patientId} personLabel={fullName} compact onUploaded={refreshPhoto} />
            ) : photoUrl ? (
              <img src={photoUrl} alt={fullName} className="h-40 w-40 rounded-md object-cover" />
            ) : (
              <div className="flex h-40 w-40 items-center justify-center rounded-md bg-slate-100 text-xs text-slate-400">
                No photo
              </div>
            )}
          </div>
        </div>
      )}

      {tab === 'contacts' && (
        <div className="rounded-lg border border-slate-200 bg-white p-4">
          {patient.contacts.length === 0 && <p className="text-sm text-slate-400">No guardian or attender on file.</p>}
          <div className="space-y-2">
            {patient.contacts.map((contact) => (
              <div key={contact.id} className="flex items-center justify-between rounded-md border border-slate-200 px-3 py-2">
                <div>
                  <p className="text-sm font-medium text-slate-800">
                    {contact.name} {contact.isPrimary && <StatusBadge tone="info">Primary</StatusBadge>}
                  </p>
                  <p className="text-xs text-slate-500">
                    {CONTACT_TYPE_LABELS[contact.contactType]}
                    {contact.relationship ? ` · ${contact.relationship}` : ''}
                    {contact.phone ? ` · ${contact.phone}` : ''}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {tab === 'documents' && <DocumentsTab patientId={patient.id} canUpdate={canUpdate} />}
      {tab === 'consents' && <ConsentsTab patientId={patient.id} canUpdate={canUpdate} />}
      {tab === 'emr' && <EmrTab patientId={patient.id} patient={patient} photoUrl={photoUrl} />}
      {tab === 'abdm' && (
        <PatientAbdmWorkspace
          patientId={patient.id}
          patientUhid={patient.uhid}
          canManage={canManageAbha}
        />
      )}

      {editing && (
        <EditPatientModal patient={patient} onClose={() => setEditing(false)} />
      )}
      {merging && canMerge && <MergeModal patientId={patient.id} onClose={() => setMerging(false)} />}
      {blocking && canBlock && <BlockModal patientId={patient.id} onClose={() => setBlocking(false)} />}
    </>
  )
}

function AbhaPanel({ patientId, canManage }: { patientId: number; canManage: boolean }) {
  const { data: abha, isLoading } = useAbhaProfile(patientId)
  const unlink = useUnlinkAbha(patientId)
  const [linking, setLinking] = useState(false)

  const doUnlink = async () => {
    const reason = window.prompt('Reason for unlinking this ABHA?')
    if (!reason?.trim()) return
    try {
      await unlink.mutateAsync(reason.trim())
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  return (
    <div className="mt-4 border-t border-slate-100 pt-4">
      <div className="mb-2 flex items-center justify-between">
        <p className="text-sm font-semibold text-slate-700">ABHA / ABDM</p>
        {canManage && !abha && !isLoading && (
          <Button variant="secondary" onClick={() => setLinking(true)}>
            Link ABHA
          </Button>
        )}
      </div>
      {isLoading ? (
        <p className="text-sm text-slate-400">Loading…</p>
      ) : abha ? (
        <div className="space-y-1.5">
          <div className="flex flex-wrap items-center gap-2">
            <StatusBadge tone="success">{abha.abhaNumber}</StatusBadge>
            <StatusBadge tone={abha.kycVerified ? 'success' : 'neutral'}>
              {ABHA_STATUS_LABELS[abha.status] ?? 'Linked'}
              {abha.kycVerified ? ' · KYC' : ''}
            </StatusBadge>
          </div>
          {abha.abhaAddress && <p className="text-xs text-slate-500">Address: {abha.abhaAddress}</p>}
          {canManage && (
            <button
              type="button"
              className="text-xs font-medium text-red-600 hover:underline"
              onClick={doUnlink}
            >
              Unlink
            </button>
          )}
        </div>
      ) : (
        <p className="text-sm text-slate-400">
          No ABHA linked.{canManage ? ' Use “Link ABHA” to verify via ABDM.' : ''}
        </p>
      )}
      {linking && <AbhaModal patientId={patientId} onClose={() => setLinking(false)} />}
    </div>
  )
}

function AbhaModal({ patientId, onClose }: { patientId: number; onClose: () => void }) {
  const initiate = useInitiateAbha(patientId)
  const complete = useCompleteAbha(patientId)
  const [step, setStep] = useState<'request' | 'otp'>('request')
  const [method, setMethod] = useState<AbdmMethod>(ABDM_METHODS.MobileOtp)
  const [identifier, setIdentifier] = useState('')
  const [sessionId, setSessionId] = useState<number | null>(null)
  const [maskedTarget, setMaskedTarget] = useState<string | null>(null)
  const [otp, setOtp] = useState('')
  const [error, setError] = useState<string | null>(null)

  const requestOtp = async () => {
    setError(null)
    if (!identifier.trim()) {
      setError('Enter the Aadhaar, mobile or ABHA to verify.')
      return
    }
    try {
      const session = await initiate.mutateAsync({ method, identifier: identifier.trim() })
      setSessionId(session.sessionId)
      setMaskedTarget(session.maskedTarget)
      setStep('otp')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const submitOtp = async () => {
    setError(null)
    if (sessionId === null) return
    try {
      await complete.mutateAsync({ sessionId, otp: otp.trim() })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Link ABHA (ABDM)"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          {step === 'request' ? (
            <Button onClick={requestOtp} loading={initiate.isPending}>
              Send OTP
            </Button>
          ) : (
            <Button onClick={submitOtp} loading={complete.isPending}>
              Verify &amp; link
            </Button>
          )}
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        {step === 'request' ? (
          <>
            <Select
              label="Verify using"
              required
              value={String(method)}
              onChange={(e) => setMethod(Number(e.target.value) as AbdmMethod)}
              options={Object.entries(ABDM_METHOD_LABELS).map(([value, label]) => ({ value, label }))}
            />
            <Input
              label={
                method === ABDM_METHODS.AadhaarOtp
                  ? 'Aadhaar number'
                  : method === ABDM_METHODS.MobileOtp
                    ? 'Registered mobile'
                    : 'ABHA number / address'
              }
              required
              value={identifier}
              onChange={(e) => setIdentifier(e.target.value)}
            />
            <p className="text-xs text-slate-400">
              An OTP will be sent by ABDM to the linked mobile. Requires ABDM sandbox credentials configured on the server.
            </p>
          </>
        ) : (
          <>
            <p className="text-sm text-slate-600">
              Enter the OTP{maskedTarget ? ` sent to ${maskedTarget}` : ''}.
            </p>
            <Input label="OTP" required value={otp} onChange={(e) => setOtp(e.target.value)} />
          </>
        )}
      </div>
    </Modal>
  )
}

function BlockModal({ patientId, onClose }: { patientId: number; onClose: () => void }) {
  const block = useBlockPatient()
  const [reason, setReason] = useState('')

  const submit = async () => {
    if (!reason.trim()) return
    try {
      await block.mutateAsync({ patientId, reason })
      onClose()
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Block patient"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button variant="danger" onClick={submit} loading={block.isPending} disabled={!reason.trim()}>
            Block patient
          </Button>
        </>
      }
    >
      <p className="mb-3 text-sm text-slate-600">
        A blocked patient's record stays visible everywhere, but front-desk staff will see a warning on it (e.g. for
        non-payment or fraud). This can be reversed at any time via Unblock.
      </p>
      <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
    </Modal>
  )
}

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <dt className="text-xs text-slate-400">{label}</dt>
      <dd className="font-medium text-slate-800">{value}</dd>
    </div>
  )
}

function DocumentsTab({ patientId, canUpdate }: { patientId: number; canUpdate: boolean }) {
  const { data: documents = [], isLoading } = usePatientDocuments(patientId)
  const addDocument = useAddPatientDocument()
  const [documentType, setDocumentType] = useState<DocumentType>(DOCUMENT_TYPES.IdProof)
  const [description, setDescription] = useState('')

  return (
    <div className="space-y-4">
      {canUpdate && (
        <div className="flex flex-wrap items-end gap-2 rounded-lg border border-slate-200 bg-white p-4">
          <Select
            label="Document type"
            options={Object.entries(DOCUMENT_TYPE_LABELS).map(([value, label]) => ({ value, label }))}
            value={String(documentType)}
            onChange={(e) => setDocumentType(Number(e.target.value) as DocumentType)}
          />
          <Input label="Description" value={description} onChange={(e) => setDescription(e.target.value)} />
          <label>
            <span className="mb-1 block text-sm font-medium text-slate-700">File</span>
            <input
              type="file"
              className="block text-sm"
              onChange={async (e) => {
                const file = e.target.files?.[0]
                if (!file) return
                try {
                  await addDocument.mutateAsync({ patientId, file, documentType, description: description || null })
                  setDescription('')
                } catch (err) {
                  window.alert(errorMessage(err))
                }
                e.target.value = ''
              }}
            />
          </label>
        </div>
      )}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">File</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Size</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Uploaded</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {documents.map((doc) => (
              <tr key={doc.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5">{DOCUMENT_TYPE_LABELS[doc.documentType]}</td>
                <td className="px-4 py-2.5">
                  {doc.fileName}
                  {doc.description && <span className="ml-1 text-xs text-slate-400">({doc.description})</span>}
                </td>
                <td className="px-4 py-2.5 text-slate-500">{formatBytes(doc.sizeBytes)}</td>
                <td className="px-4 py-2.5 text-slate-500">{new Date(doc.uploadedAt).toLocaleString()}</td>
                <td className="px-4 py-2.5 text-right">
                  <button
                    type="button"
                    className="text-primary-600 hover:underline"
                    onClick={() => downloadPatientDocument(patientId, doc.id, doc.fileName)}
                  >
                    Download
                  </button>
                </td>
              </tr>
            ))}
            {!isLoading && documents.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                  No documents uploaded.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function ConsentsTab({ patientId, canUpdate }: { patientId: number; canUpdate: boolean }) {
  const { data: consents = [], isLoading } = usePatientConsents(patientId)
  const { data: templates = [] } = useConsentTemplates()
  const capture = useCaptureConsent()
  const [templateId, setTemplateId] = useState('')
  const [signedByName, setSignedByName] = useState('')
  const [relationship, setRelationship] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    const errors = compactErrors({
      templateId: requiredSelect(templateId, 'Consent template'),
      signedByName: requiredText(signedByName, 'Signed by'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await capture.mutateAsync({
        patientId,
        consentTemplateId: Number(templateId),
        signedByName,
        relationshipToPatient: relationship || null,
        scan: null,
      })
      setTemplateId('')
      setSignedByName('')
      setRelationship('')
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  return (
    <div className="space-y-4">
      {canUpdate && (
        <div className="flex flex-wrap items-end gap-2 rounded-lg border border-slate-200 bg-white p-4">
          <Select
            label="Consent template"
            required
            error={fieldErrors.templateId}
            placeholder="Select…"
            options={templates.map((t) => ({ value: t.id, label: t.title }))}
            value={templateId}
            onChange={(e) => setTemplateId(e.target.value)}
          />
          <Input
            label="Signed by"
            required
            error={fieldErrors.signedByName}
            value={signedByName}
            onChange={(e) => setSignedByName(e.target.value)}
          />
          <Input
            label="Relationship (if not patient)"
            value={relationship}
            onChange={(e) => setRelationship(e.target.value)}
          />
          <Button onClick={submit} loading={capture.isPending}>
            Capture consent
          </Button>
        </div>
      )}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Template</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Signed by</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Relationship</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Signed at</th>
            </tr>
          </thead>
          <tbody>
            {consents.map((consent) => (
              <tr key={consent.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-medium text-slate-700">{consent.templateTitle}</td>
                <td className="px-4 py-2.5">{consent.signedByName}</td>
                <td className="px-4 py-2.5 text-slate-500">{consent.relationshipToPatient ?? 'Self'}</td>
                <td className="px-4 py-2.5 text-slate-500">{new Date(consent.signedAt).toLocaleString()}</td>
              </tr>
            ))}
            {!isLoading && consents.length === 0 && (
              <tr>
                <td colSpan={4} className="px-4 py-10 text-center text-slate-400">
                  No consents recorded.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

interface EditFormState {
  salutationId: string
  firstName: string
  middleName: string
  lastName: string
  gender: Gender
  dobKnown: boolean
  dateOfBirth: string
  estimatedAge: string
  bloodGroup: string
  maritalStatusId: string
  languageId: string
  patientCaseTypeId: string
  patientHcTagId: string
  speciallyAbledCategoryId: string
  isVip: boolean
  smsOptIn: boolean
  occupation: string
  monthlyIncome: string
  motherName: string
  nationalId: string
  mobile: string
  alternateMobile: string
  whatsAppNumber: string
  email: string
  address: string
  permanentAddress: string
  isPermanentSameAsPresent: boolean
  countryId: string
  nationalityId: string
  stateId: string
  cityId: string
  areaId: string
  pincode: string
  patientCategoryId: string
  referrerId: string
  insuranceRequired: boolean
  remarks: string
  isActive: boolean
  contacts: SavePatientContact[]
  identifiers: SavePatientIdentifier[]
  insurancePolicies: SavePatientInsurancePolicy[]
}

// ── EMR ──────────────────────────────────────────────────────────────────
type EmrSubTab =
  | 'consultation'
  | 'notes'
  | 'orders'
  | 'results'
  | 'records'
  | 'attachments'
  | 'timeline'
  | 'histories'
  | 'allergies'
  | 'problems'
  | 'vitals'

/**
 * Pulls the real OPD "workspace" (vitals/complaints/exam/diagnoses/prescription/orders) for the
 * patient's most recent OP visits. EMR has no vitals storage of its own — this reuses OPD's
 * existing per-visit data (same query key as opd/api's useWorkspace, so results are shared/deduped).
 */
function useRecentOpWorkspaces(patientId: number, canViewOpd: boolean, count: number) {
  const { data: events = [] } = useTimeline(canViewOpd ? patientId : null)
  const visitIds = events.filter((e) => e.type === 1).slice(0, count).map((e) => e.linkId)
  const queries = useQueries({
    queries: visitIds.map((visitId) => ({
      queryKey: ['opd', 'workspace', visitId],
      queryFn: async () => {
        const { data } = await api.get<ApiResponse<ConsultationWorkspace>>(`${endpoints.opdVisits}/${visitId}/workspace`)
        return data.data!
      },
      enabled: canViewOpd,
    })),
  })
  const isLoading = canViewOpd && visitIds.length > 0 && queries.some((q) => q.isLoading)
  const workspaces = queries.map((q) => q.data).filter((w): w is ConsultationWorkspace => !!w)
  return { latestVisitId: visitIds[0] ?? null, workspaces, isLoading }
}

function EmrTab({ patientId, patient, photoUrl }: { patientId: number; patient: PatientDetail; photoUrl: string | null }) {
  const { hasPermission } = useAuth()
  const canViewOpd = hasPermission('OPD.VISIT.VIEW')
  const [sub, setSub] = useState<EmrSubTab>('consultation')
  const [error, setError] = useState<string | null>(null)
  const { data: allergies = [] } = useAllergies(patientId, true)
  const { workspaces, latestVisitId, isLoading: vitalsLoading } = useRecentOpWorkspaces(patientId, canViewOpd, 5)
  const fullName = [patient.firstName, patient.middleName, patient.lastName].filter(Boolean).join(' ')

  const subTabs: { key: EmrSubTab; label: string }[] = [
    { key: 'consultation', label: 'Consultation' },
    { key: 'notes', label: 'Notes' },
    { key: 'orders', label: 'Orders' },
    { key: 'results', label: 'Results' },
    { key: 'records', label: 'Prescriptions' },
    { key: 'attachments', label: 'Documents' },
    { key: 'timeline', label: 'Timeline' },
    { key: 'histories', label: 'Histories' },
    { key: 'allergies', label: 'Allergies' },
    { key: 'problems', label: 'Problem List' },
    { key: 'vitals', label: 'Vitals' },
  ]

  return (
    <div className="space-y-4">
      <EmrHeaderBar patient={patient} fullName={fullName} allergies={allergies} />

      <div className="grid gap-4 xl:grid-cols-[260px_1fr_300px]">
        {/* Left — patient summary panel */}
        <EmrPatientSummaryPanel
          patientId={patientId}
          patient={patient}
          photoUrl={photoUrl}
          allergies={allergies}
          latestVitals={workspaces[0]?.vitals ?? null}
          onViewAllVitals={() => setSub('vitals')}
        />

        {/* Center — EMR workspace tabs */}
        <div className="min-w-0 space-y-3">
          <div className="flex flex-wrap gap-4 overflow-x-auto rounded-t-lg border border-b-0 border-slate-200 bg-white px-3">
            {subTabs.map((t) => (
              <button
                key={t.key}
                type="button"
                onClick={() => setSub(t.key)}
                className={`whitespace-nowrap border-b-2 px-1 py-2.5 text-xs font-medium transition-colors ${
                  sub === t.key ? 'border-primary-600 text-primary-700' : 'border-transparent text-slate-500 hover:text-slate-700'
                }`}
              >
                {t.label}
              </button>
            ))}
          </div>
          {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
          {sub === 'consultation' && <ConsultationSubTab canViewOpd={canViewOpd} visitId={latestVisitId} />}
          {sub === 'notes' && <NotesSubTab patientId={patientId} setError={setError} />}
          {sub === 'orders' && <OrdersSubTab canViewOpd={canViewOpd} visitId={latestVisitId} />}
          {sub === 'results' && <ResultsSubTab patientId={patientId} />}
          {sub === 'records' && <RecordsSubTab patientId={patientId} />}
          {sub === 'attachments' && <AttachmentsSubTab patientId={patientId} setError={setError} />}
          {sub === 'timeline' && <TimelineSubTab patientId={patientId} />}
          {sub === 'histories' && <HistoriesSubTab patientId={patientId} setError={setError} />}
          {sub === 'allergies' && <AllergiesSubTab patientId={patientId} setError={setError} />}
          {sub === 'problems' && <ProblemListSubTab patientId={patientId} setError={setError} />}
          {sub === 'vitals' && <VitalsSubTab canViewOpd={canViewOpd} workspaces={workspaces} isLoading={vitalsLoading} />}
        </div>

        {/* Right — investigations / recent visits / documents / vitals trend */}
        <div className="space-y-4">
          <EmrInvestigationsPanel patientId={patientId} onViewAll={() => setSub('results')} />
          <EmrRecentVisitsPanel patientId={patientId} onViewAll={() => setSub('timeline')} />
          <EmrDocumentsPanel patientId={patientId} onViewAll={() => setSub('attachments')} />
          <EmrVitalsTrendPanel workspaces={workspaces} isLoading={vitalsLoading} onViewAll={() => setSub('vitals')} />
        </div>
      </div>
    </div>
  )
}

function EmrHeaderBar({ patient, fullName, allergies }: { patient: PatientDetail; fullName: string; allergies: AllergyEntry[] }) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 rounded-lg bg-slate-800 px-4 py-3 text-white">
      <div className="flex flex-wrap items-center gap-x-5 gap-y-1">
        <span className="text-sm font-semibold">EMR Workspace</span>
        <span className="text-xs text-slate-300">
          UHID <b className="font-semibold text-white">{patient.uhid}</b>
        </span>
        <span className="text-xs text-slate-300">
          Name <b className="font-semibold text-white">{fullName}</b>
        </span>
        <span className="text-xs text-slate-300">
          Age <b className="font-semibold text-white">{patient.ageDisplay}</b>
        </span>
        <span className="text-xs text-slate-300">
          Sex <b className="font-semibold text-white">{GENDER_LABELS[patient.gender]}</b>
        </span>
        <span className="text-xs text-slate-300">
          Phone <b className="font-semibold text-white">{patient.mobile}</b>
        </span>
        <span className="text-xs text-slate-300">
          Blood Group <b className="font-semibold text-white">{patient.bloodGroup ?? '—'}</b>
        </span>
        <span className="text-xs text-slate-300">
          Allergies <b className="font-semibold text-white">{allergies.length ? allergies.map((a) => a.allergenName).join(', ') : 'None'}</b>
        </span>
      </div>
      <div className="flex items-center gap-2">
        {allergies.length > 0 && <span className="rounded-md bg-red-600 px-2.5 py-1 text-xs font-semibold">Allergy Alert</span>}
        <button
          type="button"
          onClick={() => window.print()}
          className="rounded-md bg-white/10 px-2.5 py-1.5 text-xs font-medium hover:bg-white/20"
        >
          Print
        </button>
      </div>
    </div>
  )
}

function VitalField({ label, value }: { label: string; value: string | number | null }) {
  return (
    <div>
      <dt className="text-slate-400">{label}</dt>
      <dd className="font-medium text-slate-800">{value ?? '—'}</dd>
    </div>
  )
}

function EmrPatientSummaryPanel({
  patientId,
  patient,
  photoUrl,
  allergies,
  latestVitals,
  onViewAllVitals,
}: {
  patientId: number
  patient: PatientDetail
  photoUrl: string | null
  allergies: AllergyEntry[]
  latestVitals: VisitVitals | null
  onViewAllVitals: () => void
}) {
  const { data: problems = [] } = useProblemList(patientId, 1)
  const { data: prescriptions = [] } = usePrescriptionHistory(patientId)
  const latestRx = prescriptions[0]
  const fullName = [patient.firstName, patient.middleName, patient.lastName].filter(Boolean).join(' ')

  return (
    <div className="space-y-3 xl:sticky xl:top-4 xl:self-start">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-400">Patient Summary</p>
        <div className="flex flex-col items-center text-center">
          {photoUrl ? (
            <img src={photoUrl} alt={fullName} className="h-20 w-20 rounded-full object-cover" />
          ) : (
            <div className="flex h-20 w-20 items-center justify-center rounded-full bg-slate-100 text-xs text-slate-400">
              No photo
            </div>
          )}
          <p className="mt-2 text-sm font-semibold text-slate-800">{fullName}</p>
          <p className="text-xs text-slate-500">
            {patient.ageDisplay} · {GENDER_LABELS[patient.gender]}
          </p>
          <p className="text-xs text-slate-500">{patient.mobile}</p>
        </div>
        {allergies.length > 0 && (
          <div className="mt-3 rounded-md border border-red-200 bg-red-50 px-2.5 py-1.5">
            <p className="text-xs font-semibold text-red-800">⚠ Allergy : {allergies.map((a) => a.allergenName).join(', ')}</p>
          </div>
        )}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-400">Active Medications</p>
        {latestRx ? (
          <ul className="space-y-1 text-xs text-slate-700">
            {latestRx.items.map((i) => (
              <li key={i.drugId}>
                {i.drugName} — {i.dose} {i.frequency}
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-xs text-slate-400">None on file.</p>
        )}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-400">Chronic Conditions</p>
        {problems.length > 0 ? (
          <ul className="space-y-1 text-xs text-slate-700">
            {problems.map((p) => (
              <li key={p.id}>{p.diagnosisText}</li>
            ))}
          </ul>
        ) : (
          <p className="text-xs text-slate-400">None active.</p>
        )}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <div className="mb-2 flex items-center justify-between">
          <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Last Vitals</p>
          <button type="button" onClick={onViewAllVitals} className="text-xs text-primary-600 hover:underline">
            View all
          </button>
        </div>
        {latestVitals ? (
          <dl className="grid grid-cols-2 gap-x-2 gap-y-1.5 text-xs text-slate-700">
            <VitalField
              label="BP"
              value={
                latestVitals.bloodPressureSystolic && latestVitals.bloodPressureDiastolic
                  ? `${latestVitals.bloodPressureSystolic}/${latestVitals.bloodPressureDiastolic} mmHg`
                  : null
              }
            />
            <VitalField label="Pulse" value={latestVitals.pulseRate ? `${latestVitals.pulseRate} /min` : null} />
            <VitalField label="Temp" value={latestVitals.temperatureF ? `${latestVitals.temperatureF} °F` : null} />
            <VitalField label="SpO2" value={latestVitals.spO2 ? `${latestVitals.spO2} %` : null} />
            <VitalField label="Weight" value={latestVitals.weightKg ? `${latestVitals.weightKg} kg` : null} />
            <VitalField label="Height" value={latestVitals.heightCm ? `${latestVitals.heightCm} cm` : null} />
            <VitalField label="BMI" value={latestVitals.bmi ?? null} />
          </dl>
        ) : (
          <p className="text-xs text-slate-400">No vitals recorded yet.</p>
        )}
      </div>
    </div>
  )
}

function EmrInvestigationsPanel({ patientId, onViewAll }: { patientId: number; onViewAll: () => void }) {
  const { data: investigations = [] } = useInvestigationHistory(patientId)
  const recent = investigations.slice(0, 4)

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="mb-2 flex items-center justify-between">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Investigations</p>
        <button type="button" onClick={onViewAll} className="text-xs text-primary-600 hover:underline">
          View all
        </button>
      </div>
      <div className="mb-2 flex gap-1.5 text-xs">
        <span className="rounded-md bg-primary-50 px-2 py-1 font-medium text-primary-700">Lab ({investigations.length})</span>
        <span className="rounded-md bg-slate-50 px-2 py-1 text-slate-400" title="Not yet aggregated by EMR">
          Radiology (0)
        </span>
        <span className="rounded-md bg-slate-50 px-2 py-1 text-slate-400" title="Not yet aggregated by EMR">
          Other (0)
        </span>
      </div>
      <ul className="space-y-2">
        {recent.map((inv) => (
          <li key={inv.labOrderItemId} className="text-xs">
            <p className="font-medium text-slate-700">{inv.testName}</p>
            <p className="text-slate-400">
              {inv.orderDate} · {inv.resultSummary ?? inv.status}
            </p>
            {inv.resultSummary && (
              <button type="button" className="mt-0.5 text-primary-600 hover:underline" onClick={() => openLabReport(inv.labOrderId)}>
                View report
              </button>
            )}
          </li>
        ))}
        {recent.length === 0 && <li className="text-xs text-slate-400">No investigations recorded.</li>}
      </ul>
    </div>
  )
}

function EmrRecentVisitsPanel({ patientId, onViewAll }: { patientId: number; onViewAll: () => void }) {
  const { data: events = [] } = useTimeline(patientId)
  const visits = events.filter((e) => e.type === 1 || e.type === 2).slice(0, 4)

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="mb-2 flex items-center justify-between">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Recent Visits</p>
        <button type="button" onClick={onViewAll} className="text-xs text-primary-600 hover:underline">
          View all
        </button>
      </div>
      <ul className="space-y-2">
        {visits.map((e, i) => (
          <li key={`${e.type}-${e.linkId}-${i}`} className="text-xs">
            <p className="font-medium text-slate-700">{e.title}</p>
            <p className="text-slate-400">{new Date(e.at).toLocaleDateString()}</p>
          </li>
        ))}
        {visits.length === 0 && <li className="text-xs text-slate-400">No visits recorded.</li>}
      </ul>
    </div>
  )
}

function EmrDocumentsPanel({ patientId, onViewAll }: { patientId: number; onViewAll: () => void }) {
  const { data: attachments = [] } = useAttachments(patientId)
  const recent = attachments.slice(0, 5)

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="mb-2 flex items-center justify-between">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Documents</p>
        <button type="button" onClick={onViewAll} className="text-xs text-primary-600 hover:underline">
          View all
        </button>
      </div>
      <ul className="space-y-1.5">
        {recent.map((a) => (
          <li key={a.id}>
            <button
              type="button"
              className="text-left text-xs text-primary-600 hover:underline"
              onClick={() => downloadAttachment(patientId, a.id, a.fileName)}
            >
              {a.fileName}
            </button>
          </li>
        ))}
        {recent.length === 0 && <li className="text-xs text-slate-400">No documents uploaded.</li>}
      </ul>
    </div>
  )
}

function EmrVitalsTrendPanel({
  workspaces,
  isLoading,
  onViewAll,
}: {
  workspaces: ConsultationWorkspace[]
  isLoading: boolean
  onViewAll: () => void
}) {
  const points = [...workspaces]
    .reverse()
    .filter((w) => w.vitals?.bloodPressureSystolic != null)
    .map((w) => ({ date: w.visit.visitDate, value: w.vitals!.bloodPressureSystolic! }))

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="mb-2 flex items-center justify-between">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Vitals Trend (BP systolic)</p>
        <button type="button" onClick={onViewAll} className="text-xs text-primary-600 hover:underline">
          View all
        </button>
      </div>
      {isLoading && <Spinner />}
      {!isLoading && points.length === 0 && <p className="text-xs text-slate-400">No vitals recorded yet.</p>}
      {!isLoading && points.length > 0 && <MiniSparkline points={points} />}
    </div>
  )
}

function MiniSparkline({ points }: { points: { date: string; value: number }[] }) {
  const W = 260
  const H = 80
  const PAD = 10
  const values = points.map((p) => p.value)
  const min = Math.min(...values)
  const max = Math.max(...values)
  const range = max - min || 1
  const x = (i: number) => PAD + (points.length === 1 ? (W - PAD * 2) / 2 : (i / (points.length - 1)) * (W - PAD * 2))
  const y = (v: number) => H - PAD - ((v - min) / range) * (H - PAD * 2)
  const path = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${x(i).toFixed(1)} ${y(p.value).toFixed(1)}`).join(' ')

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="w-full" role="img" aria-label="BP systolic trend">
      <path d={path} fill="none" stroke="#2a78d6" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />
      {points.map((p, i) => (
        <circle key={`${p.date}-${i}`} cx={x(i)} cy={y(p.value)} r={3} fill="#2a78d6">
          <title>
            {p.date}: {p.value}
          </title>
        </circle>
      ))}
    </svg>
  )
}

function ConsultationSubTab({ canViewOpd, visitId }: { canViewOpd: boolean; visitId: number | null }) {
  const { data: workspace, isLoading } = useWorkspace(canViewOpd ? visitId : null)

  if (!canViewOpd) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-4 text-sm text-slate-400">
        You don't have permission to view OPD consultation details.
      </div>
    )
  }
  if (visitId === null) {
    return <div className="rounded-lg border border-slate-200 bg-white p-4 text-sm text-slate-400">No OPD visits recorded yet.</div>
  }
  if (isLoading || !workspace) {
    return <Spinner />
  }

  const { visit, complaints, examination, diagnoses, prescription } = workspace

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <div className="mb-3 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-slate-700">Latest consultation</h3>
          <Link to={`/opd/visits/${visit.id}`} className="text-xs font-medium text-primary-600 hover:underline">
            Open in OPD Workspace →
          </Link>
        </div>
        <dl className="grid grid-cols-2 gap-x-4 gap-y-2 text-sm sm:grid-cols-4">
          <Field label="Visit type" value={VISIT_TYPE_LABELS[visit.visitType]} />
          <Field label="Visit date" value={visit.visitDate} />
          <Field label="Doctor" value={visit.doctorName} />
          <Field label="Status" value={OP_VISIT_STATUS_LABELS[visit.status]} />
        </dl>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Chief Complaint</h3>
        {complaints.length > 0 ? (
          <ul className="space-y-1 text-sm text-slate-700">
            {complaints.map((c) => (
              <li key={c.id}>
                {c.complaint}
                {c.duration ? ` (${c.duration})` : ''}
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-sm text-slate-400">None recorded.</p>
        )}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">History / Examination</h3>
        <p className="text-sm text-slate-700">{examination?.historyNotes || '—'}</p>
        <p className="mt-2 text-sm text-slate-700">{examination?.examinationNotes || '—'}</p>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Diagnosis</h3>
        {diagnoses.length > 0 ? (
          <ul className="space-y-1.5 text-sm text-slate-700">
            {diagnoses.map((d) => (
              <li key={d.id} className="flex items-center gap-2">
                <span>
                  {d.icdCode} — {d.icdDescription}
                </span>
                <StatusBadge tone="info">{DIAGNOSIS_TYPE_LABELS[d.type]}</StatusBadge>
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-sm text-slate-400">None recorded.</p>
        )}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Prescription</h3>
        {prescription && prescription.items.length > 0 ? (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-left text-slate-500">
                <th className="py-1.5 pr-2 font-medium">Medicine</th>
                <th className="py-1.5 pr-2 font-medium">Dose</th>
                <th className="py-1.5 pr-2 font-medium">Frequency</th>
                <th className="py-1.5 font-medium">Duration</th>
              </tr>
            </thead>
            <tbody>
              {prescription.items.map((i) => (
                <tr key={i.id} className="border-b border-slate-100 last:border-0">
                  <td className="py-1.5 pr-2">{i.drugName}</td>
                  <td className="py-1.5 pr-2">{i.dose}</td>
                  <td className="py-1.5 pr-2">{i.frequency}</td>
                  <td className="py-1.5">{i.duration}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <p className="text-sm text-slate-400">No prescription on file.</p>
        )}
      </div>
    </div>
  )
}

function OrdersSubTab({ canViewOpd, visitId }: { canViewOpd: boolean; visitId: number | null }) {
  const { data: workspace, isLoading } = useWorkspace(canViewOpd ? visitId : null)

  if (!canViewOpd) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-4 text-sm text-slate-400">
        You don't have permission to view OPD orders.
      </div>
    )
  }
  if (visitId === null) {
    return <div className="rounded-lg border border-slate-200 bg-white p-4 text-sm text-slate-400">No OPD visits recorded yet.</div>
  }
  if (isLoading || !workspace) {
    return <Spinner />
  }

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="mb-3 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-700">Orders — latest visit ({workspace.visit.visitDate})</h3>
        <Link to={`/opd/visits/${workspace.visit.id}`} className="text-xs font-medium text-primary-600 hover:underline">
          + Add order (open in OPD) →
        </Link>
      </div>
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 text-left text-slate-500">
            <th className="py-1.5 pr-2 font-medium">Type</th>
            <th className="py-1.5 pr-2 font-medium">Description</th>
            <th className="py-1.5 pr-2 font-medium">Priority</th>
            <th className="py-1.5 font-medium">Status</th>
          </tr>
        </thead>
        <tbody>
          {workspace.orders.map((o) => (
            <tr key={o.id} className="border-b border-slate-100 last:border-0">
              <td className="py-1.5 pr-2">{ORDER_TYPE_LABELS[o.orderType]}</td>
              <td className="py-1.5 pr-2">{o.description}</td>
              <td className="py-1.5 pr-2">{ORDER_PRIORITY_LABELS[o.priority]}</td>
              <td className="py-1.5">
                <StatusBadge tone={o.status === 3 ? 'success' : o.status === 4 ? 'danger' : 'info'}>
                  {ORDER_STATUS_LABELS[o.status]}
                </StatusBadge>
              </td>
            </tr>
          ))}
          {workspace.orders.length === 0 && (
            <tr>
              <td colSpan={4} className="py-8 text-center text-slate-400">
                No orders on this visit.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  )
}

function ResultsSubTab({ patientId }: { patientId: number }) {
  const { data: investigations = [], isLoading: loadingInv } = useInvestigationHistory(patientId)
  const { data: tests = [] } = useLabTests()
  const [testId, setTestId] = useState('')
  const { data: series = [], isLoading } = useResultsTrend(patientId, testId ? Number(testId) : null)

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Investigation History</h3>
        {loadingInv && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-left text-slate-500">
              <th className="py-1.5 pr-2 font-medium">Test</th>
              <th className="py-1.5 pr-2 font-medium">Ordered</th>
              <th className="py-1.5 pr-2 font-medium">Status / Result</th>
              <th className="py-1.5" />
            </tr>
          </thead>
          <tbody>
            {investigations.map((inv) => (
              <tr key={inv.labOrderItemId} className="border-b border-slate-100 last:border-0">
                <td className="py-1.5 pr-2 font-medium text-slate-700">{inv.testName}</td>
                <td className="py-1.5 pr-2 text-slate-500">{inv.orderDate}</td>
                <td className="py-1.5 pr-2 text-slate-500">{inv.resultSummary ?? inv.status}</td>
                <td className="py-1.5 text-right">
                  {inv.resultSummary && (
                    <button type="button" className="text-primary-600 hover:underline" onClick={() => openLabReport(inv.labOrderId)}>
                      View report
                    </button>
                  )}
                </td>
              </tr>
            ))}
            {!loadingInv && investigations.length === 0 && (
              <tr>
                <td colSpan={4} className="py-8 text-center text-slate-400">
                  No investigations on file.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Results Trend</h3>
        <Select
          label="Test"
          placeholder="Select a lab test…"
          value={testId}
          onChange={(e) => setTestId(e.target.value)}
          options={tests.map((t) => ({ value: t.id, label: t.name }))}
          className="max-w-sm"
        />
        {isLoading && <Spinner />}
        {testId && !isLoading && series.length === 0 && (
          <p className="mt-3 text-sm text-slate-400">No approved results for this test yet.</p>
        )}
        {series.map((s) => (
          <div key={s.parameterId} className="mt-4">
            <h4 className="mb-2 text-xs font-semibold text-slate-600">
              {s.parameterName}
              {s.unit ? ` (${s.unit})` : ''}
            </h4>
            <ResultsTrendChart series={s} />
          </div>
        ))}
      </div>
    </div>
  )
}

function VitalsSubTab({
  canViewOpd,
  workspaces,
  isLoading,
}: {
  canViewOpd: boolean
  workspaces: ConsultationWorkspace[]
  isLoading: boolean
}) {
  if (!canViewOpd) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-4 text-sm text-slate-400">
        You don't have permission to view OPD vitals.
      </div>
    )
  }
  if (isLoading) {
    return <Spinner />
  }

  const rows = [...workspaces].reverse().filter((w) => w.vitals)

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-2 text-sm font-semibold text-slate-700">Vitals — recent visits</h3>
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 text-left text-slate-500">
            <th className="py-1.5 pr-2 font-medium">Date</th>
            <th className="py-1.5 pr-2 font-medium">BP</th>
            <th className="py-1.5 pr-2 font-medium">Pulse</th>
            <th className="py-1.5 pr-2 font-medium">Temp</th>
            <th className="py-1.5 pr-2 font-medium">SpO2</th>
            <th className="py-1.5 pr-2 font-medium">Weight</th>
            <th className="py-1.5 pr-2 font-medium">Height</th>
            <th className="py-1.5 font-medium">BMI</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((w) => (
            <tr key={w.visit.id} className="border-b border-slate-100 last:border-0">
              <td className="py-1.5 pr-2 text-slate-500">{w.visit.visitDate}</td>
              <td className="py-1.5 pr-2">
                {w.vitals!.bloodPressureSystolic && w.vitals!.bloodPressureDiastolic
                  ? `${w.vitals!.bloodPressureSystolic}/${w.vitals!.bloodPressureDiastolic}`
                  : '—'}
              </td>
              <td className="py-1.5 pr-2">{w.vitals!.pulseRate ?? '—'}</td>
              <td className="py-1.5 pr-2">{w.vitals!.temperatureF ?? '—'}</td>
              <td className="py-1.5 pr-2">{w.vitals!.spO2 ?? '—'}</td>
              <td className="py-1.5 pr-2">{w.vitals!.weightKg ?? '—'}</td>
              <td className="py-1.5 pr-2">{w.vitals!.heightCm ?? '—'}</td>
              <td className="py-1.5">{w.vitals!.bmi ?? '—'}</td>
            </tr>
          ))}
          {rows.length === 0 && (
            <tr>
              <td colSpan={8} className="py-8 text-center text-slate-400">
                No vitals recorded yet.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  )
}

function TimelineSubTab({ patientId }: { patientId: number }) {
  const { data: events = [], isLoading } = useTimeline(patientId)
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      {isLoading && <Spinner />}
      <ul className="space-y-3">
        {events.map((e, i) => (
          <li key={`${e.type}-${e.linkId}-${i}`} className="flex gap-3 border-b border-slate-100 pb-3 last:border-0">
            <div className="w-36 shrink-0 text-xs text-slate-400">{new Date(e.at).toLocaleString()}</div>
            <div>
              <StatusBadge tone="info">{TIMELINE_EVENT_LABELS[e.type]}</StatusBadge>
              <p className="mt-1 text-sm font-medium text-slate-800">{e.title}</p>
              {e.summary && <p className="text-sm text-slate-500">{e.summary}</p>}
            </div>
          </li>
        ))}
        {!isLoading && events.length === 0 && <p className="py-10 text-center text-slate-400">No clinical history recorded yet.</p>}
      </ul>
    </div>
  )
}

function HistoriesSubTab({ patientId, setError }: { patientId: number; setError: (e: string | null) => void }) {
  return (
    <div className="space-y-4">
      <MedicalHistorySection patientId={patientId} setError={setError} />
      <SurgicalHistorySection patientId={patientId} setError={setError} />
      <FamilyHistorySection patientId={patientId} setError={setError} />
      <ImmunizationSection patientId={patientId} setError={setError} />
    </div>
  )
}

function MedicalHistorySection({ patientId, setError }: { patientId: number; setError: (e: string | null) => void }) {
  const { data: entries = [], isLoading } = useMedicalHistory(patientId)
  const add = useAddMedicalHistory(patientId)
  const [condition, setCondition] = useState('')
  const [diagnosedDate, setDiagnosedDate] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      condition: requiredText(condition, 'Condition'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await add.mutateAsync({ condition, diagnosedDate: diagnosedDate || null, notes: null })
      setCondition('')
      setDiagnosedDate('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-2 text-sm font-semibold text-slate-700">Medical History</h3>
      <div className="flex flex-wrap items-end gap-2">
        <div className="min-w-[220px] flex-1">
          <Input
            label="Condition"
            required
            error={fieldErrors.condition}
            value={condition}
            onChange={(e) => setCondition(e.target.value)}
          />
        </div>
        <Input label="Diagnosed" type="date" value={diagnosedDate} onChange={(e) => setDiagnosedDate(e.target.value)} />
        <Button loading={add.isPending} onClick={submit}>Add</Button>
      </div>
      {isLoading && <Spinner />}
      <ul className="mt-3 space-y-1">
        {entries.map((e) => (
          <li key={e.id} className="text-sm text-slate-700">
            {e.condition} {e.diagnosedDate && <span className="text-xs text-slate-400">({e.diagnosedDate})</span>}
          </li>
        ))}
        {!isLoading && entries.length === 0 && <li className="text-sm text-slate-400">None recorded.</li>}
      </ul>
    </div>
  )
}

function SurgicalHistorySection({ patientId, setError }: { patientId: number; setError: (e: string | null) => void }) {
  const { data: entries = [], isLoading } = useSurgicalHistory(patientId)
  const add = useAddSurgicalHistory(patientId)
  const [procedureName, setProcedureName] = useState('')
  const [procedureDate, setProcedureDate] = useState('')
  const [surgeon, setSurgeon] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      procedureName: requiredText(procedureName, 'Procedure'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await add.mutateAsync({ procedureName, procedureDate: procedureDate || null, surgeon: surgeon || null, notes: null })
      setProcedureName('')
      setProcedureDate('')
      setSurgeon('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-2 text-sm font-semibold text-slate-700">Surgical History</h3>
      <div className="flex flex-wrap items-end gap-2">
        <div className="min-w-[220px] flex-1">
          <Input
            label="Procedure"
            required
            error={fieldErrors.procedureName}
            value={procedureName}
            onChange={(e) => setProcedureName(e.target.value)}
          />
        </div>
        <Input label="Date" type="date" value={procedureDate} onChange={(e) => setProcedureDate(e.target.value)} />
        <Input label="Surgeon" value={surgeon} onChange={(e) => setSurgeon(e.target.value)} />
        <Button loading={add.isPending} onClick={submit}>Add</Button>
      </div>
      {isLoading && <Spinner />}
      <ul className="mt-3 space-y-1">
        {entries.map((e) => (
          <li key={e.id} className="text-sm text-slate-700">
            {e.procedureName} {e.procedureDate && <span className="text-xs text-slate-400">({e.procedureDate}{e.surgeon ? ` · ${e.surgeon}` : ''})</span>}
          </li>
        ))}
        {!isLoading && entries.length === 0 && <li className="text-sm text-slate-400">None recorded.</li>}
      </ul>
    </div>
  )
}

function FamilyHistorySection({ patientId, setError }: { patientId: number; setError: (e: string | null) => void }) {
  const { data: entries = [], isLoading } = useFamilyHistory(patientId)
  const add = useAddFamilyHistory(patientId)
  const [relation, setRelation] = useState('')
  const [condition, setCondition] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      relation: requiredText(relation, 'Relation'),
      condition: requiredText(condition, 'Condition'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await add.mutateAsync({ relation, condition, notes: null })
      setRelation('')
      setCondition('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-2 text-sm font-semibold text-slate-700">Family History</h3>
      <div className="flex flex-wrap items-end gap-2">
        <Input
          label="Relation"
          required
          error={fieldErrors.relation}
          value={relation}
          onChange={(e) => setRelation(e.target.value)}
          placeholder="Father
          /
          Mother…"
        />
        <div className="min-w-[220px] flex-1">
          <Input
            label="Condition"
            required
            error={fieldErrors.condition}
            value={condition}
            onChange={(e) => setCondition(e.target.value)}
          />
        </div>
        <Button loading={add.isPending} onClick={submit}>Add</Button>
      </div>
      {isLoading && <Spinner />}
      <ul className="mt-3 space-y-1">
        {entries.map((e) => (
          <li key={e.id} className="text-sm text-slate-700">
            {e.relation} — {e.condition}
          </li>
        ))}
        {!isLoading && entries.length === 0 && <li className="text-sm text-slate-400">None recorded.</li>}
      </ul>
    </div>
  )
}

function ImmunizationSection({ patientId, setError }: { patientId: number; setError: (e: string | null) => void }) {
  const { data: entries = [], isLoading } = useImmunizations(patientId)
  const add = useAddImmunization(patientId)
  const [vaccineName, setVaccineName] = useState('')
  const [administeredDate, setAdministeredDate] = useState('')
  const [nextDueDate, setNextDueDate] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      vaccineName: requiredText(vaccineName, 'Vaccine'),
      administeredDate: requiredText(administeredDate, 'Administered date'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await add.mutateAsync({ vaccineName, doseNumber: null, administeredDate, nextDueDate: nextDueDate || null, administeredBy: null, notes: null })
      setVaccineName('')
      setAdministeredDate('')
      setNextDueDate('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-2 text-sm font-semibold text-slate-700">Immunizations</h3>
      <div className="flex flex-wrap items-end gap-2">
        <div className="min-w-[220px] flex-1">
          <Input
            label="Vaccine"
            required
            error={fieldErrors.vaccineName}
            value={vaccineName}
            onChange={(e) => setVaccineName(e.target.value)}
          />
        </div>
        <Input
          label="Administered"
          required
          error={fieldErrors.administeredDate}
          type="date"
          value={administeredDate}
          onChange={(e) => setAdministeredDate(e.target.value)}
        />
        <Input label="Next due" type="date" value={nextDueDate} onChange={(e) => setNextDueDate(e.target.value)} />
        <Button loading={add.isPending} onClick={submit}>Add</Button>
      </div>
      {isLoading && <Spinner />}
      <ul className="mt-3 space-y-1">
        {entries.map((e) => (
          <li key={e.id} className="text-sm text-slate-700">
            {e.vaccineName} <span className="text-xs text-slate-400">({e.administeredDate}{e.nextDueDate ? ` · next due ${e.nextDueDate}` : ''})</span>
          </li>
        ))}
        {!isLoading && entries.length === 0 && <li className="text-sm text-slate-400">None recorded.</li>}
      </ul>
    </div>
  )
}

const severityTone: Record<AllergySeverity, 'success' | 'warning' | 'danger'> = { 1: 'success', 2: 'warning', 3: 'danger' }

function AllergiesSubTab({ patientId, setError }: { patientId: number; setError: (e: string | null) => void }) {
  const { data: allergies = [], isLoading } = useAllergies(patientId)
  const add = useAddAllergy(patientId)
  const deactivate = useDeactivateAllergy(patientId)
  const [allergenType, setAllergenType] = useState<AllergenType>(1)
  const [allergenName, setAllergenName] = useState('')
  const [reaction, setReaction] = useState('')
  const [severity, setSeverity] = useState<AllergySeverity>(2)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const activeAllergies = allergies.filter((a) => a.isActive)

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      allergenName: requiredText(allergenName, 'Allergen'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await add.mutateAsync({ allergenType, allergenName, reaction: reaction || null, severity })
      setAllergenName('')
      setReaction('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="space-y-4">
      {activeAllergies.length > 0 && (
        <div className="rounded-lg border border-red-200 bg-red-50 p-4">
          <p className="mb-2 text-sm font-semibold text-red-800">⚠ Active allergies</p>
          <div className="flex flex-wrap gap-2">
            {activeAllergies.map((a) => (
              <StatusBadge key={a.id} tone={severityTone[a.severity]}>
                {a.allergenName} ({ALLERGEN_TYPE_LABELS[a.allergenType]}) — {ALLERGY_SEVERITY_LABELS[a.severity]}
              </StatusBadge>
            ))}
          </div>
        </div>
      )}

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Record allergy</h3>
        <div className="flex flex-wrap items-end gap-2">
          <Select
            label="Type"
            value={String(allergenType)}
            onChange={(e) => setAllergenType(Number(e.target.value) as AllergenType)}
            options={Object.entries(ALLERGEN_TYPE_LABELS).map(([value, label]) => ({ value, label }))}
          />
          <Input
            label="Allergen"
            required
            error={fieldErrors.allergenName}
            value={allergenName}
            onChange={(e) => setAllergenName(e.target.value)}
            placeholder="Penicillin…"
          />
          <Input label="Reaction" value={reaction} onChange={(e) => setReaction(e.target.value)} />
          <Select
            label="Severity"
            value={String(severity)}
            onChange={(e) => setSeverity(Number(e.target.value) as AllergySeverity)}
            options={Object.entries(ALLERGY_SEVERITY_LABELS).map(([value, label]) => ({ value, label }))}
          />
          <Button loading={add.isPending} onClick={submit}>Add</Button>
        </div>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Allergen</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Reaction</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Severity</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {allergies.map((a) => (
              <tr key={a.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5">{a.allergenName}</td>
                <td className="px-4 py-2.5">{ALLERGEN_TYPE_LABELS[a.allergenType]}</td>
                <td className="px-4 py-2.5 text-slate-500">{a.reaction ?? '—'}</td>
                <td className="px-4 py-2.5"><StatusBadge tone={severityTone[a.severity]}>{ALLERGY_SEVERITY_LABELS[a.severity]}</StatusBadge></td>
                <td className="px-4 py-2.5">{a.isActive ? <StatusBadge tone="danger">Active</StatusBadge> : <StatusBadge tone="neutral">Inactive</StatusBadge>}</td>
                <td className="px-4 py-2.5 text-right">
                  {a.isActive && (
                    <Button variant="ghost" onClick={async () => {
                      try { await deactivate.mutateAsync(a.id) } catch (err) { setError(errorMessage(err)) }
                    }}>Deactivate</Button>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && allergies.length === 0 && (
              <tr><td colSpan={6} className="px-4 py-10 text-center text-slate-400">No allergies recorded.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function ProblemListSubTab({ patientId, setError }: { patientId: number; setError: (e: string | null) => void }) {
  const { data: problems = [], isLoading } = useProblemList(patientId)
  const add = useAddProblem(patientId)
  const resolve = useResolveProblem(patientId)
  const [diagnosisText, setDiagnosisText] = useState('')
  const [identifiedDate, setIdentifiedDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      diagnosisText: requiredText(diagnosisText, 'Diagnosis'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await add.mutateAsync({ icdCodeId: null, diagnosisText, identifiedDate, notes: null })
      setDiagnosisText('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Add problem</h3>
        <div className="flex flex-wrap items-end gap-2">
          <div className="min-w-[220px] flex-1">
            <Input
              label="Diagnosis"
              required
              error={fieldErrors.diagnosisText}
              value={diagnosisText}
              onChange={(e) => setDiagnosisText(e.target.value)}
            />
          </div>
          <Input label="Identified" type="date" value={identifiedDate} onChange={(e) => setIdentifiedDate(e.target.value)} />
          <Button loading={add.isPending} onClick={submit}>Add</Button>
        </div>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <ul className="divide-y divide-slate-100">
          {problems.map((p) => (
            <li key={p.id} className="flex items-center justify-between p-4">
              <div>
                <p className="text-sm font-medium text-slate-800">{p.diagnosisText}</p>
                <p className="text-xs text-slate-400">Identified {p.identifiedDate}{p.resolvedDate ? ` · resolved ${p.resolvedDate}` : ''} · {p.recordedByName}</p>
              </div>
              <div className="flex items-center gap-2">
                <StatusBadge tone={p.status === 1 ? 'warning' : 'success'}>{PROBLEM_STATUS_LABELS[p.status]}</StatusBadge>
                {p.status === 1 && (
                  <Button variant="ghost" onClick={async () => {
                    try { await resolve.mutateAsync(p.id) } catch (err) { setError(errorMessage(err)) }
                  }}>Resolve</Button>
                )}
              </div>
            </li>
          ))}
          {!isLoading && problems.length === 0 && <li className="py-10 text-center text-slate-400">No problems recorded.</li>}
        </ul>
      </div>
    </div>
  )
}

function NotesSubTab({ patientId, setError }: { patientId: number; setError: (e: string | null) => void }) {
  const { data: notes = [], isLoading } = useClinicalNotes(patientId)
  const add = useAddClinicalNote(patientId)
  const [noteType, setNoteType] = useState('General')
  const [title, setTitle] = useState('')
  const [content, setContent] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})

  const submit = async () => {
    setError(null)
    const errors = compactErrors({
      content: requiredText(content, 'Note'),
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await add.mutateAsync({ noteType, title: title || null, content, opVisitId: null, admissionId: null })
      setTitle('')
      setContent('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Add clinical note</h3>
        <div className="grid gap-2 sm:grid-cols-3">
          <Input label="Type" value={noteType} onChange={(e) => setNoteType(e.target.value)} placeholder="General, Cardiology…" />
          <Input label="Title" value={title} onChange={(e) => setTitle(e.target.value)} />
        </div>
        <div className="mt-2">
          <Input
            label="Note"
            required
            error={fieldErrors.content}
            value={content}
            onChange={(e) => setContent(e.target.value)}
          />
        </div>
        <Button className="mt-3" loading={add.isPending} onClick={submit}>Add</Button>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <ul className="divide-y divide-slate-100">
          {notes.map((n) => (
            <li key={n.id} className="p-4">
              <p className="text-xs text-slate-400">{new Date(n.noteAt).toLocaleString()} — {n.noteType}{n.title ? ` · ${n.title}` : ''} — {n.recordedByName}</p>
              <p className="mt-1 text-sm text-slate-700">{n.content}</p>
            </li>
          ))}
          {!isLoading && notes.length === 0 && <li className="py-10 text-center text-slate-400">No clinical notes yet.</li>}
        </ul>
      </div>
    </div>
  )
}

function AttachmentsSubTab({ patientId, setError }: { patientId: number; setError: (e: string | null) => void }) {
  const { data: attachments = [], isLoading } = useAttachments(patientId)
  const upload = useUploadAttachment(patientId)
  const [attachmentType, setAttachmentType] = useState('')
  const [description, setDescription] = useState('')

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end gap-2 rounded-lg border border-slate-200 bg-white p-4">
        <Input label="Type" value={attachmentType} onChange={(e) => setAttachmentType(e.target.value)} placeholder="Lab Report, Referral Letter…" />
        <Input label="Description" value={description} onChange={(e) => setDescription(e.target.value)} />
        <label>
          <span className="mb-1 block text-sm font-medium text-slate-700">File</span>
          <input
            type="file"
            className="block text-sm"
            onChange={async (e) => {
              const file = e.target.files?.[0]
              if (!file || !attachmentType.trim()) {
                setError('Attachment type is required.')
                return
              }
              setError(null)
              try {
                await upload.mutateAsync({ file, attachmentType, description: description || null })
                setAttachmentType('')
                setDescription('')
              } catch (err) {
                setError(errorMessage(err))
              }
              e.target.value = ''
            }}
          />
        </label>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">File</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Size</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Uploaded</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {attachments.map((a) => (
              <tr key={a.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5">{a.attachmentType}</td>
                <td className="px-4 py-2.5">
                  {a.fileName}
                  {a.description && <span className="ml-1 text-xs text-slate-400">({a.description})</span>}
                </td>
                <td className="px-4 py-2.5 text-slate-500">{formatBytes(a.sizeBytes)}</td>
                <td className="px-4 py-2.5 text-slate-500">{new Date(a.uploadedAt).toLocaleString()}</td>
                <td className="px-4 py-2.5 text-right">
                  <button type="button" className="text-primary-600 hover:underline" onClick={() => downloadAttachment(patientId, a.id, a.fileName)}>
                    Download
                  </button>
                </td>
              </tr>
            ))}
            {!isLoading && attachments.length === 0 && (
              <tr><td colSpan={5} className="px-4 py-10 text-center text-slate-400">No attachments uploaded.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function RecordsSubTab({ patientId }: { patientId: number }) {
  const { data: prescriptions = [], isLoading: loadingRx } = usePrescriptionHistory(patientId)
  const { data: procedures = [], isLoading: loadingProc } = useProcedureHistory(patientId)
  const { data: summaries = [], isLoading: loadingSummaries } = useDischargeSummaryArchive(patientId)

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Prescription History</h3>
        {loadingRx && <Spinner />}
        <ul className="space-y-2">
          {prescriptions.map((rx) => (
            <li key={rx.visitId} className="border-b border-slate-100 pb-2 text-sm last:border-0">
              <p className="text-xs text-slate-400">{rx.visitDate} — {rx.doctorName}</p>
              <p className="text-slate-700">{rx.items.map((i) => `${i.drugName} ${i.dose} ${i.frequency}`).join(', ')}</p>
            </li>
          ))}
          {!loadingRx && prescriptions.length === 0 && <li className="text-sm text-slate-400">No prescriptions on file.</li>}
        </ul>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Procedure History</h3>
        {loadingProc && <Spinner />}
        <ul className="space-y-2">
          {procedures.map((p, i) => (
            <li key={i} className="border-b border-slate-100 pb-2 text-sm last:border-0">
              <p className="text-xs text-slate-400">{new Date(p.performedAt).toLocaleString()} — {p.performedByDoctorName} — {p.admissionNumber}</p>
              <p className="text-slate-700">{p.procedureName}{p.findings ? ` — ${p.findings}` : ''}</p>
            </li>
          ))}
          {!loadingProc && procedures.length === 0 && <li className="text-sm text-slate-400">No procedures on file.</li>}
        </ul>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-2 text-sm font-semibold text-slate-700">Discharge Summary Archive</h3>
        {loadingSummaries && <Spinner />}
        <ul className="space-y-2">
          {summaries.map((s) => (
            <li key={s.admissionId} className="flex items-center justify-between border-b border-slate-100 pb-2 text-sm last:border-0">
              <span className="text-slate-700">{s.admissionNumber} — {s.finalDiagnosis}</span>
              <StatusBadge tone={s.isApproved ? 'success' : 'neutral'}>{s.isApproved ? 'Approved' : 'Pending'}</StatusBadge>
            </li>
          ))}
          {!loadingSummaries && summaries.length === 0 && <li className="text-sm text-slate-400">No discharge summaries on file.</li>}
        </ul>
      </div>
    </div>
  )
}

function EditPatientModal({ patient, onClose }: { patient: import('../types').PatientDetail; onClose: () => void }) {
  const updatePatient = useUpdatePatient()
  const { data: categories = [] } = useSimpleMaster('patientcategories', endpoints.masters.patientCategories)
  const { data: referrersResult } = useReferrers({ page: 1, pageSize: 100 })
  const { data: salutations = [] } = useSimpleMaster('salutations', endpoints.masters.salutations)
  const { data: maritalStatuses = [] } = useSimpleMaster('marital-statuses', endpoints.masters.maritalStatuses)
  const { data: countries = [] } = useSimpleMaster('nationalities', endpoints.masters.nationalities)
  const { data: states = [] } = useSimpleMaster('states', endpoints.masters.states)
  const { data: cities = [] } = useSimpleMaster('cities', endpoints.masters.cities)
  const { data: insuranceCompanies = [] } = useSimpleMaster('insurance-companies', endpoints.masters.insuranceCompanies)
  const [error, setError] = useState<string | null>(null)
  const [cityIdForAreas, setCityIdForAreas] = useState<number | undefined>(patient.cityId ?? undefined)
  const { data: areas = [] } = useAreas(cityIdForAreas)
  const [form, setForm] = useState<EditFormState>({
    salutationId: patient.salutationId ? String(patient.salutationId) : '',
    firstName: patient.firstName,
    middleName: patient.middleName ?? '',
    lastName: patient.lastName,
    gender: patient.gender,
    dobKnown: patient.dateOfBirth !== null,
    dateOfBirth: patient.dateOfBirth ?? '',
    estimatedAge: patient.estimatedAge ? String(patient.estimatedAge) : '',
    bloodGroup: patient.bloodGroup ?? '',
    maritalStatusId: patient.maritalStatusId ? String(patient.maritalStatusId) : '',
    languageId: patient.languageId ? String(patient.languageId) : '',
    patientCaseTypeId: patient.patientCaseTypeId ? String(patient.patientCaseTypeId) : '',
    patientHcTagId: patient.patientHcTagId ? String(patient.patientHcTagId) : '',
    speciallyAbledCategoryId: patient.speciallyAbledCategoryId ? String(patient.speciallyAbledCategoryId) : '',
    isVip: patient.isVip,
    smsOptIn: patient.smsOptIn,
    occupation: patient.occupation ?? '',
    monthlyIncome: patient.monthlyIncome != null ? String(patient.monthlyIncome) : '',
    motherName: patient.motherName ?? '',
    nationalId: patient.nationalId ?? '',
    mobile: patient.mobile,
    alternateMobile: patient.alternateMobile ?? '',
    whatsAppNumber: patient.whatsAppNumber ?? '',
    email: patient.email ?? '',
    address: patient.address ?? '',
    permanentAddress: patient.permanentAddress ?? '',
    isPermanentSameAsPresent: patient.isPermanentSameAsPresent,
    countryId: patient.countryId ? String(patient.countryId) : '',
    nationalityId: patient.nationalityId ? String(patient.nationalityId) : '',
    stateId: patient.stateId ? String(patient.stateId) : '',
    cityId: patient.cityId ? String(patient.cityId) : '',
    areaId: patient.areaId ? String(patient.areaId) : '',
    pincode: patient.pincode ?? '',
    patientCategoryId: patient.patientCategoryId ? String(patient.patientCategoryId) : '',
    referrerId: patient.referrerId ? String(patient.referrerId) : '',
    insuranceRequired: patient.insuranceRequired,
    remarks: patient.remarks ?? '',
    isActive: patient.isActive,
    contacts: patient.contacts.map((c) => ({
      contactType: c.contactType,
      name: c.name,
      relationship: c.relationship,
      phone: c.phone,
      email: c.email,
      address: c.address,
      isPrimary: c.isPrimary,
    })),
    identifiers: patient.identifiers.map((i) => ({
      type: i.type,
      identityDocumentTypeId: i.identityDocumentTypeId,
      value: i.value,
    })),
    insurancePolicies: patient.insurancePolicies.map((p) => ({
      insuranceCompanyId: p.insuranceCompanyId,
      policyNumber: p.policyNumber,
      policyValidFrom: p.policyValidFrom,
      policyValidTo: p.policyValidTo,
      isPrimary: p.isPrimary,
    })),
  })

  const set = <K extends keyof EditFormState>(key: K, value: EditFormState[K]) => setForm((f) => ({ ...f, [key]: value }))
  const addContact = () =>
    set('contacts', [
      ...form.contacts,
      { contactType: CONTACT_TYPES.Guardian, name: '', relationship: '', phone: '', email: '', address: '', isPrimary: false },
    ])
  const updateContact = (index: number, patch: Partial<SavePatientContact>) =>
    set('contacts', form.contacts.map((c, i) => (i === index ? { ...c, ...patch } : c)))
  const removeContact = (index: number) => set('contacts', form.contacts.filter((_, i) => i !== index))

  const addIdentifier = () =>
    set('identifiers', [...form.identifiers, { type: IDENTIFIER_TYPES.Aadhaar, identityDocumentTypeId: null, value: '' }])
  const updateIdentifier = (index: number, patch: Partial<SavePatientIdentifier>) =>
    set('identifiers', form.identifiers.map((idf, i) => (i === index ? { ...idf, ...patch } : idf)))
  const removeIdentifier = (index: number) => set('identifiers', form.identifiers.filter((_, i) => i !== index))

  const submit = async () => {
    setError(null)
    const payload: UpdatePatientPayload = {
      salutationId: form.salutationId ? Number(form.salutationId) : null,
      firstName: form.firstName.trim(),
      middleName: form.middleName.trim() || null,
      lastName: form.lastName.trim(),
      gender: form.gender,
      dateOfBirth: form.dobKnown && form.dateOfBirth ? form.dateOfBirth : null,
      estimatedAge: !form.dobKnown && form.estimatedAge ? Number(form.estimatedAge) : null,
      bloodGroup: form.bloodGroup || null,
      maritalStatusId: form.maritalStatusId ? Number(form.maritalStatusId) : null,
      mobile: form.mobile.trim(),
      alternateMobile: form.alternateMobile || null,
      whatsAppNumber: form.whatsAppNumber || null,
      email: form.email || null,
      address: form.address || null,
      permanentAddress: form.isPermanentSameAsPresent ? null : form.permanentAddress || null,
      isPermanentSameAsPresent: form.isPermanentSameAsPresent,
      stateId: form.stateId ? Number(form.stateId) : null,
      cityId: form.cityId ? Number(form.cityId) : null,
      areaId: form.areaId ? Number(form.areaId) : null,
      pincode: form.pincode || null,
      countryId: form.countryId ? Number(form.countryId) : null,
      nationalityId: form.nationalityId ? Number(form.nationalityId) : null,
      occupation: form.occupation || null,
      monthlyIncome: form.monthlyIncome ? Number(form.monthlyIncome) : null,
      languageId: form.languageId ? Number(form.languageId) : null,
      motherName: form.motherName || null,
      nationalId: form.nationalId || null,
      patientCaseTypeId: form.patientCaseTypeId ? Number(form.patientCaseTypeId) : null,
      patientHcTagId: form.patientHcTagId ? Number(form.patientHcTagId) : null,
      speciallyAbledCategoryId: form.speciallyAbledCategoryId ? Number(form.speciallyAbledCategoryId) : null,
      isVip: form.isVip,
      smsOptIn: form.smsOptIn,
      insuranceRequired: form.insuranceRequired,
      remarks: form.remarks || null,
      patientCategoryId: form.patientCategoryId ? Number(form.patientCategoryId) : null,
      referrerId: form.referrerId ? Number(form.referrerId) : null,
      isActive: form.isActive,
      contacts: form.contacts,
      identifiers: form.identifiers.filter((i) => i.value.trim().length > 0),
      insurancePolicies: form.insuranceRequired
        ? form.insurancePolicies.filter((p) => p.insuranceCompanyId > 0 && p.policyNumber.trim().length > 0)
        : [],
    }
    try {
      await updatePatient.mutateAsync({ id: patient.id, ...payload })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Edit patient"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={updatePatient.isPending}>
            Save
          </Button>
        </>
      }
    >
      <div className="max-h-[65vh] space-y-4 overflow-y-auto pr-1">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

        <div className="grid grid-cols-4 gap-2">
          <Select
            label="Salutation"
            placeholder="—"
            options={salutations.map((s) => ({ value: s.id, label: s.name }))}
            value={form.salutationId}
            onChange={(e) => set('salutationId', e.target.value)}
          />
          <Input label="First name" value={form.firstName} onChange={(e) => set('firstName', e.target.value)} />
          <Input label="Middle name" value={form.middleName} onChange={(e) => set('middleName', e.target.value)} />
          <Input label="Last name" value={form.lastName} onChange={(e) => set('lastName', e.target.value)} />
        </div>
        <div className="grid grid-cols-3 gap-2">
          <Select
            label="Gender"
            options={Object.entries(GENDER_LABELS).map(([value, label]) => ({ value, label }))}
            value={String(form.gender)}
            onChange={(e) => set('gender', Number(e.target.value) as Gender)}
          />
          <Input label="Blood group" value={form.bloodGroup} onChange={(e) => set('bloodGroup', e.target.value)} />
          <Select
            label="Marital status"
            placeholder="—"
            options={maritalStatuses.map((s) => ({ value: s.id, label: s.name }))}
            value={form.maritalStatusId}
            onChange={(e) => set('maritalStatusId', e.target.value)}
          />
        </div>
        <div className="grid grid-cols-2 gap-2">
          <label className="flex items-center gap-1.5 text-sm text-slate-600">
            <input type="radio" checked={form.dobKnown} onChange={() => set('dobKnown', true)} /> DOB
          </label>
          <label className="flex items-center gap-1.5 text-sm text-slate-600">
            <input type="radio" checked={!form.dobKnown} onChange={() => set('dobKnown', false)} /> Estimated age
          </label>
        </div>
        {form.dobKnown ? (
          <Input type="date" value={form.dateOfBirth} onChange={(e) => set('dateOfBirth', e.target.value)} />
        ) : (
          <Input type="number" value={form.estimatedAge} onChange={(e) => set('estimatedAge', e.target.value)} />
        )}
        <div className="grid grid-cols-3 gap-2">
          <Input label="Mobile" value={form.mobile} onChange={(e) => set('mobile', e.target.value)} />
          <Input label="Alternate mobile" value={form.alternateMobile} onChange={(e) => set('alternateMobile', e.target.value)} />
          <Input label="WhatsApp" value={form.whatsAppNumber} onChange={(e) => set('whatsAppNumber', e.target.value)} />
        </div>
        <Input label="Email" value={form.email} onChange={(e) => set('email', e.target.value)} />
        <Input label="Present address" value={form.address} onChange={(e) => set('address', e.target.value)} />
        <div className="grid grid-cols-3 gap-2">
          <Select
            label="State"
            placeholder="—"
            options={states.map((s) => ({ value: s.id, label: s.name }))}
            value={form.stateId}
            onChange={(e) => set('stateId', e.target.value)}
          />
          <Select
            label="City"
            placeholder="—"
            options={cities.map((c) => ({ value: c.id, label: c.name }))}
            value={form.cityId}
            onChange={(e) => {
              set('cityId', e.target.value)
              set('areaId', '')
              setCityIdForAreas(e.target.value ? Number(e.target.value) : undefined)
            }}
          />
          <Select
            label="Area"
            placeholder="—"
            options={areas.map((a) => ({ value: a.id, label: a.name }))}
            value={form.areaId}
            onChange={(e) => set('areaId', e.target.value)}
            disabled={!form.cityId}
          />
        </div>
        <div className="grid grid-cols-3 gap-2">
          <Input label="Pincode" value={form.pincode} onChange={(e) => set('pincode', e.target.value)} />
          <Select
            label="Country"
            placeholder="— India —"
            options={countries.map((c) => ({ value: c.id, label: c.name }))}
            value={form.countryId}
            onChange={(e) => set('countryId', e.target.value)}
          />
          <Select
            label="Nationality"
            placeholder="— India —"
            options={countries.map((c) => ({ value: c.id, label: c.name }))}
            value={form.nationalityId}
            onChange={(e) => set('nationalityId', e.target.value)}
          />
        </div>
        <div className="grid grid-cols-2 gap-2">
          <Input label="Occupation" value={form.occupation} onChange={(e) => set('occupation', e.target.value)} />
          <Input label="Mother's name" value={form.motherName} onChange={(e) => set('motherName', e.target.value)} />
        </div>
        <div className="grid grid-cols-2 gap-2">
          <Select
            label="Patient category"
            placeholder="— none —"
            options={categories.map((c) => ({ value: c.id, label: c.name }))}
            value={form.patientCategoryId}
            onChange={(e) => set('patientCategoryId', e.target.value)}
          />
          <Select
            label="Referrer"
            placeholder="— none —"
            options={(referrersResult?.items ?? []).map((r) => ({ value: r.id, label: r.name }))}
            value={form.referrerId}
            onChange={(e) => set('referrerId', e.target.value)}
          />
        </div>
        <Input label="Remarks" value={form.remarks} onChange={(e) => set('remarks', e.target.value)} />
        <div className="flex items-center gap-6">
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input type="checkbox" checked={form.isActive} onChange={(e) => set('isActive', e.target.checked)} />
            Active
          </label>
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input type="checkbox" checked={form.isVip} onChange={(e) => set('isVip', e.target.checked)} />
            VIP
          </label>
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input type="checkbox" checked={form.smsOptIn} onChange={(e) => set('smsOptIn', e.target.checked)} />
            SMS updates
          </label>
        </div>

        <div className="border-t border-slate-100 pt-3">
          <label className="mb-2 flex items-center gap-2 text-sm font-semibold text-slate-700">
            <input
              type="checkbox"
              checked={form.insuranceRequired}
              onChange={(e) => set('insuranceRequired', e.target.checked)}
            />
            Insurance required
          </label>
          {form.insuranceRequired && (
            <>
              <div className="mb-2 flex justify-end">
                <Button
                  variant="secondary"
                  onClick={() =>
                    set('insurancePolicies', [
                      ...form.insurancePolicies,
                      { insuranceCompanyId: 0, policyNumber: '', policyValidFrom: null, policyValidTo: null, isPrimary: false },
                    ])
                  }
                >
                  Add policy
                </Button>
              </div>
              {form.insurancePolicies.map((policy, index) => (
                <div key={index} className="mb-2 flex items-end gap-2">
                  <Select
                    label="Insurer / TPA"
                    placeholder="—"
                    options={insuranceCompanies.map((c) => ({ value: c.id, label: c.name }))}
                    value={policy.insuranceCompanyId || ''}
                    onChange={(e) =>
                      set(
                        'insurancePolicies',
                        form.insurancePolicies.map((p, i) =>
                          i === index ? { ...p, insuranceCompanyId: Number(e.target.value) } : p,
                        ),
                      )
                    }
                    className="w-40"
                  />
                  <Input
                    label="Policy number"
                    className="flex-1"
                    value={policy.policyNumber}
                    onChange={(e) =>
                      set(
                        'insurancePolicies',
                        form.insurancePolicies.map((p, i) => (i === index ? { ...p, policyNumber: e.target.value } : p)),
                      )
                    }
                  />
                  <Button
                    variant="ghost"
                    className="text-red-600"
                    onClick={() => set('insurancePolicies', form.insurancePolicies.filter((_, i) => i !== index))}
                  >
                    Remove
                  </Button>
                </div>
              ))}
            </>
          )}
        </div>

        <div className="border-t border-slate-100 pt-3">
          <div className="mb-2 flex items-center justify-between">
            <span className="text-sm font-semibold text-slate-700">Guardian / Attender</span>
            <Button variant="secondary" onClick={addContact}>
              Add contact
            </Button>
          </div>
          {form.contacts.map((contact, index) => (
            <div key={index} className="mb-2 rounded-md border border-slate-200 p-2">
              <div className="grid grid-cols-2 gap-2">
                <Select
                  options={Object.entries(CONTACT_TYPE_LABELS).map(([value, label]) => ({ value, label }))}
                  value={String(contact.contactType)}
                  onChange={(e) => updateContact(index, { contactType: Number(e.target.value) as ContactType })}
                />
                <Input value={contact.name} onChange={(e) => updateContact(index, { name: e.target.value })} placeholder="Name" />
                <Input
                  value={contact.relationship ?? ''}
                  onChange={(e) => updateContact(index, { relationship: e.target.value })}
                  placeholder="Relationship"
                />
                <Input value={contact.phone ?? ''} onChange={(e) => updateContact(index, { phone: e.target.value })} placeholder="Phone" />
              </div>
              <div className="mt-1 flex justify-end">
                <Button variant="ghost" className="text-red-600" onClick={() => removeContact(index)}>
                  Remove
                </Button>
              </div>
            </div>
          ))}
        </div>

        <div className="border-t border-slate-100 pt-3">
          <div className="mb-2 flex items-center justify-between">
            <span className="text-sm font-semibold text-slate-700">Aadhaar / ABHA</span>
            <Button variant="secondary" onClick={addIdentifier}>
              Add ID
            </Button>
          </div>
          {form.identifiers.map((identifier, index) => (
            <div key={index} className="mb-2 flex items-center gap-2">
              <Select
                options={Object.entries(IDENTIFIER_TYPE_LABELS).map(([value, label]) => ({ value, label }))}
                value={String(identifier.type)}
                onChange={(e) => updateIdentifier(index, { type: Number(e.target.value) as IdentifierType })}
                className="w-32"
              />
              <Input
                className="flex-1"
                value={identifier.value}
                onChange={(e) => updateIdentifier(index, { value: e.target.value })}
              />
              <Button variant="ghost" className="text-red-600" onClick={() => removeIdentifier(index)}>
                Remove
              </Button>
            </div>
          ))}
        </div>
      </div>
    </Modal>
  )
}

function MergeModal({ patientId, onClose }: { patientId: number; onClose: () => void }) {
  const { data: candidates = [], isLoading } = usePatientDuplicates(patientId)
  const merge = useMergePatient()
  const [reason, setReason] = useState('')
  const [selected, setSelected] = useState<number | null>(null)
  const navigate = useNavigate()

  const submit = async () => {
    if (!selected || !reason.trim()) return
    try {
      await merge.mutateAsync({ survivorId: patientId, mergedPatientId: selected, reason })
      onClose()
      navigate(0)
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Merge duplicate patient"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button variant="danger" onClick={submit} loading={merge.isPending} disabled={!selected || !reason.trim()}>
            Merge into this patient
          </Button>
        </>
      }
    >
      <p className="mb-3 text-sm text-slate-600">
        Merging moves all contacts, documents and consents from the selected record onto this patient, then
        deactivates the duplicate. This cannot be undone.
      </p>

      {isLoading && <Spinner />}
      {!isLoading && candidates.length === 0 && (
        <p className="text-sm text-slate-400">No likely duplicates were found automatically.</p>
      )}
      <div className="space-y-2">
        {candidates.map((candidate) => (
          <label
            key={candidate.id}
            className={`flex cursor-pointer items-center justify-between rounded-md border px-3 py-2 ${
              selected === candidate.id ? 'border-primary-400 bg-primary-50' : 'border-slate-200'
            }`}
          >
            <div>
              <p className="text-sm font-medium text-slate-800">
                {candidate.fullName} <span className="font-mono text-xs text-slate-500">({candidate.uhid})</span>
              </p>
              <p className="text-xs text-slate-500">{candidate.matchReasons.join(', ')}</p>
            </div>
            <input type="radio" checked={selected === candidate.id} onChange={() => setSelected(candidate.id)} />
          </label>
        ))}
      </div>

      <div className="mt-3">
        <Input label="Reason for merge" value={reason} onChange={(e) => setReason(e.target.value)} />
      </div>
    </Modal>
  )
}
