import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useCodingWorklist, useMarkCoded } from '../api'
import { CODING_STATUS_LABELS, type CodingWorklistItem } from '../types'

export function CodingWorklistPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MRD.CODING.MANAGE')
  const [onlyPending, setOnlyPending] = useState(true)
  const { data: items = [], isLoading } = useCodingWorklist(onlyPending)
  const [coding, setCoding] = useState<CodingWorklistItem | null>(null)

  return (
    <>
      <PageHeader title="ICD Coding Worklist" subtitle="Discharged cases pending or completed ICD coding" />

      <div className="mb-3">
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" checked={onlyPending} onChange={(e) => setOnlyPending(e.target.checked)} />
          Only pending
        </label>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Admission</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Discharged</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Coded by</th>
              {canManage && <th className="px-4 py-2.5 font-medium text-slate-600">Actions</th>}
            </tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr key={item.admissionId} className="border-b border-slate-100 last:border-0 hover:bg-slate-50">
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{item.admissionNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">
                  {item.patientName} <span className="text-slate-400">({item.patientUhid})</span>
                </td>
                <td className="px-4 py-2.5 text-slate-700">{item.dischargedAt ? new Date(item.dischargedAt).toLocaleDateString() : '—'}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={item.status === 2 ? 'success' : 'warning'}>{CODING_STATUS_LABELS[item.status]}</StatusBadge>
                </td>
                <td className="px-4 py-2.5 text-slate-700">{item.codedByName ?? '—'}</td>
                {canManage && (
                  <td className="px-4 py-2.5">
                    {item.status === 1 && (
                      <Button variant="secondary" onClick={() => setCoding(item)}>Mark coded</Button>
                    )}
                  </td>
                )}
              </tr>
            ))}
            {!isLoading && items.length === 0 && (
              <tr>
                <td colSpan={canManage ? 6 : 5} className="px-4 py-10 text-center text-slate-400">
                  No discharged cases found.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {coding && <MarkCodedModal item={coding} onClose={() => setCoding(null)} />}
    </>
  )
}

function MarkCodedModal({ item, onClose }: { item: CodingWorklistItem; onClose: () => void }) {
  const mark = useMarkCoded()
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await mark.mutateAsync({ admissionId: item.admissionId, notes: notes || null })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Mark ${item.admissionNumber} coded`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={mark.isPending}>Mark coded</Button>
        </>
      }
    >
      {error && <p className="mb-2 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <textarea
        className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
        rows={3}
        placeholder="Coding notes (optional)"
        value={notes}
        onChange={(e) => setNotes(e.target.value)}
      />
    </Modal>
  )
}
