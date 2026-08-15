<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import {
  getAssetImportDiagnostics,
  getAssetImportWorkItems,
  getImportJobs,
  startAssetFileImport
} from '../../../services/studio-api'
import type { AssetImportKind } from '../../../types/models/asset-catalog'
import type {
  AssetImportDiagnostic,
  AssetImportWorkItem
} from '../../../types/models/asset-import-job'
import type {
  ImportJob,
  ImportJobCategory,
  ImportJobStatus
} from '../../../types/models/import-job'
import { assetImportKindLabel, assetImportKindOptions } from '../../../utils/asset-import-kinds'

const jobs = ref<ImportJob[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(25)
const query = ref('')
const category = ref<ImportJobCategory>()
const target = ref<string>()
const status = ref<ImportJobStatus>()
const loading = ref(true)
const error = ref<string>()
const selected = ref<ImportJob>()
const workItems = ref<AssetImportWorkItem[]>([])
const workItemTotal = ref(0)
const detailLoading = ref(false)
const detailError = ref<string>()
const selectedWorkItem = ref<AssetImportWorkItem>()
const diagnostics = ref<AssetImportDiagnostic[]>([])
const reimporting = ref<string>()
const notifications = useStudioToasts()
let pollTimer: ReturnType<typeof setTimeout> | undefined
let searchTimer: ReturnType<typeof setTimeout> | undefined

const columns: TableColumn<ImportJob>[] = [
  { accessorKey: 'category', header: 'Category' },
  { accessorKey: 'target', header: 'Target' },
  { accessorKey: 'operation', header: 'Operation' },
  { accessorKey: 'status', header: 'Status' },
  { accessorKey: 'completedCount', header: 'Progress' },
  { accessorKey: 'requestedAt', header: 'Requested' }
]
const filterValues = computed({
  get: () => ({ category: category.value, target: target.value, status: status.value }),
  set: (value: Record<string, string | number | boolean | undefined>) => {
    category.value = value.category === 'content' || value.category === 'asset' ? value.category : undefined
    target.value = typeof value.target === 'string' ? value.target : undefined
    status.value = isStatus(value.status) ? value.status : undefined
  }
})
const targetItems = computed(() => {
  const values = new Set(jobs.value.map(job => job.target))
  for (const option of assetImportKindOptions) if (option.value !== 'all') values.add(option.value)
  return [...values].sort().map(value => ({ label: targetLabel(value), value }))
})
const filters = computed(() => [
  {
    key: 'category', placeholder: 'All categories', items: [
      { label: 'Content', value: 'content' },
      { label: 'Assets', value: 'asset' }
    ]
  },
  { key: 'target', placeholder: 'All targets', items: targetItems.value },
  {
    key: 'status', placeholder: 'All statuses', items: [
      { label: 'Queued', value: 'queued' },
      { label: 'Discovering', value: 'discovering' },
      { label: 'Running', value: 'running' },
      { label: 'Succeeded', value: 'succeeded' },
      { label: 'Warnings', value: 'succeeded_with_warnings' },
      { label: 'Failed', value: 'failed' }
    ]
  }
])
const hasActiveJobs = computed(() => jobs.value.some(job => isActive(job.status)))

async function load(schedule = true) {
  clearTimeout(pollTimer)
  loading.value = true
  try {
    const result = await getImportJobs({
      category: category.value,
      target: target.value,
      status: status.value,
      query: query.value.trim() || undefined,
      page: page.value,
      pageSize: pageSize.value
    })
    jobs.value = result.items
    total.value = result.total
    error.value = undefined
    const routeJob = useRoute().query.job
    if (!selected.value && typeof routeJob === 'string') {
      selected.value = jobs.value.find(job => job.id === routeJob)
      if (selected.value) await loadDetails(selected.value)
    }
    if (schedule && hasActiveJobs.value) pollTimer = setTimeout(() => void load(), 1000)
  } catch {
    error.value = 'Import history could not be loaded.'
  } finally {
    loading.value = false
  }
}

async function open(job: ImportJob) {
  selected.value = job
  selectedWorkItem.value = undefined
  diagnostics.value = []
  await loadDetails(job)
}

async function loadDetails(job: ImportJob) {
  workItems.value = []
  workItemTotal.value = 0
  detailError.value = undefined
  if (job.category !== 'asset') return
  detailLoading.value = true
  try {
    const result = await getAssetImportWorkItems(job.target as AssetImportKind, job.id, {
      page: 1,
      pageSize: 25
    })
    workItems.value = result.items
    workItemTotal.value = result.total
  } catch {
    detailError.value = 'Asset work items could not be loaded.'
  } finally {
    detailLoading.value = false
  }
}

async function inspectWorkItem(item: AssetImportWorkItem) {
  const job = selected.value
  if (!job || job.category !== 'asset') return
  selectedWorkItem.value = item
  try {
    const result = await getAssetImportDiagnostics(job.target as AssetImportKind, job.id, {
      sourceKey: item.sourceKey,
      page: 1,
      pageSize: 25
    })
    diagnostics.value = result.items
  } catch {
    detailError.value = 'Asset diagnostics could not be loaded.'
  }
}

async function reimport(item: AssetImportWorkItem, force: boolean) {
  const job = selected.value
  if (!job || job.category !== 'asset') return
  reimporting.value = item.id
  try {
    await startAssetFileImport(job.target as AssetImportKind, item.sourceKey, force)
    notifications.success({ title: force ? 'Forced rebuild queued' : 'Re-import queued' })
    selected.value = undefined
    await load()
  } catch {
    notifications.error({ title: 'Re-import could not be queued' })
  } finally {
    reimporting.value = undefined
  }
}

function scheduleLoad() {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => void load(), 300)
}

