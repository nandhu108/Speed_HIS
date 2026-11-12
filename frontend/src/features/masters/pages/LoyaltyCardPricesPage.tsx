import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import {
  useDeleteLoyaltyCardPrice,
  useLoyaltyCardPrice,
  useLoyaltyCardPrices,
  useLoyaltyCardTypes,
  useSaveLoyaltyCardPrice,
} from '../api'

interface EditorLine {
  durationMonths: string
  amount: string
}

interface EditorState {
  id?: number
  loyaltyCardTypeId: string
  effectiveFrom: string
  effectiveTo: string
  items: EditorLine[]
  isActive: boolean
}

export function LoyaltyCardPricesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.LOYALTYCARDPRICE.MANAGE')
  const { data: rows = [], isLoading } = useLoyaltyCardPrices()
  const { data: cardTypes = [] } = useLoyaltyCardTypes()
  const save = useSaveLoyaltyCardPrice()
  const remove = useDeleteLoyaltyCardPrice()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [editId, setEditId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  const { data: detail } = useLoyaltyCardPrice(editId)
  if (editId !== null && detail && editor === null) {
    setEditor({
      id: detail.id,
      loyaltyCardTypeId: String(detail.loyaltyCardTypeId),
      effectiveFrom: detail.effectiveFrom,
      effectiveTo: detail.effectiveTo ?? '',
      items: detail.items.map((item) => ({ durationMonths: String(item.durationMonths), amount: String(item.amount) })),
      isActive: detail.isActive,
    })
  }

  const closeEditor = () => {
    setEditor(null)
    setEditId(null)
  }

  const submit = async () => {
    if (!editor) return
    setError(null)
    try {
      await save.mutateAsync({
        id: editor.id,
        loyaltyCardTypeId: Number(editor.loyaltyCardTypeId),
        effectiveFrom: editor.effectiveFrom,
        effectiveTo: editor.effectiveTo || null,
        items: editor.items.map((item) => ({ durationMonths: Number(item.durationMonths), amount: Number(item.amount) })),
        isActive: editor.isActive,
      })
      closeEditor()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader
        title="Loyalty Card Prices"
        subtitle="The Loyalty Card Price Matrix — card type x effective window x duration -> price"
        actions={
          canManage && (
            <Button
              onClick={() => {
                setError(null)
                setEditId(null)
                setEditor({ loyaltyCardTypeId: '', effectiveFrom: '', effectiveTo: '', items: [], isActive: true })
              }}
            >
              New price list
            </Button>
          )
        }
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Card Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Effective From</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Effective To</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Durations</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-medium text-slate-700">{row.loyaltyCardTypeName}</td>
                <td className="px-4 py-2.5 text-slate-700">{row.effectiveFrom}</td>
                <td className="px-4 py-2.5 text-slate-500">{row.effectiveTo ?? 'Open-ended'}</td>
                <td className="px-4 py-2.5 text-slate-500">{row.itemCount}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={row.isActive ? 'success' : 'neutral'}>
                    {row.isActive ? 'Active' : 'Inactive'}
                  </StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canManage && (
                    <div className="flex justify-end gap-1">
                      <Button
                        variant="ghost"
                        onClick={() => {
                          setError(null)
                          setEditor(null)
                          setEditId(row.id)
                        }}
                      >
                        Edit
                      </Button>
                      <Button
                        variant="ghost"
                        className="text-red-600"
                        onClick={async () => {
                          if (window.confirm(`Delete this price list for '${row.loyaltyCardTypeName}'?`)) {
                            try {
                              await remove.mutateAsync(row.id)
                            } catch (err) {
                              window.alert(errorMessage(err))
                            }
                          }
                        }}
                      >
                        Delete
                      </Button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && rows.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  No price lists yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit price list' : 'New price list'}
          onClose={closeEditor}
          footer={
            <>
              <Button variant="secondary" onClick={closeEditor}>
                Cancel
              </Button>
              <Button onClick={submit} loading={save.isPending}>
                Save
              </Button>
            </>
          }
        >
          <div className="space-y-3">
            {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
            <Select
              label="Card Type"
              placeholder="Select…"
              value={editor.loyaltyCardTypeId}
              onChange={(e) => setEditor({ ...editor, loyaltyCardTypeId: e.target.value })}
              options={cardTypes.map((t) => ({ value: t.id, label: t.name }))}
            />
            <div className="grid grid-cols-2 gap-3">
              <Input
                label="Effective from"
                type="date"
                value={editor.effectiveFrom}
                onChange={(e) => setEditor({ ...editor, effectiveFrom: e.target.value })}
              />
              <Input
                label="Effective to (optional)"
                type="date"
                value={editor.effectiveTo}
                onChange={(e) => setEditor({ ...editor, effectiveTo: e.target.value })}
              />
            </div>

            <div>
              <span className="mb-1 block text-sm font-medium text-slate-700">Duration / Amount lines</span>
              {editor.items.length > 0 && (
                <table className="mb-2 w-full text-sm">
                  <tbody>
                    {editor.items.map((item, index) => (
                      <tr key={index} className="border-b border-slate-100 last:border-0">
                        <td className="py-1 pr-2">{item.durationMonths} month(s)</td>
                        <td className="w-28 py-1 pr-2">
                          <Input
                            type="number"
                            min={0}
                            step="0.01"
                            aria-label="Amount"
                            value={item.amount}
                            onChange={(e) => {
                              const items = [...editor.items]
                              items[index] = { ...item, amount: e.target.value }
                              setEditor({ ...editor, items })
                            }}
                          />
                        </td>
                        <td className="w-10 py-1 text-right">
                          <Button
                            variant="ghost"
                            className="px-2 py-1 text-xs text-red-600"
                            onClick={() => setEditor({ ...editor, items: editor.items.filter((_, i) => i !== index) })}
                          >
                            ✕
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
              <AddLineForm onAdd={(line) => setEditor({ ...editor, items: [...editor.items, line] })} />
            </div>

            <label className="flex items-center gap-2 text-sm text-slate-700">
              <input
                type="checkbox"
                checked={editor.isActive}
                onChange={(e) => setEditor({ ...editor, isActive: e.target.checked })}
              />
              Active
            </label>
          </div>
        </Modal>
      )}
    </>
  )
}

function AddLineForm({ onAdd }: { onAdd: (line: EditorLine) => void }) {
  const [durationMonths, setDurationMonths] = useState('12')
  const [amount, setAmount] = useState('')

  const add = () => {
    if (!durationMonths || !amount) return
    onAdd({ durationMonths, amount })
    setDurationMonths('12')
    setAmount('')
  }

  return (
    <div className="flex items-end gap-2">
      <Input label="Duration (months)" type="number" min={1} className="w-32" value={durationMonths} onChange={(e) => setDurationMonths(e.target.value)} />
      <Input label="Amount" type="number" min={0} step="0.01" className="w-28" value={amount} onChange={(e) => setAmount(e.target.value)} />
      <Button variant="secondary" onClick={add} disabled={!durationMonths || !amount}>
        Add
      </Button>
    </div>
  )
}
