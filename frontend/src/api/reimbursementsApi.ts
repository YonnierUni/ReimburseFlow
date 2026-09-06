import type {
  CreateReimbursementRequest,
  ProblemDetails,
  Reimbursement,
  ReimbursementFilters,
} from '../models/reimbursement'

export const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5087/api').replace(/\/$/, '')
export const apiDocsUrl = `${apiBaseUrl.replace(/\/api$/, '')}/swagger`

export class ApiError extends Error {
  status?: number
  title?: string
  traceId?: string

  constructor(problem: ProblemDetails, fallback = 'Request failed.') {
    super(problem.detail || problem.title || fallback)
    this.name = 'ApiError'
    this.status = problem.status
    this.title = problem.title
    this.traceId = problem.traceId
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  try {
    const response = await fetch(`${apiBaseUrl}${path}`, {
      ...init,
      headers: {
        Accept: 'application/json',
        ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
        ...init?.headers,
      },
    })

    if (!response.ok) {
      let problem: ProblemDetails = {}
      try {
        problem = (await response.json()) as ProblemDetails
      } catch {
        problem = { status: response.status, title: response.statusText }
      }
      throw new ApiError({ ...problem, status: problem.status ?? response.status })
    }

    return (await response.json()) as T
  } catch (error) {
    if (error instanceof ApiError) throw error
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    throw new Error('Could not connect to the server.')
  }
}

export function createReimbursement(
  requestBody: CreateReimbursementRequest,
  idempotencyKey: string,
): Promise<Reimbursement> {
  return request<Reimbursement>('/reimbursements', {
    method: 'POST',
    headers: { 'Idempotency-Key': idempotencyKey },
    body: JSON.stringify(requestBody),
  })
}

export function getReimbursements(filters: ReimbursementFilters = {}, signal?: AbortSignal): Promise<Reimbursement[]> {
  const params = new URLSearchParams()
  if (filters.status) params.set('status', filters.status)
  if (filters.category) params.set('category', filters.category)
  const query = params.toString() ? `?${params.toString()}` : ''
  return request<Reimbursement[]>(`/reimbursements${query}`, { signal })
}

export function getReimbursementById(id: string): Promise<Reimbursement> {
  return request<Reimbursement>(`/reimbursements/${id}`)
}

export function approveReimbursement(id: string): Promise<Reimbursement> {
  return request<Reimbursement>(`/reimbursements/${id}/approve`, { method: 'POST' })
}

export function rejectReimbursement(id: string, reason: string): Promise<Reimbursement> {
  return request<Reimbursement>(`/reimbursements/${id}/reject`, {
    method: 'POST',
    body: JSON.stringify({ reason }),
  })
}
