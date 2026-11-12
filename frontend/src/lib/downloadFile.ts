import { api } from '../api/axios'

/** Fetches a file-returning endpoint as a blob and triggers a browser download. */
export async function downloadFile(url: string, filename: string) {
  const response = await api.get(url, { responseType: 'blob' })
  const objectUrl = URL.createObjectURL(response.data as Blob)
  const link = document.createElement('a')
  link.href = objectUrl
  link.download = filename
  link.click()
  URL.revokeObjectURL(objectUrl)
}
