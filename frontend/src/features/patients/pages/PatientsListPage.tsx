import { Phone, Search } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { usePatients } from '../api'
import { GENDER_LABELS } from '../types'
import type { PatientListItem } from '../types'

/* Flat corporate palette (2026-08-11): solid fills off the navy/slate ramp, replacing the old
   gradient pairs. Two of those pairs had gone stale under the new tokens — `violet` now resolves
   to slate, and `from-warn to-crit` put an amber→rose gradient on a patient avatar in an
   otherwise navy shell, reading as a status warning about the patient rather than as decoration. */
const AVATAR_TONES = ['bg-accent', 'bg-accent-2', 'bg-indigo', 'bg-violet']

function avatarTone(id: number) {
  return AVATAR_TONES[id % AVATAR_TONES.length]
}

function initialsOf(name: string) {
  return name
    .split(' ')
    .map((p) => p[0])
    .slice(0, 2)
    .join('')
    .toUpperCase()
}

export function PatientsListPage() {
  const { hasPermission } = useAuth()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [searchDraft, setSearchDraft] = useState('')
  const [search, setSearch] = useState('')
  const { data, isLoading } = usePatients({ page, pageSize: 20, search: search || undefined })

  return (
    <>
      <PageHeader
        title="Patients"
        subtitle="Search by UHID, name, mobile or date of birth (YYYY-MM-DD)"
        actions={
          hasPermission('PATIENT.PATIENT.CREATE') && (
            <Button onClick={() => navigate('/patients/new')}>New registration</Button>
          )
        }
      />

      <form
        className="mb-5 max-w-md"
        onSubmit={(e) => {
          e.preventDefault()
          setPage(1)
          setSearch(searchDraft)
        }}
      >
        <div className="relative">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-ink-faint" aria-hidden />
          <Input
            value={searchDraft}
            onChange={(e) => setSearchDraft(e.target.value)}
            placeholder="Search patients by name, UHID, mobile…"
            inputClassName="pl-10"
          />
        </div>
      </form>

      {isLoading && <Spinner />}

      {!isLoading && data?.items.length === 0 && (
        <p className="rounded-shell border border-border-soft bg-panel py-10 text-center text-ink-faint">
          No patients found.
        </p>
      )}

      <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        {data?.items.map((patient: PatientListItem) => (
          <button
            key={patient.id}
            type="button"
            onClick={() => navigate(`/patients/${patient.id}`)}
            className="rounded-shell border border-border-soft bg-panel p-4 text-left shadow-shell-sm transition-all hover:-translate-y-0.5 hover:border-border-strong hover:shadow-shell-md"
          >
            <div className="flex items-center gap-3">
              <span
                className={`flex size-11 shrink-0 items-center justify-center rounded-shell-sm text-[15px] font-bold text-white ${avatarTone(patient.id)}`}
              >
                {initialsOf(patient.fullName)}
              </span>
              <div className="min-w-0 flex-1">
                <div className="truncate text-sm font-bold text-ink">{patient.fullName}</div>
                <div className="font-mono text-[11px] text-ink-faint">
                  {patient.uhid} · {GENDER_LABELS[patient.gender]} · {patient.ageDisplay}
                </div>
              </div>
              {patient.isBlocked ? (
                <StatusBadge tone="danger">Blocked</StatusBadge>
              ) : (
                <StatusBadge tone={patient.isActive ? 'success' : 'neutral'}>
                  {patient.isActive ? 'Active' : 'Inactive'}
                </StatusBadge>
              )}
            </div>
            <div className="mt-3.5 flex items-center gap-1.5 border-t border-border-soft pt-3 text-[12.5px] text-ink-muted">
              <Phone className="size-3.5 text-ink-faint" aria-hidden />
              {patient.mobile}
            </div>
            <div className="mt-1 flex items-center gap-1.5 text-[11.5px] text-ink-faint">
              {patient.patientCategoryColorHex && (
                <span
                  className="inline-block size-2 shrink-0 rounded-full"
                  style={{ backgroundColor: patient.patientCategoryColorHex }}
                  aria-hidden
                />
              )}
              {patient.patientCategoryName ?? 'General'} · {patient.registeredBranchName}
            </div>
          </button>
        ))}
      </div>

      {data && data.totalPages > 1 && (
        <div className="mt-5 flex items-center justify-between text-sm text-ink-muted">
          <span>
            Page {data.page} of {data.totalPages} · {data.totalCount} patients
          </span>
          <div className="flex gap-1">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
              className="rounded-shell-sm border border-border-soft px-3 py-1 hover:border-border-strong disabled:opacity-40"
            >
              Previous
            </button>
            <button
              type="button"
              disabled={page >= data.totalPages}
              onClick={() => setPage((p) => p + 1)}
              className="rounded-shell-sm border border-border-soft px-3 py-1 hover:border-border-strong disabled:opacity-40"
            >
              Next
            </button>
          </div>
        </div>
      )}
    </>
  )
}
