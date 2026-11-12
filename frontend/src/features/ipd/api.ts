import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse, PagedQuery, PagedResult } from '../../api/types/api'
import { downloadFile } from '../../lib/downloadFile'
import type {
  AddProcedureNoteRequestPayload,
  AdmissionDetail,
  AdmissionListItem,
  AdmissionRequestItem,
  AdmissionRequestStatus,
  AdmissionStatus,
  AdmitPatientPayload,
  BedBoardItem,
  BedOccupancyDashboard,
  BedStatus,
  ClearanceType,
  CreateAdmissionRequestPayload,
  CreateDoctorOrderPayload,
  DischargeClearance,
  DischargeSummary,
  DoctorOrder,
  DoctorOrderType,
  IntakeOutputEntry,
  IpReportMode,
  IpReportResult,
  IpVitals,
  ProcedureNote,
  ProgressNote,
} from './types'

const p = endpoints.ipd

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

async function post<T>(url: string, payload?: object): Promise<T> {
  const { data } = await api.post<ApiResponse<T>>(url, payload)
  return data.data!
}

function openPdf(url: string, filename: string) {
  return api.get(url, { responseType: 'blob' }).then(({ data }) => {
    const blobUrl = URL.createObjectURL(data as Blob)
    window.open(blobUrl, '_blank', 'noopener,noreferrer')
    setTimeout(() => URL.revokeObjectURL(blobUrl), 60_000)
    void filename
  })
}

// ── Admissions ───────────────────────────────────────────────────────────
export interface AdmissionFilter extends PagedQuery {
  patientId?: number
  status?: number
}
export const useAdmissions = (query: AdmissionFilter) =>
  useQuery({ queryKey: ['ipd', 'admissions', query], queryFn: () => get<PagedResult<AdmissionListItem>>(p.admissions, query) })
export const useAdmission = (id: number | null) =>
  useQuery({
    queryKey: ['ipd', 'admissions', 'detail', id],
    queryFn: () => get<AdmissionDetail>(`${p.admissions}/${id}`),
    enabled: id !== null,
  })
export const useAdmitPatient = () =>
  useInvalidating((payload: AdmitPatientPayload) => post<number>(p.admissions, payload), [['ipd', 'admissions'], ['ipd', 'bed-board']])
export const useCancelAdmission = () =>
  useInvalidating(
    ({ id, reason }: { id: number; reason: string }) => api.post(`${p.admissions}/${id}/cancel`, { reason }),
    [['ipd', 'admissions'], ['ipd', 'bed-board']],
  )
export const useTransferBed = () =>
  useInvalidating(
    ({ id, newBedId, reason }: { id: number; newBedId: number; reason: string | null }) =>
      api.post(`${p.admissions}/${id}/transfer-bed`, { newBedId, reason }),
    [['ipd', 'admissions'], ['ipd', 'bed-board']],
  )
export const useTransferConsultant = () =>
  useInvalidating(
    ({ id, newPrimaryConsultantId, notes }: { id: number; newPrimaryConsultantId: number; notes: string | null }) =>
      api.post(`${p.admissions}/${id}/transfer-consultant`, { newPrimaryConsultantId, notes }),
    [['ipd', 'admissions']],
  )
export const useAddSecondaryConsultant = () =>
  useInvalidating(
    ({ id, doctorId, notes }: { id: number; doctorId: number; notes: string | null }) =>
      api.post(`${p.admissions}/${id}/consultants`, { doctorId, notes }),
    [['ipd', 'admissions']],
  )
export const useEndSecondaryConsultant = () =>
  useInvalidating(
    ({ id, consultantId }: { id: number; consultantId: number }) => api.post(`${p.admissions}/${id}/consultants/${consultantId}/end`),
    [['ipd', 'admissions']],
  )
export const openAdmissionSlip = (id: number) => openPdf(`${p.admissions}/${id}/admission-slip`, `admission-slip-${id}.pdf`)

// ── Admission requests ───────────────────────────────────────────────────
export interface AdmissionRequestFilter extends PagedQuery {
  status?: AdmissionRequestStatus
}
export const useAdmissionRequests = (query: AdmissionRequestFilter) =>
  useQuery({
    queryKey: ['ipd', 'admission-requests', query],
    queryFn: () => get<PagedResult<AdmissionRequestItem>>(p.admissionRequests, query),
  })
export const useCreateAdmissionRequest = () =>
  useInvalidating(
    (payload: CreateAdmissionRequestPayload) => post<number>(p.admissionRequests, payload),
    [['ipd', 'admission-requests']],
  )
