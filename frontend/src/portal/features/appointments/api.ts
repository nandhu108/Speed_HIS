import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { ApiResponse } from '../../../api/types/api'
import { portalApi } from '../../api/portalAxios'
import type { BookAppointmentPayload, PagedResult, PortalAppointment, PortalSlot } from './types'

async function get<T>(url: string, params?: object): Promise<T> {
  const { data } = await portalApi.get<ApiResponse<T>>(url, { params })
  return data.data!
}

export const useSlots = (doctorId: number | null, date: string | null) =>
  useQuery({
    queryKey: ['portal', 'slots', doctorId, date],
    queryFn: () => get<PortalSlot[]>('/portal/appointments/slots', { doctorId, date }),
    enabled: doctorId !== null && date !== null,
  })

export const useMyAppointments = () =>
  useQuery({
    queryKey: ['portal', 'appointments'],
    queryFn: () => get<PagedResult<PortalAppointment>>('/portal/appointments', { page: 1, pageSize: 50 }),
  })

export const useBookAppointment = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (payload: BookAppointmentPayload) => {
      const { data } = await portalApi.post<ApiResponse<PortalAppointment>>('/portal/appointments', payload)
      return data.data!
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['portal', 'appointments'] }),
  })
}

export interface PortalBranch { id: number; code: string; name: string }
export interface PortalDoctor { id: number; fullName: string; specialtyName: string | null; consultationFee: number }

export const useBranches = () => useQuery({ queryKey: ['portal', 'branches'], queryFn: () => get<PortalBranch[]>('/portal/branches') })
export const useDoctors = () =>
  useQuery({ queryKey: ['portal', 'doctors'], queryFn: () => get<{ items: PortalDoctor[] }>('/portal/doctors') })

export const useCancelAppointment = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, reason }: { id: number; reason: string }) => portalApi.post(`/portal/appointments/${id}/cancel`, { reason }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['portal', 'appointments'] }),
  })
}
