<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue'
import { useStudioDialogs } from '../../../composables/use-studio-dialogs'
import {
  getAssetImportJob,
  getAssetImportJobs,
  getNpcLookupDirectory,
  startAssetImport,
  deleteNpcDefinition,
  updateNpcDefinition
} from '../../../services/studio-api'
import type { NpcLookupRecord, NpcRecord, NpcVisualFilter } from '../../../types/models/content-directory'
import type { AssetImportJob } from '../../../types/models/asset-import-job'
import type { UpdateNpcRequest } from '../../../types/requests/update-npc-request'
import { selectedGameVersionKey } from '../../../utils/game-version'
import { assetImportProgressItem } from '../../../utils/import-progress'
import { npcRaceNoneValue } from '../../../utils/npc-directory'
import { loadDirectoryOptions } from '../../../utils/directory-pages'

const query = defineModel<string>('query', { required: true })
const page = defineModel<number>('page', { required: true })
const pageSize = defineModel<number>('pageSize', { required: true })
const npcTypeName = defineModel<string | undefined>('npcTypeName', { required: true })
const npcRaceName = defineModel<string | undefined>('npcRaceName', { required: true })
const npcSexName = defineModel<string | undefined>('npcSexName', { required: true })
const visualFilter = defineModel<NpcVisualFilter | undefined>('visualFilter', { required: true })

defineProps<{
  items: NpcRecord[]
  total: number
  loading: boolean
  error?: string
}>()

const emit = defineEmits<{ refresh: [] }>()

const importStatusError = ref<string>()
const latestAppearanceJob = ref<AssetImportJob>()
const queueingAppearances = ref(false)
const progressAppearanceJobId = ref<string>()
const importDrawerOpen = ref(false)
const selectedNpc = ref<NpcRecord>()
const npcTypes = ref<NpcLookupRecord[]>([])
const npcRaces = ref<NpcLookupRecord[]>([])
const npcSexes = ref<NpcLookupRecord[]>([])
const editOpen = ref(false)
const lookupsLoading = ref(false)
const lookupsLoaded = ref(false)
const lookupsError = ref<string>()
const saving = ref(false)
const deletingId = ref<number>()
const editError = ref<string>()
const notifications = useStudioToasts()
const dialogs = useStudioDialogs()
const isC1 = selectedGameVersionKey() === 'c1'
const editForm = reactive<UpdateNpcRequest>({
  name: '',
  level: 1,
  npcTypeName: '',
  npcRaceName: null,
  npcSexName: ''
})
let pollTimer: ReturnType<typeof setTimeout> | undefined

const activeAppearanceJob = computed(() => latestAppearanceJob.value
  ? ['queued', 'discovering', 'running'].includes(latestAppearanceJob.value.status)
  : false)
