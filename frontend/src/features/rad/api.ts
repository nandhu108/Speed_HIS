import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse, PagedQuery, PagedResult } from '../../api/types/api'
import type {
  CaptureContrastConsentPayload,
  CaptureWorklistItem,
  CreateRadOrderPayload,
  EnterReportPayload,
  RadOrderDetail,
  RadOrderItemStatus,
  RadOrderListItem,
  RadOrderStatus,
  RadReport,
  RadReportTemplateDetail,
  RadReportTemplateListItem,
  RadTatReportRow,
  RadiologyTestDetail,
  RadiologyTestListItem,
  ReportingWorklistItem,
  SaveRadReportTemplatePayload,
  SaveRadiologyTestPayload,
  ScheduleItemPayload,
  SchedulingWorklistItem,
  UnbilledRadOrderRow,
} from './types'

const r = endpoints.rad

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

// ── Radiology tests ──────────────────────────────────────────────────────
export const useRadiologyTests = () =>
  useQuery({ queryKey: ['rad', 'tests'], queryFn: () => get<RadiologyTestListItem[]>(r.tests) })

export const useRadiologyTest = (id: number | null) =>
  useQuery({
    queryKey: ['rad', 'tests', 'detail', id],
    queryFn: () => get<RadiologyTestDetail>(`${r.tests}/${id}`),
    enabled: id !== null,
  })

export const useSaveRadiologyTest = () =>
  useInvalidating(
    ({ id, ...payload }: SaveRadiologyTestPayload & { id?: number }) =>
      id ? api.put(`${r.tests}/${id}`, payload) : api.post(r.tests, payload),
    [['rad', 'tests']],
  )

export const useDeleteRadiologyTest = () =>
  useInvalidating((id: number) => api.delete(`${r.tests}/${id}`), [['rad', 'tests']])

// ── Report templates ─────────────────────────────────────────────────────
export const useRadReportTemplates = (radiologyTestId?: number) =>
  useQuery({
    queryKey: ['rad', 'templates', radiologyTestId],
    queryFn: () => get<RadReportTemplateListItem[]>(r.templates, { radiologyTestId }),
  })

export const useRadReportTemplate = (id: number | null) =>
  useQuery({
    queryKey: ['rad', 'templates', 'detail', id],
    queryFn: () => get<RadReportTemplateDetail>(`${r.templates}/${id}`),
    enabled: id !== null,
  })

export const useSaveRadReportTemplate = () =>
  useInvalidating(
    ({ id, ...payload }: SaveRadReportTemplatePayload & { id?: number }) =>
      id ? api.put(`${r.templates}/${id}`, payload) : api.post(r.templates, payload),
    [['rad', 'templates']],
  )

export const useDeleteRadReportTemplate = () =>
  useInvalidating((id: number) => api.delete(`${r.templates}/${id}`), [['rad', 'templates']])

// ── Orders ───────────────────────────────────────────────────────────────
export interface RadOrderFilter extends PagedQuery {
  patientId?: number
  status?: RadOrderStatus
}

export const useRadOrders = (query: RadOrderFilter) =>
  useQuery({ queryKey: ['rad', 'orders', 'list', query], queryFn: () => get<PagedResult<RadOrderListItem>>(r.orders, query) })

export const useRadOrder = (id: number | null) =>
  useQuery({
    queryKey: ['rad', 'orders', 'detail', id],
    queryFn: () => get<RadOrderDetail>(`${r.orders}/${id}`),
    enabled: id !== null,
  })

const orderKeys = [['rad', 'orders']]
const orderDetailKeys = (id: number) => [
  ['rad', 'orders', 'detail', String(id)],
  ['rad', 'orders'],
  ['rad', 'scheduling'],
  ['rad', 'capture'],
  ['rad', 'worklist'],
  ['rad', 'reports', 'queue'],
]

export const useCreateRadOrder = () =>
  useInvalidating(async (payload: CreateRadOrderPayload) => {
    const { data } = await api.post<ApiResponse<RadOrderDetail>>(r.orders, payload)
    return data.data!
  }, orderKeys)

export const useCancelRadOrder = (orderId: number) =>
  useInvalidating((reason: string) => api.post(`${r.orders}/${orderId}/cancel`, { reason }), orderDetailKeys(orderId))

