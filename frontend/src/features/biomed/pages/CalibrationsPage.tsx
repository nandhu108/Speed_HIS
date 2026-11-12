import { useState } from 'react'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { calibrationCertificateUrl, useCalibrations } from '../api'

export function CalibrationsPage() {
  const [dueOnly, setDueOnly] = useState(false)
  const { data: calibrations = [], isLoading } = useCalibrations(undefined, dueOnly)

  return (
    <>
      <PageHeader title="Calibrations" subtitle="Due tracking and certificates — new calibrations are recorded from an equipment's detail page" />

      <label className="mb-3 flex items-center gap-2 text-sm text-slate-700">
        <input type="checkbox" checked={dueOnly} onChange={(e) => setDueOnly(e.target.checked)} />
        Due only
      </label>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Equipment</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Calibrated</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Next Due</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Performed By</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Certificate</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            </tr>
          </thead>
          <tbody>
            {calibrations.map((c) => (
              <tr key={c.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-2.5 text-slate-700">{c.equipmentName}</td>
                <td className="px-4 py-2.5 text-slate-700">{c.calibrationDate}</td>
                <td className="px-4 py-2.5 text-slate-700">{c.nextDueDate}</td>
                <td className="px-4 py-2.5 text-slate-700">{c.performedBy ?? '—'}</td>
                <td className="px-4 py-2.5">
                  {c.hasCertificateFile ? (
                    <a href={calibrationCertificateUrl(c.id)} target="_blank" rel="noreferrer" className="text-primary-600 hover:underline">Download</a>
                  ) : '—'}
                </td>
                <td className="px-4 py-2.5">{c.isDue && <StatusBadge tone="warning">Due</StatusBadge>}</td>
              </tr>
            ))}
            {!isLoading && calibrations.length === 0 && (
              <tr><td colSpan={6} className="px-4 py-10 text-center text-slate-400">No calibrations found.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}
