import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import {
  compactErrors,
  hasErrors,
  isBlank,
  mobileError,
  optionalEmailError,
  optionalMobileError,
  requiredSelect,
  requiredText,
  type FieldErrors,
} from '../../../lib/formValidation'
import { endpoints } from '../../../api/endpoints'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { useAreas, useSimpleMaster, useReferrers } from '../../masters/api'
import { useCheckDuplicates, useRegisterPatient, useUploadPatientPhoto } from '../api'
import { PatientPhotoCapture } from '../components/PatientPhotoCapture'
import {
  CONTACT_TYPE_LABELS,
  CONTACT_TYPES,
  GENDER_LABELS,
  GENDERS,
  IDENTIFIER_TYPE_LABELS,
  IDENTIFIER_TYPES,
  type ContactType,
  type DuplicateCandidate,
  type Gender,
  type IdentifierType,
  type SavePatientContact,
  type SavePatientIdentifier,
  type SavePatientInsurancePolicy,
} from '../types'
import { DuplicateWarningModal } from '../components/DuplicateWarningModal'

const steps = ['Demographics', 'Contact & Address', 'Guardian & IDs', 'Review'] as const

interface FormState {
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
  contacts: SavePatientContact[]
  identifiers: SavePatientIdentifier[]
  insurancePolicies: SavePatientInsurancePolicy[]
}

const emptyForm: FormState = {
  salutationId: '',
  firstName: '',
  middleName: '',
  lastName: '',
  gender: GENDERS.Male,
  dobKnown: true,
  dateOfBirth: '',
  estimatedAge: '',
  bloodGroup: '',
  maritalStatusId: '',
  languageId: '',
  patientCaseTypeId: '',
  patientHcTagId: '',
  speciallyAbledCategoryId: '',
  isVip: false,
  smsOptIn: true,
  occupation: '',
  monthlyIncome: '',
  motherName: '',
  nationalId: '',
  mobile: '',
  alternateMobile: '',
  whatsAppNumber: '',
  email: '',
  address: '',
  permanentAddress: '',
  isPermanentSameAsPresent: true,
  countryId: '',
  nationalityId: '',
  stateId: '',
  cityId: '',
  areaId: '',
  pincode: '',
  patientCategoryId: '',
  referrerId: '',
  insuranceRequired: false,
  remarks: '',
  contacts: [],
  identifiers: [],
  insurancePolicies: [],
}

const emptyContact: SavePatientContact = {
  contactType: CONTACT_TYPES.Guardian,
  name: '',
  relationship: '',
  phone: '',
  email: '',
  address: '',
  isPrimary: false,
}

const emptyInsurancePolicy: SavePatientInsurancePolicy = {
  insuranceCompanyId: 0,
  policyNumber: '',
  policyValidFrom: null,
  policyValidTo: null,
  isPrimary: false,
}

