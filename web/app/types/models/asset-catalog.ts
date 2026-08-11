export type TextureImportKind = 'systextures' | 'textures'
export type AssetImportKind =
  | TextureImportKind
  | 'music'
  | 'sounds'
  | 'staticmeshes'
  | 'levels'
  | 'levelpreviews'
  | 'scenes'

export interface AssetCatalogSummary {
  kind: AssetImportKind
  sourceFolder: string
  sourceHash: string
  schemaVersion: number
  protocol: number | null
  total: number
  resolved: number
  skipped: number
  groupCount: number
  publishedAt: string
}

export interface AssetCatalogPage<TItem, TGroup = never> {
  summary: AssetCatalogSummary
  groups: TGroup[]
  items: TItem[]
  total: number
  page: number
  pageSize: number
}

export interface TexturePackage {
  name: string
  fileName: string
  sha256: string
  textureCount: number
  materialCount: number
}

export interface TextureMaterialReference {
  packageName: string
  objectName: string
  className: string
}

export interface TextureMaterialManifestEntry {
  packageName: string
  objectName: string
  className:
    | 'Shader'
    | 'FinalBlend'
    | 'Panner'
    | 'Rotator'
    | 'TexPanner'
    | 'TexRotator'
    | 'Combiner'
    | 'TexOscillator'
    | 'TexOscillatorTriggered'
    | 'ColorModifier'
    | 'FadeColor'
  material: TextureMaterialReference | null
  diffuse: TextureMaterialReference | null
  opacity: TextureMaterialReference | null
  selfIllumination: TextureMaterialReference | null
  outputBlending: number
  frameBufferBlending: number
  twoSided: boolean
  alphaTest: boolean
  alphaRef: number
  zWrite: boolean
  zTest: boolean
  material2: TextureMaterialReference | null
  mask: TextureMaterialReference | null
  panRate: number
  rotationRate: number
  combineOperation: number
  alphaOperation: number
  detail: TextureMaterialReference | null
  detailScale: number
  treatAsTwoSided: boolean
  selfIlluminationMask: TextureMaterialReference | null
  specular: TextureMaterialReference | null
  specularityMask: TextureMaterialReference | null
  performLightingOnSpecularPass: boolean
  fadeColor1: TextureMaterialColor | null
  fadeColor2: TextureMaterialColor | null
  colorFadeType: number
  fadePeriod: number
  fadePhase: number
  invertMask: boolean
  modulate2X: boolean
  modulate4X: boolean
}

export interface TextureMaterialColor {
  red: number
  green: number
  blue: number
  alpha: number
}

export interface TextureManifestEntry {
  packageName: string
  objectName: string
  url: string | null
  width: number
  height: number
  format: string
  sha256: string | null
  status: 'resolved' | 'skipped'
  error: string | null
  gpuUrl: string | null
  gpuSha256: string | null
  gpuCompressed: boolean
  mipCount: number
  animation: TextureAnimationManifestEntry | null
}

export interface TextureAnimationManifestEntry {
  frameUrls: string[]
  minFrameRate: number
  maxFrameRate: number
}

export interface MusicManifestEntry {
  name: string
  fileName: string
  url: string | null
  durationSeconds: number | null
  sampleRate: number | null
  channels: number | null
  sizeBytes: number
  sha256: string | null
  status: 'resolved' | 'skipped'
  error: string | null
}

export interface SoundManifestEntry {
  packageName: string
  objectName: string
  url: string
  durationSeconds: number
  sampleRate: number
  channels: number
  sizeBytes: number
  sha256: string
}

export interface StaticMeshPackage {
  name: string
  fileName: string
  sha256: string
  meshCount: number
}

export interface StaticMeshManifestEntry {
  packageName: string
  objectName: string
  url: string | null
  vertexCount: number
  triangleCount: number
  sectionCount: number
  materialCount: number
  resolvedMaterialCount: number
  materialStatus: 'resolved' | 'partial' | 'unresolved' | 'none'
  materialError: string | null
  sha256: string | null
  status: 'resolved' | 'skipped'
  error: string | null
}
