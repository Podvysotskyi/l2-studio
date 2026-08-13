import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useStudioToasts } from '../../app/composables/use-studio-toasts'

describe('Studio toasts', () => {
  const add = vi.fn()

  beforeEach(() => {
    vi.stubGlobal('useToast', () => ({ add }))
  })

  afterEach(() => {
    add.mockReset()
    vi.unstubAllGlobals()
  })

  it('uses the shared success styling', () => {
    useStudioToasts().success({ title: 'Draft saved' })

    expect(add).toHaveBeenCalledWith({
      title: 'Draft saved',
      color: 'success',
      icon: 'i-lucide-circle-check',
      duration: 5000
    })
  })

  it('uses readable warning and error durations', () => {
    const toasts = useStudioToasts()
    toasts.warning({ title: 'Validation completed with issues' })
    toasts.error({ title: 'Save failed', description: 'Try again.' })

    expect(add).toHaveBeenNthCalledWith(1, {
      title: 'Validation completed with issues',
      color: 'warning',
      icon: 'i-lucide-triangle-alert',
      duration: 7000
    })
    expect(add).toHaveBeenNthCalledWith(2, {
      title: 'Save failed',
      description: 'Try again.',
      color: 'error',
      icon: 'i-lucide-circle-alert',
      duration: 7000
    })
  })
})
