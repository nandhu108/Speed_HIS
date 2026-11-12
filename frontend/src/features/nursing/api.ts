import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse, PagedQuery, PagedResult } from '../../api/types/api'
import type {
  CarePlan,
  CreateShiftHandoverPayload,
  Incident,
  IncidentSeverity,
  IncidentStatus,
  MarActionPayload,
  MarSchedule,
  NursingAssessment,
  NursingNote,
  NursingShift,
  RecordIncidentRcaPayload,
  ReportIncidentPayload,
  RiskAssessment,
  RiskAssessmentType,
  ShiftHandoverDetail,
  ShiftHandoverListItem,
  TransferNote,
  WardPatient,
} from './types'

const p = endpoints.nursing

async function get<T>(url: string, params?: object): Promise<T> {
  const { data } = await api.get<ApiResponse<T>>(url, { params })
  return data.data!
}

async function post<T>(url: string, payload?: object): Promise<T> {
  const { data } = await api.post<ApiResponse<T>>(url, payload)
  return data.data!
}

function useInvalidating<TArgs, TResult = unknown>(fn: (args: TArgs) => Promise<TResult>, keys: string[][]) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () => keys.forEach((key) => queryClient.invalidateQueries({ queryKey: key })),
  })
}

// ── Admission-scoped charting ───────────────────────────────────────────────
export const useNursingAssessments = (admissionId: number) =>
  useQuery({
    queryKey: ['nursing', 'admissions', admissionId, 'assessments'],
    queryFn: () => get<NursingAssessment[]>(`${p.admissions}/${admissionId}/assessments`),
  })
export const useAddNursingAssessment = (admissionId: number) =>
  useInvalidating(
    (payload: { templateCode: string; dataJson: string }) => post<number>(`${p.admissions}/${admissionId}/assessments`, payload),
    [['nursing', 'admissions', String(admissionId), 'assessments']],
  )

export const useNursingNotes = (admissionId: number) =>
  useQuery({
    queryKey: ['nursing', 'admissions', admissionId, 'notes'],
    queryFn: () => get<NursingNote[]>(`${p.admissions}/${admissionId}/notes`),
  })
export const useAddNursingNote = (admissionId: number) =>
  useInvalidating(
    (payload: { shift: NursingShift; notes: string }) => post<number>(`${p.admissions}/${admissionId}/notes`, payload),
    [['nursing', 'admissions', String(admissionId), 'notes']],
  )

export const useRiskAssessments = (admissionId: number, type?: RiskAssessmentType) =>
  useQuery({
    queryKey: ['nursing', 'admissions', admissionId, 'risk-assessments', type],
    queryFn: () => get<RiskAssessment[]>(`${p.admissions}/${admissionId}/risk-assessments`, { type }),
  })
export const useAddRiskAssessment = (admissionId: number) =>
  useInvalidating(
    (payload: { assessmentType: RiskAssessmentType; itemScores: Record<string, number> }) =>
      post<number>(`${p.admissions}/${admissionId}/risk-assessments`, payload),
    [['nursing', 'admissions', String(admissionId), 'risk-assessments']],
  )

export const useCarePlans = (admissionId: number) =>
  useQuery({
    queryKey: ['nursing', 'admissions', admissionId, 'care-plans'],
    queryFn: () => get<CarePlan[]>(`${p.admissions}/${admissionId}/care-plans`),
  })
export const useAddCarePlan = (admissionId: number) =>
  useInvalidating(
    (payload: { problemStatement: string; goal: string; interventions: string }) =>
      post<number>(`${p.admissions}/${admissionId}/care-plans`, payload),
    [['nursing', 'admissions', String(admissionId), 'care-plans']],
  )
export const useResolveCarePlan = (admissionId: number) =>
  useInvalidating(
    ({ carePlanId, evaluationNotes }: { carePlanId: number; evaluationNotes: string | null }) =>
      api.post(`${p.admissions}/${admissionId}/care-plans/${carePlanId}/resolve`, { evaluationNotes }),
    [['nursing', 'admissions', String(admissionId), 'care-plans']],
  )

