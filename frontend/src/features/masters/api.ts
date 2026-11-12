import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import { downloadFile } from '../../lib/downloadFile'
import type { ApiResponse, PagedQuery, PagedResult } from '../../api/types/api'
import type {
  Area,
  BedItem,
  BillEstimateTemplateDetail,
  BillEstimateTemplateListItem,
  Building,
  ConsentTemplate,
  Corporate,
  DiscountType,
  DoctorDetail,
  DoctorListItem,
  DoctorPayoutRule,
  Department,
  Drug,
  Employee,
  Floor,
  AppointmentType,
  IcdCode,
  IcdImportResult,
  InsuranceCompany,
  Item,
  LoyaltyCardPriceDetail,
  LoyaltyCardPriceListItem,
  LoyaltyCardType,
  MhcPackage,
  PackageDetail,
  PackageListItem,
  PatientCategory,
  Referrer,
  Room,
  RoomType,
  RoomTypeCharge,
  SaveAreaPayload,
  SaveBillEstimateTemplatePayload,
  SaveDoctorPayload,
  SaveDoctorPayoutRulePayload,
  SaveDrugPayload,
  SaveEmployeePayload,
  SaveAppointmentTypePayload,
  SaveItemPayload,
  SaveLoyaltyCardPricePayload,
  SaveLoyaltyCardTypePayload,
  SaveMhcPackagePayload,
  SavePackagePayload,
  SavePatientCategoryPayload,
  SaveDepartmentPayload,
  SaveRoomTypeChargePayload,
  SaveServicePayload,
  SaveTariffPayload,
  Service,
  SimpleMaster,
  TariffDetail,
  TariffImportResult,
  TariffListItem,
  Tax,
  Vendor,
  Ward,
} from './types'

const m = endpoints.masters

async function get<T>(url: string, params?: object): Promise<T> {
  const { data } = await api.get<ApiResponse<T>>(url, { params })
  return data.data!
}

/** Invalidate the given namespaced keys after a successful mutation. */
function useInvalidating<TArgs>(fn: (args: TArgs) => Promise<unknown>, keys: string[][]) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () => keys.forEach((key) => queryClient.invalidateQueries({ queryKey: key })),
  })
}

// ── Simple masters (generic framework) ─────────────────────────────────────
// One hook trio per code+name master; every page reuses the same shape.

export interface SaveSimpleMasterPayload {
  id?: number
  code: string
  name: string
  description: string | null
  isActive: boolean
}

export function useSimpleMaster(key: string, endpoint: string) {
  return useQuery({
    queryKey: ['masters', key],
    queryFn: () => get<SimpleMaster[]>(endpoint),
  })
}

export function useSaveSimpleMaster(key: string, endpoint: string) {
  return useInvalidating(
    ({ id, ...payload }: SaveSimpleMasterPayload) =>
      id ? api.put(`${endpoint}/${id}`, payload) : api.post(endpoint, payload),
    [['masters', key]],
  )
}

export function useDeleteSimpleMaster(key: string, endpoint: string) {
  return useInvalidating((id: number) => api.delete(`${endpoint}/${id}`), [['masters', key]])
}

// ── Doctors ────────────────────────────────────────────────────────────────
export const useDoctors = (query: PagedQuery) =>
  useQuery({
    queryKey: ['masters', 'doctors', query],
    queryFn: () => get<PagedResult<DoctorListItem>>(m.doctors, query),
  })

export const useDoctor = (id: number | null) =>
  useQuery({
    queryKey: ['masters', 'doctors', 'detail', id],
    queryFn: () => get<DoctorDetail>(`${m.doctors}/${id}`),
    enabled: id !== null,
  })

export const useSaveDoctor = () =>
  useInvalidating(
    ({ id, ...payload }: SaveDoctorPayload & { id?: number }) =>
      id ? api.put(`${m.doctors}/${id}`, payload) : api.post(m.doctors, payload),
    [['masters', 'doctors']],
  )

