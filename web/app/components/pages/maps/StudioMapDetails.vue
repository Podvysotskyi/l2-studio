<script setup lang="ts">
import type {
  MapActorManifestEntry,
  MapBspMeshManifestEntry,
  MapCatalogEntry,
  MapLightManifestEntry,
  MapManifest,
  MapPlayerStartManifestEntry,
  MapPreviewCatalogEntry,
  MapWaterVolumeManifestEntry
} from '~/types/studio'
import type { AssetImportJob } from '../../../types/models/asset-import-job'
import { computed, nextTick, onBeforeUnmount, watch } from 'vue'
import { useRoute } from 'vue-router'
import {
  getAssetCatalogEntry,
  getAssetImportJob,
  startAssetFileImport
} from '../../../services/studio-api'
import { getPublishedManifestWithRaw } from '../../../services/published-assets'
import {
  createTerrainLayerStates,
  enableAllTerrainLayers,
  filterMapLights,
  filterMapWaterVolumes,
  hasMapLevelSummaryData,
  mapSkyZonePreviewManifest,
  mapIdealPlayerCount,
  mapEnvironmentColor,
  mapLightColor,
  previewableMapSkyZones,
  setTerrainLayerEnabled,
  toggleSoloTerrainLayer,
  type TerrainLayerStates
} from '../../../utils/map-inspector'
import { filterMapActors } from '../../../utils/map-actors'
import { filterMapPlayerStarts } from '../../../utils/map-spawns'
import { paginate } from '../../../utils/directory'

interface MapPreviewApi {
  focusActor(name: string): void
  focusPlayerStart(name: string): void
  focusBsp(name: string): void
  focusLight(name: string): void
  focusWater(name: string): void
  focusWaterSurface(name: string): void
  frameBsp(): void
}

type InspectorTab =
  | 'actors'
  | 'spawns'
  | 'bsp'
  | 'terrain'
  | 'lights'
  | 'water'
  | 'summary'
  | 'environment'

const route = useRoute()
const catalogEntry = ref<MapCatalogEntry>()
const manifest = ref<MapManifest>()
const rawManifest = ref<MapManifest>()
const mapPreview = ref<MapPreviewCatalogEntry>()
const mapPreviewJob = ref<AssetImportJob>()
const preview = ref<MapPreviewApi>()
const selectedActorName = ref<string>()
const selectedPlayerStartName = ref<string>()
const selectedBspName = ref<string>()
const selectedLightName = ref<string>()
const selectedWaterName = ref<string>()
const selectedWaterSurfaceName = ref<string>()
const inspectorTab = ref<InspectorTab>('actors')
const actorsVisible = ref(true)
const playerStartsVisible = ref(true)
const bspVisible = ref(true)
const worldBaseVisible = ref(false)
const lightHelpersVisible = ref(false)
const waterVolumesVisible = ref(false)
const waterSurfacesVisible = ref(true)
const terrainLayerStates = ref<TerrainLayerStates>({})
const query = ref('')
const spawnQuery = ref('')
const lightQuery = ref('')
const waterQuery = ref('')
const page = ref(1)
const pageSize = ref(50)
const spawnPage = ref(1)
const spawnPageSize = ref(50)
const loading = ref(true)
const sceneReady = ref(false)
const error = ref<string>()
const previewError = ref<string>()
const terrainMaterialError = ref<string>()
const diagnosticsOpen = ref(false)
const skyZonePreviewOpen = ref(false)
const selectedSkyZoneName = ref<string>()
const skyZonePreviewError = ref<string>()
const mapPreviewJobError = ref<string>()
const notifications = useStudioToasts()
let mapPreviewPollTimer: ReturnType<typeof setTimeout> | undefined

const routeName = computed(() =>
  Array.isArray(route.params.name)
    ? (route.params.name[0] ?? '')
    : (route.params.name ?? '')
)
const routeSourceKey = computed(() =>
  typeof route.query.source === 'string' ? route.query.source : undefined
)
const unresolvedActors = computed(
  () => manifest.value?.actors.filter((actor) => !actor.meshUrl).length ?? 0
)
const terrainLayerCount = computed(
  () =>
    manifest.value?.terrains.reduce(
      (count, terrain) => count + (terrain.layers?.length ?? 0),
      0
    ) ?? 0
)
const terrainMaterialsResolved = computed(
  () =>
    manifest.value?.terrains.every(
      (terrain) => terrain.materialStatus === 'resolved'
    ) ?? false
)
const worldBspMeshes = computed(() =>
  (manifest.value?.bspMeshes ?? []).filter((mesh) => mesh.role === 'geometry')
)
const waterSurfaceMeshes = computed(() =>
  (manifest.value?.bspMeshes ?? []).filter(
    (mesh) => mesh.role === 'water-surface'
  )
)
const skyZoneBspMeshes = computed(() =>
  (manifest.value?.bspMeshes ?? []).filter((mesh) => mesh.role === 'sky-zone')
)
const previewableSkyZones = computed(() =>
  manifest.value ? previewableMapSkyZones(manifest.value) : []
)
const skyZonePreviewOptions = computed(() =>
  previewableSkyZones.value.map((zone) => zone.name)
)
const skyZonePreviewManifest = computed(() =>
  manifest.value
    ? mapSkyZonePreviewManifest(manifest.value, selectedSkyZoneName.value)
    : undefined
)
const worldBaseBspMeshes = computed(() =>
  (manifest.value?.bspMeshes ?? []).filter((mesh) => mesh.role === 'world-base')
)
const bspTotals = computed(() =>
  worldBspMeshes.value.reduce(
    (totals, mesh) => ({
      vertices: totals.vertices + mesh.vertexCount,
      triangles: totals.triangles + mesh.triangleCount,
      surfaces: totals.surfaces + mesh.surfaceCount,
      errors: totals.errors + (mesh.error && !mesh.meshUrl ? 1 : 0),
      fallbacks:
        totals.fallbacks +
        (mesh.materialStatus === 'resolved' || mesh.materialStatus === 'none'
          ? 0
          : 1)
    }),
    { vertices: 0, triangles: 0, surfaces: 0, errors: 0, fallbacks: 0 }
  )
)
const filteredActors = computed(() =>
  filterMapActors(manifest.value?.actors ?? [], query.value)
)
const visibleActors = computed(() =>
  paginate(filteredActors.value, page.value, pageSize.value)
)
const playerStarts = computed(() => manifest.value?.playerStarts ?? [])
const filteredPlayerStarts = computed(() =>
  filterMapPlayerStarts(playerStarts.value, spawnQuery.value)
)
const visiblePlayerStarts = computed(() =>
  paginate(filteredPlayerStarts.value, spawnPage.value, spawnPageSize.value)
)
const filteredLights = computed(() =>
  filterMapLights(manifest.value?.lights ?? [], lightQuery.value)
)
const filteredWaterVolumes = computed(() =>
  filterMapWaterVolumes(manifest.value?.waterVolumes ?? [], waterQuery.value)
)
const filteredWaterSurfaces = computed(() => {
  const normalized = waterQuery.value.trim().toLocaleLowerCase()
  if (!normalized) return waterSurfaceMeshes.value
  return waterSurfaceMeshes.value.filter(
    (surface) =>
      surface.name.toLocaleLowerCase().includes(normalized) ||
      surface.waterVolumeNames.some((name) =>
        name.toLocaleLowerCase().includes(normalized)
      )
  )
})
const terrainLayerVisibility = computed(() =>
  Object.fromEntries(
    Object.entries(terrainLayerStates.value).map(([name, state]) => [
      name,
      state.enabled
    ])
  )
)
const levelSummary = computed(() => manifest.value?.summary ?? null)
const idealPlayerCount = computed(() => mapIdealPlayerCount(levelSummary.value))
const levelSummaryHasData = computed(() =>
  levelSummary.value ? hasMapLevelSummaryData(levelSummary.value) : false
)
const mapPreviewJobActive = computed(() =>
  mapPreviewJob.value
    ? ['queued', 'discovering', 'running'].includes(mapPreviewJob.value.status)
    : false
)

