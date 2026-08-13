import type { SkyZoneManifestEntry } from './sky-manifest'

export interface MapVector {
  x: number
  y: number
  z: number
}

export interface MapRotation {
  pitch: number
  yaw: number
  roll: number
}

export interface MapCatalogEntry {
  name: string
  fileName: string
  manifestUrl: string | null
  terrainCount: number
  actorCount: number
  waterVolumeCount: number
  sha256: string
  status: 'resolved' | 'skipped'
  error: string | null
  sourceKey: string
}

export interface MapPreviewCatalogEntry {
  name: string
  mapSourceHash: string
  imageUrl: string | null
  width: number
  height: number
  status: 'resolved' | 'skipped'
  error: string | null
  sourceKey: string
}

export interface MapTerrainManifestEntry {
  name: string
  location: MapVector
  rotation: MapRotation
  scale: MapVector
  heightmap: string | null
  heightmapWidth: number
  heightmapHeight: number
  meshUrl: string | null
  layers: MapTerrainLayerManifestEntry[]
  controlMapUrls: string[]
  controlMapWidth: number
  controlMapHeight: number
  controlMapEncoding: 'webp-rgb-a-horizontal'
  controlMapArrayGroup: number
  materialStatus: 'resolved' | 'skipped'
  materialError: string | null
}

export interface MapTerrainLayerManifestEntry {
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
  layerRotation: MapRotation
  uvTransform: MapTerrainUvTransform
}

export interface MapTerrainUvTransformRow {
  x: number
  y: number
  z: number
  offset: number
}

export interface MapTerrainUvTransform {
  u: MapTerrainUvTransformRow
  v: MapTerrainUvTransformRow
}

export interface MapActorManifestEntry {
  name: string
  className: string
  location: MapVector
  rotation: MapRotation
  prePivot: MapVector
  drawScale: number
  drawScale3D: MapVector
  meshPackage: string | null
  meshObject: string | null
  meshUrl: string | null
  vertexLighting: MapVertexLightingReference | null
}

export interface MapVertexLightingReference {
  url: string
  textureWidth: number
  textureHeight: number
  texelOffset: number
  vertexCount: number
}

export interface MapEnvironmentManifestEntry {
  ambientColor: { r: number; g: number; b: number }
  ambientBrightness: number
  distanceFog: {
    color: { r: number; g: number; b: number }
    start: number
    end: number
  } | null
}

export interface MapLightManifestEntry {
  name: string
  className: string
  location: MapVector
  rotation: MapRotation
  brightness: number
  hue: number
  saturation: number
  radius: number
  properties?: Record<string, string>
  resourceUrl?: string | null
}

export interface MapWaterVolumeManifestEntry {
  name: string
  className: string
  brushName: string | null
  location: MapVector
  rotation: MapRotation
  prePivot: MapVector
  drawScale: number
  drawScale3D: MapVector
  meshUrl: string | null
  vertexCount: number
  triangleCount: number
  status: 'resolved' | 'skipped'
  error: string | null
}

export interface MapBspMeshManifestEntry {
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

export interface MapManifest {
  schemaVersion: number
  name: string
  fileName: string
  sourceHash: string
  protocol: number
  environment: MapEnvironmentManifestEntry
  terrains: MapTerrainManifestEntry[]
  actors: MapActorManifestEntry[]
  lights: MapLightManifestEntry[]
  waterVolumes: MapWaterVolumeManifestEntry[]
  skyZones: SkyZoneManifestEntry[]
  bspMeshes: MapBspMeshManifestEntry[]
  unrepresentedObjectClasses: Record<string, number>
  gpuTextureFormats?: string[]
}
