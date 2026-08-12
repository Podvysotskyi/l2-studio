import type { MapEnvironmentManifestEntry } from '~/types/studio'
import {
  Camera,
  Color3,
  Color4,
  Scene,
  type TargetCamera
} from '@babylonjs/core'

export const loginReferenceAspect = 16 / 9
export const loginReferenceHorizontalFov = Math.PI / 3

export function coverCameraFraming(aspect: number) {
  const verticalFov =
    2 *
    Math.atan(Math.tan(loginReferenceHorizontalFov / 2) / loginReferenceAspect)
  return aspect < loginReferenceAspect
    ? { fovMode: Camera.FOVMODE_VERTICAL_FIXED, fov: verticalFov }
    : {
        fovMode: Camera.FOVMODE_HORIZONTAL_FIXED,
        fov: loginReferenceHorizontalFov
      }
}

export function applyCoverCameraFraming(camera: TargetCamera, aspect: number) {
  const framing = coverCameraFraming(aspect)
  camera.fovMode = framing.fovMode
  camera.fov = framing.fov
}

export function applyMapEnvironment(
  scene: Scene,
  environment: MapEnvironmentManifestEntry
) {
  const ambient = color(environment.ambientColor).toLinearSpace()
  scene.ambientColor = ambient.scale(environment.ambientBrightness)
  scene.imageProcessingConfiguration.toneMappingEnabled = false
  scene.imageProcessingConfiguration.exposure = 1
  scene.imageProcessingConfiguration.contrast = 1
  const fog = environment.distanceFog
  if (fog) {
    const fogColor = color(fog.color).toLinearSpace()
    scene.fogMode = Scene.FOGMODE_LINEAR
    scene.fogColor = fogColor
    scene.fogStart = fog.start
    scene.fogEnd = fog.end
    scene.clearColor = new Color4(fogColor.r, fogColor.g, fogColor.b, 1)
  } else {
    scene.fogMode = Scene.FOGMODE_NONE
    scene.clearColor = new Color4(ambient.r, ambient.g, ambient.b, 1)
  }
}

function color(value: { r: number; g: number; b: number }) {
  return new Color3(value.r, value.g, value.b)
}
