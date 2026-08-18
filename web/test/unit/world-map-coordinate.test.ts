import { describe, expect, it } from 'vitest'
import { worldMapTileName } from '../../app/utils/world-map-coordinate'

describe('world map coordinates', () => {
  it('maps game-world coordinates to the imported map tile names', () => {
    expect(worldMapTileName(-87140, 251482)).toBe('17_25')
    expect(worldMapTileName(135880, -173379)).toBe('24_12')
  })
})
