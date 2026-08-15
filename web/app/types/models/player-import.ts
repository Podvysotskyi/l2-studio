export type PlayerImportMode = 'add_missing' | 'restore_defaults'

export interface PlayerImportRun {
  id: string
  mode: PlayerImportMode
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
