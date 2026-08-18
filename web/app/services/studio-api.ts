import type {
  AssetCatalogPage,
  AssetCatalogSummary,
  AssetArtifactDetail,
  AssetArtifactPage,
  NpcAppearanceManifestReference,
  AssetImportKind
} from '../types/models/asset-catalog'
import type {
  AssetCatalogDiagnosticPage,
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
  NpcRecord,
  PlayerAppearanceKind,
  PlayerAppearanceRecord,
  PlayerClassRecord,
  SkillRecord,
  SkillLookupKind,
  SkillLookupRecord
} from '../types/models/content-directory'
import type {
  DirectoryRequest,
  ItemDirectoryRequest,
  NpcDirectoryRequest,
  PlayerAppearanceDirectoryRequest
} from '../types/requests/directory-request'
import type { UpdateNpcRequest } from '../types/requests/update-npc-request'
import type { ItemConditionRecord, ItemDetailRecord, ItemLookupKind, ItemLookupRecord, ItemPage, ItemPrimarySkillRecord, ItemRecord, ItemSkillRecord } from '../types/models/item'
import type { ItemSetPage, ItemSetRecord } from '../types/models/item-set'
import type { ItemRecipePage, ItemRecipeTypePage } from '../types/models/item-recipe'
import type { ItemFamily } from '../types/requests/directory-request'
import type {
  NpcPage,
  SkillPage,
  DirectoryPage
} from '../types/responses/content-directory-response'
import type { StudioServiceInfo } from '../types/responses/studio-service-info'
import type { GameVersionSummary } from '../types/models/game-version'
import type {
  ContentImportMode,
  ContentImportTarget,
  ImportJob,
  ImportJobCategory,
  ImportJobPage,
  ImportJobStatus
} from '../types/models/import-job'
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

export function getImportJobs(request: {
  category?: ImportJobCategory
  target?: string
  status?: ImportJobStatus
  query?: string
  page?: number
  pageSize?: number
} = {}): Promise<ImportJobPage> {
  return $fetch<ImportJobPage>(versionPath('/imports'), {
    query: {
      ...request,
      page: request.page ?? 1,
      pageSize: request.pageSize ?? 25
    }
  })
}

export function getImportJob(id: string): Promise<ImportJob> {
  return $fetch<ImportJob>(versionPath(`/imports/${encodeURIComponent(id)}`))
}

export function startContentImport(
  target: ContentImportTarget,
  mode: ContentImportMode
): Promise<ImportJob> {
  return $fetch<ImportJob>(versionPath(`/imports/content/${target}`), {
    method: 'POST',
    body: { mode }
  })
}

export function getNpcDirectory(
  request: NpcDirectoryRequest = {}
): Promise<NpcPage> {
  return $fetch<NpcPage>(versionPath('/content/npcs'), {
    query: npcDirectoryQuery(request)
  })
}

export function getItemDirectory(family: ItemFamily, request: ItemDirectoryRequest = {}): Promise<ItemPage> {
  return $fetch<ItemPage>(versionPath(`/content/items/${family}`), { query: itemDirectoryQuery(request) })
}

