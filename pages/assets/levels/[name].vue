<script setup lang="ts">
import type {
  LevelActorManifestEntry,
  LevelCatalogEntry,
  LevelCatalogManifest,
  LevelLightManifestEntry,
  LevelManifest,
  LevelWaterVolumeManifestEntry
} from '@l2/ui'
import { levelCatalogManifestUrl } from '@l2/ui'
import { computed, nextTick, watch } from 'vue'
import { useRoute } from 'vue-router'
import {
  createTerrainLayerStates,
  enableAllTerrainLayers,
  filterLevelLights,
  filterLevelWaterVolumes,
  levelLightColor,
  setTerrainLayerEnabled,
  toggleSoloTerrainLayer,
  type TerrainLayerStates
} from '../../../lib/level-inspector'
import { filterLevelActors } from '../../../lib/level-map'
import { paginate } from '../../../lib/studio-content'

interface LevelPreviewApi {
  focusActor(name: string): void
  focusLight(name: string): void
  focusWater(name: string): void
}

type InspectorTab = 'actors' | 'terrain' | 'lights' | 'water'

const route = useRoute()
const catalogEntry = ref<LevelCatalogEntry>()
const manifest = ref<LevelManifest>()
const preview = ref<LevelPreviewApi>()
const selectedActorName = ref<string>()
const selectedLightName = ref<string>()
const selectedWaterName = ref<string>()
const inspectorTab = ref<InspectorTab>('actors')
const actorsVisible = ref(true)
const lightHelpersVisible = ref(false)
const waterVolumesVisible = ref(true)
const terrainLayerStates = ref<TerrainLayerStates>({})
const query = ref('')
const lightQuery = ref('')
const waterQuery = ref('')
const page = ref(1)
const pageSize = ref(50)
const loading = ref(true)
const error = ref<string>()
const previewError = ref<string>()
const terrainMaterialError = ref<string>()

