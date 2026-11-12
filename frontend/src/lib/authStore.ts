/**
 * Token storage shared by the axios interceptors and the AuthProvider.
 * Access token lives in memory only (never persisted); the refresh token is
 * kept in localStorage so a page reload can restore the session.
 */
const REFRESH_KEY = 'prixo.refreshToken'

let accessToken: string | null = null

export const authStore = {
  getAccessToken: () => accessToken,
  setAccessToken: (token: string | null) => {
    accessToken = token
  },
  getRefreshToken: () => localStorage.getItem(REFRESH_KEY),
  setRefreshToken: (token: string | null) => {
    if (token) localStorage.setItem(REFRESH_KEY, token)
    else localStorage.removeItem(REFRESH_KEY)
  },
  clear: () => {
    accessToken = null
    localStorage.removeItem(REFRESH_KEY)
  },
}
