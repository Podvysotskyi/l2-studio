import type {
  AssetCatalogPage,
  AssetCatalogSummary,
  AssetArtifactDetail,
  AssetArtifactPage,
  AssetImportKind
} from '../types/models/asset-catalog'
import type {
  AssetImportDiagnostic,
  AssetImportJob,
  AssetImportPage,
  AssetImportWorkItem,
  StaleAssetSource
} from '../types/models/asset-import-job'
import type {
  LookupKind,
  LookupRecord,
  NpcLookupKind,
  NpcLookupRecord,
  PlayerClassRecord
} from '../types/models/content-directory'
import type { DirectoryRequest } from '../types/requests/directory-request'
import type {
  NpcPage,
  SkillPage
} from '../types/responses/content-directory-response'
import type { StudioServiceInfo } from '../types/responses/studio-service-info'
import type { GameVersionSummary } from '../types/models/game-version'
import type {
  NpcLookupImportMode,
  NpcLookupImportRun
} from '../types/models/npc-lookup-import'
import type {
  AssetReleaseDetail,
  AssetReleasePage,
  AssetReleaseResourcePage,
  AssetReleaseStatus
} from '../types/models/asset-release'
import { selectedGameVersionKey } from '../utils/game-version'
import { resolvePublishedAssetUrls } from '../utils/published-asset-url'

function versionPath(path: string) {
  return `/api/game-versions/${encodeURIComponent(selectedGameVersionKey())}${path}`
}

export function getGameVersions(): Promise<GameVersionSummary[]> {
  return $fetch<GameVersionSummary[]>('/api/game-versions')
}

export function getStudioServiceInfo(): Promise<StudioServiceInfo> {
  return $fetch<StudioServiceInfo>('/api/system/info')
}

export function getNpcDirectory(
  request: DirectoryRequest = {}
): Promise<NpcPage> {
  return $fetch<NpcPage>(versionPath('/content/npcs'), {
    query: directoryQuery(request)
  })
}

export function getSkillDirectory(
  request: DirectoryRequest = {}
): Promise<SkillPage> {
  return $fetch<SkillPage>(versionPath('/content/skills'), {
    query: directoryQuery(request)
  })
}

export function getLookupDirectory(kind: LookupKind): Promise<LookupRecord[]> {
  return $fetch<LookupRecord[]>(versionPath(`/content/${kind}`))
}

export function getNpcLookupDirectory(kind: NpcLookupKind): Promise<NpcLookupRecord[]> {
  return $fetch<NpcLookupRecord[]>(versionPath(`/content/${kind}`))
}

export function updateNpcLookupDisplayName(
  kind: NpcLookupKind,
  name: string,
  displayName: string
): Promise<NpcLookupRecord> {
  return $fetch<NpcLookupRecord>(
    versionPath(`/content/${kind}/${encodeURIComponent(name)}`),
    { method: 'PATCH', body: { displayName } }
  )
}

export function getNpcLookupImportJobs(
  kind: NpcLookupKind,
  limit = 1
): Promise<NpcLookupImportRun[]> {
  return $fetch<NpcLookupImportRun[]>(versionPath(`/content/${kind}/imports`), {
    query: { limit }
  })
}

export function getNpcLookupImportJob(
  kind: NpcLookupKind,
  id: string
): Promise<NpcLookupImportRun> {
  return $fetch<NpcLookupImportRun>(versionPath(`/content/${kind}/imports/${id}`))
}

export function startNpcLookupImport(
  kind: NpcLookupKind,
  mode?: NpcLookupImportMode
): Promise<NpcLookupImportRun> {
  return $fetch<NpcLookupImportRun>(versionPath(`/content/${kind}/imports`), {
    method: 'POST',
    ...(mode ? { body: { mode } } : {})
  })
}

export function getPlayerClasses(): Promise<PlayerClassRecord[]> {
  return $fetch<PlayerClassRecord[]>(versionPath('/content/player-classes'))
}

export function getAssetCatalogs(): Promise<AssetCatalogSummary[]> {
  return $fetch<AssetCatalogSummary[]>(versionPath('/assets/catalogs'))
}

