export type QualityAutoComputeSource = 1 | 2 | 3 // LabTat | BedOccupancy | IncidentCount
export const AUTO_COMPUTE_SOURCE_LABELS: Record<QualityAutoComputeSource, string> = {
  1: 'Lab TAT (average hours)', 2: 'Bed Occupancy', 3: 'Incident Count',
}

export type QualityAuditStatus = 1 | 2 | 3 | 4 // Scheduled | InProgress | Completed | Cancelled
export const AUDIT_STATUS_LABELS: Record<QualityAuditStatus, string> = {
  1: 'Scheduled', 2: 'In Progress', 3: 'Completed', 4: 'Cancelled',
}

export type NonConformanceSeverity = 1 | 2 // Minor | Major
export const NC_SEVERITY_LABELS: Record<NonConformanceSeverity, string> = { 1: 'Minor', 2: 'Major' }

export type NonConformanceStatus = 1 | 2 | 3 // Open | InProgress | Closed
export const NC_STATUS_LABELS: Record<NonConformanceStatus, string> = { 1: 'Open', 2: 'In Progress', 3: 'Closed' }

export type RiskStatus = 1 | 2 | 3 // Identified | Mitigating | Closed
export const RISK_STATUS_LABELS: Record<RiskStatus, string> = { 1: 'Identified', 2: 'Mitigating', 3: 'Closed' }

export type CapaSourceType = 1 | 2 | 3 | 4 // Incident | NonConformance | Audit | Risk
export const CAPA_SOURCE_LABELS: Record<CapaSourceType, string> = {
  1: 'Incident', 2: 'Non-Conformance', 3: 'Audit', 4: 'Risk',
}

export type CapaStatus = 1 | 2 | 3 | 4 // Open | InProgress | Verified | Closed
export const CAPA_STATUS_LABELS: Record<CapaStatus, string> = {
  1: 'Open', 2: 'In Progress', 3: 'Verified', 4: 'Closed',
}

// ── Indicators ───────────────────────────────────────────────────────────
export interface SaveIndicatorPayload {
  code: string
  name: string
  description: string | null
  category: string | null
  numeratorDefinition: string
  denominatorDefinition: string
  unit: string | null
  targetValue: number | null
  autoComputeSource: QualityAutoComputeSource | null
}

export interface Indicator {
  id: number
  code: string
  name: string
  description: string | null
  category: string | null
  numeratorDefinition: string
  denominatorDefinition: string
  unit: string | null
  targetValue: number | null
  autoComputeSource: QualityAutoComputeSource | null
}

export interface RecordIndicatorEntryPayload {
  periodMonth: string
  numerator: number
  denominator: number
  remarks: string | null
}

export interface IndicatorEntry {
  id: number
  indicatorId: number
  periodMonth: string
  numerator: number
  denominator: number
  computedValue: number | null
  isAutoComputed: boolean
  remarks: string | null
  enteredByName: string
}

// ── Audit checklists ─────────────────────────────────────────────────────
export interface SaveAuditTemplateItemPayload { sortOrder: number; question: string; maxScore: number }
export interface SaveAuditTemplatePayload {
  code: string
  name: string
  category: string | null
  items: SaveAuditTemplateItemPayload[]
}

export interface AuditTemplateItem { id: number; sortOrder: number; question: string; maxScore: number }
export interface AuditTemplate { id: number; code: string; name: string; category: string | null; items: AuditTemplateItem[] }

export interface ScheduleAuditPayload { templateId: number; scheduledDate: string }
export interface RecordAuditResponsePayload { templateItemId: number; score: number; remarks: string | null }
export interface ScoreAuditPayload { responses: RecordAuditResponsePayload[] }

export interface AuditResponse { templateItemId: number; question: string; maxScore: number; score: number; remarks: string | null }
export interface Audit {
  id: number
  templateId: number
  templateName: string
  scheduledDate: string
  conductedDate: string | null
  status: QualityAuditStatus
  conductedByName: string | null
  remarks: string | null
  scorePercent: number | null
  responses: AuditResponse[]
}

// ── Non-conformances ─────────────────────────────────────────────────────
export interface RaiseNonConformancePayload {
  standardId: number | null
  departmentId: number | null
  title: string
  description: string
  severity: NonConformanceSeverity
}

export interface NonConformance {
  id: number
  standardId: number | null
  standardCode: string | null
  departmentId: number | null
  departmentName: string | null
  title: string
  description: string
  severity: NonConformanceSeverity
  raisedByName: string
  status: NonConformanceStatus
  closedAt: string | null
  closedByName: string | null
}

// ── Risk register ────────────────────────────────────────────────────────
export interface SaveRiskPayload {
  title: string
  category: string | null
  description: string
  likelihood: number
  impact: number
  mitigationPlan: string | null
  ownerUserId: number
  identifiedDate: string
  reviewDate: string | null
}

export interface Risk {
  id: number
  title: string
  category: string | null
  description: string
  likelihood: number
  impact: number
  riskScore: number
  mitigationPlan: string | null
  ownerName: string
  status: RiskStatus
  identifiedDate: string
  reviewDate: string | null
  closedAt: string | null
}

// ── CAPA tracker ──────────────────────────────────────────────────────────
export interface RaiseCapaPayload {
  sourceType: CapaSourceType
  sourceId: number
  actionPlan: string
  ownerUserId: number
  dueDate: string
}

export interface Capa {
  id: number
  sourceType: CapaSourceType
  sourceId: number
  actionPlan: string
  ownerName: string
  dueDate: string
  status: CapaStatus
  completedAt: string | null
  verifiedByName: string | null
  verifiedAt: string | null
  verificationNotes: string | null
}

// ── Committee meetings ────────────────────────────────────────────────────
export interface SaveCommitteeActionItemPayload { description: string; ownerUserId: number; dueDate: string }
export interface SaveCommitteeMeetingPayload {
  committeeId: number
  meetingDate: string
  minutes: string
  attendeesText: string | null
  actionItems: SaveCommitteeActionItemPayload[]
}

export interface CommitteeActionItem {
  id: number
  description: string
  ownerName: string
  dueDate: string
  isCompleted: boolean
  completedAt: string | null
}
export interface CommitteeMeeting {
  id: number
  committeeId: number
  committeeName: string
  meetingDate: string
  minutes: string
  attendeesText: string | null
  actionItems: CommitteeActionItem[]
}

// ── NABH evidence ─────────────────────────────────────────────────────────
export interface Evidence {
  id: number
  standardId: number
  standardCode: string
  title: string
  description: string | null
  fileName: string
  uploadedByName: string
  uploadedAt: string
}

// ── Dashboards ────────────────────────────────────────────────────────────
export interface IndicatorTrendPoint {
  indicatorId: number
  indicatorName: string
  unit: string | null
  targetValue: number | null
  periodMonth: string
  computedValue: number | null
}
export interface KpiDashboard { points: IndicatorTrendPoint[] }

export interface ChapterEvidenceCount { chapter: string; count: number }
export interface ComplianceDashboard {
  openNonConformances: number
  openCapas: number
  overdueCapas: number
  openRisks: number
  sentinelIncidentsThisYear: number
  averageAuditScorePercent: number | null
  evidenceCount: number
  evidenceByChapter: ChapterEvidenceCount[]
}
