import type { LevelVector } from './level-manifest'

export interface SkyBackdropManifestEntry {
  name: string
  meshUrl: string | null
  skyZone: string | null
  texUPanSpeed: number
  texVPanSpeed: number
  collision: false
  error: string | null
}

export interface SkyZoneLensFlareManifestEntry {
  index: number
  texturePackage: string
  textureObject: string
  textureUrl: string | null
  offset: number
  scale: number
}

export interface SkyZoneManifestEntry {
  order: number
  name: string
  location: LevelVector
  drawScale: number
  texUPanSpeed: number
  texVPanSpeed: number
  lensFlares: SkyZoneLensFlareManifestEntry[]
}
