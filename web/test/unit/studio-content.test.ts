import { describe, expect, it } from 'vitest'
import {
  directoryRouteQuery,
  directoryRouteState,
  paginate,
  paginationRange,
  positiveInteger
} from '../../app/utils/directory'
import {
  npcDirectoryRouteQuery,
  npcDirectoryRouteState,
  npcRaceNoneValue
} from '../../app/utils/npc-directory'
import {
  buildPlayerClassHierarchy,
  flattenPlayerClassHierarchy
} from '../../app/utils/player-class'

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

describe('Studio content utilities', () => {
  it('normalizes directory route state and omits default query values', () => {
    expect(
      directoryRouteState({ query: 'Goblin', page: '2', pageSize: '50' })
    ).toEqual({ query: 'Goblin', page: 2, pageSize: 50 })
    expect(directoryRouteState({ page: '-1', pageSize: ['50'] })).toEqual({
      query: '',
      page: 1,
      pageSize: 25
    })
    expect(directoryRouteQuery('  Goblin  ', 1, 25)).toEqual({
      query: 'Goblin'
    })
  })

  it('builds and searches the player class hierarchy with ancestor paths', () => {
    const roots = buildPlayerClassHierarchy([
      playerClass(88, 'Duelist', 2),
      playerClass(2, 'Gladiator', 1),
      playerClass(0, 'Human Fighter', null),
      playerClass(1, 'Warrior', 0),
      playerClass(3, 'Warlord', 1)
    ])

    expect(roots[0]).toMatchObject({ id: 0, depth: 0, stage: 'Base' })
    expect(flattenPlayerClassHierarchy(roots, new Set([0]))).toMatchObject([
      { id: 0 },
      { id: 1 }
    ])
    expect(flattenPlayerClassHierarchy(roots, new Set(), 'duelist')).toMatchObject(
      [{ id: 0 }, { id: 1 }, { id: 2 }, { id: 88 }]
    )
    expect(flattenPlayerClassHierarchy(roots, new Set(), 'female')).toHaveLength(
      5
    )
  })

  it('accepts only positive integer query values', () => {
    expect(positiveInteger('12', 1)).toBe(12)
    expect(positiveInteger('0', 25)).toBe(25)
    expect(positiveInteger('-4', 25)).toBe(25)
    expect(positiveInteger(['10'], 25)).toBe(25)
  })

  it('serializes and restores NPC directory filters in the route', () => {
    const state = npcDirectoryRouteState({
      query: 'Goblin',
      page: '2',
      npcTypeName: 'Monster',
      withoutRace: 'true',
      npcSexName: 'MALE',
      hasVisuals: 'without'
    })

    expect(state).toMatchObject({
      query: 'Goblin',
      page: 2,
      npcTypeName: 'Monster',
      npcRaceName: npcRaceNoneValue,
      npcSexName: 'MALE',
      visualFilter: 'without'
    })
    expect(npcDirectoryRouteQuery(state)).toEqual({
      query: 'Goblin',
      page: '2',
      npcTypeName: 'Monster',
      withoutRace: 'true',
      npcSexName: 'MALE',
      hasVisuals: 'without'
    })
  })

  it('omits inactive NPC filters from the route', () => {
    expect(npcDirectoryRouteQuery({ query: '', page: 1, pageSize: 25 })).toEqual({})
  })

  it('paginates local catalogs without mutating their records', () => {
    const records = [1, 2, 3, 4, 5]
    expect(paginate(records, 2, 2)).toEqual([3, 4])
    expect(records).toEqual([1, 2, 3, 4, 5])
  })

  it('reports bounded visible ranges', () => {
    expect(paginationRange(48, 2, 10)).toEqual({ first: 11, last: 20 })
    expect(paginationRange(3, 99, 10)).toEqual({ first: 3, last: 3 })
    expect(paginationRange(0, 1, 10)).toEqual({ first: 0, last: 0 })
  })
})

function playerClass(id: number, name: string, parentClassId: number | null) {
  return {
    id,
    name,
    parentClassId,
    isMage: false,
    allowedRaces: humanAvailability
  }
}
