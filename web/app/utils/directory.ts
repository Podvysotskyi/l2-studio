export function directoryRouteState(query: Record<string, unknown>) {
  return {
    query: typeof query.query === 'string' ? query.query : '',
    page: positiveInteger(query.page, 1),
    pageSize: positiveInteger(query.pageSize, 25)
  }
}

export function directoryRouteQuery(
  query: string,
  page: number,
  pageSize: number
) {
  return {
    ...(query.trim() ? { query: query.trim() } : {}),
    ...(page > 1 ? { page: String(page) } : {}),
    ...(pageSize !== 25 ? { pageSize: String(pageSize) } : {})
  }
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
