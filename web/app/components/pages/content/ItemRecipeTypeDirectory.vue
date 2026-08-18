<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import type { ItemRecipeTypeRecord } from '~/types/models/item-recipe'

const props = defineProps<{ items: ItemRecipeTypeRecord[]; total: number; loading: boolean; error?: string }>()
const query = defineModel<string>('query', { required: true })
const page = defineModel<number>('page', { required: true })
const pageSize = defineModel<number>('pageSize', { required: true })
const emit = defineEmits<{ refresh: [] }>()

const columns: TableColumn<ItemRecipeTypeRecord>[] = [
  { accessorKey: 'name', header: 'Recipe type' },
  { accessorKey: 'recipeCount', header: 'Recipes' }
]
</script>

<template>
  <StudioContentDirectoryLayout
    title="Recipe types"
    description="Crafting classifications available in the selected game-version recipe catalog."
    icon="i-lucide-tags"
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
        :data="props.items"
        :total="props.total"
        :columns="columns"
        :loading="props.loading"
        empty="No recipe types match this search."
        search-placeholder="Search recipe types"
        search-aria-label="Search recipe types"
        :page-size-options="[10, 25, 50]"
      >
        <template #toolbar-start>
          <div>
            <p class="text-sm font-medium text-highlighted">Recipe-type catalog</p>
            <p class="text-xs text-muted">{{ props.total.toLocaleString() }} types</p>
          </div>
        </template>
        <template #recipeCount-cell="{ row }"><span class="text-sm text-highlighted">{{ row.original.recipeCount.toLocaleString() }}</span></template>
        <template #mobile="{ rows }">
          <div class="divide-y divide-default">
            <article v-for="type in rows" :key="type.name" class="flex items-center justify-between gap-3 px-4 py-3"><span class="font-medium text-highlighted">{{ type.name }}</span><span class="text-sm text-muted">{{ type.recipeCount.toLocaleString() }} recipes</span></article>
          </div>
        </template>
      </StudioDataTable>
    </UCard>
  </StudioContentDirectoryLayout>
</template>
