import { useState } from 'react'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { useDrugs, useItems } from '../../masters/api'
import { useBatchLedger, useBatches, useStores } from '../api'
import { MOVEMENT_TYPE_LABELS, type Batch } from '../types'

export function StockExplorerPage() {
  const { data: storesResult } = useStores()
  const { data: drugsResult } = useDrugs({ page: 1, pageSize: 500 })
  const { data: itemsResult } = useItems({ page: 1, pageSize: 500 })
  const [storeId, setStoreId] = useState('')
  const [drugId, setDrugId] = useState('')
  const [itemId, setItemId] = useState('')
  const [viewingBatch, setViewingBatch] = useState<Batch | null>(null)

  const { data: batches = [], isLoading } = useBatches(
    drugId ? Number(drugId) : undefined,
    itemId ? Number(itemId) : undefined,
    storeId ? Number(storeId) : undefined,
    true,
  )

  return (
    <>
      <PageHeader title="Stock Explorer" subtitle="Batch-level stock across stores" />

      <div className="mb-4 flex gap-3">
        <Select
          label="Store"
          placeholder="All stores"
          value={storeId}
          onChange={(e) => setStoreId(e.target.value)}
          options={(storesResult ?? []).map((s) => ({ value: s.id, label: s.name }))}
        />
        <Select
          label="Drug"
          placeholder="All drugs"
          value={drugId}
          onChange={(e) => {
            setDrugId(e.target.value)
            if (e.target.value) setItemId('')
          }}
          options={(drugsResult?.items ?? []).map((d) => ({ value: d.id, label: d.name }))}
        />
        <Select
          label="Item"
          placeholder="All general items"
          value={itemId}
          onChange={(e) => {
            setItemId(e.target.value)
            if (e.target.value) setDrugId('')
          }}
          options={(itemsResult?.items ?? []).map((i) => ({ value: i.id, label: i.name }))}
        />
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Product</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Store</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Batch</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Expiry</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">MRP</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">On Hand</th>
            </tr>
          </thead>
          <tbody>
            {batches.map((b) => (
              <tr
                key={b.id}
                className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50"
                onClick={() => setViewingBatch(b)}
              >
                <td className="px-4 py-2.5 text-slate-700">{b.productName}</td>
                <td className="px-4 py-2.5 text-slate-700">{b.storeName}</td>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{b.batchNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{b.expiryDate ?? '—'}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{b.mrp?.toFixed(2) ?? '—'}</td>
                <td className="px-4 py-2.5 text-right font-medium text-slate-800">{b.quantityOnHand}</td>
              </tr>
            ))}
            {!isLoading && batches.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  No stock found.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {viewingBatch && <LedgerModal batch={viewingBatch} onClose={() => setViewingBatch(null)} />}
    </>
  )
}

function LedgerModal({ batch, onClose }: { batch: Batch; onClose: () => void }) {
  const { data: entries = [], isLoading } = useBatchLedger(batch.id)
  return (
    <Modal open title={`${batch.productName} — ${batch.batchNumber}`} onClose={onClose} footer={null}>
      {isLoading && <Spinner />}
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 text-left">
            <th className="py-1.5 font-medium text-slate-600">Movement</th>
            <th className="py-1.5 text-right font-medium text-slate-600">Qty</th>
            <th className="py-1.5 text-right font-medium text-slate-600">Balance</th>
            <th className="py-1.5 font-medium text-slate-600">When</th>
          </tr>
        </thead>
        <tbody>
          {entries.map((e) => (
            <tr key={e.id} className="border-b border-slate-100 last:border-0">
              <td className="py-1.5 text-slate-700">{MOVEMENT_TYPE_LABELS[e.movementType]}</td>
              <td className="py-1.5 text-right text-slate-700">{e.quantity}</td>
              <td className="py-1.5 text-right font-medium text-slate-800">{e.runningBalance}</td>
              <td className="py-1.5 text-slate-500">{new Date(e.movementAt).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </Modal>
  )
}
