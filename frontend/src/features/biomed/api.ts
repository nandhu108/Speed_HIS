import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse } from '../../api/types/api'
import type {
  AmcContractDto,
  BreakdownComplaintDto,
  CalibrationDto,
  ComplaintStatus,
  DowntimeReportRow,
  EquipmentCriticality,
  EquipmentDetail,
  EquipmentListItem,
  EquipmentLocationLogDto,
  EquipmentStatus,
  PmComplianceRow,
  PmScheduleDto,
  SaveAmcContractPayload,
  SaveEquipmentPayload,
  SavePmSchedulePayload,
  ServiceHistoryDto,
  WorkOrderDto,
  WorkOrderStatus,
  WorkOrderType,
} from './types'

const p = endpoints.biomed

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

// ── Equipment ────────────────────────────────────────────────────────────────
export const useEquipmentList = (filter: { status?: EquipmentStatus; criticality?: EquipmentCriticality; search?: string }) =>
  useQuery({ queryKey: ['biomed', 'equipment', filter], queryFn: () => get<EquipmentListItem[]>(p.equipment, filter) })

export const useEquipmentDetail = (id: number | null) =>
  useQuery({
    queryKey: ['biomed', 'equipment', 'detail', id],
    queryFn: () => get<EquipmentDetail>(`${p.equipment}/${id}`),
    enabled: id !== null,
  })

export const useSaveEquipment = () =>
  useInvalidating(async ({ id, ...payload }: SaveEquipmentPayload & { id?: number }) => {
    if (id) {
      await api.put(`${p.equipment}/${id}`, payload)
      return id
    }
    const { data } = await api.post<ApiResponse<number>>(p.equipment, payload)
    return data.data!
  }, [['biomed', 'equipment']])

export const useEquipmentLocationHistory = (id: number | null) =>
  useQuery({
    queryKey: ['biomed', 'equipment', 'locations', id],
    queryFn: () => get<EquipmentLocationLogDto[]>(`${p.equipment}/${id}/locations`),
    enabled: id !== null,
  })

export const useRecordEquipmentMovement = (id: number) =>
  useInvalidating(
    (payload: { toLocation: string; reason: string | null }) => api.post(`${p.equipment}/${id}/locations`, payload),
    [['biomed', 'equipment', 'locations', String(id)], ['biomed', 'equipment', 'detail', String(id)], ['biomed', 'equipment']],
  )

// ── AMC/CMC contracts ─────────────────────────────────────────────────────────
export const useAmcContracts = (equipmentId?: number, renewalDueOnly?: boolean) =>
  useQuery({
    queryKey: ['biomed', 'contracts', equipmentId, renewalDueOnly],
    queryFn: () => get<AmcContractDto[]>(p.contracts, { equipmentId, renewalDueOnly }),
  })

export const useCreateAmcContract = () => useInvalidating((payload: SaveAmcContractPayload) => api.post(p.contracts, payload), [['biomed', 'contracts']])

// ── PM schedules ──────────────────────────────────────────────────────────────
export const usePmSchedules = (dueOnly: boolean) =>
  useQuery({ queryKey: ['biomed', 'pm-schedules', dueOnly], queryFn: () => get<PmScheduleDto[]>(p.pmSchedules, { dueOnly }) })

export const useCreatePmSchedule = () => useInvalidating((payload: SavePmSchedulePayload) => api.post(p.pmSchedules, payload), [['biomed', 'pm-schedules']])

export const useGenerateDueWorkOrders = () =>
  useInvalidating(async () => {
    const { data } = await api.post<ApiResponse<number>>(`${p.pmSchedules}/generate-due`)
    return data.data!
  }, [['biomed', 'pm-schedules'], ['biomed', 'work-orders']])

// ── Work orders ───────────────────────────────────────────────────────────────
export const useWorkOrders = (filter: { status?: WorkOrderStatus; workOrderType?: WorkOrderType; equipmentId?: number }) =>
  useQuery({ queryKey: ['biomed', 'work-orders', filter], queryFn: () => get<WorkOrderDto[]>(p.workOrders, filter) })

export const useWorkOrder = (id: number | null) =>
  useQuery({
    queryKey: ['biomed', 'work-orders', 'detail', id],
    queryFn: () => get<WorkOrderDto>(`${p.workOrders}/${id}`),
    enabled: id !== null,
  })

const workOrderKeys = [['biomed', 'work-orders'], ['biomed', 'complaints']]

export const useCreateWorkOrderFromComplaint = () =>
  useInvalidating((payload: { breakdownComplaintId: number; scheduledDate: string; notes: string | null }) => api.post(`${p.workOrders}/from-complaint`, payload), workOrderKeys)

