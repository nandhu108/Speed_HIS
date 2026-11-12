import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { PatientPicker } from '../../appointments/components/PatientPicker'
import { usePaymentModes } from '../../billing/api'
import { useDrugs } from '../../masters/api'
import type { PatientListItem } from '../../patients/types'
import { useBatchAvailability, useCreateSale, useStores } from '../api'

interface CartLine {
  drugId: number
  drugName: string
  quantity: number
  discountPercent: number
}

/** Set by OPD's ConsultationWorkspace "Send to Pharmacy" action — see docs/modules/pharmacy.md's "OPD prescription bridge" note. */
interface PrescriptionPrefill {
  prescriptionId: number
  patientId: number
  patientName: string
  patientUhid: string
  items: { drugId: number; drugName: string }[]
}

export function PosSalePage() {
  const navigate = useNavigate()
  const location = useLocation()
  const prefill = (location.state as { prescription?: PrescriptionPrefill } | null)?.prescription ?? null
  const { data: storesResult } = useStores()
  const { data: drugsResult } = useDrugs({ page: 1, pageSize: 500 })
  const { data: paymentModes = [] } = usePaymentModes()
  const createSale = useCreateSale()

  const [storeId, setStoreId] = useState('')
  const [paymentModeId, setPaymentModeId] = useState('')
  const [patient, setPatient] = useState<PatientListItem | null>(null)
  const [cart, setCart] = useState<CartLine[]>(
    prefill?.items.map((i) => ({ drugId: i.drugId, drugName: i.drugName, quantity: 1, discountPercent: 0 })) ?? [],
  )
  const [drugId, setDrugId] = useState('')
  const [quantity, setQuantity] = useState('1')
  const [discountPercent, setDiscountPercent] = useState('0')
  const [error, setError] = useState<string | null>(null)

  const { data: availability = [] } = useBatchAvailability(drugId ? Number(drugId) : null, storeId ? Number(storeId) : null)
  const totalAvailable = availability.reduce((sum, b) => sum + b.available, 0)

  const addLine = () => {
    if (!drugId || !quantity) return
    const drug = drugsResult?.items.find((d) => d.id === Number(drugId))
    if (!drug) return
    setCart([...cart, { drugId: Number(drugId), drugName: drug.name, quantity: Number(quantity), discountPercent: Number(discountPercent || 0) }])
    setDrugId('')
    setQuantity('1')
    setDiscountPercent('0')
  }

  const submit = async () => {
    if (!storeId || !paymentModeId || cart.length === 0) return
    setError(null)
    try {
      const sale = await createSale.mutateAsync({
        storeId: Number(storeId),
        patientId: prefill?.patientId ?? patient?.id ?? null,
        prescriptionId: prefill?.prescriptionId ?? null,
        paymentModeId: Number(paymentModeId),
        items: cart.map((c) => ({ drugId: c.drugId, quantity: c.quantity, discountPercent: c.discountPercent })),
      })
      navigate(`/pharmacy/sales/${sale.id}`)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader title="POS Sale" subtitle="Over-the-counter pharmacy sale — batches are allocated earliest-expiry-first" />

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="grid grid-cols-3 gap-6">
        <div className="col-span-2 space-y-4">
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <div className="grid grid-cols-2 gap-3">
              <Select
                label="Store"
                placeholder="Select…"
                value={storeId}
                onChange={(e) => setStoreId(e.target.value)}
                options={(storesResult ?? []).map((s) => ({ value: s.id, label: s.name }))}
              />
              <Select
                label="Payment mode"
                placeholder="Select…"
                value={paymentModeId}
                onChange={(e) => setPaymentModeId(e.target.value)}
                options={paymentModes.filter((m) => m.isActive).map((m) => ({ value: m.id, label: m.name }))}
              />
              <div>
                {prefill ? (
                  <div className="rounded-md border border-blue-200 bg-blue-50 px-3 py-2 text-sm text-blue-800">
                    From OPD prescription for <span className="font-medium">{prefill.patientName}</span>{' '}
                    <span className="font-mono text-xs">({prefill.patientUhid})</span>
                  </div>
                ) : (
                  <>
                    <PatientPicker selected={patient} onSelect={setPatient} />
                    <p className="mt-1 text-xs text-slate-400">Optional — leave blank for a walk-in OTC sale.</p>
                  </>
                )}
              </div>
            </div>
          </div>

          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <h2 className="mb-3 text-sm font-semibold text-slate-800">Add drug</h2>
            <div className="grid grid-cols-4 gap-2">
              <Select
                label="Drug"
                className="col-span-2"
                placeholder="Select…"
                value={drugId}
                onChange={(e) => setDrugId(e.target.value)}
                options={(drugsResult?.items ?? []).map((d) => ({ value: d.id, label: d.name }))}
              />
              <Input label="Quantity" type="number" min={1} value={quantity} onChange={(e) => setQuantity(e.target.value)} />
              <Input label="Discount %" type="number" min={0} max={100} value={discountPercent} onChange={(e) => setDiscountPercent(e.target.value)} />
            </div>
            {drugId && storeId && (
              <p className="mt-2 text-xs text-slate-500">Available: {totalAvailable} across {availability.length} batch(es)</p>
            )}
            <div className="mt-3">
              <Button variant="secondary" onClick={addLine} disabled={!drugId || !storeId || !quantity}>
                Add to cart
              </Button>
            </div>
          </div>
        </div>

        <div className="rounded-lg border border-slate-200 bg-white p-4">
          <h2 className="mb-3 text-sm font-semibold text-slate-800">Cart</h2>
          {cart.length === 0 && <p className="text-sm text-slate-400">No items yet.</p>}
          <ul className="space-y-2">
            {cart.map((line, idx) => (
              <li key={idx} className="flex items-center justify-between border-b border-slate-100 pb-2 text-sm">
                <div>
                  <p className="font-medium text-slate-700">{line.drugName}</p>
                  <p className="text-xs text-slate-400">
                    Qty {line.quantity} {line.discountPercent > 0 && `· ${line.discountPercent}% off`}
                  </p>
                </div>
                <button
                  type="button"
                  className="text-xs text-red-600 hover:underline"
                  onClick={() => setCart(cart.filter((_, i) => i !== idx))}
                >
                  Remove
                </button>
              </li>
            ))}
          </ul>
          <Button className="mt-4 w-full" onClick={submit} loading={createSale.isPending} disabled={!storeId || !paymentModeId || cart.length === 0}>
            Checkout
          </Button>
        </div>
      </div>
    </>
  )
}
