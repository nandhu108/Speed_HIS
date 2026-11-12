import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse } from '../../api/types/api'
import type {
  AddAllergyPayload,
  AddClinicalNotePayload,
  AddFamilyHistoryPayload,
  AddImmunizationPayload,
  AddMedicalHistoryPayload,
  AddProblemPayload,
  AddSurgicalHistoryPayload,
  AllergyEntry,
  AllergyMatch,
  AttachmentEntry,
  ClinicalNoteEntry,
  DischargeSummaryArchiveEntry,
  FamilyHistoryEntry,
  ImmunizationEntry,
  InvestigationHistoryEntry,
  MedicalHistoryEntry,
  PrescriptionHistoryEntry,
  ProblemListEntry,
  ProblemStatus,
  ProcedureHistoryEntry,
  ResultsTrendSeries,
  SurgicalHistoryEntry,
  TimelineEvent,
} from './types'

const p = endpoints.emr.patients

async function get<T>(url: string, params?: object): Promise<T> {
  const { data } = await api.get<ApiResponse<T>>(url, { params })
  return data.data!
}

async function post<T>(url: string, payload?: object): Promise<T> {
  const { data } = await api.post<ApiResponse<T>>(url, payload)
  return data.data!
}

function useInvalidating<TArgs, TResult = unknown>(fn: (args: TArgs) => Promise<TResult>, keys: string[][]) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () => keys.forEach((key) => queryClient.invalidateQueries({ queryKey: key })),
  })
}

// ── Histories ────────────────────────────────────────────────────────────
export const useMedicalHistory = (patientId: number) =>
  useQuery({ queryKey: ['emr', 'patients', patientId, 'medical-history'], queryFn: () => get<MedicalHistoryEntry[]>(`${p}/${patientId}/medical-history`) })
export const useAddMedicalHistory = (patientId: number) =>
  useInvalidating((payload: AddMedicalHistoryPayload) => post<number>(`${p}/${patientId}/medical-history`, payload), [
    ['emr', 'patients', String(patientId), 'medical-history'],
  ])

export const useSurgicalHistory = (patientId: number) =>
  useQuery({ queryKey: ['emr', 'patients', patientId, 'surgical-history'], queryFn: () => get<SurgicalHistoryEntry[]>(`${p}/${patientId}/surgical-history`) })
export const useAddSurgicalHistory = (patientId: number) =>
  useInvalidating((payload: AddSurgicalHistoryPayload) => post<number>(`${p}/${patientId}/surgical-history`, payload), [
    ['emr', 'patients', String(patientId), 'surgical-history'],
  ])

export const useFamilyHistory = (patientId: number) =>
  useQuery({ queryKey: ['emr', 'patients', patientId, 'family-history'], queryFn: () => get<FamilyHistoryEntry[]>(`${p}/${patientId}/family-history`) })
export const useAddFamilyHistory = (patientId: number) =>
  useInvalidating((payload: AddFamilyHistoryPayload) => post<number>(`${p}/${patientId}/family-history`, payload), [
    ['emr', 'patients', String(patientId), 'family-history'],
  ])

export const useImmunizations = (patientId: number) =>
  useQuery({ queryKey: ['emr', 'patients', patientId, 'immunizations'], queryFn: () => get<ImmunizationEntry[]>(`${p}/${patientId}/immunizations`) })
export const useAddImmunization = (patientId: number) =>
  useInvalidating((payload: AddImmunizationPayload) => post<number>(`${p}/${patientId}/immunizations`, payload), [
    ['emr', 'patients', String(patientId), 'immunizations'],
  ])

export const useAllergies = (patientId: number, activeOnly = false) =>
  useQuery({
    queryKey: ['emr', 'patients', patientId, 'allergies', activeOnly],
    queryFn: () => get<AllergyEntry[]>(`${p}/${patientId}/allergies`, { activeOnly }),
  })
export const useAddAllergy = (patientId: number) =>
  useInvalidating((payload: AddAllergyPayload) => post<number>(`${p}/${patientId}/allergies`, payload), [
    ['emr', 'patients', String(patientId), 'allergies'],
  ])
export const useDeactivateAllergy = (patientId: number) =>
  useInvalidating((allergyId: number) => api.post(`${p}/${patientId}/allergies/${allergyId}/deactivate`), [
    ['emr', 'patients', String(patientId), 'allergies'],
  ])

/** The "prescription warning" hook — advisory cross-check against the patient's active drug allergies. */
export const useAllergyCheck = (patientId: number, drugNames: string[]) =>
  useQuery({
    queryKey: ['emr', 'patients', patientId, 'allergy-check', drugNames],
    queryFn: () => get<AllergyMatch[]>(`${p}/${patientId}/allergy-check`, { drugNames }),
    enabled: patientId > 0 && drugNames.length > 0,
  })

