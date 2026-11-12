export type AdmissionStatus = 1 | 2 | 3 // Admitted, Discharged, Cancelled
export type AdmissionType = 1 | 2 | 3 // Planned, Emergency, Referral
export type DischargeType = 1 | 2 | 3 | 4 | 5 // Normal, Lama, Dama, Death, Referral
export type ClearanceType = 1 | 2 | 3 | 4 // Billing, Pharmacy, Nursing, FinalInvestigation
export type ConsultantRole = 1 | 2 // Primary, Secondary
export type DoctorOrderType = 1 | 2 | 3 | 4 | 5 // Medication, Lab, Radiology, Diet, Nursing
export type ClinicalOrderPriority = 1 | 2 | 3 // Routine, Urgent, Stat
export type ClinicalOrderStatus = 1 | 2 | 3 | 4 // Ordered, InProgress, Completed, Cancelled
export type BedStatus = 1 | 2 | 3 | 4 | 5 // Vacant, Occupied, Reserved, Cleaning, Blocked
export type AdmissionRequestStatus = 1 | 2 | 3 // Pending, Approved, Rejected

export const ADMISSION_STATUS_LABELS: Record<AdmissionStatus, string> = { 1: 'Admitted', 2: 'Discharged', 3: 'Cancelled' }
export const ADMISSION_TYPE_LABELS: Record<AdmissionType, string> = { 1: 'Planned', 2: 'Emergency', 3: 'Referral' }
export const DISCHARGE_TYPE_LABELS: Record<DischargeType, string> = {
  1: 'Normal', 2: 'LAMA', 3: 'DAMA', 4: 'Death', 5: 'Referral',
}
export const CLEARANCE_TYPE_LABELS: Record<ClearanceType, string> = {
  1: 'Billing', 2: 'Pharmacy', 3: 'Nursing', 4: 'Final Investigation',
}
export const DOCTOR_ORDER_TYPE_LABELS: Record<DoctorOrderType, string> = {
  1: 'Medication', 2: 'Lab', 3: 'Radiology', 4: 'Diet', 5: 'Nursing',
}
export const ORDER_PRIORITY_LABELS: Record<ClinicalOrderPriority, string> = { 1: 'Routine', 2: 'Urgent', 3: 'Stat' }
export const ORDER_STATUS_LABELS: Record<ClinicalOrderStatus, string> = {
  1: 'Ordered', 2: 'In Progress', 3: 'Completed', 4: 'Cancelled',
}
export const BED_STATUS_LABELS: Record<BedStatus, string> = {
  1: 'Vacant', 2: 'Occupied', 3: 'Reserved', 4: 'Cleaning', 5: 'Blocked',
}
export const ADMISSION_REQUEST_STATUS_LABELS: Record<AdmissionRequestStatus, string> = {
  1: 'Pending', 2: 'Approved', 3: 'Rejected',
}

// ── Admissions ───────────────────────────────────────────────────────────
export interface AdmitPatientPayload {
  patientId: number
  primaryConsultantId: number
  /** Mandatory clinical department (General Medicine, General Surgery, Paediatrics…). */
  departmentId: number
  bedId: number
  admissionType: AdmissionType
  provisionalDiagnosis: string | null
  referringOpVisitId: number | null
  referringEmergencyVisitId: number | null
  secondaryConsultantIds: number[]
  consentTemplateId: number | null
  consentSignedByName: string | null
  consentRelationshipToPatient: string | null
  initialDepositAmount: number
  depositPaymentModeId: number | null
  depositReferenceNumber: string | null
  admissionRequestId?: number | null
}

// ── Admission requests ──────────────────────────────────────────────────────
export interface CreateAdmissionRequestPayload {
  patientId: number
  requestedConsultantId: number | null
  wardId: number | null
  expectedAdmissionDate: string
  reason: string | null
}

export interface AdmissionRequestItem {
  id: number
  patientId: number
  patientUhid: string
  patientName: string
  requestedConsultantId: number | null
  requestedConsultantName: string | null
  wardId: number | null
  wardName: string | null
  expectedAdmissionDate: string
  reason: string | null
  status: AdmissionRequestStatus
  rejectionReason: string | null
  requestedByName: string
  createdAt: string
  decidedByName: string | null
  decidedAt: string | null
  approvedAdmissionId: number | null
}

export interface AdmissionListItem {
  id: number
  admissionNumber: string
  admissionDate: string
  patientId: number
  patientUhid: string
  patientName: string
  admissionType: AdmissionType
  status: AdmissionStatus
  primaryConsultantName: string
  wardName: string
  roomName: string
  bedCode: string
}

export interface AdmissionConsultant {
  id: number
  doctorId: number
  doctorName: string
  role: ConsultantRole
  assignedAt: string
  endedAt: string | null
  notes: string | null
}

