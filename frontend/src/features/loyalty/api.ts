import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse, PagedQuery, PagedResult } from '../../api/types/api'
import type {
  AddDependentPayload,
  LoyaltyCardDetail,
  LoyaltyCardListItem,
  LoyaltyCardStatus,
  RegisterLoyaltyCardPayload,
  RenewLoyaltyCardPayload,
} from './types'

const p = endpoints.loyaltyCards

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

const cardKeys = [['loyalty', 'cards']]
const detailKeys = (id: number): (string | number)[][] => [['loyalty', 'cards', 'detail', id], ['loyalty', 'cards']]

export interface LoyaltyCardFilter extends PagedQuery {
  patientId?: number
  status?: LoyaltyCardStatus
}

export const useLoyaltyCards = (query: LoyaltyCardFilter) =>
  useQuery({
    queryKey: ['loyalty', 'cards', 'list', query],
    queryFn: () => get<PagedResult<LoyaltyCardListItem>>(p, query),
  })

export const useLoyaltyCard = (id: number | null) =>
  useQuery({
    queryKey: ['loyalty', 'cards', 'detail', id],
    queryFn: () => get<LoyaltyCardDetail>(`${p}/${id}`),
    enabled: id !== null,
  })

export const useRegisterLoyaltyCard = () =>
  useInvalidating(async (payload: RegisterLoyaltyCardPayload) => {
    const { data } = await api.post<ApiResponse<LoyaltyCardDetail>>(p, payload)
    return data.data!
  }, cardKeys)

export const useRenewLoyaltyCard = (id: number) =>
  useInvalidating((payload: RenewLoyaltyCardPayload) => api.post(`${p}/${id}/renew`, payload), detailKeys(id))

export const useAddDependent = (id: number) =>
  useInvalidating((payload: AddDependentPayload) => api.post(`${p}/${id}/members`, payload), detailKeys(id))

export const useRemoveDependent = (id: number) =>
  useInvalidating((memberId: number) => api.delete(`${p}/${id}/members/${memberId}`), detailKeys(id))

export const useDuplicateIssue = (id: number) =>
  useInvalidating(() => api.post(`${p}/${id}/duplicate`), detailKeys(id))

export const useDeactivateLoyaltyCard = (id: number) =>
  useInvalidating((reason: string) => api.post(`${p}/${id}/deactivate`, { reason }), detailKeys(id))

async function fetchCardBlob(id: number): Promise<Blob> {
  const { data } = await api.get(`${p}/${id}/card`, { responseType: 'blob' })
  return data as Blob
}

/** Preview — opens the card PDF in a new tab, same pattern as every other slip/summary PDF. */
export async function openLoyaltyCardPdf(id: number) {
  const blob = await fetchCardBlob(id)
  const url = URL.createObjectURL(blob)
  window.open(url, '_blank', 'noopener,noreferrer')
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

/** Direct — fetches the same PDF, then triggers the browser print dialog immediately via a hidden iframe. */
export async function printLoyaltyCardDirect(id: number) {
  const blob = await fetchCardBlob(id)
  const url = URL.createObjectURL(blob)
  const iframe = document.createElement('iframe')
  iframe.style.display = 'none'
  iframe.src = url
  document.body.appendChild(iframe)
  iframe.onload = () => {
    iframe.contentWindow?.print()
    setTimeout(() => {
      document.body.removeChild(iframe)
      URL.revokeObjectURL(url)
    }, 60_000)
  }
}
