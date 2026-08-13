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
import { hasImportedMaps } from '../../../utils/map-catalog-state'
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
const reimportingMap = ref<string>()
const progressJobId = ref<string>()
const progressPreviewJobId = ref<string>()
const importDrawerOpen = ref(false)
const notifications = useStudioToasts()
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
const hasMaps = computed(() => hasImportedMaps(catalog.value))
const progressItems = computed(() => {
  const items = []
  const job = jobs.value.find(item => item.id === progressJobId.value)
  const previewJob = previewJobs.value.find(item => item.id === progressPreviewJobId.value)
  if (job) items.push(assetImportProgressItem(job, 'Maps'))
  if (previewJob) items.push(assetImportProgressItem(previewJob, 'Map previews'))
  return items
})
const mapImportMenuItems = computed(() => [[
  {
    label: 'Import maps',
    icon: 'i-lucide-play',
    onSelect: (): void => { void queueImport() }
  },
  {
    label: 'Force rebuild maps',
    icon: 'i-lucide-hammer',
    color: 'warning' as const,
    onSelect: (): void => { void queueImport(true) }
  }
]])
const previewImportMenuItems = computed(() => [[
  {
    label: 'Generate previews',
    icon: 'i-lucide-image',
    onSelect: (): void => { void queuePreviews() }
  },
  {
    label: 'Force regenerate previews',
    icon: 'i-lucide-hammer',
    color: 'warning' as const,
    onSelect: (): void => { void queuePreviews(undefined, true) }
  }
]])

function mapMenuItems(map: MapCatalogEntry) {
  return [[
    {
      label: 'Re-import map',
      icon: 'i-lucide-rotate-cw',
      onSelect: (): void => { void reimportMap(map) }
    },
    {
      label: 'Force rebuild map',
      icon: 'i-lucide-hammer',
      color: 'warning' as const,
      onSelect: (): void => { void reimportMap(map, true) }
    }
  ]]
}

async function loadCatalog() {
  try {
    catalog.value = await getAssetCatalog<MapCatalogEntry>('maps', {
      pageSize: 500
    })
  } catch {
    catalog.value = undefined
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

async function queuePreviews(map?: MapCatalogEntry, force = false) {
  queueingPreviews.value = true
  queueingPreviewName.value = map?.name
  jobsError.value = undefined
  try {
    const job = map
      ? await startAssetFileImport('mappreviews', map.sourceKey, force)
      : await startAssetImport('mappreviews', { force })
    progressPreviewJobId.value = job.id
    importDrawerOpen.value = true
    await loadJobs()
  } catch {
    const title = map
      ? force
        ? `Forced preview rebuild for ${map.name} could not be queued`
        : `Preview for ${map.name} could not be queued`
      : force
        ? 'Forced map-preview rebuild could not be queued'
        : 'Map previews could not be queued'
    notifications.error({ title, description: 'Try the action again.' })
  } finally {
    queueingPreviews.value = false
    queueingPreviewName.value = undefined
  }
}

async function queueImport(force = false) {
  queueing.value = true
  jobsError.value = undefined
  try {
    const job = await startAssetImport('maps', { force })
    progressJobId.value = job.id
    importDrawerOpen.value = true
    await loadJobs()
  } catch {
    notifications.error({
      title: force ? 'Forced map rebuild could not be queued' : 'Map import could not be queued',
      description: 'Try the action again.'
    })
  } finally {
    queueing.value = false
  }
}

async function reimportMap(map: MapCatalogEntry, force = false) {
  reimportingMap.value = map.name
  jobsError.value = undefined
  try {
    const job = await startAssetResourceImport(
      'maps', map.name, undefined, map.sourceKey, force
    )
    progressJobId.value = job.id
    importDrawerOpen.value = true
    await loadJobs()
  } catch {
    notifications.error({
      title: force
        ? `Forced map rebuild for ${map.name} could not be queued`
        : `Map re-import for ${map.name} could not be queued`,
      description: 'Try the action again.'
    })
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
        <UDropdownMenu
          v-if="hasMaps"
          :items="previewImportMenuItems"
          :content="{ align: 'end' }"
        >
          <UButton
            label="Generate previews"
            icon="i-lucide-image"
            trailing-icon="i-lucide-chevron-down"
            color="neutral"
            variant="outline"
            :loading="queueingPreviews"
            :disabled="Boolean(activePreviewJob || activeJob)"
          />
        </UDropdownMenu>
        <UButton
          label="Import jobs"
          icon="i-lucide-history"
          color="neutral"
          variant="outline"
          to="/pipeline/imports"
        />
        <UDropdownMenu :items="mapImportMenuItems" :content="{ align: 'end' }">
          <UButton
            label="Import maps"
            icon="i-lucide-play"
            trailing-icon="i-lucide-chevron-down"
            :loading="queueing"
            :disabled="Boolean(activeJob || activePreviewJob)"
          />
        </UDropdownMenu>
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="jobsError"
      color="error"
      variant="subtle"
      title="Map imports unavailable"
      :description="jobsError"
    />
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
        :preview-job-active="Boolean(activePreviewJob || queueingPreviews || activeJob || queueing)"
        :queueing-preview-name="queueingPreviewName"
        :reimporting-map-name="reimportingMap"
        @generate-preview="queuePreviews"
        @force-generate-preview="map => queuePreviews(map, true)"
        @reimport="reimportMap"
        @force-reimport="map => reimportMap(map, true)"
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
        <UDropdownMenu :items="mapMenuItems(map)" :content="{ align: 'end' }">
          <UButton icon="i-lucide-ellipsis" color="neutral" variant="ghost" size="xs" :aria-label="`Actions for ${map.name}`" :loading="reimportingMap === map.name" :disabled="Boolean(activeJob || activePreviewJob)" />
        </UDropdownMenu>
        </div>
      </div>
    </UCard>
    <UCard v-if="!hasMaps">
      <p class="py-16 text-center text-sm text-muted">
        No generated map catalog is available. Queue the first import.
      </p>
    </UCard>

    <StudioImportProgressDrawer
      v-model:open="importDrawerOpen"
      :items="progressItems"
    />
  </div>
</template>
