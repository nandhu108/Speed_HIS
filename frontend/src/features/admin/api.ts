import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse, PagedQuery, PagedResult } from '../../api/types/api'
import type {
  ActivityLogEntry,
  AuditLogEntry,
  Branch,
  Designation,
  MenuNodeAdmin,
  Permission,
  PermissionOverride,
  Role,
  RolePreset,
  SaveUserPayload,
  Session,
  Setting,
  UserDetail,
  UserListItem,
  UserPermissions,
} from './types'

async function get<T>(url: string, params?: object): Promise<T> {
  const { data } = await api.get<ApiResponse<T>>(url, { params })
  return data.data!
}

/** Invalidate the given namespaced key after a successful mutation. */
function useInvalidating<TArgs>(fn: (args: TArgs) => Promise<unknown>, keys: (string | number)[][]) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () => keys.forEach((key) => queryClient.invalidateQueries({ queryKey: key })),
  })
}

// ── Users ──────────────────────────────────────────────────────────────────
export interface UserQuery extends PagedQuery {
  departmentId?: number
  designationId?: number
  roleId?: number
  branchId?: number
  isActive?: boolean
}

export const useUsers = (query: UserQuery) =>
  useQuery({
    queryKey: ['admin', 'users', query],
    queryFn: () => get<PagedResult<UserListItem>>(endpoints.users, query),
  })

export const useUser = (id: number | null) =>
  useQuery({
    queryKey: ['admin', 'users', 'detail', id],
    queryFn: () => get<UserDetail>(`${endpoints.users}/${id}`),
    enabled: id !== null,
  })

export const useCreateUser = () =>
  useInvalidating((payload: SaveUserPayload) => api.post(endpoints.users, payload), [['admin', 'users']])

export const useUpdateUser = () =>
  useInvalidating(
    ({ id, ...payload }: SaveUserPayload & { id: number }) => api.put(`${endpoints.users}/${id}`, payload),
    [['admin', 'users']],
  )

export const useDeleteUser = () =>
  useInvalidating((id: number) => api.delete(`${endpoints.users}/${id}`), [['admin', 'users']])

export const useResetPassword = () =>
  useInvalidating(
    ({ id, newPassword }: { id: number; newPassword: string }) =>
      api.post(`${endpoints.users}/${id}/reset-password`, { newPassword, mustChangePassword: true }),
    [['admin', 'users']],
  )

export const useActivateUser = () =>
  useInvalidating((id: number) => api.post(`${endpoints.users}/${id}/activate`), [['admin', 'users']])

export const useDeactivateUser = () =>
  useInvalidating((id: number) => api.post(`${endpoints.users}/${id}/deactivate`), [['admin', 'users']])

export const useUserPermissions = (userId: number | null) =>
  useQuery({
    queryKey: ['admin', 'users', userId, 'permission-overrides'],
    queryFn: () => get<UserPermissions>(`${endpoints.users}/${userId}/permission-overrides`),
    enabled: userId !== null,
  })

export const useSetUserPermissionOverrides = () =>
  useInvalidating(
    ({ userId, overrides }: { userId: number; overrides: PermissionOverride[] }) =>
      api.put(`${endpoints.users}/${userId}/permission-overrides`, { overrides }),
    [['admin', 'users']],
  )

// ── Roles & permissions ────────────────────────────────────────────────────
export const useRoles = () =>
  useQuery({ queryKey: ['admin', 'roles'], queryFn: () => get<Role[]>(endpoints.roles) })

export const usePermissionCatalogue = () =>
  useQuery({
    queryKey: ['admin', 'permissions'],
    queryFn: () => get<Permission[]>(endpoints.permissions),
    staleTime: 10 * 60_000,
  })

export const useRolePermissions = (roleId: number | null) =>
  useQuery({
    queryKey: ['admin', 'roles', roleId, 'permissions'],
    queryFn: () => get<string[]>(`${endpoints.roles}/${roleId}/permissions`),
    enabled: roleId !== null,
  })

export const useSaveRole = () =>
  useInvalidating(
    ({
      id,
      ...payload
    }: {
      id?: number
      code: string
      name: string
      description: string | null
      defaultLandingPath: string | null
    }) => (id ? api.put(`${endpoints.roles}/${id}`, payload) : api.post(endpoints.roles, payload)),
    [['admin', 'roles']],
  )

export const useDeleteRole = () =>
  useInvalidating((id: number) => api.delete(`${endpoints.roles}/${id}`), [['admin', 'roles']])

export const useSetRolePermissions = () =>
  useInvalidating(
    ({ roleId, permissionCodes }: { roleId: number; permissionCodes: string[] }) =>
      api.put(`${endpoints.roles}/${roleId}/permissions`, { permissionCodes }),
    [['admin', 'roles']],
  )

/** Resets a system role's permissions back to its seeded default (undoes any hand-tuning drift). */
export const useRestoreDefaultPermissions = () =>
  useInvalidating(
    (roleId: number) => api.post(`${endpoints.roles}/${roleId}/restore-defaults`),
    [['admin', 'roles']],
  )

export const useRolePresets = () =>
  useQuery({ queryKey: ['admin', 'role-presets'], queryFn: () => get<RolePreset[]>(endpoints.rolePresets) })

export const useSaveRolePreset = () =>
  useInvalidating(
    (payload: { name: string; roleIds: number[] }) => api.post(endpoints.rolePresets, payload),
    [['admin', 'role-presets']],
  )

export const useDeleteRolePreset = () =>
  useInvalidating((id: number) => api.delete(`${endpoints.rolePresets}/${id}`), [['admin', 'role-presets']])

