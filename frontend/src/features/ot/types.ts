export type SurgeryStatus = 1 | 2 | 3 | 4 | 5 // Scheduled, InProgress, Completed, Cancelled, Postponed
export type SurgeryTeamRole = 1 | 2 | 3 | 4 | 5 // AssistantSurgeon, AssistantAnaesthetist, ScrubNurse, CirculatingNurse, Other
export type AnaesthesiaType = 1 | 2 | 3 | 4 | 5 | 6 // General, Spinal, Epidural, Local, Regional, Sedation

export const SURGERY_STATUS_LABELS: Record<SurgeryStatus, string> = {
  1: 'Scheduled', 2: 'In Progress', 3: 'Completed', 4: 'Cancelled', 5: 'Postponed',
}
export const TEAM_ROLE_LABELS: Record<SurgeryTeamRole, string> = {
  1: 'Assistant Surgeon', 2: 'Assistant Anaesthetist', 3: 'Scrub Nurse', 4: 'Circulating Nurse', 5: 'Other',
}
export const ANAESTHESIA_TYPE_LABELS: Record<AnaesthesiaType, string> = {
  1: 'General', 2: 'Spinal', 3: 'Epidural', 4: 'Local', 5: 'Regional', 6: 'Sedation',
}

// ── Masters ──────────────────────────────────────────────────────────────
export interface Theatre {
  id: number
  code: string
  name: string
  isActive: boolean
}
export interface SaveTheatrePayload {
  code: string
  name: string
  isActive: boolean
}

export interface Equipment {
  id: number
  code: string
  name: string
  theatreId: number | null
  theatreName: string | null
  isActive: boolean
}
export interface SaveEquipmentPayload {
  code: string
  name: string
  theatreId: number | null
  isActive: boolean
}

// ── Bookings ─────────────────────────────────────────────────────────────
export interface CreateBookingPayload {
  admissionId: number
  theatreId: number
  procedureName: string
  surgeonId: number
  anaesthetistId: number | null
  scheduledStart: string
  scheduledEnd: string
  notes: string | null
}

export interface SurgeryTeamMember {
  id: number
  role: SurgeryTeamRole
  doctorId: number | null
  doctorName: string | null
  staffName: string | null
}
export interface AddTeamMemberPayload {
  role: SurgeryTeamRole
  doctorId: number | null
  staffName: string | null
}

export interface BookingListItem {
  id: number
  bookingNumber: string
  patientName: string
  theatreName: string
  procedureName: string
  surgeonName: string
  anaesthetistName: string | null
  scheduledStart: string
  scheduledEnd: string
  status: SurgeryStatus
}

export interface BookingDetail {
  id: number
  bookingNumber: string
  patientId: number
  patientName: string
  admissionId: number
  admissionNumber: string
  theatreId: number
  theatreName: string
  procedureName: string
  surgeonId: number
  surgeonName: string
  anaesthetistId: number | null
  anaesthetistName: string | null
  scheduledStart: string
  scheduledEnd: string
  actualStart: string | null
  actualEnd: string | null
  status: SurgeryStatus
  statusReason: string | null
  patientConsentId: number | null
  consentVerifiedAt: string | null
  notes: string | null
  teamMembers: SurgeryTeamMember[]
}

export interface BookingFilter {
  status?: SurgeryStatus
  theatreId?: number
  fromDate?: string
  toDate?: string
}

export interface OtRegisterRow {
  bookingId: number
  bookingNumber: string
  patientName: string
  procedureName: string
  theatreName: string
  surgeonName: string
  anaesthetistName: string | null
  scheduledStart: string
  actualStart: string | null
  actualEnd: string | null
  status: SurgeryStatus
}

