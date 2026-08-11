import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  getAssetImportJobs,
  startAssetImport
} from '../../app/services/studio-api'
import { useAssetImportsStore } from '../../app/stores/asset-imports'

vi.mock('../../app/services/studio-api', () => ({
  getAssetImportJobs: vi.fn(),
  startAssetImport: vi.fn()
}))

describe('Asset imports store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.mocked(getAssetImportJobs).mockReset()
    vi.mocked(startAssetImport).mockReset()
  })

  it('loads each requested kind and replaces its cached jobs', async () => {
    vi.mocked(getAssetImportJobs)
      .mockResolvedValueOnce([job('textures-1', 'textures')])
      .mockResolvedValueOnce([job('music-1', 'music')])
    const store = useAssetImportsStore()

    await store.load(['textures', 'music'])

    expect(getAssetImportJobs).toHaveBeenNthCalledWith(1, 'textures', 100)
    expect(getAssetImportJobs).toHaveBeenNthCalledWith(2, 'music', 100)
    expect(store.jobs.textures?.[0]?.id).toBe('textures-1')
    expect(store.jobs.music?.[0]?.id).toBe('music-1')
    expect(store.loading).toBe(false)
    expect(store.error).toBeUndefined()
  })

  it('prepends a started job to its kind', async () => {
    const store = useAssetImportsStore()
    store.jobs.textures = [job('existing', 'textures')]
    vi.mocked(startAssetImport).mockResolvedValue(job('new', 'textures'))

    const result = await store.start('textures')

    expect(startAssetImport).toHaveBeenCalledWith('textures')
    expect(result.id).toBe('new')
    expect(store.jobs.textures?.map(item => item.id)).toEqual(['new', 'existing'])
  })

  it('exposes a stable error when loading fails', async () => {
    vi.mocked(getAssetImportJobs).mockRejectedValue(new Error('Unavailable'))
    const store = useAssetImportsStore()

    await expect(store.load(['textures'])).rejects.toThrow('Unavailable')

    expect(store.error).toBe('Asset import jobs could not be loaded.')
    expect(store.loading).toBe(false)
  })
})

function job(id: string, kind: 'textures' | 'music') {
  return {
    id,
    kind,
    triggerType: 'full_scan' as const,
    status: 'queued' as const,
    requestedSourceKey: null,
    requestedAt: '2026-08-11T12:00:00Z',
    startedAt: null,
    discoveryFinishedAt: null,
    finishedAt: null,
    discoveredFileCount: 0,
    completedFileCount: 0,
    succeededFileCount: 0,
    warningFileCount: 0,
    failedFileCount: 0,
    error: null
  }
}
