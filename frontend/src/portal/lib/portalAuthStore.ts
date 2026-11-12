/** Token storage for the Patient Portal — mirrors lib/authStore.ts but under its own keys, kept fully separate from the staff session. */
const REFRESH_KEY = 'prixo.portal.refreshToken'

let accessToken: string | null = null

export const portalAuthStore = {
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
