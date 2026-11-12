export type NursingShift = 1 | 2 | 3 // Morning, Evening, Night
export type RiskAssessmentType = 1 | 2 | 3 // Pain, FallRisk, BedSore
export type CarePlanStatus = 1 | 2 // Active, Resolved
export type MarStatus = 1 | 2 | 3 | 4 // Pending, Administered, Skipped, Refused
export type IncidentSeverity = 1 | 2 | 3 | 4 // Minor, Moderate, Major, Sentinel
export type IncidentStatus = 1 | 2 | 3 // Reported, UnderReview, Closed

export const SHIFT_LABELS: Record<NursingShift, string> = { 1: 'Morning', 2: 'Evening', 3: 'Night' }
export const RISK_TYPE_LABELS: Record<RiskAssessmentType, string> = { 1: 'Pain', 2: 'Fall Risk (Morse)', 3: 'Bed Sore (Braden)' }
export const CARE_PLAN_STATUS_LABELS: Record<CarePlanStatus, string> = { 1: 'Active', 2: 'Resolved' }
export const MAR_STATUS_LABELS: Record<MarStatus, string> = { 1: 'Pending', 2: 'Administered', 3: 'Skipped', 4: 'Refused' }
export const INCIDENT_SEVERITY_LABELS: Record<IncidentSeverity, string> = { 1: 'Minor', 2: 'Moderate', 3: 'Major', 4: 'Sentinel' }
export const INCIDENT_STATUS_LABELS: Record<IncidentStatus, string> = { 1: 'Reported', 2: 'Under Review', 3: 'Closed' }

// ── Admission-scoped charting ───────────────────────────────────────────────
export interface NursingAssessment {
  id: number
  templateCode: string
  assessedAt: string
  assessedByName: string
  dataJson: string
}

export interface NursingNote {
  id: number
  shift: NursingShift
  noteAt: string
  recordedByName: string
  notes: string
}

export interface RiskAssessment {
  id: number
  assessmentType: RiskAssessmentType
  assessedAt: string
  assessedByName: string
  itemScores: Record<string, number>
  totalScore: number
  riskLevel: string
}

export interface CarePlan {
  id: number
  problemStatement: string
  goal: string
  interventions: string
  status: CarePlanStatus
  createdByName: string
  createdAt: string
  evaluationNotes: string | null
  resolvedAt: string | null
}

export interface TransferNote {
  id: number
  transferredAt: string
  handoverNotes: string
  transferredByName: string
  receivedByName: string | null
}

// ── MAR ──────────────────────────────────────────────────────────────────
export interface MarAdministration {
  action: MarStatus
  actionAt: string
  actionByName: string
  verifiedByName: string | null
  reason: string | null
  notes: string | null
  batchId: number | null
  batchNumber: string | null
  quantity: number | null
  stockDeducted: boolean
}

export interface MarSchedule {
  id: number
  medicationOrderId: number
  drugId: number
  drugName: string
  dose: string
  route: string
  frequency: string
  dueAt: string
  status: MarStatus
  isHighAlert: boolean
  administration: MarAdministration | null
}

export interface MarActionPayload {
  action: MarStatus
  reason: string | null
  notes: string | null
  verifiedByUserId: number | null
  // Optional even when administering — stock decrement is opt-in so charting a dose is
  // never blocked by not knowing/having a batch on hand.
  batchId: number | null
  quantity: number | null
}

// ── Shift handover ───────────────────────────────────────────────────────
export interface WardPatient {
  admissionId: number
  admissionNumber: string
  patientName: string
  bedCode: string
}

export interface PatientHandoverNotePayload {
  admissionId: number
  situation: string
  background: string
  assessment: string
  recommendation: string
}

export interface CreateShiftHandoverPayload {
  wardId: number
  shiftDate: string
  shift: NursingShift
  handoverToUserId: number | null
  generalNotes: string | null
  patientNotes: PatientHandoverNotePayload[]
}

export interface ShiftHandoverListItem {
  id: number
  wardId: number
  wardName: string
  shiftDate: string
  shift: NursingShift
  handoverFromName: string
  handoverToName: string | null
  handedOverAt: string
  patientCount: number
}

export interface PatientHandoverNote extends PatientHandoverNotePayload {
  id: number
  admissionNumber: string
  patientName: string
}

export interface ShiftHandoverDetail {
  id: number
  wardId: number
  wardName: string
  shiftDate: string
  shift: NursingShift
  handoverFromName: string
  handoverToName: string | null
  generalNotes: string | null
  handedOverAt: string
  patientNotes: PatientHandoverNote[]
}

// ── Incidents ────────────────────────────────────────────────────────────
export interface ReportIncidentPayload {
  incidentDate: string
  incidentType: string
  location: string
  severity: IncidentSeverity
  description: string
  immediateActionTaken: string | null
  patientId: number | null
  admissionId: number | null
}

export interface Incident {
  id: number
  incidentDate: string
  incidentType: string
  location: string
  severity: IncidentSeverity
  description: string
  immediateActionTaken: string | null
  patientId: number | null
  patientName: string | null
  admissionId: number | null
  admissionNumber: string | null
  reportedByName: string
  status: IncidentStatus
  reviewNotes: string | null
  reviewedByName: string | null
  closedAt: string | null
  /** Set by Quality's RCA action (QUALITY.INCIDENT.RCA) — see backend Incident's doc comment. */
  sourceModule: string | null
  isSentinelEvent: boolean
  rootCauseAnalysis: string | null
}

export interface RecordIncidentRcaPayload {
  rootCauseAnalysis: string
  isSentinelEvent: boolean
}
