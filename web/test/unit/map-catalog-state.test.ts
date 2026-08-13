import type { AssetCatalogPage, MapCatalogEntry } from '~/types/studio'
import { describe, expect, it } from 'vitest'
import { hasImportedMaps } from '../../app/utils/map-catalog-state'

const map: MapCatalogEntry = {
  name: '17_25',
  fileName: '17_25.unr',
  manifestUrl: '/maps/17_25/manifest.json',
  terrainCount: 1,
  actorCount: 2,
  waterVolumeCount: 1,
  sha256: 'hash',
  status: 'resolved',
  error: null,
  sourceKey: 'Maps/17_25.unr'
}

function catalog(items: MapCatalogEntry[]): AssetCatalogPage<MapCatalogEntry> {
  return {
    summary: {
      kind: 'maps',
      sourceFolder: 'Maps',
      sourceHash: 'hash',
      schemaVersion: 1,
      protocol: null,
      total: items.length,
      resolved: items.length,
      skipped: 0,
      groupCount: 0,
      publishedAt: '2026-08-13T00:00:00Z'
    },
    groups: [],
    items,
    total: items.length,
    page: 1,
    pageSize: 500
  }
}

describe('map catalog state', () => {
  it('treats missing and empty catalogs as having no imported maps', () => {
    expect(hasImportedMaps(undefined)).toBe(false)
    expect(hasImportedMaps(catalog([]))).toBe(false)
  })

  it('recognizes imported maps, including maps outside the world grid', () => {
    expect(hasImportedMaps(catalog([{ ...map, name: 'Lobby' }]))).toBe(true)
  })
})
