import type {
  AssetCatalogPage,
  AssetCatalogSummary,
  AssetImportKind
} from '../types/models/asset-catalog'
import type { AssetImportJob } from '../types/models/asset-import-job'
import type {
  LookupKind,
  LookupRecord,
  PlayerClassRecord
} from '../types/models/content-directory'
import type { DirectoryRequest } from '../types/requests/directory-request'
import type {
  NpcPage,
  SkillPage
} from '../types/responses/content-directory-response'
import type { StudioServiceInfo } from '../types/responses/studio-service-info'

export function getStudioServiceInfo(): Promise<StudioServiceInfo> {
  return $fetch<StudioServiceInfo>('/api/system/info')
}

export function getNpcDirectory(
  request: DirectoryRequest = {}
): Promise<NpcPage> {
  return $fetch<NpcPage>('/api/content/npcs', {
    query: directoryQuery(request)
  })
}

export function getSkillDirectory(
  request: DirectoryRequest = {}
): Promise<SkillPage> {
  return $fetch<SkillPage>('/api/content/skills', {
    query: directoryQuery(request)
  })
}

export function getLookupDirectory(kind: LookupKind): Promise<LookupRecord[]> {
  return $fetch<LookupRecord[]>(`/api/content/${kind}`)
}

export function getPlayerClasses(): Promise<PlayerClassRecord[]> {
  return $fetch<PlayerClassRecord[]>('/api/content/player-classes')
}

export function getAssetCatalogs(): Promise<AssetCatalogSummary[]> {
  return $fetch<AssetCatalogSummary[]>('/api/assets/catalogs')
}

export function getAssetCatalog<T, TPackage = never>(
  kind: AssetImportKind,
  request: {
    query?: string
    packageName?: string
    page?: number
    pageSize?: number
  } = {}
): Promise<AssetCatalogPage<T, TPackage>> {
  const query = request.query?.trim()
  return $fetch<AssetCatalogPage<T, TPackage>>(
    `/api/assets/${kind}/catalog`,
    {
      query: {
        ...(query ? { query } : {}),
        ...(request.packageName ? { packageName: request.packageName } : {}),
        page: request.page ?? 1,
        pageSize: request.pageSize ?? 50
      }
    }
  )
}

export function getAssetCatalogEntry<T>(
  kind: 'levels' | 'scenes',
  name: string
): Promise<T> {
  return $fetch<T>(`/api/assets/${kind}/catalog/${encodeURIComponent(name)}`)
}

export function getAssetImportJobs(
  kind: AssetImportKind,
  limit = 20
): Promise<AssetImportJob[]> {
  return $fetch<AssetImportJob[]>(`/api/assets/${kind}/imports`, {
    query: { limit }
  })
}

export function startAssetImport(
  kind: AssetImportKind,
  query?: Record<string, string>
): Promise<AssetImportJob> {
  return $fetch<AssetImportJob>(`/api/assets/${kind}/imports`, {
    method: 'POST',
    query
  })
}

function directoryQuery(request: DirectoryRequest) {
  const query = request.query?.trim()
  return {
    ...(query ? { query } : {}),
    page: request.page ?? 1,
    pageSize: request.pageSize ?? 25
  }
}
