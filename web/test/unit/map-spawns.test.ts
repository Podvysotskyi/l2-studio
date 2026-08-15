import type { MapPlayerStartManifestEntry } from '~/types/studio'
import { describe, expect, it } from 'vitest'
import { paginate } from '../../app/utils/directory'
import { filterMapPlayerStarts } from '../../app/utils/map-spawns'

const playerStarts: MapPlayerStartManifestEntry[] = [
  {
    name: 'PlayerStart0',
    location: { x: 1, y: 2, z: 3 },
    rotation: { pitch: 0, yaw: 0, roll: 0 }
  },
  {
    name: 'PlayerStartVillage',
    location: { x: 4, y: 5, z: 6 },
    rotation: { pitch: 0, yaw: 0, roll: 0 }
  }
]

describe('Map player starts', () => {
  it('searches PlayerStart names without changing their order', () => {
    expect(filterMapPlayerStarts(playerStarts, 'village')).toEqual([
      playerStarts[1]
    ])
    expect(filterMapPlayerStarts(playerStarts, '  ')).toEqual(playerStarts)
  })

  it('paginates PlayerStart locations', () => {
    expect(paginate(playerStarts, 2, 1)).toEqual([playerStarts[1]])
  })
})
