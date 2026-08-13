import type { NpcLookupKind } from './content-directory'

export type NpcLookupImportMode = 'add_missing' | 'restore_defaults'

export interface NpcLookupImportRun {
  id: string
  kind: NpcLookupKind
  mode: NpcLookupImportMode
  status: 'queued' | 'running' | 'succeeded' | 'failed'
  requestedAt: string
  startedAt: string | null
  finishedAt: string | null
  totalCount: number
  insertedCount: number
  existingCount: number
  restoredCount: number
  error: string | null
}
