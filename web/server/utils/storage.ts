import { randomUUID } from 'node:crypto'
import {
  createReadStream,
  createWriteStream,
  promises as fs
} from 'node:fs'
import {
  basename,
  dirname,
  isAbsolute,
  join,
  relative,
  resolve,
  sep
} from 'node:path'
import type { Readable } from 'node:stream'
import { pipeline } from 'node:stream/promises'
import { createError } from 'h3'

export type StorageKind = 'resources' | 'assets'

export interface StorageVersion {
  key: string
  sourceFolder: string
}

export interface StorageRoots {
  resources: string
  assets: string
}

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

export function storageRoot(
  kind: StorageKind,
  version: StorageVersion,
  roots: StorageRoots
) {
  requireSegment(version.key, 'game version')
  requireSegment(version.sourceFolder, 'source folder')
  return kind === 'resources'
    ? resolve(roots.resources, version.sourceFolder)
    : resolve(roots.assets, 'versions', version.key)
}

export function normalizeStoragePath(value: unknown, allowRoot = true) {
  if (value === undefined || value === null || value === '') {
    if (allowRoot) return ''
    throw storageError(400, 'A storage path is required.')
  }
  if (typeof value !== 'string' || isAbsolute(value) || value.includes('\\'))
    throw storageError(400, 'Storage paths must be relative POSIX paths.')

  const segments = value.split('/')
  if (
    segments.some(
      segment =>
        !segment ||
        segment === '.' ||
        segment === '..' ||
        /[\0-\x1f\x7f]/.test(segment)
    )
  )
    throw storageError(400, 'The storage path contains an invalid segment.')
  return segments.join('/')
}

export async function listStorage(
  root: string,
  pathValue: unknown,
  kind: StorageKind
): Promise<StorageListing> {
  const storagePath = normalizeStoragePath(pathValue)
  const target = containedPath(root, storagePath)
  await rejectSymlinks(root, storagePath)

  let directory
  try {
    directory = await fs.opendir(target)
  } catch (error) {
    if (isNodeError(error, 'ENOENT') && storagePath === '')
      return { path: '', entries: [] }
    throw mapFileSystemError(error, 'The directory could not be opened.')
  }

  const entries: StorageEntry[] = []
  for await (const item of directory) {
    if (
      item.isSymbolicLink() ||
      item.name.startsWith('.l2-upload-') ||
      (kind === 'assets' && item.name === '.l2-asset-version')
    )
      continue
    if (!item.isDirectory() && !item.isFile()) continue
    const itemPath = join(target, item.name)
    const stats = await fs.stat(itemPath)
    entries.push({
      name: item.name,
      path: storagePath ? `${storagePath}/${item.name}` : item.name,
      type: item.isDirectory() ? 'directory' : 'file',
      size: item.isFile() ? stats.size : null,
      modifiedAt: stats.mtime.toISOString()
    })
  }
  entries.sort((left, right) => {
    if (left.type !== right.type) return left.type === 'directory' ? -1 : 1
    return left.name.localeCompare(right.name, undefined, { sensitivity: 'base' })
  })
  return { path: storagePath, entries }
}

export async function readableStorageFile(root: string, pathValue: unknown) {
  const storagePath = normalizeStoragePath(pathValue, false)
  const target = containedPath(root, storagePath)
  await rejectSymlinks(root, storagePath)
  let stats
  try {
    stats = await fs.stat(target)
  } catch (error) {
    throw mapFileSystemError(error, 'The file could not be opened.')
  }
  if (!stats.isFile()) throw storageError(400, 'The selected entry is not a file.')
  return {
    name: basename(target),
    size: stats.size,
    stream: createReadStream(target)
  }
}

export async function writeStorageFile(
  root: string,
  pathValue: unknown,
  source: Readable,
  overwrite: boolean
) {
  const storagePath = normalizeStoragePath(pathValue, false)
  const target = containedPath(root, storagePath)
  const parent = dirname(target)
  await ensureStorageDirectory(root, relative(root, parent))
  await rejectSymlinks(root, storagePath, true)

  const temporary = join(parent, `.l2-upload-${randomUUID()}.tmp`)
  try {
    await pipeline(source, createWriteStream(temporary, { flags: 'wx', mode: 0o644 }))
    const existing = await optionalStat(target)
    if (existing && !overwrite)
      throw storageError(409, `The destination '${storagePath}' already exists.`)
    if (existing?.isDirectory())
      throw storageError(409, 'A directory cannot be replaced by an uploaded file.')
    await fs.rename(temporary, target)
  } catch (error) {
    await fs.rm(temporary, { force: true }).catch(() => undefined)
    throw mapFileSystemError(error, 'The upload could not be stored.')
  }
}

