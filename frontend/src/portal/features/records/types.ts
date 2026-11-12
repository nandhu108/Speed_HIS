export interface PortalLabOrder {
  id: number
  orderNumber: string
  orderDate: string
  hasApprovedReport: boolean
}

export interface PortalRadOrderItem {
  id: number
  accessionNumber: string
  testName: string
  orderDate: string
  isApproved: boolean
}

export interface PortalPrescription {
  id: number
  visitDate: string
  doctorName: string
  items: string[]
}

export interface PortalBill {
  id: number
  billNumber: string
  billDate: string
  totalAmount: number
  balanceAmount: number
  status: string
}

export interface PortalDischargeSummary {
  admissionId: number
  admissionNumber: string
  dischargedAt: string
  finalDiagnosis: string
}

export interface OnlinePaymentRequested {
  requestId: number
  amount: number
  requestedAt: string
}

export interface MedicalHistoryDto {
  id: number
  condition: string
  diagnosedDate: string | null
  notes: string | null
}

export interface SurgicalHistoryDto {
  id: number
  procedureName: string
  procedureDate: string | null
  surgeon: string | null
  notes: string | null
}

export interface AllergyDto {
  id: number
  allergenType: number
  allergenName: string
  reaction: string | null
  severity: number
  isActive: boolean
}

export interface ImmunizationDto {
  id: number
  vaccineName: string
  doseNumber: number | null
  administeredDate: string
  nextDueDate: string | null
}

export interface PortalMedicalHistory {
  medical: MedicalHistoryDto[]
  surgical: SurgicalHistoryDto[]
  allergies: AllergyDto[]
  immunizations: ImmunizationDto[]
}
