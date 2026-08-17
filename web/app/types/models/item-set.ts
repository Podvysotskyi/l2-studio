export interface ItemSetBodyPartRecord {
  bodyPartName: string
  bodyPartDisplayName: string
  itemId: number
  itemName: string | null
}

export interface ItemSetSkillRecord {
  skillId: number
  skillLevel: number
  skillName: string | null
  skillLevels: number | null
}

export interface ItemSetStatsRecord {
  str: number | null
  dex: number | null
  con: number | null
  int: number | null
  wit: number | null
  men: number | null
}

export interface ItemSetRecord {
  setId: number
  bodyParts: ItemSetBodyPartRecord[]
  skill: ItemSetSkillRecord | null
  stats: ItemSetStatsRecord | null
}

export interface ItemSetPage {
  items: ItemSetRecord[]
  total: number
  page: number
  pageSize: number
}
