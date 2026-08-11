<script setup lang="ts">
import '@babylonjs/loaders/glTF/index.js'
import type { LevelManifest } from '~/types/studio'
import { Camera, Engine, FreeCamera, Scene, Vector3 } from '@babylonjs/core'
import { composeLevelManifest, configureUnrealScene } from '~/runtime'
import { getPublishedManifest } from '../../../services/published-assets'
import { nextTick, onBeforeUnmount, onMounted } from 'vue'
import { calculateLevelPreviewFrame } from '../../../utils/level-preview-frame'

const props = defineProps<{ manifestUrl: string }>()
const canvas = ref<HTMLCanvasElement>()
let engine: Engine | undefined
let scene: Scene | undefined

type CaptureState =
  | { status: 'loading' }
  | { status: 'ready' }
  | { status: 'error'; error: string }

function publish(state: CaptureState) {
  ;(
    window as typeof window & { __l2LevelPreview?: CaptureState }
  ).__l2LevelPreview = state
}

function frameTopDown(
  composed: Awaited<ReturnType<typeof composeLevelManifest>>
) {
  if (!scene) return
  const terrain = composed.terrainMeshes.filter((mesh) => mesh.isEnabled())
  const actors = [...composed.actorMeshes.values()]
    .flat()
    .filter((mesh) => mesh.isEnabled())
  const bsp = composed.bspMeshes.filter((mesh) => mesh.isEnabled())
  const water = composed.waterSurfaceMeshes.filter((mesh) => mesh.isEnabled())
  const meshes = terrain.length
    ? terrain
    : actors.length
      ? actors
      : bsp.length
        ? bsp
        : water
  if (!meshes.length)
    throw new Error('The level contains no renderable geometry.')

  for (const mesh of meshes) {
    mesh.computeWorldMatrix(true)
    mesh.refreshBoundingInfo(false, false)
  }
  const minimum = new Vector3(
    Math.min(
      ...meshes.map((mesh) => mesh.getBoundingInfo().boundingBox.minimumWorld.x)
    ),
    Math.min(
      ...meshes.map((mesh) => mesh.getBoundingInfo().boundingBox.minimumWorld.y)
    ),
    Math.min(
      ...meshes.map((mesh) => mesh.getBoundingInfo().boundingBox.minimumWorld.z)
    )
  )
  const maximum = new Vector3(
    Math.max(
      ...meshes.map((mesh) => mesh.getBoundingInfo().boundingBox.maximumWorld.x)
    ),
    Math.max(
      ...meshes.map((mesh) => mesh.getBoundingInfo().boundingBox.maximumWorld.y)
    ),
    Math.max(
      ...meshes.map((mesh) => mesh.getBoundingInfo().boundingBox.maximumWorld.z)
    )
  )
  const frame = calculateLevelPreviewFrame({ minimum, maximum })
  const center = new Vector3(frame.center.x, frame.center.y, frame.center.z)
  const camera = new FreeCamera(
    'level-preview-camera',
    new Vector3(frame.camera.x, frame.camera.y, frame.camera.z),
    scene
  )
  camera.mode = Camera.ORTHOGRAPHIC_CAMERA
  camera.upVector.set(frame.up.x, frame.up.y, frame.up.z)
  camera.setTarget(center)
  camera.orthoLeft = -frame.extent / 2
  camera.orthoRight = frame.extent / 2
  camera.orthoTop = frame.extent / 2
  camera.orthoBottom = -frame.extent / 2
  camera.minZ = 0.1
  camera.maxZ = frame.maxZ
  scene.activeCamera = camera
}

onMounted(async () => {
  publish({ status: 'loading' })
  try {
    await nextTick()
    const target =
      canvas.value ??
      document.querySelector<HTMLCanvasElement>('[data-level-preview-canvas]')
    if (!target) throw new Error('The preview canvas is unavailable.')
    const manifest = await getPublishedManifest<LevelManifest>(props.manifestUrl)
    engine = new Engine(target, true, {
      preserveDrawingBuffer: true,
      stencil: true
    })
    engine.setHardwareScalingLevel(1)
    scene = new Scene(engine)
    configureUnrealScene(scene)
    const materialErrors: string[] = []
    const composed = await composeLevelManifest(scene, manifest, {
      batchSize: 24,
      includeSkyZoneBsp: false,
      includeWorldBaseBsp: false,
      onMaterialError: (message) => materialErrors.push(message)
    })
    if (materialErrors.length) throw new Error(materialErrors.join(' '))
    // Authored distance fog is calibrated for an in-world camera. The elevated
    // orthographic overview otherwise sits beyond the fog end and captures only
    // the fog color for every fog-enabled coordinate map.
    scene.fogMode = Scene.FOGMODE_NONE
    frameTopDown(composed)
    await scene.whenReadyAsync()
    scene.render()
    scene.render()
    publish({ status: 'ready' })
  } catch (error) {
    publish({
      status: 'error',
      error:
        error instanceof Error
          ? error.message
          : 'Level preview rendering failed.'
    })
  }
})

onBeforeUnmount(() => {
  scene?.dispose()
  engine?.dispose()
})
</script>

<template>
  <canvas
    ref="canvas"
    data-level-preview-canvas
    width="512"
    height="512"
    class="block size-[512px]"
  />
</template>
