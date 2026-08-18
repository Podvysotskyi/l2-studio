<script setup lang="ts">
import type { NpcSpawnWorldMap } from '~/types/models/npc-spawn-world-map'
import type { WorldMapOverviewManifest } from '~/types/models/world-map-overview'
import { getNpcSpawnWorldMap, getWorldMapOverview } from '~/services/studio-api'
import { getPublishedManifest } from '~/services/published-assets'
import { NpcSpawnWorldRenderer, type NpcSpawnWorldSelection } from '~/runtime/world/npc-spawn-world-renderer'
import { worldMapTileName } from '~/utils/world-map-coordinate'

const canvas = ref<HTMLCanvasElement>()
const data = ref<NpcSpawnWorldMap>()
const loading = ref(true)
const error = ref<string>()
const terrainUnavailable = ref(false)
const query = ref('')
const pointsVisible = ref(true)
const zonesVisible = ref(true)
const selection = ref<NpcSpawnWorldSelection>()
let renderer: NpcSpawnWorldRenderer | undefined
let resizeObserver: ResizeObserver | undefined

const pointCount = computed(() => data.value?.points.length ?? 0)
const zoneCount = computed(() => data.value?.zones.length ?? 0)
const selectedMap = computed(() => {
  if (!selection.value) return undefined
  if (selection.value.kind === 'point')
    return worldMapTileName(selection.value.value.x, selection.value.value.y)
  const node = selection.value.value.territoryNodes[0]
  return node ? worldMapTileName(node.x, node.y) : undefined
})

async function load() {
  loading.value = true
  error.value = undefined
  terrainUnavailable.value = false
  try {
    const map = await getNpcSpawnWorldMap()
    data.value = map
    let overview: WorldMapOverviewManifest | undefined
    try {
      const reference = await getWorldMapOverview()
      overview = await getPublishedManifest<WorldMapOverviewManifest>(reference.manifestUrl)
    } catch {
      terrainUnavailable.value = true
    }
    await renderer?.load(map, overview)
  } catch {
    error.value = 'NPC spawn data could not be loaded from the Studio API.'
  } finally {
    loading.value = false
  }
}

function applyFilters() {
  renderer?.setFilters(query.value, pointsVisible.value, zonesVisible.value)
}

watch([query, pointsVisible, zonesVisible], applyFilters)

onMounted(() => {
  if (!canvas.value) return
  renderer = new NpcSpawnWorldRenderer(canvas.value, {
    onSelect: value => { selection.value = value }
  })
  resizeObserver = new ResizeObserver(() => renderer?.resize())
  resizeObserver.observe(canvas.value)
  void load()
})

