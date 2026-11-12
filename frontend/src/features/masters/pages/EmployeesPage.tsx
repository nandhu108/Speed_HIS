import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { endpoints } from '../../../api/endpoints'
import { Button } from '../../../components/ui/Button'
import { DataTable, type Column } from '../../../components/ui/DataTable'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useDesignations } from '../../admin/api'
import { useAuth } from '../../auth/AuthContext'
import { useDeleteEmployee, useEmployees, useSaveEmployee, useSimpleMaster } from '../api'
import type { Employee } from '../types'

interface EditorState {
  id?: number
  code: string
  fullName: string
  phone: string
  email: string
  designationId: string
  departmentId: string
  joinedOn: string
  isActive: boolean
}

export function EmployeesPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.EMPLOYEE.MANAGE')
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const { data: result, isLoading } = useEmployees({ page, pageSize: 20, search: search || undefined })
  const { data: designations = [] } = useDesignations()
  const { data: departments = [] } = useSimpleMaster('departments', endpoints.masters.departments)
  const save = useSaveEmployee()
  const remove = useDeleteEmployee()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const openEditor = (employee: Employee | null) => {
    setError(null)
    setEditor(
      employee
        ? {
            id: employee.id,
            code: employee.code,
            fullName: employee.fullName,
            phone: employee.phone ?? '',
            email: employee.email ?? '',
            designationId: employee.designationId ? String(employee.designationId) : '',
            departmentId: employee.departmentId ? String(employee.departmentId) : '',
            joinedOn: employee.joinedOn ?? '',
            isActive: employee.isActive,
          }
        : { code: '', fullName: '', phone: '', email: '', designationId: '', departmentId: '', joinedOn: '', isActive: true },
    )
  }

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({
        id: editor.id,
        code: editor.code,
        fullName: editor.fullName,
        phone: editor.phone || null,
        email: editor.email || null,
        designationId: editor.designationId ? Number(editor.designationId) : null,
        departmentId: editor.departmentId ? Number(editor.departmentId) : null,
        joinedOn: editor.joinedOn || null,
        isActive: editor.isActive,
      })
      setEditor(null)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const columns: Column<Employee>[] = [
    { header: 'Code', cell: (row) => <span className="font-mono text-xs text-slate-500">{row.code}</span> },
    { header: 'Name', cell: (row) => <span className="font-medium">{row.fullName}</span> },
    { header: 'Designation', cell: (row) => row.designationName ?? '—' },
    { header: 'Department', cell: (row) => row.departmentName ?? '—' },
    {
      header: 'Contact',
      cell: (row) => (
        <span className="text-slate-500">
          {row.phone ?? '—'}
          {row.email && <span className="block text-xs">{row.email}</span>}
        </span>
      ),
    },
    {
      header: 'Status',
      cell: (row) => (
        <StatusBadge tone={row.isActive ? 'success' : 'neutral'}>{row.isActive ? 'Active' : 'Inactive'}</StatusBadge>
      ),
    },
    {
      header: '',
      className: 'text-right',
      cell: (row) =>
        canManage && (
          <div className="flex justify-end gap-1">
            <Button variant="ghost" onClick={() => openEditor(row)}>
              Edit
            </Button>
            <Button
              variant="ghost"
              className="text-red-600"
              onClick={async () => {
                if (window.confirm(`Delete employee '${row.fullName}'?`)) {
                  try {
                    await remove.mutateAsync(row.id)
                  } catch (err) {
                    window.alert(errorMessage(err))
                  }
                }
              }}
            >
              Delete
            </Button>
          </div>
        ),
    },
  ]

  return (
    <>
      <PageHeader
        title="Employees"
        subtitle="Staff registry for the current branch"
        actions={canManage && <Button onClick={() => openEditor(null)}>New employee</Button>}
      />

      <DataTable
        columns={columns}
        result={result}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onSearch={setSearch}
        searchPlaceholder="Search employees…"
        rowKey={(row) => row.id}
        emptyMessage="No employees yet."
      />

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit employee' : 'New employee'}
          onClose={() => setEditor(null)}
          footer={
            <>
              <Button variant="secondary" onClick={() => setEditor(null)}>
                Cancel
              </Button>
              <Button onClick={submit} loading={save.isPending}>
                Save
              </Button>
            </>
          }
        >
          <div className="space-y-3">
            {error && <p className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}
            <div className="grid grid-cols-2 gap-3">
              <Input label="Code" value={editor.code} onChange={(e) => setEditor({ ...editor, code: e.target.value })} />
              <Input
                label="Full name"
                value={editor.fullName}
                onChange={(e) => setEditor({ ...editor, fullName: e.target.value })}
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <Select
                label="Designation"
                placeholder="— none —"
                options={designations.map((d) => ({ value: d.id, label: d.name }))}
                value={editor.designationId}
                onChange={(e) => setEditor({ ...editor, designationId: e.target.value })}
              />
              <Select
                label="Department"
                placeholder="— none —"
                options={departments.map((d) => ({ value: d.id, label: d.name }))}
                value={editor.departmentId}
                onChange={(e) => setEditor({ ...editor, departmentId: e.target.value })}
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <Input label="Phone" value={editor.phone} onChange={(e) => setEditor({ ...editor, phone: e.target.value })} />
              <Input
                label="Joined on"
                type="date"
                value={editor.joinedOn}
                onChange={(e) => setEditor({ ...editor, joinedOn: e.target.value })}
              />
            </div>
            <Input label="Email" type="email" value={editor.email} onChange={(e) => setEditor({ ...editor, email: e.target.value })} />
            <label className="flex items-center gap-2 text-sm text-slate-700">
              <input
                type="checkbox"
                checked={editor.isActive}
                onChange={(e) => setEditor({ ...editor, isActive: e.target.checked })}
              />
              Active
            </label>
          </div>
        </Modal>
      )}
    </>
  )
}
