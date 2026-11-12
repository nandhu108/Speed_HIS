import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { DateRangeFilter, defaultDateRange } from '../../../components/ui/DateRangeFilter'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useGenerateOpBill, useOpBillingSearch, type OpBillingSearchQuery } from '../api'
import { BILL_STATUS_LABELS, BILLING_GENDER_LABELS, type OpBillingSearchResult } from '../types'
import { BillWorkspace } from './BillDetailPage'

const emptyFilters = {
  patientName: '',
  doctorName: '',
  regNo: '',
  mobile: '',
  gender: '',
  ipNumber: '',
  mrdOnly: false,
  emrOnly: false,
  packageOnly: false,
}

const TAB_LABELS = ['I', 'II', 'III'] as const

/**
 * OP Billing's own patient search — the legacy "OP Bill Payment" patient search screen,
 * with Aosta's "multi-window" behavior: 3 parallel tabs, each an independent session (its own
 * search filters, or its own open bill) that keeps its state while another tab is active. All
 * 3 tabs stay mounted (just hidden via CSS) so switching tabs never loses what's in progress.
 */
export function OpBillingSearchPage() {
  const { hasPermission } = useAuth()
  const navigate = useNavigate()
  const [activeTab, setActiveTab] = useState(0)
  const [billIds, setBillIds] = useState<(number | null)[]>([null, null, null])
  const canRegisterPatient = hasPermission('PATIENT.PATIENT.CREATE')

  const openBillInTab = (index: number, billId: number | null) =>
    setBillIds((prev) => prev.map((id, i) => (i === index ? billId : id)))

  return (
    <>
      <PageHeader
        title="OP Billing — Patient Search"
        subtitle="Search today's (or any date range's) OP/MHC visits to generate or open a bill — separate from Front Office's patient search."
        actions={
          canRegisterPatient && (
            <Button variant="secondary" onClick={() => navigate('/patients/new')}>
              New Patient
            </Button>
          )
        }
      />

      <div className="mb-4 flex gap-1 border-b border-border-soft">
        {TAB_LABELS.map((label, i) => (
          <button
            key={label}
            type="button"
            onClick={() => setActiveTab(i)}
            className={`px-4 py-2 text-sm font-semibold transition-colors ${
              i === activeTab ? 'border-b-2 border-accent-2 text-ink' : 'text-ink-faint hover:text-ink-muted'
            }`}
          >
            OP Bill Payment {label}
          </button>
        ))}
      </div>

      {billIds.map((billId, i) => (
        <div key={i} className={i === activeTab ? '' : 'hidden'}>
          {billId == null ? (
            <PatientSearchPanel onOpenBill={(id) => openBillInTab(i, id)} />
          ) : (
            <BillWorkspace billId={billId} onBack={() => openBillInTab(i, null)} onOpenBill={(id) => openBillInTab(i, id)} />
          )}
        </div>
      ))}
    </>
  )
}

