import { useEffect, useMemo, useState } from 'react'
import { Button } from '../../../components/ui/Button'
import { Modal } from '../../../components/ui/Modal'
import { Spinner } from '../../../components/ui/Spinner'
import { usePermissionCatalogue, useRestoreDefaultPermissions, useRolePermissions, useSetRolePermissions } from '../api'
import { MenuPermissionTree } from './MenuPermissionTree'

type View = 'module' | 'menu'

/** Shared by RolesPage and UsersPage (click a role chip to edit its module permissions inline). */
export function PermissionMatrixModal({
  role, onClose,
}: {
  role: { id: number; name: string; isSystem: boolean }
  onClose: () => void
}) {
  const { data: catalogue = [] } = usePermissionCatalogue()
  const { data: assigned, isLoading } = useRolePermissions(role.id)
  const setPermissions = useSetRolePermissions()
  const restoreDefaults = useRestoreDefaultPermissions()
  const [view, setView] = useState<View>('module')
  const [initial, setInitial] = useState<Set<string>>(new Set())
  const [selected, setSelected] = useState<Set<string>>(new Set())

  useEffect(() => {
    if (assigned) {
      setInitial(new Set(assigned))
      setSelected(new Set(assigned))
    }
  }, [assigned])

  const byModule = useMemo(() => {
    const groups = new Map<string, typeof catalogue>()
    for (const permission of catalogue) {
      const list = groups.get(permission.module) ?? []
      list.push(permission)
      groups.set(permission.module, list)
    }
    return groups
  }, [catalogue])

  const toggle = (code: string) => {
    setSelected((current) => {
      const next = new Set(current)
      if (next.has(code)) next.delete(code)
      else next.add(code)
      return next
    })
  }

  const added = [...selected].filter((code) => !initial.has(code)).length
  const removed = [...initial].filter((code) => !selected.has(code)).length

  const save = async () => {
    try {
      await setPermissions.mutateAsync({ roleId: role.id, permissionCodes: [...selected] })
      onClose()
    } catch (err) {
      window.alert(err instanceof Error ? err.message : String(err))
    }
  }

  const restore = async () => {
    if (!window.confirm(`Reset '${role.name}' back to its seeded default permissions? Any hand-tuning beyond that will be lost.`)) {
      return
    }
    try {
      await restoreDefaults.mutateAsync(role.id)
      onClose()
    } catch (err) {
      window.alert(err instanceof Error ? err.message : String(err))
    }
  }

  return (
    <Modal
      open
      title={`Permissions — ${role.name}`}
      onClose={onClose}
      footer={
        <>
          {role.isSystem && (
            <Button variant="ghost" className="text-crit" onClick={restore} loading={restoreDefaults.isPending}>
              Restore to default
            </Button>
          )}
          <div className="flex-1" />
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={save} loading={setPermissions.isPending}>
            Save permissions
          </Button>
        </>
      }
    >
      <div className="mb-3 flex items-center justify-between">
        <div className="flex gap-1 rounded-shell-sm border border-border-soft bg-panel-2 p-0.5">
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
        {(added > 0 || removed > 0) && (
          <p className="text-xs font-medium">
            {added > 0 && <span className="text-ok">+{added} added</span>}
            {added > 0 && removed > 0 && ' · '}
            {removed > 0 && <span className="text-crit">−{removed} removed</span>}
          </p>
        )}
      </div>
      {isLoading && <Spinner />}

      {view === 'module' ? (
        <div className="max-h-[55vh] space-y-4 overflow-y-auto pr-1">
          {[...byModule.entries()].map(([module, permissions]) => (
            <div key={module}>
              <div className="mb-1.5 flex items-center justify-between">
                <h3 className="text-sm font-semibold text-ink">{module}</h3>
                <button
                  type="button"
                  className="text-xs text-accent-2 hover:underline"
                  onClick={() =>
                    setSelected((current) => {
                      const next = new Set(current)
                      const allSelected = permissions.every((p) => next.has(p.code))
                      permissions.forEach((p) => (allSelected ? next.delete(p.code) : next.add(p.code)))
                      return next
                    })
                  }
                >
                  Toggle all
                </button>
              </div>
              <div className="grid grid-cols-2 gap-1.5">
                {permissions.map((permission) => {
                  const checked = selected.has(permission.code)
                  const wasChecked = initial.has(permission.code)
                  const changed = checked !== wasChecked
                  return (
                    <label
                      key={permission.code}
                      className={`flex items-center gap-2 rounded-shell-sm px-1.5 py-1 text-sm text-ink-muted ${
                        changed ? (checked ? 'bg-ok/10' : 'bg-crit/10') : ''
                      }`}
                    >
                      <input type="checkbox" checked={checked} onChange={() => toggle(permission.code)} />
                      <span title={permission.description ?? permission.code}>
                        {permission.resource}.{permission.action}
                      </span>
                    </label>
                  )
                })}
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="max-h-[55vh] overflow-y-auto pr-1">
          <p className="mb-2 text-xs text-ink-faint">
            Toggling a menu item grants/denies the permission that gates it in the sidebar — items with no permission
            are visible to everyone and can't be toggled here.
          </p>
          <MenuPermissionTree
            roleId={role.id}
            isGranted={(code) => selected.has(code)}
            renderControl={(code) => {
              const checked = selected.has(code)
              const changed = checked !== initial.has(code)
              return (
                <button
                  type="button"
                  onClick={() => toggle(code)}
                  className={`shrink-0 rounded-full px-2.5 py-0.5 text-[11px] font-semibold ${
                    checked ? 'bg-ok text-white' : 'bg-panel-2 text-ink-faint'
                  } ${changed ? 'ring-2 ring-warn/50' : ''}`}
                >
                  {checked ? 'Visible' : 'Hidden'}
                </button>
              )
            }}
          />
        </div>
      )}
    </Modal>
  )
}
