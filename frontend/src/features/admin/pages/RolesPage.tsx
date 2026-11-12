import { useState } from 'react'
import { errorMessage } from '../../../api/axios'
import { Button } from '../../../components/ui/Button'
import { Input } from '../../../components/ui/Input'
import { Modal } from '../../../components/ui/Modal'
import { PageHeader } from '../../../components/ui/PageHeader'
import { Select } from '../../../components/ui/Select'
import { Spinner } from '../../../components/ui/Spinner'
import { StatusBadge } from '../../../components/ui/StatusBadge'
import { useAuth } from '../../auth/AuthContext'
import { PermissionMatrixModal } from '../components/PermissionMatrixModal'
import { useDeleteRole, useMenuPathOptions, useRoles, useSaveRole } from '../api'
import type { Role } from '../types'

export function RolesPage() {
  const { hasPermission } = useAuth()
  const { data: roles = [], isLoading } = useRoles()
  const pathOptions = useMenuPathOptions()

  const [editorOpen, setEditorOpen] = useState(false)
  const [editing, setEditing] = useState<Role | null>(null)
  const [form, setForm] = useState({ code: '', name: '', description: '', defaultLandingPath: '' })
  const [error, setError] = useState<string | null>(null)
  const [matrixRole, setMatrixRole] = useState<Role | null>(null)

  const saveRole = useSaveRole()
  const deleteRole = useDeleteRole()

  const openEditor = (role: Role | null) => {
    setEditing(role)
    setForm(
      role
        ? { code: role.code, name: role.name, description: role.description ?? '', defaultLandingPath: role.defaultLandingPath ?? '' }
        : { code: '', name: '', description: '', defaultLandingPath: '' },
    )
    setError(null)
    setEditorOpen(true)
  }

  const submit = async () => {
    try {
      await saveRole.mutateAsync({
        id: editing?.id,
        code: form.code,
        name: form.name,
        description: form.description || null,
        defaultLandingPath: form.defaultLandingPath || null,
      })
      setEditorOpen(false)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <>
      <PageHeader
        title="Roles & Permissions"
        subtitle="Roles group permissions; users get roles"
        actions={hasPermission('ADMIN.ROLE.CREATE') && <Button onClick={() => openEditor(null)}>New role</Button>}
      />

      {isLoading && <Spinner />}

      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
        {roles.map((role) => (
          <div key={role.id} className="rounded-shell border border-border-soft bg-panel p-4 shadow-shell-sm">
            <div className="flex items-start justify-between">
              <div>
                <p className="font-semibold text-ink">{role.name}</p>
                <p className="font-mono text-xs text-ink-faint">{role.code}</p>
              </div>
              {role.isSystem && <StatusBadge tone="info">System</StatusBadge>}
            </div>
            {role.description && <p className="mt-2 text-sm text-ink-muted">{role.description}</p>}
            <p className="mt-2 text-xs text-ink-faint">
              {role.userCount} user(s) · {role.permissionCount} permission(s)
            </p>
            <div className="mt-3 flex gap-1">
              {hasPermission('ADMIN.ROLE.ASSIGNPERMISSIONS') && (
                <Button variant="secondary" onClick={() => setMatrixRole(role)}>
                  Permissions
                </Button>
              )}
              {hasPermission('ADMIN.ROLE.UPDATE') && (
                <Button variant="ghost" onClick={() => openEditor(role)}>
                  Edit
                </Button>
              )}
              {hasPermission('ADMIN.ROLE.DELETE') && !role.isSystem && (
                <Button
                  variant="ghost"
                  className="text-crit"
                  onClick={async () => {
                    if (window.confirm(`Delete role '${role.name}'?`)) {
                      try {
                        await deleteRole.mutateAsync(role.id)
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

      <Modal
        open={editorOpen}
        title={editing ? 'Edit role' : 'New role'}
        onClose={() => setEditorOpen(false)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setEditorOpen(false)}>
              Cancel
            </Button>
            <Button onClick={submit} loading={saveRole.isPending}>
              Save
            </Button>
          </>
        }
      >
        <div className="space-y-3">
          {error && <p className="rounded-shell-sm bg-crit/10 px-3 py-2 text-sm text-crit">{error}</p>}
          {editing?.isSystem && (
            <p className="rounded-shell-sm bg-info/10 px-3 py-2 text-xs text-ink-muted">
              This is a system role — its code is locked (it's the seeding key), but the name, description and default
              landing page can be changed.
            </p>
          )}
          <Input
            label="Code (UPPER_SNAKE_CASE)"
            value={form.code}
            onChange={(e) => setForm({ ...form, code: e.target.value.toUpperCase() })}
            disabled={editing?.isSystem}
          />
          <Input label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          <Input
            label="Description"
            value={form.description}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
          />
          <Select
            label="Default landing page"
            placeholder="No override — use the branch's default"
            options={pathOptions}
            value={form.defaultLandingPath}
            onChange={(e) => setForm({ ...form, defaultLandingPath: e.target.value })}
          />
        </div>
      </Modal>

      {matrixRole && <PermissionMatrixModal role={matrixRole} onClose={() => setMatrixRole(null)} />}
    </>
  )
}
