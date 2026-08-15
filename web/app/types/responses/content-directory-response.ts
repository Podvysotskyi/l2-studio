import type { NpcRecord, SkillRecord } from '../models/content-directory'

export interface DirectoryPage<TItem> {
  items: TItem[]
  total: number
  page: number
  pageSize: number
}

export type NpcPage = DirectoryPage<NpcRecord>

export type SkillPage = DirectoryPage<SkillRecord>
