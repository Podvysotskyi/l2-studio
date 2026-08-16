<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { computed, ref, watch } from 'vue'
import { deleteItemLookup, updateItemLookupDisplayName } from '../../../services/studio-api'
import type { ItemLookupRecord } from '../../../types/models/item'
import {
  buildItemTypeHierarchy,
  flattenItemTypeHierarchy,
  type ItemTypeNode
} from '../../../utils/item-type'

const props = defineProps<{
  records: ItemLookupRecord[]
  loading: boolean
  error?: string
}>()

const emit = defineEmits<{ refresh: [] }>()
const dialogs = useStudioDialogs()
const notifications = useStudioToasts()
const query = ref('')
const expandedNames = ref<Set<string>>(new Set())
const deletingName = ref<string>()

const columns: TableColumn<ItemTypeNode>[] = [
  { accessorKey: 'displayName', header: 'Item type' },
  { accessorKey: 'name', header: 'Canonical name' },
  { accessorKey: 'parentDisplayName', header: 'Parent type' },
  { id: 'subtypes', header: 'Direct subtypes' },
  { id: 'actions', header: '' }
]
const roots = computed(() => buildItemTypeHierarchy(props.records))
const visibleRows = computed(() =>
  flattenItemTypeHierarchy(roots.value, expandedNames.value, query.value)
)
const searching = computed(() => query.value.trim().length > 0)
const matchCount = computed(() => {
  const term = query.value.trim().toLocaleLowerCase()
  if (!term) return props.records.length
  return props.records.filter(record =>
    [record.name, record.displayName, record.parentTypeName, record.parentTypeDisplayName]
      .some(value => value?.toLocaleLowerCase().includes(term))
  ).length
})

watch(
  roots,
  nextRoots => {
    if (expandedNames.value.size === 0) {
      expandedNames.value = new Set(nextRoots.map(root => root.name))
    }
  },
  { immediate: true }
)

function isExpanded(node: ItemTypeNode): boolean {
  return searching.value || expandedNames.value.has(node.name)
}

function toggleNode(node: ItemTypeNode) {
  if (searching.value) return
  const next = new Set(expandedNames.value)
  if (next.has(node.name)) next.delete(node.name)
  else next.add(node.name)
  expandedNames.value = next
}

function expandAll() {
  const names = new Set<string>()
  const visit = (node: ItemTypeNode) => {
    if (node.children.length > 0) names.add(node.name)
    for (const child of node.children) visit(child)
  }
  for (const root of roots.value) visit(root)
  expandedNames.value = names
}

function collapseAll() {
  expandedNames.value = new Set()
}

async function edit(node: ItemTypeNode) {
  const displayName = await dialogs.prompt({
    title: `Edit ${node.displayName}`,
    label: 'Display name',
    initialValue: node.displayName
  })
  if (!displayName?.trim()) return
  try {
    await updateItemLookupDisplayName('item-types', node.name, displayName.trim())
    notifications.success({ title: 'Item type saved' })
    emit('refresh')
  } catch {
    notifications.error({ title: 'Item type could not be saved' })
  }
}

async function remove(node: ItemTypeNode) {
  const confirmed = await dialogs.confirm({
    title: `Delete ${node.displayName}?`,
    description: 'This permanently removes the item type. Delete or reassign its subtypes and item definitions first.',
    confirmLabel: 'Delete item type',
    confirmColor: 'error'
  })
  if (!confirmed) return
  deletingName.value = node.name
  try {
    await deleteItemLookup('item-types', node.name)
    notifications.success({ title: 'Item type deleted' })
    emit('refresh')
  } catch {
    notifications.error({ title: 'Item type could not be deleted', description: 'It may still be used by item definitions or subtypes.' })
  } finally {
    deletingName.value = undefined
  }
}
</script>

<template>
  <StudioContentDirectoryLayout
    title="Item types"
    description="Explore the canonical item classification hierarchy used by item definitions."
    icon="i-lucide-workflow"
    import-target="item-types"
    import-label="item types"
    :loading="loading"
    :error="error"
    @refresh="emit('refresh')"
  >
    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <div class="flex flex-wrap items-center justify-between gap-4 border-b border-default px-4 py-3">
        <div>
          <p class="text-sm font-medium text-highlighted">Type hierarchy</p>
          <p class="text-xs text-muted">
            <template v-if="searching">
              {{ matchCount }} matches · {{ visibleRows.length }} rows including ancestors
            </template>
            <template v-else>
              {{ records.length }} types · {{ roots.length }} root types
            </template>
          </p>
        </div>
        <div class="flex w-full flex-wrap items-center gap-2 sm:w-auto">
          <UButton label="Expand all" icon="i-lucide-chevrons-down-up" color="neutral" variant="ghost" size="sm" :disabled="searching || loading" @click="expandAll" />
          <UButton label="Collapse all" icon="i-lucide-chevrons-up-down" color="neutral" variant="ghost" size="sm" :disabled="searching || loading" @click="collapseAll" />
          <UInput v-model="query" icon="i-lucide-search" placeholder="Search item type or canonical name" aria-label="Search item types" class="w-full sm:w-72" />
        </div>
      </div>

      <StudioDataTable
        :data="visibleRows"
        :columns="columns"
        :loading="loading"
        pagination-mode="none"
        empty="No item types match this search."
        table-class="min-w-[56rem]"
      >
        <template #displayName-cell="{ row }">
          <div class="flex items-center gap-2" :style="{ paddingLeft: `${row.original.depth * 1.5}rem` }">
            <UButton
              v-if="row.original.children.length > 0"
              :icon="isExpanded(row.original) ? 'i-lucide-chevron-down' : 'i-lucide-chevron-right'"
              color="neutral"
              variant="ghost"
              size="xs"
              :disabled="searching"
              :aria-label="`${isExpanded(row.original) ? 'Collapse' : 'Expand'} ${row.original.displayName}`"
              :aria-expanded="isExpanded(row.original)"
              @click="toggleNode(row.original)"
            />
            <span v-else class="block size-7 shrink-0" aria-hidden="true" />
            <span class="grid size-8 shrink-0 place-items-center rounded-lg bg-elevated">
              <UIcon :name="row.original.depth === 0 ? 'i-lucide-layers-3' : 'i-lucide-tag'" class="size-4 text-muted" />
            </span>
            <span class="font-medium text-highlighted">{{ row.original.displayName }}</span>
          </div>
        </template>
        <template #name-cell="{ row }"><code class="text-xs text-muted">{{ row.original.name }}</code></template>
        <template #parentDisplayName-cell="{ row }">
          <span :class="row.original.parentDisplayName ? '' : 'text-muted'">{{ row.original.parentDisplayName ?? 'Root type' }}</span>
        </template>
        <template #subtypes-cell="{ row }"><span class="text-sm">{{ row.original.children.length }}</span></template>
        <template #actions-cell="{ row }">
          <StudioTableRowActions :show-edit="true" :show-delete="true" :delete-loading="deletingName === row.original.name" @edit="edit(row.original)" @delete="remove(row.original)" />
        </template>
      </StudioDataTable>
    </UCard>
  </StudioContentDirectoryLayout>
</template>
