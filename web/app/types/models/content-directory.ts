export interface NpcRecord {
  id: number
  appearanceId: number | null
  level: number
  name: string | null
  npcTypeName: string
  npcTypeDisplayName: string
  npcRaceName: string | null
  npcRaceDisplayName: string | null
  npcSexName: string
  npcSexDisplayName: string
  hasVisuals: boolean
  status: NpcStatusRecord | null
  stats: NpcStatsRecord | null
  statsVitals: NpcStatsVitalsRecord | null
  statsAttack: NpcStatsAttackRecord | null
  statsDefence: NpcStatsDefenceRecord | null
  statsSpeed: NpcStatsSpeedRecord | null
}

export interface NpcStatusRecord {
  attackable: boolean
  targetable: boolean
  talkable: boolean
  undying: boolean
  showName: boolean
  randomWalk: boolean
  canMove: boolean
  noSleepMode: boolean
  canBeSown: boolean
}

export interface NpcStatsRecord {
  str: number | null
  int: number | null
  dex: number | null
  wit: number | null
  con: number | null
  men: number | null
  hitTime: number | null
}

export interface NpcStatsVitalsRecord {
  hp: number | null
  hpRegen: number | null
  mp: number | null
  mpRegen: number | null
}

export interface NpcStatsAttackRecord {
  physical: number | null
  magical: number | null
  random: number | null
  critical: number | null
  accuracy: number | null
  attackSpeed: number | null
  reuseDelay: number | null
  type: string | null
  range: number | null
  distance: number | null
  width: number | null
}

export interface NpcStatsDefenceRecord {
  physical: number | null
  magical: number | null
  evasion: number | null
  shield: number | null
  shieldRate: number | null
}

export interface NpcStatsSpeedRecord {
  walkGround: number | null
  runGround: number | null
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

export interface PlayerAppearanceRecord {
  id: number
  name: string
  playerRaceId: number
  playerRaceName: string
  playerSexId: number
  playerSexName: string
}

export type PlayerAppearanceKind =
  | 'player-faces'
  | 'player-hair-styles'
  | 'player-hair-colors'

export interface SkillRecord {
  id: number
  levels: number
  name: string
  skillOperateTypeName: string | null
  skillOperateTypeDisplayName: string | null
  skillTargetTypeName: string | null
  skillTargetTypeDisplayName: string | null
  iconCount: number
}

export interface SkillLookupRecord {
  name: string
  displayName: string
}

export type SkillLookupKind = 'skill-operate-types' | 'skill-target-types'

export interface LookupRecord {
  id: number
  name: string
}

export interface NpcLookupRecord {
  name: string
  displayName: string
}

export type NpcLookupKind = 'npc-types' | 'npc-races' | 'npc-sexes'
export type NpcImportKind = NpcLookupKind | 'npcs'
export type NpcVisualFilter = 'with' | 'without'

export type LookupKind =
  | 'player-races'
  | 'player-sexes'