const routeName = computed(() =>
  Array.isArray(route.params.name)
    ? (route.params.name[0] ?? '')
    : (route.params.name ?? '')
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
const filteredActors = computed(() =>
  filterLevelActors(manifest.value?.actors ?? [], query.value)
)
const visibleActors = computed(() =>
  paginate(filteredActors.value, page.value, pageSize.value)
)
const filteredLights = computed(() =>
  filterLevelLights(manifest.value?.lights ?? [], lightQuery.value)
)
const filteredWaterVolumes = computed(() =>
  filterLevelWaterVolumes(manifest.value?.waterVolumes ?? [], waterQuery.value)
)
const terrainLayerVisibility = computed(() =>
  Object.fromEntries(
    Object.entries(terrainLayerStates.value).map(([name, state]) => [
      name,
      state.enabled
    ])
  )
)

watch([query, pageSize], () => (page.value = 1))
watch(routeName, () => void loadLevel(), { immediate: true })

function selectActor(actor: LevelActorManifestEntry) {
  selectedActorName.value = actor.name
}

async function focusActor(actor: LevelActorManifestEntry) {
  if (!actor.meshUrl || !actorsVisible.value) return
  selectActor(actor)
  await nextTick()
  preview.value?.focusActor(actor.name)
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

function selectLight(light: LevelLightManifestEntry) {
  selectedLightName.value = light.name
  lightHelpersVisible.value = true
}

async function focusLight(light: LevelLightManifestEntry) {
  selectLight(light)
  await nextTick()
  preview.value?.focusLight(light.name)
}

function selectWater(volume: LevelWaterVolumeManifestEntry) {
  selectedWaterName.value = volume.name
}

async function focusWater(volume: LevelWaterVolumeManifestEntry) {
  if (volume.status !== 'resolved' || !waterVolumesVisible.value) return
  selectWater(volume)
  await nextTick()
  preview.value?.focusWater(volume.name)
}

async function loadLevel() {
  loading.value = true
  error.value = undefined
  previewError.value = undefined
  terrainMaterialError.value = undefined
  catalogEntry.value = undefined
  manifest.value = undefined
  selectedActorName.value = undefined
  selectedLightName.value = undefined
  selectedWaterName.value = undefined
  inspectorTab.value = 'actors'
  actorsVisible.value = true
  lightHelpersVisible.value = false
  waterVolumesVisible.value = true
  terrainLayerStates.value = {}
  query.value = ''
  lightQuery.value = ''
  waterQuery.value = ''

  try {
    const catalog = await $fetch<LevelCatalogManifest>(
      levelCatalogManifestUrl(),
      { query: { refresh: Date.now() } }
    )
    const entry = catalog.levels.find((level) => level.name === routeName.value)
    if (!entry) {
      error.value =
        'Map “' +
        routeName.value +
        '” is not present in the generated level catalog.'
      return
    }
    catalogEntry.value = entry
    if (!entry.manifestUrl) {
      error.value = entry.error ?? 'Map “' + entry.name + '” was not imported.'
      return
    }
    manifest.value = await $fetch<LevelManifest>(entry.manifestUrl)
    terrainLayerStates.value = createTerrainLayerStates(manifest.value.terrains)
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
      eyebrow="Level map"
      :title="catalogEntry?.name ?? routeName"
      description="Inspect terrain layers, placed static-mesh instances, and imported lights."
      icon="i-lucide-map-pinned"
    >
      <template #actions>
        <UButton
          label="All levels"
          icon="i-lucide-arrow-left"
          color="neutral"
          variant="outline"
          to="/assets/levels"
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
        <UButton color="error" variant="soft" size="sm" @click="loadLevel">
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
      <div class="grid gap-3 sm:grid-cols-5">
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
        title="Terrain material fallback"
        :description="terrainMaterialError"
      />

      <div
        class="grid items-start gap-4 xl:grid-cols-[minmax(0,2fr)_minmax(24rem,1fr)]"
      >
        <UCard :ui="{ body: 'p-2 sm:p-2' }">
          <StudioLevelPreview
            ref="preview"
            :manifest="manifest"
            :selected-actor-name="selectedActorName"
            :actors-visible="actorsVisible"
            :terrain-layer-visibility="terrainLayerVisibility"
            :light-helpers-visible="lightHelpersVisible"
            :selected-light-name="selectedLightName"
            :water-volumes-visible="waterVolumesVisible"
            :selected-water-name="selectedWaterName"
            @error="previewError = $event"
            @material-error="terrainMaterialError = $event"
            @light-select="selectedLightName = $event"
          />
          <p class="mt-2 text-center text-xs text-muted">
            Drag to orbit · scroll to zoom toward the pointer · right-drag to
            pan · double-click the preview to restore the framed view
          </p>
        </UCard>

        <UCard class="xl:sticky xl:top-4" :ui="{ body: 'p-0 sm:p-0' }">
          <template #header>
            <div
              class="grid grid-cols-4 gap-1"
              role="tablist"
              aria-label="Level inspector"
            >
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
            </div>
          </template>

          <template v-if="inspectorTab === 'actors'">
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
                    :style="{ backgroundColor: levelLightColor(light) }"
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

          <template v-else>
            <div class="space-y-3 border-b border-default p-4">
              <div class="flex items-start justify-between gap-3">
                <div>
                  <h2 class="text-sm font-semibold text-highlighted">
                    Water volumes
                  </h2>
                  <p class="text-xs text-muted">
                    {{ filteredWaterVolumes.length }} of
                    {{ manifest.waterVolumes.length }} volumes
                  </p>
                </div>
                <USwitch
                  v-model="waterVolumesVisible"
                  label="Show"
                  aria-label="Show water volumes"
                />
              </div>
              <UInput
                v-model="waterQuery"
                icon="i-lucide-search"
                placeholder="Search water or brush names"
                aria-label="Search water volumes"
                class="w-full"
              />
            </div>
            <div class="max-h-[62vh] divide-y divide-default overflow-y-auto">
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
                    <span class="truncate text-sm font-medium text-highlighted">
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
                  <span
                    v-if="volume.error"
                    class="mt-1 block text-xs text-warning"
                  >
                    {{ volume.error }}
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
              <div
                v-if="filteredWaterVolumes.length === 0"
                class="grid min-h-48 place-items-center p-8 text-center text-sm text-muted"
              >
                This map has no water volumes matching this search.
              </div>
            </div>
          </template>
        </UCard>
      </div>
    </template>
  </div>
</template>
