/** Mirrors backend Prixo.Domain.Enums.PreAuthTreatmentType. */
export const TREATMENT_TYPES = { Op: 1, Ip: 2 } as const
export type TreatmentType = (typeof TREATMENT_TYPES)[keyof typeof TREATMENT_TYPES]
export const TREATMENT_TYPE_LABELS: Record<TreatmentType, string> = { 1: 'OP', 2: 'IP' }

/** Mirrors backend Prixo.Domain.Enums.PreAuthStatus. */
export const PRE_AUTH_STATUSES = { Requested: 1, Approved: 2, PartiallyApproved: 3, Rejected: 4 } as const
export type PreAuthStatus = (typeof PRE_AUTH_STATUSES)[keyof typeof PRE_AUTH_STATUSES]
export const PRE_AUTH_STATUS_LABELS: Record<PreAuthStatus, string> = {
  1: 'Requested',
  2: 'Approved',
  3: 'Partially Approved',
  4: 'Rejected',
}

/** Mirrors backend Prixo.Domain.Enums.PreAuthEnhancementStatus. */
export const ENHANCEMENT_STATUSES = { Requested: 1, Approved: 2, Rejected: 3 } as const
export type EnhancementStatus = (typeof ENHANCEMENT_STATUSES)[keyof typeof ENHANCEMENT_STATUSES]
export const ENHANCEMENT_STATUS_LABELS: Record<EnhancementStatus, string> = {
  1: 'Requested',
  2: 'Approved',
  3: 'Rejected',
}

/** Mirrors backend Prixo.Domain.Enums.ClaimStatus. */
export const CLAIM_STATUSES = {
  Draft: 1,
  Submitted: 2,
  UnderReview: 3,
  PartiallyApproved: 4,
  Approved: 5,
  Denied: 6,
  Settled: 7,
} as const
export type ClaimStatus = (typeof CLAIM_STATUSES)[keyof typeof CLAIM_STATUSES]
export const CLAIM_STATUS_LABELS: Record<ClaimStatus, string> = {
  1: 'Draft',
  2: 'Submitted',
  3: 'Under Review',
  4: 'Partially Approved',
  5: 'Approved',
  6: 'Denied',
  7: 'Settled',
}

export interface PreAuthRequest {
  id: number
  preAuthNumber: string
  patientId: number
  patientUhid: string
  patientName: string
  policyId: number
  policyNumber: string
  insuranceCompanyName: string
  treatmentType: TreatmentType
  opVisitId: number | null
  admissionId: number | null
  estimatedAmount: number
  diagnosis: string
  requestedByUserName: string
  requestedAt: string
  status: PreAuthStatus
  approvedAmount: number | null
  decidedByUserName: string | null
  decidedAt: string | null
  rejectionReason: string | null
}

export interface RaisePreAuthPayload {
  patientId: number
  policyId: number
  treatmentType: TreatmentType
  opVisitId: number | null
  admissionId: number | null
  estimatedAmount: number
  diagnosis: string
}

export interface DecidePreAuthPayload {
  approve: boolean
  approvedAmount: number | null
  rejectionReason: string | null
}

export interface PreAuthEnhancement {
  id: number
  preAuthRequestId: number
  requestedAmount: number
  reason: string
  requestedByUserName: string
  requestedAt: string
  status: EnhancementStatus
  decidedByUserName: string | null
  decidedAt: string | null
  rejectionReason: string | null
}

export interface RequestEnhancementPayload {
  requestedAmount: number
  reason: string
}

export interface DecideEnhancementPayload {
  approve: boolean
  rejectionReason: string | null
}

export interface Claim {
  id: number
  claimNumber: string
  patientId: number
  patientUhid: string
  patientName: string
  policyId: number
  policyNumber: string
  insuranceCompanyName: string
  preAuthRequestId: number | null
  billId: number
  billNumber: string
  claimAmount: number
  approvedAmount: number | null
  status: ClaimStatus
  submittedByUserName: string | null
  submittedAt: string | null
  decidedByUserName: string | null
  decidedAt: string | null
  denialReasonId: number | null
  denialReasonName: string | null
  denialNotes: string | null
  settledAmount: number | null
  deductionAmount: number | null
  settlementReferenceNumber: string | null
  settledAt: string | null
  /** The bill's *current* totals — surfaced so staff can see whether the counter has already collected cash against it. */
  billPaidAmount: number
  billBalanceAmount: number
}

export interface SubmitClaimPayload {
  patientId: number
  policyId: number
  preAuthRequestId: number | null
  billId: number
  claimAmount: number
}

export interface UpdateClaimStatusPayload {
  status: ClaimStatus
  approvedAmount: number | null
  denialReasonId: number | null
  denialNotes: string | null
}

export interface RecordClaimSettlementPayload {
  settledAmount: number
  deductionAmount: number | null
  paymentModeId: number
  settlementReferenceNumber: string | null
}

export interface ClaimDocument {
  id: number
  documentTypeId: number
  documentTypeName: string
  fileName: string
  contentType: string
  sizeBytes: number
  uploadedAt: string
}