watch([query, pageSize], () => (page.value = 1))
watch([spawnQuery, spawnPageSize], () => (spawnPage.value = 1))
watch([routeName, routeSourceKey], () => void loadMap(), { immediate: true })
watch(selectedSkyZoneName, () => (skyZonePreviewError.value = undefined))
onBeforeUnmount(() => clearTimeout(mapPreviewPollTimer))

function selectActor(actor: MapActorManifestEntry) {
  selectedActorName.value = actor.name
}

function selectPlayerStart(playerStart: MapPlayerStartManifestEntry) {
  selectedPlayerStartName.value = playerStart.name
}

function selectBsp(bsp: MapBspMeshManifestEntry) {
  selectedBspName.value = bsp.name
}

async function focusBsp(bsp: MapBspMeshManifestEntry) {
  if (
    !bsp.meshUrl ||
    bsp.role === 'sky-zone' ||
    (bsp.role === 'geometry' && !bspVisible.value) ||
    (bsp.role === 'world-base' && !worldBaseVisible.value)
  )
    return
  selectBsp(bsp)
  await nextTick()
  preview.value?.focusBsp(bsp.name)
}

async function focusActor(actor: MapActorManifestEntry) {
  if (!actor.meshUrl || !actorsVisible.value) return
  selectActor(actor)
  await nextTick()
  preview.value?.focusActor(actor.name)
}

async function focusPlayerStart(playerStart: MapPlayerStartManifestEntry) {
  if (!playerStartsVisible.value) return
  selectPlayerStart(playerStart)
  await nextTick()
  preview.value?.focusPlayerStart(playerStart.name)
}

function layerEnabled(terrainName: string, index: number) {
  return terrainLayerStates.value[terrainName]?.enabled[index] ?? true
}

function setLayerEnabled(terrainName: string, index: number, enabled: boolean) {
  const state = terrainLayerStates.value[terrainName]
  if (!state) return
  terrainLayerStates.value[terrainName] = setTerrainLayerEnabled(
    state,
    index,
    enabled
  )
}

function enableAllLayers(terrainName: string) {
  const state = terrainLayerStates.value[terrainName]
  if (!state) return
  terrainLayerStates.value[terrainName] = enableAllTerrainLayers(state)
}

function soloLayer(terrainName: string, index: number) {
  const state = terrainLayerStates.value[terrainName]
  if (!state) return
  terrainLayerStates.value[terrainName] = toggleSoloTerrainLayer(state, index)
}

function selectLight(light: MapLightManifestEntry) {
  selectedLightName.value = light.name
  lightHelpersVisible.value = true
}

async function focusLight(light: MapLightManifestEntry) {
  selectLight(light)
  await nextTick()
  preview.value?.focusLight(light.name)
}

function selectWater(volume: MapWaterVolumeManifestEntry) {
  selectedWaterName.value = volume.name
}

async function focusWater(volume: MapWaterVolumeManifestEntry) {
  if (volume.status !== 'resolved' || !waterVolumesVisible.value) return
  selectWater(volume)
  await nextTick()
  preview.value?.focusWater(volume.name)
}

function selectWaterSurface(surface: MapBspMeshManifestEntry) {
  selectedWaterSurfaceName.value = surface.name
}

async function focusWaterSurface(surface: MapBspMeshManifestEntry) {
  if (!surface.meshUrl || !waterSurfacesVisible.value) return
  selectWaterSurface(surface)
  await nextTick()
  preview.value?.focusWaterSurface(surface.name)
}

function openSkyZonePreview() {
  if (!skyZonePreviewOptions.value.includes(selectedSkyZoneName.value ?? ''))
    selectedSkyZoneName.value = skyZonePreviewOptions.value[0]
  if (!selectedSkyZoneName.value) return
  skyZonePreviewOpen.value = true
}

async function loadMapPreview(entry: MapCatalogEntry) {
  try {
    mapPreview.value = await getAssetCatalogEntry<MapPreviewCatalogEntry>(
      'mappreviews',
      entry.name,
      entry.sourceKey
    )
  } catch {
    mapPreview.value = undefined
  }
}

function scheduleMapPreviewPoll() {
  clearTimeout(mapPreviewPollTimer)
  mapPreviewPollTimer = setTimeout(() => void pollMapPreviewJob(), 1000)
}

async function pollMapPreviewJob() {
  const job = mapPreviewJob.value
  if (!job) return

  try {
    const latestJob = await getAssetImportJob('mappreviews', job.id)
    if (mapPreviewJob.value?.id !== latestJob.id) return
    mapPreviewJob.value = latestJob
    if (['queued', 'discovering', 'running'].includes(latestJob.status)) {
      scheduleMapPreviewPoll()
      return
    }

    if (catalogEntry.value) await loadMapPreview(catalogEntry.value)
    if (latestJob.status === 'failed') {
      mapPreviewJobError.value =
        latestJob.error ?? 'Map preview generation failed.'
      return
    }
    notifications.success({ title: 'Map preview regenerated' })
  } catch {
    mapPreviewJobError.value =
      'Map preview generation status could not be loaded.'
  }
}

async function regenerateMapPreview() {
  const entry = catalogEntry.value
  if (!entry || mapPreviewJobActive.value) return

  mapPreviewJobError.value = undefined
  try {
    mapPreviewJob.value = await startAssetFileImport(
      'mappreviews',
      entry.sourceKey,
      true
    )
    notifications.success({ title: 'Map preview regeneration queued' })
    await pollMapPreviewJob()
  } catch {
    mapPreviewJobError.value = 'Map preview regeneration could not be queued.'
    notifications.error({
      title: 'Map preview regeneration could not be queued',
      description: 'Another preview import may already be active.'
    })
  }
}

