import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useBills, useGenerateOpBill } from '../api'
import { BILL_STATUS_LABELS, type BillListItem, type BillStatus } from '../types'

const statusTone: Record<BillStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info',
  2: 'warning',
  3: 'success',
  4: 'neutral',
  5: 'warning',
}

function today() {
  return new Date().toISOString().slice(0, 10)
}

export function BillsListPage() {
  const { hasPermission } = useAuth()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [fromDate, setFromDate] = useState(today())
  const [toDate, setToDate] = useState(today())
  const [generating, setGenerating] = useState(false)

  const { data, isLoading } = useBills({ page, pageSize: 20, search: search || undefined, fromDate, toDate })
  const canCreate = hasPermission('BILLING.BILL.CREATEOP')

  const resetFilters = () => {
    setSearch('')
    setFromDate(today())
    setToDate(today())
    setPage(1)
  }

  const columns: Column<BillListItem>[] = [
    { header: 'Bill No', cell: (row) => <span className="font-mono text-xs">{row.billNumber}</span> },
    { header: 'Date', cell: (row) => row.billDate },
    { header: 'Patient', cell: (row) => `${row.patientName} (${row.patientUhid})` },
    {
      header: 'Status',
      cell: (row) => <StatusBadge tone={statusTone[row.status]}>{BILL_STATUS_LABELS[row.status]}</StatusBadge>,
    },
    { header: 'Total', className: 'text-right', cell: (row) => row.totalAmount.toFixed(2) },
    { header: 'Paid', className: 'text-right', cell: (row) => row.paidAmount.toFixed(2) },
    { header: 'Balance', className: 'text-right', cell: (row) => row.balanceAmount.toFixed(2) },
    {
      header: '',
      className: 'text-right',
      cell: (row) => (
        <Button variant="ghost" onClick={() => navigate(`/billing/bills/${row.id}`)}>
          Open
        </Button>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title="Bills"
        subtitle="OP bills — generate, collect payments, apply discounts, refund and cancel"
        actions={canCreate && <Button onClick={() => setGenerating(true)}>Generate OP bill</Button>}
      />

      <div className="mb-4 rounded-shell border border-border-soft bg-panel p-4 shadow-shell-sm">
        <div className="flex flex-wrap items-end gap-3">
          <div className="w-72">
            <Input
              label="Bill No / UHID / Name"
              placeholder="Search bill no, UHID or name…"
              value={search}
              onChange={(e) => {
                setSearch(e.target.value)
                setPage(1)
              }}
            />
          </div>
          <div className="w-40">
            <Input
              label="From Date"
              type="date"
              value={fromDate}
              onChange={(e) => {
                setFromDate(e.target.value)
                setPage(1)
              }}
            />
          </div>
          <div className="w-40">
            <Input
              label="To Date"
              type="date"
              value={toDate}
              onChange={(e) => {
                setToDate(e.target.value)
                setPage(1)
              }}
            />
          </div>
          <div className="ml-auto flex gap-2">
            <Button variant="secondary" onClick={resetFilters}>Reset</Button>
          </div>
        </div>
      </div>

      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        rowKey={(row) => row.id}
        emptyMessage="No bills found."
      />

      {generating && <GenerateBillModal onClose={() => setGenerating(false)} />}
    </>
  )
}

function GenerateBillModal({ onClose }: { onClose: () => void }) {
  const navigate = useNavigate()
  const generate = useGenerateOpBill()
  const [visitId, setVisitId] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      const bill = await generate.mutateAsync({ opVisitId: Number(visitId) })
      onClose()
      navigate(`/billing/bills/${bill.id}`)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title="Generate OP bill"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} loading={generate.isPending} disabled={!visitId.trim()}>
            Generate
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
      <Input
        label="OPD Visit ID"
        type="number"
        min={1}
        value={visitId}
        onChange={(e) => setVisitId(e.target.value)}
        placeholder="e.g. the visit ID from the OPD front desk"
      />
      <p className="mt-2 text-xs text-slate-500">
        Pulls the consultation fee and any non-cancelled clinical orders from that visit onto a new bill.
      </p>
    </Modal>
  )
}
