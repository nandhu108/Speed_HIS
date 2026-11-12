import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse } from '../../api/types/api'
import { downloadFile } from '../../lib/downloadFile'
import type { ReportResult } from '../reports/types'
import type {
  AccidentCaseRecordDto,
  AssessmentDto,
  DispositionType,
  EmergencyStatus,
  EmergencyVisitDetail,
  EmergencyVisitListItem,
  EsiLevel,
  MlcRecordDto,
  ObservationAllocationDto,
  RecordTriagePayload,
  RegisterEmergencyVisitPayload,
  SaveAccidentPayload,
  SaveAssessmentPayload,
  SaveMlcPayload,
  TriageDto,
} from './types'

const p = endpoints.emergency

async function get<T>(url: string, params?: object): Promise<T> {
  const { data } = await api.get<ApiResponse<T>>(url, { params })
  return data.data!
}

function useInvalidating<TArgs, TResult = unknown>(fn: (args: TArgs) => Promise<TResult>, keys: (string | number)[][]) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () => keys.forEach((key) => queryClient.invalidateQueries({ queryKey: key })),
  })
}

// ── Queue / visits ───────────────────────────────────────────────────────
export const useEmergencyQueue = (filter: { status?: EmergencyStatus; fromDate?: string; toDate?: string }) =>
  useQuery({
    queryKey: ['er', 'queue', filter],
    queryFn: () => get<EmergencyVisitListItem[]>(p.visits, filter),
    refetchInterval: 15000,
  })

export const useEmergencyVisit = (id: number | null) =>
  useQuery({
    queryKey: ['er', 'visits', 'detail', id],
    queryFn: () => get<EmergencyVisitDetail>(`${p.visits}/${id}`),
    enabled: id !== null,
  })

export const useRegisterVisit = () =>
  useInvalidating(async (payload: RegisterEmergencyVisitPayload) => {
    const { data } = await api.post<ApiResponse<number>>(p.visits, payload)
    return data.data!
  }, [['er', 'queue']])

const visitKeys = (id: number): (string | number)[][] => [['er', 'queue'], ['er', 'visits', 'detail', id]]

export const useRecordTriage = (id: number) =>
  useInvalidating(async (payload: RecordTriagePayload) => {
    const { data } = await api.post<ApiResponse<TriageDto>>(`${p.visits}/${id}/triage`, payload)
    return data.data!
  }, visitKeys(id))

export const useCancelVisit = (id: number) =>
  useInvalidating((reason: string) => api.post(`${p.visits}/${id}/cancel`, { reason }), visitKeys(id))

// ── Assessment / disposition ─────────────────────────────────────────────
export const useAssessment = (visitId: number | null) =>
  useQuery({
    queryKey: ['er', 'visits', 'assessment', visitId],
    queryFn: () => get<AssessmentDto>(`${p.visits}/${visitId}/assessment`),
    enabled: visitId !== null,
    retry: false,
  })

export const useSaveAssessment = (visitId: number) =>
  useInvalidating(
    async (payload: SaveAssessmentPayload) => {
      const { data } = await api.put<ApiResponse<AssessmentDto>>(`${p.visits}/${visitId}/assessment`, payload)
      return data.data!
    },
    [['er', 'visits', 'assessment', visitId], ...visitKeys(visitId)],
  )

// ── Observation beds ─────────────────────────────────────────────────────
export const useObservations = (visitId: number | null) =>
  useQuery({
    queryKey: ['er', 'visits', 'observations', visitId],
    queryFn: () => get<ObservationAllocationDto[]>(`${p.visits}/${visitId}/observations`),
    enabled: visitId !== null,
  })

export const useAllocateObservation = (visitId: number) =>
  useInvalidating((bedId: number) => api.post(`${p.visits}/${visitId}/observations`, { bedId }), [
    ['er', 'visits', 'observations', visitId],
  ])

export const useReleaseObservation = (visitId: number) =>
  useInvalidating((allocationId: number) => api.post(`${p.visits}/${visitId}/observations/${allocationId}/release`), [
    ['er', 'visits', 'observations', visitId],
  ])

// ── MLC / accident ───────────────────────────────────────────────────────
export const useMlc = (visitId: number | null) =>
  useQuery({
    queryKey: ['er', 'visits', 'mlc', visitId],
    queryFn: () => get<MlcRecordDto>(`${p.visits}/${visitId}/mlc`),
    enabled: visitId !== null,
    retry: false,
  })

export const useSaveMlc = (visitId: number) =>
  useInvalidating(
    async (payload: SaveMlcPayload) => {
      const { data } = await api.put<ApiResponse<number>>(`${p.visits}/${visitId}/mlc`, payload)
      return data.data!
    },
    [['er', 'visits', 'mlc', visitId]],
  )

export const useAccident = (visitId: number | null) =>
  useQuery({
    queryKey: ['er', 'visits', 'accident', visitId],
    queryFn: () => get<AccidentCaseRecordDto>(`${p.visits}/${visitId}/accident`),
    enabled: visitId !== null,
    retry: false,
  })

export const useSaveAccident = (visitId: number) =>
  useInvalidating(
    async (payload: SaveAccidentPayload) => {
      const { data } = await api.put<ApiResponse<number>>(`${p.visits}/${visitId}/accident`, payload)
      return data.data!
    },
    [['er', 'visits', 'accident', visitId]],
  )

// ── ER Report ─────────────────────────────────────────────────────────────
export interface ErReportFilter {
  fromDate?: string | null
  toDate?: string | null
  mode: 'summary' | 'detail'
  doctorId?: number | null
  esiLevel?: EsiLevel | null
  status?: EmergencyStatus | null
  disposition?: DispositionType | null
  isMlc?: boolean | null
  patientCategoryId?: number | null
  regNo?: string | null
  patientName?: string | null
}

const r = endpoints.erReports

export const useErReport = (filter: ErReportFilter, enabled: boolean) =>
  useQuery({
    queryKey: ['er', 'report', 'visits', filter],
    queryFn: () => get<ReportResult>(r.visits, filter),
    enabled,
  })

export async function downloadErReport(filter: ErReportFilter, format: 'csv' | 'excel' | 'pdf') {
  const params = new URLSearchParams({ mode: filter.mode, format })
  if (filter.fromDate) params.set('fromDate', filter.fromDate)
  if (filter.toDate) params.set('toDate', filter.toDate)
  if (filter.doctorId) params.set('doctorId', String(filter.doctorId))
  if (filter.esiLevel) params.set('esiLevel', String(filter.esiLevel))
  if (filter.status) params.set('status', String(filter.status))
  if (filter.disposition) params.set('disposition', String(filter.disposition))
  if (filter.isMlc !== null && filter.isMlc !== undefined) params.set('isMlc', String(filter.isMlc))
  if (filter.patientCategoryId) params.set('patientCategoryId', String(filter.patientCategoryId))
  if (filter.regNo) params.set('regNo', filter.regNo)
  if (filter.patientName) params.set('patientName', filter.patientName)
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`${r.visits}/export?${params}`, `er-report.${extension}`)
}