export const useApproveAdmissionRequest = () =>
  useInvalidating((id: number) => api.post(`${p.admissionRequests}/${id}/approve`), [['ipd', 'admission-requests']])
export const useRejectAdmissionRequest = () =>
  useInvalidating(
    ({ id, reason }: { id: number; reason: string }) => api.post(`${p.admissionRequests}/${id}/reject`, { reason }),
    [['ipd', 'admission-requests']],
  )

// ── Doctor orders ────────────────────────────────────────────────────────
export const useDoctorOrders = (admissionId: number, orderType?: DoctorOrderType) =>
  useQuery({
    queryKey: ['ipd', 'admissions', admissionId, 'orders', orderType],
    queryFn: () => get<DoctorOrder[]>(`${p.admissions}/${admissionId}/orders`, { orderType }),
  })
export const useCreateDoctorOrder = (admissionId: number) =>
  useInvalidating(
    (payload: CreateDoctorOrderPayload) => post<number>(`${p.admissions}/${admissionId}/orders`, payload),
    [['ipd', 'admissions', admissionId, 'orders']],
  )
export const useCancelDoctorOrder = (admissionId: number) =>
  useInvalidating(
    ({ orderId, reason }: { orderId: number; reason: string }) =>
      api.post(`${p.admissions}/${admissionId}/orders/${orderId}/cancel`, { reason }),
    [['ipd', 'admissions', admissionId, 'orders']],
  )

// ── Clinical charts ──────────────────────────────────────────────────────
export const useProgressNotes = (admissionId: number) =>
  useQuery({ queryKey: ['ipd', 'admissions', admissionId, 'progress-notes'], queryFn: () => get<ProgressNote[]>(`${p.admissions}/${admissionId}/progress-notes`) })
export const useAddProgressNote = (admissionId: number) =>
  useInvalidating(
    (payload: { doctorId: number; notes: string }) => post<number>(`${p.admissions}/${admissionId}/progress-notes`, payload),
    [['ipd', 'admissions', admissionId, 'progress-notes']],
  )

export const useIpVitals = (admissionId: number) =>
  useQuery({ queryKey: ['ipd', 'admissions', admissionId, 'vitals'], queryFn: () => get<IpVitals[]>(`${p.admissions}/${admissionId}/vitals`) })
export const useAddIpVitals = (admissionId: number) =>
  useInvalidating(
    (payload: Partial<Record<'temperatureF' | 'pulseRate' | 'respiratoryRate' | 'bloodPressureSystolic' | 'bloodPressureDiastolic' | 'spO2' | 'randomBloodSugar', number | null>>) =>
      post<number>(`${p.admissions}/${admissionId}/vitals`, payload),
    [['ipd', 'admissions', admissionId, 'vitals']],
  )

export const useIntakeOutput = (admissionId: number) =>
  useQuery({ queryKey: ['ipd', 'admissions', admissionId, 'intake-output'], queryFn: () => get<IntakeOutputEntry[]>(`${p.admissions}/${admissionId}/intake-output`) })
export const useAddIntakeOutput = (admissionId: number) =>
  useInvalidating(
    (payload: { isIntake: boolean; category: string; quantityMl: number; notes: string | null }) =>
      post<number>(`${p.admissions}/${admissionId}/intake-output`, payload),
    [['ipd', 'admissions', admissionId, 'intake-output']],
  )

export const useProcedureNotes = (admissionId: number) =>
  useQuery({ queryKey: ['ipd', 'admissions', admissionId, 'procedure-notes'], queryFn: () => get<ProcedureNote[]>(`${p.admissions}/${admissionId}/procedure-notes`) })
export const useAddProcedureNote = (admissionId: number) =>
  useInvalidating(
    (payload: AddProcedureNoteRequestPayload) => post<number>(`${p.admissions}/${admissionId}/procedure-notes`, payload),
    [['ipd', 'admissions', admissionId, 'procedure-notes']],
  )

// ── Discharge ────────────────────────────────────────────────────────────
export const useRequestDischarge = (admissionId: number) =>
  useInvalidating(
    (payload: { requestedByDoctorId: number; dischargeType: number; dischargeTypeNotes: string | null }) =>
      api.post(`${p.admissions}/${admissionId}/discharge/request`, payload),
    [['ipd', 'admissions', 'detail', admissionId], ['ipd', 'admissions', admissionId, 'clearances']],
  )
export const useDischargeClearances = (admissionId: number) =>
  useQuery({
    queryKey: ['ipd', 'admissions', admissionId, 'clearances'],
    queryFn: () => get<DischargeClearance[]>(`${p.admissions}/${admissionId}/discharge/clearances`),
  })