/** The filter form + results grid for one OP Bill Payment tab. */
function PatientSearchPanel({ onOpenBill }: { onOpenBill: (billId: number) => void }) {
  const { hasPermission } = useAuth()
  const [{ fromDate, toDate }, setRange] = useState(defaultDateRange(0))
  const [filters, setFilters] = useState(emptyFilters)
  const [query, setQuery] = useState<OpBillingSearchQuery>({ page: 1, pageSize: 10, fromDate, toDate })
  const [error, setError] = useState<string | null>(null)
  const [openingVisitId, setOpeningVisitId] = useState<number | null>(null)

  const { data, isLoading } = useOpBillingSearch(query)
  const generateBill = useGenerateOpBill()
  const canCreate = hasPermission('BILLING.BILL.CREATEOP')

  const runSearch = () => {
    setError(null)
    setQuery({
      page: 1,
      pageSize: 10,
      fromDate,
      toDate,
      patientName: filters.patientName.trim() || undefined,
      doctorName: filters.doctorName.trim() || undefined,
      regNo: filters.regNo.trim() || undefined,
      mobile: filters.mobile.trim() || undefined,
      gender: filters.gender ? Number(filters.gender) : undefined,
      ipNumber: filters.ipNumber.trim() || undefined,
      mrdOnly: filters.mrdOnly || undefined,
      emrOnly: filters.emrOnly || undefined,
      packageOnly: filters.packageOnly || undefined,
    })
  }

  const resetSearch = () => {
    setFilters(emptyFilters)
    setRange(defaultDateRange(0))
    setError(null)
    setQuery({ page: 1, pageSize: 10, ...defaultDateRange(0) })
  }

  const openRow = async (row: OpBillingSearchResult) => {
    setError(null)
    if (row.billId) {
      onOpenBill(row.billId)
      return
    }
    if (!canCreate) return
    setOpeningVisitId(row.opVisitId)
    try {
      const bill = await generateBill.mutateAsync({ opVisitId: row.opVisitId })
      onOpenBill(bill.id)
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setOpeningVisitId(null)
    }
  }

  const columns: Column<OpBillingSearchResult>[] = [
    { header: 'Reg No', cell: (row) => <span className="font-mono text-xs">{row.regNo}</span> },
    { header: 'Patient Name', cell: (row) => row.patientName },
    { header: 'Encounter Date', cell: (row) => row.encounterDate },
    { header: 'Age / Sex', cell: (row) => `${row.age} / ${BILLING_GENDER_LABELS[row.gender]}` },
    { header: 'Mobile', cell: (row) => row.mobile || '—' },
    {
      header: 'Doctor / Dept',
      cell: (row) => [row.doctorName, row.specialtyName].filter(Boolean).join(' / ') || '—',
    },
    { header: 'Patient Type', cell: (row) => row.patientType ?? '—' },
    { header: 'Source Name', cell: (row) => row.sourceName ?? '—' },
    {
      header: 'Bill',
      cell: (row) =>
        row.status != null ? (
          <StatusBadge tone="info">{BILL_STATUS_LABELS[row.status]}</StatusBadge>
        ) : (
          <span className="text-ink-faint">Not billed</span>
        ),
    },
    {
      header: '',
      className: 'text-right',
      cell: (row) => (
        <Button
          variant={row.billId ? 'ghost' : 'primary'}
          disabled={!row.billId && !canCreate}
          loading={openingVisitId === row.opVisitId}
          onClick={() => openRow(row)}
        >
          {row.billId ? 'Open' : 'Generate bill'}
        </Button>
      ),
    },
  ]

  return (
    <>
      {error && <p className="mb-4 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="mb-4 rounded-shell border border-border-soft bg-panel p-4 shadow-shell-sm">
        <DateRangeFilter fromDate={fromDate} toDate={toDate} onFromDate={(v) => setRange((r) => ({ ...r, fromDate: v }))} onToDate={(v) => setRange((r) => ({ ...r, toDate: v }))} />
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          <Input label="Patient Name" value={filters.patientName} onChange={(e) => setFilters((f) => ({ ...f, patientName: e.target.value }))} />
          <Input label="Doctor Name" value={filters.doctorName} onChange={(e) => setFilters((f) => ({ ...f, doctorName: e.target.value }))} />
          <Input label="Reg No" value={filters.regNo} onChange={(e) => setFilters((f) => ({ ...f, regNo: e.target.value }))} />
          <Input label="Mobile Number" value={filters.mobile} onChange={(e) => setFilters((f) => ({ ...f, mobile: e.target.value }))} />
          <Input label="IP Number" value={filters.ipNumber} onChange={(e) => setFilters((f) => ({ ...f, ipNumber: e.target.value }))} />
          <Select
            label="Gender"
            placeholder="Any"
            value={filters.gender}
            onChange={(e) => setFilters((f) => ({ ...f, gender: e.target.value }))}
            options={[
              { value: 1, label: 'Male' },
              { value: 2, label: 'Female' },
              { value: 3, label: 'Other' },
            ]}
          />
        </div>
        <div className="mt-3 flex flex-wrap items-center gap-4">
          <label className="flex items-center gap-1.5 text-sm text-ink-muted">
            <input type="checkbox" checked={filters.mrdOnly} onChange={(e) => setFilters((f) => ({ ...f, mrdOnly: e.target.checked }))} />
            MRD
          </label>
          <label className="flex items-center gap-1.5 text-sm text-ink-muted">
            <input type="checkbox" checked={filters.emrOnly} onChange={(e) => setFilters((f) => ({ ...f, emrOnly: e.target.checked }))} />
            EMR
          </label>
          <label className="flex items-center gap-1.5 text-sm text-ink-muted">
            <input type="checkbox" checked={filters.packageOnly} onChange={(e) => setFilters((f) => ({ ...f, packageOnly: e.target.checked }))} />
            OP Multiple Visit Package
          </label>
          <div className="ml-auto flex gap-2">
            <Button variant="secondary" onClick={resetSearch}>
              Cancel
            </Button>
            <Button onClick={runSearch}>Search</Button>
          </div>
        </div>
      </div>

      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={query.page ?? 1}
        onPageChange={(page) => setQuery((q) => ({ ...q, page }))}
        onPageSizeChange={(pageSize) => setQuery((q) => ({ ...q, page: 1, pageSize }))}
        rowKey={(row) => row.opVisitId}
        emptyMessage="No visits found for these filters."
        enableColumnPicker
      />
    </>
  )
}