export const useDeleteDoctor = () =>
  useInvalidating((id: number) => api.delete(`${m.doctors}/${id}`), [['masters', 'doctors']])

// ── Employees ──────────────────────────────────────────────────────────────
export const useEmployees = (query: PagedQuery) =>
  useQuery({
    queryKey: ['masters', 'employees', query],
    queryFn: () => get<PagedResult<Employee>>(m.employees, query),
  })

export const useSaveEmployee = () =>
  useInvalidating(
    ({ id, ...payload }: SaveEmployeePayload & { id?: number }) =>
      id ? api.put(`${m.employees}/${id}`, payload) : api.post(m.employees, payload),
    [['masters', 'employees']],
  )

export const useDeleteEmployee = () =>
  useInvalidating((id: number) => api.delete(`${m.employees}/${id}`), [['masters', 'employees']])

// ── Services ───────────────────────────────────────────────────────────────
export const useServices = (query: PagedQuery & { categoryId?: number }) =>
  useQuery({
    queryKey: ['masters', 'services', query],
    queryFn: () => get<PagedResult<Service>>(m.services, query),
  })

export const useSaveService = () =>
  useInvalidating(
    ({ id, ...payload }: SaveServicePayload & { id?: number }) =>
      id ? api.put(`${m.services}/${id}`, payload) : api.post(m.services, payload),
    [['masters', 'services']],
  )

export const useDeleteService = () =>
  useInvalidating((id: number) => api.delete(`${m.services}/${id}`), [['masters', 'services']])

// ── Tariffs ────────────────────────────────────────────────────────────────
export const useTariffs = () =>
  useQuery({ queryKey: ['masters', 'tariffs'], queryFn: () => get<TariffListItem[]>(m.tariffs) })

export const useTariff = (id: number | null) =>
  useQuery({
    queryKey: ['masters', 'tariffs', 'detail', id],
    queryFn: () => get<TariffDetail>(`${m.tariffs}/${id}`),
    enabled: id !== null,
  })

export const useSaveTariff = () =>
  useInvalidating(
    ({ id, ...payload }: SaveTariffPayload & { id?: number }) =>
      id ? api.put(`${m.tariffs}/${id}`, payload) : api.post(m.tariffs, payload),
    [['masters', 'tariffs']],
  )

export const useDeleteTariff = () =>
  useInvalidating((id: number) => api.delete(`${m.tariffs}/${id}`), [['masters', 'tariffs']])

export async function downloadTariffExport(format: 'csv' | 'excel' | 'pdf') {
  const extension = format === 'excel' ? 'xlsx' : format
  await downloadFile(`${m.tariffs}/export?format=${format}`, `tariff-list.${extension}`)
}

export function useImportTariffCsv() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (file: File) => {
      const form = new FormData()
      form.append('file', file)
      const { data } = await api.post<ApiResponse<TariffImportResult>>(`${m.tariffs}/import`, form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      return data.data!
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['masters', 'tariffs'] }),
  })
}

// ── Packages ───────────────────────────────────────────────────────────────
export const usePackages = () =>
  useQuery({ queryKey: ['masters', 'packages'], queryFn: () => get<PackageListItem[]>(m.packages) })

export const usePackage = (id: number | null) =>
  useQuery({
    queryKey: ['masters', 'packages', 'detail', id],
    queryFn: () => get<PackageDetail>(`${m.packages}/${id}`),
    enabled: id !== null,
  })

export const useSavePackage = () =>
  useInvalidating(
    ({ id, ...payload }: SavePackagePayload & { id?: number }) =>
      id ? api.put(`${m.packages}/${id}`, payload) : api.post(m.packages, payload),
    [['masters', 'packages']],
  )

export const useDeletePackage = () =>
  useInvalidating((id: number) => api.delete(`${m.packages}/${id}`), [['masters', 'packages']])

// ── Bill estimate templates ─────────────────────────────────────────────────
export const useBillEstimateTemplates = () =>
  useQuery({
    queryKey: ['masters', 'bill-estimate-templates'],
    queryFn: () => get<BillEstimateTemplateListItem[]>(m.billEstimateTemplates),
  })

