import { useState, type ReactNode } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import {
  useAddRoomTypeCharge,
  useBeds,
  useBuildings,
  useDeleteBed,
  useDeleteBuilding,
  useDeleteFloor,
  useDeleteRoom,
  useDeleteRoomType,
  useDeleteRoomTypeCharge,
  useDeleteWard,
  useFloors,
  useRooms,
  useRoomTypeCharges,
  useRoomTypes,
  useSaveBed,
  useSaveBuilding,
  useSaveFloor,
  useSaveRoom,
  useSaveRoomType,
  useSaveWard,
  useWards,
} from '../api'
import { Tabs } from '../components/Tabs'

const tabs = [
  { key: 'buildings', label: 'Buildings' },
  { key: 'floors', label: 'Floors' },
  { key: 'room-types', label: 'Room Types' },
  { key: 'wards', label: 'Wards' },
  { key: 'rooms', label: 'Rooms' },
  { key: 'beds', label: 'Beds' },
] as const

export function LocationsPage() {
  const [tab, setTab] = useState<(typeof tabs)[number]['key']>('buildings')

  return (
    <>
      <PageHeader title="Locations & Beds" subtitle="Building, floor, ward, room and bed hierarchy" />
      <Tabs tabs={tabs} active={tab} onChange={setTab} />
      {tab === 'buildings' && <BuildingsTab />}
      {tab === 'floors' && <FloorsTab />}
      {tab === 'room-types' && <RoomTypesTab />}
      {tab === 'wards' && <WardsTab />}
      {tab === 'rooms' && <RoomsTab />}
      {tab === 'beds' && <BedsTab />}
    </>
  )
}

// ── Shared list scaffolding ─────────────────────────────────────────────────

interface LocationTableProps<T extends { id: number; isActive: boolean }> {
  noun: string
  headers: string[]
  rows: T[]
  isLoading: boolean
  renderCells: (row: T) => ReactNode[]
  rowName: (row: T) => string
  canManage: boolean
  onNew: () => void
  onEdit: (row: T) => void
  onDelete: (id: number) => Promise<unknown>
}

function LocationTable<T extends { id: number; isActive: boolean }>({
  noun,
  headers,
  rows,
  isLoading,
  renderCells,
  rowName,
  canManage,
  onNew,
  onEdit,
  onDelete,
}: LocationTableProps<T>) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      {canManage && (
        <div className="flex justify-end border-b border-slate-200 px-4 py-3">
          <Button onClick={onNew}>New {noun}</Button>
        </div>
      )}
      {isLoading && <Spinner />}
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50 text-left">
            {headers.map((header) => (
              <th key={header} className="px-4 py-2.5 font-medium text-slate-600">
                {header}
              </th>
            ))}
            <th className="px-4 py-2.5 font-medium text-slate-600">Status</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.id} className="border-b border-slate-100 last:border-0">
              {renderCells(row).map((cell, index) => (
                <td key={index} className="px-4 py-2.5 text-slate-700">
                  {cell}
                </td>
              ))}
              <td className="px-4 py-2.5">
                <StatusBadge tone={row.isActive ? 'success' : 'neutral'}>
                  {row.isActive ? 'Active' : 'Inactive'}
                </StatusBadge>
              </td>
              <td className="px-4 py-2.5 text-right">
                {canManage && (
                  <div className="flex justify-end gap-1">
                    <Button variant="ghost" onClick={() => onEdit(row)}>
                      Edit
                    </Button>
                    <Button
                      variant="ghost"
                      className="text-red-600"
                      onClick={async () => {
                        if (window.confirm(`Delete ${noun} '${rowName(row)}'?`)) {
                          try {
                            await onDelete(row.id)
                          } catch (err) {
                            window.alert(errorMessage(err))
                          }
                        }
                      }}
                    >
                      Delete
                    </Button>
                  </div>
                )}
              </td>
            </tr>
          ))}
          {!isLoading && rows.length === 0 && (
            <tr>
              <td colSpan={headers.length + 2} className="px-4 py-10 text-center text-slate-400">
                No {noun}s yet.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  )
}

function EditorModal({
  title,
  onClose,
  onSubmit,
  saving,
  error,
  children,
}: {
  title: string
  onClose: () => void
  onSubmit: () => void
  saving: boolean
  error: string | null
  children: ReactNode
}) {
  return (
    <Modal
      open
      title={title}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={onSubmit} loading={saving}>
            Save
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        {children}
      </div>
    </Modal>
  )
}

const useCanManage = () => useAuth().hasPermission('MASTER.LOCATION.MANAGE')

