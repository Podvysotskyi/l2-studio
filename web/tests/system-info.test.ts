import { describe, expect, it } from 'vitest'
import { systemInfoUrl } from '../lib/system-info'

describe('systemInfoUrl', () => {
  it('normalizes a trailing slash', () => {
    expect(systemInfoUrl('http://localhost:5101/')).toBe(
      'http://localhost:5101/api/system/info'
    )
  })
})
