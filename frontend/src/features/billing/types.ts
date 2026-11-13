export type BillType = 1 | 2 | 3 // Op, Ip, Emergency
export type BillStatus = 1 | 2 | 3 | 4 | 5 // Open, PartiallyPaid, Paid, Cancelled, Parked
export type BillItemSourceType = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 // Consultation, ClinicalOrder, Service, Package, BedCharge, OtCharge, Pharmacy, LabTest, RadiologyTest
export type RefundMethod = 1 | 2 // Cash, CreditNote
export type RefundStatus = 1 | 2 | 3 | 4 // Pending, Approved, Rejected, Issued
export type DiscountApprovalStatus = 1 | 2 | 3 | 4 // AutoApproved, Pending, Approved, Rejected
export type DayCloseStatus = 1 | 2 // Open, Closed
export type CreditNoteStatus = 1 | 2 | 3 // Pending, Approved, Rejected
export type OnlinePaymentRequestStatus = 0 | 1 | 2 // Requested, Completed, Cancelled
/** Mirrors backend Prixo.Domain.Enums.PreAuthStatus — duplicated rather than imported from ../insurance/types, per this codebase's "feature folders stay self-contained" convention. */
export type BillPreAuthStatus = 1 | 2 | 3 | 4 // Requested, Approved, PartiallyApproved, Rejected
/** Mirrors backend Prixo.Domain.Enums.ClaimStatus — same reasoning as BillPreAuthStatus. */
export type BillClaimStatus = 1 | 2 | 3 | 4 | 5 | 6 | 7 // Draft, Submitted, UnderReview, PartiallyApproved, Approved, Denied, Settled

export const BILL_TYPE_LABELS: Record<BillType, string> = { 1: 'OP', 2: 'IP', 3: 'Emergency' }
export const BILL_STATUS_LABELS: Record<BillStatus, string> = {
  1: 'Open',
  2: 'Partially Paid',
  3: 'Paid',
  4: 'Cancelled',
  5: 'Parked',
}
export const CREDIT_NOTE_STATUS_LABELS: Record<CreditNoteStatus, string> = {
  1: 'Pending',
  2: 'Approved',
  3: 'Rejected',
}
export const BILL_ITEM_SOURCE_LABELS: Record<BillItemSourceType, string> = {
  1: 'Consultation',
  2: 'Clinical Order',
  3: 'Service',
  4: 'Package',
  5: 'Bed Charge',
  6: 'OT Charge',
  7: 'Pharmacy',
  8: 'Lab Test',
  9: 'Radiology',
}
export const ONLINE_PAYMENT_REQUEST_STATUS_LABELS: Record<OnlinePaymentRequestStatus, string> = {
  0: 'Requested',
  1: 'Completed',
  2: 'Cancelled',
}
export const BILL_PRE_AUTH_STATUS_LABELS: Record<BillPreAuthStatus, string> = {
  1: 'Requested',
  2: 'Approved',
  3: 'Partially Approved',
  4: 'Rejected',
}
export const BILL_CLAIM_STATUS_LABELS: Record<BillClaimStatus, string> = {
  1: 'Draft',
  2: 'Submitted',
  3: 'Under Review',
  4: 'Partially Approved',
  5: 'Approved',
  6: 'Denied',
  7: 'Settled',
}
export const REFUND_STATUS_LABELS: Record<RefundStatus, string> = {
  1: 'Pending',
  2: 'Approved',
  3: 'Rejected',
  4: 'Issued',
}
export const DISCOUNT_APPROVAL_STATUS_LABELS: Record<DiscountApprovalStatus, string> = {
  1: 'Auto-approved',
  2: 'Pending',
  3: 'Approved',
  4: 'Rejected',
}
export const DAY_CLOSE_STATUS_LABELS: Record<DayCloseStatus, string> = { 1: 'Open', 2: 'Closed' }

export interface BillItem {
  id: number
  sourceType: BillItemSourceType
  serviceId: number | null
  serviceName: string | null
  packageId: number | null
  packageName: string | null
  clinicalOrderId: number | null
  description: string
  quantity: number
  rate: number
  discountPercent: number
  discountAmount: number
  taxPercent: number
  taxAmount: number
  totalAmount: number
}

