import type { AssetCatalogPage, AssetCatalogSummary, AssetImportKind } from '@podvysotskyi/l2-ui'
import type { AssetImportJob } from '../types/models/asset-import-job'
import type { LookupKind, LookupRecord, PlayerClassRecord } from '../types/models/content-directory'
import type { DirectoryRequest } from '../types/requests/directory-request'
import type { NpcPage, SkillPage } from '../types/responses/content-directory-response'
import type { StudioServiceInfo } from '../types/responses/studio-service-info'
import {
  assetCatalogEntryUrl,
  assetCatalogsUrl,
  assetCatalogUrl,
  assetImportsUrl,
  lookupUrl,
  npcDirectoryUrl,
  playerClassDirectoryUrl,
  skillDirectoryUrl
} from '../utils/studio-content'
import { systemInfoUrl } from '../utils/system-info'

const apiBase = ''

export function getStudioServiceInfo(): Promise<StudioServiceInfo> {
  return $fetch<StudioServiceInfo>(systemInfoUrl(apiBase))
}

export function getNpcDirectory(request: DirectoryRequest = {}): Promise<NpcPage> {
  return $fetch<NpcPage>(npcDirectoryUrl(apiBase, request))
}

export function getSkillDirectory(request: DirectoryRequest = {}): Promise<SkillPage> {
  return $fetch<SkillPage>(skillDirectoryUrl(apiBase, request))
}

export function getLookupDirectory(kind: LookupKind): Promise<LookupRecord[]> {
  return $fetch<LookupRecord[]>(lookupUrl(apiBase, kind))
}

export function getPlayerClasses(): Promise<PlayerClassRecord[]> {
  return $fetch<PlayerClassRecord[]>(playerClassDirectoryUrl(apiBase))
}

export function getAssetCatalogs(): Promise<AssetCatalogSummary[]> {
  return $fetch<AssetCatalogSummary[]>(assetCatalogsUrl(apiBase))
}

export function getAssetCatalog<T, TPackage = never>(
  kind: AssetImportKind,
  request: { query?: string; packageName?: string; page?: number; pageSize?: number } = {}
): Promise<AssetCatalogPage<T, TPackage>> {
  return $fetch<AssetCatalogPage<T, TPackage>>(
    assetCatalogUrl(apiBase, kind, request)
  )
}

export function getAssetCatalogEntry<T>(
  kind: 'levels' | 'scenes',
  name: string
): Promise<T> {
  return $fetch<T>(assetCatalogEntryUrl(apiBase, kind, name))
}

export function getAssetImportJobs(
  kind: AssetImportKind,
  limit = 20
): Promise<AssetImportJob[]> {
  return $fetch<AssetImportJob[]>(assetImportsUrl(apiBase, kind), { query: { limit } })
}

export function startAssetImport(kind: AssetImportKind): Promise<AssetImportJob> {
  return $fetch<AssetImportJob>(assetImportsUrl(apiBase, kind), { method: 'POST' })
}
