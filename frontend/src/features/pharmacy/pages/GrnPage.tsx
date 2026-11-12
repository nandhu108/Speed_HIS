import { useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDrugs, useItems, useVendors } from '../../masters/api'
import { ProductPicker } from '../components/ProductPicker'
import { useCreateGrn, useGrns, usePostGrn, usePurchaseOrder, useStores } from '../api'
import { DOCUMENT_STATUS_LABELS, type DocumentStatus, type GrnListItem } from '../types'
import { compactErrors, hasErrors, requiredSelect, type FieldErrors } from '../../../lib/formValidation'

/** Set by PurchaseOrdersPage's "Receive" action — see docs/modules/pharmacy.md's "GRN ↔
 * Purchase Order linkage" note. Only the PO id is carried; CreateModal fetches the rest so
 * it always reflects the PO's current (possibly already-partial) received quantities. */
interface GrnPrefill {
  purchaseOrderId: number
}

const statusTone: Record<DocumentStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'neutral', 2: 'success', 3: 'danger',
}

interface GrnLine {
  drugId: string
  itemId: string
  batchNumber: string
  expiryDate: string
  mrp: string
  purchaseRate: string
  receivedQuantity: string
  freeQuantity: string
  taxPercent: string
}

function emptyLine(): GrnLine {
  return {
    drugId: '', itemId: '', batchNumber: '', expiryDate: '', mrp: '',
    purchaseRate: '', receivedQuantity: '', freeQuantity: '0', taxPercent: '0',
  }
}

