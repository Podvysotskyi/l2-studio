<script setup lang="ts">
import type { AssetImportKind } from '~/types/studio'
import { storeToRefs } from 'pinia'
import { computed, onBeforeUnmount, ref } from 'vue'
import {
  getStaleAssetSources,
  rebuildStaleAssetSources,
  startAssetFileImport
} from '../../../services/studio-api'
import { useAssetImportsStore } from '../../../stores/asset-imports'
import type { AssetImportJob, StaleAssetSource } from '../../../types/models/asset-import-job'
import {
  assetImportKindLabel,
  assetImportKindOptions,
  assetImportKinds
} from '../../../utils/asset-import-kinds'
import { assetImportProgressItem } from '../../../utils/import-progress'

const importStore = useAssetImportsStore()
const { jobs: jobsByKind, loading: jobsLoading, error: jobsError } = storeToRefs(importStore)
const kindFilter = ref<'all' | AssetImportKind>('all')
const staleByKind = ref<Partial<Record<AssetImportKind, StaleAssetSource[]>>>({})
const staleLoading = ref(false)
const staleError = ref<string>()
const rebuildingStale = ref(false)
const reimporting = ref<string>()
const progressJobIds = ref<string[]>([])
const importDrawerOpen = ref(false)
const notifications = useStudioToasts()
let pollTimer: ReturnType<typeof setTimeout> | undefined

const jobs = computed(() =>
  assetImportKinds.flatMap((kind) => jobsByKind.value[kind] ?? [])
)
const hasActiveJob = computed(() => jobs.value.some((job) => isActive(job.status)))
const progressItems = computed(() => progressJobIds.value.flatMap((id) => {
  const job = jobs.value.find(candidate => candidate.id === id)
  return job ? [assetImportProgressItem(job, assetImportKindLabel(job.kind))] : []
}))
const visibleStaleSources = computed(() => assetImportKinds
  .filter((kind) => kindFilter.value === 'all' || kind === kindFilter.value)
  .flatMap((kind) => (staleByKind.value[kind] ?? []).map((source) => ({ kind, source }))))

function isActive(status: AssetImportJob['status']) {
  return status === 'queued' || status === 'discovering' || status === 'running'
}

async function loadStaleSources() {
  staleLoading.value = true
  staleError.value = undefined
  try {
    const responses = await Promise.all(
      assetImportKinds.map((kind) => getStaleAssetSources(kind))
    )
    staleByKind.value = Object.fromEntries(
      assetImportKinds.map((kind, index) => [kind, responses[index] ?? []])
    )
  } catch {
    staleError.value = 'Stale resources could not be loaded.'
  } finally {
    staleLoading.value = false
  }
}

async function loadJobs(schedule = true, refreshStaleWhenSettled = true) {
  clearTimeout(pollTimer)
  await importStore.load(assetImportKinds).catch(() => undefined)
  const activeJobs = jobs.value.filter(job => isActive(job.status))
  const newActiveJobs = activeJobs.filter(job => !progressJobIds.value.includes(job.id))
  if (newActiveJobs.length) {
    progressJobIds.value = [...progressJobIds.value, ...newActiveJobs.map(job => job.id)]
    importDrawerOpen.value = true
  }
  if (schedule && activeJobs.length) {
    pollTimer = setTimeout(() => void loadJobs(), 1000)
  } else if (!activeJobs.length && refreshStaleWhenSettled) {
    await loadStaleSources()
  }
}

async function refreshWorkspace() {
  await Promise.all([loadJobs(true, false), loadStaleSources()])
}

function trackJobs(queuedJobs: AssetImportJob[]) {
  progressJobIds.value = [
    ...progressJobIds.value,
    ...queuedJobs.map(job => job.id).filter(id => !progressJobIds.value.includes(id))
  ]
  importDrawerOpen.value = true
}

async function rebuildAllStale() {
  rebuildingStale.value = true
  try {
    const kinds = assetImportKinds.filter((kind) =>
      (kindFilter.value === 'all' || kind === kindFilter.value)
      && staleByKind.value[kind]?.length
    )
    const results = await Promise.allSettled(kinds.map(kind => rebuildStaleAssetSources(kind)))
    const queuedJobs = results.flatMap(result => result.status === 'fulfilled' ? [result.value] : [])
    const failureCount = results.length - queuedJobs.length
    if (queuedJobs.length) {
      trackJobs(queuedJobs)
      await loadJobs()
    }
    if (failureCount) throw new Error(`${failureCount} stale rebuilds could not be queued.`)
  } catch {
    notifications.error({
      title: 'One or more stale rebuilds could not be queued',
      description: 'Try the action again.'
    })
  } finally {
    rebuildingStale.value = false
  }
}

