<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue'
import { useStudioDialogs } from '../../../composables/use-studio-dialogs'
import {
  getAssetImportJob,
  getAssetImportJobs,
  getNpcLookupImportJob,
  getNpcLookupImportJobs,
  getNpcLookupDirectory,
  startAssetImport,
  updateNpcDefinition,
  startNpcLookupImport
} from '../../../services/studio-api'
import type { NpcLookupRecord, NpcRecord, NpcVisualFilter } from '../../../types/models/content-directory'
import type { AssetImportJob } from '../../../types/models/asset-import-job'
import type { NpcLookupImportMode, NpcLookupImportRun } from '../../../types/models/npc-lookup-import'
import type { UpdateNpcRequest } from '../../../types/requests/update-npc-request'
import { selectedGameVersionKey } from '../../../utils/game-version'
import { assetImportProgressItem, npcLookupImportProgressItem } from '../../../utils/import-progress'
import { npcRaceNoneValue } from '../../../utils/npc-directory'

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
const latestRun = ref<NpcLookupImportRun>()
const latestAppearanceJob = ref<AssetImportJob>()
const queueingMode = ref<NpcLookupImportMode>()
const queueingAppearances = ref(false)
const progressRunId = ref<string>()
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

const activeRun = computed(() => latestRun.value
  ? ['queued', 'running'].includes(latestRun.value.status)
  : false)
const activeAppearanceJob = computed(() => latestAppearanceJob.value
  ? ['queued', 'discovering', 'running'].includes(latestAppearanceJob.value.status)
  : false)
const hasActiveImport = computed(() => activeRun.value || activeAppearanceJob.value)
const progressItems = computed(() => {
  const items = []
  const run = latestRun.value
  const appearanceJob = latestAppearanceJob.value
  if (run && run.id === progressRunId.value)
    items.push(npcLookupImportProgressItem(run, 'NPC definitions'))
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
const hasFilters = computed(() => Boolean(
  npcTypeName.value || npcRaceName.value || npcSexName.value || visualFilter.value
))

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
    const [runs, appearanceJobs] = await Promise.all([
      getNpcLookupImportJobs('npcs', 1),
      getAssetImportJobs('npcappearances', 1)
    ])
    const latest = runs[0]
    const latestAppearance = appearanceJobs[0]
    latestRun.value = latest
    latestAppearanceJob.value = latestAppearance
    if (latest && ['queued', 'running'].includes(latest.status) && latest.id !== progressRunId.value) {
      progressRunId.value = latest.id
      importDrawerOpen.value = true
    }
    if (latestAppearance && ['queued', 'discovering', 'running'].includes(latestAppearance.status) &&
      latestAppearance.id !== progressAppearanceJobId.value) {
      progressAppearanceJobId.value = latestAppearance.id
      importDrawerOpen.value = true
    }
    importStatusError.value = undefined
    if (schedule && hasActiveImport.value) schedulePoll()
  } catch {
    importStatusError.value = 'NPC import progress could not be loaded.'
  }
}

function schedulePoll() {
  clearTimeout(pollTimer)
  pollTimer = setTimeout(() => void pollRun(), 1000)
}

async function pollRun() {
  if (!isC1 || !hasActiveImport.value) return
  const run = activeRun.value ? latestRun.value : undefined
  const appearanceJob = activeAppearanceJob.value ? latestAppearanceJob.value : undefined
  try {
    const [nextRun, nextAppearanceJob] = await Promise.all([
      run ? getNpcLookupImportJob('npcs', run.id) : Promise.resolve(undefined),
      appearanceJob
        ? getAssetImportJob('npcappearances', appearanceJob.id)
        : Promise.resolve(undefined)
    ])
    if (nextRun) {
      latestRun.value = nextRun
      if (nextRun.status === 'succeeded') emit('refresh')
    }
    if (nextAppearanceJob) latestAppearanceJob.value = nextAppearanceJob
    if (hasActiveImport.value) schedulePoll()
  } catch {
    importStatusError.value = 'Active NPC import progress could not be refreshed.'
  }
}