const progressItems = computed(() => {
  const items = []
  const appearanceJob = latestAppearanceJob.value
  if (appearanceJob && appearanceJob.id === progressAppearanceJobId.value)
    items.push(assetImportProgressItem(appearanceJob, 'NPC appearances'))
  return items
})
const typeOptions = computed(() => lookupOptions(npcTypes.value))
const raceOptions = computed(() => [
  { label: 'No race', value: null },
  ...lookupOptions(npcRaces.value)
])
const sexOptions = computed(() => lookupOptions(npcSexes.value))
const typeFilterOptions = computed(() => typeOptions.value)
const raceFilterOptions = computed(() => [
  { label: 'No race', value: npcRaceNoneValue },
  ...lookupOptions(npcRaces.value)
])
const sexFilterOptions = computed(() => sexOptions.value)
const visualFilterOptions = [
  { label: 'Has appearance', value: 'with' },
  { label: 'No appearance', value: 'without' }
]
const tableFilterValues = computed({
  get: () => ({
    npcTypeName: npcTypeName.value,
    npcRaceName: npcRaceName.value,
    npcSexName: npcSexName.value,
    visualFilter: visualFilter.value
  }),
  set: (value: Record<string, string | number | boolean | undefined>) => {
    npcTypeName.value = stringValue(value.npcTypeName)
    npcRaceName.value = stringValue(value.npcRaceName)
    npcSexName.value = stringValue(value.npcSexName)
    visualFilter.value = value.visualFilter === 'with' || value.visualFilter === 'without'
      ? value.visualFilter
      : undefined
  }
})
const tableFilters = computed(() => [
  { key: 'npcTypeName', placeholder: 'All types', ariaLabel: 'Filter by NPC type', items: typeFilterOptions.value, loading: lookupsLoading.value },
  { key: 'npcRaceName', placeholder: 'All races', ariaLabel: 'Filter by NPC race', items: raceFilterOptions.value, loading: lookupsLoading.value },
  { key: 'npcSexName', placeholder: 'All sexes', ariaLabel: 'Filter by NPC sex', items: sexFilterOptions.value, loading: lookupsLoading.value },
  { key: 'visualFilter', placeholder: 'All appearances', ariaLabel: 'Filter by appearance availability', items: visualFilterOptions }
])
const columns: TableColumn<NpcRecord>[] = [
  { accessorKey: 'id', header: 'ID' },
  { accessorKey: 'name', header: 'NPC' },
  { accessorKey: 'hasVisuals', header: 'Visuals' },
  { accessorKey: 'level', header: 'Level' },
  { accessorKey: 'npcType', header: 'Type' },
  { accessorKey: 'npcRace', header: 'Race' },
  { accessorKey: 'npcSex', header: 'Sex' },
  { id: 'actions', header: '' }
]

async function loadLatestImports(schedule = true) {
  if (!isC1) return
  try {
    const latestAppearance = (await getAssetImportJobs('npcappearances', 1))[0]
    latestAppearanceJob.value = latestAppearance
    if (latestAppearance && ['queued', 'discovering', 'running'].includes(latestAppearance.status) &&
      latestAppearance.id !== progressAppearanceJobId.value) {
      progressAppearanceJobId.value = latestAppearance.id
      importDrawerOpen.value = true
    }
    importStatusError.value = undefined
    if (schedule && activeAppearanceJob.value) schedulePoll()
  } catch {
    importStatusError.value = 'NPC import progress could not be loaded.'
  }
}

function schedulePoll() {
  clearTimeout(pollTimer)
  pollTimer = setTimeout(() => void pollRun(), 1000)
}

async function pollRun() {
  if (!isC1 || !activeAppearanceJob.value) return
  const appearanceJob = activeAppearanceJob.value ? latestAppearanceJob.value : undefined
  try {
    const nextAppearanceJob = appearanceJob
      ? await getAssetImportJob('npcappearances', appearanceJob.id)
      : undefined
    if (nextAppearanceJob) latestAppearanceJob.value = nextAppearanceJob
    if (activeAppearanceJob.value) schedulePoll()
  } catch {
    importStatusError.value = 'Active NPC import progress could not be refreshed.'
  }
}

async function queueAppearanceImport() {
  if (!isC1) return
  queueingAppearances.value = true
  try {
    latestAppearanceJob.value = await startAssetImport('npcappearances')
    progressAppearanceJobId.value = latestAppearanceJob.value.id
    importDrawerOpen.value = true
    schedulePoll()
  } catch {
    notifications.error({
      title: 'NPC appearance import could not be queued',
      description: 'Another NPC appearance import may already be active.'
    })
  } finally {
    queueingAppearances.value = false
  }
}

function lookupOptions(records: NpcLookupRecord[]) {
  return records.map(record => ({
    label: record.displayName === record.name
      ? record.name
      : `${record.displayName} (${record.name})`,
    value: record.name
  }))
}

async function edit(record: NpcRecord) {
  selectedNpc.value = record
  editForm.name = record.name ?? ''
  editForm.level = record.level
  editForm.npcTypeName = record.npcTypeName
  editForm.npcRaceName = record.npcRaceName
  editForm.npcSexName = record.npcSexName
  editError.value = undefined
  editOpen.value = true
  await loadLookups()
  if (lookupsError.value) editError.value = lookupsError.value
}

