import { useEffect, useState } from 'react'
import { Input } from '../../../components/ui/Input'
import { useDrugs } from '../../masters/api'
import type { Drug } from '../../masters/types'

interface DrugPickerProps {
  onSelect: (drug: Drug) => void
  placeholder?: string
}

/** Debounced drug search — clears itself after a pick so it can be used repeatedly to add rows. */
export function DrugPicker({ onSelect, placeholder = 'Search drug…' }: DrugPickerProps) {
  const [term, setTerm] = useState('')
  const [debounced, setDebounced] = useState('')

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(term), 300)
    return () => clearTimeout(timer)
  }, [term])

  const { data } = useDrugs({ page: 1, pageSize: 10, search: debounced || undefined })
  const showResults = debounced.length > 0

  return (
    <div className="relative">
      <Input placeholder={placeholder} value={term} onChange={(e) => setTerm(e.target.value)} />
      {showResults && (
        <div className="absolute z-10 mt-1 w-full rounded-md border border-slate-200 bg-white shadow-lg">
          {(data?.items ?? []).map((drug) => (
            <button
              key={drug.id}
              type="button"
              className="block w-full px-3 py-2 text-left text-sm hover:bg-slate-50"
              onClick={() => {
                onSelect(drug)
                setTerm('')
              }}
            >
              <span className="font-medium">{drug.name}</span>
              {drug.strength && <span className="ml-1 text-xs text-slate-400">{drug.strength}</span>}
            </button>
          ))}
          {(data?.items.length ?? 0) === 0 && <p className="px-3 py-2 text-sm text-slate-400">No matching drugs.</p>}
        </div>
      )}
    </div>
  )
}
