export interface NpcRecord {
  id: number
  level: number
  name: string | null
  npcTypeName: string
  npcTypeDisplayName: string
  npcRaceName: string | null
  npcRaceDisplayName: string | null
  npcSexName: string
  npcSexDisplayName: string
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

export interface NpcLookupRecord {
  name: string
  displayName: string
}

export type NpcLookupKind = 'npc-types' | 'npc-races' | 'npc-sexes'

export type LookupKind =
  | 'player-races'
  | 'player-sexes'
  | 'skill-operate-types'
  | 'skill-target-types'
