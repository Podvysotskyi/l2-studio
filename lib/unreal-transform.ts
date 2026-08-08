import type { LevelRotation, LevelVector } from '@l2/ui'
import { Matrix, Quaternion, Vector3, type Scene } from '@babylonjs/core'

const unrealRotationUnit = (Math.PI * 2) / 65536

export interface UnrealNodeTransform {
  position: Vector3
  rotation: Quaternion
  scaling: Vector3
}

export function configureUnrealScene(scene: Scene) {
  // Imported meshes are already converted from Unreal's left-handed, Z-up
  // basis to glTF's right-handed, Y-up basis. Keeping the composed level scene
  // right-handed prevents Babylon from adding a mirror to GLB geometry that is
  // not also applied to manifest placement nodes.
  scene.useRightHandedSystem = true
}

export function unrealVector(value: LevelVector) {
  return new Vector3(value.x, value.z, value.y)
}

export function unrealRotationQuaternion(value: LevelRotation) {
  const pitch = value.pitch * unrealRotationUnit
  const yaw = value.yaw * unrealRotationUnit
  const roll = value.roll * unrealRotationUnit
  const sr = Math.sin(roll)
  const sp = Math.sin(pitch)
  const sy = Math.sin(yaw)
  const cr = Math.cos(roll)
  const cp = Math.cos(pitch)
  const cy = Math.cos(yaw)

  const lx = cp * cy
  const ly = cp * sy
  const lz = sp
  const px = sr * sp * cy - cr * sy
  const py = sr * sp * sy + cr * cy
  const pz = -sr * cp
  const yx = -(cr * sp * cy + sr * sy)
  const yy = cy * sr - cr * sp * sy
  const yz = cr * cp

  return Quaternion.FromRotationMatrix(
    Matrix.FromValues(lx, lz, ly, 0, yx, yz, yy, 0, px, pz, py, 0, 0, 0, 0, 1)
  ).normalize()
}

export function unrealForward(value: LevelRotation) {
  return Vector3.Right().rotateByQuaternionToRef(
    unrealRotationQuaternion(value),
    new Vector3()
  )
}

export function unrealNodeTransform(
  location: LevelVector,
  rotation: LevelRotation,
  drawScale: number,
  drawScale3D: LevelVector,
  prePivot: LevelVector
): UnrealNodeTransform {
  const quaternion = unrealRotationQuaternion(rotation)
  const scaling = unrealVector(drawScale3D).scale(drawScale)
  const pivotOffset = unrealVector(prePivot).multiply(scaling)
  const rotatedPivotOffset = pivotOffset.rotateByQuaternionToRef(
    quaternion,
    new Vector3()
  )

  return {
    position: unrealVector(location).subtract(rotatedPivotOffset),
    rotation: quaternion,
    scaling
  }
}
