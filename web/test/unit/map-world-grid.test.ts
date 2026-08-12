import type { MapCatalogEntry } from '~/types/studio'
import { describe, expect, it } from 'vitest'
import {
  buildMapWorldGrid,
  parseMapWorldCoordinate
} from '../../app/utils/map-world-grid'

function map(name: string): MapCatalogEntry {
  return {
    name,
    fileName: `${name}.unr`,
    manifestUrl: `/maps/${name}/manifest.json`,
    terrainCount: 1,
    actorCount: 2,
    waterVolumeCount: 1,
    sha256: name,
    status: 'resolved',
    error: null
  }
}

describe('map world grid', () => {
  it('parses map names as world coordinates', () => {
    expect(parseMapWorldCoordinate('17_25')).toEqual({ x: 17, y: 25 })
    expect(parseMapWorldCoordinate('Lobby')).toBeUndefined()
  })

  it('preserves empty coordinates between imported maps', () => {
    const grid = buildMapWorldGrid([
      map('17_24'),
      map('19_24'),
      map('17_25')
    ])

    expect(grid).toMatchObject({
      minX: 17,
      maxX: 19,
      minY: 24,
      maxY: 25,
      width: 3,
      height: 2
    })
    expect(
      grid.cells.map(({ key, map: entry }) => [key, entry?.name])
    ).toEqual([
      ['17_24', '17_24'],
      ['18_24', undefined],
      ['19_24', '19_24'],
      ['17_25', '17_25'],
      ['18_25', undefined],
      ['19_25', undefined]
    ])
  })

  it('keeps non-coordinate maps outside the world grid', () => {
    const grid = buildMapWorldGrid([map('17_25'), map('Lobby')])

    expect(grid.unpositioned.map(({ name }) => name)).toEqual(['Lobby'])
  })
})
