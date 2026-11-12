import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse } from '../../api/types/api'
import { downloadFile } from '../../lib/downloadFile'
import type {
  BillingDashboard,
  CensusScope,
  IpDashboard,
  ManagementDashboard,
  OpDashboard,
  ReportCatalogEntry,
  ReportResult,
  ReportSubscription,
  RevenueDashboard,
} from './types'

const d = endpoints.dashboards
const r = endpoints.reports
const census = endpoints.censusReports

async function get<T>(url: string, params?: object): Promise<T> {
  const { data } = await api.get<ApiResponse<T>>(url, { params })
  return data.data!
}

export interface DateRangeParams {
  fromDate?: string | null
  toDate?: string | null
}

export const useManagementDashboard = (range: DateRangeParams) =>
  useQuery({ queryKey: ['dashboards', 'management', range], queryFn: () => get<ManagementDashboard>(d.management, range) })

export const useOpDashboard = (range: DateRangeParams) =>
  useQuery({ queryKey: ['dashboards', 'op', range], queryFn: () => get<OpDashboard>(d.op, range) })

export const useIpDashboard = (range: DateRangeParams) =>
  useQuery({ queryKey: ['dashboards', 'ip', range], queryFn: () => get<IpDashboard>(d.ip, range) })

export const useBillingDashboard = (range: DateRangeParams) =>
  useQuery({ queryKey: ['dashboards', 'billing', range], queryFn: () => get<BillingDashboard>(d.billing, range) })

export const useRevenueDashboard = (range: DateRangeParams) =>
  useQuery({ queryKey: ['dashboards', 'revenue', range], queryFn: () => get<RevenueDashboard>(d.revenue, range) })

export const useReportCatalog = () =>
  useQuery({ queryKey: ['reports', 'catalog'], queryFn: () => get<ReportCatalogEntry[]>(r.catalog) })

export const useExecuteReport = () =>
  useMutation({
    mutationFn: async ({ code, ...body }: DateRangeParams & { code: string; crossBranch?: boolean }) => {
      const { data } = await api.post<ApiResponse<ReportResult>>(`/reports/${code}/execute`, body)
      return data.data!
    },
  })

export async function downloadReportExport(code: string, format: 'csv' | 'excel' | 'pdf', range: DateRangeParams, crossBranch?: boolean) {
  const params = new URLSearchParams({ format })
  if (range.fromDate) params.set('fromDate', range.fromDate)
  if (range.toDate) params.set('toDate', range.toDate)
  if (crossBranch) params.set('crossBranch', 'true')
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`/reports/${code}/export?${params}`, `${code.toLowerCase()}.${extension}`)
}

export const useReportSubscriptions = () =>
  useQuery({ queryKey: ['reports', 'subscriptions'], queryFn: () => get<ReportSubscription[]>(r.subscriptions) })

export const useCreateReportSubscription = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: { reportCode: string; cronExpression: string; recipientEmail: string }) => api.post(r.subscriptions, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['reports', 'subscriptions'] }),
  })
}

export const useDeleteReportSubscription = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => api.delete(`${r.subscriptions}/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['reports', 'subscriptions'] }),
  })
}

// ── OPIP Census Report ───────────────────────────────────────────────────
export interface CensusReportFilter {
  fromDate?: string | null
  toDate?: string | null
  scope: CensusScope
  departmentId?: number | null
  wardId?: number | null
  patientCategoryId?: number | null
}

export const useDepartmentWiseCensus = (filter: CensusReportFilter, enabled: boolean) =>
  useQuery({
    queryKey: ['reports', 'census', 'department-wise', filter],
    queryFn: () => get<ReportResult>(census.departmentWise, filter),
    enabled,
  })

export const useCensusPatientWiseDetails = (filter: CensusReportFilter, enabled: boolean) =>
  useQuery({
    queryKey: ['reports', 'census', 'patient-wise', filter],
    queryFn: () => get<ReportResult>(census.patientWise, filter),
    enabled,
  })

function censusParams(filter: CensusReportFilter, format: string) {
  const params = new URLSearchParams({ scope: String(filter.scope), format })
  if (filter.fromDate) params.set('fromDate', filter.fromDate)
  if (filter.toDate) params.set('toDate', filter.toDate)
  if (filter.departmentId) params.set('departmentId', String(filter.departmentId))
  if (filter.wardId) params.set('wardId', String(filter.wardId))
  if (filter.patientCategoryId) params.set('patientCategoryId', String(filter.patientCategoryId))
  return params
}

export async function downloadDepartmentWiseCensus(filter: CensusReportFilter, format: 'csv' | 'excel' | 'pdf') {
  const params = censusParams(filter, format)
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`${census.departmentWise}/export?${params}`, `opip-census-department-wise.${extension}`)
}

export async function downloadCensusPatientWiseDetails(filter: CensusReportFilter, format: 'csv' | 'excel' | 'pdf') {
  const params = censusParams(filter, format)
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`${census.patientWise}/export?${params}`, `opip-census-patient-wise.${extension}`)
}
