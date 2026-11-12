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
import { useAuditTemplates, useAudits, useCompleteAudit, useCreateAuditTemplate, useScheduleAudit, useScoreAudit } from '../api'
import { AUDIT_STATUS_LABELS, type Audit, type QualityAuditStatus } from '../types'

const statusTone: Record<QualityAuditStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info', 2: 'warning', 3: 'success', 4: 'neutral',
}

export function AuditsPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('QUALITY.AUDIT.MANAGE')
  const canManageTemplates = hasPermission('QUALITY.AUDITTEMPLATE.MANAGE')
  const [page, setPage] = useState(1)
  const { data, isLoading } = useAudits({ page, pageSize: 20 })
  const { data: templates = [] } = useAuditTemplates()
  const [schedulingTemplate, setSchedulingTemplate] = useState(false)
  const [creatingTemplate, setCreatingTemplate] = useState(false)
  const [selected, setSelected] = useState<Audit | null>(null)

  return (
    <>
      <PageHeader
        title="Audit Checklists"
        subtitle="Templates, scheduled audits and scoring"
        actions={
          <>
            {canManageTemplates && <Button variant="secondary" onClick={() => setCreatingTemplate(true)}>New template</Button>}
            {canManage && <Button onClick={() => setSchedulingTemplate(true)}>Schedule audit</Button>}
          </>
        }
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Template</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Scheduled</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Conducted</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Score</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            </tr>
          </thead>
          <tbody>
            {(data?.items ?? []).map((a) => (
              <tr key={a.id} className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50" onClick={() => setSelected(a)}>
                <td className="px-4 py-2.5 text-slate-700">{a.templateName}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.scheduledDate}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.conductedDate ?? '—'}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.scorePercent !== null ? `${a.scorePercent}%` : '—'}</td>
                <td className="px-4 py-2.5"><StatusBadge tone={statusTone[a.status]}>{AUDIT_STATUS_LABELS[a.status]}</StatusBadge></td>
              </tr>
            ))}
            {!isLoading && (data?.items.length ?? 0) === 0 && (
              <tr><td colSpan={5} className="px-4 py-10 text-center text-slate-400">No audits scheduled.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {data && data.totalPages > 1 && (
        <div className="mt-3 flex items-center justify-end gap-2 text-sm">
          <Button variant="secondary" disabled={page <= 1} onClick={() => setPage((n) => n - 1)}>Prev</Button>
          <span className="text-slate-500">Page {page} of {data.totalPages}</span>
          <Button variant="secondary" disabled={page >= data.totalPages} onClick={() => setPage((n) => n + 1)}>Next</Button>
        </div>
      )}

      {creatingTemplate && <TemplateEditorModal onClose={() => setCreatingTemplate(false)} />}
      {schedulingTemplate && <ScheduleModal templates={templates} onClose={() => setSchedulingTemplate(false)} />}
      {selected && <AuditDetailModal audit={selected} canManage={canManage} onClose={() => setSelected(null)} />}
    </>
  )
}

function TemplateEditorModal({ onClose }: { onClose: () => void }) {
  const create = useCreateAuditTemplate()
  const [code, setCode] = useState('')
  const [name, setName] = useState('')
  const [category, setCategory] = useState('')
  const [questions, setQuestions] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    const lines = questions.split('\n').map((l) => l.trim()).filter(Boolean)
    if (lines.length === 0) { setError('Add at least one checklist question (one per line).'); return }
    try {
      await create.mutateAsync({
        code: code.toUpperCase(), name, category: category || null,
        items: lines.map((question, i) => ({ sortOrder: (i + 1) * 10, question, maxScore: 5 })),
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal open title="New audit checklist template" onClose={onClose} footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={submit} loading={create.isPending}>Create</Button></>}>
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <div className="grid grid-cols-2 gap-2">
          <Input label="Code" value={code} onChange={(e) => setCode(e.target.value)} />
          <Input label="Name" value={name} onChange={(e) => setName(e.target.value)} />
        </div>
        <Input label="Category" value={category} onChange={(e) => setCategory(e.target.value)} />
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700">Checklist questions (one per line, max score 5 each)</label>
          <textarea className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" rows={5} value={questions} onChange={(e) => setQuestions(e.target.value)} />
        </div>
      </div>
    </Modal>
  )
}

function ScheduleModal({ templates, onClose }: { templates: { id: number; name: string }[]; onClose: () => void }) {
  const schedule = useScheduleAudit()
  const [templateId, setTemplateId] = useState('')
  const [scheduledDate, setScheduledDate] = useState(new Date().toISOString().slice(0, 10))
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await schedule.mutateAsync({ templateId: Number(templateId), scheduledDate })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal open title="Schedule audit" onClose={onClose} footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={submit} loading={schedule.isPending} disabled={!templateId}>Schedule</Button></>}>
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Select label="Template" placeholder="Select a template" value={templateId} onChange={(e) => setTemplateId(e.target.value)} options={templates.map((t) => ({ value: t.id, label: t.name }))} />
        <Input label="Scheduled date" type="date" value={scheduledDate} onChange={(e) => setScheduledDate(e.target.value)} />
      </div>
    </Modal>
  )
}

function AuditDetailModal({ audit, canManage, onClose }: { audit: Audit; canManage: boolean; onClose: () => void }) {
  const score = useScoreAudit()
  const complete = useCompleteAudit()
  const [scores, setScores] = useState<Record<number, string>>(
    Object.fromEntries(audit.responses.map((r) => [r.templateItemId, r.score ? String(r.score) : ''])),
  )
  const [error, setError] = useState<string | null>(null)

  const submitScores = async () => {
    setError(null)
    try {
      await score.mutateAsync({
        id: audit.id,
        responses: audit.responses.map((r) => ({ templateItemId: r.templateItemId, score: Number(scores[r.templateItemId] || 0), remarks: null })),
      })
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const doComplete = async () => {
    setError(null)
    try {
      await complete.mutateAsync(audit.id)
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const canEdit = canManage && audit.status !== 3 && audit.status !== 4

  return (
    <Modal open title={audit.templateName} onClose={onClose} footer={<Button variant="secondary" onClick={onClose}>Close</Button>}>
      <div className="max-h-[65vh] space-y-3 overflow-y-auto">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        {audit.responses.map((r) => (
          <div key={r.templateItemId} className="flex items-center justify-between gap-3 border-b border-slate-100 pb-2">
            <span className="text-sm text-slate-700">{r.question}</span>
            <Input
              type="number"
              className="w-20"
              value={scores[r.templateItemId] ?? ''}
              onChange={(e) => setScores((s) => ({ ...s, [r.templateItemId]: e.target.value }))}
              disabled={!canEdit}
              max={r.maxScore}
              min={0}
            />
          </div>
        ))}
        {canEdit && (
          <div className="flex gap-2 pt-2">
            <Button onClick={submitScores} loading={score.isPending}>Save scores</Button>
            <Button variant="secondary" onClick={doComplete} loading={complete.isPending}>Complete audit</Button>
          </div>
        )}
      </div>
    </Modal>
  )
}
