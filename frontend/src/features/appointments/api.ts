import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse, PagedQuery, PagedResult } from '../../api/types/api'
import { downloadFile } from '../../lib/downloadFile'
import type {
  AppointmentDetail,
  AppointmentListItem,
  AppointmentReportResult,
  AppointmentStatus,
  BookAppointmentPayload,
  DoctorOnDuty,
  DoctorSchedule,
  QueueEntry,
  RescheduleAppointmentPayload,
  SaveDoctorScheduleBlock,
  SaveScheduleOverride,
  SaveTeleconsultLinkPayload,
  SaveUnitScheduleBlock,
  Slot,
  TransferTokenPayload,
  UnitDoctor,
  UnitSchedule,
} from './types'

async function get<T>(url: string, params?: object): Promise<T> {
  const { data } = await api.get<ApiResponse<T>>(url, { params })
  return data.data!
}

function useInvalidating<TArgs, TResult = unknown>(fn: (args: TArgs) => Promise<TResult>, keys: string[][]) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () => keys.forEach((key) => queryClient.invalidateQueries({ queryKey: key })),
  })
}

// ── Doctor schedule ──────────────────────────────────────────────────────
export const useDoctorSchedule = (doctorId: number | null) =>
  useQuery({
    queryKey: ['appointments', 'schedule', doctorId],
    queryFn: () => get<DoctorSchedule>(endpoints.doctorSchedule(doctorId!)),
    enabled: doctorId !== null,
  })

export const useReplaceScheduleBlocks = (doctorId: number) =>
  useInvalidating(
    (blocks: SaveDoctorScheduleBlock[]) => api.put(`${endpoints.doctorSchedule(doctorId)}/blocks`, { blocks }),
    [['appointments', 'schedule', String(doctorId)]],
  )

export const useAddScheduleOverride = (doctorId: number) =>
  useInvalidating(
    (payload: SaveScheduleOverride) => api.post(`${endpoints.doctorSchedule(doctorId)}/overrides`, payload),
    [['appointments', 'schedule', String(doctorId)]],
  )

export const useDeleteScheduleOverride = (doctorId: number) =>
  useInvalidating(
    (overrideId: number) => api.delete(`${endpoints.doctorSchedule(doctorId)}/overrides/${overrideId}`),
    [['appointments', 'schedule', String(doctorId)]],
  )

// ── Unit scheduling ("Schedule Unit - Day Wise") ────────────────────────
export const useUnitSchedule = (unitId: number | null) =>
  useQuery({
    queryKey: ['appointments', 'unit-schedule', unitId],
    queryFn: () => get<UnitSchedule>(endpoints.unitSchedule(unitId!)),
    enabled: unitId !== null,
  })

export const useReplaceUnitScheduleBlocks = (unitId: number) =>
  useInvalidating(
    (blocks: SaveUnitScheduleBlock[]) => api.put(`${endpoints.unitSchedule(unitId)}/blocks`, { blocks }),
    [['appointments', 'unit-schedule', String(unitId)]],
  )

// ── Unit-Doctor link ("Link Unit & Doctors") ────────────────────────────
export const useUnitDoctors = (unitId: number | null) =>
  useQuery({
    queryKey: ['appointments', 'unit-doctors', unitId],
    queryFn: () => get<UnitDoctor[]>(endpoints.unitDoctors(unitId!)),
    enabled: unitId !== null,
  })

export const useReplaceUnitDoctors = (unitId: number) =>
  useInvalidating(
    (doctorIds: number[]) => api.put(endpoints.unitDoctors(unitId), { doctorIds }),
    [['appointments', 'unit-doctors', String(unitId)]],
  )

// ── Slots & booking ──────────────────────────────────────────────────────
export const useSlots = (doctorId: number | null, date: string | null) =>
  useQuery({
    queryKey: ['appointments', 'slots', doctorId, date],
    queryFn: () => get<Slot[]>(endpoints.appointmentSlots, { doctorId, date }),
    enabled: doctorId !== null && !!date,
  })

export interface AppointmentFilter extends PagedQuery {
  doctorId?: number
  patientId?: number
  date?: string
  status?: AppointmentStatus
  emergencyVisitId?: number
}

export const useAppointments = (query: AppointmentFilter) =>
  useQuery({
    queryKey: ['appointments', 'list', query],
    queryFn: () => get<PagedResult<AppointmentListItem>>(endpoints.appointments, query),
  })

export const useAppointment = (id: number | null) =>
  useQuery({
    queryKey: ['appointments', 'detail', id],
    queryFn: () => get<AppointmentDetail>(`${endpoints.appointments}/${id}`),
    enabled: id !== null,
  })

/** Looks up the OPD token already mapped for an ER visit, if "Map ER Token" was used before. */
export const useAppointmentForEmergencyVisit = (emergencyVisitId: number | null) =>
  useQuery({
    queryKey: ['appointments', 'list', 'by-emergency-visit', emergencyVisitId],
    queryFn: () =>
      get<PagedResult<AppointmentListItem>>(endpoints.appointments, { emergencyVisitId, page: 1, pageSize: 1 }),
    enabled: emergencyVisitId !== null,
    select: (result) => result.items.find((a) => a.status !== 4) ?? null,
  })

