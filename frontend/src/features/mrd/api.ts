import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse } from '../../api/types/api'
import type {
  CodingStatsDto,
  CodingWorklistItem,
  CreateFileRequestPayload,
  DestructionRegisterEntryDto,
  DestructionStatus,
  EligibleFileDto,
  FileAuditDto,
  FileMovementDto,
  FileMovementReportRow,
  FileRequestDto,
  FileRequestStatus,
  IncompleteRecordRow,
  RecordFileDetail,
  RecordFileListItem,
  RecordFileStatus,
  RecordMovementPayload,
  RetentionPolicyDto,
  SaveFileAuditPayload,
  SaveRetentionPolicyPayload,
  ScannedDocumentDto,
} from './types'

const p = endpoints.mrd

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

// ── Record files / movements ────────────────────────────────────────────────
export const useRecordFiles = (filter: { status?: RecordFileStatus; search?: string; admissionId?: number }) =>
  useQuery({ queryKey: ['mrd', 'files', filter], queryFn: () => get<RecordFileListItem[]>(p.files, filter) })

export const useRecordFile = (id: number | null) =>
  useQuery({
    queryKey: ['mrd', 'files', 'detail', id],
    queryFn: () => get<RecordFileDetail>(`${p.files}/${id}`),
    enabled: id !== null,
  })

export const useCreateRecordFile = () =>
  useInvalidating(async (admissionId: number) => {
    const { data } = await api.post<ApiResponse<number>>(p.files, { admissionId })
    return data.data!
  }, [['mrd', 'files'], ['mrd', 'admissions']])

export const useFileMovements = (recordFileId: number | null) =>
  useQuery({
    queryKey: ['mrd', 'files', 'movements', recordFileId],
    queryFn: () => get<FileMovementDto[]>(`${p.files}/${recordFileId}/movements`),
    enabled: recordFileId !== null,
  })

export const useRecordMovement = (recordFileId: number) =>
  useInvalidating(
    (payload: RecordMovementPayload) => api.post(`${p.files}/${recordFileId}/movements`, payload),
    [['mrd', 'files', 'movements', String(recordFileId)], ['mrd', 'files', 'detail', String(recordFileId)]],
  )

// ── File requests ────────────────────────────────────────────────────────────
export const useFileRequests = (filter: { status?: FileRequestStatus; overdueOnly?: boolean }) =>
  useQuery({ queryKey: ['mrd', 'requests', filter], queryFn: () => get<FileRequestDto[]>(p.requests, filter) })

export const useFileRequestsForFile = (recordFileId: number | null) =>
  useQuery({
    queryKey: ['mrd', 'requests', 'by-file', recordFileId],
    queryFn: () => get<FileRequestDto[]>(p.requests, { recordFileId }),
    enabled: recordFileId !== null,
  })

const requestKeys = [['mrd', 'requests'], ['mrd', 'files']]

export const useCreateFileRequest = () =>
  useInvalidating(async (payload: CreateFileRequestPayload) => {
    const { data } = await api.post<ApiResponse<number>>(p.requests, payload)
    return data.data!
  }, requestKeys)

export const useApproveFileRequest = () => useInvalidating((id: number) => api.post(`${p.requests}/${id}/approve`), requestKeys)
export const useRejectFileRequest = () =>
  useInvalidating(({ id, reason }: { id: number; reason: string }) => api.post(`${p.requests}/${id}/reject`, { reason }), requestKeys)
export const useIssueFileRequest = () => useInvalidating((id: number) => api.post(`${p.requests}/${id}/issue`), requestKeys)
export const useReturnFileRequest = () => useInvalidating((id: number) => api.post(`${p.requests}/${id}/return`), requestKeys)

// ── Scanned documents ────────────────────────────────────────────────────────
export const useScannedDocuments = (admissionId: number | null) =>
  useQuery({
    queryKey: ['mrd', 'documents', admissionId],
    queryFn: () => get<ScannedDocumentDto[]>(`${p.admissions}/${admissionId}/documents`),
    enabled: admissionId !== null,
  })

export const useUploadScannedDocument = (admissionId: number) => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ documentTypeId, description, file }: { documentTypeId: number; description: string | null; file: File }) => {
      const form = new FormData()
      form.append('documentTypeId', String(documentTypeId))
      if (description) form.append('description', description)
      form.append('file', file)
      const { data } = await api.post<ApiResponse<number>>(`${p.admissions}/${admissionId}/documents`, form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      return data.data!
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['mrd', 'documents', admissionId] }),
  })
}

