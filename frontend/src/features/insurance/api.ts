import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse } from '../../api/types/api'
import type {
  Claim,
  ClaimDocument,
  DecideEnhancementPayload,
  DecidePreAuthPayload,
  PreAuthEnhancement,
  PreAuthRequest,
  RaisePreAuthPayload,
  RecordClaimSettlementPayload,
  RequestEnhancementPayload,
  SubmitClaimPayload,
  UpdateClaimStatusPayload,
} from './types'
import type { PatientInsurancePolicy } from '../patients/types'

const ins = endpoints.insurance

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

// ── Patient policies (read-only lookup — captured inline on Patient registration) ──
export const usePatientInsurancePolicies = (patientId: number | null) =>
  useQuery({
    queryKey: ['insurance', 'policies', patientId],
    queryFn: () => get<PatientInsurancePolicy[]>(ins.patientPolicies(patientId!)),
    enabled: patientId != null,
  })

// ── Pre-authorization requests ───────────────────────────────────────────
export const usePendingPreAuthRequests = () =>
  useQuery({ queryKey: ['insurance', 'pre-auth'], queryFn: () => get<PreAuthRequest[]>(`${ins.preAuthRequests}/pending`) })

/** Approved/PartiallyApproved pre-auths — the pool an "enhancement" can be requested against (see GetApprovedAsync). */
export const useApprovedPreAuthRequests = () =>
  useQuery({
    queryKey: ['insurance', 'pre-auth', 'approved'],
    queryFn: () => get<PreAuthRequest[]>(`${ins.preAuthRequests}/approved`),
  })

export const usePreAuthRequest = (id: number | null) =>
  useQuery({
    queryKey: ['insurance', 'pre-auth', id],
    queryFn: () => get<PreAuthRequest>(`${ins.preAuthRequests}/${id}`),
    enabled: id != null,
  })

export const useRaisePreAuth = () =>
  useInvalidating((payload: RaisePreAuthPayload) => api.post(ins.preAuthRequests, payload), [['insurance', 'pre-auth']])

export const useDecidePreAuth = () =>
  useInvalidating(
    ({ id, ...payload }: DecidePreAuthPayload & { id: number }) =>
      api.post(`${ins.preAuthRequests}/${id}/decide`, payload),
    [['insurance', 'pre-auth']],
  )

export const usePreAuthEnhancements = (preAuthId: number | null) =>
  useQuery({
    queryKey: ['insurance', 'pre-auth', preAuthId, 'enhancements'],
    queryFn: () => get<PreAuthEnhancement[]>(`${ins.preAuthRequests}/${preAuthId}/enhancements`),
    enabled: preAuthId != null,
  })

export const useRequestEnhancement = (preAuthId: number) =>
  useInvalidating(
    (payload: RequestEnhancementPayload) => api.post(`${ins.preAuthRequests}/${preAuthId}/enhancements`, payload),
    [['insurance', 'pre-auth', String(preAuthId), 'enhancements']],
  )

export const useDecideEnhancement = () =>
  useInvalidating(
    ({ enhancementId, ...payload }: DecideEnhancementPayload & { enhancementId: number }) =>
      api.post(`${ins.preAuthRequests}/enhancements/${enhancementId}/decide`, payload),
    [['insurance', 'pre-auth']],
  )

// ── Claims ────────────────────────────────────────────────────────────────
export const usePendingClaims = () =>
  useQuery({ queryKey: ['insurance', 'claims'], queryFn: () => get<Claim[]>(`${ins.claims}/pending`) })

export const useClaim = (id: number | null) =>
  useQuery({
    queryKey: ['insurance', 'claims', id],
    queryFn: () => get<Claim>(`${ins.claims}/${id}`),
    enabled: id != null,
  })

export const useSubmitClaim = () =>
  useInvalidating((payload: SubmitClaimPayload) => api.post(ins.claims, payload), [['insurance', 'claims']])

export const useUpdateClaimStatus = () =>
  useInvalidating(
    ({ id, ...payload }: UpdateClaimStatusPayload & { id: number }) => api.post(`${ins.claims}/${id}/status`, payload),
    [['insurance', 'claims']],
  )

export const useSettleClaim = () =>
  useInvalidating(
    ({ id, ...payload }: RecordClaimSettlementPayload & { id: number }) => api.post(`${ins.claims}/${id}/settle`, payload),
    [['insurance', 'claims']],
  )

export const useClaimDocuments = (claimId: number | null) =>
  useQuery({
    queryKey: ['insurance', 'claims', claimId, 'documents'],
    queryFn: () => get<ClaimDocument[]>(`${ins.claims}/${claimId}/documents`),
    enabled: claimId != null,
  })

export const useAddClaimDocument = (claimId: number) =>
  useInvalidating(
    ({ documentTypeId, file }: { documentTypeId: number; file: File }) => {
      const form = new FormData()
      form.append('documentTypeId', String(documentTypeId))
      form.append('file', file)
      return api.post(`${ins.claims}/${claimId}/documents`, form, { headers: { 'Content-Type': 'multipart/form-data' } })
    },
    [['insurance', 'claims', String(claimId), 'documents']],
  )
