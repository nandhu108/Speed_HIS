/** Mirrors backend Prixo.Domain.Enums.Gender. */
export const GENDERS = { Male: 1, Female: 2, Other: 3 } as const
export type Gender = (typeof GENDERS)[keyof typeof GENDERS]
export const GENDER_LABELS: Record<Gender, string> = { 1: 'Male', 2: 'Female', 3: 'Other' }

/** Mirrors backend Prixo.Domain.Enums.PatientContactType. */
export const CONTACT_TYPES = { Guardian: 1, Attender: 2, EmergencyContact: 3, Other: 4 } as const
export type ContactType = (typeof CONTACT_TYPES)[keyof typeof CONTACT_TYPES]
export const CONTACT_TYPE_LABELS: Record<ContactType, string> = {
  1: 'Guardian',
  2: 'Attender',
  3: 'Emergency contact',
  4: 'Other',
}

/** Mirrors backend Prixo.Domain.Enums.PatientIdentifierType — Aadhaar/ABHA are placeholders. */
export const IDENTIFIER_TYPES = { Aadhaar: 1, Abha: 2, Other: 3 } as const
export type IdentifierType = (typeof IDENTIFIER_TYPES)[keyof typeof IDENTIFIER_TYPES]
export const IDENTIFIER_TYPE_LABELS: Record<IdentifierType, string> = { 1: 'Aadhaar', 2: 'ABHA', 3: 'Other' }

/** Mirrors backend Prixo.Domain.Enums.PatientDocumentType. */
export const DOCUMENT_TYPES = { Photo: 1, IdProof: 2, ConsentScan: 3, Other: 4 } as const
export type DocumentType = (typeof DOCUMENT_TYPES)[keyof typeof DOCUMENT_TYPES]
export const DOCUMENT_TYPE_LABELS: Record<DocumentType, string> = {
  1: 'Photo',
  2: 'ID proof',
  3: 'Consent scan',
  4: 'Other',
}

export interface PatientContact {
  id: number
  contactType: ContactType
  name: string
  relationship: string | null
  phone: string | null
  email: string | null
  address: string | null
  isPrimary: boolean
}

export interface SavePatientContact {
  contactType: ContactType
  name: string
  relationship: string | null
  phone: string | null
  email: string | null
  address: string | null
  isPrimary: boolean
}

export interface PatientIdentifier {
  id: number
  type: IdentifierType
  identityDocumentTypeId: number | null
  identityDocumentTypeName: string | null
  value: string
  isVerified: boolean
}

export interface SavePatientIdentifier {
  type: IdentifierType
  identityDocumentTypeId: number | null
  value: string
}

export interface PatientInsurancePolicy {
  id: number
  insuranceCompanyId: number
  insuranceCompanyName: string
  policyNumber: string
  policyValidFrom: string | null
  policyValidTo: string | null
  isPrimary: boolean
}

export interface SavePatientInsurancePolicy {
  insuranceCompanyId: number
  policyNumber: string
  policyValidFrom: string | null
  policyValidTo: string | null
  isPrimary: boolean
}

export interface PatientListItem {
  id: number
  uhid: string
  fullName: string
  gender: Gender
  ageDisplay: string
  mobile: string
  patientCategoryName: string | null
  patientCategoryColorHex: string | null
  registeredBranchName: string
  isActive: boolean
  isBlocked: boolean
  createdAt: string
}

export interface PatientDetail {
  id: number
  uhid: string
  salutationId: number | null
  salutationName: string | null
  firstName: string
  middleName: string | null
  lastName: string
  gender: Gender
  dateOfBirth: string | null
  estimatedAge: number | null
  ageDisplay: string
  bloodGroup: string | null
  maritalStatusId: number | null
  maritalStatusName: string | null
  mobile: string
  alternateMobile: string | null
  whatsAppNumber: string | null
  email: string | null
  address: string | null
  permanentAddress: string | null
  isPermanentSameAsPresent: boolean
  stateId: number | null
  stateName: string | null
  cityId: number | null
  cityName: string | null
  areaId: number | null
  areaName: string | null
  pincode: string | null
  countryId: number | null
  countryName: string | null
  nationalityId: number | null
  nationalityName: string | null
  occupation: string | null
  monthlyIncome: number | null
  languageId: number | null
  languageName: string | null
  motherName: string | null
  nationalId: string | null
  patientCaseTypeId: number | null
  patientCaseTypeName: string | null
  patientHcTagId: number | null
  patientHcTagName: string | null
  speciallyAbledCategoryId: number | null
  speciallyAbledCategoryName: string | null
  isVip: boolean
  smsOptIn: boolean
  insuranceRequired: boolean
  remarks: string | null
  patientCategoryId: number | null
  patientCategoryName: string | null
  patientCategoryColorHex: string | null
  referrerId: number | null
  referrerName: string | null
  registeredBranchId: number
  registeredBranchName: string
  photoPath: string | null
  isActive: boolean
  isBlocked: boolean
  blockedReason: string | null
  blockedAt: string | null
  blockedByUserName: string | null
  contacts: PatientContact[]
  identifiers: PatientIdentifier[]
  insurancePolicies: PatientInsurancePolicy[]
}

