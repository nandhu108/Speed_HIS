export interface PortalSlot {
  startTime: string
  isAvailable: boolean
}

export interface PortalAppointment {
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
  status: number
  bookingType: number
}

export const APPOINTMENT_STATUS_LABELS: Record<number, string> = {
  1: 'Scheduled', 2: 'Checked In', 3: 'Completed', 4: 'Cancelled', 5: 'No Show',
}

export interface BookAppointmentPayload {
  branchId: number
  doctorId: number
  appointmentDate: string
  startTime: string
  notes: string | null
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}
