import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useState } from 'react'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse, PagedQuery, PagedResult } from '../../api/types/api'
import { downloadFile } from '../../lib/downloadFile'
import type {
  CheckDuplicatesPayload,
  ConsentEntry,
  DocumentType,
  DuplicateCandidate,
  PatientDetail,
  PatientDocumentEntry,
  PatientListItem,
  PatientReportResult,
  RegisterPatientPayload,
  RegisterPatientResult,
  UpdatePatientPayload,
} from './types'

const p = endpoints.patients

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

export const usePatients = (query: PagedQuery, options?: { enabled?: boolean }) =>
  useQuery({
    queryKey: ['patients', 'list', query],
    queryFn: () => get<PagedResult<PatientListItem>>(p, query),
    enabled: options?.enabled,
  })

export const usePatient = (id: number | null) =>
  useQuery({
    queryKey: ['patients', 'detail', id],
    queryFn: () => get<PatientDetail>(`${p}/${id}`),
    enabled: id !== null,
  })

export const useRegisterPatient = () =>
  useInvalidating(async (payload: RegisterPatientPayload) => {
    const { data } = await api.post<ApiResponse<RegisterPatientResult>>(p, payload)
    return data.data!
  }, [['patients', 'list']])

export const useUpdatePatient = () =>
  useInvalidating(
    ({ id, ...payload }: UpdatePatientPayload & { id: number }) => api.put(`${p}/${id}`, payload),
    [['patients', 'list'], ['patients', 'detail']],
  )

export const useCheckDuplicates = () =>
  useMutation({
    mutationFn: async (payload: CheckDuplicatesPayload) => {
      const { data } = await api.post<ApiResponse<DuplicateCandidate[]>>(`${p}/duplicates/check`, payload)
      return data.data ?? []
    },
  })

export const usePatientDuplicates = (id: number | null) =>
  useQuery({
    queryKey: ['patients', 'duplicates', id],
    queryFn: () => get<DuplicateCandidate[]>(`${p}/${id}/duplicates`),
    enabled: id !== null,
  })

export const useMergePatient = () =>
  useInvalidating(
    ({ survivorId, mergedPatientId, reason }: { survivorId: number; mergedPatientId: number; reason: string }) =>
      api.post(`${p}/${survivorId}/merge`, { mergedPatientId, reason }),
    [['patients', 'list'], ['patients', 'detail']],
  )

export const useBlockPatient = () =>
  useInvalidating(
    ({ patientId, reason }: { patientId: number; reason: string }) => api.post(`${p}/${patientId}/block`, { reason }),
    [['patients', 'list'], ['patients', 'detail']],
  )

export const useUnblockPatient = () =>
  useInvalidating(
    (patientId: number) => api.post(`${p}/${patientId}/unblock`),
    [['patients', 'list'], ['patients', 'detail']],
  )

export const useUploadPatientPhoto = () =>
  useInvalidating(
    ({ patientId, file }: { patientId: number; file: File }) => {
      const form = new FormData()
      form.append('file', file)
      return api.post(`${p}/${patientId}/photo`, form, { headers: { 'Content-Type': 'multipart/form-data' } })
    },
    [['patients', 'detail']],
  )

export const usePatientDocuments = (patientId: number | null) =>
  useQuery({
    queryKey: ['patients', 'documents', patientId],
    queryFn: () => get<PatientDocumentEntry[]>(`${p}/${patientId}/documents`),
    enabled: patientId !== null,
  })

export const useAddPatientDocument = () =>
  useInvalidating(
    ({
      patientId,
      file,
      documentType,
      description,
    }: {
      patientId: number
      file: File
      documentType: DocumentType
      description: string | null
    }) => {
      const form = new FormData()
      form.append('file', file)
      form.append('documentType', String(documentType))
      if (description) form.append('description', description)
      return api.post(`${p}/${patientId}/documents`, form, { headers: { 'Content-Type': 'multipart/form-data' } })
    },
    [['patients', 'documents']],
  )

export const usePatientConsents = (patientId: number | null) =>
  useQuery({
    queryKey: ['patients', 'consents', patientId],
    queryFn: () => get<ConsentEntry[]>(`${p}/${patientId}/consents`),
    enabled: patientId !== null,
  })

export const useCaptureConsent = () =>
  useInvalidating(
    ({
      patientId,
      consentTemplateId,
      signedByName,
      relationshipToPatient,
      scan,
    }: {
      patientId: number
      consentTemplateId: number
      signedByName: string
      relationshipToPatient: string | null
      scan: File | null
    }) => {
      const form = new FormData()
      form.append('consentTemplateId', String(consentTemplateId))
      form.append('signedByName', signedByName)
      if (relationshipToPatient) form.append('relationshipToPatient', relationshipToPatient)
      if (scan) form.append('scan', scan)
      return api.post(`${p}/${patientId}/consents`, form, { headers: { 'Content-Type': 'multipart/form-data' } })
    },
    [['patients', 'consents']],
  )

