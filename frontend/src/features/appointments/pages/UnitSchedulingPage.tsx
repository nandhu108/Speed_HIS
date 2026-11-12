import { useEffect, useState } from 'react'
import { endpoints } from '../../../api/endpoints'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { useAuth } from '../../auth/AuthContext'
import { useDoctors, useSimpleMaster } from '../../masters/api'
import { useReplaceUnitDoctors, useReplaceUnitScheduleBlocks, useUnitDoctors, useUnitSchedule } from '../api'
import { DAYS_OF_WEEK, type SaveUnitScheduleBlock } from '../types'

/** "Link Unit & Doctors" + "Schedule Unit - Day Wise" — combined since both are Unit-scoped
 * configuration reached the same way (pick a Unit, then configure it). */
export function UnitSchedulingPage() {
  const { hasPermission } = useAuth()
  const canManageDoctors = hasPermission('MASTER.UNIT.MANAGE')
  const canManageSchedule = hasPermission('APPOINTMENT.UNITSCHEDULE.MANAGE')

  const { data: units = [] } = useSimpleMaster('units', endpoints.masters.units)
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 500 })
  const [unitId, setUnitId] = useState<number | null>(null)

  const { data: linkedDoctors, isLoading: doctorsLoading } = useUnitDoctors(unitId)
  const replaceDoctors = useReplaceUnitDoctors(unitId ?? 0)
  const { data: schedule, isLoading: scheduleLoading } = useUnitSchedule(unitId)
  const replaceBlocks = useReplaceUnitScheduleBlocks(unitId ?? 0)

  const [selectedDoctorIds, setSelectedDoctorIds] = useState<number[]>([])
  const [blocks, setBlocks] = useState<SaveUnitScheduleBlock[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setSelectedDoctorIds((linkedDoctors ?? []).map((d) => d.doctorId))
  }, [linkedDoctors])

  useEffect(() => {
    if (schedule) {
      setBlocks(schedule.blocks.map((b) => ({ ...b })))
    }
  }, [schedule])

  const toggleDoctor = (doctorId: number) =>
    setSelectedDoctorIds((current) =>
      current.includes(doctorId) ? current.filter((id) => id !== doctorId) : [...current, doctorId],
    )

  const saveDoctors = async () => {
    setError(null)
    try {
      await replaceDoctors.mutateAsync(selectedDoctorIds)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const addBlock = () => setBlocks((current) => [...current, { dayOfWeek: 1, startTime: '09:00:00', endTime: '17:00:00' }])
  const updateBlock = (index: number, patch: Partial<SaveUnitScheduleBlock>) =>
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

  return (
    <>
      <PageHeader title="Unit Scheduling" subtitle="Link doctors to a Unit and set its weekly operating hours" />

      <div className="mb-4 max-w-xs">
        <Select
          label="Unit"
          placeholder="Select a unit…"
          options={units.map((u) => ({ value: u.id, label: u.name }))}
          value={unitId ?? ''}
          onChange={(e) => setUnitId(e.target.value ? Number(e.target.value) : null)}
        />
      </div>

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      {unitId && (
        <>
          <div className="mb-6 rounded-lg border border-slate-200 bg-white p-4">
            <div className="mb-3 flex items-center justify-between">
              <h2 className="text-sm font-semibold text-slate-700">Linked Doctors</h2>
              {canManageDoctors && (
                <Button onClick={saveDoctors} loading={replaceDoctors.isPending}>
                  Save doctors
                </Button>
              )}
            </div>
            {doctorsLoading && <Spinner />}
            <div className="grid max-h-60 grid-cols-2 gap-1 overflow-y-auto rounded-md border border-slate-200 p-2 sm:grid-cols-3">
              {(doctorsResult?.items ?? []).map((d) => (
                <label key={d.id} className="flex items-center gap-2 text-sm text-slate-700">
                  <input
                    type="checkbox"
                    checked={selectedDoctorIds.includes(d.id)}
                    onChange={() => toggleDoctor(d.id)}
                  />
                  {d.fullName}
                </label>
              ))}
              {(doctorsResult?.items ?? []).length === 0 && (
                <span className="text-sm text-slate-400">No doctors defined.</span>
              )}
            </div>
          </div>

          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <div className="mb-3 flex items-center justify-between">
              <h2 className="text-sm font-semibold text-slate-700">Weekly Schedule</h2>
              {canManageSchedule && (
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
            {scheduleLoading && <Spinner />}
            <div className="space-y-2">
              {blocks.map((block, index) => (
                <div key={index} className="grid grid-cols-4 items-end gap-2 rounded-md border border-slate-200 p-2">
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
                  {canManageSchedule && (
                    <Button variant="ghost" className="text-red-600" onClick={() => removeBlock(index)}>
                      Remove
                    </Button>
                  )}
                </div>
              ))}
              {blocks.length === 0 && <p className="text-sm text-slate-400">No weekly hours configured.</p>}
            </div>
          </div>
        </>
      )}
    </>
  )
}