export interface BillPayment {
  id: number
  paymentModeId: number
  paymentModeName: string
  amount: number
  referenceNumber: string | null
  receiptNumber: string
  paidAt: string
}

export interface BillListItem {
  id: number
  billNumber: string
  billDate: string
  patientId: number
  patientUhid: string
  patientName: string
  billType: BillType
  status: BillStatus
  totalAmount: number
  paidAmount: number
  balanceAmount: number
  isCreditBill: boolean
  admissionId: number | null
}

/** Mirrors backend Prixo.Domain.Enums.Gender — kept local to avoid a cross-feature type import. */
export type BillingGender = 1 | 2 | 3
export const BILLING_GENDER_LABELS: Record<BillingGender, string> = { 1: 'Male', 2: 'Female', 3: 'Other' }

/** One row of OP Billing's own patient search (legacy "OP Bill Payment" search) — a visit, not a
 * patient master record. billId/billNumber/status are null when no bill has been generated yet. */
export interface OpBillingSearchResult {
  opVisitId: number
  patientId: number
  regNo: string
  patientName: string
  encounterDate: string
  age: string
  gender: BillingGender
  mobile: string
  doctorName: string | null
  specialtyName: string | null
  patientType: string | null
  sourceName: string | null
  billId: number | null
  billNumber: string | null
  status: BillStatus | null
  /** Only set once a bill exists for this visit (Bill.CorporateId) — null for an as-yet-unbilled encounter. */
  corporateName: string | null
}

/**
 * Set only when this bill's encounter has a not-yet-resolved insurance pre-auth, or this
 * exact bill has a not-yet-resolved claim (a claim outranks a pre-auth when both exist).
 * Advisory only — nothing in Billing blocks cash collection because of this.
 */
export interface BillInsuranceStatus {
  preAuthRequestId: number | null
  preAuthNumber: string | null
  preAuthStatus: BillPreAuthStatus | null
  preAuthApprovedAmount: number | null
  claimId: number | null
  claimNumber: string | null
  claimStatus: BillClaimStatus | null
}

export interface BillDetail {
  id: number
  billNumber: string
  billDate: string
  patientId: number
  patientUhid: string
  patientName: string
  patientMobile: string
  opVisitId: number | null
  admissionId: number | null
  emergencyVisitId: number | null
  billType: BillType
  status: BillStatus
  subTotal: number
  discountAmount: number
  taxAmount: number
  totalAmount: number
  paidAmount: number
  balanceAmount: number
  isCreditBill: boolean
  corporateId: number | null
  corporateName: string | null
  cancelReason: string | null
  cancelledAt: string | null
  creditLimit: number | null
  creditExposure: number | null
  insuranceStatus: BillInsuranceStatus | null
  items: BillItem[]
  payments: BillPayment[]
}

export interface BillNote {
  id: number
  note: string
  createdBy: number
  createdByName: string | null
  createdAt: string
}

export interface AddBillNotePayload {
  note: string
}

export interface ParkBillPayload {
  reason: string
}

export interface DrawAdvanceOnBillPayload {
  advanceId: number
  amount: number
}

export interface CreditNoteItem {
  id: number
  billItemId: number
  billItemDescription: string
  amount: number
}

export interface CreditNoteItemPayload {
  billItemId: number
  amount: number
}

export interface RequestCreditNotePayload {
  reason: string
  items: CreditNoteItemPayload[]
}

export interface CreditNote {
  id: number
  billId: number
  billNumber: string
  creditNoteNumber: string
  creditNoteDate: string
  reason: string
  amount: number
  status: CreditNoteStatus
  decidedByUserId: number | null
  decidedByName: string | null
  decidedAt: string | null
  rejectionReason: string | null
  createdAt: string
  items: CreditNoteItem[]
}

export interface DecideCreditNotePayload {
  approve: boolean
  rejectionReason: string | null
}

// ── Cashier shifts ───────────────────────────────────────────────────────
export type CashierShiftStatus = 1 | 2 | 3 // Open, Closed, Received
export const CASHIER_SHIFT_STATUS_LABELS: Record<CashierShiftStatus, string> = {
  1: 'Open',
  2: 'Closed',
  3: 'Received',
}

export interface CashierShiftModeAmount {
  paymentModeId: number
  paymentModeName: string
  systemAmount: number
  declaredAmount: number
  variance: number
}

