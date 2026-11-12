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
import { downloadVisitSummary, openVisitSummary, useCancelVisit, useOpVisits } from '../api'
import { OP_VISIT_STATUS_LABELS, VISIT_TYPE_LABELS, type OpVisitListItem, type OpVisitStatus } from '../types'

const statusTone: Record<OpVisitStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'info',
  2: 'success',
  3: 'neutral',
  4: 'danger',
}

/**
 * "OP Registration Cancel" / "OP Case Sheet Reprint-Direct" / "OP Casesheet Reprint-Preview" —
 * legacy listed these as three separate menu entries, but they're all the same underlying need
 * (find a visit, then act on it), so this one search screen serves all three; the Registration
 * menu just links to it three times with the legacy wording, same pattern as the Tools group's
 * three Control-Panel entries sharing one Settings page.
 *
 * "Case sheet" here means the consultation summary (diagnosis/prescription/advice/follow-up),
 * same document ConsultationWorkspace's "Print summary" button produces — not the front-desk
 * token slip ("Print slip"/`downloadVisitSlip`), which this screen used to call by mistake.
 */
export function OpRegistrationUtilityPage() {
  const { hasPermission } = useAuth()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [cancelling, setCancelling] = useState<OpVisitListItem | null>(null)
  const [reprintError, setReprintError] = useState<string | null>(null)

  const { data, isLoading } = useOpVisits({ page, pageSize: 20, search: search || undefined })
  const canManage = hasPermission('OPD.VISIT.MANAGE')
  const canView = hasPermission('OPD.VISIT.VIEW')

  const reprint = async (row: OpVisitListItem, mode: 'direct' | 'preview') => {
    setReprintError(null)
    try {
      if (mode === 'direct') {
        await downloadVisitSummary(row.id, row.visitNumber)
      } else {
        await openVisitSummary(row.id)
      }
    } catch (err) {
      setReprintError(errorMessage(err))
    }
  }

  const columns: Column<OpVisitListItem>[] = [
    { header: 'Token', cell: (row) => <span className="font-mono">#{row.tokenNumber}</span> },
    { header: 'Visit No', cell: (row) => <span className="font-mono text-xs">{row.visitNumber}</span> },
    { header: 'Patient', cell: (row) => `${row.patientName} (${row.patientUhid})` },
    { header: 'Doctor', cell: (row) => row.doctorName },
    { header: 'Type', cell: (row) => VISIT_TYPE_LABELS[row.visitType] },
    {
      header: 'Status',
      cell: (row) => <StatusBadge tone={statusTone[row.status]}>{OP_VISIT_STATUS_LABELS[row.status]}</StatusBadge>,
    },
    {
      header: '',
      className: 'text-right',
      cell: (row) => (
        <div className="flex justify-end gap-1">
          <Button variant="ghost" onClick={() => navigate(`/opd/visits/${row.id}`)}>
            Open
          </Button>
          {canView && (
            <>
              <Button variant="ghost" onClick={() => reprint(row, 'direct')}>
                Case sheet (direct)
              </Button>
              <Button variant="ghost" onClick={() => reprint(row, 'preview')}>
                Case sheet (preview)
              </Button>
            </>
          )}
          {canManage && row.status !== 3 && row.status !== 4 && (
            <Button variant="ghost" className="text-crit" onClick={() => setCancelling(row)}>
              Cancel
            </Button>
          )}
        </div>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title="OP Registration Cancel / Reprint"
        subtitle="Search for a visit to cancel its registration or reprint the case sheet"
      />

      {reprintError && <p className="mb-3 rounded-shell-sm bg-crit/10 px-3 py-2 text-sm text-crit">{reprintError}</p>}

      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onSearch={setSearch}
        searchPlaceholder="Search visit no, UHID or name…"
        rowKey={(row) => row.id}
        emptyMessage="No visits found."
      />

      {cancelling && <CancelVisitModal visit={cancelling} onClose={() => setCancelling(null)} />}
    </>
  )
}

function CancelVisitModal({ visit, onClose }: { visit: OpVisitListItem; onClose: () => void }) {
  const cancel = useCancelVisit()
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await cancel.mutateAsync({ visitId: visit.id, reason })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Cancel visit #${visit.tokenNumber}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Back
          </Button>
          <Button variant="danger" onClick={submit} loading={cancel.isPending} disabled={!reason.trim()}>
            Cancel visit
          </Button>
        </>
      }
    >
      {error && <p className="mb-3 rounded-shell-sm bg-crit/10 px-3 py-2 text-sm text-crit">{error}</p>}
      <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} />
    </Modal>
  )
}
