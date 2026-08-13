<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { computed, watch } from 'vue'
import { useStudioDialogs } from '../../../composables/use-studio-dialogs'
import {
  getNpcLookupImportJob,
  getNpcLookupImportJobs,
  startNpcLookupImport
} from '../../../services/studio-api'
import { useNpcLookupDirectoryStore } from '../../../stores/npc-lookup-directory'
import type {
  NpcLookupKind,
  NpcLookupRecord
} from '../../../types/models/content-directory'
import type { NpcLookupImportRun } from '../../../types/models/npc-lookup-import'
import { paginate } from '../../../utils/directory'

const props = defineProps<{
  kind: NpcLookupKind
  title: string
  description: string
  icon: string
  itemLabel: string
  importable?: boolean
}>()

const store = useNpcLookupDirectoryStore()
const dialogs = useStudioDialogs()
const records = computed<NpcLookupRecord[]>(() => store.records[props.kind] ?? [])
const query = ref('')
const page = ref(1)
const pageSize = ref(10)
const loading = computed(() => store.isLoading(props.kind))
const error = computed(() => store.errors[props.kind])
const actionError = ref<string>()
const savingName = ref<string>()
const latestRun = ref<NpcLookupImportRun>()
const queueing = ref(false)
let pollTimer: ReturnType<typeof setTimeout> | undefined

const columns: TableColumn<NpcLookupRecord>[] = [
  { accessorKey: 'name', header: 'Canonical name' },
  { accessorKey: 'displayName', header: 'Display name' },
  { id: 'actions', header: '' }
]
const filteredRecords = computed(() => {
  const term = query.value.trim().toLocaleLowerCase()
  if (!term) return records.value
  return records.value.filter(record =>
    record.name.toLocaleLowerCase().includes(term) ||
    record.displayName.toLocaleLowerCase().includes(term)
  )
})
const visibleRecords = computed(() => paginate(filteredRecords.value, page.value, pageSize.value))
const importKind = computed(() => props.kind === 'npc-sexes' ? undefined : props.kind)
const importLabel = computed(() => props.kind === 'npc-types' ? 'Import NPC types' : 'Import NPC races')
const activeRun = computed(() => latestRun.value && ['queued', 'running'].includes(latestRun.value.status))

watch([query, pageSize], () => { page.value = 1 })

async function loadRecords() {
  await store.load(props.kind, props.itemLabel)
}

async function loadLatestRun(schedule = true) {
  if (!props.importable || !importKind.value) return
  try {
    const runs = await getNpcLookupImportJobs(importKind.value, 1)
    latestRun.value = runs[0]
    actionError.value = undefined
    if (schedule && activeRun.value) schedulePoll()
  } catch {
    actionError.value = 'The latest import status could not be loaded.'
  }
}

function schedulePoll() {
  clearTimeout(pollTimer)
  pollTimer = setTimeout(() => void pollRun(), 1000)
}

async function pollRun() {
  if (!latestRun.value || !importKind.value) return
  try {
    latestRun.value = await getNpcLookupImportJob(importKind.value, latestRun.value.id)
    if (activeRun.value) schedulePoll()
    else if (latestRun.value.status === 'succeeded') await loadRecords()
  } catch {
    actionError.value = 'The active import status could not be refreshed.'
  }
}

async function queueImport() {
  if (!importKind.value) return
  queueing.value = true
  actionError.value = undefined
  try {
    latestRun.value = await startNpcLookupImport(importKind.value)
    schedulePoll()
  } catch {
    actionError.value = `The ${props.itemLabel.toLowerCase()} import could not be queued.`
  } finally {
    queueing.value = false
  }
}

async function edit(record: NpcLookupRecord) {
  const displayName = await dialogs.prompt({
    title: `Edit ${record.name}`,
    description: 'The canonical source name remains unchanged.',
    label: 'Display name',
    initialValue: record.displayName,
    confirmLabel: 'Save display name'
  })
  if (!displayName || displayName === record.displayName) return
  savingName.value = record.name
  actionError.value = undefined
  try {
    await store.updateDisplayName(props.kind, record.name, displayName)
  } catch {
    actionError.value = `The display name for ${record.name} could not be saved.`
  } finally {
    savingName.value = undefined
  }
}

onMounted(() => {
  void loadRecords()
  void loadLatestRun()
})
onUnmounted(() => clearTimeout(pollTimer))
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader eyebrow="Game content" :title="title" :description="description" :icon="icon">
      <template #actions>
        <UButton
          v-if="importable"
          :label="importLabel"
          icon="i-lucide-play"
          :loading="queueing || Boolean(activeRun)"
          :disabled="Boolean(activeRun)"
          @click="queueImport"
        />
        <UButton label="Refresh" icon="i-lucide-refresh-cw" color="neutral" variant="outline" :loading="loading" @click="loadRecords" />
      </template>
    </StudioPageHeader>

    <UAlert v-if="error || actionError" color="error" variant="subtle" icon="i-lucide-circle-alert" title="Catalog action failed" :description="error ?? actionError" />

    <UCard v-if="latestRun" variant="subtle">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p class="text-sm font-medium text-highlighted">Latest import: {{ latestRun.status.replaceAll('_', ' ') }}</p>
          <p v-if="latestRun.status === 'succeeded'" class="text-xs text-muted">
            {{ latestRun.insertedCount }} inserted · {{ latestRun.existingCount }} already existed · {{ latestRun.totalCount }} total
          </p>
          <p v-else-if="latestRun.error" class="text-xs text-error">{{ latestRun.error }}</p>
          <p v-else class="text-xs text-muted">Requested {{ new Date(latestRun.requestedAt).toLocaleString() }}</p>
        </div>
        <UBadge :color="latestRun.status === 'failed' ? 'error' : latestRun.status === 'succeeded' ? 'success' : 'info'" variant="subtle">
          {{ latestRun.status }}
        </UBadge>
      </div>
    </UCard>

    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <div class="flex flex-wrap items-center justify-between gap-4 border-b border-default px-4 py-3">
        <div>
          <p class="text-sm font-medium text-highlighted">{{ itemLabel }}</p>
          <p class="text-xs text-muted">{{ filteredRecords.length }} of {{ records.length }} records</p>
        </div>
        <UInput v-model="query" icon="i-lucide-search" :placeholder="`Search ${itemLabel.toLowerCase()}`" class="w-full sm:w-72" />
      </div>
      <div class="overflow-x-auto">
        <UTable :data="visibleRecords" :columns="columns" :loading="loading" :empty="`No ${itemLabel.toLowerCase()} match this search.`" class="min-w-[40rem]">
          <template #name-cell="{ row }"><code class="text-xs text-muted">{{ row.original.name }}</code></template>
          <template #displayName-cell="{ row }"><span class="font-medium text-highlighted">{{ row.original.displayName }}</span></template>
          <template #actions-cell="{ row }">
            <div class="flex justify-end"><UButton label="Edit" icon="i-lucide-pencil" color="neutral" variant="ghost" size="sm" :loading="savingName === row.original.name" @click="edit(row.original)" /></div>
          </template>
        </UTable>
      </div>
      <StudioTableFooter v-model:page="page" v-model:page-size="pageSize" :total="filteredRecords.length" />
    </UCard>
  </div>
</template>
