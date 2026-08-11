<script setup lang="ts">
import type { AssetImportKind } from '~/types/studio'
import { storeToRefs } from 'pinia'
import { computed, onBeforeUnmount } from 'vue'
import { useAssetImportsStore } from '../../../stores/asset-imports'
import type { AssetImportJob } from '../../../types/models/asset-import-job'

const importStore = useAssetImportsStore()
const { jobs: jobsByKind, loading, error } = storeToRefs(importStore)
const kindFilter = ref<'all' | AssetImportKind>('all')
let pollTimer: ReturnType<typeof setTimeout> | undefined

const importKinds: AssetImportKind[] = [
  'systextures',
  'textures',
  'music',
  'sounds',
  'staticmeshes',
  'levels',
  'levelpreviews',
  'scenes'
]
const jobs = computed(() =>
  importKinds
    .flatMap((kind) => jobsByKind.value[kind] ?? [])
    .sort(
      (left, right) =>
        new Date(right.requestedAt).getTime() -
        new Date(left.requestedAt).getTime()
    )
)

const kindOptions = [
  { label: 'All collections', value: 'all' },
  { label: 'System textures', value: 'systextures' },
  { label: 'World textures', value: 'textures' },
  { label: 'Music', value: 'music' },
  { label: 'Sounds', value: 'sounds' },
  { label: 'Static meshes', value: 'staticmeshes' },
  { label: 'Levels', value: 'levels' },
  { label: 'Level previews', value: 'levelpreviews' },
  { label: 'Scenes', value: 'scenes' }
]
const visibleJobs = computed(() =>
  jobs.value.filter(
    (job) => kindFilter.value === 'all' || job.kind === kindFilter.value
  )
)
const hasActiveJob = computed(() =>
  jobs.value.some((job) => job.status === 'queued' || job.status === 'running')
)

function statusColor(status: AssetImportJob['status']) {
  if (status === 'succeeded') return 'success'
  if (status === 'succeeded_with_warnings') return 'warning'
  if (status === 'failed') return 'error'
  return 'info'
}

function kindLabel(kind: AssetImportKind) {
  if (kind === 'systextures') return 'System textures'
  if (kind === 'textures') return 'World textures'
  if (kind === 'music') return 'Music'
  if (kind === 'sounds') return 'Sounds'
  if (kind === 'staticmeshes') return 'Static meshes'
  if (kind === 'levels') return 'Levels'
  return kind === 'levelpreviews' ? 'Level previews' : 'Scenes'
}

function formatDate(value: string | null) {
  return value ? new Date(value).toLocaleString() : '—'
}

async function loadJobs(schedule = true) {
  clearTimeout(pollTimer)
  try {
    await importStore.load(importKinds)
  } catch {}

  if (schedule && hasActiveJob.value) {
    pollTimer = setTimeout(() => void loadJobs(), 1000)
  }
}

onMounted(() => void loadJobs())
onBeforeUnmount(() => clearTimeout(pollTimer))
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Asset pipeline"
      title="Import jobs"
      description="Review import history for textures, music, sounds, meshes, world levels, previews, and client scenes."
      icon="i-lucide-history"
    >
      <template #actions>
        <UButton
          icon="i-lucide-refresh-cw"
          label="Refresh"
          color="neutral"
          variant="outline"
          :loading="loading"
          @click="loadJobs()"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="Import history unavailable"
      :description="error"
    />

    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <template #header>
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="text-sm font-semibold text-highlighted">Recent jobs</h2>
            <p class="text-xs text-muted">
              {{ visibleJobs.length }} jobs across the selected collection
            </p>
          </div>
          <USelect
            v-model="kindFilter"
            :items="kindOptions"
            aria-label="Filter import jobs by collection"
            class="w-full sm:w-52"
          />
        </div>
      </template>

      <div v-if="visibleJobs.length" class="divide-y divide-default">
        <article v-for="job in visibleJobs" :key="job.id" class="p-4 sm:p-5">
          <div class="flex flex-wrap items-start justify-between gap-3">
            <div class="flex flex-wrap items-center gap-2">
              <UBadge color="neutral" variant="subtle">
                {{ kindLabel(job.kind) }}
              </UBadge>
              <UBadge :color="statusColor(job.status)" variant="subtle">
                {{ job.status.replaceAll('_', ' ') }}
              </UBadge>
            </div>
            <time class="text-xs text-dimmed" :datetime="job.requestedAt">
              {{ formatDate(job.requestedAt) }}
            </time>
          </div>

          <p class="mt-3 truncate text-sm text-muted" :title="job.sourcePath">
            {{ job.sourcePath }}
          </p>
          <div class="mt-3 flex flex-wrap gap-x-5 gap-y-1 text-xs text-muted">
            <span
              >{{ job.processedCount }} / {{ job.totalCount }} processed</span
            >
            <span>{{ job.skippedCount }} skipped</span>
            <span>Started {{ formatDate(job.startedAt) }}</span>
            <span>Finished {{ formatDate(job.finishedAt) }}</span>
          </div>
          <UProgress
            v-if="job.status === 'queued' || job.status === 'running'"
            class="mt-3"
            :model-value="job.processedCount"
            :max="job.totalCount || 1"
          />
          <p v-if="job.error" class="mt-3 text-sm text-error">
            {{ job.error }}
          </p>
          <details v-if="job.warnings.length" class="mt-3 text-xs text-warning">
            <summary class="cursor-pointer">
              {{ job.warnings.length }} warnings
            </summary>
            <ul class="mt-2 max-h-52 list-disc overflow-auto pl-5">
              <li v-for="warning in job.warnings" :key="warning">
                {{ warning }}
              </li>
            </ul>
          </details>
        </article>
      </div>
      <div
        v-else
        class="grid min-h-64 place-items-center p-8 text-sm text-muted"
      >
        {{ loading ? 'Loading import jobs…' : 'No imports have been queued.' }}
      </div>
    </UCard>
  </div>
</template>