export const useBillEstimateTemplate = (id: number | null) =>
  useQuery({
    queryKey: ['masters', 'bill-estimate-templates', 'detail', id],
    queryFn: () => get<BillEstimateTemplateDetail>(`${m.billEstimateTemplates}/${id}`),
    enabled: id !== null,
  })

export const useSaveBillEstimateTemplate = () =>
  useInvalidating(
    ({ id, ...payload }: SaveBillEstimateTemplatePayload & { id?: number }) =>
      id ? api.put(`${m.billEstimateTemplates}/${id}`, payload) : api.post(m.billEstimateTemplates, payload),
    [['masters', 'bill-estimate-templates']],
  )

export const useDeleteBillEstimateTemplate = () =>
  useInvalidating((id: number) => api.delete(`${m.billEstimateTemplates}/${id}`), [['masters', 'bill-estimate-templates']])

// ── Doctor payout rules ──────────────────────────────────────────────────────
export const useDoctorPayoutRules = () =>
  useQuery({ queryKey: ['masters', 'doctor-payout-rules'], queryFn: () => get<DoctorPayoutRule[]>(m.doctorPayoutRules) })

export const useSaveDoctorPayoutRule = () =>
  useInvalidating(
    ({ id, ...payload }: SaveDoctorPayoutRulePayload) =>
      id ? api.put(`${m.doctorPayoutRules}/${id}`, payload) : api.post(m.doctorPayoutRules, payload),
    [['masters', 'doctor-payout-rules']],
  )

export const useDeleteDoctorPayoutRule = () =>
  useInvalidating((id: number) => api.delete(`${m.doctorPayoutRules}/${id}`), [['masters', 'doctor-payout-rules']])

// ── Appointment types ("Appointment Masters") ───────────────────────────────
export const useAppointmentTypes = () =>
  useQuery({ queryKey: ['masters', 'appointment-types'], queryFn: () => get<AppointmentType[]>(m.appointmentTypes) })

export const useSaveAppointmentType = () =>
  useInvalidating(
    ({ id, ...payload }: SaveAppointmentTypePayload) =>
      id ? api.put(`${m.appointmentTypes}/${id}`, payload) : api.post(m.appointmentTypes, payload),
    [['masters', 'appointment-types']],
  )

export const useDeleteAppointmentType = () =>
  useInvalidating((id: number) => api.delete(`${m.appointmentTypes}/${id}`), [['masters', 'appointment-types']])

// ── Departments (bespoke — carries a Clinical / Non-Clinical type) ──────────
// Same cache key the generic useSimpleMaster('departments') uses, so every existing department
// dropdown shares this one fetch (the response is a superset of SimpleMaster).
export const useDepartments = () =>
  useQuery({ queryKey: ['masters', 'departments'], queryFn: () => get<Department[]>(m.departments) })

/** Active Clinical departments only — what OP registration and IP admission must choose from. */
export const useClinicalDepartments = () => {
  const query = useDepartments()
  return { ...query, data: (query.data ?? []).filter((d) => d.isActive && d.type === 1) }
}

export const useSaveDepartment = () =>
  useInvalidating(
    ({ id, ...payload }: SaveDepartmentPayload) =>
      id ? api.put(`${m.departments}/${id}`, payload) : api.post(m.departments, payload),
    [['masters', 'departments']],
  )

export const useDeleteDepartment = () =>
  useInvalidating((id: number) => api.delete(`${m.departments}/${id}`), [['masters', 'departments']])

/**
 * Doctors to offer once a department is picked: those mapped to it, plus any doctor with no
 * department mapping at all (so an unmapped doctor never vanishes from the picker).
 */
export function doctorsForDepartment<T extends { departments: string[] }>(
  doctors: T[], departmentName: string | undefined,
): T[] {
  if (!departmentName) return doctors
  return doctors.filter((d) => d.departments.length === 0 || d.departments.includes(departmentName))
}