// ── Problem list ─────────────────────────────────────────────────────────
export const useProblemList = (patientId: number, status?: ProblemStatus) =>
  useQuery({
    queryKey: ['emr', 'patients', patientId, 'problem-list', status],
    queryFn: () => get<ProblemListEntry[]>(`${p}/${patientId}/problem-list`, { status }),
  })
export const useAddProblem = (patientId: number) =>
  useInvalidating((payload: AddProblemPayload) => post<number>(`${p}/${patientId}/problem-list`, payload), [
    ['emr', 'patients', String(patientId), 'problem-list'],
  ])
export const useResolveProblem = (patientId: number) =>
  useInvalidating((problemId: number) => api.post(`${p}/${patientId}/problem-list/${problemId}/resolve`), [
    ['emr', 'patients', String(patientId), 'problem-list'],
  ])

// ── Clinical notes ───────────────────────────────────────────────────────
export const useClinicalNotes = (patientId: number) =>
  useQuery({ queryKey: ['emr', 'patients', patientId, 'clinical-notes'], queryFn: () => get<ClinicalNoteEntry[]>(`${p}/${patientId}/clinical-notes`) })
export const useAddClinicalNote = (patientId: number) =>
  useInvalidating((payload: AddClinicalNotePayload) => post<number>(`${p}/${patientId}/clinical-notes`, payload), [
    ['emr', 'patients', String(patientId), 'clinical-notes'],
  ])

// ── Attachments ──────────────────────────────────────────────────────────
export const useAttachments = (patientId: number) =>
  useQuery({ queryKey: ['emr', 'patients', patientId, 'attachments'], queryFn: () => get<AttachmentEntry[]>(`${p}/${patientId}/attachments`) })
export const useUploadAttachment = (patientId: number) =>
  useInvalidating(
    ({ file, attachmentType, description }: { file: File; attachmentType: string; description: string | null }) => {
      const form = new FormData()
      form.append('file', file)
      form.append('attachmentType', attachmentType)
      if (description) form.append('description', description)
      return api.post<ApiResponse<number>>(`${p}/${patientId}/attachments`, form, { headers: { 'Content-Type': 'multipart/form-data' } })
    },
    [['emr', 'patients', String(patientId), 'attachments']],
  )
export async function downloadAttachment(patientId: number, attachmentId: number, fileName: string) {
  const { data } = await api.get(`${p}/${patientId}/attachments/${attachmentId}/download`, { responseType: 'blob' })
  const url = URL.createObjectURL(data as Blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  anchor.click()
  URL.revokeObjectURL(url)
}

// ── Timeline & history views ─────────────────────────────────────────────
export const useTimeline = (patientId: number | null) =>
  useQuery({
    queryKey: ['emr', 'patients', patientId, 'timeline'],
    queryFn: () => get<TimelineEvent[]>(`${p}/${patientId}/timeline`),
    enabled: patientId !== null,
  })
export const usePrescriptionHistory = (patientId: number | null) =>
  useQuery({
    queryKey: ['emr', 'patients', patientId, 'prescriptions'],
    queryFn: () => get<PrescriptionHistoryEntry[]>(`${p}/${patientId}/prescriptions`),
    enabled: patientId !== null,
  })
export const useInvestigationHistory = (patientId: number | null) =>
  useQuery({
    queryKey: ['emr', 'patients', patientId, 'investigations'],
    queryFn: () => get<InvestigationHistoryEntry[]>(`${p}/${patientId}/investigations`),
    enabled: patientId !== null,
  })
export const useProcedureHistory = (patientId: number | null) =>
  useQuery({
    queryKey: ['emr', 'patients', patientId, 'procedures'],
    queryFn: () => get<ProcedureHistoryEntry[]>(`${p}/${patientId}/procedures`),
    enabled: patientId !== null,
  })
export const useDischargeSummaryArchive = (patientId: number | null) =>
  useQuery({
    queryKey: ['emr', 'patients', patientId, 'discharge-summaries'],
    queryFn: () => get<DischargeSummaryArchiveEntry[]>(`${p}/${patientId}/discharge-summaries`),
    enabled: patientId !== null,
  })
export const useResultsTrend = (patientId: number | null, testId: number | null) =>
  useQuery({
    queryKey: ['emr', 'patients', patientId, 'results-trend', testId],
    queryFn: () => get<ResultsTrendSeries[]>(`${p}/${patientId}/results-trend`, { testId }),
    enabled: patientId !== null && testId !== null,
  })