// ── Buildings ───────────────────────────────────────────────────────────────

function BuildingsTab() {
  const canManage = useCanManage()
  const { data: rows = [], isLoading } = useBuildings()
  const save = useSaveBuilding()
  const remove = useDeleteBuilding()
  const [editor, setEditor] = useState<{ id?: number; code: string; name: string; isActive: boolean } | null>(null)
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync(editor)
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <LocationTable
        noun="building"
        headers={['Code', 'Name', 'Floors']}
        rows={rows}
        isLoading={isLoading}
        renderCells={(row) => [
          <span key="code" className="font-mono text-xs text-slate-500">{row.code}</span>,
          <span key="name" className="font-medium">{row.name}</span>,
          row.floorCount,
        ]}
        rowName={(row) => row.name}
        canManage={canManage}
        onNew={() => {
          setError(null)
          setEditor({ code: '', name: '', isActive: true })
        }}
        onEdit={(row) => {
          setError(null)
          setEditor({ id: row.id, code: row.code, name: row.name, isActive: row.isActive })
        }}
        onDelete={(id) => remove.mutateAsync(id)}
      />
      {editor && (
        <EditorModal
          title={editor.id ? 'Edit building' : 'New building'}
          onClose={() => setEditor(null)}
          onSubmit={submit}
          saving={save.isPending}
          error={error}
        >
          <Input label="Code" value={editor.code} onChange={(e) => setEditor({ ...editor, code: e.target.value })} />
          <Input label="Name" value={editor.name} onChange={(e) => setEditor({ ...editor, name: e.target.value })} />
          <ActiveCheckbox checked={editor.isActive} onChange={(isActive) => setEditor({ ...editor, isActive })} />
        </EditorModal>
      )}
    </>
  )
}

// ── Floors ──────────────────────────────────────────────────────────────────

function FloorsTab() {
  const canManage = useCanManage()
  const { data: rows = [], isLoading } = useFloors()
  const { data: buildings = [] } = useBuildings()
  const save = useSaveFloor()
  const remove = useDeleteFloor()
  const [editor, setEditor] = useState<{
    id?: number
    code: string
    name: string
    buildingId: string
    isActive: boolean
  } | null>(null)
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({ ...editor, buildingId: Number(editor.buildingId) })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <LocationTable
        noun="floor"
        headers={['Code', 'Name', 'Building', 'Wards']}
        rows={rows}
        isLoading={isLoading}
        renderCells={(row) => [
          <span key="code" className="font-mono text-xs text-slate-500">{row.code}</span>,
          <span key="name" className="font-medium">{row.name}</span>,
          row.buildingName,
          row.wardCount,
        ]}
        rowName={(row) => row.name}
        canManage={canManage}
        onNew={() => {
          setError(null)
          setEditor({ code: '', name: '', buildingId: '', isActive: true })
        }}
        onEdit={(row) => {
          setError(null)
          setEditor({ id: row.id, code: row.code, name: row.name, buildingId: String(row.buildingId), isActive: row.isActive })
        }}
        onDelete={(id) => remove.mutateAsync(id)}
      />
      {editor && (
        <EditorModal
          title={editor.id ? 'Edit floor' : 'New floor'}
          onClose={() => setEditor(null)}
          onSubmit={submit}
          saving={save.isPending}
          error={error}
        >
          <Input label="Code" value={editor.code} onChange={(e) => setEditor({ ...editor, code: e.target.value })} />
          <Input label="Name" value={editor.name} onChange={(e) => setEditor({ ...editor, name: e.target.value })} />
          <Select
            label="Building"
            placeholder="Select building…"
            options={buildings.map((b) => ({ value: b.id, label: b.name }))}
            value={editor.buildingId}
            onChange={(e) => setEditor({ ...editor, buildingId: e.target.value })}
          />
          <ActiveCheckbox checked={editor.isActive} onChange={(isActive) => setEditor({ ...editor, isActive })} />
        </EditorModal>
      )}
    </>
  )
}

// ── Room types ──────────────────────────────────────────────────────────────

