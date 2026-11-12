import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { ApiResponse } from '../../../api/types/api'
import { portalApi } from '../../api/portalAxios'
import { portalAuthStore } from '../../lib/portalAuthStore'
import type { OnlinePaymentRequested, PortalBill, PortalDischargeSummary, PortalLabOrder, PortalMedicalHistory, PortalPrescription, PortalRadOrderItem } from './types'

async function get<T>(url: string): Promise<T> {
  const { data } = await portalApi.get<ApiResponse<T>>(url)
  return data.data!
}

export const useLabOrders = () => useQuery({ queryKey: ['portal', 'lab-reports'], queryFn: () => get<PortalLabOrder[]>('/portal/lab-reports') })
export const useRadOrders = () => useQuery({ queryKey: ['portal', 'rad-reports'], queryFn: () => get<PortalRadOrderItem[]>('/portal/rad-reports') })
export const usePrescriptions = () => useQuery({ queryKey: ['portal', 'prescriptions'], queryFn: () => get<PortalPrescription[]>('/portal/prescriptions') })
export const useBills = () => useQuery({ queryKey: ['portal', 'bills'], queryFn: () => get<PortalBill[]>('/portal/bills') })
export const useDischargeSummaries = () =>
  useQuery({ queryKey: ['portal', 'discharge-summaries'], queryFn: () => get<PortalDischargeSummary[]>('/portal/discharge-summaries') })
export const useMedicalHistory = () =>
  useQuery({ queryKey: ['portal', 'medical-history'], queryFn: () => get<PortalMedicalHistory>('/portal/medical-history') })

/** Payment placeholder (plan §21: "gateway held") — queues a follow-up request, no gateway is wired up. */
export const useRequestOnlinePayment = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (billId: number) => {
      const { data } = await portalApi.post<ApiResponse<OnlinePaymentRequested>>(`/portal/bills/${billId}/pay-online`)
      return data.data!
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['portal', 'bills'] }),
  })
}

/** PDF downloads need the bearer header, so a plain <a href> won't work — fetch as a blob and open it. */
export async function downloadPortalPdf(url: string, fileName: string) {
  const response = await fetch(`${portalApi.defaults.baseURL}${url}`, {
    headers: { Authorization: `Bearer ${portalAuthStore.getAccessToken() ?? ''}` },
  })
  if (!response.ok) throw new Error('Download failed')
  const blob = await response.blob()
  const link = document.createElement('a')
  link.href = URL.createObjectURL(blob)
  link.download = fileName
  link.click()
  URL.revokeObjectURL(link.href)
}
