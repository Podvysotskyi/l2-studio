import { describe, expect, it } from 'vitest'
import {
  lookupUrl,
  buildPlayerClassHierarchy,
  flattenPlayerClassHierarchy,
  assetCatalogEntryUrl,
  assetCatalogsUrl,
  assetCatalogUrl,
  assetImportsUrl,
  npcDirectoryUrl,
  playerClassDirectoryUrl,
  paginate,
  paginationRange,
  positiveInteger,
  skillDirectoryUrl
} from '../../app/utils/studio-content'

const humanAvailability = [
  {
    id: 0,
    name: 'Human',
    allowedSexes: [
      { id: 0, name: 'Male' },
      { id: 1, name: 'Female' }
    ]
  }
]

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
    expect(lookupUrl('http://localhost:5101/', 'skill-target-types')).toBe(
      'http://localhost:5101/api/content/skill-target-types'
    )
    expect(lookupUrl('http://localhost:5101/', 'player-races')).toBe(
      'http://localhost:5101/api/content/player-races'
    )
    expect(lookupUrl('http://localhost:5101/', 'player-sexes')).toBe(
      'http://localhost:5101/api/content/player-sexes'
    )
  })

  it('builds the player class directory URL', () => {
    expect(playerClassDirectoryUrl('http://localhost:5101/')).toBe(
      'http://localhost:5101/api/content/player-classes'
    )
  })

  it('builds and expands the player class hierarchy', () => {
    const roots = buildPlayerClassHierarchy([
      {
        id: 88,
        name: 'Duelist',
        parentClassId: 2,
        isMage: false,
        allowedRaces: humanAvailability
      },
      {
        id: 2,
        name: 'Gladiator',
        parentClassId: 1,
        isMage: false,
        allowedRaces: humanAvailability
      },
      {
        id: 0,
        name: 'Human Fighter',
        parentClassId: null,
        isMage: false,
        allowedRaces: humanAvailability
      },
      {
        id: 1,
        name: 'Warrior',
        parentClassId: 0,
        isMage: false,
        allowedRaces: humanAvailability
      }
    ])

    expect(roots).toHaveLength(1)
    expect(roots[0]).toMatchObject({
      id: 0,
      depth: 0,
      stage: 'Base',
      parentName: null
    })
    expect(roots[0]?.children[0]).toMatchObject({
      id: 1,
      depth: 1,
      stage: 'First',
      parentName: 'Human Fighter'
    })

    expect(flattenPlayerClassHierarchy(roots, new Set([0]))).toMatchObject([
      { id: 0 },
      { id: 1 }
    ])
    expect(
      flattenPlayerClassHierarchy(roots, new Set([0, 1, 2]))
    ).toMatchObject([{ id: 0 }, { id: 1 }, { id: 2 }, { id: 88 }])
  })

  it('includes ancestor paths while searching player classes', () => {
    const roots = buildPlayerClassHierarchy([
      {
        id: 0,
        name: 'Human Fighter',
        parentClassId: null,
        isMage: false,
        allowedRaces: humanAvailability
      },
      {
        id: 1,
        name: 'Warrior',
        parentClassId: 0,
        isMage: false,
        allowedRaces: humanAvailability
      },
      {
        id: 2,
        name: 'Gladiator',
        parentClassId: 1,
        isMage: false,
        allowedRaces: humanAvailability
      },
      {
        id: 88,
        name: 'Duelist',
        parentClassId: 2,
        isMage: false,
        allowedRaces: humanAvailability
      },
      {
        id: 3,
        name: 'Warlord',
        parentClassId: 1,
        isMage: false,
        allowedRaces: humanAvailability
      }
    ])

    expect(
      flattenPlayerClassHierarchy(roots, new Set(), 'duelist')
    ).toMatchObject([{ id: 0 }, { id: 1 }, { id: 2 }, { id: 88 }])
    expect(flattenPlayerClassHierarchy(roots, new Set(), '3')).toMatchObject([
      { id: 0 },
      { id: 1 },
      { id: 3 }
    ])
    expect(
      flattenPlayerClassHierarchy(roots, new Set(), 'female')
    ).toHaveLength(5)
  })

  it('builds a normalized skill directory URL', () => {
    expect(
      skillDirectoryUrl('https://studio.example.com/root/', {
        query: ' Triple Slash ',
        page: 2,
        pageSize: 10
      })
    ).toBe(
      'https://studio.example.com/root/api/content/skills?query=Triple+Slash&page=2&pageSize=10'
    )
  })

  it('accepts only positive integer query values', () => {
    expect(positiveInteger('12', 1)).toBe(12)
    expect(positiveInteger('0', 25)).toBe(25)
    expect(positiveInteger('-4', 25)).toBe(25)
    expect(positiveInteger(['10'], 25)).toBe(25)
  })

  it('builds texture import URLs', () => {
    expect(assetImportsUrl('http://localhost:5101/', 'systextures')).toBe(
      'http://localhost:5101/api/assets/systextures/imports'
    )
    expect(assetImportsUrl('http://localhost:5101', 'textures', 'job-id')).toBe(
      'http://localhost:5101/api/assets/textures/imports/job-id'
    )
  })

  it('builds music import URLs', () => {
    expect(assetImportsUrl('http://localhost:5101/', 'music')).toBe(
      'http://localhost:5101/api/assets/music/imports'
    )
  })

  it('builds static mesh import URLs', () => {
    expect(assetImportsUrl('http://localhost:5101/', 'staticmeshes')).toBe(
      'http://localhost:5101/api/assets/staticmeshes/imports'
    )
  })

  it('builds level import URLs', () => {
    expect(assetImportsUrl('http://localhost:5101/', 'levels')).toBe(
      'http://localhost:5101/api/assets/levels/imports'
    )
    expect(assetImportsUrl('http://localhost:5101/', 'scenes')).toBe(
      'http://localhost:5101/api/assets/scenes/imports'
    )
  })

  it('builds paginated asset catalog URLs', () => {
    expect(
      assetCatalogUrl('http://localhost:5101/', 'textures', {
        query: ' icon sword ',
        packageName: 'Interface',
        page: 2,
        pageSize: 100
      })
    ).toBe(
      'http://localhost:5101/api/assets/textures/catalog?query=icon+sword&packageName=Interface&page=2&pageSize=100'
    )
    expect(assetCatalogsUrl('http://localhost:5101/')).toBe(
      'http://localhost:5101/api/assets/catalogs'
    )
    expect(
      assetCatalogEntryUrl('http://localhost:5101/', 'levels', '17 25')
    ).toBe('http://localhost:5101/api/assets/levels/catalog/17%2025')
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