export function getItemDefinition(family: ItemFamily, id: number): Promise<ItemDetailRecord> { return $fetch<ItemDetailRecord>(versionPath(`/content/items/${family}/${id}`)) }
export function updateItemDefinition(family: ItemFamily, id: number, request: {
  name: string; itemActionName?: string | null; itemBodyPartName?: string | null
  itemMaterialName?: string | null; itemCrystalTypeName?: string | null; icon?: string | null
  weight?: number | null; price?: number | null; handlerName?: string | null
  attackGeometry?: { offsetX: number; offsetY: number; radius: number; length: number } | null
}): Promise<ItemRecord> { return $fetch<ItemRecord>(versionPath(`/content/items/${family}/${id}`), { method: 'PATCH', body: request }) }
export function deleteItemDefinition(family: ItemFamily, id: number): Promise<void> {
  return $fetch<void>(versionPath(`/content/items/${family}/${id}`), { method: 'DELETE' })
}
export function updateItemCondition(family: ItemFamily, id: number, request: {
  messageId: number; addName: boolean; isPvpFlagged: boolean | null
  playerRaces: string[]; playerCategoryTypes: string[]
}): Promise<ItemConditionRecord> {
  return $fetch<ItemConditionRecord>(versionPath(`/content/items/${family}/${id}/condition`), { method: 'PUT', body: request })
}
export function deleteItemCondition(family: ItemFamily, id: number): Promise<void> {
  return $fetch<void>(versionPath(`/content/items/${family}/${id}/condition`), { method: 'DELETE' })
}
export function getItemSetDirectory(request: { query?: string; page?: number; pageSize?: number } = {}): Promise<ItemSetPage> {
  return $fetch<ItemSetPage>(versionPath('/content/item-sets'), { query: directoryQuery(request) })
}
export function getItemRecipeDirectory(request: DirectoryRequest = {}): Promise<ItemRecipePage> {
  return $fetch<ItemRecipePage>(versionPath('/content/item-recipes'), { query: directoryQuery(request) })
}
export function getItemRecipeTypeDirectory(request: DirectoryRequest = {}): Promise<ItemRecipeTypePage> {
  return $fetch<ItemRecipeTypePage>(versionPath('/content/item-recipe-types'), { query: directoryQuery(request) })
}
export function getItemSet(id: number): Promise<ItemSetRecord> {
  return $fetch<ItemSetRecord>(versionPath(`/content/item-sets/${id}`))
}
export function updateItemSet(id: number, request: {
  skillId: number; skillLevel: number; str: number | null; dex: number | null; con: number | null
  int: number | null; wit: number | null; men: number | null
}): Promise<ItemSetRecord> {
  return $fetch<ItemSetRecord>(versionPath(`/content/item-sets/${id}`), { method: 'PATCH', body: request })
}
export function setItemPrimarySkill(family: ItemFamily, id: number, request: { skillId: number; skillLevel: number }): Promise<ItemPrimarySkillRecord> {
  return $fetch<ItemPrimarySkillRecord>(versionPath(`/content/items/${family}/${id}/primary-skill`), { method: 'PUT', body: request })
}
export function clearItemPrimarySkill(family: ItemFamily, id: number): Promise<void> {
  return $fetch<void>(versionPath(`/content/items/${family}/${id}/primary-skill`), { method: 'DELETE' })
}
export function createItemSkill(family: ItemFamily, id: number, request: {
  skillId: number; skillLevel: number; itemSkillTypeName?: string | null; chance?: number | null
}): Promise<ItemSkillRecord> {
  return $fetch<ItemSkillRecord>(versionPath(`/content/items/${family}/${id}/skills`), { method: 'POST', body: request })
}
export function updateItemSkill(family: ItemFamily, id: number, skillId: number, skillLevel: number, request: {
  itemSkillTypeName?: string | null; chance?: number | null
}): Promise<ItemSkillRecord> {
  return $fetch<ItemSkillRecord>(versionPath(`/content/items/${family}/${id}/skills/${skillId}/${skillLevel}`), { method: 'PATCH', body: request })
}
export function deleteItemSkill(family: ItemFamily, id: number, skillId: number, skillLevel: number): Promise<void> {
  return $fetch<void>(versionPath(`/content/items/${family}/${id}/skills/${skillId}/${skillLevel}`), { method: 'DELETE' })
}
export function getItemLookups(
  kind: ItemLookupKind,
  request: DirectoryRequest = {}
): Promise<DirectoryPage<ItemLookupRecord>> {
  return $fetch<DirectoryPage<ItemLookupRecord>>(versionPath(`/content/${kind}`), {
    query: directoryQuery(request)
  })
}
export function updateItemLookupDisplayName(kind: ItemLookupKind, name: string, displayName: string): Promise<ItemLookupRecord> {
  return $fetch<ItemLookupRecord>(versionPath(`/content/${kind}/${encodeURIComponent(name)}`), { method: 'PATCH', body: { displayName } })
}
export function deleteItemLookup(kind: ItemLookupKind, name: string): Promise<void> {
  return $fetch<void>(versionPath(`/content/${kind}/${encodeURIComponent(name)}`), { method: 'DELETE' })
}
export function getNpcDefinition(id: number): Promise<NpcRecord> {
  return $fetch<NpcRecord>(versionPath(`/content/npcs/${id}`))
}

export function updateNpcDefinition(
  id: number,
  request: UpdateNpcRequest
): Promise<NpcRecord> {
  return $fetch<NpcRecord>(versionPath(`/content/npcs/${id}`), {
    method: 'PATCH',
    body: request
  })
}
export function deleteNpcDefinition(id: number): Promise<void> {
  return $fetch<void>(versionPath(`/content/npcs/${id}`), { method: 'DELETE' })
}

