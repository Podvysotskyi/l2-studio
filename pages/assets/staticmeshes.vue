<script setup lang="ts">
import type { StaticMeshManifest, StaticMeshManifestEntry } from '@l2/ui'
import { staticMeshManifestUrl } from '@l2/ui'
import { computed, onBeforeUnmount, watch } from 'vue'
import { assetImportsUrl, type AssetImportJob } from '../../lib/studio-content'

const config = useRuntimeConfig()
const jobs = ref<AssetImportJob[]>([])
const manifest = ref<StaticMeshManifest>()
const selectedPackage = ref<string>('all')
const selectedMesh = ref<StaticMeshManifestEntry>()
const previewOpen = ref(false)
const previewError = ref<string>()
const query = ref('')
const page = ref(1)
const pageSize = ref(50)
const queueing = ref(false)
const error = ref<string>()
let pollTimer: ReturnType<typeof setTimeout> | undefined

const activeJob = computed(() =>
  jobs.value.find((job) => job.status === 'queued' || job.status === 'running')
)
const packages = computed(() => manifest.value?.packages ?? [])
const filteredMeshes = computed(() => {
  const term = query.value.trim().toLocaleLowerCase()
  return (manifest.value?.meshes ?? []).filter(
    (mesh) =>
      (selectedPackage.value === 'all' ||
        mesh.packageName === selectedPackage.value) &&
      (!term ||
        mesh.objectName.toLocaleLowerCase().includes(term) ||
        mesh.packageName.toLocaleLowerCase().includes(term))
  )
})
const visibleMeshes = computed(() => {
  const offset = (page.value - 1) * pageSize.value
  return filteredMeshes.value.slice(offset, offset + pageSize.value)
})
const resolvedCount = computed(
  () =>
    manifest.value?.meshes.filter((mesh) => mesh.status === 'resolved')
      .length ?? 0
)
const materialCounts = computed(() =>
  (manifest.value?.meshes ?? []).reduce(
    (total, mesh) => ({
      resolved: total.resolved + (mesh.resolvedMaterialCount ?? 0),
      available: total.available + (mesh.materialCount ?? 0)
    }),
    { resolved: 0, available: 0 }
  )
)

watch([query, selectedPackage, pageSize], () => (page.value = 1))

function showPreview(mesh: StaticMeshManifestEntry) {
  if (!mesh.url) return
  selectedMesh.value = mesh
  previewError.value = undefined
  previewOpen.value = true
}

async function loadManifest() {
  try {
    manifest.value = await $fetch<StaticMeshManifest>(staticMeshManifestUrl(), {
      query: { refresh: Date.now() }
    })
  } catch {
    manifest.value = undefined
  }
}

async function loadJobs(schedule = true) {
  clearTimeout(pollTimer)
  try {
    jobs.value = await $fetch<AssetImportJob[]>(
      assetImportsUrl(config.public.apiBase, 'staticmeshes'),
      { query: { limit: 20 } }
    )
    error.value = undefined
    if (!activeJob.value) await loadManifest()
  } catch {
    error.value =
      'Static-mesh import jobs could not be loaded from the Studio API.'
  }
  if (schedule && activeJob.value)
    pollTimer = setTimeout(() => void loadJobs(), 1000)
}