export interface BedAllocation {
  id: number
  bedId: number
  wardName: string
  roomName: string
  bedCode: string
  bedChargePerDay: number
  allocatedAt: string
  releasedAt: string | null
  transferReason: string | null
}

export interface AdmissionDetail {
  id: number
  admissionNumber: string
  admissionDate: string
  patientId: number
  patientUhid: string
  patientName: string
  patientGender: string
  patientAgeDisplay: string
  admissionType: AdmissionType
  provisionalDiagnosis: string | null
  primaryConsultantId: number
  primaryConsultantName: string
  referringOpVisitId: number | null
  referringOpVisitNumber: string | null
  referringEmergencyVisitId: number | null
  referringEmergencyVisitNumber: string | null
  status: AdmissionStatus
  currentBedId: number
  wardName: string
  roomName: string
  bedCode: string
  dischargeRequestedAt: string | null
  dischargeType: DischargeType | null
  dischargeTypeNotes: string | null
  dischargedAt: string | null
  cancelReason: string | null
  consultants: AdmissionConsultant[]
  bedAllocations: BedAllocation[]
  departmentId: number | null
  departmentName: string | null
}

// ── Doctor orders ────────────────────────────────────────────────────────
export interface MedicationOrderPayload {
  drugId: number
  dose: string
  route: string
  frequency: string
  startDate: string
  endDate: string | null
  instructions: string | null
}
export interface DietOrderPayload {
  dietType: string
  instructions: string | null
}
export interface CreateDoctorOrderPayload {
  orderedByDoctorId: number
  orderType: DoctorOrderType
  description: string
  priority: ClinicalOrderPriority
  notes: string | null
  medication: MedicationOrderPayload | null
  diet: DietOrderPayload | null
}
export interface DoctorOrder {
  id: number
  admissionId: number
  orderType: DoctorOrderType
  orderedByDoctorId: number
  orderedByDoctorName: string
  orderedAt: string
  priority: ClinicalOrderPriority
  status: ClinicalOrderStatus
  description: string
  notes: string | null
  medication: { drugId: number; drugName: string; dose: string; route: string; frequency: string; startDate: string; endDate: string | null; instructions: string | null } | null
  diet: { dietType: string; instructions: string | null } | null
}

// ── Clinical charts ──────────────────────────────────────────────────────
export interface ProgressNote {
  id: number
  doctorId: number
  doctorName: string
  noteAt: string
  notes: string
}
export interface IpVitals {
  id: number
  recordedAt: string
  recordedByName: string
  temperatureF: number | null
  pulseRate: number | null
  respiratoryRate: number | null
  bloodPressureSystolic: number | null
  bloodPressureDiastolic: number | null
  spO2: number | null
  randomBloodSugar: number | null
}
export interface IntakeOutputEntry {
  id: number
  recordedAt: string
  isIntake: boolean
  category: string
  quantityMl: number
  notes: string | null
}
export interface AddProcedureNoteRequestPayload {
  procedureName: string
  performedByDoctorId: number
  performedAt: string
  findings: string | null
  notes: string | null
}
export interface ProcedureNote {
  id: number
  procedureName: string
  performedByDoctorId: number
  performedByDoctorName: string
  performedAt: string
  findings: string | null
  notes: string | null
}

// ── Discharge ────────────────────────────────────────────────────────────
export interface DischargeClearance {
  id: number
  clearanceType: ClearanceType
  isCleared: boolean
  clearedByName: string | null
  clearedAt: string | null
  remarks: string | null
  /** Set only on the Billing row, from the admission's latest bill. Advisory — doesn't block clearing. */
  billBalanceAmount: number | null
}
export interface DischargeSummary {
  id: number
  admissionId: number
  finalDiagnosis: string
  courseInHospital: string | null
  medicationsOnDischarge: string | null
  followUpAdvice: string | null
  conditionAtDischarge: string | null
  isApproved: boolean
  approvedByName: string | null
  approvedAt: string | null
}

// ── Bed board ────────────────────────────────────────────────────────────
export interface BedBoardItem {
  bedId: number
  bedCode: string
  status: BedStatus
  wardName: string
  roomName: string
  roomTypeName: string
  admissionId: number | null
  admissionNumber: string | null
  patientName: string | null
  primaryConsultantName: string | null
  allocatedAt: string | null
}
export interface WardOccupancy {
  wardId: number
  wardName: string
  totalBeds: number
  occupied: number
  occupancyRatePercent: number
}
export interface BedOccupancyDashboard {
  totalBeds: number
  occupied: number
  vacant: number
  reserved: number
  cleaning: number
  blocked: number
  occupancyRatePercent: number
  byWard: WardOccupancy[]
}

// ── IP Report / Ward Occupancy Report ───────────────────────────────────
export type IpReportMode = 'summary' | 'detail'
export interface IpReportColumn {
  key: string
  label: string
}
export interface IpReportResult {
  reportName: string
  columns: IpReportColumn[]
  rows: Record<string, unknown>[]
}
