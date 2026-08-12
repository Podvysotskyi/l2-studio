<script setup lang="ts">
import type { TreeItem } from '@nuxt/ui'
import type { AssetCatalogPage, TextureManifestEntry, TexturePackage } from '~/types/studio'
import type { AssetImportJob } from '../../../types/models/asset-import-job'
import { computed, onBeforeUnmount, watch } from 'vue'
import { getAssetCatalog, getAssetImportJobs, startAssetImport, startAssetResourceImport } from '../../../services/studio-api'

interface TextureTreeItem extends TreeItem {
  folder?: string
  packageName?: string
  children?: TextureTreeItem[]
}

const route = useRoute()
const router = useRouter()
const jobs = ref<AssetImportJob[]>([])
const catalog = ref<AssetCatalogPage<TextureManifestEntry, TexturePackage>>()
const treeCatalog = ref<AssetCatalogPage<TextureManifestEntry, TexturePackage>>()
const query = ref('')
const page = ref(1)
const pageSize = ref(100)
const queueing = ref(false)
const error = ref<string>()
const selectedTexture = ref<TextureManifestEntry>()
const reimporting = ref(false)
let pollTimer: ReturnType<typeof setTimeout> | undefined

const folder = computed(() => typeof route.query.folder === 'string' && route.query.folder.length ? route.query.folder : undefined)
const packageName = computed(() => typeof route.query.package === 'string' ? route.query.package : undefined)
const activeJob = computed(() => jobs.value.find(job => ['queued', 'discovering', 'running'].includes(job.status)))
const textures = computed(() => catalog.value?.items ?? [])
const selectedFolder = computed(() => folder.value && packageName.value ? { folder: folder.value, packageName: packageName.value } : undefined)
const treeItems = computed<TextureTreeItem[]>(() => {
  const groups = (treeCatalog.value?.groups ?? []).filter(item => item.textureCount > 0)
  return [...new Set(groups.map(item => item.originalFolder))].sort().flatMap((sourceFolder) => {
    const children = groups
      .filter(item => item.originalFolder === sourceFolder)
      .map(item => ({
        label: item.name,
        folder: sourceFolder,
        packageName: item.name,
        icon: 'i-lucide-folder'
      }))
    return children.length ? [{
      label: sourceFolder,
      icon: 'i-lucide-folder',
      defaultExpanded: true,
      children
    }] : []
  })
})
const selectedTreeItem = computed(() => treeItems.value
  .flatMap(item => item.children ?? [])
  .find(item => item.folder === folder.value && item.packageName === packageName.value))

watch([folder, packageName], () => {
  selectedTexture.value = undefined
  page.value = 1
  void loadCatalog()
})
watch([query, pageSize], () => {
  page.value = 1
  void loadCatalog()
})
watch(page, () => void loadCatalog())

function selectFolder(item: TextureTreeItem | undefined) {
  if (!item?.folder || !item.packageName) return
  selectedTexture.value = undefined
  void router.push({
    path: '/assets/textures',
    query: { folder: item.folder, package: item.packageName }
  })
}

async function loadTree() {
  try {
    treeCatalog.value = await getAssetCatalog<TextureManifestEntry, TexturePackage>('textures', { pageSize: 1 })
  } catch {
    treeCatalog.value = undefined
  }
}

async function loadCatalog() {
  if (!selectedFolder.value) {
    catalog.value = undefined
    return
  }
  try {
    catalog.value = await getAssetCatalog<TextureManifestEntry, TexturePackage>('textures', {
      query: query.value,
      originalFolder: selectedFolder.value.folder,
      packageName: selectedFolder.value.packageName,
      page: page.value,
      pageSize: pageSize.value
    })
  } catch {
    catalog.value = undefined
  }
}

async function loadJobs(schedule = true) {
  clearTimeout(pollTimer)
  try {
    jobs.value = await getAssetImportJobs('textures')
    error.value = undefined
    if (!activeJob.value) {
      await Promise.all([loadTree(), loadCatalog()])
    }
  } catch {
    error.value = 'Texture import jobs could not be loaded from the Studio API.'
  }
  if (schedule && activeJob.value) pollTimer = setTimeout(() => void loadJobs(), 1000)
}

async function queueImport() {
  queueing.value = true
  error.value = undefined
  try {
    await startAssetImport('textures')
    await loadJobs()
  } catch {
    error.value = 'The texture import could not be queued. Another texture import may already be active.'
  } finally {
    queueing.value = false
  }
}

async function reimportTexture() {
  if (!selectedTexture.value) return
  reimporting.value = true
  error.value = undefined
  try {
    await startAssetResourceImport('textures', selectedTexture.value.objectName, selectedTexture.value.packageName)
    await loadJobs()
  } catch {
    error.value = 'The texture package re-import could not be queued.'
  } finally {
    reimporting.value = false
  }
}

function previewWidth(texture: TextureManifestEntry) {
  return `${Math.min(1024, Math.max(256, texture.width * 4))}px`
}

