<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { deletePlayerAppearance, getLookupDirectory, getPlayerAppearanceDirectory, updatePlayerAppearanceName } from '../../../services/studio-api'
import type { PlayerAppearanceKind, PlayerAppearanceRecord } from '../../../types/models/content-directory'
import { loadDirectoryOptions } from '../../../utils/directory-pages'

const props = defineProps<{
  kind: PlayerAppearanceKind
  title: string
  description: string
  icon: string
}>()

const records = ref<PlayerAppearanceRecord[]>([])
const total = ref(0)
const query = ref('')
const page = ref(1)
const pageSize = ref(25)
const playerRaceId = ref<number>()
const playerSexId = ref<number>()
const loading = ref(true)
const filtersLoading = ref(true)
const error = ref<string>()
const dialogs = useStudioDialogs()
const notifications = useStudioToasts()
const deletingKey = ref<string>()
const races = ref<Array<{ label: string; value: number }>>([])
const sexes = ref<Array<{ label: string; value: number }>>([])
let searchTimer: ReturnType<typeof setTimeout> | undefined

const columns: TableColumn<PlayerAppearanceRecord>[] = [
  { accessorKey: 'id', header: 'ID' },
  { accessorKey: 'name', header: 'Option' },
  { accessorKey: 'playerRaceName', header: 'Race' },
  { accessorKey: 'playerSexName', header: 'Sex' },
  { id: 'actions', header: '' }
]
const filterValues = computed({
  get: () => ({ playerRaceId: playerRaceId.value, playerSexId: playerSexId.value }),
  set: (value: Record<string, string | number | boolean | undefined>) => {
    playerRaceId.value = typeof value.playerRaceId === 'number' ? value.playerRaceId : undefined
    playerSexId.value = typeof value.playerSexId === 'number' ? value.playerSexId : undefined
  }
})
const filters = computed(() => [
  { key: 'playerRaceId', placeholder: 'All races', ariaLabel: 'Filter by player race', items: races.value, loading: filtersLoading.value },
  { key: 'playerSexId', placeholder: 'All sexes', ariaLabel: 'Filter by player sex', items: sexes.value, loading: filtersLoading.value }
])

async function load() {
  loading.value = true
  error.value = undefined
  try {
    const response = await getPlayerAppearanceDirectory(props.kind, {
      query: query.value,
      page: page.value,
      pageSize: pageSize.value,
      playerRaceId: playerRaceId.value,
      playerSexId: playerSexId.value
    })
    records.value = response.items
    total.value = response.total
  } catch {
    error.value = 'The player appearance directory could not be loaded from the Studio API.'
  } finally {
    loading.value = false
  }
}

async function loadFilters() {
  filtersLoading.value = true
  try {
    const [availableRaces, availableSexes] = await Promise.all([
      loadDirectoryOptions((nextPage, nextPageSize) => getLookupDirectory('player-races', { page: nextPage, pageSize: nextPageSize })),
      loadDirectoryOptions((nextPage, nextPageSize) => getLookupDirectory('player-sexes', { page: nextPage, pageSize: nextPageSize }))
    ])
    races.value = availableRaces.map(item => ({ label: item.name, value: item.id }))
    sexes.value = availableSexes.map(item => ({ label: item.name, value: item.id }))
  } finally {
    filtersLoading.value = false
  }
}

function scheduleLoad() {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => void load(), 300)
}

async function refreshDirectory() {
  await Promise.all([load(), loadFilters()])
}

function recordKey(record: PlayerAppearanceRecord) {
  return `${record.id}:${record.playerRaceId}:${record.playerSexId}`
}

async function edit(record: PlayerAppearanceRecord) {
  const name = await dialogs.prompt({ title: `Edit ${record.name}`, label: 'Name', initialValue: record.name, confirmLabel: 'Save name' })
  if (!name?.trim() || name.trim() === record.name) return
  try {
    await updatePlayerAppearanceName(props.kind, record, name.trim())
    notifications.success({ title: 'Appearance option saved' })
    await load()
  } catch {
    notifications.error({ title: 'Appearance option could not be saved' })
  }
}

async function remove(record: PlayerAppearanceRecord) {
  const confirmed = await dialogs.confirm({
    title: `Delete ${record.name}?`,
    description: 'This permanently removes the appearance option. A later import can restore the source record.',
    confirmLabel: 'Delete option',
    confirmColor: 'error'
  })
  if (!confirmed) return
  deletingKey.value = recordKey(record)
  try {
    await deletePlayerAppearance(props.kind, record)
    notifications.success({ title: 'Appearance option deleted' })
    await load()
  } catch {
    notifications.error({ title: 'Appearance option could not be deleted' })
  } finally {
    deletingKey.value = undefined
  }
}

watch(query, () => { page.value = 1; scheduleLoad() })
watch([playerRaceId, playerSexId], () => { page.value = 1; void load() })
watch(page, () => void load())
watch(pageSize, () => { page.value = 1; scheduleLoad() })
watch(() => props.kind, () => { page.value = 1; void load() })
onMounted(() => { void load(); void loadFilters() })
onUnmounted(() => clearTimeout(searchTimer))
</script>

<template>
  <StudioContentDirectoryLayout
    :title="title"
    :description="description"
    :icon="icon"
    :import-target="kind"
    :import-label="title.toLowerCase()"
    :loading="loading"
    :error="error"
    @refresh="refreshDirectory"
  >
    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <StudioDataTable
        v-model:query="query"
        v-model:filter-values="filterValues"
        v-model:page="page"
        v-model:page-size="pageSize"
        :data="records"
        :total="total"
        :columns="columns"
        :filters="filters"
        :loading="loading"
        empty="No player appearance options match these filters."
        search-placeholder="Search option, race, or sex"
        search-aria-label="Search player appearance options"
        table-class="min-w-[46rem]"
      >
        <template #toolbar-start>
          <div>
            <p class="text-sm font-medium text-highlighted">Appearance options</p>
            <p class="text-xs text-muted">{{ total.toLocaleString() }} records</p>
          </div>
        </template>
        <template #actions-cell="{ row }">
          <StudioTableRowActions
            :show-edit="true"
            :show-delete="true"
            :delete-loading="deletingKey === recordKey(row.original)"
            @edit="edit(row.original)"
            @delete="remove(row.original)"
          />
        </template>
      </StudioDataTable>
    </UCard>
  </StudioContentDirectoryLayout>
</template>
