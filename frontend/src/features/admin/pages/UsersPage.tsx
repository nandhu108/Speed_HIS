import { ChevronRight, Link2, Power, PowerOff, RotateCcw, Search as SearchIcon, ShieldCheck, UserPlus, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { endpoints } from '../../../api/endpoints'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { useSimpleMaster } from '../../masters/api'
import { MenuPermissionTree } from '../components/MenuPermissionTree'
import { PermissionMatrixModal } from '../components/PermissionMatrixModal'
import {
  useActivateUser,
  useBranches,
  useCreateUser,
  useDeactivateUser,
  useDeleteUser,
  useDesignations,
  usePermissionCatalogue,
  useResetPassword,
  useRolePermissions,
  useRolePresets,
  useRoles,
  useSaveRolePreset,
  useSetUserPermissionOverrides,
  useUpdateUser,
  useUser,
  useUserPermissions,
  useUsers,
} from '../api'
import type { Permission, PermissionOverride, Role, UserListItem } from '../types'

interface FormState {
  username: string
  password: string
  fullName: string
  email: string
  phone: string
  employeeCode: string
  designationId: string
  departmentId: string
  defaultBranchId: string
  isActive: boolean
  roleIds: number[]
  branchIds: number[]
}

const emptyForm: FormState = {
  username: '',
  password: '',
  fullName: '',
  email: '',
  phone: '',
  employeeCode: '',
  designationId: '',
  departmentId: '',
  defaultBranchId: '',
  isActive: true,
  roleIds: [],
  branchIds: [],
}

interface EmployeeGroup {
  key: string
  employeeCode: string | null
  users: UserListItem[]
}

/** Groups the current page's rows by Employee Code — accounts without a code stand alone. */
function groupByEmployee(items: UserListItem[]): EmployeeGroup[] {
  const groups: EmployeeGroup[] = []
  const byCode = new Map<string, EmployeeGroup>()
  for (const user of items) {
    if (!user.employeeCode) {
      groups.push({ key: `user-${user.id}`, employeeCode: null, users: [user] })
      continue
    }
    let group = byCode.get(user.employeeCode)
    if (!group) {
      group = { key: `emp-${user.employeeCode}`, employeeCode: user.employeeCode, users: [] }
      byCode.set(user.employeeCode, group)
      groups.push(group)
    }
    group.users.push(user)
  }
  return groups
}

/** Shared column template so the header labels and every data row line up like a real table.
 * The Actions column is a fixed width (not `auto`/`fr`) so it doesn't shrink or grow per-row
 * based on how many action buttons that particular row happens to render. */
const USER_ROW_COLS = 'grid grid-cols-[1.2fr_1.6fr_1fr_1fr_110px_100px_140px_380px] gap-3'
const COLUMN_LABEL = 'text-[10.5px] font-bold tracking-wider text-ink-faint uppercase'

export function UsersPage() {
  const { hasPermission } = useAuth()
  const [page, setPage] = useState(1)
  const [searchDraft, setSearchDraft] = useState('')
  const [search, setSearch] = useState('')
  const [departmentFilter, setDepartmentFilter] = useState('')
  const [roleFilter, setRoleFilter] = useState('')
  const [branchFilter, setBranchFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set())
  const { data, isLoading } = useUsers({
    page,
    pageSize: 20,
    search: search || undefined,
    departmentId: departmentFilter ? Number(departmentFilter) : undefined,
    roleId: roleFilter ? Number(roleFilter) : undefined,
    branchId: branchFilter ? Number(branchFilter) : undefined,
    isActive: statusFilter ? statusFilter === 'active' : undefined,
  })

  const [editorOpen, setEditorOpen] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [prefillSourceId, setPrefillSourceId] = useState<number | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)
  const [initialRoleIds, setInitialRoleIds] = useState<number[]>([])
  const [error, setError] = useState<string | null>(null)
  const editorScrollRef = useRef<HTMLDivElement>(null)
  const [resetTarget, setResetTarget] = useState<UserListItem | null>(null)
  const [resetPassword, setResetPasswordValue] = useState('')
  const [permissionsTarget, setPermissionsTarget] = useState<UserListItem | null>(null)
  const [roleMatrixTarget, setRoleMatrixTarget] = useState<Role | null>(null)
  const [roleOverrideTarget, setRoleOverrideTarget] = useState<Role | null>(null)
  const [copySearch, setCopySearch] = useState('')
  const [copySourceId, setCopySourceId] = useState<number | null>(null)
  const [presetToApply, setPresetToApply] = useState('')

  const { data: roles = [] } = useRoles()
  const { data: branches = [] } = useBranches()
  const { data: designations = [] } = useDesignations()
  const { data: departments = [] } = useSimpleMaster('departments', endpoints.masters.departments)
  const { data: presets = [] } = useRolePresets()
  const { data: editingUser } = useUser(editingId)
  const { data: prefillUser } = useUser(prefillSourceId)
  const { data: copyCandidates } = useUsers({ page: 1, pageSize: 8, search: copySearch || undefined })
  const { data: copySourceUser } = useUser(copySourceId)

  const createUser = useCreateUser()
  const updateUser = useUpdateUser()
  const deleteUser = useDeleteUser()
  const resetUserPassword = useResetPassword()
  const activateUser = useActivateUser()
  const deactivateUser = useDeactivateUser()
  const saveRolePreset = useSaveRolePreset()

  const canCreate = hasPermission('ADMIN.USER.CREATE')
  const canUpdate = hasPermission('ADMIN.USER.UPDATE')
  const canReset = hasPermission('ADMIN.USER.RESETPASSWORD')
  const canDelete = hasPermission('ADMIN.USER.DELETE')
  const canManagePermissions = hasPermission('ADMIN.USER.MANAGEPERMISSIONS')

  const resetCopyPresetState = () => {
    setCopySearch('')
    setCopySourceId(null)
    setPresetToApply('')
  }

  const openCreate = () => {
    setEditingId(null)
    setPrefillSourceId(null)
    setForm(emptyForm)
    setInitialRoleIds([])
    setError(null)
    resetCopyPresetState()
    setEditorOpen(true)
  }

  const openEdit = (id: number) => {
    setEditingId(id)
    setPrefillSourceId(null)
    setForm(emptyForm)
    setError(null)
    resetCopyPresetState()
    setEditorOpen(true)
  }

  /** Start a second (third, ...) login for someone who already has an account — same profile, blank username/password/roles. */
  const openAddLogin = (sourceUser: UserListItem) => {
    setEditingId(null)
    setPrefillSourceId(sourceUser.id)
    setForm(emptyForm)
    setInitialRoleIds([])
    setError(null)
    resetCopyPresetState()
    setEditorOpen(true)
  }

  // Copying roles from another user is a one-shot action: once the picked user's detail loads, apply their
  // roleIds and clear the picker so it doesn't re-apply on every re-render.
  useEffect(() => {
    if (copySourceUser && copySourceId === copySourceUser.id) {
      setForm((f) => ({ ...f, roleIds: copySourceUser.roleIds }))
      setCopySourceId(null)
      setCopySearch('')
    }
  }, [copySourceUser, copySourceId])

  const applyPreset = () => {
    const preset = presets.find((p) => p.id.toString() === presetToApply)
    if (!preset) return
    setForm((f) => ({ ...f, roleIds: [...new Set([...f.roleIds, ...preset.roleIds])] }))
    setPresetToApply('')
  }

  const saveCurrentRolesAsPreset = async () => {
    const name = window.prompt('Name this role combination (e.g. "Front Office Bundle"):')
    if (!name || !name.trim()) return
    try {
      await saveRolePreset.mutateAsync({ name: name.trim(), roleIds: form.roleIds })
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  // Populate the form once the detail query lands (either editing an existing account, or prefilling from a sibling account).
  if (editingId !== null && editingUser && form.fullName === '' && editingUser.id === editingId) {
    setForm({
      username: editingUser.username,
      password: '',
      fullName: editingUser.fullName,
      email: editingUser.email ?? '',
      phone: editingUser.phone ?? '',
      employeeCode: editingUser.employeeCode ?? '',
      designationId: editingUser.designationId?.toString() ?? '',
      departmentId: editingUser.departmentId?.toString() ?? '',
      defaultBranchId: editingUser.defaultBranchId?.toString() ?? '',
      isActive: editingUser.isActive,
      roleIds: editingUser.roleIds,
      branchIds: editingUser.branchIds,
    })
    setInitialRoleIds(editingUser.roleIds)
  }
  if (editingId === null && prefillSourceId !== null && prefillUser && form.fullName === '' && prefillUser.id === prefillSourceId) {
    setForm({
      ...emptyForm,
      fullName: prefillUser.fullName,
      email: prefillUser.email ?? '',
      phone: prefillUser.phone ?? '',
      employeeCode: prefillUser.employeeCode ?? '',
      designationId: prefillUser.designationId?.toString() ?? '',
      departmentId: prefillUser.departmentId?.toString() ?? '',
      defaultBranchId: prefillUser.defaultBranchId?.toString() ?? '',
      branchIds: prefillUser.branchIds,
    })
  }

  const submit = async () => {
    setError(null)
    const payload = {
      fullName: form.fullName,
      email: form.email || null,
      phone: form.phone || null,
      employeeCode: form.employeeCode || null,
      designationId: form.designationId ? Number(form.designationId) : null,
      departmentId: form.departmentId ? Number(form.departmentId) : null,
      defaultBranchId: form.defaultBranchId ? Number(form.defaultBranchId) : null,
      roleIds: form.roleIds,
      branchIds: form.branchIds,
    }
    try {
      if (editingId === null) {
        await createUser.mutateAsync({ ...payload, username: form.username, password: form.password })
      } else {
        await updateUser.mutateAsync({ ...payload, id: editingId, isActive: form.isActive })
      }
      setEditorOpen(false)
    } catch (err) {
      setError(errorMessage(err))
      editorScrollRef.current?.scrollTo({ top: 0, behavior: 'smooth' })
    }
  }

  const toggleId = (list: number[], id: number) =>
    list.includes(id) ? list.filter((value) => value !== id) : [...list, id]

  const setActive = async (user: UserListItem, isActive: boolean) => {
    try {
      await (isActive ? activateUser : deactivateUser).mutateAsync(user.id)
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  const roleByName = (name: string) => roles.find((r) => r.name === name) ?? null

  const hasActiveFilters = !!(departmentFilter || roleFilter || branchFilter || statusFilter)
  const clearFilters = () => {
    setDepartmentFilter('')
    setRoleFilter('')
    setBranchFilter('')
    setStatusFilter('')
    setPage(1)
  }
  const applyFilter = (setter: (value: string) => void) => (value: string) => {
    setter(value)
    setPage(1)
  }

  const toggleGroup = (key: string) =>
    setExpandedGroups((prev) => {
      const next = new Set(prev)
      if (next.has(key)) next.delete(key)
      else next.add(key)
      return next
    })

  const isAddingLogin = editingId === null && prefillSourceId !== null
  const modalTitle = editingId !== null ? 'Edit user' : isAddingLogin ? `Add login for ${prefillUser?.fullName ?? '…'}` : 'New user'

  const groups = data ? groupByEmployee(data.items) : []

  return (
    <>
      <PageHeader
        title="Users"
        subtitle="Staff accounts, roles and branch access — one real person can hold several logins (e.g. Front Office + Billing) grouped by Employee Code"
        actions={canCreate && <Button onClick={openCreate}>New user</Button>}
      />

      <div className="mb-4 rounded-shell border border-border-soft bg-panel p-4 shadow-shell-sm">
        <div className="flex flex-wrap items-end gap-3">
          <form
            className="flex w-full max-w-sm items-end gap-2"
            onSubmit={(e) => {
              e.preventDefault()
              setPage(1)
              setSearch(searchDraft)
            }}
          >
            <Input
              label="Search"
              className="flex-1"
              value={searchDraft}
              onChange={(e) => setSearchDraft(e.target.value)}
              placeholder="Username, name, email, employee code…"
            />
            <Button type="submit" title="Search">
              <SearchIcon className="size-4" aria-hidden />
            </Button>
          </form>

          <Select
            label="Department"
            className="w-60"
            placeholder="All departments"
            options={departments.map((d) => ({ value: d.id, label: d.name }))}
            value={departmentFilter}
            onChange={(e) => applyFilter(setDepartmentFilter)(e.target.value)}
          />
          <Select
            label="Role"
            className="w-52"
            placeholder="All roles"
            options={roles.map((r) => ({ value: r.id, label: r.name }))}
            value={roleFilter}
            onChange={(e) => applyFilter(setRoleFilter)(e.target.value)}
          />
          <Select
            label="Branch"
            className="w-52"
            placeholder="All branches"
            options={branches.map((b) => ({ value: b.id, label: b.name }))}
            value={branchFilter}
            onChange={(e) => applyFilter(setBranchFilter)(e.target.value)}
          />
          <Select
            label="Status"
            className="w-36"
            placeholder="All statuses"
            options={[
              { value: 'active', label: 'Active' },
              { value: 'inactive', label: 'Inactive' },
            ]}
            value={statusFilter}
            onChange={(e) => applyFilter(setStatusFilter)(e.target.value)}
          />
          {hasActiveFilters && (
            <button
              type="button"
              onClick={clearFilters}
              className="flex items-center gap-1 rounded-shell-sm px-2.5 py-2 text-[12.5px] font-semibold text-ink-faint hover:text-ink"
            >
              <X className="size-3.5" aria-hidden />
              Clear filters
            </button>
          )}
        </div>
      </div>

      {isLoading && <Spinner />}
      {!isLoading && groups.length === 0 && (
        <p className="rounded-shell border border-border-soft bg-panel py-10 text-center text-ink-faint">No users found.</p>
      )}

      <div className="overflow-x-auto">
        <div className="min-w-[1320px]">
          {!isLoading && groups.length > 0 && (
            <div className={`${USER_ROW_COLS} px-4 pb-1.5`}>
              <span className={COLUMN_LABEL}>User</span>
              <span className={COLUMN_LABEL}>Roles</span>
              <span className={COLUMN_LABEL}>Department</span>
              <span className={COLUMN_LABEL}>Designation</span>
              <span className={COLUMN_LABEL}>Emp. code</span>
              <span className={COLUMN_LABEL}>Status</span>
              <span className={COLUMN_LABEL}>Last login</span>
              <span className={`${COLUMN_LABEL} text-right`}>Actions</span>
            </div>
          )}

          <div className="flex flex-col gap-3">
            {groups.map((group) => {
          const isMulti = group.users.length > 1
          const isExpanded = !isMulti || expandedGroups.has(group.key)
          const uniqueRoles = isMulti ? [...new Set(group.users.flatMap((u) => u.roles))] : []
          return (
          <div key={group.key} className="overflow-hidden rounded-shell border border-border-soft bg-panel shadow-shell-sm">
            {isMulti && (
              <div className="flex flex-wrap items-center gap-2.5 border-b border-border-soft bg-panel-2 px-4 py-2.5">
                <button
                  type="button"
                  onClick={() => toggleGroup(group.key)}
                  title={isExpanded ? 'Collapse logins' : 'Expand logins'}
                  className="flex items-center gap-2 text-left"
                >
                  <ChevronRight
                    className={`size-4 shrink-0 text-ink-faint transition-transform ${isExpanded ? 'rotate-90' : ''}`}
                    aria-hidden
                  />
                  <span className="text-sm font-bold text-ink">{group.users[0].fullName}</span>
                </button>
                <span className="inline-flex items-center gap-1 rounded-full bg-accent-2/10 px-2 py-0.5 font-mono text-[11px] font-semibold text-accent-2">
                  <Link2 className="size-3" aria-hidden />
                  {group.employeeCode}
                </span>
                <span className="text-[11px] text-ink-faint">{group.users.length} logins</span>
                {!isExpanded && (
                  <div className="flex flex-wrap items-center gap-1">
                    {uniqueRoles.length === 0 && <span className="text-[11px] text-ink-faint">No roles</span>}
                    {uniqueRoles.map((roleName) => (
                      <span
                        key={roleName}
                        className="rounded-full border border-border-soft bg-panel-solid px-2 py-0.5 text-[10.5px] font-medium text-ink-muted"
                      >
                        {roleName}
                      </span>
                    ))}
                  </div>
                )}
                <div className="ml-auto flex items-center gap-3">
                  {canUpdate && (
                    <>
                      <button
                        type="button"
                        title="Activate every login for this person"
                        onClick={() => group.users.forEach((u) => !u.isActive && setActive(u, true))}
                        className="flex items-center gap-1 text-[12px] font-semibold text-ok hover:underline"
                      >
                        <Power className="size-3.5" aria-hidden />
                        Activate all
                      </button>
                      <button
                        type="button"
                        title="Deactivate every login for this person"
                        onClick={() => group.users.forEach((u) => u.isActive && setActive(u, false))}
                        className="flex items-center gap-1 text-[12px] font-semibold text-crit hover:underline"
                      >
                        <PowerOff className="size-3.5" aria-hidden />
                        Deactivate all
                      </button>
                    </>
                  )}
                  {canCreate && (
                    <button
                      type="button"
                      onClick={() => openAddLogin(group.users[0])}
                      className="flex items-center gap-1 text-[12px] font-semibold text-accent-2 hover:underline"
                    >
                      <UserPlus className="size-3.5" aria-hidden />
                      Add login
                    </button>
                  )}
                </div>
              </div>
            )}
            {isExpanded && (
            <div className="divide-y divide-border-soft">
              {group.users.map((user) => (
                <div key={user.id} className={`${USER_ROW_COLS} items-center px-4 py-3`}>
                  <div className="min-w-0">
                    <p className="truncate text-sm font-semibold text-ink">{user.username}</p>
                    {group.users.length === 1 && <p className="truncate text-[11px] text-ink-faint">{user.fullName}</p>}
                  </div>
                  <div className="flex flex-wrap items-center gap-1">
                    {user.roles.length === 0 && <span className="text-[12.5px] text-ink-faint">—</span>}
                    {user.roles.map((roleName) => {
                      const role = roleByName(roleName)
                      return role && canManagePermissions ? (
                        <button
                          key={roleName}
                          type="button"
                          title={`Edit module permissions for ${roleName}`}
                          onClick={() => setRoleMatrixTarget(role)}
                          className="rounded-full border border-border-soft bg-panel-2 px-2 py-0.5 text-[11px] font-medium text-ink-muted hover:border-accent-2/50 hover:text-accent-2"
                        >
                          {roleName}
                        </button>
                      ) : (
                        <span
                          key={roleName}
                          className="rounded-full border border-border-soft bg-panel-2 px-2 py-0.5 text-[11px] font-medium text-ink-muted"
                        >
                          {roleName}
                        </span>
                      )
                    })}
                  </div>
                  <p className="truncate text-[12.5px] text-ink-faint">{user.departmentName ?? '—'}</p>
                  <p className="truncate text-[12.5px] text-ink-faint">{user.designationName ?? '—'}</p>
                  <p className="truncate font-mono text-[12px] text-ink-faint">{user.employeeCode ?? '—'}</p>
                  <div>
                    {user.isLocked ? (
                      <StatusBadge tone="warning">Locked</StatusBadge>
                    ) : user.isActive ? (
                      <StatusBadge tone="success">Active</StatusBadge>
                    ) : (
                      <StatusBadge tone="neutral">Inactive</StatusBadge>
                    )}
                  </div>
                  <p className="truncate text-[11.5px] text-ink-faint">
                    {user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString() : 'Never logged in'}
                  </p>
                  <div className="flex flex-wrap items-center justify-end gap-1">
                    {group.users.length === 1 && canCreate && (
                      <Button variant="ghost" title="Add another login for this person" onClick={() => openAddLogin(user)}>
                        <UserPlus className="size-4" aria-hidden />
                      </Button>
                    )}
                    {canUpdate && user.id !== 1 && (
                      <Button
                        variant="ghost"
                        title={user.isActive ? 'Deactivate this login' : 'Activate this login'}
                        className={user.isActive ? 'text-crit' : 'text-ok'}
                        onClick={() => setActive(user, !user.isActive)}
                      >
                        {user.isActive ? <PowerOff className="size-4" aria-hidden /> : <Power className="size-4" aria-hidden />}
                      </Button>
                    )}
                    {canUpdate && (
                      <Button variant="ghost" onClick={() => openEdit(user.id)}>
                        Edit
                      </Button>
                    )}
                    {canManagePermissions && (
                      <Button variant="ghost" title="Grant/deny individual permissions for this login" onClick={() => setPermissionsTarget(user)}>
                        <ShieldCheck className="size-4" aria-hidden />
                      </Button>
                    )}
                    {canReset && (
                      <Button variant="ghost" onClick={() => setResetTarget(user)}>
                        Reset password
                      </Button>
                    )}
                    {canDelete && user.id !== 1 && (
                      <Button
                        variant="ghost"
                        className="text-crit"
                        onClick={async () => {
                          if (window.confirm(`Delete user '${user.username}'?`)) {
                            try {
                              await deleteUser.mutateAsync(user.id)
                            } catch (err) {
                              window.alert(errorMessage(err))
                            }
                          }
                        }}
                      >
                        Delete
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
            )}
          </div>
          )
        })}
          </div>
        </div>
      </div>

      {data && data.totalPages > 1 && (
        <div className="mt-4 flex items-center justify-between text-sm text-ink-muted">
          <span>
            Page {data.page} of {data.totalPages} · {data.totalCount} users
          </span>
          <div className="flex gap-1">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
              className="rounded-shell-sm border border-border-soft px-3 py-1 hover:border-border-strong disabled:opacity-40"
            >
              Previous
            </button>
            <button
              type="button"
              disabled={page >= data.totalPages}
              onClick={() => setPage((p) => p + 1)}
              className="rounded-shell-sm border border-border-soft px-3 py-1 hover:border-border-strong disabled:opacity-40"
            >
              Next
            </button>
          </div>
        </div>
      )}

      <Modal
        open={editorOpen}
        title={modalTitle}
        onClose={() => setEditorOpen(false)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setEditorOpen(false)}>
              Cancel
            </Button>
            <Button onClick={submit} loading={createUser.isPending || updateUser.isPending}>
              Save
            </Button>
          </>
        }
      >
        <div ref={editorScrollRef} className="grid max-h-[60vh] grid-cols-2 gap-3 overflow-y-auto pr-1">
          {error && (
            <p className="col-span-2 animate-pulse rounded-shell-sm bg-crit/10 px-3 py-2 text-sm font-medium text-crit">
              {error}
            </p>
          )}
          {isAddingLogin && (
            <p className="col-span-2 rounded-shell-sm bg-accent-2/10 px-3 py-2 text-xs text-ink-muted">
              Profile details are copied from {prefillUser?.fullName}. Pick a new username, password and the roles this
              login should have — it won't inherit the other account's roles.
            </p>
          )}
          {editingId === null && (
            <>
              <Input
                label="Username"
                value={form.username}
                onChange={(e) => setForm({ ...form, username: e.target.value })}
              />
              <Input
                label="Initial password"
                type="password"
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
              />
            </>
          )}
          <Input
            label="Full name"
            className="col-span-2"
            value={form.fullName}
            onChange={(e) => setForm({ ...form, fullName: e.target.value })}
          />
          <Input label="Email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
          <Input label="Phone" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
          <Input
            label="Employee code"
            value={form.employeeCode}
            onChange={(e) => setForm({ ...form, employeeCode: e.target.value })}
          />
          <Select
            label="Designation"
            placeholder="— None —"
            options={designations.map((d) => ({ value: d.id, label: d.name }))}
            value={form.designationId}
            onChange={(e) => setForm({ ...form, designationId: e.target.value })}
          />
          <Select
            label="Department"
            placeholder="— None —"
            options={departments.map((d) => ({ value: d.id, label: d.name }))}
            value={form.departmentId}
            onChange={(e) => setForm({ ...form, departmentId: e.target.value })}
          />
          <div className="col-span-2">
            <div className="mb-1.5 flex items-center justify-between">
              <span className="text-sm font-medium text-ink">Roles</span>
              <div className="flex items-center gap-2">
                {(() => {
                  const added = form.roleIds.filter((id) => !initialRoleIds.includes(id)).length
                  const removed = initialRoleIds.filter((id) => !form.roleIds.includes(id)).length
                  return (added > 0 || removed > 0) ? (
                    <span className="text-[11px] font-medium">
                      {added > 0 && <span className="text-ok">+{added} added</span>}
                      {added > 0 && removed > 0 && ' · '}
                      {removed > 0 && <span className="text-crit">−{removed} removed</span>}
                    </span>
                  ) : null
                })()}
                <button
                  type="button"
                  title="Revert roles back to what they were when this form opened"
                  onClick={() => setForm((f) => ({ ...f, roleIds: initialRoleIds }))}
                  className="flex items-center gap-1 text-[11px] font-semibold text-ink-faint hover:text-ink"
                >
                  <RotateCcw className="size-3" aria-hidden />
                  Reset
                </button>
              </div>
            </div>

            <div className="mb-2 flex flex-wrap items-center gap-2 rounded-shell-sm border border-border-soft bg-panel-2 p-2">
              <div className="relative min-w-[180px] flex-1">
                <Input
                  value={copySearch}
                  onChange={(e) => setCopySearch(e.target.value)}
                  placeholder="Copy roles from another user…"
                />
                {copySearch && copyCandidates && copyCandidates.items.length > 0 && (
                  <div className="absolute z-10 mt-1 w-full rounded-shell-sm border border-border-soft bg-panel-solid shadow-shell-md">
                    {copyCandidates.items.map((u) => (
                      <button
                        key={u.id}
                        type="button"
                        onClick={() => setCopySourceId(u.id)}
                        className="flex w-full items-center justify-between px-3 py-1.5 text-left text-[12.5px] hover:bg-panel-2"
                      >
                        <span className="text-ink">{u.fullName}</span>
                        <span className="font-mono text-ink-faint">{u.username}</span>
                      </button>
                    ))}
                  </div>
                )}
              </div>
              {presets.length > 0 && (
                <>
                  <Select
                    placeholder="Apply a saved preset…"
                    options={presets.map((p) => ({ value: p.id, label: `${p.name} (${p.roleNames.join(', ')})` }))}
                    value={presetToApply}
                    onChange={(e) => setPresetToApply(e.target.value)}
                  />
                  <Button variant="secondary" disabled={!presetToApply} onClick={applyPreset}>
                    Apply
                  </Button>
                </>
              )}
              <Button
                variant="ghost"
                disabled={form.roleIds.length === 0}
                loading={saveRolePreset.isPending}
                onClick={saveCurrentRolesAsPreset}
              >
                Save as preset
              </Button>
            </div>

            {canManagePermissions && (
              <p className="mb-1.5 text-[11px] text-ink-faint">
                Double-click a role to grant/deny its individual permissions for this user only — the role itself is
                unchanged.
              </p>
            )}
            <div className="flex flex-wrap gap-2">
              {roles.map((role) => {
                const checked = form.roleIds.includes(role.id)
                const wasChecked = initialRoleIds.includes(role.id)
                const changed = checked !== wasChecked
                return (
                  <label
                    key={role.id}
                    title={canManagePermissions && editingId !== null ? `Double-click to fine-tune ${role.name}'s permissions for this user` : undefined}
                    onDoubleClick={() => {
                      if (!canManagePermissions) return
                      if (editingId === null) {
                        window.alert('Save this user first — per-user permission overrides need a saved account to attach to.')
                        return
                      }
                      setRoleOverrideTarget(role)
                    }}
                    className={`flex items-center gap-1.5 rounded-shell-sm border px-2 py-1 text-sm text-ink select-none ${
                      changed
                        ? checked
                          ? 'border-ok/50 bg-ok/10'
                          : 'border-crit/50 bg-crit/10'
                        : 'border-border-soft'
                    }`}
                  >
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={() => setForm({ ...form, roleIds: toggleId(form.roleIds, role.id) })}
                    />
                    {role.name}
                  </label>
                )
              })}
            </div>
          </div>
          <div className="col-span-2">
            <span className="mb-1 block text-sm font-medium text-ink">Branches</span>
            <div className="flex flex-wrap gap-2">
              {branches.map((branch) => (
                <label key={branch.id} className="flex items-center gap-1.5 rounded-shell-sm border border-border-soft px-2 py-1 text-sm text-ink">
                  <input
                    type="checkbox"
                    checked={form.branchIds.includes(branch.id)}
                    onChange={() => setForm({ ...form, branchIds: toggleId(form.branchIds, branch.id) })}
                  />
                  {branch.name}
                </label>
              ))}
            </div>
          </div>
          <Select
            label="Default branch"
            placeholder="— First mapped branch —"
            options={branches
              .filter((branch) => form.branchIds.includes(branch.id))
              .map((branch) => ({ value: branch.id, label: branch.name }))}
            value={form.defaultBranchId}
            onChange={(e) => setForm({ ...form, defaultBranchId: e.target.value })}
          />
          {editingId !== null && (
            <label className="flex items-end gap-2 pb-2 text-sm text-ink">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
              />
              Active
            </label>
          )}
        </div>
      </Modal>

      <Modal
        open={resetTarget !== null}
        title={`Reset password — ${resetTarget?.username}`}
        onClose={() => {
          setResetTarget(null)
          setResetPasswordValue('')
        }}
        footer={
          <>
            <Button variant="secondary" onClick={() => setResetTarget(null)}>
              Cancel
            </Button>
            <Button
              loading={resetUserPassword.isPending}
              onClick={async () => {
                if (!resetTarget) return
                try {
                  await resetUserPassword.mutateAsync({ id: resetTarget.id, newPassword: resetPassword })
                  setResetTarget(null)
                  setResetPasswordValue('')
                } catch (err) {
                  window.alert(errorMessage(err))
                }
              }}
            >
              Reset
            </Button>
          </>
        }
      >
        <Input
          label="New password"
          type="password"
          value={resetPassword}
          onChange={(e) => setResetPasswordValue(e.target.value)}
        />
        <p className="mt-2 text-xs text-ink-faint">The user must change this password at their next sign-in.</p>
      </Modal>

      {permissionsTarget && (
        <UserPermissionsModal user={permissionsTarget} onClose={() => setPermissionsTarget(null)} />
      )}

      {roleMatrixTarget && (
        <PermissionMatrixModal role={roleMatrixTarget} onClose={() => setRoleMatrixTarget(null)} />
      )}

      {roleOverrideTarget && editingId !== null && (
        <RoleScopedOverridesModal
          userId={editingId}
          username={editingUser?.username ?? form.username}
          role={roleOverrideTarget}
          onClose={() => setRoleOverrideTarget(null)}
        />
      )}
    </>
  )
}

type OverrideState = 'default' | 'grant' | 'deny'

function ThreeWayToggle({ state, onSet }: { state: OverrideState; onSet: (s: OverrideState) => void }) {
  return (
    <div className="flex shrink-0 gap-1 rounded-shell-sm border border-border-soft bg-panel-2 p-0.5">
      <button
        type="button"
        onClick={() => onSet('default')}
        className={`rounded-[4px] px-2 py-0.5 text-[11px] font-medium ${
          state === 'default' ? 'bg-panel-solid text-ink shadow-shell-sm' : 'text-ink-faint'
        }`}
      >
        Default
      </button>
      <button
        type="button"
        onClick={() => onSet('grant')}
        className={`rounded-[4px] px-2 py-0.5 text-[11px] font-medium ${state === 'grant' ? 'bg-ok text-white' : 'text-ink-faint'}`}
      >
        Grant
      </button>
      <button
        type="button"
        onClick={() => onSet('deny')}
        className={`rounded-[4px] px-2 py-0.5 text-[11px] font-medium ${state === 'deny' ? 'bg-crit text-white' : 'text-ink-faint'}`}
      >
        Deny
      </button>
    </div>
  )
}

type PermissionsView = 'module' | 'menu'

function UserPermissionsModal({ user, onClose }: { user: UserListItem; onClose: () => void }) {
  const { data: catalogue = [] } = usePermissionCatalogue()
  const { data, isLoading } = useUserPermissions(user.id)
  const setOverrides = useSetUserPermissionOverrides()
  const [view, setView] = useState<PermissionsView>('module')

  const [initial, setInitial] = useState<Record<string, OverrideState>>({})
  const [current, setCurrent] = useState<Record<string, OverrideState>>({})

  useEffect(() => {
    if (!data) return
    const map: Record<string, OverrideState> = {}
    for (const o of data.overrides) map[o.permissionCode] = o.isGranted ? 'grant' : 'deny'
    setInitial(map)
    setCurrent(map)
  }, [data])

  const byModule = new Map<string, Permission[]>()
  for (const p of catalogue) {
    const list = byModule.get(p.module) ?? []
    list.push(p)
    byModule.set(p.module, list)
  }

  const rolePermissionCodes = new Set(data?.rolePermissionCodes ?? [])
  const stateOf = (code: string): OverrideState => current[code] ?? 'default'
  const setState = (code: string, state: OverrideState) => {
    setCurrent((prev) => {
      const next = { ...prev }
      if (state === 'default') delete next[code]
      else next[code] = state
      return next
    })
  }

  const changedCodes = Object.keys({ ...initial, ...current }).filter((code) => (initial[code] ?? 'default') !== (current[code] ?? 'default'))
  const grantedCount = Object.values(current).filter((s) => s === 'grant').length
  const deniedCount = Object.values(current).filter((s) => s === 'deny').length

  const save = async () => {
    try {
      const overrides = Object.entries(current).map(([permissionCode, state]) => ({
        permissionCode,
        isGranted: state === 'grant',
      }))
      await setOverrides.mutateAsync({ userId: user.id, overrides })
      onClose()
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`Permission overrides — ${user.username}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={save} loading={setOverrides.isPending}>
            Save
          </Button>
        </>
      }
    >
      <div className="mb-3 flex flex-wrap items-center gap-2 text-xs">
        <span className="text-ink-muted">
          Roles: <span className="font-medium text-ink">{user.roles.join(', ') || '—'}</span>
        </span>
        {changedCodes.length > 0 && (
          <span className="ml-auto font-medium">
            {grantedCount > 0 && <span className="text-ok">+{grantedCount} granted</span>}
            {grantedCount > 0 && deniedCount > 0 && ' · '}
            {deniedCount > 0 && <span className="text-crit">−{deniedCount} denied</span>}
            {' · '}
            {changedCodes.length} unsaved change{changedCodes.length === 1 ? '' : 's'}
          </span>
        )}
      </div>
      <p className="mb-3 rounded-shell-sm bg-accent-2/10 px-3 py-2 text-xs text-ink-muted">
        <strong className="text-ink">Via role</strong> = granted by one of this user's roles. <strong className="text-ink">Grant</strong>{' '}
        adds a permission on top of their roles. <strong className="text-ink">Deny</strong> takes one away even if a role grants it.
      </p>

      <div className="mb-3 flex gap-1 rounded-shell-sm border border-border-soft bg-panel-2 p-0.5">
        <button
          type="button"
          onClick={() => setView('module')}
          className={`rounded-[4px] px-2.5 py-1 text-xs font-medium ${view === 'module' ? 'bg-panel-solid text-ink shadow-shell-sm' : 'text-ink-faint'}`}
        >
          By module
        </button>
        <button
          type="button"
          onClick={() => setView('menu')}
          className={`rounded-[4px] px-2.5 py-1 text-xs font-medium ${view === 'menu' ? 'bg-panel-solid text-ink shadow-shell-sm' : 'text-ink-faint'}`}
        >
          By menu
        </button>
      </div>

      {isLoading && <Spinner />}

      {view === 'module' ? (
        <div className="max-h-[50vh] space-y-4 overflow-y-auto pr-1">
          {[...byModule.entries()].map(([module, permissions]) => (
            <div key={module}>
              <h3 className="mb-1.5 text-sm font-semibold text-ink">{module}</h3>
              <div className="space-y-1">
                {permissions.map((permission) => {
                  const fromRole = rolePermissionCodes.has(permission.code)
                  const state = stateOf(permission.code)
                  const changed = (initial[permission.code] ?? 'default') !== state
                  const effectivelyGranted = state === 'grant' || (state === 'default' && fromRole)
                  const badgeTone = state === 'deny' ? 'danger' : effectivelyGranted ? 'success' : 'neutral'
                  const badgeLabel = state === 'deny' ? 'Denied' : effectivelyGranted ? 'Granted' : 'Not granted'
                  return (
                    <div
                      key={permission.code}
                      className={`flex items-center gap-2 rounded-shell-sm px-2 py-1.5 ${
                        changed ? (state === 'grant' ? 'bg-ok/10' : state === 'deny' ? 'bg-crit/10' : 'bg-warn/10') : ''
                      }`}
                    >
                      <StatusBadge tone={badgeTone}>{badgeLabel}</StatusBadge>
                      <span className="min-w-0 flex-1 text-sm text-ink" title={permission.description ?? permission.code}>
                        {permission.resource}.{permission.action}
                        {fromRole && <span className="ml-1.5 text-[10px] font-medium text-ink-faint">via role</span>}
                      </span>
                      <ThreeWayToggle state={state} onSet={(s) => setState(permission.code, s)} />
                    </div>
                  )
                })}
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="max-h-[50vh] overflow-y-auto pr-1">
          <MenuPermissionTree
            isGranted={(code) => {
              const state = stateOf(code)
              return state === 'grant' || (state === 'default' && rolePermissionCodes.has(code))
            }}
            renderControl={(code) => <ThreeWayToggle state={stateOf(code)} onSet={(s) => setState(code, s)} />}
          />
        </div>
      )}
    </Modal>
  )
}

/**
 * Same Default/Grant/Deny mechanics as UserPermissionsModal, but scoped to a single role's
 * permission set — reachable by double-clicking that role in the Edit User modal. Saving here
 * only touches this user's overrides (never the role itself), and preserves any existing
 * overrides for permissions outside this role's scope.
 */
function RoleScopedOverridesModal({
  userId, username, role, onClose,
}: { userId: number; username: string; role: Role; onClose: () => void }) {
  const { data: catalogue = [] } = usePermissionCatalogue()
  const { data: rolePermissionCodes = [] } = useRolePermissions(role.id)
  const { data, isLoading } = useUserPermissions(userId)
  const setOverrides = useSetUserPermissionOverrides()

  const [initial, setInitial] = useState<Record<string, OverrideState>>({})
  const [current, setCurrent] = useState<Record<string, OverrideState>>({})
  const [otherOverrides, setOtherOverrides] = useState<PermissionOverride[]>([])

  const scopedCodes = new Set(rolePermissionCodes)

  useEffect(() => {
    if (!data) return
    const codes = new Set(rolePermissionCodes)
    const map: Record<string, OverrideState> = {}
    for (const o of data.overrides) {
      if (codes.has(o.permissionCode)) map[o.permissionCode] = o.isGranted ? 'grant' : 'deny'
    }
    setInitial(map)
    setCurrent(map)
    setOtherOverrides(data.overrides.filter((o) => !codes.has(o.permissionCode)))
  }, [data, rolePermissionCodes])

  const scopedPermissions = catalogue.filter((p) => scopedCodes.has(p.code))
  const stateOf = (code: string): OverrideState => current[code] ?? 'default'
  const setState = (code: string, state: OverrideState) => {
    setCurrent((prev) => {
      const next = { ...prev }
      if (state === 'default') delete next[code]
      else next[code] = state
      return next
    })
  }

  const changedCodes = Object.keys({ ...initial, ...current }).filter((code) => (initial[code] ?? 'default') !== (current[code] ?? 'default'))

  const save = async () => {
    try {
      const scopedOverrides = Object.entries(current).map(([permissionCode, state]) => ({
        permissionCode,
        isGranted: state === 'grant',
      }))
      await setOverrides.mutateAsync({ userId, overrides: [...otherOverrides, ...scopedOverrides] })
      onClose()
    } catch (err) {
      window.alert(errorMessage(err))
    }
  }

  return (
    <Modal
      open
      title={`${role.name} permissions — ${username}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={save} loading={setOverrides.isPending}>
            Save
          </Button>
        </>
      }
    >
      <p className="mb-3 rounded-shell-sm bg-accent-2/10 px-3 py-2 text-xs text-ink-muted">
        These are the permissions the <strong className="text-ink">{role.name}</strong> role grants. Denying one here
        only affects <strong className="text-ink">{username}</strong> — the role itself, and everyone else who holds
        it, is unchanged.
      </p>
      {changedCodes.length > 0 && (
        <p className="mb-3 text-xs font-medium text-ink">{changedCodes.length} unsaved change{changedCodes.length === 1 ? '' : 's'}</p>
      )}

      {isLoading && <Spinner />}

      <div className="max-h-[50vh] space-y-1 overflow-y-auto pr-1">
        {scopedPermissions.map((permission) => {
          const state = stateOf(permission.code)
          const changed = (initial[permission.code] ?? 'default') !== state
          const effectivelyGranted = state !== 'deny'
          return (
            <div
              key={permission.code}
              className={`flex items-center gap-2 rounded-shell-sm px-2 py-1.5 ${
                changed ? (state === 'grant' ? 'bg-ok/10' : state === 'deny' ? 'bg-crit/10' : 'bg-warn/10') : ''
              }`}
            >
              <StatusBadge tone={effectivelyGranted ? 'success' : 'danger'}>
                {effectivelyGranted ? 'Granted' : 'Denied'}
              </StatusBadge>
              <span className="min-w-0 flex-1 text-sm text-ink" title={permission.description ?? permission.code}>
                {permission.resource}.{permission.action}
              </span>
              <ThreeWayToggle state={state} onSet={(s) => setState(permission.code, s)} />
            </div>
          )
        })}
      </div>
    </Modal>
  )
}
