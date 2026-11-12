import { PageHeader } from '../../components/ui/PageHeader'
import { Spinner } from '../../components/ui/Spinner'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useMedicalHistory } from '../features/records/api'

export function PortalMedicalHistoryPage() {
  const { data, isLoading } = useMedicalHistory()
  if (isLoading || !data) return <Spinner />

  return (
    <>
      <PageHeader title="Medical History" subtitle="Conditions, surgeries, allergies and immunizations on file" />
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Section title="Medical Conditions">
          {data.medical.map((m) => (
            <Row key={m.id} label={m.condition} sub={m.diagnosedDate ?? undefined} notes={m.notes} />
          ))}
          {data.medical.length === 0 && <Empty />}
        </Section>
        <Section title="Surgical History">
          {data.surgical.map((s) => (
            <Row key={s.id} label={s.procedureName} sub={s.procedureDate ?? undefined} notes={s.notes} />
          ))}
          {data.surgical.length === 0 && <Empty />}
        </Section>
        <Section title="Allergies">
          {data.allergies.map((a) => (
            <div key={a.id} className="flex items-center justify-between border-t border-slate-100 py-2 text-sm first:border-0">
              <span>{a.allergenName}{a.reaction ? ` — ${a.reaction}` : ''}</span>
              <StatusBadge tone="danger">Severity {a.severity}</StatusBadge>
            </div>
          ))}
          {data.allergies.length === 0 && <Empty />}
        </Section>
        <Section title="Immunizations">
          {data.immunizations.map((i) => (
            <Row key={i.id} label={i.vaccineName} sub={i.administeredDate ?? undefined} />
          ))}
          {data.immunizations.length === 0 && <Empty />}
        </Section>
      </div>
    </>
  )
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-2 text-sm font-semibold text-slate-700">{title}</h3>
      {children}
    </div>
  )
}

function Row({ label, sub, notes }: { label: string; sub?: string; notes?: string | null }) {
  return (
    <div className="border-t border-slate-100 py-2 text-sm first:border-0">
      <div className="flex justify-between">
        <span className="text-slate-700">{label}</span>
        {sub && <span className="text-slate-400">{sub}</span>}
      </div>
      {notes && <p className="mt-0.5 text-xs text-slate-500">{notes}</p>}
    </div>
  )
}

function Empty() {
  return <p className="py-3 text-center text-sm text-slate-400">Nothing on file.</p>
}