export async function createStorageDirectory(root: string, pathValue: unknown) {
  const storagePath = normalizeStoragePath(pathValue, false)
  const target = containedPath(root, storagePath)
  await ensureStorageDirectory(root, relative(root, dirname(target)))
  await rejectSymlinks(root, storagePath, true)
  try {
    await fs.mkdir(target)
  } catch (error) {
    if (isNodeError(error, 'EEXIST'))
      throw storageError(409, `The destination '${storagePath}' already exists.`)
    throw mapFileSystemError(error, 'The directory could not be created.')
  }
}

export async function moveStorageEntry(
  root: string,
  sourceValue: unknown,
  destinationValue: unknown,
  overwrite: boolean
) {
  const sourcePath = normalizeStoragePath(sourceValue, false)
  const destinationPath = normalizeStoragePath(destinationValue, false)
  if (sourcePath === destinationPath) return
  if (destinationPath.startsWith(`${sourcePath}/`))
    throw storageError(400, 'An entry cannot be moved inside itself.')

  const source = containedPath(root, sourcePath)
  const destination = containedPath(root, destinationPath)
  await rejectSymlinks(root, sourcePath)
  await rejectSymlinks(root, destinationPath, true)
  const sourceStats = await optionalStat(source)
  if (!sourceStats) throw storageError(404, `The source '${sourcePath}' was not found.`)
  const destinationParent = await optionalStat(dirname(destination))
  if (!destinationParent?.isDirectory())
    throw storageError(404, 'The destination directory was not found.')

  const existing = await optionalStat(destination)
  if (existing && !overwrite)
    throw storageError(409, `The destination '${destinationPath}' already exists.`)
  try {
    if (existing) await fs.rm(destination, { recursive: true, force: true })
    await fs.rename(source, destination)
  } catch (error) {
    throw mapFileSystemError(error, 'The entry could not be moved.')
  }
}

export async function deleteStorageEntry(root: string, pathValue: unknown) {
  const storagePath = normalizeStoragePath(pathValue, false)
  const target = containedPath(root, storagePath)
  await rejectSymlinks(root, storagePath)
  if (!(await optionalStat(target)))
    throw storageError(404, `The entry '${storagePath}' was not found.`)
  try {
    await fs.rm(target, { recursive: true, force: false })
  } catch (error) {
    throw mapFileSystemError(error, 'The entry could not be deleted.')
  }
}

function containedPath(root: string, storagePath: string) {
  const normalizedRoot = resolve(root)
  const target = resolve(normalizedRoot, storagePath)
  if (target !== normalizedRoot && !target.startsWith(`${normalizedRoot}${sep}`))
    throw storageError(400, 'The storage path escapes its version root.')
  return target
}

async function ensureStorageDirectory(root: string, relativePath: string) {
  const storagePath = relativePath === '' ? '' : normalizeStoragePath(relativePath)
  await rejectSymlinks(root, storagePath, true)
  try {
    await fs.mkdir(containedPath(root, storagePath), { recursive: true })
  } catch (error) {
    throw mapFileSystemError(error, 'The destination directory could not be created.')
  }
}

async function rejectSymlinks(
  root: string,
  storagePath: string,
  allowMissing = false
) {
  const segments = storagePath ? storagePath.split('/') : []
  let current = resolve(root)
  for (const segment of segments) {
    current = join(current, segment)
    try {
      const stats = await fs.lstat(current)
      if (stats.isSymbolicLink())
        throw storageError(400, 'Symbolic links are not available through storage management.')
    } catch (error) {
      if (allowMissing && isNodeError(error, 'ENOENT')) return
      throw mapFileSystemError(error, 'The storage path could not be resolved.')
    }
  }
}

async function optionalStat(path: string) {
  try {
    return await fs.lstat(path)
  } catch (error) {
    if (isNodeError(error, 'ENOENT')) return undefined
    throw error
  }
}

function requireSegment(value: string, description: string) {
  if (!value || value === '.' || value === '..' || value.includes('/') || value.includes('\\'))
    throw storageError(502, `Studio returned an invalid ${description}.`)
}

function isNodeError(error: unknown, code: string): error is NodeJS.ErrnoException {
  return error instanceof Error && 'code' in error && error.code === code
}

function mapFileSystemError(error: unknown, fallback: string): Error {
  if (error && typeof error === 'object' && 'statusCode' in error)
    return error as unknown as Error
  if (isNodeError(error, 'ENOENT')) return storageError(404, 'The storage entry was not found.')
  if (isNodeError(error, 'EACCES') || isNodeError(error, 'EPERM'))
    return storageError(403, 'The storage entry is not writable by Studio.')
  if (isNodeError(error, 'ENOSPC')) return storageError(507, 'The storage volume is full.')
  return storageError(500, fallback)
}

function storageError(statusCode: number, message: string) {
  return createError({ statusCode, statusMessage: message, data: { message } })
}
