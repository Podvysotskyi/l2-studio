<script setup lang="ts">
import type { LevelCatalogEntry, LevelPreviewCatalogEntry } from '@l2/ui'
import { computed, nextTick, onBeforeUnmount, watch } from 'vue'
import type { LevelWorldCell, LevelWorldGrid } from '../lib/level-world-grid'
import {
  centerLevelWorldMap,
  constrainLevelWorldMapPan,
  fitLevelWorldMap,
  levelWorldMapMaximumScale,
  levelWorldMapMinimumScale,
  levelWorldMapTileSize,
  zoomLevelWorldMapAt,
  type LevelWorldMapPoint,
  type LevelWorldMapSize,
  type LevelWorldMapTransform
} from '../lib/level-world-map'

const props = defineProps<{
  grid: LevelWorldGrid
  previews: ReadonlyMap<string, LevelPreviewCatalogEntry>
  previewJobActive: boolean
  queueingPreviewName?: string
}>()
const emit = defineEmits<{
  generatePreview: [levelName: string]
}>()

const viewport = ref<HTMLElement>()
const selectedLevel = ref<LevelCatalogEntry>()
const transform = ref<LevelWorldMapTransform>({ x: 0, y: 0, scale: 1 })
const dragging = ref(false)
let resizeObserver: ResizeObserver | undefined
let pointer:
  | {
      id: number
      start: LevelWorldMapPoint
      origin: LevelWorldMapPoint
      moved: boolean
    }
  | undefined
let suppressClick = false

const worldSize = computed<LevelWorldMapSize>(() => ({
  width: props.grid.width * levelWorldMapTileSize,
  height: props.grid.height * levelWorldMapTileSize
}))
const mapStyle = computed(() => ({
  gridTemplateColumns: `repeat(${props.grid.width}, ${levelWorldMapTileSize}px)`,
  width: `${worldSize.value.width}px`,
  height: `${worldSize.value.height}px`,
  transform: `translate3d(${transform.value.x}px, ${transform.value.y}px, 0) scale(${transform.value.scale})`
}))
const selectedPreview = computed(() =>
  selectedLevel.value ? props.previews.get(selectedLevel.value.name) : undefined
)
const zoomLabel = computed(() => `${Math.round(transform.value.scale * 100)}%`)

function viewportSize(): LevelWorldMapSize {
  return {
    width: viewport.value?.clientWidth ?? 0,
    height: viewport.value?.clientHeight ?? 0
  }
}

function constrain(next: LevelWorldMapTransform) {
  transform.value = constrainLevelWorldMapPan(
    next,
    viewportSize(),
    worldSize.value
  )
}

function resetView() {
  transform.value = centerLevelWorldMap(viewportSize(), worldSize.value)
}

function fitView() {
  transform.value = fitLevelWorldMap(viewportSize(), worldSize.value)
}

function zoomTo(scale: number, anchor?: LevelWorldMapPoint) {
  const size = viewportSize()
  constrain(
    zoomLevelWorldMapAt(
      transform.value,
      scale,
      anchor ?? {
        x: size.width / 2,
        y: size.height / 2
      }
    )
  )
}

function onWheel(event: WheelEvent) {
  if (!viewport.value) return
  const bounds = viewport.value.getBoundingClientRect()
  const factor = Math.exp(-event.deltaY * 0.0015)
  zoomTo(transform.value.scale * factor, {
    x: event.clientX - bounds.left,
    y: event.clientY - bounds.top
  })
}

function onPointerDown(event: PointerEvent) {
  const target = event.target as HTMLElement
  if (target.closest('[data-map-control]')) return
  pointer = {
    id: event.pointerId,
    start: { x: event.clientX, y: event.clientY },
    origin: { x: transform.value.x, y: transform.value.y },
    moved: false
  }
}

