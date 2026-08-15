import type { AssetImportJob } from '../types/models/asset-import-job'
import type { ImportJob, ImportJobStatus } from '../types/models/import-job'

export type ImportProgressStatus = ImportJobStatus
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
  to?: string
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

export function importJobProgressItem(
  job: ImportJob,
  label: string
): ImportProgressItem {
  const operation = operationLabel(job.operation)
  return {
    id: job.id,
    label,
    detail: job.requestedSourceKey ? `${operation} · ${job.requestedSourceKey}` : operation,
    status: job.status,
    completed: job.completedCount,
    total: job.totalCount,
    stats: job.metrics.map(metric => ({
      label: metric.key.replaceAll('_', ' '),
      value: metric.value,
      color: metricColor(metric.key)
    })),
    error: job.error,
    to: `/pipeline/imports?job=${job.id}`
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

function operationLabel(operation: string) {
  if (operation === 'add_missing') return 'Import missing'
  if (operation === 'restore_defaults') return 'Restore defaults'
  return operation.replaceAll('_', ' ')
}

function metricColor(metric: string): ImportProgressColor {
  if (metric === 'inserted' || metric === 'succeeded') return 'success'
  if (metric === 'restored' || metric === 'warnings') return 'warning'
  if (metric === 'failed') return 'error'
  return 'neutral'
}