async function loadMap() {
  clearTimeout(mapPreviewPollTimer)
  loading.value = true
  sceneReady.value = false
  error.value = undefined
  previewError.value = undefined
  terrainMaterialError.value = undefined
  diagnosticsOpen.value = false
  skyZonePreviewOpen.value = false
  selectedSkyZoneName.value = undefined
  skyZonePreviewError.value = undefined
  catalogEntry.value = undefined
  manifest.value = undefined
  rawManifest.value = undefined
  mapPreview.value = undefined
  mapPreviewJob.value = undefined
  mapPreviewJobError.value = undefined
  selectedActorName.value = undefined
  selectedPlayerStartName.value = undefined
  selectedBspName.value = undefined
  selectedLightName.value = undefined
  selectedWaterName.value = undefined
  selectedWaterSurfaceName.value = undefined
  inspectorTab.value = 'actors'
  actorsVisible.value = true
  playerStartsVisible.value = true
  bspVisible.value = true
  worldBaseVisible.value = false
  lightHelpersVisible.value = false
  waterVolumesVisible.value = false
  waterSurfacesVisible.value = true
  terrainLayerStates.value = {}
  query.value = ''
  spawnQuery.value = ''
  lightQuery.value = ''
  waterQuery.value = ''

  try {
    const entry = await getAssetCatalogEntry<MapCatalogEntry>(
      'maps',
      routeName.value,
      routeSourceKey.value
    )
    catalogEntry.value = entry
    if (!entry.manifestUrl) {
      error.value = entry.error ?? 'Map “' + entry.name + '” was not imported.'
      return
    }
    const publishedManifest = await getPublishedManifestWithRaw<MapManifest>(
      entry.manifestUrl
    )
    rawManifest.value = publishedManifest.raw
    manifest.value = publishedManifest.resolved
    terrainLayerStates.value = createTerrainLayerStates(manifest.value.terrains)
    await loadMapPreview(entry)
  } catch {
    error.value = 'Map “' + routeName.value + '” could not be loaded.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Map"
      :title="catalogEntry?.name ?? routeName"
      description="Inspect render-only BSP, terrain layers, placed static-mesh instances, and imported lights."
      icon="i-lucide-map-pinned"
    >
      <template #actions>
        <UButton
          label="Diagnostics"
          icon="i-lucide-message-square-warning"
          color="neutral"
          variant="outline"
          :disabled="!catalogEntry"
          @click="diagnosticsOpen = true"
        />
        <UButton
          label="All maps"
          icon="i-lucide-arrow-left"
          color="neutral"
          variant="outline"
          to="/library/maps"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      title="Map unavailable"
      :description="error"
    >
      <template #actions>
        <UButton color="error" variant="soft" size="sm" @click="loadMap">
          Try again
        </UButton>
      </template>
    </UAlert>

    <div v-if="loading" class="grid min-h-64 place-items-center">
      <div class="flex items-center gap-3 text-sm text-muted">
        <UIcon name="i-lucide-loader-circle" class="size-5 animate-spin" />
        Loading map…
      </div>
    </div>

    <template v-else-if="manifest">
      <div class="grid gap-3 sm:grid-cols-2 xl:grid-cols-6">
        <UCard>
          <p class="text-xs text-muted">BSP</p>
          <p class="text-2xl font-semibold">
            {{ worldBspMeshes.length.toLocaleString() }}
          </p>
          <p class="text-xs text-muted">
            {{ bspTotals.surfaces.toLocaleString() }} surfaces ·
            {{ bspTotals.triangles.toLocaleString() }} triangles
          </p>
        </UCard>
        <UCard>
          <p class="text-xs text-muted">Terrains</p>
          <p class="text-2xl font-semibold">{{ manifest.terrains.length }}</p>
          <p class="text-xs text-muted">
            {{ terrainLayerCount }} layers ·
            {{ terrainMaterialsResolved ? 'material resolved' : 'fallback' }}
          </p>
        </UCard>
        <UCard>
          <p class="text-xs text-muted">Placed meshes</p>
          <p class="text-2xl font-semibold">
            {{ manifest.actors.length.toLocaleString() }}
          </p>
        </UCard>
        <UCard>
          <p class="text-xs text-muted">Lights</p>
          <p class="text-2xl font-semibold">{{ manifest.lights.length }}</p>
        </UCard>
        <UCard>
          <p class="text-xs text-muted">Water</p>
          <p class="text-2xl font-semibold">
            {{ manifest.waterVolumes.length }}
          </p>
          <p class="text-xs text-muted">diagnostic volumes</p>
        </UCard>
        <UCard>
          <p class="text-xs text-muted">Unresolved</p>
          <p class="text-2xl font-semibold">
            {{ unresolvedActors.toLocaleString() }}
          </p>
        </UCard>
      </div>

      <UAlert
        v-if="previewError"
        color="error"
        variant="subtle"
        title="Preview unavailable"
        :description="previewError"
      />

      <UAlert
        v-if="terrainMaterialError"
        color="warning"
        variant="subtle"
        title="Material fallback"
        :description="terrainMaterialError"
      />

      <div
        class="grid items-start gap-4 xl:grid-cols-[minmax(0,2fr)_minmax(24rem,1fr)]"
      >
        <UCard :ui="{ body: 'p-2 sm:p-2' }">
          <StudioMapPreview
            ref="preview"
            :manifest="manifest"
            :selected-actor-name="selectedActorName"
            :selected-bsp-name="selectedBspName"
            :actors-visible="actorsVisible"
            :player-starts-visible="playerStartsVisible"
            :selected-player-start-name="selectedPlayerStartName"
            :bsp-visible="bspVisible"
            :world-base-visible="worldBaseVisible"
            :terrain-layer-visibility="terrainLayerVisibility"
            :light-helpers-visible="lightHelpersVisible"
            :selected-light-name="selectedLightName"
            :water-surfaces-visible="waterSurfacesVisible"
            :selected-water-surface-name="selectedWaterSurfaceName"
            :water-volumes-visible="waterVolumesVisible"
            :selected-water-name="selectedWaterName"
            @error="previewError = $event"
            @material-error="terrainMaterialError = $event"
            @light-select="selectedLightName = $event"
            @ready-change="sceneReady = $event"
          />
          <p class="mt-2 text-center text-xs text-muted">
            Drag to orbit · scroll to zoom toward the pointer · right-drag to
            pan · double-click the preview to restore the framed view
          </p>
        </UCard>

        <UCard
          v-if="sceneReady"
          class="xl:sticky xl:top-4"
          :ui="{ body: 'p-0 sm:p-0' }"
        >
          <template #header>
            <div
              class="grid grid-cols-3 gap-1 sm:grid-cols-4 xl:grid-cols-3 2xl:grid-cols-4"
              role="tablist"
              aria-label="Map inspector"
            >
              <UButton
                label="BSP"
                icon="i-lucide-blocks"
                color="neutral"
                :variant="inspectorTab === 'bsp' ? 'soft' : 'ghost'"
                role="tab"
                :aria-selected="inspectorTab === 'bsp'"
                class="justify-center"
                @click="inspectorTab = 'bsp'"
              />
              <UButton
                label="Meshes"
                icon="i-lucide-box"
                color="neutral"
                :variant="inspectorTab === 'actors' ? 'soft' : 'ghost'"
                role="tab"
                :aria-selected="inspectorTab === 'actors'"
                class="justify-center"
                @click="inspectorTab = 'actors'"
              />
              <UButton
                label="Spawns"
                icon="i-lucide-map-pin"
                color="neutral"
                :variant="inspectorTab === 'spawns' ? 'soft' : 'ghost'"
                role="tab"
                :aria-selected="inspectorTab === 'spawns'"
                class="justify-center"
                @click="inspectorTab = 'spawns'"
              />
              <UButton
                label="Terrain"
                icon="i-lucide-layers-3"
                color="neutral"
                :variant="inspectorTab === 'terrain' ? 'soft' : 'ghost'"
                role="tab"
                :aria-selected="inspectorTab === 'terrain'"
                class="justify-center"
                @click="inspectorTab = 'terrain'"
              />
              <UButton
                label="Lights"
                icon="i-lucide-lightbulb"
                color="neutral"
                :variant="inspectorTab === 'lights' ? 'soft' : 'ghost'"
                role="tab"
                :aria-selected="inspectorTab === 'lights'"
                class="justify-center"
                @click="inspectorTab = 'lights'"
              />
              <UButton
                label="Water"
                icon="i-lucide-waves"
                color="neutral"
                :variant="inspectorTab === 'water' ? 'soft' : 'ghost'"
                role="tab"
                :aria-selected="inspectorTab === 'water'"
                class="justify-center"
                @click="inspectorTab = 'water'"
              />
              <UButton
                label="Summary"
                icon="i-lucide-notebook-tabs"
                color="neutral"
                :variant="inspectorTab === 'summary' ? 'soft' : 'ghost'"
                role="tab"
                :aria-selected="inspectorTab === 'summary'"
                class="justify-center"
                @click="inspectorTab = 'summary'"
              />
              <UButton
                label="Environment"
                icon="i-lucide-cloud-fog"
                color="neutral"
                :variant="inspectorTab === 'environment' ? 'soft' : 'ghost'"
                role="tab"
                :aria-selected="inspectorTab === 'environment'"
                class="justify-center"
                @click="inspectorTab = 'environment'"
              />
            </div>
          </template>

          <template v-if="inspectorTab === 'bsp'">
            <div class="space-y-3 border-b border-default p-4">
              <div class="flex items-start justify-between gap-3">
                <div>
                  <h2 class="text-sm font-semibold text-highlighted">
                    World BSP chunks
                  </h2>
                  <p class="text-xs text-muted">
                    {{ bspTotals.vertices.toLocaleString() }} vertices ·
                    {{ bspTotals.triangles.toLocaleString() }} triangles
                  </p>
                </div>
                <USwitch
                  v-model="bspVisible"
                  label="Show"
                  aria-label="Show world BSP"
                />
              </div>
              <UAlert
                v-if="bspTotals.errors"
                color="error"
                variant="subtle"
                title="BSP load errors"
                :description="`${bspTotals.errors} chunks could not be published or loaded.`"
              />
              <UAlert
                v-else-if="bspTotals.fallbacks"
                color="warning"
                variant="subtle"
                title="Neutral material fallback"
                :description="`${bspTotals.fallbacks} chunks contain unresolved BSP materials.`"
              />
            </div>
            <div class="max-h-[52vh] divide-y divide-default overflow-y-auto">
              <div
                v-for="bsp in worldBspMeshes"
                :key="bsp.name"
                class="flex items-center gap-2 p-2"
                :class="selectedBspName === bsp.name ? 'bg-primary/10' : ''"
              >
                <button
                  type="button"
                  class="min-w-0 flex-1 rounded-md p-2 text-left hover:bg-elevated focus-visible:outline-2 focus-visible:outline-primary"
                  :aria-pressed="selectedBspName === bsp.name"
                  @click="selectBsp(bsp)"
                  @dblclick="focusBsp(bsp)"
                >
                  <span class="flex items-center justify-between gap-3">
                    <span class="font-medium text-highlighted">{{
                      bsp.name
                    }}</span>
                    <span class="flex shrink-0 items-center gap-2">
                      <UBadge
                        :color="
                          bsp.error && !bsp.meshUrl
                            ? 'error'
                            : bsp.materialStatus === 'resolved' ||
                                bsp.materialStatus === 'none'
                              ? 'success'
                              : 'warning'
                        "
                        variant="subtle"
                      >
                        {{
                          bsp.error && !bsp.meshUrl
                            ? 'error'
                            : bsp.materialStatus
                        }}
                      </UBadge>
                    </span>
                  </span>
                  <span class="mt-1 block text-xs text-muted">
                    {{ bsp.surfaceCount }} surfaces ·
                    {{ bsp.triangleCount }} triangles ·
                    {{ bsp.resolvedMaterialCount }}/{{ bsp.materialCount }}
                    materials
                  </span>
                  <span
                    v-if="bsp.error"
                    class="mt-1 block text-xs"
                    :class="bsp.meshUrl ? 'text-warning' : 'text-error'"
                  >
                    {{ bsp.error }}
                  </span>
                  <span
                    v-if="
                      bsp.invisibleSurfaceCount ||
                      bsp.portalSurfaceCount ||
                      bsp.fakeBackdropSurfaceCount ||
                      bsp.malformedSurfaceCount ||
                      bsp.unresolvedMaterialReferenceCount
                    "
                    class="mt-1 block text-xs text-muted"
                  >
                    Skipped: {{ bsp.invisibleSurfaceCount }} invisible,
                    {{ bsp.portalSurfaceCount }} portal,
                    {{ bsp.fakeBackdropSurfaceCount }} backdrop,
                    {{ bsp.malformedSurfaceCount }} malformed,
                    {{ bsp.unresolvedMaterialReferenceCount }} neutral-material
                  </span>
                </button>
                <UButton
                  icon="i-lucide-focus"
                  color="neutral"
                  variant="ghost"
                  size="sm"
                  :disabled="!bsp.meshUrl || !bspVisible"
                  :aria-label="'Focus ' + bsp.name"
                  @click="focusBsp(bsp)"
                />
              </div>
            </div>
          </template>

          <template v-else-if="inspectorTab === 'actors'">
            <div class="space-y-3 border-b border-default p-4">
              <div class="flex items-start justify-between gap-3">
                <div>
                  <h2 class="text-sm font-semibold text-highlighted">
                    Placed mesh instances
                  </h2>
                  <p class="text-xs text-muted">
                    {{ filteredActors.length.toLocaleString() }} of
                    {{ manifest.actors.length.toLocaleString() }} instances
                  </p>
                </div>
                <USwitch
                  v-model="actorsVisible"
                  label="Show"
                  aria-label="Show placed meshes"
                />
              </div>
              <UInput
                v-model="query"
                icon="i-lucide-search"
                placeholder="Search actors or meshes"
                aria-label="Search placed mesh instances"
                class="w-full"
              />
            </div>
            <div class="max-h-[54vh] divide-y divide-default overflow-y-auto">
              <div
                v-for="actor in visibleActors"
                :key="actor.name"
                class="flex items-center gap-2 p-2"
                :class="selectedActorName === actor.name ? 'bg-primary/10' : ''"
              >
                <button
                  type="button"
                  class="min-w-0 flex-1 rounded-md p-2 text-left hover:bg-elevated focus-visible:outline-2 focus-visible:outline-primary"
                  @click="selectActor(actor)"
                  @dblclick="focusActor(actor)"
                >
                  <span class="flex items-center gap-2">
                    <span
                      class="truncate text-sm font-medium text-highlighted"
                      >{{ actor.name }}</span
                    >
                    <UBadge
                      :color="actor.meshUrl ? 'success' : 'warning'"
                      variant="subtle"
                      size="sm"
                    >
                      {{ actor.meshUrl ? 'resolved' : 'unresolved' }}
                    </UBadge>
                  </span>
                  <span class="mt-1 block truncate text-xs text-muted">
                    {{ actor.meshPackage ?? 'Unknown package' }}.{{
                      actor.meshObject ?? 'Unknown mesh'
                    }}
                  </span>
                  <span class="mt-1 block truncate text-xs text-dimmed">
                    {{ actor.className }} · X
                    {{ actor.location.x.toFixed(0) }} · Y
                    {{ actor.location.y.toFixed(0) }} · Z
                    {{ actor.location.z.toFixed(0) }}
                  </span>
                </button>
                <UButton
                  icon="i-lucide-focus"
                  color="neutral"
                  variant="ghost"
                  size="sm"
                  :disabled="!actor.meshUrl || !actorsVisible"
                  :aria-label="'Focus ' + actor.name"
                  @click="focusActor(actor)"
                />
              </div>
              <div
                v-if="visibleActors.length === 0"
                class="grid min-h-48 place-items-center p-8 text-center text-sm text-muted"
              >
                No placed meshes match this search.
              </div>
            </div>
            <StudioTableFooter
              v-model:page="page"
              v-model:page-size="pageSize"
              :total="filteredActors.length"
              :page-size-options="[50, 100, 200]"
            />
          </template>

          <template v-else-if="inspectorTab === 'spawns'">
            <div class="space-y-3 border-b border-default p-4">
              <div class="flex items-start justify-between gap-3">
                <div>
                  <h2 class="text-sm font-semibold text-highlighted">
                    Spawn locations
                  </h2>
                  <p class="text-xs text-muted">
                    {{ filteredPlayerStarts.length.toLocaleString() }} of
                    {{ playerStarts.length.toLocaleString() }} PlayerStart markers
                  </p>
                </div>
                <USwitch
                  v-model="playerStartsVisible"
                  label="Show"
                  aria-label="Show PlayerStart markers"
                />
              </div>
              <p class="text-xs text-muted">
                UE map player-start markers. Authoritative L2 character spawns are managed by the Game Server.
              </p>
              <UInput
                v-model="spawnQuery"
                icon="i-lucide-search"
                placeholder="Search PlayerStart markers"
                aria-label="Search PlayerStart markers"
                class="w-full"
              />
            </div>
            <div class="max-h-[54vh] divide-y divide-default overflow-y-auto">
              <div
                v-for="playerStart in visiblePlayerStarts"
                :key="playerStart.name"
                class="flex items-center gap-2 p-2"
                :class="selectedPlayerStartName === playerStart.name ? 'bg-primary/10' : ''"
              >
                <button
                  type="button"
                  class="min-w-0 flex-1 rounded-md p-2 text-left hover:bg-elevated focus-visible:outline-2 focus-visible:outline-primary"
                  :aria-pressed="selectedPlayerStartName === playerStart.name"
                  @click="selectPlayerStart(playerStart)"
                  @dblclick="focusPlayerStart(playerStart)"
                >
                  <span class="flex items-center gap-2">
                    <span class="truncate text-sm font-medium text-highlighted">{{
                      playerStart.name
                    }}</span>
                    <UBadge color="success" variant="subtle" size="sm">
                      PlayerStart
                    </UBadge>
                  </span>
                  <span class="mt-1 block truncate text-xs text-dimmed">
                    X {{ playerStart.location.x.toFixed(0) }} · Y
                    {{ playerStart.location.y.toFixed(0) }} · Z
                    {{ playerStart.location.z.toFixed(0) }}
                  </span>
                </button>
                <UButton
                  icon="i-lucide-focus"
                  color="neutral"
                  variant="ghost"
                  size="sm"
                  :disabled="!playerStartsVisible"
                  :aria-label="'Focus ' + playerStart.name"
                  @click="focusPlayerStart(playerStart)"
                />
              </div>
              <div
                v-if="visiblePlayerStarts.length === 0"
                class="grid min-h-48 place-items-center p-8 text-center text-sm text-muted"
              >
                No PlayerStart markers match this search.
              </div>
            </div>
            <StudioTableFooter
              v-model:page="spawnPage"
              v-model:page-size="spawnPageSize"
              :total="filteredPlayerStarts.length"
              :page-size-options="[50, 100, 200]"
            />
          </template>

          <template v-else-if="inspectorTab === 'terrain'">
            <div class="border-b border-default p-4">
              <h2 class="text-sm font-semibold text-highlighted">
                Terrain layers
              </h2>
              <p class="text-xs text-muted">
                Toggle imported blend layers or isolate one with Solo.
              </p>
            </div>
            <div class="max-h-[62vh] overflow-y-auto">
              <section
                v-for="terrain in manifest.terrains"
                :key="terrain.name"
                class="border-b border-default last:border-b-0"
              >
                <div
                  class="flex items-center justify-between gap-3 bg-elevated/40 px-4 py-3"
                >
                  <div class="min-w-0">
                    <h3 class="truncate text-sm font-medium text-highlighted">
                      {{ terrain.name }}
                    </h3>
                    <p class="text-xs text-muted">
                      {{ terrain.layers.length }} layers
                    </p>
                  </div>
                  <UButton
                    label="Enable all"
                    color="neutral"
                    variant="ghost"
                    size="xs"
                    @click="enableAllLayers(terrain.name)"
                  />
                </div>
                <div class="divide-y divide-default">
                  <div
                    v-for="(layer, layerPosition) in terrain.layers"
                    :key="layer.index"
                    class="flex items-center gap-3 p-3"
                  >
                    <div
                      class="grid size-14 shrink-0 place-items-center overflow-hidden rounded-md border border-default bg-muted"
                    >
                      <img
                        v-if="layer.textureUrl"
                        :src="layer.textureUrl"
                        :alt="`${layer.textureObject ?? 'Terrain'} texture`"
                        loading="lazy"
                        class="size-full object-cover [image-rendering:pixelated]"
                      />
                      <UIcon
                        v-else
                        name="i-lucide-image-off"
                        class="size-5 text-dimmed"
                      />
                    </div>
                    <div class="min-w-0 flex-1">
                      <div class="flex items-center gap-2">
                        <span class="text-sm font-medium text-highlighted">
                          Layer {{ layer.index }}
                        </span>
                        <UBadge
                          v-if="
                            terrainLayerStates[terrain.name]?.soloIndex ===
                            layerPosition
                          "
                          color="primary"
                          variant="subtle"
                          size="sm"
                        >
                          solo
                        </UBadge>
                      </div>
                      <p class="truncate text-xs text-muted">
                        {{ layer.texturePackage ?? 'Unknown' }}.{{
                          layer.textureObject ?? 'Unknown texture'
                        }}
                      </p>
                      <p class="truncate text-xs text-dimmed">
                        Mask {{ layer.alphaObject ?? 'unavailable' }}
                      </p>
                    </div>
                    <div class="flex shrink-0 flex-col items-end gap-1">
                      <USwitch
                        :model-value="layerEnabled(terrain.name, layerPosition)"
                        :aria-label="`Enable ${terrain.name} layer ${layer.index}`"
                        @update:model-value="
                          setLayerEnabled(terrain.name, layerPosition, $event)
                        "
                      />
                      <UButton
                        label="Solo"
                        color="neutral"
                        :variant="
                          terrainLayerStates[terrain.name]?.soloIndex ===
                          layerPosition
                            ? 'soft'
                            : 'ghost'
                        "
                        size="xs"
                        @click="soloLayer(terrain.name, layerPosition)"
                      />
                    </div>
                  </div>
                </div>
              </section>
              <div
                v-if="manifest.terrains.length === 0"
                class="grid min-h-48 place-items-center p-8 text-sm text-muted"
              >
                This map has no imported terrain.
              </div>
            </div>
          </template>

          <template v-else-if="inspectorTab === 'lights'">
            <div class="space-y-3 border-b border-default p-4">
              <div class="flex items-start justify-between gap-3">
                <div>
                  <h2 class="text-sm font-semibold text-highlighted">
                    Imported lights
                  </h2>
                  <p class="text-xs text-muted">
                    {{ filteredLights.length }} of {{ manifest.lights.length }}
                    lights
                  </p>
                </div>
                <USwitch
                  v-model="lightHelpersVisible"
                  label="Helpers"
                  aria-label="Show light helpers"
                />
              </div>
              <UInput
                v-model="lightQuery"
                icon="i-lucide-search"
                placeholder="Search lights"
                aria-label="Search imported lights"
                class="w-full"
              />
            </div>
            <div class="max-h-[62vh] divide-y divide-default overflow-y-auto">
              <div
                v-for="light in filteredLights"
                :key="light.name"
                class="flex items-center gap-2 p-2"
                :class="selectedLightName === light.name ? 'bg-primary/10' : ''"
              >
                <button
                  type="button"
                  class="flex min-w-0 flex-1 items-start gap-3 rounded-md p-2 text-left hover:bg-elevated focus-visible:outline-2 focus-visible:outline-primary"
                  @click="selectLight(light)"
                  @dblclick="focusLight(light)"
                >
                  <span
                    class="mt-1 size-3 shrink-0 rounded-full ring-2 ring-default"
                    :style="{ backgroundColor: mapLightColor(light) }"
                  />
                  <span class="min-w-0 flex-1">
                    <span class="flex items-center gap-2">
                      <span
                        class="truncate text-sm font-medium text-highlighted"
                        >{{ light.name }}</span
                      >
                      <UBadge color="neutral" variant="subtle" size="sm">
                        {{ light.className.includes('Sun') ? 'sun' : 'point' }}
                      </UBadge>
                    </span>
                    <span class="mt-1 block text-xs text-muted">
                      Brightness {{ light.brightness }} · radius
                      {{ light.radius }}
                    </span>
                    <span class="mt-1 block truncate text-xs text-dimmed">
                      X {{ light.location.x.toFixed(0) }} · Y
                      {{ light.location.y.toFixed(0) }} · Z
                      {{ light.location.z.toFixed(0) }}
                    </span>
                  </span>
                </button>
                <UButton
                  icon="i-lucide-focus"
                  color="neutral"
                  variant="ghost"
                  size="sm"
                  :aria-label="'Focus ' + light.name"
                  @click="focusLight(light)"
                />
              </div>
              <div
                v-if="filteredLights.length === 0"
                class="grid min-h-48 place-items-center p-8 text-center text-sm text-muted"
              >
                No imported lights match this search.
              </div>
            </div>
          </template>

          <template v-else-if="inspectorTab === 'water'">
            <div class="space-y-3 border-b border-default p-4">
              <div>
                <h2 class="text-sm font-semibold text-highlighted">Water</h2>
                <p class="text-xs text-muted">
                  {{ waterSurfaceMeshes.length }} rendered surfaces ·
                  {{ manifest.waterVolumes.length }} gameplay volumes
                </p>
              </div>
              <div class="grid grid-cols-2 gap-3">
                <USwitch
                  v-model="waterSurfacesVisible"
                  label="Show surfaces"
                  aria-label="Show water surfaces"
                />
                <USwitch
                  v-model="waterVolumesVisible"
                  label="Show volumes"
                  aria-label="Show water volumes"
                />
              </div>
              <UInput
                v-model="waterQuery"
                icon="i-lucide-search"
                placeholder="Search surfaces, volumes, or brushes"
                aria-label="Search water"
                class="w-full"
              />
            </div>
            <div class="max-h-[62vh] overflow-y-auto">
              <section
                v-if="waterSurfaceMeshes.length"
                aria-label="Water surfaces"
              >
                <div class="border-b border-default bg-elevated/50 px-4 py-2">
                  <p
                    class="text-xs font-semibold tracking-wide text-muted uppercase"
                  >
                    Water surfaces · {{ filteredWaterSurfaces.length }} of
                    {{ waterSurfaceMeshes.length }}
                  </p>
                </div>
                <div class="divide-y divide-default">
                  <div
                    v-for="surface in filteredWaterSurfaces"
                    :key="surface.name"
                    class="flex items-center gap-2 p-2"
                    :class="
                      selectedWaterSurfaceName === surface.name
                        ? 'bg-primary/10'
                        : ''
                    "
                  >
                    <button
                      type="button"
                      class="min-w-0 flex-1 rounded-md p-2 text-left hover:bg-elevated focus-visible:outline-2 focus-visible:outline-primary"
                      @click="selectWaterSurface(surface)"
                      @dblclick="focusWaterSurface(surface)"
                    >
                      <span class="flex items-center justify-between gap-2">
                        <span
                          class="truncate text-sm font-medium text-highlighted"
                        >
                          {{ surface.name }}
                        </span>
                        <UBadge
                          :color="
                            surface.materialStatus === 'resolved'
                              ? 'success'
                              : 'warning'
                          "
                          variant="subtle"
                          size="sm"
                        >
                          {{ surface.materialStatus }}
                        </UBadge>
                      </span>
                      <span class="mt-1 block text-xs text-muted">
                        {{ surface.surfaceCount }} surfaces ·
                        {{ surface.triangleCount }} triangles
                      </span>
                      <span class="mt-1 block text-xs text-dimmed">
                        Volumes:
                        {{
                          surface.waterVolumeNames.length
                            ? surface.waterVolumeNames.join(', ')
                            : 'none in this coordinate map'
                        }}
                      </span>
                    </button>
                    <UButton
                      icon="i-lucide-focus"
                      color="neutral"
                      variant="ghost"
                      size="sm"
                      :disabled="!surface.meshUrl || !waterSurfacesVisible"
                      :aria-label="'Focus ' + surface.name"
                      @click="focusWaterSurface(surface)"
                    />
                  </div>
                </div>
              </section>
              <section
                v-if="manifest.waterVolumes.length"
                aria-label="Water volumes"
              >
                <div class="border-y border-default bg-elevated/50 px-4 py-2">
                  <p
                    class="text-xs font-semibold tracking-wide text-muted uppercase"
                  >
                    Water volumes · {{ filteredWaterVolumes.length }} of
                    {{ manifest.waterVolumes.length }}
                  </p>
                </div>
                <div class="divide-y divide-default">
                  <div
                    v-for="volume in filteredWaterVolumes"
                    :key="volume.name"
                    class="flex items-center gap-2 p-2"
                    :class="
                      selectedWaterName === volume.name ? 'bg-primary/10' : ''
                    "
                  >
                    <button
                      type="button"
                      class="min-w-0 flex-1 rounded-md p-2 text-left hover:bg-elevated focus-visible:outline-2 focus-visible:outline-primary"
                      @click="selectWater(volume)"
                      @dblclick="focusWater(volume)"
                    >
                      <span class="flex items-center gap-2">
                        <span
                          class="truncate text-sm font-medium text-highlighted"
                        >
                          {{ volume.name }}
                        </span>
                        <UBadge
                          :color="
                            volume.status === 'resolved' ? 'success' : 'warning'
                          "
                          variant="subtle"
                          size="sm"
                        >
                          {{ volume.status }}
                        </UBadge>
                      </span>
                      <span class="mt-1 block truncate text-xs text-muted">
                        Brush {{ volume.brushName ?? 'unavailable' }} ·
                        {{ volume.triangleCount }} triangles
                      </span>
                      <span class="mt-1 block truncate text-xs text-dimmed">
                        X {{ volume.location.x.toFixed(0) }} · Y
                        {{ volume.location.y.toFixed(0) }} · Z
                        {{ volume.location.z.toFixed(0) }}
                      </span>
                    </button>
                    <UButton
                      icon="i-lucide-focus"
                      color="neutral"
                      variant="ghost"
                      size="sm"
                      :disabled="
                        volume.status !== 'resolved' || !waterVolumesVisible
                      "
                      :aria-label="'Focus ' + volume.name"
                      @click="focusWater(volume)"
                    />
                  </div>
                </div>
              </section>
              <div
                v-if="
                  filteredWaterSurfaces.length === 0 &&
                  filteredWaterVolumes.length === 0
                "
                class="grid min-h-48 place-items-center p-8 text-center text-sm text-muted"
              >
                This map has no water matching this search.
              </div>
            </div>
          </template>

          <template v-else-if="inspectorTab === 'summary'">
            <div class="border-b border-default p-4">
              <h2 class="text-sm font-semibold text-highlighted">
                Level summary
              </h2>
              <p class="text-xs text-muted">
                Generated preview, published manifest, and authored UE2 metadata
              </p>
            </div>
            <div class="max-h-[58vh] space-y-4 overflow-y-auto p-4">
              <section aria-label="Generated map preview">
                <div class="flex items-start justify-between gap-3">
                  <div>
                    <h3 class="text-sm font-semibold text-highlighted">
                      Map preview
                    </h3>
                    <p class="text-xs text-muted">
                      Top-down image generated from the published map manifest
                    </p>
                  </div>
                  <UButton
                    :label="mapPreview?.imageUrl ? 'Regenerate preview' : 'Generate preview'"
                    icon="i-lucide-image"
                    size="xs"
                    color="neutral"
                    variant="outline"
                    :loading="mapPreviewJobActive"
                    :disabled="mapPreviewJobActive"
                    @click="regenerateMapPreview"
                  />
                </div>
                <UAlert
                  v-if="mapPreviewJobError"
                  class="mt-3"
                  color="error"
                  variant="subtle"
                  title="Preview generation failed"
                  :description="mapPreviewJobError"
                />
                <UAlert
                  v-else-if="mapPreview?.error"
                  class="mt-3"
                  color="warning"
                  variant="subtle"
                  title="Preview unavailable"
                  :description="mapPreview.error"
                />
                <div
                  v-if="mapPreview?.imageUrl"
                  class="mt-3 overflow-hidden rounded-md border border-default bg-muted/30"
                >
                  <img
                    :src="mapPreview.imageUrl"
                    :alt="`${catalogEntry?.name ?? routeName} map preview`"
                    class="aspect-square w-full object-cover"
                  >
                  <p class="border-t border-default px-3 py-2 text-xs text-muted">
                    {{ mapPreview.width }} × {{ mapPreview.height }} ·
                    {{ mapPreview.status }}
                  </p>
                </div>
                <div
                  v-else-if="mapPreviewJobActive"
                  class="mt-3 flex min-h-32 items-center justify-center gap-2 rounded-md border border-dashed border-default text-xs text-muted"
                >
                  <UIcon name="i-lucide-loader-circle" class="size-4 animate-spin" />
                  Generating preview…
                </div>
                <div
                  v-else
                  class="mt-3 grid min-h-32 place-items-center rounded-md border border-dashed border-default px-4 text-center text-xs text-muted"
                >
                  No generated map preview is available.
                </div>
              </section>

              <section class="border-t border-default pt-4" aria-label="Level Summary">
                <div v-if="levelSummary">
                  <div v-if="levelSummary.title">
                    <p class="text-xs text-muted">Title</p>
                    <p class="text-base font-semibold text-highlighted">
                      {{ levelSummary.title }}
                    </p>
                  </div>

                  <dl class="mt-4 space-y-3 text-sm">
                    <div v-if="levelSummary.author">
                      <dt class="text-muted">Author</dt>
                      <dd class="mt-1 text-highlighted">
                        {{ levelSummary.author }}
                      </dd>
                    </div>
                    <div v-if="idealPlayerCount">
                      <dt class="text-muted">Ideal player count</dt>
                      <dd class="mt-1 text-highlighted">
                        {{ idealPlayerCount }}
                      </dd>
                    </div>
                    <div v-if="levelSummary.singlePlayerTeamSize !== null">
                      <dt class="text-muted">Single-player team size</dt>
                      <dd class="mt-1 text-highlighted">
                        {{ levelSummary.singlePlayerTeamSize }}
                      </dd>
                    </div>
                    <div v-if="levelSummary.hideFromMenus !== null">
                      <dt class="text-muted">Menu visibility</dt>
                      <dd class="mt-1">
                        <UBadge
                          :color="levelSummary.hideFromMenus ? 'warning' : 'success'"
                          variant="subtle"
                          size="sm"
                        >
                          {{ levelSummary.hideFromMenus ? 'Hidden' : 'Visible' }}
                        </UBadge>
                      </dd>
                    </div>
                    <div v-if="levelSummary.description">
                      <dt class="text-muted">Description</dt>
                      <dd class="mt-1 whitespace-pre-wrap text-highlighted">
                        {{ levelSummary.description }}
                      </dd>
                    </div>
                    <div v-if="levelSummary.levelEnterText">
                      <dt class="text-muted">Level entry text</dt>
                      <dd class="mt-1 whitespace-pre-wrap text-highlighted">
                        {{ levelSummary.levelEnterText }}
                      </dd>
                    </div>
                    <div v-if="levelSummary.extraInfo">
                      <dt class="text-muted">Extra info</dt>
                      <dd class="mt-1 whitespace-pre-wrap text-highlighted">
                        {{ levelSummary.extraInfo }}
                      </dd>
                    </div>
                    <div v-if="levelSummary.decoTextName">
                      <dt class="text-muted">Deco text name</dt>
                      <dd class="mt-1 text-highlighted">
                        {{ levelSummary.decoTextName }}
                      </dd>
                    </div>
                    <div v-if="levelSummary.screenshot">
                      <dt class="text-muted">Screenshot material</dt>
                      <dd class="mt-1 break-all font-mono text-xs text-highlighted">
                        {{ levelSummary.screenshot }}
                      </dd>
                    </div>
                  </dl>

                  <p
                    v-if="!levelSummaryHasData"
                    class="mt-4 text-sm text-muted"
                  >
                    This LevelSummary contains no authored metadata.
                  </p>
                </div>
                <p v-else class="text-sm text-muted">
                  This map has no readable LevelSummary metadata.
                </p>
              </section>

              <section
                v-if="rawManifest"
                class="border-t border-default pt-4"
                aria-label="Published map manifest"
              >
                <h3 class="text-sm font-semibold text-highlighted">
                  Published manifest
                </h3>
                <p class="mt-1 text-xs text-muted">
                  Raw JSON stored with this generated map artifact
                </p>
                <div class="mt-3 overflow-x-auto rounded-md bg-muted/40 p-3">
                  <StudioJsonTree :value="rawManifest" />
                </div>
              </section>
            </div>
          </template>

          <template v-else-if="inspectorTab === 'environment'">
            <div class="border-b border-default p-4">
              <div>
                <h2 class="text-sm font-semibold text-highlighted">
                  Map environment
                </h2>
                <p class="text-xs text-muted">
                  Authored atmosphere and diagnostic world geometry
                </p>
              </div>
            </div>
            <div class="max-h-[58vh] overflow-y-auto">
              <section
                class="border-b border-default p-4"
                aria-label="Atmosphere"
              >
                <div class="flex items-start justify-between gap-3">
                  <div>
                    <h3 class="text-sm font-semibold text-highlighted">
                      Atmosphere
                    </h3>
                    <p class="text-xs text-muted">
                      Generated atlas previews suppress distance fog for map
                      readability.
                    </p>
                  </div>
                </div>

                <dl class="mt-4 space-y-3 text-sm">
                  <div class="flex items-center justify-between gap-4">
                    <dt class="text-muted">Ambient color</dt>
                    <dd class="flex items-center gap-2 text-highlighted">
                      <span
                        class="size-4 rounded-full border border-default"
                        :style="{
                          backgroundColor: mapEnvironmentColor(
                            manifest.environment.ambientColor
                          ).css
                        }"
                      />
                      {{
                        mapEnvironmentColor(manifest.environment.ambientColor)
                          .label
                      }}
                    </dd>
                  </div>
                  <div class="flex items-center justify-between gap-4">
                    <dt class="text-muted">Ambient brightness</dt>
                    <dd class="text-highlighted">
                      {{
                        Math.round(
                          manifest.environment.ambientBrightness * 100
                        )
                      }}%
                    </dd>
                  </div>
                  <div class="flex items-center justify-between gap-4">
                    <dt class="text-muted">Distance fog</dt>
                    <dd>
                      <UBadge
                        :color="
                          manifest.environment.distanceFog
                            ? 'success'
                            : 'neutral'
                        "
                        variant="subtle"
                        size="sm"
                      >
                        {{
                          manifest.environment.distanceFog
                            ? 'Authored'
                            : 'Not authored'
                        }}
                      </UBadge>
                    </dd>
                  </div>
                  <template v-if="manifest.environment.distanceFog">
                    <div class="flex items-center justify-between gap-4">
                      <dt class="text-muted">Fog color</dt>
                      <dd class="flex items-center gap-2 text-highlighted">
                        <span
                          class="size-4 rounded-full border border-default"
                          :style="{
                            backgroundColor: mapEnvironmentColor(
                              manifest.environment.distanceFog.color
                            ).css
                          }"
                        />
                        {{
                          mapEnvironmentColor(
                            manifest.environment.distanceFog.color
                          ).label
                        }}
                      </dd>
                    </div>
                    <div class="flex items-center justify-between gap-4">
                      <dt class="text-muted">Fog start</dt>
                      <dd class="text-highlighted">
                        {{
                          manifest.environment.distanceFog.start.toLocaleString()
                        }}
                        units
                      </dd>
                    </div>
                    <div class="flex items-center justify-between gap-4">
                      <dt class="text-muted">Fog end</dt>
                      <dd class="text-highlighted">
                        {{
                          manifest.environment.distanceFog.end.toLocaleString()
                        }}
                        units
                      </dd>
                    </div>
                  </template>
                </dl>
              </section>
              <section v-if="skyZoneBspMeshes.length" aria-label="Sky Zones">
                <div
                  class="flex items-center justify-between gap-3 border-b border-default bg-elevated/50 px-4 py-3"
                >
                  <div>
                    <p class="text-sm font-semibold text-highlighted">
                      Sky Zones
                    </p>
                    <p class="text-xs text-muted">
                      {{ previewableSkyZones.length }} published zones ·
                      {{ skyZoneBspMeshes.length }} chunks
                    </p>
                  </div>
                  <UButton
                    label="Preview Sky Zone"
                    icon="i-lucide-eye"
                    color="neutral"
                    variant="outline"
                    :disabled="skyZonePreviewOptions.length === 0"
                    @click="openSkyZonePreview"
                  />
                </div>
                <p
                  v-if="skyZonePreviewOptions.length === 0"
                  class="p-4 text-xs text-muted"
                >
                  No Sky Zone BSP chunks are currently published.
                </p>
              </section>
              <section
                v-if="worldBaseBspMeshes.length"
                aria-label="World Bases"
              >
                <div
                  class="flex items-center justify-between gap-3 border-y border-default bg-elevated/50 px-4 py-3"
                >
                  <div>
                    <p class="text-sm font-semibold text-highlighted">
                      World Bases
                    </p>
                    <p class="text-xs text-muted">
                      {{ worldBaseBspMeshes.length }} diagnostic foundations
                    </p>
                  </div>
                  <USwitch
                    v-model="worldBaseVisible"
                    label="Show group"
                    aria-label="Show world bases group"
                  />
                </div>
                <div class="divide-y divide-default">
                  <div
                    v-for="bsp in worldBaseBspMeshes"
                    :key="bsp.name"
                    class="flex items-center gap-2 p-2"
                    :class="selectedBspName === bsp.name ? 'bg-primary/10' : ''"
                  >
                    <button
                      type="button"
                      class="min-w-0 flex-1 rounded-md p-2 text-left hover:bg-elevated"
                      @click="selectBsp(bsp)"
                      @dblclick="focusBsp(bsp)"
                    >
                      <span
                        class="block truncate text-sm font-medium text-highlighted"
                      >
                        {{ bsp.name }}
                      </span>
                      <span class="mt-1 block text-xs text-muted">
                        {{ bsp.surfaceCount }} surfaces ·
                        {{ bsp.triangleCount }} triangles
                      </span>
                    </button>
                    <UButton
                      icon="i-lucide-focus"
                      color="neutral"
                      variant="ghost"
                      size="sm"
                      :disabled="!bsp.meshUrl || !worldBaseVisible"
                      :aria-label="'Focus ' + bsp.name"
                      @click="focusBsp(bsp)"
                    />
                  </div>
                </div>
              </section>
              <div
                v-if="
                  skyZoneBspMeshes.length === 0 &&
                  worldBaseBspMeshes.length === 0
                "
                class="grid min-h-48 place-items-center p-8 text-center text-sm text-muted"
              >
                This map has no special BSP geometry.
              </div>
            </div>
          </template>
        </UCard>
        <UCard
          v-else-if="previewError"
          class="xl:sticky xl:top-4"
          aria-live="polite"
        >
          <div
            class="flex min-h-64 flex-col items-center justify-center px-6 text-center"
          >
            <span
              class="flex size-11 items-center justify-center rounded-full bg-error/10 text-error"
            >
              <UIcon name="i-lucide-circle-alert" class="size-5" />
            </span>
            <h2 class="mt-3 text-sm font-semibold text-highlighted">
              Scene controls unavailable
            </h2>
            <p class="mt-1 max-w-sm text-xs leading-5 text-muted">
              The inspector remains locked because the scene did not finish
              loading.
            </p>
          </div>
        </UCard>
        <UCard v-else class="xl:sticky xl:top-4" aria-live="polite">
          <div
            class="flex min-h-64 flex-col items-center justify-center px-6 text-center"
          >
            <UIcon
              name="i-lucide-loader-circle"
              class="size-6 animate-spin text-primary"
            />
            <h2 class="mt-3 text-sm font-semibold text-highlighted">
              Loading scene controls…
            </h2>
            <p class="mt-1 max-w-sm text-xs leading-5 text-muted">
              Camera and inspector actions unlock after the complete scene is
              ready.
            </p>
          </div>
        </UCard>
      </div>
    </template>

    <StudioMapDiagnosticsSlideover
      v-model:open="diagnosticsOpen"
      :map-name="catalogEntry?.name ?? routeName"
      :source-key="catalogEntry?.sourceKey ?? routeSourceKey"
    />

    <UModal
      v-model:open="skyZonePreviewOpen"
      title="Sky Zone preview"
      :description="selectedSkyZoneName"
      :ui="{ content: 'max-w-6xl' }"
    >
      <template #body>
        <div class="space-y-4">
          <UFormField label="Sky Zone">
            <USelect
              v-model="selectedSkyZoneName"
              :items="skyZonePreviewOptions"
              class="w-full"
            />
          </UFormField>
          <UAlert
            v-if="skyZonePreviewError"
            color="error"
            variant="subtle"
            title="Sky Zone preview unavailable"
            :description="skyZonePreviewError"
          />
          <StudioMapPreview
            v-if="skyZonePreviewManifest"
            :key="selectedSkyZoneName"
            :manifest="skyZonePreviewManifest"
            :actors-visible="false"
            :bsp-visible="false"
            :sky-zone-visible="true"
            :world-base-visible="false"
            :light-helpers-visible="false"
            :water-surfaces-visible="false"
            :water-volumes-visible="false"
            @error="skyZonePreviewError = $event"
          />
        </div>
      </template>
    </UModal>
  </div>
</template>
