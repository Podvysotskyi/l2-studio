import type { AssetImportKind } from '@podvysotskyi/l2-ui'

export interface NpcRecord {
  id: number
  level: number
  name: string | null
  npcTypeId: number
  npcType: string
  npcRaceId: number | null
  npcRace: string | null
  npcSexId: number
  npcSex: string
}

export interface NpcPage {
  items: NpcRecord[]
  total: number
  page: number
  pageSize: number
}

export interface PlayerClassRecord {
  id: number
  name: string
  parentClassId: number | null
  isMage: boolean
  allowedRaces: PlayerClassRaceRecord[]
}

export interface PlayerClassRaceRecord {
  id: number
  name: string
  allowedSexes: LookupRecord[]
}

export type PlayerClassStage = 'Base' | 'First' | 'Second' | 'Third'

export interface PlayerClassNode extends PlayerClassRecord {
  parentName: string | null
  depth: number
  stage: PlayerClassStage
  children: PlayerClassNode[]
}

export interface SkillRecord {
  id: number
  levels: number
  name: string
  skillOperateTypeId: number | null
  skillOperateType: string | null
  skillTargetTypeId: number | null
  skillTargetType: string | null
  iconCount: number
}

export interface SkillPage {
  items: SkillRecord[]
  total: number
  page: number
  pageSize: number
}

export interface LookupRecord {
  id: number
  name: string
}

export type LookupKind =
  | 'player-races'
  | 'player-sexes'
  | 'npc-races'
  | 'npc-sexes'
  | 'npc-types'
  | 'skill-operate-types'
  | 'skill-target-types'

export type AssetImportStatus =
  'queued' | 'running' | 'succeeded' | 'succeeded_with_warnings' | 'failed'

export interface AssetImportJob {
  id: string
  kind: AssetImportKind
  status: AssetImportStatus
  sourcePath: string
  sourceHash: string | null
  requestedAt: string
  startedAt: string | null
  finishedAt: string | null
  totalCount: number
  processedCount: number
  skippedCount: number
  warnings: string[]
  error: string | null
}

export function npcDirectoryUrl(
  apiBase: string,
  options: { query?: string; page?: number; pageSize?: number } = {}
): string {
  const url = contentUrl(apiBase, 'npcs')
  const query = options.query?.trim()
  if (query) url.searchParams.set('query', query)
  url.searchParams.set('page', String(options.page ?? 1))
  url.searchParams.set('pageSize', String(options.pageSize ?? 25))
  return url.toString()
}

export function lookupUrl(apiBase: string, kind: LookupKind): string {
  return contentUrl(apiBase, kind).toString()
}

export function playerClassDirectoryUrl(apiBase: string): string {
  return contentUrl(apiBase, 'player-classes').toString()
}

export function buildPlayerClassHierarchy(
  records: PlayerClassRecord[]
): PlayerClassNode[] {
  const nodes = new Map<number, PlayerClassNode>()
  for (const record of [...records].sort((left, right) => left.id - right.id)) {
    nodes.set(record.id, {
      ...record,
      parentName: null,
      depth: 0,
      stage: 'Base',
      children: []
    })
  }

  const roots: PlayerClassNode[] = []
  for (const node of nodes.values()) {
    const parent =
      node.parentClassId === null ? undefined : nodes.get(node.parentClassId)
    if (!parent || parent === node) {
      roots.push(node)
      continue
    }

    node.parentName = parent.name
    parent.children.push(node)
  }

  const assignDepth = (node: PlayerClassNode, depth: number) => {
    node.depth = depth
    node.stage = playerClassStage(depth)
    for (const child of node.children) assignDepth(child, depth + 1)
  }
  for (const root of roots) assignDepth(root, 0)
  return roots
}

export function flattenPlayerClassHierarchy(
  roots: PlayerClassNode[],
  expandedIds: ReadonlySet<number>,
  query = ''
): PlayerClassNode[] {
  const term = query.trim().toLocaleLowerCase()
  const visibleIds = term ? matchingPlayerClassPathIds(roots, term) : undefined
  const visible: PlayerClassNode[] = []

  const visit = (node: PlayerClassNode) => {
    if (visibleIds && !visibleIds.has(node.id)) return
    visible.push(node)
    if (visibleIds || expandedIds.has(node.id)) {
      for (const child of node.children) visit(child)
    }
  }
  for (const root of roots) visit(root)
  return visible
}

