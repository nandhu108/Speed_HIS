import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useEquipmentList, useSaveEquipment } from '../api'
import { CRITICALITY_LABELS, EQUIPMENT_STATUS_LABELS, type EquipmentCriticality } from '../types'

const criticalityTone: Record<EquipmentCriticality, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'danger', 2: 'warning', 3: 'info', 4: 'neutral',
}

export function EquipmentListPage() {
  const navigate = useNavigate()
  const { hasPermission } = useAuth()
  const canManage = hasPermission('BIOMED.EQUIPMENT.MANAGE')
  const [search, setSearch] = useState('')
  const { data: equipment = [], isLoading } = useEquipmentList({ search: search || undefined })
  const [creating, setCreating] = useState(false)

  return (
    <>
      <PageHeader
        title="Equipment Registry"
        subtitle="Serial, model, criticality, department and current location"
        actions={canManage && <Button onClick={() => setCreating(true)}>New equipment</Button>}
      />

      <Input placeholder="Search by code, name or serial number" value={search} onChange={(e) => setSearch(e.target.value)} className="mb-3 max-w-sm" />

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Department</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Location</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Criticality</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            </tr>
          </thead>
          <tbody>
            {equipment.map((e) => (
              <tr key={e.id} className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50" onClick={() => navigate(`/biomed/equipment/${e.id}`)}>
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{e.equipmentCode}</td>
                <td className="px-4 py-2.5 text-slate-700">{e.name}</td>
                <td className="px-4 py-2.5 text-slate-700">{e.departmentName ?? '—'}</td>
                <td className="px-4 py-2.5 text-slate-700">{e.currentLocation}</td>
                <td className="px-4 py-2.5"><StatusBadge tone={criticalityTone[e.criticality]}>{CRITICALITY_LABELS[e.criticality]}</StatusBadge></td>
                <td className="px-4 py-2.5"><StatusBadge tone={e.status === 1 ? 'success' : e.status === 2 ? 'warning' : 'neutral'}>{EQUIPMENT_STATUS_LABELS[e.status]}</StatusBadge></td>
              </tr>
            ))}
            {!isLoading && equipment.length === 0 && (
              <tr><td colSpan={6} className="px-4 py-10 text-center text-slate-400">No equipment registered yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {creating && <EquipmentModal onClose={() => setCreating(false)} />}
    </>
  )
}

function EquipmentModal({ onClose }: { onClose: () => void }) {
  const navigate = useNavigate()
  const save = useSaveEquipment()
  const [code, setCode] = useState('')
  const [name, setName] = useState('')
  const [serialNumber, setSerialNumber] = useState('')
  const [model, setModel] = useState('')
  const [criticality, setCriticality] = useState<EquipmentCriticality>(3)
  const [location, setLocation] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      const id = await save.mutateAsync({
        equipmentCode: code, name, serialNumber: serialNumber || null, model: model || null, manufacturer: null,
        departmentId: null, vendorId: null, criticality, currentLocation: location,
        purchaseDate: null, purchaseCost: null, warrantyExpiryDate: null, notes: null,
      })
      onClose()
      navigate(`/biomed/equipment/${id}`)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="New equipment"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={save.isPending} disabled={!code || !name || !location}>Save</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Input label="Equipment code" value={code} onChange={(e) => setCode(e.target.value)} />
        <Input label="Name" value={name} onChange={(e) => setName(e.target.value)} />
        <Input label="Serial number" value={serialNumber} onChange={(e) => setSerialNumber(e.target.value)} />
        <Input label="Model" value={model} onChange={(e) => setModel(e.target.value)} />
        <Select
          label="Criticality"
          value={String(criticality)}
          onChange={(e) => setCriticality(Number(e.target.value) as EquipmentCriticality)}
          options={[{ value: 1, label: 'Critical' }, { value: 2, label: 'High' }, { value: 3, label: 'Medium' }, { value: 4, label: 'Low' }]}
        />
        <Input label="Current location" value={location} onChange={(e) => setLocation(e.target.value)} placeholder="e.g. ICU Room 3" />
      </div>
    </Modal>
  )
}
