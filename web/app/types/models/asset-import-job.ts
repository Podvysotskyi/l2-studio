import type { AssetImportKind } from './asset-catalog'

export type AssetImportStatus =
  | 'queued'
  | 'discovering'
  | 'running'
  | 'succeeded'
  | 'succeeded_with_warnings'
  | 'failed'

export type AssetImportTriggerType = 'full_scan' | 'single_file'
export type AssetImportWorkItemStatus =
  | 'queued'
  | 'running'
  | 'succeeded'
  | 'succeeded_with_warnings'
  | 'failed'

export interface AssetImportRun {
  id: string
  kind: AssetImportKind
  triggerType: AssetImportTriggerType
  status: AssetImportStatus
  requestedSourceKey: string | null
  requestedAt: string
  startedAt: string | null
  discoveryFinishedAt: string | null
  finishedAt: string | null
  discoveredFileCount: number
  completedFileCount: number
  succeededFileCount: number
  warningFileCount: number
  failedFileCount: number
  error: string | null
}

export type AssetImportJob = AssetImportRun

export interface AssetImportWorkItem {
  id: string
  runId: string
  importKind: AssetImportKind
  sourceKey: string
  sourcePath: string
  sourceHash: string | null
  status: AssetImportWorkItemStatus
  attemptCount: number
  createdAt: string
  startedAt: string | null
  finishedAt: string | null
  totalResourceCount: number
  processedResourceCount: number
  skippedResourceCount: number
  warningCount: number
  error: string | null
  unpublishedAt: string | null
}

export interface AssetImportDiagnostic {
  id: number
  runId: string
  workItemId: string | null
  severity: 'warning' | 'error'
  code: string
  stage: string
  sourceKey: string | null
  objectName: string | null
  message: string
  createdAt: string
}

export interface AssetImportPage<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}