async function queueImport(mode: NpcLookupImportMode) {
  if (!isC1) return
  if (mode === 'restore_defaults') {
    const confirmed = await dialogs.confirm({
      title: 'Restore default NPC definitions?',
      description: 'Built-in C1 NPC names, levels, types, races, and sexes will be reset to their catalog defaults. Extra NPC records will be preserved.',
      confirmLabel: 'Restore defaults',
      confirmColor: 'warning'
    })
    if (!confirmed) return
  }
  queueingMode.value = mode
  importStatusError.value = undefined
  try {
    latestRun.value = await startNpcLookupImport('npcs', mode)
    progressRunId.value = latestRun.value.id
    importDrawerOpen.value = true
    schedulePoll()
  } catch {
    notifications.error({
      title: mode === 'restore_defaults'
        ? 'NPC defaults could not be restored'
        : 'NPC import could not be queued',
      description: 'Import NPC types, races, and sexes first, then try again.'
    })
  } finally {
    queueingMode.value = undefined
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
      getNpcLookupDirectory('npc-types'),
      getNpcLookupDirectory('npc-races'),
      getNpcLookupDirectory('npc-sexes')
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

function clearFilters() {
  npcTypeName.value = undefined
  npcRaceName.value = undefined
  npcSexName.value = undefined
  visualFilter.value = undefined
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

onMounted(() => {
  void loadLatestImports()
  void loadLookups()
})
onUnmounted(() => clearTimeout(pollTimer))
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Game content"
      title="NPC definitions"
      description="Browse normalized NPC records and the lookup values that classify their server behavior."
      icon="i-lucide-users-round"
    >
      <template #actions>
        <UButton
          v-if="isC1"
          label="Import missing NPCs"
          icon="i-lucide-play"
          :loading="queueingMode === 'add_missing'"
          :disabled="Boolean(activeRun) || Boolean(queueingMode)"
          @click="queueImport('add_missing')"
        />
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
        <UButton
          v-if="isC1"
          label="Restore defaults"
          icon="i-lucide-rotate-ccw"
          color="warning"
          variant="outline"
          :loading="queueingMode === 'restore_defaults'"
          :disabled="Boolean(activeRun) || Boolean(queueingMode)"
          @click="queueImport('restore_defaults')"
        />
        <UButton
          label="Refresh"
          icon="i-lucide-refresh-cw"
          color="neutral"
          variant="outline"
          :loading="loading"
          @click="emit('refresh')"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="error || importStatusError"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="NPC directory unavailable"
      :description="error ?? importStatusError"
    >
      <template #actions>
        <UButton color="error" variant="soft" size="sm" @click="emit('refresh')">
          Try again
        </UButton>
      </template>
    </UAlert>

    <UCard v-else :ui="{ body: 'p-0 sm:p-0' }">
      <div class="space-y-3 border-b border-default px-4 py-3">
        <div class="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p class="text-sm font-medium text-highlighted">NPC catalog</p>
            <p class="text-xs text-muted">
              {{ total.toLocaleString() }} definitions
            </p>
          </div>
          <UInput
            v-model="query"
            icon="i-lucide-search"
            placeholder="Search NPC name"
            aria-label="Search NPC name"
            maxlength="100"
            class="w-full sm:w-80"
          />
        </div>
        <div class="flex flex-wrap items-center gap-2">
          <USelect
            v-model="npcTypeName"
            :items="typeFilterOptions"
            :loading="lookupsLoading"
            placeholder="All types"
            class="w-full sm:w-44"
            aria-label="Filter by NPC type"
          />
          <USelect
            v-model="npcRaceName"
            :items="raceFilterOptions"
            :loading="lookupsLoading"
            placeholder="All races"
            class="w-full sm:w-44"
            aria-label="Filter by NPC race"
          />
          <USelect
            v-model="npcSexName"
            :items="sexFilterOptions"
            :loading="lookupsLoading"
            placeholder="All sexes"
            class="w-full sm:w-40"
            aria-label="Filter by NPC sex"
          />
          <USelect
            v-model="visualFilter"
            :items="visualFilterOptions"
            placeholder="All appearances"
            class="w-full sm:w-48"
            aria-label="Filter by appearance availability"
          />
          <UButton
            v-if="hasFilters"
            label="Clear filters"
            color="neutral"
            variant="ghost"
            size="sm"
            @click="clearFilters"
          />
        </div>
        <p v-if="lookupsError" class="text-xs text-error">{{ lookupsError }}</p>
      </div>

      <div class="overflow-x-auto">
        <UTable
          :data="items"
          :columns="columns"
          :loading="loading"
          empty="No NPC definitions match this search."
          class="min-w-[58rem]"
        >
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
            <div class="flex justify-end gap-1">
              <UButton
                label="Edit"
                icon="i-lucide-pencil"
                color="neutral"
                variant="ghost"
                size="sm"
                @click="edit(row.original)"
              />
              <UButton
                label="View"
                icon="i-lucide-arrow-up-right"
                color="neutral"
                variant="ghost"
                size="sm"
                :to="`/authoring/npcs/${row.original.id}`"
              />
            </div>
          </template>
        </UTable>
      </div>

      <StudioTableFooter
        v-model:page="page"
        v-model:page-size="pageSize"
        :total="total"
        :page-size-options="[10, 25, 50, 100]"
      />
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
  </div>
</template>
