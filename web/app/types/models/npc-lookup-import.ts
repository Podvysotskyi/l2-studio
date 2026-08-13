import type { NpcLookupKind } from './content-directory'

export interface NpcLookupImportRun {
  id: string
  kind: NpcLookupKind
  status: 'queued' | 'running' | 'succeeded' | 'failed'
  requestedAt: string
  startedAt: string | null
  finishedAt: string | null
  totalCount: number
  insertedCount: number
  existingCount: number
  error: string | null
}
