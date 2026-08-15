<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { deleteSkillLookup, getSkillLookupDirectory, updateSkillLookupDisplayName } from '../../../services/studio-api'
import type { SkillLookupKind, SkillLookupRecord } from '../../../types/models/content-directory'
import type { ContentImportTarget } from '../../../types/models/import-job'

const props = defineProps<{
  kind: SkillLookupKind
  title: string
  description: string
  icon: string
  itemLabel: string
  importTarget: ContentImportTarget
}>()

const records = ref<SkillLookupRecord[]>([])
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
const columns: TableColumn<SkillLookupRecord>[] = [
  { accessorKey: 'name', header: 'Canonical name' },
  { accessorKey: 'displayName', header: 'Display name' },
  { id: 'actions', header: '' }
]

async function loadRecords() {
  loading.value = true
  try {
    const response = await getSkillLookupDirectory(props.kind, {
      query: query.value,
      page: page.value,
      pageSize: pageSize.value
    })
    records.value = response.items
    total.value = response.total
    error.value = undefined
  } catch {
    error.value = `The ${props.itemLabel.toLowerCase()} catalog could not be loaded.`
  } finally {
    loading.value = false
  }
}

async function edit(row: SkillLookupRecord) {
  const displayName = await dialogs.prompt({
    title: `Edit ${props.title}`,
    label: 'Display name',
    initialValue: row.displayName
  })
  if (!displayName?.trim()) return
  await updateSkillLookupDisplayName(props.kind, row.name, displayName.trim())
  await loadRecords()
}

async function remove(row: SkillLookupRecord) {
  const confirmed = await dialogs.confirm({
    title: `Delete ${row.name}?`,
    description: 'This permanently removes the lookup value. Deletion is unavailable while skill definitions still use it.',
    confirmLabel: 'Delete value',
    confirmColor: 'error'
  })
  if (!confirmed) return
  deletingName.value = row.name
  try {
    await deleteSkillLookup(props.kind, row.name)
    notifications.success({ title: 'Lookup value deleted' })
    await loadRecords()
  } catch {
    notifications.error({ title: 'Lookup value could not be deleted', description: 'It may still be used by skill definitions.' })
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
        table-class="min-w-[40rem]"
      >
        <template #toolbar-start>
          <div>
            <p class="text-sm font-medium text-highlighted">{{ itemLabel }}</p>
            <p class="text-xs text-muted">{{ total.toLocaleString() }} records</p>
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