export function getSkillDirectory(
  request: DirectoryRequest = {}
): Promise<SkillPage> {
  return $fetch<SkillPage>(versionPath('/content/skills'), {
    query: directoryQuery(request)
  })
}
export function getSkillDefinition(id: number): Promise<SkillRecord> {
  return $fetch<SkillRecord>(versionPath(`/content/skills/${id}`))
}
export function updateSkillDefinition(id: number, request: {
  name: string
  levels: number
  skillOperateTypeName?: string | null
  skillTargetTypeName?: string | null
}): Promise<SkillRecord> {
  return $fetch<SkillRecord>(versionPath(`/content/skills/${id}`), { method: 'PATCH', body: request })
}
export function deleteSkillDefinition(id: number): Promise<void> {
  return $fetch<void>(versionPath(`/content/skills/${id}`), { method: 'DELETE' })
}

export function getLookupDirectory(
  kind: LookupKind,
  request: DirectoryRequest = {}
): Promise<DirectoryPage<LookupRecord>> {
  return $fetch<DirectoryPage<LookupRecord>>(versionPath(`/content/${kind}`), {
    query: directoryQuery(request)
  })
}
export function updatePlayerLookupName(kind: Extract<LookupKind, 'player-races' | 'player-sexes'>, id: number, name: string): Promise<LookupRecord> {
  return $fetch<LookupRecord>(versionPath(`/content/${kind}/${id}`), { method: 'PATCH', body: { name } })
}
export function deletePlayerLookup(kind: Extract<LookupKind, 'player-races' | 'player-sexes'>, id: number): Promise<void> {
  return $fetch<void>(versionPath(`/content/${kind}/${id}`), { method: 'DELETE' })
}

export function getSkillLookupDirectory(
  kind: SkillLookupKind,
  request: DirectoryRequest = {}
): Promise<DirectoryPage<SkillLookupRecord>> {
  return $fetch<DirectoryPage<SkillLookupRecord>>(versionPath(`/content/${kind}`), {
    query: directoryQuery(request)
  })
}

export function updateSkillLookupDisplayName(
  kind: SkillLookupKind,
  name: string,
  displayName: string
): Promise<SkillLookupRecord> {
  return $fetch<SkillLookupRecord>(
    versionPath(`/content/${kind}/${encodeURIComponent(name)}`),
    { method: 'PATCH', body: { displayName } }
  )
}
export function deleteSkillLookup(kind: SkillLookupKind, name: string): Promise<void> {
  return $fetch<void>(versionPath(`/content/${kind}/${encodeURIComponent(name)}`), { method: 'DELETE' })
}

