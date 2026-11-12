/** Mirrors backend Prixo.Domain.Enums.VisitType (shared with OPD's OpVisit table). */
export const MHC_VISIT_TYPES = { New: 1, Review: 2 } as const
export type MhcVisitType = (typeof MHC_VISIT_TYPES)[keyof typeof MHC_VISIT_TYPES]
export const MHC_VISIT_TYPE_LABELS: Record<MhcVisitType, string> = { 1: 'New', 2: 'Review' }

/** Mirrors backend Prixo.Domain.Enums.OpVisitStatus (shared with OPD's OpVisit table). */
export const MHC_VISIT_STATUSES = { Waiting: 1, InConsult: 2, Completed: 3, Cancelled: 4 } as const
export type MhcVisitStatus = (typeof MHC_VISIT_STATUSES)[keyof typeof MHC_VISIT_STATUSES]
export const MHC_VISIT_STATUS_LABELS: Record<MhcVisitStatus, string> = {
  1: 'Waiting',
  2: 'In consult',
  3: 'Completed',
  4: 'Cancelled',
}

export interface RegisterMhcVisitPayload {
  patientId: number
  mhcDoctorId: number
  mhcPackageId: number
}

export interface BulkImportResult {
  inserted: number
  skipped: number
  errors: string[]
}

/** DoctorId/DoctorName hold the MHC panel doctor's id/name — the backend reuses OpVisitListItemDto. */
export interface MhcVisitListItem {
  id: number
  visitNumber: string
  tokenNumber: number
  patientId: number
  patientUhid: string
  patientName: string
  doctorId: number
  doctorName: string
  visitDate: string
  visitType: MhcVisitType
  status: MhcVisitStatus
  alreadyAdmitted: boolean
}

/** DoctorId/DoctorName hold the MHC panel doctor's id/name — the backend reuses OpVisitDetailDto. */
export interface MhcVisitDetail {
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
  visitType: MhcVisitType
  status: MhcVisitStatus
  consultationFee: number
  inConsultAt: string | null
  completedAt: string | null
  cancelReason: string | null
  cancelledAt: string | null
}

// ── MHC Reports ──────────────────────────────────────────────────────────
export interface MhcReportColumn {
  key: string
  label: string
}
export interface MhcReportResult {
  reportName: string
  columns: MhcReportColumn[]
  rows: Record<string, unknown>[]
}
