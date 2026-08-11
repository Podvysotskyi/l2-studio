<script setup lang="ts">
import { computed } from 'vue'
import { paginationRange } from '../../utils/studio-content'

const props = defineProps<{
  page: number
  pageSize: number
  total: number
  pageSizeOptions?: number[]
}>()
const emit = defineEmits<{
  'update:page': [value: number]
  'update:pageSize': [value: number]
}>()

const pageModel = computed({
  get: () => props.page,
  set: (value: number) => emit('update:page', value)
})
const pageSizeModel = computed({
  get: () => props.pageSize,
  set: (value: number) => emit('update:pageSize', value)
})
const range = computed(() =>
  paginationRange(props.total, props.page, props.pageSize)
)
</script>

<template>
  <footer
    class="flex flex-wrap items-center justify-between gap-4 border-t border-default px-4 py-3"
  >
    <p class="text-xs text-muted">
      <template v-if="total > 0">
        Showing {{ range.first }}–{{ range.last }} of {{ total }}
      </template>
      <template v-else>No records</template>
    </p>

    <div class="flex flex-wrap items-center justify-end gap-3">
      <label class="flex items-center gap-2 text-xs text-muted">
        Rows
        <USelect
          v-model="pageSizeModel"
          aria-label="Rows per page"
          :items="pageSizeOptions ?? [10, 25, 50]"
          size="sm"
          class="w-20"
        />
      </label>
      <UPagination
        v-model:page="pageModel"
        :total="total"
        :items-per-page="pageSize"
        size="sm"
      />
    </div>
  </footer>
</template>
