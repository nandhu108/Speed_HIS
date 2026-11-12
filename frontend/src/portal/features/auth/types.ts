export interface PortalPatient {
  patientId: number
  uhid: string
  fullName: string
  mobile: string
  mustChangePassword: boolean
}

export interface PortalLoginResponse {
  accessToken: string
  expiresAt: string
  refreshToken: string
  patient: PortalPatient
}

export interface PortalLoginPayload {
  uhidOrMobile: string
  password: string
}

export interface PortalRegisterPayload {
  uhid: string
  mobile: string
  dateOfBirth: string
  password: string
}
