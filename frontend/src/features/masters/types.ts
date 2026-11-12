/** DTO types mirroring the backend Masters feature (Phase 2). */

export interface SimpleMaster {
  id: number
  code: string
  name: string
  description: string | null
  isActive: boolean
}

export interface DoctorListItem {
  id: number
  userId: number
  fullName: string
  registrationNo: string
  specialtyName: string | null
  departments: string[]
  consultationFee: number
  followUpFee: number
  isActive: boolean
}

export interface DoctorDetail {
  id: number
  userId: number
  fullName: string
  registrationNo: string
  specialtyId: number | null
  qualification: string | null
  consultationFee: number
  followUpFee: number
  followUpWindowDays: number
  defaultSlotMinutes: number
  departmentIds: number[]
  isActive: boolean
}

export interface SaveDoctorPayload {
  userId: number
  registrationNo: string
  specialtyId: number | null
  qualification: string | null
  consultationFee: number
  followUpFee: number
  followUpWindowDays: number
  defaultSlotMinutes: number
  departmentIds: number[]
  isActive: boolean
}

export interface Employee {
  id: number
  code: string
  fullName: string
  phone: string | null
  email: string | null
  designationId: number | null
  designationName: string | null
  departmentId: number | null
  departmentName: string | null
  joinedOn: string | null
  isActive: boolean
}

export interface SaveEmployeePayload {
  code: string
  fullName: string
  phone: string | null
  email: string | null
  designationId: number | null
  departmentId: number | null
  joinedOn: string | null
  isActive: boolean
}

export interface Service {
  id: number
  code: string
  name: string
  serviceCategoryId: number
  categoryName: string
  departmentId: number | null
  departmentName: string | null
  isTaxable: boolean
  taxId: number | null
  taxName: string | null
  defaultRate: number
  isActive: boolean
}

export interface SaveServicePayload {
  code: string
  name: string
  serviceCategoryId: number
  departmentId: number | null
  isTaxable: boolean
  taxId: number | null
  defaultRate: number
  isActive: boolean
}

export interface TariffListItem {
  id: number
  code: string
  name: string
  patientCategoryId: number | null
  patientCategoryName: string | null
  effectiveFrom: string
  effectiveTo: string | null
  itemCount: number
  isActive: boolean
}

export interface TariffItem {
  id: number
  serviceId: number
  serviceCode: string
  serviceName: string
  /** Null prices the service the same regardless of room; set, this row only applies while the patient is in that room type. */
  roomTypeId: number | null
  roomTypeName: string | null
  rate: number
}

export interface TariffDetail {
  id: number
  code: string
  name: string
  patientCategoryId: number | null
  effectiveFrom: string
  effectiveTo: string | null
  isActive: boolean
  items: TariffItem[]
}

export interface SaveTariffPayload {
  code: string
  name: string
  patientCategoryId: number | null
  effectiveFrom: string
  effectiveTo: string | null
  items: { serviceId: number; rate: number; roomTypeId: number | null }[]
  isActive: boolean
}

export interface TariffImportResult {
  tariffsInserted: number
  tariffsUpdated: number
  itemsUpserted: number
  errors: string[]
}

export interface PackageListItem {
  id: number
  code: string
  name: string
  price: number
  validityDays: number
  itemCount: number
  isActive: boolean
}

export interface PackageItem {
  id: number
  serviceId: number
  serviceCode: string
  serviceName: string
  quantity: number
  defaultRate: number
}

export interface PackageDetail {
  id: number
  code: string
  name: string
  description: string | null
  price: number
  validityDays: number
  isActive: boolean
  items: PackageItem[]
}

export interface SavePackagePayload {
  code: string
  name: string
  description: string | null
  price: number
  validityDays: number
  items: { serviceId: number; quantity: number }[]
  isActive: boolean
}

// Mirrors backend BillItemSourceType — only Service/Package are valid on an estimate template line.
export type EstimateLineSourceType = 3 | 4

export interface BillEstimateTemplateListItem {
  id: number
  code: string
  name: string
  itemCount: number
  isActive: boolean
}

export interface BillEstimateTemplateLine {
  id: number
  sourceType: EstimateLineSourceType
  serviceId: number | null
  serviceName: string | null
  packageId: number | null
  packageName: string | null
  quantity: number
}