// ── Patient categories ("Patient-type colors") ──────────────────────────────
export const usePatientCategories = () =>
  useQuery({ queryKey: ['masters', 'patient-categories'], queryFn: () => get<PatientCategory[]>(m.patientCategories) })

export const useSavePatientCategory = () =>
  useInvalidating(
    ({ id, ...payload }: SavePatientCategoryPayload) =>
      id ? api.put(`${m.patientCategories}/${id}`, payload) : api.post(m.patientCategories, payload),
    [['masters', 'patient-categories']],
  )

export const useDeletePatientCategory = () =>
  useInvalidating((id: number) => api.delete(`${m.patientCategories}/${id}`), [['masters', 'patient-categories']])

// ── Loyalty card types (bespoke — carries MemberLimit + an optional category FK) ──
export const useLoyaltyCardTypes = () =>
  useQuery({ queryKey: ['masters', 'loyalty-card-types'], queryFn: () => get<LoyaltyCardType[]>(m.loyaltyCardTypes) })

export const useSaveLoyaltyCardType = () =>
  useInvalidating(
    ({ id, ...payload }: SaveLoyaltyCardTypePayload) =>
      id ? api.put(`${m.loyaltyCardTypes}/${id}`, payload) : api.post(m.loyaltyCardTypes, payload),
    [['masters', 'loyalty-card-types']],
  )

export const useDeleteLoyaltyCardType = () =>
  useInvalidating((id: number) => api.delete(`${m.loyaltyCardTypes}/${id}`), [['masters', 'loyalty-card-types']])

// ── Loyalty card prices (the "Card Price Matrix" — header+child, mirrors Bill Estimate Templates) ──
export const useLoyaltyCardPrices = () =>
  useQuery({
    queryKey: ['masters', 'loyalty-card-prices'],
    queryFn: () => get<LoyaltyCardPriceListItem[]>(m.loyaltyCardPrices),
  })

export const useLoyaltyCardPrice = (id: number | null) =>
  useQuery({
    queryKey: ['masters', 'loyalty-card-prices', 'detail', id],
    queryFn: () => get<LoyaltyCardPriceDetail>(`${m.loyaltyCardPrices}/${id}`),
    enabled: id !== null,
  })

export const useSaveLoyaltyCardPrice = () =>
  useInvalidating(
    ({ id, ...payload }: SaveLoyaltyCardPricePayload & { id?: number }) =>
      id ? api.put(`${m.loyaltyCardPrices}/${id}`, payload) : api.post(m.loyaltyCardPrices, payload),
    [['masters', 'loyalty-card-prices']],
  )

export const useDeleteLoyaltyCardPrice = () =>
  useInvalidating((id: number) => api.delete(`${m.loyaltyCardPrices}/${id}`), [['masters', 'loyalty-card-prices']])

// ── Locations (building → floor → ward → room → bed + room types) ──────────
type LocationLevel = 'buildings' | 'floors' | 'room-types' | 'wards' | 'rooms' | 'beds'

function useLocationList<T>(level: LocationLevel) {
  return useQuery({
    queryKey: ['masters', 'locations', level],
    queryFn: () => get<T[]>(`${m.locations}/${level}`),
  })
}

const allLocationKeys = [['masters', 'locations']]

function useSaveLocation<TPayload extends { id?: number }>(level: LocationLevel) {
  return useInvalidating(({ id, ...payload }: TPayload) => {
    return id
      ? api.put(`${m.locations}/${level}/${id}`, payload)
      : api.post(`${m.locations}/${level}`, payload)
  }, allLocationKeys)
}

function useDeleteLocation(level: LocationLevel) {
  return useInvalidating((id: number) => api.delete(`${m.locations}/${level}/${id}`), allLocationKeys)
}

export const useBuildings = () => useLocationList<Building>('buildings')
export const useFloors = () => useLocationList<Floor>('floors')
export const useRoomTypes = () => useLocationList<RoomType>('room-types')

