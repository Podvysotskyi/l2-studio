import type { NpcRecord, SkillRecord } from '../models/content-directory'

export interface NpcPage {
  items: NpcRecord[]
  total: number
  page: number
  pageSize: number
}

export interface SkillPage {
  items: SkillRecord[]
  total: number
  page: number
  pageSize: number
}
