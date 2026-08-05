export interface NpcRecord {
  id: number
  level: number
  name: string
  npcTypeId: number
  npcType: string
  npcRaceId: number
  npcRace: string
  npcSexId: number
  npcSex: string
}

export interface NpcPage {
  items: NpcRecord[]
  total: number
  page: number
  pageSize: number
}

export interface LookupRecord {
  id: number
  name: string
}

export type LookupKind = 'npc-races' | 'npc-sexes' | 'npc-types'

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
  return new URL(`${base}/api/content/${path}`)
}
