<script setup lang="ts">
import type { TextureImportKind } from '@l2/ui'
import { computed, onBeforeUnmount } from 'vue'
import {
  textureImportsUrl,
  type AssetImportJob
} from '../../lib/studio-content'

const config = useRuntimeConfig()
const jobs = ref<AssetImportJob[]>([])
const kindFilter = ref<'all' | TextureImportKind>('all')
const loading = ref(true)
const error = ref<string>()
let pollTimer: ReturnType<typeof setTimeout> | undefined

const kindOptions = [
  { label: 'All collections', value: 'all' },
  { label: 'System textures', value: 'systextures' },
  { label: 'World textures', value: 'textures' }
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

function kindLabel(kind: TextureImportKind) {
  return kind === 'systextures' ? 'System textures' : 'World textures'
}

function formatDate(value: string | null) {
  return value ? new Date(value).toLocaleString() : '—'
}

async function loadJobs(schedule = true) {
  clearTimeout(pollTimer)
  loading.value = true
  try {
    const results = await Promise.all(
      (['systextures', 'textures'] as const).map((kind) =>
        $fetch<AssetImportJob[]>(
          textureImportsUrl(config.public.apiBase, kind),
          { query: { limit: 100 } }
        )
      )
    )
    jobs.value = results
      .flat()
      .sort(
        (left, right) =>
          new Date(right.requestedAt).getTime() -
          new Date(left.requestedAt).getTime()
      )
    error.value = undefined
  } catch {
    error.value = 'Import jobs could not be loaded from the Studio API.'
  } finally {
    loading.value = false
  }

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
      description="Review import history for both system and world texture collections."
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
