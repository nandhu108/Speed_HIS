import { useMemo, useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import {
  useCashierShiftCollection,
  useCashierShifts,
  useCloseCashierShift,
  useMyCashierShift,
  useOpenCashierShift,
  useReceiveCashierShift,
} from '../api'
import { CASHIER_SHIFT_STATUS_LABELS, type CashierShift, type CashierShiftStatus } from '../types'

const statusTone: Record<CashierShiftStatus, 'success' | 'warning' | 'info' | 'neutral'> = {
  1: 'info',
  2: 'warning',
  3: 'success',
}

export function CashierShiftsPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('BILLING.CASHIERSHIFT.MANAGE')
  const canReceive = hasPermission('BILLING.CASHIERSHIFT.RECEIVE')

  const { data: myShift } = useMyCashierShift()
  const receive = useReceiveCashierShift()

  const [opening, setOpening] = useState(false)
  const [closing, setClosing] = useState(false)
  const [page, setPage] = useState(1)

  const { data, isLoading } = useCashierShifts({ page, pageSize: 20 })

  const columns: Column<CashierShift>[] = [
    { header: 'Cashier', cell: (row) => row.userName },
    { header: 'Opened', cell: (row) => new Date(row.openedAt).toLocaleString() },
    { header: 'Opening Float', className: 'text-right', cell: (row) => row.openingFloat.toFixed(2) },
    {
      header: 'Status',
      cell: (row) => <StatusBadge tone={statusTone[row.status]}>{CASHIER_SHIFT_STATUS_LABELS[row.status]}</StatusBadge>,
    },
    { header: 'Received By', cell: (row) => row.receivedByName ?? '—' },
    {
      header: '',
      className: 'text-right',
      cell: (row) =>
        canReceive && row.status === 2 ? (
          <Button
            variant="secondary"
            onClick={async () => {
              if (window.confirm(`Confirm physical cash received from ${row.userName}?`)) {
                try {
                  await receive.mutateAsync(row.id)
                } catch (err) {
                  window.alert(errorMessage(err))
                }
              }
            }}
          >
            Receive
          </Button>
        ) : null,
    },
  ]

  return (
    <>
      <PageHeader
        title="Cashier Shifts"
        subtitle="Per-cashier cash reconciliation, alongside the branch-level Day Close"
        actions={
          canManage && (
            <div className="flex gap-2">
              {!myShift && <Button onClick={() => setOpening(true)}>Open my shift</Button>}
              {myShift && myShift.status === 1 && <Button onClick={() => setClosing(true)}>Close my shift</Button>}
            </div>
          )
        }
      />

      {myShift && myShift.status === 1 && (
        <p className="mb-4 rounded-md bg-blue-50 px-3 py-2 text-sm text-blue-700">
          You have an open shift since {new Date(myShift.openedAt).toLocaleString()} (opening float{' '}
          {myShift.openingFloat.toFixed(2)}).
        </p>
      )}

      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        rowKey={(row) => row.id}
        emptyMessage="No cashier shifts yet."
      />

      {opening && <OpenShiftModal onClose={() => setOpening(false)} />}
      {closing && myShift && <CloseShiftModal shift={myShift} onClose={() => setClosing(false)} />}
    </>
  )
}

function OpenShiftModal({ onClose }: { onClose: () => void }) {
  const open = useOpenCashierShift()
  const [openingFloat, setOpeningFloat] = useState('0')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await open.mutateAsync({ openingFloat: Number(openingFloat) })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Open cashier shift"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={open.isPending}>
            Open
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <Input
        label="Opening float"
        type="number"
        min={0}
        step="0.01"
        value={openingFloat}
        onChange={(e) => setOpeningFloat(e.target.value)}
      />
    </Modal>
  )
}

function CloseShiftModal({ shift, onClose }: { shift: CashierShift; onClose: () => void }) {
  const { data: collection = [] } = useCashierShiftCollection(shift.id)
  const close = useCloseCashierShift(shift.id)
  const [declared, setDeclared] = useState<Record<number, string>>({})
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)

  const modes = useMemo(() => collection, [collection])

  const submit = async () => {
    setError(null)
    try {
      await close.mutateAsync({
        declaredAmounts: modes.map((m) => ({
          paymentModeId: m.paymentModeId,
          declaredAmount: Number(declared[m.paymentModeId] || 0),
        })),
        notes: notes || null,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Close cashier shift"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button
            onClick={() => {
              if (window.confirm('Close this shift? This cannot be undone.')) submit()
            }}
            loading={close.isPending}
          >
            Close shift
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        {modes.length === 0 && <p className="text-sm text-slate-500">No payments collected on this shift yet.</p>}
        {modes.length > 0 && (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50 text-left">
                <th className="px-2 py-2 font-medium text-slate-600">Mode</th>
                <th className="px-2 py-2 text-right font-medium text-slate-600">System</th>
                <th className="px-2 py-2 text-right font-medium text-slate-600">Declared</th>
              </tr>
            </thead>
            <tbody>
              {modes.map((m) => (
                <tr key={m.paymentModeId} className="border-b border-slate-100 last:border-0">
                  <td className="px-2 py-2 text-slate-700">{m.paymentModeName}</td>
                  <td className="px-2 py-2 text-right text-slate-700">{m.amount.toFixed(2)}</td>
                  <td className="px-2 py-2 text-right">
                    <input
                      type="number"
                      step="0.01"
                      placeholder={m.amount.toFixed(2)}
                      className="w-28 rounded-md border border-slate-300 px-2 py-1 text-right text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
                      value={declared[m.paymentModeId] ?? ''}
                      onChange={(e) => setDeclared({ ...declared, [m.paymentModeId]: e.target.value })}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
      </div>
    </Modal>
  )
}
