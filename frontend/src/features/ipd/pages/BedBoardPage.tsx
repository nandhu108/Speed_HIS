import { Bed, Ban, Sparkles, User, Clock3, type LucideIcon } from 'lucide-react'
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
import { useBedBoard, useBedOccupancy, useSetBedStatus } from '../api'
import { BED_STATUS_LABELS, type BedBoardItem, type BedStatus } from '../types'

const statusIcon: Record<BedStatus, LucideIcon> = {
  1: Bed, 2: User, 3: Clock3, 4: Sparkles, 5: Ban,
}

const statusTileClass: Record<BedStatus, string> = {
  1: 'bg-ok/10 border-ok/30 text-ok',
  2: 'bg-crit/10 border-crit/30 text-crit',
  3: 'bg-info/10 border-info/30 text-info',
  4: 'bg-warn/10 border-warn/30 text-warn',
  5: 'bg-panel-2 border-border-soft text-ink-faint',
}

const statusDotClass: Record<BedStatus, string> = {
  1: 'bg-ok', 2: 'bg-crit', 3: 'bg-info', 4: 'bg-warn', 5: 'bg-ink-faint',
}

export function BedBoardPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('IPD.BED.MANAGE')
  const { data: beds = [], isLoading } = useBedBoard()
  const { data: occupancy } = useBedOccupancy()
  const [editing, setEditing] = useState<BedBoardItem | null>(null)

  const byWard = groupBy(beds, (b) => b.wardName)

  return (
    <>
      <PageHeader title="Live Ward Map" subtitle="Bed status across every ward — occupancy is set automatically by admissions" />

      {occupancy && (
        <div className="mb-6 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
          <StatTile label="Total" value={occupancy.totalBeds} />
          <StatTile label="Occupied" value={occupancy.occupied} tone="danger" />
          <StatTile label="Vacant" value={occupancy.vacant} tone="success" />
          <StatTile label="Reserved" value={occupancy.reserved} tone="info" />
          <StatTile label="Cleaning" value={occupancy.cleaning} tone="warning" />
          <StatTile label="Blocked" value={occupancy.blocked} tone="neutral" />
        </div>
      )}

      <div className="mb-5 flex flex-wrap gap-4 text-[11.5px] text-ink-muted">
        {([1, 2, 3, 4, 5] as BedStatus[]).map((s) => (
          <span key={s} className="flex items-center gap-1.5">
            <span className={`size-2.5 rounded-[3px] ${statusDotClass[s]}`} />
            {BED_STATUS_LABELS[s]}
          </span>
        ))}
      </div>

      {isLoading && <Spinner />}

      <div className="flex flex-col gap-5">
        {Object.entries(byWard).map(([ward, wardBeds]) => {
          const occupied = wardBeds.filter((b) => b.status === 2).length
          const byRoom = groupBy(wardBeds, (b) => b.roomName)
          return (
            <div key={ward} className="overflow-hidden rounded-shell border border-border-soft bg-panel shadow-shell-sm">
              <div className="flex items-center gap-2.5 border-b border-border-soft bg-panel-2 px-5 py-3.5">
                <Bed className="size-4 text-accent-2" aria-hidden />
                <span className="text-[13px] font-bold tracking-wide text-ink">{ward}</span>
                <span className="font-mono ml-auto text-[11.5px] text-ink-muted">{occupied}/{wardBeds.length} occupied</span>
              </div>
              <div className="flex flex-col gap-4 p-5">
                {Object.entries(byRoom).map(([room, roomBeds]) => (
                  <div key={room}>
                    <div className="mb-2 text-[10.5px] font-bold tracking-wider text-ink-faint uppercase">{room}</div>
                    <div className="flex flex-wrap gap-3">
                      {roomBeds.map((b) => {
                        const Icon = statusIcon[b.status]
                        return (
                          <button
                            key={b.bedId}
                            type="button"
                            disabled={!canManage}
                            onClick={() => setEditing(b)}
                            title={b.patientName ? `${b.patientName} · ${b.admissionNumber}${b.primaryConsultantName ? ` · ${b.primaryConsultantName}` : ''}` : BED_STATUS_LABELS[b.status]}
                            className={`relative flex h-[74px] w-[86px] flex-col justify-between rounded-shell-sm border p-2.5 text-left transition-transform hover:-translate-y-1 disabled:cursor-default disabled:hover:translate-y-0 ${statusTileClass[b.status]}`}
                          >
                            <span className="font-mono text-[10px] font-semibold opacity-85">{b.bedCode}</span>
                            <Icon className="size-4" aria-hidden />
                            {b.patientName && (
                              <span className="absolute right-2 bottom-2 max-w-[52px] truncate text-[9px] font-bold">
                                {b.patientName.split(' ')[0]}
                              </span>
                            )}
                          </button>
                        )
                      })}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )
        })}
        {!isLoading && beds.length === 0 && (
          <p className="rounded-shell border border-border-soft bg-panel py-10 text-center text-ink-faint">No beds configured.</p>
        )}
      </div>

      {editing && <BedStatusModal bed={editing} onClose={() => setEditing(null)} />}
    </>
  )
}

function groupBy<T>(items: T[], key: (item: T) => string): Record<string, T[]> {
  const map = new Map<string, T[]>()
  for (const item of items) {
    const k = key(item)
    const list = map.get(k) ?? []
    list.push(item)
    map.set(k, list)
  }
  return Object.fromEntries(map)
}

function StatTile({ label, value, tone }: { label: string; value: number; tone?: 'success' | 'warning' | 'danger' | 'info' | 'neutral' }) {
  return (
    <div className="rounded-shell border border-border-soft bg-panel p-3 text-center shadow-shell-sm">
      <p className="font-mono text-2xl font-semibold text-ink">{value}</p>
      <p className="mt-1 text-xs text-ink-muted">
        {tone ? <StatusBadge tone={tone}>{label}</StatusBadge> : label}
      </p>
    </div>
  )
}

function BedStatusModal({ bed, onClose }: { bed: BedBoardItem; onClose: () => void }) {
  const setStatus = useSetBedStatus()
  const [status, setStatusValue] = useState<BedStatus>(bed.status === 2 ? bed.status : 1)
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await setStatus.mutateAsync({ bedId: bed.bedId, status, reason: reason || null })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  if (bed.status === 2) {
    return (
      <Modal open title={`${bed.roomName}/${bed.bedCode}`} onClose={onClose} footer={<Button variant="secondary" onClick={onClose}>Close</Button>}>
        <p className="text-sm text-ink-muted">
          Occupied by <span className="font-medium text-ink">{bed.patientName}</span> ({bed.admissionNumber}). Transfer or discharge the patient to free this bed.
        </p>
      </Modal>
    )
  }

  return (
    <Modal
      open
      title={`Set status — ${bed.roomName}/${bed.bedCode}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={setStatus.isPending}>Save</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-shell-sm bg-crit/10 px-3 py-2 text-sm text-crit">{error}</p>}
        <Select
          label="Status"
          value={status}
          onChange={(e) => setStatusValue(Number(e.target.value) as BedStatus)}
          options={[{ value: 1, label: 'Vacant' }, { value: 3, label: 'Reserved' }, { value: 4, label: 'Cleaning' }, { value: 5, label: 'Blocked' }]}
        />
        <Input label="Reason (optional)" value={reason} onChange={(e) => setReason(e.target.value)} />
      </div>
    </Modal>
  )
}
