<script setup lang="ts">
import '@babylonjs/loaders/glTF/index.js'
import type {
  LevelLightManifestEntry,
  LevelManifest,
  LevelRotation,
  LevelVector,
  SceneManifest
} from '@l2/ui'
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
  LightGizmo,
  LoadAssetContainerAsync,
  Mesh,
  PBRMaterial,
  PointLight,
  Scene,
  TransformNode,
  UtilityLayerRenderer,
  Vector3
} from '@babylonjs/core'
import {
  applyL2MaterialMetadata,
  composeAuthoredEffects,
  type ComposedAuthoredEffects
} from '@l2/babylon-runtime'
import type { Light } from '@babylonjs/core'
import { onBeforeUnmount, onMounted, watch } from 'vue'
import {
  configureUnrealScene,
  unrealNodeTransform,
  unrealForward,
  unrealVector
} from '../lib/unreal-transform'
import {
  createTerrainMaterial,
  type TerrainMaterialController
} from '../lib/terrain-material'

const props = withDefaults(
  defineProps<{
    manifest: LevelManifest | SceneManifest
    selectedActorName?: string
    actorsVisible?: boolean
    bspVisible?: boolean
    terrainLayerVisibility?: Record<string, boolean[]>
    lightHelpersVisible?: boolean
    selectedLightName?: string
    waterVolumesVisible?: boolean
    selectedWaterName?: string
  }>(),
  {
    selectedActorName: undefined,
    actorsVisible: true,
    bspVisible: true,
    terrainLayerVisibility: () => ({}),
    lightHelpersVisible: false,
    selectedLightName: undefined,
    waterVolumesVisible: true,
    selectedWaterName: undefined
  }
)
const emit = defineEmits<{
  error: [message: string]
  materialError: [message: string | undefined]
  lightSelect: [name: string]
}>()
const canvas = ref<HTMLCanvasElement>()
const loading = ref(false)
let engine: Engine | undefined
let scene: Scene | undefined
let resizeObserver: ResizeObserver | undefined
let loadVersion = 0
const containers = new Map<string, Promise<AssetContainer>>()
let terrainMeshes: AbstractMesh[] = []
let bspMeshes: AbstractMesh[] = []
let terrainMaterials: PBRMaterial[] = []
const terrainControllers = new Map<string, TerrainMaterialController>()
const actorMeshes = new Map<string, AbstractMesh[]>()
const waterMeshes = new Map<string, AbstractMesh[]>()
const levelLights = new Map<
  string,
  { source: LevelLightManifestEntry; rendered: Light }
>()
const lightGizmos = new Map<string, LightGizmo>()
let highlightLayer: HighlightLayer | undefined
let lightGizmoLayer: UtilityLayerRenderer | undefined
let pendingFocusActorName: string | undefined
let pendingFocusLightName: string | undefined
let pendingFocusWaterName: string | undefined
let waterMaterial: PBRMaterial | undefined
let authoredEffects: ComposedAuthoredEffects | undefined
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

function applySelectionHighlight() {
  highlightLayer?.removeAllMeshes()
  if (!highlightLayer) return
  if (props.actorsVisible && props.selectedActorName) {
    for (const mesh of actorMeshes.get(props.selectedActorName) ?? []) {
      if (mesh instanceof Mesh)
        highlightLayer.addMesh(mesh, highlightColor, false)
    }
  }
  if (props.waterVolumesVisible && props.selectedWaterName) {
    for (const mesh of waterMeshes.get(props.selectedWaterName) ?? []) {
      if (mesh instanceof Mesh)
        highlightLayer.addMesh(mesh, new Color3(0.2, 1, 1), false)
    }
  }
}

function applyActorVisibility() {
  for (const meshes of actorMeshes.values()) {
    for (const mesh of meshes) mesh.setEnabled(props.actorsVisible)
  }
  applySelectionHighlight()
}

function applyBspVisibility() {
  for (const mesh of bspMeshes) mesh.setEnabled(props.bspVisible)
}

function applyWaterVisibility() {
  for (const meshes of waterMeshes.values())
    for (const mesh of meshes) mesh.setEnabled(props.waterVolumesVisible)
  applySelectionHighlight()
}

function applyTerrainLayerVisibility() {
  for (const [terrainName, controller] of terrainControllers) {
    const enabled = props.terrainLayerVisibility[terrainName]
    if (!enabled) {
      controller.setAllLayersEnabled(true)
      continue
    }
    enabled.forEach((visible, index) =>
      controller.setLayerEnabled(index, visible)
    )
  }
}

function disposeLightGizmos() {
  for (const gizmo of lightGizmos.values()) gizmo.dispose()
  lightGizmos.clear()
}

function applyLightGizmoSelection() {
  for (const [name, gizmo] of lightGizmos) {
    const selected = name === props.selectedLightName
    gizmo.scaleRatio = selected ? 1.45 : 1
    const color = levelLights.get(name)?.rendered.diffuse ?? Color3.White()
    gizmo.material.diffuseColor.copyFrom(color)
    gizmo.material.emissiveColor.copyFrom(color.scale(selected ? 0.8 : 0.2))
  }
}