async function loadLookups() {
  if (lookupsLoaded.value || lookupsLoading.value) return

  lookupsLoading.value = true
  lookupsError.value = undefined
  try {
    const [types, races, sexes] = await Promise.all([
      loadDirectoryOptions((page, pageSize) => getNpcLookupDirectory('npc-types', { page, pageSize })),
      loadDirectoryOptions((page, pageSize) => getNpcLookupDirectory('npc-races', { page, pageSize })),
      loadDirectoryOptions((page, pageSize) => getNpcLookupDirectory('npc-sexes', { page, pageSize }))
    ])
    npcTypes.value = types
    npcRaces.value = races
    npcSexes.value = sexes
    lookupsLoaded.value = true
  } catch {
    lookupsError.value = 'The NPC lookup values could not be loaded.'
  } finally {
    lookupsLoading.value = false
  }
}

function stringValue(value: string | number | boolean | undefined) {
  return typeof value === 'string' ? value : undefined
}

async function saveNpc() {
  const npc = selectedNpc.value
  const name = editForm.name.trim()
  if (!npc) return
  if (!name || name.length > 100) {
    editError.value = 'Name must contain between 1 and 100 characters.'
    return
  }
  if (!Number.isInteger(editForm.level) || editForm.level < 1 || editForm.level > 255) {
    editError.value = 'Level must be between 1 and 255.'
    return
  }
  if (!editForm.npcTypeName || !editForm.npcSexName) {
    editError.value = 'Choose an NPC type and sex.'
    return
  }

  saving.value = true
  editError.value = undefined
  try {
    await updateNpcDefinition(npc.id, {
      name,
      level: editForm.level,
      npcTypeName: editForm.npcTypeName,
      npcRaceName: editForm.npcRaceName,
      npcSexName: editForm.npcSexName
    })
    editOpen.value = false
    notifications.success({ title: 'NPC definition saved' })
    emit('refresh')
  } catch {
    editError.value = 'The NPC definition could not be saved. Check the selected lookup values and try again.'
  } finally {
    saving.value = false
  }
}

async function remove(npc: NpcRecord) {
  const confirmed = await dialogs.confirm({
    title: `Delete ${npc.name ?? `NPC #${npc.id}`} ?`,
    description: `NPC #${npc.id} and its definition-owned status and statistics will be permanently removed. A later import can restore the source record.`,
    confirmLabel: 'Delete NPC',
    confirmColor: 'error'
  })
  if (!confirmed) return
  deletingId.value = npc.id
  try {
    await deleteNpcDefinition(npc.id)
    notifications.success({ title: 'NPC definition deleted' })
    emit('refresh')
  } catch {
    notifications.error({ title: 'NPC definition could not be deleted' })
  } finally {
    deletingId.value = undefined
  }
}

onMounted(() => {
  void loadLatestImports()
  void loadLookups()
})
onUnmounted(() => clearTimeout(pollTimer))
</script>

