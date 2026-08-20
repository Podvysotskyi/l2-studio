import { describe, expect, it } from 'vitest'
import { defineComponent, nextTick } from 'vue'
import { mountSuspended } from '@nuxt/test-utils/runtime'
import { useStudioApiError } from '../../app/composables/use-studio-api-error'
import { studioFetchFailure, validationProblem } from '../support/problem-details'

const ErrorProbe = defineComponent({
  setup() {
    const error = useStudioApiError()
    return { ...error }
  },
  template: `
    <p data-testid="page-error">{{ pageError }}</p>
    <p data-testid="name-error">{{ fieldError('name') }}</p>
    <p data-testid="definition-error">{{ fieldError('definition') }}</p>
  `
})

describe('typed Studio API error boundary', () => {
  it('projects a validation problem into page and field errors, then clears it', async () => {
    const wrapper = await mountSuspended(ErrorProbe)
    const apiError = wrapper.vm.capture(
      studioFetchFailure(validationProblem({ name: ['Name is required.'], definition: ['Definition is invalid.'] }, 'The item could not be saved.')),
      'Fallback'
    )
    await nextTick()

    expect(apiError.status).toBe(400)
    expect(wrapper.get('[data-testid="page-error"]').text()).toBe('The item could not be saved.')
    expect(wrapper.get('[data-testid="name-error"]').text()).toBe('Name is required.')
    expect(wrapper.get('[data-testid="definition-error"]').text()).toBe('Definition is invalid.')

    wrapper.vm.clear()
    await nextTick()

    expect(wrapper.get('[data-testid="page-error"]').text()).toBe('')
    expect(wrapper.get('[data-testid="name-error"]').text()).toBe('')
  })
})
