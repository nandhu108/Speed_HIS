import { useState } from 'react'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { PageHeader } from '../../../components/ui/PageHeader'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useActivityLogs } from '../api'
import type { ActivityLogEntry } from '../types'

const eventTones: Record<string, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  LOGIN: 'success',
  LOGOUT: 'neutral',
  LOGIN_FAILED: 'warning',
  ACCOUNT_LOCKED: 'danger',
  TOKEN_REUSE_DETECTED: 'danger',
  PASSWORD_CHANGED: 'info',
  PASSWORD_RESET: 'info',
  SESSION_REVOKED: 'warning',
}

export function ActivityLogsPage() {
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const { data, isLoading } = useActivityLogs({ page, pageSize: 20, search: search || undefined })

  const columns: Column<ActivityLogEntry>[] = [
    { header: 'When', cell: (row) => new Date(row.createdAt).toLocaleString() },
    {
      header: 'Event',
      cell: (row) => <StatusBadge tone={eventTones[row.eventType] ?? 'neutral'}>{row.eventType}</StatusBadge>,
    },
    { header: 'User', cell: (row) => row.username ?? '—' },
    { header: 'Description', cell: (row) => row.description },
    { header: 'IP', cell: (row) => row.ipAddress ?? '—' },
  ]

  return (
    <>
      <PageHeader title="Activity Log" subtitle="Security and business events (logins, lockouts, permission changes)" />
      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onSearch={setSearch}
        searchPlaceholder="Search user or description…"
        rowKey={(row) => row.id}
      />
    </>
  )
}