// ── Room type charges (date-effective bed-charge revisions) ────────────────
export const useRoomTypeCharges = (roomTypeId: number | null) =>
  useQuery({
    queryKey: ['masters', 'locations', 'room-types', String(roomTypeId), 'charges'],
    queryFn: () => get<RoomTypeCharge[]>(`${m.locations}/room-types/${roomTypeId}/charges`),
    enabled: roomTypeId !== null,
  })

export const useAddRoomTypeCharge = (roomTypeId: number) =>
  useInvalidating(
    (payload: SaveRoomTypeChargePayload) => api.post(`${m.locations}/room-types/${roomTypeId}/charges`, payload),
    [['masters', 'locations', 'room-types', String(roomTypeId), 'charges']],
  )

export const useDeleteRoomTypeCharge = (roomTypeId: number) =>
  useInvalidating(
    (id: number) => api.delete(`${m.locations}/room-types/${roomTypeId}/charges/${id}`),
    [['masters', 'locations', 'room-types', String(roomTypeId), 'charges']],
  )
export const useWards = () => useLocationList<Ward>('wards')
export const useRooms = () => useLocationList<Room>('rooms')
export const useBeds = () => useLocationList<BedItem>('beds')

export const useSaveBuilding = () =>
  useSaveLocation<{ id?: number; code: string; name: string; isActive: boolean }>('buildings')
export const useSaveFloor = () =>
  useSaveLocation<{ id?: number; code: string; name: string; buildingId: number; isActive: boolean }>('floors')
export const useSaveRoomType = () =>
  useSaveLocation<{ id?: number; code: string; name: string; bedChargePerDay: number; isActive: boolean }>(
    'room-types',
  )
export const useSaveWard = () =>
  useSaveLocation<{
    id?: number
    code: string
    name: string
    floorId: number
    isIcu: boolean
    isObservation: boolean
    isActive: boolean
  }>('wards')
export const useSaveRoom = () =>
  useSaveLocation<{
    id?: number
    code: string
    name: string
    wardId: number
    roomTypeId: number
    isActive: boolean
  }>('rooms')
export const useSaveBed = () =>
  useSaveLocation<{ id?: number; code: string; roomId: number; isActive: boolean }>('beds')

export const useDeleteBuilding = () => useDeleteLocation('buildings')
export const useDeleteFloor = () => useDeleteLocation('floors')
export const useDeleteRoomType = () => useDeleteLocation('room-types')
export const useDeleteWard = () => useDeleteLocation('wards')
export const useDeleteRoom = () => useDeleteLocation('rooms')
export const useDeleteBed = () => useDeleteLocation('beds')

// ── Vendors ────────────────────────────────────────────────────────────────
export const useVendors = (query: PagedQuery) =>
  useQuery({
    queryKey: ['masters', 'vendors', query],
    queryFn: () => get<PagedResult<Vendor>>(m.vendors, query),
  })

export type SaveVendorPayload = Omit<Vendor, 'id'> & { id?: number }

export const useSaveVendor = () =>
  useInvalidating(
    ({ id, ...payload }: SaveVendorPayload) =>
      id ? api.put(`${m.vendors}/${id}`, payload) : api.post(m.vendors, payload),
    [['masters', 'vendors']],
  )

export const useDeleteVendor = () =>
  useInvalidating((id: number) => api.delete(`${m.vendors}/${id}`), [['masters', 'vendors']])

// ── Items ──────────────────────────────────────────────────────────────────
export const useItems = (query: PagedQuery) =>
  useQuery({
    queryKey: ['masters', 'items', query],
    queryFn: () => get<PagedResult<Item>>(m.items, query),
  })

export const useSaveItem = () =>
  useInvalidating(
    ({ id, ...payload }: SaveItemPayload & { id?: number }) =>
      id ? api.put(`${m.items}/${id}`, payload) : api.post(m.items, payload),
    [['masters', 'items']],
  )

export const useDeleteItem = () =>
  useInvalidating((id: number) => api.delete(`${m.items}/${id}`), [['masters', 'items']])

