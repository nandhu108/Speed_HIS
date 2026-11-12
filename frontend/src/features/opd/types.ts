/** Mirrors backend Prixo.Domain.Enums.VisitType. */
export const VISIT_TYPES = { New: 1, Review: 2 } as const
export type VisitType = (typeof VISIT_TYPES)[keyof typeof VISIT_TYPES]
export const VISIT_TYPE_LABELS: Record<VisitType, string> = { 1: 'New', 2: 'Review' }

/** Mirrors backend Prixo.Domain.Enums.OpVisitStatus. */
export const OP_VISIT_STATUSES = { Waiting: 1, InConsult: 2, Completed: 3, Cancelled: 4 } as const
export type OpVisitStatus = (typeof OP_VISIT_STATUSES)[keyof typeof OP_VISIT_STATUSES]
export const OP_VISIT_STATUS_LABELS: Record<OpVisitStatus, string> = {
  1: 'Waiting',
  2: 'In consult',
  3: 'Completed',
  4: 'Cancelled',
}

/** Mirrors backend Prixo.Domain.Enums.DiagnosisType. */
export const DIAGNOSIS_TYPES = { Provisional: 1, Final: 2 } as const
export type DiagnosisType = (typeof DIAGNOSIS_TYPES)[keyof typeof DIAGNOSIS_TYPES]
export const DIAGNOSIS_TYPE_LABELS: Record<DiagnosisType, string> = { 1: 'Provisional', 2: 'Final' }

/** Mirrors backend Prixo.Domain.Enums.ClinicalOrderType. */
export const ORDER_TYPES = { Lab: 1, Radiology: 2, Procedure: 3 } as const
export type ClinicalOrderType = (typeof ORDER_TYPES)[keyof typeof ORDER_TYPES]
export const ORDER_TYPE_LABELS: Record<ClinicalOrderType, string> = { 1: 'Lab', 2: 'Radiology', 3: 'Procedure' }

/** Mirrors backend Prixo.Domain.Enums.ClinicalOrderPriority. */
export const ORDER_PRIORITIES = { Routine: 1, Urgent: 2, Stat: 3 } as const
export type ClinicalOrderPriority = (typeof ORDER_PRIORITIES)[keyof typeof ORDER_PRIORITIES]
export const ORDER_PRIORITY_LABELS: Record<ClinicalOrderPriority, string> = { 1: 'Routine', 2: 'Urgent', 3: 'Stat' }

/** Mirrors backend Prixo.Domain.Enums.ClinicalOrderStatus. */
export const ORDER_STATUSES = { Ordered: 1, InProgress: 2, Completed: 3, Cancelled: 4 } as const
export type ClinicalOrderStatus = (typeof ORDER_STATUSES)[keyof typeof ORDER_STATUSES]
export const ORDER_STATUS_LABELS: Record<ClinicalOrderStatus, string> = {
  1: 'Ordered',
  2: 'In progress',
  3: 'Completed',
  4: 'Cancelled',
}

/** Mirrors backend Prixo.Domain.Enums.ReferralDirection. */
export const REFERRAL_DIRECTIONS = { Internal: 1, External: 2 } as const
export type ReferralDirection = (typeof REFERRAL_DIRECTIONS)[keyof typeof REFERRAL_DIRECTIONS]
export const REFERRAL_DIRECTION_LABELS: Record<ReferralDirection, string> = { 1: 'Internal', 2: 'External' }

/** Mirrors backend Prixo.Domain.Enums.AppointmentBookingType. */
export const BOOKING_TYPES = { Booked: 1, WalkIn: 2 } as const
export type BookingType = (typeof BOOKING_TYPES)[keyof typeof BOOKING_TYPES]
export const BOOKING_TYPE_LABELS: Record<BookingType, string> = { 1: 'Booked', 2: 'Walk-In' }

export interface RegisterOpVisitPayload {
  patientId: number
  doctorId: number
  /** Mandatory clinical department (General Medicine, General Surgery, Paediatrics…). */
  departmentId: number
  appointmentId: number | null
  campId?: number | null
  bookingType?: BookingType
  visitClassId?: number | null
  sessionId?: number | null
  isMlcCase?: boolean
  advanceAmount?: number | null
  paymentModeId?: number | null
  advanceReferenceNumber?: string | null
}

export interface BulkImportResult {
  inserted: number
  skipped: number
  errors: string[]
}

export interface OpVisitListItem {
  id: number
  visitNumber: string
  tokenNumber: number
  patientId: number
  patientUhid: string
  patientName: string
  doctorId: number
  doctorName: string
  visitDate: string
  visitType: VisitType
  status: OpVisitStatus
  alreadyAdmitted: boolean
  campId: number | null
  campName: string | null
}

