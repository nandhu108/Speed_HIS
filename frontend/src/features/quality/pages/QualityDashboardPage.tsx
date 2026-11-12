import { useState } from 'react'
import { Button } from '../../../components/ui/Button'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatTile } from '../../reports/components/StatTile'
import { useComplianceDashboard, useKpiDashboard } from '../api'
import type { IndicatorTrendPoint } from '../types'

function firstOfMonthMinus(monthsAgo: number) {
  const d = new Date()
  d.setDate(1)
  d.setMonth(d.getMonth() - monthsAgo)
  return d.toISOString().slice(0, 10)
}

export function QualityDashboardPage() {
  const [tab, setTab] = useState<'kpi' | 'compliance'>('compliance')

  return (
    <>
      <PageHeader title="Compliance Dashboard" subtitle="NABH readiness at a glance — KPI trends and compliance posture" />
      <div className="mb-4 flex gap-2">
        <Button variant={tab === 'compliance' ? 'primary' : 'secondary'} onClick={() => setTab('compliance')}>Compliance</Button>
        <Button variant={tab === 'kpi' ? 'primary' : 'secondary'} onClick={() => setTab('kpi')}>KPI Trends</Button>
      </div>
      {tab === 'compliance' ? <ComplianceTab /> : <KpiTab />}
    </>
  )
}

function ComplianceTab() {
  const { data, isLoading } = useComplianceDashboard()
  if (isLoading || !data) return <Spinner />

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
        <StatTile label="Open Non-Conformances" value={data.openNonConformances} />
        <StatTile label="Open CAPAs" value={data.openCapas} />
        <StatTile label="Overdue CAPAs" value={data.overdueCapas} tone={data.overdueCapas > 0 ? 'danger' : 'success'} />
        <StatTile label="Open Risks" value={data.openRisks} />
        <StatTile label="Sentinel Events (YTD)" value={data.sentinelIncidentsThisYear} tone={data.sentinelIncidentsThisYear > 0 ? 'danger' : 'success'} />
        <StatTile label="Avg Audit Score" value={data.averageAuditScorePercent !== null ? `${data.averageAuditScorePercent}%` : '—'} />
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-3 font-medium text-slate-900">Evidence coverage by chapter ({data.evidenceCount} total)</h3>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-left text-xs text-slate-500">
              <th className="py-1.5 font-medium">Chapter</th>
              <th className="py-1.5 font-medium">Documents</th>
            </tr>
          </thead>
          <tbody>
            {data.evidenceByChapter.map((c) => (
              <tr key={c.chapter} className="border-b border-slate-100 last:border-0">
                <td className="py-1.5 text-slate-700">{c.chapter}</td>
                <td className="py-1.5 text-slate-700">{c.count}</td>
              </tr>
            ))}
            {data.evidenceByChapter.length === 0 && (
              <tr><td colSpan={2} className="py-6 text-center text-slate-400">No evidence uploaded yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function KpiTab() {
  const fromPeriod = firstOfMonthMinus(11)
  const toPeriod = firstOfMonthMinus(0)
  const { data, isLoading } = useKpiDashboard(fromPeriod, toPeriod)

  if (isLoading) return <Spinner />

  const byIndicator = new Map<number, IndicatorTrendPoint[]>()
  for (const point of data?.points ?? []) {
    const existing = byIndicator.get(point.indicatorId) ?? []
    byIndicator.set(point.indicatorId, [...existing, point])
  }

  return (
    <div className="space-y-4">
      {[...byIndicator.entries()].map(([indicatorId, points]) => (
        <div key={indicatorId} className="rounded-lg border border-slate-200 bg-white p-4">
          <h3 className="mb-2 font-medium text-slate-900">{points[0].indicatorName} {points[0].unit && <span className="text-xs text-slate-400">({points[0].unit})</span>}</h3>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-left text-xs text-slate-500">
                <th className="py-1.5 font-medium">Period</th>
                <th className="py-1.5 font-medium">Value</th>
                <th className="py-1.5 font-medium">Target</th>
              </tr>
            </thead>
            <tbody>
              {points.map((p) => (
                <tr key={p.periodMonth} className="border-b border-slate-100 last:border-0">
                  <td className="py-1.5 text-slate-700">{p.periodMonth}</td>
                  <td className="py-1.5 text-slate-700">{p.computedValue ?? '—'}</td>
                  <td className="py-1.5 text-slate-500">{p.targetValue ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}
      {byIndicator.size === 0 && <p className="py-10 text-center text-slate-400">No indicator entries recorded for this period.</p>}
    </div>
  )
}
