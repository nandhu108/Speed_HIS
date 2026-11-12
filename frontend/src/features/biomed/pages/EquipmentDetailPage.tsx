import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import {
  calibrationCertificateUrl,
  useAmcContracts,
  useCalibrations,
  useCreateAmcContract,
  useCreateCalibration,
  useEquipmentDetail,
  useEquipmentLocationHistory,
  useLogServiceHistory,
  useRecordEquipmentMovement,
  useServiceHistory,
} from '../api'
import { CRITICALITY_LABELS, EQUIPMENT_STATUS_LABELS } from '../types'

export function EquipmentDetailPage() {
  const { id } = useParams<{ id: string }>()
  const equipmentId = Number(id)
  const { hasPermission } = useAuth()
  const canManage = hasPermission('BIOMED.EQUIPMENT.MANAGE')
  const { data: equipment, isLoading } = useEquipmentDetail(equipmentId)
  const { data: locations = [] } = useEquipmentLocationHistory(equipmentId)
  const { data: contracts = [] } = useAmcContracts(equipmentId)
  const { data: calibrations = [] } = useCalibrations(equipmentId)
  const { data: history = [] } = useServiceHistory(equipmentId)

  const move = useRecordEquipmentMovement(equipmentId)
  const [toLocation, setToLocation] = useState('')
  const [error, setError] = useState<string | null>(null)

  const run = async (fn: () => Promise<unknown>) => {
    setError(null)
    try {
      await fn()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  if (isLoading || !equipment) return <Spinner />

  return (
    <>
      <PageHeader
        title={`${equipment.equipmentCode} — ${equipment.name}`}
        subtitle={`${equipment.model ?? ''} ${equipment.serialNumber ? `· SN ${equipment.serialNumber}` : ''}`}
        backTo="/biomed/equipment"
        actions={
          <div className="flex items-center gap-2">
            <StatusBadge tone={equipment.criticality <= 2 ? 'danger' : 'neutral'}>{CRITICALITY_LABELS[equipment.criticality]}</StatusBadge>
            <StatusBadge tone={equipment.status === 1 ? 'success' : equipment.status === 2 ? 'warning' : 'neutral'}>{EQUIPMENT_STATUS_LABELS[equipment.status]}</StatusBadge>
          </div>
        }
      />
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <div className="rounded-lg border border-slate-200 bg-white p-4">
          <h3 className="mb-2 text-sm font-semibold text-slate-700">Location</h3>
          <p className="mb-2 text-sm text-slate-600">Current: {equipment.currentLocation}</p>
          {canManage && (
            <div className="mb-3 flex items-end gap-2">
              <Input label="Move to" value={toLocation} onChange={(e) => setToLocation(e.target.value)} />
              <Button
                variant="secondary"
                onClick={() => run(() => move.mutateAsync({ toLocation, reason: null })).then(() => setToLocation(''))}
                disabled={!toLocation}
                loading={move.isPending}
              >
                Move
              </Button>
            </div>
          )}
          <table className="w-full text-xs">
            <tbody>
              {locations.slice(0, 5).map((l) => (
                <tr key={l.id} className="border-t border-slate-100">
                  <td className="py-1 text-slate-500">{new Date(l.movedAt).toLocaleDateString()}</td>
                  <td className="py-1">{l.fromLocation} → {l.toLocation}</td>
                </tr>
              ))}
              {locations.length === 0 && <tr><td className="py-3 text-center text-slate-400">No movements recorded.</td></tr>}
            </tbody>
          </table>
        </div>

        <AmcCard equipmentId={equipmentId} contracts={contracts} canManage={canManage} run={run} />
        <CalibrationCard equipmentId={equipmentId} calibrations={calibrations} canManage={canManage} run={run} />
        <ServiceHistoryCard equipmentId={equipmentId} history={history} canManage={canManage} run={run} />
      </div>
    </>
  )
}

function AmcCard({ equipmentId, contracts, canManage, run }: { equipmentId: number; contracts: ReturnType<typeof useAmcContracts>['data']; canManage: boolean; run: (fn: () => Promise<unknown>) => Promise<void> }) {
  const create = useCreateAmcContract()
  const [vendorId, setVendorId] = useState('')
  const [contractNumber, setContractNumber] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [cost, setCost] = useState('')

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-2 text-sm font-semibold text-slate-700">AMC / CMC Contracts</h3>
      <table className="mb-3 w-full text-xs">
        <tbody>
          {(contracts ?? []).map((c) => (
            <tr key={c.id} className="border-t border-slate-100">
              <td className="py-1">{c.contractNumber}</td>
              <td className="py-1">{c.startDate} → {c.endDate}</td>
              <td className="py-1">{c.isRenewalDue && <StatusBadge tone="warning">Renewal due</StatusBadge>}</td>
            </tr>
          ))}
          {(contracts ?? []).length === 0 && <tr><td className="py-3 text-center text-slate-400">No contracts.</td></tr>}
        </tbody>
      </table>
      {canManage && (
        <div className="grid grid-cols-2 gap-2 border-t border-slate-100 pt-2">
          <Input label="Vendor ID" type="number" value={vendorId} onChange={(e) => setVendorId(e.target.value)} />
          <Input label="Contract No" value={contractNumber} onChange={(e) => setContractNumber(e.target.value)} />
          <Input label="Start" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
          <Input label="End" type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
          <Input label="Cost" type="number" value={cost} onChange={(e) => setCost(e.target.value)} />
          <Button
            className="col-span-2"
            variant="secondary"
            loading={create.isPending}
            disabled={!vendorId || !contractNumber || !startDate || !endDate}
            onClick={() =>
              run(() =>
                create.mutateAsync({
                  equipmentId, vendorId: Number(vendorId), contractType: 1, contractNumber, startDate, endDate,
                  cost: Number(cost) || 0, renewalReminderDays: 30, notes: null,
                }),
              ).then(() => { setVendorId(''); setContractNumber(''); setStartDate(''); setEndDate(''); setCost('') })
            }
          >
            Add contract
          </Button>
        </div>
      )}
    </div>
  )
}

function CalibrationCard({ equipmentId, calibrations, canManage, run }: { equipmentId: number; calibrations: ReturnType<typeof useCalibrations>['data']; canManage: boolean; run: (fn: () => Promise<unknown>) => Promise<void> }) {
  const create = useCreateCalibration()
  const [calibrationDate, setCalibrationDate] = useState('')
  const [nextDueDate, setNextDueDate] = useState('')
  const [performedBy, setPerformedBy] = useState('')
  const [certificate, setCertificate] = useState<File | null>(null)

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-2 text-sm font-semibold text-slate-700">Calibrations</h3>
      <table className="mb-3 w-full text-xs">
        <tbody>
          {(calibrations ?? []).map((c) => (
            <tr key={c.id} className="border-t border-slate-100">
              <td className="py-1">{c.calibrationDate} → due {c.nextDueDate}</td>
              <td className="py-1">{c.performedBy ?? ''}</td>
              <td className="py-1">
                {c.hasCertificateFile && <a href={calibrationCertificateUrl(c.id)} target="_blank" rel="noreferrer" className="text-primary-600 hover:underline">Certificate</a>}
                {c.isDue && <StatusBadge tone="warning">Due</StatusBadge>}
              </td>
            </tr>
          ))}
          {(calibrations ?? []).length === 0 && <tr><td className="py-3 text-center text-slate-400">No calibrations recorded.</td></tr>}
        </tbody>
      </table>
      {canManage && (
        <div className="grid grid-cols-2 gap-2 border-t border-slate-100 pt-2">
          <Input label="Calibration date" type="date" value={calibrationDate} onChange={(e) => setCalibrationDate(e.target.value)} />
          <Input label="Next due" type="date" value={nextDueDate} onChange={(e) => setNextDueDate(e.target.value)} />
          <Input label="Performed by" value={performedBy} onChange={(e) => setPerformedBy(e.target.value)} />
          <input type="file" onChange={(e) => setCertificate(e.target.files?.[0] ?? null)} className="self-end text-xs" />
          <Button
            className="col-span-2"
            variant="secondary"
            loading={create.isPending}
            disabled={!calibrationDate || !nextDueDate}
            onClick={() =>
              run(() =>
                create.mutateAsync({
                  equipmentId, calibrationDate, nextDueDate, performedBy: performedBy || null,
                  certificateNumber: null, notes: null, certificate,
                }),
              ).then(() => { setCalibrationDate(''); setNextDueDate(''); setPerformedBy(''); setCertificate(null) })
            }
          >
            Record calibration
          </Button>
        </div>
      )}
    </div>
  )
}

