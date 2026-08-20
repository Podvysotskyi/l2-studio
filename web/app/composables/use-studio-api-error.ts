import { ref } from 'vue'
import { studioApiError } from '../utils/studio-api-error'

export function useStudioApiError() {
  const pageError = ref<string>()
  const fieldErrors = ref<Record<string, string[]>>({})

  function clear() {
    pageError.value = undefined
    fieldErrors.value = {}
  }

  function set(message: string, fields: Record<string, string[]> = {}) {
    pageError.value = message
    fieldErrors.value = fields
  }

  function capture(cause: unknown, fallback: string) {
    const error = studioApiError(cause, fallback)
    set(error.message, error.fieldErrors)
    return error
  }

  function fieldError(field: string) {
    return fieldErrors.value[field]?.[0]
  }

  return { pageError, fieldErrors, clear, set, capture, fieldError }
}