export interface BillEstimateTemplateDetail {
  id: number
  code: string
  name: string
  description: string | null
  isActive: boolean
  items: BillEstimateTemplateLine[]
}

export interface SaveBillEstimateTemplatePayload {
  code: string
  name: string
  description: string | null
  items: { sourceType: EstimateLineSourceType; serviceId: number | null; packageId: number | null; quantity: number }[]
  isActive: boolean
}

// Mirrors backend PayoutType enum.
export type PayoutType = 1 | 2 // Percent, Flat
export const PAYOUT_TYPE_LABELS: Record<PayoutType, string> = { 1: 'Percent', 2: 'Flat' }

export interface DoctorPayoutRule {
  id: number
  doctorId: number
  doctorName: string
  serviceCategoryId: number
  serviceCategoryName: string
  payoutType: PayoutType
  value: number
  isActive: boolean
}

export interface SaveDoctorPayoutRulePayload {
  id?: number
  doctorId: number
  serviceCategoryId: number
  payoutType: PayoutType
  value: number
  isActive: boolean
}

export interface AppointmentType {
  id: number
  code: string
  name: string
  description: string | null
  defaultSlotMinutes: number
  isActive: boolean
}

export interface SaveAppointmentTypePayload {
  id?: number
  code: string
  name: string
  description: string | null
  defaultSlotMinutes: number
  isActive: boolean
}

/** Mirrors backend Prixo.Domain.Enums.DepartmentType. */
export const DEPARTMENT_TYPES = { Clinical: 1, NonClinical: 2 } as const
export type DepartmentType = (typeof DEPARTMENT_TYPES)[keyof typeof DEPARTMENT_TYPES]
export const DEPARTMENT_TYPE_LABELS: Record<DepartmentType, string> = { 1: 'Clinical', 2: 'Non-Clinical' }

/** Department master — a SimpleMaster plus its Clinical / Non-Clinical type. */
export interface Department extends SimpleMaster {
  type: DepartmentType
}

export interface SaveDepartmentPayload {
  id?: number
  code: string
  name: string
  description: string | null
  type: DepartmentType
  isActive: boolean
}

/** "Patient-type colors" — bespoke (not the generic SimpleMaster) since it carries colorHex. */
export interface PatientCategory {
  id: number
  code: string
  name: string
  description: string | null
  colorHex: string | null
  isActive: boolean
}

export interface SavePatientCategoryPayload {
  id?: number
  code: string
  name: string
  description: string | null
  colorHex: string | null
  isActive: boolean
}

export interface LoyaltyCardType {
  id: number
  code: string
  name: string
  description: string | null
  memberLimit: number
  loyaltyCardCategoryId: number | null
  loyaltyCardCategoryName: string | null
  isActive: boolean
}

export interface SaveLoyaltyCardTypePayload {
  id?: number
  code: string
  name: string
  description: string | null
  memberLimit: number
  loyaltyCardCategoryId: number | null
  isActive: boolean
}

export interface LoyaltyCardPriceListItem {
  id: number
  loyaltyCardTypeId: number
  loyaltyCardTypeName: string
  effectiveFrom: string
  effectiveTo: string | null
  itemCount: number
  isActive: boolean
}

export interface LoyaltyCardPriceLine {
  id: number
  durationMonths: number
  amount: number
}

export interface LoyaltyCardPriceDetail {
  id: number
  loyaltyCardTypeId: number
  loyaltyCardTypeName: string
  effectiveFrom: string
  effectiveTo: string | null
  isActive: boolean
  items: LoyaltyCardPriceLine[]
}

export interface SaveLoyaltyCardPricePayload {
  id?: number
  loyaltyCardTypeId: number
  effectiveFrom: string
  effectiveTo: string | null
  items: { durationMonths: number; amount: number }[]
  isActive: boolean
}

export interface Building {
  id: number
  code: string
  name: string
  isActive: boolean
  floorCount: number
}

export interface Floor {
  id: number
  code: string
  name: string
  buildingId: number
  buildingName: string
  isActive: boolean
  wardCount: number
}

export interface RoomType {
  id: number
  code: string
  name: string
  bedChargePerDay: number
  isActive: boolean
  roomCount: number
}

