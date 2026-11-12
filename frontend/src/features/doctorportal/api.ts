import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse } from '../../api/types/api'
import type { AdmissionListItem } from '../ipd/types'
import type { LabOrderListItem } from '../lab/types'
import type { QueueEntry } from '../opd/types'
import type { DoctorDashboard, OpinionRequest, PendingDischargeSummary } from './types'

const e = endpoints.doctorPortal

async function get<T>(url: string, params?: object): Promise<T> {
  const { data } = await api.get<ApiResponse<T>>(url, { params })
  return data.data!
}

export const useDoctorDashboard = () =>
  useQuery({ queryKey: ['doctor-portal', 'dashboard'], queryFn: () => get<DoctorDashboard>(e.dashboard), refetchInterval: 30_000 })

export const useMyQueue = (date: string) =>
  useQuery({ queryKey: ['doctor-portal', 'queue', date], queryFn: () => get<QueueEntry[]>(e.queue, { date }), refetchInterval: 15_000 })

export const useMyInpatients = () =>
  useQuery({ queryKey: ['doctor-portal', 'inpatients'], queryFn: () => get<AdmissionListItem[]>(e.inpatients) })

export const useMyOrders = () =>
  useQuery({ queryKey: ['doctor-portal', 'orders'], queryFn: () => get<LabOrderListItem[]>(e.orders) })

export const useMyPendingDischargeSummaries = () =>
  useQuery({ queryKey: ['doctor-portal', 'discharge-approvals'], queryFn: () => get<PendingDischargeSummary[]>(e.dischargeApprovals) })

export const useApproveMyDischargeSummary = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (admissionId: number) => api.post(`${e.dischargeApprovals}/${admissionId}/approve`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['doctor-portal', 'discharge-approvals'] })
      queryClient.invalidateQueries({ queryKey: ['doctor-portal', 'dashboard'] })
    },
  })
}

export const useMyOpinionRequests = () =>
  useQuery({ queryKey: ['doctor-portal', 'opinion-requests'], queryFn: () => get<OpinionRequest[]>(e.opinionRequests) })

export const useSaveMyOpinion = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ referralId, opinionNotes }: { referralId: number; opinionNotes: string }) =>
      api.put(`${e.opinionRequests}/${referralId}`, { opinionNotes }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['doctor-portal', 'opinion-requests'] }),
  })
}