async function queueImport() {
  queueing.value = true
  error.value = undefined
  try {
    await $fetch(assetImportsUrl(config.public.apiBase, 'staticmeshes'), {
      method: 'POST'
    })
    await loadJobs()
  } catch {
    error.value =
      'The static-mesh import could not be queued. Another import may already be active.'
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
      title="Static meshes"
      description="Convert Interlude StaticMesh geometry, sections, and classic materials into browser-previewable GLB assets."
      icon="i-lucide-box"
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
          label="Import static meshes"
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
      title="Static-mesh import unavailable"
      :description="error"
    />
    <UCard v-if="activeJob" variant="subtle">
      <div class="flex items-center gap-4">
        <UIcon
          name="i-lucide-loader-circle"
          class="size-5 animate-spin text-primary"
        />
        <div class="min-w-0 flex-1">
          <p class="font-medium text-highlighted">
            Import {{ activeJob.status }}
          </p>
          <p class="truncate text-xs text-muted">{{ activeJob.sourcePath }}</p>
        </div>
        <UBadge color="info" variant="subtle">
          {{ activeJob.processedCount }} /
          {{ activeJob.totalCount || '…' }}
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
            <h2 class="text-sm font-semibold text-highlighted">Mesh library</h2>
            <p class="text-xs text-muted">
              {{ resolvedCount }} resolved ·
              {{ (manifest?.meshes.length ?? 0) - resolvedCount }} skipped ·
              {{ materialCounts.resolved.toLocaleString() }} /
              {{ materialCounts.available.toLocaleString() }} materials
            </p>
          </div>
          <UInput
            v-model="query"
            icon="i-lucide-search"
            placeholder="Search meshes"
            aria-label="Search static meshes"
            class="w-full sm:w-72"
          />
        </div>
      </template>
      <div
        v-if="manifest"
        class="grid min-h-[32rem] md:grid-cols-[16rem_minmax(0,1fr)]"
      >
        <aside class="border-b border-default p-2 md:border-r md:border-b-0">
          <button
            type="button"
            class="flex w-full items-center justify-between rounded-md px-3 py-2 text-left text-sm"
            :class="
              selectedPackage === 'all'
                ? 'bg-primary/10 text-primary'
                : 'text-muted hover:bg-elevated'
            "
            @click="selectedPackage = 'all'"
          >
            <span>All packages</span><span>{{ manifest.meshes.length }}</span>
          </button>
          <div class="max-h-[42rem] overflow-y-auto">
            <button
              v-for="item in packages"
              :key="item.name"
              type="button"
              class="flex w-full items-center justify-between gap-2 rounded-md px-3 py-2 text-left text-sm"
              :class="
                selectedPackage === item.name
                  ? 'bg-primary/10 text-primary'
                  : 'text-muted hover:bg-elevated'
              "
              @click="selectedPackage = item.name"
            >
              <span class="truncate">{{ item.name }}</span
              ><span class="shrink-0 text-xs">{{ item.meshCount }}</span>
            </button>
          </div>
        </aside>
        <section class="min-w-0">
          <div class="max-h-[42rem] divide-y divide-default overflow-y-auto">
            <button
              v-for="mesh in visibleMeshes"
              :key="`${mesh.packageName}/${mesh.objectName}`"
              type="button"
              class="flex w-full items-center gap-4 p-4 text-left hover:bg-elevated disabled:cursor-not-allowed"
              :disabled="!mesh.url"
              @click="showPreview(mesh)"
            >
              <span
                class="grid size-14 shrink-0 place-items-center rounded-lg bg-elevated text-primary"
                ><UIcon name="i-lucide-box" class="size-7"
              /></span>
              <span class="min-w-0 flex-1"
                ><span
                  class="block truncate text-sm font-medium text-highlighted"
                  >{{ mesh.objectName }}</span
                ><span class="mt-1 block truncate text-xs text-muted"
                  >{{ mesh.packageName }} ·
                  {{ mesh.vertexCount.toLocaleString() }} vertices ·
                  {{ mesh.triangleCount.toLocaleString() }} triangles ·
                  {{ mesh.sectionCount }} sections ·
                  {{ mesh.resolvedMaterialCount ?? 0 }} /
                  {{ mesh.materialCount ?? 0 }} materials</span
                ><span
                  v-if="mesh.materialError || mesh.error"
                  class="mt-1 block truncate text-xs text-error"
                  >{{ mesh.materialError || mesh.error }}</span
                ></span
              >
              <UBadge
                v-if="mesh.materialStatus && mesh.materialStatus !== 'none'"
                :color="
                  mesh.materialStatus === 'resolved' ? 'success' : 'warning'
                "
                variant="subtle"
              >
                {{ mesh.materialStatus }} materials
              </UBadge>
              <UBadge
                :color="mesh.status === 'resolved' ? 'success' : 'warning'"
                variant="subtle"
              >
                {{ mesh.status }}
              </UBadge>
              <UIcon
                v-if="mesh.url"
                name="i-lucide-maximize-2"
                class="size-4 text-muted"
              />
            </button>
            <div
              v-if="visibleMeshes.length === 0"
              class="grid min-h-48 place-items-center p-8 text-sm text-muted"
            >
              No meshes match the current package and search.
            </div>
          </div>
          <StudioTableFooter
            v-model:page="page"
            v-model:page-size="pageSize"
            :total="filteredMeshes.length"
            :page-size-options="[50, 100, 200]"
          />
        </section>
      </div>
      <div
        v-else
        class="grid min-h-64 place-items-center p-8 text-sm text-muted"
      >
        No generated static-mesh manifest is available. Queue the first import.
      </div>
    </UCard>

    <UModal
      v-model:open="previewOpen"
      :title="selectedMesh?.objectName"
      :description="
        selectedMesh
          ? `${selectedMesh.packageName} · ${selectedMesh.vertexCount.toLocaleString()} vertices · ${selectedMesh.triangleCount.toLocaleString()} triangles`
          : undefined
      "
      :ui="{ content: 'max-w-[min(96vw,90rem)]' }"
    >
      <template #body>
        <UAlert
          v-if="previewError"
          class="mb-3"
          color="error"
          variant="subtle"
          title="Preview unavailable"
          :description="previewError"
        />
        <StudioStaticMeshPreview
          v-if="selectedMesh?.url"
          :url="selectedMesh.url"
          @error="previewError = $event"
        />
        <p class="mt-2 text-center text-xs text-muted">
          Drag to orbit · scroll to zoom · right-drag to pan
        </p>
      </template>
    </UModal>
  </div>
</template>