export interface OpVisitDetail {
  id: number
  visitNumber: string
  tokenNumber: number
  patientId: number
  patientUhid: string
  patientName: string
  patientMobile: string
  patientGender: string
  patientAgeDisplay: string
  doctorId: number
  doctorName: string
  appointmentId: number | null
  visitDate: string
  visitType: VisitType
  status: OpVisitStatus
  consultationFee: number
  inConsultAt: string | null
  completedAt: string | null
  cancelReason: string | null
  cancelledAt: string | null
  campId: number | null
  campName: string | null
  bookingType: BookingType
  visitClassId: number | null
  visitClassName: string | null
  sessionId: number | null
  sessionName: string | null
  isMlcCase: boolean
  departmentId: number | null
  departmentName: string | null
}

export interface QueueEntry {
  visitId: number
  tokenNumber: number
  patientId: number
  patientName: string
  patientUhid: string
  doctorId: number
  doctorName: string
  visitType: VisitType
  status: OpVisitStatus
}

export interface RecentVisitSummary {
  id: number
  visitDate: string
  doctorName: string
  diagnoses: string[]
}

export interface RecentMedication {
  drugName: string
  dose: string
  frequency: string
  prescribedOn: string
}

export interface VisitVitals {
  id: number | null
  heightCm: number | null
  weightKg: number | null
  bmi: number | null
  temperatureF: number | null
  pulseRate: number | null
  respiratoryRate: number | null
  bloodPressureSystolic: number | null
  bloodPressureDiastolic: number | null
  spO2: number | null
  randomBloodSugar: number | null
}

export interface SaveVitalsPayload {
  heightCm: number | null
  weightKg: number | null
  temperatureF: number | null
  pulseRate: number | null
  respiratoryRate: number | null
  bloodPressureSystolic: number | null
  bloodPressureDiastolic: number | null
  spO2: number | null
  randomBloodSugar: number | null
}

export interface VisitComplaint {
  id: number
  complaint: string
  duration: string | null
}
export interface SaveComplaint {
  complaint: string
  duration: string | null
}

export interface VisitExamination {
  historyNotes: string | null
  examinationNotes: string | null
}

export interface VisitDiagnosis {
  id: number
  icdCodeId: number
  icdCode: string
  icdDescription: string
  type: DiagnosisType
  notes: string | null
}
export interface SaveDiagnosis {
  icdCodeId: number
  type: DiagnosisType
  notes: string | null
}

export interface PrescriptionItem {
  id: number
  drugId: number
  drugName: string
  dose: string
  frequency: string
  duration: string
  route: string | null
  instructions: string | null
}
export interface SavePrescriptionItem {
  drugId: number
  dose: string
  frequency: string
  duration: string
  route: string | null
  instructions: string | null
}
export interface Prescription {
  id: number | null
  notes: string | null
  items: PrescriptionItem[]
}
export interface SavePrescriptionPayload {
  notes: string | null
  items: SavePrescriptionItem[]
}

export interface ClinicalOrder {
  id: number
  orderType: ClinicalOrderType
  description: string
  priority: ClinicalOrderPriority
  status: ClinicalOrderStatus
  notes: string | null
}
export interface AddOrderPayload {
  orderType: ClinicalOrderType
  description: string
  priority: ClinicalOrderPriority
  notes: string | null
}

export interface VisitReferral {
  id: number
  direction: ReferralDirection
  referredToDoctorId: number | null
  referredToDoctorName: string | null
  externalDetails: string | null
  reason: string
  opinionNotes: string | null
  opinionEnteredAt: string | null
}
export interface AddReferralPayload {
  direction: ReferralDirection
  referredToDoctorId: number | null
  externalDetails: string | null
  reason: string
}
export interface SaveReferralOpinionPayload {
  opinionNotes: string
}

export interface FollowUpAdvice {
  id: number | null
  followUpDate: string | null
  notes: string | null
}
export interface SaveFollowUpPayload {
  followUpDate: string
  notes: string | null
}

export interface ConsultationWorkspace {
  visit: OpVisitDetail
  recentVisits: RecentVisitSummary[]
  recentMedications: RecentMedication[]
  vitals: VisitVitals | null
  complaints: VisitComplaint[]
  examination: VisitExamination | null
  diagnoses: VisitDiagnosis[]
  prescription: Prescription | null
  orders: ClinicalOrder[]
  referrals: VisitReferral[]
  followUp: FollowUpAdvice | null
}

export interface PrescriptionTemplateItem {
  id: number
  drugId: number
  drugName: string
  dose: string
  frequency: string
  duration: string
  route: string | null
  instructions: string | null
}
export interface SaveTemplateItem {
  drugId: number
  dose: string
  frequency: string
  duration: string
  route: string | null
  instructions: string | null
}
export interface PrescriptionTemplate {
  id: number
  doctorId: number
  name: string
  items: PrescriptionTemplateItem[]
}
export interface SavePrescriptionTemplatePayload {
  doctorId: number
  name: string
  items: SaveTemplateItem[]
}

// ── OP Report ────────────────────────────────────────────────────────────
export type OpReportMode = 'summary' | 'detail'
export interface OpReportColumn {
  key: string
  label: string
}
export interface OpReportResult {
  reportName: string
  columns: OpReportColumn[]
  rows: Record<string, unknown>[]
}
