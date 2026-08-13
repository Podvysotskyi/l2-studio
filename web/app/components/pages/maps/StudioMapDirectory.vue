<script setup lang="ts">
import type {
  AssetCatalogPage,
  MapCatalogEntry,
  MapPreviewCatalogEntry
} from '~/types/studio'
import type { AssetImportJob } from '../../../types/models/asset-import-job'
import { computed, onBeforeUnmount } from 'vue'
import {
  getAssetCatalog,
  getAssetImportJobs,
  startAssetFileImport,
  startAssetImport,
  startAssetResourceImport
} from '../../../services/studio-api'
import { buildMapWorldGrid } from '../../../utils/map-world-grid'
import { assetImportProgressItem } from '../../../utils/import-progress'

const jobs = ref<AssetImportJob[]>([])
const previewJobs = ref<AssetImportJob[]>([])
const catalog = ref<AssetCatalogPage<MapCatalogEntry>>()
const previewCatalog = ref<AssetCatalogPage<MapPreviewCatalogEntry>>()
const queueing = ref(false)
const queueingPreviews = ref(false)
const queueingPreviewName = ref<string>()
const jobsError = ref<string>()
const catalogError = ref<string>()
const reimportingMap = ref<string>()
const progressJobId = ref<string>()
const progressPreviewJobId = ref<string>()
const importDrawerOpen = ref(false)
let pollTimer: ReturnType<typeof setTimeout> | undefined

const activeJob = computed(() =>
  jobs.value.find((job) =>
    ['queued', 'discovering', 'running'].includes(job.status)
  )
)
const activePreviewJob = computed(() =>
  previewJobs.value.find(
    (job) => ['queued', 'discovering', 'running'].includes(job.status)
  )
)
const previews = computed(
  () => new Map(previewCatalog.value?.items.map((item) => [item.sourceKey, item]))
)
const worldGrid = computed(() =>
  buildMapWorldGrid(catalog.value?.items ?? [])
)
const progressItems = computed(() => {
  const items = []
  const job = jobs.value.find(item => item.id === progressJobId.value)
  const previewJob = previewJobs.value.find(item => item.id === progressPreviewJobId.value)
  if (job) items.push(assetImportProgressItem(job, 'Maps'))
  if (previewJob) items.push(assetImportProgressItem(previewJob, 'Map previews'))
  return items
})

async function loadCatalog() {
  try {
    catalog.value = await getAssetCatalog<MapCatalogEntry>('maps', {
      pageSize: 500
    })
    catalogError.value = undefined
  } catch {
    catalog.value = undefined
    catalogError.value = 'The generated map catalog could not be loaded.'
  }
  try {
    previewCatalog.value =
      await getAssetCatalog<MapPreviewCatalogEntry>('mappreviews', {
        pageSize: 500
      })
  } catch {
    previewCatalog.value = undefined
  }
}

async function loadJobs(schedule = true) {
  clearTimeout(pollTimer)
  try {
    jobs.value = await getAssetImportJobs('maps')
    previewJobs.value = await getAssetImportJobs('mappreviews')
    if (activeJob.value && activeJob.value.id !== progressJobId.value) {
      progressJobId.value = activeJob.value.id
      importDrawerOpen.value = true
    }
    if (activePreviewJob.value && activePreviewJob.value.id !== progressPreviewJobId.value) {
      progressPreviewJobId.value = activePreviewJob.value.id
      importDrawerOpen.value = true
    }
    jobsError.value = undefined
    if (!activeJob.value && !activePreviewJob.value) await loadCatalog()
  } catch {
    jobsError.value =
      'Map import jobs could not be loaded from the Studio API.'
  }
  if (schedule && (activeJob.value || activePreviewJob.value))
    pollTimer = setTimeout(() => void loadJobs(), 1000)
}

async function queuePreviews(map?: MapCatalogEntry) {
  queueingPreviews.value = true
  queueingPreviewName.value = map?.name
  jobsError.value = undefined
  try {
    const job = map
      ? await startAssetFileImport('mappreviews', map.sourceKey)
      : await startAssetImport('mappreviews')
    progressPreviewJobId.value = job.id
    importDrawerOpen.value = true
    await loadJobs()
  } catch {
    jobsError.value = map
      ? `The preview for ${map.name} could not be queued.`
      : 'The map previews could not be queued.'
  } finally {
    queueingPreviews.value = false
    queueingPreviewName.value = undefined
  }
}

