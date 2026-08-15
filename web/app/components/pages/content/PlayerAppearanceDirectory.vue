<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { getPlayerAppearanceDirectory } from '../../../services/studio-api'
import type { PlayerAppearanceKind, PlayerAppearanceRecord } from '../../../types/models/content-directory'

const props = defineProps<{
  kind: PlayerAppearanceKind
  title: string
  description: string
  icon: string
}>()

const records = ref<PlayerAppearanceRecord[]>([])
const loading = ref(true)
const error = ref<string>()
const columns: TableColumn<PlayerAppearanceRecord>[] = [
  { accessorKey: 'id', header: 'ID' },
  { accessorKey: 'name', header: 'Option' },
  { accessorKey: 'playerRaceName', header: 'Race' },
  { accessorKey: 'playerSexName', header: 'Sex' }
]

async function load() {
  loading.value = true
  error.value = undefined
  try {
    records.value = await getPlayerAppearanceDirectory(props.kind)
  } catch {
    error.value = 'The player appearance directory could not be loaded from the Studio API.'
  } finally {
    loading.value = false
  }
}

watch(() => props.kind, () => void load(), { immediate: true })
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader :title="title" :description="description" :icon="icon">
      <template #actions>
        <UButton label="Refresh" icon="i-lucide-refresh-cw" color="neutral" variant="outline" :loading="loading" @click="load" />
      </template>
    </StudioPageHeader>
    <PlayerImportActions />
    <UAlert v-if="error" color="error" variant="subtle" title="Player appearance directory unavailable" :description="error" />
    <UCard v-else :ui="{ body: 'p-0 sm:p-0' }">
      <UTable :data="records" :columns="columns" :loading="loading" empty="No player appearance options are available." />
    </UCard>
  </div>
</template>
