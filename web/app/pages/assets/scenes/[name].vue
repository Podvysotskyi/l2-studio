<script setup lang="ts">
import type {
  LevelActorManifestEntry,
  LevelBspMeshManifestEntry,
  LevelLightManifestEntry,
  LevelRotation,
  LevelVector,
  LevelWaterVolumeManifestEntry,
  SceneCatalogEntry,
  SceneManifest,
  SceneObjectManifestEntry
} from '@podvysotskyi/l2-ui'
import { computed, nextTick, onBeforeUnmount, watch } from 'vue'
import { useRoute } from 'vue-router'
import {
  createTerrainLayerStates,
  enableAllTerrainLayers,
  filterLevelLights,
  filterLevelWaterVolumes,
  levelEnvironmentColor,
  levelLightColor,
  setTerrainLayerEnabled,
  toggleSoloTerrainLayer,
  type TerrainLayerStates
} from '../../../utils/level-inspector'
import { filterLevelActors } from '../../../utils/level-map'
import {
  interpolateScenePose,
  sceneManagerLabel,
  scenePlaybackFrames
} from '../../../utils/scene-cinematic'
import {
  filterSceneObjects,
  sceneObjectStatus
} from '../../../utils/scene-inspector'
import { assetCatalogEntryUrl, paginate } from '../../../utils/studio-content'

interface ScenePreviewApi {
  focusActor(name: string): void
  focusBsp(name: string): void
  focusLight(name: string): void
  focusPosition(location: LevelVector, radius?: number): void
  focusWater(name: string): void
  focusWaterSurface(name: string): void
  frameMap(): void
  setCameraPose(location: LevelVector, rotation: LevelRotation): void
}

type InspectorTab =
  | 'bsp'
  | 'actors'
  | 'terrain'
  | 'lights'
  | 'water'
  | 'cinematic'
  | 'sounds'
  | 'effects'
  | 'environment'

const route = useRoute()
const apiBase = ''
const manifest = ref<SceneManifest>()
const preview = ref<ScenePreviewApi>()
const loading = ref(true)
const sceneReady = ref(false)
const error = ref<string>()
const previewError = ref<string>()
const materialError = ref<string>()
const inspectorTab = ref<InspectorTab>('cinematic')
const selectedActorName = ref<string>()
const selectedBspName = ref<string>()
const selectedLightName = ref<string>()
const selectedWaterName = ref<string>()
const selectedWaterSurfaceName = ref<string>()
const actorsVisible = ref(true)
const bspVisible = ref(true)
const skyZoneVisible = ref(false)
const skyZoneChunkVisibility = ref<Record<string, boolean>>({})
const worldBaseVisible = ref(false)
const lightHelpersVisible = ref(false)
const waterVolumesVisible = ref(true)
const waterSurfacesVisible = ref(true)
const distanceFogEnabled = ref(false)
const terrainLayerStates = ref<TerrainLayerStates>({})
const actorQuery = ref('')
const lightQuery = ref('')
const waterQuery = ref('')
const soundQuery = ref('')
const effectQuery = ref('')
const actorPage = ref(1)
const actorPageSize = ref(50)
const frameIndex = ref(0)
const selectedManagerName = ref<string>()
const playing = ref(false)
let animationFrame: number | undefined
let segmentStartedAt = 0

