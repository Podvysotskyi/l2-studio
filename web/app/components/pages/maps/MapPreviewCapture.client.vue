<script setup lang="ts">
import type { MapManifest } from '~/types/studio'
import { StudioWorldRenderer } from '~/runtime'
import { getPublishedManifest } from '../../../services/published-assets'
import { nextTick, onBeforeUnmount, onMounted } from 'vue'

const props = defineProps<{
  manifestUrl: string
  assetBaseUrl?: string
}>()
const canvas = ref<HTMLCanvasElement>()
let preview: StudioWorldRenderer | undefined

type CaptureState =
  | { status: 'loading' }
  | { status: 'ready' }
  | { status: 'error'; error: string }

function publish(state: CaptureState) {
  ;(
    window as typeof window & { __l2MapPreview?: CaptureState }
  ).__l2MapPreview = state
}

onMounted(async () => {
  publish({ status: 'loading' })
  try {
    await nextTick()
    const target =
      canvas.value ??
      document.querySelector<HTMLCanvasElement>('[data-map-preview-canvas]')
    if (!target) throw new Error('The preview canvas is unavailable.')
    const manifest = await getPublishedManifest<MapManifest>(
      props.manifestUrl,
      props.assetBaseUrl || undefined
    )
    preview = new StudioWorldRenderer(target, {
      interactive: false,
      preserveDrawingBuffer: true
    })
    await preview.loadManifest(manifest, {
      includeSkyZoneBsp: false,
      includeWorldBaseBsp: false,
      failOnTerrainMaterialError: true
    })
    preview.setVisibility({
      actors: true,
      bsp: true,
      skyZone: false,
      skyZoneChunks: {},
      worldBase: false,
      waterSurfaces: true,
      waterVolumes: true,
      lightHelpers: false
    })
    await preview.renderTopDown()
    publish({ status: 'ready' })
  } catch (error) {
    publish({
      status: 'error',
      error:
        error instanceof Error
          ? error.message
          : 'Map preview rendering failed.'
    })
  }
})

onBeforeUnmount(() => preview?.dispose())
</script>

<template>
  <canvas
    ref="canvas"
    data-map-preview-canvas
    width="512"
    height="512"
    class="block size-[512px]"
  />
</template>
