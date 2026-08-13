import type { AssetImportJob } from '../types/models/asset-import-job'
import type { NpcLookupImportRun } from '../types/models/npc-lookup-import'

export type ImportProgressStatus = AssetImportJob['status'] | NpcLookupImportRun['status']
export type ImportProgressColor = 'neutral' | 'primary' | 'success' | 'warning' | 'error'

export interface ImportProgressStat {
  label: string
  value: number
  color: ImportProgressColor
}

export interface ImportProgressItem {
  id: string
  label: string
  detail: string
  status: ImportProgressStatus
  completed: number
  total: number
  stats: ImportProgressStat[]
  error: string | null
}

export function assetImportProgressItem(
  job: AssetImportJob,
  label: string
): ImportProgressItem {
  return {
    id: job.id,
    label,
    detail: job.requestedSourceKey ?? 'Full scan',
    status: job.status,
    completed: job.completedFileCount,
    total: job.discoveredFileCount,
    stats: [
      { label: 'succeeded', value: job.succeededFileCount, color: 'success' },
      { label: 'warnings', value: job.warningFileCount, color: 'warning' },
      { label: 'failed', value: job.failedFileCount, color: 'error' },
      { label: 'reused', value: job.reusedFileCount, color: 'neutral' }
    ],
    error: job.error
  }
}

export function npcLookupImportProgressItem(
  run: NpcLookupImportRun,
  label: string
): ImportProgressItem {
  const processed = run.status === 'succeeded' || run.status === 'failed'
    ? run.totalCount
    : Math.min(run.totalCount, run.insertedCount + run.existingCount)
  const stats: ImportProgressStat[] = run.mode === 'restore_defaults'
    ? [
        { label: 'inserted', value: run.insertedCount, color: 'success' },
        { label: 'restored', value: run.restoredCount, color: 'warning' },
        {
          label: 'already default',
          value: Math.max(0, run.existingCount - run.restoredCount),
          color: 'neutral'
        }
      ]
    : [
        { label: 'inserted', value: run.insertedCount, color: 'success' },
        { label: 'already existed', value: run.existingCount, color: 'neutral' }
      ]

  return {
    id: run.id,
    label,
    detail: run.mode === 'restore_defaults' ? 'Restore defaults' : 'Import missing',
    status: run.status,
    completed: processed,
    total: run.totalCount,
    stats,
    error: run.error
  }
}

export function isActiveImportStatus(status: ImportProgressStatus) {
  return status === 'queued' || status === 'discovering' || status === 'running'
}

export function importProgressPercent(item: ImportProgressItem): number | undefined {
  if (isActiveImportStatus(item.status) && !item.total) return undefined
  if (!isActiveImportStatus(item.status)) return 100
  return Math.round((Math.min(item.completed, item.total) / item.total) * 100)
}
