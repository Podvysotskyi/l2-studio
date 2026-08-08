<script setup lang="ts">
import type { LevelCatalogManifest } from '@l2/ui'
import { levelCatalogManifestUrl } from '@l2/ui'
import { computed, onBeforeUnmount } from 'vue'
import {
  assetImportsUrl,
  type AssetImportJob
} from '../../../lib/studio-content'

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
      description="Browse imported Interlude maps and open their reconstructed scene manifests."
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

    <div v-if="catalog?.levels.length" class="grid gap-4 lg:grid-cols-2">
      <UCard v-for="level in catalog.levels" :key="level.name">
        <div class="flex items-start gap-4">
          <span
            class="grid size-12 shrink-0 place-items-center rounded-xl bg-primary/10 text-primary"
          >
            <UIcon name="i-lucide-map" class="size-6" />
          </span>
          <div class="min-w-0 flex-1">
            <div class="flex flex-wrap items-center gap-2">
              <h2 class="font-semibold text-highlighted">{{ level.name }}</h2>
              <UBadge
                :color="level.status === 'resolved' ? 'success' : 'warning'"
                variant="subtle"
                size="sm"
              >
                {{ level.status }}
              </UBadge>
            </div>
            <p class="mt-1 text-xs text-muted">{{ level.fileName }}</p>
            <p class="mt-3 text-sm text-muted">
              {{ level.terrainCount }} terrain ·
              {{ level.actorCount.toLocaleString() }} placed meshes
            </p>
            <p v-if="level.error" class="mt-2 text-xs text-error">
              {{ level.error }}
            </p>
          </div>
          <UButton
            label="Open map"
            icon="i-lucide-arrow-right"
            trailing
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
    </div>
    <UCard v-else-if="!catalogError">
      <p class="py-16 text-center text-sm text-muted">
        No generated level manifest is available. Queue the first import.
      </p>
    </UCard>
  </div>
</template>