async function queueImport() {
  queueing.value = true
  jobsError.value = undefined
  try {
    const job = await startAssetImport('maps')
    progressJobId.value = job.id
    importDrawerOpen.value = true
    await loadJobs()
  } catch {
    jobsError.value = 'The map import could not be queued.'
  } finally {
    queueing.value = false
  }
}

async function reimportMap(map: MapCatalogEntry) {
  reimportingMap.value = map.name
  jobsError.value = undefined
  try {
    const job = await startAssetResourceImport('maps', map.name, undefined, map.sourceKey)
    progressJobId.value = job.id
    importDrawerOpen.value = true
    await loadJobs()
  } catch {
    jobsError.value = `The map re-import for ${map.name} could not be queued.`
  } finally {
    reimportingMap.value = undefined
  }
}

onMounted(() => {
  void loadCatalog()
  void loadJobs()
})
onBeforeUnmount(() => clearTimeout(pollTimer))
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Asset pipeline"
      title="Maps"
      description="Explore imported Interlude maps in their world-grid positions and open their reconstructed scenes."
      icon="i-lucide-map"
    >
      <template #actions>
        <UButton
          label="Generate previews"
          icon="i-lucide-image"
          color="neutral"
          variant="outline"
          :loading="queueingPreviews"
          :disabled="Boolean(activePreviewJob || activeJob)"
          @click="queuePreviews()"
        />
        <UButton
          label="Import jobs"
          icon="i-lucide-history"
          color="neutral"
          variant="outline"
          to="/pipeline/imports"
        />
        <UButton
          label="Import maps"
          icon="i-lucide-play"
          :loading="queueing"
          :disabled="Boolean(activeJob || activePreviewJob)"
          @click="queueImport"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="jobsError"
      color="error"
      variant="subtle"
      title="Map imports unavailable"
      :description="jobsError"
    />
    <UAlert
      v-if="catalogError"
      color="error"
      variant="subtle"
      title="Map catalog unavailable"
      :description="catalogError"
    >
      <template #actions>
        <UButton color="error" variant="soft" size="sm" @click="loadCatalog">
          Try again
        </UButton>
      </template>
    </UAlert>

    <UCard v-if="worldGrid.cells.length" :ui="{ body: 'p-0 sm:p-0' }">
      <template #header>
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="font-semibold text-highlighted">World map</h2>
            <p class="mt-1 text-xs text-muted">
              {{ worldGrid.minX }}_{{ worldGrid.minY }} to
              {{ worldGrid.maxX }}_{{ worldGrid.maxY }} · north is up
            </p>
          </div>
          <div class="flex items-center gap-4 text-xs text-muted">
            <span class="flex items-center gap-1.5">
              <span class="size-2 rounded-full bg-success" /> Resolved
            </span>
            <span class="flex items-center gap-1.5">
              <span class="size-2 rounded-full bg-warning" /> Skipped
            </span>
          </div>
        </div>
      </template>

      <StudioMapWorldMap
        :grid="worldGrid"
        :previews="previews"
        :preview-job-active="Boolean(activePreviewJob || queueingPreviews)"
        :queueing-preview-name="queueingPreviewName"
        :reimporting-map-name="reimportingMap"
        @generate-preview="queuePreviews"
        @reimport="reimportMap"
      />
    </UCard>

    <UCard v-if="worldGrid.unpositioned.length" variant="subtle">
      <template #header>
        <h2 class="font-semibold text-highlighted">Other maps</h2>
      </template>
      <div class="flex flex-wrap gap-2">
        <div v-for="map in worldGrid.unpositioned" :key="map.sourceKey" class="flex items-center gap-1">
        <UButton
          :label="`${map.name} · ${map.sourceKey}`"
          color="neutral"
          variant="outline"
          :disabled="!map.manifestUrl"
          :to="
            map.manifestUrl
              ? { name: 'library-maps-name', params: { name: map.name }, query: { source: map.sourceKey } }
              : undefined
          "
        />
        <UButton icon="i-lucide-rotate-cw" color="neutral" variant="ghost" size="xs" aria-label="Re-import map" :loading="reimportingMap === map.name" @click="reimportMap(map)" />
        </div>
      </div>
    </UCard>
    <UCard v-if="!worldGrid.cells.length && !catalogError">
      <p class="py-16 text-center text-sm text-muted">
        No generated map manifest is available. Queue the first import.
      </p>
    </UCard>

    <StudioImportProgressDrawer
      v-model:open="importDrawerOpen"
      :items="progressItems"
    />
  </div>
</template>
