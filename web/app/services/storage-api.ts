import type {
  StorageKind,
  StorageListing,
  StorageUploadProgress
} from '../types/models/storage'
import { StorageRequestError } from '../types/models/storage'
import { selectedGameVersionKey } from '../utils/game-version'

function storageUrl(
  kind: StorageKind,
  action: string,
  query: Record<string, string | boolean | undefined> = {}
) {
  const parameters = new URLSearchParams({ version: selectedGameVersionKey() })
  Object.entries(query).forEach(([key, value]) => {
    if (value !== undefined) parameters.set(key, String(value))
  })
  return `/storage-api/${kind}/${action}?${parameters}`
}

export function getStorageEntries(kind: StorageKind, path = '') {
  return $fetch<StorageListing>(storageUrl(kind, 'entries', { path }))
}

export function storageDownloadUrl(kind: StorageKind, path: string) {
  return storageUrl(kind, 'file', { path })
}

export function createStorageFolder(path: string) {
  return $fetch<void>(storageUrl('resources', 'directory'), {
    method: 'POST',
    body: { path }
  })
}

export function moveStorageEntry(
  path: string,
  destination: string,
  overwrite = false
) {
  return $fetch<void>(storageUrl('resources', 'entry'), {
    method: 'PATCH',
    body: { path, destination, overwrite }
  })
}

export function deleteStorageEntry(path: string) {
  return $fetch<void>(storageUrl('resources', 'entry', { path }), {
    method: 'DELETE'
  })
}

export function uploadStorageFile(
  path: string,
  file: File,
  onProgress: (progress: StorageUploadProgress) => void,
  overwrite = false
) {
  return new Promise<void>((resolve, reject) => {
    const request = new XMLHttpRequest()
    request.open('PUT', storageUrl('resources', 'file', { path, overwrite }))
    request.setRequestHeader('content-type', 'application/octet-stream')
    request.upload.addEventListener('progress', event => {
      onProgress({
        loaded: event.loaded,
        total: event.lengthComputable ? event.total : file.size
      })
    })
    request.addEventListener('load', () => {
      if (request.status >= 200 && request.status < 300) {
        resolve()
        return
      }
      reject(new StorageRequestError(request.status, responseMessage(request)))
    })
    request.addEventListener('error', () =>
      reject(new StorageRequestError(0, 'The upload connection failed.'))
    )
    request.addEventListener('abort', () =>
      reject(new StorageRequestError(0, 'The upload was cancelled.'))
    )
    request.send(file)
  })
}

function responseMessage(request: XMLHttpRequest) {
  try {
    const response = JSON.parse(request.responseText)
    return response.data?.message ?? response.statusMessage ?? response.message
  } catch {
    return `The upload failed with status ${request.status}.`
  }
}
