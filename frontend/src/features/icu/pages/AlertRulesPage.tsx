import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useCriticalAlertRules, useSaveCriticalAlertRule } from '../api'
import { ALERT_SEVERITY_LABELS, OPERATOR_LABELS, PARAMETER_LABELS, type AlertSeverity, type ComparisonOperator, type CriticalAlertParameter, type CriticalAlertRule } from '../types'

export function AlertRulesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('ICU.RULE.MANAGE')
  const { data: rules = [], isLoading } = useCriticalAlertRules()
  const [editing, setEditing] = useState<CriticalAlertRule | null>(null)
  const [creating, setCreating] = useState(false)

  return (
    <>
      <PageHeader
        title="Critical Alert Rules"
        subtitle="Branch-configurable vitals thresholds — evaluated against every new ICU vitals reading"
        actions={canManage && <Button onClick={() => setCreating(true)}>New rule</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Parameter</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Condition</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Severity</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Description</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {rules.map((r) => (
              <tr key={r.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-medium text-slate-700">{PARAMETER_LABELS[r.parameter]}</td>
                <td className="px-4 py-2.5 text-slate-700">{OPERATOR_LABELS[r.operator]} {r.thresholdValue}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={r.severity === 2 ? 'danger' : 'warning'}>{ALERT_SEVERITY_LABELS[r.severity]}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-slate-500">{r.description ?? '—'}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={r.isActive ? 'success' : 'neutral'}>{r.isActive ? 'Active' : 'Inactive'}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-right">
                  {canManage && (
                    <Button variant="ghost" onClick={() => setEditing(r)}>
                      Edit
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {!isLoading && rules.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  No rules yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <RuleEditor rule={null} onClose={() => setCreating(false)} />}
      {editing && <RuleEditor rule={editing} onClose={() => setEditing(null)} />}
    </>
  )
}

function RuleEditor({ rule, onClose }: { rule: CriticalAlertRule | null; onClose: () => void }) {
  const save = useSaveCriticalAlertRule()
  const [parameter, setParameter] = useState<string>(rule ? String(rule.parameter) : '1')
  const [operator, setOperator] = useState<string>(rule ? String(rule.operator) : '1')
  const [thresholdValue, setThresholdValue] = useState(rule ? String(rule.thresholdValue) : '')
  const [severity, setSeverity] = useState<string>(rule ? String(rule.severity) : '2')
  const [description, setDescription] = useState(rule?.description ?? '')
  const [isActive, setIsActive] = useState(rule?.isActive ?? true)
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await save.mutateAsync({
        id: rule?.id,
        parameter: Number(parameter) as CriticalAlertParameter,
        operator: Number(operator) as ComparisonOperator,
        thresholdValue: Number(thresholdValue),
        severity: Number(severity) as AlertSeverity,
        description: description || null,
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
      title={rule ? 'Edit rule' : 'New rule'}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={save.isPending} disabled={!thresholdValue}>Save</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Select
          label="Parameter"
          value={parameter}
          onChange={(e) => setParameter(e.target.value)}
          options={Object.entries(PARAMETER_LABELS).map(([value, label]) => ({ value, label }))}
        />
        <div className="grid grid-cols-2 gap-3">
          <Select
            label="Condition"
            value={operator}
            onChange={(e) => setOperator(e.target.value)}
            options={Object.entries(OPERATOR_LABELS).map(([value, label]) => ({ value, label }))}
          />
          <Input label="Threshold" type="number" value={thresholdValue} onChange={(e) => setThresholdValue(e.target.value)} />
        </div>
        <Select
          label="Severity"
          value={severity}
          onChange={(e) => setSeverity(e.target.value)}
          options={Object.entries(ALERT_SEVERITY_LABELS).map(([value, label]) => ({ value, label }))}
        />
        <Input label="Description" value={description} onChange={(e) => setDescription(e.target.value)} />
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
          Active
        </label>
      </div>
    </Modal>
  )
}
