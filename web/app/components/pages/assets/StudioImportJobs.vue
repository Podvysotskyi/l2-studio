<script setup lang="ts">
import type { AssetImportKind } from '~/types/studio'
import { storeToRefs } from 'pinia'
import { computed, onBeforeUnmount, ref } from 'vue'
import {
  getAssetImportDiagnostics,
  getStaleAssetSources,
  getAssetImportWorkItems,
  rebuildStaleAssetSources,
  startAssetImport,
  startAssetFileImport
} from '../../../services/studio-api'
import { useAssetImportsStore } from '../../../stores/asset-imports'
import type {
  AssetImportDiagnostic,
  AssetImportJob,
  AssetImportWorkItem,
  StaleAssetSource
} from '../../../types/models/asset-import-job'

const importStore = useAssetImportsStore()
const { jobs: jobsByKind, loading, error } = storeToRefs(importStore)
const activeView = ref<'runs' | 'stale'>('runs')
const initialViewResolved = ref(false)
const kindFilter = ref<'all' | AssetImportKind>('all')
const expandedRunId = ref<string>()
const workItems = ref<AssetImportWorkItem[]>([])
const workItemTotal = ref(0)
const workItemPage = ref(1)
const workItemPageSize = ref(25)
const selectedWorkItemId = ref<string>()
const selectedDiagnostics = ref<AssetImportDiagnostic[]>([])
const selectedDiagnosticTotal = ref(0)
const selectedDiagnosticPage = ref(1)
const selectedDiagnosticLoading = ref(false)
const runDiagnostics = ref<AssetImportDiagnostic[]>([])
const detailLoading = ref(false)
const detailError = ref<string>()
const detailQuery = ref('')
const workItemStatusFilter = ref<'all' | AssetImportWorkItem['status']>('all')
const diagnosticSeverityFilter = ref<'all' | 'warning' | 'error'>('all')
const reimporting = ref<string>()
const staleByKind = ref<Partial<Record<AssetImportKind, StaleAssetSource[]>>>({})
const staleLoading = ref(false)
const staleError = ref<string>()
const actionError = ref<string>()
const rebuildingStale = ref(false)
const forcingKind = ref(false)
let pollTimer: ReturnType<typeof setTimeout> | undefined

const importKinds: AssetImportKind[] = [
  'textures',
  'music',
  'sounds',
  'staticmeshes',
  'maps',
  'mappreviews',
  'scenes'
]
const jobs = computed(() =>
  importKinds
    .flatMap((kind) => jobsByKind.value[kind] ?? [])
    .sort(
      (left, right) =>
        new Date(right.requestedAt).getTime() -
        new Date(left.requestedAt).getTime()
    )
)
const visibleJobs = computed(() =>
  jobs.value.filter(
    (job) => kindFilter.value === 'all' || job.kind === kindFilter.value
  )
)
const hasActiveJob = computed(() =>
  jobs.value.some((job) => isActive(job.status))
)
const staleCount = computed(() => Object.values(staleByKind.value)
  .reduce((total, sources) => total + (sources?.length ?? 0), 0))
const visibleStaleSources = computed(() => importKinds
  .filter((kind) => kindFilter.value === 'all' || kind === kindFilter.value)
  .flatMap((kind) => (staleByKind.value[kind] ?? []).map((source) => ({ kind, source }))))
const expandedRun = computed(() =>
  jobs.value.find((job) => job.id === expandedRunId.value)
)
const selectedWorkItem = computed(() =>
  workItems.value.find((item) => item.id === selectedWorkItemId.value)
)
const runDiagnosticHasError = computed(() =>
  runDiagnostics.value.some((diagnostic) => diagnostic.severity === 'error')
)
const kindOptions = [
  { label: 'All collections', value: 'all' },
  { label: 'Textures', value: 'textures' },
  { label: 'Music', value: 'music' },
  { label: 'Sounds', value: 'sounds' },
  { label: 'Static meshes', value: 'staticmeshes' },
  { label: 'Maps', value: 'maps' },
  { label: 'Map previews', value: 'mappreviews' },
  { label: 'Scenes', value: 'scenes' }
]
const workItemStatusOptions = [
  { label: 'All file statuses', value: 'all' },
  { label: 'Queued', value: 'queued' },
  { label: 'Running', value: 'running' },
  { label: 'Succeeded', value: 'succeeded' },
  { label: 'Warnings', value: 'succeeded_with_warnings' },
  { label: 'Reused', value: 'reused' },
  { label: 'Failed', value: 'failed' }
]
const diagnosticSeverityOptions = [
  { label: 'All diagnostics', value: 'all' },
  { label: 'Has warnings', value: 'warning' },
  { label: 'Has errors', value: 'error' }
]

