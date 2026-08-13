<script setup lang="ts">
import type {
  MapManifest,
  MapRotation,
  MapVector,
  SceneManifest
} from '~/types/studio'
import { StudioWorldRenderer } from '~/runtime'
import { onBeforeUnmount, onMounted, watch } from 'vue'

const props = withDefaults(
  defineProps<{
    manifest: MapManifest | SceneManifest
    selectedActorName?: string
    selectedBspName?: string
    actorsVisible?: boolean
    bspVisible?: boolean
    skyZoneVisible?: boolean
    skyZoneChunkVisibility?: Record<string, boolean>
    worldBaseVisible?: boolean
    terrainLayerVisibility?: Record<string, boolean[]>
    lightHelpersVisible?: boolean
    selectedLightName?: string
    waterSurfacesVisible?: boolean
    selectedWaterSurfaceName?: string
    waterVolumesVisible?: boolean
    selectedWaterName?: string
  }>(),
  {
    selectedActorName: undefined,
    selectedBspName: undefined,
    actorsVisible: true,
    bspVisible: true,
    skyZoneVisible: false,
    skyZoneChunkVisibility: () => ({}),
    worldBaseVisible: false,
    terrainLayerVisibility: () => ({}),
    lightHelpersVisible: false,
    selectedLightName: undefined,
    waterSurfacesVisible: true,
    selectedWaterSurfaceName: undefined,
    waterVolumesVisible: true,
    selectedWaterName: undefined
  }
)
const emit = defineEmits<{
  error: [message: string]
  materialError: [message: string | undefined]
  lightSelect: [name: string]
  readyChange: [ready: boolean]
}>()
const canvas = ref<HTMLCanvasElement>()
const loading = ref(false)
const ready = ref(false)
let preview: StudioWorldRenderer | undefined
let resizeObserver: ResizeObserver | undefined
let loadVersion = 0

function setReady(value: boolean) {
  ready.value = value
  preview?.setInteractionEnabled(value)
  emit('readyChange', value)
}

function applyVisibility() {
  preview?.setVisibility({
    actors: props.actorsVisible,
    bsp: props.bspVisible,
    skyZone: props.skyZoneVisible,
    skyZoneChunks: props.skyZoneChunkVisibility,
    worldBase: props.worldBaseVisible,
    waterSurfaces: props.waterSurfacesVisible,
    waterVolumes: props.waterVolumesVisible,
    lightHelpers: props.lightHelpersVisible
  })
}

function applySelection() {
  preview?.setSelection({
    actor: props.selectedActorName,
    bsp: props.selectedBspName,
    light: props.selectedLightName,
    waterSurface: props.selectedWaterSurfaceName,
    water: props.selectedWaterName
  })
}

async function loadMap() {
  if (!preview) return
  const version = ++loadVersion
  loading.value = true
  setReady(false)
  emit('materialError', undefined)
  try {
    await preview.loadManifest(props.manifest, {
      includeSkyZoneBsp: true,
      includeWorldBaseBsp: true,
      onMaterialError: message => emit('materialError', message)
    })
    if (version !== loadVersion) return
    applyVisibility()
    applySelection()
    preview.setTerrainLayerVisibility(props.terrainLayerVisibility)
    setReady(true)
  } catch (error) {
    if (version !== loadVersion) return
    emit(
      'error',
      error instanceof Error
        ? error.message
        : 'The map preview could not be loaded.'
    )
  } finally {
    if (version === loadVersion) loading.value = false
  }
}

function focusActor(name: string) {
  preview?.focusActor(name)
}

function focusBsp(name: string) {
  preview?.focusBsp(name)
}

function focusLight(name: string) {
  preview?.focusLight(name)
}

function focusWater(name: string) {
  preview?.focusWater(name)
}

function focusWaterSurface(name: string) {
  preview?.focusWaterSurface(name)
}

function setCameraPose(location: MapVector, rotation: MapRotation) {
  preview?.setCameraPose(location, rotation)
}

function focusPosition(location: MapVector, radius = 1024) {
  preview?.focusPosition(location, radius)
}

function frameMap(topDown = false) {
  preview?.frameMap(topDown)
}

function frameBsp() {
  preview?.frameBsp()
}

defineExpose({
  focusActor,
  focusBsp,
  focusLight,
  focusWater,
  focusWaterSurface,
  focusPosition,
  setCameraPose,
  frameMap,
  frameBsp
})

onMounted(() => {
  if (!canvas.value) return
  preview = new StudioWorldRenderer(canvas.value, {
    interactive: true,
    preserveDrawingBuffer: true,
    onLightSelect: name => emit('lightSelect', name)
  })
  resizeObserver = new ResizeObserver(() => preview?.resize())
  resizeObserver.observe(canvas.value)
  void loadMap()
})

watch(() => props.manifest, () => void loadMap())
watch(
  () => [
    props.actorsVisible,
    props.bspVisible,
    props.skyZoneVisible,
    props.skyZoneChunkVisibility,
    props.worldBaseVisible,
    props.waterSurfacesVisible,
    props.waterVolumesVisible,
    props.lightHelpersVisible
  ],
  applyVisibility,
  { deep: true }
)
watch(
  () => [
    props.selectedActorName,
    props.selectedBspName,
    props.selectedLightName,
    props.selectedWaterSurfaceName,
    props.selectedWaterName
  ],
  applySelection
)
watch(
  () => props.terrainLayerVisibility,
  visibility => preview?.setTerrainLayerVisibility(visibility),
  { deep: true }
)

onBeforeUnmount(() => {
  loadVersion++
  resizeObserver?.disconnect()
  preview?.dispose()
})
</script>

<template>
  <div class="relative overflow-hidden rounded-lg">
    <canvas
      ref="canvas"
      class="h-[70vh] min-h-96 w-full touch-none outline-none"
      :aria-busy="loading"
      :aria-disabled="!ready"
    />
    <div
      v-if="!ready"
      class="absolute inset-0 z-10 cursor-wait"
      aria-hidden="true"
    />
    <div class="absolute top-3 right-3 z-20 flex gap-2">
      <UButton
        label="Frame map"
        icon="i-lucide-scan"
        color="neutral"
        variant="solid"
        size="sm"
        :disabled="!ready"
        @click="frameMap()"
      />
      <UButton
        label="Top view"
        icon="i-lucide-map"
        color="neutral"
        variant="solid"
        size="sm"
        :disabled="!ready"
        @click="frameMap(true)"
      />
      <UButton
        label="Frame BSP"
        icon="i-lucide-blocks"
        color="neutral"
        variant="solid"
        size="sm"
        :disabled="!ready"
        @click="frameBsp()"
      />
    </div>
    <div
      v-if="loading"
      class="pointer-events-none absolute bottom-3 left-3 z-20 flex items-center gap-2 rounded-md bg-default/80 px-3 py-2 text-xs text-muted shadow-sm backdrop-blur"
    >
      <UIcon name="i-lucide-loader-circle" class="size-4 animate-spin" />
      Loading scene…
    </div>
  </div>
</template>
