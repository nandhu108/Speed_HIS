/** Mirrors backend Prixo.Application.Common.Models.ApiResponse<T>. */
export interface ApiResponse<T> {
  success: boolean
  message: string | null
  data: T | null
  errors: Record<string, string[]> | null
  traceId: string | null
}

/** Mirrors backend PagedResult<T>. */
export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

/** Standard list-endpoint query parameters (backend PagedQuery). */
export interface PagedQuery {
  page?: number
  pageSize?: number
  search?: string
  sort?: string
  order?: 'asc' | 'desc'
}
