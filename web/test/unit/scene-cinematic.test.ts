import type { SceneManifest, SceneObjectManifestEntry } from '~/types/studio'
import { describe, expect, it } from 'vitest'
import {
  interpolateScenePose,
  sceneManagerLabel,
  scenePlaybackFrames
} from '../../app/utils/scene-cinematic'

function object(
  order: number,
  className: string,
  x: number
): SceneObjectManifestEntry {
  return {
    order,
    name: `${className}${order}`,
    className,
    location: { x, y: 0, z: 0 },
    rotation: { pitch: 0, yaw: order * 100, roll: 0 },
    duration: 1,
    target: null,
    properties: {}
  }
}

const manifest: SceneManifest = {
  schemaVersion: 12,
  name: 'Lobby',
  fileName: 'Lobby.unr',
  sourceHash: 'hash',
  protocol: 111,
  environment: {
    ambientColor: { r: 0, g: 0, b: 0 },
    ambientBrightness: 0,
    distanceFog: null
  },
  terrains: [],
  actors: [],
  lights: [],
  skyZones: [],
  bspMeshes: [],
  skyBackdrops: [],
  cameras: [object(20, 'Camera', 20)],
  interpolationPoints: [object(10, 'InterpolationPoint', 10)],
  sceneManagers: [],
  actions: [],
  ambientSounds: [],
  effects: [],
  waterVolumes: [],
  unrepresentedObjectClasses: {}
}

describe('scene cinematics', () => {
  it('orders camera and interpolation anchors by export order', () => {
    expect(scenePlaybackFrames(manifest).map((frame) => frame.order)).toEqual([
      10, 20
    ])
  })

  it('interpolates and clamps scene poses', () => {
    const from = object(1, 'Camera', 0)
    const to = object(2, 'Camera', 100)
    expect(interpolateScenePose(from, to, 0.25).location.x).toBe(25)
    expect(interpolateScenePose(from, to, 2).location.x).toBe(100)
  })

  it('resolves manager actions to ordered interpolation targets', () => {
    const point = object(10, 'InterpolationPoint', 50)
    point.name = 'InterpolationPoint7'
    const action = object(20, 'ActionMoveCamera', 0)
    action.name = 'myLevel.ActionMoveCamera2'
    action.target = 'InterpolationPoint7'
    action.duration = 2
    const manager = object(30, 'SceneManager', 0)
    manager.name = 'SceneManager0'
    manager.properties = {
      Actions: 'myLevel.ActionMoveCamera2',
      Tag: 'Elf_Fighter'
    }
    const scene = {
      ...manifest,
      interpolationPoints: [point],
      actions: [action],
      sceneManagers: [manager]
    }

    expect(scenePlaybackFrames(scene, manager.name)).toMatchObject([
      { className: 'ActionMoveCamera', duration: 2, location: { x: 50 } }
    ])
    expect(sceneManagerLabel(manager)).toBe('Elf_Fighter')
  })
})
