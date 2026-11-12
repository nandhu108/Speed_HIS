import { useState } from 'react'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useLowStock, useNearExpiry } from '../api'

type Tab = 'expiry' | 'stock'

export function AlertsPage() {
  const [tab, setTab] = useState<Tab>('expiry')
  const { data: nearExpiry = [], isLoading: loadingExpiry } = useNearExpiry(90)
  const { data: lowStock = [], isLoading: loadingStock } = useLowStock()

  return (
    <>
      <PageHeader title="Alerts" subtitle="Near-expiry batches and products below their reorder level" />

      <div className="mb-4 flex gap-1 border-b border-slate-200">
        <button
          type="button"
          onClick={() => setTab('expiry')}
          className={`border-b-2 px-4 py-2 text-sm font-medium ${
            tab === 'expiry' ? 'border-primary-600 text-primary-700' : 'border-transparent text-slate-500 hover:text-slate-700'
          }`}
        >
          Near Expiry (90 days)
        </button>
        <button
          type="button"
          onClick={() => setTab('stock')}
          className={`border-b-2 px-4 py-2 text-sm font-medium ${
            tab === 'stock' ? 'border-primary-600 text-primary-700' : 'border-transparent text-slate-500 hover:text-slate-700'
          }`}
        >
          Low Stock
        </button>
      </div>

      {tab === 'expiry' && (
        <div className="rounded-lg border border-slate-200 bg-white">
          {loadingExpiry && <Spinner />}
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50 text-left">
                <th className="px-4 py-2.5 font-medium text-slate-600">Product</th>
                <th className="px-4 py-2.5 font-medium text-slate-600">Store</th>
                <th className="px-4 py-2.5 font-medium text-slate-600">Batch</th>
                <th className="px-4 py-2.5 font-medium text-slate-600">Expiry</th>
                <th className="px-4 py-2.5 text-right font-medium text-slate-600">Days Left</th>
                <th className="px-4 py-2.5 text-right font-medium text-slate-600">Qty</th>
              </tr>
            </thead>
            <tbody>
              {nearExpiry.map((b) => (
                <tr key={b.batchId} className="border-b border-slate-100 last:border-0">
                  <td className="px-4 py-2.5 text-slate-700">{b.productName}</td>
                  <td className="px-4 py-2.5 text-slate-700">{b.storeName}</td>
                  <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{b.batchNumber}</td>
                  <td className="px-4 py-2.5 text-slate-700">{b.expiryDate}</td>
                  <td className={`px-4 py-2.5 text-right ${b.daysToExpiry <= 30 ? 'font-medium text-red-600' : 'text-amber-600'}`}>
                    {b.daysToExpiry}
                  </td>
                  <td className="px-4 py-2.5 text-right text-slate-700">{b.quantityOnHand}</td>
                </tr>
              ))}
              {!loadingExpiry && nearExpiry.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                    Nothing expiring soon.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {tab === 'stock' && (
        <div className="rounded-lg border border-slate-200 bg-white">
          {loadingStock && <Spinner />}
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50 text-left">
                <th className="px-4 py-2.5 font-medium text-slate-600">Product</th>
                <th className="px-4 py-2.5 text-right font-medium text-slate-600">Reorder Level</th>
                <th className="px-4 py-2.5 text-right font-medium text-slate-600">On Hand</th>
              </tr>
            </thead>
            <tbody>
              {lowStock.map((d) => (
                <tr key={`${d.drugId ?? 'i'}-${d.itemId ?? 'd'}`} className="border-b border-slate-100 last:border-0">
                  <td className="px-4 py-2.5 text-slate-700">{d.productName}</td>
                  <td className="px-4 py-2.5 text-right text-slate-700">{d.reorderLevel}</td>
                  <td className="px-4 py-2.5 text-right font-medium text-red-600">{d.totalOnHand}</td>
                </tr>
              ))}
              {!loadingStock && lowStock.length === 0 && (
                <tr>
                  <td colSpan={3} className="px-4 py-10 text-center text-slate-400">
                    Nothing below its reorder level.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}
