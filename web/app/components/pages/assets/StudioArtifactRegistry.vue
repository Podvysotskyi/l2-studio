<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import {
  getAssetArtifact,
  getAssetArtifacts,
  verifyAssetArtifact
} from '../../../services/studio-api'
import type {
  AssetArtifactDetail,
  AssetArtifactPage,
  AssetArtifactSummary,
  AssetImportKind
} from '../../../types/models/asset-catalog'

const artifacts = ref<AssetArtifactPage>()
const selected = ref<AssetArtifactDetail>()
const loading = ref(false)
const error = ref<string>()
const sourceKey = ref('')
const kind = ref<'all' | AssetImportKind>('all')
const state = ref<'all' | 'current' | 'history'>('all')
const integrity = ref<'all' | 'healthy' | 'missing' | 'corrupt'>('all')
const page = ref(1)
const verifying = ref(false)
const config = useRuntimeConfig()

const kinds = [
  { label: 'All kinds', value: 'all' },
  { label: 'Textures', value: 'textures' },
  { label: 'Static meshes', value: 'staticmeshes' },
  { label: 'Sounds', value: 'sounds' },
  { label: 'Music', value: 'music' },
  { label: 'Maps', value: 'maps' },
  { label: 'Map previews', value: 'mappreviews' },
  { label: 'Scenes', value: 'scenes' }
]
const stateOptions = [
  { label: 'Current and history', value: 'all' },
  { label: 'Current only', value: 'current' },
  { label: 'History only', value: 'history' }
]
const integrityOptions = [
  { label: 'All integrity states', value: 'all' },
  { label: 'Healthy', value: 'healthy' },
  { label: 'Missing', value: 'missing' },
  { label: 'Corrupt', value: 'corrupt' }
]
const pageCount = computed(() => Math.max(1, Math.ceil((artifacts.value?.total ?? 0) / 50)))

watch([kind, state, integrity], () => {
  page.value = 1
  void load()
})
watch(page, () => void load())

async function load() {
  loading.value = true
  error.value = undefined
  try {
    artifacts.value = await getAssetArtifacts({
      ...(kind.value === 'all' ? {} : { kind: kind.value }),
      ...(sourceKey.value.trim() ? { sourceKey: sourceKey.value.trim() } : {}),
      ...(state.value === 'all' ? {} : { current: state.value === 'current' }),
      ...(integrity.value === 'all' ? {} : { integrityStatus: integrity.value }),
      page: page.value,
      pageSize: 50
    })
  } catch {
    error.value = 'The generated-asset registry could not be loaded.'
  } finally {
    loading.value = false
  }
}

async function inspect(artifact: AssetArtifactSummary) {
  error.value = undefined
  try {
    selected.value = await getAssetArtifact(artifact.id)
  } catch {
    error.value = 'The selected artifact could not be loaded.'
  }
}

async function verify() {
  if (!selected.value) return
  verifying.value = true
  try {
    selected.value = await verifyAssetArtifact(selected.value.artifact.id)
    await load()
  } catch {
    error.value = 'Artifact integrity verification failed.'
  } finally {
    verifying.value = false
  }
}

function shortHash(value: string) {
  return value.slice(0, 12)
}

