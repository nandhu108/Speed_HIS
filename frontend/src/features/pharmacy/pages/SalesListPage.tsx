import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { PageHeader } from '../../../components/ui/PageHeader'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { usePharmacySales } from '../api'
import { SALE_STATUS_LABELS, type PharmacySaleListItem, type PharmacySaleStatus } from '../types'

const statusTone: Record<PharmacySaleStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'success', 2: 'neutral',
}

export function SalesListPage() {
  const { hasPermission } = useAuth()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const { data, isLoading } = usePharmacySales({ page, pageSize: 20, search: search || undefined })
  const canCreate = hasPermission('PHARMACY.SALE.CREATE')

  const columns: Column<PharmacySaleListItem>[] = [
    { header: 'Sale No', cell: (row) => <span className="font-mono text-xs">{row.saleNumber}</span> },
    { header: 'Date', cell: (row) => row.saleDate },
    { header: 'Patient', cell: (row) => row.patientName ?? 'Walk-in' },
    {
      header: 'Status',
      cell: (row) => <StatusBadge tone={statusTone[row.status]}>{SALE_STATUS_LABELS[row.status]}</StatusBadge>,
    },
    { header: 'Total', className: 'text-right', cell: (row) => row.totalAmount.toFixed(2) },
    {
      header: '',
      className: 'text-right',
      cell: (row) => (
        <Button variant="ghost" onClick={() => navigate(`/pharmacy/sales/${row.id}`)}>
          Open
        </Button>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title="Pharmacy Sales"
        subtitle="OP pharmacy POS sales"
        actions={canCreate && <Button onClick={() => navigate('/pharmacy/sales/new')}>New sale</Button>}
      />

      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onSearch={setSearch}
        searchPlaceholder="Search sale no…"
        rowKey={(row) => row.id}
      />
    </>
  )
}
