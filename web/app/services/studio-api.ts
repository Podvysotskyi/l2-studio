import type {
  AssetCatalogPage,
  AssetCatalogSummary,
  AssetImportKind
} from '../types/models/asset-catalog'
import type {
  AssetImportDiagnostic,
  AssetImportJob,
  AssetImportPage,
  AssetImportWorkItem
} from '../types/models/asset-import-job'
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

export function startAssetFileImport(
  kind: AssetImportKind,
  fileName: string
): Promise<AssetImportJob> {
  return $fetch<AssetImportJob>(
    `/api/assets/${kind}/imports/files/${encodeURIComponent(fileName)}`,
    { method: 'POST' }
  )
}

export function getAssetImportWorkItems(
  kind: AssetImportKind,
  runId: string,
  request: {
    sourceKey?: string
    status?: string
    page?: number
    pageSize?: number
  } = {}
): Promise<AssetImportPage<AssetImportWorkItem>> {
  return $fetch<AssetImportPage<AssetImportWorkItem>>(
    `/api/assets/${kind}/imports/${runId}/work-items`,
    {
      query: {
        ...(request.sourceKey ? { sourceKey: request.sourceKey } : {}),
        ...(request.status ? { status: request.status } : {}),
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
    page?: number
    pageSize?: number
  } = {}
): Promise<AssetImportPage<AssetImportDiagnostic>> {
  return $fetch<AssetImportPage<AssetImportDiagnostic>>(
    `/api/assets/${kind}/imports/${runId}/diagnostics`,
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
