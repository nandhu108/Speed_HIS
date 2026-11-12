import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { useAuth } from '../../auth/AuthContext'
import { useWards } from '../../masters/api'
import { useCreateShiftHandover, useShiftHandover, useShiftHandovers, useWardPatients } from '../api'
import { SHIFT_LABELS, type NursingShift, type PatientHandoverNotePayload } from '../types'

export function ShiftHandoversPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('NURSING.HANDOVER.MANAGE')
  const { data: wards = [] } = useWards()
  const [wardId, setWardId] = useState('')
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10))
  const { data: handovers = [], isLoading } = useShiftHandovers(wardId ? Number(wardId) : undefined, date || undefined)
  const [creating, setCreating] = useState(false)
  const [viewingId, setViewingId] = useState<number | null>(null)

  return (
    <>
      <PageHeader
        title="Shift Handovers"
        subtitle="Ward-level, per-shift SBAR handover"
        actions={canManage && <Button onClick={() => setCreating(true)}>New handover</Button>}
      />

      <div className="mb-4 flex flex-wrap gap-2">
        <Select
          label="Ward"
          placeholder="All wards"
          value={wardId}
          onChange={(e) => setWardId(e.target.value)}
          options={wards.map((w) => ({ value: w.id, label: w.name }))}
        />
        <Input label="Date" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
      </div>

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Ward</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Date</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Shift</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">From</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">To</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patients</th>
            </tr>
          </thead>
          <tbody>
            {handovers.map((h) => (
              <tr key={h.id} className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50" onClick={() => setViewingId(h.id)}>
                <td className="px-4 py-2.5 text-slate-700">{h.wardName}</td>
                <td className="px-4 py-2.5 text-slate-700">{h.shiftDate}</td>
                <td className="px-4 py-2.5 text-slate-700">{SHIFT_LABELS[h.shift]}</td>
                <td className="px-4 py-2.5 text-slate-700">{h.handoverFromName}</td>
                <td className="px-4 py-2.5 text-slate-700">{h.handoverToName ?? '—'}</td>
                <td className="px-4 py-2.5 text-slate-700">{h.patientCount}</td>
              </tr>
            ))}
            {!isLoading && handovers.length === 0 && (
              <tr><td colSpan={6} className="px-4 py-10 text-center text-slate-400">No handovers found.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <CreateHandoverModal onClose={() => setCreating(false)} />}
      {viewingId !== null && <HandoverDetailModal id={viewingId} onClose={() => setViewingId(null)} />}
    </>
  )
}

function HandoverDetailModal({ id, onClose }: { id: number; onClose: () => void }) {
  const { data: handover, isLoading } = useShiftHandover(id)
  return (
    <Modal open title="Shift handover" onClose={onClose} footer={<Button variant="secondary" onClick={onClose}>Close</Button>}>
      {isLoading || !handover ? (
        <Spinner />
      ) : (
        <div className="max-h-[65vh] space-y-4 overflow-y-auto text-sm">
          <p className="text-slate-600">
            {handover.wardName} · {handover.shiftDate} · {SHIFT_LABELS[handover.shift]} · {handover.handoverFromName}
            {handover.handoverToName ? ` → ${handover.handoverToName}` : ''}
          </p>
          {handover.generalNotes && <p className="rounded-md bg-slate-50 p-2 text-slate-700">{handover.generalNotes}</p>}
          <ul className="space-y-3">
            {handover.patientNotes.map((n) => (
              <li key={n.id} className="rounded-md border border-slate-200 p-3">
                <p className="mb-1 text-xs font-semibold text-slate-500">{n.patientName} ({n.admissionNumber})</p>
                <p><span className="font-medium">S:</span> {n.situation}</p>
                <p><span className="font-medium">B:</span> {n.background}</p>
                <p><span className="font-medium">A:</span> {n.assessment}</p>
                <p><span className="font-medium">R:</span> {n.recommendation}</p>
              </li>
            ))}
            {handover.patientNotes.length === 0 && <p className="text-slate-400">No patient notes recorded.</p>}
          </ul>
        </div>
      )}
    </Modal>
  )
}

