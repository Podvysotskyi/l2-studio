<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { getItemLookups, updateItemLookupDisplayName } from '../../../services/studio-api'
import type { ItemLookupKind, ItemLookupRecord } from '../../../types/models/item'

const props = defineProps<{ kind: ItemLookupKind; title: string }>()
const items = ref<ItemLookupRecord[]>([])
const dialogs = useStudioDialogs()
const loading = ref(true)
const error = ref<string>()
const columns: TableColumn<ItemLookupRecord>[] = [{ accessorKey: 'name', header: 'Canonical name' }, { accessorKey: 'displayName', header: 'Display name' }]
async function load() { loading.value = true; try { items.value = await getItemLookups(props.kind); error.value = undefined } catch { error.value = 'The lookup values could not be loaded.' } finally { loading.value = false } }
async function edit(row: ItemLookupRecord) {
  const displayName = await dialogs.prompt({ title: `Edit ${props.title}`, label: 'Display name', initialValue: row.displayName })
  if (!displayName?.trim()) return
  await updateItemLookupDisplayName(props.kind, row.name, displayName.trim())
  await load()
}
onMounted(() => void load())
</script>

<template>
  <div class="space-y-4"><UPageHeader :title="title" description="Canonical values imported from the C1 item catalogue." /><UAlert v-if="error" color="error" :description="error" /><UTable :data="items" :columns="columns" :loading="loading" @select="(_, row) => edit(row.original)" /></div>
</template>
