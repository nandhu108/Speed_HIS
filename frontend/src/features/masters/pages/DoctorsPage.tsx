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
import { useUsers } from '../../admin/api'
import { useAuth } from '../../auth/AuthContext'
import { useDeleteDoctor, useDoctor, useDoctors, useSaveDoctor, useSimpleMaster } from '../api'
import type { DoctorListItem } from '../types'

interface EditorState {
  id?: number
  userId: string
  registrationNo: string
  specialtyId: string
  qualification: string
  consultationFee: string
  followUpFee: string
  followUpWindowDays: string
  defaultSlotMinutes: string
  departmentIds: number[]
  isActive: boolean
}

const emptyEditor: EditorState = {
  userId: '',
  registrationNo: '',
  specialtyId: '',
  qualification: '',
  consultationFee: '0',
  followUpFee: '0',
  followUpWindowDays: '7',
  defaultSlotMinutes: '15',
  departmentIds: [],
  isActive: true,
}

export function DoctorsPage() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('MASTER.DOCTOR.MANAGE')
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const { data: result, isLoading } = useDoctors({ page, pageSize: 20, search: search || undefined })
  const { data: users } = useUsers({ page: 1, pageSize: 100 })
  const { data: specialties = [] } = useSimpleMaster('specialties', endpoints.masters.specialties)
  const { data: departments = [] } = useSimpleMaster('departments', endpoints.masters.departments)
  const save = useSaveDoctor()
  const remove = useDeleteDoctor()

  const [editor, setEditor] = useState<EditorState | null>(null)
  const [editId, setEditId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  // Edit needs the detail DTO (departmentIds etc.), so fetch it when a row is picked.
  const { data: detail } = useDoctor(editId)
  if (editId !== null && detail && editor === null) {
    setEditor({
      id: detail.id,
      userId: String(detail.userId),
      registrationNo: detail.registrationNo,
      specialtyId: detail.specialtyId ? String(detail.specialtyId) : '',
      qualification: detail.qualification ?? '',
      consultationFee: String(detail.consultationFee),
      followUpFee: String(detail.followUpFee),
      followUpWindowDays: String(detail.followUpWindowDays),
      defaultSlotMinutes: String(detail.defaultSlotMinutes),
      departmentIds: [...detail.departmentIds],
      isActive: detail.isActive,
    })
  }

  const closeEditor = () => {
    setEditor(null)
    setEditId(null)
  }

  const submit = async () => {
    if (!editor) return
    try {
      await save.mutateAsync({
        id: editor.id,
        userId: Number(editor.userId),
        registrationNo: editor.registrationNo,
        specialtyId: editor.specialtyId ? Number(editor.specialtyId) : null,
        qualification: editor.qualification || null,
        consultationFee: Number(editor.consultationFee),
        followUpFee: Number(editor.followUpFee),
        followUpWindowDays: Number(editor.followUpWindowDays),
        defaultSlotMinutes: Number(editor.defaultSlotMinutes),
        departmentIds: editor.departmentIds,
        isActive: editor.isActive,
      })
      closeEditor()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const toggleDepartment = (id: number) => {
    if (!editor) return
    setEditor({
      ...editor,
      departmentIds: editor.departmentIds.includes(id)
        ? editor.departmentIds.filter((d) => d !== id)
        : [...editor.departmentIds, id],
    })
  }

  const columns: Column<DoctorListItem>[] = [
    { header: 'Name', cell: (row) => <span className="font-medium">{row.fullName}</span> },
    { header: 'Reg. no', cell: (row) => <span className="font-mono text-xs text-slate-500">{row.registrationNo}</span> },
    { header: 'Specialty', cell: (row) => row.specialtyName ?? '—' },
    { header: 'Departments', cell: (row) => (row.departments.length ? row.departments.join(', ') : '—') },
    { header: 'Consult fee', cell: (row) => row.consultationFee.toLocaleString() },
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
            <Button
              variant="ghost"
              onClick={() => {
                setError(null)
                setEditor(null)
                setEditId(row.id)
              }}
            >
              Edit
            </Button>
            <Button
              variant="ghost"
              className="text-red-600"
              onClick={async () => {
                if (window.confirm(`Delete doctor '${row.fullName}'?`)) {
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
        title="Doctors"
        subtitle="Doctor profiles linked to user accounts"
        actions={
          canManage && (
            <Button
              onClick={() => {
                setError(null)
                setEditId(null)
                setEditor({ ...emptyEditor })
              }}
            >
              New doctor
            </Button>
          )
        }
      />

      <DataTable
        columns={columns}
        result={result}
        isLoading={isLoading}
        page={page}
        onPageChange={setPage}
        onSearch={setSearch}
        searchPlaceholder="Search doctors…"
        rowKey={(row) => row.id}
        emptyMessage="No doctors yet."
      />

      {editor && (
        <Modal
          open
          title={editor.id ? 'Edit doctor' : 'New doctor'}
          onClose={closeEditor}
          footer={
            <>
              <Button variant="secondary" onClick={closeEditor}>
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
            <Select
              label="User account"
              placeholder="Select user…"
              options={(users?.items ?? []).map((u) => ({ value: u.id, label: `${u.fullName} (${u.username})` }))}
              value={editor.userId}
              onChange={(e) => setEditor({ ...editor, userId: e.target.value })}
            />
            <div className="grid grid-cols-2 gap-3">
              <Input
                label="Registration no"
                value={editor.registrationNo}
                onChange={(e) => setEditor({ ...editor, registrationNo: e.target.value })}
              />
              <Select
                label="Specialty"
                placeholder="— none —"
                options={specialties.map((s) => ({ value: s.id, label: s.name }))}
                value={editor.specialtyId}
                onChange={(e) => setEditor({ ...editor, specialtyId: e.target.value })}
              />
            </div>
            <Input
              label="Qualification"
              value={editor.qualification}
              onChange={(e) => setEditor({ ...editor, qualification: e.target.value })}
            />
            <div className="grid grid-cols-2 gap-3">
              <Input
                label="Consultation fee"
                type="number"
                min={0}
                step="0.01"
                value={editor.consultationFee}
                onChange={(e) => setEditor({ ...editor, consultationFee: e.target.value })}
              />
              <Input
                label="Follow-up fee"
                type="number"
                min={0}
                step="0.01"
                value={editor.followUpFee}
                onChange={(e) => setEditor({ ...editor, followUpFee: e.target.value })}
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <Input
                label="Follow-up window (days)"
                type="number"
                min={0}
                value={editor.followUpWindowDays}
                onChange={(e) => setEditor({ ...editor, followUpWindowDays: e.target.value })}
              />
              <Input
                label="Default slot (minutes)"
                type="number"
                min={5}
                value={editor.defaultSlotMinutes}
                onChange={(e) => setEditor({ ...editor, defaultSlotMinutes: e.target.value })}
              />
            </div>
            <div>
              <span className="mb-1 block text-sm font-medium text-slate-700">Departments</span>
              <div className="grid max-h-40 grid-cols-2 gap-1 overflow-y-auto rounded-md border border-slate-200 p-2">
                {departments.map((d) => (
                  <label key={d.id} className="flex items-center gap-2 text-sm text-slate-700">
                    <input
                      type="checkbox"
                      checked={editor.departmentIds.includes(d.id)}
                      onChange={() => toggleDepartment(d.id)}
                    />
                    {d.name}
                  </label>
                ))}
                {departments.length === 0 && <span className="text-sm text-slate-400">No departments defined.</span>}
              </div>
            </div>
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