export const useBookAppointment = () =>
  useInvalidating(async (payload: BookAppointmentPayload) => {
    const { data } = await api.post<ApiResponse<AppointmentDetail>>(endpoints.appointments, payload)
    return data.data!
  }, [['appointments', 'list'], ['appointments', 'slots'], ['appointments', 'queue']])

export const useRescheduleAppointment = () =>
  useInvalidating(async ({ id, ...payload }: RescheduleAppointmentPayload & { id: number }) => {
    const { data } = await api.post<ApiResponse<AppointmentDetail>>(`${endpoints.appointments}/${id}/reschedule`, payload)
    return data.data!
  }, [['appointments', 'list'], ['appointments', 'slots'], ['appointments', 'queue']])

export const useCancelAppointment = () =>
  useInvalidating(
    ({ id, reason }: { id: number; reason: string }) => api.post(`${endpoints.appointments}/${id}/cancel`, { reason }),
    [['appointments', 'list'], ['appointments', 'slots'], ['appointments', 'queue']],
  )

export const useCheckInAppointment = () =>
  useInvalidating(
    (id: number) => api.post(`${endpoints.appointments}/${id}/check-in`),
    [['appointments', 'list'], ['appointments', 'queue']],
  )

// ── Token management ("Skip Token" / "Token Transfer") ──────────────────
export const useSkipToken = () =>
  useInvalidating(
    (id: number) => api.post(`${endpoints.appointments}/${id}/skip`),
    [['appointments', 'list'], ['appointments', 'queue']],
  )

export const useRecallToken = () =>
  useInvalidating(
    (id: number) => api.post(`${endpoints.appointments}/${id}/recall`),
    [['appointments', 'list'], ['appointments', 'queue']],
  )

export const useTransferToken = () =>
  useInvalidating(async ({ id, ...payload }: TransferTokenPayload & { id: number }) => {
    const { data } = await api.post<ApiResponse<AppointmentDetail>>(`${endpoints.appointments}/${id}/transfer`, payload)
    return data.data!
  }, [['appointments', 'list'], ['appointments', 'slots'], ['appointments', 'queue']])

// ── ER Token Mapping (mints an OPD token for an ER visit's assigned doctor) ──
export const useMapEmergencyToken = () =>
  useInvalidating(async (emergencyVisitId: number) => {
    const { data } = await api.post<ApiResponse<AppointmentDetail>>(endpoints.appointmentMapEmergencyToken(emergencyVisitId))
    return data.data!
  }, [['appointments', 'list'], ['appointments', 'queue'], ['er', 'queue']])

// ── Tele-consulting ──────────────────────────────────────────────────────
export const useSetTeleconsultLink = () =>
  useInvalidating(
    ({ id, ...payload }: SaveTeleconsultLinkPayload & { id: number }) =>
      api.put(`${endpoints.appointments}/${id}/teleconsult-link`, payload),
    [['appointments', 'list'], ['appointments', 'detail']],
  )

// ── Doctors On Duty ──────────────────────────────────────────────────────
export const useDoctorsOnDuty = (date: string, departmentId: number | null) =>
  useQuery({
    queryKey: ['appointments', 'doctors-on-duty', date, departmentId],
    queryFn: () => get<DoctorOnDuty[]>(endpoints.doctorsOnDuty, { date, departmentId }),
  })

// ── Queue display ────────────────────────────────────────────────────────
export const useQueueDisplay = (doctorId: number | null, deptId: number | null, date: string) =>
  useQuery({
    queryKey: ['appointments', 'queue', doctorId, deptId, date],
    queryFn: () => get<QueueEntry[]>(endpoints.queueDisplay, { doctorId, deptId, date }),
    refetchInterval: 15_000,
  })

// ── Doctor-Appointment Report ────────────────────────────────────────────
export interface DoctorAppointmentReportFilter {
  fromDate?: string | null
  toDate?: string | null
  mode: 'summary' | 'detail'
  doctorId?: number | null
  departmentId?: number | null
  unitId?: number | null
  status?: AppointmentStatus | null
  regNo?: string | null
  patientName?: string | null
  mobile?: string | null
}

export const useDoctorAppointmentReport = (filter: DoctorAppointmentReportFilter, enabled: boolean) =>
  useQuery({
    queryKey: ['appointments', 'report', 'doctor-appointment', filter],
    queryFn: () => get<AppointmentReportResult>(endpoints.appointmentReports.doctorAppointment, filter),
    enabled,
  })

export async function downloadDoctorAppointmentReport(filter: DoctorAppointmentReportFilter, format: 'csv' | 'excel' | 'pdf') {
  const params = new URLSearchParams({ mode: filter.mode, format })
  if (filter.fromDate) params.set('fromDate', filter.fromDate)
  if (filter.toDate) params.set('toDate', filter.toDate)
  if (filter.doctorId) params.set('doctorId', String(filter.doctorId))
  if (filter.departmentId) params.set('departmentId', String(filter.departmentId))
  if (filter.unitId) params.set('unitId', String(filter.unitId))
  if (filter.status) params.set('status', String(filter.status))
  if (filter.regNo) params.set('regNo', filter.regNo)
  if (filter.patientName) params.set('patientName', filter.patientName)
  if (filter.mobile) params.set('mobile', filter.mobile)
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`${endpoints.appointmentReports.doctorAppointment}/export?${params}`, `doctor-appointment-report.${extension}`)
}
