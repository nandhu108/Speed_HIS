import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useCreateFileRequest, useFileMovements, useFileRequestsForFile, useRecordFile, useRecordMovement } from '../api'
import {
  FILE_REQUEST_STATUS_LABELS,
  FILE_REQUEST_TYPE_LABELS,
  RECORD_FILE_STATUS_LABELS,
  type FileRequestType,
} from '../types'

export function RecordFileDetailPage() {
  const { id } = useParams<{ id: string }>()
  const fileId = Number(id)
  const { hasPermission } = useAuth()
  const { data: file, isLoading } = useRecordFile(fileId)
  const { data: movements = [] } = useFileMovements(fileId)
  const { data: requests = [] } = useFileRequestsForFile(fileId)

  const [moving, setMoving] = useState(false)
  const [requesting, setRequesting] = useState(false)

  if (isLoading || !file) return <Spinner />

  return (
    <>
      <PageHeader
        title={file.fileNumber}
        subtitle={`${file.patientName} (${file.patientUhid}) — Admission ${file.admissionNumber}`}
        backTo="/mrd/files"
        actions={
          <div className="flex gap-2">
            {hasPermission('MRD.FILE.MANAGE') && (
              <Button variant="secondary" onClick={() => setMoving(true)}>
                Record movement
              </Button>
            )}
            {hasPermission('MRD.REQUEST.CREATE') && <Button onClick={() => setRequesting(true)}>Raise request</Button>}
          </div>
        }
      />

      <div className="mb-4 grid grid-cols-1 gap-4 rounded-lg border border-slate-200 bg-white p-4 sm:grid-cols-4">
        <Field label="Status">
          <StatusBadge tone={file.status === 1 ? 'success' : 'neutral'}>{RECORD_FILE_STATUS_LABELS[file.status]}</StatusBadge>
        </Field>
        <Field label="Current location">{file.currentLocation}</Field>
        <Field label="Custodian">{file.currentCustodianName ?? '—'}</Field>
        <Field label="Created by">
          {file.createdByName} · {new Date(file.createdAt).toLocaleDateString()}
        </Field>
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <div className="rounded-lg border border-slate-200 bg-white p-4">
          <h3 className="mb-2 text-sm font-semibold text-slate-700">Movement history</h3>
          <table className="w-full text-xs">
            <tbody>
              {movements.map((m) => (
                <tr key={m.id} className="border-t border-slate-100">
                  <td className="py-1.5 text-slate-500">{new Date(m.movedAt).toLocaleString()}</td>
                  <td className="py-1.5">
                    {m.fromLocation} → {m.toLocation}
                  </td>
                  <td className="py-1.5 text-slate-500">{m.toCustodianName ?? ''}</td>
                </tr>
              ))}
              {movements.length === 0 && (
                <tr>
                  <td className="py-4 text-center text-slate-400">No movements recorded.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        <div className="rounded-lg border border-slate-200 bg-white p-4">
          <h3 className="mb-2 text-sm font-semibold text-slate-700">File requests</h3>
          <table className="w-full text-xs">
            <tbody>
              {requests.map((r) => (
                <tr key={r.id} className="border-t border-slate-100">
                  <td className="py-1.5 text-slate-500">{new Date(r.requestedAt).toLocaleDateString()}</td>
                  <td className="py-1.5">{FILE_REQUEST_TYPE_LABELS[r.requestType]}</td>
                  <td className="py-1.5">{r.purpose}</td>
                  <td className="py-1.5">
                    <StatusBadge tone={r.isOverdue ? 'danger' : 'neutral'}>{FILE_REQUEST_STATUS_LABELS[r.status]}</StatusBadge>
                  </td>
                </tr>
              ))}
              {requests.length === 0 && (
                <tr>
                  <td className="py-4 text-center text-slate-400">No requests raised.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {moving && <RecordMovementModal fileId={fileId} onClose={() => setMoving(false)} />}
      {requesting && <RaiseRequestModal fileId={fileId} onClose={() => setRequesting(false)} />}
    </>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <div className="text-xs font-medium uppercase tracking-wide text-slate-400">{label}</div>
      <div className="mt-0.5 text-sm text-slate-700">{children}</div>
    </div>
  )
}

function RecordMovementModal({ fileId, onClose }: { fileId: number; onClose: () => void }) {
  const move = useRecordMovement(fileId)
  const [toLocation, setToLocation] = useState('')
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await move.mutateAsync({ toLocation, toCustodianUserId: null, reason: reason || null })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Record movement"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={move.isPending} disabled={!toLocation}>Record</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Input label="New location" value={toLocation} onChange={(e) => setToLocation(e.target.value)} placeholder="e.g. Ward 3 nursing station" />
        <Input label="Reason (optional)" value={reason} onChange={(e) => setReason(e.target.value)} />
      </div>
    </Modal>
  )
}

function RaiseRequestModal({ fileId, onClose }: { fileId: number; onClose: () => void }) {
  const create = useCreateFileRequest()
  const [requestType, setRequestType] = useState<FileRequestType>(1)
  const [requesterName, setRequesterName] = useState('')
  const [requesterOrganization, setRequesterOrganization] = useState('')
  const [purpose, setPurpose] = useState('')
  const [expectedReturnDate, setExpectedReturnDate] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await create.mutateAsync({
        recordFileId: fileId, requestType, requesterName: requesterName || null,
        requesterOrganization: requesterOrganization || null, purpose, expectedReturnDate: expectedReturnDate || null,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Raise a file request"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={create.isPending} disabled={!purpose}>Raise request</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Select
          label="Request type"
          value={String(requestType)}
          onChange={(e) => setRequestType(Number(e.target.value) as FileRequestType)}
          options={[{ value: 1, label: 'Internal' }, { value: 2, label: 'External' }, { value: 3, label: 'Legal' }]}
        />
        {requestType !== 1 && (
          <>
            <Input label="Requester name" value={requesterName} onChange={(e) => setRequesterName(e.target.value)} />
            <Input label="Organization" value={requesterOrganization} onChange={(e) => setRequesterOrganization(e.target.value)} />
          </>
        )}
        <Input label="Purpose" value={purpose} onChange={(e) => setPurpose(e.target.value)} />
        <Input label="Expected return date" type="date" value={expectedReturnDate} onChange={(e) => setExpectedReturnDate(e.target.value)} />
      </div>
    </Modal>
  )
}
