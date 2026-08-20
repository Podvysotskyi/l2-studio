<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { getExpandedRowModel } from '@tanstack/vue-table'
import { updateItemSet } from '~/services/studio-api'
import type { ItemSetRecord } from '~/types/models/item-set'

const props = defineProps<{ items: ItemSetRecord[]; total: number; loading: boolean; error?: string }>()
const query = defineModel<string>('query', { required: true })
const page = defineModel<number>('page', { required: true })
const pageSize = defineModel<number>('pageSize', { required: true })
const emit = defineEmits<{ refresh: [] }>()
const toasts = useStudioToasts()
const editorOpen = ref(false)
const saving = ref(false)
const {
  pageError: editorError,
  capture: captureEditorError,
  clear: clearEditorError,
  fieldError: editorFieldError,
  set: setEditorError
} = useStudioApiError()
const selected = ref<ItemSetRecord>()
const expanded = ref({})
const form = reactive({
  str: null as number | null,
  dex: null as number | null,
  con: null as number | null,
  int: null as number | null,
  wit: null as number | null,
  men: null as number | null
})

const columns: TableColumn<ItemSetRecord>[] = [
  { id: 'expand', header: '' },
  { accessorKey: 'setId', header: 'Set' },
  { accessorKey: 'bodyParts', header: 'Required equipment' },
  { id: 'actions', header: '' }
]
const statFields: Array<[keyof Pick<typeof form, 'str' | 'dex' | 'con' | 'int' | 'wit' | 'men'>, string]> = [
  ['str', 'STR'], ['dex', 'DEX'], ['con', 'CON'], ['int', 'INT'], ['wit', 'WIT'], ['men', 'MEN']
]

function openEditor(itemSet: ItemSetRecord) {
  selected.value = itemSet
  Object.assign(form, {
    str: itemSet.stats?.str ?? null, dex: itemSet.stats?.dex ?? null, con: itemSet.stats?.con ?? null,
    int: itemSet.stats?.int ?? null, wit: itemSet.stats?.wit ?? null, men: itemSet.stats?.men ?? null
  })
  clearEditorError()
  editorOpen.value = true
}

async function save() {
  const itemSet = selected.value
  if (!itemSet?.skill) {
    setEditorError('This item set has no skill to preserve.', {
      skill: ['This item set has no skill to preserve.']
    })
    return
  }
  saving.value = true
  clearEditorError()
  try {
    await updateItemSet(itemSet.setId, {
      skillId: itemSet.skill.skillId,
      skillLevel: itemSet.skill.skillLevel,
      ...form
    })
    editorOpen.value = false
    toasts.success({ title: `Item set #${itemSet.setId} saved` })
    emit('refresh')
  } catch (cause) {
    captureEditorError(cause, 'The item set could not be saved. Check the selected skill and level.')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <StudioContentDirectoryLayout
    title="Item sets"
    description="C1 armor-set equipment requirements and their set effects."
    icon="i-lucide-shield-check"
    import-target="item-sets"
    :loading="loading"
    :error="error"
    @refresh="emit('refresh')"
  >
    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <StudioDataTable
        v-model:query="query"
        v-model:page="page"
        v-model:page-size="pageSize"
        :data="props.items"
        :total="props.total"
        :columns="columns"
        :loading="props.loading"
        v-model:expanded="expanded"
        :get-expanded-row-model="getExpandedRowModel()"
        :get-row-can-expand="() => true"
        empty="No item sets match this search."
        search-placeholder="Search set ID or equipped item"
        search-aria-label="Search item sets"
        :page-size-options="[10, 25, 50, 100]"
        table-class="min-w-[54rem]"
      >
        <template #toolbar-start>
          <div>
            <p class="text-sm font-medium text-highlighted">Item-set catalog</p>
            <p class="text-xs text-muted">{{ props.total.toLocaleString() }} sets</p>
          </div>
        </template>
        <template #setId-cell="{ row }">
          <code class="text-xs text-muted">#{{ row.original.setId }}</code>
        </template>
        <template #expand-cell="{ row }">
          <UButton
            :icon="row.getIsExpanded() ? 'i-lucide-chevron-down' : 'i-lucide-chevron-right'"
            color="neutral"
            variant="ghost"
            size="sm"
            :aria-label="`${row.getIsExpanded() ? 'Collapse' : 'Expand'} item set #${row.original.setId}`"
            @click="row.toggleExpanded()"
          />
        </template>
        <template #bodyParts-cell="{ row }">
          <span class="text-sm text-highlighted">{{ row.original.bodyParts.length }} required items</span>
        </template>
        <template #actions-cell="{ row }">
          <StudioTableRowActions
            :view-to="`/authoring/items/sets/${row.original.setId}`"
            :show-edit="true"
            edit-label="Edit effect"
            @edit="openEditor(row.original)"
          />
        </template>
        <template #expanded="{ row }">
          <div class="bg-muted/30 px-4 py-3">
            <table class="w-full text-left text-sm">
              <thead class="text-xs text-muted">
                <tr>
                  <th class="pb-2 font-medium">Item ID</th>
                  <th class="pb-2 font-medium">Item name</th>
                  <th class="pb-2 font-medium">Body part</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-default">
                <tr v-for="part in row.original.bodyParts" :key="part.bodyPartName">
                  <td class="py-2 pr-4"><code class="text-xs text-muted">#{{ part.itemId }}</code></td>
                  <td class="py-2 pr-4">
                    <NuxtLink v-if="part.itemName" :to="`/authoring/items/armor/${part.itemId}`" class="text-primary hover:underline">{{ part.itemName }}</NuxtLink>
                    <span v-else class="text-muted">Unavailable source item</span>
                  </td>
                  <td class="py-2">{{ part.bodyPartDisplayName }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </template>
      </StudioDataTable>
    </UCard>

    <UModal v-model:open="editorOpen" :title="selected ? `Edit item set #${selected.setId}` : 'Edit item set'">
      <template #body>
        <form class="space-y-5" @submit.prevent="save">
          <UAlert v-if="editorError" color="error" variant="subtle" :description="editorError" />
          <div class="grid gap-3 sm:grid-cols-2">
            <UFormField v-for="[key, label] in statFields" :key="key" :label="label" :error="editorFieldError('skill')"><UInput v-model.number="form[key]" type="number" placeholder="No modifier" /></UFormField>
          </div>
          <div class="flex justify-end gap-3">
            <UButton label="Cancel" color="neutral" variant="outline" :disabled="saving" @click="editorOpen = false" />
            <UButton type="submit" label="Save effect" :loading="saving" />
          </div>
        </form>
      </template>
    </UModal>
  </StudioContentDirectoryLayout>
</template>