export const useClearChecklistItem = (admissionId: number) =>
  useInvalidating(
    ({ clearanceType, remarks }: { clearanceType: ClearanceType; remarks: string | null }) =>
      api.post(`${p.admissions}/${admissionId}/discharge/clearances/${clearanceType}`, { remarks }),
    [['ipd', 'admissions', admissionId, 'clearances']],
  )
export const useDischargeSummary = (admissionId: number) =>
  useQuery({
    queryKey: ['ipd', 'admissions', admissionId, 'discharge-summary'],
    queryFn: () => get<DischargeSummary | null>(`${p.admissions}/${admissionId}/discharge/summary`),
  })
export const useUpsertDischargeSummary = (admissionId: number) =>
  useInvalidating(
    (payload: { finalDiagnosis: string; courseInHospital: string | null; medicationsOnDischarge: string | null; followUpAdvice: string | null; conditionAtDischarge: string | null }) =>
      api.put(`${p.admissions}/${admissionId}/discharge/summary`, payload),
    [['ipd', 'admissions', admissionId, 'discharge-summary']],
  )
export const useApproveDischargeSummary = (admissionId: number) =>
  useInvalidating(
    (approvedByDoctorId: number) => api.post(`${p.admissions}/${admissionId}/discharge/summary/approve`, { approvedByDoctorId }),
    [['ipd', 'admissions', admissionId, 'discharge-summary']],
  )
export const useFinalizeDischarge = (admissionId: number) =>
  useInvalidating(() => api.post(`${p.admissions}/${admissionId}/discharge/finalize`), [
    ['ipd', 'admissions'],
    ['ipd', 'bed-board'],
  ])
export const openDischargeSummaryPdf = (admissionId: number) =>
  openPdf(`${p.admissions}/${admissionId}/discharge/summary-pdf`, `discharge-summary-${admissionId}.pdf`)
export const openDischargeSlip = (admissionId: number) =>
  openPdf(`${p.admissions}/${admissionId}/discharge/slip`, `discharge-slip-${admissionId}.pdf`)

// ── IP billing (billing module owns the endpoints; triggered from here) ──
export const useGenerateIpBill = () =>
  useInvalidating((admissionId: number) => post<{ id: number; billNumber: string }>(`${endpoints.billing.bills}/generate-ip/${admissionId}`), [
    ['billing', 'bills'],
  ])
export const useRefreshBedCharges = () =>
  useInvalidating((billId: number) => post<{ id: number }>(`${endpoints.billing.bills}/${billId}/refresh-bed-charges`), [['billing', 'bills']])

// ── Bed board ────────────────────────────────────────────────────────────
export const useBedBoard = (wardId?: number) =>
  useQuery({ queryKey: ['ipd', 'bed-board', wardId], queryFn: () => get<BedBoardItem[]>(p.bedBoard, { wardId }) })
export const useBedOccupancy = () =>
  useQuery({ queryKey: ['ipd', 'bed-board', 'occupancy'], queryFn: () => get<BedOccupancyDashboard>(`${p.bedBoard}/occupancy`) })
export const useSetBedStatus = () =>
  useInvalidating(
    ({ bedId, status, reason }: { bedId: number; status: BedStatus; reason: string | null }) =>
      api.post(`${p.bedBoard}/${bedId}/status`, { status, reason }),
    [['ipd', 'bed-board']],
  )

// ── IP Report / Ward Occupancy Report ───────────────────────────────────
export interface IpReportFilter {
  mode: IpReportMode
  fromDate?: string | null
  toDate?: string | null
  doctorId?: number | null
  wardId?: number | null
  departmentId?: number | null
  patientCategoryId?: number | null
  referrerId?: number | null
  roomTypeId?: number | null
  status?: AdmissionStatus | null
  unitId?: number | null
  regNo?: string | null
  patientName?: string | null
  ipNo?: string | null
}

export const useIpAdmissionsReport = (filter: IpReportFilter, enabled: boolean) =>
  useQuery({
    queryKey: ['ipd', 'report', 'admissions', filter],
    queryFn: () => get<IpReportResult>(endpoints.ipdReports.admissions, filter),
    enabled,
  })