// ── Menus ──────────────────────────────────────────────────────────────────
export const useMenuTree = (enabled = true) =>
  useQuery({ queryKey: ['admin', 'menus'], queryFn: () => get<MenuNodeAdmin[]>(endpoints.menus), enabled })

/** Flattens the menu tree into pickable pages (folders without a path are skipped) — feeds the default-landing-page pickers on Branches and Roles. */
function flattenMenuPaths(nodes: MenuNodeAdmin[], trail: string[]): { value: string; label: string }[] {
  return nodes.flatMap((node) => {
    const label = [...trail, node.title].join(' / ')
    const self = node.isActive && node.path ? [{ value: node.path, label }] : []
    return [...self, ...flattenMenuPaths(node.children, [...trail, node.title])]
  })
}

export const useMenuPathOptions = () => {
  const { data: tree = [] } = useMenuTree()
  return flattenMenuPaths(tree, [])
}

export interface SaveMenuPayload {
  parentId: number | null
  title: string
  icon: string | null
  path: string | null
  permissionCode: string | null
  sortOrder: number
  isActive: boolean
  hiddenByDefault: boolean
}

export const useSaveMenu = () =>
  useInvalidating(
    ({ id, ...payload }: SaveMenuPayload & { id?: number }) =>
      id ? api.put(`${endpoints.menus}/${id}`, payload) : api.post(endpoints.menus, payload),
    [['admin', 'menus'], ['menus', 'my']],
  )

export const useDeleteMenu = () =>
  useInvalidating((id: number) => api.delete(`${endpoints.menus}/${id}`), [['admin', 'menus'], ['menus', 'my']])

/** This role's menu tree with its own position overrides applied — powers the Permissions modal's "By menu" tab. */
export const useRoleMenuTree = (roleId: number, enabled = true) =>
  useQuery({
    queryKey: ['admin', 'roles', roleId, 'menu-tree'],
    queryFn: () => get<MenuNodeAdmin[]>(`${endpoints.roles}/${roleId}/menu-tree`),
    enabled,
  })

export const useSetRoleMenuPosition = (roleId: number) =>
  useInvalidating(
    (
      { menuId, parentId, sortOrder, title, hidden }:
      { menuId: number; parentId: number | null; sortOrder: number; title?: string | null; hidden?: boolean },
    ) =>
      api.put(`${endpoints.roles}/${roleId}/menu-layout/${menuId}`, {
        parentId, sortOrder, title: title ?? null, hidden: hidden ?? false,
      }),
    [['admin', 'roles', roleId, 'menu-tree']],
  )

export const useResetRoleMenuPosition = (roleId: number) =>
  useInvalidating(
    (menuId: number) => api.delete(`${endpoints.roles}/${roleId}/menu-layout/${menuId}`),
    [['admin', 'roles', roleId, 'menu-tree']],
  )

// ── Branches ───────────────────────────────────────────────────────────────
export const useBranches = () =>
  useQuery({ queryKey: ['admin', 'branches'], queryFn: () => get<Branch[]>(endpoints.branches) })

export type SaveBranchPayload = Omit<Branch, 'id'> & { id?: number }

export const useSaveBranch = () =>
  useInvalidating(
    ({ id, ...payload }: SaveBranchPayload) =>
      id ? api.put(`${endpoints.branches}/${id}`, payload) : api.post(endpoints.branches, payload),
    [['admin', 'branches']],
  )

export const useDeleteBranch = () =>
  useInvalidating((id: number) => api.delete(`${endpoints.branches}/${id}`), [['admin', 'branches']])

// ── Designations ───────────────────────────────────────────────────────────
export const useDesignations = () =>
  useQuery({ queryKey: ['admin', 'designations'], queryFn: () => get<Designation[]>(endpoints.designations) })

export const useSaveDesignation = () =>
  useInvalidating(
    ({ id, ...payload }: { id?: number; name: string; description: string | null; isActive: boolean }) =>
      id ? api.put(`${endpoints.designations}/${id}`, payload) : api.post(endpoints.designations, payload),
    [['admin', 'designations']],
  )

export const useDeleteDesignation = () =>
  useInvalidating((id: number) => api.delete(`${endpoints.designations}/${id}`), [['admin', 'designations']])

// ── Settings ───────────────────────────────────────────────────────────────
export const useSettings = () =>
  useQuery({ queryKey: ['admin', 'settings'], queryFn: () => get<Setting[]>(endpoints.settings) })

export const useUpdateSetting = () =>
  useInvalidating(
    ({ key, value }: { key: string; value: string }) => api.put(`${endpoints.settings}/${key}`, { value }),
    [['admin', 'settings']],
  )

// ── Sessions ───────────────────────────────────────────────────────────────
export const useSessions = (query: PagedQuery & { activeOnly: boolean }) =>
  useQuery({
    queryKey: ['admin', 'sessions', query],
    queryFn: () => get<PagedResult<Session>>(endpoints.sessions, query),
  })

export const useRevokeSession = () =>
  useInvalidating(
    (sessionId: string) => api.post(`${endpoints.sessions}/${sessionId}/revoke`),
    [['admin', 'sessions']],
  )

// ── Logs ───────────────────────────────────────────────────────────────────
export const useAuditLogs = (query: PagedQuery & { entityName?: string; action?: string }) =>
  useQuery({
    queryKey: ['admin', 'audit-logs', query],
    queryFn: () => get<PagedResult<AuditLogEntry>>(endpoints.auditLogs, query),
  })

export const useActivityLogs = (query: PagedQuery & { eventType?: string }) =>
  useQuery({
    queryKey: ['admin', 'activity-logs', query],
    queryFn: () => get<PagedResult<ActivityLogEntry>>(endpoints.activityLogs, query),
  })
