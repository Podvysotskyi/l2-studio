import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getStudioServiceInfo } from '../../app/services/studio-api'
import { useSystemStore } from '../../app/stores/system'

vi.mock('../../app/services/studio-api', () => ({
  getStudioServiceInfo: vi.fn()
}))

describe('System store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.mocked(getStudioServiceInfo).mockReset()
  })

  it('deduplicates service information and exposes its description', async () => {
    vi.mocked(getStudioServiceInfo).mockResolvedValue({
      service: 'l2-studio-api',
      buildVersion: '1.0.0',
      environment: 'Testing'
    })
    const store = useSystemStore()

    await Promise.all([store.load(), store.load()])
    await store.load()

    expect(getStudioServiceInfo).toHaveBeenCalledTimes(1)
    expect(store.serviceState).toBe('connected')
    expect(store.description).toBe('Testing · 1.0.0')
  })

  it('reports service failures', async () => {
    vi.mocked(getStudioServiceInfo).mockRejectedValue(new Error('Unavailable'))
    const store = useSystemStore()

    await expect(store.load()).rejects.toThrow('Unavailable')
    expect(store.serviceState).toBe('error')
    expect(store.loading).toBe(false)
  })
})
