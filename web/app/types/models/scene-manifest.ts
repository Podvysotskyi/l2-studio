import type {
  LevelActorManifestEntry,
  LevelBspMeshManifestEntry,
  LevelEnvironmentManifestEntry,
  LevelLightManifestEntry,
  LevelRotation,
  LevelTerrainManifestEntry,
  LevelVector,
  LevelWaterVolumeManifestEntry
} from './level-manifest'
import type { ParticleEmitterManifestEntry } from './particle-manifest'
import type { SkyBackdropManifestEntry, SkyZoneManifestEntry } from './sky-manifest'

export interface SceneCatalogEntry {
  name: string
  fileName: string
  manifestUrl: string | null
  terrainCount: number
  actorCount: number
  cinematicObjectCount: number
  sha256: string
  status: 'resolved' | 'skipped'
  error: string | null
}

export interface SceneObjectManifestEntry {
  order: number
  name: string
  className: string
  location: LevelVector
  rotation: LevelRotation
  duration: number
  target: string | null
  properties: Record<string, string>
  resourceUrl?: string | null
  owner?: string | null
  particle?: ParticleEmitterManifestEntry | null
  diagnostic?: string | null
}

export interface SceneManifest {
  schemaVersion: number
  name: string
  fileName: string
  sourceHash: string
  protocol: number
  environment: LevelEnvironmentManifestEntry
  terrains: LevelTerrainManifestEntry[]
  actors: LevelActorManifestEntry[]
  lights: LevelLightManifestEntry[]
  waterVolumes: LevelWaterVolumeManifestEntry[]
  skyZones: SkyZoneManifestEntry[]
  bspMeshes: LevelBspMeshManifestEntry[]
  skyBackdrops: SkyBackdropManifestEntry[]
  cameras: SceneObjectManifestEntry[]
  interpolationPoints: SceneObjectManifestEntry[]
  sceneManagers: SceneObjectManifestEntry[]
  actions: SceneObjectManifestEntry[]
  ambientSounds: SceneObjectManifestEntry[]
  effects: SceneObjectManifestEntry[]
  unrepresentedObjectClasses: Record<string, number>
  gpuTextureFormats?: string[]
}
