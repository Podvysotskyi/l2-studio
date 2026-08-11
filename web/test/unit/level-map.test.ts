import type { LevelActorManifestEntry } from '~/types/studio'
import { describe, expect, it } from 'vitest'
import { filterLevelActors } from '../../app/utils/level-map'
import { paginate } from '../../app/utils/directory'

const actors: LevelActorManifestEntry[] = [
  {
    name: 'StaticMeshActor12',
    className: 'StaticMeshActor',
    location: { x: 1, y: 2, z: 3 },
    rotation: { pitch: 0, yaw: 0, roll: 0 },
    prePivot: { x: 0, y: 0, z: 0 },
    drawScale: 1,
    drawScale3D: { x: 1, y: 1, z: 1 },
    meshPackage: 'village_architecture',
    meshObject: 'StoneTower',
    meshUrl: '/staticmeshes/village_architecture/StoneTower.glb',
    vertexLighting: null
  },
  {
    name: 'DecorationActor4',
    className: 'Decoration',
    location: { x: 4, y: 5, z: 6 },
    rotation: { pitch: 0, yaw: 0, roll: 0 },
    prePivot: { x: 0, y: 0, z: 0 },
    drawScale: 1,
    drawScale3D: { x: 1, y: 1, z: 1 },
    meshPackage: null,
    meshObject: null,
    meshUrl: null,
    vertexLighting: null
  }
]

describe('Level map instances', () => {
  it('searches actor names, classes, packages, and mesh objects', () => {
    expect(filterLevelActors(actors, 'actor12')).toEqual([actors[0]])
    expect(filterLevelActors(actors, 'decoration')).toEqual([actors[1]])
    expect(filterLevelActors(actors, 'VILLAGE')).toEqual([actors[0]])
    expect(filterLevelActors(actors, 'tower')).toEqual([actors[0]])
  })

  it('returns all actors for an empty query without changing their order', () => {
    expect(filterLevelActors(actors, '  ')).toEqual(actors)
  })

  it('paginates placed instances without grouping repeated assets', () => {
    const repeated = [actors[0]!, { ...actors[0]!, name: 'StaticMeshActor13' }]

    expect(paginate(repeated, 1, 1)).toEqual([repeated[0]])
    expect(paginate(repeated, 2, 1)).toEqual([repeated[1]])
  })
})