export const useTransferNotes = (admissionId: number) =>
  useQuery({
    queryKey: ['nursing', 'admissions', admissionId, 'transfer-notes'],
    queryFn: () => get<TransferNote[]>(`${p.admissions}/${admissionId}/transfer-notes`),
  })
export const useAddTransferNote = (admissionId: number) =>
  useInvalidating(
    (payload: { bedAllocationId: number | null; handoverNotes: string; receivedByUserId: number | null }) =>
      post<number>(`${p.admissions}/${admissionId}/transfer-notes`, payload),
    [['nursing', 'admissions', String(admissionId), 'transfer-notes']],
  )

// ── MAR ──────────────────────────────────────────────────────────────────
export const useMar = (admissionId: number, date: string) =>
  useQuery({
    queryKey: ['nursing', 'mar', String(admissionId), date],
    queryFn: () => get<MarSchedule[]>(p.mar, { admissionId, date }),
    enabled: admissionId > 0 && !!date,
  })
export const useMarAction = (admissionId: number, date: string) =>
  useInvalidating(
    ({ id, payload }: { id: number; payload: MarActionPayload }) => api.post(`${p.mar}/${id}/administer`, payload),
    [['nursing', 'mar', String(admissionId), date]],
  )

// ── Shift handover ───────────────────────────────────────────────────────
export const useWardPatients = (wardId: number | null) =>
  useQuery({
    queryKey: ['nursing', 'handovers', 'ward-patients', wardId],
    queryFn: () => get<WardPatient[]>(`${p.handovers}/ward-patients`, { wardId }),
    enabled: wardId !== null,
  })
export const useShiftHandovers = (wardId?: number, date?: string) =>
  useQuery({
    queryKey: ['nursing', 'handovers', wardId, date],
    queryFn: () => get<ShiftHandoverListItem[]>(p.handovers, { wardId, date }),
  })
export const useShiftHandover = (id: number | null) =>
  useQuery({
    queryKey: ['nursing', 'handovers', 'detail', id],
    queryFn: () => get<ShiftHandoverDetail>(`${p.handovers}/${id}`),
    enabled: id !== null,
  })
export const useCreateShiftHandover = () =>
  useInvalidating((payload: CreateShiftHandoverPayload) => post<number>(p.handovers, payload), [['nursing', 'handovers']])

// ── Incidents ────────────────────────────────────────────────────────────
export interface IncidentFilter extends PagedQuery {
  status?: IncidentStatus
  severity?: IncidentSeverity
  fromDate?: string
  toDate?: string
  sentinelOnly?: boolean
}
export const useIncidents = (query: IncidentFilter) =>
  useQuery({ queryKey: ['nursing', 'incidents', query], queryFn: () => get<PagedResult<Incident>>(p.incidents, query) })
export const useReportIncident = () =>
  useInvalidating((payload: ReportIncidentPayload) => post<number>(p.incidents, payload), [['nursing', 'incidents']])
export const useReviewIncident = () =>
  useInvalidating(
    ({ id, reviewNotes }: { id: number; reviewNotes: string }) => api.post(`${p.incidents}/${id}/review`, { reviewNotes }),
    [['nursing', 'incidents']],
  )
export const useCloseIncident = () =>
  useInvalidating(
    ({ id, reviewNotes }: { id: number; reviewNotes: string | null }) => api.post(`${p.incidents}/${id}/close`, { reviewNotes }),
    [['nursing', 'incidents']],
  )
export const useRecordIncidentRca = () =>
  useInvalidating(
    ({ id, ...payload }: { id: number } & RecordIncidentRcaPayload) => api.post(`${p.incidents}/${id}/rca`, payload),
    [['nursing', 'incidents']],
  )
