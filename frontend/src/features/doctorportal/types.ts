export interface DoctorDashboard {
  todayOpCount: number
  activeInpatients: number
  pendingDischargeSigns: number
  unacknowledgedCriticalAlerts: number
}

export interface PendingDischargeSummary {
  admissionId: number
  admissionNumber: string
  patientId: number
  patientUhid: string
  patientName: string
  finalDiagnosis: string
}

/** "Doctor Opinion Entry" inbox row — an Internal cross-consult referral addressed to me. */
export interface OpinionRequest {
  referralId: number
  visitId: number
  visitNumber: string
  visitDate: string
  patientId: number
  patientUhid: string
  patientName: string
  referringDoctorName: string
  reason: string
  opinionNotes: string | null
  opinionEnteredAt: string | null
}
