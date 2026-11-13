import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import '../registration.css'
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
import { Input } from '../../../components/ui/Input'
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

/** Per-step copy for the stepper, hero and form header. */
const stepMeta = [
  { short: 'Name, age, gender', hero: 'Demographics — identity & basics', title: 'Patient demographics', sub: 'Fields marked * are required to register' },
  { short: 'Phone, address, category', hero: 'Contact & address', title: 'Contact & address', sub: 'Mobile is used for tokens, SMS and reports' },
  { short: 'Kin, IDs, insurance', hero: 'Guardian, identity & insurance', title: 'Guardian, IDs & insurance', sub: 'Attender contacts, identity documents and policies' },
  { short: 'Verify & register', hero: 'Review & confirm', title: 'Review & confirm', sub: 'Check the details, add a photo, then register' },
] as const

/** Whole years between a yyyy-mm-dd date of birth and today, or null if unparsable/future. */
function ageFromDob(dob: string): number | null {
  if (!dob) return null
  const birth = new Date(`${dob}T00:00:00`)
  if (Number.isNaN(birth.getTime())) return null
  const now = new Date()
  let age = now.getFullYear() - birth.getFullYear()
  const m = now.getMonth() - birth.getMonth()
  if (m < 0 || (m === 0 && now.getDate() < birth.getDate())) age--
  return age >= 0 ? age : null
}

