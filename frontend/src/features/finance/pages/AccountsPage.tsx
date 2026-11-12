import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useAccounts, useDeleteAccount, useSaveAccount } from '../api'
import { ACCOUNT_TYPE_LABELS, type AccountDto, type AccountType } from '../types'

export function AccountsPage() {
  const navigate = useNavigate()
  const { hasPermission } = useAuth()
  const canManage = hasPermission('FINANCE.ACCOUNT.MANAGE')
  const { data: accounts = [], isLoading } = useAccounts()
  const remove = useDeleteAccount()
  const [editing, setEditing] = useState<AccountDto | 'new' | null>(null)
  const [error, setError] = useState<string | null>(null)

  return (
    <>
      <PageHeader
        title="Chart of Accounts"
        subtitle="Every account posted to by vouchers, payables and receivables"
        actions={canManage && <Button onClick={() => setEditing('new')}>New account</Button>}
      />
      {error && <p className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="rounded-lg border border-slate-200 bg-white">
        {isLoading && <Spinner />}
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-left">
              <th className="px-4 py-2.5 font-medium text-slate-600">Code</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Name</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Type</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">Parent</th>
              <th className="px-4 py-2.5 font-medium text-slate-600">System</th>
              {canManage && <th className="px-4 py-2.5 font-medium text-slate-600">Actions</th>}
            </tr>
          </thead>
          <tbody>
            {accounts.map((a) => (
              <tr
                key={a.id}
                className="cursor-pointer border-b border-slate-100 last:border-0 hover:bg-slate-50"
                onClick={() => navigate(`/finance/ledger/${a.id}`)}
              >
                <td className="px-4 py-2.5 font-mono text-xs text-slate-500">{a.code}</td>
                <td className="px-4 py-2.5 text-slate-700">{a.name}</td>
                <td className="px-4 py-2.5 text-slate-700">{ACCOUNT_TYPE_LABELS[a.accountType]}</td>
                <td className="px-4 py-2.5 text-slate-500">{a.parentAccountName ?? '—'}</td>
                <td className="px-4 py-2.5">{a.isSystemAccount && <StatusBadge tone="info">System</StatusBadge>}</td>
                {canManage && (
                  <td className="space-x-2 px-4 py-2.5" onClick={(e) => e.stopPropagation()}>
                    {!a.isSystemAccount && (
                      <>
                        <Button variant="ghost" onClick={() => setEditing(a)}>Edit</Button>
                        <Button
                          variant="ghost"
                          className="text-red-600"
                          onClick={async () => {
                            setError(null)
                            try {
                              await remove.mutateAsync(a.id)
                            } catch (err) {
                              setError(errorMessage(err))
                            }
                          }}
                        >
                          Delete
                        </Button>
                      </>
                    )}
                  </td>
                )}
              </tr>
            ))}
            {!isLoading && accounts.length === 0 && (
              <tr>
                <td colSpan={canManage ? 6 : 5} className="px-4 py-10 text-center text-slate-400">
                  No accounts yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editing && (
        <AccountModal account={editing === 'new' ? null : editing} accounts={accounts} onClose={() => setEditing(null)} />
      )}
    </>
  )
}

function AccountModal({ account, accounts, onClose }: { account: AccountDto | null; accounts: AccountDto[]; onClose: () => void }) {
  const save = useSaveAccount()
  const [code, setCode] = useState(account?.code ?? '')
  const [name, setName] = useState(account?.name ?? '')
  const [accountType, setAccountType] = useState<AccountType>(account?.accountType ?? 1)
  const [parentAccountId, setParentAccountId] = useState(account?.parentAccountId ? String(account.parentAccountId) : '')
  const [description, setDescription] = useState(account?.description ?? '')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    setError(null)
    try {
      await save.mutateAsync({
        id: account?.id, code, name, accountType,
        parentAccountId: parentAccountId ? Number(parentAccountId) : null, description: description || null,
      })
      onClose()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={account ? 'Edit account' : 'New account'}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={submit} loading={save.isPending} disabled={!code || !name}>Save</Button>
        </>
      }
    >
      <div className="space-y-3">
        {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
        <Input label="Code" value={code} onChange={(e) => setCode(e.target.value)} />
        <Input label="Name" value={name} onChange={(e) => setName(e.target.value)} />
        <Select
          label="Type"
          value={String(accountType)}
          onChange={(e) => setAccountType(Number(e.target.value) as AccountType)}
          options={[{ value: 1, label: 'Asset' }, { value: 2, label: 'Liability' }, { value: 3, label: 'Equity' }, { value: 4, label: 'Income' }, { value: 5, label: 'Expense' }]}
        />
        <Select
          label="Parent account (optional)"
          value={parentAccountId}
          onChange={(e) => setParentAccountId(e.target.value)}
          placeholder="None"
          options={accounts.filter((a) => a.id !== account?.id).map((a) => ({ value: a.id, label: `${a.code} — ${a.name}` }))}
        />
        <Input label="Description" value={description ?? ''} onChange={(e) => setDescription(e.target.value)} />
      </div>
    </Modal>
  )
}
