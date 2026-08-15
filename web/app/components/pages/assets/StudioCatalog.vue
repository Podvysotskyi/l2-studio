<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { watch } from 'vue'
import { deletePlayerLookup, getLookupDirectory, updatePlayerLookupName } from '../../../services/studio-api'
import type { LookupKind, LookupRecord } from '../../../types/models/content-directory'
import type { ContentImportTarget } from '../../../types/models/import-job'

const props = defineProps<{
  kind: Extract<LookupKind, 'player-races' | 'player-sexes'>
  title: string
  description: string
  icon: string
  itemLabel: string
  importTarget?: ContentImportTarget
}>()

const records = ref<LookupRecord[]>([])
const total = ref(0)
const query = ref('')
const page = ref(1)
const pageSize = ref(25)
const loading = ref(true)
const error = ref<string>()
const dialogs = useStudioDialogs()
const notifications = useStudioToasts()
const deletingId = ref<number>()
let searchTimer: ReturnType<typeof setTimeout> | undefined

const columns: TableColumn<LookupRecord>[] = [
  { accessorKey: 'id', header: 'ID' },
  { accessorKey: 'name', header: 'Canonical name' },
  { id: 'actions', header: '' }
]

async function loadRecords() {
  loading.value = true
  error.value = undefined
  try {
    const response = await getLookupDirectory(props.kind, {
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

async function edit(record: LookupRecord) {
  const name = await dialogs.prompt({ title: `Edit ${record.name}`, label: 'Name', initialValue: record.name, confirmLabel: 'Save name' })
  if (!name?.trim() || name.trim() === record.name) return
  try {
    await updatePlayerLookupName(props.kind, record.id, name.trim())
    notifications.success({ title: 'Name saved' })
    await loadRecords()
  } catch {
    notifications.error({ title: 'Name could not be saved' })
  }
}

async function remove(record: LookupRecord) {
  const confirmed = await dialogs.confirm({
    title: `Delete ${record.name}?`,
    description: 'Deletion is unavailable while player classes or appearance options still use this value.',
    confirmLabel: 'Delete value',
    confirmColor: 'error'
  })
  if (!confirmed) return
  deletingId.value = record.id
  try {
    await deletePlayerLookup(props.kind, record.id)
    notifications.success({ title: 'Lookup value deleted' })
    await loadRecords()
  } catch {
    notifications.error({ title: 'Lookup value could not be deleted', description: 'It may still be used by player classes or appearance options.' })
  } finally {
    deletingId.value = undefined
  }
}

function scheduleLoad() {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => void loadRecords(), 300)
}

watch(query, () => {
  page.value = 1
  scheduleLoad()
})
watch(page, () => void loadRecords())
watch(pageSize, () => {
  page.value = 1
  scheduleLoad()
})
watch(() => props.kind, () => {
  page.value = 1
  void loadRecords()
})
onMounted(() => void loadRecords())
onUnmounted(() => clearTimeout(searchTimer))
</script>

<template>
  <StudioContentDirectoryLayout
      :title="title"
      :description="description"
      :icon="icon"
      :import-target="importTarget"
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
        table-class="min-w-[34rem]"
      >
        <template #toolbar-start>
          <div>
            <p class="text-sm font-medium text-highlighted">{{ itemLabel }}</p>
            <p class="text-xs text-muted">{{ total.toLocaleString() }} records</p>
          </div>
        </template>
        <template #id-cell="{ row }">
          <UBadge color="neutral" variant="subtle" size="sm">{{ row.original.id }}</UBadge>
        </template>
        <template #name-cell="{ row }">
          <span class="font-medium text-highlighted">{{ row.original.name }}</span>
        </template>
        <template #actions-cell="{ row }">
          <StudioTableRowActions
            :show-edit="true"
            :show-delete="true"
            :delete-loading="deletingId === row.original.id"
            @edit="edit(row.original)"
            @delete="remove(row.original)"
          />
        </template>
      </StudioDataTable>
    </UCard>
  </StudioContentDirectoryLayout>
</template>
