import type { MapRotation, MapVector } from '~/types/studio'
import { Matrix4, Quaternion, Vector3 } from 'three'

const unrealRotationUnit = (Math.PI * 2) / 65536

export interface UnrealNodeTransform {
  position: Vector3
  rotation: Quaternion
  scaling: Vector3
}

export function unrealVector(value: MapVector) {
  return new Vector3(value.x, value.z, value.y)
}

export function unrealRotationQuaternion(value: MapRotation) {
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

  return new Quaternion().setFromRotationMatrix(
    new Matrix4().set(
      lx, yx, px, 0,
      lz, yz, pz, 0,
      ly, yy, py, 0,
      0, 0, 0, 1
    )
  ).normalize()
}

export function unrealForward(value: MapRotation) {
  return new Vector3(1, 0, 0).applyQuaternion(
    unrealRotationQuaternion(value)
  )
}

export function unrealNodeTransform(
  location: MapVector,
  rotation: MapRotation,
  drawScale: number,
  drawScale3D: MapVector,
  prePivot: MapVector
): UnrealNodeTransform {
  const quaternion = unrealRotationQuaternion(rotation)
  const scaling = unrealVector(drawScale3D).multiplyScalar(drawScale)
  const rotatedPivotOffset = unrealVector(prePivot)
    .multiply(scaling)
    .applyQuaternion(quaternion)

  return {
    position: unrealVector(location).sub(rotatedPivotOffset),
    rotation: quaternion,
    scaling
  }
}
