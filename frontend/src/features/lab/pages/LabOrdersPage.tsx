import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Modal } from '../../../components/ui/Modal'
import { Select } from '../../../components/ui/Select'
import { PageHeader } from '../../../components/ui/PageHeader'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { PatientPicker } from '../../appointments/components/PatientPicker'
import { useAuth } from '../../auth/AuthContext'
import { useDoctors } from '../../masters/api'
import type { PatientListItem } from '../../patients/types'
import { useCreateLabOrder, useLabOrders, useLabTests, useTestProfiles } from '../api'
import { LAB_ORDER_STATUS_LABELS, PRIORITY_LABELS, type LabOrderListItem, type LabOrderStatus } from '../types'

const statusTone: Record<LabOrderStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info',
  2: 'warning',
  3: 'success',
  4: 'neutral',
}

interface ErOrderPrefill {
  emergencyVisitId: number
  patientId: number
  patientName: string
  patientUhid: string
}

/** Set by OPD's ConsultationWorkspace "Order in Lab" action — see docs/modules/lab.md's "OPD order bridge" note. */
interface OpOrderPrefill {
  opVisitId: number
  clinicalOrderId: number
  patientId: number
  patientName: string
  patientUhid: string
}

export function LabOrdersPage() {
  const { hasPermission } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const state = location.state as { erOrder?: ErOrderPrefill; opOrder?: OpOrderPrefill } | null
  const erPrefill = state?.erOrder ?? null
  const opPrefill = state?.opOrder ?? null
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(!!erPrefill || !!opPrefill)

  const { data, isLoading } = useLabOrders({ page, pageSize: 20, search: search || undefined })
  const canCreate = hasPermission('LAB.ORDER.CREATE')

  const columns: Column<LabOrderListItem>[] = [
    { header: 'Order No', cell: (row) => <span className="font-mono text-xs">{row.orderNumber}</span> },
    { header: 'Date', cell: (row) => row.orderDate },
    { header: 'Patient', cell: (row) => `${row.patientName} (${row.patientUhid})` },
    { header: 'Priority', cell: (row) => PRIORITY_LABELS[row.priority] },
    { header: 'Items', className: 'text-right', cell: (row) => row.itemCount },
    {
      header: 'Status',
      cell: (row) => <StatusBadge tone={statusTone[row.status]}>{LAB_ORDER_STATUS_LABELS[row.status]}</StatusBadge>,
    },
    {
      header: '',
      className: 'text-right',
      cell: (row) => (
        <Button variant="ghost" onClick={() => navigate(`/lab/orders/${row.id}`)}>
          Open
        </Button>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title="Lab Orders"
        subtitle="Order lab tests and profiles for a patient"
        actions={canCreate && <Button onClick={() => setCreating(true)}>New order</Button>}
      />

      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onSearch={setSearch}
        searchPlaceholder="Search order no, UHID or name…"
        rowKey={(row) => row.id}
        emptyMessage="No lab orders found."
      />

      {creating && <CreateOrderModal onClose={() => setCreating(false)} erPrefill={erPrefill} opPrefill={opPrefill} />}
    </>
  )
}

function CreateOrderModal({
  onClose, erPrefill, opPrefill,
}: { onClose: () => void; erPrefill: ErOrderPrefill | null; opPrefill: OpOrderPrefill | null }) {
  const navigate = useNavigate()
  const create = useCreateLabOrder()
  const { data: tests = [] } = useLabTests()
  const { data: profiles = [] } = useTestProfiles()
  const { data: doctorsResult } = useDoctors({ page: 1, pageSize: 200 })

  const [patient, setPatient] = useState<PatientListItem | null>(null)
  const [doctorId, setDoctorId] = useState('')
  const [priority, setPriority] = useState<'1' | '2' | '3'>(erPrefill ? '3' : '1')
  const [notes, setNotes] = useState('')
  const [selectedTests, setSelectedTests] = useState<Set<number>>(new Set())
  const [selectedProfiles, setSelectedProfiles] = useState<Set<number>>(new Set())
  const [error, setError] = useState<string | null>(null)

  const toggleTest = (id: number) => {
    const next = new Set(selectedTests)
    if (next.has(id)) next.delete(id)
    else next.add(id)
    setSelectedTests(next)
  }
  const toggleProfile = (id: number) => {
    const next = new Set(selectedProfiles)
    if (next.has(id)) next.delete(id)
    else next.add(id)
    setSelectedProfiles(next)
  }

  const submit = async () => {
    const patientId = erPrefill?.patientId ?? opPrefill?.patientId ?? patient?.id
    if (!patientId) return
    setError(null)
    try {
      const order = await create.mutateAsync({
        patientId,
        opVisitId: opPrefill?.opVisitId ?? null,
        emergencyVisitId: erPrefill?.emergencyVisitId ?? null,
        clinicalOrderId: opPrefill?.clinicalOrderId ?? null,
        orderedByDoctorId: doctorId ? Number(doctorId) : null,
        priority: Number(priority) as 1 | 2 | 3,
        clinicalNotes: notes || null,
        items: [
          ...Array.from(selectedTests).map((labTestId) => ({ labTestId, testProfileId: null })),
          ...Array.from(selectedProfiles).map((testProfileId) => ({ labTestId: null, testProfileId })),
        ],
      })
      onClose()
      navigate(`/lab/orders/${order.id}`)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const hasSelection = selectedTests.size > 0 || selectedProfiles.size > 0

  return (
    <Modal
      open
      title="New lab order"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={create.isPending} disabled={(!erPrefill && !opPrefill && !patient) || !hasSelection}>
            Create order
          </Button>
        </>
      }
    >
      <div className="max-h-[65vh] space-y-3 overflow-y-auto">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        {erPrefill ? (
          <div className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800">
            ER STAT order for <span className="font-medium">{erPrefill.patientName}</span>{' '}
            <span className="font-mono text-xs">({erPrefill.patientUhid})</span>
          </div>
        ) : opPrefill ? (
          <div className="rounded-md border border-blue-200 bg-blue-50 px-3 py-2 text-sm text-blue-800">
            From OPD consultation for <span className="font-medium">{opPrefill.patientName}</span>{' '}
            <span className="font-mono text-xs">({opPrefill.patientUhid})</span>
          </div>
        ) : (
          <PatientPicker selected={patient} onSelect={setPatient} />
        )}
        <div className="grid grid-cols-2 gap-3">
          <Select
            label="Referring doctor (optional)"
            placeholder="None"
            value={doctorId}
            onChange={(e) => setDoctorId(e.target.value)}
            options={(doctorsResult?.items ?? []).map((d) => ({ value: d.id, label: d.fullName }))}
          />
          <Select
            label="Priority"
            value={priority}
            onChange={(e) => setPriority(e.target.value as '1' | '2' | '3')}
            options={[
              { value: '1', label: 'Routine' },
              { value: '2', label: 'Urgent' },
              { value: '3', label: 'STAT' },
            ]}
          />
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div>
            <p className="mb-1 text-sm font-medium text-slate-700">Tests</p>
            <div className="max-h-48 overflow-y-auto rounded-md border border-slate-200 p-2">
              {tests.map((t) => (
                <label key={t.id} className="flex items-center gap-2 px-2 py-1 text-sm text-slate-700 hover:bg-slate-50">
                  <input type="checkbox" checked={selectedTests.has(t.id)} onChange={() => toggleTest(t.id)} />
                  {t.name}
                </label>
              ))}
            </div>
          </div>
          <div>
            <p className="mb-1 text-sm font-medium text-slate-700">Profiles</p>
            <div className="max-h-48 overflow-y-auto rounded-md border border-slate-200 p-2">
              {profiles.map((p) => (
                <label key={p.id} className="flex items-center gap-2 px-2 py-1 text-sm text-slate-700 hover:bg-slate-50">
                  <input type="checkbox" checked={selectedProfiles.has(p.id)} onChange={() => toggleProfile(p.id)} />
                  {p.name} <span className="text-xs text-slate-400">({p.testCount})</span>
                </label>
              ))}
            </div>
          </div>
        </div>

        <label className="block text-sm font-medium text-slate-700">
          Clinical notes
          <textarea
            className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100"
            rows={2}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
          />
        </label>
      </div>
    </Modal>
  )
}
