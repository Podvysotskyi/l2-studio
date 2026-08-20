import { describe, expect, it } from 'vitest'
import { useStudioApiError } from '../../app/composables/use-studio-api-error'
import { studioApiError } from '../../app/utils/studio-api-error'

describe('Studio API errors', () => {
  it('extracts ASP.NET validation errors for a page and its fields', () => {
    expect(studioApiError({
      statusCode: 400,
      data: {
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: {
          name: ['Name is required.'],
          level: ['Level must be between 1 and 255.']
        }
      }
    }, 'The NPC could not be saved.')).toEqual({
      status: 400,
      message: 'Correct the highlighted fields and try again.',
      fieldErrors: {
        name: ['Name is required.'],
        level: ['Level must be between 1 and 255.']
      }
    })
  })

  it('prefers a domain problem detail and safely falls back for unknown errors', () => {
    expect(studioApiError({
      status: 409,
      data: { title: 'Record is in use', detail: 'This record is used by 2 items.' }
    }, 'Could not delete the record.').message).toBe('This record is used by 2 items.')
    expect(studioApiError(new Error('offline'), 'Could not delete the record.')).toEqual({
      status: undefined,
      message: 'Could not delete the record.',
      fieldErrors: {}
    })
  })

  it('resets stale field errors before a later request', () => {
    const errors = useStudioApiError()
    errors.capture({ data: { errors: { name: ['Name is required.'] } } }, 'Save failed.')
    expect(errors.fieldError('name')).toBe('Name is required.')

    errors.clear()

    expect(errors.pageError.value).toBeUndefined()
    expect(errors.fieldErrors.value).toEqual({})
  })
})
