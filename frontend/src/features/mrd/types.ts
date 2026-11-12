import type { VirusScanStatus } from '../emr/types'

export type RecordFileStatus = 1 | 2 // Active, Destroyed
export type FileRequestType = 1 | 2 | 3 // Internal, External, Legal
export type FileRequestStatus = 1 | 2 | 3 | 4 | 5 // Requested, Approved, Rejected, Issued, Returned
export type CodingStatus = 1 | 2 // Pending, Coded
export type DestructionStatus = 1 | 2 | 3 // Eligible, Approved, Destroyed

export const RECORD_FILE_STATUS_LABELS: Record<RecordFileStatus, string> = { 1: 'Active', 2: 'Destroyed' }
export const FILE_REQUEST_TYPE_LABELS: Record<FileRequestType, string> = { 1: 'Internal', 2: 'External', 3: 'Legal' }
export const FILE_REQUEST_STATUS_LABELS: Record<FileRequestStatus, string> = {
  1: 'Requested', 2: 'Approved', 3: 'Rejected', 4: 'Issued', 5: 'Returned',
}
export const CODING_STATUS_LABELS: Record<CodingStatus, string> = { 1: 'Pending', 2: 'Coded' }
export const DESTRUCTION_STATUS_LABELS: Record<DestructionStatus, string> = { 1: 'Eligible', 2: 'Approved', 3: 'Destroyed' }

// ── Record files / movements ────────────────────────────────────────────────
export interface RecordFileListItem {
  id: number
  fileNumber: string
  status: RecordFileStatus
  admissionId: number
  admissionNumber: string
  patientId: number
  patientName: string
  patientUhid: string
  currentLocation: string
  currentCustodianName: string | null
  createdAt: string
}

export interface RecordFileDetail {
  id: number
  fileNumber: string
  status: RecordFileStatus
  admissionId: number
  admissionNumber: string
  patientId: number
  patientName: string
  patientUhid: string
  currentLocation: string
  currentCustodianUserId: number | null
  currentCustodianName: string | null
  createdByName: string
  createdAt: string
  destroyedAt: string | null
}

export interface FileMovementDto {
  id: number
  movedAt: string
  fromLocation: string
  toLocation: string
  fromCustodianName: string | null
  toCustodianName: string | null
  reason: string | null
  movedByName: string
}

export interface RecordMovementPayload {
  toLocation: string
  toCustodianUserId: number | null
  reason: string | null
}

// ── File requests ────────────────────────────────────────────────────────────
export interface FileRequestDto {
  id: number
  recordFileId: number
  fileNumber: string
  requestType: FileRequestType
  requestedByUserId: number | null
  requestedByName: string | null
  requesterName: string | null
  requesterOrganization: string | null
  purpose: string
  expectedReturnDate: string | null
  status: FileRequestStatus
  requestedAt: string
  approvedByName: string | null
  approvedAt: string | null
  rejectReason: string | null
  issuedByName: string | null
  issuedAt: string | null
  returnedToName: string | null
  returnedAt: string | null
  isOverdue: boolean
  notes: string | null
}

export interface CreateFileRequestPayload {
  recordFileId: number
  requestType: FileRequestType
  requesterName: string | null
  requesterOrganization: string | null
  purpose: string
  expectedReturnDate: string | null
}

// ── Scanned documents ────────────────────────────────────────────────────────
export interface ScannedDocumentDto {
  id: number
  admissionId: number
  documentTypeId: number
  documentTypeName: string
  fileName: string
  contentType: string
  sizeBytes: number
  description: string | null
  virusScanStatus: VirusScanStatus
  uploadedByName: string
  uploadedAt: string
}

// ── Coding worklist ──────────────────────────────────────────────────────────
export interface CodingWorklistItem {
  admissionId: number
  admissionNumber: string
  patientId: number
  patientName: string
  patientUhid: string
  dischargedAt: string | null
  status: CodingStatus
  codedByName: string | null
  codedAt: string | null
  notes: string | null
}

// ── File audit ───────────────────────────────────────────────────────────────
export interface FileAuditDto {
  id: number
  admissionId: number
  dischargeSummarySigned: boolean
  consentsPresent: boolean
  progressNotesComplete: boolean
  investigationReportsPresent: boolean
  nursingRecordsComplete: boolean
  isComplete: boolean
  missingDocumentsNotes: string | null
  auditedByName: string
  auditedAt: string
}

export interface SaveFileAuditPayload {
  dischargeSummarySigned: boolean
  consentsPresent: boolean
  progressNotesComplete: boolean
  investigationReportsPresent: boolean
  nursingRecordsComplete: boolean
  missingDocumentsNotes: string | null
}

// ── Retention / destruction ──────────────────────────────────────────────────
export interface RetentionPolicyDto {
  id: number
  code: string
  name: string
  retentionYears: number
  description: string | null
  isActive: boolean
}

export interface SaveRetentionPolicyPayload {
  code: string
  name: string
  retentionYears: number
  description: string | null
}

export interface EligibleFileDto {
  recordFileId: number
  fileNumber: string
  patientId: number
  patientName: string
  dischargedAt: string | null
  retentionPolicyId: number
  retentionPolicyName: string
  eligibleDate: string
}

export interface DestructionRegisterEntryDto {
  id: number
  recordFileId: number
  fileNumber: string
  retentionPolicyId: number
  retentionPolicyName: string
  eligibleDate: string
  status: DestructionStatus
  approvedByName: string | null
  approvedAt: string | null
  destroyedByName: string | null
  destroyedAt: string | null
  destructionMethod: string | null
  notes: string | null
}

// ── Reports ───────────────────────────────────────────────────────────────────
export interface CodingStatsDto {
  totalDischarged: number
  coded: number
  pending: number
  codedPercent: number
}

export interface FileMovementReportRow {
  movedAt: string
  fileNumber: string
  patientName: string
  fromLocation: string
  toLocation: string
  movedByName: string
}

export interface IncompleteRecordRow {
  admissionId: number
  admissionNumber: string
  patientName: string
  dischargedAt: string | null
  hasRecordFile: boolean
  hasFileAudit: boolean
  auditComplete: boolean
  isCoded: boolean
}
