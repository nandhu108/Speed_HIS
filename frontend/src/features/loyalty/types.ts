/** Mirrors backend Prixo.Domain.Enums.LoyaltyCardStatus. */
export const LOYALTY_CARD_STATUSES = { Active: 1, Deactivated: 2 } as const
export type LoyaltyCardStatus = (typeof LOYALTY_CARD_STATUSES)[keyof typeof LOYALTY_CARD_STATUSES]
export const LOYALTY_CARD_STATUS_LABELS: Record<LoyaltyCardStatus, string> = { 1: 'Active', 2: 'Deactivated' }

export interface RegisterLoyaltyCardPayload {
  patientId: number
  loyaltyCardTypeId: number
  durationMonths: number
}

export interface RenewLoyaltyCardPayload {
  loyaltyCardTypeId: number | null
  durationMonths: number
}

export interface AddDependentPayload {
  patientId: number
  memberRelationTagId: number
}

export interface LoyaltyCardListItem {
  id: number
  cardNumber: string
  patientId: number
  patientUhid: string
  patientName: string
  loyaltyCardTypeId: number
  loyaltyCardTypeName: string
  issuedDate: string
  expiryDate: string
  amount: number
  status: LoyaltyCardStatus
}

export interface LoyaltyCardMember {
  id: number
  patientId: number
  patientUhid: string
  patientName: string
  memberRelationTagId: number
  memberRelationTagName: string
  addedAt: string
}

export interface LoyaltyCardDetail {
  id: number
  cardNumber: string
  patientId: number
  patientUhid: string
  patientName: string
  patientMobile: string
  loyaltyCardTypeId: number
  loyaltyCardTypeName: string
  memberLimit: number
  categoryName: string | null
  issuedDate: string
  durationMonths: number
  expiryDate: string
  amount: number
  status: LoyaltyCardStatus
  deactivateReason: string | null
  deactivatedAt: string | null
  duplicateIssueCount: number
  members: LoyaltyCardMember[]
}
