import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  getAssetImportDiagnostics,
  getAssetImportJobs,
  getAssetImportWorkItems,
  getNpcDirectory,
  getStudioServiceInfo,
  startAssetFileImport,
  startAssetImport
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
      query: undefined
    })
  })

  it('loads per-file progress and filtered diagnostics', async () => {
    fetchMock.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 25 })
    await getAssetImportWorkItems('maps', 'run-id', {
      sourceKey: '17_25.unr',
      status: 'failed',
      pageSize: 25
    })
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/maps/imports/run-id/work-items',
      {
        query: {
          sourceKey: '17_25.unr',
          status: 'failed',
          page: 1,
          pageSize: 25
        }
      }
    )

    await getAssetImportDiagnostics('maps', 'run-id', {
      severity: 'error',
      code: 'conversion.failed',
      query: 'terrain',
      page: 2
    })
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/maps/imports/run-id/diagnostics',
      {
        query: {
          severity: 'error',
          code: 'conversion.failed',
          query: 'terrain',
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
      { method: 'POST' }
    )
  })
})
