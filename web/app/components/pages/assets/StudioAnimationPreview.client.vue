<script setup lang="ts">
import { onBeforeUnmount, onMounted, watch } from 'vue'
import {
  StudioAnimationRenderer,
  type StudioAnimationState
} from '~/runtime'

const props = defineProps<{ url: string, animationUrl?: string | null }>()
const emit = defineEmits<{ error: [message: string] }>()
const canvas = ref<HTMLCanvasElement>()
const state = ref<StudioAnimationState>({ clipNames: [], duration: 0, time: 0, playing: false })
const speed = ref(1)
let preview: StudioAnimationRenderer | undefined
let resizeObserver: ResizeObserver | undefined

async function load() {
  if (!preview) return
  try {
    await preview.load(props.url, props.animationUrl)
  } catch (error) {
    emit('error', error instanceof Error ? error.message : 'The animation preview could not be loaded.')
  }
}

function selectClip(value: string) { preview?.select(value) }
function togglePlayback() { preview?.setPlaying(!state.value.playing) }
function seek(value: number) { preview?.seek(value) }
function updateSpeed(value: number) { speed.value = value; preview?.setSpeed(value) }

onMounted(() => {
  if (!canvas.value) return
  preview = new StudioAnimationRenderer(canvas.value, value => { state.value = value })
  resizeObserver = new ResizeObserver(() => preview?.resize())
  resizeObserver.observe(canvas.value)
  void load()
})
watch(() => [props.url, props.animationUrl], () => void load())
onBeforeUnmount(() => { resizeObserver?.disconnect(); preview?.dispose() })
</script>

<template>
  <div>
    <canvas ref="canvas" class="h-[55vh] min-h-80 w-full touch-none outline-none" />
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
