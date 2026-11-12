import { useState } from 'react'
import { endpoints } from '../../../api/endpoints'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DateRangeFilter, defaultDateRange } from '../../../components/ui/DateRangeFilter'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { useSimpleMaster } from '../../masters/api'
import { downloadOpVisitsReport, useOpVisitsReport } from '../api'
import type { OpReportMode } from '../types'

/** Camp-scoped entry point into the same OP Report engine — not a separate report. */
export function CampRegistrationReportPage() {
  const { data: camps = [] } = useSimpleMaster('camps', endpoints.masters.camps)
  const [campId, setCampId] = useState('')
  const [mode, setMode] = useState<OpReportMode>('summary')
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const [hasRun, setHasRun] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const filter = { mode, fromDate, toDate, campId: campId ? Number(campId) : null }
  const { data: result, isFetching, refetch } = useOpVisitsReport(filter, hasRun && !!campId)

  const run = async () => {
    setError(null)
    setHasRun(true)
    await refetch()
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
      <PageHeader title="Camp Registration Report" subtitle="OP visits registered under a health camp — filter by date range, choose Summary or Detail, and export" />

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <div className="mb-4 flex flex-wrap items-end justify-between gap-4">
          <div className="flex flex-wrap items-end gap-4">
            <div className="w-64">
              <Select
                label="Camp"
                placeholder="Select a camp…"
                options={camps.map((c) => ({ value: c.id, label: c.name }))}
                value={campId}
                onChange={(e) => setCampId(e.target.value)}
              />
            </div>
            <DateRangeFilter
              fromDate={fromDate} toDate={toDate}
              onFromDate={(v) => setRange((r) => ({ ...r, fromDate: v }))} onToDate={(v) => setRange((r) => ({ ...r, toDate: v }))}
            />
          </div>
          <div className="mb-4 flex gap-1 rounded-md border border-slate-200 p-1">
            {(['summary', 'detail'] as const).map((m) => (
              <button
                key={m}
                type="button"
                onClick={() => setMode(m)}
                className={`rounded px-3 py-1 text-sm font-medium capitalize ${mode === m ? 'bg-primary-600 text-white' : 'text-slate-600 hover:bg-slate-50'}`}
              >
                {m}
              </button>
            ))}
          </div>
        </div>

        <div className="mb-4 flex gap-2">
          <Button onClick={run} loading={isFetching} disabled={!campId}>Run</Button>
          {result && (
            <>
              <Button variant="secondary" onClick={() => download('csv')}>Export CSV</Button>
              <Button variant="secondary" onClick={() => download('excel')}>Export Excel</Button>
              <Button variant="secondary" onClick={() => download('pdf')}>Export PDF</Button>
            </>
          )}
        </div>

        {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        {isFetching && <Spinner />}

        {result && (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50 text-left">
                  {result.columns.map((c) => <th key={c.key} className="px-3 py-2 font-medium text-slate-600">{c.label}</th>)}
                </tr>
              </thead>
              <tbody>
                {result.rows.map((row, i) => (
                  <tr key={i} className="border-b border-slate-100 last:border-0">
                    {result.columns.map((c) => <td key={c.key} className="px-3 py-2 text-slate-700">{String(row[c.key] ?? '')}</td>)}
                  </tr>
                ))}
                {result.rows.length === 0 && (
                  <tr><td colSpan={result.columns.length} className="px-3 py-8 text-center text-slate-400">No rows for this range.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}
        {!hasRun && <p className="py-10 text-center text-sm text-slate-400">Select a camp and date range, then Run.</p>}
      </div>
    </>
  )
}
