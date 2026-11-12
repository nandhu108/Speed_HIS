import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse, PagedQuery, PagedResult } from '../../api/types/api'
import { downloadFile } from '../../lib/downloadFile'
import type {
  AddOrderPayload,
  AddReferralPayload,
  BulkImportResult,
  ConsultationWorkspace,
  OpReportMode,
  OpReportResult,
  OpVisitDetail,
  OpVisitListItem,
  OpVisitStatus,
  PrescriptionTemplate,
  QueueEntry,
  ReferralDirection,
  RegisterOpVisitPayload,
  SaveComplaint,
  SaveDiagnosis,
  SaveFollowUpPayload,
  SavePrescriptionPayload,
  SavePrescriptionTemplatePayload,
  SaveReferralOpinionPayload,
  SaveVitalsPayload,
  VisitExamination,
  VisitType,
} from './types'

const p = endpoints.opdVisits

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

const visitKeys = [['opd', 'visits'], ['opd', 'queue']]
const workspaceKeys = (id: number): (string | number)[][] => [['opd', 'workspace', id], ['opd', 'visits'], ['opd', 'queue']]

export interface OpVisitFilter extends PagedQuery {
  doctorId?: number
  patientId?: number
  campId?: number
  date?: string
  status?: OpVisitStatus
}

export const useOpVisits = (query: OpVisitFilter) =>
  useQuery({
    queryKey: ['opd', 'visits', 'list', query],
    queryFn: () => get<PagedResult<OpVisitListItem>>(p, query),
  })

export const useOpVisit = (id: number | null) =>
  useQuery({
    queryKey: ['opd', 'visits', 'detail', id],
    queryFn: () => get<OpVisitDetail>(`${p}/${id}`),
    enabled: id !== null,
  })

export const useRegisterOpVisit = () =>
  useInvalidating(async (payload: RegisterOpVisitPayload) => {
    const { data } = await api.post<ApiResponse<OpVisitDetail>>(p, payload)
    return data.data!
  }, visitKeys)

export const useOpdQueue = (doctorId: number | null, date: string) =>
  useQuery({
    queryKey: ['opd', 'queue', doctorId, date],
    queryFn: () => get<QueueEntry[]>(endpoints.opdQueue, { doctorId, date }),
    refetchInterval: 15_000,
  })

