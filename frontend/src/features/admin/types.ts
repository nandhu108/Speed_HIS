export interface UserListItem {
  id: number
  username: string
  fullName: string
  email: string | null
  phone: string | null
  employeeCode: string | null
  designationName: string | null
  departmentName: string | null
  isActive: boolean
  isLocked: boolean
  lastLoginAt: string | null
  roles: string[]
}

export interface UserDetail {
  id: number
  username: string
  fullName: string
  email: string | null
  phone: string | null
  employeeCode: string | null
  designationId: number | null
  departmentId: number | null
  photoPath: string | null
  defaultBranchId: number | null
  isActive: boolean
  mustChangePassword: boolean
  lastLoginAt: string | null
  roleIds: number[]
  branchIds: number[]
}

export interface SaveUserPayload {
  username?: string
  password?: string
  fullName: string
  email: string | null
  phone: string | null
  employeeCode: string | null
  designationId: number | null
  departmentId: number | null
  defaultBranchId: number | null
  isActive?: boolean
  roleIds: number[]
  branchIds: number[]
}

export interface RolePreset {
  id: number
  name: string
  roleIds: number[]
  roleNames: string[]
}

export interface Role {
  id: number
  code: string
  name: string
  description: string | null
  isSystem: boolean
  isActive: boolean
  userCount: number
  permissionCount: number
  defaultLandingPath: string | null
}

export interface Permission {
  id: number
  code: string
  module: string
  resource: string
  action: string
  description: string | null
}

export interface PermissionOverride {
  permissionCode: string
  isGranted: boolean
}

/** rolePermissionCodes is what the user's roles alone grant; overrides are per-user exceptions on top. */
export interface UserPermissions {
  rolePermissionCodes: string[]
  overrides: PermissionOverride[]
}

export interface MenuNodeAdmin {
  id: number
  parentId: number | null
  title: string
  icon: string | null
  path: string | null
  permissionCode: string | null
  sortOrder: number
  isActive: boolean
  /** True when this position comes from a role-scoped override (only set by useRoleMenuTree), not the shared global position. */
  isOverridden: boolean
  /** True when this role has hidden this item from its own sidebar (only set by useRoleMenuTree) — the item still exists globally and for every other role. */
  isHidden: boolean
  /** Raw global flag: off by default for every role until a role has an explicit "shown" override — distinct from isHidden, which is the resolved per-role state. */
  hiddenByDefault: boolean
  children: MenuNodeAdmin[]
}

export interface Branch {
  id: number
  code: string
  name: string
  address: string | null
  city: string | null
  state: string | null
  pincode: string | null
  phone: string | null
  email: string | null
  gstNumber: string | null
  reportHeader: string | null
  timeZone: string
  isActive: boolean
  defaultLandingPath: string | null
}

export interface Designation {
  id: number
  name: string
  description: string | null
  isActive: boolean
  userCount: number
}

export interface Setting {
  id: number
  key: string
  value: string
  description: string | null
  group: string
}

export interface Session {
  id: number
  sessionId: string
  userId: number
  username: string
  fullName: string
  branchId: number
  branchName: string | null
  ipAddress: string | null
  userAgent: string | null
  loginAt: string
  lastActivityAt: string
  loggedOutAt: string | null
  isRevoked: boolean
  isCurrent: boolean
}

export interface AuditLogEntry {
  id: number
  userId: number
  username: string | null
  branchId: number | null
  entityName: string
  entityId: string
  action: string
  oldValues: string | null
  newValues: string | null
  traceId: string | null
  ipAddress: string | null
  createdAt: string
}

export interface ActivityLogEntry {
  id: number
  userId: number | null
  username: string | null
  branchId: number | null
  eventType: string
  description: string
  ipAddress: string | null
  userAgent: string | null
  createdAt: string
}
