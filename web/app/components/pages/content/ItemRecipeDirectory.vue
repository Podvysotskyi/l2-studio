<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { getExpandedRowModel } from '@tanstack/vue-table'
import type { ItemRecipeItemRecord, ItemRecipeRecord } from '~/types/models/item-recipe'

const props = defineProps<{ items: ItemRecipeRecord[]; total: number; loading: boolean; error?: string }>()
const query = defineModel<string>('query', { required: true })
const page = defineModel<number>('page', { required: true })
const pageSize = defineModel<number>('pageSize', { required: true })
const emit = defineEmits<{ refresh: [] }>()
const expanded = ref({})

const columns: TableColumn<ItemRecipeRecord>[] = [
  { id: 'expand', header: '' },
  { accessorKey: 'id', header: 'Recipe' },
  { accessorKey: 'name', header: 'Name' },
  { accessorKey: 'itemRecipeTypeName', header: 'Type' },
  { accessorKey: 'craftLevel', header: 'Craft level' },
  { accessorKey: 'successRate', header: 'Success' },
  { id: 'production', header: 'Produces' },
  { id: 'statUse', header: 'Stat use' }
]

function itemLabel(item: ItemRecipeItemRecord) {
  return item.itemName ? `#${item.itemId} · ${item.itemName}` : `#${item.itemId}`
}

function statUseLabel(recipe: ItemRecipeRecord) {
  const values = [
    recipe.statUse?.mp === null || recipe.statUse?.mp === undefined ? null : `${recipe.statUse.mp} MP`,
    recipe.statUse?.hp === null || recipe.statUse?.hp === undefined ? null : `${recipe.statUse.hp} HP`
  ].filter((value): value is string => value !== null)
  return values.join(', ') || '—'
}
</script>

<template>
  <StudioContentDirectoryLayout
    title="Crafting recipes"
    description="C1 recipe formulas, required materials, produced items, and crafting costs."
    icon="i-lucide-hammer"
    import-target="item-recipes"
    import-label="recipe catalog"
    :loading="loading"
    :error="error"
    @refresh="emit('refresh')"
  >
    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <StudioDataTable
        v-model:query="query"
        v-model:page="page"
        v-model:page-size="pageSize"
        v-model:expanded="expanded"
        :data="props.items"
        :total="props.total"
        :columns="columns"
        :loading="props.loading"
        :get-expanded-row-model="getExpandedRowModel()"
        :get-row-can-expand="() => true"
        empty="No crafting recipes match this search."
        search-placeholder="Search recipe or item ID/name"
        search-aria-label="Search crafting recipes"
        :page-size-options="[10, 25, 50, 100]"
        table-class="min-w-[66rem]"
      >
        <template #toolbar-start>
          <div>
            <p class="text-sm font-medium text-highlighted">Recipe catalog</p>
            <p class="text-xs text-muted">{{ props.total.toLocaleString() }} recipes</p>
          </div>
        </template>
        <template #expand-cell="{ row }">
          <UButton
            :icon="row.getIsExpanded() ? 'i-lucide-chevron-down' : 'i-lucide-chevron-right'"
            color="neutral"
            variant="ghost"
            size="sm"
            :aria-label="`${row.getIsExpanded() ? 'Collapse' : 'Expand'} recipe #${row.original.id}`"
            @click="row.toggleExpanded()"
          />
        </template>
        <template #id-cell="{ row }"><code class="text-xs text-muted">#{{ row.original.id }}</code></template>
        <template #itemRecipeTypeName-cell="{ row }"><UBadge color="neutral" variant="subtle">{{ row.original.itemRecipeTypeName }}</UBadge></template>
        <template #successRate-cell="{ row }">{{ row.original.successRate }}%</template>
        <template #production-cell="{ row }">
          <span class="text-sm text-highlighted">{{ row.original.productions.length }} item{{ row.original.productions.length === 1 ? '' : 's' }}</span>
        </template>
        <template #statUse-cell="{ row }"><span class="text-sm text-muted">{{ statUseLabel(row.original) }}</span></template>
        <template #expanded="{ row }">
          <div class="grid gap-5 bg-muted/30 px-4 py-4 lg:grid-cols-2">
            <section>
              <h3 class="text-sm font-medium text-highlighted">Ingredients</h3>
              <ul class="mt-2 divide-y divide-default rounded-md border border-default bg-default px-3">
                <li v-for="item in row.original.ingredients" :key="item.itemId" class="flex items-center justify-between gap-3 py-2 text-sm">
                  <span>{{ itemLabel(item) }}</span><span class="text-muted">×{{ item.count }}</span>
                </li>
              </ul>
            </section>
            <section>
              <h3 class="text-sm font-medium text-highlighted">Production</h3>
              <ul class="mt-2 divide-y divide-default rounded-md border border-default bg-default px-3">
                <li v-for="item in row.original.productions" :key="item.itemId" class="flex items-center justify-between gap-3 py-2 text-sm">
                  <span>{{ itemLabel(item) }}</span><span class="text-muted">×{{ item.count }}</span>
                </li>
              </ul>
            </section>
          </div>
        </template>
        <template #mobile="{ rows }">
          <div class="divide-y divide-default">
            <article v-for="recipe in rows" :key="recipe.id" class="space-y-2 px-4 py-3">
              <div class="flex items-start justify-between gap-3"><div><code class="text-xs text-muted">#{{ recipe.id }}</code><p class="font-medium text-highlighted">{{ recipe.name }}</p></div><span class="text-sm text-muted">{{ recipe.successRate }}%</span></div>
              <div class="flex flex-wrap gap-2 text-xs text-muted"><UBadge color="neutral" variant="subtle">{{ recipe.itemRecipeTypeName }}</UBadge><span>Level {{ recipe.craftLevel }}</span><span>{{ recipe.productions.length }} produced</span><span>{{ statUseLabel(recipe) }}</span></div>
            </article>
          </div>
        </template>
      </StudioDataTable>
    </UCard>
  </StudioContentDirectoryLayout>
</template>
