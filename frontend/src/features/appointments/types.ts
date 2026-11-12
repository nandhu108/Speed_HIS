/** Mirrors backend Prixo.Domain.Enums.AppointmentStatus. */
export const APPOINTMENT_STATUSES = {
  Scheduled: 1,
  CheckedIn: 2,
  Completed: 3,
  Cancelled: 4,
  NoShow: 5,
} as const
export type AppointmentStatus = (typeof APPOINTMENT_STATUSES)[keyof typeof APPOINTMENT_STATUSES]
export const APPOINTMENT_STATUS_LABELS: Record<AppointmentStatus, string> = {
  1: 'Scheduled',
  2: 'Checked in',
  3: 'Completed',
  4: 'Cancelled',
  5: 'No-show',
}

/** Mirrors backend Prixo.Domain.Enums.AppointmentBookingType. */
export const BOOKING_TYPES = { Booked: 1, WalkIn: 2 } as const
export type BookingType = (typeof BOOKING_TYPES)[keyof typeof BOOKING_TYPES]
export const BOOKING_TYPE_LABELS: Record<BookingType, string> = { 1: 'Booked', 2: 'Walk-in' }

/** Mirrors backend Prixo.Domain.Enums.ConsultationMode. */
export const CONSULTATION_MODES = { InPerson: 1, TeleConsult: 2 } as const
export type ConsultationMode = (typeof CONSULTATION_MODES)[keyof typeof CONSULTATION_MODES]
export const CONSULTATION_MODE_LABELS: Record<ConsultationMode, string> = { 1: 'In-person', 2: 'Tele-consult' }

export const DAYS_OF_WEEK = [
  { value: 0, label: 'Sunday' },
  { value: 1, label: 'Monday' },
  { value: 2, label: 'Tuesday' },
  { value: 3, label: 'Wednesday' },
  { value: 4, label: 'Thursday' },
  { value: 5, label: 'Friday' },
  { value: 6, label: 'Saturday' },
] as const

export interface DoctorScheduleBlock {
  id: number
  dayOfWeek: number
  startTime: string
  endTime: string
  slotMinutes: number
}

export interface SaveDoctorScheduleBlock {
  dayOfWeek: number
  startTime: string
  endTime: string
  slotMinutes: number
}

export interface ScheduleOverrideEntry {
  id: number
  date: string
  isAvailable: boolean
  startTime: string | null
  endTime: string | null
  slotMinutes: number | null
  reason: string | null
}

export interface SaveScheduleOverride {
  date: string
  isAvailable: boolean
  startTime: string | null
  endTime: string | null
  slotMinutes: number | null
  reason: string | null
}

export interface DoctorSchedule {
  doctorId: number
  doctorName: string
  blocks: DoctorScheduleBlock[]
  overrides: ScheduleOverrideEntry[]
}

export interface UnitScheduleBlock {
  id: number
  dayOfWeek: number
  startTime: string
  endTime: string
}

export interface SaveUnitScheduleBlock {
  dayOfWeek: number
  startTime: string
  endTime: string
}

export interface UnitSchedule {
  unitId: number
  unitName: string
  blocks: UnitScheduleBlock[]
}

export interface UnitDoctor {
  doctorId: number
  doctorName: string
}

export interface Slot {
  startTime: string
  endTime: string
  isAvailable: boolean
}

export interface AppointmentListItem {
  id: number
  patientId: number
  patientUhid: string
  patientName: string
  doctorId: number
  doctorName: string
  appointmentDate: string
  startTime: string
  endTime: string
  tokenNumber: number
  status: AppointmentStatus
  bookingType: BookingType
  appointmentTypeId: number | null
  appointmentTypeName: string | null
  skippedAt: string | null
  emergencyVisitId: number | null
  consultationMode: ConsultationMode
  teleconsultLink: string | null
}

export interface AppointmentDetail {
  id: number
  patientId: number
  patientUhid: string
  patientName: string
  patientMobile: string
  doctorId: number
  doctorName: string
  appointmentDate: string
  startTime: string
  endTime: string
  tokenNumber: number
  status: AppointmentStatus
  bookingType: BookingType
  notes: string | null
  cancelReason: string | null
  cancelledAt: string | null
  checkedInAt: string | null
  noShowMarkedAt: string | null
  rescheduledFromId: number | null
  appointmentTypeId: number | null
  appointmentTypeName: string | null
  skippedAt: string | null
  skipCount: number
  transferredFromDoctorId: number | null
  transferredFromDoctorName: string | null
  transferredAt: string | null
  emergencyVisitId: number | null
  consultationMode: ConsultationMode
  teleconsultLink: string | null
}

export interface BookAppointmentPayload {
  patientId: number
  doctorId: number
  appointmentDate: string
  startTime: string
  bookingType: BookingType
  notes: string | null
  appointmentTypeId?: number | null
  consultationMode?: ConsultationMode
  teleconsultLink?: string | null
}

export interface SaveTeleconsultLinkPayload {
  teleconsultLink: string | null
}

export interface RescheduleAppointmentPayload {
  newDate: string
  newStartTime: string
  reason: string
}

export interface TransferTokenPayload {
  newDoctorId: number
  reason: string | null
}

export interface QueueEntry {
  appointmentId: number
  tokenNumber: number
  patientId: number
  patientName: string
  patientUhid: string
  doctorId: number
  doctorName: string
  startTime: string
  status: AppointmentStatus
  skippedAt: string | null
  emergencyVisitId: number | null
  consultationMode: ConsultationMode
}

/** "Doctors On Duty" — a projection of the existing DoctorSchedule/ScheduleOverride data. */
export interface DoctorOnDutyWindow {
  startTime: string
  endTime: string
}
export interface DoctorOnDuty {
  doctorId: number
  doctorName: string
  specialtyName: string | null
  departmentNames: string[]
  isOnDuty: boolean
  windows: DoctorOnDutyWindow[]
  leaveReason: string | null
}

// ── Doctor-Appointment Report ────────────────────────────────────────────
export interface AppointmentReportColumn {
  key: string
  label: string
}
export interface AppointmentReportResult {
  reportName: string
  columns: AppointmentReportColumn[]
  rows: Record<string, unknown>[]
}
