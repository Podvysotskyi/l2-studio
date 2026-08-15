<script setup lang="ts" generic="TData">
import type { TableColumn } from '@nuxt/ui'
import { computed, useSlots, watch } from 'vue'
import { paginate } from '../../utils/directory'

defineOptions({ inheritAttrs: false })

interface StudioTableFilter {
  key: string
  placeholder: string
  ariaLabel?: string
  items: Array<{ label: string; value: string | number | boolean }>
  loading?: boolean
}

type PaginationMode = 'server' | 'client' | 'none'

const props = withDefaults(defineProps<{
  data: TData[]
  columns: TableColumn<TData>[]
  total?: number
  loading?: boolean
  empty?: string
  paginationMode?: PaginationMode
  page?: number
  pageSize?: number
  pageSizeOptions?: number[]
  query?: string
  searchPlaceholder?: string
  searchAriaLabel?: string
  filters?: StudioTableFilter[]
  filterValues?: Record<string, string | number | boolean | undefined>
  tableClass?: string
}>(), {
  total: undefined,
  loading: false,
  empty: 'No records are available.',
  paginationMode: 'server',
  page: 1,
  pageSize: 25,
  pageSizeOptions: () => [10, 25, 50],
  query: undefined,
  searchPlaceholder: 'Search records',
  searchAriaLabel: 'Search records',
  filters: () => [],
  filterValues: () => ({}),
  tableClass: undefined
})

const emit = defineEmits<{
  'update:page': [value: number]
  'update:pageSize': [value: number]
  'update:query': [value: string]
  'update:filterValues': [value: Record<string, string | number | boolean | undefined>]
}>()

const slots = useSlots()
const specialSlots = new Set(['toolbar-start', 'toolbar-end', 'mobile'])
const tableSlotNames = computed(() => Object.keys(slots).filter(name => !specialSlots.has(name)))
const total = computed(() => props.total ?? props.data.length)
const rows = computed(() => props.paginationMode === 'client'
  ? paginate(props.data, props.page, props.pageSize)
  : props.data)
const hasToolbar = computed(() => Boolean(
  props.query !== undefined || props.filters.length || slots['toolbar-start'] || slots['toolbar-end']
))
const hasMobileRows = computed(() => Boolean(slots.mobile))

watch([total, () => props.page, () => props.pageSize, () => props.paginationMode], () => {
  if (props.paginationMode === 'none') return
  const lastPage = Math.max(1, Math.ceil(total.value / props.pageSize))
  if (props.page > lastPage) emit('update:page', lastPage)
}, { immediate: true })

function updateFilter(key: string, value: unknown) {
  emit('update:filterValues', {
    ...props.filterValues,
    [key]: value === '' || value === null ? undefined : value as string | number | boolean
  })
}

function filterIsActive(key: string) {
  const value = props.filterValues[key]
  return value !== undefined && value !== null && value !== ''
}

function clearFilter(key: string) {
  emit('update:filterValues', {
    ...props.filterValues,
    [key]: undefined
  })
}
</script>

<template>
  <div>
    <div
      v-if="hasToolbar"
      class="flex flex-wrap items-center justify-between gap-3 border-b border-default px-4 py-3"
    >
      <slot name="toolbar-start" />

      <div class="flex w-full flex-wrap items-center gap-2 sm:w-auto">
        <UInput
          v-if="query !== undefined"
          :model-value="query"
          icon="i-lucide-search"
          :placeholder="searchPlaceholder"
          :aria-label="searchAriaLabel"
          class="min-w-48 flex-1 sm:w-72"
          @update:model-value="emit('update:query', String($event))"
        />
        <div
          v-for="filter in filters"
          :key="filter.key"
          class="flex w-full min-w-0 items-center gap-1 sm:w-44"
        >
          <USelect
            :model-value="filterValues[filter.key]"
            :items="filter.items"
            :loading="filter.loading"
            :placeholder="filter.placeholder"
            :aria-label="filter.ariaLabel ?? filter.placeholder"
            class="min-w-0 flex-1"
            @update:model-value="updateFilter(filter.key, $event)"
          />
          <UTooltip
            v-if="filterIsActive(filter.key)"
            :text="`Clear ${filter.ariaLabel ?? filter.placeholder}`"
          >
            <UButton
              icon="i-lucide-x"
              color="neutral"
              variant="ghost"
              size="sm"
              :aria-label="`Clear ${filter.ariaLabel ?? filter.placeholder}`"
              @click="clearFilter(filter.key)"
            />
          </UTooltip>
        </div>
        <slot name="toolbar-end" />
      </div>
    </div>

    <div :class="[hasMobileRows ? 'hidden sm:block' : '', 'overflow-x-auto']">
      <UTable
        v-bind="$attrs"
        :data="rows"
        :columns="columns"
        :loading="loading"
        :empty="empty"
        :class="tableClass"
      >
        <template v-for="slotName in tableSlotNames" :key="slotName" #[slotName]="slotProps">
          <slot :name="slotName" v-bind="{ ...slotProps, rows }" />
        </template>
      </UTable>
    </div>

    <div v-if="hasMobileRows" class="sm:hidden">
      <slot name="mobile" :rows="rows" />
    </div>

    <StudioTableFooter
      v-if="paginationMode !== 'none'"
      :page="page"
      :page-size="pageSize"
      :total="total"
      :page-size-options="pageSizeOptions"
      @update:page="emit('update:page', $event)"
      @update:page-size="emit('update:pageSize', $event)"
    />
  </div>
</template>