function isActive(value: ImportJobStatus) {
  return value === 'queued' || value === 'discovering' || value === 'running'
}

function isStatus(value: unknown): value is ImportJobStatus {
  return typeof value === 'string' && [
    'queued', 'discovering', 'running', 'succeeded', 'succeeded_with_warnings', 'failed'
  ].includes(value)
}

function targetLabel(value: string) {
  if (assetImportKindOptions.some(option => option.value === value))
    return assetImportKindLabel(value as AssetImportKind)
  return value.split('-').map(part => part[0]?.toUpperCase() + part.slice(1)).join(' ')
}

function statusColor(value: ImportJobStatus) {
  if (value === 'failed') return 'error' as const
  if (value === 'succeeded_with_warnings') return 'warning' as const
  if (value === 'succeeded') return 'success' as const
  return 'info' as const
}

watch(query, scheduleLoad)
watch([category, target, status], () => { page.value = 1; void load() })
watch(page, () => void load())
watch(pageSize, () => { page.value = 1; void load() })
onMounted(() => void load())
onUnmounted(() => { clearTimeout(pollTimer); clearTimeout(searchTimer) })
</script>

<template>
  <StudioContentDirectoryLayout
    eyebrow="Pipeline"
    title="Import jobs"
    description="Track content reconciliation and asset conversion jobs from one durable history."
    icon="i-lucide-history"
    :loading="loading"
    :error="error"
    @refresh="load"
  >
    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <StudioDataTable
        v-model:query="query"
        v-model:filter-values="filterValues"
        v-model:page="page"
        v-model:page-size="pageSize"
        :data="jobs"
        :total="total"
        :columns="columns"
        :filters="filters"
        :loading="loading"
        search-placeholder="Search target or error"
        search-aria-label="Search import jobs"
        empty="No import jobs match these filters."
        :page-size-options="[10, 25, 50, 100]"
        table-class="min-w-[64rem]"
        @select="(_event: unknown, row: { original: ImportJob }) => open(row.original)"
      >
        <template #toolbar-start>
          <div>
            <p class="text-sm font-medium text-highlighted">Import history</p>
            <p class="text-xs text-muted">{{ total.toLocaleString() }} jobs</p>
          </div>
        </template>
        <template #category-cell="{ row }"><UBadge color="neutral" variant="subtle">{{ row.original.category }}</UBadge></template>
        <template #target-cell="{ row }"><span class="font-medium text-highlighted">{{ targetLabel(row.original.target) }}</span></template>
        <template #operation-cell="{ row }">{{ row.original.operation.replaceAll('_', ' ') }}</template>
        <template #status-cell="{ row }"><UBadge :color="statusColor(row.original.status)" variant="subtle">{{ row.original.status.replaceAll('_', ' ') }}</UBadge></template>
        <template #completedCount-cell="{ row }">{{ row.original.completedCount }} / {{ row.original.totalCount || '—' }}</template>
        <template #requestedAt-cell="{ row }">{{ new Date(row.original.requestedAt).toLocaleString() }}</template>
      </StudioDataTable>
    </UCard>

    <USlideover
      :open="Boolean(selected)"
      :title="selected ? `${targetLabel(selected.target)} import` : 'Import job'"
      :description="selected?.operation.replaceAll('_', ' ')"
      :ui="{ content: 'max-w-3xl' }"
      @update:open="open => { if (!open) selected = undefined }"
    >
      <template #body>
        <div v-if="selected" class="space-y-5">
          <div class="flex flex-wrap gap-2">
            <UBadge :color="statusColor(selected.status)">{{ selected.status.replaceAll('_', ' ') }}</UBadge>
            <UBadge color="neutral" variant="outline">{{ selected.category }}</UBadge>
          </div>
          <UAlert v-if="selected.error" color="error" title="Import failed" :description="selected.error" />
          <div class="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <div v-for="metric in selected.metrics" :key="metric.key" class="rounded-lg bg-elevated p-3">
              <p class="text-xs text-muted">{{ metric.key.replaceAll('_', ' ') }}</p>
              <p class="mt-1 text-xl font-semibold text-highlighted">{{ metric.value.toLocaleString() }}</p>
            </div>
          </div>
          <template v-if="selected.category === 'asset'">
            <UAlert v-if="detailError" color="error" :description="detailError" />
            <p v-if="detailLoading" class="text-sm text-muted">Loading work items…</p>
            <div v-else class="divide-y divide-default overflow-hidden rounded-lg border border-default">
              <article v-for="item in workItems" :key="item.id" class="p-4">
                <button type="button" class="w-full text-left" @click="inspectWorkItem(item)">
                  <div class="flex items-center justify-between gap-3">
                    <span class="truncate text-sm font-medium text-highlighted">{{ item.sourceKey }}</span>
                    <UBadge color="neutral" variant="subtle">{{ item.status.replaceAll('_', ' ') }}</UBadge>
                  </div>
                  <p class="mt-1 text-xs text-muted">{{ item.processedResourceCount }} processed · {{ item.warningCount }} warnings · {{ item.errorCount }} errors</p>
                </button>
                <div class="mt-3 flex gap-2">
                  <UButton label="Re-import" size="xs" color="neutral" variant="outline" :loading="reimporting === item.id" @click="reimport(item, false)" />
                  <UButton label="Force rebuild" size="xs" color="warning" variant="soft" :loading="reimporting === item.id" @click="reimport(item, true)" />
                </div>
                <div v-if="selectedWorkItem?.id === item.id" class="mt-3 space-y-2 border-t border-default pt-3">
                  <p v-if="!diagnostics.length" class="text-xs text-muted">No diagnostics recorded.</p>
                  <div v-for="diagnostic in diagnostics" :key="diagnostic.id" class="rounded-md bg-elevated p-3 text-sm">
                    <UBadge :color="diagnostic.severity === 'error' ? 'error' : 'warning'" size="sm">{{ diagnostic.code }}</UBadge>
                    <p class="mt-2 text-muted">{{ diagnostic.message }}</p>
                  </div>
                </div>
              </article>
              <p v-if="!workItems.length" class="p-5 text-sm text-muted">No work items recorded.</p>
            </div>
            <p v-if="workItemTotal > workItems.length" class="text-xs text-muted">Showing the first {{ workItems.length }} of {{ workItemTotal }} work items.</p>
          </template>
        </div>
      </template>
    </USlideover>
  </StudioContentDirectoryLayout>
</template>