export interface CashierShift {
  id: number
  userId: number
  userName: string
  openedAt: string
  openingFloat: number
  status: CashierShiftStatus
  closedAt: string | null
  notes: string | null
  receivedByUserId: number | null
  receivedByName: string | null
  receivedAt: string | null
  modeAmounts: CashierShiftModeAmount[]
}

export interface OpenCashierShiftPayload {
  openingFloat: number
}

export interface CloseCashierShiftPayload {
  declaredAmounts: DeclaredModeAmount[]
  notes: string | null
}

// ── Bill estimates ───────────────────────────────────────────────────────
// Mirrors backend BillItemSourceType — only Service/Package are valid on an estimate line.
export type EstimateLineSourceType = 3 | 4

export type BillEstimateStatus = 1 | 2 // Draft, Printed
export const BILL_ESTIMATE_STATUS_LABELS: Record<BillEstimateStatus, string> = {
  1: 'Draft',
  2: 'Printed',
}

export interface BillEstimateListItem {
  id: number
  estimateNumber: string
  estimateDate: string
  patientId: number
  patientUhid: string
  patientName: string
  status: BillEstimateStatus
  totalAmount: number
}

export interface BillEstimateLine {
  id: number
  sourceType: EstimateLineSourceType
  serviceId: number | null
  serviceName: string | null
  packageId: number | null
  packageName: string | null
  description: string
  quantity: number
  rate: number
  totalAmount: number
}

export interface BillEstimateDetail {
  id: number
  estimateNumber: string
  estimateDate: string
  patientId: number
  patientUhid: string
  patientName: string
  billEstimateTemplateId: number | null
  billEstimateTemplateName: string | null
  status: BillEstimateStatus
  totalAmount: number
  notes: string | null
  items: BillEstimateLine[]
}

export interface CreateBillEstimatePayload {
  patientId: number
  billEstimateTemplateId: number | null
  items: { sourceType: EstimateLineSourceType; serviceId: number | null; packageId: number | null; quantity: number }[]
  notes: string | null
}

// ── Billing reports ──────────────────────────────────────────────────────
export interface BillingReportRange {
  fromDate?: string
  toDate?: string
}

/**
 * Extra narrowing filters shared by the report keys that need more than a date range — on top
 * of the caller's own allowed bill types. billedByUserId is Day Book's; doctorId/serviceId/
 * serviceCategoryId/isCredit/reportType are Revenue's; ipNumber/wardId/patientCategoryId are
 * Ward-wise Outstanding's; departmentId/corporateId/isCredit are shared by both — each report
 * key ignores whichever of these it doesn't use.
 */
export interface BillingReportExtraFilters {
  billType?: BillType
  billedByUserId?: number
  corporateId?: number
  doctorId?: number
  serviceId?: number
  serviceCategoryId?: number
  departmentId?: number
  isCredit?: boolean
  /** Revenue Report only — "summary" (grouped by Department) or "detail" (one row per bill item); defaults to summary. */
  reportType?: 'summary' | 'detail'
  /** Ward-wise Outstanding only — partial match on the admission's IP number (AdmissionNumber). */
  ipNumber?: string
  wardId?: number
  patientCategoryId?: number
}

// Ageing/Ward-wise Outstanding/Day Book/Bill Cancelled/Refund/Advance/Tariff List/Doctors
// Payout/User Settlement/Service-wise Breakup/IP Service Billing Detail all used to have
// their own typed Row/Report interface here. Replaced by the generic ReportResult
// ({ reportName, columns, rows: Record<string, unknown>[] }) from
// components/ui/ReportResultTable — one shape for every plain-list report tab, matching
// BillingReportsController's single GetResult dispatcher.

/** One row of the "OP Bill Concession Search" screen — an approved concession with its bill/patient/doctor context. */
export interface ConcessionSearchResult {
  billId: number
  billNumber: string
  billDate: string
  regNo: string
  patientName: string
  age: string
  gender: BillingGender
  doctorName: string | null
  specialtyName: string | null
  billAmount: number
  requestedPercent: number
  requestedAmount: number
  reason: string | null
  requestedByName: string
  requestedAt: string
  approvedByName: string | null
  approvedAt: string | null
}

