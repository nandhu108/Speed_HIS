import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse, PagedQuery, PagedResult } from '../../api/types/api'
import type {
  CollectSamplePayload,
  CollectionWorklistItem,
  CreateLabOrderPayload,
  CriticalAlert,
  DepartmentWorklistItem,
  EnterResultPayload,
  LabOrderDetail,
  LabOrderItemStatus,
  LabOrderListItem,
  LabOrderStatus,
  LabResult,
  LabTestDetail,
  LabTestListItem,
  Sample,
  SaveLabTestPayload,
  SaveTestProfilePayload,
  TatReportRow,
  TestProfileDetail,
  TestProfileListItem,
  UnbilledOrderRow,
} from './types'

const l = endpoints.lab

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

// ── Lab tests ────────────────────────────────────────────────────────────
export const useLabTests = () =>
  useQuery({ queryKey: ['lab', 'tests'], queryFn: () => get<LabTestListItem[]>(l.tests) })

export const useLabTest = (id: number | null) =>
  useQuery({
    queryKey: ['lab', 'tests', 'detail', id],
    queryFn: () => get<LabTestDetail>(`${l.tests}/${id}`),
    enabled: id !== null,
  })

export const useSaveLabTest = () =>
  useInvalidating(
    ({ id, ...payload }: SaveLabTestPayload & { id?: number }) =>
      id ? api.put(`${l.tests}/${id}`, payload) : api.post(l.tests, payload),
    [['lab', 'tests']],
  )

export const useDeleteLabTest = () =>
  useInvalidating((id: number) => api.delete(`${l.tests}/${id}`), [['lab', 'tests']])

// ── Test profiles ────────────────────────────────────────────────────────
export const useTestProfiles = () =>
  useQuery({ queryKey: ['lab', 'profiles'], queryFn: () => get<TestProfileListItem[]>(l.profiles) })

export const useTestProfile = (id: number | null) =>
  useQuery({
    queryKey: ['lab', 'profiles', 'detail', id],
    queryFn: () => get<TestProfileDetail>(`${l.profiles}/${id}`),
    enabled: id !== null,
  })

export const useSaveTestProfile = () =>
  useInvalidating(
    ({ id, ...payload }: SaveTestProfilePayload & { id?: number }) =>
      id ? api.put(`${l.profiles}/${id}`, payload) : api.post(l.profiles, payload),
    [['lab', 'profiles']],
  )

export const useDeleteTestProfile = () =>
  useInvalidating((id: number) => api.delete(`${l.profiles}/${id}`), [['lab', 'profiles']])

// ── Orders ───────────────────────────────────────────────────────────────
export interface LabOrderFilter extends PagedQuery {
  patientId?: number
  status?: LabOrderStatus
}

export const useLabOrders = (query: LabOrderFilter) =>
  useQuery({
    queryKey: ['lab', 'orders', 'list', query],
    queryFn: () => get<PagedResult<LabOrderListItem>>(l.orders, query),
  })

export const useLabOrder = (id: number | null) =>
  useQuery({
    queryKey: ['lab', 'orders', 'detail', id],
    queryFn: () => get<LabOrderDetail>(`${l.orders}/${id}`),
    enabled: id !== null,
  })

const orderKeys = [['lab', 'orders']]
const orderDetailKeys = (id: number) => [
  ['lab', 'orders', 'detail', String(id)],
  ['lab', 'orders'],
  ['lab', 'collection'],
  ['lab', 'worklist'],
  ['lab', 'results', 'queue'],
]

export const useCreateLabOrder = () =>
  useInvalidating(async (payload: CreateLabOrderPayload) => {
    const { data } = await api.post<ApiResponse<LabOrderDetail>>(l.orders, payload)
    return data.data!
  }, orderKeys)

export const useCancelLabOrder = (orderId: number) =>
  useInvalidating((reason: string) => api.post(`${l.orders}/${orderId}/cancel`, { reason }), orderDetailKeys(orderId))

export const useCancelLabOrderItem = (orderId: number) =>
  useInvalidating((itemId: number) => api.post(`${l.orders}/${orderId}/items/${itemId}/cancel`), orderDetailKeys(orderId))

