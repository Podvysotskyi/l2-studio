<script setup lang="ts">
import type {
  AnimationManifestPackage,
  AnimationMeshManifestEntry,
  AssetCatalogPage
} from '~/types/studio'
import type { AssetImportJob } from '../../../types/models/asset-import-job'
import { computed, onBeforeUnmount, watch } from 'vue'
import {
  getAssetCatalog,
  getAssetImportJobs,
  startAssetImport,
  startAssetResourceImport
} from '../../../services/studio-api'
import { selectedGameVersionKey } from '../../../utils/game-version'
import { assetImportProgressItem } from '../../../utils/import-progress'

const jobs = ref<AssetImportJob[]>([])
const catalog = ref<AssetCatalogPage<AnimationMeshManifestEntry, AnimationManifestPackage>>()
const selectedPackage = ref<string>('all')
const selectedMesh = ref<AnimationMeshManifestEntry>()
const query = ref('')
const page = ref(1)
const pageSize = ref(50)
const queueing = ref(false)
const reimporting = ref<string>()
const error = ref<string>()
const previewError = ref<string>()
const progressJobId = ref<string>()
const importDrawerOpen = ref(false)
const notifications = useStudioToasts()
const isC1 = selectedGameVersionKey() === 'c1'
let pollTimer: ReturnType<typeof setTimeout> | undefined

const activeJob = computed(() => jobs.value.find(job => ['queued', 'discovering', 'running'].includes(job.status)))
const packages = computed(() => catalog.value?.groups ?? [])
const selectedPackageEntry = computed(() => packages.value.find(item => item.sourceKey === selectedPackage.value))
const progressItems = computed(() => {
  const job = jobs.value.find(item => item.id === progressJobId.value)
  return job ? [assetImportProgressItem(job, 'Animations')] : []
})

watch([query, selectedPackage, pageSize], () => { page.value = 1; void loadCatalog() })
watch(page, () => void loadCatalog())

async function loadCatalog() {
  if (!isC1) return
  try {
    catalog.value = await getAssetCatalog<AnimationMeshManifestEntry, AnimationManifestPackage>('animations', {
      query: query.value,
      packageName: selectedPackageEntry.value?.name,
      page: page.value,
      pageSize: pageSize.value
    })
  } catch { catalog.value = undefined }
}

async function loadJobs(schedule = true) {
  clearTimeout(pollTimer)
  if (!isC1) return
  try {
    jobs.value = await getAssetImportJobs('animations')
    if (activeJob.value && activeJob.value.id !== progressJobId.value) {
      progressJobId.value = activeJob.value.id
      importDrawerOpen.value = true
    }
    error.value = undefined
    if (!activeJob.value) await loadCatalog()
  } catch { error.value = 'Animation import jobs could not be loaded from the Studio API.' }
  if (schedule && activeJob.value) pollTimer = setTimeout(() => void loadJobs(), 1000)
}

async function queueImport(force = false) {
  queueing.value = true
  try {
    const job = await startAssetImport('animations', { force })
    progressJobId.value = job.id
    importDrawerOpen.value = true
    await loadJobs()
  } catch {
    notifications.error({ title: 'Animation import could not be queued', description: 'Another import may already be active.' })
  } finally { queueing.value = false }
}

async function reimport(mesh: AnimationMeshManifestEntry, force = false) {
  reimporting.value = `${mesh.sourceKey}/${mesh.objectName}`
  try {
    const job = await startAssetResourceImport('animations', mesh.objectName, mesh.packageName, mesh.sourceKey, force)
    progressJobId.value = job.id
    importDrawerOpen.value = true
    await loadJobs()
  } catch { notifications.error({ title: 'Animation package re-import could not be queued' }) }
  finally { reimporting.value = undefined }
}

function selectMesh(mesh: AnimationMeshManifestEntry) { selectedMesh.value = mesh; previewError.value = undefined }
function notifyLabel(mesh: AnimationMeshManifestEntry) { return mesh.clips.reduce((total, clip) => total + clip.notifies.length, 0) }

