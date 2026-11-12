export type ParameterValueType = 1 | 2 | 3 // Numeric, Text, Dropdown
export type ClinicalOrderPriority = 1 | 2 | 3 // Routine, Urgent, Stat
export type LabOrderStatus = 1 | 2 | 3 | 4 // Ordered, InProgress, Completed, Cancelled
export type LabOrderItemStatus = 1 | 2 | 3 | 4 | 5 | 6 | 7 // Ordered..Approved, Cancelled
export type SampleStatus = 1 | 2 | 3 // Collected, Received, Rejected
export type ResultFlag = 1 | 2 | 3 | 4 // Normal, Low, High, Critical
export type Gender = 1 | 2 | 3

export const PRIORITY_LABELS: Record<ClinicalOrderPriority, string> = { 1: 'Routine', 2: 'Urgent', 3: 'STAT' }
export const LAB_ORDER_STATUS_LABELS: Record<LabOrderStatus, string> = {
  1: 'Ordered',
  2: 'In Progress',
  3: 'Completed',
  4: 'Cancelled',
}
export const ITEM_STATUS_LABELS: Record<LabOrderItemStatus, string> = {
  1: 'Ordered',
  2: 'Collected',
  3: 'Received',
  4: 'Result Entered',
  5: 'Verified',
  6: 'Approved',
  7: 'Cancelled',
}
export const SAMPLE_STATUS_LABELS: Record<SampleStatus, string> = { 1: 'Collected', 2: 'Received', 3: 'Rejected' }
export const FLAG_LABELS: Record<ResultFlag, string> = { 1: 'Normal', 2: 'Low', 3: 'High', 4: 'Critical' }

// ── Masters ──────────────────────────────────────────────────────────────
export interface ReferenceRange {
  id: number
  sex: Gender | null
  minAgeYears: number | null
  maxAgeYears: number | null
  normalLow: number | null
  normalHigh: number | null
  criticalLow: number | null
  criticalHigh: number | null
  textNormalValue: string | null
}

export interface SaveReferenceRangePayload {
  sex: Gender | null
  minAgeYears: number | null
  maxAgeYears: number | null
  normalLow: number | null
  normalHigh: number | null
  criticalLow: number | null
  criticalHigh: number | null
  textNormalValue: string | null
}

export interface LabTestParameter {
  id: number
  code: string
  name: string
  unit: string | null
  valueType: ParameterValueType
  dropdownOptions: string | null
  sortOrder: number
  referenceRanges: ReferenceRange[]
}

export interface SaveLabTestParameterPayload {
  code: string
  name: string
  unit: string | null
  valueType: ParameterValueType
  dropdownOptions: string | null
  sortOrder: number
  referenceRanges: SaveReferenceRangePayload[]
}

export interface LabTestListItem {
  id: number
  code: string
  name: string
  departmentName: string | null
  sampleTypeName: string
  tatHours: number
  isPanel: boolean
  isActive: boolean
}

export interface LabTestDetail {
  id: number
  code: string
  name: string
  departmentId: number | null
  departmentName: string | null
  sampleTypeId: number
  sampleTypeName: string
  serviceId: number | null
  serviceName: string | null
  method: string | null
  tatHours: number
  isPanel: boolean
  isActive: boolean
  parameters: LabTestParameter[]
}

export interface SaveLabTestPayload {
  code: string
  name: string
  departmentId: number | null
  sampleTypeId: number
  serviceId: number | null
  method: string | null
  tatHours: number
  isPanel: boolean
  parameters: SaveLabTestParameterPayload[]
  isActive: boolean
}

export interface TestProfileListItem {
  id: number
  code: string
  name: string
  price: number | null
  testCount: number
  isActive: boolean
}

export interface TestProfileMember {
  labTestId: number
  labTestName: string
}

export interface TestProfileDetail {
  id: number
  code: string
  name: string
  price: number | null
  isActive: boolean
  tests: TestProfileMember[]
}

export interface SaveTestProfilePayload {
  code: string
  name: string
  price: number | null
  labTestIds: number[]
  isActive: boolean
}

// ── Orders ───────────────────────────────────────────────────────────────
export interface LabOrderItem {
  id: number
  labTestId: number
  labTestName: string
  testProfileId: number | null
  testProfileName: string | null
  sampleId: number | null
  sampleNumber: string | null
  status: LabOrderItemStatus
}

