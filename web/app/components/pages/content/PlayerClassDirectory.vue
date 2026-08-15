<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { computed, ref, watch } from 'vue'
import type {
  PlayerClassNode,
  PlayerClassRecord
} from '../../../types/models/content-directory'
import {
  buildPlayerClassHierarchy,
  flattenPlayerClassHierarchy
} from '../../../utils/player-class'
import { deletePlayerClass, updatePlayerClass } from '../../../services/studio-api'

const props = defineProps<{
  records: PlayerClassRecord[]
  loading: boolean
  error?: string
}>()

const emit = defineEmits<{ refresh: [] }>()
const dialogs = useStudioDialogs()
const notifications = useStudioToasts()
const selectedClass = ref<PlayerClassNode>()
const editOpen = ref(false)
const saving = ref(false)
const deletingId = ref<number>()
const editError = ref<string>()
const editForm = reactive({ name: '', isMage: false, parentClassId: undefined as number | undefined })

const query = ref('')
const expandedIds = ref<Set<number>>(new Set())

const columns: TableColumn<PlayerClassNode>[] = [
  { accessorKey: 'name', header: 'Player class' },
  { accessorKey: 'id', header: 'ID' },
  { accessorKey: 'stage', header: 'Stage' },
  { accessorKey: 'isMage', header: 'Archetype' },
  { id: 'availability', header: 'Race and sex' },
  { accessorKey: 'parentName', header: 'Parent class' },
  { id: 'subclasses', header: 'Direct subclasses' },
  { id: 'actions', header: '' }
]
const roots = computed(() => buildPlayerClassHierarchy(props.records))
const visibleRows = computed(() =>
  flattenPlayerClassHierarchy(roots.value, expandedIds.value, query.value)
)
const searching = computed(() => query.value.trim().length > 0)
const parentOptions = computed(() => [
  { label: 'No parent class', value: undefined },
  ...props.records
    .filter(record => record.id !== selectedClass.value?.id)
    .map(record => ({ label: `${record.name} (${record.id})`, value: record.id }))
])
const matchCount = computed(() => {
  const term = query.value.trim().toLocaleLowerCase()
  if (!term) return props.records.length
  return props.records.filter(
    record =>
      record.name.toLocaleLowerCase().includes(term) ||
      String(record.id).includes(term) ||
      record.allowedRaces.some(
        race =>
          race.name.toLocaleLowerCase().includes(term) ||
          race.allowedSexes.some(sex =>
            sex.name.toLocaleLowerCase().includes(term)
          )
      )
  ).length
})

watch(
  roots,
  nextRoots => {
    if (expandedIds.value.size === 0) {
      expandedIds.value = new Set(nextRoots.map(root => root.id))
    }
  },
  { immediate: true }
)

function isExpanded(node: PlayerClassNode): boolean {
  return searching.value || expandedIds.value.has(node.id)
}

function toggleNode(node: PlayerClassNode) {
  if (searching.value) return
  const next = new Set(expandedIds.value)
  if (next.has(node.id)) next.delete(node.id)
  else next.add(node.id)
  expandedIds.value = next
}

function expandAll() {
  const ids = new Set<number>()
  const visit = (node: PlayerClassNode) => {
    if (node.children.length > 0) ids.add(node.id)
    for (const child of node.children) visit(child)
  }
  for (const root of roots.value) visit(root)
  expandedIds.value = ids
}

function collapseAll() {
  expandedIds.value = new Set()
}

function edit(record: PlayerClassNode) {
  selectedClass.value = record
  editForm.name = record.name
  editForm.isMage = record.isMage
  editForm.parentClassId = record.parentClassId ?? undefined
  editError.value = undefined
  editOpen.value = true
}

async function save() {
  const record = selectedClass.value
  const name = editForm.name.trim()
  if (!record) return
  if (!name || name.length > 64) {
    editError.value = 'Name must contain between 1 and 64 characters.'
    return
  }
  saving.value = true
  editError.value = undefined
  try {
    await updatePlayerClass(record.id, { name, isMage: editForm.isMage, parentClassId: editForm.parentClassId ?? null })
    editOpen.value = false
    notifications.success({ title: 'Player class saved' })
    emit('refresh')
  } catch {
    editError.value = 'Player class could not be saved. The selected parent must be available for every class variant.'
  } finally {
    saving.value = false
  }
}

async function remove(record: PlayerClassNode) {
  const confirmed = await dialogs.confirm({
    title: `Delete ${record.name}?`,
    description: 'This removes every race and sex variant. Delete or reassign child classes first.',
    confirmLabel: 'Delete class',
    confirmColor: 'error'
  })
  if (!confirmed) return
  deletingId.value = record.id
  try {
    await deletePlayerClass(record.id)
    notifications.success({ title: 'Player class deleted' })
    emit('refresh')
  } catch {
    notifications.error({ title: 'Player class could not be deleted', description: 'It may still have child classes.' })
  } finally {
    deletingId.value = undefined
  }
}
</script>

