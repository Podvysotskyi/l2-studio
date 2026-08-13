import { describe, expect, it } from 'vitest'
import type { AssetImportJob } from '../../app/types/models/asset-import-job'
import type { NpcLookupImportRun } from '../../app/types/models/npc-lookup-import'
import {
  assetImportProgressItem,
  importProgressPercent,
  isActiveImportStatus,
  npcLookupImportProgressItem
} from '../../app/utils/import-progress'

describe('import progress', () => {
  it('normalizes asset results and calculates progress', () => {
    const item = assetImportProgressItem({
      id: 'run-1',
      kind: 'textures',
      triggerType: 'full_scan',
      status: 'running',
      requestedSourceKey: null,
      requestedAt: '2026-08-12T00:00:00Z',
      startedAt: '2026-08-12T00:00:01Z',
      discoveryFinishedAt: '2026-08-12T00:00:02Z',
      finishedAt: null,
      discoveredFileCount: 8,
      completedFileCount: 3,
      succeededFileCount: 2,
      warningFileCount: 1,
      failedFileCount: 0,
      reusedFileCount: 0,
      error: null
    }, 'Textures')

    expect(item.detail).toBe('Full scan')
    expect(item.stats.map(stat => stat.value)).toEqual([2, 1, 0, 0])
    expect(importProgressPercent(item)).toBe(38)
  })

  it('uses indeterminate progress while an active total is unknown', () => {
    const item = assetImportProgressItem({
      id: 'run-1',
      kind: 'maps',
      triggerType: 'full_scan',
      status: 'discovering',
      requestedSourceKey: null,
      requestedAt: '2026-08-12T00:00:00Z',
      startedAt: null,
      discoveryFinishedAt: null,
      finishedAt: null,
      discoveredFileCount: 0,
      completedFileCount: 0,
      succeededFileCount: 0,
      warningFileCount: 0,
      failedFileCount: 0,
      reusedFileCount: 0,
      error: null
    }, 'Maps')

    expect(isActiveImportStatus(item.status)).toBe(true)
    expect(importProgressPercent(item)).toBeUndefined()
  })

  it('normalizes restore-default NPC results', () => {
    const run: NpcLookupImportRun = {
      id: 'run-2',
      kind: 'npc-races',
      mode: 'restore_defaults',
      status: 'succeeded',
      requestedAt: '2026-08-12T00:00:00Z',
      startedAt: '2026-08-12T00:00:01Z',
      finishedAt: '2026-08-12T00:00:02Z',
      totalCount: 7,
      insertedCount: 1,
      existingCount: 6,
      restoredCount: 2,
      error: null
    }
    const item = npcLookupImportProgressItem(run, 'NPC races')

    expect(item.detail).toBe('Restore defaults')
    expect(item.stats.map(stat => [stat.label, stat.value])).toEqual([
      ['inserted', 1],
      ['restored', 2],
      ['already default', 4]
    ])
    expect(importProgressPercent(item)).toBe(100)
  })
})