const routeName = computed(() =>
  Array.isArray(route.params.name)
    ? (route.params.name[0] ?? '')
    : (route.params.name ?? '')
)
const frames = computed(() =>
  manifest.value
    ? scenePlaybackFrames(manifest.value, selectedManagerName.value)
    : []
)
const managerOptions = computed(() =>
  (manifest.value?.sceneManagers ?? []).map((manager) => ({
    label: sceneManagerLabel(manager),
    value: manager.name
  }))
)
const cinematicCount = computed(() =>
  manifest.value
    ? manifest.value.cameras.length +
      manifest.value.interpolationPoints.length +
      manifest.value.sceneManagers.length +
      manifest.value.actions.length
    : 0
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
  filterLevelActors(manifest.value?.actors ?? [], actorQuery.value)
)
const visibleActors = computed(() =>
  paginate(filteredActors.value, actorPage.value, actorPageSize.value)
)
const filteredLights = computed(() =>
  filterLevelLights(manifest.value?.lights ?? [], lightQuery.value)
)
const filteredWaterVolumes = computed(() =>
  filterLevelWaterVolumes(manifest.value?.waterVolumes ?? [], waterQuery.value)
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
const filteredSounds = computed(() =>
  filterSceneObjects(manifest.value?.ambientSounds ?? [], soundQuery.value)
)
const filteredEffects = computed(() =>
  filterSceneObjects(manifest.value?.effects ?? [], effectQuery.value)
)
const terrainLayerVisibility = computed(() =>
  Object.fromEntries(
    Object.entries(terrainLayerStates.value).map(([name, state]) => [
      name,
      state.enabled
    ])
  )
)

watch([actorQuery, actorPageSize], () => (actorPage.value = 1))
watch(routeName, () => void loadScene(), { immediate: true })

function stop() {
  playing.value = false
  if (animationFrame !== undefined) cancelAnimationFrame(animationFrame)
  animationFrame = undefined
}

function showFrame(index: number) {
  if (!frames.value.length) return
  frameIndex.value = Math.min(Math.max(index, 0), frames.value.length - 1)
  const frame = frames.value[frameIndex.value]!
  preview.value?.setCameraPose(frame.location, frame.rotation)
}

function tick(timestamp: number) {
  if (!playing.value || frames.value.length < 2) return
  if (!segmentStartedAt) segmentStartedAt = timestamp
  const from = frames.value[frameIndex.value]!
  const nextIndex = (frameIndex.value + 1) % frames.value.length
  const to = frames.value[nextIndex]!
  const duration =
    (to.className === 'ActionWarp' ? 0.05 : Math.max(to.duration || 1, 0.1)) *
    1000
  const amount = (timestamp - segmentStartedAt) / duration
  const pose = interpolateScenePose(from, to, amount)
  preview.value?.setCameraPose(pose.location, pose.rotation)
  if (amount >= 1) {
    frameIndex.value = nextIndex
    segmentStartedAt = timestamp
  }
  animationFrame = requestAnimationFrame(tick)
}

function togglePlayback() {
  if (playing.value) return stop()
  if (frames.value.length < 2) return
  playing.value = true
  segmentStartedAt = 0
  animationFrame = requestAnimationFrame(tick)
}

function selectManager() {
  stop()
  showFrame(0)
}

function scrub(event: Event) {
  stop()
  showFrame(Number((event.target as HTMLInputElement).value))
}

function layerEnabled(terrainName: string, index: number) {
  return terrainLayerStates.value[terrainName]?.enabled[index] ?? true
}

function setLayerEnabled(terrainName: string, index: number, enabled: boolean) {
  const state = terrainLayerStates.value[terrainName]
  if (state)
    terrainLayerStates.value[terrainName] = setTerrainLayerEnabled(
      state,
      index,
      enabled
    )
}

function enableAllLayers(terrainName: string) {
  const state = terrainLayerStates.value[terrainName]
  if (state)
    terrainLayerStates.value[terrainName] = enableAllTerrainLayers(state)
}

function soloLayer(terrainName: string, index: number) {
  const state = terrainLayerStates.value[terrainName]
  if (state)
    terrainLayerStates.value[terrainName] = toggleSoloTerrainLayer(state, index)
}

async function focusActor(actor: LevelActorManifestEntry) {
  if (!actor.meshUrl || !actorsVisible.value) return
  selectedActorName.value = actor.name
  await nextTick()
  preview.value?.focusActor(actor.name)
}

async function focusBsp(bsp: LevelBspMeshManifestEntry) {
  if (!bsp.meshUrl) return
  selectedBspName.value = bsp.name
  await nextTick()
  preview.value?.focusBsp(bsp.name)
}

async function focusLight(light: LevelLightManifestEntry) {
  selectedLightName.value = light.name
  lightHelpersVisible.value = true
  await nextTick()
  preview.value?.focusLight(light.name)
}

async function focusWater(volume: LevelWaterVolumeManifestEntry) {
  if (volume.status !== 'resolved' || !waterVolumesVisible.value) return
  selectedWaterName.value = volume.name
  await nextTick()
  preview.value?.focusWater(volume.name)
}

async function focusWaterSurface(surface: LevelBspMeshManifestEntry) {
  if (!surface.meshUrl || !waterSurfacesVisible.value) return
  selectedWaterSurfaceName.value = surface.name
  await nextTick()
  preview.value?.focusWaterSurface(surface.name)
}

function focusSceneObject(object: SceneObjectManifestEntry) {
  preview.value?.focusPosition(object.location)
}

function viewSceneObject(object: SceneObjectManifestEntry) {
  stop()
  preview.value?.setCameraPose(object.location, object.rotation)
}

function setSkyZoneChunkVisible(name: string, visible: boolean) {
  skyZoneChunkVisibility.value = {
    ...skyZoneChunkVisibility.value,
    [name]: visible
  }
}

async function loadScene() {
  stop()
  loading.value = true
  sceneReady.value = false
  error.value = undefined
  previewError.value = undefined
  materialError.value = undefined
  manifest.value = undefined
  inspectorTab.value = 'cinematic'
  selectedActorName.value = undefined
  selectedBspName.value = undefined
  selectedLightName.value = undefined
  selectedWaterName.value = undefined
  selectedWaterSurfaceName.value = undefined
  skyZoneVisible.value = false
  worldBaseVisible.value = false
  actorQuery.value = ''
  lightQuery.value = ''
  waterQuery.value = ''
  soundQuery.value = ''
  effectQuery.value = ''
  frameIndex.value = 0
  try {
    const entry = await $fetch<SceneCatalogEntry>(
      assetCatalogEntryUrl(apiBase, 'scenes', routeName.value)
    )
    if (!entry?.manifestUrl) {
      error.value = entry?.error ?? `Scene “${routeName.value}” is unavailable.`
      return
    }
    const loadedManifest = await $fetch<SceneManifest>(entry.manifestUrl)
    if (loadedManifest.schemaVersion !== 11)
      throw new Error('The scene manifest schema is unsupported.')
    manifest.value = loadedManifest
    selectedManagerName.value = loadedManifest.sceneManagers[0]?.name
    terrainLayerStates.value = createTerrainLayerStates(loadedManifest.terrains)
    skyZoneChunkVisibility.value = Object.fromEntries(
      loadedManifest.bspMeshes
        .filter((mesh) => mesh.role === 'sky-zone')
        .map((mesh) => [mesh.name, true])
    )
  } catch {
    error.value = `Scene “${routeName.value}” could not be loaded.`
  } finally {
    loading.value = false
  }
}

onBeforeUnmount(stop)
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Client scene"
      :title="manifest?.name ?? routeName"
      description="Inspect reconstructed geometry, orchestration, sounds, effects, and environment."
      icon="i-lucide-clapperboard"
    >
      <template #actions>
        <UButton
          label="All scenes"
          icon="i-lucide-arrow-left"
          color="neutral"
          variant="outline"
          to="/assets/scenes"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="error"
      color="error"
      title="Scene unavailable"
      :description="error"
    />
    <div
      v-if="loading"
      class="grid min-h-64 place-items-center text-sm text-muted"
    >
      Loading scene…
    </div>

    <template v-else-if="manifest">
      <div class="grid gap-3 sm:grid-cols-2 xl:grid-cols-6">
        <UCard>
          <p class="text-xs text-muted">BSP</p>
          <p class="text-2xl font-semibold">{{ manifest.bspMeshes.length }}</p>
          <p class="text-xs text-muted">
            {{ bspTotals.surfaces.toLocaleString() }} structural surfaces
          </p>
        </UCard>
        <UCard>
          <p class="text-xs text-muted">Placed meshes</p>
          <p class="text-2xl font-semibold">
            {{ manifest.actors.length.toLocaleString() }}
          </p>
        </UCard>
        <UCard>
          <p class="text-xs text-muted">Terrain</p>
          <p class="text-2xl font-semibold">
            {{ manifest.terrains.length }}
          </p>
        </UCard>
        <UCard>
          <p class="text-xs text-muted">Lights</p>
          <p class="text-2xl font-semibold">
            {{ manifest.lights.length }}
          </p>
        </UCard>
        <UCard>
          <p class="text-xs text-muted">Cinematic</p>
          <p class="text-2xl font-semibold">
            {{ cinematicCount.toLocaleString() }}
          </p>
        </UCard>
        <UCard>
          <p class="text-xs text-muted">Sounds · effects</p>
          <p class="text-2xl font-semibold">
            {{ manifest.ambientSounds.length }} · {{ manifest.effects.length }}
          </p>
        </UCard>
      </div>

      <UAlert
        v-if="previewError"
        color="error"
        title="Preview unavailable"
        :description="previewError"
      />
      <UAlert
        v-if="materialError"
        color="warning"
        title="Material fallback"
        :description="materialError"
      />

      <div
        class="grid items-start gap-4 xl:grid-cols-[minmax(0,2fr)_minmax(24rem,1fr)]"
      >
        <UCard :ui="{ body: 'p-2 sm:p-2' }">
          <StudioLevelPreview
            ref="preview"
            :manifest="manifest"
            :selected-actor-name="selectedActorName"
            :selected-bsp-name="selectedBspName"
            :actors-visible="actorsVisible"
            :bsp-visible="bspVisible"
            :sky-zone-visible="skyZoneVisible"
            :sky-zone-chunk-visibility="skyZoneChunkVisibility"
            :world-base-visible="worldBaseVisible"
            :terrain-layer-visibility="terrainLayerVisibility"
            :light-helpers-visible="lightHelpersVisible"
            :selected-light-name="selectedLightName"
            :water-surfaces-visible="waterSurfacesVisible"
            :selected-water-surface-name="selectedWaterSurfaceName"
            :water-volumes-visible="waterVolumesVisible"
            :selected-water-name="selectedWaterName"
            :distance-fog-enabled="distanceFogEnabled"
            @error="previewError = $event"
            @material-error="materialError = $event"
            @light-select="selectedLightName = $event"
            @ready-change="sceneReady = $event"
          />
          <p class="mt-2 text-center text-xs text-muted">
            Drag to orbit · scroll to zoom · right-drag to pan
          </p>
        </UCard>

        <UCard
          v-if="sceneReady"
          class="xl:sticky xl:top-4"
          :ui="{ body: 'p-0 sm:p-0' }"
        >
          <template #header>
            <div
              class="grid grid-cols-3 gap-1"
              role="tablist"
              aria-label="Scene inspector"
            >
              <UButton
                v-for="tab in [
                  ['bsp', 'BSP', 'i-lucide-blocks'],
                  ['actors', 'Meshes', 'i-lucide-box'],
                  ['terrain', 'Terrain', 'i-lucide-layers-3'],
                  ['lights', 'Lights', 'i-lucide-lightbulb'],
                  ['water', 'Water', 'i-lucide-waves'],
                  ['cinematic', 'Cinematic', 'i-lucide-clapperboard'],
                  ['sounds', 'Sounds', 'i-lucide-volume-2'],
                  ['effects', 'Effects', 'i-lucide-sparkles'],
                  ['environment', 'Environment', 'i-lucide-cloud-fog']
                ]"
                :key="tab[0]"
                :label="tab[1]"
                :icon="tab[2]"
                color="neutral"
                :variant="inspectorTab === tab[0] ? 'soft' : 'ghost'"
                role="tab"
                :aria-selected="inspectorTab === tab[0]"
                class="justify-center"
                @click="inspectorTab = tab[0] as InspectorTab"
              />
            </div>
          </template>

          <template v-if="inspectorTab === 'bsp'">
            <div class="space-y-3 border-b border-default p-4">
              <div class="flex justify-between gap-3">
                <div>
                  <h2 class="text-sm font-semibold">Structural BSP</h2>
                  <p class="text-xs text-muted">
                    {{ bspTotals.vertices.toLocaleString() }} vertices ·
                    {{ bspTotals.triangles.toLocaleString() }} triangles
                  </p>
                </div>
                <USwitch
                  v-model="bspVisible"
                  label="Show"
                  aria-label="Show scene BSP"
                />
              </div>
              <UAlert
                v-if="bspTotals.errors"
                color="error"
                variant="subtle"
                title="BSP load errors"
                :description="`${bspTotals.errors} chunks are unavailable.`"
              />
              <UAlert
                v-else-if="bspTotals.fallbacks"
                color="warning"
                variant="subtle"
                title="Material fallback"
                :description="`${bspTotals.fallbacks} chunks use fallback materials.`"
              />
            </div>
            <div class="max-h-[58vh] divide-y divide-default overflow-y-auto">
              <div
                v-for="bsp in worldBspMeshes"
                :key="bsp.name"
                class="flex items-center gap-2 p-2"
                :class="selectedBspName === bsp.name ? 'bg-primary/10' : ''"
              >
                <button
                  class="min-w-0 flex-1 rounded-md p-2 text-left hover:bg-elevated"
                  @click="selectedBspName = bsp.name"
                  @dblclick="focusBsp(bsp)"
                >
                  <span class="flex justify-between gap-2"
                    ><span class="truncate text-sm font-medium">{{
                      bsp.name
                    }}</span
                    ><UBadge
                      :color="
                        bsp.materialStatus === 'resolved'
                          ? 'success'
                          : 'warning'
                      "
                      variant="subtle"
                      >{{ bsp.materialStatus }}</UBadge
                    ></span
                  ><span class="mt-1 block text-xs text-muted"
                    >{{ bsp.surfaceCount }} surfaces ·
                    {{ bsp.triangleCount }} triangles ·
                    {{ bsp.resolvedMaterialCount }}/{{
                      bsp.materialCount
                    }}
                    materials</span
                  ><span
                    v-if="bsp.error"
                    class="mt-1 block text-xs text-warning"
                    >{{ bsp.error }}</span
                  >
                </button>
                <UButton
                  icon="i-lucide-focus"
                  color="neutral"
                  variant="ghost"
                  :disabled="!bsp.meshUrl || !bspVisible"
                  :aria-label="'Focus ' + bsp.name"
                  @click="focusBsp(bsp)"
                />
              </div>
              <p
                v-if="!worldBspMeshes.length"
                class="p-8 text-center text-sm text-muted"
              >
                This scene has no ordinary structural BSP.
              </p>
            </div>
          </template>

          <template v-else-if="inspectorTab === 'actors'">
            <div class="space-y-3 border-b border-default p-4">
              <div class="flex justify-between gap-3">
                <div>
                  <h2 class="text-sm font-semibold">Placed mesh instances</h2>
                  <p class="text-xs text-muted">
                    {{ filteredActors.length }} of {{ manifest.actors.length }}
                  </p>
                </div>
                <USwitch
                  v-model="actorsVisible"
                  label="Show"
                  aria-label="Show placed meshes"
                />
              </div>
              <UInput
                v-model="actorQuery"
                icon="i-lucide-search"
                placeholder="Search actors or meshes"
                aria-label="Search placed mesh instances"
              />
            </div>
            <div class="max-h-[52vh] divide-y divide-default overflow-y-auto">
              <div
                v-for="actor in visibleActors"
                :key="actor.name"
                class="flex items-center gap-2 p-2"
                :class="selectedActorName === actor.name ? 'bg-primary/10' : ''"
              >
                <button
                  class="min-w-0 flex-1 rounded-md p-2 text-left hover:bg-elevated"
                  @click="selectedActorName = actor.name"
                  @dblclick="focusActor(actor)"
                >
                  <span class="flex items-center gap-2">
                    <span class="text-sm font-medium">{{ actor.name }}</span>
                    <UBadge
                      :color="actor.meshUrl ? 'success' : 'warning'"
                      variant="subtle"
                    >
                      {{ actor.meshUrl ? 'resolved' : 'unresolved' }}
                    </UBadge>
                  </span>
                  <span class="mt-1 block truncate text-xs text-muted">
                    {{ actor.meshPackage }}.{{ actor.meshObject }} ·
                    {{ actor.className }}
                  </span>
                </button>
                <UButton
                  icon="i-lucide-focus"
                  color="neutral"
                  variant="ghost"
                  :disabled="!actor.meshUrl || !actorsVisible"
                  :aria-label="'Focus ' + actor.name"
                  @click="focusActor(actor)"
                />
              </div>
              <p
                v-if="!visibleActors.length"
                class="p-8 text-center text-sm text-muted"
              >
                No placed meshes match this search.
              </p>
            </div>
            <StudioTableFooter
              v-model:page="actorPage"
              v-model:page-size="actorPageSize"
              :total="filteredActors.length"
              :page-size-options="[50, 100, 200]"
            />
          </template>

          <template v-else-if="inspectorTab === 'terrain'">
            <div class="border-b border-default p-4">
              <h2 class="text-sm font-semibold">Terrain layers</h2>
              <p class="text-xs text-muted">
                Toggle imported blend layers or isolate one.
              </p>
            </div>
            <div class="max-h-[62vh] overflow-y-auto">
              <section
                v-for="terrain in manifest.terrains"
                :key="terrain.name"
                class="border-b border-default"
              >
                <div class="flex justify-between bg-elevated/40 px-4 py-3">
                  <div>
                    <h3 class="text-sm font-medium">{{ terrain.name }}</h3>
                    <p class="text-xs text-muted">
                      {{ terrain.layers.length }} layers ·
                      {{ terrain.materialStatus }}
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
                    v-for="(layer, index) in terrain.layers"
                    :key="layer.index"
                    class="flex items-center gap-3 p-3"
                  >
                    <img
                      v-if="layer.textureUrl"
                      :src="layer.textureUrl"
                      :alt="`${layer.textureObject} texture`"
                      class="size-12 rounded object-cover [image-rendering:pixelated]"
                    />
                    <div class="min-w-0 flex-1">
                      <p class="text-sm font-medium">Layer {{ layer.index }}</p>
                      <p class="truncate text-xs text-muted">
                        {{ layer.texturePackage }}.{{ layer.textureObject }}
                      </p>
                    </div>
                    <USwitch
                      :model-value="layerEnabled(terrain.name, index)"
                      :aria-label="`Enable ${terrain.name} layer ${layer.index}`"
                      @update:model-value="
                        setLayerEnabled(terrain.name, index, $event)
                      "
                    /><UButton
                      label="Solo"
                      size="xs"
                      color="neutral"
                      variant="ghost"
                      @click="soloLayer(terrain.name, index)"
                    />
                  </div>
                </div>
              </section>
              <p
                v-if="!manifest.terrains.length"
                class="p-8 text-center text-sm text-muted"
              >
                This scene has no terrain.
              </p>
            </div>
          </template>

          <template v-else-if="inspectorTab === 'lights'">
            <div class="space-y-3 border-b border-default p-4">
              <div class="flex justify-between gap-3">
                <div>
                  <h2 class="text-sm font-semibold">Imported lights</h2>
                  <p class="text-xs text-muted">
                    {{ filteredLights.length }} of {{ manifest.lights.length }}
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
                  class="min-w-0 flex-1 rounded-md p-2 text-left hover:bg-elevated"
                  @click="selectedLightName = light.name"
                  @dblclick="focusLight(light)"
                >
                  <span
                    class="inline-block size-3 rounded-full"
                    :style="{ backgroundColor: levelLightColor(light) }"
                  />
                  <span class="ml-2 text-sm font-medium">{{ light.name }}</span>
                  <span class="mt-1 block text-xs text-muted">
                    {{ light.className }} · brightness {{ light.brightness }} ·
                    radius {{ light.radius }}
                  </span>
                </button>
                <UButton
                  icon="i-lucide-focus"
                  color="neutral"
                  variant="ghost"
                  :aria-label="'Focus ' + light.name"
                  @click="focusLight(light)"
                />
              </div>
              <p
                v-if="!filteredLights.length"
                class="p-8 text-center text-sm text-muted"
              >
                No lights match this search.
              </p>
            </div>
          </template>

          <template v-else-if="inspectorTab === 'water'">
            <div class="space-y-3 border-b border-default p-4">
              <h2 class="text-sm font-semibold">Water</h2>
              <p class="text-xs text-muted">
                {{ waterSurfaceMeshes.length }} surfaces ·
                {{ manifest.waterVolumes.length }} volumes
              </p>
              <div class="grid grid-cols-2 gap-3">
                <USwitch
                  v-model="waterSurfacesVisible"
                  label="Surfaces"
                /><USwitch v-model="waterVolumesVisible" label="Volumes" />
              </div>
              <UInput
                v-model="waterQuery"
                icon="i-lucide-search"
                placeholder="Search water"
                aria-label="Search water"
              />
            </div>
            <div class="max-h-[58vh] divide-y divide-default overflow-y-auto">
              <div
                v-for="surface in filteredWaterSurfaces"
                :key="surface.name"
                class="flex items-center gap-2 p-2"
              >
                <button
                  class="min-w-0 flex-1 p-2 text-left"
                  @click="selectedWaterSurfaceName = surface.name"
                  @dblclick="focusWaterSurface(surface)"
                >
                  <span class="text-sm font-medium">{{ surface.name }}</span>
                  <span class="block text-xs text-muted">
                    Surface · {{ surface.triangleCount }} triangles
                  </span>
                </button>
                <UButton
                  icon="i-lucide-focus"
                  color="neutral"
                  variant="ghost"
                  @click="focusWaterSurface(surface)"
                />
              </div>
              <div
                v-for="volume in filteredWaterVolumes"
                :key="volume.name"
                class="flex items-center gap-2 p-2"
              >
                <button
                  class="min-w-0 flex-1 p-2 text-left"
                  @click="selectedWaterName = volume.name"
                  @dblclick="focusWater(volume)"
                >
                  <span class="text-sm font-medium">{{ volume.name }}</span>
                  <span class="block text-xs text-muted">
                    Volume · {{ volume.status }} ·
                    {{ volume.triangleCount }} triangles
                  </span>
                </button>
                <UButton
                  icon="i-lucide-focus"
                  color="neutral"
                  variant="ghost"
                  @click="focusWater(volume)"
                />
              </div>
              <p
                v-if="
                  !filteredWaterSurfaces.length && !filteredWaterVolumes.length
                "
                class="p-8 text-center text-sm text-muted"
              >
                This scene has no matching water.
              </p>
            </div>
          </template>

          <template v-else-if="inspectorTab === 'cinematic'">
            <div class="space-y-3 border-b border-default p-4">
              <div class="flex gap-2">
                <UButton
                  :label="playing ? 'Pause' : 'Play camera path'"
                  :icon="playing ? 'i-lucide-pause' : 'i-lucide-play'"
                  :disabled="frames.length < 2"
                  @click="togglePlayback"
                /><UButton
                  label="Frame scene"
                  color="neutral"
                  variant="outline"
                  @click="preview?.frameMap()"
                />
              </div>
              <select
                v-if="managerOptions.length"
                v-model="selectedManagerName"
                aria-label="Scene manager"
                class="w-full rounded-md border border-default bg-default px-3 py-2 text-sm"
                @change="selectManager"
              >
                <option
                  v-for="manager in managerOptions"
                  :key="manager.value"
                  :value="manager.value"
                >
                  {{ manager.label }}
                </option>
              </select>
              <input
                v-if="frames.length"
                class="w-full"
                type="range"
                min="0"
                :max="frames.length - 1"
                :value="frameIndex"
                aria-label="Camera frame"
                @input="scrub"
              />
              <p class="text-xs text-muted">
                {{
                  frames.length
                    ? `${frameIndex + 1} / ${frames.length} resolved frames`
                    : 'No resolved camera path'
                }}
              </p>
            </div>
            <div class="max-h-[56vh] overflow-y-auto">
              <section>
                <p
                  class="border-b border-default bg-elevated/50 px-4 py-2 text-xs font-semibold uppercase text-muted"
                >
                  Resolved path
                </p>
                <div class="divide-y divide-default">
                  <button
                    v-for="(frame, index) in frames"
                    :key="`${frame.name}-${index}`"
                    class="block w-full p-3 text-left hover:bg-elevated"
                    :class="frameIndex === index ? 'bg-primary/10' : ''"
                    @click="showFrame(index)"
                  >
                    <span class="text-sm font-medium"
                      >{{ frame.className }} · {{ frame.name }}</span
                    ><span class="block text-xs text-muted"
                      >Target {{ frame.target ?? 'direct pose' }} ·
                      {{ frame.duration }}s</span
                    >
                  </button>
                </div>
              </section>
              <section>
                <p
                  class="border-y border-default bg-elevated/50 px-4 py-2 text-xs font-semibold uppercase text-muted"
                >
                  Cameras · {{ manifest.cameras.length }}
                </p>
                <button
                  v-for="camera in manifest.cameras"
                  :key="camera.name"
                  class="block w-full p-3 text-left hover:bg-elevated"
                  @click="viewSceneObject(camera)"
                >
                  <span class="text-sm font-medium">{{ camera.name }}</span
                  ><span class="block text-xs text-muted"
                    >View authored pose</span
                  >
                </button>
              </section>
              <section>
                <p
                  class="border-y border-default bg-elevated/50 px-4 py-2 text-xs font-semibold uppercase text-muted"
                >
                  Interpolation points ·
                  {{ manifest.interpolationPoints.length }}
                </p>
                <button
                  v-for="point in manifest.interpolationPoints"
                  :key="point.name"
                  class="block w-full p-3 text-left hover:bg-elevated"
                  @click="viewSceneObject(point)"
                >
                  <span class="text-sm font-medium">{{ point.name }}</span>
                  <span class="block text-xs text-muted">
                    View authored pose · X {{ point.location.x.toFixed(0) }} · Y
                    {{ point.location.y.toFixed(0) }} · Z
                    {{ point.location.z.toFixed(0) }}
                  </span>
                </button>
              </section>
              <section>
                <p
                  class="border-y border-default bg-elevated/50 px-4 py-2 text-xs font-semibold uppercase text-muted"
                >
                  Actions · {{ manifest.actions.length }}
                </p>
                <div
                  v-for="action in manifest.actions"
                  :key="action.name"
                  class="p-3"
                >
                  <p class="text-sm font-medium">
                    {{ action.className }} · {{ action.name }}
                  </p>
                  <p class="truncate text-xs text-muted">
                    Target {{ action.target || 'none' }} ·
                    {{ action.duration }}s
                  </p>
                </div>
              </section>
              <section>
                <p
                  class="border-y border-default bg-elevated/50 px-4 py-2 text-xs font-semibold uppercase text-muted"
                >
                  Managers · {{ manifest.sceneManagers.length }}
                </p>
                <div
                  v-for="manager in manifest.sceneManagers"
                  :key="manager.name"
                  class="p-3"
                >
                  <p class="text-sm font-medium">
                    {{ sceneManagerLabel(manager) }}
                  </p>
                  <p class="truncate text-xs text-muted">
                    {{ manager.properties.Actions || 'No linked actions' }}
                  </p>
                </div>
              </section>
            </div>
          </template>

          <template v-else-if="inspectorTab === 'sounds'">
            <div class="space-y-3 border-b border-default p-4">
              <h2 class="text-sm font-semibold">Ambient sounds</h2>
              <p class="text-xs text-muted">
                {{ filteredSounds.length }} of
                {{ manifest.ambientSounds.length }} references
              </p>
              <UInput
                v-model="soundQuery"
                icon="i-lucide-search"
                placeholder="Search sounds"
                aria-label="Search ambient sounds"
              />
            </div>
            <div class="max-h-[62vh] divide-y divide-default overflow-y-auto">
              <div
                v-for="sound in filteredSounds"
                :key="sound.name"
                class="flex items-center gap-2 p-2"
              >
                <button
                  class="min-w-0 flex-1 rounded-md p-2 text-left hover:bg-elevated"
                  @dblclick="focusSceneObject(sound)"
                >
                  <span class="flex items-center gap-2">
                    <span class="text-sm font-medium">{{ sound.name }}</span>
                    <UBadge
                      :color="
                        sceneObjectStatus(sound) === 'resolved'
                          ? 'success'
                          : sceneObjectStatus(sound) === 'diagnostic'
                            ? 'warning'
                            : 'neutral'
                      "
                      variant="subtle"
                    >
                      {{ sceneObjectStatus(sound) }}
                    </UBadge>
                  </span>
                  <span class="mt-1 block truncate text-xs text-muted">
                    {{ sound.className }} ·
                    {{ sound.target || sound.resourceUrl || 'metadata only' }}
                  </span>
                  <span class="mt-1 block truncate text-xs text-dimmed">
                    Type {{ sound.properties.AmbientSoundType ?? 'unknown' }} ·
                    radius {{ sound.properties.SoundRadius ?? 'default' }} ·
                    volume {{ sound.properties.SoundVolume ?? 'default' }} ·
                    random {{ sound.properties.AmbientRandom ?? 'none' }}
                  </span>
                  <span
                    v-if="sound.diagnostic"
                    class="mt-1 block text-xs text-warning"
                  >
                    {{ sound.diagnostic }}
                  </span>
                </button>
                <UButton
                  icon="i-lucide-focus"
                  color="neutral"
                  variant="ghost"
                  :aria-label="'Focus ' + sound.name"
                  @click="focusSceneObject(sound)"
                />
              </div>
              <p
                v-if="!filteredSounds.length"
                class="p-8 text-center text-sm text-muted"
              >
                No sounds match this search.
              </p>
            </div>
          </template>

          <template v-else-if="inspectorTab === 'effects'">
            <div class="space-y-3 border-b border-default p-4">
              <h2 class="text-sm font-semibold">Authored effects</h2>
              <p class="text-xs text-muted">
                {{ filteredEffects.length }} of
                {{ manifest.effects.length }} objects
              </p>
              <UInput
                v-model="effectQuery"
                icon="i-lucide-search"
                placeholder="Search effects"
                aria-label="Search authored effects"
              />
            </div>
            <div class="max-h-[62vh] divide-y divide-default overflow-y-auto">
              <div
                v-for="effect in filteredEffects"
                :key="effect.name"
                class="flex items-center gap-2 p-2"
              >
                <button
                  class="min-w-0 flex-1 rounded-md p-2 text-left hover:bg-elevated"
                  @dblclick="focusSceneObject(effect)"
                >
                  <span class="flex items-center gap-2">
                    <span class="text-sm font-medium">{{ effect.name }}</span>
                    <UBadge
                      :color="
                        effect.particle?.enabled === false
                          ? 'neutral'
                          : effect.diagnostic
                            ? 'warning'
                            : 'success'
                      "
                      variant="subtle"
                    >
                      {{
                        effect.particle?.enabled === false
                          ? 'disabled'
                          : effect.className
                      }}
                    </UBadge>
                  </span>
                  <span class="mt-1 block truncate text-xs text-muted">
                    Owner {{ effect.owner || 'none' }} ·
                    {{
                      effect.resourceUrl ? 'resource resolved' : 'metadata only'
                    }}
                  </span>
                  <span
                    v-if="effect.diagnostic"
                    class="mt-1 block text-xs text-warning"
                  >
                    {{ effect.diagnostic }}
                  </span>
                </button>
                <UButton
                  icon="i-lucide-focus"
                  color="neutral"
                  variant="ghost"
                  :aria-label="'Focus ' + effect.name"
                  @click="focusSceneObject(effect)"
                />
              </div>
              <p
                v-if="!filteredEffects.length"
                class="p-8 text-center text-sm text-muted"
              >
                No effects match this search.
              </p>
            </div>
          </template>

          <template v-else-if="inspectorTab === 'environment'">
            <div class="border-b border-default p-4">
              <h2 class="text-sm font-semibold">Scene environment</h2>
              <p class="text-xs text-muted">
                Atmosphere and special scene geometry
              </p>
            </div>
            <div class="max-h-[62vh] overflow-y-auto">
              <section class="space-y-3 border-b border-default p-4">
                <div class="flex justify-between gap-3">
                  <div>
                    <h3 class="text-sm font-semibold">Atmosphere</h3>
                    <p class="text-xs text-muted">
                      Ambient
                      {{
                        levelEnvironmentColor(manifest.environment.ambientColor)
                          .label
                      }}
                    </p>
                  </div>
                  <USwitch
                    v-model="distanceFogEnabled"
                    label="Apply fog"
                    :disabled="!manifest.environment.distanceFog"
                  />
                </div>
                <p class="text-xs text-muted">
                  Brightness
                  {{
                    Math.round(manifest.environment.ambientBrightness * 100)
                  }}% ·
                  {{
                    manifest.environment.distanceFog
                      ? `fog ${manifest.environment.distanceFog.start}–${manifest.environment.distanceFog.end}`
                      : 'no authored fog'
                  }}
                </p>
              </section>
              <section v-if="manifest.skyZones.length">
                <p
                  class="border-b border-default bg-elevated/50 px-4 py-2 text-xs font-semibold uppercase text-muted"
                >
                  Sky zones
                </p>
                <div
                  v-for="zone in manifest.skyZones"
                  :key="zone.name"
                  class="p-3"
                >
                  <p class="text-sm font-medium">{{ zone.name }}</p>
                  <p class="text-xs text-muted">
                    {{ zone.lensFlares.length }} flares · pan
                    {{ zone.texUPanSpeed }}, {{ zone.texVPanSpeed }}
                  </p>
                </div>
              </section>
              <section v-if="manifest.skyBackdrops.length">
                <p
                  class="border-y border-default bg-elevated/50 px-4 py-2 text-xs font-semibold uppercase text-muted"
                >
                  Fake backdrops · {{ manifest.skyBackdrops.length }}
                </p>
                <div
                  v-for="backdrop in manifest.skyBackdrops"
                  :key="backdrop.name"
                  class="p-3"
                >
                  <p class="text-sm font-medium">{{ backdrop.name }}</p>
                  <p class="text-xs text-muted">
                    {{
                      backdrop.meshUrl
                        ? 'resolved'
                        : backdrop.error || 'unavailable'
                    }}
                  </p>
                </div>
              </section>
              <section v-if="skyZoneBspMeshes.length">
                <div
                  class="flex justify-between border-y border-default bg-elevated/50 px-4 py-3"
                >
                  <div>
                    <p class="text-sm font-semibold">Sky BSP</p>
                    <p class="text-xs text-muted">
                      {{ skyZoneBspMeshes.length }} chunks
                    </p>
                  </div>
                  <USwitch
                    v-model="skyZoneVisible"
                    label="Show group"
                    aria-label="Show sky BSP group"
                  />
                </div>
                <div
                  v-for="bsp in skyZoneBspMeshes"
                  :key="bsp.name"
                  class="flex items-center justify-between gap-3 p-3"
                >
                  <span class="min-w-0"
                    ><span class="block truncate text-sm font-medium">{{
                      bsp.name
                    }}</span
                    ><span class="text-xs text-muted"
                      >{{ bsp.surfaceCount }} surfaces · {{ bsp.skyZone }}</span
                    ></span
                  ><USwitch
                    :model-value="skyZoneChunkVisibility[bsp.name] !== false"
                    :aria-label="'Show ' + bsp.name"
                    @update:model-value="
                      setSkyZoneChunkVisible(bsp.name, $event)
                    "
                  />
                </div>
              </section>
              <section v-if="worldBaseBspMeshes.length">
                <div
                  class="flex justify-between border-y border-default bg-elevated/50 px-4 py-3"
                >
                  <div>
                    <p class="text-sm font-semibold">World bases</p>
                    <p class="text-xs text-muted">Structural foundations</p>
                  </div>
                  <USwitch
                    v-model="worldBaseVisible"
                    label="Show group"
                    aria-label="Show world bases group"
                  />
                </div>
                <div
                  v-for="bsp in worldBaseBspMeshes"
                  :key="bsp.name"
                  class="flex items-center gap-2 p-2"
                >
                  <button
                    class="min-w-0 flex-1 p-2 text-left"
                    @click="selectedBspName = bsp.name"
                    @dblclick="focusBsp(bsp)"
                  >
                    <span class="text-sm font-medium">{{ bsp.name }}</span>
                    <span class="block text-xs text-muted">
                      {{ bsp.surfaceCount }} surfaces ·
                      {{ bsp.triangleCount }} triangles
                    </span>
                  </button>
                  <UButton
                    icon="i-lucide-focus"
                    color="neutral"
                    variant="ghost"
                    :disabled="!worldBaseVisible"
                    @click="focusBsp(bsp)"
                  />
                </div>
              </section>
            </div>
          </template>
        </UCard>

        <UCard v-else class="xl:sticky xl:top-4">
          <div
            class="grid min-h-64 place-items-center p-8 text-center text-sm text-muted"
          >
            <span>{{
              previewError
                ? 'Scene controls unavailable.'
                : 'Loading scene controls…'
            }}</span>
          </div>
        </UCard>
      </div>
    </template>
  </div>
</template>
