import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { PageHeader } from '../../../components/ui/PageHeader'
import { useBills } from '../api'
import type { BillListItem } from '../types'

export function ParkedBillsPage() {
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')

  const { data, isLoading } = useBills({ page, pageSize: 20, search: search || undefined, status: 5 })

  const columns: Column<BillListItem>[] = [
    { header: 'Bill No', cell: (row) => <span className="font-mono text-xs">{row.billNumber}</span> },
    { header: 'Date', cell: (row) => row.billDate },
    { header: 'Patient', cell: (row) => `${row.patientName} (${row.patientUhid})` },
    { header: 'Total', className: 'text-right', cell: (row) => row.totalAmount.toFixed(2) },
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
      <PageHeader title="Parked Bills" subtitle="Bills on hold — open one to unpark it or review why it was parked" />

      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onSearch={setSearch}
        searchPlaceholder="Search bill no, UHID or name…"
        rowKey={(row) => row.id}
        emptyMessage="No parked bills."
      />
    </>
  )
}
