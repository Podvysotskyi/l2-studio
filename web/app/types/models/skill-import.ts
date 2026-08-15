export type SkillImportMode = 'add_missing' | 'restore_defaults'

export interface SkillImportRun {
  id: string
  mode: SkillImportMode
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
