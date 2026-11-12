import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { PageHeader } from '../../../components/ui/PageHeader'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useRevokeSession, useSessions } from '../api'
import type { Session } from '../types'

export function SessionsPage() {
  const { hasPermission } = useAuth()
  const canRevoke = hasPermission('ADMIN.SESSION.REVOKE')
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [activeOnly, setActiveOnly] = useState(true)
  const { data, isLoading } = useSessions({ page, pageSize: 20, search: search || undefined, activeOnly })
  const revokeSession = useRevokeSession()

  const columns: Column<Session>[] = [
    {
      header: 'User',
      cell: (row) => (
        <span>
          <span className="font-medium">{row.fullName}</span>{' '}
          <span className="text-slate-400">({row.username})</span>
          {row.isCurrent && (
            <span className="ml-1.5">
              <StatusBadge tone="info">This session</StatusBadge>
            </span>
          )}
        </span>
      ),
    },
    { header: 'Branch', cell: (row) => row.branchName ?? row.branchId },
    { header: 'IP', cell: (row) => row.ipAddress ?? '—' },
    { header: 'Signed in', cell: (row) => new Date(row.loginAt).toLocaleString() },
    { header: 'Last activity', cell: (row) => new Date(row.lastActivityAt).toLocaleString() },
    {
      header: 'Status',
      cell: (row) =>
        row.isRevoked || row.loggedOutAt ? (
          <StatusBadge tone="neutral">Ended</StatusBadge>
        ) : (
          <StatusBadge tone="success">Active</StatusBadge>
        ),
    },
    {
      header: '',
      className: 'text-right',
      cell: (row) =>
        canRevoke && !row.isRevoked && !row.loggedOutAt && !row.isCurrent ? (
          <Button
            variant="ghost"
            className="text-red-600"
            onClick={async () => {
              if (window.confirm(`Force sign-out for '${row.username}'?`)) {
                try {
                  await revokeSession.mutateAsync(row.sessionId)
                } catch (err) {
                  window.alert(errorMessage(err))
                }
              }
            }}
          >
            Force sign-out
          </Button>
        ) : null,
    },
  ]

  return (
    <>
      <PageHeader title="Sessions" subtitle="Active sign-ins across all devices" />
      <DataTable
        columns={columns}
        result={data}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onSearch={setSearch}
        searchPlaceholder="Search user…"
        rowKey={(row) => row.id}
        toolbar={
          <label className="flex items-center gap-2 text-sm text-slate-600">
            <input
              type="checkbox"
              checked={activeOnly}
              onChange={(e) => {
                setActiveOnly(e.target.checked)
                setPage(1)
              }}
            />
            Active only
          </label>
        }
      />
    </>
  )
}
