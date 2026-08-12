import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mkdtemp, mkdir, readFile, rm, symlink, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { Readable } from 'node:stream'
import {
  createStorageDirectory,
  deleteStorageEntry,
  listStorage,
  moveStorageEntry,
  normalizeStoragePath,
  storageRoot,
  writeStorageFile
} from '../../server/utils/storage'
import {
  createStorageFolder,
  deleteStorageEntry as deleteStorageEntryRequest,
  getStorageEntries,
  moveStorageEntry as moveStorageEntryRequest,
  storageDownloadUrl
} from '../../app/services/storage-api'
import { storageUploadPath } from '../../app/utils/storage-upload'
import { visibleStorageEntries } from '../../app/utils/storage-browser'

describe('storage filesystem', () => {
  let root: string

  beforeEach(async () => {
    root = await mkdtemp(join(tmpdir(), 'l2-studio-storage-'))
  })

  afterEach(async () => {
    await rm(root, { recursive: true, force: true })
  })

  it('maps version metadata to isolated resource and asset roots', () => {
    const version = { key: 'interlude', sourceFolder: 'Interlude' }
    expect(storageRoot('resources', version, { resources: '/resources', assets: '/assets' }))
      .toBe('/resources/Interlude')
    expect(storageRoot('assets', version, { resources: '/resources', assets: '/assets' }))
      .toBe('/assets/versions/interlude')
  })

  it('rejects traversal, absolute paths, and platform separators', () => {
    expect(() => normalizeStoragePath('../secret')).toThrow()
    expect(() => normalizeStoragePath('/secret')).toThrow()
    expect(() => normalizeStoragePath('maps\\secret')).toThrow()
    expect(() => normalizeStoragePath('maps//secret')).toThrow()
  })

  it('streams, lists, overwrites, moves, and deletes resource files', async () => {
    await createStorageDirectory(root, 'maps')
    await writeStorageFile(
      root,
      'maps/17_25.unr',
      Readable.from(Buffer.from('first')),
      false
    )

    const listing = await listStorage(root, 'maps', 'resources')
    expect(listing.entries).toMatchObject([
      { name: '17_25.unr', path: 'maps/17_25.unr', type: 'file', size: 5 }
    ])
    await expect(
      writeStorageFile(root, 'maps/17_25.unr', Readable.from('second'), false)
    ).rejects.toMatchObject({ statusCode: 409 })

    await writeStorageFile(root, 'maps/17_25.unr', Readable.from('second'), true)
    expect(await readFile(join(root, 'maps/17_25.unr'), 'utf8')).toBe('second')
    await createStorageDirectory(root, 'archive')
    await moveStorageEntry(root, 'maps/17_25.unr', 'archive/17_25.unr', false)
    expect(await readFile(join(root, 'archive/17_25.unr'), 'utf8')).toBe('second')
    await deleteStorageEntry(root, 'archive')
    expect((await listStorage(root, '', 'resources')).entries.map(item => item.name))
      .toEqual(['maps'])
  })

  it('stores arbitrary file types at the root and in custom folders', async () => {
    await writeStorageFile(root, 'README', Readable.from('notes'), false)
    await writeStorageFile(
      root,
      'custom/data/example.bin',
      Readable.from(Buffer.from([0, 1, 2])),
      false
    )

    expect(await readFile(join(root, 'README'), 'utf8')).toBe('notes')
    expect(await readFile(join(root, 'custom/data/example.bin')))
      .toEqual(Buffer.from([0, 1, 2]))
  })

  it('hides internal asset markers and rejects symbolic links', async () => {
    await writeFile(join(root, '.l2-asset-version'), 'hash')
    await writeFile(join(root, 'manifest.json'), '{}')
    await mkdir(join(root, 'outside'))
    await symlink(join(root, 'outside'), join(root, 'linked'))

    const listing = await listStorage(root, '', 'assets')
    expect(listing.entries.map(item => item.name)).toEqual(['outside', 'manifest.json'])
    await expect(listStorage(root, 'linked', 'assets')).rejects.toMatchObject({
      statusCode: 400
    })
  })
})

describe('storage upload paths', () => {
  it('targets the current folder for individual files', () => {
    expect(storageUploadPath('', 'notes.txt')).toBe('notes.txt')
    expect(storageUploadPath('custom/files', 'notes.txt'))
      .toBe('custom/files/notes.txt')
  })

  it('uploads folder contents without the selected top-level folder', () => {
    expect(storageUploadPath('imports', 'readme.txt', 'client/readme.txt'))
      .toBe('imports/readme.txt')
    expect(storageUploadPath('imports', 'data.bin', 'client/system/data.bin'))
      .toBe('imports/system/data.bin')
  })
})

describe('storage browser entries', () => {
  const entries = [
    { name: 'zeta.txt', path: 'zeta.txt', type: 'file' as const, size: 10, modifiedAt: '2026-01-01T00:00:00.000Z' },
    { name: 'Assets', path: 'Assets', type: 'directory' as const, size: null, modifiedAt: '2026-01-03T00:00:00.000Z' },
    { name: 'alpha.txt', path: 'alpha.txt', type: 'file' as const, size: 100, modifiedAt: '2026-01-02T00:00:00.000Z' },
    { name: 'Maps', path: 'Maps', type: 'directory' as const, size: null, modifiedAt: '2026-01-04T00:00:00.000Z' }
  ]

  it('filters names case-insensitively and keeps directories first', () => {
    expect(visibleStorageEntries(entries, 'a', 'name-asc').map(entry => entry.name))
      .toEqual(['Assets', 'Maps', 'alpha.txt', 'zeta.txt'])
    expect(visibleStorageEntries(entries, 'ALPHA', 'name-asc').map(entry => entry.name))
      .toEqual(['alpha.txt'])
  })

  it('sorts files while preserving directory-first grouping', () => {
    expect(visibleStorageEntries(entries, '', 'size-desc').map(entry => entry.name))
      .toEqual(['Assets', 'Maps', 'alpha.txt', 'zeta.txt'])
    expect(visibleStorageEntries(entries, '', 'modified-desc').map(entry => entry.name))
      .toEqual(['Maps', 'Assets', 'alpha.txt', 'zeta.txt'])
  })
})

describe('storage browser service', () => {
  const fetchMock = vi.fn()

  beforeEach(() => vi.stubGlobal('$fetch', fetchMock))
  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('uses version-scoped same-origin storage endpoints', async () => {
    fetchMock.mockResolvedValue({ path: 'maps', entries: [] })
    await getStorageEntries('resources', 'maps')
    expect(fetchMock).toHaveBeenCalledWith(
      '/storage-api/resources/entries?version=c1&path=maps'
    )
    expect(storageDownloadUrl('assets', 'textures/example.webp')).toBe(
      '/storage-api/assets/file?version=c1&path=textures%2Fexample.webp'
    )
  })

  it('sends resource mutations through the Nuxt service boundary', async () => {
    fetchMock.mockResolvedValue(undefined)
    await createStorageFolder('maps')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/storage-api/resources/directory?version=c1',
      { method: 'POST', body: { path: 'maps' } }
    )
    await moveStorageEntryRequest('maps/a.unr', 'maps/b.unr', true)
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/storage-api/resources/entry?version=c1',
      {
        method: 'PATCH',
        body: {
          path: 'maps/a.unr',
          destination: 'maps/b.unr',
          overwrite: true
        }
      }
    )
    await deleteStorageEntryRequest('maps/b.unr')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/storage-api/resources/entry?version=c1&path=maps%2Fb.unr',
      { method: 'DELETE' }
    )
  })
})
