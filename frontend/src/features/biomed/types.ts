export type EquipmentCriticality = 1 | 2 | 3 | 4 // Critical, High, Medium, Low
export type EquipmentStatus = 1 | 2 | 3 // InService, UnderMaintenance, Decommissioned
export type ContractType = 1 | 2 // Amc, Cmc
export type WorkOrderType = 1 | 2 // PreventiveMaintenance, Breakdown
export type WorkOrderStatus = 1 | 2 | 3 | 4 // Open, InProgress, Completed, Cancelled
export type ComplaintStatus = 1 | 2 | 3 | 4 | 5 // Reported, Assigned, InProgress, Resolved, Closed

export const CRITICALITY_LABELS: Record<EquipmentCriticality, string> = { 1: 'Critical', 2: 'High', 3: 'Medium', 4: 'Low' }
export const EQUIPMENT_STATUS_LABELS: Record<EquipmentStatus, string> = { 1: 'In Service', 2: 'Under Maintenance', 3: 'Decommissioned' }
export const CONTRACT_TYPE_LABELS: Record<ContractType, string> = { 1: 'AMC', 2: 'CMC' }
export const WORK_ORDER_TYPE_LABELS: Record<WorkOrderType, string> = { 1: 'Preventive Maintenance', 2: 'Breakdown' }
export const WORK_ORDER_STATUS_LABELS: Record<WorkOrderStatus, string> = { 1: 'Open', 2: 'In Progress', 3: 'Completed', 4: 'Cancelled' }
export const COMPLAINT_STATUS_LABELS: Record<ComplaintStatus, string> = { 1: 'Reported', 2: 'Assigned', 3: 'In Progress', 4: 'Resolved', 5: 'Closed' }

export interface EquipmentListItem {
  id: number
  equipmentCode: string
  name: string
  serialNumber: string | null
  model: string | null
  departmentName: string | null
  criticality: EquipmentCriticality
  status: EquipmentStatus
  currentLocation: string
  warrantyExpiryDate: string | null
}

export interface EquipmentDetail {
  id: number
  equipmentCode: string
  name: string
  serialNumber: string | null
  model: string | null
  manufacturer: string | null
  departmentId: number | null
  departmentName: string | null
  vendorId: number | null
  vendorName: string | null
  criticality: EquipmentCriticality
  status: EquipmentStatus
  currentLocation: string
  purchaseDate: string | null
  purchaseCost: number | null
  warrantyExpiryDate: string | null
  notes: string | null
}

export interface SaveEquipmentPayload {
  equipmentCode: string
  name: string
  serialNumber: string | null
  model: string | null
  manufacturer: string | null
  departmentId: number | null
  vendorId: number | null
  criticality: EquipmentCriticality
  currentLocation: string
  purchaseDate: string | null
  purchaseCost: number | null
  warrantyExpiryDate: string | null
  notes: string | null
}

export interface EquipmentLocationLogDto {
  id: number
  movedAt: string
  fromLocation: string
  toLocation: string
  reason: string | null
  movedByName: string
}

export interface AmcContractDto {
  id: number
  equipmentId: number
  equipmentName: string
  vendorId: number
  vendorName: string
  contractType: ContractType
  contractNumber: string
  startDate: string
  endDate: string
  cost: number
  renewalReminderDays: number
  isRenewalDue: boolean
  notes: string | null
}

export interface SaveAmcContractPayload {
  equipmentId: number
  vendorId: number
  contractType: ContractType
  contractNumber: string
  startDate: string
  endDate: string
  cost: number
  renewalReminderDays: number
  notes: string | null
}

export interface PmScheduleDto {
  id: number
  equipmentId: number
  equipmentName: string
  frequencyDays: number
  nextDueDate: string
  lastGeneratedDate: string | null
  isDue: boolean
  checklistNotes: string | null
}

export interface SavePmSchedulePayload {
  equipmentId: number
  frequencyDays: number
  nextDueDate: string
  checklistNotes: string | null
}

export interface SpareUsageDto {
  id: number
  batchId: number
  productName: string
  quantity: number
}

export interface WorkOrderDto {
  id: number
  workOrderNumber: string
  equipmentId: number
  equipmentName: string
  workOrderType: WorkOrderType
  pmScheduleId: number | null
  breakdownComplaintId: number | null
  status: WorkOrderStatus
  scheduledDate: string
  completedAt: string | null
  assignedToUserId: number | null
  assignedToName: string | null
  notes: string | null
  spareUsages: SpareUsageDto[]
}

export interface BreakdownComplaintDto {
  id: number
  complaintNumber: string
  equipmentId: number
  equipmentName: string
  reportedByName: string
  reportedAt: string
  description: string
  status: ComplaintStatus
  assignedToName: string | null
  assignedAt: string | null
  resolvedAt: string | null
  downtimeMinutes: number | null
  resolution: string | null
}

export interface CalibrationDto {
  id: number
  equipmentId: number
  equipmentName: string
  calibrationDate: string
  nextDueDate: string
  performedBy: string | null
  certificateNumber: string | null
  hasCertificateFile: boolean
  isDue: boolean
  notes: string | null
}

export interface ServiceHistoryDto {
  id: number
  equipmentId: number
  workOrderId: number | null
  workOrderNumber: string | null
  serviceDate: string
  serviceType: string
  performedBy: string | null
  cost: number | null
  notes: string | null
}

export interface DowntimeReportRow {
  equipmentId: number
  equipmentName: string
  complaintCount: number
  totalDowntimeMinutes: number
}

export interface PmComplianceRow {
  equipmentId: number
  equipmentName: string
  totalCompleted: number
  completedOnTime: number
  compliancePercent: number
}