/** "Camp Bulk Import" — one camp/doctor for the whole batch, a CSV of UHIDs per row. */
export const useImportCampVisits = () =>
  useInvalidating(async ({ campId, doctorId, file }: { campId: number; doctorId: number; file: File }) => {
    const form = new FormData()
    form.append('campId', String(campId))
    form.append('doctorId', String(doctorId))
    form.append('file', file)
    const { data } = await api.post<ApiResponse<BulkImportResult>>(`${p}/import-camp`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return data.data!
  }, visitKeys)

export const useWorkspace = (visitId: number | null) =>
  useQuery({
    queryKey: ['opd', 'workspace', visitId],
    queryFn: () => get<ConsultationWorkspace>(`${p}/${visitId}/workspace`),
    enabled: visitId !== null,
  })

export const useStartConsult = () =>
  useInvalidating((visitId: number) => api.post(`${p}/${visitId}/start-consult`), visitKeys)

export const useCompleteVisit = () =>
  useInvalidating((visitId: number) => api.post(`${p}/${visitId}/complete`), visitKeys)

export const useCancelVisit = () =>
  useInvalidating(
    ({ visitId, reason }: { visitId: number; reason: string }) => api.post(`${p}/${visitId}/cancel`, { reason }),
    visitKeys,
  )

export const useUpdateVisitCamp = () =>
  useInvalidating(
    ({ visitId, campId }: { visitId: number; campId: number | null }) => api.put(`${p}/${visitId}/camp`, { campId }),
    visitKeys,
  )

export const useSaveVitals = (visitId: number) =>
  useInvalidating((payload: SaveVitalsPayload) => api.put(`${p}/${visitId}/vitals`, payload), workspaceKeys(visitId))

export const useSaveComplaints = (visitId: number) =>
  useInvalidating(
    (complaints: SaveComplaint[]) => api.put(`${p}/${visitId}/complaints`, { complaints }),
    workspaceKeys(visitId),
  )

export const useSaveExamination = (visitId: number) =>
  useInvalidating(
    (payload: VisitExamination) => api.put(`${p}/${visitId}/examination`, payload),
    workspaceKeys(visitId),
  )

export const useSaveDiagnoses = (visitId: number) =>
  useInvalidating(
    (diagnoses: SaveDiagnosis[]) => api.put(`${p}/${visitId}/diagnoses`, { diagnoses }),
    workspaceKeys(visitId),
  )

export const useSavePrescription = (visitId: number) =>
  useInvalidating(
    (payload: SavePrescriptionPayload) => api.put(`${p}/${visitId}/prescription`, payload),
    workspaceKeys(visitId),
  )

export const useAddOrder = (visitId: number) =>
  useInvalidating((payload: AddOrderPayload) => api.post(`${p}/${visitId}/orders`, payload), workspaceKeys(visitId))

export const useCancelOrder = (visitId: number) =>
  useInvalidating((orderId: number) => api.post(`${p}/${visitId}/orders/${orderId}/cancel`), workspaceKeys(visitId))

export const useAddReferral = (visitId: number) =>
  useInvalidating(
    (payload: AddReferralPayload) => api.post(`${p}/${visitId}/referrals`, payload),
    workspaceKeys(visitId),
  )

export const useDeleteReferral = (visitId: number) =>
  useInvalidating((referralId: number) => api.delete(`${p}/${visitId}/referrals/${referralId}`), workspaceKeys(visitId))

export const useSaveReferralOpinion = (visitId: number) =>
  useInvalidating(
    ({ referralId, ...payload }: SaveReferralOpinionPayload & { referralId: number }) =>
      api.put(`${p}/${visitId}/referrals/${referralId}/opinion`, payload),
    workspaceKeys(visitId),
  )

export const useSaveFollowUp = (visitId: number) =>
  useInvalidating(
    (payload: SaveFollowUpPayload) => api.put(`${p}/${visitId}/follow-up`, payload),
    workspaceKeys(visitId),
  )

async function fetchBlob(url: string): Promise<Blob> {
  const { data } = await api.get(url, { responseType: 'blob' })
  return data as Blob
}

export async function openVisitSlip(visitId: number) {
  const blob = await fetchBlob(`${p}/${visitId}/slip`)
  const url = URL.createObjectURL(blob)
  window.open(url, '_blank', 'noopener,noreferrer')
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

/** Downloads the OPD token/registration slip straight to disk instead of opening a preview tab. */
export async function downloadVisitSlip(visitId: number, visitNumber: string) {
  await downloadFile(`${p}/${visitId}/slip`, `visit-slip-${visitNumber}.pdf`)
}

export async function openVisitSummary(visitId: number) {
  const blob = await fetchBlob(`${p}/${visitId}/summary`)
  const url = URL.createObjectURL(blob)
  window.open(url, '_blank', 'noopener,noreferrer')
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

/** "OP Case Sheet Reprint-Direct" — the consultation summary (diagnosis/prescription/advice) is
 * this app's case sheet; downloads it straight to disk instead of opening a preview tab. */
export async function downloadVisitSummary(visitId: number, visitNumber: string) {
  await downloadFile(`${p}/${visitId}/summary`, `case-sheet-${visitNumber}.pdf`)
}

// ── Prescription templates ──────────────────────────────────────────────
export const usePrescriptionTemplates = (doctorId: number | null) =>
  useQuery({
    queryKey: ['opd', 'templates', doctorId],
    queryFn: () => get<PrescriptionTemplate[]>(endpoints.prescriptionTemplates, { doctorId }),
    enabled: doctorId !== null,
  })

export const useSavePrescriptionTemplate = () =>
  useInvalidating(
    ({ id, ...payload }: SavePrescriptionTemplatePayload & { id?: number }) =>
      id
        ? api.put(`${endpoints.prescriptionTemplates}/${id}`, payload)
        : api.post(endpoints.prescriptionTemplates, payload),
    [['opd', 'templates']],
  )

export const useDeletePrescriptionTemplate = () =>
  useInvalidating(
    (id: number) => api.delete(`${endpoints.prescriptionTemplates}/${id}`),
    [['opd', 'templates']],
  )

// ── OP Report ────────────────────────────────────────────────────────────
export interface OpReportFilter {
  mode: OpReportMode
  fromDate?: string | null
  toDate?: string | null
  campId?: number | null
  doctorId?: number | null
  visitType?: VisitType | null
  patientCategoryId?: number | null
  departmentId?: number | null
  referrerId?: number | null
  referralDoctorId?: number | null
  regNo?: string | null
  patientName?: string | null
  /** Filters to OP visits registered with the MLC tag — a plain flag, not Emergency's own statutory MLC register (see docs/modules/opd.md's registration gap-fill notes). */
  isMlcCase?: boolean | null
}

export const useOpVisitsReport = (filter: OpReportFilter, enabled: boolean) =>
  useQuery({
    queryKey: ['opd', 'report', 'visits', filter],
    queryFn: () => get<OpReportResult>(endpoints.opdReports.visits, filter),
    enabled,
  })

export async function downloadOpVisitsReport(filter: OpReportFilter, format: 'csv' | 'excel' | 'pdf') {
  const params = new URLSearchParams({ mode: filter.mode, format })
  if (filter.fromDate) params.set('fromDate', filter.fromDate)
  if (filter.toDate) params.set('toDate', filter.toDate)
  if (filter.campId) params.set('campId', String(filter.campId))
  if (filter.doctorId) params.set('doctorId', String(filter.doctorId))
  if (filter.visitType) params.set('visitType', String(filter.visitType))
  if (filter.patientCategoryId) params.set('patientCategoryId', String(filter.patientCategoryId))
  if (filter.departmentId) params.set('departmentId', String(filter.departmentId))
  if (filter.referrerId) params.set('referrerId', String(filter.referrerId))
  if (filter.referralDoctorId) params.set('referralDoctorId', String(filter.referralDoctorId))
  if (filter.regNo) params.set('regNo', filter.regNo)
  if (filter.patientName) params.set('patientName', filter.patientName)
  if (filter.isMlcCase) params.set('isMlcCase', String(filter.isMlcCase))
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`${endpoints.opdReports.visits}/export?${params}`, `op-report.${extension}`)
}

// ── OP Cancel Report ─────────────────────────────────────────────────────
export interface DateRangeReportFilter {
  fromDate?: string | null
  toDate?: string | null
}

export const useOpCancelReport = (filter: DateRangeReportFilter, enabled: boolean) =>
  useQuery({
    queryKey: ['opd', 'report', 'cancelled', filter],
    queryFn: () => get<OpReportResult>(endpoints.opdReports.cancelled, filter),
    enabled,
  })

export async function downloadOpCancelReport(filter: DateRangeReportFilter, format: 'csv' | 'excel' | 'pdf') {
  const params = new URLSearchParams({ format })
  if (filter.fromDate) params.set('fromDate', filter.fromDate)
  if (filter.toDate) params.set('toDate', filter.toDate)
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`${endpoints.opdReports.cancelled}/export?${params}`, `op-cancel-report.${extension}`)
}

// ── OP TAT Report ────────────────────────────────────────────────────────
export interface OpTatReportFilter extends DateRangeReportFilter {
  doctorId?: number | null
}

export const useOpTatReport = (filter: OpTatReportFilter, enabled: boolean) =>
  useQuery({
    queryKey: ['opd', 'report', 'tat', filter],
    queryFn: () => get<OpReportResult>(endpoints.opdReports.tat, filter),
    enabled,
  })

export async function downloadOpTatReport(filter: OpTatReportFilter, format: 'csv' | 'excel' | 'pdf') {
  const params = new URLSearchParams({ format })
  if (filter.fromDate) params.set('fromDate', filter.fromDate)
  if (filter.toDate) params.set('toDate', filter.toDate)
  if (filter.doctorId) params.set('doctorId', String(filter.doctorId))
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`${endpoints.opdReports.tat}/export?${params}`, `op-tat-report.${extension}`)
}

