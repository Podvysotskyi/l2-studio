import { describe, expect, it } from 'vitest'
import {
  assetImportProgressItem,
  importJobProgressItem,
  importProgressPercent,
  isActiveImportStatus
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

  it('normalizes an active content import for the shared drawer', () => {
    const item = importJobProgressItem({
      id: 'job-1',
      category: 'content',
      target: 'items',
      operation: 'add_missing',
      status: 'running',
      requestedSourceKey: null,
      force: false,
      requestedAt: '2026-08-12T00:00:00Z',
      startedAt: '2026-08-12T00:00:01Z',
      discoveryFinishedAt: '2026-08-12T00:00:02Z',
      finishedAt: null,
      totalCount: 10,
      completedCount: 5,
      metrics: [
        { key: 'inserted', value: 3 },
        { key: 'existing', value: 2 }
      ],
      error: null
    }, 'Items')

    expect(item).toMatchObject({
      id: 'job-1',
      label: 'Items',
      detail: 'Import missing',
      status: 'running',
      completed: 5,
      total: 10,
      to: '/pipeline/imports?job=job-1'
    })
    expect(item.stats).toEqual([
      { label: 'inserted', value: 3, color: 'success' },
      { label: 'existing', value: 2, color: 'neutral' }
    ])
    expect(importProgressPercent(item)).toBe(50)
  })

  it('preserves completed content import results and errors', () => {
    const item = importJobProgressItem({
      id: 'job-2',
      category: 'content',
      target: 'player-classes',
      operation: 'restore_defaults',
      status: 'failed',
      requestedSourceKey: 'c1',
      force: true,
      requestedAt: '2026-08-12T00:00:00Z',
      startedAt: '2026-08-12T00:00:01Z',
      discoveryFinishedAt: '2026-08-12T00:00:02Z',
      finishedAt: '2026-08-12T00:00:03Z',
      totalCount: 8,
      completedCount: 4,
      metrics: [
        { key: 'restored', value: 3 },
        { key: 'failed', value: 1 }
      ],
      error: 'Source record could not be converted.'
    }, 'Player classes')

    expect(item.detail).toBe('Restore defaults · c1')
    expect(item.error).toBe('Source record could not be converted.')
    expect(item.stats).toEqual([
      { label: 'restored', value: 3, color: 'warning' },
      { label: 'failed', value: 1, color: 'error' }
    ])
    expect(importProgressPercent(item)).toBe(100)
  })

})
