export interface DirectoryRequest {
  query?: string
  page?: number
  pageSize?: number
}

export interface NpcDirectoryRequest extends DirectoryRequest {
  npcTypeName?: string
  npcRaceName?: string
  withoutRace?: boolean
  npcSexName?: string
  hasVisuals?: boolean
}

export interface ItemDirectoryRequest extends DirectoryRequest {
  itemTypeName?: string
  itemActionName?: string
  itemBodyPartName?: string
  itemMaterialName?: string
  itemCrystalTypeName?: string
  handlerName?: string
}

export type ItemFamily = 'armor' | 'weapon' | 'arrow' | 'material' | 'potion' | 'recipe' | 'enchant' | 'scroll' | 'pet-collar' | 'etc'

export interface PlayerAppearanceDirectoryRequest extends DirectoryRequest {
  playerRaceId?: number
  playerSexId?: number
}