function matchingPlayerClassPathIds(
  roots: PlayerClassNode[],
  term: string
): Set<number> {
  const nodes = new Map<number, PlayerClassNode>()
  const visit = (node: PlayerClassNode) => {
    nodes.set(node.id, node)
    for (const child of node.children) visit(child)
  }
  for (const root of roots) visit(root)

  const visible = new Set<number>()
  for (const node of nodes.values()) {
    if (
      !node.name.toLocaleLowerCase().includes(term) &&
      !String(node.id).includes(term) &&
      !node.allowedRaces.some(
        (race) =>
          race.name.toLocaleLowerCase().includes(term) ||
          race.allowedSexes.some((sex) =>
            sex.name.toLocaleLowerCase().includes(term)
          )
      )
    )
      continue

    let current: PlayerClassNode | undefined = node
    while (current && !visible.has(current.id)) {
      visible.add(current.id)
      current =
        current.parentClassId === null
          ? undefined
          : nodes.get(current.parentClassId)
    }
  }
  return visible
}

function playerClassStage(depth: number): PlayerClassStage {
  if (depth === 0) return 'Base'
  if (depth === 1) return 'First'
  if (depth === 2) return 'Second'
  return 'Third'
}

export function skillDirectoryUrl(
  apiBase: string,
  options: { query?: string; page?: number; pageSize?: number } = {}
): string {
  const url = contentUrl(apiBase, 'skills')
  const query = options.query?.trim()
  if (query) url.searchParams.set('query', query)
  url.searchParams.set('page', String(options.page ?? 1))
  url.searchParams.set('pageSize', String(options.pageSize ?? 25))
  return url.toString()
}

export function assetImportsUrl(
  apiBase: string,
  kind: AssetImportKind,
  id?: string
): string {
  const base = apiBase.replace(/\/$/, '')
  const suffix = id ? `/${encodeURIComponent(id)}` : ''
  return `${base}/api/assets/${kind}/imports${suffix}`
}

export function assetCatalogsUrl(apiBase: string): string {
  return `${apiBase.replace(/\/$/, '')}/api/assets/catalogs`
}

export function assetCatalogUrl(
  apiBase: string,
  kind: AssetImportKind,
  options: {
    query?: string
    packageName?: string
    page?: number
    pageSize?: number
  } = {}
): string {
  const base = apiBase.replace(/\/$/, '')
  const url = new URL(
    `${base}/api/assets/${kind}/catalog`,
    'http://studio.internal'
  )
  const query = options.query?.trim()
  if (query) url.searchParams.set('query', query)
  if (options.packageName)
    url.searchParams.set('packageName', options.packageName)
  url.searchParams.set('page', String(options.page ?? 1))
  url.searchParams.set('pageSize', String(options.pageSize ?? 50))
  return base.startsWith('http://') || base.startsWith('https://')
    ? url.toString()
    : `${url.pathname}${url.search}`
}

export function assetCatalogEntryUrl(
  apiBase: string,
  kind: 'levels' | 'scenes',
  name: string
): string {
  const base = apiBase.replace(/\/$/, '')
  return `${base}/api/assets/${kind}/catalog/${encodeURIComponent(name)}`
}

export function positiveInteger(value: unknown, fallback: number): number {
  if (typeof value !== 'string') return fallback
  const parsed = Number.parseInt(value, 10)
  return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback
}

export function paginate<T>(items: T[], page: number, pageSize: number): T[] {
  const offset = Math.max(0, page - 1) * pageSize
  return items.slice(offset, offset + pageSize)
}

export function paginationRange(
  total: number,
  page: number,
  pageSize: number
): { first: number; last: number } {
  if (total <= 0) return { first: 0, last: 0 }
  const first = (Math.max(1, page) - 1) * pageSize + 1
  return {
    first: Math.min(first, total),
    last: Math.min(first + pageSize - 1, total)
  }
}

function contentUrl(apiBase: string, path: string): URL {
  const base = apiBase.replace(/\/$/, '')
  const url = new URL(`${base}/api/content/${path}`, 'http://studio.internal')
  if (!base.startsWith('http://') && !base.startsWith('https://')) {
    Object.defineProperty(url, 'toString', {
      value: () => `${url.pathname}${url.searchParams.size ? `?${url.searchParams}` : ''}`
    })
  }
  return url
}
