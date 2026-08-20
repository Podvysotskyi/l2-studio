import type { ValidationProblemDetails } from '../../app/types/responses/problem-details'

export function validationProblem(
  errors: Record<string, string[]>,
  detail = 'Correct the highlighted fields and try again.'
): ValidationProblemDetails {
  return {
    title: 'One or more validation errors occurred.',
    status: 400,
    detail,
    errors
  }
}

export function studioFetchFailure(data: ValidationProblemDetails, status = 400) {
  return { statusCode: status, data }
}
