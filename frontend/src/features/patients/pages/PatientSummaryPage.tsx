import { PatientPicker } from '../../appointments/components/PatientPicker'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { useAllergies, useProblemList, usePrescriptionHistory, useTimeline } from '../../emr/api'
import { TIMELINE_EVENT_LABELS } from '../../emr/types'
import { usePatient, usePatientPhotoUrl } from '../api'
import { GENDER_LABELS } from '../types'
import type { PatientListItem } from '../types'
import { useState } from 'react'

/** Standalone, printable one-page clinical/demographic summary for a single patient —
 * picks a patient, then composes the same EMR data (allergies, active medications,
 * problem list, recent visits) already used inline in the EMR workspace's left sidebar,
 * reusing those hooks rather than a new backend endpoint. */
export function PatientSummaryPage() {
  const [selected, setSelected] = useState<PatientListItem | null>(null)
  const patientId = selected?.id ?? null

  const { data: patient, isLoading: patientLoading } = usePatient(patientId)
  const { data: allergies = [] } = useAllergies(patientId ?? 0, true)
  const { data: problems = [] } = useProblemList(patientId ?? 0, 1)
  const { data: prescriptions = [] } = usePrescriptionHistory(patientId)
  const { data: timeline = [] } = useTimeline(patientId)
  const [photoUrl] = usePatientPhotoUrl(patientId, !!patient?.photoPath)

  const latestRx = prescriptions[0]
  const recentVisits = timeline.slice(0, 10)
  const fullName = patient ? [patient.firstName, patient.middleName, patient.lastName].filter(Boolean).join(' ') : ''

  return (
    <>
      <PageHeader title="Patient Summary" subtitle="Search a patient to view a printable clinical & demographic summary" />

      <div className="print:hidden mb-4 max-w-md rounded-lg border border-border-soft bg-panel-solid p-4">
        <PatientPicker selected={selected} onSelect={setSelected} />
      </div>

      {patientLoading && <Spinner />}

      {!patientId && !patientLoading && (
        <p className="py-10 text-center text-sm text-ink-faint">Search and select a patient above to view their summary.</p>
      )}

      {patient && (
        <div className="space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-3 rounded-lg bg-slate-800 px-4 py-3 text-white">
            <div className="flex flex-wrap items-center gap-x-5 gap-y-1">
              <span className="text-sm font-semibold">Patient Summary</span>
              <span className="text-xs text-slate-300">UHID <b className="font-semibold text-white">{patient.uhid}</b></span>
              <span className="text-xs text-slate-300">Name <b className="font-semibold text-white">{fullName}</b></span>
              <span className="text-xs text-slate-300">Age <b className="font-semibold text-white">{patient.ageDisplay}</b></span>
              <span className="text-xs text-slate-300">Sex <b className="font-semibold text-white">{GENDER_LABELS[patient.gender]}</b></span>
              <span className="text-xs text-slate-300">Phone <b className="font-semibold text-white">{patient.mobile}</b></span>
              <span className="text-xs text-slate-300">Blood Group <b className="font-semibold text-white">{patient.bloodGroup ?? '—'}</b></span>
            </div>
            <div className="flex items-center gap-2 print:hidden">
              {allergies.length > 0 && <span className="rounded-md bg-red-600 px-2.5 py-1 text-xs font-semibold">Allergy Alert</span>}
              <button
                type="button"
                onClick={() => window.print()}
                className="rounded-md bg-white/10 px-2.5 py-1.5 text-xs font-medium hover:bg-white/20"
              >
                Print
              </button>
            </div>
          </div>

          <div className="grid gap-4 lg:grid-cols-[280px_1fr]">
            <div className="space-y-3">
              <div className="rounded-lg border border-border-soft bg-panel-solid p-4">
                <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-ink-faint">Demographics</p>
                <div className="flex flex-col items-center text-center">
                  {photoUrl ? (
                    <img src={photoUrl} alt={fullName} className="h-20 w-20 rounded-full object-cover" />
                  ) : (
                    <div className="flex h-20 w-20 items-center justify-center rounded-full bg-panel-2 text-xs text-ink-faint">No photo</div>
                  )}
                  <p className="mt-2 text-sm font-semibold text-ink">{fullName}</p>
                  <p className="text-xs text-ink-muted">{patient.ageDisplay} · {GENDER_LABELS[patient.gender]}</p>
                  <p className="text-xs text-ink-muted">{patient.mobile}</p>
                  <p className="text-xs text-ink-muted">{patient.patientCategoryName ?? '—'}</p>
                </div>
                {allergies.length > 0 && (
                  <div className="mt-3 rounded-md border border-crit/30 bg-crit/10 px-2.5 py-1.5">
                    <p className="text-xs font-semibold text-crit">⚠ Allergy: {allergies.map((a) => a.allergenName).join(', ')}</p>
                  </div>
                )}
              </div>

              <div className="rounded-lg border border-border-soft bg-panel-solid p-4">
                <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-ink-faint">Active Medications</p>
                {latestRx ? (
                  <ul className="space-y-1 text-xs text-ink">
                    {latestRx.items.map((i) => (
                      <li key={i.drugId}>{i.drugName} — {i.dose} {i.frequency}</li>
                    ))}
                  </ul>
                ) : (
                  <p className="text-xs text-ink-faint">None on file.</p>
                )}
              </div>

              <div className="rounded-lg border border-border-soft bg-panel-solid p-4">
                <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-ink-faint">Chronic Conditions</p>
                {problems.length > 0 ? (
                  <ul className="space-y-1 text-xs text-ink">
                    {problems.map((p) => <li key={p.id}>{p.diagnosisText}</li>)}
                  </ul>
                ) : (
                  <p className="text-xs text-ink-faint">None active.</p>
                )}
              </div>
            </div>

            <div className="rounded-lg border border-border-soft bg-panel-solid p-4">
              <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-ink-faint">Recent Visits & Events</p>
              {recentVisits.length > 0 ? (
                <ul className="divide-y divide-border-soft text-sm">
                  {recentVisits.map((e, i) => (
                    <li key={i} className="flex items-start justify-between gap-3 py-2">
                      <div>
                        <p className="font-medium text-ink">{e.title}</p>
                        <p className="text-xs text-ink-muted">{e.summary}</p>
                      </div>
                      <div className="whitespace-nowrap text-right text-xs text-ink-faint">
                        <p>{new Date(e.at).toLocaleString()}</p>
                        <p>{TIMELINE_EVENT_LABELS[e.type]}</p>
                      </div>
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="text-xs text-ink-faint">No recorded visits or events.</p>
              )}
            </div>
          </div>
        </div>
      )}
    </>
  )
}