function CreateHandoverModal({ onClose }: { onClose: () => void }) {
  const { data: wards = [] } = useWards()
  const create = useCreateShiftHandover()
  const [wardId, setWardId] = useState('')
  const [shiftDate, setShiftDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [shift, setShift] = useState('1')
  const [generalNotes, setGeneralNotes] = useState('')
  const { data: wardPatients = [] } = useWardPatients(wardId ? Number(wardId) : null)
  const [notes, setNotes] = useState<Record<number, PatientHandoverNotePayload>>({})
  const [error, setError] = useState<string | null>(null)

  const updateNote = (admissionId: number, field: keyof PatientHandoverNotePayload, value: string) => {
    setNotes((prev) => ({
      ...prev,
      [admissionId]: { ...(prev[admissionId] ?? { admissionId, situation: '', background: '', assessment: '', recommendation: '' }), [field]: value },
    }))
  }

  const submit = async () => {
    setError(null)
    if (!wardId) {
      setError('Ward is required.')
      return
    }
    const patientNotes = Object.values(notes).filter((n) => n.situation.trim() || n.background.trim() || n.assessment.trim() || n.recommendation.trim())
    try {
      await create.mutateAsync({
        wardId: Number(wardId), shiftDate, shift: Number(shift) as NursingShift, handoverToUserId: null,
        generalNotes: generalNotes || null, patientNotes,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="New shift handover"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={create.isPending}>Save handover</Button>
        </>
      }
    >
      <div className="max-h-[70vh] space-y-3 overflow-y-auto">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <div className="grid grid-cols-3 gap-2">
          <Select label="Ward" placeholder="Select…" value={wardId} onChange={(e) => setWardId(e.target.value)} options={wards.map((w) => ({ value: w.id, label: w.name }))} />
          <Input label="Date" type="date" value={shiftDate} onChange={(e) => setShiftDate(e.target.value)} />
          <Select label="Shift" value={shift} onChange={(e) => setShift(e.target.value)} options={Object.entries(SHIFT_LABELS).map(([value, label]) => ({ value, label }))} />
        </div>
        <Input label="General notes" value={generalNotes} onChange={(e) => setGeneralNotes(e.target.value)} />

        {wardId && (
          <div>
            <p className="mb-2 text-xs font-semibold uppercase text-slate-500">Patient SBAR notes</p>
            <div className="space-y-3">
              {wardPatients.map((wp) => (
                <div key={wp.admissionId} className="rounded-md border border-slate-200 p-3">
                  <p className="mb-2 text-sm font-medium text-slate-700">{wp.patientName} — {wp.bedCode} ({wp.admissionNumber})</p>
                  <div className="grid grid-cols-2 gap-2">
                    <Input label="Situation" value={notes[wp.admissionId]?.situation ?? ''} onChange={(e) => updateNote(wp.admissionId, 'situation', e.target.value)} />
                    <Input label="Background" value={notes[wp.admissionId]?.background ?? ''} onChange={(e) => updateNote(wp.admissionId, 'background', e.target.value)} />
                    <Input label="Assessment" value={notes[wp.admissionId]?.assessment ?? ''} onChange={(e) => updateNote(wp.admissionId, 'assessment', e.target.value)} />
                    <Input label="Recommendation" value={notes[wp.admissionId]?.recommendation ?? ''} onChange={(e) => updateNote(wp.admissionId, 'recommendation', e.target.value)} />
                  </div>
                </div>
              ))}
              {wardPatients.length === 0 && <p className="text-sm text-slate-400">No patients currently admitted in this ward.</p>}
            </div>
          </div>
        )}
      </div>
    </Modal>
  )
}