function RoomTypesTab() {
  const canManage = useCanManage()
  const { data: rows = [], isLoading } = useRoomTypes()
  const save = useSaveRoomType()
  const remove = useDeleteRoomType()
  const [editor, setEditor] = useState<{
    id?: number
    code: string
    name: string
    bedChargePerDay: string
    isActive: boolean
  } | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [chargesFor, setChargesFor] = useState<{ id: number; name: string } | null>(null)

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({ ...editor, bedChargePerDay: Number(editor.bedChargePerDay) })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <LocationTable
        noun="room type"
        headers={['Code', 'Name', 'Bed charge / day', 'Rooms', 'Rate history']}
        rows={rows}
        isLoading={isLoading}
        renderCells={(row) => [
          <span key="code" className="font-mono text-xs text-slate-500">{row.code}</span>,
          <span key="name" className="font-medium">{row.name}</span>,
          row.bedChargePerDay.toLocaleString(),
          row.roomCount,
          <Button key="charges" variant="ghost" className="px-2 py-1 text-xs" onClick={() => setChargesFor({ id: row.id, name: row.name })}>
            View / add rates
          </Button>,
        ]}
        rowName={(row) => row.name}
        canManage={canManage}
        onNew={() => {
          setError(null)
          setEditor({ code: '', name: '', bedChargePerDay: '0', isActive: true })
        }}
        onEdit={(row) => {
          setError(null)
          setEditor({
            id: row.id,
            code: row.code,
            name: row.name,
            bedChargePerDay: String(row.bedChargePerDay),
            isActive: row.isActive,
          })
        }}
        onDelete={(id) => remove.mutateAsync(id)}
      />
      {editor && (
        <EditorModal
          title={editor.id ? 'Edit room type' : 'New room type'}
          onClose={() => setEditor(null)}
          onSubmit={submit}
          saving={save.isPending}
          error={error}
        >
          <Input label="Code" value={editor.code} onChange={(e) => setEditor({ ...editor, code: e.target.value })} />
          <Input label="Name" value={editor.name} onChange={(e) => setEditor({ ...editor, name: e.target.value })} />
          <Input
            label="Bed charge per day"
            type="number"
            min={0}
            step="0.01"
            value={editor.bedChargePerDay}
            onChange={(e) => setEditor({ ...editor, bedChargePerDay: e.target.value })}
          />
          <ActiveCheckbox checked={editor.isActive} onChange={(isActive) => setEditor({ ...editor, isActive })} />
        </EditorModal>
      )}
      {chargesFor && (
        <RoomTypeChargesModal roomTypeId={chargesFor.id} roomTypeName={chargesFor.name} canManage={canManage} onClose={() => setChargesFor(null)} />
      )}
    </>
  )
}

/**
 * Date-effective bed-charge history for one room type — add/delete only, no edit, same "a rate
 * revision is a new row" philosophy as Tariff. The room type's own BedChargePerDay stays the
 * fallback used whenever no row here is effective as of the allocation date.
 */
