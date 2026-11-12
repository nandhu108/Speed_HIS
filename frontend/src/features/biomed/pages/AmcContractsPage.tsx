import { useState } from 'react'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAmcContracts } from '../api'
import { CONTRACT_TYPE_LABELS } from '../types'

export function AmcContractsPage() {
  const [renewalDueOnly, setRenewalDueOnly] = useState(false)
  const { data: contracts = [], isLoading } = useAmcContracts(undefined, renewalDueOnly)

  return (
    <>
      <PageHeader title="AMC / CMC Contracts" subtitle="Vendor maintenance contracts and renewal alerts — new contracts are added from an equipment's detail page" />

      <label className="mb-3 flex items-center gap-2 text-sm text-slate-700">
        <input type="checkbox" checked={renewalDueOnly} onChange={(e) => setRenewalDueOnly(e.target.checked)} />
        Renewal due only
      </label>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Equipment</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Vendor</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Contract No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Period</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">Cost</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            </tr>
          </thead>
          <tbody>
            {contracts.map((c) => (
              <tr key={c.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{c.equipmentName}</td>
                <td className="px-4 py-2.5 text-slate-700">{c.vendorName}</td>
                <td className="px-4 py-2.5 text-slate-700">{CONTRACT_TYPE_LABELS[c.contractType]}</td>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{c.contractNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{c.startDate} → {c.endDate}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{c.cost.toFixed(2)}</td>
                <td className="px-4 py-2.5">{c.isRenewalDue && <StatusBadge tone="warning">Renewal due</StatusBadge>}</td>
              </tr>
            ))}
            {!isLoading && contracts.length === 0 && (
              <tr><td colSpan={7} className="px-4 py-10 text-center text-slate-400">No contracts found.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}