export function getNpcLookupDirectory(
  kind: NpcLookupKind,
  request: DirectoryRequest = {}
): Promise<DirectoryPage<NpcLookupRecord>> {
  return $fetch<DirectoryPage<NpcLookupRecord>>(versionPath(`/content/${kind}`), {
    query: directoryQuery(request)
  })
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
export function deleteNpcLookup(kind: NpcLookupKind, name: string): Promise<void> {
  return $fetch<void>(versionPath(`/content/${kind}/${encodeURIComponent(name)}`), { method: 'DELETE' })
}

export function getPlayerClasses(): Promise<PlayerClassRecord[]> {
  return $fetch<PlayerClassRecord[]>(versionPath('/content/player-classes'))
}
export function updatePlayerClass(id: number, request: { name: string; isMage: boolean; parentClassId: number | null }): Promise<PlayerClassRecord> {
  return $fetch<PlayerClassRecord>(versionPath(`/content/player-classes/${id}`), { method: 'PATCH', body: request })
}
export function deletePlayerClass(id: number): Promise<void> {
  return $fetch<void>(versionPath(`/content/player-classes/${id}`), { method: 'DELETE' })
}

export function getPlayerAppearanceDirectory(
  kind: PlayerAppearanceKind,
  request: PlayerAppearanceDirectoryRequest = {}
): Promise<DirectoryPage<PlayerAppearanceRecord>> {
  return $fetch<DirectoryPage<PlayerAppearanceRecord>>(versionPath(`/content/${kind}`), {
    query: playerAppearanceDirectoryQuery(request)
  })
}
export function updatePlayerAppearanceName(kind: PlayerAppearanceKind, item: Pick<PlayerAppearanceRecord, 'id' | 'playerRaceId' | 'playerSexId'>, name: string): Promise<PlayerAppearanceRecord> {
  return $fetch<PlayerAppearanceRecord>(versionPath(`/content/${kind}/${item.id}/races/${item.playerRaceId}/sexes/${item.playerSexId}`), { method: 'PATCH', body: { name } })
}
export function deletePlayerAppearance(kind: PlayerAppearanceKind, item: Pick<PlayerAppearanceRecord, 'id' | 'playerRaceId' | 'playerSexId'>): Promise<void> {
  return $fetch<void>(versionPath(`/content/${kind}/${item.id}/races/${item.playerRaceId}/sexes/${item.playerSexId}`), { method: 'DELETE' })
}

export function getAssetCatalogs(): Promise<AssetCatalogSummary[]> {
  return $fetch<AssetCatalogSummary[]>(versionPath('/assets/catalogs'))
}

export async function getNpcAppearanceManifest(id: number): Promise<NpcAppearanceManifestReference> {
  return $fetch<NpcAppearanceManifestReference>(
    versionPath(`/assets/npcappearances/npcs/${id}/manifest`)
  )
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
  kind: 'maps' | 'mappreviews' | 'scenes',
  name: string,
  sourceKey?: string
): Promise<T> {
  const entry = await $fetch<T>(
    versionPath(`/assets/${kind}/catalog/${encodeURIComponent(name)}`),
    { query: sourceKey ? { sourceKey } : undefined }
  ) as T
  return resolvePublishedAssetUrls(entry, String(useRuntimeConfig().public.assetBaseUrl))
}

export function getAssetCatalogDiagnostics(
  kind: AssetImportKind,
  name: string,
  request: {
    sourceKey?: string
    severity?: 'warning' | 'error'
    query?: string
    page?: number
    pageSize?: number
  } = {}
): Promise<AssetCatalogDiagnosticPage> {
  const query = request.query?.trim()
  return $fetch<AssetCatalogDiagnosticPage>(
    versionPath(
      `/assets/${kind}/catalog/${encodeURIComponent(name)}/diagnostics`
    ),
    {
      query: {
        ...(request.sourceKey ? { sourceKey: request.sourceKey } : {}),
        ...(request.severity ? { severity: request.severity } : {}),
        ...(query ? { query } : {}),
        page: request.page ?? 1,
        pageSize: request.pageSize ?? 25
      }
    }
  )
}

export function getAssetImportJobs(
  kind: AssetImportKind,
  limit = 20
): Promise<AssetImportJob[]> {
  return $fetch<AssetImportJob[]>(versionPath(`/assets/${kind}/imports`), {
    query: { limit }
  })
}

export function getAssetImportJob(
  kind: AssetImportKind,
  id: string
): Promise<AssetImportJob> {
  return $fetch<AssetImportJob>(
    versionPath(`/assets/${kind}/imports/${encodeURIComponent(id)}`)
  )
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
  kind: 'textures' | 'staticmeshes' | 'animations' | 'maps',
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

function npcDirectoryQuery(request: NpcDirectoryRequest) {
  const npcTypeName = request.npcTypeName?.trim()
  const npcRaceName = request.npcRaceName?.trim()
  const npcSexName = request.npcSexName?.trim()
  return {
    ...directoryQuery(request),
    ...(npcTypeName ? { npcTypeName } : {}),
    ...(npcRaceName ? { npcRaceName } : {}),
    ...(request.withoutRace ? { withoutRace: true } : {}),
    ...(npcSexName ? { npcSexName } : {}),
    ...(request.hasVisuals !== undefined ? { hasVisuals: request.hasVisuals } : {})
  }
}

function itemDirectoryQuery(request: ItemDirectoryRequest) {
  const itemTypeName = request.itemTypeName?.trim()
  const itemActionName = request.itemActionName?.trim()
  const itemBodyPartName = request.itemBodyPartName?.trim()
  const itemMaterialName = request.itemMaterialName?.trim()
  const itemCrystalTypeName = request.itemCrystalTypeName?.trim()
  const handlerName = request.handlerName?.trim()
  return {
    ...directoryQuery(request),
    ...(itemTypeName ? { itemTypeName } : {}),
    ...(itemActionName ? { itemActionName } : {}),
    ...(itemBodyPartName ? { itemBodyPartName } : {}),
    ...(itemMaterialName ? { itemMaterialName } : {}),
    ...(itemCrystalTypeName ? { itemCrystalTypeName } : {}),
    ...(handlerName ? { handlerName } : {})
  }
}

function playerAppearanceDirectoryQuery(request: PlayerAppearanceDirectoryRequest) {
  return {
    ...directoryQuery(request),
    ...(request.playerRaceId === undefined ? {} : { playerRaceId: request.playerRaceId }),
    ...(request.playerSexId === undefined ? {} : { playerSexId: request.playerSexId })
  }
}