export async function openLabReport(orderId: number) {
  const { data } = await api.get(`${l.orders}/${orderId}/report`, { responseType: 'blob' })
  const url = URL.createObjectURL(data as Blob)
  window.open(url, '_blank', 'noopener,noreferrer')
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

export const useTatReport = (fromDate: string, toDate: string, enabled: boolean) =>
  useQuery({
    queryKey: ['lab', 'tat-report', fromDate, toDate],
    queryFn: () => get<TatReportRow[]>(`${l.orders}/tat-report`, { fromDate, toDate }),
    enabled,
  })

export const useUnbilledOrdersReport = (fromDate: string, toDate: string, enabled: boolean) =>
  useQuery({
    queryKey: ['lab', 'unbilled-report', fromDate, toDate],
    queryFn: () => get<UnbilledOrderRow[]>(`${l.orders}/unbilled-report`, { fromDate, toDate }),
    enabled,
  })

// ── Collection ───────────────────────────────────────────────────────────
export const useCollectionWorklist = () =>
  useQuery({ queryKey: ['lab', 'collection'], queryFn: () => get<CollectionWorklistItem[]>(`${l.collection}/worklist`) })

export const useSamplesAwaitingReceipt = () =>
  useQuery({
    queryKey: ['lab', 'collection', 'awaiting-receipt'],
    queryFn: () => get<Sample[]>(`${l.collection}/samples/awaiting-receipt`),
  })

export const useCollectSample = () =>
  useInvalidating(async (payload: CollectSamplePayload) => {
    const { data } = await api.post<ApiResponse<Sample>>(`${l.collection}/samples`, payload)
    return data.data!
  }, [['lab', 'collection'], ['lab', 'orders']])

export const useReceiveSample = () =>
  useInvalidating(
    (sampleId: number) => api.post(`${l.collection}/samples/${sampleId}/receive`),
    [['lab', 'collection'], ['lab', 'worklist'], ['lab', 'orders']],
  )

export const useRejectSample = () =>
  useInvalidating(
    ({ sampleId, reason }: { sampleId: number; reason: string }) =>
      api.post(`${l.collection}/samples/${sampleId}/reject`, { reason }),
    [['lab', 'collection'], ['lab', 'worklist'], ['lab', 'orders']],
  )

// ── Results ──────────────────────────────────────────────────────────────
export const useDepartmentWorklist = (departmentId?: number) =>
  useQuery({
    queryKey: ['lab', 'worklist', departmentId],
    queryFn: () => get<DepartmentWorklistItem[]>(l.worklist, { departmentId }),
  })

export const useResultQueue = (status: LabOrderItemStatus) =>
  useQuery({
    queryKey: ['lab', 'results', 'queue', status],
    queryFn: () => get<DepartmentWorklistItem[]>(l.resultsQueue, { status }),
  })

export const useLabResult = (itemId: number | null) =>
  useQuery({
    queryKey: ['lab', 'order-items', itemId, 'result'],
    queryFn: () => get<LabResult>(`${l.orderItems}/${itemId}/result`),
    enabled: itemId !== null,
  })

const resultKeys = [['lab', 'worklist'], ['lab', 'results', 'queue'], ['lab', 'orders']]

export const useEnterResult = () =>
  useInvalidating(
    ({ itemId, ...payload }: EnterResultPayload & { itemId: number }) =>
      api.post(`${l.orderItems}/${itemId}/result`, payload),
    resultKeys,
  )

export const useVerifyResult = () =>
  useInvalidating((itemId: number) => api.post(`${l.orderItems}/${itemId}/result/verify`), resultKeys)

export const useApproveResult = () =>
  useInvalidating((itemId: number) => api.post(`${l.orderItems}/${itemId}/result/approve`), resultKeys)

// ── Critical alerts ──────────────────────────────────────────────────────
export const usePendingCriticalAlerts = () =>
  useQuery({ queryKey: ['lab', 'critical-alerts'], queryFn: () => get<CriticalAlert[]>(l.criticalAlerts) })

export const useAcknowledgeAlert = () =>
  useInvalidating(
    ({ id, notes }: { id: number; notes: string | null }) => api.post(`${l.criticalAlerts}/${id}/acknowledge`, { notes }),
    [['lab', 'critical-alerts']],
  )
