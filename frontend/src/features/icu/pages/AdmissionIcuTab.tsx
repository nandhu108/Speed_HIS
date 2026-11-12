import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Select } from '../../../components/ui/Select'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useDrugs } from '../../masters/api'
import {
  useAddInfusionRate,
  useAddVentilatorObservation,
  useInfusions,
  useRecordIcuVitals,
  useRecordSeverity,
  useSeverityHistory,
  useStartInfusion,
  useStartVentilatorSession,
  useStopInfusion,
  useStopVentilatorSession,
  useVentilatorSessions,
} from '../api'
import { SEVERITY_LEVEL_LABELS, VENTILATOR_MODE_LABELS, type IcuSeverityLevel, type VentilatorMode } from '../types'

/// Embedded in IPD's AdmissionDetailPage as an "ICU" tab — kept as its own component/file so
/// that already-huge file only needs one import + one render line, not a few hundred more.
export function AdmissionIcuTab({ admissionId, setError }: { admissionId: number; setError: (e: string | null) => void }) {
  const run = async (fn: () => Promise<unknown>) => {
    setError(null)
    try {
      await fn()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="space-y-4">
      <VitalsCard admissionId={admissionId} run={run} />
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <VentilatorCard admissionId={admissionId} run={run} />
        <InfusionCard admissionId={admissionId} run={run} />
      </div>
      <SeverityCard admissionId={admissionId} run={run} />
    </div>
  )
}

function VitalsCard({ admissionId, run }: { admissionId: number; run: (fn: () => Promise<unknown>) => Promise<void> }) {
  const record = useRecordIcuVitals(admissionId)
  const [form, setForm] = useState({
    temperatureF: '', pulseRate: '', respiratoryRate: '', bloodPressureSystolic: '', bloodPressureDiastolic: '', spO2: '',
    glasgowComaScale: '', meanArterialPressure: '', centralVenousPressure: '', urineOutputMl: '',
  })

  const submit = () =>
    run(() =>
      record.mutateAsync({
        temperatureF: form.temperatureF ? Number(form.temperatureF) : null,
        pulseRate: form.pulseRate ? Number(form.pulseRate) : null,
        respiratoryRate: form.respiratoryRate ? Number(form.respiratoryRate) : null,
        bloodPressureSystolic: form.bloodPressureSystolic ? Number(form.bloodPressureSystolic) : null,
        bloodPressureDiastolic: form.bloodPressureDiastolic ? Number(form.bloodPressureDiastolic) : null,
        spO2: form.spO2 ? Number(form.spO2) : null,
        randomBloodSugar: null,
        glasgowComaScale: form.glasgowComaScale ? Number(form.glasgowComaScale) : null,
        meanArterialPressure: form.meanArterialPressure ? Number(form.meanArterialPressure) : null,
        centralVenousPressure: form.centralVenousPressure ? Number(form.centralVenousPressure) : null,
        urineOutputMl: form.urineOutputMl ? Number(form.urineOutputMl) : null,
      }),
    ).then(() =>
      setForm({
        temperatureF: '', pulseRate: '', respiratoryRate: '', bloodPressureSystolic: '', bloodPressureDiastolic: '', spO2: '',
        glasgowComaScale: '', meanArterialPressure: '', centralVenousPressure: '', urineOutputMl: '',
      }),
    )

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-2 text-sm font-semibold text-slate-700">Record ICU vitals</h3>
      <p className="mb-3 text-xs text-slate-500">Evaluated against every active critical alert rule the moment it's saved.</p>
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-5">
        <Input label="Temp (°F)" type="number" value={form.temperatureF} onChange={(e) => setForm({ ...form, temperatureF: e.target.value })} />
        <Input label="Pulse" type="number" value={form.pulseRate} onChange={(e) => setForm({ ...form, pulseRate: e.target.value })} />
        <Input label="Resp. rate" type="number" value={form.respiratoryRate} onChange={(e) => setForm({ ...form, respiratoryRate: e.target.value })} />
        <Input label="SpO2 (%)" type="number" value={form.spO2} onChange={(e) => setForm({ ...form, spO2: e.target.value })} />
        <Input label="GCS" type="number" value={form.glasgowComaScale} onChange={(e) => setForm({ ...form, glasgowComaScale: e.target.value })} />
        <Input label="BP systolic" type="number" value={form.bloodPressureSystolic} onChange={(e) => setForm({ ...form, bloodPressureSystolic: e.target.value })} />
        <Input label="BP diastolic" type="number" value={form.bloodPressureDiastolic} onChange={(e) => setForm({ ...form, bloodPressureDiastolic: e.target.value })} />
        <Input label="MAP" type="number" value={form.meanArterialPressure} onChange={(e) => setForm({ ...form, meanArterialPressure: e.target.value })} />
        <Input label="CVP" type="number" value={form.centralVenousPressure} onChange={(e) => setForm({ ...form, centralVenousPressure: e.target.value })} />
        <Input label="Urine output (ml)" type="number" value={form.urineOutputMl} onChange={(e) => setForm({ ...form, urineOutputMl: e.target.value })} />
      </div>
      <Button className="mt-3" loading={record.isPending} onClick={submit}>Record</Button>
    </div>
  )
}

