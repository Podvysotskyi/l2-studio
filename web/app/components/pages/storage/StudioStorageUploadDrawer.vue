<script setup lang="ts">
import type { StorageUploadItem } from '../../../utils/storage-browser'

type UploadStatus = StorageUploadItem['status']
type UploadColor = 'primary' | 'neutral' | 'success' | 'error'

const props = defineProps<{
  open: boolean
  uploads: StorageUploadItem[]
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
}>()

const activeUploads = computed(() =>
  props.uploads.filter(item => item.status === 'queued' || item.status === 'uploading')
)
const completedCount = computed(() =>
  props.uploads.filter(item => item.status === 'complete').length
)
const failedCount = computed(() =>
  props.uploads.filter(item => item.status === 'failed').length
)
const settledCount = computed(() => completedCount.value + failedCount.value)
const progress = computed(() => {
  const total = props.uploads.reduce((sum, item) => sum + item.total, 0)
  const processed = props.uploads.reduce(
    (sum, item) => sum + (
      item.status === 'complete' || item.status === 'failed'
        ? item.total
        : item.loaded
    ),
    0
  )
  if (total) return Math.round((processed / total) * 100)
  return props.uploads.length && !activeUploads.value.length ? 100 : 0
})
const title = computed(() => {
  if (activeUploads.value.length) return 'Uploading resources'
  if (failedCount.value) return 'Upload completed with errors'
  return 'Upload complete'
})
const description = computed(() =>
  `${settledCount.value} of ${props.uploads.length} files processed`
)
const icon = computed(() => {
  if (activeUploads.value.length) return 'i-lucide-cloud-upload'
  if (failedCount.value) return 'i-lucide-triangle-alert'
  return 'i-lucide-circle-check'
})
const color = computed<UploadColor>(() => {
  if (activeUploads.value.length) return 'primary'
  if (failedCount.value) return 'error'
  return 'success'
})

function updateOpen(open: boolean) {
  if (!open && activeUploads.value.length) return
  emit('update:open', open)
}

function itemProgress(item: StorageUploadItem) {
  if (!item.total) return item.status === 'complete' ? 100 : 0
  return Math.round((item.loaded / item.total) * 100)
}

function statusLabel(status: UploadStatus) {
  if (status === 'complete') return 'Complete'
  if (status === 'failed') return 'Failed'
  if (status === 'uploading') return 'Uploading'
  return 'Queued'
}

function statusColor(status: UploadStatus): UploadColor {
  if (status === 'complete') return 'success'
  if (status === 'failed') return 'error'
  if (status === 'uploading') return 'primary'
  return 'neutral'
}

function statusIcon(status: UploadStatus) {
  if (status === 'complete') return 'i-lucide-circle-check'
  if (status === 'failed') return 'i-lucide-circle-x'
  if (status === 'uploading') return 'i-lucide-loader-circle'
  return 'i-lucide-clock-3'
}

function formatSize(size: number) {
  if (size < 1024) return `${size} B`
  const units = ['KB', 'MB', 'GB', 'TB']
  let value = size / 1024
  let unit = 0
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024
    unit++
  }
  return `${value.toFixed(value >= 10 ? 1 : 2)} ${units[unit]}`
}
</script>

<template>
  <UDrawer
    :open="open"
    direction="bottom"
    :inset="false"
    :overlay="true"
    :dismissible="!activeUploads.length"
    :close="!activeUploads.length"
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
            'bg-primary/10 text-primary': activeUploads.length,
            'bg-error/10 text-error': !activeUploads.length && failedCount,
            'bg-success/10 text-success': !activeUploads.length && !failedCount
          }"
        >
          <UIcon
            :name="icon"
            class="size-5"
            :class="activeUploads.length ? 'animate-pulse' : ''"
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
                {{ progress }}%
              </p>
            </div>
            <div class="flex flex-wrap gap-2">
              <UBadge color="success" variant="subtle">
                {{ completedCount }} completed
              </UBadge>
              <UBadge v-if="failedCount" color="error" variant="subtle">
                {{ failedCount }} failed
              </UBadge>
              <UBadge v-if="activeUploads.length" color="primary" variant="subtle">
                {{ activeUploads.length }} remaining
              </UBadge>
            </div>
          </div>
          <UProgress
            class="mt-4"
            :model-value="progress"
            :max="100"
            :color="color"
            size="lg"
          />
        </section>

        <div class="overflow-hidden rounded-lg border border-default">
          <div class="border-b border-default bg-elevated/40 px-4 py-2.5">
            <p class="text-xs font-medium uppercase tracking-wide text-muted">
              Transfer queue
            </p>
          </div>
          <div class="max-h-72 divide-y divide-default overflow-y-auto">
            <div
              v-for="item in uploads"
              :key="item.id"
              class="space-y-2.5 px-4 py-3"
            >
              <div class="flex min-w-0 items-start gap-3">
                <UIcon
                  :name="statusIcon(item.status)"
                  class="mt-0.5 size-4 shrink-0"
                  :class="[
                    item.status === 'uploading' ? 'animate-spin' : '',
                    item.status === 'complete'
                      ? 'text-success'
                      : item.status === 'failed'
                        ? 'text-error'
                        : item.status === 'uploading'
                          ? 'text-primary'
                          : 'text-muted'
                  ]"
                />
                <div class="min-w-0 flex-1">
                  <div class="flex flex-wrap items-center justify-between gap-x-4 gap-y-1">
                    <p class="min-w-0 truncate text-sm font-medium text-highlighted">
                      {{ item.path }}
                    </p>
                    <div class="flex shrink-0 items-center gap-2">
                      <span class="text-xs tabular-nums text-muted">
                        {{ itemProgress(item) }}%
                      </span>
                      <UBadge
                        :color="statusColor(item.status)"
                        variant="subtle"
                        size="sm"
                      >
                        {{ statusLabel(item.status) }}
                      </UBadge>
                    </div>
                  </div>
                  <p class="mt-0.5 text-xs text-muted">
                    {{ formatSize(item.loaded) }} of {{ formatSize(item.total) }}
                  </p>
                  <p v-if="item.error" class="mt-1 text-xs text-error">
                    {{ item.error }}
                  </p>
                </div>
              </div>
              <UProgress
                :model-value="itemProgress(item)"
                :max="100"
                :color="statusColor(item.status)"
                size="xs"
              />
            </div>
          </div>
        </div>
      </div>
    </template>
  </UDrawer>
</template>
