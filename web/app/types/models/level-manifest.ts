import type { SkyZoneManifestEntry } from './sky-manifest'

export interface LevelVector {
  x: number
  y: number
  z: number
}

export interface LevelRotation {
  pitch: number
  yaw: number
  roll: number
}

export interface LevelCatalogEntry {
  name: string
  fileName: string
  manifestUrl: string | null
  terrainCount: number
  actorCount: number
  waterVolumeCount: number
  sha256: string
  status: 'resolved' | 'skipped'
  error: string | null
}

export interface LevelPreviewCatalogEntry {
  name: string
  levelSourceHash: string
  imageUrl: string | null
  width: number
  height: number
  status: 'resolved' | 'skipped'
  error: string | null
}

export interface LevelTerrainManifestEntry {
  name: string
  location: LevelVector
  rotation: LevelRotation
  scale: LevelVector
  heightmap: string | null
  heightmapWidth: number
  heightmapHeight: number
  meshUrl: string | null
  layers: LevelTerrainLayerManifestEntry[]
  controlMapUrls: string[]
  controlMapWidth: number
  controlMapHeight: number
  controlMapEncoding: 'webp-rgb-a-horizontal'
  controlMapArrayGroup: number
  materialStatus: 'resolved' | 'skipped'
  materialError: string | null
}

export interface LevelTerrainLayerManifestEntry {
  index: number
  texturePackage: string | null
  textureObject: string | null
  textureUrl: string | null
  textureWidth: number
  textureHeight: number
  textureArrayGroup: number
  textureArrayLayer: number
  alphaPackage: string | null
  alphaObject: string | null
  controlMapIndex: number
  controlMapChannel: number
  uScale: number
  vScale: number
  uPan: number
  vPan: number
  textureMapAxis: 'xy' | 'xz' | 'yz' | 'unknown'
  textureRotation: number
  layerRotation: LevelRotation
  uvTransform: LevelTerrainUvTransform
}

export interface LevelTerrainUvTransformRow {
  x: number
  y: number
  z: number
  offset: number
}

export interface LevelTerrainUvTransform {
  u: LevelTerrainUvTransformRow
  v: LevelTerrainUvTransformRow
}

export interface LevelActorManifestEntry {
  name: string
  className: string
  location: LevelVector
  rotation: LevelRotation
  prePivot: LevelVector
  drawScale: number
  drawScale3D: LevelVector
  meshPackage: string | null
  meshObject: string | null
  meshUrl: string | null
  vertexLighting: LevelVertexLightingReference | null
}

export interface LevelVertexLightingReference {
  url: string
  textureWidth: number
  textureHeight: number
  texelOffset: number
  vertexCount: number
}

export interface LevelEnvironmentManifestEntry {
  ambientColor: { r: number; g: number; b: number }
  ambientBrightness: number
  distanceFog: {
    color: { r: number; g: number; b: number }
    start: number
    end: number
  } | null
}

export interface LevelLightManifestEntry {
  name: string
  className: string
  location: LevelVector
  rotation: LevelRotation
  brightness: number
  hue: number
  saturation: number
  radius: number
  properties?: Record<string, string>
  resourceUrl?: string | null
}

export interface LevelWaterVolumeManifestEntry {
  name: string
  className: string
  brushName: string | null
  location: LevelVector
  rotation: LevelRotation
  prePivot: LevelVector
  drawScale: number
  drawScale3D: LevelVector
  meshUrl: string | null
  vertexCount: number
  triangleCount: number
  status: 'resolved' | 'skipped'
  error: string | null
}

export interface LevelBspMeshManifestEntry {
  name: string
  modelName: string
  role: 'geometry' | 'water-surface' | 'sky-zone' | 'world-base'
  skyZone: string | null
  waterVolumeNames: string[]
  meshUrl: string | null
  vertexCount: number
  triangleCount: number
  surfaceCount: number
  materialCount: number
  resolvedMaterialCount: number
  materialStatus: 'resolved' | 'partial' | 'unresolved' | 'none'
  polyFlags: number
  splitterNodeCount: number
  invisibleSurfaceCount: number
  portalSurfaceCount: number
  fakeBackdropSurfaceCount: number
  malformedSurfaceCount: number
  unresolvedMaterialReferenceCount: number
  error: string | null
}

export interface LevelManifest {
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
  unrepresentedObjectClasses: Record<string, number>
  gpuTextureFormats?: string[]
}