function isActive(status: AssetImportJob['status']) {
  return status === 'queued' || status === 'discovering' || status === 'running'
}

function statusColor(status: AssetImportJob['status']) {
  if (status === 'succeeded') return 'success'
  if (status === 'succeeded_with_warnings') return 'warning'
  if (status === 'failed') return 'error'
  return 'info'
}

function kindLabel(kind: AssetImportKind) {
  if (kind === 'textures') return 'Textures'
  if (kind === 'music') return 'Music'
  if (kind === 'sounds') return 'Sounds'
  if (kind === 'staticmeshes') return 'Static meshes'
  if (kind === 'maps') return 'Maps'
  return kind === 'mappreviews' ? 'Map previews' : 'Scenes'
}

function formatDate(value: string | null) {
  return value ? new Date(value).toLocaleString() : '—'
}

async function loadJobs(schedule = true) {
  clearTimeout(pollTimer)
  const [, staleLoaded] = await Promise.all([
    importStore.load(importKinds).catch(() => undefined),
    loadStaleSources()
  ])
  if (!initialViewResolved.value) {
    activeView.value = staleLoaded && staleCount.value > 0 ? 'stale' : 'runs'
    initialViewResolved.value = true
  }
  if (expandedRun.value && isActive(expandedRun.value.status)) {
    await loadDetails(expandedRun.value)
  }
  if (schedule && hasActiveJob.value) {
    pollTimer = setTimeout(() => void loadJobs(), 1000)
  }
}

async function refreshWorkspace() {
  actionError.value = undefined
  await loadJobs()
}

function selectView(view: 'runs' | 'stale', focus = false) {
  activeView.value = view
  if (focus) {
    document.getElementById(view === 'runs' ? 'import-runs-tab' : 'stale-resources-tab')?.focus()
  }
}

async function loadStaleSources() {
  staleLoading.value = true
  staleError.value = undefined
  try {
    const responses = await Promise.all(
      importKinds.map((kind) => getStaleAssetSources(kind))
    )
    staleByKind.value = Object.fromEntries(
      importKinds.map((kind, index) => [kind, responses[index] ?? []])
    )
    return true
  } catch {
    staleError.value = 'Stale resources could not be loaded.'
    return false
  } finally {
    staleLoading.value = false
  }
}

async function toggleDetails(run: AssetImportJob) {
  if (expandedRunId.value === run.id) {
    expandedRunId.value = undefined
    return
  }
  expandedRunId.value = run.id
  detailQuery.value = ''
  workItemStatusFilter.value = 'all'
  diagnosticSeverityFilter.value = 'all'
  workItemPage.value = 1
  selectedWorkItemId.value = undefined
  selectedDiagnostics.value = []
  await loadDetails(run)
}

async function loadDetails(run: AssetImportJob) {
  detailLoading.value = true
  detailError.value = undefined
  try {
    const [workPage, runDiagnosticPage] = await Promise.all([
      getAssetImportWorkItems(run.kind, run.id, {
        status: workItemStatusFilter.value === 'all' ? undefined : workItemStatusFilter.value,
        query: detailQuery.value.trim() || undefined,
        diagnosticSeverity: diagnosticSeverityFilter.value === 'all'
          ? undefined
          : diagnosticSeverityFilter.value,
        page: workItemPage.value,
        pageSize: workItemPageSize.value
      }),
      getAssetImportDiagnostics(run.kind, run.id, {
        scope: 'run',
        pageSize: 25
      })
    ])
    workItems.value = workPage.items
    workItemTotal.value = workPage.total
    runDiagnostics.value = runDiagnosticPage.items
    if (!selectedWorkItem.value) {
      selectedWorkItemId.value = undefined
      selectedDiagnostics.value = []
      selectedDiagnosticTotal.value = 0
    }
  } catch {
    detailError.value = 'Per-file import details could not be loaded.'
  } finally {
    detailLoading.value = false
  }
}

async function selectWorkItem(run: AssetImportJob, item: AssetImportWorkItem) {
  if (selectedWorkItemId.value === item.id) {
    selectedWorkItemId.value = undefined
    selectedDiagnostics.value = []
    selectedDiagnosticTotal.value = 0
    return
  }
  selectedWorkItemId.value = item.id
  selectedDiagnosticPage.value = 1
  await loadSelectedDiagnostics(run, item)
}