export function PatientRegistrationWizard() {
  const navigate = useNavigate()
  const [step, setStep] = useState(0)
  const [form, setForm] = useState<FormState>(emptyForm)
  const [photoFile, setPhotoFile] = useState<File | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [duplicates, setDuplicates] = useState<DuplicateCandidate[] | null>(null)

  const { data: categories = [] } = useSimpleMaster('patientcategories', endpoints.masters.patientCategories)
  const { data: referrersResult } = useReferrers({ page: 1, pageSize: 100 })
  const { data: salutations = [] } = useSimpleMaster('salutations', endpoints.masters.salutations)
  const { data: maritalStatuses = [] } = useSimpleMaster('marital-statuses', endpoints.masters.maritalStatuses)
  const { data: languages = [] } = useSimpleMaster('languages', endpoints.masters.languages)
  const { data: patientCaseTypes = [] } = useSimpleMaster('patient-case-types', endpoints.masters.patientCaseTypes)
  const { data: patientHcTags = [] } = useSimpleMaster('patient-hc-tags', endpoints.masters.patientHcTags)
  const { data: speciallyAbledCategories = [] } = useSimpleMaster(
    'specially-abled-categories', endpoints.masters.speciallyAbledCategories,
  )
  const { data: countries = [] } = useSimpleMaster('nationalities', endpoints.masters.nationalities)
  const { data: states = [] } = useSimpleMaster('states', endpoints.masters.states)
  const { data: cities = [] } = useSimpleMaster('cities', endpoints.masters.cities)
  const { data: areas = [] } = useAreas(form.cityId ? Number(form.cityId) : undefined)
  const { data: identityDocumentTypes = [] } = useSimpleMaster(
    'identity-document-types', endpoints.masters.identityDocumentTypes,
  )
  const { data: insuranceCompanies = [] } = useSimpleMaster('insurance-companies', endpoints.masters.insuranceCompanies)
  const checkDuplicates = useCheckDuplicates()
  const register = useRegisterPatient()
  const uploadPhoto = useUploadPatientPhoto()

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => setForm((f) => ({ ...f, [key]: value }))

  /**
   * Mirrors RegisterPatientRequestValidator (and the contact/identifier/policy child
   * validators) so staff see what is missing on the step they are standing on, instead
   * of one server message at a time after a four-step round trip. The API is still the gate.
   */
  const validateStep = (target: number): FieldErrors => {
    if (target === 0) {
      return compactErrors({
        firstName: requiredText(form.firstName, 'First name'),
        lastName: requiredText(form.lastName, 'Last name'),
        dateOfBirth: form.dobKnown ? requiredText(form.dateOfBirth, 'Date of birth') : undefined,
        estimatedAge: form.dobKnown ? undefined : requiredText(form.estimatedAge, 'Estimated age'),
      })
    }
    if (target === 1) {
      return compactErrors({
        mobile: mobileError(form.mobile),
        alternateMobile: optionalMobileError(form.alternateMobile),
        whatsAppNumber: optionalMobileError(form.whatsAppNumber),
        email: optionalEmailError(form.email),
      })
    }
    if (target === 2) {
      const errors: FieldErrors = {}
      form.contacts.forEach((contact, index) => {
        const name = requiredText(contact.name, 'Name')
        if (name) errors[`contact-${index}-name`] = name
        const phone = optionalMobileError(contact.phone)
        if (phone) errors[`contact-${index}-phone`] = phone
      })
      form.identifiers.forEach((identifier, index) => {
        // A wholly blank row is dropped from the payload, so only flag a half-filled one —
        // otherwise the number silently vanishes on save.
        if (isBlank(identifier.value) && identifier.identityDocumentTypeId === null) return
        const value = requiredText(identifier.value, 'ID number')
        if (value) errors[`identifier-${index}-value`] = value
        if (identifier.type === IDENTIFIER_TYPES.Other && !identifier.identityDocumentTypeId) {
          errors[`identifier-${index}-type`] = 'Select a document type.'
        }
      })
      if (form.insuranceRequired) {
        if (form.insurancePolicies.length === 0) {
          errors.insurancePolicies = 'Add at least one policy, or uncheck Insurance required.'
        }
        form.insurancePolicies.forEach((policy, index) => {
          const insurer = requiredSelect(policy.insuranceCompanyId, 'Insurer / TPA')
          if (insurer) errors[`policy-${index}-insurer`] = insurer
          const number = requiredText(policy.policyNumber, 'Policy number')
          if (number) errors[`policy-${index}-number`] = number
        })
      }
      return errors
    }
    return {}
  }

  const goNext = () => {
    const errors = validateStep(step)
    setFieldErrors(errors)
    if (hasErrors(errors)) {
      setError('Check the highlighted fields below.')
      return
    }
    setError(null)
    setStep((s) => Math.min(s + 1, steps.length - 1))
  }
  const goBack = () => {
    setError(null)
    setFieldErrors({})
    setStep((s) => Math.max(s - 1, 0))
  }

  const buildPayload = () => ({
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
    contacts: form.contacts,
    identifiers: form.identifiers.filter((i) => i.value.trim().length > 0),
    insurancePolicies: form.insuranceRequired
      ? form.insurancePolicies.filter((p) => p.insuranceCompanyId > 0 && p.policyNumber.trim().length > 0)
      : [],
  })

  const submitRegistration = async (confirmDuplicate = false) => {
    setError(null)
    try {
      const result = await register.mutateAsync({ ...buildPayload(), confirmDuplicate })
      if (photoFile) {
        // Best-effort — the patient record is already saved; a photo-upload hiccup shouldn't
        // block registration. Staff can add/retake it from the patient detail page afterwards.
        await uploadPhoto.mutateAsync({ patientId: result.id, file: photoFile }).catch(() => {})
      }
      navigate(`/patients/${result.id}`, { state: { justRegistered: true } })
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const checkAndRegister = async () => {
    setError(null)
    // Review is reachable from Back, so re-check the earlier steps and jump to the first
    // one that is short rather than letting the server reject the whole registration.
    for (const target of [0, 1, 2]) {
      const errors = validateStep(target)
      if (hasErrors(errors)) {
        setFieldErrors(errors)
        setStep(target)
        setError('Check the highlighted fields below.')
        return
      }
    }
    setFieldErrors({})
    try {
      const candidates = await checkDuplicates.mutateAsync({
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        dateOfBirth: form.dobKnown && form.dateOfBirth ? form.dateOfBirth : null,
        mobile: form.mobile.trim() || null,
        excludePatientId: null,
      })
      if (candidates.length > 0) {
        setDuplicates(candidates)
      } else {
        await submitRegistration()
      }
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const addContact = () => set('contacts', [...form.contacts, { ...emptyContact }])
  const updateContact = (index: number, patch: Partial<SavePatientContact>) =>
    set('contacts', form.contacts.map((c, i) => (i === index ? { ...c, ...patch } : c)))
  const removeContact = (index: number) => set('contacts', form.contacts.filter((_, i) => i !== index))

  const addIdentifier = () =>
    set('identifiers', [...form.identifiers, { type: IDENTIFIER_TYPES.Aadhaar, identityDocumentTypeId: null, value: '' }])
  const updateIdentifier = (index: number, patch: Partial<SavePatientIdentifier>) =>
    set('identifiers', form.identifiers.map((idf, i) => (i === index ? { ...idf, ...patch } : idf)))
  const removeIdentifier = (index: number) => set('identifiers', form.identifiers.filter((_, i) => i !== index))

  const addInsurancePolicy = () => set('insurancePolicies', [...form.insurancePolicies, { ...emptyInsurancePolicy }])
  const updateInsurancePolicy = (index: number, patch: Partial<SavePatientInsurancePolicy>) =>
    set('insurancePolicies', form.insurancePolicies.map((p, i) => (i === index ? { ...p, ...patch } : p)))
  const removeInsurancePolicy = (index: number) =>
    set('insurancePolicies', form.insurancePolicies.filter((_, i) => i !== index))

  return (
    <>
      <PageHeader title="New Patient Registration" subtitle={`Step ${step + 1} of ${steps.length}: ${steps[step]}`} />

      <div className="mb-6 flex gap-1">
        {steps.map((label, index) => (
          <div
            key={label}
            className={`h-1.5 flex-1 rounded-full ${index <= step ? 'bg-primary-600' : 'bg-slate-200'}`}
          />
        ))}
      </div>

      <div className="max-w-2xl rounded-lg border border-slate-200 bg-white p-6">
        {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

        {step === 0 && (
          <div className="space-y-3">
            <div className="grid grid-cols-4 gap-3">
              <Select
                label="Salutation"
                placeholder="—"
                options={salutations.map((s) => ({ value: s.id, label: s.name }))}
                value={form.salutationId}
                onChange={(e) => set('salutationId', e.target.value)}
              />
              <Input
                label="First name"
                required
                error={fieldErrors.firstName}
                value={form.firstName}
                onChange={(e) => set('firstName', e.target.value)}
              />
              <Input label="Middle name" value={form.middleName} onChange={(e) => set('middleName', e.target.value)} />
              <Input
                label="Last name"
                required
                error={fieldErrors.lastName}
                value={form.lastName}
                onChange={(e) => set('lastName', e.target.value)}
              />
            </div>
            <div className="grid grid-cols-3 gap-3">
              <Select
                label="Gender"
                options={Object.entries(GENDER_LABELS).map(([value, label]) => ({ value, label }))}
                value={String(form.gender)}
                onChange={(e) => set('gender', Number(e.target.value) as Gender)}
              />
              <Input label="Blood group" placeholder="e.g. O+" value={form.bloodGroup} onChange={(e) => set('bloodGroup', e.target.value)} />
              <Select
                label="Marital status"
                placeholder="—"
                options={maritalStatuses.map((s) => ({ value: s.id, label: s.name }))}
                value={form.maritalStatusId}
                onChange={(e) => set('maritalStatusId', e.target.value)}
              />
            </div>
            <div>
              <div className="mb-1 flex items-center gap-4">
                <span className="text-sm font-medium text-slate-700">
                  Age
                  <span className="ml-0.5 text-crit" title="Required">
                    *
                  </span>
                </span>
                <label className="flex items-center gap-1.5 text-sm text-slate-600">
                  <input type="radio" checked={form.dobKnown} onChange={() => set('dobKnown', true)} />
                  Date of birth
                </label>
                <label className="flex items-center gap-1.5 text-sm text-slate-600">
                  <input type="radio" checked={!form.dobKnown} onChange={() => set('dobKnown', false)} />
                  Estimated age
                </label>
              </div>
              {form.dobKnown ? (
                <Input
                  type="date"
                  required
                  error={fieldErrors.dateOfBirth}
                  value={form.dateOfBirth}
                  onChange={(e) => set('dateOfBirth', e.target.value)}
                />
              ) : (
                <Input
                  type="number"
                  min={0}
                  max={130}
                  required
                  error={fieldErrors.estimatedAge}
                  placeholder="Age in years"
                  value={form.estimatedAge}
                  onChange={(e) => set('estimatedAge', e.target.value)}
                />
              )}
            </div>
            <Select
              label="Language"
              placeholder="—"
              options={languages.map((l) => ({ value: l.id, label: l.name }))}
              value={form.languageId}
              onChange={(e) => set('languageId', e.target.value)}
            />
            <div className="grid grid-cols-2 gap-3">
              <Select
                label="Patient case type"
                placeholder="—"
                options={patientCaseTypes.map((c) => ({ value: c.id, label: c.name }))}
                value={form.patientCaseTypeId}
                onChange={(e) => set('patientCaseTypeId', e.target.value)}
              />
              <Select
                label="Patient tag"
                placeholder="—"
                options={patientHcTags.map((t) => ({ value: t.id, label: t.name }))}
                value={form.patientHcTagId}
                onChange={(e) => set('patientHcTagId', e.target.value)}
              />
            </div>
            <Select
              label="Specially abled category"
              placeholder="— none —"
              options={speciallyAbledCategories.map((c) => ({ value: c.id, label: c.name }))}
              value={form.speciallyAbledCategoryId}
              onChange={(e) => set('speciallyAbledCategoryId', e.target.value)}
            />
            <div className="flex items-center gap-6">
              <label className="flex items-center gap-1.5 text-sm text-slate-600">
                <input type="checkbox" checked={form.isVip} onChange={(e) => set('isVip', e.target.checked)} />
                VIP
              </label>
              <label className="flex items-center gap-1.5 text-sm text-slate-600">
                <input type="checkbox" checked={form.smsOptIn} onChange={(e) => set('smsOptIn', e.target.checked)} />
                SMS updates
              </label>
            </div>
            <div className="grid grid-cols-3 gap-3">
              <Input label="Occupation" value={form.occupation} onChange={(e) => set('occupation', e.target.value)} />
              <Input
                label="Monthly income"
                type="number"
                min={0}
                value={form.monthlyIncome}
                onChange={(e) => set('monthlyIncome', e.target.value)}
              />
              <Input label="Mother's name" value={form.motherName} onChange={(e) => set('motherName', e.target.value)} />
            </div>
          </div>
        )}

        {step === 1 && (
          <div className="space-y-3">
            <div className="grid grid-cols-3 gap-3">
              <Input
                label="Mobile"
                required
                error={fieldErrors.mobile}
                value={form.mobile}
                onChange={(e) => set('mobile', e.target.value)}
              />
              <Input
                label="Alternate mobile"
                error={fieldErrors.alternateMobile}
                value={form.alternateMobile}
                onChange={(e) => set('alternateMobile', e.target.value)}
              />
              <Input
                label="WhatsApp number"
                error={fieldErrors.whatsAppNumber}
                value={form.whatsAppNumber}
                onChange={(e) => set('whatsAppNumber', e.target.value)}
              />
            </div>
            <Input
              label="Email"
              type="email"
              error={fieldErrors.email}
              value={form.email}
              onChange={(e) => set('email', e.target.value)}
            />
            <Input label="Present address" value={form.address} onChange={(e) => set('address', e.target.value)} />
            <label className="flex items-center gap-1.5 text-sm text-slate-600">
              <input
                type="checkbox"
                checked={form.isPermanentSameAsPresent}
                onChange={(e) => set('isPermanentSameAsPresent', e.target.checked)}
              />
              Permanent address same as present address
            </label>
            {!form.isPermanentSameAsPresent && (
              <Input
                label="Permanent address"
                value={form.permanentAddress}
                onChange={(e) => set('permanentAddress', e.target.value)}
              />
            )}
            <div className="grid grid-cols-3 gap-3">
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
              <Input label="Pincode" value={form.pincode} onChange={(e) => set('pincode', e.target.value)} />
            </div>
            <div className="grid grid-cols-3 gap-3">
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
            <Input label="National ID" value={form.nationalId} onChange={(e) => set('nationalId', e.target.value)} />
            <div className="grid grid-cols-2 gap-3">
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
          </div>
        )}

        {step === 2 && (
          <div className="space-y-5">
            <div>
              <div className="mb-2 flex items-center justify-between">
                <span className="text-sm font-semibold text-slate-700">Guardian / Attender</span>
                <Button variant="secondary" onClick={addContact}>
                  Add contact
                </Button>
              </div>
              {form.contacts.length === 0 && <p className="text-sm text-slate-400">No guardian or attender added.</p>}
              <div className="space-y-3">
                {form.contacts.map((contact, index) => (
                  <div key={index} className="rounded-md border border-slate-200 p-3">
                    <div className="grid grid-cols-2 gap-2">
                      <Select
                        label="Type"
                        options={Object.entries(CONTACT_TYPE_LABELS).map(([value, label]) => ({ value, label }))}
                        value={String(contact.contactType)}
                        onChange={(e) => updateContact(index, { contactType: Number(e.target.value) as ContactType })}
                      />
                      <Input
                        label="Name"
                        required
                        error={fieldErrors[`contact-${index}-name`]}
                        value={contact.name}
                        onChange={(e) => updateContact(index, { name: e.target.value })}
                      />
                      <Input
                        label="Relationship"
                        value={contact.relationship ?? ''}
                        onChange={(e) => updateContact(index, { relationship: e.target.value })}
                      />
                      <Input
                        label="Phone"
                        error={fieldErrors[`contact-${index}-phone`]}
                        value={contact.phone ?? ''}
                        onChange={(e) => updateContact(index, { phone: e.target.value })}
                      />
                    </div>
                    <div className="mt-2 flex items-center justify-between">
                      <label className="flex items-center gap-1.5 text-sm text-slate-600">
                        <input
                          type="checkbox"
                          checked={contact.isPrimary}
                          onChange={(e) => updateContact(index, { isPrimary: e.target.checked })}
                        />
                        Primary contact
                      </label>
                      <Button variant="ghost" className="text-red-600" onClick={() => removeContact(index)}>
                        Remove
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div>
              <div className="mb-2 flex items-center justify-between">
                <span className="text-sm font-semibold text-slate-700">Identity documents</span>
                <Button variant="secondary" onClick={addIdentifier}>
                  Add ID
                </Button>
              </div>
              <p className="mb-2 text-xs text-slate-400">
                Aadhaar/ABHA are capture only — verification against UIDAI/ABDM is a future integration.
              </p>
              <div className="space-y-2">
                {form.identifiers.map((identifier, index) => (
                  <div key={index} className="flex items-end gap-2">
                    <Select
                      label="Type"
                      options={Object.entries(IDENTIFIER_TYPE_LABELS).map(([value, label]) => ({ value, label }))}
                      value={String(identifier.type)}
                      onChange={(e) => updateIdentifier(index, { type: Number(e.target.value) as IdentifierType })}
                      className="w-28"
                    />
                    {identifier.type === IDENTIFIER_TYPES.Other && (
                      <Select
                        label="Doc type"
                        required
                        error={fieldErrors[`identifier-${index}-type`]}
                        placeholder="—"
                        options={identityDocumentTypes.map((t) => ({ value: t.id, label: t.name }))}
                        value={identifier.identityDocumentTypeId ?? ''}
                        onChange={(e) =>
                          updateIdentifier(index, { identityDocumentTypeId: e.target.value ? Number(e.target.value) : null })
                        }
                        className="w-40"
                      />
                    )}
                    <Input
                      label="Number"
                      required
                      error={fieldErrors[`identifier-${index}-value`]}
                      className="flex-1"
                      value={identifier.value}
                      onChange={(e) => updateIdentifier(index, { value: e.target.value })}
                    />
                    <Button variant="secondary" disabled title="Verification integration not yet available">
                      Verify
                    </Button>
                    <Button variant="ghost" className="text-red-600" onClick={() => removeIdentifier(index)}>
                      Remove
                    </Button>
                  </div>
                ))}
              </div>
            </div>

            <div>
              <label className="mb-2 flex items-center gap-1.5 text-sm font-semibold text-slate-700">
                <input
                  type="checkbox"
                  checked={form.insuranceRequired}
                  onChange={(e) => set('insuranceRequired', e.target.checked)}
                />
                Insurance required
              </label>
              {form.insuranceRequired && (
                <>
                  {fieldErrors.insurancePolicies && (
                    <p role="alert" className="mb-2 text-xs font-medium text-crit">
                      {fieldErrors.insurancePolicies}
                    </p>
                  )}
                  <div className="mb-2 flex justify-end">
                    <Button variant="secondary" onClick={addInsurancePolicy}>
                      Add policy
                    </Button>
                  </div>
                  <div className="space-y-2">
                    {form.insurancePolicies.map((policy, index) => (
                      <div key={index} className="flex items-end gap-2">
                        <Select
                          label="Insurer / TPA"
                          required
                          error={fieldErrors[`policy-${index}-insurer`]}
                          placeholder="—"
                          options={insuranceCompanies.map((c) => ({ value: c.id, label: c.name }))}
                          value={policy.insuranceCompanyId || ''}
                          onChange={(e) => updateInsurancePolicy(index, { insuranceCompanyId: Number(e.target.value) })}
                          className="w-48"
                        />
                        <Input
                          label="Policy number"
                          required
                          error={fieldErrors[`policy-${index}-number`]}
                          className="flex-1"
                          value={policy.policyNumber}
                          onChange={(e) => updateInsurancePolicy(index, { policyNumber: e.target.value })}
                        />
                        <Input
                          label="Valid to"
                          type="date"
                          value={policy.policyValidTo ?? ''}
                          onChange={(e) => updateInsurancePolicy(index, { policyValidTo: e.target.value || null })}
                        />
                        <Button variant="ghost" className="text-red-600" onClick={() => removeInsurancePolicy(index)}>
                          Remove
                        </Button>
                      </div>
                    ))}
                  </div>
                </>
              )}
            </div>
          </div>
        )}

        {step === 3 && (
          <div className="space-y-4 text-sm">
            <div className="rounded-md border border-slate-200 p-3">
              <p className="mb-2 text-xs font-semibold uppercase text-slate-500">Photo (optional)</p>
              <PatientPhotoCapture patientId={null} personLabel="new-patient" onCaptured={setPhotoFile} compact />
            </div>
            <p>
              <span className="text-slate-500">Name:</span>{' '}
              <span className="font-medium">
                {[form.firstName, form.middleName, form.lastName].filter(Boolean).join(' ')}
              </span>
            </p>
            <p>
              <span className="text-slate-500">Gender:</span> {GENDER_LABELS[form.gender]}
            </p>
            <p>
              <span className="text-slate-500">Age:</span>{' '}
              {form.dobKnown ? form.dateOfBirth || '—' : `~${form.estimatedAge || '—'} yrs (estimated)`}
            </p>
            <p>
              <span className="text-slate-500">Mobile:</span> {form.mobile}
            </p>
            <p>
              <span className="text-slate-500">Address:</span>{' '}
              {[form.address, form.pincode].filter(Boolean).join(', ') || '—'}
            </p>
            <p>
              <span className="text-slate-500">Contacts:</span> {form.contacts.length}
            </p>
            <p>
              <span className="text-slate-500">Identifiers:</span> {form.identifiers.length}
            </p>
            <p>
              <span className="text-slate-500">Insurance policies:</span> {form.insurancePolicies.length}
            </p>
          </div>
        )}

        <div className="mt-6 flex justify-between border-t border-slate-100 pt-4">
          <Button variant="secondary" onClick={goBack} disabled={step === 0}>
            Back
          </Button>
          {step < steps.length - 1 ? (
            <Button onClick={goNext}>Next</Button>
          ) : (
            <Button onClick={checkAndRegister} loading={checkDuplicates.isPending || register.isPending}>
              Check &amp; Register
            </Button>
          )}
        </div>
      </div>

      {duplicates && (
        <DuplicateWarningModal
          candidates={duplicates}
          loading={register.isPending}
          onCancel={() => setDuplicates(null)}
          onProceed={async () => {
            await submitRegistration(true)
            setDuplicates(null)
          }}
        />
      )}
    </>
  )
}
