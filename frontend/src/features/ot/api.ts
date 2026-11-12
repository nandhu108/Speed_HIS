import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse } from '../../api/types/api'
import type {
  AddTeamMemberPayload,
  BookingDetail,
  BookingFilter,
  BookingListItem,
  Checklist,
  ConsumableUsage,
  CreateBookingPayload,
  Equipment,
  ImplantUsage,
  IntraOp,
  OperativeNote,
  OtRegisterRow,
  PostOpNote,
  PreAnaesthesia,
  RecordConsumableUsagePayload,
  RecordImplantUsagePayload,
  SaveEquipmentPayload,
  SaveIntraOpPayload,
  SaveOperativeNotePayload,
  SavePostOpNotePayload,
  SavePreAnaesthesiaPayload,
  SaveTheatrePayload,
  Theatre,
  UpdateSignInPayload,
  UpdateSignOutPayload,
  UpdateTimeOutPayload,
} from './types'

const p = endpoints.ot

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

// ── Theatres / equipment ─────────────────────────────────────────────────
export const useTheatres = () => useQuery({ queryKey: ['ot', 'theatres'], queryFn: () => get<Theatre[]>(p.theatres) })
export const useSaveTheatre = () =>
  useInvalidating(
    ({ id, ...payload }: SaveTheatrePayload & { id?: number }) =>
      id ? api.put(`${p.theatres}/${id}`, payload) : api.post(p.theatres, payload),
    [['ot', 'theatres']],
  )

export const useEquipment = (theatreId?: number) =>
  useQuery({ queryKey: ['ot', 'equipment', theatreId], queryFn: () => get<Equipment[]>(p.equipment, { theatreId }) })
export const useSaveEquipment = () =>
  useInvalidating(
    ({ id, ...payload }: SaveEquipmentPayload & { id?: number }) =>
      id ? api.put(`${p.equipment}/${id}`, payload) : api.post(p.equipment, payload),
    [['ot', 'equipment']],
  )

// ── Bookings ─────────────────────────────────────────────────────────────
export const useBookings = (filter: BookingFilter) =>
  useQuery({ queryKey: ['ot', 'bookings', filter], queryFn: () => get<BookingListItem[]>(p.bookings, filter) })
export const useBooking = (id: number | null) =>
  useQuery({
    queryKey: ['ot', 'bookings', 'detail', id],
    queryFn: () => get<BookingDetail>(`${p.bookings}/${id}`),
    enabled: id !== null,
  })
export const useCreateBooking = () =>
  useInvalidating(async (payload: CreateBookingPayload) => {
    const { data } = await api.post<ApiResponse<number>>(p.bookings, payload)
    return data.data!
  }, [['ot', 'bookings']])

const bookingKeys = (id: number) => [['ot', 'bookings'], ['ot', 'bookings', 'detail', String(id)]]

export const useRescheduleBooking = (id: number) =>
  useInvalidating((payload: { scheduledStart: string; scheduledEnd: string }) => api.post(`${p.bookings}/${id}/reschedule`, payload), bookingKeys(id))
export const useUpdateBookingPatient = (id: number) =>
  useInvalidating((admissionId: number) => api.post(`${p.bookings}/${id}/update-patient`, { admissionId }), bookingKeys(id))
export const useCancelBooking = (id: number) =>
  useInvalidating((reason: string) => api.post(`${p.bookings}/${id}/cancel`, { reason }), bookingKeys(id))
export const usePostponeBooking = (id: number) =>
  useInvalidating((reason: string) => api.post(`${p.bookings}/${id}/postpone`, { reason }), bookingKeys(id))
export const useVerifyConsent = (id: number) =>
  useInvalidating((patientConsentId: number) => api.post(`${p.bookings}/${id}/verify-consent`, { patientConsentId }), bookingKeys(id))
export const useStartSurgery = (id: number) => useInvalidating(() => api.post(`${p.bookings}/${id}/start`), bookingKeys(id))
export const useCompleteSurgery = (id: number) => useInvalidating(() => api.post(`${p.bookings}/${id}/complete`), bookingKeys(id))
export const useAddTeamMember = (id: number) =>
  useInvalidating((payload: AddTeamMemberPayload) => api.post(`${p.bookings}/${id}/team`, payload), bookingKeys(id))
export const useRemoveTeamMember = (id: number) =>
  useInvalidating((memberId: number) => api.delete(`${p.bookings}/${id}/team/${memberId}`), bookingKeys(id))

export const useOtRegister = (fromDate: string, toDate: string, enabled: boolean) =>
  useQuery({
    queryKey: ['ot', 'register', fromDate, toDate],
    queryFn: () => get<OtRegisterRow[]>(`${p.bookings}/register`, { fromDate, toDate }),
    enabled,
  })