export const useAssignWorkOrder = () =>
  useInvalidating(({ id, assignedToUserId }: { id: number; assignedToUserId: number }) => api.post(`${p.workOrders}/${id}/assign`, { assignedToUserId }), workOrderKeys)

export const useCompleteWorkOrder = () =>
  useInvalidating(({ id, notes }: { id: number; notes: string | null }) => api.post(`${p.workOrders}/${id}/complete`, { notes }), workOrderKeys)

export const useCancelWorkOrder = () =>
  useInvalidating(({ id, reason }: { id: number; reason: string }) => api.post(`${p.workOrders}/${id}/cancel`, { reason }), workOrderKeys)

export const useRecordSpareUsage = (id: number) =>
  useInvalidating(
    (payload: { batchId: number; quantity: number }) => api.post(`${p.workOrders}/${id}/spare-usage`, payload),
    [['biomed', 'work-orders', 'detail', String(id)], ['biomed', 'work-orders']],
  )

// ── Breakdown complaints ──────────────────────────────────────────────────────
export const useComplaints = (filter: { status?: ComplaintStatus; equipmentId?: number }) =>
  useQuery({ queryKey: ['biomed', 'complaints', filter], queryFn: () => get<BreakdownComplaintDto[]>(p.complaints, filter) })

const complaintKeys = [['biomed', 'complaints']]

export const useRaiseComplaint = () => useInvalidating((payload: { equipmentId: number; description: string }) => api.post(p.complaints, payload), complaintKeys)
export const useAssignComplaint = () =>
  useInvalidating(({ id, assignedToUserId }: { id: number; assignedToUserId: number }) => api.post(`${p.complaints}/${id}/assign`, { assignedToUserId }), complaintKeys)
export const useResolveComplaint = () =>
  useInvalidating(({ id, resolution }: { id: number; resolution: string }) => api.post(`${p.complaints}/${id}/resolve`, { resolution }), complaintKeys)
export const useCloseComplaint = () => useInvalidating((id: number) => api.post(`${p.complaints}/${id}/close`), complaintKeys)

// ── Calibrations ──────────────────────────────────────────────────────────────
export const useCalibrations = (equipmentId?: number, dueOnly?: boolean) =>
  useQuery({ queryKey: ['biomed', 'calibrations', equipmentId, dueOnly], queryFn: () => get<CalibrationDto[]>(p.calibrations, { equipmentId, dueOnly }) })

export const useCreateCalibration = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (payload: {
      equipmentId: number; calibrationDate: string; nextDueDate: string; performedBy: string | null;
      certificateNumber: string | null; notes: string | null; certificate: File | null
    }) => {
      const form = new FormData()
      form.append('equipmentId', String(payload.equipmentId))
      form.append('calibrationDate', payload.calibrationDate)
      form.append('nextDueDate', payload.nextDueDate)
      if (payload.performedBy) form.append('performedBy', payload.performedBy)
      if (payload.certificateNumber) form.append('certificateNumber', payload.certificateNumber)
      if (payload.notes) form.append('notes', payload.notes)
      if (payload.certificate) form.append('certificate', payload.certificate)
      const { data } = await api.post<ApiResponse<number>>(p.calibrations, form, { headers: { 'Content-Type': 'multipart/form-data' } })
      return data.data!
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['biomed', 'calibrations'] }),
  })
}

export const calibrationCertificateUrl = (id: number) => `${p.calibrations}/${id}/certificate`

// ── Service history ───────────────────────────────────────────────────────────
export const useServiceHistory = (equipmentId: number | null) =>
  useQuery({
    queryKey: ['biomed', 'service-history', equipmentId],
    queryFn: () => get<ServiceHistoryDto[]>(`${p.equipment}/${equipmentId}/service-history`),
    enabled: equipmentId !== null,
  })

export const useLogServiceHistory = (equipmentId: number) =>
  useInvalidating(
    (payload: { serviceDate: string; serviceType: string; performedBy: string | null; cost: number | null; notes: string | null }) =>
      api.post(`${p.equipment}/${equipmentId}/service-history`, { equipmentId, ...payload }),
    [['biomed', 'service-history', String(equipmentId)]],
  )

// ── Reports ───────────────────────────────────────────────────────────────────
export const useDowntimeReport = (range: { fromDate?: string; toDate?: string }) =>
  useQuery({ queryKey: ['biomed', 'reports', 'downtime', range], queryFn: () => get<DowntimeReportRow[]>(`${p.reports}/downtime`, range) })

export const usePmComplianceReport = (range: { fromDate?: string; toDate?: string }) =>
  useQuery({ queryKey: ['biomed', 'reports', 'pm-compliance', range], queryFn: () => get<PmComplianceRow[]>(`${p.reports}/pm-compliance`, range) })
