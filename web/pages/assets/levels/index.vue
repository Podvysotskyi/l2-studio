<script setup lang="ts">
import type {
  AssetCatalogPage,
  LevelCatalogEntry,
  LevelPreviewCatalogEntry
} from '@podvysotskyi/l2-ui'
import { computed, onBeforeUnmount } from 'vue'
import {
  assetCatalogUrl,
  assetImportsUrl,
  type AssetImportJob
} from '../../../lib/studio-content'
import { buildLevelWorldGrid } from '../../../lib/level-world-grid'

const config = useRuntimeConfig()
const jobs = ref<AssetImportJob[]>([])
const previewJobs = ref<AssetImportJob[]>([])
const catalog = ref<AssetCatalogPage<LevelCatalogEntry>>()
const previewCatalog = ref<AssetCatalogPage<LevelPreviewCatalogEntry>>()
const queueing = ref(false)
const queueingPreviews = ref(false)
const queueingPreviewName = ref<string>()
const jobsError = ref<string>()
const catalogError = ref<string>()
let pollTimer: ReturnType<typeof setTimeout> | undefined

const activeJob = computed(() =>
  jobs.value.find((job) => job.status === 'queued' || job.status === 'running')
)
const activePreviewJob = computed(() =>
  previewJobs.value.find(
    (job) => job.status === 'queued' || job.status === 'running'
  )
)
const previews = computed(
  () => new Map(previewCatalog.value?.items.map((item) => [item.name, item]))
)
const worldGrid = computed(() =>
  buildLevelWorldGrid(catalog.value?.items ?? [])
)

async function loadCatalog() {
  try {
    catalog.value = await $fetch(
      assetCatalogUrl(config.public.apiBase, 'levels', { pageSize: 500 })
    )
    catalogError.value = undefined
  } catch {
    catalog.value = undefined
    catalogError.value = 'The generated level catalog could not be loaded.'
  }
  try {
    previewCatalog.value = await $fetch(
      assetCatalogUrl(config.public.apiBase, 'levelpreviews', { pageSize: 500 })
    )
  } catch {
    previewCatalog.value = undefined
  }
}

async function loadJobs(schedule = true) {
  clearTimeout(pollTimer)
  try {
    jobs.value = await $fetch<AssetImportJob[]>(
      assetImportsUrl(config.public.apiBase, 'levels'),
      { query: { limit: 20 } }
    )
    previewJobs.value = await $fetch<AssetImportJob[]>(
      assetImportsUrl(config.public.apiBase, 'levelpreviews'),
      { query: { limit: 20 } }
    )
    jobsError.value = undefined
    if (!activeJob.value && !activePreviewJob.value) await loadCatalog()
  } catch {
    jobsError.value =
      'Level import jobs could not be loaded from the Studio API.'
  }
  if (schedule && (activeJob.value || activePreviewJob.value))
    pollTimer = setTimeout(() => void loadJobs(), 1000)
}

async function queuePreviews(levelName?: string) {
  queueingPreviews.value = true
  queueingPreviewName.value = levelName
  jobsError.value = undefined
  try {
    await $fetch(assetImportsUrl(config.public.apiBase, 'levelpreviews'), {
      method: 'POST',
      query: levelName ? { levelName } : undefined
    })
    await loadJobs()
  } catch {
    jobsError.value = levelName
      ? `The preview for ${levelName} could not be queued.`
      : 'The level previews could not be queued.'
  } finally {
    queueingPreviews.value = false
    queueingPreviewName.value = undefined
  }
}

async function queueImport() {
  queueing.value = true
  jobsError.value = undefined
  try {
    await $fetch(assetImportsUrl(config.public.apiBase, 'levels'), {
      method: 'POST'
    })
    await loadJobs()
  } catch {
    jobsError.value = 'The level import could not be queued.'
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
      title="Levels"
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
          label="Import levels"
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
      title="Level imports unavailable"
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
            {{ activeJob.processedCount }} / {{ activeJob.totalCount || '…' }}
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
            {{ activePreviewJob.processedCount }} /
            {{ activePreviewJob.totalCount || '…' }}
          </p>
        </div>
      </div>
    </UCard>

    <UAlert
      v-if="catalogError"
      color="error"
      variant="subtle"
      title="Level catalog unavailable"
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

      <StudioLevelWorldMap
        :grid="worldGrid"
        :previews="previews"
        :preview-job-active="Boolean(activePreviewJob || queueingPreviews)"
        :queueing-preview-name="queueingPreviewName"
        @generate-preview="queuePreviews"
      />
    </UCard>

    <UCard v-if="worldGrid.unpositioned.length" variant="subtle">
      <template #header>
        <h2 class="font-semibold text-highlighted">Other levels</h2>
      </template>
      <div class="flex flex-wrap gap-2">
        <UButton
          v-for="level in worldGrid.unpositioned"
          :key="level.name"
          :label="level.name"
          color="neutral"
          variant="outline"
          :disabled="!level.manifestUrl"
          :to="
            level.manifestUrl
              ? { name: 'assets-levels-name', params: { name: level.name } }
              : undefined
          "
        />
      </div>
    </UCard>
    <UCard v-if="!worldGrid.cells.length && !catalogError">
      <p class="py-16 text-center text-sm text-muted">
        No generated level manifest is available. Queue the first import.
      </p>
    </UCard>
  </div>
</template>
