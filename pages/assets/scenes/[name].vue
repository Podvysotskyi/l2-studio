<script setup lang="ts">
import type {
  LevelRotation,
  LevelVector,
  SceneCatalogEntry,
  SceneManifest
} from '@l2/ui'
import { computed, onBeforeUnmount, watch } from 'vue'
import { useRoute } from 'vue-router'
import { assetCatalogEntryUrl } from '../../../lib/studio-content'
import {
  interpolateScenePose,
  sceneManagerLabel,
  scenePlaybackFrames
} from '../../../lib/scene-cinematic'

interface ScenePreviewApi {
  frameMap(): void
  setCameraPose(location: LevelVector, rotation: LevelRotation): void
}

const route = useRoute()
const config = useRuntimeConfig()
const manifest = ref<SceneManifest>()
const preview = ref<ScenePreviewApi>()
const loading = ref(true)
const error = ref<string>()
const previewError = ref<string>()
const frameIndex = ref(0)
const selectedManagerName = ref<string>()
const playing = ref(false)
let animationFrame: number | undefined
let segmentStartedAt = 0

const routeName = computed(() =>
  Array.isArray(route.params.name)
    ? (route.params.name[0] ?? '')
    : (route.params.name ?? '')
)
const frames = computed(() =>
  manifest.value
    ? scenePlaybackFrames(manifest.value, selectedManagerName.value)
    : []
)
const managerOptions = computed(() =>
  (manifest.value?.sceneManagers ?? []).map((manager) => ({
    label: sceneManagerLabel(manager),
    value: manager.name
  }))
)
const cinematicCount = computed(() =>
  manifest.value
    ? manifest.value.cameras.length +
      manifest.value.interpolationPoints.length +
      manifest.value.sceneManagers.length +
      manifest.value.actions.length
    : 0
)

function showFrame(index: number) {
  if (!frames.value.length) return
  frameIndex.value = Math.min(Math.max(index, 0), frames.value.length - 1)
  const frame = frames.value[frameIndex.value]!
  preview.value?.setCameraPose(frame.location, frame.rotation)
}

function stop() {
  playing.value = false
  if (animationFrame !== undefined) cancelAnimationFrame(animationFrame)
  animationFrame = undefined
}

function tick(timestamp: number) {
  if (!playing.value || frames.value.length < 2) return
  if (!segmentStartedAt) segmentStartedAt = timestamp
  const from = frames.value[frameIndex.value]!
  const nextIndex = (frameIndex.value + 1) % frames.value.length
  const to = frames.value[nextIndex]!
  const duration =
    (to.className === 'ActionWarp' ? 0.05 : Math.max(to.duration || 1, 0.1)) *
    1000
  const amount = (timestamp - segmentStartedAt) / duration
  const pose = interpolateScenePose(from, to, amount)
  preview.value?.setCameraPose(pose.location, pose.rotation)
  if (amount >= 1) {
    frameIndex.value = nextIndex
    segmentStartedAt = timestamp
  }
  animationFrame = requestAnimationFrame(tick)
}

function togglePlayback() {
  if (playing.value) {
    stop()
    return
  }
  if (frames.value.length < 2) return
  playing.value = true
  segmentStartedAt = 0
  animationFrame = requestAnimationFrame(tick)
}

function selectManager() {
  stop()
  showFrame(0)
}

function scrub(event: Event) {
  stop()
  showFrame(Number((event.target as HTMLInputElement).value))
}

async function loadScene() {
  stop()
  loading.value = true
  error.value = undefined
  manifest.value = undefined
  try {
    const entry = await $fetch<SceneCatalogEntry>(
      assetCatalogEntryUrl(config.public.apiBase, 'scenes', routeName.value)
    )
    if (!entry?.manifestUrl) {
      error.value = entry?.error ?? `Scene “${routeName.value}” is unavailable.`
      return
    }
    manifest.value = await $fetch<SceneManifest>(entry.manifestUrl)
    selectedManagerName.value = manifest.value.sceneManagers[0]?.name
  } catch {
    error.value = `Scene “${routeName.value}” could not be loaded.`
  } finally {
    loading.value = false
  }
}