onBeforeUnmount(() => {
  resizeObserver?.disconnect()
  renderer?.dispose()
})
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-6">
    <StudioPageHeader
      eyebrow="NPC authoring"
      title="Spawn map"
      description="Inspect fixed NPC locations and territory-based spawn zones across the world."
      icon="i-lucide-map-pinned"
    >
      <template #actions>
        <UButton label="Fit world" icon="i-lucide-scan" color="neutral" variant="outline" @click="renderer?.fit()" />
        <UButton label="Refresh" icon="i-lucide-refresh-cw" color="neutral" variant="outline" :loading="loading" @click="load" />
      </template>
    </StudioPageHeader>

    <UAlert v-if="error" color="error" variant="subtle" title="Spawn map unavailable" :description="error">
      <template #actions><UButton color="error" variant="soft" size="sm" @click="load">Try again</UButton></template>
    </UAlert>
    <UAlert v-else-if="!loading && !pointCount && !zoneCount" color="info" variant="subtle" title="No NPC spawns are imported" description="Import NPC spawns for the selected game version to populate this map." />
    <UAlert v-else-if="terrainUnavailable" color="warning" variant="subtle" title="Terrain overview unavailable" description="Import maps to generate the 101 × 101 terrain overview. Spawn overlays remain available on the coordinate grid." />

    <UCard class="flex min-h-0 flex-1 flex-col" :ui="{ body: 'flex min-h-0 flex-1 flex-col gap-3 p-3 sm:p-4' }">
      <div class="flex flex-wrap items-center gap-3">
        <UInput v-model="query" icon="i-lucide-search" placeholder="Find NPC ID, name, group, or zone" class="min-w-64 flex-1" />
        <USwitch v-model="pointsVisible" label="Fixed spawns" />
        <USwitch v-model="zonesVisible" label="Spawn zones" />
        <span class="text-xs text-muted">{{ pointCount.toLocaleString() }} points · {{ zoneCount.toLocaleString() }} zones</span>
      </div>
      <div class="relative min-h-[34rem] flex-1 overflow-hidden rounded-lg border border-default bg-[#09120f]">
        <canvas ref="canvas" class="size-full touch-none outline-none" aria-label="Interactive NPC spawn world map" />
        <div class="pointer-events-none absolute top-3 left-3 rounded-md border border-white/10 bg-black/65 px-2.5 py-1.5 text-xs text-white/80 backdrop-blur-sm">
          Drag to orbit · scroll to zoom · select a marker or zone
        </div>
        <div v-if="loading" class="absolute inset-0 flex items-center justify-center bg-black/35 text-sm text-white backdrop-blur-sm">
          Loading spawn world…
        </div>
      </div>
      <div class="flex flex-wrap gap-4 text-xs text-muted">
        <span class="flex items-center gap-1.5"><span class="size-2 rounded-full bg-teal-400" /> Fixed NPC spawn</span>
        <span class="flex items-center gap-1.5"><span class="size-2 rounded-full bg-amber-400" /> Spawn-zone territory</span>
      </div>
    </UCard>

    <USlideover :open="Boolean(selection)" :title="selection?.kind === 'point' ? selection.value.spawnName : selection?.value.name" :description="selection?.kind === 'point' ? `NPC ${selection.value.npcId}` : 'Territory spawn zone'" :ui="{ content: 'max-w-lg' }" @update:open="open => { if (!open) selection = undefined }">
      <template #body>
        <template v-if="selection?.kind === 'point'">
          <dl class="grid grid-cols-2 gap-3 text-sm">
            <div><dt class="text-muted">NPC</dt><dd class="font-medium">{{ selection.value.npcName || `NPC ${selection.value.npcId}` }}</dd></div>
            <div><dt class="text-muted">Respawn</dt><dd class="font-medium">{{ selection.value.respawnDelaySeconds }} s</dd></div>
            <div><dt class="text-muted">Coordinates</dt><dd class="font-mono text-xs">{{ selection.value.x }}, {{ selection.value.y }}, {{ selection.value.z }}</dd></div>
            <div><dt class="text-muted">Heading</dt><dd class="font-mono text-xs">{{ selection.value.heading }}</dd></div>
          </dl>
          <div class="mt-6 grid gap-2">
            <UButton :to="`/authoring/npcs/${selection.value.npcId}`" label="Open NPC" icon="i-lucide-user-round" />
            <UButton v-if="selectedMap" :to="`/library/maps/${selectedMap}`" label="Open map" icon="i-lucide-map" color="neutral" variant="outline" />
          </div>
        </template>
        <template v-else-if="selection?.kind === 'zone'">
          <dl class="grid grid-cols-2 gap-3 text-sm"><div><dt class="text-muted">Vertical range</dt><dd class="font-mono text-xs">{{ selection.value.minZ }} to {{ selection.value.maxZ }}</dd></div><div><dt class="text-muted">NPC entries</dt><dd class="font-medium">{{ selection.value.npcs.length }}</dd></div></dl>
          <div class="mt-5 divide-y divide-default rounded-lg border border-default">
            <div v-for="npc in selection.value.npcs" :key="npc.npcId" class="flex items-center justify-between gap-3 px-3 py-2 text-sm">
              <NuxtLink :to="`/authoring/npcs/${npc.npcId}`" class="font-medium text-primary hover:underline">{{ npc.npcName || `NPC ${npc.npcId}` }}</NuxtLink>
              <span class="text-xs text-muted">×{{ npc.count }} · {{ npc.respawnDelaySeconds }} s</span>
            </div>
          </div>
          <UButton v-if="selectedMap" class="mt-6" :to="`/library/maps/${selectedMap}`" label="Open map" icon="i-lucide-map" color="neutral" variant="outline" block />
        </template>
      </template>
    </USlideover>
  </div>
</template>