<template>
  <StudioContentDirectoryLayout
      title="NPC definitions"
      description="Browse normalized NPC records and the lookup values that classify their server behavior."
      icon="i-lucide-users-round"
      import-target="npcs"
      import-label="NPCs"
      :loading="loading"
      :error="error"
      @refresh="emit('refresh')"
    >
      <template #actions>
        <UButton
          v-if="isC1"
          label="Import NPC appearances"
          icon="i-lucide-boxes"
          color="neutral"
          variant="outline"
          :loading="queueingAppearances"
          :disabled="queueingAppearances"
          @click="queueAppearanceImport"
        />
      </template>
      <template #alerts>
        <UAlert v-if="importStatusError" color="error" variant="subtle" title="NPC appearance import unavailable" :description="importStatusError" />
      </template>

    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <StudioDataTable
        v-model:query="query"
        v-model:filter-values="tableFilterValues"
        v-model:page="page"
        v-model:page-size="pageSize"
        :data="items"
        :total="total"
        :columns="columns"
        :filters="tableFilters"
        :loading="loading"
        empty="No NPC definitions match this search."
        search-placeholder="Search NPC name"
        search-aria-label="Search NPC name"
        :page-size-options="[10, 25, 50, 100]"
        table-class="min-w-[58rem]"
      >
        <template #toolbar-start>
          <div>
            <p class="text-sm font-medium text-highlighted">NPC catalog</p>
            <p class="text-xs text-muted">{{ total.toLocaleString() }} definitions</p>
            <p v-if="lookupsError" class="mt-1 text-xs text-error">{{ lookupsError }}</p>
          </div>
        </template>
          <template #id-cell="{ row }">
            <code class="text-xs text-muted">{{ row.original.id }}</code>
          </template>
          <template #name-cell="{ row }">
            <div class="flex items-center gap-3">
              <span
                class="grid size-8 shrink-0 place-items-center rounded-lg bg-elevated"
              >
                <UIcon name="i-lucide-user-round" class="size-4 text-muted" />
              </span>
              <span class="font-medium text-highlighted">
                {{ row.original.name ?? 'Unnamed NPC' }}
              </span>
            </div>
          </template>
          <template #level-cell="{ row }">
            <UBadge color="neutral" variant="subtle">
              {{ row.original.level }}
            </UBadge>
          </template>
          <template #hasVisuals-cell="{ row }">
            <UBadge v-if="row.original.hasVisuals" color="success" variant="subtle" icon="i-lucide-image">
              Appearance
            </UBadge>
            <span v-else class="text-sm text-dimmed">—</span>
          </template>
          <template #npcType-cell="{ row }">
            <span class="text-sm">{{ row.original.npcTypeDisplayName }}</span>
          </template>
          <template #npcRace-cell="{ row }">
            <span class="text-sm">{{ row.original.npcRaceDisplayName ?? 'No race' }}</span>
          </template>
          <template #npcSex-cell="{ row }">
            <span class="text-sm">{{ row.original.npcSexDisplayName }}</span>
          </template>
          <template #actions-cell="{ row }">
            <StudioTableRowActions
              :view-to="`/authoring/npcs/${row.original.id}`"
              :show-edit="true"
              :show-delete="true"
              :delete-loading="deletingId === row.original.id"
              @edit="edit(row.original)"
              @delete="remove(row.original)"
            />
          </template>
      </StudioDataTable>
    </UCard>

    <StudioImportProgressDrawer
      v-model:open="importDrawerOpen"
      :items="progressItems"
    />

    <UModal v-model:open="editOpen" title="Edit NPC definition">
      <template #body>
        <form class="space-y-4" @submit.prevent="saveNpc">
          <UAlert v-if="editError" color="error" variant="subtle" :description="editError" />
          <UFormField label="Name" required>
            <UInput v-model="editForm.name" maxlength="100" class="w-full" />
          </UFormField>
          <UFormField label="Level" required>
            <UInput v-model.number="editForm.level" type="number" min="1" max="255" class="w-full" />
          </UFormField>
          <UFormField label="Type" required>
            <USelect v-model="editForm.npcTypeName" :items="typeOptions" :loading="lookupsLoading" class="w-full" />
          </UFormField>
          <UFormField label="Race">
            <USelect v-model="editForm.npcRaceName" :items="raceOptions" :loading="lookupsLoading" class="w-full" />
          </UFormField>
          <UFormField label="Sex" required>
            <USelect v-model="editForm.npcSexName" :items="sexOptions" :loading="lookupsLoading" class="w-full" />
          </UFormField>
          <div class="flex justify-end gap-3 pt-2">
            <UButton label="Cancel" color="neutral" variant="outline" @click="editOpen = false" />
            <UButton type="submit" label="Save changes" icon="i-lucide-save" :loading="saving" :disabled="lookupsLoading" />
          </div>
        </form>
      </template>
    </UModal>
  </StudioContentDirectoryLayout>
</template>
