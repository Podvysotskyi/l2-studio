import type { SceneObjectManifestEntry } from '~/types/studio'
import { describe, expect, it } from 'vitest'
import { filterSceneObjects, sceneObjectStatus } from '../../app/utils/scene-inspector'

function object(
  name: string,
  className: string,
  overrides: Partial<SceneObjectManifestEntry> = {}
): SceneObjectManifestEntry {
  return {
    order: 0,
    name,
    className,
    location: { x: 0, y: 0, z: 0 },
    rotation: { pitch: 0, yaw: 0, roll: 0 },
    duration: 0,
    target: null,
    properties: {},
    ...overrides
  }
}

describe('scene inspector helpers', () => {
  it('filters scene objects across identity, ownership, targets, and tags', () => {
    const effects = [
      object('Emitter0', 'SpriteEmitter', { owner: 'FireRoot' }),
      object('ActionWarp2', 'ActionWarp', {
        target: 'InterpolationPoint0',
        properties: { Tag: 'Char_Select_Warp' }
      })
    ]

    expect(filterSceneObjects(effects, 'fire')).toEqual([effects[0]])
    expect(filterSceneObjects(effects, 'char_select')).toEqual([effects[1]])
    expect(filterSceneObjects(effects, 'interpolationpoint')).toEqual([
      effects[1]
    ])
  })

  it('distinguishes resolved resources, diagnostics, and metadata', () => {
    expect(sceneObjectStatus(object('Sound0', 'AmbientSoundObject'))).toBe(
      'metadata'
    )
    expect(
      sceneObjectStatus(
        object('Sound1', 'AmbientSoundObject', { resourceUrl: '/sounds/a.wav' })
      )
    ).toBe('resolved')
    expect(
      sceneObjectStatus(
        object('Sound2', 'AmbientSoundObject', { diagnostic: 'Missing sound' })
      )
    ).toBe('diagnostic')
  })
})
