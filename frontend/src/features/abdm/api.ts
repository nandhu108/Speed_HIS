import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse } from '../../api/types/api'
import type {
  AbdmAdminStatus,
  AbhaProfile,
  CareContext,
  ConsentRequest,
  DataTransferLog,
  ExternalHealthRecord,
  UpdateAbdmAdminConfigRequest,
} from './types'

// ── Admin Hooks ────────────────────────────────────────────────────────────

export function useAbdmAdminStatus() {
  return useQuery({
    queryKey: ['abdm', 'admin', 'status'],
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<AbdmAdminStatus>>(endpoints.abdm.adminStatus)
      return data.data!
    },
  })
}

export function useUpdateAbdmConfig() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (payload: UpdateAbdmAdminConfigRequest) => {
      const { data } = await api.post<ApiResponse<AbdmAdminStatus>>(endpoints.abdm.adminConfig, payload)
      return data.data!
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['abdm'] })
      queryClient.invalidateQueries({ queryKey: ['settings'] })
    },
  })
}

export function useToggleAbdmMilestone() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ milestone, enabled }: { milestone: 'master' | 'm1' | 'm2' | 'm3'; enabled: boolean }) => {
      const { data } = await api.post<ApiResponse<AbdmAdminStatus>>(endpoints.abdm.adminToggle, {
        milestone,
        enabled,
      })
      return data.data!
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['abdm'] })
      queryClient.invalidateQueries({ queryKey: ['settings'] })
    },
  })
}

export function useTestAbdmConnection() {
  return useMutation({
    mutationFn: async () => {
      const { data } = await api.get<ApiResponse<{ online: boolean; message: string; gatewayUrl: string }>>(
        endpoints.abdm.adminTestConnection
      )
      return data.data!
    },
  })
}

// ── Milestone M1: Scan & Share Hook ────────────────────────────────────────

export function useScanAndShareAbha(patientId: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (qrPayload: string) => {
      const { data } = await api.post<ApiResponse<AbhaProfile>>(
        endpoints.abdm.abhaScanAndShare(patientId),
        { qrPayload }
      )
      return data.data!
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['patients', 'abha', patientId] })
      queryClient.invalidateQueries({ queryKey: ['patients', patientId] })
    },
  })
}

// ── Milestone M2: Health Information Provider (HIP) Hooks ──────────────────

export function useCareContexts(patientId: number) {
  return useQuery({
    queryKey: ['abdm', 'care-contexts', patientId],
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<CareContext[]>>(
        endpoints.abdm.careContexts(patientId)
      )
      return data.data ?? []
    },
    enabled: patientId > 0,
  })
}

export function useSyncCareContexts(patientId: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async () => {
      const { data } = await api.post<ApiResponse<CareContext[]>>(
        endpoints.abdm.syncCareContexts(patientId)
      )
      return data.data ?? []
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['abdm', 'care-contexts', patientId] })
    },
  })
}

export function useLinkCareContexts(patientId: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (payload: { careContextIds?: number[]; linkAllUnlinked?: boolean }) => {
      const { data } = await api.post<ApiResponse<CareContext[]>>(
        endpoints.abdm.linkCareContexts(patientId),
        payload
      )
      return data.data ?? []
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['abdm', 'care-contexts', patientId] })
      queryClient.invalidateQueries({ queryKey: ['abdm', 'admin'] })
    },
  })
}

export function useDataTransfers() {
  return useQuery({
    queryKey: ['abdm', 'transfers'],
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<DataTransferLog[]>>(endpoints.abdm.transfers)
      return data.data ?? []
    },
  })
}

// ── Milestone M3: Health Information User (HIU) Hooks ──────────────────────

export function useConsentRequests(patientId: number) {
  return useQuery({
    queryKey: ['abdm', 'consent-requests', patientId],
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<ConsentRequest[]>>(
        endpoints.abdm.patientConsentRequests(patientId)
      )
      return data.data ?? []
    },
    enabled: patientId > 0,
  })
}

export function useCreateConsentRequest(patientId: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (payload: {
      patientId: number
      purpose: string
      hiTypes: number[]
      dateFrom: string
      dateTo: string
      expiryAt: string
    }) => {
      const { data } = await api.post<ApiResponse<ConsentRequest>>(
        endpoints.abdm.createConsentRequest,
        payload
      )
      return data.data!
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['abdm', 'consent-requests', patientId] })
      queryClient.invalidateQueries({ queryKey: ['abdm', 'admin'] })
    },
  })
}

export function useCheckConsentStatus(patientId: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (consentRequestId: number) => {
      const { data } = await api.post<ApiResponse<ConsentRequest>>(
        endpoints.abdm.checkConsentStatus(consentRequestId)
      )
      return data.data!
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['abdm', 'consent-requests', patientId] })
    },
  })
}

export function useFetchExternalRecords(patientId: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (consentRequestId: number) => {
      const { data } = await api.post<ApiResponse<ExternalHealthRecord[]>>(
        endpoints.abdm.fetchRecords(consentRequestId)
      )
      return data.data ?? []
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['abdm', 'records', patientId] })
      queryClient.invalidateQueries({ queryKey: ['abdm', 'consent-requests', patientId] })
      queryClient.invalidateQueries({ queryKey: ['abdm', 'admin'] })
    },
  })
}

export function useExternalHealthRecords(patientId: number) {
  return useQuery({
    queryKey: ['abdm', 'records', patientId],
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<ExternalHealthRecord[]>>(
        endpoints.abdm.records(patientId)
      )
      return data.data ?? []
    },
    enabled: patientId > 0,
  })
}