export async function downloadIpAdmissionsReport(filter: IpReportFilter, format: 'csv' | 'excel' | 'pdf') {
  const params = new URLSearchParams({ mode: filter.mode, format })
  if (filter.fromDate) params.set('fromDate', filter.fromDate)
  if (filter.toDate) params.set('toDate', filter.toDate)
  if (filter.doctorId) params.set('doctorId', String(filter.doctorId))
  if (filter.wardId) params.set('wardId', String(filter.wardId))
  if (filter.departmentId) params.set('departmentId', String(filter.departmentId))
  if (filter.patientCategoryId) params.set('patientCategoryId', String(filter.patientCategoryId))
  if (filter.referrerId) params.set('referrerId', String(filter.referrerId))
  if (filter.roomTypeId) params.set('roomTypeId', String(filter.roomTypeId))
  if (filter.status) params.set('status', String(filter.status))
  if (filter.unitId) params.set('unitId', String(filter.unitId))
  if (filter.regNo) params.set('regNo', filter.regNo)
  if (filter.patientName) params.set('patientName', filter.patientName)
  if (filter.ipNo) params.set('ipNo', filter.ipNo)
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`${endpoints.ipdReports.admissions}/export?${params}`, `ip-report.${extension}`)
}

export interface WardOccupancyReportFilter {
  mode: IpReportMode
  fromDate?: string | null
  toDate?: string | null
  wardId?: number | null
  doctorId?: number | null
  unitId?: number | null
  /** "icu" | "observation" — anything else (or omitted) means no ward-flag filter. */
  wardFlag?: string | null
  bedStatus?: BedStatus | null
}

export const useWardOccupancyReport = (filter: WardOccupancyReportFilter, enabled: boolean) =>
  useQuery({
    queryKey: ['ipd', 'report', 'ward-occupancy', filter],
    queryFn: () => get<IpReportResult>(endpoints.ipdReports.wardOccupancy, filter),
    enabled,
  })

export async function downloadWardOccupancyReport(filter: WardOccupancyReportFilter, format: 'csv' | 'excel' | 'pdf') {
  const params = new URLSearchParams({ mode: filter.mode, format })
  if (filter.fromDate) params.set('fromDate', filter.fromDate)
  if (filter.toDate) params.set('toDate', filter.toDate)
  if (filter.wardId) params.set('wardId', String(filter.wardId))
  if (filter.doctorId) params.set('doctorId', String(filter.doctorId))
  if (filter.unitId) params.set('unitId', String(filter.unitId))
  if (filter.wardFlag) params.set('wardFlag', filter.wardFlag)
  if (filter.bedStatus) params.set('bedStatus', String(filter.bedStatus))
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`${endpoints.ipdReports.wardOccupancy}/export?${params}`, `ward-occupancy-report.${extension}`)
}

// ── Doctor Admission Request Report ──────────────────────────────────────
export interface AdmissionRequestConversionReportFilter {
  fromDate?: string | null
  toDate?: string | null
  departmentId?: number | null
  doctorId?: number | null
  advisedDoctorId?: number | null
  wardId?: number | null
  roomTypeId?: number | null
  patientCategoryId?: number | null
  status?: AdmissionRequestStatus | null
  regNo?: string | null
  patientName?: string | null
}

export const useAdmissionRequestConversionReport = (filter: AdmissionRequestConversionReportFilter, enabled: boolean) =>
  useQuery({
    queryKey: ['ipd', 'report', 'admission-request-conversion', filter],
    queryFn: () => get<IpReportResult>(endpoints.ipdReports.admissionRequestConversion, filter),
    enabled,
  })

export async function downloadAdmissionRequestConversionReport(
  filter: AdmissionRequestConversionReportFilter,
  format: 'csv' | 'excel' | 'pdf',
) {
  const params = new URLSearchParams({ format })
  if (filter.fromDate) params.set('fromDate', filter.fromDate)
  if (filter.toDate) params.set('toDate', filter.toDate)
  if (filter.departmentId) params.set('departmentId', String(filter.departmentId))
  if (filter.doctorId) params.set('doctorId', String(filter.doctorId))
  if (filter.advisedDoctorId) params.set('advisedDoctorId', String(filter.advisedDoctorId))
  if (filter.wardId) params.set('wardId', String(filter.wardId))
  if (filter.roomTypeId) params.set('roomTypeId', String(filter.roomTypeId))
  if (filter.patientCategoryId) params.set('patientCategoryId', String(filter.patientCategoryId))
  if (filter.status) params.set('status', String(filter.status))
  if (filter.regNo) params.set('regNo', filter.regNo)
  if (filter.patientName) params.set('patientName', filter.patientName)
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`${endpoints.ipdReports.admissionRequestConversion}/export?${params}`, `doctor-admission-request-report.${extension}`)
}
