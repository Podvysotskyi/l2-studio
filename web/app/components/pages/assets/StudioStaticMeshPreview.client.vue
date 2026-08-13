<script setup lang="ts">
import { onBeforeUnmount, onMounted, watch } from 'vue'
import { StudioStaticMeshRenderer } from '~/runtime'

const props = defineProps<{ url: string }>()
const emit = defineEmits<{
  error: [message: string]
  materialWarning: [message: string | undefined]
}>()
const canvas = ref<HTMLCanvasElement>()
let preview: StudioStaticMeshRenderer | undefined
let resizeObserver: ResizeObserver | undefined

async function loadMesh() {
  if (!preview) return
  emit('materialWarning', undefined)
  try {
    const warnings = await preview.load(props.url)
    emit('materialWarning', warnings.length ? warnings.join(' ') : undefined)
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
</script>

<template>
  <canvas
    ref="canvas"
    class="h-[70vh] min-h-96 w-full touch-none outline-none"
  />
</template>
