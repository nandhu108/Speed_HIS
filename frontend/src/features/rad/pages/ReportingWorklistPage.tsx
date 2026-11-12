import { useEffect, useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { useAuth } from '../../auth/AuthContext'
import {
  useApproveReport,
  useEnterReport,
  useRadReport,
  useRadReportTemplate,
  useRadReportTemplates,
  useReportingEntryQueue,
  useReportingReviewQueue,
  useVerifyReport,
} from '../api'
import { PRIORITY_LABELS, type ReportingWorklistItem } from '../types'

type Tab = 'entry' | 'verify' | 'approve'

export function ReportingWorklistPage() {
  const { hasPermission } = useAuth()
  const [tab, setTab] = useState<Tab>('entry')
  const [entering, setEntering] = useState<ReportingWorklistItem | null>(null)
  const [reviewing, setReviewing] = useState<{ item: ReportingWorklistItem; mode: 'verify' | 'approve' } | null>(null)
  const [error, setError] = useState<string | null>(null)

  const { data: entryQueue = [], isLoading: loadingEntry } = useReportingEntryQueue()
  const { data: verifyQueue = [], isLoading: loadingVerify } = useReportingReviewQueue(4)
  const { data: approveQueue = [], isLoading: loadingApprove } = useReportingReviewQueue(5)

  const canEnter = hasPermission('RAD.REPORT.CREATE')
  const canVerify = hasPermission('RAD.REPORT.VERIFY')
  const canApprove = hasPermission('RAD.REPORT.APPROVE')

  const tabs: { key: Tab; label: string; count: number }[] = [
    { key: 'entry', label: 'Entry', count: entryQueue.length },
    { key: 'verify', label: 'Verify', count: verifyQueue.length },
    { key: 'approve', label: 'Approve', count: approveQueue.length },
  ]

  const activeQueue = tab === 'entry' ? entryQueue : tab === 'verify' ? verifyQueue : approveQueue
  const activeLoading = tab === 'entry' ? loadingEntry : tab === 'verify' ? loadingVerify : loadingApprove

  return (
    <>
      <PageHeader title="Reporting Worklist" subtitle="Enter, verify and approve radiology reports" />

      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="mb-4 flex gap-1 border-b border-slate-200">
        {tabs.map((t) => (
          <button
            key={t.key}
            type="button"
            onClick={() => setTab(t.key)}
            className={`border-b-2 px-4 py-2 text-sm font-medium ${
              tab === t.key ? 'border-primary-600 text-primary-700' : 'border-transparent text-slate-500 hover:text-slate-700'
            }`}
          >
            {t.label} {t.count > 0 && <span className="ml-1 text-xs text-slate-400">({t.count})</span>}
          </button>
        ))}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {activeLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Order No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Test</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Priority</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {activeQueue.map((item) => (
              <tr key={item.radOrderItemId} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{item.orderNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">
                  {item.patientName} ({item.patientUhid}) — {item.patientAgeDisplay} / {item.patientGender}
                </td>
                <td className="px-4 py-2.5 text-slate-700">{item.radiologyTestName}</td>
                <td className="px-4 py-2.5 text-slate-700">{PRIORITY_LABELS[item.priority]}</td>
                <td className="px-4 py-2.5 text-right">
                  {tab === 'entry' && canEnter && (
                    <Button variant="secondary" onClick={() => setEntering(item)}>
                      Enter report
                    </Button>
                  )}
                  {tab === 'verify' && canVerify && (
                    <Button variant="secondary" onClick={() => setReviewing({ item, mode: 'verify' })}>
                      Review
                    </Button>
                  )}
                  {tab === 'approve' && canApprove && (
                    <Button variant="secondary" onClick={() => setReviewing({ item, mode: 'approve' })}>
                      Review
                    </Button>
                  )}
                </td>
              </tr>
            ))}
            {!activeLoading && activeQueue.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                  Nothing here.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {entering && <EnterReportModal item={entering} onClose={() => setEntering(null)} onError={setError} />}
      {reviewing && (
        <ReviewReportModal item={reviewing.item} mode={reviewing.mode} onClose={() => setReviewing(null)} onError={setError} />
      )}
    </>
  )
}

function EnterReportModal({
  item,
  onClose,
  onError,
}: {
  item: ReportingWorklistItem
  onClose: () => void
  onError: (msg: string) => void
}) {
  const { data: templates = [] } = useRadReportTemplates(item.radiologyTestId)
  const enterReport = useEnterReport()
  const [templateId, setTemplateId] = useState('')
  const [findings, setFindings] = useState('')
  const [impression, setImpression] = useState('')
  const { data: templateDetail } = useRadReportTemplate(templateId ? Number(templateId) : null)

  useEffect(() => {
    if (templateDetail) {
      setFindings(templateDetail.findingsTemplate)
      setImpression(templateDetail.impressionTemplate)
    }
  }, [templateDetail])

  const submit = async () => {
    if (!findings.trim() || !impression.trim()) return
    try {
      await enterReport.mutateAsync({
        itemId: item.radOrderItemId, templateId: templateId ? Number(templateId) : null, findings, impression,
      })
      onClose()
    } catch (err) {
      onError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Enter report — ${item.radiologyTestName}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={enterReport.isPending} disabled={!findings.trim() || !impression.trim()}>
            Save
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        <p className="text-sm text-slate-600">
          {item.patientName} ({item.patientUhid}) — {item.patientAgeDisplay} / {item.patientGender}
        </p>
        {templates.length > 0 && (
          <Select
            label="Template"
            placeholder="Start from a template…"
            value={templateId}
            onChange={(e) => setTemplateId(e.target.value)}
            options={templates.map((t) => ({ value: t.id, label: t.name }))}
          />
        )}
        <label className="block text-sm font-medium text-slate-700">
          Findings
          <textarea
            className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
            rows={5}
            value={findings}
            onChange={(e) => setFindings(e.target.value)}
          />
        </label>
        <label className="block text-sm font-medium text-slate-700">
          Impression
          <textarea
            className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
            rows={3}
            value={impression}
            onChange={(e) => setImpression(e.target.value)}
          />
        </label>
      </div>
    </Modal>
  )
}

function ReviewReportModal({
  item,
  mode,
  onClose,
  onError,
}: {
  item: ReportingWorklistItem
  mode: 'verify' | 'approve'
  onClose: () => void
  onError: (msg: string) => void
}) {
  const { data: report, isLoading } = useRadReport(item.radOrderItemId)
  const verify = useVerifyReport()
  const approve = useApproveReport()
  const pending = mode === 'verify' ? verify.isPending : approve.isPending

  const submit = async () => {
    try {
      if (mode === 'verify') await verify.mutateAsync(item.radOrderItemId)
      else await approve.mutateAsync(item.radOrderItemId)
      onClose()
    } catch (err) {
      onError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`${mode === 'verify' ? 'Verify' : 'Approve'} — ${item.radiologyTestName}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={pending}>
            {mode === 'verify' ? 'Verify' : 'Approve'}
          </Button>
        </>
      }
    >
      {isLoading && <Spinner />}
      {report && (
        <div className="space-y-3 text-sm">
          <p className="text-slate-600">
            {item.patientName} ({item.patientUhid}) — {item.patientAgeDisplay} / {item.patientGender}
          </p>
          <div>
            <p className="mb-1 text-xs font-semibold uppercase text-slate-500">Findings</p>
            <p className="text-slate-700">{report.findings}</p>
          </div>
          <div>
            <p className="mb-1 text-xs font-semibold uppercase text-slate-500">Impression</p>
            <p className="text-slate-700">{report.impression}</p>
          </div>
        </div>
      )}
    </Modal>
  )
}