export const useCancelRadOrderItem = (orderId: number) =>
  useInvalidating((itemId: number) => api.post(`${r.orders}/${orderId}/items/${itemId}/cancel`), orderDetailKeys(orderId))

export const useRadTatReport = (fromDate: string, toDate: string, enabled: boolean) =>
  useQuery({
    queryKey: ['rad', 'tat-report', fromDate, toDate],
    queryFn: () => get<RadTatReportRow[]>(`${r.orders}/tat-report`, { fromDate, toDate }),
    enabled,
  })

export const useUnbilledRadOrdersReport = (fromDate: string, toDate: string, enabled: boolean) =>
  useQuery({
    queryKey: ['rad', 'unbilled-report', fromDate, toDate],
    queryFn: () => get<UnbilledRadOrderRow[]>(`${r.orders}/unbilled-report`, { fromDate, toDate }),
    enabled,
  })

// ── Scheduling ───────────────────────────────────────────────────────────
export const useSchedulingWorklist = (modalityId?: number) =>
  useQuery({
    queryKey: ['rad', 'scheduling', modalityId],
    queryFn: () => get<SchedulingWorklistItem[]>(`${r.scheduling}/worklist`, { modalityId }),
  })

const schedulingKeys = [['rad', 'scheduling'], ['rad', 'capture'], ['rad', 'orders']]

export const useScheduleItem = () =>
  useInvalidating(
    ({ itemId, ...payload }: ScheduleItemPayload & { itemId: number }) =>
      api.post(`${r.scheduling}/order-items/${itemId}`, payload),
    schedulingKeys,
  )

// ── Image capture ────────────────────────────────────────────────────────
export const useCaptureWorklist = (modalityId?: number) =>
  useQuery({
    queryKey: ['rad', 'capture', modalityId],
    queryFn: () => get<CaptureWorklistItem[]>(`${r.capture}/worklist`, { modalityId }),
  })

const captureKeys = [['rad', 'capture'], ['rad', 'worklist'], ['rad', 'orders']]

export const useMarkImageCaptured = () =>
  useInvalidating((itemId: number) => api.post(`${r.capture}/order-items/${itemId}`), captureKeys)

export const useCaptureContrastConsent = () =>
  useInvalidating(
    ({ itemId, ...payload }: CaptureContrastConsentPayload & { itemId: number }) =>
      api.post(`${r.orderItems}/${itemId}/contrast-consent`, payload),
    [['rad', 'capture'], ['rad', 'orders']],
  )

// ── Reporting ────────────────────────────────────────────────────────────
export const useReportingEntryQueue = () =>
  useQuery({ queryKey: ['rad', 'worklist'], queryFn: () => get<ReportingWorklistItem[]>(r.worklist) })

export const useReportingReviewQueue = (status: RadOrderItemStatus) =>
  useQuery({
    queryKey: ['rad', 'reports', 'queue', status],
    queryFn: () => get<ReportingWorklistItem[]>(r.reportsQueue, { status }),
  })

export const useRadReport = (itemId: number | null) =>
  useQuery({
    queryKey: ['rad', 'order-items', itemId, 'report'],
    queryFn: () => get<RadReport>(`${r.orderItems}/${itemId}/report`),
    enabled: itemId !== null,
  })

const reportKeys = [['rad', 'worklist'], ['rad', 'reports', 'queue'], ['rad', 'orders']]

export const useEnterReport = () =>
  useInvalidating(
    ({ itemId, ...payload }: EnterReportPayload & { itemId: number }) => api.post(`${r.orderItems}/${itemId}/report`, payload),
    reportKeys,
  )

export const useVerifyReport = () =>
  useInvalidating((itemId: number) => api.post(`${r.orderItems}/${itemId}/report/verify`), reportKeys)

export const useApproveReport = () =>
  useInvalidating((itemId: number) => api.post(`${r.orderItems}/${itemId}/report/approve`), reportKeys)

export async function openRadReportPdf(itemId: number) {
  const { data } = await api.get(`${r.orderItems}/${itemId}/report/pdf`, { responseType: 'blob' })
  const url = URL.createObjectURL(data as Blob)
  window.open(url, '_blank', 'noopener,noreferrer')
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}
