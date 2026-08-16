export type ImportJobCategory = 'content' | 'asset'
export type ImportJobStatus =
  | 'queued'
  | 'discovering'
  | 'running'
  | 'succeeded'
  | 'succeeded_with_warnings'
  | 'failed'
export type ContentImportMode = 'add_missing' | 'restore_defaults'
export type ContentImportTarget =
  | 'items'
  | 'item-types'
  | 'item-actions'
  | 'item-body-parts'
  | 'item-materials'
  | 'item-crystal-types'
  | 'item-handlers'
  | 'item-skill-types'
  | 'npcs'
  | 'npc-types'
  | 'npc-races'
  | 'npc-sexes'
  | 'player-races'
  | 'player-sexes'
  | 'player-classes'
  | 'player-faces'
  | 'player-hair-styles'
  | 'player-hair-colors'
  | 'skills'
  | 'skill-operate-types'
  | 'skill-target-types'

export interface ImportJobMetric {
  key: string
  value: number
}

export interface ImportJob {
  id: string
  category: ImportJobCategory
  target: string
  operation: string
  status: ImportJobStatus
  requestedSourceKey: string | null
  force: boolean
  requestedAt: string
  startedAt: string | null
  discoveryFinishedAt: string | null
  finishedAt: string | null
  totalCount: number
  completedCount: number
  metrics: ImportJobMetric[]
  error: string | null
}

export interface ImportJobPage {
  items: ImportJob[]
  total: number
  page: number
  pageSize: number
}
