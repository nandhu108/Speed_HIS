import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse, PagedQuery, PagedResult } from '../../api/types/api'
import type {
  Audit,
  AuditTemplate,
  Capa,
  CapaSourceType,
  CapaStatus,
  ComplianceDashboard,
  Evidence,
  Indicator,
  IndicatorEntry,
  KpiDashboard,
  NonConformance,
  NonConformanceStatus,
  RaiseCapaPayload,
  RaiseNonConformancePayload,
  RecordIndicatorEntryPayload,
  Risk,
  RiskStatus,
  SaveAuditTemplatePayload,
  SaveCommitteeMeetingPayload,
  SaveIndicatorPayload,
  SaveRiskPayload,
  ScheduleAuditPayload,
  ScoreAuditPayload,
  CommitteeMeeting,
} from './types'

const p = endpoints.quality

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

// ── Indicators ───────────────────────────────────────────────────────────
export const useIndicators = (search?: string) =>
  useQuery({ queryKey: ['quality', 'indicators', search], queryFn: () => get<Indicator[]>(p.indicators, { search }) })

export const useSaveIndicator = () =>
  useInvalidating(async ({ id, ...payload }: SaveIndicatorPayload & { id?: number }) => {
    if (id) { await api.put(`${p.indicators}/${id}`, payload); return id }
    const { data } = await api.post<ApiResponse<number>>(p.indicators, payload)
    return data.data!
  }, [['quality', 'indicators']])

export const useIndicatorEntries = (indicatorId: number | null) =>
  useQuery({
    queryKey: ['quality', 'indicators', indicatorId, 'entries'],
    queryFn: () => get<IndicatorEntry[]>(`${p.indicators}/${indicatorId}/entries`),
    enabled: indicatorId !== null,
  })

export const useRecordIndicatorEntry = (indicatorId: number) =>
  useInvalidating(
    (payload: RecordIndicatorEntryPayload) => api.post(`${p.indicators}/${indicatorId}/entries`, payload),
    [['quality', 'indicators', String(indicatorId), 'entries']],
  )

export const useAutoComputeIndicatorEntry = (indicatorId: number) =>
  useInvalidating(
    (period: string) => api.post(`${p.indicators}/${indicatorId}/entries/auto-compute`, null, { params: { period } }),
    [['quality', 'indicators', String(indicatorId), 'entries']],
  )

// ── Audit checklists ──────────────────────────────────────────────────────
export const useAuditTemplates = () =>
  useQuery({ queryKey: ['quality', 'audit-templates'], queryFn: () => get<AuditTemplate[]>(p.auditTemplates) })

export const useCreateAuditTemplate = () =>
  useInvalidating((payload: SaveAuditTemplatePayload) => api.post(p.auditTemplates, payload), [['quality', 'audit-templates']])

export const useAudits = (query: PagedQuery & { status?: number }) =>
  useQuery({ queryKey: ['quality', 'audits', query], queryFn: () => get<PagedResult<Audit>>(p.audits, query) })

const auditKeys = [['quality', 'audits']]
export const useScheduleAudit = () => useInvalidating((payload: ScheduleAuditPayload) => api.post(p.audits, payload), auditKeys)
export const useScoreAudit = () =>
  useInvalidating(({ id, ...payload }: ScoreAuditPayload & { id: number }) => api.post(`${p.audits}/${id}/score`, payload), auditKeys)
export const useCompleteAudit = () => useInvalidating((id: number) => api.post(`${p.audits}/${id}/complete`), auditKeys)

// ── Non-conformances ──────────────────────────────────────────────────────
export const useNonConformances = (query: PagedQuery & { status?: NonConformanceStatus }) =>
  useQuery({ queryKey: ['quality', 'non-conformances', query], queryFn: () => get<PagedResult<NonConformance>>(p.nonConformances, query) })

const ncKeys = [['quality', 'non-conformances']]
export const useRaiseNonConformance = () =>
  useInvalidating((payload: RaiseNonConformancePayload) => api.post(p.nonConformances, payload), ncKeys)
export const useMarkNonConformanceInProgress = () =>
  useInvalidating((id: number) => api.post(`${p.nonConformances}/${id}/in-progress`), ncKeys)
