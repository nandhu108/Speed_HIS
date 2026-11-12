import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { useAuth } from '../../auth/AuthContext'
import { useAutoComputeIndicatorEntry, useIndicatorEntries, useIndicators, useRecordIndicatorEntry, useSaveIndicator } from '../api'
import { AUTO_COMPUTE_SOURCE_LABELS, type Indicator, type QualityAutoComputeSource } from '../types'

export function IndicatorsPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('QUALITY.INDICATOR.MANAGE')
  const canRecord = hasPermission('QUALITY.INDICATORENTRY.RECORD')
  const { data: indicators = [], isLoading } = useIndicators()
  const [creating, setCreating] = useState(false)
  const [selected, setSelected] = useState<Indicator | null>(null)

  return (
    <>
      <PageHeader
        title="Quality Indicators"
        subtitle="Numerator/denominator definitions, monthly data entry and auto-pull"
        actions={canManage && <Button onClick={() => setCreating(true)}>New indicator</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Category</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Unit</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Target</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Auto-compute</th>
            </tr>
          </thead>
          <tbody>
            {indicators.map((i) => (
              <tr key={i.id} className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50" onClick={() => setSelected(i)}>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{i.code}</td>
                <td className="px-4 py-2.5 text-slate-700">{i.name}</td>
                <td className="px-4 py-2.5 text-slate-700">{i.category ?? '—'}</td>
                <td className="px-4 py-2.5 text-slate-700">{i.unit ?? '—'}</td>
                <td className="px-4 py-2.5 text-slate-700">{i.targetValue ?? '—'}</td>
                <td className="px-4 py-2.5 text-slate-700">{i.autoComputeSource ? AUTO_COMPUTE_SOURCE_LABELS[i.autoComputeSource] : 'Manual'}</td>
              </tr>
            ))}
            {!isLoading && indicators.length === 0 && (
              <tr><td colSpan={6} className="px-4 py-10 text-center text-slate-400">No indicators defined yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <IndicatorEditorModal onClose={() => setCreating(false)} />}
      {selected && <IndicatorEntriesModal indicator={selected} canRecord={canRecord} onClose={() => setSelected(null)} />}
    </>
  )
}

function IndicatorEditorModal({ onClose }: { onClose: () => void }) {
  const save = useSaveIndicator()
  const [code, setCode] = useState('')
  const [name, setName] = useState('')
  const [category, setCategory] = useState('')
  const [numeratorDefinition, setNumeratorDefinition] = useState('')
  const [denominatorDefinition, setDenominatorDefinition] = useState('')
  const [unit, setUnit] = useState('')
  const [targetValue, setTargetValue] = useState('')
  const [autoComputeSource, setAutoComputeSource] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await save.mutateAsync({
        code: code.toUpperCase(), name, description: null, category: category || null,
        numeratorDefinition, denominatorDefinition, unit: unit || null,
        targetValue: targetValue ? Number(targetValue) : null,
        autoComputeSource: autoComputeSource ? (Number(autoComputeSource) as QualityAutoComputeSource) : null,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="New quality indicator"
      onClose={onClose}
      footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={submit} loading={save.isPending}>Create</Button></>}
    >
      <div className="max-h-[65vh] space-y-3 overflow-y-auto">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <div className="grid grid-cols-2 gap-2">
          <Input label="Code" value={code} onChange={(e) => setCode(e.target.value)} />
          <Input label="Name" value={name} onChange={(e) => setName(e.target.value)} />
        </div>
        <Input label="Category" value={category} onChange={(e) => setCategory(e.target.value)} />
        <Input label="Numerator definition" value={numeratorDefinition} onChange={(e) => setNumeratorDefinition(e.target.value)} />
        <Input label="Denominator definition" value={denominatorDefinition} onChange={(e) => setDenominatorDefinition(e.target.value)} />
        <div className="grid grid-cols-2 gap-2">
          <Input label="Unit" value={unit} onChange={(e) => setUnit(e.target.value)} />
          <Input label="Target value" type="number" value={targetValue} onChange={(e) => setTargetValue(e.target.value)} />
        </div>
        <Select
          label="Auto-compute source"
          placeholder="Manual entry only"
          value={autoComputeSource}
          onChange={(e) => setAutoComputeSource(e.target.value)}
          options={Object.entries(AUTO_COMPUTE_SOURCE_LABELS).map(([value, label]) => ({ value, label }))}
        />
      </div>
    </Modal>
  )
}

function IndicatorEntriesModal({ indicator, canRecord, onClose }: { indicator: Indicator; canRecord: boolean; onClose: () => void }) {
  const { data: entries = [], isLoading } = useIndicatorEntries(indicator.id)
  const recordEntry = useRecordIndicatorEntry(indicator.id)
  const autoCompute = useAutoComputeIndicatorEntry(indicator.id)
  const [period, setPeriod] = useState(new Date().toISOString().slice(0, 7) + '-01')
  const [numerator, setNumerator] = useState('')
  const [denominator, setDenominator] = useState('')
  const [error, setError] = useState<string | null>(null)

  const doRecord = async () => {
    setError(null)
    try {
      await recordEntry.mutateAsync({ periodMonth: period, numerator: Number(numerator), denominator: Number(denominator), remarks: null })
      setNumerator(''); setDenominator('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const doAutoCompute = async () => {
    setError(null)
    try {
      await autoCompute.mutateAsync(period)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal open title={`${indicator.name} — monthly entries`} onClose={onClose} footer={<Button variant="secondary" onClick={onClose}>Close</Button>}>
      <div className="max-h-[70vh] space-y-4 overflow-y-auto">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

        {canRecord && (
          <div className="rounded-md border border-slate-200 p-3">
            <div className="grid grid-cols-3 gap-2">
              <Input label="Period (month)" type="date" value={period} onChange={(e) => setPeriod(e.target.value)} />
              <Input label="Numerator" type="number" value={numerator} onChange={(e) => setNumerator(e.target.value)} />
              <Input label="Denominator" type="number" value={denominator} onChange={(e) => setDenominator(e.target.value)} />
            </div>
            <div className="mt-2 flex gap-2">
              <Button onClick={doRecord} disabled={!numerator || !denominator} loading={recordEntry.isPending}>Record entry</Button>
              {indicator.autoComputeSource && (
                <Button variant="secondary" onClick={doAutoCompute} loading={autoCompute.isPending}>Auto-compute for this period</Button>
              )}
            </div>
          </div>
        )}

        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-3 py-2 font-medium text-slate-600">Period</th>
              <th className="px-3 py-2 font-medium text-slate-600">Numerator</th>
              <th className="px-3 py-2 font-medium text-slate-600">Denominator</th>
              <th className="px-3 py-2 font-medium text-slate-600">Value</th>
              <th className="px-3 py-2 font-medium text-slate-600">Source</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((e) => (
              <tr key={e.id} className="border-b border-slate-100 last:border-0">
                <td className="px-3 py-2 text-slate-700">{e.periodMonth}</td>
                <td className="px-3 py-2 text-slate-700">{e.numerator}</td>
                <td className="px-3 py-2 text-slate-700">{e.denominator}</td>
                <td className="px-3 py-2 font-medium text-slate-900">{e.computedValue ?? '—'}</td>
                <td className="px-3 py-2 text-slate-500">{e.isAutoComputed ? 'Auto' : 'Manual'}</td>
              </tr>
            ))}
            {!isLoading && entries.length === 0 && (
              <tr><td colSpan={5} className="px-3 py-6 text-center text-slate-400">No entries recorded yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </Modal>
  )
}
