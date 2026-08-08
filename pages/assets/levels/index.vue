<script setup lang="ts">
import type { LevelCatalogManifest } from '@l2/ui'
import { levelCatalogManifestUrl } from '@l2/ui'
import { computed, onBeforeUnmount } from 'vue'
import {
  assetImportsUrl,
  type AssetImportJob
} from '../../../lib/studio-content'
import { buildLevelWorldGrid } from '../../../lib/level-world-grid'

const config = useRuntimeConfig()
const jobs = ref<AssetImportJob[]>([])
const catalog = ref<LevelCatalogManifest>()
const queueing = ref(false)
const jobsError = ref<string>()
const catalogError = ref<string>()
let pollTimer: ReturnType<typeof setTimeout> | undefined

const activeJob = computed(() =>
  jobs.value.find((job) => job.status === 'queued' || job.status === 'running')
)
const worldGrid = computed(() =>
  buildLevelWorldGrid(catalog.value?.levels ?? [])
)
const worldGridStyle = computed(() => ({
  gridTemplateColumns: `repeat(${worldGrid.value.width}, minmax(4.75rem, 1fr))`,
  minWidth: `${worldGrid.value.width * 4.75}rem`
}))

async function loadCatalog() {
  try {
    catalog.value = await $fetch<LevelCatalogManifest>(
      levelCatalogManifestUrl(),
      { query: { refresh: Date.now() } }
    )
    catalogError.value = undefined
  } catch {
    catalog.value = undefined
    catalogError.value = 'The generated level catalog could not be loaded.'
  }
}

async function loadJobs(schedule = true) {
  clearTimeout(pollTimer)
  try {
    jobs.value = await $fetch<AssetImportJob[]>(
      assetImportsUrl(config.public.apiBase, 'levels'),
      { query: { limit: 20 } }
    )
    jobsError.value = undefined
    if (!activeJob.value) await loadCatalog()
  } catch {
    jobsError.value =
      'Level import jobs could not be loaded from the Studio API.'
  }
  if (schedule && activeJob.value)
    pollTimer = setTimeout(() => void loadJobs(), 1000)
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
          :disabled="Boolean(activeJob)"
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

    <UCard v-if="worldGrid.cells.length">
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

      <div class="overflow-x-auto pb-2">
        <div class="grid gap-1.5" :style="worldGridStyle">
          <template v-for="cell in worldGrid.cells" :key="cell.key">
            <UButton
              v-if="cell.level"
              color="neutral"
              :variant="cell.level.status === 'resolved' ? 'soft' : 'outline'"
              :disabled="!cell.level.manifestUrl"
              :to="
                cell.level.manifestUrl
                  ? {
                      name: 'assets-levels-name',
                      params: { name: cell.level.name }
                    }
                  : undefined
              "
              :title="cell.level.error || `Open ${cell.level.fileName}`"
              class="group min-h-20 flex-col items-stretch justify-between gap-2 rounded-lg p-2 text-left"
            >
              <span class="flex w-full items-start justify-between gap-1">
                <span class="font-mono text-sm font-semibold">
                  {{ cell.level.name }}
                </span>
                <span
                  class="mt-1 size-2 shrink-0 rounded-full"
                  :class="
                    cell.level.status === 'resolved'
                      ? 'bg-success'
                      : 'bg-warning'
                  "
                />
              </span>
              <span class="w-full text-[0.6875rem] text-muted">
                {{ cell.level.terrainCount }} terrain ·
                {{ cell.level.actorCount.toLocaleString() }} meshes ·
                {{ cell.level.waterVolumeCount }} water
              </span>
            </UButton>
            <div
              v-else
              class="min-h-20 rounded-lg border border-dashed border-default/60 bg-muted/20"
              :title="`No imported level at ${cell.key}`"
              aria-hidden="true"
            />
          </template>
        </div>
      </div>
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
