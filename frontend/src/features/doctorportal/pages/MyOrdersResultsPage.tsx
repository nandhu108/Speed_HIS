import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { ResultsTrendChart } from '../../emr/components/ResultsTrendChart'
import { useResultsTrend } from '../../emr/api'
import { useLabTests } from '../../lab/api'
import { LAB_ORDER_STATUS_LABELS } from '../../lab/types'
import { useMyOrders } from '../api'

/** Orders & Results (plan §22: "cumulative lab trends") — the order list is new, but the trend chart is EMR's existing ResultsTrendChart/useResultsTrend wired to whichever of the doctor's own patients + test is picked below; no parallel trend computation. */
export function MyOrdersResultsPage() {
  const navigate = useNavigate()
  const { data: orders = [], isLoading } = useMyOrders()
  const { data: labTests = [] } = useLabTests()

  const patients = useMemo(() => {
    const seen = new Map<number, string>()
    for (const o of orders) seen.set(o.patientId, `${o.patientName} (${o.patientUhid})`)
    return [...seen.entries()].map(([id, label]) => ({ value: id, label }))
  }, [orders])

  const [patientId, setPatientId] = useState('')
  const [testId, setTestId] = useState('')
  const { data: trendSeries = [] } = useResultsTrend(patientId ? Number(patientId) : null, testId ? Number(testId) : null)

  return (
    <>
      <PageHeader title="Orders & Results" subtitle="Lab orders you've placed, and result trends for your patients" />

      <div className="mb-6 rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-3 text-sm font-semibold text-slate-700">Results Trend</h3>
        <div className="mb-3 grid grid-cols-1 gap-2 sm:grid-cols-2">
          <Select label="Patient" placeholder="Select patient" value={patientId} onChange={(e) => setPatientId(e.target.value)} options={patients} />
          <Select label="Test" placeholder="Select test" value={testId} onChange={(e) => setTestId(e.target.value)}
            options={labTests.map((t) => ({ value: t.id, label: t.name }))} />
        </div>
        {patientId && testId && (
          trendSeries.length > 0
            ? trendSeries.map((series) => <ResultsTrendChart key={series.parameterId} series={series} />)
            : <p className="py-6 text-center text-sm text-slate-400">No approved results yet for this test.</p>
        )}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Order</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Items</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            </tr>
          </thead>
          <tbody>
            {orders.map((o) => (
              <tr key={o.id} className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50" onClick={() => navigate(`/lab/orders/${o.id}`)}>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{o.orderNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{o.patientName} <span className="font-mono text-xs text-slate-400">({o.patientUhid})</span></td>
                <td className="px-4 py-2.5 text-slate-500">{o.orderDate}</td>
                <td className="px-4 py-2.5 text-slate-700">{o.itemCount}</td>
                <td className="px-4 py-2.5"><StatusBadge tone="info">{LAB_ORDER_STATUS_LABELS[o.status]}</StatusBadge></td>
              </tr>
            ))}
            {!isLoading && orders.length === 0 && (
              <tr><td colSpan={5} className="px-4 py-10 text-center text-slate-400">No lab orders placed yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}
