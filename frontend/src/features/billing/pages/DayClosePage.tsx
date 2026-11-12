import { useMemo, useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useCloseDay, useDailyCollection, useDayClose } from '../api'

function today() {
  return new Date().toISOString().slice(0, 10)
}

export function DayClosePage() {
  const { hasPermission } = useAuth()
  const canClose = hasPermission('BILLING.DAYCLOSE.CLOSE')

  const [date, setDate] = useState(today())
  const { data: collection, isLoading: loadingCollection } = useDailyCollection(date)
  const { data: dayClose, isLoading: loadingClose } = useDayClose(date)
  const closeDay = useCloseDay()

  const [declared, setDeclared] = useState<Record<number, string>>({})
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)

  const modes = useMemo(() => collection?.byMode ?? [], [collection])
  const isClosed = dayClose?.status === 2

  const submit = async () => {
    setError(null)
    try {
      await closeDay.mutateAsync({
        date,
        declaredAmounts: modes.map((m) => ({
          paymentModeId: m.paymentModeId,
          declaredAmount: Number(declared[m.paymentModeId] || 0),
        })),
        notes: notes || null,
      })
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader title="Day Close" subtitle="Branch cash reconciliation — system vs. declared amounts by payment mode" />

      <div className="mb-6 max-w-xs">
        <Input label="Date" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
      </div>

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      {isClosed && dayClose && (
        <div className="mb-6 rounded-lg border border-slate-200 bg-white">
          <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
            <h2 className="text-sm font-semibold text-slate-800">Closed</h2>
            <StatusBadge tone="success">Closed {dayClose.closedAt ? new Date(dayClose.closedAt).toLocaleString() : ''}</StatusBadge>
          </div>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50 text-left">
                <th className="px-4 py-2.5 font-medium text-slate-600">Mode</th>
                <th className="px-4 py-2.5 text-right font-medium text-slate-600">System</th>
                <th className="px-4 py-2.5 text-right font-medium text-slate-600">Declared</th>
                <th className="px-4 py-2.5 text-right font-medium text-slate-600">Variance</th>
              </tr>
            </thead>
            <tbody>
              {dayClose.modeAmounts.map((m) => (
                <tr key={m.paymentModeId} className="border-b border-slate-100 last:border-0">
                  <td className="px-4 py-2.5 text-slate-700">{m.paymentModeName}</td>
                  <td className="px-4 py-2.5 text-right text-slate-700">{m.systemAmount.toFixed(2)}</td>
                  <td className="px-4 py-2.5 text-right text-slate-700">{m.declaredAmount.toFixed(2)}</td>
                  <td className={`px-4 py-2.5 text-right ${m.variance !== 0 ? 'font-medium text-red-600' : 'text-slate-700'}`}>
                    {m.variance.toFixed(2)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {dayClose.notes && <p className="border-t border-slate-200 px-4 py-3 text-sm text-slate-600">{dayClose.notes}</p>}
        </div>
      )}

      {!isClosed && (
        <div className="rounded-lg border border-slate-200 bg-white">
          <div className="border-b border-slate-200 px-4 py-3">
            <h2 className="text-sm font-semibold text-slate-800">Today's collection</h2>
          </div>
          {(loadingCollection || loadingClose) && <Spinner />}
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50 text-left">
                <th className="px-4 py-2.5 font-medium text-slate-600">Mode</th>
                <th className="px-4 py-2.5 text-right font-medium text-slate-600">Count</th>
                <th className="px-4 py-2.5 text-right font-medium text-slate-600">System amount</th>
                <th className="px-4 py-2.5 text-right font-medium text-slate-600">Declared amount</th>
              </tr>
            </thead>
            <tbody>
              {modes.map((m) => (
                <tr key={m.paymentModeId} className="border-b border-slate-100 last:border-0">
                  <td className="px-4 py-2.5 text-slate-700">{m.paymentModeName}</td>
                  <td className="px-4 py-2.5 text-right text-slate-700">{m.count}</td>
                  <td className="px-4 py-2.5 text-right text-slate-700">{m.amount.toFixed(2)}</td>
                  <td className="px-4 py-2.5 text-right">
                    <input
                      type="number"
                      step="0.01"
                      placeholder={m.amount.toFixed(2)}
                      className="w-32 rounded-md border border-slate-300 px-2 py-1 text-right text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
                      value={declared[m.paymentModeId] ?? ''}
                      onChange={(e) => setDeclared({ ...declared, [m.paymentModeId]: e.target.value })}
                    />
                  </td>
                </tr>
              ))}
              {!loadingCollection && modes.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-10 text-center text-slate-400">
                    No collection recorded for this date.
                  </td>
                </tr>
              )}
            </tbody>
            {modes.length > 0 && (
              <tfoot>
                <tr className="border-t border-slate-200 font-medium">
                  <td className="px-4 py-2.5 text-slate-800">Total</td>
                  <td />
                  <td className="px-4 py-2.5 text-right text-slate-800">{collection?.total.toFixed(2)}</td>
                  <td />
                </tr>
              </tfoot>
            )}
          </table>

          {canClose && modes.length > 0 && (
            <div className="border-t border-slate-200 p-4">
              <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} className="mb-3 max-w-md" />
              <Button
                onClick={() => {
                  if (window.confirm(`Close the books for ${date}? This cannot be undone.`)) submit()
                }}
                loading={closeDay.isPending}
              >
                Close day
              </Button>
            </div>
          )}
        </div>
      )}
    </>
  )
}
