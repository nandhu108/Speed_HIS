import axios from 'axios'
import { authStore } from '../lib/authStore'
import type { ApiResponse } from './types/api'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api/v1',
  timeout: 30_000,
})

api.interceptors.request.use((config) => {
  const token = authStore.getAccessToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

/** Called by the interceptor when the session is unrecoverable (refresh failed). */
let onSessionExpired: (() => void) | null = null
export function setSessionExpiredHandler(handler: () => void) {
  onSessionExpired = handler
}

// Single-flight refresh: concurrent 401s all await the same request.
let refreshPromise: Promise<string | null> | null = null

// Only these truly can't be retried via a refresh: /auth/login 401s on bad credentials, not an
// expired token, and /auth/refresh is the refresh call itself (issued via a raw axios instance
// below, bypassing these interceptors, but excluded here too for defense in depth). Every other
// /auth/* endpoint — notably /auth/me, which AuthProvider calls on every page load to restore
// the session from the persisted refresh token — must go through the normal retry path below;
// a broader "/auth/" prefix match here previously caught /auth/me too, so the very call meant to
// restore the session on reload could never recover from its own access-token-expired 401.
const NO_REFRESH_RETRY_URLS = ['/auth/login', '/auth/refresh']

async function refreshAccessToken(): Promise<string | null> {
  const refreshToken = authStore.getRefreshToken()
  if (!refreshToken) return null
  try {
    // Raw axios: must not recurse through this instance's interceptors.
    const { data } = await axios.post<ApiResponse<{ accessToken: string; refreshToken: string }>>(
      `${api.defaults.baseURL}/auth/refresh`,
      { refreshToken },
    )
    if (!data.success || !data.data) return null
    authStore.setAccessToken(data.data.accessToken)
    authStore.setRefreshToken(data.data.refreshToken)
    return data.data.accessToken
  } catch {
    return null
  }
}

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config
    const status = error.response?.status

    // 401 on a normal call: try one refresh, then replay the original request.
    if (
      status === 401 &&
      original &&
      !original._retried &&
      !NO_REFRESH_RETRY_URLS.some((url) => original.url?.includes(url))
    ) {
      original._retried = true
      refreshPromise ??= refreshAccessToken().finally(() => {
        refreshPromise = null
      })
      const newToken = await refreshPromise
      if (newToken) {
        original.headers.Authorization = `Bearer ${newToken}`
        return api(original)
      }
      authStore.clear()
      onSessionExpired?.()
    }

    // Normalize network/API failures so callers always see an ApiResponse-shaped error.
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

/** Extracts a display message from a normalized axios error. */
export function errorMessage(error: unknown): string {
  const apiResponse = (error as { apiResponse?: ApiResponse<unknown> })?.apiResponse
  if (apiResponse?.errors) {
    const all = Object.values(apiResponse.errors).flat()
    if (all.length) return all.join(' ')
  }
  return apiResponse?.message ?? (error as Error)?.message ?? 'Something went wrong'
}
