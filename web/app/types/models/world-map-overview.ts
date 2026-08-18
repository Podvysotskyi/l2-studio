export interface WorldMapOverviewReference {
  manifestUrl: string
  terrainResolution: number
}

export interface WorldMapOverviewManifest {
  schemaVersion: number
  terrainResolution: number
  tiles: WorldMapOverviewTerrainTile[]
}

export interface WorldMapOverviewTerrainTile {
  name: string
  meshUrl: string
}
