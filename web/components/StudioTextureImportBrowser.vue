<script setup lang="ts">
import type {
  AssetCatalogPage,
  TextureImportKind,
  TexturePackage,
  TextureManifestEntry
} from '@podvysotskyi/l2-ui'
import { computed, onBeforeUnmount, watch } from 'vue'
import {
  assetCatalogUrl,
  assetImportsUrl,
  type AssetImportJob
} from '../lib/studio-content'

const props = defineProps<{
  kind: TextureImportKind
  title: string
  description: string
  importLabel: string
}>()

const config = useRuntimeConfig()
const jobs = ref<AssetImportJob[]>([])
const catalog = ref<AssetCatalogPage<TextureManifestEntry, TexturePackage>>()
const query = ref('')
const packageQuery = ref('')
const packageFilter = ref('all')
const page = ref(1)
const pageSize = ref(100)
const queueing = ref(false)
const error = ref<string>()
const selectedTexture = ref<TextureManifestEntry>()
let pollTimer: ReturnType<typeof setTimeout> | undefined

const previewOpen = computed({
  get: () => selectedTexture.value !== undefined,
  set: (open: boolean) => {
    if (!open) selectedTexture.value = undefined
  }
})
const activeJob = computed(() =>
  jobs.value.find((job) => job.status === 'queued' || job.status === 'running')
)
const visiblePackages = computed(() => {
  const term = packageQuery.value.trim().toLocaleLowerCase()
  return (catalog.value?.groups ?? []).filter(
    (item) => !term || item.name.toLocaleLowerCase().includes(term)
  )
})
const filteredTextures = computed(() => catalog.value?.items ?? [])
const visibleTextures = computed(() => filteredTextures.value)
const resolvedCount = computed(() => catalog.value?.summary.resolved ?? 0)

watch([query, packageFilter, pageSize], () => {
  page.value = 1
  void loadCatalog()
})
watch(page, () => void loadCatalog())

function showPreview(texture: TextureManifestEntry) {
  if (texture.url) selectedTexture.value = texture
}

function isEmptyPlaceholder(texture: TextureManifestEntry) {
  return (
    texture.status === 'skipped' &&
    texture.error === 'Texture export contains no native mip data.'
  )
}

function textureStatusColor(texture: TextureManifestEntry) {
  if (texture.status === 'resolved') return 'success'
  return isEmptyPlaceholder(texture) ? 'neutral' : 'warning'
}

function previewWidth(texture: TextureManifestEntry | undefined) {
  return texture
    ? `${Math.min(1024, Math.max(256, texture.width * 4))}px`
    : undefined
}

async function loadCatalog() {
  try {
    catalog.value = await $fetch(
      assetCatalogUrl(config.public.apiBase, props.kind, {
        query: query.value,
        packageName:
          packageFilter.value === 'all' ? undefined : packageFilter.value,
        page: page.value,
        pageSize: pageSize.value
      })
    )
  } catch {
    catalog.value = undefined
  }
}

async function loadJobs(schedule = true) {
  clearTimeout(pollTimer)
  try {
    jobs.value = await $fetch<AssetImportJob[]>(
      assetImportsUrl(config.public.apiBase, props.kind),
      { query: { limit: 20 } }
    )
    error.value = undefined
    if (!activeJob.value) await loadCatalog()
  } catch {
    error.value = 'Asset import jobs could not be loaded from the Studio API.'
  }

  if (schedule && activeJob.value) {
    pollTimer = setTimeout(() => void loadJobs(), 1000)
  }
}

async function queueImport() {
  queueing.value = true
  error.value = undefined
  try {
    await $fetch(assetImportsUrl(config.public.apiBase, props.kind), {
      method: 'POST'
    })
    await loadJobs()
  } catch {
    error.value =
      'The texture import could not be queued. Another import of this kind may already be active.'
  } finally {
    queueing.value = false
  }
}

