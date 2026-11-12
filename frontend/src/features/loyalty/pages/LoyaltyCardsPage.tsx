import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Input } from '../../../components/ui/Input'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { PatientPicker } from '../../appointments/components/PatientPicker'
import { useAuth } from '../../auth/AuthContext'
import { useLoyaltyCardTypes } from '../../masters/api'
import type { PatientListItem } from '../../patients/types'
import { useLoyaltyCards, useRegisterLoyaltyCard } from '../api'
import { LOYALTY_CARD_STATUS_LABELS, type LoyaltyCardListItem, type LoyaltyCardStatus } from '../types'

const statusTone: Record<LoyaltyCardStatus, 'success' | 'danger'> = {
  1: 'success',
  2: 'danger',
}

export function LoyaltyCardsPage() {
  const { hasPermission } = useAuth()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [patient, setPatient] = useState<PatientListItem | null>(null)
  const [cardTypeId, setCardTypeId] = useState('')
  const [durationMonths, setDurationMonths] = useState('12')
  const [error, setError] = useState<string | null>(null)

  const { data: cardTypes = [] } = useLoyaltyCardTypes()
  const { data, isLoading } = useLoyaltyCards({ page, pageSize: 20, search: search || undefined })
  const register = useRegisterLoyaltyCard()

  const canCreate = hasPermission('LOYALTY.CARD.CREATE')

  const submit = async () => {
    if (!patient || !cardTypeId || !durationMonths) return
    setError(null)
    try {
      const card = await register.mutateAsync({
        patientId: patient.id, loyaltyCardTypeId: Number(cardTypeId), durationMonths: Number(durationMonths),
      })
      navigate(`/loyalty/cards/${card.id}`)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const columns: Column<LoyaltyCardListItem>[] = [
    { header: 'Card No', cell: (row) => <span className="font-mono text-xs">{row.cardNumber}</span> },
    { header: 'Member', cell: (row) => `${row.patientName} (${row.patientUhid})` },
    { header: 'Type', cell: (row) => row.loyaltyCardTypeName },
    { header: 'Issued', cell: (row) => row.issuedDate },
    { header: 'Expires', cell: (row) => row.expiryDate },
    { header: 'Amount', cell: (row) => row.amount.toFixed(2) },
    {
      header: 'Status',
      cell: (row) => <StatusBadge tone={statusTone[row.status]}>{LOYALTY_CARD_STATUS_LABELS[row.status]}</StatusBadge>,
    },
    {
      header: '',
      className: 'text-right',
      cell: (row) => (
        <Button variant="ghost" onClick={() => navigate(`/loyalty/cards/${row.id}`)}>
          View
        </Button>
      ),
    },
  ]

  return (
    <>
      <PageHeader title="Loyalty Registration" subtitle="Register a loyalty membership card or find an existing one" />

      {canCreate && (
        <div className="mb-6 max-w-3xl rounded-lg border border-slate-200 bg-white p-4">
          {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
          <div className="flex flex-wrap items-end gap-3">
            <div className="w-72">
              <PatientPicker selected={patient} onSelect={setPatient} />
            </div>
            <Select
              label="Card Type"
              placeholder="Select a card type…"
              options={cardTypes.filter((t) => t.isActive).map((t) => ({ value: t.id, label: t.name }))}
              value={cardTypeId}
              onChange={(e) => setCardTypeId(e.target.value)}
            />
            <Input
              label="Duration (months)"
              type="number"
              min={1}
              className="w-40"
              value={durationMonths}
              onChange={(e) => setDurationMonths(e.target.value)}
            />
            <Button disabled={!patient || !cardTypeId || !durationMonths} loading={register.isPending} onClick={submit}>
              Register card
            </Button>
          </div>
        </div>
      )}

      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onSearch={setSearch}
        searchPlaceholder="Search card no, UHID or name…"
        rowKey={(row) => row.id}
        emptyMessage="No loyalty cards found."
      />
    </>
  )
}
