import type { ApiResponse } from '../../../api/types/api'
import { portalApi } from '../../api/portalAxios'
import type { PortalLoginPayload, PortalLoginResponse, PortalRegisterPayload } from './types'

export async function portalRegister(payload: PortalRegisterPayload): Promise<void> {
  await portalApi.post<ApiResponse<null>>('/portal/auth/register', payload)
}

export async function portalLogin(payload: PortalLoginPayload): Promise<PortalLoginResponse> {
  const { data } = await portalApi.post<ApiResponse<PortalLoginResponse>>('/portal/auth/login', payload)
  return data.data!
}

export async function portalRefresh(refreshToken: string): Promise<PortalLoginResponse> {
  const { data } = await portalApi.post<ApiResponse<PortalLoginResponse>>('/portal/auth/refresh', { refreshToken })
  return data.data!
}

export async function portalLogout(): Promise<void> {
  await portalApi.post('/portal/auth/logout')
}

export async function portalChangePassword(currentPassword: string, newPassword: string): Promise<void> {
  await portalApi.post('/portal/auth/change-password', { currentPassword, newPassword })
}
