<script setup lang="ts">
import type { AssetCatalogPage, TextureManifestEntry, TexturePackage } from '~/types/studio'
import type { AssetImportJob } from '../../../types/models/asset-import-job'
import { computed, onBeforeUnmount, watch } from 'vue'
import { getAssetCatalog, getAssetImportJobs, startAssetImport } from '../../../services/studio-api'

const route = useRoute()
const router = useRouter()
const jobs = ref<AssetImportJob[]>([])
const catalog = ref<AssetCatalogPage<TextureManifestEntry, TexturePackage>>()
const query = ref('')
const page = ref(1)
const pageSize = ref(100)
const queueing = ref(false)
const error = ref<string>()
const selectedTexture = ref<TextureManifestEntry>()
let pollTimer: ReturnType<typeof setTimeout> | undefined

const folder = computed(() => route.query.folder === 'systextures' || route.query.folder === 'textures' ? route.query.folder : undefined)
const packageName = computed(() => typeof route.query.package === 'string' ? route.query.package : undefined)
const activeJob = computed(() => jobs.value.find(job => ['queued', 'discovering', 'running'].includes(job.status)))
const folders = ['systextures', 'textures'] as const
const packages = computed(() => catalog.value?.groups ?? [])
const textures = computed(() => catalog.value?.items ?? [])
const previewOpen = computed({
  get: () => selectedTexture.value !== undefined,
  set: open => { if (!open) selectedTexture.value = undefined }
})
const title = computed(() => packageName.value ?? folder.value ?? 'Textures')
const description = computed(() => packageName.value
  ? `/${folder.value}/${packageName.value}`
  : folder.value ? `/${folder.value}` : '/')

watch([() => route.query.folder, () => route.query.package, query, pageSize], () => {
  page.value = 1
  void loadCatalog()
})
watch(page, () => void loadCatalog())

function navigate(nextFolder?: string, nextPackage?: string) {
  void router.push({ path: '/assets/textures', query: {
    ...(nextFolder ? { folder: nextFolder } : {}),
    ...(nextPackage ? { package: nextPackage } : {})
  } })
}

function back() {
  if (packageName.value) navigate(folder.value)
  else if (folder.value) navigate()
}

async function loadCatalog() {
  try {
    catalog.value = await getAssetCatalog<TextureManifestEntry, TexturePackage>('textures', {
      query: query.value,
      originalFolder: folder.value,
      packageName: packageName.value,
      page: page.value,
      pageSize: pageSize.value
    })
  } catch { catalog.value = undefined }
}

async function loadJobs(schedule = true) {
  clearTimeout(pollTimer)
  try {
    jobs.value = await getAssetImportJobs('textures')
    error.value = undefined
    if (!activeJob.value) await loadCatalog()
  } catch { error.value = 'Texture import jobs could not be loaded from the Studio API.' }
  if (schedule && activeJob.value) pollTimer = setTimeout(() => void loadJobs(), 1000)
}

async function queueImport() {
  queueing.value = true
  error.value = undefined
  try { await startAssetImport('textures'); await loadJobs() }
  catch { error.value = 'The texture import could not be queued. Another texture import may already be active.' }
  finally { queueing.value = false }
}

function previewWidth(texture: TextureManifestEntry | undefined) {
  return texture ? `${Math.min(1024, Math.max(256, texture.width * 4))}px` : undefined
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
      <template #header>
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div><h2 class="text-sm font-semibold text-highlighted">{{ title }}</h2><p class="text-xs text-muted">{{ description }}</p></div>
          <UButton v-if="folder" label="Up" icon="i-lucide-arrow-up" color="neutral" variant="outline" @click="back" />
        </div>
        <div class="mt-3 flex items-center gap-1 text-xs text-muted"><button type="button" class="hover:text-primary" @click="navigate()">/</button><template v-if="folder"><span>/</span><button type="button" class="hover:text-primary" @click="navigate(folder)">{{ folder }}</button></template><template v-if="packageName"><span>/</span><span>{{ packageName }}</span></template></div>
      </template>

      <div v-if="catalog" class="min-h-[36rem]">
        <div class="border-b border-default p-3"><UInput v-model="query" icon="i-lucide-search" placeholder="Search texture paths" aria-label="Search texture paths" /></div>
        <div v-if="!folder && !query" class="grid gap-3 p-4 sm:grid-cols-2">
          <button v-for="item in folders" :key="item" type="button" class="flex items-center gap-3 rounded-lg border border-default p-4 text-left hover:bg-elevated" @click="navigate(item)"><UIcon name="i-lucide-folder" class="size-6 text-primary" /><span class="font-medium">{{ item }}</span></button>
        </div>
        <div v-else-if="!packageName && !query" class="divide-y divide-default">
          <button v-for="item in packages" :key="item.path" type="button" class="flex w-full items-center gap-3 p-4 text-left hover:bg-elevated" @click="navigate(folder, item.name)"><UIcon name="i-lucide-folder" class="size-5 text-primary" /><span class="min-w-0 flex-1 truncate font-medium">{{ item.name }}</span><span class="text-xs text-muted">{{ item.textureCount }} textures</span></button>
          <p v-if="packages.length === 0" class="p-8 text-center text-sm text-muted">No texture packages are available in this folder.</p>
        </div>
        <div v-else class="divide-y divide-default">
          <article v-for="texture in textures" :key="texture.path" class="flex min-w-0 items-center gap-4 p-4">
            <button v-if="texture.url" type="button" class="shrink-0" :aria-label="`Preview ${texture.path}`" @click="selectedTexture = texture"><img :src="texture.url" :alt="texture.path" width="64" height="64" class="size-16 rounded-md bg-elevated object-contain [image-rendering:pixelated]" /></button>
            <div v-else class="grid size-16 shrink-0 place-items-center rounded-md bg-elevated text-warning"><UIcon name="i-lucide-image-off" class="size-6" /></div>
            <div class="min-w-0 flex-1"><p class="truncate text-sm font-medium text-highlighted">{{ texture.objectName }}</p><p class="mt-1 truncate text-xs text-muted">{{ texture.path }} · {{ texture.width }}×{{ texture.height }} · {{ texture.format }}</p><p v-if="texture.error" class="mt-1 truncate text-xs text-error">{{ texture.error }}</p></div>
            <UBadge :color="texture.status === 'resolved' ? 'success' : 'warning'" variant="subtle" class="shrink-0">{{ texture.status }}</UBadge>
          </article>
          <p v-if="textures.length === 0" class="p-8 text-center text-sm text-muted">No textures match the current path and search.</p>
          <StudioTableFooter v-model:page="page" v-model:page-size="pageSize" :total="catalog.total" :page-size-options="[50, 100, 200]" />
        </div>
      </div>
      <div v-else class="grid min-h-64 place-items-center p-8 text-center text-sm text-muted">No imported texture catalog is available. Queue the first import.</div>
    </UCard>

    <UModal v-model:open="previewOpen" :title="selectedTexture?.objectName" :description="selectedTexture?.path" :ui="{ content: 'max-w-[min(96vw,80rem)]' }"><template #body><div class="grid max-h-[78vh] place-items-center overflow-auto bg-black/30 p-2"><img v-if="selectedTexture?.url" :src="selectedTexture.url" :alt="selectedTexture.path" :style="{ width: previewWidth(selectedTexture) }" class="h-auto max-h-[74vh] max-w-full object-contain [image-rendering:pixelated]" /></div></template></UModal>
  </div>
</template>
