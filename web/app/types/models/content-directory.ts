export interface NpcRecord {
  id: number
  level: number
  name: string | null
  npcTypeId: number
  npcType: string
  npcRaceId: number | null
  npcRace: string | null
  npcSexId: number
  npcSex: string
}

export interface PlayerClassRecord {
  id: number
  name: string
  parentClassId: number | null
  isMage: boolean
  allowedRaces: PlayerClassRaceRecord[]
}

export interface PlayerClassRaceRecord {
  id: number
  name: string
  allowedSexes: LookupRecord[]
}

export type PlayerClassStage = 'Base' | 'First' | 'Second' | 'Third'

export interface PlayerClassNode extends PlayerClassRecord {
  parentName: string | null
  depth: number
  stage: PlayerClassStage
  children: PlayerClassNode[]
}

export interface SkillRecord {
  id: number
  levels: number
  name: string
  skillOperateTypeId: number | null
  skillOperateType: string | null
  skillTargetTypeId: number | null
  skillTargetType: string | null
  iconCount: number
}

export interface LookupRecord {
  id: number
  name: string
}

export type LookupKind =
  | 'player-races'
  | 'player-sexes'
  | 'npc-races'
  | 'npc-sexes'
  | 'npc-types'
  | 'skill-operate-types'
  | 'skill-target-types'
