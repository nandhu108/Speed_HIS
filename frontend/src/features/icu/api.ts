import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse } from '../../api/types/api'
import type {
  AddInfusionRatePayload,
  AddVentilatorObservationPayload,
  CriticalAlertRule,
  IcuDashboard,
  IcuSeverityAssessment,
  IcuVitalsAlert,
  InfusionChartEntry,
  RecordSeverityPayload,
  RecordVitalsPayload,
  SaveCriticalAlertRulePayload,
  StartInfusionPayload,
  StartVentilatorSessionPayload,
  VentilatorSession,
} from './types'
import type { IpVitals } from '../ipd/types'

const p = endpoints.icu

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

// ── Critical alert rules ─────────────────────────────────────────────────
export const useCriticalAlertRules = () =>
  useQuery({ queryKey: ['icu', 'alert-rules'], queryFn: () => get<CriticalAlertRule[]>(p.alertRules) })
export const useSaveCriticalAlertRule = () =>
  useInvalidating(
    ({ id, ...payload }: SaveCriticalAlertRulePayload & { id?: number }) =>
      id ? api.put(`${p.alertRules}/${id}`, payload) : api.post(p.alertRules, payload),
    [['icu', 'alert-rules']],
  )

// ── Vitals / alerts ──────────────────────────────────────────────────────
export const useRecordIcuVitals = (admissionId: number) =>
  useInvalidating(async (payload: RecordVitalsPayload) => {
    const { data } = await api.post<ApiResponse<IpVitals>>(`${p.vitals}/admissions/${admissionId}/vitals`, payload)
    return data.data!
  }, [['ipd', 'admissions', String(admissionId), 'vitals'], ['icu', 'alerts'], ['icu', 'dashboard']])

export const useIcuAlerts = (onlyUnacknowledged: boolean) =>
  useQuery({ queryKey: ['icu', 'alerts', onlyUnacknowledged], queryFn: () => get<IcuVitalsAlert[]>(p.alerts, { onlyUnacknowledged }) })
export const useAcknowledgeAlert = () =>
  useInvalidating(
    ({ id, notes }: { id: number; notes: string | null }) => api.post(`${p.alerts}/${id}/acknowledge`, { notes }),
    [['icu', 'alerts'], ['icu', 'dashboard']],
  )

// ── Dashboard ────────────────────────────────────────────────────────────
export const useIcuDashboard = () => useQuery({ queryKey: ['icu', 'dashboard'], queryFn: () => get<IcuDashboard>(p.dashboard) })

// ── Ventilator ───────────────────────────────────────────────────────────
export const useVentilatorSessions = (admissionId: number) =>
  useQuery({
    queryKey: ['icu', 'admissions', admissionId, 'ventilator-sessions'],
    queryFn: () => get<VentilatorSession[]>(`${p.vitals}/admissions/${admissionId}/ventilator-sessions`),
  })
const ventilatorKeys = (admissionId: number) => [['icu', 'admissions', String(admissionId), 'ventilator-sessions'], ['icu', 'dashboard']]
export const useStartVentilatorSession = (admissionId: number) =>
  useInvalidating(
    (payload: StartVentilatorSessionPayload) => api.post(`${p.vitals}/admissions/${admissionId}/ventilator-sessions`, payload),
    ventilatorKeys(admissionId),
  )
export const useStopVentilatorSession = (admissionId: number) =>
  useInvalidating((sessionId: number) => api.post(`${p.vitals}/ventilator-sessions/${sessionId}/stop`), ventilatorKeys(admissionId))
export const useAddVentilatorObservation = (admissionId: number) =>
  useInvalidating(
    ({ sessionId, ...payload }: AddVentilatorObservationPayload & { sessionId: number }) =>
      api.post(`${p.vitals}/ventilator-sessions/${sessionId}/observations`, payload),
    ventilatorKeys(admissionId),
  )

// ── Infusion ─────────────────────────────────────────────────────────────
export const useInfusions = (admissionId: number) =>
  useQuery({
    queryKey: ['icu', 'admissions', admissionId, 'infusions'],
    queryFn: () => get<InfusionChartEntry[]>(`${p.vitals}/admissions/${admissionId}/infusions`),
  })
const infusionKeys = (admissionId: number) => [['icu', 'admissions', String(admissionId), 'infusions']]
export const useStartInfusion = (admissionId: number) =>
  useInvalidating((payload: StartInfusionPayload) => api.post(`${p.vitals}/admissions/${admissionId}/infusions`, payload), infusionKeys(admissionId))
export const useStopInfusion = (admissionId: number) =>
  useInvalidating((entryId: number) => api.post(`${p.vitals}/infusions/${entryId}/stop`), infusionKeys(admissionId))
export const useAddInfusionRate = (admissionId: number) =>
  useInvalidating(
    ({ entryId, ...payload }: AddInfusionRatePayload & { entryId: number }) => api.post(`${p.vitals}/infusions/${entryId}/rates`, payload),
    infusionKeys(admissionId),
  )

// ── Severity ─────────────────────────────────────────────────────────────
export const useSeverityHistory = (admissionId: number) =>
  useQuery({
    queryKey: ['icu', 'admissions', admissionId, 'severity'],
    queryFn: () => get<IcuSeverityAssessment[]>(`${p.vitals}/admissions/${admissionId}/severity`),
  })
export const useRecordSeverity = (admissionId: number) =>
  useInvalidating((payload: RecordSeverityPayload) => api.post(`${p.vitals}/admissions/${admissionId}/severity`, payload), [
    ['icu', 'admissions', String(admissionId), 'severity'],
    ['icu', 'dashboard'],
  ])
