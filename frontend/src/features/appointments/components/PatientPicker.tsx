import { useEffect, useState } from 'react'
import { Input } from '../../../components/ui/Input'
import { usePatients } from '../../patients/api'
import type { PatientListItem } from '../../patients/types'

interface PatientPickerProps {
  selected: PatientListItem | null
  onSelect: (patient: PatientListItem) => void
}

/**
 * Debounced UHID/name/mobile search — a first, scoped instance of the shared SearchSelect
 * pattern. Reused across Appointments/OPD/Billing/IPD/Lab/Rad/Pharmacy/Emergency/Loyalty/
 * Insurance/MHC/Camp/Nursing, so a `Blocked` badge here (added 2026-09-16, closing
 * docs/PATIENT_JOURNEY_GAP_ANALYSIS.md §2.2's picker-visibility half) reaches every one of
 * those flows in one place. Advisory only, same as everywhere else `IsBlocked` is
 * surfaced — front-desk staff still decide whether to proceed; nothing here stops selecting
 * a blocked patient.
 */
export function PatientPicker({ selected, onSelect }: PatientPickerProps) {
  const [term, setTerm] = useState('')
  const [debounced, setDebounced] = useState('')

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(term), 300)
    return () => clearTimeout(timer)
  }, [term])

  const { data } = usePatients({ page: 1, pageSize: 10, search: debounced || undefined })
  const showResults = debounced.length > 0 && !selected

  return (
    <div className="relative">
      <Input
        label="Patient"
        placeholder="Search by UHID, name or mobile…"
        value={selected ? `${selected.fullName} (${selected.uhid})` : term}
        onChange={(e) => {
          setTerm(e.target.value)
          if (selected) onSelect(null as unknown as PatientListItem)
        }}
      />
      {selected?.isBlocked && (
        <p className="mt-1 text-xs font-medium text-red-600">
          This patient is blocked — check the reason on their profile before proceeding.
        </p>
      )}
      {showResults && (
        <div className="absolute z-10 mt-1 w-full rounded-md border border-slate-200 bg-white shadow-lg">
          {(data?.items ?? []).map((patient) => (
            <button
              key={patient.id}
              type="button"
              className="block w-full px-3 py-2 text-left text-sm hover:bg-slate-50"
              onClick={() => {
                onSelect(patient)
                setTerm('')
              }}
            >
              <span className="font-medium">{patient.fullName}</span>{' '}
              <span className="font-mono text-xs text-slate-400">{patient.uhid}</span>
              <span className="ml-2 text-xs text-slate-400">{patient.mobile}</span>
              {patient.isBlocked && <span className="ml-2 rounded bg-red-100 px-1.5 py-0.5 text-xs font-medium text-red-700">Blocked</span>}
            </button>
          ))}
          {(data?.items.length ?? 0) === 0 && (
            <p className="px-3 py-2 text-sm text-slate-400">No matching patients.</p>
          )}
        </div>
      )}
    </div>
  )
}
