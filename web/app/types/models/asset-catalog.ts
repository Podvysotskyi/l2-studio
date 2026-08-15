export type TextureImportKind = 'textures'
export type AssetImportKind =
  | TextureImportKind
  | 'music'
  | 'sounds'
  | 'staticmeshes'
  | 'animations'
  | 'npcappearances'
  | 'maps'
  | 'mappreviews'
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

export interface AssetArtifactSummary {
  id: string
  kind: AssetImportKind
  sourceKey: string
  sourceHash: string
  recipeVersion: string
  buildFingerprint: string
  contentHash: string
  outputRoot: string
  schemaVersion: number
  protocol: number | null
  fileCount: number
  sizeBytes: number
  integrityStatus: 'healthy' | 'missing' | 'corrupt'
  lastVerifiedAt: string | null
  createdAt: string
  isCurrent: boolean
}

export interface AssetArtifactFile {
  relativePath: string
  publicPath: string
  role: string
  mediaType: string
  sizeBytes: number
  sha256: string
}

export interface AssetArtifactDependency {
  kind: AssetImportKind
  dependencyKey: string
  resolvedArtifactId: string | null
  resolvedSourceKey: string | null
  buildFingerprint: string | null
  isResolved: boolean
}

export interface AssetArtifactDetail {
  artifact: AssetArtifactSummary
  files: AssetArtifactFile[]
  dependencies: AssetArtifactDependency[]
}

export interface AssetArtifactPage {
  items: AssetArtifactSummary[]
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
  originalFolder: string
  path: string
  sourceKey: string
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
  originalFolder: string
  path: string
  sourceKey: string
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
  sourceKey: string
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
  sourceKey: string
}

export interface StaticMeshPackage {
  name: string
  fileName: string
  sha256: string
  meshCount: number
  sourceKey: string
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
  sourceKey: string
}

export interface AnimationManifestPackage {
  name: string
  fileName: string
  sha256: string
  skeletalMeshCount: number
  animationSetCount: number
  clipCount: number
  notifyCount: number
  unsupportedVertexMeshCount: number
  sourceKey: string
}

export interface AnimationNotifyManifestEntry {
  normalizedTime: number
  timeSeconds: number
  functionName: string
  objectPath: string | null
  className: string | null
  properties: Record<string, string>
}

export interface AnimationClipManifestEntry {
  name: string
  frameCount: number
  frameRate: number
  durationSeconds: number
  groups: string[]
  notifies: AnimationNotifyManifestEntry[]
}

export interface AnimationMeshManifestEntry {
  packageName: string
  objectName: string
  url: string | null
  vertexCount: number
  triangleCount: number
  sectionCount: number
  boneCount: number
  skeletonSignature: string
  animationSetName: string | null
  animationUrl: string | null
  clips: AnimationClipManifestEntry[]
  materialCount: number
  resolvedMaterialCount: number
  materialStatus: 'resolved' | 'partial' | 'unresolved' | 'none' | 'unavailable'
  materialError: string | null
  defaultMaterials: AnimationMeshMaterialSlot[]
  sha256: string | null
  status: 'resolved' | 'skipped'
  error: string | null
  sourceKey: string
}

export interface AnimationMeshMaterialSlot {
  sectionIndex: number
  reference: TextureMaterialReference | null
  status: 'resolved' | 'unresolved' | 'none'
}

export interface TextureMaterialReference {
  packageName: string
  objectName: string
  className: string
}

export interface NpcAppearanceManifestReference {
  manifestUrl: string
}

export interface NpcAppearanceManifest {
  schemaVersion: number
  kind: 'npcappearances'
  sourceKey: string
  sourceHash: string
  protocol: number
  npc: NpcAppearanceManifestEntry
}

export interface NpcAssetReference {
  reference: string
  url: string | null
}

export interface NpcAnimationAssetReference extends NpcAssetReference {
  animationUrl: string | null
}

export interface NpcAppearanceMaterialBinding {
  name: string
  diffuseUrl: string | null
  opacityUrl: string | null
  emissiveUrl: string | null
  blendMode: 'opaque' | 'masked' | 'alphablend' | 'additive' | 'modulate' | 'invisible'
  doubleSided: boolean
  alphaCutoff: number
  depthWrite: boolean
  depthTest: boolean
  opacitySource: 'none' | 'texture'
  opacityChannel: 'alpha' | 'luminance'
  panRate: number
  panRateV: number
  rotationRate: number
  detailUrl: string | null
  detailScale: number
  diffuseAnimation: { frameUrls: string[], frameRate: number } | null
  opacityAnimation: { frameUrls: string[], frameRate: number } | null
  emissiveAnimation: { frameUrls: string[], frameRate: number } | null
  windMode: 'none' | 'grass' | 'foliage'
  tint: { r: number, g: number, b: number, a: number } | null
  uvOscillation: { uType: number, vType: number, uRate: number, vRate: number, uAmplitude: number, vAmplitude: number, uPhase: number, vPhase: number } | null
  unlit: boolean
  fade: unknown | null
  composite: unknown | null
  selfIlluminationMaskUrl: string | null
  specularUrl: string | null
  specularityMaskUrl: string | null
  performLightingOnSpecularPass: boolean
  clampU: boolean
  clampV: boolean
}

export interface NpcMaterialReference {
  reference: string
  url: string | null
  material: NpcAppearanceMaterialBinding | null
}

export interface NpcAppearanceMaterialSlot {
  sectionIndex: number
  defaultMaterial: NpcMaterialReference | null
  overrideMaterial: NpcMaterialReference | null
  effectiveMaterial: NpcMaterialReference | null
  effectiveSource: 'override' | 'default' | 'fallback'
  warning: string | null
}

export interface NpcAppearanceManifestEntry {
  id: number
  appearanceId: number
  appearanceName: string
  speed: number
  className: string
  mesh: NpcAnimationAssetReference
  textures: NpcMaterialReference[]
  materialSlots: NpcAppearanceMaterialSlot[]
  collisionRadius: number
  collisionHeight: number
  attackSounds: NpcAssetReference[]
  defenceSounds: NpcAssetReference[]
  damageSounds: NpcAssetReference[]
  soundVolume: number
  soundRadius: number
  soundRandomness: number
  attackEffect: NpcAssetReference
}
