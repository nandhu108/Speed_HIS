import { useEffect, useState } from 'react'
import { Input } from '../../../components/ui/Input'
import { useIcdCodes } from '../../masters/api'
import type { IcdCode } from '../../masters/types'

interface IcdPickerProps {
  onSelect: (icd: IcdCode) => void
}

/** Debounced ICD-10 search over the code/description catalog. */
export function IcdPicker({ onSelect }: IcdPickerProps) {
  const [term, setTerm] = useState('')
  const [debounced, setDebounced] = useState('')

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(term), 300)
    return () => clearTimeout(timer)
  }, [term])

  const { data } = useIcdCodes({ page: 1, pageSize: 10, search: debounced || undefined })
  const showResults = debounced.length > 0

  return (
    <div className="relative">
      <Input placeholder="Search ICD-10 code or description…" value={term} onChange={(e) => setTerm(e.target.value)} />
      {showResults && (
        <div className="absolute z-10 mt-1 w-full rounded-md border border-slate-200 bg-white shadow-lg">
          {(data?.items ?? []).map((icd) => (
            <button
              key={icd.id}
              type="button"
              className="block w-full px-3 py-2 text-left text-sm hover:bg-slate-50"
              onClick={() => {
                onSelect(icd)
                setTerm('')
              }}
            >
              <span className="font-mono text-xs text-primary-700">{icd.code}</span>{' '}
              <span>{icd.description}</span>
            </button>
          ))}
          {(data?.items.length ?? 0) === 0 && <p className="px-3 py-2 text-sm text-slate-400">No matching codes.</p>}
        </div>
      )}
    </div>
  )
}
