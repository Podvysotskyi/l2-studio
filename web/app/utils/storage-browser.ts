import type { StorageEntry } from '../types/models/storage'

export type StorageSort =
  | 'name-asc'
  | 'name-desc'
  | 'modified-desc'
  | 'modified-asc'
  | 'size-desc'
  | 'size-asc'

export interface StorageUploadItem {
  id: string
  path: string
  file: File
  loaded: number
  total: number
  status: 'queued' | 'uploading' | 'complete' | 'failed'
  error?: string
}

export const storageSortOptions: Array<{ label: string, value: StorageSort }> = [
  { label: 'Name A–Z', value: 'name-asc' },
  { label: 'Name Z–A', value: 'name-desc' },
  { label: 'Newest first', value: 'modified-desc' },
  { label: 'Oldest first', value: 'modified-asc' },
  { label: 'Largest first', value: 'size-desc' },
  { label: 'Smallest first', value: 'size-asc' }
]

export function visibleStorageEntries(
  entries: StorageEntry[],
  query: string,
  sort: StorageSort
) {
  const normalizedQuery = query.trim().toLocaleLowerCase()
  return [...entries]
    .filter(entry => entry.name.toLocaleLowerCase().includes(normalizedQuery))
    .sort((left, right) => {
      if (left.type !== right.type) return left.type === 'directory' ? -1 : 1
      const compared = compareEntries(left, right, sort)
      return compared || compareNames(left, right)
    })
}

export function droppedStorageFiles(transfer: DataTransfer) {
  let containsDirectory = false
  const files: File[] = []

  for (const item of Array.from(transfer.items)) {
    if (item.kind !== 'file') continue
    const entry = (item as DataTransferItem & {
      webkitGetAsEntry?: () => { isDirectory: boolean }
    }).webkitGetAsEntry?.()
    if (entry?.isDirectory) {
      containsDirectory = true
      continue
    }
    const file = item.getAsFile()
    if (file) files.push(file)
  }

  if (!transfer.items.length) files.push(...Array.from(transfer.files))
  return { files, containsDirectory }
}

function compareEntries(left: StorageEntry, right: StorageEntry, sort: StorageSort) {
  if (sort === 'name-asc') return compareNames(left, right)
  if (sort === 'name-desc') return compareNames(right, left)
  if (sort === 'modified-desc')
    return Date.parse(right.modifiedAt) - Date.parse(left.modifiedAt)
  if (sort === 'modified-asc')
    return Date.parse(left.modifiedAt) - Date.parse(right.modifiedAt)
  if (left.type === 'directory') return compareNames(left, right)
  if (sort === 'size-desc') return (right.size ?? 0) - (left.size ?? 0)
  return (left.size ?? 0) - (right.size ?? 0)
}

function compareNames(left: StorageEntry, right: StorageEntry) {
  return left.name.localeCompare(right.name, undefined, { sensitivity: 'base' })
}