// ── Checklist ────────────────────────────────────────────────────────────
export interface Checklist {
  bookingId: number
  patientIdentityConfirmed: boolean
  siteMarked: boolean
  anaesthesiaSafetyCheckComplete: boolean
  pulseOximeterFunctioning: boolean
  knownAllergy: boolean
  difficultAirwayRisk: boolean
  riskOfBloodLoss: boolean
  signInCompletedAt: string | null
  teamIntroduced: boolean
  patientSiteProcedureConfirmed: boolean
  criticalEventsReviewed: boolean
  antibioticProphylaxisGiven: boolean
  imagingDisplayed: boolean
  timeOutCompletedAt: string | null
  procedureNameRecorded: boolean
  instrumentSpongeNeedleCountCorrect: boolean
  specimenLabeled: boolean
  equipmentProblemsAddressed: boolean
  recoveryConcernsReviewed: boolean
  signOutCompletedAt: string | null
}

export interface UpdateSignInPayload {
  patientIdentityConfirmed: boolean
  siteMarked: boolean
  anaesthesiaSafetyCheckComplete: boolean
  pulseOximeterFunctioning: boolean
  knownAllergy: boolean
  difficultAirwayRisk: boolean
  riskOfBloodLoss: boolean
  complete: boolean
}
export interface UpdateTimeOutPayload {
  teamIntroduced: boolean
  patientSiteProcedureConfirmed: boolean
  criticalEventsReviewed: boolean
  antibioticProphylaxisGiven: boolean
  imagingDisplayed: boolean
  complete: boolean
}
export interface UpdateSignOutPayload {
  procedureNameRecorded: boolean
  instrumentSpongeNeedleCountCorrect: boolean
  specimenLabeled: boolean
  equipmentProblemsAddressed: boolean
  recoveryConcernsReviewed: boolean
  complete: boolean
}

// ── Anaesthesia / operative / post-op ───────────────────────────────────
export interface PreAnaesthesia {
  id: number
  bookingId: number
  assessedByDoctorId: number
  assessedByDoctorName: string
  assessedAt: string
  asaGrade: string
  airwayAssessment: string | null
  comorbidities: string | null
  investigations: string | null
  anaesthesiaPlan: string | null
  notes: string | null
}
export interface SavePreAnaesthesiaPayload {
  assessedByDoctorId: number
  asaGrade: string
  airwayAssessment: string | null
  comorbidities: string | null
  investigations: string | null
  anaesthesiaPlan: string | null
  notes: string | null
}

export interface IntraOp {
  id: number
  bookingId: number
  anaesthesiaType: AnaesthesiaType
  administeredByDoctorId: number
  administeredByDoctorName: string
  startedAt: string
  endedAt: string | null
  notes: string | null
}
export interface SaveIntraOpPayload {
  anaesthesiaType: AnaesthesiaType
  administeredByDoctorId: number
  startedAt: string
  endedAt: string | null
  notes: string | null
}

export interface OperativeNote {
  id: number
  bookingId: number
  surgeonId: number
  surgeonName: string
  procedurePerformed: string
  findings: string | null
  complications: string | null
  startedAt: string
  endedAt: string | null
  notes: string | null
}
export interface SaveOperativeNotePayload {
  surgeonId: number
  procedurePerformed: string
  findings: string | null
  complications: string | null
  startedAt: string
  endedAt: string | null
  notes: string | null
}

export interface PostOpNote {
  id: number
  bookingId: number
  recoveryNotes: string | null
  vitalsSummary: string | null
  complications: string | null
  shiftedAt: string | null
  notes: string | null
}
export interface SavePostOpNotePayload {
  recoveryNotes: string | null
  vitalsSummary: string | null
  complications: string | null
  shiftedAt: string | null
  notes: string | null
}

// ── Usage ────────────────────────────────────────────────────────────────
export interface ImplantUsage {
  id: number
  bookingId: number
  batchId: number
  productName: string
  serialNumber: string | null
  quantity: number
  rate: number
}
export interface RecordImplantUsagePayload {
  batchId: number
  serialNumber: string | null
  quantity: number
}

export interface ConsumableUsage {
  id: number
  bookingId: number
  batchId: number
  productName: string
  quantity: number
  rate: number
}
export interface RecordConsumableUsagePayload {
  batchId: number
  quantity: number
}