// ── Checklist ────────────────────────────────────────────────────────────
export const useChecklist = (bookingId: number | null) =>
  useQuery({
    queryKey: ['ot', 'bookings', 'checklist', bookingId],
    queryFn: () => get<Checklist>(`${p.bookings}/${bookingId}/checklist`),
    enabled: bookingId !== null,
  })
const checklistKeys = (id: number) => [['ot', 'bookings', 'checklist', String(id)], ['ot', 'bookings', 'detail', String(id)]]
export const useUpdateSignIn = (bookingId: number) =>
  useInvalidating((payload: UpdateSignInPayload) => api.put(`${p.bookings}/${bookingId}/checklist/sign-in`, payload), checklistKeys(bookingId))
export const useUpdateTimeOut = (bookingId: number) =>
  useInvalidating((payload: UpdateTimeOutPayload) => api.put(`${p.bookings}/${bookingId}/checklist/time-out`, payload), checklistKeys(bookingId))
export const useUpdateSignOut = (bookingId: number) =>
  useInvalidating((payload: UpdateSignOutPayload) => api.put(`${p.bookings}/${bookingId}/checklist/sign-out`, payload), checklistKeys(bookingId))

// ── Anaesthesia / operative / post-op notes ─────────────────────────────
export const usePreAnaesthesia = (bookingId: number | null) =>
  useQuery({
    queryKey: ['ot', 'bookings', 'pre-anaesthesia', bookingId],
    queryFn: () => get<PreAnaesthesia>(`${p.bookings}/${bookingId}/pre-anaesthesia`),
    enabled: bookingId !== null,
    retry: false,
  })
export const useSavePreAnaesthesia = (bookingId: number) =>
  useInvalidating((payload: SavePreAnaesthesiaPayload) => api.put(`${p.bookings}/${bookingId}/pre-anaesthesia`, payload), [
    ['ot', 'bookings', 'pre-anaesthesia', String(bookingId)],
  ])

export const useIntraOp = (bookingId: number | null) =>
  useQuery({
    queryKey: ['ot', 'bookings', 'intra-op', bookingId],
    queryFn: () => get<IntraOp>(`${p.bookings}/${bookingId}/intra-op`),
    enabled: bookingId !== null,
    retry: false,
  })
export const useSaveIntraOp = (bookingId: number) =>
  useInvalidating((payload: SaveIntraOpPayload) => api.put(`${p.bookings}/${bookingId}/intra-op`, payload), [
    ['ot', 'bookings', 'intra-op', String(bookingId)],
  ])

export const useOperativeNote = (bookingId: number | null) =>
  useQuery({
    queryKey: ['ot', 'bookings', 'operative-note', bookingId],
    queryFn: () => get<OperativeNote>(`${p.bookings}/${bookingId}/operative-note`),
    enabled: bookingId !== null,
    retry: false,
  })
export const useSaveOperativeNote = (bookingId: number) =>
  useInvalidating((payload: SaveOperativeNotePayload) => api.put(`${p.bookings}/${bookingId}/operative-note`, payload), [
    ['ot', 'bookings', 'operative-note', String(bookingId)],
  ])

export const usePostOpNote = (bookingId: number | null) =>
  useQuery({
    queryKey: ['ot', 'bookings', 'post-op-note', bookingId],
    queryFn: () => get<PostOpNote>(`${p.bookings}/${bookingId}/post-op-note`),
    enabled: bookingId !== null,
    retry: false,
  })
export const useSavePostOpNote = (bookingId: number) =>
  useInvalidating((payload: SavePostOpNotePayload) => api.put(`${p.bookings}/${bookingId}/post-op-note`, payload), [
    ['ot', 'bookings', 'post-op-note', String(bookingId)],
  ])

// ── Usage ────────────────────────────────────────────────────────────────
export const useImplantUsage = (bookingId: number) =>
  useQuery({ queryKey: ['ot', 'bookings', 'implants', bookingId], queryFn: () => get<ImplantUsage[]>(`${p.bookings}/${bookingId}/usage/implants`) })
export const useRecordImplantUsage = (bookingId: number) =>
  useInvalidating((payload: RecordImplantUsagePayload) => api.post(`${p.bookings}/${bookingId}/usage/implants`, payload), [
    ['ot', 'bookings', 'implants', String(bookingId)],
  ])

export const useConsumableUsage = (bookingId: number) =>
  useQuery({
    queryKey: ['ot', 'bookings', 'consumables', bookingId],
    queryFn: () => get<ConsumableUsage[]>(`${p.bookings}/${bookingId}/usage/consumables`),
  })
export const useRecordConsumableUsage = (bookingId: number) =>
  useInvalidating((payload: RecordConsumableUsagePayload) => api.post(`${p.bookings}/${bookingId}/usage/consumables`, payload), [
    ['ot', 'bookings', 'consumables', String(bookingId)],
  ])