async function loadSelectedDiagnostics(run: AssetImportJob, item: AssetImportWorkItem) {
  selectedDiagnosticLoading.value = true
  detailError.value = undefined
  try {
    const page = await getAssetImportDiagnostics(run.kind, run.id, {
      sourceKey: item.sourceKey,
      page: selectedDiagnosticPage.value,
      pageSize: 25
    })
    selectedDiagnostics.value = page.items
    selectedDiagnosticTotal.value = page.total
  } catch {
    detailError.value = 'File diagnostics could not be loaded.'
  } finally {
    selectedDiagnosticLoading.value = false
  }
}

async function applyDetailFilters(run: AssetImportJob) {
  workItemPage.value = 1
  selectedWorkItemId.value = undefined
  selectedDiagnostics.value = []
  selectedDiagnosticTotal.value = 0
  await loadDetails(run)
}

async function changeWorkItemPage(run: AssetImportJob, page: number) {
  workItemPage.value = page
  selectedWorkItemId.value = undefined
  selectedDiagnostics.value = []
  selectedDiagnosticTotal.value = 0
  await loadDetails(run)
}

async function changeWorkItemPageSize(run: AssetImportJob, pageSize: number) {
  workItemPageSize.value = pageSize
  workItemPage.value = 1
  selectedWorkItemId.value = undefined
  selectedDiagnostics.value = []
  selectedDiagnosticTotal.value = 0
  await loadDetails(run)
}

async function changeSelectedDiagnosticPage(run: AssetImportJob, page: number) {
  if (!selectedWorkItem.value) return
  selectedDiagnosticPage.value = page
  await loadSelectedDiagnostics(run, selectedWorkItem.value)
}

async function reimport(run: AssetImportJob, item: AssetImportWorkItem) {
  reimporting.value = item.id
  detailError.value = undefined
  try {
    await startAssetFileImport(run.kind, item.sourceKey)
    await loadJobs(false)
  } catch {
    detailError.value = 'The single-file re-import could not be started.'
  } finally {
    reimporting.value = undefined
  }
}

async function forceReimport(run: AssetImportJob, item: AssetImportWorkItem) {
  reimporting.value = item.id
  detailError.value = undefined
  try {
    await startAssetFileImport(run.kind, item.sourceKey, true)
    await loadJobs(false)
  } catch {
    detailError.value = 'The forced single-file rebuild could not be started.'
  } finally {
    reimporting.value = undefined
  }
}

async function rebuildAllStale() {
  rebuildingStale.value = true
  actionError.value = undefined
  try {
    const kinds = importKinds.filter((kind) =>
      (kindFilter.value === 'all' || kind === kindFilter.value)
      && staleByKind.value[kind]?.length
    )
    await Promise.all(kinds.map((kind) => rebuildStaleAssetSources(kind)))
    await loadJobs(false)
  } catch {
    actionError.value = 'One or more stale rebuilds could not be queued.'
  } finally {
    rebuildingStale.value = false
  }
}

async function rebuildStaleSource(kind: AssetImportKind, source: StaleAssetSource) {
  reimporting.value = `${kind}:${source.sourceKey}`
  actionError.value = undefined
  try {
    await startAssetFileImport(kind, source.sourceKey)
    await loadJobs(false)
  } catch {
    actionError.value = `The stale rebuild for ${source.sourceKey} could not be queued.`
  } finally {
    reimporting.value = undefined
  }
}

async function forceRebuildKind() {
  if (kindFilter.value === 'all') return
  forcingKind.value = true
  actionError.value = undefined
  try {
    await startAssetImport(kindFilter.value, { force: true })
    await loadJobs(false)
  } catch {
    actionError.value = `The forced ${kindLabel(kindFilter.value).toLowerCase()} rebuild could not be queued.`
  } finally {
    forcingKind.value = false
  }
}