/** Accessible on/off card used for VIP / SMS / Insurance toggles. */
function SwitchCard(props: { on: boolean; onToggle: () => void; title: ReactNode; sub: string; gold?: boolean }) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={props.on}
      onClick={props.onToggle}
      className={`reg-switch${props.gold ? ' gold' : ''}${props.on ? ' on' : ''}`}
    >
      <span className="reg-sw" aria-hidden />
      <span>
        <b>{props.title}</b>
        <small>{props.sub}</small>
      </span>
    </button>
  )
}

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

  // ── Derived view state (live preview + completeness) ───────────────────────
  const [photoUrl, setPhotoUrl] = useState<string | null>(null)
  useEffect(() => {
    if (!photoFile) {
      setPhotoUrl(null)
      return
    }
    const url = URL.createObjectURL(photoFile)
    setPhotoUrl(url)
    return () => URL.revokeObjectURL(url)
  }, [photoFile])

  const nameOf = (list: { id: number; name: string }[], id: string) =>
    id ? (list.find((x) => String(x.id) === id)?.name ?? '—') : '—'

  const fullName = [nameOf(salutations, form.salutationId), form.firstName, form.middleName, form.lastName]
    .filter((p) => p && p !== '—')
    .join(' ')
    .trim()
  const initials = [form.firstName, form.lastName]
    .map((p) => p.trim().charAt(0).toUpperCase())
    .join('')
  const age = form.dobKnown ? ageFromDob(form.dateOfBirth) : form.estimatedAge ? Number(form.estimatedAge) : null
  const ageText = age === null ? 'Age —' : `${age} yrs${form.dobKnown ? '' : ' (est.)'}`

  const checks = useMemo(
    () => [
      { label: 'Patient full name', ok: !isBlank(form.firstName) && !isBlank(form.lastName) },
      { label: 'Age / date of birth', ok: form.dobKnown ? !isBlank(form.dateOfBirth) : !isBlank(form.estimatedAge) },
      { label: 'Mobile number', ok: !isBlank(form.mobile) && !mobileError(form.mobile) },
      { label: 'Blood group', ok: !isBlank(form.bloodGroup) },
      { label: 'Address', ok: !isBlank(form.address) },
      { label: 'Patient photo', ok: !!photoFile },
    ],
    [form.firstName, form.lastName, form.dobKnown, form.dateOfBirth, form.estimatedAge, form.mobile, form.bloodGroup, form.address, photoFile],
  )
  const requiredDone = checks.slice(0, 3).filter((c) => c.ok).length
  const completeness = Math.round((checks.filter((c) => c.ok).length / checks.length) * 100)
  const progressPct = ((step + 1) / steps.length) * 100
  const submitting = checkDuplicates.isPending || register.isPending

  const jumpTo = (target: number) => {
    if (target < step) {
      setError(null)
      setFieldErrors({})
      setStep(target)
    } else if (target === step + 1) {
      goNext()
    }
  }

  const ageField = (
    <div>
      <span className="reg-label">
        Age <span className="reg-req">*</span>
      </span>
      <div className="reg-radioRow">
        <label className="reg-radio">
          <input type="radio" checked={form.dobKnown} onChange={() => set('dobKnown', true)} />
          Date of birth
        </label>
        <label className="reg-radio">
          <input type="radio" checked={!form.dobKnown} onChange={() => set('dobKnown', false)} />
          Estimated age
        </label>
      </div>
      {form.dobKnown ? (
        <div className="reg-dateWrap">
          <Input
            type="date"
            required
            aria-label="Date of birth"
            max={new Date().toISOString().slice(0, 10)}
            error={fieldErrors.dateOfBirth}
            value={form.dateOfBirth}
            onChange={(e) => set('dateOfBirth', e.target.value)}
          />
          {age !== null && !fieldErrors.dateOfBirth && <span className="reg-ageEcho">{age} yrs</span>}
        </div>
      ) : (
        <Input
          type="number"
          min={0}
          max={130}
          required
          aria-label="Estimated age"
          error={fieldErrors.estimatedAge}
          placeholder="Age in years, e.g. 34"
          value={form.estimatedAge}
          onChange={(e) => set('estimatedAge', e.target.value)}
        />
      )}
      <p className="reg-hint">Age is calculated automatically from the date of birth.</p>
    </div>
  )

  return (
    <div className="reg">
      <div className="reg-tabs">
        <span className="reg-miniTab">
          <i>◈</i> OP Registration
        </span>
        <span className="reg-crumbs">Front Office / Patients / New</span>
        <button type="button" className="reg-linkBtn" onClick={() => navigate('/patients')}>
          ⌕ Find existing patient
        </button>
      </div>

      <section className="reg-hero">
        <div className="reg-heroTop">
          <div className="reg-heroTitle">
            <h1 className="reg-display">
              New Patient <span className="reg-gold">Registration</span>
            </h1>
            <p>
              <span className="reg-stepPill">
                STEP {step + 1} OF {steps.length}
              </span>
              <span>{stepMeta[step].hero}</span>
            </p>
          </div>
          <div className="reg-heroStats">
            <div className={`reg-stat${requiredDone === 3 ? ' good' : ''}`}>
              <small>Required</small>
              <b className="reg-display">{requiredDone} / 3</b>
              <em>name · age · mobile</em>
            </div>
            <div className="reg-stat">
              <small>Completeness</small>
              <b className="reg-display">{completeness}%</b>
              <em>of key details</em>
            </div>
            <div className="reg-stat good">
              <small>UHID</small>
              <b className="reg-display">● Auto</b>
              <em>assigned on register</em>
            </div>
          </div>
        </div>

        <div className="reg-stepper">
          {steps.map((label, index) => {
            const state = index === step ? 'current' : index < step ? 'done' : ''
            return (
              <button
                key={label}
                type="button"
                className={`reg-step ${state}`}
                aria-current={index === step ? 'step' : undefined}
                disabled={index > step + 1}
                onClick={() => jumpTo(index)}
              >
                <span className="n">{index < step ? '✓' : `0${index + 1}`}</span>
                <span>
                  <b>{label}</b>
                  <small>{stepMeta[index].short}</small>
                </span>
              </button>
            )
          })}
        </div>
        <div className="reg-progressWrap">
          <div className="reg-progressMeta">
            <span>
              Step {step + 1} of {steps.length} • {steps[step]}
            </span>
            <span>{progressPct}% complete</span>
          </div>
          <div className="reg-progress">
            <i style={{ width: `${progressPct}%` }} />
          </div>
        </div>
      </section>

      <div className="reg-layout">
        <section className="reg-card reg-formCard">
          <div className="reg-formHead">
            <div className="fi" aria-hidden>
              ◍
            </div>
            <div>
              <b>{stepMeta[step].title}</b>
              <small>{stepMeta[step].sub}</small>
            </div>
            <div className="reg-uhid">
              <small>UHID</small>
              <br />
              <b>Assigned on save</b>
              <em className="reg-pill">● New</em>
            </div>
          </div>

          {error && (
            <p role="alert" className="reg-alert">
              {error}
            </p>
          )}

          {step === 0 && (
            <div className="reg-formBody reg-stack" key="s0">
              <div className="reg-grid4">
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
                  placeholder="e.g. Aarav"
                  autoComplete="off"
                  error={fieldErrors.firstName}
                  value={form.firstName}
                  onChange={(e) => set('firstName', e.target.value)}
                />
                <Input
                  label="Middle name"
                  placeholder="Optional"
                  autoComplete="off"
                  value={form.middleName}
                  onChange={(e) => set('middleName', e.target.value)}
                />
                <Input
                  label="Last name"
                  required
                  placeholder="e.g. Sharma"
                  autoComplete="off"
                  error={fieldErrors.lastName}
                  value={form.lastName}
                  onChange={(e) => set('lastName', e.target.value)}
                />
              </div>
              <div className="reg-grid3">
                <Select
                  label="Gender"
                  options={Object.entries(GENDER_LABELS).map(([value, label]) => ({ value, label }))}
                  value={String(form.gender)}
                  onChange={(e) => set('gender', Number(e.target.value) as Gender)}
                />
                <div>
                  <Input
                    label="Blood group"
                    placeholder="e.g. O+"
                    value={form.bloodGroup}
                    onChange={(e) => set('bloodGroup', e.target.value)}
                  />
                  <p className="reg-hint">Used for emergency &amp; lab matching</p>
                </div>
                <Select
                  label="Marital status"
                  placeholder="—"
                  options={maritalStatuses.map((s) => ({ value: s.id, label: s.name }))}
                  value={form.maritalStatusId}
                  onChange={(e) => set('maritalStatusId', e.target.value)}
                />
              </div>
              {ageField}
              <Select
                label="Language"
                placeholder="— Select preferred language —"
                options={languages.map((l) => ({ value: l.id, label: l.name }))}
                value={form.languageId}
                onChange={(e) => set('languageId', e.target.value)}
              />
              <div className="reg-grid2">
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
              <div className="reg-switchRow">
                <SwitchCard
                  gold
                  on={form.isVip}
                  onToggle={() => set('isVip', !form.isVip)}
                  title={
                    <>
                      <span className="reg-crown">♛</span> VIP
                    </>
                  }
                  sub="Flag the patient as VIP"
                />
                <SwitchCard
                  on={form.smsOptIn}
                  onToggle={() => set('smsOptIn', !form.smsOptIn)}
                  title="✉ SMS updates"
                  sub="Token, report & payment alerts"
                />
              </div>
              <div className="reg-grid3">
                <Input
                  label="Occupation"
                  placeholder="e.g. Teacher"
                  value={form.occupation}
                  onChange={(e) => set('occupation', e.target.value)}
                />
                <Input
                  label="Monthly income"
                  type="number"
                  min={0}
                  placeholder="e.g. 45000"
                  value={form.monthlyIncome}
                  onChange={(e) => set('monthlyIncome', e.target.value)}
                />
                <Input
                  label="Mother's name"
                  placeholder="For minor / verification"
                  value={form.motherName}
                  onChange={(e) => set('motherName', e.target.value)}
                />
              </div>
            </div>
          )}

          {step === 1 && (
            <div className="reg-formBody reg-stack" key="s1">
              <div className="reg-grid3">
                <Input
                  label="Mobile"
                  required
                  inputMode="tel"
                  placeholder="10-digit mobile"
                  error={fieldErrors.mobile}
                  value={form.mobile}
                  onChange={(e) => set('mobile', e.target.value)}
                />
                <Input
                  label="Alternate mobile"
                  inputMode="tel"
                  placeholder="Optional"
                  error={fieldErrors.alternateMobile}
                  value={form.alternateMobile}
                  onChange={(e) => set('alternateMobile', e.target.value)}
                />
                <Input
                  label="WhatsApp number"
                  inputMode="tel"
                  placeholder="Optional"
                  error={fieldErrors.whatsAppNumber}
                  value={form.whatsAppNumber}
                  onChange={(e) => set('whatsAppNumber', e.target.value)}
                />
              </div>
              <Input
                label="Email"
                type="email"
                placeholder="name@mail.com"
                error={fieldErrors.email}
                value={form.email}
                onChange={(e) => set('email', e.target.value)}
              />
              <Input
                label="Present address"
                placeholder="House / street / locality"
                value={form.address}
                onChange={(e) => set('address', e.target.value)}
              />
              <label className="reg-check">
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
              <div className="reg-grid3">
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
                <Input
                  label="Pincode"
                  inputMode="numeric"
                  placeholder="e.g. 600001"
                  value={form.pincode}
                  onChange={(e) => set('pincode', e.target.value)}
                />
              </div>
              <div className="reg-grid3">
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
                  placeholder={form.cityId ? '—' : 'Select a city first'}
                  options={areas.map((a) => ({ value: a.id, label: a.name }))}
                  value={form.areaId}
                  onChange={(e) => set('areaId', e.target.value)}
                  disabled={!form.cityId}
                />
              </div>
              <Input label="National ID" value={form.nationalId} onChange={(e) => set('nationalId', e.target.value)} />
              <div className="reg-grid2">
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
            <div className="reg-formBody" key="s2">
              <div className="reg-sectionTitle">
                <span>Guardian / Attender</span>
                <button type="button" className="reg-btn reg-btnGhost reg-btnSm" onClick={addContact}>
                  + Add contact
                </button>
              </div>
              {form.contacts.length === 0 && <div className="reg-empty">No guardian or attender added.</div>}
              <div className="reg-stack">
                {form.contacts.map((contact, index) => (
                  <div key={index} className="reg-rowCard">
                    <div className="reg-grid2">
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
                        inputMode="tel"
                        error={fieldErrors[`contact-${index}-phone`]}
                        value={contact.phone ?? ''}
                        onChange={(e) => updateContact(index, { phone: e.target.value })}
                      />
                    </div>
                    <div className="mt-3 flex items-center justify-between">
                      <label className="reg-check">
                        <input
                          type="checkbox"
                          checked={contact.isPrimary}
                          onChange={(e) => updateContact(index, { isPrimary: e.target.checked })}
                        />
                        Primary contact
                      </label>
                      <button type="button" className="reg-btn reg-btnDanger" onClick={() => removeContact(index)}>
                        Remove
                      </button>
                    </div>
                  </div>
                ))}
              </div>

              <div className="reg-divider" />

              <div className="reg-sectionTitle">
                <span>Identity documents</span>
                <button type="button" className="reg-btn reg-btnGhost reg-btnSm" onClick={addIdentifier}>
                  + Add ID
                </button>
              </div>
              <p className="reg-hint" style={{ marginTop: -4, marginBottom: 10 }}>
                Aadhaar/ABHA are capture only — verification against UIDAI/ABDM is a future integration.
              </p>
              {form.identifiers.length === 0 && <div className="reg-empty">No identity document added.</div>}
              <div className="reg-stack">
                {form.identifiers.map((identifier, index) => (
                  <div key={index} className="reg-rowCard flex flex-wrap items-end gap-3">
                    <Select
                      label="Type"
                      options={Object.entries(IDENTIFIER_TYPE_LABELS).map(([value, label]) => ({ value, label }))}
                      value={String(identifier.type)}
                      onChange={(e) => updateIdentifier(index, { type: Number(e.target.value) as IdentifierType })}
                      className="w-32"
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
                        className="w-44"
                      />
                    )}
                    <Input
                      label="Number"
                      required
                      error={fieldErrors[`identifier-${index}-value`]}
                      className="min-w-40 flex-1"
                      value={identifier.value}
                      onChange={(e) => updateIdentifier(index, { value: e.target.value })}
                    />
                    <button
                      type="button"
                      className="reg-btn reg-btnGhost reg-btnSm"
                      disabled
                      title="Verification integration not yet available"
                    >
                      Verify
                    </button>
                    <button type="button" className="reg-btn reg-btnDanger" onClick={() => removeIdentifier(index)}>
                      Remove
                    </button>
                  </div>
                ))}
              </div>

              <div className="reg-divider" />

              <div className="reg-switchRow">
                <SwitchCard
                  on={form.insuranceRequired}
                  onToggle={() => set('insuranceRequired', !form.insuranceRequired)}
                  title="♥ Insurance required"
                  sub="Capture the patient's insurer / TPA policy"
                />
              </div>
              {form.insuranceRequired && (
                <div className="mt-4">
                  {fieldErrors.insurancePolicies && (
                    <p role="alert" className="reg-hint" style={{ color: 'var(--r-crit)', fontWeight: 600 }}>
                      {fieldErrors.insurancePolicies}
                    </p>
                  )}
                  <div className="reg-sectionTitle mt-2">
                    <span>Policies</span>
                    <button type="button" className="reg-btn reg-btnGhost reg-btnSm" onClick={addInsurancePolicy}>
                      + Add policy
                    </button>
                  </div>
                  <div className="reg-stack">
                    {form.insurancePolicies.map((policy, index) => (
                      <div key={index} className="reg-rowCard flex flex-wrap items-end gap-3">
                        <Select
                          label="Insurer / TPA"
                          required
                          error={fieldErrors[`policy-${index}-insurer`]}
                          placeholder="—"
                          options={insuranceCompanies.map((c) => ({ value: c.id, label: c.name }))}
                          value={policy.insuranceCompanyId || ''}
                          onChange={(e) => updateInsurancePolicy(index, { insuranceCompanyId: Number(e.target.value) })}
                          className="w-52"
                        />
                        <Input
                          label="Policy number"
                          required
                          error={fieldErrors[`policy-${index}-number`]}
                          className="min-w-40 flex-1"
                          value={policy.policyNumber}
                          onChange={(e) => updateInsurancePolicy(index, { policyNumber: e.target.value })}
                        />
                        <Input
                          label="Valid to"
                          type="date"
                          value={policy.policyValidTo ?? ''}
                          onChange={(e) => updateInsurancePolicy(index, { policyValidTo: e.target.value || null })}
                        />
                        <button type="button" className="reg-btn reg-btnDanger" onClick={() => removeInsurancePolicy(index)}>
                          Remove
                        </button>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}

          {step === 3 && (
            <div className="reg-formBody" key="s3">
              <div className="reg-rowCard">
                <div className="reg-sectionTitle">
                  <span>Photo (optional)</span>
                </div>
                <PatientPhotoCapture patientId={null} personLabel="new-patient" onCaptured={setPhotoFile} compact />
              </div>
              <div className="reg-review mt-4">
                {[
                  ['Name', fullName || '—'],
                  ['Gender', GENDER_LABELS[form.gender]],
                  ['Age', form.dobKnown ? (form.dateOfBirth ? `${form.dateOfBirth} (${ageText})` : '—') : `~${form.estimatedAge || '—'} yrs (estimated)`],
                  ['Blood group', form.bloodGroup || '—'],
                  ['Mobile', form.mobile || '—'],
                  ['Email', form.email || '—'],
                  ['Address', [form.address, form.pincode].filter(Boolean).join(', ') || '—'],
                  ['Language', nameOf(languages, form.languageId)],
                  ['Patient category', nameOf(categories, form.patientCategoryId)],
                  ['Case type', nameOf(patientCaseTypes, form.patientCaseTypeId)],
                  ['VIP', form.isVip ? 'Yes' : 'No'],
                  ['SMS updates', form.smsOptIn ? 'On' : 'Off'],
                  ['Guardian / attender contacts', String(form.contacts.length)],
                  ['Identity documents', String(form.identifiers.filter((i) => i.value.trim()).length)],
                  ['Insurance policies', form.insuranceRequired ? String(form.insurancePolicies.length) : 'Not required'],
                ].map(([k, v]) => (
                  <div className="reg-kv" key={k}>
                    <span>{k}</span>
                    <b>{v}</b>
                  </div>
                ))}
              </div>
              <p className="reg-hint mt-3">A duplicate check runs before the patient is saved.</p>
            </div>
          )}

          <div className="reg-formFoot">
            <button type="button" className="reg-btn reg-btnGhost" onClick={goBack} disabled={step === 0 || submitting}>
              ← Back
            </button>
            <div className="reg-dots" aria-hidden>
              {steps.map((label, index) => (
                <i key={label} className={index === step ? 'on' : ''} />
              ))}
            </div>
            {step < steps.length - 1 ? (
              <button type="button" className="reg-btn reg-btnPrimary" onClick={goNext}>
                Next →
              </button>
            ) : (
              <button
                type="button"
                className="reg-btn reg-btnPrimary"
                onClick={checkAndRegister}
                disabled={submitting}
                aria-busy={submitting || undefined}
              >
                {submitting && <span className="reg-spin" aria-hidden />}✓ Check &amp; Register
              </button>
            )}
          </div>
        </section>

        <aside className="reg-rail">
          <div className="reg-card reg-preview">
            <div className="reg-previewTop">
              <small>LIVE PREVIEW</small>
              <div className="reg-prevName reg-display">{fullName || '— —'}</div>
              <div className="reg-prevSub">
                {ageText} • {GENDER_LABELS[form.gender]} • Blood {form.bloodGroup || '—'}
              </div>
              <div className="reg-prevTags">
                {form.patientCaseTypeId && <span className="reg-tag teal">{nameOf(patientCaseTypes, form.patientCaseTypeId)}</span>}
                {form.patientHcTagId && <span className="reg-tag">{nameOf(patientHcTags, form.patientHcTagId)}</span>}
                <span className="reg-tag">New</span>
                {form.isVip && <span className="reg-tag gold">★ VIP</span>}
              </div>
              <div className="reg-prevAvatar reg-display">
                {photoUrl ? <img src={photoUrl} alt="Patient photo" /> : initials || '••'}
              </div>
            </div>
            <div className="reg-previewBody">
              <div className="reg-kv">
                <span>UHID</span>
                <b>Assigned on save</b>
              </div>
              <div className="reg-kv">
                <span>Mobile</span>
                <b>{form.mobile || '—'}</b>
              </div>
              <div className="reg-kv">
                <span>Language</span>
                <b>{nameOf(languages, form.languageId)}</b>
              </div>
              <div className="reg-kv">
                <span>SMS alerts</span>
                <b>{form.smsOptIn ? 'On' : 'Off'}</b>
              </div>
            </div>
          </div>

          <div className="reg-card reg-meterCard">
            <h4>Form completeness</h4>
            <p>
              {completeness}% —{' '}
              {requiredDone < 3 ? 'add name, age and mobile to register' : 'all required details captured'}
            </p>
            <div className="reg-meter">
              <i style={{ width: `${completeness}%` }} />
            </div>
            <ul className="reg-checkList">
              {checks.map((c) => (
                <li key={c.label} className={c.ok ? 'ok' : ''}>
                  <span className="reg-tick">✓</span> {c.label}
                </li>
              ))}
            </ul>
          </div>

          <div className="reg-card reg-helpCard">
            <h4>Before you register</h4>
            <p>Search first to avoid a duplicate UHID. Insurance and MLC cases need extra documents — keep Aadhaar and the referral letter ready.</p>
            <div className="reg-helpBtns">
              <button type="button" className="solid" onClick={() => navigate('/patients')}>
                Search patients
              </button>
            </div>
          </div>
        </aside>
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
    </div>
  )
}
