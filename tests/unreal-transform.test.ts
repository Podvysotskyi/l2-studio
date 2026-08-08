import { NullEngine, Scene, Vector3 } from '@babylonjs/core'
import { describe, expect, it } from 'vitest'
import {
  configureUnrealScene,
  unrealForward,
  unrealNodeTransform,
  unrealRotationQuaternion,
  unrealVector
} from '../lib/unreal-transform'

function expectVector(actual: Vector3, expected: Vector3) {
  expect(actual.x).toBeCloseTo(expected.x, 5)
  expect(actual.y).toBeCloseTo(expected.y, 5)
  expect(actual.z).toBeCloseTo(expected.z, 5)
}

function rotatedForward(pitch: number, yaw: number, roll: number) {
  return new Vector3(1, 0, 0).rotateByQuaternionToRef(
    unrealRotationQuaternion({ pitch, yaw, roll }),
    new Vector3()
  )
}

describe('Unreal level transforms', () => {
  it('configures composed level scenes to use the glTF coordinate system', () => {
    const engine = new NullEngine()
    const scene = new Scene(engine)

    configureUnrealScene(scene)

    expect(scene.useRightHandedSystem).toBe(true)
    scene.dispose()
    engine.dispose()
  })

  it('maps Unreal Z-up vectors to Babylon Y-up vectors', () => {
    expectVector(unrealVector({ x: 1, y: 2, z: 3 }), new Vector3(1, 3, 2))
  })

  it('maps cardinal Unreal yaw and pitch rotations', () => {
    expectVector(rotatedForward(0, 16384, 0), new Vector3(0, 0, 1))
    expectVector(rotatedForward(16384, 0, 0), new Vector3(0, 1, 0))
  })

  it('maps Unreal roll around the converted forward axis', () => {
    const quaternion = unrealRotationQuaternion({
      pitch: 0,
      yaw: 0,
      roll: 16384
    })
    const rotatedUp = new Vector3(0, 1, 0).rotateByQuaternionToRef(
      quaternion,
      new Vector3()
    )
    expectVector(rotatedUp, new Vector3(0, 0, 1))
  })

  it('maps compound rotations across the complete converted basis', () => {
    const quaternion = unrealRotationQuaternion({
      pitch: 7000,
      yaw: -7336,
      roll: 3000
    })
    const convertedBasis = [
      new Vector3(1, 0, 0),
      new Vector3(0, 0, 1),
      new Vector3(0, 1, 0)
    ]
    const expected = [
      new Vector3(0.59728575, 0.62186081, -0.50649665),
      new Vector3(0.75473556, -0.22215153, 0.61727055),
      new Vector3(-0.27133736, 0.75095794, 0.60202841)
    ]

    convertedBasis.forEach((axis, index) => {
      expectVector(
        axis.rotateByQuaternionToRef(quaternion, new Vector3()),
        expected[index]!
      )
    })
  })

  it('uses Unreal positive X as the forward direction', () => {
    expectVector(
      unrealForward({ pitch: 0, yaw: 0, roll: 0 }),
      new Vector3(1, 0, 0)
    )
    expectVector(
      unrealForward({ pitch: 0, yaw: 16384, roll: 0 }),
      new Vector3(0, 0, 1)
    )
  })

  it('keeps terrain-local and actor world coordinates in one basis', () => {
    const terrainLocation = { x: -81920, y: 245760, z: 160.65128 }
    const localOffset = { x: 10750.84375, y: 12201.453125, z: -3020.65128 }
    const actorLocation = {
      x: terrainLocation.x + localOffset.x,
      y: terrainLocation.y + localOffset.y,
      z: terrainLocation.z + localOffset.z
    }

    expectVector(
      unrealVector(terrainLocation).add(unrealVector(localOffset)),
      unrealVector(actorLocation)
    )
  })

  it('applies scaled and rotated PrePivot as a translation offset', () => {
    const transform = unrealNodeTransform(
      { x: 100, y: 200, z: 300 },
      { pitch: 0, yaw: 16384, roll: 0 },
      2,
      { x: 2, y: 3, z: 4 },
      { x: 1, y: 2, z: 3 }
    )

    expectVector(transform.scaling, new Vector3(4, 8, 6))
    expectVector(transform.position, new Vector3(112, 276, 196))
  })
})
