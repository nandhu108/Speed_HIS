import axios from 'axios'
import type { ApiResponse } from '../../api/types/api'
import { portalAuthStore } from '../lib/portalAuthStore'

/** Axios instance for the Patient Portal (plan §21's separate JWT audience) — a distinct token store/refresh flow from the staff `api` instance; never shares a header interceptor with it. */
export const portalApi = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api/v1',
  timeout: 30_000,
})

portalApi.interceptors.request.use((config) => {
  const token = portalAuthStore.getAccessToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

let onSessionExpired: (() => void) | null = null
export function setPortalSessionExpiredHandler(handler: () => void) {
  onSessionExpired = handler
}

let refreshPromise: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
  const refreshToken = portalAuthStore.getRefreshToken()
  if (!refreshToken) return null
  try {
    const { data } = await axios.post<ApiResponse<{ accessToken: string; refreshToken: string }>>(
      `${portalApi.defaults.baseURL}/portal/auth/refresh`,
      { refreshToken },
    )
    if (!data.success || !data.data) return null
    portalAuthStore.setAccessToken(data.data.accessToken)
    portalAuthStore.setRefreshToken(data.data.refreshToken)
    return data.data.accessToken
  } catch {
    return null
  }
}

portalApi.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config
    const status = error.response?.status

    if (status === 401 && original && !original._retried && !original.url?.includes('/portal/auth/')) {
      original._retried = true
      refreshPromise ??= refreshAccessToken().finally(() => {
        refreshPromise = null
      })
      const newToken = await refreshPromise
      if (newToken) {
        original.headers.Authorization = `Bearer ${newToken}`
        return portalApi(original)
      }
      portalAuthStore.clear()
      onSessionExpired?.()
    }

    const fallback: ApiResponse<null> = {
      success: false,
      message: error.response?.data?.message ?? error.message ?? 'Request failed',
      data: null,
      errors: error.response?.data?.errors ?? null,
      traceId: error.response?.data?.traceId ?? null,
    }
    return Promise.reject(Object.assign(error, { apiResponse: error.response?.data ?? fallback }))
  },
)