export interface GenerateOpBillPayload {
  opVisitId: number
}

export interface AddManualBillItemPayload {
  sourceType: BillItemSourceType
  serviceId: number | null
  packageId: number | null
  quantity: number
}

export interface UpdateBillItemPayload {
  description: string
  quantity: number
  rate: number
}

export interface ApplyDiscountPayload {
  discountTypeId: number | null
  percent: number
  reason: string | null
}

export interface CollectPaymentPayload {
  paymentModeId: number
  amount: number
  referenceNumber: string | null
}

export interface CancelBillPayload {
  reason: string
}

export interface MakeCreditBillPayload {
  corporateId: number | null
}

export interface RequestRefundPayload {
  billPaymentId: number | null
  amount: number
  reason: string
  method: RefundMethod
}

/** No billPaymentId concept — an advance has no payment rows to pick from, just one BalanceAmount. */
export interface RequestAdvanceRefundPayload {
  amount: number
  reason: string
  method: RefundMethod
}

export interface TransferAdvancePayload {
  targetPatientId: number
  targetAdmissionId: number | null
  amount: number
  notes: string | null
}

export interface DecideRefundPayload {
  approve: boolean
  rejectionReason: string | null
}

export interface DecideDiscountPayload {
  approve: boolean
  rejectionReason: string | null
}

export interface DiscountApproval {
  id: number
  billId: number
  billNumber: string
  billItemId: number | null
  billItemDescription: string | null
  requestedPercent: number
  requestedAmount: number
  reason: string | null
  status: DiscountApprovalStatus
  decidedByUserId: number | null
  decidedByName: string | null
  decidedAt: string | null
  rejectionReason: string | null
  createdAt: string
}

/** Exactly one of (billId, billNumber) / advanceId is populated — a bill refund or an advance refund. issuedByUserId/issuedAt are set once the money has actually moved (RefundStatus 4 "Issued"), a separate step from the approvedByUserId/approvedAt decision. */
export interface Refund {
  id: number
  billId: number | null
  billNumber: string | null
  billPaymentId: number | null
  advanceId: number | null
  amount: number
  reason: string
  method: RefundMethod
  status: RefundStatus
  approvedByUserId: number | null
  approvedByName: string | null
  approvedAt: string | null
  issuedByUserId: number | null
  issuedByName: string | null
  issuedAt: string | null
  rejectionReason: string | null
  createdAt: string
}

/** The Patient Portal's "Pay Online" outbox — staff follow-up worklist, no money moves through this row itself. */
export interface OnlinePaymentRequest {
  id: number
  billId: number
  billNumber: string
  patientId: number
  patientName: string
  amount: number
  status: OnlinePaymentRequestStatus
  requestedAt: string
  processedAt: string | null
}

export interface PaymentMode {
  id: number
  code: string
  name: string
  description: string | null
  requiresReference: boolean
  isActive: boolean
}

export interface SavePaymentModePayload {
  id?: number
  code: string
  name: string
  description: string | null
  requiresReference: boolean
  isActive: boolean
}

export interface Advance {
  id: number
  patientId: number
  patientUhid: string
  patientName: string
  amount: number
  balanceAmount: number
  paymentModeId: number
  paymentModeName: string
  referenceNumber: string | null
  notes: string | null
  receivedAt: string
}

export interface CollectAdvancePayload {
  patientId: number
  amount: number
  paymentModeId: number
  referenceNumber: string | null
  notes: string | null
}

export interface DailyCollectionMode {
  paymentModeId: number
  paymentModeName: string
  amount: number
  count: number
}

export interface DailyCollection {
  date: string
  byMode: DailyCollectionMode[]
  total: number
}

export interface DayCloseModeAmount {
  paymentModeId: number
  paymentModeName: string
  systemAmount: number
  declaredAmount: number
  variance: number
}

export interface DayClose {
  id: number
  date: string
  status: DayCloseStatus
  closedAt: string | null
  closedByName: string | null
  notes: string | null
  modeAmounts: DayCloseModeAmount[]
}

export interface DeclaredModeAmount {
  paymentModeId: number
  declaredAmount: number
}

export interface CloseDayPayload {
  date: string
  declaredAmounts: DeclaredModeAmount[]
  notes: string | null
}