onMounted(() => void loadJobs())
onBeforeUnmount(() => clearTimeout(pollTimer))
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader eyebrow="Asset pipeline" title="Textures" description="Browse generated textures by their original client path." icon="i-lucide-images">
      <template #actions>
        <UButton label="Import jobs" icon="i-lucide-history" color="neutral" variant="outline" to="/assets/jobs" />
        <UButton label="Import textures" icon="i-lucide-play" :loading="queueing" :disabled="Boolean(activeJob)" @click="queueImport" />
      </template>
    </StudioPageHeader>

    <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" title="Asset import unavailable" :description="error" />
    <UCard v-if="activeJob" variant="subtle">
      <div class="flex items-center gap-4"><UIcon name="i-lucide-loader-circle" class="size-5 animate-spin text-primary" /><div class="min-w-0 flex-1"><p class="font-medium text-highlighted">Import {{ activeJob.status }}</p><p class="truncate text-xs text-muted">{{ activeJob.requestedSourceKey ?? 'Full scan' }}</p></div><UBadge color="info" variant="subtle">{{ activeJob.completedFileCount }} / {{ activeJob.discoveredFileCount || '…' }}</UBadge></div>
      <UProgress class="mt-4" :model-value="activeJob.completedFileCount" :max="activeJob.discoveredFileCount || 1" />
    </UCard>

    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <div v-if="treeCatalog" class="grid min-h-[36rem] md:h-[clamp(40rem,calc(100dvh-20rem),64rem)] md:min-h-0" :class="selectedTexture ? 'md:grid-cols-[16rem_minmax(0,1fr)_minmax(20rem,28rem)]' : 'md:grid-cols-[16rem_minmax(0,1fr)]'">
        <aside class="border-b border-default p-3 md:flex md:min-h-0 md:flex-col md:border-r md:border-b-0">
          <h2 class="mb-3 text-sm font-semibold text-highlighted">Folders</h2>
          <UTree :items="treeItems" :model-value="selectedTreeItem" :get-key="item => item.packageName ? `${item.folder}/${item.packageName}` : item.label ?? ''" class="min-h-0 flex-1 overflow-y-auto" @update:model-value="selectFolder" />
          <p v-if="treeItems.length === 0" class="p-3 text-sm text-muted">No texture folders are available.</p>
        </aside>

        <section class="min-w-0 md:flex md:min-h-0 md:flex-col" aria-label="Texture files">
          <div class="border-b border-default p-3">
            <h2 class="text-sm font-semibold text-highlighted">{{ packageName ?? 'Texture files' }}</h2>
            <p class="mt-1 text-xs text-muted">{{ selectedFolder ? `/${selectedFolder.folder}/${selectedFolder.packageName}` : 'Select a folder to view its textures.' }}</p>
            <UInput v-model="query" class="mt-3" icon="i-lucide-search" placeholder="Search texture paths" aria-label="Search texture paths" :disabled="!selectedFolder" />
          </div>
          <div v-if="selectedFolder && catalog" class="min-h-0 divide-y divide-default overflow-y-auto md:flex-1">
            <button v-for="texture in textures" :key="texture.path" type="button" class="flex w-full min-w-0 items-center gap-4 p-4 text-left hover:bg-elevated" :class="selectedTexture?.path === texture.path ? 'bg-primary/5' : ''" :aria-label="`Select ${texture.path}`" @click="selectedTexture = texture">
              <img v-if="texture.url" :src="texture.url" :alt="texture.path" width="64" height="64" class="size-16 shrink-0 rounded-md bg-elevated object-contain [image-rendering:pixelated]" />
              <div v-else class="grid size-16 shrink-0 place-items-center rounded-md bg-elevated text-warning"><UIcon name="i-lucide-image-off" class="size-6" /></div>
              <span class="min-w-0 flex-1"><span class="block truncate text-sm font-medium text-highlighted">{{ texture.objectName }}</span><span class="mt-1 block truncate text-xs text-muted">{{ texture.path }} · {{ texture.width }}×{{ texture.height }} · {{ texture.format }}</span><span v-if="texture.error" class="mt-1 block truncate text-xs text-error">{{ texture.error }}</span></span>
              <UBadge :color="texture.status === 'resolved' ? 'success' : 'warning'" variant="subtle" class="shrink-0">{{ texture.status }}</UBadge>
            </button>
            <p v-if="textures.length === 0" class="p-8 text-center text-sm text-muted">No textures match the selected folder and search.</p>
          </div>
          <div v-else class="grid min-h-48 place-items-center p-8 text-center text-sm text-muted">Select a folder to view its textures.</div>
          <StudioTableFooter v-if="selectedFolder && catalog" v-model:page="page" v-model:page-size="pageSize" :total="catalog.total" :page-size-options="[50, 100, 200]" />
        </section>

        <aside v-if="selectedTexture" class="min-w-0 border-t border-default md:flex md:min-h-0 md:flex-col md:border-t-0 md:border-l">
          <div class="border-b border-default p-3"><h2 class="truncate text-sm font-semibold text-highlighted">{{ selectedTexture.objectName }}</h2><p class="mt-1 truncate text-xs text-muted">{{ selectedTexture.path }}</p></div>
          <div class="grid min-h-64 flex-1 place-items-center overflow-auto bg-black/30 p-3">
            <img v-if="selectedTexture.url" :src="selectedTexture.url" :alt="selectedTexture.path" :style="{ width: previewWidth(selectedTexture) }" class="h-auto max-h-[58vh] max-w-full object-contain [image-rendering:pixelated]" />
            <div v-else class="text-center text-sm text-muted"><UIcon name="i-lucide-image-off" class="mx-auto mb-2 size-6" />Preview unavailable</div>
          </div>
          <div class="flex items-center justify-between gap-3 border-t border-default p-3 text-xs text-muted"><span>{{ selectedTexture.width }}×{{ selectedTexture.height }} · {{ selectedTexture.format }} · {{ selectedTexture.mipCount }} mips</span><UButton label="Re-import package" icon="i-lucide-rotate-cw" size="xs" color="neutral" variant="outline" :loading="reimporting" @click="reimportTexture" /></div>
        </aside>
      </div>
      <div v-else class="grid min-h-64 place-items-center p-8 text-center text-sm text-muted">No imported texture catalog is available. Queue the first import.</div>
    </UCard>
  </div>
</template>
