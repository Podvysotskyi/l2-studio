import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  getAssetCatalogs,
  getAssetImportJobs,
  getLookupDirectory,
  getNpcLookupDirectory,
  getNpcDirectory,
  getPlayerClasses,
  getSkillDirectory
} from '../../app/services/studio-api'
import { useDashboardStore } from '../../app/stores/dashboard'

vi.mock('../../app/services/studio-api', () => ({
  getAssetCatalogs: vi.fn(),
  getAssetImportJobs: vi.fn(),
  getLookupDirectory: vi.fn(),
  getNpcLookupDirectory: vi.fn(),
  getNpcDirectory: vi.fn(),
  getPlayerClasses: vi.fn(),
  getSkillDirectory: vi.fn()
}))

describe('Dashboard store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.mocked(getAssetCatalogs).mockReset()
    vi.mocked(getAssetImportJobs).mockReset()
    vi.mocked(getLookupDirectory).mockReset()
    vi.mocked(getNpcLookupDirectory).mockReset()
    vi.mocked(getNpcDirectory).mockReset()
    vi.mocked(getPlayerClasses).mockReset()
    vi.mocked(getSkillDirectory).mockReset()
    vi.mocked(getLookupDirectory).mockResolvedValue([])
    vi.mocked(getNpcLookupDirectory).mockResolvedValue([])
  })

  it('aggregates content, asset, and recent-job summaries', async () => {
    vi.mocked(getNpcDirectory).mockResolvedValue({
      items: [],
      total: 12,
      page: 1,
      pageSize: 1
    })
    vi.mocked(getSkillDirectory).mockResolvedValue({
      items: [],
      total: 34,
      page: 1,
      pageSize: 1
    })
    vi.mocked(getPlayerClasses).mockResolvedValue([])
    vi.mocked(getLookupDirectory).mockResolvedValue([])
    vi.mocked(getNpcLookupDirectory).mockResolvedValue([])
    vi.mocked(getAssetCatalogs).mockResolvedValue([
      {
        kind: 'textures',
        sourceFolder: 'textures',
        sourceHash: 'hash',
        schemaVersion: 1,
        protocol: 1,
        total: 8,
        resolved: 7,
        skipped: 1,
        groupCount: 2,
        publishedAt: '2026-08-11T12:00:00Z'
      }
    ])
    vi.mocked(getAssetImportJobs).mockImplementation(async kind => [
      job(`${kind}-job`, kind, kind === 'textures'
        ? '2026-08-11T13:00:00Z'
        : '2026-08-11T12:00:00Z')
    ])
    const store = useDashboardStore()

    await store.load()

    expect(store.counts.npcs).toBe(12)
    expect(store.counts.skills).toBe(34)
    expect(store.assets.find(item => item.kind === 'textures')).toMatchObject({
      total: 8,
      resolved: 7,
      skipped: 1,
      groups: 2,
      available: true
    })
    expect(store.jobs).toHaveLength(5)
    expect(store.jobs[0]?.id).toBe('textures-job')
    expect(store.loading).toBe(false)
    expect(store.contentError).toBe(false)
    expect(store.assetError).toBe(false)
    expect(store.jobsError).toBe(false)
  })

  it('keeps partial dashboard results and reports independent failures', async () => {
    vi.mocked(getNpcDirectory).mockRejectedValue(new Error('Unavailable'))
    vi.mocked(getAssetCatalogs).mockRejectedValue(new Error('Unavailable'))
    vi.mocked(getAssetImportJobs).mockRejectedValue(new Error('Unavailable'))
    const store = useDashboardStore()

    await store.load()

    expect(store.contentError).toBe(true)
    expect(store.assetError).toBe(true)
    expect(store.jobsError).toBe(true)
    expect(store.loading).toBe(false)
  })
})

function job(id: string, kind: Parameters<typeof getAssetImportJobs>[0], requestedAt: string) {
  return {
    id,
    kind,
    triggerType: 'full_scan' as const,
    status: 'succeeded' as const,
    requestedSourceKey: null,
    requestedAt,
    startedAt: requestedAt,
    discoveryFinishedAt: requestedAt,
    finishedAt: requestedAt,
    discoveredFileCount: 1,
    completedFileCount: 1,
    succeededFileCount: 1,
    warningFileCount: 0,
    failedFileCount: 0,
    error: null
  }
}