function onPointerMove(event: PointerEvent) {
  if (!pointer || event.pointerId !== pointer.id) return
  const deltaX = event.clientX - pointer.start.x
  const deltaY = event.clientY - pointer.start.y
  if (!pointer.moved && Math.hypot(deltaX, deltaY) < 5) return
  if (!pointer.moved) viewport.value?.setPointerCapture(event.pointerId)
  pointer.moved = true
  dragging.value = true
  constrain({
    x: pointer.origin.x + deltaX,
    y: pointer.origin.y + deltaY,
    scale: transform.value.scale
  })
}

function finishPointer(event: PointerEvent) {
  if (!pointer || event.pointerId !== pointer.id) return
  suppressClick = pointer.moved
  if (suppressClick)
    window.setTimeout(() => {
      suppressClick = false
    }, 0)
  pointer = undefined
  dragging.value = false
  if (viewport.value?.hasPointerCapture(event.pointerId))
    viewport.value.releasePointerCapture(event.pointerId)
}

function onClickCapture(event: MouseEvent) {
  if (!suppressClick) return
  suppressClick = false
  event.preventDefault()
  event.stopPropagation()
}

function selectCell(cell: LevelWorldCell) {
  if (cell.level) selectedLevel.value = cell.level
}

function onViewportKeydown(event: KeyboardEvent) {
  if (event.target !== viewport.value) return
  const step = 48
  const movement: Record<string, LevelWorldMapPoint> = {
    ArrowLeft: { x: step, y: 0 },
    ArrowRight: { x: -step, y: 0 },
    ArrowUp: { x: 0, y: step },
    ArrowDown: { x: 0, y: -step }
  }
  const delta = movement[event.key]
  if (delta) {
    event.preventDefault()
    constrain({
      ...transform.value,
      x: transform.value.x + delta.x,
      y: transform.value.y + delta.y
    })
  }
}

watch(worldSize, async () => {
  await nextTick()
  resetView()
})

onMounted(() => {
  resetView()
  resizeObserver = new ResizeObserver(() => constrain(transform.value))
  if (viewport.value) resizeObserver.observe(viewport.value)
})

onBeforeUnmount(() => resizeObserver?.disconnect())
</script>

