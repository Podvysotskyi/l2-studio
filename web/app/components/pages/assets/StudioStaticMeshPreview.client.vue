<script setup lang="ts">
import { onBeforeUnmount, onMounted, watch } from 'vue'
import {
  StudioStaticMeshRenderer,
  studioStaticMeshPreviewBackgrounds,
  type StudioStaticMeshPreviewBackground
} from '~/runtime'
import type {
  StaticMeshMaterialBehavior,
  StaticMeshMaterialInspection,
  StaticMeshTextureRole
} from '~/runtime/materials/static-mesh-material'

const props = defineProps<{ url: string }>()
const emit = defineEmits<{
  error: [message: string]
  materialWarning: [message: string | undefined]
  materials: [materials: StaticMeshMaterialInspection[]]
}>()
const canvas = ref<HTMLCanvasElement>()
const background = ref<StudioStaticMeshPreviewBackground>('dark')
let preview: StudioStaticMeshRenderer | undefined
let resizeObserver: ResizeObserver | undefined

async function loadMesh() {
  if (!preview) return
  emit('materialWarning', undefined)
  background.value = 'dark'
  preview.setBackground(background.value)
  try {
    const warnings = await preview.load(props.url)
    emit('materialWarning', warnings.length ? warnings.join(' ') : undefined)
    emit('materials', preview.materialInspections())
  } catch (error) {
    emit(
      'error',
      error instanceof Error
        ? error.message
        : 'The mesh preview could not be loaded.'
    )
  }
}

function setBackground(value: StudioStaticMeshPreviewBackground) {
  background.value = value
  preview?.setBackground(value)
}

function setMaterialEnabled(id: string, enabled: boolean) {
  return preview?.setMaterialEnabled(id, enabled) ?? []
}

function setTextureEnabled(id: string, role: StaticMeshTextureRole, enabled: boolean) {
  return preview?.setTextureEnabled(id, role, enabled) ?? []
}

function setBehaviorEnabled(id: string, behavior: StaticMeshMaterialBehavior, enabled: boolean) {
  return preview?.setBehaviorEnabled(id, behavior, enabled) ?? []
}

function resetMaterialInspections() {
  return preview?.resetMaterialInspections() ?? []
}

onMounted(() => {
  if (!canvas.value) return
  preview = new StudioStaticMeshRenderer(canvas.value)
  resizeObserver = new ResizeObserver(() => preview?.resize())
  resizeObserver.observe(canvas.value)
  void loadMesh()
})

watch(() => props.url, () => void loadMesh())

onBeforeUnmount(() => {
  resizeObserver?.disconnect()
  preview?.dispose()
})

defineExpose({
  setMaterialEnabled,
  setTextureEnabled,
  setBehaviorEnabled,
  resetMaterialInspections
})
</script>

<template>
  <div class="relative">
    <canvas
      ref="canvas"
      class="h-[70vh] min-h-96 w-full touch-none outline-none"
    />
    <div class="absolute right-3 top-3 flex rounded-md bg-default/90 p-1 shadow-sm backdrop-blur">
      <UButton
        v-for="preset in studioStaticMeshPreviewBackgrounds"
        :key="preset.id"
        :aria-label="`Use ${preset.label} preview background`"
        :title="preset.label"
        :variant="background === preset.id ? 'soft' : 'ghost'"
        color="neutral"
        size="xs"
        @click="setBackground(preset.id)"
      >
        <span
          class="size-3 rounded-full border border-default"
          :style="{ backgroundColor: `#${preset.color.toString(16).padStart(6, '0')}` }"
        />
      </UButton>
    </div>
  </div>
</template>