function VentilatorCard({ admissionId, run }: { admissionId: number; run: (fn: () => Promise<unknown>) => Promise<void> }) {
  const { data: sessions = [] } = useVentilatorSessions(admissionId)
  const start = useStartVentilatorSession(admissionId)
  const stop = useStopVentilatorSession(admissionId)
  const addObservation = useAddVentilatorObservation(admissionId)
  const activeSession = sessions.find((s) => s.stoppedAt === null) ?? null

  const [mode, setMode] = useState<string>('1')
  const [fiO2, setFiO2] = useState('40')
  const [peep, setPeep] = useState('5')
  const [tidalVolume, setTidalVolume] = useState('')
  const [respRate, setRespRate] = useState('')

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="mb-2 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-700">Ventilator Chart</h3>
        {activeSession ? <StatusBadge tone="warning">Active</StatusBadge> : <StatusBadge tone="neutral">Off ventilator</StatusBadge>}
      </div>

      {!activeSession ? (
        <Button onClick={() => run(() => start.mutateAsync({ notes: null }))} loading={start.isPending}>
          Start ventilator session
        </Button>
      ) : (
        <>
          <div className="grid grid-cols-2 gap-2">
            <Select label="Mode" value={mode} onChange={(e) => setMode(e.target.value)} options={Object.entries(VENTILATOR_MODE_LABELS).map(([value, label]) => ({ value, label }))} />
            <Input label="FiO2 (%)" type="number" value={fiO2} onChange={(e) => setFiO2(e.target.value)} />
            <Input label="PEEP" type="number" value={peep} onChange={(e) => setPeep(e.target.value)} />
            <Input label="Tidal volume (ml)" type="number" value={tidalVolume} onChange={(e) => setTidalVolume(e.target.value)} />
            <Input label="RR set" type="number" value={respRate} onChange={(e) => setRespRate(e.target.value)} />
          </div>
          <div className="mt-2 flex gap-2">
            <Button
              variant="secondary"
              onClick={() =>
                run(() =>
                  addObservation.mutateAsync({
                    sessionId: activeSession.id, mode: Number(mode) as VentilatorMode, fiO2Percent: Number(fiO2), peep: Number(peep),
                    tidalVolumeMl: tidalVolume ? Number(tidalVolume) : null, respiratoryRateSet: respRate ? Number(respRate) : null,
                    peakPressure: null, plateauPressure: null, notes: null,
                  }),
                )
              }
              loading={addObservation.isPending}
            >
              Chart observation
            </Button>
            <Button variant="ghost" className="text-red-600" onClick={() => run(() => stop.mutateAsync(activeSession.id))} loading={stop.isPending}>
              Stop session
            </Button>
          </div>
          <table className="mt-3 w-full text-xs">
            <tbody>
              {activeSession.observations.slice(0, 5).map((o) => (
                <tr key={o.id} className="border-t border-slate-100">
                  <td className="py-1 text-slate-500">{new Date(o.recordedAt).toLocaleTimeString()}</td>
                  <td className="py-1">{VENTILATOR_MODE_LABELS[o.mode]}</td>
                  <td className="py-1">FiO2 {o.fiO2Percent}%</td>
                  <td className="py-1">PEEP {o.peep}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}
    </div>
  )
}

function InfusionCard({ admissionId, run }: { admissionId: number; run: (fn: () => Promise<unknown>) => Promise<void> }) {
  const { data: infusions = [] } = useInfusions(admissionId)
  const { data: drugsResult } = useDrugs({ page: 1, pageSize: 500 })
  const start = useStartInfusion(admissionId)
  const stop = useStopInfusion(admissionId)
  const addRate = useAddInfusionRate(admissionId)
  const running = infusions.filter((i) => i.stoppedAt === null)

  const [drugId, setDrugId] = useState('')
  const [nameOverride, setNameOverride] = useState('')
  const [rateFor, setRateFor] = useState<Record<number, { rate: string; unit: string }>>({})

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-2 text-sm font-semibold text-slate-700">Infusion Chart</h3>

      {running.map((i) => {
        const rateState = rateFor[i.id] ?? { rate: '', unit: 'ml/hr' }
        return (
          <div key={i.id} className="mb-2 rounded-md border border-slate-100 p-2">
            <div className="flex items-center justify-between">
              <span className="text-sm font-medium text-slate-700">{i.productName}</span>
              <Button variant="ghost" className="text-red-600" onClick={() => run(() => stop.mutateAsync(i.id))} loading={stop.isPending}>
                Stop
              </Button>
            </div>
            <div className="mt-1 flex items-end gap-2">
              <Input label="Rate" type="number" value={rateState.rate} onChange={(e) => setRateFor({ ...rateFor, [i.id]: { ...rateState, rate: e.target.value } })} />
              <Input label="Unit" value={rateState.unit} onChange={(e) => setRateFor({ ...rateFor, [i.id]: { ...rateState, unit: e.target.value } })} />
              <Button
                variant="secondary"
                onClick={() =>
                  run(() => addRate.mutateAsync({ entryId: i.id, rate: Number(rateState.rate), rateUnit: rateState.unit, notes: null })).then(() =>
                    setRateFor({ ...rateFor, [i.id]: { rate: '', unit: rateState.unit } }),
                  )
                }
                disabled={!rateState.rate}
                loading={addRate.isPending}
              >
                Titrate
              </Button>
            </div>
            {i.rateEntries[0] && <p className="mt-1 text-xs text-slate-500">Current: {i.rateEntries[0].rate} {i.rateEntries[0].rateUnit}</p>}
          </div>
        )
      })}

      <div className="mt-2 space-y-2 border-t border-slate-100 pt-2">
        <Select
          label="Drug (optional)"
          placeholder="Not in catalog"
          value={drugId}
          onChange={(e) => setDrugId(e.target.value)}
          options={(drugsResult?.items ?? []).map((d) => ({ value: d.id, label: d.name }))}
        />
        <Input label="Fluid name (if not a catalog drug)" value={nameOverride} onChange={(e) => setNameOverride(e.target.value)} />
        <Button
          onClick={() =>
            run(() => start.mutateAsync({ drugId: drugId ? Number(drugId) : null, drugNameOverride: nameOverride || null, notes: null })).then(() => {
              setDrugId('')
              setNameOverride('')
            })
          }
          loading={start.isPending}
          disabled={!drugId && !nameOverride}
        >
          Start infusion
        </Button>
      </div>
    </div>
  )
}

function SeverityCard({ admissionId, run }: { admissionId: number; run: (fn: () => Promise<unknown>) => Promise<void> }) {
  const { data: history = [] } = useSeverityHistory(admissionId)
  const record = useRecordSeverity(admissionId)
  const [level, setLevel] = useState<string>('2')
  const [notes, setNotes] = useState('')

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-2 text-sm font-semibold text-slate-700">Severity Assessment</h3>
      <div className="flex flex-wrap items-end gap-2">
        <Select label="Level" value={level} onChange={(e) => setLevel(e.target.value)} options={Object.entries(SEVERITY_LEVEL_LABELS).map(([value, label]) => ({ value, label }))} />
        <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
        <Button
          onClick={() => run(() => record.mutateAsync({ severityLevel: Number(level) as IcuSeverityLevel, notes: notes || null })).then(() => setNotes(''))}
          loading={record.isPending}
        >
          Record
        </Button>
      </div>
      <table className="mt-3 w-full text-xs">
        <tbody>
          {history.slice(0, 5).map((h) => (
            <tr key={h.id} className="border-t border-slate-100">
              <td className="py-1 text-slate-500">{new Date(h.assessedAt).toLocaleString()}</td>
              <td className="py-1 font-medium">{SEVERITY_LEVEL_LABELS[h.severityLevel]}</td>
              <td className="py-1 text-slate-500">{h.notes ?? ''}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
