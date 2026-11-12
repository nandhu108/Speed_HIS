export type EmergencyStatus = 1 | 2 | 3 | 4 | 5 // Registered, Triaged, UnderTreatment, Disposed, Cancelled
export type EsiLevel = 1 | 2 | 3 | 4 | 5 // Resuscitation, Emergent, Urgent, LessUrgent, NonUrgent
export type ArrivalMode = 1 | 2 | 3 | 4 | 5 // WalkIn, Ambulance, PoliceBrought, Referred, Other
export type DispositionType = 1 | 2 | 3 | 4 | 5 | 6 // Discharged, AdmittedConverted, TransferredOut, Lama, BroughtDead, Observation

export const EMERGENCY_STATUS_LABELS: Record<EmergencyStatus, string> = {
  1: 'Registered', 2: 'Triaged', 3: 'Under Treatment', 4: 'Disposed', 5: 'Cancelled',
}
export const ESI_LEVEL_LABELS: Record<EsiLevel, string> = {
  1: 'ESI 1 — Resuscitation', 2: 'ESI 2 — Emergent', 3: 'ESI 3 — Urgent', 4: 'ESI 4 — Less Urgent', 5: 'ESI 5 — Non-Urgent',
}
export const ARRIVAL_MODE_LABELS: Record<ArrivalMode, string> = {
  1: 'Walk-in', 2: 'Ambulance', 3: 'Police Brought', 4: 'Referred', 5: 'Other',
}
export const DISPOSITION_LABELS: Record<DispositionType, string> = {
  1: 'Discharged', 2: 'Admitted (Converted)', 3: 'Transferred Out', 4: 'LAMA', 5: 'Brought Dead', 6: 'Observation',
}

// ── Visits / queue ───────────────────────────────────────────────────────
export interface EmergencyVisitListItem {
  id: number
  visitNumber: string
  tokenNumber: number
  patientId: number
  patientName: string
  patientUhid: string
  arrivedAt: string
  arrivalMode: ArrivalMode
  status: EmergencyStatus
  esiLevel: EsiLevel | null
  assignedDoctorName: string | null
  isMlc: boolean
  disposition: DispositionType | null
  alreadyAdmitted: boolean
}

export interface TriageDto {
  id: number
  triagedAt: string
  triagedByName: string
  esiLevel: EsiLevel
  chiefComplaint: string
  temperatureF: number | null
  pulseRate: number | null
  respiratoryRate: number | null
  bloodPressureSystolic: number | null
  bloodPressureDiastolic: number | null
  spO2: number | null
  notes: string | null
}

export interface EmergencyVisitDetail {
  id: number
  visitNumber: string
  tokenNumber: number
  patientId: number
  patientName: string
  patientUhid: string
  arrivedAt: string
  arrivalMode: ArrivalMode
  isMlc: boolean
  status: EmergencyStatus
  assignedDoctorId: number | null
  assignedDoctorName: string | null
  cancelReason: string | null
  triage: TriageDto | null
}

export interface RegisterEmergencyVisitPayload {
  patientId: number
  arrivalMode: ArrivalMode
  isMlc: boolean
}

export interface RecordTriagePayload {
  esiLevel: EsiLevel
  chiefComplaint: string
  temperatureF: number | null
  pulseRate: number | null
  respiratoryRate: number | null
  bloodPressureSystolic: number | null
  bloodPressureDiastolic: number | null
  spO2: number | null
  notes: string | null
  assignDoctorId: number | null
}

// ── Assessment / disposition ─────────────────────────────────────────────
export interface AssessmentDto {
  id: number
  emergencyVisitId: number
  assessedByDoctorId: number
  assessedByDoctorName: string
  assessedAt: string
  history: string | null
  examinationFindings: string | null
  provisionalDiagnosis: string | null
  treatmentGiven: string | null
  disposition: DispositionType | null
  dispositionNotes: string | null
  transferredToFacility: string | null
  transferSummary: string | null
}

export interface SaveAssessmentPayload {
  assessedByDoctorId: number
  history: string | null
  examinationFindings: string | null
  provisionalDiagnosis: string | null
  treatmentGiven: string | null
  disposition: DispositionType | null
  dispositionNotes: string | null
  transferredToFacility: string | null
  transferSummary: string | null
}

// ── Observation beds ─────────────────────────────────────────────────────
export interface ObservationAllocationDto {
  id: number
  emergencyVisitId: number
  bedId: number
  wardName: string
  roomName: string
  bedCode: string
  allocatedAt: string
  releasedAt: string | null
}

// ── MLC / accident ───────────────────────────────────────────────────────
export interface MlcRecordDto {
  id: number
  emergencyVisitId: number
  mlcNumber: string
  policeStationName: string | null
  policeInformedAt: string | null
  firNumber: string | null
  broughtByName: string | null
  broughtByDesignation: string | null
  injuryDescription: string | null
  notes: string | null
  recordedByName: string
}

export interface SaveMlcPayload {
  policeStationName: string | null
  policeInformedAt: string | null
  firNumber: string | null
  broughtByName: string | null
  broughtByDesignation: string | null
  injuryDescription: string | null
  notes: string | null
}

export interface AccidentCaseRecordDto {
  id: number
  emergencyVisitId: number
  accidentType: string
  accidentAt: string | null
  accidentLocation: string | null
  vehicleInvolved: boolean
  notes: string | null
  recordedByName: string
}

export interface SaveAccidentPayload {
  accidentType: string
  accidentAt: string | null
  accidentLocation: string | null
  vehicleInvolved: boolean
  notes: string | null
}