// ── Drugs ──────────────────────────────────────────────────────────────────
export const useDrugs = (query: PagedQuery) =>
  useQuery({
    queryKey: ['masters', 'drugs', query],
    queryFn: () => get<PagedResult<Drug>>(m.drugs, query),
  })

export const useSaveDrug = () =>
  useInvalidating(
    ({ id, ...payload }: SaveDrugPayload & { id?: number }) =>
      id ? api.put(`${m.drugs}/${id}`, payload) : api.post(m.drugs, payload),
    [['masters', 'drugs']],
  )

export const useDeleteDrug = () =>
  useInvalidating((id: number) => api.delete(`${m.drugs}/${id}`), [['masters', 'drugs']])

// ── Taxes ──────────────────────────────────────────────────────────────────
export const useTaxes = () =>
  useQuery({ queryKey: ['masters', 'taxes'], queryFn: () => get<Tax[]>(m.taxes) })

export const useSaveTax = () =>
  useInvalidating(
    ({ id, ...payload }: { id?: number; code: string; name: string; ratePercent: number; isActive: boolean }) =>
      id ? api.put(`${m.taxes}/${id}`, payload) : api.post(m.taxes, payload),
    [['masters', 'taxes']],
  )

export const useDeleteTax = () =>
  useInvalidating((id: number) => api.delete(`${m.taxes}/${id}`), [['masters', 'taxes']])

// ── Discount types ─────────────────────────────────────────────────────────
export const useDiscountTypes = () =>
  useQuery({ queryKey: ['masters', 'discount-types'], queryFn: () => get<DiscountType[]>(m.discountTypes) })

export const useSaveDiscountType = () =>
  useInvalidating(
    ({
      id,
      ...payload
    }: {
      id?: number
      code: string
      name: string
      maxPercent: number
      approvalThresholdPercent: number
      isActive: boolean
    }) => (id ? api.put(`${m.discountTypes}/${id}`, payload) : api.post(m.discountTypes, payload)),
    [['masters', 'discount-types']],
  )

export const useDeleteDiscountType = () =>
  useInvalidating((id: number) => api.delete(`${m.discountTypes}/${id}`), [['masters', 'discount-types']])

// ── Referrers ──────────────────────────────────────────────────────────────
export const useReferrers = (query: PagedQuery) =>
  useQuery({
    queryKey: ['masters', 'referrers', query],
    queryFn: () => get<PagedResult<Referrer>>(m.referrers, query),
  })

export type SaveReferrerPayload = Omit<Referrer, 'id' | 'doctorName'> & { id?: number }

export const useSaveReferrer = () =>
  useInvalidating(
    ({ id, ...payload }: SaveReferrerPayload) =>
      id ? api.put(`${m.referrers}/${id}`, payload) : api.post(m.referrers, payload),
    [['masters', 'referrers']],
  )

export const useDeleteReferrer = () =>
  useInvalidating((id: number) => api.delete(`${m.referrers}/${id}`), [['masters', 'referrers']])

// ── ICD-10 codes ───────────────────────────────────────────────────────────
export const useIcdCodes = (query: PagedQuery) =>
  useQuery({
    queryKey: ['masters', 'icd-codes', query],
    queryFn: () => get<PagedResult<IcdCode>>(m.icdCodes, query),
  })

export const useSaveIcdCode = () =>
  useInvalidating(
    ({
      id,
      ...payload
    }: {
      id?: number
      code: string
      description: string
      category: string | null
      isActive: boolean
    }) => (id ? api.put(`${m.icdCodes}/${id}`, payload) : api.post(m.icdCodes, payload)),
    [['masters', 'icd-codes']],
  )

export const useDeleteIcdCode = () =>
  useInvalidating((id: number) => api.delete(`${m.icdCodes}/${id}`), [['masters', 'icd-codes']])

export function useImportIcdCsv() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (file: File) => {
      const form = new FormData()
      form.append('file', file)
      const { data } = await api.post<ApiResponse<IcdImportResult>>(`${m.icdCodes}/import`, form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      return data.data!
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['masters', 'icd-codes'] }),
  })
}