function RoomTypeChargesModal({
  roomTypeId, roomTypeName, canManage, onClose,
}: {
  roomTypeId: number
  roomTypeName: string
  canManage: boolean
  onClose: () => void
}) {
  const { data: charges = [], isLoading } = useRoomTypeCharges(roomTypeId)
  const add = useAddRoomTypeCharge(roomTypeId)
  const remove = useDeleteRoomTypeCharge(roomTypeId)
  const [rate, setRate] = useState('')
  const [effectiveFrom, setEffectiveFrom] = useState('')
  const [effectiveTo, setEffectiveTo] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await add.mutateAsync({ rate: Number(rate), effectiveFrom, effectiveTo: effectiveTo || null })
      setRate('')
      setEffectiveFrom('')
      setEffectiveTo('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal open title={`Rate history — ${roomTypeName}`} onClose={onClose} footer={<Button onClick={onClose}>Close</Button>}>
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        {isLoading && <Spinner />}
        {!isLoading && (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-left">
                <th className="py-1.5 pr-2 font-medium text-slate-600">Rate</th>
                <th className="py-1.5 pr-2 font-medium text-slate-600">Effective from</th>
                <th className="py-1.5 pr-2 font-medium text-slate-600">Effective to</th>
                {canManage && <th />}
              </tr>
            </thead>
            <tbody>
              {charges.map((c) => (
                <tr key={c.id} className="border-b border-slate-100 last:border-0">
                  <td className="py-1.5 pr-2">{c.rate.toLocaleString()}</td>
                  <td className="py-1.5 pr-2">{c.effectiveFrom}</td>
                  <td className="py-1.5 pr-2">{c.effectiveTo ?? 'onwards'}</td>
                  {canManage && (
                    <td className="py-1.5 text-right">
                      <Button
                        variant="ghost"
                        className="px-2 py-1 text-xs text-red-600"
                        onClick={async () => {
                          if (window.confirm('Delete this rate history entry?')) {
                            try {
                              await remove.mutateAsync(c.id)
                            } catch (err) {
                              window.alert(errorMessage(err))
                            }
                          }
                        }}
                      >
                        Delete
                      </Button>
                    </td>
                  )}
                </tr>
              ))}
              {charges.length === 0 && (
                <tr>
                  <td colSpan={canManage ? 4 : 3} className="py-6 text-center text-slate-400">
                    No dated rate revisions — the flat bed charge per day above always applies.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        )}
        {canManage && (
          <div className="flex items-end gap-2 border-t border-slate-200 pt-3">
            <Input label="Rate" type="number" min={0} step="0.01" className="w-28" value={rate} onChange={(e) => setRate(e.target.value)} />
            <Input label="Effective from" type="date" value={effectiveFrom} onChange={(e) => setEffectiveFrom(e.target.value)} />
            <Input label="Effective to (optional)" type="date" value={effectiveTo} onChange={(e) => setEffectiveTo(e.target.value)} />
            <Button onClick={submit} loading={add.isPending} disabled={!rate || !effectiveFrom}>
              Add
            </Button>
          </div>
        )}
      </div>
    </Modal>
  )
}

// ── Wards ───────────────────────────────────────────────────────────────────

function WardsTab() {
  const canManage = useCanManage()
  const { data: rows = [], isLoading } = useWards()
  const { data: floors = [] } = useFloors()
  const save = useSaveWard()
  const remove = useDeleteWard()
  const [editor, setEditor] = useState<{
    id?: number
    code: string
    name: string
    floorId: string
    isIcu: boolean
    isObservation: boolean
    isActive: boolean
  } | null>(null)
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({ ...editor, floorId: Number(editor.floorId) })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <LocationTable
        noun="ward"
        headers={['Code', 'Name', 'Floor', 'Type', 'Rooms']}
        rows={rows}
        isLoading={isLoading}
        renderCells={(row) => [
          <span key="code" className="font-mono text-xs text-slate-500">{row.code}</span>,
          <span key="name" className="font-medium">{row.name}</span>,
          row.floorName,
          <span key="type" className="flex gap-1">
            {row.isIcu && <StatusBadge tone="warning">ICU</StatusBadge>}
            {row.isObservation && <StatusBadge tone="info">Observation</StatusBadge>}
            {!row.isIcu && !row.isObservation && '—'}
          </span>,
          row.roomCount,
        ]}
        rowName={(row) => row.name}
        canManage={canManage}
        onNew={() => {
          setError(null)
          setEditor({ code: '', name: '', floorId: '', isIcu: false, isObservation: false, isActive: true })
        }}
        onEdit={(row) => {
          setError(null)
          setEditor({
            id: row.id,
            code: row.code,
            name: row.name,
            floorId: String(row.floorId),
            isIcu: row.isIcu,
            isObservation: row.isObservation,
            isActive: row.isActive,
          })
        }}
        onDelete={(id) => remove.mutateAsync(id)}
      />
      {editor && (
        <EditorModal
          title={editor.id ? 'Edit ward' : 'New ward'}
          onClose={() => setEditor(null)}
          onSubmit={submit}
          saving={save.isPending}
          error={error}
        >
          <Input label="Code" value={editor.code} onChange={(e) => setEditor({ ...editor, code: e.target.value })} />
          <Input label="Name" value={editor.name} onChange={(e) => setEditor({ ...editor, name: e.target.value })} />
          <Select
            label="Floor"
            placeholder="Select floor…"
            options={floors.map((f) => ({ value: f.id, label: `${f.name} (${f.buildingName})` }))}
            value={editor.floorId}
            onChange={(e) => setEditor({ ...editor, floorId: e.target.value })}
          />
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input
              type="checkbox"
              checked={editor.isIcu}
              onChange={(e) => setEditor({ ...editor, isIcu: e.target.checked })}
            />
            ICU ward
          </label>
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input
              type="checkbox"
              checked={editor.isObservation}
              onChange={(e) => setEditor({ ...editor, isObservation: e.target.checked })}
            />
            Observation ward (ER short-stay beds)
          </label>
          <ActiveCheckbox checked={editor.isActive} onChange={(isActive) => setEditor({ ...editor, isActive })} />
        </EditorModal>
      )}
    </>
  )
}

// ── Rooms ───────────────────────────────────────────────────────────────────

function RoomsTab() {
  const canManage = useCanManage()
  const { data: rows = [], isLoading } = useRooms()
  const { data: wards = [] } = useWards()
  const { data: roomTypes = [] } = useRoomTypes()
  const save = useSaveRoom()
  const remove = useDeleteRoom()
  const [editor, setEditor] = useState<{
    id?: number
    code: string
    name: string
    wardId: string
    roomTypeId: string
    isActive: boolean
  } | null>(null)
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({ ...editor, wardId: Number(editor.wardId), roomTypeId: Number(editor.roomTypeId) })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <LocationTable
        noun="room"
        headers={['Code', 'Name', 'Ward', 'Room type', 'Beds']}
        rows={rows}
        isLoading={isLoading}
        renderCells={(row) => [
          <span key="code" className="font-mono text-xs text-slate-500">{row.code}</span>,
          <span key="name" className="font-medium">{row.name}</span>,
          row.wardName,
          row.roomTypeName,
          row.bedCount,
        ]}
        rowName={(row) => row.name}
        canManage={canManage}
        onNew={() => {
          setError(null)
          setEditor({ code: '', name: '', wardId: '', roomTypeId: '', isActive: true })
        }}
        onEdit={(row) => {
          setError(null)
          setEditor({
            id: row.id,
            code: row.code,
            name: row.name,
            wardId: String(row.wardId),
            roomTypeId: String(row.roomTypeId),
            isActive: row.isActive,
          })
        }}
        onDelete={(id) => remove.mutateAsync(id)}
      />
      {editor && (
        <EditorModal
          title={editor.id ? 'Edit room' : 'New room'}
          onClose={() => setEditor(null)}
          onSubmit={submit}
          saving={save.isPending}
          error={error}
        >
          <Input label="Code" value={editor.code} onChange={(e) => setEditor({ ...editor, code: e.target.value })} />
          <Input label="Name" value={editor.name} onChange={(e) => setEditor({ ...editor, name: e.target.value })} />
          <Select
            label="Ward"
            placeholder="Select ward…"
            options={wards.map((w) => ({ value: w.id, label: w.name }))}
            value={editor.wardId}
            onChange={(e) => setEditor({ ...editor, wardId: e.target.value })}
          />
          <Select
            label="Room type"
            placeholder="Select room type…"
            options={roomTypes.map((rt) => ({ value: rt.id, label: rt.name }))}
            value={editor.roomTypeId}
            onChange={(e) => setEditor({ ...editor, roomTypeId: e.target.value })}
          />
          <ActiveCheckbox checked={editor.isActive} onChange={(isActive) => setEditor({ ...editor, isActive })} />
        </EditorModal>
      )}
    </>
  )
}

