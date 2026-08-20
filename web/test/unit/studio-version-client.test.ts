import { describe, expect, it } from 'vitest'
import { createStudioVersionClient } from '../../app/services/studio-version-client'

describe('Studio version client', () => {
  it('requires and encodes an explicit version context', () => {
    const client = createStudioVersionClient('c1 test')

    expect(client.gameVersion).toBe('c1 test')
    expect(client.path('/content/items/etc/57')).toBe(
      '/api/game-versions/c1%20test/content/items/etc/57'
    )
    expect(() => createStudioVersionClient('  ')).toThrow(
      'A game version is required.'
    )
  })
})
