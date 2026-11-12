import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useAudit, useAudits, useCompleteAudit, useCreateAudit, useRecordAuditCount, useStores } from '../api'
import { AUDIT_STATUS_LABELS, type StockAuditStatus } from '../types'

const statusTone: Record<StockAuditStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'warning', 2: 'success',
}

export function StockAuditsPage() {
  const { hasPermission } = useAuth()
  const canAudit = hasPermission('PHARMACY.STOCK.AUDIT')
  const { data: audits = [], isLoading } = useAudits()
  const { data: storesResult } = useStores()
  const createAudit = useCreateAudit()
  const [storeId, setStoreId] = useState('')
  const [openId, setOpenId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  const start = async () => {
    if (!storeId) return
    setError(null)
    try {
      const id = await createAudit.mutateAsync(Number(storeId))
      setOpenId(id)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader title="Stock Audits" subtitle="Physical count sheet — completing posts a variance adjustment per batch" />

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      {canAudit && (
        <div className="mb-6 flex items-end gap-3 rounded-lg border border-slate-200 bg-white p-4">
          <Select
            label="Store"
            placeholder="Select…"
            value={storeId}
            onChange={(e) => setStoreId(e.target.value)}
            options={(storesResult ?? []).map((s) => ({ value: s.id, label: s.name }))}
          />
          <Button onClick={start} loading={createAudit.isPending} disabled={!storeId}>
            Start audit
          </Button>
        </div>
      )}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Audit No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Store</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {audits.map((a) => (
              <tr key={a.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{a.auditNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.auditDate}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.storeName}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={statusTone[a.status]}>{AUDIT_STATUS_LABELS[a.status]}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  <Button variant="ghost" onClick={() => setOpenId(a.id)}>
                    Open
                  </Button>
                </td>
              </tr>
            ))}
            {!isLoading && audits.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                  No audits yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {openId !== null && <AuditModal auditId={openId} onClose={() => setOpenId(null)} />}
    </>
  )
}

function AuditModal({ auditId, onClose }: { auditId: number; onClose: () => void }) {
  const { data: audit, isLoading } = useAudit(auditId)
  const recordCount = useRecordAuditCount(auditId)
  const complete = useCompleteAudit()
  const { hasPermission } = useAuth()
  const canAudit = hasPermission('PHARMACY.STOCK.AUDIT')
  const [counts, setCounts] = useState<Record<number, string>>({})
  const [error, setError] = useState<string | null>(null)

  const saveCount = async (batchId: number) => {
    const value = counts[batchId]
    if (value === undefined) return
    try {
      await recordCount.mutateAsync({ batchId, countedQuantity: Number(value) })
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const finish = async () => {
    setError(null)
    try {
      await complete.mutateAsync(auditId)
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={audit ? audit.auditNumber : 'Stock audit'}
      onClose={onClose}
      footer={
        audit?.status === 1 && canAudit ? (
          <>
            <Button variant="secondary" onClick={onClose}>
              Close
            </Button>
            <Button onClick={finish} loading={complete.isPending}>
              Complete audit
            </Button>
          </>
        ) : (
          <Button onClick={onClose}>Close</Button>
        )
      }
    >
      {isLoading && <Spinner />}
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      {audit && (
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 text-left">
              <th className="py-1.5 font-medium text-slate-600">Drug</th>
              <th className="py-1.5 font-medium text-slate-600">Batch</th>
              <th className="py-1.5 text-right font-medium text-slate-600">System</th>
              <th className="py-1.5 text-right font-medium text-slate-600">Counted</th>
            </tr>
          </thead>
          <tbody>
            {audit.items.map((item) => (
              <tr key={item.id} className="border-b border-slate-100 last:border-0">
                <td className="py-1.5 text-slate-700">{item.productName}</td>
                <td className="py-1.5 font-mono text-xs text-slate-500">{item.batchNumber}</td>
                <td className="py-1.5 text-right text-slate-700">{item.systemQuantity}</td>
                <td className="py-1.5 text-right">
                  {audit.status === 1 && canAudit ? (
                    <input
                      type="number"
                      className="w-24 rounded-md border border-slate-300 px-2 py-1 text-right text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
                      defaultValue={item.countedQuantity}
                      onChange={(e) => setCounts({ ...counts, [item.batchId]: e.target.value })}
                      onBlur={() => saveCount(item.batchId)}
                    />
                  ) : (
                    <span className={item.variance !== 0 ? 'font-medium text-red-600' : ''}>{item.countedQuantity}</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </Modal>
  )
}