// ── Beds ────────────────────────────────────────────────────────────────────

function BedsTab() {
  const canManage = useCanManage()
  const { data: rows = [], isLoading } = useBeds()
  const { data: rooms = [] } = useRooms()
  const save = useSaveBed()
  const remove = useDeleteBed()
  const [editor, setEditor] = useState<{ id?: number; code: string; roomId: string; isActive: boolean } | null>(null)
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({ ...editor, roomId: Number(editor.roomId) })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <LocationTable
        noun="bed"
        headers={['Code', 'Room', 'Ward']}
        rows={rows}
        isLoading={isLoading}
        renderCells={(row) => [
          <span key="code" className="font-mono text-xs font-medium text-slate-700">{row.code}</span>,
          row.roomName,
          row.wardName,
        ]}
        rowName={(row) => row.code}
        canManage={canManage}
        onNew={() => {
          setError(null)
          setEditor({ code: '', roomId: '', isActive: true })
        }}
        onEdit={(row) => {
          setError(null)
          setEditor({ id: row.id, code: row.code, roomId: String(row.roomId), isActive: row.isActive })
        }}
        onDelete={(id) => remove.mutateAsync(id)}
      />
      {editor && (
        <EditorModal
          title={editor.id ? 'Edit bed' : 'New bed'}
          onClose={() => setEditor(null)}
          onSubmit={submit}
          saving={save.isPending}
          error={error}
        >
          <Input label="Code" value={editor.code} onChange={(e) => setEditor({ ...editor, code: e.target.value })} />
          <Select
            label="Room"
            placeholder="Select room…"
            options={rooms.map((r) => ({ value: r.id, label: `${r.name} (${r.wardName})` }))}
            value={editor.roomId}
            onChange={(e) => setEditor({ ...editor, roomId: e.target.value })}
          />
          <ActiveCheckbox checked={editor.isActive} onChange={(isActive) => setEditor({ ...editor, isActive })} />
        </EditorModal>
      )}
    </>
  )
}

function ActiveCheckbox({ checked, onChange }: { checked: boolean; onChange: (checked: boolean) => void }) {
  return (
    <label className="flex items-center gap-2 text-sm text-slate-700">
      <input type="checkbox" checked={checked} onChange={(e) => onChange(e.target.checked)} />
      Active
    </label>
  )
}