function syncLightGizmos() {
  disposeLightGizmos()
  if (!props.lightHelpersVisible || !lightGizmoLayer) return

  for (const [name, { rendered }] of levelLights) {
    const gizmo = new LightGizmo(lightGizmoLayer)
    gizmo.light = rendered
    gizmo.onClickedObservable.add(() => emit('lightSelect', name))
    lightGizmos.set(name, gizmo)
  }
  applyLightGizmoSelection()
}

function createLevelLights() {
  if (!scene) return
  for (const source of props.manifest.lights) {
    const color = Color3.FromHSV(
      (source.hue / 255) * 360,
      1 - source.saturation / 255,
      1
    )
    let rendered: Light
    if (
      source.className === 'NMovableSunLight' ||
      source.className === 'Sunlight'
    ) {
      const sun = new DirectionalLight(
        source.name,
        unrealForward(source.rotation),
        scene
      )
      sun.position = unrealVector(source.location)
      sun.diffuse = color
      sun.intensity = source.brightness / 64
      rendered = sun
    } else {
      const point = new PointLight(
        source.name,
        unrealVector(source.location),
        scene
      )
      point.diffuse = color
      point.intensity = source.brightness / 64
      point.range = source.radius * 64
      rendered = point
    }
    levelLights.set(source.name, { source, rendered })
  }

  for (const material of scene.materials) {
    if ('maxSimultaneousLights' in material) material.maxSimultaneousLights = 4
  }
  syncLightGizmos()
}