watch(routeName, () => void loadScene(), { immediate: true })
onBeforeUnmount(stop)
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Client scene"
      :title="manifest?.name ?? routeName"
      description="Inspect reconstructed geometry, lighting, cameras, actions, sounds, and effects."
      icon="i-lucide-clapperboard"
    >
      <template #actions>
        <UButton
          label="All scenes"
          icon="i-lucide-arrow-left"
          color="neutral"
          variant="outline"
          to="/assets/scenes"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="error"
      color="error"
      title="Scene unavailable"
      :description="error"
    />
    <div
      v-if="loading"
      class="grid min-h-64 place-items-center text-sm text-muted"
    >
      Loading scene…
    </div>

    <template v-else-if="manifest">
      <div class="grid gap-3 sm:grid-cols-4">
        <UCard>
          <p class="text-xs text-muted">Placed meshes</p>
          <p class="text-2xl font-semibold">
            {{ manifest.actors.length.toLocaleString() }}
          </p>
        </UCard>
        <UCard>
          <p class="text-xs text-muted">Terrain</p>
          <p class="text-2xl font-semibold">
            {{ manifest.terrains.length }}
          </p>
        </UCard>
        <UCard>
          <p class="text-xs text-muted">Lights</p>
          <p class="text-2xl font-semibold">
            {{ manifest.lights.length }}
          </p>
        </UCard>
        <UCard>
          <p class="text-xs text-muted">Cinematic</p>
          <p class="text-2xl font-semibold">
            {{ cinematicCount.toLocaleString() }}
          </p>
        </UCard>
      </div>

      <UAlert
        v-if="previewError"
        color="error"
        title="Preview unavailable"
        :description="previewError"
      />
      <UCard :ui="{ body: 'p-2 sm:p-2' }">
        <StudioLevelPreview
          ref="preview"
          :manifest="manifest"
          @error="previewError = $event"
        />
        <div class="flex flex-wrap items-center gap-3 p-3">
          <UButton
            :label="playing ? 'Pause' : 'Play camera path'"
            :icon="playing ? 'i-lucide-pause' : 'i-lucide-play'"
            :disabled="frames.length < 2"
            @click="togglePlayback"
          />
          <select
            v-if="managerOptions.length"
            v-model="selectedManagerName"
            class="rounded-md border border-default bg-default px-3 py-2 text-sm"
            @change="selectManager"
          >
            <option
              v-for="manager in managerOptions"
              :key="manager.value"
              :value="manager.value"
            >
              {{ manager.label }}
            </option>
          </select>
          <UButton
            label="Frame scene"
            variant="outline"
            color="neutral"
            @click="preview?.frameMap()"
          />
          <input
            v-if="frames.length"
            class="min-w-48 flex-1"
            type="range"
            min="0"
            :max="frames.length - 1"
            :value="frameIndex"
            @input="scrub"
          />
          <span class="text-xs text-muted">
            {{
              frames.length
                ? `${frameIndex + 1} / ${frames.length}`
                : 'No camera path'
            }}
          </span>
        </div>
      </UCard>

      <div class="grid gap-4 lg:grid-cols-3">
        <UCard>
          <template #header>
            <h2 class="font-semibold">Scene orchestration</h2>
          </template>
          <p class="text-sm text-muted">
            {{ manifest.sceneManagers.length }} managers ·
            {{ manifest.actions.length }} actions
          </p>
          <ul class="mt-3 max-h-64 space-y-1 overflow-auto text-xs">
            <li v-for="action in manifest.actions" :key="action.name">
              {{ action.className }} · {{ action.name }}
            </li>
          </ul>
        </UCard>
        <UCard>
          <template #header>
            <h2 class="font-semibold">Ambient sound references</h2>
          </template>
          <ul class="max-h-72 space-y-1 overflow-auto text-xs">
            <li v-for="sound in manifest.ambientSounds" :key="sound.name">
              {{ sound.name
              }}<span v-if="sound.target" class="text-muted">
                · {{ sound.target }}</span
              >
            </li>
          </ul>
        </UCard>
        <UCard>
          <template #header>
            <h2 class="font-semibold">Effects</h2>
          </template>
          <ul class="max-h-72 space-y-1 overflow-auto text-xs">
            <li v-for="effect in manifest.effects" :key="effect.name">
              {{ effect.className }} · {{ effect.name }}
            </li>
          </ul>
        </UCard>
      </div>
    </template>
  </div>
</template>
