import { useState } from 'react'
import { endpoints } from '../../../api/endpoints'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { useUsers } from '../../admin/api'
import { useAuth } from '../../auth/AuthContext'
import { useSimpleMaster } from '../../masters/api'
import { useCompleteCommitteeActionItem, useCreateCommitteeMeeting, useCommitteeMeetings } from '../api'
import type { SaveCommitteeActionItemPayload } from '../types'

export function CommitteeMeetingsPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('QUALITY.COMMITTEE.MANAGE')
  const { data: meetings = [], isLoading } = useCommitteeMeetings()
  const [creating, setCreating] = useState(false)
  const completeItem = useCompleteCommitteeActionItem()
  const [error, setError] = useState<string | null>(null)

  return (
    <>
      <PageHeader title="Committee Meetings" subtitle="Minutes and action items — manage committees under Quality / NABH > Committees" actions={canManage && <Button onClick={() => setCreating(true)}>Record meeting</Button>} />
      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      {isLoading && <Spinner />}
      <div className="space-y-4">
        {meetings.map((m) => (
          <div key={m.id} className="rounded-lg border border-slate-200 bg-white p-4">
            <div className="mb-2 flex items-center justify-between">
              <h3 className="font-medium text-slate-900">{m.committeeName} — {m.meetingDate}</h3>
            </div>
            <p className="mb-3 whitespace-pre-wrap text-sm text-slate-700">{m.minutes}</p>
            {m.attendeesText && <p className="mb-3 text-xs text-slate-500">Attendees: {m.attendeesText}</p>}
            {m.actionItems.length > 0 && (
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-200 text-left text-xs text-slate-500">
                    <th className="py-1.5 font-medium">Action item</th>
                    <th className="py-1.5 font-medium">Owner</th>
                    <th className="py-1.5 font-medium">Due</th>
                    <th className="py-1.5 font-medium"></th>
                  </tr>
                </thead>
                <tbody>
                  {m.actionItems.map((a) => (
                    <tr key={a.id} className="border-b border-slate-100 last:border-0">
                      <td className="py-1.5 text-slate-700">{a.description}</td>
                      <td className="py-1.5 text-slate-700">{a.ownerName}</td>
                      <td className="py-1.5 text-slate-700">{a.dueDate}</td>
                      <td className="py-1.5 text-right">
                        {a.isCompleted ? (
                          <span className="text-xs text-emerald-600">Done</span>
                        ) : canManage && (
                          <Button variant="ghost" onClick={async () => { try { await completeItem.mutateAsync({ meetingId: m.id, actionItemId: a.id }) } catch (err) { setError(errorMessage(err)) } }}>
                            Mark done
                          </Button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        ))}
        {!isLoading && meetings.length === 0 && <p className="py-10 text-center text-slate-400">No committee meetings recorded.</p>}
      </div>

      {creating && <RecordMeetingModal onClose={() => setCreating(false)} />}
    </>
  )
}

function RecordMeetingModal({ onClose }: { onClose: () => void }) {
  const create = useCreateCommitteeMeeting()
  const { data: committees = [] } = useSimpleMaster('quality-committees', endpoints.quality.committees)
  const { data: users } = useUsers({ page: 1, pageSize: 500 })
  const [committeeId, setCommitteeId] = useState('')
  const [meetingDate, setMeetingDate] = useState(new Date().toISOString().slice(0, 10))
  const [minutes, setMinutes] = useState('')
  const [attendeesText, setAttendeesText] = useState('')
  const [actionItems, setActionItems] = useState<SaveCommitteeActionItemPayload[]>([])
  const [error, setError] = useState<string | null>(null)

  const addActionItem = () => setActionItems((items) => [...items, { description: '', ownerUserId: 0, dueDate: meetingDate }])

  const submit = async () => {
    setError(null)
    if (!committeeId || !minutes.trim()) { setError('Committee and minutes are required.'); return }
    try {
      await create.mutateAsync({
        committeeId: Number(committeeId), meetingDate, minutes, attendeesText: attendeesText || null,
        actionItems: actionItems.filter((a) => a.description.trim() && a.ownerUserId),
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal open title="Record committee meeting" onClose={onClose} footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={submit} loading={create.isPending}>Save</Button></>}>
      <div className="max-h-[65vh] space-y-3 overflow-y-auto">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <div className="grid grid-cols-2 gap-2">
          <Select label="Committee" placeholder="Select committee" value={committeeId} onChange={(e) => setCommitteeId(e.target.value)} options={committees.map((c) => ({ value: c.id, label: c.name }))} />
          <Input label="Meeting date" type="date" value={meetingDate} onChange={(e) => setMeetingDate(e.target.value)} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700">Minutes</label>
          <textarea className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" rows={4} value={minutes} onChange={(e) => setMinutes(e.target.value)} />
        </div>
        <Input label="Attendees" value={attendeesText} onChange={(e) => setAttendeesText(e.target.value)} />

        <div className="border-t border-slate-100 pt-3">
          <div className="mb-2 flex items-center justify-between">
            <span className="text-sm font-medium text-slate-700">Action items</span>
            <Button variant="secondary" onClick={addActionItem}>Add item</Button>
          </div>
          {actionItems.map((item, i) => (
            <div key={i} className="mb-2 grid grid-cols-3 gap-2">
              <Input placeholder="Description" value={item.description} onChange={(e) => setActionItems((items) => items.map((it, idx) => idx === i ? { ...it, description: e.target.value } : it))} />
              <Select placeholder="Owner" value={item.ownerUserId || ''} onChange={(e) => setActionItems((items) => items.map((it, idx) => idx === i ? { ...it, ownerUserId: Number(e.target.value) } : it))} options={(users?.items ?? []).map((u) => ({ value: u.id, label: u.fullName }))} />
              <Input type="date" value={item.dueDate} onChange={(e) => setActionItems((items) => items.map((it, idx) => idx === i ? { ...it, dueDate: e.target.value } : it))} />
            </div>
          ))}
        </div>
      </div>
    </Modal>
  )
}
