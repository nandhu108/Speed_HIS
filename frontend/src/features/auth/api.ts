import { api } from '../../api/axios'
import { endpoints } from '../../api/endpoints'
import type { ApiResponse } from '../../api/types/api'
import type { AuthUser, BranchOption, LoginRequest, LoginResponse } from './types'

export async function loginRequest(payload: LoginRequest): Promise<LoginResponse> {
  const { data } = await api.post<ApiResponse<LoginResponse>>(endpoints.auth.login, payload)
  return data.data!
}

export async function logoutRequest(): Promise<void> {
  await api.post(endpoints.auth.logout)
}

export async function fetchMe(): Promise<AuthUser> {
  const { data } = await api.get<ApiResponse<AuthUser>>(endpoints.auth.me)
  return data.data!
}

export async function changePasswordRequest(currentPassword: string, newPassword: string): Promise<void> {
  await api.post(endpoints.auth.changePassword, { currentPassword, newPassword })
}

export async function fetchMyBranches(): Promise<BranchOption[]> {
  const { data } = await api.get<ApiResponse<BranchOption[]>>(endpoints.auth.myBranches)
  return data.data!
}
