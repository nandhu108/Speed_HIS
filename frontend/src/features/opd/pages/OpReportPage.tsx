import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { endpoints } from '../../../api/endpoints'
import { Button } from '../../../components/ui/Button'
import { DateRangeFilter, defaultDateRange } from '../../../components/ui/DateRangeFilter'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { ReportResultTable } from '../../../components/ui/ReportResultTable'
import { Select } from '../../../components/ui/Select'
import { useDoctors, useReferrers, useSimpleMaster } from '../../masters/api'
import { downloadOpVisitsReport, useOpVisitsReport, type OpReportFilter } from '../api'
import { VISIT_TYPE_LABELS, VISIT_TYPES, type OpReportMode, type VisitType } from '../types'

/** OP Report — one filter card (date range/free filters/Summary-Detail toggle/Run/Reset/
 * Export), then a separate ReportResultTable below it (search box, show/hide column picker,
 * sticky header + sticky first column, client-side pagination) — not everything nested in
 * one box. */
export function OpReportPage() {
  const [mode, setMode] = useState<OpReportMode>('summary')
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const [doctorId, setDoctorId] = useState('')
  const [visitType, setVisitType] = useState('')
  const [patientCategoryId, setPatientCategoryId] = useState('')
  const [departmentId, setDepartmentId] = useState('')
  const [referrerId, setReferrerId] = useState('')
  const [referralDoctorId, setReferralDoctorId] = useState('')
  const [regNo, setRegNo] = useState('')
  const [patientName, setPatientName] = useState('')
  const [isMlcCase, setIsMlcCase] = useState(false)
  const [hasRun, setHasRun] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const { data: patientCategories = [] } = useSimpleMaster('patientcategories', endpoints.masters.patientCategories)
  const { data: departments = [] } = useSimpleMaster('departments', endpoints.masters.departments)
  const { data: referrersResult } = useReferrers({ page: 1, pageSize: 200 })

  const filter: OpReportFilter = {
    mode, fromDate, toDate,
    doctorId: doctorId ? Number(doctorId) : null,
    visitType: visitType ? (Number(visitType) as VisitType) : null,
    patientCategoryId: patientCategoryId ? Number(patientCategoryId) : null,
    departmentId: departmentId ? Number(departmentId) : null,
    referrerId: referrerId ? Number(referrerId) : null,
    referralDoctorId: referralDoctorId ? Number(referralDoctorId) : null,
    regNo: regNo.trim() || null,
    patientName: patientName.trim() || null,
    isMlcCase: isMlcCase || null,
  }
  const { data: result, isFetching, isError: isFetchError, error: fetchError, refetch } = useOpVisitsReport(filter, hasRun)

  const run = async () => {
    setError(null)
    setHasRun(true)
    await refetch()
  }

  const resetFilters = () => {
    setDoctorId(''); setVisitType(''); setPatientCategoryId(''); setDepartmentId('')
    setReferrerId(''); setReferralDoctorId(''); setRegNo(''); setPatientName(''); setIsMlcCase(false)
  }

  const download = async (format: 'csv' | 'excel' | 'pdf') => {
    setError(null)
    try {
      await downloadOpVisitsReport(filter, format)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader title="OP Report" subtitle="OPD visits — filter, choose Summary or Detail, pick columns, and export" />

      <div className="mb-4 rounded-shell border border-border-soft bg-panel p-4 shadow-shell-sm">
        <div className="mb-4 flex flex-wrap items-end justify-between gap-4">
          <DateRangeFilter
            fromDate={fromDate} toDate={toDate}
            onFromDate={(v) => setRange((r) => ({ ...r, fromDate: v }))} onToDate={(v) => setRange((r) => ({ ...r, toDate: v }))}
          />
          <div className="mb-4 flex gap-1 rounded-md border border-border-soft p-1">
            {(['summary', 'detail'] as const).map((m) => (
              <button
                key={m}
                type="button"
                onClick={() => setMode(m)}
                className={`rounded px-3 py-1 text-sm font-medium capitalize ${mode === m ? 'bg-primary-600 text-white' : 'text-ink-muted hover:bg-panel-2'}`}
              >
                {m}
              </button>
            ))}
          </div>
        </div>

        <div className="mb-4 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          <Input label="UHID" value={regNo} onChange={(e) => setRegNo(e.target.value)} />
          <Input label="Patient Name" value={patientName} onChange={(e) => setPatientName(e.target.value)} />
          <Select
            label="Doctor" placeholder="All" value={doctorId} onChange={(e) => setDoctorId(e.target.value)}
            options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
          />
          <Select
            label="Visit Type" placeholder="All" value={visitType} onChange={(e) => setVisitType(e.target.value)}
            options={Object.values(VISIT_TYPES).map((v) => ({ value: v, label: VISIT_TYPE_LABELS[v] }))}
          />
          <Select
            label="Patient Type" placeholder="All" value={patientCategoryId} onChange={(e) => setPatientCategoryId(e.target.value)}
            options={patientCategories.map((c) => ({ value: c.id, label: c.name }))}
          />
          <Select
            label="Department" placeholder="All" value={departmentId} onChange={(e) => setDepartmentId(e.target.value)}
            options={departments.map((d) => ({ value: d.id, label: d.name }))}
          />
          <Select
            label="Referred Source" placeholder="All" value={referrerId} onChange={(e) => setReferrerId(e.target.value)}
            options={(referrersResult?.items ?? []).map((r) => ({ value: r.id, label: r.name }))}
          />
          <Select
            label="Referral Doctor" placeholder="All" value={referralDoctorId} onChange={(e) => setReferralDoctorId(e.target.value)}
            options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
          />
        </div>

        <div className="mb-4 flex flex-wrap items-center gap-4">
          <label className="flex items-center gap-1.5 text-sm text-ink-muted">
            <input type="checkbox" checked={isMlcCase} onChange={(e) => setIsMlcCase(e.target.checked)} />
            MLC cases only
          </label>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Button onClick={run} loading={isFetching}>Run</Button>
          <Button variant="secondary" onClick={resetFilters}>Reset Filters</Button>
          {result && hasRun && (
            <>
              <Button variant="secondary" onClick={() => download('csv')}>Export CSV</Button>
              <Button variant="secondary" onClick={() => download('excel')}>Export Excel</Button>
              <Button variant="secondary" onClick={() => download('pdf')}>Export PDF</Button>
            </>
          )}
        </div>

        {error && <p className="mt-3 rounded-md bg-crit/10 px-3 py-2 text-sm text-crit">{error}</p>}
      </div>

      {hasRun ? (
        <ReportResultTable
          result={result}
          isLoading={isFetching}
          errorMessage={isFetchError ? errorMessage(fetchError) : undefined}
          emptyMessage="No rows for this range."
        />
      ) : (
        <p className="py-10 text-center text-sm text-ink-faint">Pick a date range and filters, then Run.</p>
      )}
    </>
  )
}
