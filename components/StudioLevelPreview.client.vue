<script setup lang="ts">
import '@babylonjs/loaders/glTF/index.js'
import type { LevelManifest, LevelRotation, LevelVector } from '@l2/ui'
import {
  AbstractMesh,
  ArcRotateCamera,
  AssetContainer,
  Color4,
  Color3,
  DirectionalLight,
  Engine,
  HemisphericLight,
  HighlightLayer,
  LoadAssetContainerAsync,
  Mesh,
  PointLight,
  Scene,
  TransformNode,
  Vector3
} from '@babylonjs/core'
import { onBeforeUnmount, onMounted, watch } from 'vue'
import {
  configureUnrealScene,
  unrealNodeTransform,
  unrealForward,
  unrealVector
} from '../lib/unreal-transform'

const props = defineProps<{
  manifest: LevelManifest
  selectedActorName?: string
}>()
const emit = defineEmits<{ error: [message: string] }>()
const canvas = ref<HTMLCanvasElement>()
const loading = ref(false)
let engine: Engine | undefined
let scene: Scene | undefined
let resizeObserver: ResizeObserver | undefined
let loadVersion = 0
const containers = new Map<string, Promise<AssetContainer>>()
let terrainMeshes: AbstractMesh[] = []
const actorMeshes = new Map<string, AbstractMesh[]>()
let highlightLayer: HighlightLayer | undefined
let pendingFocusActorName: string | undefined
const highlightColor = new Color3(1, 0.55, 0.08)

function instanceMeshes(rootNodes: TransformNode[]) {
  return [
    ...new Set(
      rootNodes.flatMap((root) => [
        ...(root instanceof AbstractMesh && root.getTotalVertices() > 0
          ? [root]
          : []),
        ...root.getChildMeshes(false)
      ])
    )
  ]
}

function applyActorHighlight() {
  highlightLayer?.removeAllMeshes()
  if (!props.selectedActorName || !highlightLayer) return

  for (const mesh of actorMeshes.get(props.selectedActorName) ?? []) {
    if (mesh instanceof Mesh)
      highlightLayer.addMesh(mesh, highlightColor, false)
  }
}

function focusActor(name: string) {
  pendingFocusActorName = name
  if (!scene) return
  const meshes = actorMeshes.get(name)
  if (!meshes?.length) return

  pendingFocusActorName = undefined
  for (const mesh of meshes) mesh.computeWorldMatrix(true)
  const camera = scene.activeCamera as ArcRotateCamera
  camera.zoomOn(meshes, true)
  camera.radius = Math.max(camera.radius * 1.35, camera.lowerRadiusLimit ?? 0)
}

function setCameraPose(location: LevelVector, rotation: LevelRotation) {
  if (!scene) return
  const camera = scene.activeCamera as ArcRotateCamera
  const position = unrealVector(location)
  camera.setPosition(position)
  camera.setTarget(position.add(unrealForward(rotation).scale(1024)))
}

defineExpose({ focusActor, setCameraPose, frameMap })

function frameMap(topDown = false) {
  if (!scene) return
  const camera = scene.activeCamera as ArcRotateCamera
  const meshes = terrainMeshes.length
    ? terrainMeshes
    : scene.meshes.filter((mesh) => mesh.getTotalVertices() > 0)
  if (!meshes.length) return

  for (const mesh of meshes) mesh.computeWorldMatrix(true)
  camera.alpha = topDown ? -Math.PI / 2 : Math.PI / 4
  camera.beta = topDown ? 0.08 : Math.PI / 3
  camera.zoomOn(meshes, true)

  const overviewRadius = camera.radius
  camera.lowerRadiusLimit = Math.max(overviewRadius / 2_000, 1)
  camera.upperRadiusLimit = overviewRadius * 12
  camera.minZ = Math.max(overviewRadius / 20_000, 0.1)
  camera.maxZ = Math.max(overviewRadius * 20, 100_000)
  camera.storeState()
}

async function containerFor(url: string) {
  const existing = containers.get(url)
  if (existing) return existing
  const loaded = LoadAssetContainerAsync(url, scene!, {
    pluginExtension: '.glb'
  })
  containers.set(url, loaded)
  return loaded
}

function transform(
  node: TransformNode,
  location: LevelVector,
  unrealRotation: LevelRotation,
  drawScale = 1,
  drawScale3D: LevelVector = { x: 1, y: 1, z: 1 },
  prePivot?: LevelVector
) {
  const converted = unrealNodeTransform(
    location,
    unrealRotation,
    drawScale,
    drawScale3D,
    prePivot ?? { x: 0, y: 0, z: 0 }
  )
  node.position.copyFrom(converted.position)
  node.rotationQuaternion = converted.rotation
  node.scaling.copyFrom(converted.scaling)
}

