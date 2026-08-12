import { describe, expect, it } from 'vitest'
import type { GameVersionSummary } from '../../app/types/models/game-version'
import {
  defaultGameVersionKey,
  resolveSelectedGameVersionKey,
  selectedGameVersionKey
} from '../../app/utils/game-version'

const versions: GameVersionSummary[] = [
  {
    key: 'c1',
    displayName: 'Chronicle 1',
    sourceFolder: 'C1',
    sortOrder: 10,
    isDefault: true
  },
  {
    key: 'interlude',
    displayName: 'Interlude',
    sourceFolder: 'Interlude',
    sortOrder: 30,
    isDefault: false
  }
]

describe('Game version selection', () => {
  it('uses C1 before client storage is available', () => {
    expect(defaultGameVersionKey).toBe('c1')
    expect(selectedGameVersionKey()).toBe('c1')
  })

  it('preserves an available non-default selection', () => {
    expect(resolveSelectedGameVersionKey(versions, 'interlude')).toBe('interlude')
  })

  it('replaces an unavailable selection with the API default', () => {
    expect(resolveSelectedGameVersionKey(versions, 'unknown')).toBe('c1')
  })
})
