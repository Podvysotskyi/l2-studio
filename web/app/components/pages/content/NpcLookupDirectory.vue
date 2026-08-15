<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import {
  getNpcLookupDirectory,
  updateNpcLookupDisplayName,
  deleteNpcLookup
} from '../../../services/studio-api'
import type { NpcLookupKind, NpcLookupRecord } from '../../../types/models/content-directory'

const props = defineProps<{
  kind: NpcLookupKind
  title: string
  description: string
  icon: string
  itemLabel: string
  importable?: boolean
}>()

const dialogs = useStudioDialogs()
const records = ref<NpcLookupRecord[]>([])
const total = ref(0)
const query = ref('')
const page = ref(1)
const pageSize = ref(25)
const loading = ref(false)
const error = ref<string>()
const savingName = ref<string>()
const notifications = useStudioToasts()
const deletingName = ref<string>()
let searchTimer: ReturnType<typeof setTimeout> | undefined
const columns: TableColumn<NpcLookupRecord>[] = [
  { accessorKey: 'name', header: 'Canonical name' },
  { accessorKey: 'displayName', header: 'Display name' },
  { id: 'actions', header: '' }
]

async function loadRecords() {
  loading.value = true
  error.value = undefined
  try {
    const response = await getNpcLookupDirectory(props.kind, {
      query: query.value,
      page: page.value,
      pageSize: pageSize.value
    })
    records.value = response.items
    total.value = response.total
  } catch {
    error.value = `The ${props.itemLabel.toLowerCase()} catalog could not be loaded.`
  } finally {
    loading.value = false
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
  try {
    await updateNpcLookupDisplayName(props.kind, record.name, displayName)
    await loadRecords()
    notifications.success({ title: 'Display name saved' })
  } catch {
    notifications.error({ title: 'Display name could not be saved' })
  } finally {
    savingName.value = undefined
  }
}

async function remove(record: NpcLookupRecord) {
  const confirmed = await dialogs.confirm({
    title: `Delete ${record.name}?`,
    description: 'This permanently removes the lookup value. Deletion is unavailable while NPC definitions still use it.',
    confirmLabel: 'Delete value',
    confirmColor: 'error'
  })
  if (!confirmed) return
  deletingName.value = record.name
  try {
    await deleteNpcLookup(props.kind, record.name)
    notifications.success({ title: 'Lookup value deleted' })
    await loadRecords()
  } catch {
    notifications.error({ title: 'Lookup value could not be deleted', description: 'It may still be used by NPC definitions.' })
  } finally {
    deletingName.value = undefined
  }
}

function scheduleLoad() {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => void loadRecords(), 300)
}

watch(query, () => { page.value = 1; scheduleLoad() })
watch(page, () => void loadRecords())
watch(pageSize, () => { page.value = 1; scheduleLoad() })
watch(() => props.kind, () => { page.value = 1; void loadRecords() })
onMounted(() => void loadRecords())
onUnmounted(() => clearTimeout(searchTimer))
</script>

<template>
  <StudioContentDirectoryLayout
    :title="title"
    :description="description"
    :icon="icon"
    :import-target="importable ? kind : undefined"
    :import-label="itemLabel.toLowerCase()"
    :loading="loading"
    :error="error"
    @refresh="loadRecords"
  >
    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <StudioDataTable
        v-model:query="query"
        v-model:page="page"
        v-model:page-size="pageSize"
        :data="records"
        :total="total"
        :columns="columns"
        :loading="loading"
        :empty="`No ${itemLabel.toLowerCase()} match this search.`"
        :search-placeholder="`Search ${itemLabel.toLowerCase()}`"
        :search-aria-label="`Search ${itemLabel.toLowerCase()}`"
        :page-size-options="[10, 25, 50, 100]"
        table-class="min-w-[40rem]"
      >
        <template #toolbar-start>
          <div>
            <p class="text-sm font-medium text-highlighted">{{ itemLabel }}</p>
            <p class="text-xs text-muted">{{ total.toLocaleString() }} records</p>
          </div>
        </template>
        <template #name-cell="{ row }"><code class="text-xs text-muted">{{ row.original.name }}</code></template>
        <template #displayName-cell="{ row }"><span class="font-medium text-highlighted">{{ row.original.displayName }}</span></template>
        <template #actions-cell="{ row }">
          <StudioTableRowActions
            :show-edit="true"
            :show-delete="true"
            :edit-loading="savingName === row.original.name"
            :delete-loading="deletingName === row.original.name"
            @edit="edit(row.original)"
            @delete="remove(row.original)"
          />
        </template>
      </StudioDataTable>
    </UCard>
  </StudioContentDirectoryLayout>
</template>
