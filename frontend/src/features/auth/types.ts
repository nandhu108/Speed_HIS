export interface AuthUser {
  id: number
  username: string
  fullName: string
  email: string | null
  photoPath: string | null
  branchId: number
  branchName: string
  mustChangePassword: boolean
  roles: string[]
  permissions: string[]
  /** Menu path "/" should redirect to (a role's own setting wins over the branch's); null = no override. */
  defaultLandingPath: string | null
}

export interface LoginRequest {
  username: string
  password: string
  branchId?: number | null
}

export interface LoginResponse {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  user: AuthUser
}

export interface BranchOption {
  id: number
  code: string
  name: string
}