// ── Corporates ─────────────────────────────────────────────────────────────
export const useCorporates = () =>
  useQuery({ queryKey: ['masters', 'corporates'], queryFn: () => get<Corporate[]>(m.corporates) })

export type SaveCorporatePayload = Omit<Corporate, 'id'> & { id?: number }

export const useSaveCorporate = () =>
  useInvalidating(
    ({ id, ...payload }: SaveCorporatePayload) =>
      id ? api.put(`${m.corporates}/${id}`, payload) : api.post(m.corporates, payload),
    [['masters', 'corporates']],
  )

export const useDeleteCorporate = () =>
  useInvalidating((id: number) => api.delete(`${m.corporates}/${id}`), [['masters', 'corporates']])

// ── Insurance / TPA ────────────────────────────────────────────────────────
export const useInsuranceCompanies = () =>
  useQuery({
    queryKey: ['masters', 'insurance-companies'],
    queryFn: () => get<InsuranceCompany[]>(m.insuranceCompanies),
  })

export type SaveInsuranceCompanyPayload = Omit<InsuranceCompany, 'id'> & { id?: number }

export const useSaveInsuranceCompany = () =>
  useInvalidating(
    ({ id, ...payload }: SaveInsuranceCompanyPayload) =>
      id ? api.put(`${m.insuranceCompanies}/${id}`, payload) : api.post(m.insuranceCompanies, payload),
    [['masters', 'insurance-companies']],
  )

export const useDeleteInsuranceCompany = () =>
  useInvalidating((id: number) => api.delete(`${m.insuranceCompanies}/${id}`), [['masters', 'insurance-companies']])

// ── Consent templates ──────────────────────────────────────────────────────
export const useConsentTemplates = () =>
  useQuery({
    queryKey: ['masters', 'consent-templates'],
    queryFn: () => get<ConsentTemplate[]>(m.consentTemplates),
  })

export type SaveConsentTemplatePayload = Omit<ConsentTemplate, 'id'> & { id?: number }

export const useSaveConsentTemplate = () =>
  useInvalidating(
    ({ id, ...payload }: SaveConsentTemplatePayload) =>
      id ? api.put(`${m.consentTemplates}/${id}`, payload) : api.post(m.consentTemplates, payload),
    [['masters', 'consent-templates']],
  )

export const useDeleteConsentTemplate = () =>
  useInvalidating((id: number) => api.delete(`${m.consentTemplates}/${id}`), [['masters', 'consent-templates']])

// ── Areas (City → Area, bespoke two-level lookup) ──────────────────────────
export const useAreas = (cityId?: number) =>
  useQuery({
    queryKey: ['masters', 'areas', cityId ?? null],
    queryFn: () => get<Area[]>(m.areas, cityId ? { cityId } : undefined),
  })

export const useSaveArea = () =>
  useInvalidating(
    ({ id, ...payload }: SaveAreaPayload) =>
      id ? api.put(`${m.areas}/${id}`, payload) : api.post(m.areas, payload),
    [['masters', 'areas']],
  )

export const useDeleteArea = () =>
  useInvalidating((id: number) => api.delete(`${m.areas}/${id}`), [['masters', 'areas']])

// ── MHC Packages (bespoke — carries Price beyond code/name/description) ────
export const useMhcPackages = (search?: string) =>
  useQuery({
    queryKey: ['masters', 'mhc-packages', search ?? null],
    queryFn: () => get<MhcPackage[]>(m.mhcPackages, search ? { search } : undefined),
  })

export const useSaveMhcPackage = () =>
  useInvalidating(
    ({ id, ...payload }: SaveMhcPackagePayload) =>
      id ? api.put(`${m.mhcPackages}/${id}`, payload) : api.post(m.mhcPackages, payload),
    [['masters', 'mhc-packages']],
  )

export const useDeleteMhcPackage = () =>
  useInvalidating((id: number) => api.delete(`${m.mhcPackages}/${id}`), [['masters', 'mhc-packages']])