function focusActor(name: string) {
  if (!props.actorsVisible) return
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

function focusLight(name: string) {
  if (!scene) return
  const entry = levelLights.get(name)
  if (!entry) {
    pendingFocusLightName = name
    return
  }

  pendingFocusLightName = undefined
  const camera = scene.activeCamera as ArcRotateCamera
  const position = unrealVector(entry.source.location)
  camera.setTarget(position)
  camera.radius = Math.max(
    entry.source.radius * 96,
    (camera.lowerRadiusLimit ?? 1) * 2,
    512
  )
}

function focusWater(name: string) {
  if (!props.waterVolumesVisible) return
  pendingFocusWaterName = name
  if (!scene) return
  const meshes = waterMeshes.get(name)
  if (!meshes?.length) return
  pendingFocusWaterName = undefined
  for (const mesh of meshes) mesh.computeWorldMatrix(true)
  const camera = scene.activeCamera as ArcRotateCamera
  camera.zoomOn(meshes, true)
  camera.radius = Math.max(camera.radius * 1.5, camera.lowerRadiusLimit ?? 0)
}

function setCameraPose(location: LevelVector, rotation: LevelRotation) {
  if (!scene) return
  const camera = scene.activeCamera as ArcRotateCamera
  const position = unrealVector(location)
  camera.setPosition(position)
  camera.setTarget(position.add(unrealForward(rotation).scale(1024)))
}

defineExpose({ focusActor, focusLight, focusWater, setCameraPose, frameMap })

function frameMap(topDown = false) {
  if (!scene) return
  const camera = scene.activeCamera as ArcRotateCamera
  const worldMeshes = [...terrainMeshes, ...bspMeshes]
  const meshes = worldMeshes.length
    ? worldMeshes
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
  const rootRelative = url.startsWith('/')
  const loaded = LoadAssetContainerAsync(
    rootRelative ? url.slice(1) : url,
    scene!,
    {
      rootUrl: rootRelative ? '/' : undefined,
      pluginExtension: '.glb'
    }
  ).then((container) => {
    applyL2MaterialMetadata(container, scene!)
    return container
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
  authoredEffects?.dispose()
  authoredEffects = undefined
  terrainMeshes = []
  bspMeshes = []
  terrainMaterials.forEach((material) => material.dispose(true, true))
  terrainMaterials = []
  terrainControllers.clear()
  emit('materialError', undefined)
  actorMeshes.clear()
  waterMeshes.clear()
  waterMaterial?.dispose()
  waterMaterial = undefined
  disposeLightGizmos()
  levelLights.clear()
  highlightLayer?.removeAllMeshes()
  pendingFocusActorName = undefined
  pendingFocusLightName = undefined
  pendingFocusWaterName = undefined
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
    if ('bspMeshes' in props.manifest) {
      for (const bsp of props.manifest.bspMeshes) {
        if (bsp.error) emit('materialError', `${bsp.name}: ${bsp.error}`)
        if (!bsp.meshUrl) continue
        const container = await containerFor(bsp.meshUrl)
        if (version !== loadVersion) return
        const instance = container.instantiateModelsToScene(
          (name) => `${bsp.name}:${name}`,
          false
        )
        const placement = new TransformNode(`${bsp.name}:placement`, scene)
        for (const root of instance.rootNodes) root.parent = placement
        const meshes = instanceMeshes(instance.rootNodes as TransformNode[])
        for (const mesh of meshes) {
          mesh.checkCollisions = false
          mesh.isPickable = false
          mesh.setEnabled(props.bspVisible)
        }
        bspMeshes.push(...meshes)
      }
    }
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
      const meshes = instanceMeshes(instance.rootNodes as TransformNode[])
      const terrainMaterial = createTerrainMaterial(terrain, scene)
      if (terrainMaterial.material) {
        try {
          await terrainMaterial.ready
        } catch (error) {
          terrainMaterial.material.dispose(true, true)
          emit(
            'materialError',
            error instanceof Error
              ? error.message
              : 'Terrain texture arrays failed to load.'
          )
          terrainMeshes.push(...meshes)
          continue
        }
        terrainMaterials.push(terrainMaterial.material)
        if (terrainMaterial.controller)
          terrainControllers.set(terrain.name, terrainMaterial.controller)
        for (const mesh of meshes) mesh.material = terrainMaterial.material
      } else if (terrainMaterial.error) {
        emit('materialError', terrainMaterial.error)
      }
      terrainMeshes.push(...meshes)
    }
    applyTerrainLayerVisibility()

    // Frame the stable terrain bounds before actor loading completes. Actor
    // packages can contain oversized helper geometry that should not determine
    // the map overview.
    frameMap()

    const actors = props.manifest.actors.filter((actor) => actor.meshUrl)
    const failedActorMeshUrls = new Set<string>()
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
          try {
            const container = await containerFor(actor.meshUrl!)
            if (version !== loadVersion) return
            const instance = container.instantiateModelsToScene(
              (name) => `${actor.name}:${name}`,
              false
            )
            const placement = new TransformNode(
              `${actor.name}:placement`,
              scene
            )
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
            for (const mesh of actorMeshes.get(actor.name) ?? [])
              mesh.setEnabled(props.actorsVisible)
            if (props.selectedActorName === actor.name)
              applySelectionHighlight()
            if (pendingFocusActorName === actor.name) focusActor(actor.name)
          } catch (error) {
            if (!failedActorMeshUrls.has(actor.meshUrl!)) {
              failedActorMeshUrls.add(actor.meshUrl!)
              console.warn(
                `Unable to load level actors using ${actor.meshUrl}.`,
                error
              )
            }
          }
        })
      )
    }

    // Water diagnostics are loaded only after the stable terrain/actor framing
    // is established, so authored helper volumes never change the overview.
    if (!terrainMeshes.length) frameMap()

    waterMaterial = new PBRMaterial('water-volume-diagnostic', scene)
    waterMaterial.albedoColor = new Color3(0.05, 0.8, 0.95)
    waterMaterial.emissiveColor = new Color3(0.02, 0.22, 0.28)
    waterMaterial.alpha = 0.28
    waterMaterial.transparencyMode = PBRMaterial.PBRMATERIAL_ALPHABLEND
    waterMaterial.backFaceCulling = false
    for (const water of props.manifest.waterVolumes.filter(
      (volume) => volume.status === 'resolved' && volume.meshUrl
    )) {
      const container = await containerFor(water.meshUrl!)
      if (version !== loadVersion) return
      const instance = container.instantiateModelsToScene(
        (name) => `${water.name}:${name}`,
        false
      )
      const placement = new TransformNode(`${water.name}:placement`, scene)
      transform(
        placement,
        water.location,
        water.rotation,
        water.drawScale,
        water.drawScale3D,
        water.prePivot
      )
      for (const root of instance.rootNodes) root.parent = placement
      const meshes = instanceMeshes(instance.rootNodes as TransformNode[])
      waterMeshes.set(water.name, meshes)
      for (const mesh of meshes) {
        mesh.material = waterMaterial
        mesh.setEnabled(props.waterVolumesVisible)
        if (mesh instanceof Mesh) {
          mesh.enableEdgesRendering()
          mesh.edgesColor = new Color4(0.1, 0.95, 1, 0.9)
          mesh.edgesWidth = 2
        }
      }
      if (pendingFocusWaterName === water.name) focusWater(water.name)
    }
    applySelectionHighlight()

    createLevelLights()
    if ('effects' in props.manifest) {
      authoredEffects = composeAuthoredEffects(
        scene,
        props.manifest.effects,
        props.manifest.skyZones
      )
      if (authoredEffects.diagnostics.length)
        console.warn(authoredEffects.diagnostics.join('\n'))
      if (!terrainMeshes.length && !actorMeshes.size) frameMap()
    }
    if (pendingFocusLightName) focusLight(pendingFocusLightName)
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
  lightGizmoLayer = new UtilityLayerRenderer(scene)
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
watch(() => props.selectedActorName, applySelectionHighlight)
watch(() => props.actorsVisible, applyActorVisibility)
watch(() => props.bspVisible, applyBspVisibility)
watch(() => props.terrainLayerVisibility, applyTerrainLayerVisibility, {
  deep: true
})
watch(() => props.lightHelpersVisible, syncLightGizmos)
watch(() => props.selectedLightName, applyLightGizmoSelection)
watch(() => props.waterVolumesVisible, applyWaterVisibility)
watch(() => props.selectedWaterName, applySelectionHighlight)

onBeforeUnmount(() => {
  loadVersion++
  resizeObserver?.disconnect()
  authoredEffects?.dispose()
  disposeLightGizmos()
  lightGizmoLayer?.dispose()
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
