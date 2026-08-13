<script setup lang="ts">
import type {
  ImportProgressColor,
  ImportProgressItem,
  ImportProgressStatus
} from '../../utils/import-progress'
import {
  importProgressPercent,
  isActiveImportStatus
} from '../../utils/import-progress'

const props = defineProps<{
  open: boolean
  items: ImportProgressItem[]
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
}>()

const activeItems = computed(() => props.items.filter(item => isActiveImportStatus(item.status)))
const settledCount = computed(() => props.items.length - activeItems.value.length)
const failedCount = computed(() => props.items.filter(item => item.status === 'failed').length)
const warningCount = computed(() => props.items.filter(item => item.status === 'succeeded_with_warnings').length)
const overallProgress = computed<number | undefined>(() => {
  if (activeItems.value.some(item => !item.total)) return undefined
  const total = props.items.reduce((sum, item) => sum + item.total, 0)
  if (!total) return activeItems.value.length ? undefined : 100
  const completed = props.items.reduce((sum, item) => {
    if (!isActiveImportStatus(item.status)) return sum + item.total
    return sum + Math.min(item.completed, item.total)
  }, 0)
  return Math.round((completed / total) * 100)
})
const title = computed(() => {
  if (activeItems.value.length) return 'Import in progress'
  if (failedCount.value) return 'Import completed with errors'
  if (warningCount.value) return 'Import completed with warnings'
  return 'Import complete'
})
const description = computed(() => activeItems.value.length
  ? `${settledCount.value} of ${props.items.length} imports processed`
  : `${props.items.length} ${props.items.length === 1 ? 'import' : 'imports'} processed`
)
const drawerColor = computed<ImportProgressColor>(() => {
  if (activeItems.value.length) return 'primary'
  if (failedCount.value) return 'error'
  if (warningCount.value) return 'warning'
  return 'success'
})
const drawerIcon = computed(() => {
  if (activeItems.value.length) return 'i-lucide-loader-circle'
  if (failedCount.value) return 'i-lucide-circle-x'
  if (warningCount.value) return 'i-lucide-triangle-alert'
  return 'i-lucide-circle-check'
})

function updateOpen(open: boolean) {
  emit('update:open', open)
}

function statusLabel(status: ImportProgressStatus) {
  return status.replaceAll('_', ' ')
}

function statusColor(status: ImportProgressStatus): ImportProgressColor {
  if (status === 'failed') return 'error'
  if (status === 'succeeded_with_warnings') return 'warning'
  if (status === 'succeeded') return 'success'
  if (status === 'queued') return 'neutral'
  return 'primary'
}

function statusIcon(status: ImportProgressStatus) {
  if (status === 'failed') return 'i-lucide-circle-x'
  if (status === 'succeeded_with_warnings') return 'i-lucide-triangle-alert'
  if (status === 'succeeded') return 'i-lucide-circle-check'
  if (status === 'queued') return 'i-lucide-clock-3'
  return 'i-lucide-loader-circle'
}
</script>

