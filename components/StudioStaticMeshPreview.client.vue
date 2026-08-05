<script setup lang="ts">
import '@babylonjs/loaders/glTF/index.js'
import {
  ArcRotateCamera,
  Color4,
  Engine,
  HemisphericLight,
  LoadAssetContainerAsync,
  Scene,
  Vector3
} from '@babylonjs/core'
import { onBeforeUnmount, onMounted, watch } from 'vue'

const props = defineProps<{ url: string }>()
const emit = defineEmits<{ error: [message: string] }>()
const canvas = ref<HTMLCanvasElement>()
let engine: Engine | undefined
let scene: Scene | undefined
let resizeObserver: ResizeObserver | undefined
let loadVersion = 0

async function loadMesh() {
  if (!scene) return
  const version = ++loadVersion
  scene.meshes
    .filter((mesh) => mesh.name !== '__root__')
    .forEach((mesh) => mesh.dispose())
  try {
    const container = await LoadAssetContainerAsync(props.url, scene, {
      pluginExtension: '.glb'
    })
    if (version !== loadVersion) {
      container.dispose()
      return
    }
    container.addAllToScene()
    const camera = scene.activeCamera as ArcRotateCamera
    camera.zoomOn(scene.meshes, true)
  } catch (error) {
    emit(
      'error',
      error instanceof Error
        ? error.message
        : 'The mesh preview could not be loaded.'
    )
  }
}

onMounted(() => {
  if (!canvas.value) return
  engine = new Engine(canvas.value, true, { preserveDrawingBuffer: true })
  scene = new Scene(engine)
  scene.clearColor = new Color4(0.035, 0.045, 0.065, 1)
  const camera = new ArcRotateCamera(
    'preview-camera',
    Math.PI / 4,
    Math.PI / 3,
    10,
    Vector3.Zero(),
    scene
  )
  camera.attachControl(canvas.value, true)
  camera.wheelPrecision = 35
  new HemisphericLight(
    'preview-light',
    new Vector3(0.3, 1, -0.2),
    scene
  ).intensity = 1.25
  engine.runRenderLoop(() => scene?.render())
  resizeObserver = new ResizeObserver(() => engine?.resize())
  resizeObserver.observe(canvas.value)
  void loadMesh()
})

watch(
  () => props.url,
  () => void loadMesh()
)

onBeforeUnmount(() => {
  loadVersion++
  resizeObserver?.disconnect()
  scene?.dispose()
  engine?.dispose()
})
</script>

<template>
  <canvas
    ref="canvas"
    class="h-[70vh] min-h-96 w-full touch-none outline-none"
  />
</template>
