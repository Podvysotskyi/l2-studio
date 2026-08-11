import type { LevelCatalogEntry } from '~/types/studio'
import { describe, expect, it } from 'vitest'
import {
  buildLevelWorldGrid,
  parseLevelWorldCoordinate
} from '../../app/utils/level-world-grid'

function level(name: string): LevelCatalogEntry {
  return {
    name,
    fileName: `${name}.unr`,
    manifestUrl: `/levels/${name}/manifest.json`,
    terrainCount: 1,
    actorCount: 2,
    waterVolumeCount: 1,
    sha256: name,
    status: 'resolved',
    error: null
  }
}

describe('level world grid', () => {
  it('parses map names as world coordinates', () => {
    expect(parseLevelWorldCoordinate('17_25')).toEqual({ x: 17, y: 25 })
    expect(parseLevelWorldCoordinate('Lobby')).toBeUndefined()
  })

  it('preserves empty coordinates between imported levels', () => {
    const grid = buildLevelWorldGrid([
      level('17_24'),
      level('19_24'),
      level('17_25')
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
      grid.cells.map(({ key, level: entry }) => [key, entry?.name])
    ).toEqual([
      ['17_24', '17_24'],
      ['18_24', undefined],
      ['19_24', '19_24'],
      ['17_25', '17_25'],
      ['18_25', undefined],
      ['19_25', undefined]
    ])
  })

  it('keeps non-coordinate levels outside the world grid', () => {
    const grid = buildLevelWorldGrid([level('17_25'), level('Lobby')])

    expect(grid.unpositioned.map(({ name }) => name)).toEqual(['Lobby'])
  })
})
