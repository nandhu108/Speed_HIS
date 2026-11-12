import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse, PagedQuery, PagedResult } from '../../api/types/api'
import { downloadFile } from '../../lib/downloadFile'
import type {
  BulkImportResult,
  MhcReportResult,
  MhcVisitDetail,
  MhcVisitListItem,
  MhcVisitStatus,
  MhcVisitType,
  RegisterMhcVisitPayload,
} from './types'
import type { BillType } from '../billing/types'

const p = endpoints.mhcVisits

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

const visitKeys = [['mhc', 'visits']]

export interface MhcVisitFilter extends PagedQuery {
  patientId?: number
  date?: string
  status?: MhcVisitStatus
}

export const useMhcVisits = (query: MhcVisitFilter) =>
  useQuery({
    queryKey: ['mhc', 'visits', 'list', query],
    queryFn: () => get<PagedResult<MhcVisitListItem>>(p, query),
  })

export const useRegisterMhcVisit = () =>
  useInvalidating(async (payload: RegisterMhcVisitPayload) => {
    const { data } = await api.post<ApiResponse<MhcVisitDetail>>(p, payload)
    return data.data!
  }, visitKeys)

export const useCancelMhcVisit = () =>
  useInvalidating(
    ({ visitId, reason }: { visitId: number; reason: string }) => api.post(`${p}/${visitId}/cancel`, { reason }),
    visitKeys,
  )

/** "MHC Bulk Import" — one MHC doctor/package for the whole batch, a CSV of UHIDs per row. */
export const useImportMhcVisits = () =>
  useInvalidating(
    async ({ mhcDoctorId, mhcPackageId, file }: { mhcDoctorId: number; mhcPackageId: number; file: File }) => {
      const form = new FormData()
      form.append('mhcDoctorId', String(mhcDoctorId))
      form.append('mhcPackageId', String(mhcPackageId))
      form.append('file', file)
      const { data } = await api.post<ApiResponse<BulkImportResult>>(`${p}/import`, form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      return data.data!
    },
    visitKeys,
  )

export async function openMhcVisitSlip(visitId: number) {
  const { data } = await api.get(`${p}/${visitId}/slip`, { responseType: 'blob' })
  const url = URL.createObjectURL(data as Blob)
  window.open(url, '_blank', 'noopener,noreferrer')
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

// ── MHC Reports ──────────────────────────────────────────────────────────
export interface MhcReportFilter {
  fromDate?: string | null
  toDate?: string | null
  mhcDoctorId?: number | null
  visitType?: MhcVisitType | null
  patientCategoryId?: number | null
  regNo?: string | null
  patientName?: string | null
}

function useMhcReport(kind: 'package-wise' | 'doctor-wise' | 'referrer-wise' | 'patient-list', url: string, filter: MhcReportFilter, enabled: boolean) {
  return useQuery({
    queryKey: ['mhc', 'report', kind, filter],
    queryFn: () => get<MhcReportResult>(url, filter),
    enabled,
  })
}

async function downloadMhcReport(url: string, filter: MhcReportFilter, format: 'csv' | 'excel' | 'pdf', fileNameBase: string) {
  const params = new URLSearchParams({ format })
  if (filter.fromDate) params.set('fromDate', filter.fromDate)
  if (filter.toDate) params.set('toDate', filter.toDate)
  if (filter.mhcDoctorId) params.set('mhcDoctorId', String(filter.mhcDoctorId))
  if (filter.visitType) params.set('visitType', String(filter.visitType))
  if (filter.patientCategoryId) params.set('patientCategoryId', String(filter.patientCategoryId))
  if (filter.regNo) params.set('regNo', filter.regNo)
  if (filter.patientName) params.set('patientName', filter.patientName)
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`${url}/export?${params}`, `${fileNameBase}.${extension}`)
}

export const useMhcPackageWiseReport = (filter: MhcReportFilter, enabled: boolean) =>
  useMhcReport('package-wise', endpoints.mhcReports.packageWise, filter, enabled)
export const downloadMhcPackageWiseReport = (filter: MhcReportFilter, format: 'csv' | 'excel' | 'pdf') =>
  downloadMhcReport(endpoints.mhcReports.packageWise, filter, format, 'mhc-package-wise-report')

export const useMhcDoctorWiseReport = (filter: MhcReportFilter, enabled: boolean) =>
  useMhcReport('doctor-wise', endpoints.mhcReports.doctorWise, filter, enabled)
export const downloadMhcDoctorWiseReport = (filter: MhcReportFilter, format: 'csv' | 'excel' | 'pdf') =>
  downloadMhcReport(endpoints.mhcReports.doctorWise, filter, format, 'mhc-doctor-wise-report')

export const useMhcReferrerWiseReport = (filter: MhcReportFilter, enabled: boolean) =>
  useMhcReport('referrer-wise', endpoints.mhcReports.referrerWise, filter, enabled)
export const downloadMhcReferrerWiseReport = (filter: MhcReportFilter, format: 'csv' | 'excel' | 'pdf') =>
  downloadMhcReport(endpoints.mhcReports.referrerWise, filter, format, 'mhc-referrer-wise-report')

export const useMhcPatientListReport = (filter: MhcReportFilter, enabled: boolean) =>
  useMhcReport('patient-list', endpoints.mhcReports.patientList, filter, enabled)
export const downloadMhcPatientListReport = (filter: MhcReportFilter, format: 'csv' | 'excel' | 'pdf') =>
  downloadMhcReport(endpoints.mhcReports.patientList, filter, format, 'mhc-patient-list-report')

// ── Service List Report - MHC ────────────────────────────────────────────
export interface MhcServiceListReportFilter {
  fromDate?: string | null
  toDate?: string | null
  serviceId?: number | null
  billType?: BillType | null
  regNo?: string | null
}

export const useMhcServiceListReport = (filter: MhcServiceListReportFilter, enabled: boolean) =>
  useQuery({
    queryKey: ['mhc', 'report', 'service-list', filter],
    queryFn: () => get<MhcReportResult>(endpoints.mhcReports.serviceList, filter),
    enabled,
  })

export async function downloadMhcServiceListReport(filter: MhcServiceListReportFilter, format: 'csv' | 'excel' | 'pdf') {
  const params = new URLSearchParams({ format })
  if (filter.fromDate) params.set('fromDate', filter.fromDate)
  if (filter.toDate) params.set('toDate', filter.toDate)
  if (filter.serviceId) params.set('serviceId', String(filter.serviceId))
  if (filter.billType) params.set('billType', String(filter.billType))
  if (filter.regNo) params.set('regNo', filter.regNo)
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`${endpoints.mhcReports.serviceList}/export?${params}`, `mhc-service-list-report.${extension}`)
}
