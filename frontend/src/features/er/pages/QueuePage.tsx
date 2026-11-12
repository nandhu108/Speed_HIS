import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { PatientPicker } from '../../appointments/components/PatientPicker'
import { useAuth } from '../../auth/AuthContext'
import type { PatientListItem } from '../../patients/types'
import { useEmergencyQueue, useRegisterVisit } from '../api'
import {
  ARRIVAL_MODE_LABELS,
  EMERGENCY_STATUS_LABELS,
  ESI_LEVEL_LABELS,
  type ArrivalMode,
  type EmergencyStatus,
  type EmergencyVisitListItem,
  type EsiLevel,
} from '../types'

const statusTone: Record<EmergencyStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info', 2: 'warning', 3: 'warning', 4: 'success', 5: 'neutral',
}

const esiTone: Record<EsiLevel, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'danger', 2: 'danger', 3: 'warning', 4: 'info', 5: 'neutral',
}

export function QueuePage() {
  const { hasPermission } = useAuth()
  const navigate = useNavigate()
  const canCreate = hasPermission('EMERGENCY.VISIT.CREATE')
  const [creating, setCreating] = useState(false)

  const { data: visits = [], isLoading } = useEmergencyQueue({})

  return (
    <>
      <PageHeader
        title="ER Queue"
        subtitle="Active emergency visits, sorted by triage priority then arrival time"
        actions={canCreate && <Button onClick={() => setCreating(true)}>Register ER visit</Button>}
      />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Token</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Visit No</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Patient</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Arrival</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">ESI</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Doctor</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">MLC</th>
            </tr>
          </thead>
          <tbody>
            {visits.map((v: EmergencyVisitListItem) => (
              <tr
                key={v.id}
                className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50"
                onClick={() => navigate(`/emergency/visits/${v.id}`)}
              >
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">#{v.tokenNumber}</td>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{v.visitNumber}</td>
                <td className="px-4 py-2.5 text-slate-700">{v.patientName}</td>
                <td className="px-4 py-2.5 text-slate-700">
                  {new Date(v.arrivedAt).toLocaleTimeString()} · {ARRIVAL_MODE_LABELS[v.arrivalMode]}
                </td>
                <td className="px-4 py-2.5">
                  {v.esiLevel ? (
                    <StatusBadge tone={esiTone[v.esiLevel]}>{ESI_LEVEL_LABELS[v.esiLevel]}</StatusBadge>
                  ) : (
                    <StatusBadge tone="neutral">Untriaged</StatusBadge>
                  )}
                </td>
                <td className="px-4 py-2.5 text-slate-700">{v.assignedDoctorName ?? '—'}</td>
                <td className="px-4 py-2.5">
                  <StatusBadge tone={statusTone[v.status]}>{EMERGENCY_STATUS_LABELS[v.status]}</StatusBadge>
                </td>
                <td className="px-4 py-2.5">{v.isMlc ? <StatusBadge tone="danger">MLC</StatusBadge> : '—'}</td>
              </tr>
            ))}
            {!isLoading && visits.length === 0 && (
              <tr>
                <td colSpan={8} className="px-4 py-10 text-center text-slate-400">
                  No active ER visits.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <RegisterModal onClose={() => setCreating(false)} />}
    </>
  )
}

function RegisterModal({ onClose }: { onClose: () => void }) {
  const navigate = useNavigate()
  const register = useRegisterVisit()
  const [patient, setPatient] = useState<PatientListItem | null>(null)
  const [arrivalMode, setArrivalMode] = useState<ArrivalMode>(1)
  const [isMlc, setIsMlc] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    if (!patient) return
    setError(null)
    try {
      const id = await register.mutateAsync({ patientId: patient.id, arrivalMode, isMlc })
      onClose()
      navigate(`/emergency/visits/${id}`)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Register ER visit"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={register.isPending} disabled={!patient}>Register</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <PatientPicker selected={patient} onSelect={setPatient} />
        <Select
          label="Arrival mode"
          value={String(arrivalMode)}
          onChange={(e) => setArrivalMode(Number(e.target.value) as ArrivalMode)}
          options={Object.entries(ARRIVAL_MODE_LABELS).map(([value, label]) => ({ value, label }))}
        />
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" checked={isMlc} onChange={(e) => setIsMlc(e.target.checked)} />
          Medico-legal case (MLC)
        </label>
      </div>
    </Modal>
  )
}