<template>
  <div class="grid overflow-hidden lg:grid-cols-[minmax(0,1fr)_18rem]">
    <section class="relative min-w-0 bg-[#09120f]">
      <div
        ref="viewport"
        class="level-map-viewport relative h-[clamp(32rem,68vh,52rem)] touch-none overflow-hidden outline-none select-none focus-visible:ring-2 focus-visible:ring-primary"
        :class="dragging ? 'cursor-grabbing' : 'cursor-grab'"
        tabindex="0"
        role="region"
        aria-label="Interactive world map. Drag to pan and scroll to zoom."
        @wheel.prevent="onWheel"
        @pointerdown="onPointerDown"
        @pointermove="onPointerMove"
        @pointerup="finishPointer"
        @pointercancel="finishPointer"
        @click.capture="onClickCapture"
        @keydown="onViewportKeydown"
      >
        <div
          class="absolute top-0 left-0 grid origin-top-left will-change-transform"
          :style="mapStyle"
          data-world-map-grid
        >
          <template v-for="cell in grid.cells" :key="cell.key">
            <button
              v-if="cell.level"
              type="button"
              class="group relative size-32 overflow-hidden border border-black/25 bg-elevated text-left outline-none transition-[filter] hover:z-10 hover:brightness-110 focus-visible:z-20 focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-primary"
              :class="[
                selectedLevel?.name === cell.level.name
                  ? 'z-10 ring-2 ring-inset ring-primary'
                  : '',
                cell.level.status === 'skipped' ? 'level-map-skipped' : ''
              ]"
              :aria-label="`Select level ${cell.level.name}`"
              :aria-pressed="selectedLevel?.name === cell.level.name"
              :data-level-name="cell.level.name"
              @click="selectCell(cell)"
            >
              <img
                v-if="previews.get(cell.level.name)?.imageUrl"
                :src="previews.get(cell.level.name)?.imageUrl ?? undefined"
                :alt="`Top-down preview of ${cell.level.name}`"
                loading="lazy"
                decoding="async"
                draggable="false"
                class="size-full object-cover"
              />
              <span
                v-else
                class="flex size-full items-center justify-center bg-muted/30 text-dimmed"
                aria-hidden="true"
              >
                <UIcon name="i-lucide-image-off" class="size-6" />
              </span>
              <span
                class="absolute right-1.5 bottom-1.5 rounded bg-black/70 px-1.5 py-0.5 font-mono text-[0.6875rem] font-semibold text-white shadow-sm"
              >
                {{ cell.level.name }}
              </span>
              <span
                class="absolute top-1.5 right-1.5 size-2 rounded-full ring-2 ring-black/50"
                :class="
                  cell.level.status === 'resolved' ? 'bg-success' : 'bg-warning'
                "
                aria-hidden="true"
              />
            </button>
            <div
              v-else
              class="size-32 border border-white/[0.035] bg-black/15"
              :title="`No imported level at ${cell.key}`"
              aria-hidden="true"
            />
          </template>
        </div>

        <div
          class="pointer-events-none absolute top-3 left-3 flex flex-col items-center rounded-md border border-white/10 bg-black/65 px-2 py-1.5 text-white shadow-lg backdrop-blur-sm"
          aria-hidden="true"
        >
          <span class="text-[0.625rem] font-bold tracking-[0.2em]">N</span>
          <UIcon name="i-lucide-navigation" class="size-4" />
        </div>

        <div
          data-map-control
          class="absolute top-3 right-3 flex items-center gap-1 rounded-lg border border-white/10 bg-black/70 p-1 shadow-lg backdrop-blur-sm"
        >
          <UButton
            icon="i-lucide-minus"
            color="neutral"
            variant="ghost"
            size="xs"
            aria-label="Zoom out"
            :disabled="transform.scale <= levelWorldMapMinimumScale"
            @click="zoomTo(transform.scale / 1.25)"
          />
          <UButton
            :label="zoomLabel"
            color="neutral"
            variant="ghost"
            size="xs"
            class="min-w-14 justify-center"
            aria-label="Reset map zoom to 100%"
            @click="resetView"
          />
          <UButton
            icon="i-lucide-plus"
            color="neutral"
            variant="ghost"
            size="xs"
            aria-label="Zoom in"
            :disabled="transform.scale >= levelWorldMapMaximumScale"
            @click="zoomTo(transform.scale * 1.25)"
          />
          <UButton
            icon="i-lucide-scan"
            color="neutral"
            variant="ghost"
            size="xs"
            aria-label="Fit world"
            @click="fitView"
          />
        </div>

        <p
          class="pointer-events-none absolute bottom-3 left-1/2 -translate-x-1/2 rounded-md bg-black/65 px-2.5 py-1 text-[0.6875rem] text-white/75 backdrop-blur-sm"
        >
          Drag to pan · scroll to zoom · select a tile for details
        </p>
      </div>
    </section>

    <aside
      class="border-t border-default bg-elevated/60 p-4 lg:border-t-0 lg:border-l"
      aria-label="Selected level details"
    >
      <template v-if="selectedLevel">
        <div
          class="overflow-hidden rounded-lg border border-default bg-default"
        >
          <div class="aspect-square bg-muted/30">
            <img
              v-if="selectedPreview?.imageUrl"
              :src="selectedPreview.imageUrl"
              :alt="`Selected preview of ${selectedLevel.name}`"
              class="size-full object-cover"
            />
            <div
              v-else
              class="flex size-full flex-col items-center justify-center gap-2 text-dimmed"
            >
              <UIcon name="i-lucide-image-off" class="size-7" />
              <span class="text-xs">Preview unavailable</span>
            </div>
          </div>
        </div>
        <div class="mt-4 flex items-start justify-between gap-3">
          <div>
            <p class="font-mono text-lg font-semibold text-highlighted">
              {{ selectedLevel.name }}
            </p>
            <p class="mt-0.5 text-xs text-muted">
              {{ selectedLevel.fileName }}
            </p>
          </div>
          <UBadge
            :color="selectedLevel.status === 'resolved' ? 'success' : 'warning'"
            variant="subtle"
          >
            {{ selectedLevel.status }}
          </UBadge>
        </div>
        <dl class="mt-5 grid grid-cols-3 gap-2 text-center">
          <div class="rounded-md bg-muted/40 px-2 py-2">
            <dt class="text-[0.625rem] uppercase tracking-wide text-muted">
              Terrain
            </dt>
            <dd class="mt-1 font-semibold text-highlighted">
              {{ selectedLevel.terrainCount }}
            </dd>
          </div>
          <div class="rounded-md bg-muted/40 px-2 py-2">
            <dt class="text-[0.625rem] uppercase tracking-wide text-muted">
              Meshes
            </dt>
            <dd class="mt-1 font-semibold text-highlighted">
              {{ selectedLevel.actorCount.toLocaleString() }}
            </dd>
          </div>
          <div class="rounded-md bg-muted/40 px-2 py-2">
            <dt class="text-[0.625rem] uppercase tracking-wide text-muted">
              Water
            </dt>
            <dd class="mt-1 font-semibold text-highlighted">
              {{ selectedLevel.waterVolumeCount }}
            </dd>
          </div>
        </dl>
        <UAlert
          v-if="selectedLevel.error || selectedPreview?.error"
          class="mt-4"
          color="warning"
          variant="subtle"
          title="Import warning"
          :description="
            selectedLevel.error || selectedPreview?.error || undefined
          "
        />
        <div class="mt-5 grid gap-2">
          <UButton
            :label="
              selectedPreview?.imageUrl
                ? 'Regenerate preview'
                : 'Generate preview'
            "
            icon="i-lucide-refresh-cw"
            color="neutral"
            variant="outline"
            block
            :loading="queueingPreviewName === selectedLevel.name"
            :disabled="previewJobActive || !selectedLevel.manifestUrl"
            @click="emit('generatePreview', selectedLevel.name)"
          />
          <UButton
            label="Open level"
            icon="i-lucide-arrow-up-right"
            block
            :disabled="!selectedLevel.manifestUrl"
            :to="
              selectedLevel.manifestUrl
                ? {
                    name: 'assets-levels-name',
                    params: { name: selectedLevel.name }
                  }
                : undefined
            "
          />
        </div>
      </template>
      <div
        v-else
        class="flex min-h-56 flex-col items-center justify-center px-4 text-center lg:min-h-full"
      >
        <span
          class="flex size-11 items-center justify-center rounded-full bg-primary/10 text-primary"
        >
          <UIcon name="i-lucide-map-pin" class="size-5" />
        </span>
        <p class="mt-3 text-sm font-medium text-highlighted">Select a level</p>
        <p class="mt-1 text-xs leading-5 text-muted">
          Choose a tile to inspect its preview and import details.
        </p>
      </div>
    </aside>
  </div>
</template>

<style scoped>
.level-map-viewport {
  background-color: #09120f;
  background-image:
    linear-gradient(rgb(255 255 255 / 0.025) 1px, transparent 1px),
    linear-gradient(90deg, rgb(255 255 255 / 0.025) 1px, transparent 1px),
    radial-gradient(circle at center, rgb(29 78 58 / 0.24), transparent 65%);
  background-size:
    32px 32px,
    32px 32px,
    100% 100%;
}

.level-map-skipped::after {
  position: absolute;
  inset: 0;
  content: '';
  pointer-events: none;
  background-image: repeating-linear-gradient(
    -45deg,
    transparent 0 8px,
    rgb(245 158 11 / 0.16) 8px 12px
  );
}
</style>
