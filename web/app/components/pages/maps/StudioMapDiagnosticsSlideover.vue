<script setup lang="ts">
import type { AssetCatalogDiagnosticPage } from '../../../types/models/asset-import-job'
import { getAssetCatalogDiagnostics } from '../../../services/studio-api'
import { computed, ref, watch } from 'vue'

const props = defineProps<{
  mapName: string
  sourceKey?: string
}>()
const open = defineModel<boolean>('open', { default: false })
const diagnostics = ref<AssetCatalogDiagnosticPage>()
const loading = ref(false)
const error = ref<string>()
const query = ref('')
const appliedQuery = ref('')
const severity = ref<'all' | 'warning' | 'error'>('all')
const page = ref(1)
const pageSize = 25
let loadVersion = 0

const severityOptions = [
  { label: 'All diagnostics', value: 'all' },
  { label: 'Warnings', value: 'warning' },
  { label: 'Errors', value: 'error' }
]
const title = computed(() => `Diagnostics · ${props.mapName}`)

watch(open, value => {
  if (value && !diagnostics.value && !loading.value) void loadDiagnostics()
})
watch(
  () => [props.mapName, props.sourceKey],
  () => {
    loadVersion++
    diagnostics.value = undefined
    error.value = undefined
    query.value = ''
    appliedQuery.value = ''
    severity.value = 'all'
    page.value = 1
    if (open.value) void loadDiagnostics()
  }
)

async function loadDiagnostics() {
  const version = ++loadVersion
  loading.value = true
  error.value = undefined
  try {
    const result = await getAssetCatalogDiagnostics('maps', props.mapName, {
      sourceKey: props.sourceKey,
      severity: severity.value === 'all' ? undefined : severity.value,
      query: appliedQuery.value || undefined,
      page: page.value,
      pageSize
    })
    if (version === loadVersion) diagnostics.value = result
  } catch {
    if (version === loadVersion)
      error.value = 'Diagnostics for this map artifact could not be loaded.'
  } finally {
    if (version === loadVersion) loading.value = false
  }
}

function applyFilters() {
  appliedQuery.value = query.value.trim()
  page.value = 1
  void loadDiagnostics()
}

function changePage(value: number) {
  page.value = value
  void loadDiagnostics()
}

function formatDate(value: string) {
  return new Date(value).toLocaleString()
}
</script>

<template>
  <USlideover
    v-model:open="open"
    :title="title"
    :description="sourceKey ?? 'Displayed map artifact'"
    :ui="{ content: 'max-w-3xl' }"
  >
    <template #body>
      <div class="space-y-5">
        <UAlert
          v-if="error"
          color="error"
          variant="subtle"
          title="Diagnostics unavailable"
          :description="error"
        >
          <template #actions>
            <UButton color="error" variant="soft" size="sm" @click="loadDiagnostics">
              Try again
            </UButton>
          </template>
        </UAlert>

        <div
          v-if="diagnostics"
          class="grid gap-3 rounded-lg border border-default bg-muted/30 p-4 sm:grid-cols-3"
        >
          <div>
            <p class="text-xs text-muted">Artifact source</p>
            <p class="mt-1 break-all text-sm font-medium text-highlighted">
              {{ diagnostics.sourceKey }}
            </p>
          </div>
          <div>
            <p class="text-xs text-muted">Import status</p>
            <p class="mt-1 text-sm font-medium text-highlighted">
              {{ diagnostics.workItemStatus.replaceAll('_', ' ') }}
            </p>
          </div>
          <div>
            <p class="text-xs text-muted">Published</p>
            <p class="mt-1 text-sm font-medium text-highlighted">
              {{ formatDate(diagnostics.publishedAt) }}
            </p>
          </div>
        </div>

        <form
          class="grid gap-3 sm:grid-cols-[minmax(0,1fr)_12rem_auto]"
          @submit.prevent="applyFilters"
        >
          <UFormField label="Find diagnostics">
            <UInput
              v-model="query"
              icon="i-lucide-search"
              placeholder="Object, path, or message"
            />
          </UFormField>
          <UFormField label="Severity">
            <USelect v-model="severity" :items="severityOptions" />
          </UFormField>
          <div class="flex items-end">
            <UButton
              type="submit"
              label="Apply"
              color="neutral"
              variant="outline"
              class="w-full sm:w-auto"
              :loading="loading"
            />
          </div>
        </form>

        <div v-if="loading && !diagnostics" class="grid min-h-40 place-items-center">
          <div class="flex items-center gap-2 text-sm text-muted">
            <UIcon name="i-lucide-loader-circle" class="size-4 animate-spin" />
            Loading diagnostics…
          </div>
        </div>

        <div v-else-if="diagnostics?.items.length" class="space-y-3">
          <article
            v-for="diagnostic in diagnostics.items"
            :key="diagnostic.id"
            class="rounded-lg border border-default p-4"
            :class="diagnostic.severity === 'error' ? 'bg-error/5' : 'bg-warning/5'"
          >
            <div class="flex flex-wrap items-center gap-2 text-xs">
              <UBadge
                :color="diagnostic.severity === 'error' ? 'error' : 'warning'"
                variant="subtle"
              >
                {{ diagnostic.severity }}
              </UBadge>
              <code>{{ diagnostic.code }}</code>
              <span class="text-muted">{{ diagnostic.stage }}</span>
              <span v-if="diagnostic.objectName" class="text-muted">
                {{ diagnostic.objectName }}
              </span>
              <time
                class="ml-auto text-dimmed"
                :datetime="diagnostic.createdAt"
              >
                {{ formatDate(diagnostic.createdAt) }}
              </time>
            </div>
            <p class="mt-2 text-sm text-muted">{{ diagnostic.message }}</p>
          </article>
        </div>

        <p
          v-else-if="diagnostics && !loading"
          class="rounded-lg border border-dashed border-default p-8 text-center text-sm text-muted"
        >
          {{ appliedQuery || severity !== 'all'
            ? 'No diagnostics match these filters.'
            : 'The import that produced this map reported no diagnostics.' }}
        </p>

        <StudioTableFooter
          v-if="diagnostics && diagnostics.total > pageSize"
          :page="page"
          :page-size="pageSize"
          :total="diagnostics.total"
          :page-size-options="[pageSize]"
          @update:page="changePage"
        />
      </div>
    </template>
  </USlideover>
</template>