export const useCloseNonConformance = () =>
  useInvalidating(({ id, notes }: { id: number; notes: string | null }) => api.post(`${p.nonConformances}/${id}/close`, { notes }), ncKeys)

// ── Risk register ─────────────────────────────────────────────────────────
export const useRisks = (status?: RiskStatus) =>
  useQuery({ queryKey: ['quality', 'risks', status], queryFn: () => get<Risk[]>(p.risks, { status }) })

const riskKeys = [['quality', 'risks']]
export const useCreateRisk = () => useInvalidating((payload: SaveRiskPayload) => api.post(p.risks, payload), riskKeys)
export const useSetRiskStatus = () =>
  useInvalidating(({ id, status }: { id: number; status: RiskStatus }) => api.post(`${p.risks}/${id}/status`, null, { params: { status } }), riskKeys)

// ── CAPA tracker ───────────────────────────────────────────────────────────
export const useCapas = (query: { status?: CapaStatus; sourceType?: CapaSourceType }) =>
  useQuery({ queryKey: ['quality', 'capas', query], queryFn: () => get<Capa[]>(p.capas, query) })

const capaKeys = [['quality', 'capas']]
export const useRaiseCapa = () => useInvalidating((payload: RaiseCapaPayload) => api.post(p.capas, payload), capaKeys)
export const useCompleteCapa = () => useInvalidating((id: number) => api.post(`${p.capas}/${id}/complete`), capaKeys)
export const useVerifyCapa = () =>
  useInvalidating(({ id, verificationNotes }: { id: number; verificationNotes: string | null }) => api.post(`${p.capas}/${id}/verify`, { verificationNotes }), capaKeys)
export const useCloseCapa = () => useInvalidating((id: number) => api.post(`${p.capas}/${id}/close`), capaKeys)

// ── Committee meetings ─────────────────────────────────────────────────────
export const useCommitteeMeetings = (committeeId?: number) =>
  useQuery({ queryKey: ['quality', 'committee-meetings', committeeId], queryFn: () => get<CommitteeMeeting[]>(p.committeeMeetings, { committeeId }) })

export const useCreateCommitteeMeeting = () =>
  useInvalidating((payload: SaveCommitteeMeetingPayload) => api.post(p.committeeMeetings, payload), [['quality', 'committee-meetings']])

export const useCompleteCommitteeActionItem = () =>
  useInvalidating(
    ({ meetingId, actionItemId }: { meetingId: number; actionItemId: number }) =>
      api.post(`${p.committeeMeetings}/${meetingId}/action-items/${actionItemId}/complete`),
    [['quality', 'committee-meetings']],
  )

// ── NABH evidence ──────────────────────────────────────────────────────────
export const useEvidence = (standardId?: number) =>
  useQuery({ queryKey: ['quality', 'evidence', standardId], queryFn: () => get<Evidence[]>(p.evidence, { standardId }) })

export const useUploadEvidence = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (payload: { standardId: number; title: string; description: string | null; file: File }) => {
      const form = new FormData()
      form.append('standardId', String(payload.standardId))
      form.append('title', payload.title)
      if (payload.description) form.append('description', payload.description)
      form.append('file', payload.file)
      const { data } = await api.post<ApiResponse<number>>(p.evidence, form, { headers: { 'Content-Type': 'multipart/form-data' } })
      return data.data!
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['quality', 'evidence'] }),
  })
}

export const evidenceFileUrl = (id: number) => `${p.evidence}/${id}/file`

// ── Dashboards ─────────────────────────────────────────────────────────────
export const useKpiDashboard = (fromPeriod: string, toPeriod: string) =>
  useQuery({
    queryKey: ['quality', 'dashboards', 'kpi', fromPeriod, toPeriod],
    queryFn: () => get<KpiDashboard>(`${p.dashboards}/kpi`, { fromPeriod, toPeriod }),
  })

export const useComplianceDashboard = () =>
  useQuery({ queryKey: ['quality', 'dashboards', 'compliance'], queryFn: () => get<ComplianceDashboard>(`${p.dashboards}/compliance`) })
