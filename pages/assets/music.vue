<script setup lang="ts">
import type { MusicManifest, MusicManifestEntry } from '@l2/ui'
import { musicManifestUrl } from '@l2/ui'
import { computed, nextTick, onBeforeUnmount, watch } from 'vue'
import { assetImportsUrl, type AssetImportJob } from '../../lib/studio-content'

const config = useRuntimeConfig()
const jobs = ref<AssetImportJob[]>([])
const manifest = ref<MusicManifest>()
const query = ref('')
const page = ref(1)
const pageSize = ref(50)
const queueing = ref(false)
const error = ref<string>()
const selectedTrack = ref<MusicManifestEntry>()
const audioPlayer = ref<HTMLAudioElement>()
let pollTimer: ReturnType<typeof setTimeout> | undefined

const activeJob = computed(() =>
  jobs.value.find((job) => job.status === 'queued' || job.status === 'running')
)
const filteredTracks = computed(() => {
  const term = query.value.trim().toLocaleLowerCase()
  return (manifest.value?.tracks ?? []).filter(
    (track) =>
      !term ||
      track.name.toLocaleLowerCase().includes(term) ||
      track.fileName.toLocaleLowerCase().includes(term)
  )
})
const visibleTracks = computed(() => {
  const offset = (page.value - 1) * pageSize.value
  return filteredTracks.value.slice(offset, offset + pageSize.value)
})
const resolvedCount = computed(
  () =>
    manifest.value?.tracks.filter((track) => track.status === 'resolved')
      .length ?? 0
)

watch([query, pageSize], () => (page.value = 1))

function formatDuration(value: number | null) {
  if (value === null) return '—'
  const seconds = Math.round(value)
  const minutes = Math.floor(seconds / 60)
  return `${minutes}:${String(seconds % 60).padStart(2, '0')}`
}

function formatBytes(value: number) {
  return `${(value / 1024 / 1024).toFixed(1)} MB`
}

async function playTrack(track: MusicManifestEntry) {
  if (!track.url) return
  selectedTrack.value = track
  await nextTick()
  await audioPlayer.value?.play()
}

async function loadManifest() {
  try {
    manifest.value = await $fetch<MusicManifest>(musicManifestUrl(), {
      query: { refresh: Date.now() }
    })
    if (
      !selectedTrack.value ||
      !manifest.value.tracks.some(
        (track) => track.fileName === selectedTrack.value?.fileName
      )
    ) {
      selectedTrack.value = manifest.value.tracks.find((track) => track.url)
    }
  } catch {
    manifest.value = undefined
    selectedTrack.value = undefined
  }
}

async function loadJobs(schedule = true) {
  clearTimeout(pollTimer)
  try {
    jobs.value = await $fetch<AssetImportJob[]>(
      assetImportsUrl(config.public.apiBase, 'music'),
      { query: { limit: 20 } }
    )
    error.value = undefined
    if (!activeJob.value) await loadManifest()
  } catch {
    error.value = 'Music import jobs could not be loaded from the Studio API.'
  }

  if (schedule && activeJob.value) {
    pollTimer = setTimeout(() => void loadJobs(), 1000)
  }
}

async function queueImport() {
  queueing.value = true
  error.value = undefined
  try {
    await $fetch(assetImportsUrl(config.public.apiBase, 'music'), {
      method: 'POST'
    })
    await loadJobs()
  } catch {
    error.value =
      'The music import could not be queued. Another music import may already be active.'
  } finally {
    queueing.value = false
  }
}