/**
 * Loads the patient's photo as an object URL — <img src> can't carry the bearer
 * token, so the bytes are fetched via axios and swapped in once available.
 *
 * Only re-fetches when `patientId`/`hasPhoto` change, which misses the common case of
 * re-uploading a new photo over an existing one (`hasPhoto` stays `true` throughout, so nothing
 * in the dependency array moves). The returned `refresh()` lets a caller force a re-fetch after
 * it knows an upload just completed — see `PatientDetailPage`'s `onUploaded` callback.
 */
export function usePatientPhotoUrl(patientId: number | null, hasPhoto: boolean) {
  const [url, setUrl] = useState<string | null>(null)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    if (!patientId || !hasPhoto) {
      setUrl(null)
      return
    }
    let objectUrl: string | null = null
    let cancelled = false

    fetchBlob(`${p}/${patientId}/photo`)
      .then((blob) => {
        if (cancelled) return
        objectUrl = URL.createObjectURL(blob)
        setUrl(objectUrl)
      })
      .catch(() => setUrl(null))

    return () => {
      cancelled = true
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [patientId, hasPhoto, version])

  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  return [url, refresh] as const
}

/** Fetches a permission-gated binary endpoint as a blob (auth header required, so no plain <a href>). */
async function fetchBlob(url: string): Promise<Blob> {
  const { data } = await api.get(url, { responseType: 'blob' })
  return data as Blob
}

export async function downloadPatientDocument(patientId: number, documentId: number, fileName: string) {
  const blob = await fetchBlob(`${p}/${patientId}/documents/${documentId}/download`)
  triggerDownload(blob, fileName)
}

export async function openRegistrationCard(patientId: number) {
  const blob = await fetchBlob(`${p}/${patientId}/card`)
  const url = URL.createObjectURL(blob)
  window.open(url, '_blank', 'noopener,noreferrer')
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

function triggerDownload(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  anchor.click()
  URL.revokeObjectURL(url)
}

// ── Block/Unblock Report ─────────────────────────────────────────────────
export interface BlockUnblockReportFilter {
  fromDate?: string | null
  toDate?: string | null
}

export const useBlockUnblockReport = (filter: BlockUnblockReportFilter, enabled: boolean) =>
  useQuery({
    queryKey: ['patients', 'report', 'block-unblock', filter],
    queryFn: () => get<PatientReportResult>(endpoints.patientReports.blockUnblock, filter),
    enabled,
  })

export async function downloadBlockUnblockReport(filter: BlockUnblockReportFilter, format: 'csv' | 'excel' | 'pdf') {
  const params = new URLSearchParams({ format })
  if (filter.fromDate) params.set('fromDate', filter.fromDate)
  if (filter.toDate) params.set('toDate', filter.toDate)
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`${endpoints.patientReports.blockUnblock}/export?${params}`, `patient-block-unblock-report.${extension}`)
}

// ── ABDM / ABHA (Wave-I integration, M1) ──────────────────────────────────
import type {
  AbhaProfile,
  AbhaVerificationSession,
  CompleteAbhaPayload,
  InitiateAbhaPayload,
} from './types'

export const useAbhaProfile = (patientId: number | null) =>
  useQuery({
    queryKey: ['patients', 'abha', patientId],
    queryFn: () => get<AbhaProfile | null>(endpoints.patientAbha(patientId!)),
    enabled: !!patientId,
  })

export function useInitiateAbha(patientId: number) {
  return useMutation({
    mutationFn: (payload: InitiateAbhaPayload) =>
      api
        .post<ApiResponse<AbhaVerificationSession>>(`${endpoints.patientAbha(patientId)}/verify/initiate`, payload)
        .then((r) => r.data.data!),
  })
}

export function useCompleteAbha(patientId: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: CompleteAbhaPayload) =>
      api
        .post<ApiResponse<AbhaProfile>>(`${endpoints.patientAbha(patientId)}/verify/complete`, payload)
        .then((r) => r.data.data!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['patients', 'abha', patientId] })
      queryClient.invalidateQueries({ queryKey: ['patients', 'detail', patientId] })
    },
  })
}

export function useScanAndShareAbha(patientId: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (qrPayload: string) =>
      api
        .post<ApiResponse<AbhaProfile>>(`${endpoints.patientAbha(patientId)}/scan-and-share`, { qrPayload })
        .then((r) => r.data.data!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['patients', 'abha', patientId] })
      queryClient.invalidateQueries({ queryKey: ['patients', 'detail', patientId] })
    },
  })
}

export function useUnlinkAbha(patientId: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (reason: string) =>
      api.post(`${endpoints.patientAbha(patientId)}/unlink`, { reason }).then((r) => r.data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['patients', 'abha', patientId] })
      queryClient.invalidateQueries({ queryKey: ['patients', 'detail', patientId] })
    },
  })
}
