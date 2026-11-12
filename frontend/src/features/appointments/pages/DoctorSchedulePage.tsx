import { useEffect, useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { useRun } from '../../../lib/useRun'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useDoctors } from '../../masters/api'
import {
  useAddScheduleOverride,
  useDeleteScheduleOverride,
  useDoctorSchedule,
  useReplaceScheduleBlocks,
} from '../api'
import { DAYS_OF_WEEK, type SaveDoctorScheduleBlock } from '../types'

export function DoctorSchedulePage() {
  const run = useRun()
  const { hasPermission } = useAuth()
  const canManage = hasPermission('APPOINTMENT.SCHEDULE.MANAGE')
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })
  const [doctorId, setDoctorId] = useState<number | null>(null)

  const { data: schedule, isLoading } = useDoctorSchedule(doctorId)
  const replaceBlocks = useReplaceScheduleBlocks(doctorId ?? 0)
  const addOverride = useAddScheduleOverride(doctorId ?? 0)
  const deleteOverride = useDeleteScheduleOverride(doctorId ?? 0)

  const [blocks, setBlocks] = useState<SaveDoctorScheduleBlock[]>([])
  const [error, setError] = useState<string | null>(null)
  const [overrideForm, setOverrideForm] = useState({
    date: '',
    isAvailable: false,
    startTime: '',
    endTime: '',
    slotMinutes: '',
    reason: '',
  })

  useEffect(() => {
    if (schedule) {
      setBlocks(schedule.blocks.map((b) => ({ ...b })))
    }
  }, [schedule])

  const addBlock = () =>
    setBlocks((current) => [...current, { dayOfWeek: 1, startTime: '09:00:00', endTime: '13:00:00', slotMinutes: 15 }])
  const updateBlock = (index: number, patch: Partial<SaveDoctorScheduleBlock>) =>
    setBlocks((current) => current.map((b, i) => (i === index ? { ...b, ...patch } : b)))
  const removeBlock = (index: number) => setBlocks((current) => current.filter((_, i) => i !== index))

  const saveBlocks = async () => {
    setError(null)
    try {
      await replaceBlocks.mutateAsync(blocks)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const submitOverride = async () => {
    setError(null)
    try {
      await addOverride.mutateAsync({
        date: overrideForm.date,
        isAvailable: overrideForm.isAvailable,
        startTime: overrideForm.isAvailable ? `${overrideForm.startTime}:00` : null,
        endTime: overrideForm.isAvailable ? `${overrideForm.endTime}:00` : null,
        slotMinutes: overrideForm.isAvailable && overrideForm.slotMinutes ? Number(overrideForm.slotMinutes) : null,
        reason: overrideForm.reason || null,
      })
      setOverrideForm({ date: '', isAvailable: false, startTime: '', endTime: '', slotMinutes: '', reason: '' })
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader title="Doctor Schedules" subtitle="Weekly availability template plus date-specific leave/overrides" />

      <div className="mb-4 max-w-xs">
        <Select
          label="Doctor"
          placeholder="Select a doctor…"
          options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
          value={doctorId ?? ''}
          onChange={(e) => setDoctorId(e.target.value ? Number(e.target.value) : null)}
        />
      </div>

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      {doctorId && (
        <>
          {isLoading && <Spinner />}

          <div className="mb-6 rounded-lg border border-slate-200 bg-white p-4">
            <div className="mb-3 flex items-center justify-between">
              <h2 className="text-sm font-semibold text-slate-700">Weekly template</h2>
              {canManage && (
                <div className="flex gap-2">
                  <Button variant="secondary" onClick={addBlock}>
                    Add block
                  </Button>
                  <Button onClick={saveBlocks} loading={replaceBlocks.isPending}>
                    Save weekly schedule
                  </Button>
                </div>
              )}
            </div>
            <div className="space-y-2">
              {blocks.map((block, index) => (
                <div key={index} className="grid grid-cols-5 items-end gap-2 rounded-md border border-slate-200 p-2">
                  <Select
                    label="Day"
                    options={DAYS_OF_WEEK.map((d) => ({ value: d.value, label: d.label }))}
                    value={block.dayOfWeek}
                    onChange={(e) => updateBlock(index, { dayOfWeek: Number(e.target.value) })}
                  />
                  <Input
                    label="Start"
                    type="time"
                    value={block.startTime.slice(0, 5)}
                    onChange={(e) => updateBlock(index, { startTime: `${e.target.value}:00` })}
                  />
                  <Input
                    label="End"
                    type="time"
                    value={block.endTime.slice(0, 5)}
                    onChange={(e) => updateBlock(index, { endTime: `${e.target.value}:00` })}
                  />
                  <Input
                    label="Slot (min)"
                    type="number"
                    min={1}
                    value={block.slotMinutes}
                    onChange={(e) => updateBlock(index, { slotMinutes: Number(e.target.value) })}
                  />
                  {canManage && (
                    <Button variant="ghost" className="text-red-600" onClick={() => removeBlock(index)}>
                      Remove
                    </Button>
                  )}
                </div>
              ))}
              {blocks.length === 0 && <p className="text-sm text-slate-400">No weekly availability configured.</p>}
            </div>
          </div>

          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <h2 className="mb-3 text-sm font-semibold text-slate-700">Leave &amp; date overrides</h2>

            {canManage && (
              <div className="mb-4 flex flex-wrap items-end gap-2 border-b border-slate-100 pb-4">
                <Input
                  label="Date"
                  type="date"
                  value={overrideForm.date}
                  onChange={(e) => setOverrideForm({ ...overrideForm, date: e.target.value })}
                />
                <label className="flex items-center gap-1.5 pb-2 text-sm text-slate-600">
                  <input
                    type="checkbox"
                    checked={overrideForm.isAvailable}
                    onChange={(e) => setOverrideForm({ ...overrideForm, isAvailable: e.target.checked })}
                  />
                  Special hours (unchecked = full-day leave)
                </label>
                {overrideForm.isAvailable && (
                  <>
                    <Input
                      label="Start"
                      type="time"
                      value={overrideForm.startTime}
                      onChange={(e) => setOverrideForm({ ...overrideForm, startTime: e.target.value })}
                    />
                    <Input
                      label="End"
                      type="time"
                      value={overrideForm.endTime}
                      onChange={(e) => setOverrideForm({ ...overrideForm, endTime: e.target.value })}
                    />
                    <Input
                      label="Slot (min)"
                      type="number"
                      value={overrideForm.slotMinutes}
                      onChange={(e) => setOverrideForm({ ...overrideForm, slotMinutes: e.target.value })}
                    />
                  </>
                )}
                <Input
                  label="Reason"
                  value={overrideForm.reason}
                  onChange={(e) => setOverrideForm({ ...overrideForm, reason: e.target.value })}
                />
                <Button onClick={submitOverride} loading={addOverride.isPending} disabled={!overrideForm.date}>
                  Add
                </Button>
              </div>
            )}

            <div className="space-y-2">
              {(schedule?.overrides ?? []).map((o) => (
                <div key={o.id} className="flex items-center justify-between rounded-md border border-slate-200 px-3 py-2">
                  <div>
                    <span className="text-sm font-medium text-slate-700">{o.date}</span>{' '}
                    <StatusBadge tone={o.isAvailable ? 'info' : 'warning'}>
                      {o.isAvailable ? `${o.startTime?.slice(0, 5)}–${o.endTime?.slice(0, 5)}` : 'Unavailable'}
                    </StatusBadge>
                    {o.reason && <span className="ml-2 text-xs text-slate-400">{o.reason}</span>}
                  </div>
                  {canManage && (
                    <Button
                      variant="ghost"
                      className="text-red-600"
                      onClick={() => run(() => deleteOverride.mutateAsync(o.id))}
                    >
                      Remove
                    </Button>
                  )}
                </div>
              ))}
              {(schedule?.overrides.length ?? 0) === 0 && (
                <p className="text-sm text-slate-400">No upcoming leave or overrides.</p>
              )}
            </div>
          </div>
        </>
      )}
    </>
  )
}
