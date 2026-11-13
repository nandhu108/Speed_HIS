export type AllergenType = 1 | 2 | 3 | 4 // Drug, Food, Environmental, Other
export type AllergySeverity = 1 | 2 | 3 // Mild, Moderate, Severe
export type ProblemStatus = 1 | 2 // Active, Resolved
export type VirusScanStatus = 1 | 2 | 3 // NotScanned, Clean, Infected
export type TimelineEventType = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 // Visit, Admission, LabResult, Prescription, Procedure, DischargeSummary, ClinicalNote, Attachment, RadiologyReport

export const ALLERGEN_TYPE_LABELS: Record<AllergenType, string> = { 1: 'Drug', 2: 'Food', 3: 'Environmental', 4: 'Other' }
export const ALLERGY_SEVERITY_LABELS: Record<AllergySeverity, string> = { 1: 'Mild', 2: 'Moderate', 3: 'Severe' }
export const PROBLEM_STATUS_LABELS: Record<ProblemStatus, string> = { 1: 'Active', 2: 'Resolved' }
export const VIRUS_SCAN_STATUS_LABELS: Record<VirusScanStatus, string> = { 1: 'Not scanned', 2: 'Clean', 3: 'Infected' }
export const TIMELINE_EVENT_LABELS: Record<TimelineEventType, string> = {
  1: 'OPD Visit', 2: 'Admission', 3: 'Lab Result', 4: 'Prescription', 5: 'Procedure', 6: 'Discharge Summary', 7: 'Clinical Note', 8: 'Attachment',
  9: 'Radiology Report',
}

// ── Histories ────────────────────────────────────────────────────────────
export interface MedicalHistoryEntry {
  id: number
  condition: string
  diagnosedDate: string | null
  notes: string | null
  recordedByName: string
  recordedAt: string
}
export interface AddMedicalHistoryPayload {
  condition: string
  diagnosedDate: string | null
  notes: string | null
}

export interface SurgicalHistoryEntry {
  id: number
  procedureName: string
  procedureDate: string | null
  surgeon: string | null
  notes: string | null
  recordedByName: string
  recordedAt: string
}
export interface AddSurgicalHistoryPayload {
  procedureName: string
  procedureDate: string | null
  surgeon: string | null
  notes: string | null
}

export interface FamilyHistoryEntry {
  id: number
  relation: string
  condition: string
  notes: string | null
  recordedByName: string
  recordedAt: string
}
export interface AddFamilyHistoryPayload {
  relation: string
  condition: string
  notes: string | null
}

export interface ImmunizationEntry {
  id: number
  vaccineName: string
  doseNumber: number | null
  administeredDate: string
  nextDueDate: string | null
  administeredBy: string | null
  notes: string | null
  recordedByName: string
  recordedAt: string
}
export interface AddImmunizationPayload {
  vaccineName: string
  doseNumber: number | null
  administeredDate: string
  nextDueDate: string | null
  administeredBy: string | null
  notes: string | null
}

export interface AllergyEntry {
  id: number
  allergenType: AllergenType
  allergenName: string
  reaction: string | null
  severity: AllergySeverity
  isActive: boolean
  recordedByName: string
  recordedAt: string
}
export interface AddAllergyPayload {
  allergenType: AllergenType
  allergenName: string
  reaction: string | null
  severity: AllergySeverity
}
export interface AllergyMatch {
  drugName: string
  allergyId: number
  allergenName: string
  reaction: string | null
  severity: AllergySeverity
}

// ── Problem list ─────────────────────────────────────────────────────────
export interface ProblemListEntry {
  id: number
  icdCodeId: number | null
  icdCode: string | null
  diagnosisText: string
  status: ProblemStatus
  identifiedDate: string
  resolvedDate: string | null
  notes: string | null
  recordedByName: string
}
export interface AddProblemPayload {
  icdCodeId: number | null
  diagnosisText: string
  identifiedDate: string
  notes: string | null
}

// ── Clinical notes ───────────────────────────────────────────────────────
export interface ClinicalNoteEntry {
  id: number
  noteType: string
  title: string | null
  content: string
  noteAt: string
  recordedByName: string
  opVisitId: number | null
  admissionId: number | null
}
export interface AddClinicalNotePayload {
  noteType: string
  title: string | null
  content: string
  opVisitId: number | null
  admissionId: number | null
}

// ── Attachments ──────────────────────────────────────────────────────────
export interface AttachmentEntry {
  id: number
  attachmentType: string
  fileName: string
  contentType: string
  sizeBytes: number
  description: string | null
  virusScanStatus: VirusScanStatus
  uploadedByName: string
  uploadedAt: string
}

// ── Timeline & history views ─────────────────────────────────────────────
export interface TimelineEvent {
  at: string
  type: TimelineEventType
  title: string
  summary: string
  linkId: number
}

export interface PrescriptionHistoryItem {
  drugId: number
  drugName: string
  dose: string
  frequency: string
  duration: string
}
export interface PrescriptionHistoryEntry {
  visitId: number
  visitDate: string
  doctorName: string
  items: PrescriptionHistoryItem[]
}

export interface InvestigationHistoryEntry {
  labOrderItemId: number
  labOrderId: number
  orderDate: string
  testName: string
  status: string
  resultSummary: string | null
}

export interface ProcedureHistoryEntry {
  admissionId: number
  admissionNumber: string
  procedureName: string
  performedAt: string
  performedByDoctorName: string
  findings: string | null
}

export interface DischargeSummaryArchiveEntry {
  admissionId: number
  admissionNumber: string
  dischargedAt: string | null
  finalDiagnosis: string
  isApproved: boolean
}

export interface ResultsTrendPoint {
  date: string
  value: number
  flag: string
}
export interface ResultsTrendSeries {
  parameterId: number
  parameterName: string
  unit: string | null
  points: ResultsTrendPoint[]
}