<template>
  <UButton
    v-if="!open && items.length"
    class="fixed bottom-5 right-5 z-40 shadow-lg"
    :label="activeItems.length ? 'View import progress' : 'View import results'"
    :icon="drawerIcon"
    :color="drawerColor"
    :class="activeItems.length ? '[&_svg]:animate-spin' : ''"
    @click="updateOpen(true)"
  />

  <UDrawer
    :open="open"
    direction="bottom"
    :inset="false"
    :overlay="true"
    :dismissible="true"
    :close="true"
    :title="title"
    :description="description"
    :ui="{
      container: 'gap-0 p-0',
      header: 'border-b border-default px-5 py-4 sm:px-6',
      body: 'p-5 sm:p-6'
    }"
    @update:open="updateOpen"
  >
    <template #title>
      <div class="flex items-center gap-3">
        <span
          class="grid size-9 shrink-0 place-items-center rounded-lg"
          :class="{
            'bg-primary/10 text-primary': drawerColor === 'primary',
            'bg-error/10 text-error': drawerColor === 'error',
            'bg-warning/10 text-warning': drawerColor === 'warning',
            'bg-success/10 text-success': drawerColor === 'success'
          }"
        >
          <UIcon
            :name="drawerIcon"
            class="size-5"
            :class="activeItems.length ? 'animate-spin' : ''"
          />
        </span>
        <span>{{ title }}</span>
      </div>
    </template>

    <template #body>
      <div class="mx-auto w-full max-w-5xl space-y-5">
        <section class="rounded-lg bg-elevated/60 p-4 ring ring-default">
          <div class="flex flex-wrap items-end justify-between gap-4">
            <div>
              <p class="text-xs font-medium uppercase tracking-wide text-muted">
                Overall progress
              </p>
              <p class="mt-1 text-3xl font-semibold tabular-nums text-highlighted">
                {{ overallProgress === undefined ? 'Working…' : `${overallProgress}%` }}
              </p>
            </div>
            <div class="flex flex-wrap gap-2">
              <UBadge v-if="settledCount" color="neutral" variant="subtle">
                {{ settledCount }} processed
              </UBadge>
              <UBadge v-if="failedCount" color="error" variant="subtle">
                {{ failedCount }} failed
              </UBadge>
              <UBadge v-if="warningCount" color="warning" variant="subtle">
                {{ warningCount }} with warnings
              </UBadge>
              <UBadge v-if="activeItems.length" color="primary" variant="subtle">
                {{ activeItems.length }} active
              </UBadge>
            </div>
          </div>
          <UProgress
            v-if="overallProgress !== undefined"
            class="mt-4"
            :model-value="overallProgress"
            :max="100"
            :color="drawerColor"
            size="lg"
          />
          <UProgress v-else class="mt-4" :color="drawerColor" size="lg" />
        </section>

        <div class="overflow-hidden rounded-lg border border-default">
          <div class="border-b border-default bg-elevated/40 px-4 py-2.5">
            <p class="text-xs font-medium uppercase tracking-wide text-muted">
              Import queue
            </p>
          </div>
          <div class="max-h-72 divide-y divide-default overflow-y-auto">
            <div v-for="item in items" :key="item.id" class="space-y-2.5 px-4 py-3">
              <div class="flex min-w-0 items-start gap-3">
                <UIcon
                  :name="statusIcon(item.status)"
                  class="mt-0.5 size-4 shrink-0"
                  :class="{
                    'animate-spin': isActiveImportStatus(item.status) && item.status !== 'queued',
                    'text-error': statusColor(item.status) === 'error',
                    'text-warning': statusColor(item.status) === 'warning',
                    'text-success': statusColor(item.status) === 'success',
                    'text-primary': statusColor(item.status) === 'primary',
                    'text-muted': statusColor(item.status) === 'neutral'
                  }"
                />
                <div class="min-w-0 flex-1">
                  <div class="flex flex-wrap items-center justify-between gap-x-4 gap-y-1">
                    <div class="min-w-0">
                      <p class="truncate text-sm font-medium text-highlighted">{{ item.label }}</p>
                      <p class="truncate text-xs text-muted">{{ item.detail }}</p>
                    </div>
                    <div class="flex shrink-0 items-center gap-2">
                      <span class="text-xs tabular-nums text-muted">
                        {{ item.total ? `${item.completed} / ${item.total}` : 'Discovering…' }}
                      </span>
                      <UBadge :color="statusColor(item.status)" variant="subtle" size="sm">
                        {{ statusLabel(item.status) }}
                      </UBadge>
                    </div>
                  </div>
                  <div v-if="!isActiveImportStatus(item.status)" class="mt-2 flex flex-wrap gap-2">
                    <UBadge
                      v-for="stat in item.stats"
                      :key="stat.label"
                      :color="stat.color"
                      variant="subtle"
                      size="sm"
                    >
                      {{ stat.value }} {{ stat.label }}
                    </UBadge>
                  </div>
                  <p v-if="item.error" class="mt-2 text-xs text-error">{{ item.error }}</p>
                </div>
              </div>
              <UProgress
                v-if="importProgressPercent(item) !== undefined"
                :model-value="importProgressPercent(item)"
                :max="100"
                :color="statusColor(item.status)"
                size="xs"
              />
              <UProgress v-else :color="statusColor(item.status)" size="xs" />
            </div>
          </div>
        </div>
      </div>
    </template>
  </UDrawer>
</template>
