import type { AssetImportKind } from './asset-catalog'

export type AssetImportStatus =
  | 'queued'
  | 'running'
  | 'succeeded'
  | 'succeeded_with_warnings'
  | 'failed'

export interface AssetImportJob {
  id: string
  kind: AssetImportKind
  status: AssetImportStatus
  sourcePath: string
  sourceHash: string | null
  requestedAt: string
  startedAt: string | null
  finishedAt: string | null
  totalCount: number
  processedCount: number
  skippedCount: number
  warnings: string[]
  error: string | null
}
