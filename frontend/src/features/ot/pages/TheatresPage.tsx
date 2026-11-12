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
import { useEquipment, useSaveEquipment, useSaveTheatre, useTheatres } from '../api'
import type { Equipment, Theatre } from '../types'

type Tab = 'theatres' | 'equipment'

export function TheatresPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('OT.THEATRE.MANAGE')
  const [tab, setTab] = useState<Tab>('theatres')
  const { data: theatres = [], isLoading: loadingTheatres } = useTheatres()
  const { data: equipment = [], isLoading: loadingEquipment } = useEquipment()
  const [editingTheatre, setEditingTheatre] = useState<Theatre | null>(null)
  const [creatingTheatre, setCreatingTheatre] = useState(false)
  const [editingEquipment, setEditingEquipment] = useState<Equipment | null>(null)
  const [creatingEquipment, setCreatingEquipment] = useState(false)

  return (
    <>
      <PageHeader
        title="Theatres & Equipment"
        subtitle="Operation theatres and the equipment they hold"
        actions={
          canManage &&
          (tab === 'theatres' ? (
            <Button onClick={() => setCreatingTheatre(true)}>New theatre</Button>
          ) : (
            <Button onClick={() => setCreatingEquipment(true)}>New equipment</Button>
          ))
        }
      />

      <div className="mb-4 flex gap-1 border-b border-slate-200">
        <button
          type="button"
          onClick={() => setTab('theatres')}
          className={`border-b-2 px-4 py-2 text-sm font-medium ${tab === 'theatres' ? 'border-primary-600 text-primary-700' : 'border-transparent text-slate-500 hover:text-slate-700'}`}
        >
          Theatres
        </button>
        <button
          type="button"
          onClick={() => setTab('equipment')}
          className={`border-b-2 px-4 py-2 text-sm font-medium ${tab === 'equipment' ? 'border-primary-600 text-primary-700' : 'border-transparent text-slate-500 hover:text-slate-700'}`}
        >
          Equipment
        </button>
      </div>

      {tab === 'theatres' && (
        <div className="rounded-lg border border-slate-200 bg-white">
          {loadingTheatres && <Spinner />}
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50 text-left">
                <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
                <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
                <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {theatres.map((t) => (
                <tr key={t.id} className="border-b border-slate-100 last:border-0">
                  <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{t.code}</td>
                  <td className="px-4 py-2.5 font-medium text-slate-700">{t.name}</td>
                  <td className="px-4 py-2.5">
                    <StatusBadge tone={t.isActive ? 'success' : 'neutral'}>{t.isActive ? 'Active' : 'Inactive'}</StatusBadge>
                  </td>
                  <td className="px-4 py-2.5 text-right">
                    {canManage && (
                      <Button variant="ghost" onClick={() => setEditingTheatre(t)}>
                        Edit
                      </Button>
                    )}
                  </td>
                </tr>
              ))}
              {!loadingTheatres && theatres.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-10 text-center text-slate-400">
                    No theatres yet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {tab === 'equipment' && (
        <div className="rounded-lg border border-slate-200 bg-white">
          {loadingEquipment && <Spinner />}
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50 text-left">
                <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
                <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
                <th className="px-4 py-2.5 font-medium text-slate-600">Theatre</th>
                <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {equipment.map((e) => (
                <tr key={e.id} className="border-b border-slate-100 last:border-0">
                  <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{e.code}</td>
                  <td className="px-4 py-2.5 font-medium text-slate-700">{e.name}</td>
                  <td className="px-4 py-2.5 text-slate-700">{e.theatreName ?? '—'}</td>
                  <td className="px-4 py-2.5">
                    <StatusBadge tone={e.isActive ? 'success' : 'neutral'}>{e.isActive ? 'Active' : 'Inactive'}</StatusBadge>
                  </td>
                  <td className="px-4 py-2.5 text-right">
                    {canManage && (
                      <Button variant="ghost" onClick={() => setEditingEquipment(e)}>
                        Edit
                      </Button>
                    )}
                  </td>
                </tr>
              ))}
              {!loadingEquipment && equipment.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                    No equipment yet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {creatingTheatre && <TheatreEditor theatre={null} onClose={() => setCreatingTheatre(false)} />}
      {editingTheatre && <TheatreEditor theatre={editingTheatre} onClose={() => setEditingTheatre(null)} />}
      {creatingEquipment && <EquipmentEditor equipment={null} onClose={() => setCreatingEquipment(false)} />}
      {editingEquipment && <EquipmentEditor equipment={editingEquipment} onClose={() => setEditingEquipment(null)} />}
    </>
  )
}

function TheatreEditor({ theatre, onClose }: { theatre: Theatre | null; onClose: () => void }) {
  const save = useSaveTheatre()
  const [code, setCode] = useState(theatre?.code ?? '')
  const [name, setName] = useState(theatre?.name ?? '')
  const [isActive, setIsActive] = useState(theatre?.isActive ?? true)
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await save.mutateAsync({ id: theatre?.id, code, name, isActive })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={theatre ? 'Edit theatre' : 'New theatre'}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={save.isPending} disabled={!code || !name}>Save</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Input label="Code" value={code} onChange={(e) => setCode(e.target.value)} />
        <Input label="Name" value={name} onChange={(e) => setName(e.target.value)} />
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
          Active
        </label>
      </div>
    </Modal>
  )
}

function EquipmentEditor({ equipment, onClose }: { equipment: Equipment | null; onClose: () => void }) {
  const save = useSaveEquipment()
  const { data: theatres = [] } = useTheatres()
  const [code, setCode] = useState(equipment?.code ?? '')
  const [name, setName] = useState(equipment?.name ?? '')
  const [theatreId, setTheatreId] = useState(equipment?.theatreId ? String(equipment.theatreId) : '')
  const [isActive, setIsActive] = useState(equipment?.isActive ?? true)
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await save.mutateAsync({ id: equipment?.id, code, name, theatreId: theatreId ? Number(theatreId) : null, isActive })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={equipment ? 'Edit equipment' : 'New equipment'}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={save.isPending} disabled={!code || !name}>Save</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Input label="Code" value={code} onChange={(e) => setCode(e.target.value)} />
        <Input label="Name" value={name} onChange={(e) => setName(e.target.value)} />
        <Select
          label="Theatre"
          placeholder="Not assigned to a theatre"
          value={theatreId}
          onChange={(e) => setTheatreId(e.target.value)}
          options={theatres.map((t) => ({ value: t.id, label: t.name }))}
        />
        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
          Active
        </label>
      </div>
    </Modal>
  )
}
