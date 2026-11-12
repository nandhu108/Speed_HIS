import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { endpoints } from '../../../api/endpoints'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useSimpleMaster } from '../../masters/api'
import { useDeleteLabTest, useLabTest, useLabTests, useSaveLabTest } from '../api'
import type { SaveLabTestParameterPayload, SaveReferenceRangePayload } from '../types'

function newRange(): SaveReferenceRangePayload {
  return {
    sex: null,
    minAgeYears: null,
    maxAgeYears: null,
    normalLow: null,
    normalHigh: null,
    criticalLow: null,
    criticalHigh: null,
    textNormalValue: null,
  }
}

function newParameter(): SaveLabTestParameterPayload {
  return { code: '', name: '', unit: '', valueType: 1, dropdownOptions: null, sortOrder: 1, referenceRanges: [newRange()] }
}

export function LabTestsPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('LAB.TEST.MANAGE')
  const { data: tests = [], isLoading } = useLabTests()
  const remove = useDeleteLabTest()

  const [editingId, setEditingId] = useState<number | null>(null)
  const [creating, setCreating] = useState(false)

  return (
    <>
      <PageHeader
        title="Lab Tests"
        subtitle="Orderable tests, their parameters and reference ranges"
        actions={canManage && <Button onClick={() => setCreating(true)}>New test</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Sample</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Department</th>
              <th className="px-4 py-2.5 text-right font-medium text-slate-600">TAT (hrs)</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {tests.map((t) => (
              <tr key={t.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{t.code}</td>
                <td className="px-4 py-2.5 font-medium text-slate-700">
                  {t.name} {t.isPanel && <span className="ml-1 text-xs text-slate-400">(panel)</span>}
                </td>
                <td className="px-4 py-2.5 text-slate-700">{t.sampleTypeName}</td>
                <td className="px-4 py-2.5 text-slate-700">{t.departmentName ?? '—'}</td>
                <td className="px-4 py-2.5 text-right text-slate-700">{t.tatHours}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={t.isActive ? 'success' : 'neutral'}>{t.isActive ? 'Active' : 'Inactive'}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canManage && (
                    <div className="flex justify-end gap-1">
                      <Button variant="ghost" onClick={() => setEditingId(t.id)}>
                        Edit
                      </Button>
                      <Button
                        variant="ghost"
                        className="text-red-600"
                        onClick={async () => {
                          if (window.confirm(`Delete test '${t.name}'?`)) {
                            try {
                              await remove.mutateAsync(t.id)
                            } catch (err) {
                              window.alert(errorMessage(err))
                            }
                          }
                        }}
                      >
                        Delete
                      </Button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && tests.length === 0 && (
              <tr>
                <td colSpan={7} className="px-4 py-10 text-center text-slate-400">
                  No lab tests yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <LabTestEditor id={null} onClose={() => setCreating(false)} />}
      {editingId !== null && <LabTestEditor id={editingId} onClose={() => setEditingId(null)} />}
    </>
  )
}

function LabTestEditor({ id, onClose }: { id: number | null; onClose: () => void }) {
  const { data: existing } = useLabTest(id)
  const { data: departments = [] } = useSimpleMaster('departments', endpoints.masters.departments)
  const { data: sampleTypes = [] } = useSimpleMaster('sampletypes', endpoints.masters.sampleTypes)
  const save = useSaveLabTest()
  const [error, setError] = useState<string | null>(null)

  const [code, setCode] = useState(existing?.code ?? '')
  const [name, setName] = useState(existing?.name ?? '')
  const [departmentId, setDepartmentId] = useState(existing?.departmentId ? String(existing.departmentId) : '')
  const [sampleTypeId, setSampleTypeId] = useState(existing?.sampleTypeId ? String(existing.sampleTypeId) : '')
  const [method, setMethod] = useState(existing?.method ?? '')
  const [tatHours, setTatHours] = useState(String(existing?.tatHours ?? 24))
  const [isPanel, setIsPanel] = useState(existing?.isPanel ?? false)
  const [isActive, setIsActive] = useState(existing?.isActive ?? true)
  const [parameters, setParameters] = useState<SaveLabTestParameterPayload[]>(
    existing?.parameters.map((p) => ({
      code: p.code,
      name: p.name,
      unit: p.unit,
      valueType: p.valueType,
      dropdownOptions: p.dropdownOptions,
      sortOrder: p.sortOrder,
      referenceRanges: p.referenceRanges.map((r) => ({ ...r })),
    })) ?? [newParameter()],
  )

  // Re-hydrate once the detail query resolves (id set but form still had defaults).
  if (existing && code === '' && existing.code !== '') {
    setCode(existing.code)
    setName(existing.name)
    setDepartmentId(existing.departmentId ? String(existing.departmentId) : '')
    setSampleTypeId(String(existing.sampleTypeId))
    setMethod(existing.method ?? '')
    setTatHours(String(existing.tatHours))
    setIsPanel(existing.isPanel)
    setIsActive(existing.isActive)
    setParameters(
      existing.parameters.map((p) => ({
        code: p.code,
        name: p.name,
        unit: p.unit,
        valueType: p.valueType,
        dropdownOptions: p.dropdownOptions,
        sortOrder: p.sortOrder,
        referenceRanges: p.referenceRanges.map((r) => ({ ...r })),
      })),
    )
  }

  const updateParam = (index: number, patch: Partial<SaveLabTestParameterPayload>) =>
    setParameters(parameters.map((p, i) => (i === index ? { ...p, ...patch } : p)))

  const updateRange = (paramIndex: number, rangeIndex: number, patch: Partial<SaveReferenceRangePayload>) =>
    setParameters(
      parameters.map((p, i) =>
        i === paramIndex
          ? { ...p, referenceRanges: p.referenceRanges.map((r, j) => (j === rangeIndex ? { ...r, ...patch } : r)) }
          : p,
      ),
    )

  const submit = async () => {
    setError(null)
    try {
      await save.mutateAsync({
        id: id ?? undefined,
        code,
        name,
        departmentId: departmentId ? Number(departmentId) : null,
        sampleTypeId: Number(sampleTypeId),
        serviceId: null,
        method: method || null,
        tatHours: Number(tatHours),
        isPanel,
        parameters,
        isActive,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={id ? 'Edit lab test' : 'New lab test'}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={save.isPending} disabled={!code || !name || !sampleTypeId}>
            Save
          </Button>
        </>
      }
    >
      <div className="max-h-[70vh] space-y-4 overflow-y-auto">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

        <div className="grid grid-cols-2 gap-3">
          <Input label="Code" value={code} onChange={(e) => setCode(e.target.value)} />
          <Input label="Name" value={name} onChange={(e) => setName(e.target.value)} />
          <Select
            label="Sample type"
            placeholder="Select…"
            value={sampleTypeId}
            onChange={(e) => setSampleTypeId(e.target.value)}
            options={sampleTypes.map((s) => ({ value: s.id, label: s.name }))}
          />
          <Select
            label="Department (optional)"
            placeholder="None"
            value={departmentId}
            onChange={(e) => setDepartmentId(e.target.value)}
            options={departments.map((d) => ({ value: d.id, label: d.name }))}
          />
          <Input label="Method" value={method} onChange={(e) => setMethod(e.target.value)} />
          <Input label="TAT (hours)" type="number" min={1} value={tatHours} onChange={(e) => setTatHours(e.target.value)} />
        </div>
        <div className="flex gap-4">
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input type="checkbox" checked={isPanel} onChange={(e) => setIsPanel(e.target.checked)} />
            Panel (multiple parameters)
          </label>
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
            Active
          </label>
        </div>

        <div>
          <div className="mb-2 flex items-center justify-between">
            <h3 className="text-sm font-semibold text-slate-800">Parameters</h3>
            <Button variant="secondary" onClick={() => setParameters([...parameters, newParameter()])}>
              Add parameter
            </Button>
          </div>
          <div className="space-y-3">
            {parameters.map((param, pIndex) => (
              <div key={pIndex} className="rounded-md border border-slate-200 p-3">
                <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
                  <Input label="Code" value={param.code} onChange={(e) => updateParam(pIndex, { code: e.target.value })} />
                  <Input label="Name" value={param.name} onChange={(e) => updateParam(pIndex, { name: e.target.value })} />
                  <Input
                    label="Unit"
                    value={param.unit ?? ''}
                    onChange={(e) => updateParam(pIndex, { unit: e.target.value || null })}
                  />
                  <Select
                    label="Type"
                    value={String(param.valueType)}
                    onChange={(e) => updateParam(pIndex, { valueType: Number(e.target.value) as 1 | 2 | 3 })}
                    options={[
                      { value: 1, label: 'Numeric' },
                      { value: 2, label: 'Text' },
                      { value: 3, label: 'Dropdown' },
                    ]}
                  />
                </div>
                {param.valueType === 3 && (
                  <Input
                    className="mt-2"
                    label="Dropdown options (comma-separated)"
                    value={param.dropdownOptions ?? ''}
                    onChange={(e) => updateParam(pIndex, { dropdownOptions: e.target.value || null })}
                  />
                )}

                <div className="mt-2">
                  <div className="mb-1 flex items-center justify-between">
                    <span className="text-xs font-medium text-slate-500">Reference ranges</span>
                    <Button
                      variant="ghost"
                      onClick={() => updateParam(pIndex, { referenceRanges: [...param.referenceRanges, newRange()] })}
                    >
                      Add range
                    </Button>
                  </div>
                  {param.referenceRanges.map((range, rIndex) => (
                    <div key={rIndex} className="mb-1 grid grid-cols-3 gap-2 sm:grid-cols-6">
                      <Select
                        label="Sex"
                        placeholder="Any"
                        value={range.sex ? String(range.sex) : ''}
                        onChange={(e) => updateRange(pIndex, rIndex, { sex: e.target.value ? (Number(e.target.value) as 1 | 2 | 3) : null })}
                        options={[
                          { value: 1, label: 'Male' },
                          { value: 2, label: 'Female' },
                        ]}
                      />
                      <Input
                        label="Min age"
                        type="number"
                        value={range.minAgeYears ?? ''}
                        onChange={(e) => updateRange(pIndex, rIndex, { minAgeYears: e.target.value ? Number(e.target.value) : null })}
                      />
                      <Input
                        label="Max age"
                        type="number"
                        value={range.maxAgeYears ?? ''}
                        onChange={(e) => updateRange(pIndex, rIndex, { maxAgeYears: e.target.value ? Number(e.target.value) : null })}
                      />
                      <Input
                        label="Normal low"
                        type="number"
                        value={range.normalLow ?? ''}
                        onChange={(e) => updateRange(pIndex, rIndex, { normalLow: e.target.value ? Number(e.target.value) : null })}
                      />
                      <Input
                        label="Normal high"
                        type="number"
                        value={range.normalHigh ?? ''}
                        onChange={(e) => updateRange(pIndex, rIndex, { normalHigh: e.target.value ? Number(e.target.value) : null })}
                      />
                      <div className="grid grid-cols-2 gap-1">
                        <Input
                          label="Crit. low"
                          type="number"
                          value={range.criticalLow ?? ''}
                          onChange={(e) => updateRange(pIndex, rIndex, { criticalLow: e.target.value ? Number(e.target.value) : null })}
                        />
                        <Input
                          label="Crit. high"
                          type="number"
                          value={range.criticalHigh ?? ''}
                          onChange={(e) => updateRange(pIndex, rIndex, { criticalHigh: e.target.value ? Number(e.target.value) : null })}
                        />
                      </div>
                    </div>
                  ))}
                </div>

                {parameters.length > 1 && (
                  <div className="mt-2 flex justify-end">
                    <Button
                      variant="ghost"
                      className="text-red-600"
                      onClick={() => setParameters(parameters.filter((_, i) => i !== pIndex))}
                    >
                      Remove parameter
                    </Button>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      </div>
    </Modal>
  )
}
