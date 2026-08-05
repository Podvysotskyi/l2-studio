import { describe, expect, it } from 'vitest'
import {
  lookupUrl,
  npcDirectoryUrl,
  paginate,
  paginationRange,
  positiveInteger
} from '../lib/studio-content'

describe('Studio content client', () => {
  it('builds a normalized NPC directory URL', () => {
    expect(
      npcDirectoryUrl('https://studio.example.com/root/', {
        query: ' Goblin Scout ',
        page: 3,
        pageSize: 50
      })
    ).toBe(
      'https://studio.example.com/root/api/content/npcs?query=Goblin+Scout&page=3&pageSize=50'
    )
  })

  it('builds lookup URLs from the configured API base', () => {
    expect(lookupUrl('http://localhost:5101/', 'npc-types')).toBe(
      'http://localhost:5101/api/content/npc-types'
    )
  })

  it('accepts only positive integer query values', () => {
    expect(positiveInteger('12', 1)).toBe(12)
    expect(positiveInteger('0', 25)).toBe(25)
    expect(positiveInteger('-4', 25)).toBe(25)
    expect(positiveInteger(['10'], 25)).toBe(25)
  })

  it('paginates local catalogs without mutating their records', () => {
    const records = [1, 2, 3, 4, 5]
    expect(paginate(records, 2, 2)).toEqual([3, 4])
    expect(records).toEqual([1, 2, 3, 4, 5])
  })

  it('reports bounded visible ranges', () => {
    expect(paginationRange(48, 2, 10)).toEqual({ first: 11, last: 20 })
    expect(paginationRange(3, 1, 10)).toEqual({ first: 1, last: 3 })
    expect(paginationRange(3, 99, 10)).toEqual({ first: 3, last: 3 })
    expect(paginationRange(0, 1, 10)).toEqual({ first: 0, last: 0 })
  })
})