onMounted(() => void loadJobs())
onBeforeUnmount(() => clearTimeout(pollTimer))
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Asset pipeline"
      title="Animations"
      description="Import Chronicle 1 UKX skeletal meshes, clips, and animation-notify timelines into browser-playable GLB assets."
      icon="i-lucide-person-standing"
    >
      <template #actions>
        <UButton label="Import jobs" icon="i-lucide-history" color="neutral" variant="outline" to="/pipeline/imports" />
        <UDropdownMenu :items="[[
          { label: 'Import animations', icon: 'i-lucide-play', onSelect: () => queueImport() },
          { label: 'Force rebuild animations', icon: 'i-lucide-hammer', color: 'warning', onSelect: () => queueImport(true) }
        ]]" :content="{ align: 'end' }">
          <UButton label="Import animations" trailing-icon="i-lucide-chevron-down" :loading="queueing" :disabled="!isC1 || Boolean(activeJob)" />
        </UDropdownMenu>
      </template>
    </StudioPageHeader>
    <UAlert v-if="!isC1" color="warning" variant="subtle" icon="i-lucide-info" title="Chronicle 1 only" description="UKX animation import is currently validated only for Chronicle 1. Select Chronicle 1 to browse or import animations." />
    <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" title="Animation import unavailable" :description="error" />
    <UCard v-if="isC1" :ui="{ body: 'p-0 sm:p-0' }">
      <template #header>
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div><h2 class="text-sm font-semibold text-highlighted">Skeletal animation library</h2><p class="text-xs text-muted">{{ catalog?.summary.resolved ?? 0 }} resolved · {{ catalog?.summary.skipped ?? 0 }} skipped</p></div>
          <UInput v-model="query" icon="i-lucide-search" placeholder="Search skeletal meshes" class="w-full sm:w-72" />
        </div>
      </template>
      <div v-if="catalog" class="grid min-h-[36rem] md:h-[clamp(42rem,calc(100dvh-18rem),68rem)] md:min-h-0 md:grid-cols-[16rem_minmax(0,1fr)]">
        <aside class="border-b border-default p-2 md:flex md:min-h-0 md:flex-col md:border-r md:border-b-0">
          <button class="flex w-full justify-between rounded-md px-3 py-2 text-sm" :class="selectedPackage === 'all' ? 'bg-primary/10 text-primary' : 'text-muted'" @click="selectedPackage = 'all'">All packages <span>{{ catalog.summary.total }}</span></button>
          <div class="overflow-y-auto">
            <button v-for="item in packages" :key="item.sourceKey" class="flex w-full items-center justify-between gap-2 rounded-md px-3 py-2 text-left text-sm" :class="selectedPackage === item.sourceKey ? 'bg-primary/10 text-primary' : 'text-muted hover:bg-elevated'" @click="selectedPackage = item.sourceKey">
              <span class="min-w-0"><span class="block truncate">{{ item.name }}</span><span class="block truncate text-xs opacity-70">{{ item.clipCount }} clips · {{ item.notifyCount }} notifies</span></span><span>{{ item.skeletalMeshCount }}</span>
            </button>
          </div>
        </aside>
        <section class="min-w-0 overflow-y-auto">
          <button v-for="mesh in catalog.items" :key="`${mesh.sourceKey}/${mesh.objectName}`" class="grid w-full gap-3 border-b border-default p-4 text-left hover:bg-elevated sm:grid-cols-[1fr_auto]" @click="selectMesh(mesh)">
            <span class="min-w-0"><strong class="block truncate text-sm text-highlighted">{{ mesh.objectName }}</strong><span class="mt-1 block text-xs text-muted">{{ mesh.boneCount }} bones · {{ mesh.vertexCount.toLocaleString() }} vertices · {{ mesh.clips.length }} clips · {{ notifyLabel(mesh) }} notifies</span><span v-if="mesh.error" class="mt-1 block text-xs text-error">{{ mesh.error }}</span></span>
            <UBadge :color="mesh.status === 'resolved' ? 'success' : 'warning'" variant="subtle">{{ mesh.status }}</UBadge>
          </button>
          <StudioTableFooter v-model:page="page" v-model:page-size="pageSize" :total="catalog.total" :page-size-options="[25, 50, 100]" />
        </section>
      </div>
      <div v-else class="p-10 text-center text-sm text-muted">Import C1 UKX packages to build the animation catalog.</div>
    </UCard>

    <USlideover :open="Boolean(selectedMesh)" :title="selectedMesh?.objectName" :description="selectedMesh?.animationSetName ?? 'Bind pose'" side="right" :ui="{ content: 'max-w-5xl' }" @update:open="open => { if (!open) selectedMesh = undefined }">
      <template #body>
        <div v-if="selectedMesh" class="space-y-4">
          <UAlert v-if="previewError" color="error" :description="previewError" />
          <UCard v-if="selectedMesh.url" :ui="{ body: 'p-0 sm:p-0' }">
            <StudioAnimationPreview :url="selectedMesh.url" :animation-url="selectedMesh.animationUrl" @error="previewError = $event">
              <template #default="{ state }">
                <div v-if="selectedMesh.clips.find(clip => clip.name === state.clipName)?.notifies.length" class="space-y-2">
                  <p class="text-xs font-semibold text-highlighted">Notify timeline</p>
                  <div class="relative h-5 rounded bg-elevated">
                    <span v-for="(notify, index) in selectedMesh.clips.find(clip => clip.name === state.clipName)?.notifies" :key="index" class="absolute top-0 h-5 w-0.5 bg-warning" :style="{ left: `${notify.normalizedTime * 100}%` }" :title="`${notify.timeSeconds.toFixed(2)}s · ${notify.className ?? notify.functionName}`" />
                  </div>
                  <div class="max-h-44 space-y-2 overflow-y-auto">
                    <div v-for="(notify, index) in selectedMesh.clips.find(clip => clip.name === state.clipName)?.notifies" :key="index" class="rounded-md border border-default p-2 text-xs">
                      <div class="flex justify-between gap-3"><strong class="text-highlighted">{{ notify.className ?? notify.functionName ?? 'Notify' }}</strong><span class="text-muted">{{ notify.timeSeconds.toFixed(2) }}s</span></div>
                      <p v-if="notify.objectPath" class="mt-1 break-all text-muted">{{ notify.objectPath }}</p>
                      <dl v-if="Object.keys(notify.properties).length" class="mt-2 grid grid-cols-[auto_1fr] gap-x-3 gap-y-1 text-muted">
                        <template v-for="(value, key) in notify.properties" :key="key"><dt>{{ key }}</dt><dd class="break-all text-right">{{ value }}</dd></template>
                      </dl>
                    </div>
                  </div>
                </div>
              </template>
            </StudioAnimationPreview>
          </UCard>
          <div class="flex gap-2"><UButton label="Re-import package" variant="outline" :loading="Boolean(reimporting)" @click="reimport(selectedMesh)" /><UButton label="Force rebuild" color="warning" variant="soft" @click="reimport(selectedMesh, true)" /></div>
        </div>
      </template>
    </USlideover>
    <StudioImportProgressDrawer v-model:open="importDrawerOpen" title="Animation import" :items="progressItems" />
  </div>
</template>
