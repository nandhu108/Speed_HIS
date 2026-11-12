import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { setPortalSessionExpiredHandler } from '../../api/portalAxios'
import { portalAuthStore } from '../../lib/portalAuthStore'
import { portalLogin, portalLogout, portalRefresh } from './api'
import type { PortalLoginPayload, PortalPatient } from './types'

type PortalAuthStatus = 'loading' | 'authenticated' | 'anonymous'

interface PortalAuthContextValue {
  status: PortalAuthStatus
  patient: PortalPatient | null
  login: (payload: PortalLoginPayload) => Promise<PortalPatient>
  logout: () => Promise<void>
}

const PortalAuthContext = createContext<PortalAuthContextValue | null>(null)

export function PortalAuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<PortalAuthStatus>('loading')
  const [patient, setPatient] = useState<PortalPatient | null>(null)

  const becomeAnonymous = useCallback(() => {
    portalAuthStore.clear()
    setPatient(null)
    setStatus('anonymous')
  }, [])

  useEffect(() => {
    setPortalSessionExpiredHandler(becomeAnonymous)
    const stored = portalAuthStore.getRefreshToken()
    if (!stored) {
      setStatus('anonymous')
      return
    }
    // No dedicated "me" endpoint — refresh already returns the full patient profile.
    portalRefresh(stored)
      .then((response) => {
        portalAuthStore.setAccessToken(response.accessToken)
        portalAuthStore.setRefreshToken(response.refreshToken)
        setPatient(response.patient)
        setStatus('authenticated')
      })
      .catch(becomeAnonymous)
  }, [becomeAnonymous])

  const login = useCallback(async (payload: PortalLoginPayload) => {
    const response = await portalLogin(payload)
    portalAuthStore.setAccessToken(response.accessToken)
    portalAuthStore.setRefreshToken(response.refreshToken)
    setPatient(response.patient)
    setStatus('authenticated')
    return response.patient
  }, [])

  const logout = useCallback(async () => {
    try {
      await portalLogout()
    } catch {
      // Server-side revoke failed (e.g. offline) — still drop the local session.
    }
    becomeAnonymous()
  }, [becomeAnonymous])

  const value = useMemo<PortalAuthContextValue>(() => ({ status, patient, login, logout }), [status, patient, login, logout])

  return <PortalAuthContext.Provider value={value}>{children}</PortalAuthContext.Provider>
}

export function usePortalAuth(): PortalAuthContextValue {
  const context = useContext(PortalAuthContext)
  if (!context) throw new Error('usePortalAuth must be used inside <PortalAuthProvider>')
  return context
}