async function loadLevel() {
  if (!scene) return
  const version = ++loadVersion
  loading.value = true
  terrainMeshes = []
  actorMeshes.clear()
  highlightLayer?.removeAllMeshes()
  pendingFocusActorName = undefined
  scene.transformNodes.slice().forEach((node) => node.dispose())
  scene.meshes.slice().forEach((mesh) => mesh.dispose())
  scene.lights
    .filter((light) => light.name !== 'level-preview-light')
    .forEach((light) => light.dispose())
  const loadedContainers = await Promise.allSettled(containers.values())
  for (const result of loadedContainers) {
    if (result.status === 'fulfilled') result.value.dispose()
  }
  containers.clear()

  try {
    for (const terrain of props.manifest.terrains) {
      if (!terrain.meshUrl) continue
      const container = await containerFor(terrain.meshUrl)
      if (version !== loadVersion) return
      const instance = container.instantiateModelsToScene(
        (name) => `${terrain.name}:${name}`,
        false
      )
      const placement = new TransformNode(`${terrain.name}:placement`, scene)
      transform(placement, terrain.location, terrain.rotation)
      for (const root of instance.rootNodes) root.parent = placement
      terrainMeshes.push(
        ...instanceMeshes(instance.rootNodes as TransformNode[])
      )
    }

    // Frame the stable terrain bounds before actor loading completes. Actor
    // packages can contain oversized helper geometry that should not determine
    // the map overview.
    frameMap()

    const actors = props.manifest.actors.filter((actor) => actor.meshUrl)
    for (const batch of actors.reduce<(typeof actors)[]>(
      (groups, actor, index) => {
        const group = Math.floor(index / 12)
        ;(groups[group] ??= []).push(actor)
        return groups
      },
      []
    )) {
      await Promise.all(
        batch.map(async (actor) => {
          const container = await containerFor(actor.meshUrl!)
          if (version !== loadVersion) return
          const instance = container.instantiateModelsToScene(
            (name) => `${actor.name}:${name}`,
            false
          )
          const placement = new TransformNode(`${actor.name}:placement`, scene)
          transform(
            placement,
            actor.location,
            actor.rotation,
            actor.drawScale,
            actor.drawScale3D,
            actor.prePivot
          )
          for (const root of instance.rootNodes) root.parent = placement
          actorMeshes.set(
            actor.name,
            instanceMeshes(instance.rootNodes as TransformNode[])
          )
          if (props.selectedActorName === actor.name) applyActorHighlight()
          if (pendingFocusActorName === actor.name) focusActor(actor.name)
        })
      )
    }

    for (const light of props.manifest.lights) {
      const color = Color3.FromHSV(
        (light.hue / 255) * 360,
        1 - light.saturation / 255,
        1
      )
      if (
        light.className === 'NMovableSunLight' ||
        light.className === 'Sunlight'
      ) {
        const direction = unrealForward(light.rotation)
        const sun = new DirectionalLight(light.name, direction, scene)
        sun.diffuse = color
        sun.intensity = light.brightness / 64
      } else {
        const point = new PointLight(
          light.name,
          unrealVector(light.location),
          scene
        )
        point.diffuse = color
        point.intensity = light.brightness / 64
        point.range = light.radius * 64
      }
    }

    if (!terrainMeshes.length) frameMap()
  } catch (error) {
    emit(
      'error',
      error instanceof Error
        ? error.message
        : 'The level preview could not be loaded.'
    )
  } finally {
    if (version === loadVersion) loading.value = false
  }
}

onMounted(() => {
  if (!canvas.value) return
  engine = new Engine(canvas.value, true, {
    preserveDrawingBuffer: true,
    stencil: true
  })
  scene = new Scene(engine)
  configureUnrealScene(scene)
  scene.clearColor = new Color4(0.2, 0.28, 0.38, 1)
  highlightLayer = new HighlightLayer('level-preview-highlight', scene)
  const camera = new ArcRotateCamera(
    'level-preview-camera',
    Math.PI / 4,
    Math.PI / 3,
    10000,
    Vector3.Zero(),
    scene
  )
  camera.attachControl(canvas.value, true)
  camera.wheelDeltaPercentage = 0.08
  camera.pinchDeltaPercentage = 0.01
  camera.useNaturalPinchZoom = true
  camera.zoomToMouseLocation = true
  camera.panningSensibility = 250
  new HemisphericLight(
    'level-preview-light',
    new Vector3(0.3, 1, -0.2),
    scene
  ).intensity = 1.2
  engine.runRenderLoop(() => scene?.render())
  resizeObserver = new ResizeObserver(() => engine?.resize())
  resizeObserver.observe(canvas.value)
  void loadLevel()
})

watch(
  () => props.manifest,
  () => void loadLevel()
)
watch(() => props.selectedActorName, applyActorHighlight)

onBeforeUnmount(() => {
  loadVersion++
  resizeObserver?.disconnect()
  scene?.dispose()
  engine?.dispose()
})
</script>

<template>
  <div class="relative overflow-hidden rounded-lg">
    <canvas
      ref="canvas"
      class="h-[70vh] min-h-96 w-full touch-none outline-none"
    />
    <div class="absolute top-3 right-3 flex gap-2">
      <UButton
        label="Frame map"
        icon="i-lucide-scan"
        color="neutral"
        variant="solid"
        size="sm"
        @click="frameMap()"
      />
      <UButton
        label="Top view"
        icon="i-lucide-map"
        color="neutral"
        variant="solid"
        size="sm"
        @click="frameMap(true)"
      />
    </div>
    <div
      v-if="loading"
      class="pointer-events-none absolute bottom-3 left-3 flex items-center gap-2 rounded-md bg-default/80 px-3 py-2 text-xs text-muted shadow-sm backdrop-blur"
    >
      <UIcon name="i-lucide-loader-circle" class="size-4 animate-spin" />
      Loading scene…
    </div>
  </div>
</template>
