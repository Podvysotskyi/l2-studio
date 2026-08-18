export interface NpcSpawnWorldMap {
  zones: NpcSpawnWorldMapZone[]
  points: NpcSpawnWorldMapPoint[]
}

export interface NpcSpawnWorldMapZone {
  name: string
  minZ: number
  maxZ: number
  territoryNodes: NpcSpawnWorldMapTerritoryNode[]
  npcs: NpcSpawnWorldMapZoneNpc[]
}

export interface NpcSpawnWorldMapTerritoryNode {
  sequence: number
  x: number
  y: number
}

export interface NpcSpawnWorldMapZoneNpc {
  npcId: number
  npcName: string | null
  count: number
  respawnDelaySeconds: number
  respawnRandomSeconds: number | null
}

export interface NpcSpawnWorldMapPoint {
  spawnName: string
  sequence: number
  npcId: number
  npcName: string | null
  x: number
  y: number
  z: number
  heading: number
  respawnDelaySeconds: number
}