function formatBytes(value: number) {
  if (value < 1024) return `${value} B`
  if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`
  return `${(value / 1024 / 1024).toFixed(1)} MB`
}

function assetUrl(path: string) {
  return `${String(config.public.assetBaseUrl).replace(/\/$/, '')}/${path.replace(/^\//, '')}`
}

onMounted(() => void load())
</script>

<template>
  <div class="space-y-5">
    <div class="flex flex-wrap items-end justify-between gap-3">
      <div>
        <h1 class="text-xl font-semibold text-highlighted">Generated-asset registry</h1>
        <p class="mt-1 text-sm text-muted">Immutable builds, file inventories and dependency history.</p>
      </div>
      <UBadge color="neutral" variant="subtle">{{ artifacts?.total ?? 0 }} artifacts</UBadge>
    </div>

    <UAlert v-if="error" color="error" icon="i-lucide-circle-alert" :description="error" />

    <UCard>
      <div class="grid gap-3 md:grid-cols-4">
        <UInput v-model="sourceKey" placeholder="Filter source key" @keyup.enter="page = 1; load()" />
        <USelect v-model="kind" :items="kinds" />
        <USelect v-model="state" :items="stateOptions" />
        <USelect v-model="integrity" :items="integrityOptions" />
      </div>
    </UCard>

    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <div v-if="loading" class="p-8 text-center text-sm text-muted">Loading registry…</div>
      <div v-else-if="!artifacts?.items.length" class="p-8 text-center text-sm text-muted">No generated artifacts match these filters.</div>
      <div v-else class="divide-y divide-default">
        <button
          v-for="artifact in artifacts.items"
          :key="artifact.id"
          class="grid w-full gap-3 p-4 text-left hover:bg-elevated md:grid-cols-[1fr_auto_auto]"
          @click="inspect(artifact)"
        >
          <span class="min-w-0">
            <span class="flex items-center gap-2">
              <strong class="truncate text-sm text-highlighted">{{ artifact.sourceKey }}</strong>
              <UBadge v-if="artifact.isCurrent" color="primary" variant="subtle" size="sm">Current</UBadge>
            </span>
            <span class="mt-1 block font-mono text-xs text-muted">{{ artifact.kind }} · {{ shortHash(artifact.buildFingerprint) }} · {{ artifact.recipeVersion }}</span>
          </span>
          <span class="text-xs text-muted">{{ artifact.fileCount }} files · {{ formatBytes(artifact.sizeBytes) }}</span>
          <UBadge :color="artifact.integrityStatus === 'healthy' ? 'success' : 'error'" variant="subtle">{{ artifact.integrityStatus }}</UBadge>
        </button>
      </div>
    </UCard>

    <div v-if="pageCount > 1" class="flex justify-center">
      <UPagination v-model:page="page" :total="artifacts?.total ?? 0" :items-per-page="50" />
    </div>

    <USlideover
      :open="Boolean(selected)"
      title="Artifact details"
      description="Registered files and dependencies"
      @update:open="open => { if (!open) selected = undefined }"
    >
      <template #body>
        <div v-if="selected" class="space-y-6">
          <UButton
            label="Verify file integrity"
            icon="i-lucide-shield-check"
            :loading="verifying"
            block
            @click="verify"
          />
          <dl class="grid grid-cols-[auto_1fr] gap-x-3 gap-y-2 text-sm">
            <dt class="text-muted">Source</dt><dd class="break-all">{{ selected.artifact.sourceKey }}</dd>
            <dt class="text-muted">Build</dt><dd class="break-all font-mono text-xs">{{ selected.artifact.buildFingerprint }}</dd>
            <dt class="text-muted">Content</dt><dd class="break-all font-mono text-xs">{{ selected.artifact.contentHash }}</dd>
            <dt class="text-muted">Output</dt><dd class="break-all font-mono text-xs">{{ selected.artifact.outputRoot }}</dd>
          </dl>
          <section>
            <h2 class="mb-2 text-sm font-semibold text-highlighted">Files</h2>
            <div class="space-y-2">
              <a v-for="file in selected.files" :key="file.relativePath" :href="assetUrl(file.publicPath)" target="_blank" class="block rounded-md border border-default p-3 hover:bg-elevated">
                <span class="block break-all text-sm">{{ file.relativePath }}</span>
                <span class="text-xs text-muted">{{ file.role }} · {{ formatBytes(file.sizeBytes) }} · {{ shortHash(file.sha256) }}</span>
              </a>
            </div>
          </section>
          <section>
            <h2 class="mb-2 text-sm font-semibold text-highlighted">Dependencies</h2>
            <p v-if="!selected.dependencies.length" class="text-sm text-muted">No generated-asset dependencies.</p>
            <div v-for="dependency in selected.dependencies" :key="`${dependency.kind}:${dependency.dependencyKey}`" class="mb-2 rounded-md border border-default p-3 text-sm">
              <span class="block break-all">{{ dependency.kind }}:{{ dependency.dependencyKey }}</span>
              <span :class="dependency.isResolved ? 'text-success' : 'text-warning'">{{ dependency.isResolved ? 'Resolved' : 'Unresolved' }}</span>
            </div>
          </section>
        </div>
      </template>
    </USlideover>
  </div>
</template>