export const downloadScannedDocumentUrl = (admissionId: number, documentId: number) =>
  `${p.admissions}/${admissionId}/documents/${documentId}/download`

// ── Coding worklist ──────────────────────────────────────────────────────────
export const useCodingWorklist = (onlyPending: boolean) =>
  useQuery({ queryKey: ['mrd', 'coding-worklist', onlyPending], queryFn: () => get<CodingWorklistItem[]>(p.codingWorklist, { onlyPending }) })

export const useMarkCoded = () =>
  useInvalidating(
    ({ admissionId, notes }: { admissionId: number; notes: string | null }) =>
      api.post(`${p.codingWorklist}/${admissionId}/code`, { notes }),
    [['mrd', 'coding-worklist']],
  )

// ── File audit ───────────────────────────────────────────────────────────────
export const useFileAudit = (admissionId: number | null) =>
  useQuery({
    queryKey: ['mrd', 'file-audit', admissionId],
    queryFn: () => get<FileAuditDto>(`${p.admissions}/${admissionId}/file-audit`),
    enabled: admissionId !== null,
    retry: false,
  })

export const useSaveFileAudit = (admissionId: number) =>
  useInvalidating(
    (payload: SaveFileAuditPayload) => api.put(`${p.admissions}/${admissionId}/file-audit`, payload),
    [['mrd', 'file-audit', String(admissionId)], ['mrd', 'reports']],
  )

// ── Retention policies ───────────────────────────────────────────────────────
export const useRetentionPolicies = () =>
  useQuery({ queryKey: ['mrd', 'retention-policies'], queryFn: () => get<RetentionPolicyDto[]>(p.retentionPolicies) })

export const useSaveRetentionPolicy = () =>
  useInvalidating(
    ({ id, ...payload }: SaveRetentionPolicyPayload & { id?: number }) =>
      id ? api.put(`${p.retentionPolicies}/${id}`, payload) : api.post(p.retentionPolicies, payload),
    [['mrd', 'retention-policies']],
  )

export const useDeleteRetentionPolicy = () => useInvalidating((id: number) => api.delete(`${p.retentionPolicies}/${id}`), [['mrd', 'retention-policies']])

// ── Destruction register ─────────────────────────────────────────────────────
export const useDestructionRegister = (status?: DestructionStatus) =>
  useQuery({ queryKey: ['mrd', 'destruction-register', status], queryFn: () => get<DestructionRegisterEntryDto[]>(p.destructionRegister, { status }) })

export const useEligibleFiles = (retentionPolicyId: number | null) =>
  useQuery({
    queryKey: ['mrd', 'destruction-register', 'eligible', retentionPolicyId],
    queryFn: () => get<EligibleFileDto[]>(`${p.destructionRegister}/eligible`, { retentionPolicyId }),
    enabled: retentionPolicyId !== null,
  })

const destructionKeys = [['mrd', 'destruction-register']]

export const useFlagForDestruction = () =>
  useInvalidating(
    (payload: { recordFileId: number; retentionPolicyId: number }) => api.post(p.destructionRegister, payload),
    destructionKeys,
  )

export const useApproveDestruction = () => useInvalidating((id: number) => api.post(`${p.destructionRegister}/${id}/approve`), destructionKeys)
export const useDestroyRecordFile = () =>
  useInvalidating(
    ({ id, destructionMethod, notes }: { id: number; destructionMethod: string; notes: string | null }) =>
      api.post(`${p.destructionRegister}/${id}/destroy`, { destructionMethod, notes }),
    [...destructionKeys, ['mrd', 'files']],
  )

// ── Reports ───────────────────────────────────────────────────────────────────
export const useCodingStats = (range: { fromDate?: string; toDate?: string }) =>
  useQuery({ queryKey: ['mrd', 'reports', 'coding-stats', range], queryFn: () => get<CodingStatsDto>(`${p.reports}/coding-stats`, range) })

export const useFileMovementReport = (range: { fromDate?: string; toDate?: string }) =>
  useQuery({ queryKey: ['mrd', 'reports', 'file-movement', range], queryFn: () => get<FileMovementReportRow[]>(`${p.reports}/file-movement`, range) })

export const useIncompleteRecords = (range: { fromDate?: string; toDate?: string }) =>
  useQuery({ queryKey: ['mrd', 'reports', 'incomplete-records', range], queryFn: () => get<IncompleteRecordRow[]>(`${p.reports}/incomplete-records`, range) })
