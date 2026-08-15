<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { deleteItemLookup, getItemLookups, updateItemLookupDisplayName } from '../../../services/studio-api'
import type { ItemLookupKind, ItemLookupRecord } from '../../../types/models/item'

const props = defineProps<{ kind: ItemLookupKind; title: string }>()
const items = ref<ItemLookupRecord[]>([])
const total = ref(0)
const query = ref('')
const page = ref(1)
const pageSize = ref(25)
const dialogs = useStudioDialogs()
const notifications = useStudioToasts()
const deletingName = ref<string>()
const loading = ref(true)
const error = ref<string>()
let searchTimer: ReturnType<typeof setTimeout> | undefined
const columns: TableColumn<ItemLookupRecord>[] = [
  { accessorKey: 'name', header: 'Canonical name' },
  { accessorKey: 'displayName', header: 'Display name' },
  { id: 'actions', header: '' }
]

async function load() {
  loading.value = true
  try {
    const response = await getItemLookups(props.kind, {
      query: query.value,
      page: page.value,
      pageSize: pageSize.value
    })
    items.value = response.items
    total.value = response.total
    error.value = undefined
  } catch {
    error.value = 'The lookup values could not be loaded.'
  } finally {
    loading.value = false
  }
}

async function edit(row: ItemLookupRecord) {
  const displayName = await dialogs.prompt({
    title: `Edit ${props.title}`,
    label: 'Display name',
    initialValue: row.displayName
  })
  if (!displayName?.trim()) return
  await updateItemLookupDisplayName(props.kind, row.name, displayName.trim())
  await load()
}

async function remove(row: ItemLookupRecord) {
  const confirmed = await dialogs.confirm({
    title: `Delete ${row.name}?`,
    description: 'This permanently removes the lookup value. Deletion is unavailable while item definitions still use it.',
    confirmLabel: 'Delete value',
    confirmColor: 'error'
  })
  if (!confirmed) return
  deletingName.value = row.name
  try {
    await deleteItemLookup(props.kind, row.name)
    notifications.success({ title: 'Lookup value deleted' })
    await load()
  } catch {
    notifications.error({ title: 'Lookup value could not be deleted', description: 'It may still be used by item definitions.' })
  } finally {
    deletingName.value = undefined
  }
}

function scheduleLoad() {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => void load(), 300)
}

watch(query, () => { page.value = 1; scheduleLoad() })
watch(page, () => void load())
watch(pageSize, () => { page.value = 1; scheduleLoad() })
watch(() => props.kind, () => { page.value = 1; void load() })
onMounted(() => void load())
onUnmounted(() => clearTimeout(searchTimer))
</script>

<template>
  <StudioContentDirectoryLayout
    :title="title"
    description="Canonical values imported from the C1 item catalogue."
    icon="i-lucide-list-tree"
    :import-target="kind"
    :import-label="title.toLowerCase()"
    :loading="loading"
    :error="error"
    @refresh="load"
  >
    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <StudioDataTable
        v-model:query="query"
        v-model:page="page"
        v-model:page-size="pageSize"
        :data="items"
        :total="total"
        :columns="columns"
        :loading="loading"
        empty="No lookup values match this search."
        search-placeholder="Search canonical or display name"
        search-aria-label="Search lookup values"
        :page-size-options="[10, 25, 50, 100]"
        table-class="min-w-[40rem]"
      >
        <template #toolbar-start>
          <div>
            <p class="text-sm font-medium text-highlighted">{{ title }}</p>
            <p class="text-xs text-muted">{{ total.toLocaleString() }} values</p>
          </div>
        </template>
        <template #actions-cell="{ row }">
          <StudioTableRowActions
            :show-edit="true"
            :show-delete="true"
            :delete-loading="deletingName === row.original.name"
            @edit="edit(row.original)"
            @delete="remove(row.original)"
          />
        </template>
      </StudioDataTable>
    </UCard>
  </StudioContentDirectoryLayout>
</template>
