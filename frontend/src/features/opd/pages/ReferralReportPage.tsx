import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DateRangeFilter, defaultDateRange } from '../../../components/ui/DateRangeFilter'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { downloadReferralReport, useReferralReport } from '../api'
import { REFERRAL_DIRECTION_LABELS, type ReferralDirection } from '../types'

/** "Referral Report" — every Internal/External referral raised from an OPD visit, with the
 * cross-consult opinion status ("Doctor Opinion Entry") for Internal rows. */
export function ReferralReportPage() {
  const [direction, setDirection] = useState('')
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const [hasRun, setHasRun] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const filter = { fromDate, toDate, direction: direction ? (Number(direction) as ReferralDirection) : null }
  const { data: result, isFetching, refetch } = useReferralReport(filter, hasRun)

  const run = async () => {
    setError(null)
    setHasRun(true)
    await refetch()
  }

  const download = async (format: 'csv' | 'excel' | 'pdf') => {
    setError(null)
    try {
      await downloadReferralReport(filter, format)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader title="Referral Report" subtitle="Internal/external referrals raised from OPD visits — filter by date range and export" />

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <div className="mb-4 flex flex-wrap items-end justify-between gap-4">
          <div className="flex flex-wrap items-end gap-4">
            <div className="w-48">
              <Select
                label="Direction (optional)"
                placeholder="All"
                options={Object.entries(REFERRAL_DIRECTION_LABELS).map(([v, l]) => ({ value: v, label: l }))}
                value={direction}
                onChange={(e) => setDirection(e.target.value)}
              />
            </div>
            <DateRangeFilter
              fromDate={fromDate} toDate={toDate}
              onFromDate={(v) => setRange((r) => ({ ...r, fromDate: v }))} onToDate={(v) => setRange((r) => ({ ...r, toDate: v }))}
            />
          </div>
        </div>

        <div className="mb-4 flex gap-2">
          <Button onClick={run} loading={isFetching}>Run</Button>
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
                  <tr><td colSpan={result.columns.length} className="px-3 py-8 text-center text-slate-400">No referrals for this range.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}
        {!hasRun && <p className="py-10 text-center text-sm text-slate-400">Select a date range, then Run.</p>}
      </div>
    </>
  )
}
