import type { TemplateDetail, TemplateSummary, UploadResult } from './types'

const API_BASE: string = import.meta.env.VITE_API_URL ?? 'http://localhost:5182'

export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

async function toApiError(response: Response): Promise<ApiError> {
  try {
    const body = (await response.json()) as { error?: string }
    return new ApiError(response.status, body.error ?? 'Error inesperado.')
  } catch {
    return new ApiError(response.status, 'Error inesperado.')
  }
}

export async function listTemplates(): Promise<TemplateSummary[]> {
  const response = await fetch(`${API_BASE}/api/templates`)
  if (!response.ok) throw await toApiError(response)
  return (await response.json()) as TemplateSummary[]
}

export async function getTemplate(id: number): Promise<TemplateDetail> {
  const response = await fetch(`${API_BASE}/api/templates/${id}`)
  if (!response.ok) throw await toApiError(response)
  return (await response.json()) as TemplateDetail
}

export async function uploadTemplate(file: File, name: string): Promise<UploadResult> {
  const form = new FormData()
  form.append('file', file)
  form.append('name', name)
  const response = await fetch(`${API_BASE}/api/templates`, { method: 'POST', body: form })
  if (!response.ok) throw await toApiError(response)
  return (await response.json()) as UploadResult
}

export interface FillPayload {
  simpleValues: Record<string, string>
  rowValues: { rowGroupId: number; rows: Record<string, string>[] }[]
}

export async function generateDocument(id: number, payload: FillPayload): Promise<Blob> {
  const response = await fetch(`${API_BASE}/api/templates/${id}/document`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })
  if (!response.ok) throw await toApiError(response)
  return await response.blob()
}