export interface RegisterPatientPayload {
  salutationId: number | null
  firstName: string
  middleName: string | null
  lastName: string
  gender: Gender
  dateOfBirth: string | null
  estimatedAge: number | null
  bloodGroup: string | null
  maritalStatusId: number | null
  mobile: string
  alternateMobile: string | null
  whatsAppNumber: string | null
  email: string | null
  address: string | null
  permanentAddress: string | null
  isPermanentSameAsPresent: boolean
  stateId: number | null
  cityId: number | null
  areaId: number | null
  pincode: string | null
  countryId: number | null
  nationalityId: number | null
  occupation: string | null
  monthlyIncome: number | null
  languageId: number | null
  motherName: string | null
  nationalId: string | null
  patientCaseTypeId: number | null
  patientHcTagId: number | null
  speciallyAbledCategoryId: number | null
  isVip: boolean
  smsOptIn: boolean
  insuranceRequired: boolean
  remarks: string | null
  patientCategoryId: number | null
  referrerId: number | null
  contacts: SavePatientContact[]
  identifiers: SavePatientIdentifier[]
  insurancePolicies: SavePatientInsurancePolicy[]
}

export interface UpdatePatientPayload extends RegisterPatientPayload {
  isActive: boolean
}

export interface RegisterPatientResult {
  id: number
  uhid: string
}

export interface CheckDuplicatesPayload {
  firstName: string
  lastName: string
  dateOfBirth: string | null
  mobile: string | null
  excludePatientId: number | null
}

export interface DuplicateCandidate {
  id: number
  uhid: string
  fullName: string
  gender: Gender
  dateOfBirth: string | null
  mobile: string
  matchScore: number
  matchReasons: string[]
}

export interface ConsentEntry {
  id: number
  consentTemplateId: number
  templateTitle: string
  signedByName: string
  relationshipToPatient: string | null
  signedAt: string
  documentPath: string | null
}

export interface PatientDocumentEntry {
  id: number
  documentType: DocumentType
  fileName: string
  contentType: string
  sizeBytes: number
  description: string | null
  uploadedAt: string
}

// ── Block/Unblock Report ─────────────────────────────────────────────────
export interface PatientReportColumn {
  key: string
  label: string
}
export interface PatientReportResult {
  reportName: string
  columns: PatientReportColumn[]
  rows: Record<string, unknown>[]
}

// ── ABDM / ABHA (Wave-I integration, M1) ──────────────────────────────────
export const ABDM_METHODS = { AadhaarOtp: 1, MobileOtp: 2, AbhaOtp: 3, ScanAndShare: 4 } as const
export type AbdmMethod = (typeof ABDM_METHODS)[keyof typeof ABDM_METHODS]
export const ABDM_METHOD_LABELS: Record<AbdmMethod, string> = {
  1: 'Aadhaar OTP (Creation / KYC)',
  2: 'Mobile OTP (Verification)',
  3: 'ABHA number / address OTP',
  4: 'Scan & Share (ABDM QR)',
}
export const ABHA_STATUS_LABELS: Record<number, string> = {
  1: 'Pending',
  2: 'Linked',
  3: 'Created',
  4: 'Failed',
}

export interface AbhaProfile {
  patientId: number
  abhaNumber: string
  abhaAddress: string | null
  status: number
  verificationMethod: AbdmMethod
  kycVerified: boolean
  linkedName: string | null
  linkedGender: string | null
  linkedYearOfBirth: number | null
  verifiedAt: string | null
}
export interface AbhaVerificationSession {
  sessionId: number
  maskedTarget: string | null
  expiresAt: string
}
export interface InitiateAbhaPayload {
  method: AbdmMethod
  identifier: string
}
export interface CompleteAbhaPayload {
  sessionId: number
  otp: string
}
