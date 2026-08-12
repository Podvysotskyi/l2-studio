<script setup lang="ts">
import type { AssetImportKind } from '~/types/studio'
import { storeToRefs } from 'pinia'
import { computed, onBeforeUnmount, ref } from 'vue'
import {
  getAssetImportDiagnostics,
  getAssetImportWorkItems,
  startAssetFileImport
} from '../../../services/studio-api'
import { useAssetImportsStore } from '../../../stores/asset-imports'
import type {
  AssetImportDiagnostic,
  AssetImportJob,
  AssetImportWorkItem
} from '../../../types/models/asset-import-job'

const importStore = useAssetImportsStore()
const { jobs: jobsByKind, loading, error } = storeToRefs(importStore)
const kindFilter = ref<'all' | AssetImportKind>('all')
const expandedRunId = ref<string>()
const workItems = ref<AssetImportWorkItem[]>([])
const diagnostics = ref<AssetImportDiagnostic[]>([])
const detailLoading = ref(false)
const detailError = ref<string>()
const diagnosticQuery = ref('')
const severityFilter = ref<'all' | 'warning' | 'error'>('all')
const reimporting = ref<string>()
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
const expandedRun = computed(() =>
  jobs.value.find((job) => job.id === expandedRunId.value)
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
  try {
    await importStore.load(importKinds)
    if (expandedRun.value && isActive(expandedRun.value.status)) {
      await loadDetails(expandedRun.value)
    }
  } catch {}
  if (schedule && hasActiveJob.value) {
    pollTimer = setTimeout(() => void loadJobs(), 1000)
  }
}

async function toggleDetails(run: AssetImportJob) {
  if (expandedRunId.value === run.id) {
    expandedRunId.value = undefined
    return
  }
  expandedRunId.value = run.id
  diagnosticQuery.value = ''
  severityFilter.value = 'all'
  await loadDetails(run)
}

async function loadDetails(run: AssetImportJob) {
  detailLoading.value = true
  detailError.value = undefined
  try {
    const [workPage, diagnosticPage] = await Promise.all([
      getAssetImportWorkItems(run.kind, run.id, { pageSize: 100 }),
      getAssetImportDiagnostics(run.kind, run.id, {
        query: diagnosticQuery.value.trim() || undefined,
        severity:
          severityFilter.value === 'all' ? undefined : severityFilter.value,
        pageSize: 100
      })
    ])
    workItems.value = workPage.items
    diagnostics.value = diagnosticPage.items
  } catch {
    detailError.value = 'Per-file import details could not be loaded.'
  } finally {
    detailLoading.value = false
  }
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
          :loading="loading"
          @click="loadJobs()"
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

    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <template #header>
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="text-sm font-semibold text-highlighted">Recent runs</h2>
            <p class="text-xs text-muted">{{ visibleJobs.length }} recorded runs</p>
          </div>
          <USelect
            v-model="kindFilter"
            :items="kindOptions"
            aria-label="Filter import runs by collection"
            class="w-full sm:w-52"
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

            <section>
              <h3 class="mb-2 text-sm font-semibold text-highlighted">Files</h3>
              <div class="divide-y divide-default rounded-lg border border-default">
                <div v-for="item in workItems" :key="item.id" class="p-3">
                  <div class="flex flex-wrap items-center justify-between gap-3">
                    <div>
                      <div class="flex items-center gap-2">
                        <span class="text-sm font-medium text-highlighted">{{ item.sourceKey }}</span>
                        <UBadge :color="item.status === 'failed' ? 'error' : item.warningCount ? 'warning' : 'neutral'" variant="subtle">
                          {{ item.status.replaceAll('_', ' ') }}
                        </UBadge>
                        <UBadge v-if="item.unpublishedAt" color="error" variant="solid">Unpublished</UBadge>
                      </div>
                      <p class="mt-1 text-xs text-muted">
                        {{ item.processedResourceCount }} processed · {{ item.skippedResourceCount }} skipped · attempt {{ item.attemptCount }}
                      </p>
                      <p v-if="item.error" class="mt-1 text-xs text-error">{{ item.error }}</p>
                    </div>
                    <UButton
                      label="Re-import"
                      icon="i-lucide-rotate-cw"
                      size="xs"
                      color="neutral"
                      variant="outline"
                      :loading="reimporting === item.id"
                      @click="reimport(run, item)"
                    />
                  </div>
                </div>
                <p v-if="!workItems.length" class="p-4 text-sm text-muted">
                  {{ detailLoading ? 'Loading files…' : 'No files were discovered.' }}
                </p>
              </div>
            </section>

            <section>
              <div class="mb-3 flex flex-wrap items-end gap-3">
                <UFormField label="Search diagnostics" class="min-w-56 flex-1">
                  <UInput v-model="diagnosticQuery" placeholder="Message, source, or object" @keyup.enter="loadDetails(run)" />
                </UFormField>
                <USelect
                  v-model="severityFilter"
                  :items="[
                    { label: 'All severities', value: 'all' },
                    { label: 'Warnings', value: 'warning' },
                    { label: 'Errors', value: 'error' }
                  ]"
                  class="w-44"
                />
                <UButton label="Filter" color="neutral" variant="outline" @click="loadDetails(run)" />
              </div>
              <div class="space-y-2">
                <div v-for="diagnostic in diagnostics" :key="diagnostic.id" class="rounded-lg border border-default p-3">
                  <div class="flex flex-wrap items-center gap-2 text-xs">
                    <UBadge :color="diagnostic.severity === 'error' ? 'error' : 'warning'" variant="subtle">
                      {{ diagnostic.severity }}
                    </UBadge>
                    <code>{{ diagnostic.code }}</code>
                    <span class="text-muted">{{ diagnostic.stage }}</span>
                    <span v-if="diagnostic.sourceKey" class="text-muted">{{ diagnostic.sourceKey }}</span>
                  </div>
                  <p class="mt-2 text-sm text-muted">{{ diagnostic.message }}</p>
                </div>
                <p v-if="!diagnostics.length" class="text-sm text-muted">No matching diagnostics.</p>
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
