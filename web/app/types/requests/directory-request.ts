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
