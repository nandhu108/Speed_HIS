import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { setSessionExpiredHandler } from '../../api/axios'
import { authStore } from '../../lib/authStore'
import { queryClient } from '../../lib/queryClient'
import { clearPersistedTabs } from '../../lib/tabWorkspace/TabWorkspaceContext'
import { fetchMe, loginRequest, logoutRequest } from './api'
import type { AuthUser, LoginRequest } from './types'

type AuthStatus = 'loading' | 'authenticated' | 'anonymous'

interface AuthContextValue {
  status: AuthStatus
  user: AuthUser | null
  login: (payload: LoginRequest) => Promise<AuthUser>
  logout: () => Promise<void>
  refreshUser: () => Promise<void>
  /** Cosmetic only — the API enforces permissions. Used to hide UI the user cannot act on. */
  hasPermission: (code: string) => boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>('loading')
  const [user, setUser] = useState<AuthUser | null>(null)

  const becomeAnonymous = useCallback(() => {
    authStore.clear()
    queryClient.clear()
    clearPersistedTabs()
    setUser(null)
    setStatus('anonymous')
  }, [])

  // Restore the session on page load: the refresh-token replay in the axios
  // interceptor turns the stored refresh token into a fresh access token.
  useEffect(() => {
    setSessionExpiredHandler(becomeAnonymous)
    if (!authStore.getRefreshToken()) {
      setStatus('anonymous')
      return
    }
    fetchMe()
      .then((me) => {
        setUser(me)
        setStatus('authenticated')
      })
      .catch(becomeAnonymous)
  }, [becomeAnonymous])

  const login = useCallback(async (payload: LoginRequest) => {
    const response = await loginRequest(payload)
    authStore.setAccessToken(response.accessToken)
    authStore.setRefreshToken(response.refreshToken)
    clearPersistedTabs()
    setUser(response.user)
    setStatus('authenticated')
    return response.user
  }, [])

  const logout = useCallback(async () => {
    try {
      await logoutRequest()
    } catch {
      // Server-side revoke failed (e.g. offline) — still drop the local session.
    }
    becomeAnonymous()
  }, [becomeAnonymous])

  const refreshUser = useCallback(async () => {
    const me = await fetchMe()
    setUser(me)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      user,
      login,
      logout,
      refreshUser,
      hasPermission: (code) => user?.permissions.includes(code) ?? false,
    }),
    [status, user, login, logout, refreshUser],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside <AuthProvider>')
  return context
}