/** A date-effective bed-charge revision for a RoomType — rate history, add/delete only (no edit), same "new row = new version" shape as a Tariff. */
export interface RoomTypeCharge {
  id: number
  rate: number
  effectiveFrom: string
  effectiveTo: string | null
}
export interface SaveRoomTypeChargePayload {
  rate: number
  effectiveFrom: string
  effectiveTo: string | null
}

export interface Ward {
  id: number
  code: string
  name: string
  floorId: number
  floorName: string
  isIcu: boolean
  isObservation: boolean
  isActive: boolean
  roomCount: number
}

export interface Room {
  id: number
  code: string
  name: string
  wardId: number
  wardName: string
  roomTypeId: number
  roomTypeName: string
  isActive: boolean
  bedCount: number
}

export interface BedItem {
  id: number
  code: string
  roomId: number
  roomName: string
  wardName: string
  isActive: boolean
}

export interface Vendor {
  id: number
  code: string
  name: string
  contactPerson: string | null
  phone: string | null
  email: string | null
  address: string | null
  gstNumber: string | null
  isActive: boolean
}

export interface Item {
  id: number
  code: string
  name: string
  itemCategoryId: number
  categoryName: string
  uomId: number
  uomName: string
  hsnCode: string | null
  taxId: number | null
  taxName: string | null
  reorderLevel: number
  isActive: boolean
}

export interface SaveItemPayload {
  code: string
  name: string
  itemCategoryId: number
  uomId: number
  hsnCode: string | null
  taxId: number | null
  reorderLevel: number
  isActive: boolean
}

export interface Drug {
  id: number
  code: string
  name: string
  genericId: number | null
  genericName: string | null
  manufacturerId: number | null
  manufacturerName: string | null
  form: string | null
  strength: string | null
  uomId: number
  uomName: string
  hsnCode: string | null
  taxId: number | null
  taxName: string | null
  schedule: string | null
  isHighAlert: boolean
  isActive: boolean
}

export interface SaveDrugPayload {
  code: string
  name: string
  genericId: number | null
  manufacturerId: number | null
  form: string | null
  strength: string | null
  uomId: number
  hsnCode: string | null
  taxId: number | null
  schedule: string | null
  isHighAlert: boolean
  isActive: boolean
}

export interface Tax {
  id: number
  code: string
  name: string
  ratePercent: number
  isActive: boolean
}

export interface DiscountType {
  id: number
  code: string
  name: string
  maxPercent: number
  approvalThresholdPercent: number
  isActive: boolean
}

/** Mirrors backend ReferrerType enum. */
export const REFERRER_TYPES = { Internal: 1, External: 2 } as const
export type ReferrerType = (typeof REFERRER_TYPES)[keyof typeof REFERRER_TYPES]

export interface Referrer {
  id: number
  code: string
  name: string
  type: ReferrerType
  doctorId: number | null
  doctorName: string | null
  phone: string | null
  organization: string | null
  isActive: boolean
}

export interface IcdCode {
  id: number
  code: string
  description: string
  category: string | null
  isActive: boolean
}

export interface IcdImportResult {
  inserted: number
  updated: number
  skipped: number
  errors: string[]
}

export interface Corporate {
  id: number
  code: string
  name: string
  contactPerson: string | null
  phone: string | null
  email: string | null
  address: string | null
  gstNumber: string | null
  creditLimit: number
  isActive: boolean
}

export interface InsuranceCompany {
  id: number
  code: string
  name: string
  isTpa: boolean
  contactPerson: string | null
  phone: string | null
  email: string | null
  address: string | null
  isActive: boolean
}

export interface ConsentTemplate {
  id: number
  code: string
  title: string
  bodyText: string
  isActive: boolean
}

export interface Area {
  id: number
  code: string
  name: string
  cityId: number
  cityName: string
  isActive: boolean
}

export interface MhcPackage {
  id: number
  code: string
  name: string
  description: string | null
  price: number
  isActive: boolean
}

export interface SaveMhcPackagePayload {
  id?: number
  code: string
  name: string
  description: string | null
  price: number
  isActive: boolean
}

export interface SaveAreaPayload {
  id?: number
  code: string
  name: string
  cityId: number
  isActive: boolean
}