<template>
  <StudioContentDirectoryLayout
      title="Player classes"
      description="Explore the canonical Interlude class progression from base professions through third classes."
      icon="i-lucide-git-branch"
      import-target="player-classes"
      import-label="player classes"
      :loading="loading"
      :error="error"
      @refresh="$emit('refresh')"
    >
    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <div
        class="flex flex-wrap items-center justify-between gap-4 border-b border-default px-4 py-3"
      >
        <div>
          <p class="text-sm font-medium text-highlighted">Class hierarchy</p>
          <p class="text-xs text-muted">
            <template v-if="searching">
              {{ matchCount }} matches · {{ visibleRows.length }} rows including
              ancestors
            </template>
            <template v-else>
              {{ records.length }} classes · {{ roots.length }} base classes
            </template>
          </p>
        </div>
        <div class="flex w-full flex-wrap items-center gap-2 sm:w-auto">
          <UButton
            label="Expand all"
            icon="i-lucide-chevrons-down-up"
            color="neutral"
            variant="ghost"
            size="sm"
            :disabled="searching || loading"
            @click="expandAll"
          />
          <UButton
            label="Collapse all"
            icon="i-lucide-chevrons-up-down"
            color="neutral"
            variant="ghost"
            size="sm"
            :disabled="searching || loading"
            @click="collapseAll"
          />
          <UInput
            v-model="query"
            icon="i-lucide-search"
            placeholder="Search class, race, sex, or ID"
            aria-label="Search player classes"
            class="w-full sm:w-72"
          />
        </div>
      </div>

      <StudioDataTable
          :data="visibleRows"
          :columns="columns"
          :loading="loading"
          pagination-mode="none"
          empty="No player classes match this search."
          table-class="min-w-[64rem]"
        >
          <template #name-cell="{ row }">
            <div
              class="flex items-center gap-2"
              :style="{ paddingLeft: `${row.original.depth * 1.5}rem` }"
            >
              <UButton
                v-if="row.original.children.length > 0"
                :icon="
                  isExpanded(row.original)
                    ? 'i-lucide-chevron-down'
                    : 'i-lucide-chevron-right'
                "
                color="neutral"
                variant="ghost"
                size="xs"
                :disabled="searching"
                :aria-label="`${isExpanded(row.original) ? 'Collapse' : 'Expand'} ${row.original.name}`"
                :aria-expanded="isExpanded(row.original)"
                @click="toggleNode(row.original)"
              />
              <span v-else class="block size-7 shrink-0" aria-hidden="true" />
              <span
                class="grid size-8 shrink-0 place-items-center rounded-lg bg-elevated"
              >
                <UIcon
                  :name="
                    row.original.depth === 0
                      ? 'i-lucide-shield'
                      : 'i-lucide-user-round'
                  "
                  class="size-4 text-muted"
                />
              </span>
              <span class="font-medium text-highlighted">
                {{ row.original.name }}
              </span>
            </div>
          </template>
          <template #id-cell="{ row }">
            <code class="text-xs text-muted">{{ row.original.id }}</code>
          </template>
          <template #stage-cell="{ row }">
            <UBadge color="neutral" variant="subtle" size="sm">
              {{ row.original.stage }}
            </UBadge>
          </template>
          <template #isMage-cell="{ row }">
            <UBadge
              :color="row.original.isMage ? 'info' : 'warning'"
              variant="subtle"
              size="sm"
            >
              {{ row.original.isMage ? 'Mage' : 'Fighter' }}
            </UBadge>
          </template>
          <template #availability-cell="{ row }">
            <div class="flex flex-wrap gap-1.5">
              <UBadge
                v-for="race in row.original.allowedRaces"
                :key="race.id"
                color="neutral"
                variant="outline"
                size="sm"
              >
                {{ race.name }}:
                {{ race.allowedSexes.map((sex) => sex.name).join(', ') }}
              </UBadge>
            </div>
          </template>
          <template #parentName-cell="{ row }">
            <span :class="row.original.parentName ? '' : 'text-muted'">
              {{ row.original.parentName ?? 'Root class' }}
            </span>
          </template>
          <template #subclasses-cell="{ row }">
            <span class="text-sm">
              {{ row.original.children.length }}
            </span>
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
    <UModal v-model:open="editOpen" title="Edit player class">
      <template #body>
        <form class="space-y-4" @submit.prevent="save">
          <UAlert v-if="editError" color="error" variant="subtle" :description="editError" />
          <UFormField label="Name" required><UInput v-model="editForm.name" maxlength="64" class="w-full" /></UFormField>
          <UFormField label="Archetype"><USelect v-model="editForm.isMage" :items="[{ label: 'Fighter', value: false }, { label: 'Mage', value: true }]" class="w-full" /></UFormField>
          <UFormField label="Parent class"><USelect v-model="editForm.parentClassId" :items="parentOptions" class="w-full" /></UFormField>
          <div class="flex justify-end gap-3 pt-2"><UButton label="Cancel" color="neutral" variant="outline" @click="editOpen = false" /><UButton type="submit" label="Save changes" icon="i-lucide-save" :loading="saving" /></div>
        </form>
      </template>
    </UModal>
  </StudioContentDirectoryLayout>
</template>
