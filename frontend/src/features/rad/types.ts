export type ClinicalOrderPriority = 1 | 2 | 3 // Routine, Urgent, Stat
export type RadOrderStatus = 1 | 2 | 3 | 4 // Ordered, InProgress, Completed, Cancelled
export type RadOrderItemStatus = 1 | 2 | 3 | 4 | 5 | 6 | 7 // Ordered..Approved, Cancelled

export const PRIORITY_LABELS: Record<ClinicalOrderPriority, string> = { 1: 'Routine', 2: 'Urgent', 3: 'STAT' }
export const RAD_ORDER_STATUS_LABELS: Record<RadOrderStatus, string> = {
  1: 'Ordered',
  2: 'In Progress',
  3: 'Completed',
  4: 'Cancelled',
}
export const ITEM_STATUS_LABELS: Record<RadOrderItemStatus, string> = {
  1: 'Ordered',
  2: 'Scheduled',
  3: 'Image Captured',
  4: 'Report Entered',
  5: 'Verified',
  6: 'Approved',
  7: 'Cancelled',
}

// ── Masters ──────────────────────────────────────────────────────────────
export interface RadiologyTestListItem {
  id: number
  code: string
  name: string
  modalityId: number
  modalityName: string
  bodyPart: string
  requiresContrast: boolean
  tatHours: number
  isActive: boolean
}

export interface RadiologyTestDetail {
  id: number
  code: string
  name: string
  modalityId: number
  modalityName: string
  bodyPart: string
  requiresContrast: boolean
  prepInstructions: string | null
  serviceId: number | null
  serviceName: string | null
  tatHours: number
  isActive: boolean
}

export interface SaveRadiologyTestPayload {
  code: string
  name: string
  modalityId: number
  bodyPart: string
  requiresContrast: boolean
  prepInstructions: string | null
  serviceId: number | null
  tatHours: number
  isActive: boolean
}

export interface RadReportTemplateListItem {
  id: number
  radiologyTestId: number
  radiologyTestName: string
  name: string
  isActive: boolean
}

export interface RadReportTemplateDetail {
  id: number
  radiologyTestId: number
  radiologyTestName: string
  name: string
  findingsTemplate: string
  impressionTemplate: string
  isActive: boolean
}

export interface SaveRadReportTemplatePayload {
  radiologyTestId: number
  name: string
  findingsTemplate: string
  impressionTemplate: string
  isActive: boolean
}

// ── Orders ───────────────────────────────────────────────────────────────
export interface RadOrderItem {
  id: number
  radiologyTestId: number
  radiologyTestName: string
  modalityName: string
  accessionNumber: string
  status: RadOrderItemStatus
  requiresContrast: boolean
  hasContrastConsent: boolean
  scheduledAt: string | null
}

export interface RadOrderListItem {
  id: number
  orderNumber: string
  orderDate: string
  patientId: number
  patientUhid: string
  patientName: string
  priority: ClinicalOrderPriority
  status: RadOrderStatus
  itemCount: number
}

export interface RadOrderDetail {
  id: number
  orderNumber: string
  orderDate: string
  patientId: number
  patientUhid: string
  patientName: string
  patientGender: string
  patientAgeDisplay: string
  opVisitId: number | null
  admissionId: number | null
  emergencyVisitId: number | null
  orderedByDoctorId: number | null
  orderedByDoctorName: string | null
  priority: ClinicalOrderPriority
  status: RadOrderStatus
  clinicalNotes: string | null
  cancelReason: string | null
  cancelledAt: string | null
  items: RadOrderItem[]
}

export interface CreateRadOrderPayload {
  patientId: number
  opVisitId: number | null
  admissionId: number | null
  emergencyVisitId: number | null
  clinicalOrderId: number | null
  orderedByDoctorId: number | null
  priority: ClinicalOrderPriority
  clinicalNotes: string | null
  radiologyTestIds: number[]
}

// ── Scheduling ───────────────────────────────────────────────────────────
export interface SchedulingWorklistItem {
  radOrderItemId: number
  radOrderId: number
  orderNumber: string
  patientId: number
  patientUhid: string
  patientName: string
  radiologyTestId: number
  radiologyTestName: string
  modalityId: number
  modalityName: string
  requiresContrast: boolean
  priority: ClinicalOrderPriority
  orderDate: string
}

export interface ScheduleItemPayload {
  scheduledAt: string
  notes: string | null
}

// ── Image capture ────────────────────────────────────────────────────────
export interface CaptureWorklistItem {
  radOrderItemId: number
  radOrderId: number
  orderNumber: string
  patientId: number
  patientUhid: string
  patientName: string
  radiologyTestId: number
  radiologyTestName: string
  modalityId: number
  modalityName: string
  requiresContrast: boolean
  hasContrastConsent: boolean
  scheduledAt: string
  priority: ClinicalOrderPriority
}

export interface CaptureContrastConsentPayload {
  signedByName: string
  relationshipToPatient: string | null
}

// ── Reporting ────────────────────────────────────────────────────────────
export interface ReportingWorklistItem {
  radOrderItemId: number
  radOrderId: number
  orderNumber: string
  patientId: number
  patientUhid: string
  patientName: string
  patientGender: string
  patientAgeDisplay: string
  radiologyTestId: number
  radiologyTestName: string
  priority: ClinicalOrderPriority
  orderDate: string
}

export interface EnterReportPayload {
  templateId: number | null
  findings: string
  impression: string
}

export interface RadReport {
  id: number
  radOrderItemId: number
  templateId: number | null
  findings: string
  impression: string
  enteredByUserId: number
  enteredByName: string | null
  enteredAt: string
  verifiedByUserId: number | null
  verifiedByName: string | null
  verifiedAt: string | null
  approvedByUserId: number | null
  approvedByName: string | null
  approvedAt: string | null
}

/** A report was entered (or is further along) with no BillItemId set — see RadOrderItem.BillItemId's doc comment; advisory only. */
export interface UnbilledRadOrderRow {
  radOrderItemId: number
  orderNumber: string
  orderDate: string
  patientUhid: string
  patientName: string
  radiologyTestName: string
  status: RadOrderItemStatus
}

export interface RadTatReportRow {
  radOrderItemId: number
  orderNumber: string
  radiologyTestName: string
  orderDate: string
  scheduledAt: string | null
  imageCapturedAt: string | null
  approvedAt: string | null
  turnaroundHours: number | null
}