function ServiceHistoryCard({ equipmentId, history, canManage, run }: { equipmentId: number; history: ReturnType<typeof useServiceHistory>['data']; canManage: boolean; run: (fn: () => Promise<unknown>) => Promise<void> }) {
  const log = useLogServiceHistory(equipmentId)
  const [serviceDate, setServiceDate] = useState('')
  const [serviceType, setServiceType] = useState('')

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4">
      <h3 className="mb-2 text-sm font-semibold text-slate-700">Service History</h3>
      <table className="mb-3 w-full text-xs">
        <tbody>
          {(history ?? []).map((h) => (
            <tr key={h.id} className="border-t border-slate-100">
              <td className="py-1 text-slate-500">{h.serviceDate}</td>
              <td className="py-1">{h.serviceType}</td>
              <td className="py-1 text-slate-500">{h.workOrderNumber ?? h.performedBy ?? ''}</td>
            </tr>
          ))}
          {(history ?? []).length === 0 && <tr><td className="py-3 text-center text-slate-400">No service history.</td></tr>}
        </tbody>
      </table>
      {canManage && (
        <div className="flex items-end gap-2 border-t border-slate-100 pt-2">
          <Input label="Date" type="date" value={serviceDate} onChange={(e) => setServiceDate(e.target.value)} />
          <Input label="Type" value={serviceType} onChange={(e) => setServiceType(e.target.value)} placeholder="e.g. Vendor visit" />
          <Button
            variant="secondary"
            loading={log.isPending}
            disabled={!serviceDate || !serviceType}
            onClick={() => run(() => log.mutateAsync({ serviceDate, serviceType, performedBy: null, cost: null, notes: null })).then(() => { setServiceDate(''); setServiceType('') })}
          >
            Log
          </Button>
        </div>
      )}
    </div>
  )
}