onMounted(() => void loadJobs())
onBeforeUnmount(() => clearTimeout(pollTimer))
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Asset pipeline"
      title="Import runs"
      description="Track durable scans, per-file publication, and searchable conversion diagnostics."
      icon="i-lucide-history"
    >
      <template #actions>
        <UButton
          icon="i-lucide-refresh-cw"
          label="Refresh"
          color="neutral"
          variant="outline"
          :loading="loading || staleLoading"
          @click="refreshWorkspace"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="Import history unavailable"
      :description="error"
    />

    <UAlert
      v-if="staleError"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="Stale resources unavailable"
      :description="staleError"
    />

    <UAlert
      v-if="actionError"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="Import action failed"
      :description="actionError"
    />

    <UCard>
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div
          class="inline-flex w-full rounded-lg bg-elevated p-1 sm:w-auto"
          role="tablist"
          aria-label="Import workspace"
        >
          <button
            id="import-runs-tab"
            type="button"
            role="tab"
            class="flex flex-1 items-center justify-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary sm:flex-none"
            :class="activeView === 'runs' ? 'bg-default text-highlighted shadow-sm' : 'text-muted hover:text-highlighted'"
            :aria-selected="activeView === 'runs'"
            :tabindex="activeView === 'runs' ? 0 : -1"
            aria-controls="import-runs-panel"
            @click="selectView('runs')"
            @keydown.right.prevent="selectView('stale', true)"
            @keydown.end.prevent="selectView('stale', true)"
          >
            <UIcon name="i-lucide-history" class="size-4" />
            Runs
            <UBadge color="neutral" variant="subtle" size="sm">{{ visibleJobs.length }}</UBadge>
          </button>
          <button
            id="stale-resources-tab"
            type="button"
            role="tab"
            class="flex flex-1 items-center justify-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary sm:flex-none"
            :class="activeView === 'stale' ? 'bg-default text-highlighted shadow-sm' : 'text-muted hover:text-highlighted'"
            :aria-selected="activeView === 'stale'"
            :tabindex="activeView === 'stale' ? 0 : -1"
            aria-controls="stale-resources-panel"
            @click="selectView('stale')"
            @keydown.left.prevent="selectView('runs', true)"
            @keydown.home.prevent="selectView('runs', true)"
          >
            <UIcon name="i-lucide-triangle-alert" class="size-4" :class="visibleStaleSources.length ? 'text-warning' : ''" />
            Stale resources
            <UBadge :color="visibleStaleSources.length ? 'warning' : 'neutral'" variant="subtle" size="sm">
              {{ visibleStaleSources.length }}
            </UBadge>
          </button>
        </div>
        <div class="flex flex-col gap-1 sm:items-end">
          <label for="import-collection-filter" class="text-xs font-medium text-muted">Collection</label>
          <USelect
            id="import-collection-filter"
            v-model="kindFilter"
            :items="kindOptions"
            aria-label="Filter import workspace by collection"
            class="w-full sm:w-52"
          />
        </div>
      </div>
    </UCard>

    <UCard
      v-if="activeView === 'stale'"
      id="stale-resources-panel"
      role="tabpanel"
      aria-labelledby="stale-resources-tab"
      :ui="{ body: 'p-0 sm:p-0' }"
    >
      <template #header>
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="text-sm font-semibold text-highlighted">Resources requiring rebuild</h2>
            <p class="text-xs text-muted">
              Published output remains available until you explicitly rebuild it.
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
              <UBadge color="neutral" variant="outline">{{ kindLabel(entry.kind) }}</UBadge>
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

    <UCard
      v-else
      id="import-runs-panel"
      role="tabpanel"
      aria-labelledby="import-runs-tab"
      :ui="{ body: 'p-0 sm:p-0' }"
    >
      <template #header>
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="text-sm font-semibold text-highlighted">Import history</h2>
            <p class="text-xs text-muted">{{ visibleJobs.length }} recorded runs</p>
          </div>
          <UButton
            v-if="kindFilter !== 'all'"
            icon="i-lucide-hammer"
            :label="`Force rebuild ${kindLabel(kindFilter).toLowerCase()}`"
            color="warning"
            variant="outline"
            :loading="forcingKind"
            :disabled="hasActiveJob"
            @click="forceRebuildKind"
          />
        </div>
      </template>

      <div v-if="visibleJobs.length" class="divide-y divide-default">
        <article v-for="run in visibleJobs" :key="run.id" class="p-4 sm:p-5">
          <button class="w-full text-left" type="button" @click="toggleDetails(run)">
            <div class="flex flex-wrap items-start justify-between gap-3">
              <div class="flex flex-wrap items-center gap-2">
                <UBadge color="neutral" variant="subtle">{{ kindLabel(run.kind) }}</UBadge>
                <UBadge :color="statusColor(run.status)" variant="subtle">
                  {{ run.status.replaceAll('_', ' ') }}
                </UBadge>
                <UBadge color="neutral" variant="outline">
                  {{ run.triggerType.replaceAll('_', ' ') }}
                </UBadge>
              </div>
              <time class="text-xs text-dimmed" :datetime="run.requestedAt">
                {{ formatDate(run.requestedAt) }}
              </time>
            </div>
            <p v-if="run.requestedSourceKey" class="mt-3 truncate text-sm text-muted">
              {{ run.requestedSourceKey }}
            </p>
            <div class="mt-3 flex flex-wrap gap-x-5 gap-y-1 text-xs text-muted">
              <span>{{ run.completedFileCount }} / {{ run.discoveredFileCount }} completed</span>
              <span>{{ run.succeededFileCount }} succeeded</span>
              <span>{{ run.warningFileCount }} warning</span>
              <span>{{ run.failedFileCount }} failed</span>
              <span>{{ run.reusedFileCount }} reused</span>
              <span>Finished {{ formatDate(run.finishedAt) }}</span>
            </div>
            <UProgress
              v-if="isActive(run.status)"
              class="mt-3"
              :model-value="run.completedFileCount"
              :max="run.discoveredFileCount || 1"
            />
            <p v-if="run.error" class="mt-3 text-sm text-error">{{ run.error }}</p>
          </button>

          <div v-if="expandedRunId === run.id" class="mt-5 space-y-5 border-t border-default pt-5">
            <UAlert
              v-if="detailError"
              color="error"
              variant="subtle"
              title="Import details unavailable"
              :description="detailError"
            />

            <section class="space-y-4" aria-label="Import files and diagnostics">
              <div class="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <h3 class="text-sm font-semibold text-highlighted">Files and diagnostics</h3>
                  <p class="mt-1 text-xs text-muted">
                    Select a file to inspect its conversion messages without losing your place in the run.
                  </p>
                </div>
                <span class="text-xs text-muted">{{ workItemTotal }} files</span>
              </div>

              <div
                v-if="runDiagnostics.length"
                class="rounded-lg border p-4"
                :class="runDiagnosticHasError ? 'border-error/40 bg-error/5' : 'border-warning/40 bg-warning/5'"
              >
                <div class="flex items-center gap-2">
                  <UIcon name="i-lucide-triangle-alert" :class="runDiagnosticHasError ? 'text-error' : 'text-warning'" class="size-4" />
                  <h4 class="text-sm font-medium text-highlighted">Run-level messages</h4>
                </div>
                <div class="mt-3 space-y-2">
                  <div v-for="diagnostic in runDiagnostics" :key="diagnostic.id" class="text-sm text-muted">
                    <span class="font-medium text-highlighted">{{ diagnostic.code }}</span>
                    <span class="mx-1 text-dimmed">·</span>
                    {{ diagnostic.message }}
                  </div>
                </div>
              </div>

              <form class="grid gap-3 lg:grid-cols-[minmax(0,1fr)_12rem_12rem_auto]" @submit.prevent="applyDetailFilters(run)">
                <UFormField label="Find files or diagnostics">
                  <UInput
                    v-model="detailQuery"
                    icon="i-lucide-search"
                    placeholder="File path, object, or message"
                  />
                </UFormField>
                <UFormField label="File status">
                  <USelect v-model="workItemStatusFilter" :items="workItemStatusOptions" />
                </UFormField>
                <UFormField label="Diagnostic severity">
                  <USelect v-model="diagnosticSeverityFilter" :items="diagnosticSeverityOptions" />
                </UFormField>
                <div class="flex items-end">
                  <UButton type="submit" label="Apply" color="neutral" variant="outline" class="w-full lg:w-auto" />
                </div>
              </form>

              <div class="overflow-hidden rounded-lg border border-default">
                <div v-if="workItems.length" class="divide-y divide-default">
                  <article v-for="item in workItems" :key="item.id" :class="item.status === 'failed' || item.errorCount ? 'bg-error/5' : item.warningCount ? 'bg-warning/5' : ''">
                    <div class="flex flex-wrap items-center justify-between gap-3 p-3 sm:p-4">
                      <button
                        type="button"
                        class="min-w-0 flex-1 text-left"
                        :aria-expanded="selectedWorkItemId === item.id"
                        @click="selectWorkItem(run, item)"
                      >
                        <div class="flex flex-wrap items-center gap-2">
                          <UIcon :name="selectedWorkItemId === item.id ? 'i-lucide-chevron-down' : 'i-lucide-chevron-right'" class="size-4 text-muted" />
                          <span class="truncate text-sm font-medium text-highlighted">{{ item.sourceKey }}</span>
                          <UBadge :color="item.status === 'failed' || item.errorCount ? 'error' : item.warningCount ? 'warning' : 'neutral'" variant="subtle">
                            {{ item.status.replaceAll('_', ' ') }}
                          </UBadge>
                          <UBadge v-if="item.errorCount" color="error" variant="subtle">{{ item.errorCount }} error{{ item.errorCount === 1 ? '' : 's' }}</UBadge>
                          <UBadge v-if="item.warningCount" color="warning" variant="subtle">{{ item.warningCount }} warning{{ item.warningCount === 1 ? '' : 's' }}</UBadge>
                          <UBadge v-if="item.unpublishedAt" color="error" variant="solid">Unpublished</UBadge>
                        </div>
                        <p class="mt-2 pl-6 text-xs text-muted">
                          {{ item.processedResourceCount }} processed · {{ item.skippedResourceCount }} skipped · attempt {{ item.attemptCount }}
                        </p>
                        <p v-if="item.error" class="mt-1 pl-6 text-xs text-error">{{ item.error }}</p>
                      </button>
                      <div class="flex shrink-0 gap-2">
                        <UButton
                          label="Re-import"
                          icon="i-lucide-rotate-cw"
                          size="xs"
                          color="neutral"
                          variant="outline"
                          :loading="reimporting === item.id"
                          @click="reimport(run, item)"
                        />
                        <UButton
                          label="Force rebuild"
                          icon="i-lucide-hammer"
                          size="xs"
                          color="warning"
                          variant="soft"
                          :loading="reimporting === item.id"
                          @click="forceReimport(run, item)"
                        />
                      </div>
                    </div>

                    <div v-if="selectedWorkItemId === item.id" class="border-t border-default bg-default/30 p-4">
                      <div class="mb-3 flex items-center justify-between gap-3">
                        <h4 class="text-sm font-medium text-highlighted">Diagnostics for {{ item.sourceKey }}</h4>
                        <span v-if="selectedDiagnosticTotal" class="text-xs text-muted">{{ selectedDiagnosticTotal }} messages</span>
                      </div>
                      <div v-if="selectedDiagnostics.length" class="space-y-2">
                        <div v-for="diagnostic in selectedDiagnostics" :key="diagnostic.id" class="rounded-md border border-default bg-default p-3">
                          <div class="flex flex-wrap items-center gap-2 text-xs">
                            <UBadge :color="diagnostic.severity === 'error' ? 'error' : 'warning'" variant="subtle">
                              {{ diagnostic.severity }}
                            </UBadge>
                            <code>{{ diagnostic.code }}</code>
                            <span class="text-muted">{{ diagnostic.stage }}</span>
                            <span v-if="diagnostic.objectName" class="text-muted">{{ diagnostic.objectName }}</span>
                            <time class="ml-auto text-dimmed" :datetime="diagnostic.createdAt">{{ formatDate(diagnostic.createdAt) }}</time>
                          </div>
                          <p class="mt-2 text-sm text-muted">{{ diagnostic.message }}</p>
                        </div>
                      </div>
                      <p v-else class="text-sm text-muted">
                        {{ selectedDiagnosticLoading ? 'Loading diagnostics…' : 'This file has no recorded diagnostics.' }}
                      </p>
                      <StudioTableFooter
                        v-if="selectedDiagnosticTotal > 25"
                        :page="selectedDiagnosticPage"
                        :page-size="25"
                        :total="selectedDiagnosticTotal"
                        :page-size-options="[25]"
                        @update:page="changeSelectedDiagnosticPage(run, $event)"
                      />
                    </div>
                  </article>
                </div>
                <p v-else class="p-6 text-sm text-muted">
                  {{ detailLoading ? 'Loading files…' : 'No files match these filters.' }}
                </p>
                <StudioTableFooter
                  v-if="workItemTotal > 0"
                  :page="workItemPage"
                  :page-size="workItemPageSize"
                  :total="workItemTotal"
                  :page-size-options="[10, 25, 50, 100]"
                  @update:page="changeWorkItemPage(run, $event)"
                  @update:page-size="changeWorkItemPageSize(run, $event)"
                />
              </div>
            </section>
          </div>
        </article>
      </div>
      <div v-else class="grid min-h-64 place-items-center p-8 text-sm text-muted">
        {{ loading ? 'Loading import runs…' : 'No imports have been queued.' }}
      </div>
    </UCard>
  </div>
</template>
