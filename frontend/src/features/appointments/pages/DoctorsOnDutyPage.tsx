import { useState } from 'react'
import { endpoints } from '../../../api/endpoints'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useSimpleMaster } from '../../masters/api'
import { useDoctorsOnDuty } from '../api'

function today() {
  return new Date().toISOString().slice(0, 10)
}

/** "Doctors On Duty" — a read-only projection of each doctor's computed availability for a
 * date, sourced entirely from the existing Doctor Schedule / leave-override data; front-desk
 * uses this to see roster coverage at a glance, not to manage the schedule itself. */
export function DoctorsOnDutyPage() {
  const [date, setDate] = useState(today())
  const [departmentId, setDepartmentId] = useState('')
  const { data: departments = [] } = useSimpleMaster('departments', endpoints.masters.departments)
  const { data: doctors = [], isLoading } = useDoctorsOnDuty(date, departmentId ? Number(departmentId) : null)

  const onDuty = doctors.filter((d) => d.isOnDuty)
  const offDuty = doctors.filter((d) => !d.isOnDuty)

  return (
    <>
      <PageHeader
        title="Doctors On Duty"
        subtitle="Who has availability today, computed from Doctor Schedules and leave overrides"
        actions={
          <div className="flex gap-2">
            <input
              type="date"
              value={date}
              onChange={(e) => setDate(e.target.value)}
              className="rounded-shell-sm border border-border-soft bg-panel-2 px-3 py-2 text-sm text-ink outline-none"
            />
            <Select
              placeholder="All departments"
              options={departments.filter((d) => d.isActive).map((d) => ({ value: d.id, label: d.name }))}
              value={departmentId}
              onChange={(e) => setDepartmentId(e.target.value)}
            />
          </div>
        }
      />

      {isLoading && <Spinner />}

      <div className="grid gap-3 md:grid-cols-2 lg:grid-cols-3">
        {onDuty.map((d) => (
          <div key={d.doctorId} className="rounded-lg border border-slate-200 bg-white p-4">
            <div className="mb-1 flex items-center justify-between">
              <p className="text-sm font-semibold text-slate-800">{d.doctorName}</p>
              <StatusBadge tone="success">On duty</StatusBadge>
            </div>
            <p className="text-xs text-slate-400">
              {d.specialtyName ?? '—'}{d.departmentNames.length > 0 ? ` · ${d.departmentNames.join(', ')}` : ''}
            </p>
            <div className="mt-2 flex flex-wrap gap-1">
              {d.windows.map((w, i) => (
                <span key={i} className="rounded-md bg-slate-50 px-2 py-1 text-xs text-slate-600">
                  {w.startTime.slice(0, 5)}–{w.endTime.slice(0, 5)}
                </span>
              ))}
            </div>
          </div>
        ))}
      </div>

      {offDuty.length > 0 && (
        <div className="mt-8">
          <p className="mb-3 text-sm font-semibold text-slate-700">Not on duty today</p>
          <div className="space-y-2">
            {offDuty.map((d) => (
              <div key={d.doctorId} className="flex items-center justify-between rounded-md border border-slate-200 bg-white px-3 py-2 text-sm">
                <div>
                  <span className="font-medium text-slate-700">{d.doctorName}</span>{' '}
                  <span className="text-xs text-slate-400">
                    {d.specialtyName ?? '—'}{d.departmentNames.length > 0 ? ` · ${d.departmentNames.join(', ')}` : ''}
                  </span>
                </div>
                <span className="text-xs text-slate-400">{d.leaveReason ?? 'No schedule for this day'}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {!isLoading && doctors.length === 0 && <p className="py-10 text-center text-slate-400">No doctors found.</p>}
    </>
  )
}
