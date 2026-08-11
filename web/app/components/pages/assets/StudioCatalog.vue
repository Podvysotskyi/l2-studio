<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { computed, watch } from 'vue'
import { useLookupDirectoryStore } from '../../../stores/lookup-directory'
import type {
  LookupKind,
  LookupRecord
} from '../../../types/models/content-directory'
import { paginate } from '../../../utils/directory'

const props = defineProps<{
  kind: LookupKind
  title: string
  description: string
  icon: string
  itemLabel: string
}>()

const store = useLookupDirectoryStore()
const records = computed<LookupRecord[]>(() => store.records[props.kind] ?? [])
const query = ref('')
const page = ref(1)
const pageSize = ref(10)
const loading = computed(() => store.isLoading(props.kind))
const error = computed(() => store.errors[props.kind])

const columns: TableColumn<LookupRecord>[] = [
  { accessorKey: 'id', header: 'ID' },
  { accessorKey: 'name', header: 'Canonical name' }
]
const filteredRecords = computed(() => {
  const term = query.value.trim().toLocaleLowerCase()
  if (!term) return records.value
  return records.value.filter(
    (record) =>
      record.name.toLocaleLowerCase().includes(term) ||
      String(record.id).includes(term)
  )
})
const visibleRecords = computed(() =>
  paginate(filteredRecords.value, page.value, pageSize.value)
)

watch([query, pageSize], () => {
  page.value = 1
})

async function loadRecords() {
  await store.load(props.kind, props.itemLabel)
}

onMounted(loadRecords)
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Game content"
      :title="title"
      :description="description"
      :icon="icon"
    >
      <template #actions>
        <UButton
          label="Refresh"
          icon="i-lucide-refresh-cw"
          color="neutral"
          variant="outline"
          :loading="loading"
          @click="loadRecords"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="Catalog unavailable"
      :description="error"
    >
      <template #actions>
        <UButton color="error" variant="soft" size="sm" @click="loadRecords">
          Try again
        </UButton>
      </template>
    </UAlert>

    <UCard v-else :ui="{ body: 'p-0 sm:p-0' }">
      <div
        class="flex flex-wrap items-center justify-between gap-4 border-b border-default px-4 py-3"
      >
        <div>
          <p class="text-sm font-medium text-highlighted">{{ itemLabel }}</p>
          <p class="text-xs text-muted">
            {{ filteredRecords.length }} of {{ records.length }} records
          </p>
        </div>
        <UInput
          v-model="query"
          icon="i-lucide-search"
          :placeholder="`Search ${itemLabel.toLowerCase()}`"
          :aria-label="`Search ${itemLabel.toLowerCase()}`"
          class="w-full sm:w-72"
        />
      </div>

      <div class="overflow-x-auto">
        <UTable
          :data="visibleRecords"
          :columns="columns"
          :loading="loading"
          :empty="`No ${itemLabel.toLowerCase()} match this search.`"
          class="min-w-[34rem]"
        >
          <template #id-cell="{ row }">
            <UBadge color="neutral" variant="subtle" size="sm">
              {{ row.original.id }}
            </UBadge>
          </template>
          <template #name-cell="{ row }">
            <span class="font-medium text-highlighted">
              {{ row.original.name }}
            </span>
          </template>
        </UTable>
      </div>

      <StudioTableFooter
        v-model:page="page"
        v-model:page-size="pageSize"
        :total="filteredRecords.length"
      />
    </UCard>
  </div>
</template>