export function getAssetArtifacts(request: {
  kind?: AssetImportKind
  sourceKey?: string
  current?: boolean
  integrityStatus?: 'healthy' | 'missing' | 'corrupt'
  page?: number
  pageSize?: number
} = {}): Promise<AssetArtifactPage> {
  return $fetch<AssetArtifactPage>(versionPath('/assets/artifacts'), {
    query: {
      ...(request.kind ? { kind: request.kind } : {}),
      ...(request.sourceKey ? { sourceKey: request.sourceKey } : {}),
      ...(request.current === undefined ? {} : { current: request.current }),
      ...(request.integrityStatus
        ? { integrityStatus: request.integrityStatus }
        : {}),
      page: request.page ?? 1,
      pageSize: request.pageSize ?? 50
    }
  })
}

export function getAssetArtifact(id: string): Promise<AssetArtifactDetail> {
  return $fetch<AssetArtifactDetail>(
    versionPath(`/assets/artifacts/${encodeURIComponent(id)}`)
  )
}

export function verifyAssetArtifact(id: string): Promise<AssetArtifactDetail> {
  return $fetch<AssetArtifactDetail>(
    versionPath(`/assets/artifacts/${encodeURIComponent(id)}/verify`),
    { method: 'POST' }
  )
}

export function getAssetReleases(request: {
  status?: AssetReleaseStatus
  page?: number
  pageSize?: number
} = {}): Promise<AssetReleasePage> {
  return $fetch<AssetReleasePage>(versionPath('/asset-releases'), {
    query: {
      ...(request.status ? { status: request.status } : {}),
      page: request.page ?? 1,
      pageSize: request.pageSize ?? 25
    }
  })
}

export function getAssetRelease(id: string): Promise<AssetReleaseDetail> {
  return $fetch<AssetReleaseDetail>(versionPath(`/asset-releases/${id}`))
}

export function createAssetRelease(body: {
  name: string
  notes?: string
}): Promise<AssetReleaseDetail> {
  return $fetch<AssetReleaseDetail>(versionPath('/asset-releases'), {
    method: 'POST',
    body
  })
}

export function cloneAssetRelease(id: string, body: {
  name: string
  notes?: string
}): Promise<AssetReleaseDetail> {
  return $fetch<AssetReleaseDetail>(versionPath(`/asset-releases/${id}/clone`), {
    method: 'POST',
    body
  })
}

export function updateAssetRelease(
  id: string,
  body: Record<string, string | number | null | undefined>
): Promise<AssetReleaseDetail> {
  return $fetch<AssetReleaseDetail>(versionPath(`/asset-releases/${id}`), {
    method: 'PATCH',
    body
  })
}

export function refreshAssetRelease(id: string): Promise<AssetReleaseDetail> {
  return releaseAction(id, 'refresh')
}

export function validateAssetRelease(id: string): Promise<AssetReleaseDetail> {
  return releaseAction(id, 'validate')
}

export function publishAssetRelease(id: string): Promise<AssetReleaseDetail> {
  return releaseAction(id, 'publish')
}

export function activateAssetRelease(id: string): Promise<AssetReleaseDetail> {
  return releaseAction(id, 'activate')
}

export function retireAssetRelease(id: string): Promise<AssetReleaseDetail> {
  return releaseAction(id, 'retire')
}

export function deleteAssetRelease(id: string): Promise<void> {
  return $fetch<void>(versionPath(`/asset-releases/${id}`), { method: 'DELETE' })
}

export function getAssetReleaseResources(
  id: string,
  type: 'scene' | 'audio' | 'image',
  query = ''
): Promise<AssetReleaseResourcePage> {
  return $fetch<AssetReleaseResourcePage>(versionPath(`/asset-releases/${id}/resources`), {
    query: { type, query, page: 1, pageSize: 100 }
  })
}

function releaseAction(id: string, action: string): Promise<AssetReleaseDetail> {
  return $fetch<AssetReleaseDetail>(versionPath(`/asset-releases/${id}/${action}`), {
    method: 'POST'
  })
}

export async function getAssetCatalog<T, TPackage = never>(
  kind: AssetImportKind,
  request: {
    query?: string
    packageName?: string
    originalFolder?: string
    page?: number
    pageSize?: number
  } = {}
): Promise<AssetCatalogPage<T, TPackage>> {
  const query = request.query?.trim()
  const catalog = await $fetch<AssetCatalogPage<T, TPackage>>(
    versionPath(`/assets/${kind}/catalog`),
    {
      query: {
        ...(query ? { query } : {}),
        ...(request.packageName ? { packageName: request.packageName } : {}),
        ...(request.originalFolder ? { originalFolder: request.originalFolder } : {}),
        page: request.page ?? 1,
        pageSize: request.pageSize ?? 50
      }
    }
  )
  return resolvePublishedAssetUrls(catalog, String(useRuntimeConfig().public.assetBaseUrl))
}