// ── Referral Report ──────────────────────────────────────────────────────
export interface ReferralReportFilter extends DateRangeReportFilter {
  direction?: ReferralDirection | null
}

export const useReferralReport = (filter: ReferralReportFilter, enabled: boolean) =>
  useQuery({
    queryKey: ['opd', 'report', 'referrals', filter],
    queryFn: () => get<OpReportResult>(endpoints.opdReports.referrals, filter),
    enabled,
  })

export async function downloadReferralReport(filter: ReferralReportFilter, format: 'csv' | 'excel' | 'pdf') {
  const params = new URLSearchParams({ format })
  if (filter.fromDate) params.set('fromDate', filter.fromDate)
  if (filter.toDate) params.set('toDate', filter.toDate)
  if (filter.direction) params.set('direction', String(filter.direction))
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`${endpoints.opdReports.referrals}/export?${params}`, `referral-report.${extension}`)
}

// ── OP to IP Conversion Report ───────────────────────────────────────────
export interface ConversionReportFilter {
  mode: OpReportMode
  fromDate?: string | null
  toDate?: string | null
  doctorId?: number | null
  departmentId?: number | null
  patientCategoryId?: number | null
  regNo?: string | null
  patientName?: string | null
}

export const useOpConversionReport = (filter: ConversionReportFilter, enabled: boolean) =>
  useQuery({
    queryKey: ['opd', 'report', 'conversion', filter],
    queryFn: () => get<OpReportResult>(endpoints.opdReports.conversion, filter),
    enabled,
  })

export async function downloadOpConversionReport(filter: ConversionReportFilter, format: 'csv' | 'excel' | 'pdf') {
  const params = new URLSearchParams({ mode: filter.mode, format })
  if (filter.fromDate) params.set('fromDate', filter.fromDate)
  if (filter.toDate) params.set('toDate', filter.toDate)
  if (filter.doctorId) params.set('doctorId', String(filter.doctorId))
  if (filter.departmentId) params.set('departmentId', String(filter.departmentId))
  if (filter.patientCategoryId) params.set('patientCategoryId', String(filter.patientCategoryId))
  if (filter.regNo) params.set('regNo', filter.regNo)
  if (filter.patientName) params.set('patientName', filter.patientName)
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`${endpoints.opdReports.conversion}/export?${params}`, `op-conversion-report.${extension}`)
}
