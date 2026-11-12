import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { useCreateReportSubscription, useDeleteReportSubscription, useReportCatalog, useReportSubscriptions } from '../api'

/** Report subscription placeholder (plan §23: "schedule + outbox") — rows are stored but nothing dispatches them yet; no mailer exists in this codebase. */
export function ReportSubscriptionsPage() {
  const { data: catalog = [] } = useReportCatalog()
  const { data: subscriptions = [], isLoading } = useReportSubscriptions()
  const create = useCreateReportSubscription()
  const remove = useDeleteReportSubscription()

  const [reportCode, setReportCode] = useState('')
  const [cron, setCron] = useState('0 8 * * *')
  const [email, setEmail] = useState('')
  const [error, setError] = useState<string | null>(null)

  const executableReports = catalog.filter((c) => c.isExecutable || c.linkPath)

  const submit = async () => {
    setError(null)
    try {
      await create.mutateAsync({ reportCode, cronExpression: cron, recipientEmail: email })
      setReportCode('')
      setEmail('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader title="Report Subscriptions" subtitle="Schedule a report to be emailed periodically — delivery is a placeholder until a mailer is configured" />
      <div className="mb-6 rounded-lg border border-slate-200 bg-white p-4">
        <h3 className="mb-3 text-sm font-semibold text-slate-700">New subscription</h3>
        {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <div className="mb-3 grid grid-cols-1 gap-2 sm:grid-cols-3">
          <Select
            label="Report" placeholder="Select report" value={reportCode} onChange={(e) => setReportCode(e.target.value)}
            options={executableReports.map((c) => ({ value: c.code, label: c.name }))}
          />
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">Cron expression</span>
            <input value={cron} onChange={(e) => setCron(e.target.value)} className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" placeholder="0 8 * * *" />
          </label>
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">Recipient email</span>
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
          </label>
        </div>
        <Button onClick={submit} loading={create.isPending} disabled={!reportCode || !email}>Create subscription</Button>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Report</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Schedule</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Recipient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600"></th>
            </tr>
          </thead>
          <tbody>
            {subscriptions.map((s) => (
              <tr key={s.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{s.reportCode}</td>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{s.cronExpression}</td>
                <td className="px-4 py-2.5 text-slate-700">{s.recipientEmail}</td>
                <td className="px-4 py-2.5">
                  <Button variant="ghost" className="text-red-600" onClick={() => remove.mutate(s.id)}>Remove</Button>
                </td>
              </tr>
            ))}
            {!isLoading && subscriptions.length === 0 && (
              <tr><td colSpan={4} className="px-4 py-10 text-center text-slate-400">No subscriptions yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}