export async function getAssetCatalogEntry<T>(
  kind: 'maps' | 'scenes',
  name: string,
  sourceKey?: string
): Promise<T> {
  const entry = await $fetch<T>(
    versionPath(`/assets/${kind}/catalog/${encodeURIComponent(name)}`),
    { query: sourceKey ? { sourceKey } : undefined }
  ) as T
  return resolvePublishedAssetUrls(entry, String(useRuntimeConfig().public.assetBaseUrl))
}

export function getAssetImportJobs(
  kind: AssetImportKind,
  limit = 20
): Promise<AssetImportJob[]> {
  return $fetch<AssetImportJob[]>(versionPath(`/assets/${kind}/imports`), {
    query: { limit }
  })
}

export function startAssetImport(
  kind: AssetImportKind,
  request: { force?: boolean, mapName?: string } = {}
): Promise<AssetImportJob> {
  return $fetch<AssetImportJob>(versionPath(`/assets/${kind}/imports`), {
    method: 'POST',
    body: request
  })
}

export function startAssetFileImport(
  kind: AssetImportKind,
  fileName: string,
  force = false
): Promise<AssetImportJob> {
  return $fetch<AssetImportJob>(
    versionPath(`/assets/${kind}/imports/files/${fileName
      .split('/')
      .map(encodeURIComponent)
      .join('/')}`),
    { method: 'POST', query: { force } }
  )
}

export function startAssetResourceImport(
  kind: 'textures' | 'staticmeshes' | 'maps',
  resourceName: string,
  packageName?: string,
  sourceKey?: string,
  force = false
): Promise<AssetImportJob> {
  return $fetch<AssetImportJob>(versionPath(`/assets/${kind}/imports/resources`), {
    method: 'POST',
    body: { resourceName, packageName, ...(sourceKey ? { sourceKey } : {}), force }
  })
}

export function getStaleAssetSources(
  kind: AssetImportKind
): Promise<StaleAssetSource[]> {
  return $fetch<StaleAssetSource[]>(versionPath(`/assets/${kind}/imports/stale`))
}

export function rebuildStaleAssetSources(
  kind: AssetImportKind
): Promise<AssetImportJob> {
  return $fetch<AssetImportJob>(versionPath(`/assets/${kind}/imports/stale`), {
    method: 'POST'
  })
}

export function getAssetImportWorkItems(
  kind: AssetImportKind,
  runId: string,
  request: {
    sourceKey?: string
    status?: string
    query?: string
    diagnosticSeverity?: string
    page?: number
    pageSize?: number
  } = {}
): Promise<AssetImportPage<AssetImportWorkItem>> {
  return $fetch<AssetImportPage<AssetImportWorkItem>>(
    versionPath(`/assets/${kind}/imports/${runId}/work-items`),
    {
      query: {
        ...(request.sourceKey ? { sourceKey: request.sourceKey } : {}),
        ...(request.status ? { status: request.status } : {}),
        ...(request.query ? { query: request.query } : {}),
        ...(request.diagnosticSeverity ? { diagnosticSeverity: request.diagnosticSeverity } : {}),
        page: request.page ?? 1,
        pageSize: request.pageSize ?? 50
      }
    }
  )
}

export function getAssetImportDiagnostics(
  kind: AssetImportKind,
  runId: string,
  request: {
    sourceKey?: string
    severity?: string
    code?: string
    stage?: string
    workItemStatus?: string
    query?: string
    scope?: 'run'
    page?: number
    pageSize?: number
  } = {}
): Promise<AssetImportPage<AssetImportDiagnostic>> {
  return $fetch<AssetImportPage<AssetImportDiagnostic>>(
    versionPath(`/assets/${kind}/imports/${runId}/diagnostics`),
    {
      query: {
        ...request,
        page: request.page ?? 1,
        pageSize: request.pageSize ?? 50
      }
    }
  )
}

function directoryQuery(request: DirectoryRequest) {
  const query = request.query?.trim()
  return {
    ...(query ? { query } : {}),
    page: request.page ?? 1,
    pageSize: request.pageSize ?? 25
  }
}