async function rebuildStaleSource(kind: AssetImportKind, source: StaleAssetSource) {
  reimporting.value = `${kind}:${source.sourceKey}`
  try {
    const job = await startAssetFileImport(kind, source.sourceKey)
    trackJobs([job])
    await loadJobs()
  } catch {
    notifications.error({
      title: `Stale rebuild for ${source.sourceKey} could not be queued`,
      description: 'Try the action again.'
    })
  } finally {
    reimporting.value = undefined
  }
}

onMounted(() => void refreshWorkspace())
onBeforeUnmount(() => clearTimeout(pollTimer))
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Monitoring"
      title="Stale resources"
      description="Review published resources whose source content has changed and queue rebuilds when ready."
      icon="i-lucide-triangle-alert"
    >
      <template #actions>
        <UButton
          icon="i-lucide-refresh-cw"
          label="Refresh"
          color="neutral"
          variant="outline"
          :loading="staleLoading || jobsLoading"
          @click="refreshWorkspace"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="staleError"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="Stale resources unavailable"
      :description="staleError"
    />

    <UAlert
      v-if="jobsError"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="Import activity unavailable"
      :description="jobsError"
    />

    <UCard>
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 class="text-sm font-semibold text-highlighted">Resources requiring rebuild</h2>
          <p class="mt-1 text-xs text-muted">
            Published output remains available until you explicitly rebuild it.
          </p>
        </div>
        <div class="flex flex-col gap-1 sm:items-end">
          <label for="stale-resource-collection-filter" class="text-xs font-medium text-muted">Collection</label>
          <USelect
            id="stale-resource-collection-filter"
            v-model="kindFilter"
            :items="assetImportKindOptions"
            aria-label="Filter stale resources by collection"
            class="w-full sm:w-52"
          />
        </div>
      </div>
    </UCard>

    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <template #header>
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="text-sm font-semibold text-highlighted">Stale resources</h2>
            <p class="text-xs text-muted">
              {{ visibleStaleSources.length }} resource{{ visibleStaleSources.length === 1 ? '' : 's' }} require rebuild
            </p>
          </div>
          <UButton
            v-if="visibleStaleSources.length"
            icon="i-lucide-refresh-ccw-dot"
            :label="`Rebuild stale (${visibleStaleSources.length})`"
            color="warning"
            variant="soft"
            :loading="rebuildingStale"
            :disabled="hasActiveJob"
            @click="rebuildAllStale"
          />
        </div>
      </template>

      <div v-if="visibleStaleSources.length" class="divide-y divide-default">
        <div v-for="entry in visibleStaleSources" :key="`${entry.kind}:${entry.source.sourceKey}`" class="flex flex-wrap items-center justify-between gap-3 p-4">
          <div class="min-w-0">
            <div class="flex items-center gap-2">
              <UBadge color="warning" variant="subtle">Stale</UBadge>
              <UBadge color="neutral" variant="outline">{{ assetImportKindLabel(entry.kind) }}</UBadge>
              <span class="truncate text-sm font-medium text-highlighted">{{ entry.source.sourceKey }}</span>
            </div>
            <p class="mt-2 text-xs text-muted">{{ entry.source.reasons.join(' · ') }}</p>
            <p v-if="entry.source.resourceNames.length" class="mt-1 truncate text-xs text-dimmed">
              {{ entry.source.resourceNames.join(', ') }}
            </p>
          </div>
          <UButton
            label="Rebuild"
            icon="i-lucide-refresh-cw"
            size="xs"
            color="warning"
            variant="soft"
            :loading="reimporting === `${entry.kind}:${entry.source.sourceKey}`"
            @click="rebuildStaleSource(entry.kind, entry.source)"
          />
        </div>
      </div>
      <div v-else class="grid min-h-64 place-items-center p-8 text-center">
        <div>
          <UIcon name="i-lucide-circle-check" class="mx-auto size-8 text-success" />
          <p class="mt-3 text-sm font-medium text-highlighted">
            {{ staleLoading ? 'Checking for stale resources…' : 'Everything is up to date' }}
          </p>
          <p v-if="!staleLoading" class="mt-1 text-xs text-muted">
            No resources in this collection need to be rebuilt.
          </p>
        </div>
      </div>
    </UCard>
    <StudioImportProgressDrawer v-model:open="importDrawerOpen" :items="progressItems" />
  </div>
</template>
