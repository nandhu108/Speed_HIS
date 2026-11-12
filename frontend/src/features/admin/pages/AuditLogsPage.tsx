import { useState } from 'react'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuditLogs } from '../api'
import type { AuditLogEntry } from '../types'

const actionTone = { Created: 'success', Updated: 'info', Deleted: 'danger' } as const

function JsonBlock({ label, value }: { label: string; value: string | null }) {
  if (!value || value === '{}') return null
  let pretty = value
  try {
    pretty = JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    // show raw
  }
  return (
    <div>
      <p className="mb-1 text-xs font-semibold uppercase text-slate-400">{label}</p>
      <pre className="max-h-48 overflow-auto rounded-md bg-slate-50 p-3 text-xs text-slate-700">{pretty}</pre>
    </div>
  )
}

export function AuditLogsPage() {
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [detail, setDetail] = useState<AuditLogEntry | null>(null)
  const { data, isLoading } = useAuditLogs({ page, pageSize: 20, search: search || undefined })

  const columns: Column<AuditLogEntry>[] = [
    { header: 'When', cell: (row) => new Date(row.createdAt).toLocaleString() },
    { header: 'User', cell: (row) => row.username ?? row.userId },
    {
      header: 'Action',
      cell: (row) => (
        <StatusBadge tone={actionTone[row.action as keyof typeof actionTone] ?? 'neutral'}>{row.action}</StatusBadge>
      ),
    },
    { header: 'Entity', cell: (row) => `${row.entityName} #${row.entityId}` },
    {
      header: '',
      className: 'text-right',
      cell: (row) => (
        <button type="button" className="text-sm text-primary-600 hover:underline" onClick={() => setDetail(row)}>
          Details
        </button>
      ),
    },
  ]

  return (
    <>
      <PageHeader title="Audit Log" subtitle="Row-level change history (changed columns only)" />
      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onSearch={setSearch}
        searchPlaceholder="Search entity…"
        rowKey={(row) => row.id}
      />
      {detail && (
        <Modal open title={`${detail.action} — ${detail.entityName} #${detail.entityId}`} onClose={() => setDetail(null)}>
          <div className="space-y-3">
            <p className="text-xs text-slate-400">
              {new Date(detail.createdAt).toLocaleString()} · by {detail.username ?? detail.userId} · IP{' '}
              {detail.ipAddress ?? '—'} · trace {detail.traceId ?? '—'}
            </p>
            <JsonBlock label="Old values" value={detail.oldValues} />
            <JsonBlock label="New values" value={detail.newValues} />
          </div>
        </Modal>
      )}
    </>
  )
}
