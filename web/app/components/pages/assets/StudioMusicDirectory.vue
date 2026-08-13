<script setup lang="ts">
import type { AssetCatalogPage, MusicManifestEntry } from '~/types/studio'
import type { AssetImportJob } from '../../../types/models/asset-import-job'
import { computed, nextTick, onBeforeUnmount, watch } from 'vue'
import {
  getAssetCatalog,
  getAssetImportJobs,
  startAssetFileImport,
  startAssetImport
} from '../../../services/studio-api'
import { assetImportProgressItem } from '../../../utils/import-progress'

const jobs = ref<AssetImportJob[]>([])
const catalog = ref<AssetCatalogPage<MusicManifestEntry>>()
const query = ref('')
const page = ref(1)
const pageSize = ref(50)
const queueing = ref(false)
const reimporting = ref(false)
const error = ref<string>()
const selectedTrack = ref<MusicManifestEntry>()
const audioPlayer = ref<HTMLAudioElement>()
const progressJobId = ref<string>()
const importDrawerOpen = ref(false)
const notifications = useStudioToasts()
let pollTimer: ReturnType<typeof setTimeout> | undefined

const activeJob = computed(() =>
  jobs.value.find((job) =>
    ['queued', 'discovering', 'running'].includes(job.status)
  )
)
const filteredTracks = computed(() => catalog.value?.items ?? [])
const visibleTracks = computed(() => filteredTracks.value)
const resolvedCount = computed(() => catalog.value?.summary.resolved ?? 0)
const progressItems = computed(() => {
  const job = jobs.value.find(item => item.id === progressJobId.value)
  return job ? [assetImportProgressItem(job, 'Music')] : []
})
const importMenuItems = computed(() => [[
  {
    label: 'Import music',
    icon: 'i-lucide-play',
    onSelect: (): void => { void queueImport() }
  },
  {
    label: 'Force rebuild music',
    icon: 'i-lucide-hammer',
    color: 'warning' as const,
    onSelect: (): void => { void queueImport(true) }
  }
]])
const selectedTrackMenuItems = computed(() => selectedTrack.value ? [[
  {
    label: 'Re-import track',
    icon: 'i-lucide-rotate-cw',
    onSelect: (): void => { void reimportTrack() }
  },
  {
    label: 'Force rebuild track',
    icon: 'i-lucide-hammer',
    color: 'warning' as const,
    onSelect: (): void => { void reimportTrack(true) }
  }
]] : [])

watch([query, pageSize], () => {
  page.value = 1
  void loadCatalog()
})
watch(page, () => void loadCatalog())

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

async function loadCatalog() {
  try {
    const nextCatalog = await getAssetCatalog<MusicManifestEntry>('music', {
        query: query.value,
        page: page.value,
        pageSize: pageSize.value
      })
    catalog.value = nextCatalog
    if (
      !selectedTrack.value ||
      !nextCatalog.items.some(
        (track) => track.fileName === selectedTrack.value?.fileName
      )
    ) {
      selectedTrack.value = nextCatalog.items.find((track) => track.url)
    }
  } catch {
    catalog.value = undefined
    selectedTrack.value = undefined
  }
}

async function loadJobs(schedule = true) {
  clearTimeout(pollTimer)
  try {
    jobs.value = await getAssetImportJobs('music')
    if (activeJob.value && activeJob.value.id !== progressJobId.value) {
      progressJobId.value = activeJob.value.id
      importDrawerOpen.value = true
    }
    error.value = undefined
    if (!activeJob.value) await loadCatalog()
  } catch {
    error.value = 'Music import jobs could not be loaded from the Studio API.'
  }

  if (schedule && activeJob.value) {
    pollTimer = setTimeout(() => void loadJobs(), 1000)
  }
}

async function queueImport(force = false) {
  queueing.value = true
  error.value = undefined
  try {
    const job = await startAssetImport('music', { force })
    progressJobId.value = job.id
    importDrawerOpen.value = true
    await loadJobs()
  } catch {
    notifications.error({
      title: 'Music import could not be queued',
      description: 'Another music import may already be active.'
    })
  } finally {
    queueing.value = false
  }
}

async function reimportTrack(force = false) {
  if (!selectedTrack.value) return
  reimporting.value = true
  error.value = undefined
  try {
    const job = await startAssetFileImport('music', selectedTrack.value.sourceKey, force)
    progressJobId.value = job.id
    importDrawerOpen.value = true
    await loadJobs()
  } catch {
    notifications.error({
      title: force
        ? 'Forced music track rebuild could not be queued'
        : 'Music track re-import could not be queued',
      description: 'Try the action again.'
    })
  } finally {
    reimporting.value = false
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
          to="/pipeline/imports"
        />
        <UDropdownMenu :items="importMenuItems" :content="{ align: 'end' }">
          <UButton
            label="Import music"
            icon="i-lucide-play"
            trailing-icon="i-lucide-chevron-down"
            :loading="queueing"
            :disabled="Boolean(activeJob)"
          />
        </UDropdownMenu>
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
        <UDropdownMenu :items="selectedTrackMenuItems" :content="{ align: 'end' }">
          <UButton
            label="Track actions"
            icon="i-lucide-ellipsis"
            trailing-icon="i-lucide-chevron-down"
            size="xs"
            color="neutral"
            variant="outline"
            :loading="reimporting"
            :disabled="Boolean(activeJob)"
          />
        </UDropdownMenu>
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
              {{ catalog?.summary.skipped ?? 0 }} skipped
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
        v-if="catalog"
        class="h-[clamp(32rem,68vh,52rem)] divide-y divide-default overflow-y-auto lg:h-[clamp(40rem,calc(100dvh-20rem),64rem)]"
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
        v-if="catalog"
        v-model:page="page"
        v-model:page-size="pageSize"
        :total="catalog.total"
        :page-size-options="[25, 50, 100]"
      />
      <div
        v-else
        class="grid min-h-64 place-items-center p-8 text-center text-sm text-muted"
      >
        No imported music catalog is available. Queue the first import.
      </div>
    </UCard>

    <StudioImportProgressDrawer
      v-model:open="importDrawerOpen"
      :items="progressItems"
    />
  </div>
</template>