onMounted(() => void loadJobs())
onBeforeUnmount(() => clearTimeout(pollTimer))
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Asset pipeline"
      title="Music assets"
      description="Convert the Interlude L2SD music collection into browser-playable Ogg Vorbis assets."
      icon="i-lucide-music-2"
    >
      <template #actions>
        <UButton
          label="Import jobs"
          icon="i-lucide-history"
          color="neutral"
          variant="outline"
          to="/assets/jobs"
        />
        <UButton
          label="Import music"
          icon="i-lucide-play"
          :loading="queueing"
          :disabled="Boolean(activeJob)"
          @click="queueImport"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="Music import unavailable"
      :description="error"
    />

    <UCard v-if="activeJob" variant="subtle">
      <div class="flex flex-wrap items-center gap-4">
        <UIcon
          name="i-lucide-loader-circle"
          class="size-5 animate-spin text-primary"
        />
        <div class="min-w-0 flex-1">
          <p class="font-medium text-highlighted">
            Import {{ activeJob.status }}
          </p>
          <p class="truncate text-xs text-muted">{{ activeJob.sourcePath }}</p>
        </div>
        <UBadge color="info" variant="subtle">
          {{ activeJob.processedCount }} / {{ activeJob.totalCount || '…' }}
        </UBadge>
      </div>
      <UProgress
        class="mt-4"
        :model-value="activeJob.processedCount"
        :max="activeJob.totalCount || 1"
      />
    </UCard>

    <UCard v-if="selectedTrack" variant="subtle">
      <div class="flex flex-wrap items-center gap-4">
        <div
          class="grid size-12 shrink-0 place-items-center rounded-lg bg-primary/10 text-primary"
        >
          <UIcon name="i-lucide-music" class="size-6" />
        </div>
        <div class="min-w-0 flex-1">
          <p class="truncate font-medium text-highlighted">
            {{ selectedTrack.name }}
          </p>
          <p class="text-xs text-muted">
            {{ formatDuration(selectedTrack.durationSeconds) }} ·
            {{ selectedTrack.sampleRate?.toLocaleString() }} Hz ·
            {{ selectedTrack.channels }} channels ·
            {{ formatBytes(selectedTrack.sizeBytes) }}
          </p>
        </div>
        <audio
          ref="audioPlayer"
          :key="selectedTrack.url ?? undefined"
          :src="selectedTrack.url ?? undefined"
          controls
          preload="metadata"
          class="h-10 w-full sm:w-[28rem]"
        />
      </div>
    </UCard>

    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <template #header>
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="text-sm font-semibold text-highlighted">
              Music library
            </h2>
            <p class="text-xs text-muted">
              {{ resolvedCount }} resolved ·
              {{ (manifest?.tracks.length ?? 0) - resolvedCount }} skipped
            </p>
          </div>
          <UInput
            v-model="query"
            icon="i-lucide-search"
            placeholder="Search tracks"
            aria-label="Search music tracks"
            class="w-full sm:w-72"
          />
        </div>
      </template>

      <div
        v-if="manifest"
        class="max-h-[42rem] divide-y divide-default overflow-y-auto"
      >
        <div
          v-for="track in visibleTracks"
          :key="track.fileName"
          class="flex min-w-0 items-center gap-4 p-4"
          :class="
            selectedTrack?.fileName === track.fileName ? 'bg-primary/5' : ''
          "
        >
          <UButton
            :icon="
              selectedTrack?.fileName === track.fileName
                ? 'i-lucide-volume-2'
                : 'i-lucide-play'
            "
            color="neutral"
            variant="soft"
            :disabled="!track.url"
            :aria-label="`Play ${track.name}`"
            @click="playTrack(track)"
          />
          <div class="min-w-0 flex-1">
            <p class="truncate text-sm font-medium text-highlighted">
              {{ track.name }}
            </p>
            <p class="mt-1 truncate text-xs text-muted">
              {{ track.fileName }} ·
              {{ formatDuration(track.durationSeconds) }} ·
              {{ formatBytes(track.sizeBytes) }}
            </p>
            <p v-if="track.error" class="mt-1 truncate text-xs text-error">
              {{ track.error }}
            </p>
          </div>
          <UBadge
            :color="track.status === 'resolved' ? 'success' : 'warning'"
            variant="subtle"
            class="shrink-0"
          >
            {{ track.status }}
          </UBadge>
        </div>
        <div
          v-if="visibleTracks.length === 0"
          class="grid min-h-48 place-items-center p-8 text-sm text-muted"
        >
          No tracks match the current search.
        </div>
      </div>
      <StudioTableFooter
        v-if="manifest"
        v-model:page="page"
        v-model:page-size="pageSize"
        :total="filteredTracks.length"
        :page-size-options="[25, 50, 100]"
      />
      <div
        v-else
        class="grid min-h-64 place-items-center p-8 text-center text-sm text-muted"
      >
        No generated music manifest is available. Queue the first import.
      </div>
    </UCard>
  </div>
</template>
