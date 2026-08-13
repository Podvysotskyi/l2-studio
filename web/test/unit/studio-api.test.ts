import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  getAssetImportDiagnostics,
  getAssetArtifact,
  getAssetArtifacts,
  getAssetImportJobs,
  getStaleAssetSources,
  getAssetImportWorkItems,
  getNpcDirectory,
  getNpcLookupDirectory,
  getNpcLookupImportJob,
  getNpcLookupImportJobs,
  getStudioServiceInfo,
  startAssetFileImport,
  startAssetResourceImport,
  startAssetImport,
  startNpcLookupImport,
  updateNpcLookupDisplayName,
  rebuildStaleAssetSources,
  verifyAssetArtifact
} from '../../app/services/studio-api'

describe('Studio API service', () => {
  const fetchMock = vi.fn()

  beforeEach(() => vi.stubGlobal('$fetch', fetchMock))
  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('loads service information through the Nuxt proxy', async () => {
    fetchMock.mockResolvedValue({})
    await getStudioServiceInfo()
    expect(fetchMock).toHaveBeenCalledWith('/api/system/info')
  })

  it('normalizes directory requests through the service boundary', async () => {
    fetchMock.mockResolvedValue({ items: [], total: 0, page: 2, pageSize: 50 })
    await getNpcDirectory({ query: ' Goblin ', page: 2, pageSize: 50 })
    expect(fetchMock).toHaveBeenCalledWith('/api/game-versions/c1/content/npcs', {
      query: { query: 'Goblin', page: 2, pageSize: 50 }
    })
  })

  it('loads and starts import jobs through same-origin URLs', async () => {
    fetchMock.mockResolvedValue([])
    await getAssetImportJobs('textures', 100)
    expect(fetchMock).toHaveBeenCalledWith('/api/game-versions/c1/assets/textures/imports', {
      query: { limit: 100 }
    })

    await startAssetImport('textures')
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/assets/textures/imports', {
      method: 'POST',
      body: {}
    })
  })

  it('loads per-file progress and filtered diagnostics', async () => {
    fetchMock.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 25 })
    await getAssetImportWorkItems('maps', 'run-id', {
      sourceKey: '17_25.unr',
      status: 'failed',
      query: 'terrain',
      diagnosticSeverity: 'error',
      pageSize: 25
    })
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/maps/imports/run-id/work-items',
      {
        query: {
          sourceKey: '17_25.unr',
          status: 'failed',
          query: 'terrain',
          diagnosticSeverity: 'error',
          page: 1,
          pageSize: 25
        }
      }
    )

    await getAssetImportDiagnostics('maps', 'run-id', {
      severity: 'error',
      code: 'conversion.failed',
      query: 'terrain',
      scope: 'run',
      page: 2
    })
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/maps/imports/run-id/diagnostics',
      {
        query: {
          severity: 'error',
          code: 'conversion.failed',
          query: 'terrain',
          scope: 'run',
          page: 2,
          pageSize: 50
        }
      }
    )
  })

  it('encodes single-file re-import filenames', async () => {
    fetchMock.mockResolvedValue({})
    await startAssetFileImport('textures', 'systextures/Lineage Effects.utx')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/textures/imports/files/systextures%2FLineage%20Effects.utx',
      { method: 'POST', query: { force: false } }
    )
  })

  it('starts resource re-imports through the import API', async () => {
    fetchMock.mockResolvedValue({})
    await startAssetResourceImport('textures', 'Texture', 'Package')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/textures/imports/resources',
      { method: 'POST', body: { resourceName: 'Texture', packageName: 'Package', force: false } }
    )

    await startAssetResourceImport('textures', 'Texture', 'Package', 'Textures/Package.utx')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/textures/imports/resources',
      {
        method: 'POST',
        body: {
          resourceName: 'Texture',
          packageName: 'Package',
          sourceKey: 'Textures/Package.utx',
          force: false
        }
      }
    )
  })

  it('supports forced and stale rebuild controls', async () => {
    fetchMock.mockResolvedValue([])
    await startAssetImport('maps', { force: true })
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/assets/maps/imports', {
      method: 'POST',
      body: { force: true }
    })

    await getStaleAssetSources('maps')
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/assets/maps/imports/stale')
    await rebuildStaleAssetSources('maps')
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/assets/maps/imports/stale', {
      method: 'POST'
    })
  })

  it('loads and verifies generated artifacts through version-scoped APIs', async () => {
    fetchMock.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 50 })
    await getAssetArtifacts({ kind: 'maps', current: true, integrityStatus: 'healthy' })
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/assets/artifacts', {
      query: {
        kind: 'maps',
        current: true,
        integrityStatus: 'healthy',
        page: 1,
        pageSize: 50
      }
    })

    await getAssetArtifact('artifact id')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/artifacts/artifact%20id'
    )
    await verifyAssetArtifact('artifact id')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/artifacts/artifact%20id/verify',
      { method: 'POST' }
    )
  })

  it('reads, edits, and imports NPC lookups through version-scoped APIs', async () => {
    fetchMock.mockResolvedValue([])
    await getNpcLookupDirectory('npc-types')
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/npc-types')

    await updateNpcLookupDisplayName('npc-races', 'DARK_ELF', 'Dark Elf')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/content/npc-races/DARK_ELF',
      { method: 'PATCH', body: { displayName: 'Dark Elf' } }
    )

    await getNpcLookupImportJobs('npc-races')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/content/npc-races/imports',
      { query: { limit: 1 } }
    )
    await getNpcLookupImportJob('npc-races', 'run-id')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/content/npc-races/imports/run-id'
    )
    await startNpcLookupImport('npc-races')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/content/npc-races/imports',
      { method: 'POST' }
    )
    await startNpcLookupImport('npc-sexes')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/content/npc-sexes/imports',
      { method: 'POST' }
    )
    await startNpcLookupImport('npc-types', 'add_missing')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/content/npc-types/imports',
      { method: 'POST', body: { mode: 'add_missing' } }
    )
    await startNpcLookupImport('npc-types', 'restore_defaults')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/content/npc-types/imports',
      { method: 'POST', body: { mode: 'restore_defaults' } }
    )
  })
})
