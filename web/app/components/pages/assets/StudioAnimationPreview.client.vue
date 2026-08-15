<script setup lang="ts">
import { onBeforeUnmount, onMounted, watch } from 'vue'
import {
  StudioAnimationRenderer,
  studioAnimationPreviewBackgrounds,
  type StudioAnimationPreviewBackground,
  type StudioAnimationMaterialBinding,
  type StudioAnimationState
} from '~/runtime'
import type { StaticMeshMaterialInspection } from '~/runtime/materials/static-mesh-material'

const props = defineProps<{
  url: string
  animationUrl?: string | null
  materialBindings?: StudioAnimationMaterialBinding[]
}>()
const emit = defineEmits<{
  error: [message: string]
  materialWarning: [message: string | undefined]
  materials: [materials: StaticMeshMaterialInspection[]]
}>()
const canvas = ref<HTMLCanvasElement>()
const state = ref<StudioAnimationState>({ clipNames: [], duration: 0, time: 0, playing: false })
const speed = ref(1)
const background = ref<StudioAnimationPreviewBackground>('dark')
let preview: StudioAnimationRenderer | undefined
let resizeObserver: ResizeObserver | undefined

async function load() {
  if (!preview) return
  emit('materialWarning', undefined)
  background.value = 'dark'
  preview.setBackground(background.value)
  try {
    const warnings = await preview.load(props.url, props.animationUrl, props.materialBindings)
    emit('materialWarning', warnings.length ? warnings.join(' ') : undefined)
    emit('materials', preview.materialInspections())
  } catch (error) {
    emit('error', error instanceof Error ? error.message : 'The animation preview could not be loaded.')
  }
}

function selectClip(value: string) { preview?.select(value) }
function togglePlayback() { preview?.setPlaying(!state.value.playing) }
function seek(value: number) { preview?.seek(value) }
function updateSpeed(value: number) { speed.value = value; preview?.setSpeed(value) }
function setBackground(value: StudioAnimationPreviewBackground) {
  background.value = value
  preview?.setBackground(value)
}

onMounted(() => {
  if (!canvas.value) return
  preview = new StudioAnimationRenderer(canvas.value, value => { state.value = value })
  resizeObserver = new ResizeObserver(() => preview?.resize())
  resizeObserver.observe(canvas.value)
  void load()
})
watch(() => [props.url, props.animationUrl, props.materialBindings], () => void load(), { deep: true })
onBeforeUnmount(() => { resizeObserver?.disconnect(); preview?.dispose() })
</script>

<template>
  <div>
    <div class="relative">
      <canvas ref="canvas" class="h-[55vh] min-h-80 w-full touch-none outline-none" />
      <div class="absolute right-3 top-3 flex rounded-md bg-default/90 p-1 shadow-sm backdrop-blur">
        <UButton
          v-for="preset in studioAnimationPreviewBackgrounds"
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
    <div class="space-y-3 border-t border-default p-4">
      <div class="flex flex-wrap items-center gap-3">
        <UButton
          :icon="state.playing ? 'i-lucide-pause' : 'i-lucide-play'"
          :label="state.playing ? 'Pause' : 'Play'"
          :disabled="!state.clipName"
          @click="togglePlayback"
        />
        <USelect
          :model-value="state.clipName"
          :items="state.clipNames.map(name => ({ label: name, value: name }))"
          class="min-w-56"
          placeholder="Bind pose"
          @update:model-value="value => selectClip(String(value))"
        />
        <USelect
          :model-value="speed"
          :items="[0.25, 0.5, 1, 1.5, 2].map(value => ({ label: `${value}×`, value }))"
          class="w-24"
          @update:model-value="value => updateSpeed(Number(value))"
        />
        <span class="text-xs text-muted">{{ state.time.toFixed(2) }} / {{ state.duration.toFixed(2) }}s</span>
      </div>
      <input
        type="range"
        min="0"
        :max="Math.max(state.duration, 0.001)"
        step="0.01"
        :value="state.time"
        class="w-full accent-primary"
        aria-label="Animation time"
        @input="seek(Number(($event.target as HTMLInputElement).value))"
      >
      <slot :state="state" />
    </div>
  </div>
</template>
