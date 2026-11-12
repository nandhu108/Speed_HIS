// ── ABDM (Ayushman Bharat Digital Mission) Types ────────────────────────────

export const ABDM_VERIFICATION_METHODS = {
  AadhaarOtp: 1,
  MobileOtp: 2,
  AbhaOtp: 3,
  ScanAndShare: 4,
} as const

export type AbdmVerificationMethod = (typeof ABDM_VERIFICATION_METHODS)[keyof typeof ABDM_VERIFICATION_METHODS]

export const ABDM_METHOD_LABELS: Record<AbdmVerificationMethod, string> = {
  1: 'Aadhaar OTP (Creation / KYC)',
  2: 'Mobile OTP (Verification)',
  3: 'ABHA number / address OTP',
  4: 'Scan & Share (ABDM QR)',
}

export const ABHA_LINK_STATUSES = {
  Pending: 1,
  Linked: 2,
  Created: 3,
  Failed: 4,
} as const

export type AbhaLinkStatus = (typeof ABHA_LINK_STATUSES)[keyof typeof ABHA_LINK_STATUSES]

export const ABHA_STATUS_LABELS: Record<AbhaLinkStatus, string> = {
  1: 'Pending',
  2: 'Linked',
  3: 'Created',
  4: 'Failed',
}

export interface AbhaProfile {
  patientId: number
  abhaNumber: string
  abhaAddress: string | null
  status: AbhaLinkStatus
  verificationMethod: AbdmVerificationMethod
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

// ── Milestone M2: Health Information Provider (HIP) ────────────────────────

export const ABDM_CARE_CONTEXT_TYPES = {
  OpdVisit: 1,
  IpAdmission: 2,
  DiagnosticReport: 3,
  Prescription: 4,
  DischargeSummary: 5,
} as const

export type AbdmCareContextType = (typeof ABDM_CARE_CONTEXT_TYPES)[keyof typeof ABDM_CARE_CONTEXT_TYPES]

export const ABDM_CARE_CONTEXT_LABELS: Record<AbdmCareContextType, string> = {
  1: 'OPD Consultation',
  2: 'IPD Admission',
  3: 'Diagnostic Report',
  4: 'Prescription',
  5: 'Discharge Summary',
}

export interface CareContext {
  id: number
  patientId: number
  patientReference: string
  abhaNumber: string
  contextType: AbdmCareContextType
  referenceNumber: string
  display: string
  isLinked: boolean
  linkedAt: string | null
  createdAt: string
}

export interface DataTransferLog {
  id: number
  transactionId: string
  consentArtifactId: string
  careContextReference: string
  hiType: number
  recipientHiuId: string
  status: number
  transferDate: string
  fhirBundleJson: string | null
  errorMessage: string | null
}

// ── Milestone M3: Health Information User (HIU) ────────────────────────────

export const ABDM_HI_TYPES = {
  Prescription: 1,
  DiagnosticReport: 2,
  OpConsultation: 3,
  DischargeSummary: 4,
  ImmunizationRecord: 5,
  HealthDocumentRecord: 6,
} as const

export type AbdmHiType = (typeof ABDM_HI_TYPES)[keyof typeof ABDM_HI_TYPES]

export const ABDM_HI_TYPE_LABELS: Record<AbdmHiType, string> = {
  1: 'Prescription',
  2: 'Diagnostic Report',
  3: 'OP Consultation',
  4: 'Discharge Summary',
  5: 'Immunization Record',
  6: 'Health Document',
}

export const ABDM_CONSENT_STATUSES = {
  Requested: 1,
  Granted: 2,
  Denied: 3,
  Revoked: 4,
  Expired: 5,
} as const

export type AbdmConsentStatus = (typeof ABDM_CONSENT_STATUSES)[keyof typeof ABDM_CONSENT_STATUSES]

export const ABDM_CONSENT_STATUS_LABELS: Record<AbdmConsentStatus, string> = {
  1: 'Requested',
  2: 'Granted',
  3: 'Denied',
  4: 'Revoked',
  5: 'Expired',
}

export interface ConsentRequest {
  id: number
  patientId: number
  consentRequestId: string
  consentArtifactId: string | null
  abhaNumber: string
  abhaAddress: string | null
  purpose: string
  hiTypes: string
  dateFrom: string
  dateTo: string
  expiryAt: string
  status: AbdmConsentStatus
  statusUpdatedAt: string | null
  requesterDoctorName: string | null
  createdAt: string
}

export interface ExternalHealthRecord {
  id: number
  patientId: number
  consentRequestId: number | null
  hiType: AbdmHiType
  sourceFacilityName: string
  sourceFacilityHipId: string
  recordDate: string
  doctorName: string | null
  title: string
  clinicalSummary: string
  fhirBundleJson: string
  createdAt: string
}

// ── Admin Governance ───────────────────────────────────────────────────────

export interface AbdmAdminStatus {
  masterEnabled: boolean
  m1Enabled: boolean
  m2Enabled: boolean
  m3Enabled: boolean
  gatewayUrl: string
  clientId: string
  hipId: string
  hiuId: string
  isConfigured: boolean
  totalAbhaLinked: number
  totalCareContextsLinked: number
  totalDataTransfers: number
  totalConsentRequests: number
  totalExternalRecords: number
  checkedAt: string
}

export interface UpdateAbdmAdminConfigRequest {
  masterEnabled: boolean
  m1Enabled: boolean
  m2Enabled: boolean
  m3Enabled: boolean
  gatewayUrl?: string
  clientId?: string
  clientSecret?: string
  hipId?: string
  hiuId?: string
}
