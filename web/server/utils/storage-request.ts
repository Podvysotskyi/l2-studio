import type { H3Event } from 'h3'
import { createError, getQuery, getRouterParam } from 'h3'
import { storageRoot, type StorageKind, type StorageVersion } from './storage'

let versionCache:
  | { expiresAt: number; versions: StorageVersion[] }
  | undefined

export async function storageRequest(event: H3Event) {
  const kind = getRouterParam(event, 'store')
  if (kind !== 'resources' && kind !== 'assets')
    throw createError({ statusCode: 404, statusMessage: 'Storage was not found.' })
  const versionKey = getQuery(event).version
  if (typeof versionKey !== 'string' || !versionKey)
    throw createError({ statusCode: 400, statusMessage: 'A game version is required.' })

  const version = (await gameVersions(event)).find(item => item.key === versionKey)
  if (!version)
    throw createError({ statusCode: 404, statusMessage: 'Game version was not found.' })
  const config = useRuntimeConfig(event)
  return {
    kind: kind as StorageKind,
    version,
    root: storageRoot(kind, version, {
      resources: config.storageResourcesRoot,
      assets: config.storageAssetsRoot
    })
  }
}

export function requireResourceStorage(kind: StorageKind) {
  if (kind !== 'resources')
    throw createError({
      statusCode: 405,
      statusMessage: 'Generated assets are managed by the import pipeline.'
    })
}

async function gameVersions(event: H3Event) {
  const now = Date.now()
  if (versionCache && versionCache.expiresAt > now) return versionCache.versions
  const config = useRuntimeConfig(event)
  try {
    const versions = await $fetch<StorageVersion[]>(
      `${config.studioApiBase}/api/game-versions`
    )
    versionCache = { versions, expiresAt: now + 60_000 }
    return versions
  } catch {
    throw createError({
      statusCode: 503,
      statusMessage: 'Game-version metadata is unavailable.'
    })
  }
}
