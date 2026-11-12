import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { ApiResponse } from '../../../api/types/api'
import { portalApi } from '../../api/portalAxios'

export interface FeedbackDto {
  id: number
  opVisitId: number | null
  admissionId: number | null
  rating: number
  comments: string | null
  submittedAt: string
}

export interface SubmitFeedbackPayload {
  opVisitId: number | null
  admissionId: number | null
  rating: number
  comments: string | null
}

async function get<T>(url: string): Promise<T> {
  const { data } = await portalApi.get<ApiResponse<T>>(url)
  return data.data!
}

export const useMyFeedback = () => useQuery({ queryKey: ['portal', 'feedback'], queryFn: () => get<FeedbackDto[]>('/portal/feedback') })

export const useSubmitFeedback = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: SubmitFeedbackPayload) => portalApi.post('/portal/feedback', payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['portal', 'feedback'] }),
  })
}
