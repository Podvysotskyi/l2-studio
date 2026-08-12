export type AssetReleaseStatus = 'draft' | 'published' | 'active' | 'retired'
export type AssetReleaseValidationStatus =
  | 'not_validated'
  | 'queued'
  | 'running'
  | 'valid'
  | 'invalid'

export interface AssetReleaseSummary {
  id: string
  name: string
  notes: string | null
  status: AssetReleaseStatus
  validationStatus: AssetReleaseValidationStatus
  snapshotHash: string
  rootArtifactCount: number
  artifactCount: number
  sizeBytes: number
  manifestPath: string | null
  manifestHash: string | null
  createdAt: string
  updatedAt: string
  publishedAt: string | null
  retiredAt: string | null
  isActive: boolean
  isDesired: boolean
}

export interface AssetReleaseEntrypoints {
  loginSceneFileId: number | null
  loginScenePath: string | null
  loginCameraSequence: string | null
  loginMusicFileId: number | null
  loginMusicPath: string | null
  primaryLogoFileId: number | null
  primaryLogoPath: string | null
  versionLogoFileId: number | null
  versionLogoPath: string | null
  loadingArtworkFileId: number | null
  loadingArtworkPath: string | null
  characterSelectionSceneFileId: number | null
  characterSelectionScenePath: string | null
  characterSelectionCameraSequence: string | null
}

export interface AssetReleaseValidationIssue {
  code: string
  field: string | null
  message: string
}

export interface AssetReleaseArtifact {
  artifactId: string
  kind: string
  sourceKey: string
  buildFingerprint: string
  integrityStatus: string
  sizeBytes: number
  isRoot: boolean
}

export interface AssetReleaseEvent {
  id: number
  action: string
  occurredAt: string
}

export interface AssetReleaseDetail {
  release: AssetReleaseSummary
  entrypoints: AssetReleaseEntrypoints
  validationIssues: AssetReleaseValidationIssue[]
  artifacts: AssetReleaseArtifact[]
  events: AssetReleaseEvent[]
  pointerStatus: string
  pointerError: string | null
}

export interface AssetReleasePage {
  items: AssetReleaseSummary[]
  total: number
  page: number
  pageSize: number
}

export interface AssetReleaseResourceOption {
  fileId: number
  artifactId: string
  kind: string
  sourceKey: string
  label: string
  publicPath: string
  mediaType: string
  cameraSequences: string[]
}

export interface AssetReleaseResourcePage {
  items: AssetReleaseResourceOption[]
  total: number
  page: number
  pageSize: number
}