export function GrnPage() {
  const { hasPermission } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  // Captured once from router state at mount (lazy initializer) rather than re-read from
  // location.state on every render, since the effect below clears that state right after —
  // re-deriving it later would just see the clear.
  const [prefill] = useState<GrnPrefill | null>(() => (location.state as { grn?: GrnPrefill } | null)?.grn ?? null)
  const [page, setPage] = useState(1)
  const [creating, setCreating] = useState(!!prefill)
  const { data, isLoading } = useGrns({ page, pageSize: 20 })
  const post = usePostGrn()
  const [error, setError] = useState<string | null>(null)

  const canCreate = hasPermission('PHARMACY.GRN.CREATE')

  // Clear the router state once mounted so a later manual "New GRN" click (or a
  // back/forward navigation) doesn't keep reopening the prefilled modal.
  useEffect(() => {
    if (prefill) navigate(location.pathname, { replace: true, state: null })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const columns: Column<GrnListItem>[] = [
    { header: 'GRN No', cell: (row) => <span className="font-mono text-xs">{row.grnNumber}</span> },
    { header: 'Date', cell: (row) => row.receivedDate },
    { header: 'Vendor', cell: (row) => row.vendorName },
    { header: 'Total', className: 'text-right', cell: (row) => row.totalAmount.toFixed(2) },
    {
      header: 'Status',
      cell: (row) => <StatusBadge tone={statusTone[row.status]}>{DOCUMENT_STATUS_LABELS[row.status]}</StatusBadge>,
    },
    {
      header: '',
      className: 'text-right',
      cell: (row) =>
        canCreate && row.status === 1 ? (
          <Button
            variant="secondary"
            onClick={async () => {
              try {
                await post.mutateAsync(row.id)
              } catch (err) {
                setError(errorMessage(err))
              }
            }}
          >
            Post
          </Button>
        ) : null,
    },
  ]

  return (
    <>
      <PageHeader
        title="Goods Receipt Notes"
        subtitle="Receive stock against a vendor invoice — posting creates/tops up batches and writes stock in"
        actions={canCreate && <Button onClick={() => setCreating(true)}>New GRN</Button>}
      />

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <DataTable columns={columns} result={data} isLoading={isLoading} page={page} onPageChange={setPage} rowKey={(row) => row.id} />

      {creating && <CreateModal purchaseOrderId={prefill?.purchaseOrderId ?? null} onClose={() => setCreating(false)} />}
    </>
  )
}

function CreateModal({ purchaseOrderId, onClose }: { purchaseOrderId: number | null; onClose: () => void }) {
  const create = useCreateGrn()
  const { data: vendorsResult } = useVendors({ page: 1, pageSize: 200 })
  const { data: storesResult } = useStores()
  const { data: drugsResult } = useDrugs({ page: 1, pageSize: 500 })
  const { data: itemsResult } = useItems({ page: 1, pageSize: 500 })
  const { data: purchaseOrder, isFetching: isLoadingPo } = usePurchaseOrder(purchaseOrderId)
  const [vendorId, setVendorId] = useState('')
  const [storeId, setStoreId] = useState('')
  const [invoiceNumber, setInvoiceNumber] = useState('')
  const [invoiceDate, setInvoiceDate] = useState('')
  const [lines, setLines] = useState<GrnLine[]>([emptyLine()])
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [poLinesPrefilled, setPoLinesPrefilled] = useState(false)

  // Prefill vendor + one line per still-outstanding PO item (ordered − already received),
  // once, as soon as the PO detail loads — batch/expiry/MRP still can't come from a PO
  // (they're only known at physical receipt), so those three fields are left for the user.
  useEffect(() => {
    if (!purchaseOrder || poLinesPrefilled) return
    setVendorId(String(purchaseOrder.vendorId))
    const outstanding = purchaseOrder.items
      .filter((i) => i.orderedQuantity - i.receivedQuantity > 0)
      .map((i) => ({
        ...emptyLine(),
        drugId: i.drugId ? String(i.drugId) : '',
        itemId: i.itemId ? String(i.itemId) : '',
        purchaseRate: String(i.rate),
        taxPercent: String(i.taxPercent),
        receivedQuantity: String(i.orderedQuantity - i.receivedQuantity),
      }))
    setLines(outstanding.length > 0 ? outstanding : [emptyLine()])
    setPoLinesPrefilled(true)
  }, [purchaseOrder, poLinesPrefilled])

  const updateLine = (idx: number, patch: Partial<GrnLine>) =>
    setLines(lines.map((l, i) => (i === idx ? { ...l, ...patch } : l)))

  const submit = async () => {
    setError(null)
    const items = lines
      .filter((l) => (l.drugId || l.itemId) && l.batchNumber && l.purchaseRate && l.receivedQuantity && (l.itemId || (l.expiryDate && l.mrp)))
      .map((l) => ({
        drugId: l.drugId ? Number(l.drugId) : null,
        itemId: l.itemId ? Number(l.itemId) : null,
        batchNumber: l.batchNumber,
        expiryDate: l.expiryDate || null,
        mrp: l.mrp ? Number(l.mrp) : null,
        purchaseRate: Number(l.purchaseRate),
        receivedQuantity: Number(l.receivedQuantity),
        freeQuantity: Number(l.freeQuantity || 0),
        taxPercent: Number(l.taxPercent || 0),
      }))
    const errors = compactErrors({
      vendorId: requiredSelect(vendorId, 'Vendor'),
      storeId: requiredSelect(storeId, 'Store'),
      // The line filter above silently drops incomplete rows, so an all-incomplete grid used to
      // leave the button doing nothing at all. Spell out what a complete row needs instead.
      items:
        items.length === 0
          ? 'Add at least one complete line — each needs a drug or item, batch number, purchase rate and received quantity (drugs also need expiry and MRP).'
          : undefined,
    })
    setFieldErrors(errors)
    if (hasErrors(errors)) return
    try {
      await create.mutateAsync({
        purchaseOrderId,
        vendorId: Number(vendorId),
        storeId: Number(storeId),
        supplierInvoiceNumber: invoiceNumber || null,
        supplierInvoiceDate: invoiceDate || null,
        items,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={purchaseOrderId ? `New GRN — against ${purchaseOrder?.poNumber ?? 'PO'}` : 'New GRN'}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={create.isPending || isLoadingPo}>
            Create draft
          </Button>
        </>
      }
    >
      <div className="max-h-[65vh] space-y-3 overflow-y-auto">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        {fieldErrors.items && (
          <p role="alert" className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">
            {fieldErrors.items}
          </p>
        )}
        {purchaseOrderId && (
          <p className="rounded-md border border-blue-200 bg-blue-50 px-3 py-2 text-sm text-blue-800">
            {isLoadingPo
              ? 'Loading purchase order…'
              : `Receiving against ${purchaseOrder?.poNumber} — vendor and outstanding quantities are prefilled from the order; posting will roll the received quantity back onto it. Batch no, expiry and MRP aren't on the PO, so still fill those in per line from the delivery.`}
          </p>
        )}
        <div className="grid grid-cols-2 gap-3">
          <Select
            label="Vendor"
            required
            disabled={!!purchaseOrderId}
            error={fieldErrors.vendorId}
            placeholder="Select…"
            value={vendorId}
            onChange={(e) => setVendorId(e.target.value)}
            options={(vendorsResult?.items ?? []).map((v) => ({ value: v.id, label: v.name }))}
          />
          <Select
            label="Store"
            required
            error={fieldErrors.storeId}
            placeholder="Select…"
            value={storeId}
            onChange={(e) => setStoreId(e.target.value)}
            options={(storesResult ?? []).map((s) => ({ value: s.id, label: s.name }))}
          />
          <Input label="Supplier invoice no" value={invoiceNumber} onChange={(e) => setInvoiceNumber(e.target.value)} />
          <Input label="Supplier invoice date" type="date" value={invoiceDate} onChange={(e) => setInvoiceDate(e.target.value)} />
        </div>

        {lines.map((line, idx) => (
          <div key={idx} className="space-y-2 rounded-md border border-slate-200 p-3">
            <ProductPicker
              value={line}
              onChange={(v) => updateLine(idx, v)}
              drugs={drugsResult?.items ?? []}
              items={itemsResult?.items ?? []}
            />
            <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
              <Input label="Batch no" value={line.batchNumber} onChange={(e) => updateLine(idx, { batchNumber: e.target.value })} />
              <Input
                label={line.itemId ? 'Expiry (optional)' : 'Expiry'}
                type="date"
                value={line.expiryDate}
                onChange={(e) => updateLine(idx, { expiryDate: e.target.value })}
              />
              <Input
                label={line.itemId ? 'MRP (optional)' : 'MRP'}
                type="number"
                value={line.mrp}
                onChange={(e) => updateLine(idx, { mrp: e.target.value })}
              />
              <Input label="Purchase rate" type="number" value={line.purchaseRate} onChange={(e) => updateLine(idx, { purchaseRate: e.target.value })} />
              <Input label="Qty" type="number" value={line.receivedQuantity} onChange={(e) => updateLine(idx, { receivedQuantity: e.target.value })} />
              <Input label="Free qty" type="number" value={line.freeQuantity} onChange={(e) => updateLine(idx, { freeQuantity: e.target.value })} />
              <Input label="Tax %" type="number" value={line.taxPercent} onChange={(e) => updateLine(idx, { taxPercent: e.target.value })} />
            </div>
          </div>
        ))}
        <Button variant="secondary" onClick={() => setLines([...lines, emptyLine()])}>
          Add line
        </Button>
      </div>
    </Modal>
  )
}