onMounted(() => void loadJobs())
onBeforeUnmount(() => clearTimeout(pollTimer))
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Asset pipeline"
      :title="title"
      :description="description"
      icon="i-lucide-images"
    >
      <template #actions>
        <UButton
          label="Import jobs"
          icon="i-lucide-history"
          color="neutral"
          variant="outline"
          to="/assets/jobs"
        />
        <UButton
          :label="importLabel"
          icon="i-lucide-play"
          :loading="queueing"
          :disabled="Boolean(activeJob)"
          @click="queueImport"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="Asset import unavailable"
      :description="error"
    />

    <UCard v-if="activeJob" variant="subtle">
      <div class="flex flex-wrap items-center gap-4">
        <UIcon
          name="i-lucide-loader-circle"
          class="size-5 animate-spin text-primary"
        />
        <div class="min-w-0 flex-1">
          <p class="font-medium text-highlighted">
            Import {{ activeJob.status }}
          </p>
          <p class="truncate text-xs text-muted">
            {{ activeJob.sourcePath }}
          </p>
        </div>
        <UBadge color="info" variant="subtle">
          {{ activeJob.processedCount }} / {{ activeJob.totalCount || '…' }}
        </UBadge>
      </div>
      <UProgress
        class="mt-4"
        :model-value="activeJob.processedCount"
        :max="activeJob.totalCount || 1"
      />
    </UCard>

    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <template #header>
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 class="text-sm font-semibold text-highlighted">
              Generated textures
            </h2>
            <p class="text-xs text-muted">
              {{ resolvedCount }} resolved ·
              {{ catalog?.summary.skipped ?? 0 }} skipped ·
              {{ catalog?.summary.groupCount ?? 0 }} packages
            </p>
          </div>
        </div>
      </template>

      <div
        v-if="catalog"
        class="grid min-h-[42rem] md:h-[clamp(40rem,calc(100dvh-20rem),64rem)] md:min-h-0 md:grid-cols-[16rem_minmax(0,1fr)]"
      >
        <aside
          class="border-b border-default bg-elevated/40 md:flex md:min-h-0 md:flex-col md:border-r md:border-b-0"
        >
          <div class="border-b border-default p-3">
            <p
              class="mb-2 text-xs font-semibold tracking-wide text-muted uppercase"
            >
              Packages
            </p>
            <UInput
              v-model="packageQuery"
              icon="i-lucide-search"
              placeholder="Find a package"
              aria-label="Search texture packages"
              size="sm"
            />
          </div>
          <nav
            aria-label="Texture packages"
            class="max-h-72 overflow-y-auto p-2 md:min-h-0 md:flex-1 md:max-h-none"
          >
            <button
              type="button"
              class="flex w-full items-center gap-2 rounded-md px-3 py-2 text-left text-sm transition"
              :class="
                packageFilter === 'all'
                  ? 'bg-primary/10 text-primary'
                  : 'text-muted hover:bg-elevated hover:text-highlighted'
              "
              @click="packageFilter = 'all'"
            >
              <UIcon name="i-lucide-folders" class="size-4 shrink-0" />
              <span class="min-w-0 flex-1 truncate">All packages</span>
              <span class="text-xs tabular-nums">{{
                catalog.summary.total
              }}</span>
            </button>
            <button
              v-for="item in visiblePackages"
              :key="item.name"
              type="button"
              class="mt-0.5 flex w-full items-center gap-2 rounded-md px-3 py-2 text-left text-sm transition"
              :class="
                packageFilter === item.name
                  ? 'bg-primary/10 text-primary'
                  : 'text-muted hover:bg-elevated hover:text-highlighted'
              "
              @click="packageFilter = item.name"
            >
              <UIcon
                :name="
                  packageFilter === item.name
                    ? 'i-lucide-folder-open'
                    : 'i-lucide-folder'
                "
                class="size-4 shrink-0"
              />
              <span class="min-w-0 flex-1 truncate" :title="item.name">{{
                item.name
              }}</span>
              <span class="text-xs tabular-nums">{{ item.textureCount }}</span>
            </button>
          </nav>
        </aside>

        <section class="min-w-0 md:flex md:min-h-0 md:flex-col">
          <div class="border-b border-default p-3">
            <UInput
              v-model="query"
              icon="i-lucide-search"
              placeholder="Search texture objects"
              aria-label="Search texture objects"
              class="w-full"
            />
          </div>
          <div
            class="max-h-[34rem] divide-y divide-default overflow-y-auto md:min-h-0 md:flex-1 md:max-h-none"
          >
            <div
              v-for="texture in visibleTextures"
              :key="`${texture.packageName}/${texture.objectName}`"
              class="flex min-w-0 items-center gap-4 p-4"
            >
              <button
                v-if="texture.url"
                type="button"
                class="group relative shrink-0 rounded-md focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                :aria-label="`Preview ${texture.packageName}.${texture.objectName}`"
                @click="showPreview(texture)"
              >
                <img
                  :src="texture.url"
                  :alt="`${texture.packageName}.${texture.objectName}`"
                  width="64"
                  height="64"
                  class="size-16 rounded-md bg-elevated object-contain transition group-hover:scale-105 group-hover:ring-2 group-hover:ring-primary [image-rendering:pixelated]"
                />
                <span
                  class="absolute right-1 bottom-1 grid size-5 place-items-center rounded bg-black/70 text-white opacity-0 transition group-hover:opacity-100 group-focus-visible:opacity-100"
                >
                  <UIcon name="i-lucide-maximize-2" class="size-3" />
                </span>
              </button>
              <div
                v-else
                class="grid size-16 shrink-0 place-items-center rounded-md bg-elevated text-warning"
              >
                <UIcon name="i-lucide-image-off" class="size-6" />
              </div>
              <div class="min-w-0 flex-1">
                <p class="truncate text-sm font-medium text-highlighted">
                  {{ texture.objectName }}
                </p>
                <p class="mt-1 truncate text-xs text-muted">
                  {{ texture.packageName }} · {{ texture.width }}×{{
                    texture.height
                  }}
                  · {{ texture.format }}
                </p>
                <p
                  v-if="texture.error"
                  class="mt-1 truncate text-xs"
                  :class="
                    isEmptyPlaceholder(texture) ? 'text-muted' : 'text-error'
                  "
                >
                  {{ texture.error }}
                </p>
              </div>
              <UBadge
                :color="textureStatusColor(texture)"
                variant="subtle"
                class="shrink-0"
              >
                {{ isEmptyPlaceholder(texture) ? 'empty' : texture.status }}
              </UBadge>
            </div>
            <div
              v-if="visibleTextures.length === 0"
              class="grid min-h-48 place-items-center p-8 text-center text-sm text-muted"
            >
              No textures match the current folder and search.
            </div>
          </div>
          <StudioTableFooter
            v-model:page="page"
            v-model:page-size="pageSize"
            :total="catalog.total"
            :page-size-options="[50, 100, 200]"
          />
        </section>
      </div>
      <div
        v-else
        class="grid min-h-64 place-items-center p-8 text-center text-sm text-muted"
      >
        No imported texture catalog is available. Queue the first import.
      </div>
    </UCard>

    <UModal
      v-model:open="previewOpen"
      :title="selectedTexture?.objectName"
      :description="
        selectedTexture
          ? `${selectedTexture.packageName} · ${selectedTexture.width}×${selectedTexture.height} · ${selectedTexture.format}`
          : undefined
      "
      :ui="{ content: 'max-w-[min(96vw,80rem)]' }"
    >
      <template #body>
        <div
          class="grid max-h-[78vh] place-items-center overflow-auto bg-black/30 p-2"
        >
          <img
            v-if="selectedTexture?.url"
            :src="selectedTexture.url"
            :alt="`${selectedTexture.packageName}.${selectedTexture.objectName}`"
            :style="{ width: previewWidth(selectedTexture) }"
            class="h-auto max-h-[74vh] max-w-full object-contain [image-rendering:pixelated]"
          />
        </div>
      </template>
    </UModal>
  </div>
</template>
