import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  getAssetCatalogs,
  getImportJobs,
  getLookupDirectory,
  getNpcLookupDirectory,
  getNpcDirectory,
  getPlayerClasses,
  getSkillDirectory,
  getSkillLookupDirectory
} from '../../app/services/studio-api'
import { useDashboardStore } from '../../app/stores/dashboard'

vi.mock('../../app/services/studio-api', () => ({
  getAssetCatalogs: vi.fn(),
  getImportJobs: vi.fn(),
  getLookupDirectory: vi.fn(),
  getNpcLookupDirectory: vi.fn(),
  getNpcDirectory: vi.fn(),
  getPlayerClasses: vi.fn(),
  getSkillDirectory: vi.fn(),
  getSkillLookupDirectory: vi.fn()
}))

describe('Dashboard store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.mocked(getAssetCatalogs).mockReset()
    vi.mocked(getImportJobs).mockReset()
    vi.mocked(getLookupDirectory).mockReset()
    vi.mocked(getNpcLookupDirectory).mockReset()
    vi.mocked(getNpcDirectory).mockReset()
    vi.mocked(getPlayerClasses).mockReset()
    vi.mocked(getSkillDirectory).mockReset()
    vi.mocked(getSkillLookupDirectory).mockReset()
    vi.mocked(getLookupDirectory).mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 1 })
    vi.mocked(getNpcLookupDirectory).mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 1 })
    vi.mocked(getSkillLookupDirectory).mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 1 })
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
    vi.mocked(getLookupDirectory).mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 1 })
    vi.mocked(getNpcLookupDirectory).mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 1 })
    vi.mocked(getSkillLookupDirectory).mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 1 })
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
    vi.mocked(getImportJobs).mockResolvedValue({
      items: [
        job('textures-job', 'asset', 'textures', '2026-08-11T13:00:00Z'),
        job('items-job', 'content', 'items', '2026-08-11T12:00:00Z')
      ],
      total: 2,
      page: 1,
      pageSize: 25
    })
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
    expect(store.jobs).toHaveLength(2)
    expect(store.jobs[0]?.id).toBe('textures-job')
    expect(store.loading).toBe(false)
    expect(store.contentError).toBe(false)
    expect(store.assetError).toBe(false)
    expect(store.jobsError).toBe(false)
  })

  it('keeps partial dashboard results and reports independent failures', async () => {
    vi.mocked(getNpcDirectory).mockRejectedValue(new Error('Unavailable'))
    vi.mocked(getAssetCatalogs).mockRejectedValue(new Error('Unavailable'))
    vi.mocked(getImportJobs).mockRejectedValue(new Error('Unavailable'))
    const store = useDashboardStore()

    await store.load()

    expect(store.contentError).toBe(true)
    expect(store.assetError).toBe(true)
    expect(store.jobsError).toBe(true)
    expect(store.loading).toBe(false)
  })
})

function job(id: string, category: 'asset' | 'content', target: string, requestedAt: string) {
  return {
    id,
    category,
    target,
    operation: category === 'asset' ? 'full_scan' : 'add_missing',
    status: 'succeeded' as const,
    requestedSourceKey: null,
    force: false,
    requestedAt,
    startedAt: requestedAt,
    discoveryFinishedAt: requestedAt,
    finishedAt: requestedAt,
    totalCount: 1,
    completedCount: 1,
    metrics: [],
    error: null
  }
}
