import { useState } from 'react'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { useServices } from '../api'
import type { Service } from '../types'

interface ServicePickerProps {
  /** Services already added, to hide them from the results. */
  excludeIds: number[]
  onPick: (service: Service) => void
}

/** Server-side searched service list used by the tariff and package item editors. */
export function ServicePicker({ excludeIds, onPick }: ServicePickerProps) {
  const [draft, setDraft] = useState('')
  const [search, setSearch] = useState('')
  const { data, isLoading } = useServices({ page: 1, pageSize: 50, search: search || undefined })

  const candidates = (data?.items ?? []).filter((s) => !excludeIds.includes(s.id))

  return (
    <div className="rounded-md border border-slate-200">
      <form
        className="border-b border-slate-200 p-2"
        onSubmit={(event) => {
          event.preventDefault()
          setSearch(draft)
        }}
      >
        <Input placeholder="Search services…" value={draft} onChange={(e) => setDraft(e.target.value)} />
      </form>
      <div className="max-h-48 overflow-y-auto">
        {isLoading && <p className="px-3 py-2 text-sm text-slate-400">Loading…</p>}
        {!isLoading && candidates.length === 0 && (
          <p className="px-3 py-2 text-sm text-slate-400">No matching services.</p>
        )}
        {candidates.map((service) => (
          <button
            key={service.id}
            type="button"
            onClick={() => onPick(service)}
            className="flex w-full items-center justify-between px-3 py-1.5 text-left text-sm hover:bg-slate-50"
          >
            <span>
              <span className="font-mono text-xs text-slate-500">{service.code}</span>{' '}
              <span className="text-slate-700">{service.name}</span>
            </span>
            <Button variant="ghost" className="pointer-events-none px-2 py-1 text-xs">
              Add
            </Button>
          </button>
        ))}
      </div>
    </div>
  )
}
