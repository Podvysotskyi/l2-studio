import type {
  ProblemDetails
} from '../types/responses/problem-details'

export interface StudioApiError {
  status: number | undefined
  message: string
  fieldErrors: Record<string, string[]>
}

export function studioApiError(
  cause: unknown,
  fallback: string
): StudioApiError {
  const response = responseError(cause)
  const problem = problemDetails(response?.data)
  const fieldErrors = validationErrors(response?.data)
  const message = problem?.detail ||
    (Object.keys(fieldErrors).length > 0
      ? 'Correct the highlighted fields and try again.'
      : problem?.title || fallback)

  return {
    status: response?.status,
    message,
    fieldErrors
  }
}

function responseError(value: unknown): { status: number | undefined; data: unknown } | undefined {
  if (!isRecord(value)) return undefined
  const status = typeof value.statusCode === 'number'
    ? value.statusCode
    : typeof value.status === 'number'
      ? value.status
      : undefined
  return 'data' in value ? { status, data: value.data } : undefined
}

function problemDetails(value: unknown): ProblemDetails | undefined {
  if (!isRecord(value)) return undefined
  if (!['type', 'title', 'status', 'detail', 'instance', 'errors'].some(key => key in value))
    return undefined

  return {
    type: stringValue(value.type),
    title: stringValue(value.title),
    status: numberValue(value.status),
    detail: stringValue(value.detail),
    instance: stringValue(value.instance)
  }
}

function validationErrors(value: unknown): Record<string, string[]> {
  if (!isRecord(value) || !('errors' in value) || !isRecord(value.errors)) return {}
  const errors: Record<string, string[]> = {}
  for (const [field, messages] of Object.entries(value.errors)) {
    if (!Array.isArray(messages)) continue
    const values = messages.filter((message): message is string => typeof message === 'string')
    if (values.length > 0) errors[field] = values
  }
  return errors
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function stringValue(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim() ? value : undefined
}

function numberValue(value: unknown): number | undefined {
  return typeof value === 'number' ? value : undefined
}
