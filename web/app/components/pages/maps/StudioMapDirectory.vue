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
  startAssetImport
} from '../../../services/studio-api'
import { buildMapWorldGrid } from '../../../utils/map-world-grid'

const jobs = ref<AssetImportJob[]>([])
const previewJobs = ref<AssetImportJob[]>([])
const catalog = ref<AssetCatalogPage<MapCatalogEntry>>()
const previewCatalog = ref<AssetCatalogPage<MapPreviewCatalogEntry>>()
const queueing = ref(false)
const queueingPreviews = ref(false)
const queueingPreviewName = ref<string>()
const jobsError = ref<string>()
const catalogError = ref<string>()
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
  () => new Map(previewCatalog.value?.items.map((item) => [item.name, item]))
)
const worldGrid = computed(() =>
  buildMapWorldGrid(catalog.value?.items ?? [])
)

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
    jobsError.value = undefined
    if (!activeJob.value && !activePreviewJob.value) await loadCatalog()
  } catch {
    jobsError.value =
      'Map import jobs could not be loaded from the Studio API.'
  }
  if (schedule && (activeJob.value || activePreviewJob.value))
    pollTimer = setTimeout(() => void loadJobs(), 1000)
}

async function queuePreviews(mapName?: string) {
  queueingPreviews.value = true
  queueingPreviewName.value = mapName
  jobsError.value = undefined
  try {
    await startAssetImport(
      'mappreviews',
      mapName ? { mapName } : undefined
    )
    await loadJobs()
  } catch {
    jobsError.value = mapName
      ? `The preview for ${mapName} could not be queued.`
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
    await startAssetImport('maps')
    await loadJobs()
  } catch {
    jobsError.value = 'The map import could not be queued.'
  } finally {
    queueing.value = false
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
          to="/assets/jobs"
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
    <UCard v-if="activeJob" variant="subtle">
      <div class="flex items-center gap-4">
        <UIcon
          name="i-lucide-loader-circle"
          class="size-5 animate-spin text-primary"
        />
        <div class="flex-1">
          <p class="font-medium text-highlighted">
            Import {{ activeJob.status }}
          </p>
          <p class="text-xs text-muted">
            {{ activeJob.completedFileCount }} / {{ activeJob.discoveredFileCount || '…' }}
          </p>
        </div>
      </div>
    </UCard>
    <UCard v-if="activePreviewJob" variant="subtle">
      <div class="flex items-center gap-4">
        <UIcon
          name="i-lucide-loader-circle"
          class="size-5 animate-spin text-primary"
        />
        <div class="flex-1">
          <p class="font-medium text-highlighted">
            Preview generation {{ activePreviewJob.status }}
          </p>
          <p class="text-xs text-muted">
            {{ activePreviewJob.completedFileCount }} /
            {{ activePreviewJob.discoveredFileCount || '…' }}
          </p>
        </div>
      </div>
    </UCard>

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
        @generate-preview="queuePreviews"
      />
    </UCard>

    <UCard v-if="worldGrid.unpositioned.length" variant="subtle">
      <template #header>
        <h2 class="font-semibold text-highlighted">Other maps</h2>
      </template>
      <div class="flex flex-wrap gap-2">
        <UButton
          v-for="map in worldGrid.unpositioned"
          :key="map.name"
          :label="map.name"
          color="neutral"
          variant="outline"
          :disabled="!map.manifestUrl"
          :to="
            map.manifestUrl
              ? { name: 'assets-maps-name', params: { name: map.name } }
              : undefined
          "
        />
      </div>
    </UCard>
    <UCard v-if="!worldGrid.cells.length && !catalogError">
      <p class="py-16 text-center text-sm text-muted">
        No generated map manifest is available. Queue the first import.
      </p>
    </UCard>
  </div>
</template>