export interface LabOrderListItem {
  id: number
  orderNumber: string
  orderDate: string
  patientId: number
  patientUhid: string
  patientName: string
  priority: ClinicalOrderPriority
  status: LabOrderStatus
  itemCount: number
}

export interface LabOrderDetail {
  id: number
  orderNumber: string
  orderDate: string
  patientId: number
  patientUhid: string
  patientName: string
  patientGender: string
  patientAgeDisplay: string
  opVisitId: number | null
  emergencyVisitId: number | null
  orderedByDoctorId: number | null
  orderedByDoctorName: string | null
  priority: ClinicalOrderPriority
  status: LabOrderStatus
  clinicalNotes: string | null
  cancelReason: string | null
  cancelledAt: string | null
  items: LabOrderItem[]
}

export interface CreateLabOrderItemPayload {
  labTestId: number | null
  testProfileId: number | null
}

export interface CreateLabOrderPayload {
  patientId: number
  opVisitId: number | null
  emergencyVisitId: number | null
  clinicalOrderId: number | null
  orderedByDoctorId: number | null
  priority: ClinicalOrderPriority
  clinicalNotes: string | null
  items: CreateLabOrderItemPayload[]
}

// ── Collection ───────────────────────────────────────────────────────────
export interface CollectionWorklistItem {
  labOrderItemId: number
  labOrderId: number
  orderNumber: string
  patientId: number
  patientUhid: string
  patientName: string
  labTestId: number
  labTestName: string
  sampleTypeId: number
  sampleTypeName: string
  priority: ClinicalOrderPriority
  orderDate: string
}

export interface CollectSamplePayload {
  labOrderId: number
  sampleTypeId: number
  labOrderItemIds: number[]
}

export interface Sample {
  id: number
  labOrderId: number
  orderNumber: string
  sampleTypeId: number
  sampleTypeName: string
  sampleNumber: string
  status: SampleStatus
  collectedAt: string
  receivedAt: string | null
  rejectionReason: string | null
  rejectedAt: string | null
}

export interface DepartmentWorklistItem {
  labOrderItemId: number
  labOrderId: number
  orderNumber: string
  patientId: number
  patientUhid: string
  patientName: string
  patientGender: string
  patientAgeDisplay: string
  labTestId: number
  labTestName: string
  departmentId: number | null
  departmentName: string | null
  priority: ClinicalOrderPriority
  orderDate: string
}

// ── Results ──────────────────────────────────────────────────────────────
export interface EnterResultValuePayload {
  labTestParameterId: number
  value: string
}

export interface EnterResultPayload {
  values: EnterResultValuePayload[]
  notes: string | null
}

export interface ResultValue {
  id: number
  labTestParameterId: number
  parameterName: string
  value: string
  unit: string | null
  normalLow: number | null
  normalHigh: number | null
  flag: ResultFlag
}

export interface LabResult {
  id: number
  labOrderItemId: number
  enteredByUserId: number
  enteredByName: string | null
  enteredAt: string
  verifiedByUserId: number | null
  verifiedByName: string | null
  verifiedAt: string | null
  approvedByUserId: number | null
  approvedByName: string | null
  approvedAt: string | null
  notes: string | null
  hasCriticalValue: boolean
  values: ResultValue[]
}

export interface CriticalAlert {
  id: number
  labResultId: number
  labOrderItemId: number
  orderNumber: string
  patientName: string
  resultValueId: number
  parameterName: string
  value: string
  raisedAt: string
  isAcknowledged: boolean
  acknowledgedByUserId: number | null
  acknowledgedAt: string | null
  acknowledgementNotes: string | null
}

/** A result was entered (or is further along) with no BillItemId set — see LabOrderItem.BillItemId's doc comment; advisory only. */
export interface UnbilledOrderRow {
  labOrderItemId: number
  orderNumber: string
  orderDate: string
  patientUhid: string
  patientName: string
  labTestName: string
  status: LabOrderItemStatus
}

export interface TatReportRow {
  labOrderItemId: number
  orderNumber: string
  labTestName: string
  orderDate: string
  collectedAt: string | null
  receivedAt: string | null
  approvedAt: string | null
  turnaroundHours: number | null
}
