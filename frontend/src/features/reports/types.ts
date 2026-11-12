export interface TrendPoint {
  date: string
  amount: number
}

/** Mirrors backend Prixo.Domain.Enums.CensusScope. */
export const CENSUS_SCOPES = { Both: 0, Op: 1, Ip: 2 } as const
export type CensusScope = (typeof CENSUS_SCOPES)[keyof typeof CENSUS_SCOPES]
export const CENSUS_SCOPE_LABELS: Record<CensusScope, string> = {
  0: 'OPIP (Combined)',
  1: 'OP Only',
  2: 'IP Only',
}

export interface BranchSummaryRow {
  branchId: number
  branchName: string
  revenue: number
  collection: number
  opVisitCount: number
  activeInpatients: number
}

export interface ManagementDashboard {
  totalRevenue: number
  totalCollection: number
  opVisitCount: number
  activeInpatients: number
  bedOccupancyPercent: number
  unacknowledgedAlerts: number
  isCrossBranch: boolean
  byBranch: BranchSummaryRow[]
}

export interface DoctorVisitCountRow {
  doctorId: number
  doctorName: string
  visitCount: number
}

export interface OpDashboard {
  totalVisits: number
  newCount: number
  reviewCount: number
  dailyTrend: TrendPoint[]
  byDoctor: DoctorVisitCountRow[]
}

export interface IpDashboard {
  totalBeds: number
  occupied: number
  vacant: number
  occupancyRatePercent: number
  admissionsInRange: number
  dischargesInRange: number
  averageLengthOfStayDays: number | null
}

export interface PaymentModeAmountRow {
  paymentModeId: number
  paymentModeName: string
  amount: number
  txnCount: number
}

export interface BillingDashboard {
  totalCollection: number
  byMode: PaymentModeAmountRow[]
  dailyTrend: TrendPoint[]
  outstandingAmount: number
}

export interface DepartmentAmountRow {
  departmentId: number | null
  departmentName: string
  amount: number
}

export interface DoctorAmountRow {
  doctorId: number | null
  doctorName: string
  amount: number
}

export interface RevenueDashboard {
  totalRevenue: number
  dailyTrend: TrendPoint[]
  byDepartment: DepartmentAmountRow[]
  byDoctor: DoctorAmountRow[]
}

export interface ReportColumn {
  key: string
  label: string
}

export interface ReportCatalogEntry {
  code: string
  name: string
  description: string | null
  category: string
  linkPath: string | null
  isExecutable: boolean
  columns: ReportColumn[]
}

export interface ReportResult {
  reportName: string
  columns: ReportColumn[]
  rows: Record<string, unknown>[]
}

export interface ReportSubscription {
  id: number
  reportCode: string
  cronExpression: string
  recipientEmail: string
  isActive: boolean
  createdAt: string
}
