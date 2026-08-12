export type StorageKind = 'resources' | 'assets'

export interface StorageEntry {
  name: string
  path: string
  type: 'directory' | 'file'
  size: number | null
  modifiedAt: string
}

export interface StorageListing {
  path: string
  entries: StorageEntry[]
}

export interface StorageUploadProgress {
  loaded: number
  total: number
}

export class StorageRequestError extends Error {
  constructor(
    public readonly status: number,
    message: string
  ) {
    super(message)
  }
}
