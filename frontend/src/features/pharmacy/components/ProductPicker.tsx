import { Select } from '../../../components/ui/Select'

interface ProductOption {
  id: number
  name: string
}

interface ProductPickerValue {
  drugId: string
  itemId: string
}

/// Line-item picker for Phase 13's "exactly one of drug or general item" pattern —
/// a type toggle plus the matching Select, used by PR/PO/GRN forms.
export function ProductPicker({
  value,
  onChange,
  drugs,
  items,
  className = '',
}: {
  value: ProductPickerValue
  onChange: (value: ProductPickerValue) => void
  drugs: ProductOption[]
  items: ProductOption[]
  className?: string
}) {
  const type = value.itemId ? 'item' : 'drug'

  return (
    <div className={`flex items-end gap-2 ${className}`}>
      <label className="block text-sm">
        <span className="mb-1 block font-medium text-slate-700">Type</span>
        <select
          className="rounded-md border border-slate-300 bg-panel-solid px-2 py-2 text-sm text-ink outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
          value={type}
          onChange={(e) => onChange(e.target.value === 'item' ? { drugId: '', itemId: value.itemId } : { drugId: value.drugId, itemId: '' })}
        >
          <option value="drug">Drug</option>
          <option value="item">Item</option>
        </select>
      </label>
      {type === 'drug' ? (
        <Select
          label="Drug"
          className="flex-1"
          placeholder="Select…"
          value={value.drugId}
          onChange={(e) => onChange({ drugId: e.target.value, itemId: '' })}
          options={drugs.map((d) => ({ value: d.id, label: d.name }))}
        />
      ) : (
        <Select
          label="Item"
          className="flex-1"
          placeholder="Select…"
          value={value.itemId}
          onChange={(e) => onChange({ drugId: '', itemId: e.target.value })}
          options={items.map((i) => ({ value: i.id, label: i.name }))}
        />
      )}
    </div>
  )
}
