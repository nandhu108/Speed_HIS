export type CriticalAlertParameter = 1 | 2 | 3 | 4 | 5 | 6 // SpO2, PulseRate, RespiratoryRate, BloodPressureSystolic, BloodPressureDiastolic, TemperatureF
export type ComparisonOperator = 1 | 2 // LessThan, GreaterThan
export type AlertSeverity = 1 | 2 // Warning, Critical
export type VentilatorMode = 1 | 2 | 3 | 4 | 5 | 6 // VolumeControl, PressureControl, Simv, Cpap, Bipap, Other
export type IcuSeverityLevel = 1 | 2 | 3 | 4 // Mild, Moderate, Severe, Critical

export const PARAMETER_LABELS: Record<CriticalAlertParameter, string> = {
  1: 'SpO2', 2: 'Pulse Rate', 3: 'Respiratory Rate', 4: 'BP Systolic', 5: 'BP Diastolic', 6: 'Temperature (°F)',
}
export const OPERATOR_LABELS: Record<ComparisonOperator, string> = { 1: 'Less than', 2: 'Greater than' }
export const ALERT_SEVERITY_LABELS: Record<AlertSeverity, string> = { 1: 'Warning', 2: 'Critical' }
export const VENTILATOR_MODE_LABELS: Record<VentilatorMode, string> = {
  1: 'Volume Control', 2: 'Pressure Control', 3: 'SIMV', 4: 'CPAP', 5: 'BiPAP', 6: 'Other',
}
export const SEVERITY_LEVEL_LABELS: Record<IcuSeverityLevel, string> = {
  1: 'Mild', 2: 'Moderate', 3: 'Severe', 4: 'Critical',
}

// ── Critical alert rules ─────────────────────────────────────────────────
export interface CriticalAlertRule {
  id: number
  parameter: CriticalAlertParameter
  operator: ComparisonOperator
  thresholdValue: number
  severity: AlertSeverity
  description: string | null
  isActive: boolean
}
export interface SaveCriticalAlertRulePayload {
  parameter: CriticalAlertParameter
  operator: ComparisonOperator
  thresholdValue: number
  severity: AlertSeverity
  description: string | null
  isActive: boolean
}

// ── Vitals / alerts ──────────────────────────────────────────────────────
export interface RecordVitalsPayload {
  temperatureF: number | null
  pulseRate: number | null
  respiratoryRate: number | null
  bloodPressureSystolic: number | null
  bloodPressureDiastolic: number | null
  spO2: number | null
  randomBloodSugar: number | null
  glasgowComaScale: number | null
  meanArterialPressure: number | null
  centralVenousPressure: number | null
  urineOutputMl: number | null
}

export interface IcuVitalsAlert {
  id: number
  admissionId: number
  patientName: string
  ipVitalsId: number
  parameter: CriticalAlertParameter
  value: number
  thresholdValue: number
  operator: ComparisonOperator
  severity: AlertSeverity
  raisedAt: string
  isAcknowledged: boolean
  acknowledgedByName: string | null
  acknowledgedAt: string | null
  acknowledgementNotes: string | null
}

// ── Ventilator ───────────────────────────────────────────────────────────
export interface StartVentilatorSessionPayload {
  notes: string | null
}
export interface AddVentilatorObservationPayload {
  mode: VentilatorMode
  fiO2Percent: number
  peep: number
  tidalVolumeMl: number | null
  respiratoryRateSet: number | null
  peakPressure: number | null
  plateauPressure: number | null
  notes: string | null
}
export interface VentilatorObservation {
  id: number
  recordedAt: string
  recordedByName: string
  mode: VentilatorMode
  fiO2Percent: number
  peep: number
  tidalVolumeMl: number | null
  respiratoryRateSet: number | null
  peakPressure: number | null
  plateauPressure: number | null
  notes: string | null
}
export interface VentilatorSession {
  id: number
  admissionId: number
  startedAt: string
  startedByName: string
  stoppedAt: string | null
  notes: string | null
  observations: VentilatorObservation[]
}

// ── Infusion ─────────────────────────────────────────────────────────────
export interface StartInfusionPayload {
  drugId: number | null
  drugNameOverride: string | null
  notes: string | null
}
export interface AddInfusionRatePayload {
  rate: number
  rateUnit: string
  notes: string | null
}
export interface InfusionRateEntry {
  id: number
  recordedAt: string
  recordedByName: string
  rate: number
  rateUnit: string
  notes: string | null
}
export interface InfusionChartEntry {
  id: number
  admissionId: number
  drugId: number | null
  productName: string
  startedAt: string
  startedByName: string
  stoppedAt: string | null
  notes: string | null
  rateEntries: InfusionRateEntry[]
}

// ── Severity ─────────────────────────────────────────────────────────────
export interface RecordSeverityPayload {
  severityLevel: IcuSeverityLevel
  notes: string | null
}
export interface IcuSeverityAssessment {
  id: number
  admissionId: number
  assessedAt: string
  assessedByName: string
  severityLevel: IcuSeverityLevel
  notes: string | null
}

// ── Dashboard ────────────────────────────────────────────────────────────
export interface IcuCensusPatient {
  admissionId: number
  admissionNumber: string
  patientName: string
  wardName: string
  roomName: string
  bedCode: string
  isVentilated: boolean
  latestSeverity: IcuSeverityLevel | null
  unacknowledgedAlertCount: number
}
export interface IcuDashboard {
  census: number
  ventilatedCount: number
  patients: IcuCensusPatient[]
}
