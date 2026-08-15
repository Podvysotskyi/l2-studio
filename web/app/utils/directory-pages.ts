import type { DirectoryPage } from '../types/responses/content-directory-response'

export async function loadDirectoryOptions<TItem>(
  load: (page: number, pageSize: number) => Promise<DirectoryPage<TItem>>,
  pageSize = 100
): Promise<TItem[]> {
  const first = await load(1, pageSize)
  const pages = Math.ceil(first.total / pageSize)
  if (pages <= 1) return first.items

  const remaining = await Promise.all(
    Array.from({ length: pages - 1 }, (_, index) => load(index + 2, pageSize))
  )
  return [
    ...first.items,
    ...remaining.flatMap(page => page.items)
  ]
}
