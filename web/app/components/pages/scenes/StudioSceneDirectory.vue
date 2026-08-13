<script setup lang="ts">
import type { AssetCatalogPage, SceneCatalogEntry } from '~/types/studio'
import type { AssetImportJob } from '../../../types/models/asset-import-job'
import { computed, onBeforeUnmount } from 'vue'
import {
  getAssetCatalog,
  getAssetImportJobs,
  startAssetImport
} from '../../../services/studio-api'
import { assetImportProgressItem } from '../../../utils/import-progress'

const jobs = ref<AssetImportJob[]>([])
const catalog = ref<AssetCatalogPage<SceneCatalogEntry>>()
const queueing = ref(false)
const error = ref<string>()
const progressJobId = ref<string>()
const importDrawerOpen = ref(false)
let pollTimer: ReturnType<typeof setTimeout> | undefined

const activeJob = computed(() =>
  jobs.value.find((job) =>
    ['queued', 'discovering', 'running'].includes(job.status)
  )
)
const progressItems = computed(() => {
  const job = jobs.value.find(item => item.id === progressJobId.value)
  return job ? [assetImportProgressItem(job, 'Scenes')] : []
})

async function loadCatalog() {
  try {
    catalog.value = await getAssetCatalog<SceneCatalogEntry>('scenes', {
      pageSize: 500
    })
  } catch {
    catalog.value = undefined
  }
}

async function loadJobs(schedule = true) {
  clearTimeout(pollTimer)
  try {
    jobs.value = await getAssetImportJobs('scenes')
    if (activeJob.value && activeJob.value.id !== progressJobId.value) {
      progressJobId.value = activeJob.value.id
      importDrawerOpen.value = true
    }
    error.value = undefined
    if (!activeJob.value) await loadCatalog()
  } catch {
    error.value = 'Scene import jobs could not be loaded from the Studio API.'
  }
  if (schedule && activeJob.value)
    pollTimer = setTimeout(() => void loadJobs(), 1000)
}

async function queueImport() {
  queueing.value = true
  try {
    const job = await startAssetImport('scenes')
    progressJobId.value = job.id
    importDrawerOpen.value = true
    await loadJobs()
  } catch {
    error.value = 'The scene import could not be queued.'
  } finally {
    queueing.value = false
  }
}

onMounted(() => {
  void loadCatalog()
  void loadJobs()
})
onBeforeUnmount(() => clearTimeout(pollTimer))
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Asset pipeline"
      title="Scenes"
      description="Inspect non-world Unreal packages such as the client lobby, entry scene, sky scene, and support layouts."
      icon="i-lucide-clapperboard"
    >
      <template #actions>
        <UButton
          label="Import scenes"
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
      title="Scenes unavailable"
      :description="error"
    />
    <div v-if="catalog?.items.length" class="grid gap-4 lg:grid-cols-2">
      <UCard v-for="scene in catalog.items" :key="scene.sourceKey">
        <div class="flex items-start gap-4">
          <span
            class="grid size-12 shrink-0 place-items-center rounded-xl bg-primary/10 text-primary"
          >
            <UIcon name="i-lucide-clapperboard" class="size-6" />
          </span>
          <div class="min-w-0 flex-1">
            <div class="flex items-center gap-2">
              <h2 class="font-semibold text-highlighted">{{ scene.name }}</h2>
              <UBadge
                :color="scene.status === 'resolved' ? 'success' : 'warning'"
                variant="subtle"
              >
                {{ scene.status }}
              </UBadge>
            </div>
            <p class="mt-1 text-xs text-muted">{{ scene.sourceKey }}</p>
            <p class="mt-3 text-sm text-muted">
              {{ scene.actorCount.toLocaleString() }} meshes ·
              {{ scene.cinematicObjectCount.toLocaleString() }} cinematic
              objects
            </p>
            <p v-if="scene.error" class="mt-2 text-xs text-error">
              {{ scene.error }}
            </p>
          </div>
          <UButton
            label="Open scene"
            icon="i-lucide-arrow-right"
            trailing
            color="neutral"
            variant="outline"
            :disabled="!scene.manifestUrl"
            :to="
              scene.manifestUrl
                ? { name: 'library-scenes-name', params: { name: scene.name }, query: { source: scene.sourceKey } }
                : undefined
            "
          />
        </div>
      </UCard>
    </div>
    <UCard v-else>
      <p class="py-16 text-center text-sm text-muted">
        No generated scene catalog is available. Queue the first import.
      </p>
    </UCard>

    <StudioImportProgressDrawer
      v-model:open="importDrawerOpen"
      :items="progressItems"
    />
  </div>
</template>
