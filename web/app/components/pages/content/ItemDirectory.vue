<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { getItemImportRuns, startItemImport } from '../../../services/studio-api'
import type { ItemImportMode, ItemImportRun, ItemRecord } from '../../../types/models/item'

const props = defineProps<{ items: ItemRecord[]; total: number; loading: boolean; error?: string }>()
const query = defineModel<string>('query', { required: true })
const page = defineModel<number>('page', { required: true })
const pageSize = defineModel<number>('pageSize', { required: true })
const emit = defineEmits<{ refresh: [] }>()
const run = ref<ItemImportRun>()
const importing = ref<ItemImportMode>()
const toast = useStudioToasts()
const columns: TableColumn<ItemRecord>[] = [
  { accessorKey: 'id', header: 'ID' }, { accessorKey: 'name', header: 'Name' },
  { accessorKey: 'itemTypeDisplayName', header: 'Type' }, { accessorKey: 'price', header: 'Price' },
  { accessorKey: 'weight', header: 'Weight' }
]
async function importItems(mode: ItemImportMode) {
  importing.value = mode
  try { run.value = await startItemImport(mode); toast.success({ title: 'Item import queued' }) }
  catch { toast.error({ title: 'Item import could not be queued' }) }
  finally { importing.value = undefined }
}
async function loadRun() { try { run.value = (await getItemImportRuns())[0] } catch {} }
onMounted(() => void loadRun())
</script>

<template>
  <div class="space-y-4">
    <UPageHeader title="Item definitions" description="C1 Mobius item catalogue and combat statistics.">
      <template #links>
        <UButton :loading="importing === 'add_missing'" icon="i-lucide-download" @click="importItems('add_missing')">Import missing</UButton>
        <UButton :loading="importing === 'restore_defaults'" variant="soft" @click="importItems('restore_defaults')">Restore defaults</UButton>
      </template>
    </UPageHeader>
    <UAlert v-if="run" :color="run.status === 'failed' ? 'error' : 'neutral'" :title="`Latest import: ${run.status}`" :description="run.error || `${run.insertedCount} inserted, ${run.restoredCount} restored of ${run.totalCount}.`" />
    <UAlert v-if="error" color="error" title="Items unavailable" :description="error" />
    <div class="flex gap-3"><UInput v-model="query" class="max-w-md" icon="i-lucide-search" placeholder="Search item names" /><UButton variant="soft" @click="emit('refresh')">Refresh</UButton></div>
    <UTable :data="props.items" :columns="columns" :loading="loading" @select="(_, row) => navigateTo(`/authoring/items/${row.original.id}`)" />
    <UPagination v-model:page="page" v-model:page-size="pageSize" :total="total" />
  </div>
</template>
