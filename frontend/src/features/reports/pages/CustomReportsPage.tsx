import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { downloadReportExport, useExecuteReport, useReportCatalog } from '../api'
import { DateRangeFilter, defaultDateRange } from '../../../components/ui/DateRangeFilter'
import type { ReportCatalogEntry, ReportResult } from '../types'

/** Custom Report Builder v1 (plan §17/§23) — catalog of every report_definitions row; "linked" entries open their own dedicated page, "executable" entries run inline against a `reporting` view with CSV/Excel/PDF export. */
export function CustomReportsPage() {
  const navigate = useNavigate()
  const { data: catalog = [], isLoading } = useReportCatalog()
  const [selected, setSelected] = useState<ReportCatalogEntry | null>(null)
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(30))
  const execute = useExecuteReport()
  const [result, setResult] = useState<ReportResult | null>(null)
  const [error, setError] = useState<string | null>(null)

  const byCategory = catalog.reduce<Record<string, ReportCatalogEntry[]>>((acc, entry) => {
    (acc[entry.category] ??= []).push(entry)
    return acc
  }, {})

  const open = (entry: ReportCatalogEntry) => {
    if (!entry.isExecutable) {
      navigate(entry.linkPath!)
      return
    }
    setSelected(entry)
    setResult(null)
    setError(null)
  }

  const run = async () => {
    if (!selected) return
    setError(null)
    try {
      const data = await execute.mutateAsync({ code: selected.code, fromDate, toDate })
      setResult(data)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const download = (format: 'csv' | 'excel' | 'pdf') => {
    if (selected) downloadReportExport(selected.code, format, { fromDate, toDate })
  }

  return (
    <>
      <PageHeader title="Custom Reports" subtitle="Every report in the catalog — run one inline, or open its dedicated page" />
      {isLoading && <Spinner />}

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <div className="lg:col-span-1 space-y-4">
          {Object.entries(byCategory).map(([category, entries]) => (
            <div key={category} className="rounded-lg border border-slate-200 bg-white p-3">
              <h3 className="mb-2 text-xs font-semibold uppercase text-slate-400">{category}</h3>
              <div className="space-y-1">
                {entries.map((entry) => (
                  <button
                    key={entry.code}
                    onClick={() => open(entry)}
                    className={`flex w-full items-center justify-between rounded-md px-2 py-1.5 text-left text-sm hover:bg-slate-50 ${selected?.code === entry.code ? 'bg-primary-50 text-primary-700' : 'text-slate-700'}`}
                  >
                    <span>{entry.name}</span>
                    {entry.isExecutable && <StatusBadge tone="info">Run</StatusBadge>}
                  </button>
                ))}
              </div>
            </div>
          ))}
        </div>

        <div className="lg:col-span-2">
          {selected ? (
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <h3 className="mb-1 text-sm font-semibold text-slate-800">{selected.name}</h3>
              {selected.description && <p className="mb-3 text-xs text-slate-500">{selected.description}</p>}
              <DateRangeFilter
                fromDate={fromDate} toDate={toDate}
                onFromDate={(v) => setRange((r) => ({ ...r, fromDate: v }))} onToDate={(v) => setRange((r) => ({ ...r, toDate: v }))}
              />
              <div className="mb-4 flex gap-2">
                <Button onClick={run} loading={execute.isPending}>Run</Button>
                {result && (
                  <>
                    <Button variant="secondary" onClick={() => download('csv')}>Export CSV</Button>
                    <Button variant="secondary" onClick={() => download('excel')}>Export Excel</Button>
                    <Button variant="secondary" onClick={() => download('pdf')}>Export PDF</Button>
                  </>
                )}
              </div>
              {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

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
            </div>
          ) : (
            <p className="py-10 text-center text-sm text-slate-400">Select a report from the catalog.</p>
          )}
        </div>
      </div>
    </>
  )
}
