<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import type { PublishedStaticMeshMaterial, StaticMeshMaterialInspection } from '~/runtime/materials/static-mesh-material'
import type { StudioAnimationMaterialBinding } from '~/runtime'
import type { NpcAppearanceManifestEntry } from '~/types/studio'
import type { NpcRecord } from '~/types/models/content-directory'
import { publishedAssetUrl } from '~/utils/published-asset-url'

const props = defineProps<{
  npc: NpcRecord
  appearanceLoading: boolean
  appearanceError?: string
  manifestUrl?: string
  rawAppearance?: NpcAppearanceManifestEntry
  appearance?: NpcAppearanceManifestEntry
}>()
const previewError = ref<string>()
const previewMaterialWarning = ref<string>()
const previewMaterials = ref<StaticMeshMaterialInspection[]>([])
const manifestOpen = ref(false)
const publishedManifestUrl = computed(() => props.manifestUrl
  ? publishedAssetUrl(props.manifestUrl, String(useRuntimeConfig().public.assetBaseUrl))
  : undefined)
watch(() => props.npc.id, () => { manifestOpen.value = false })
const slotWarnings = computed(() => props.appearance?.materialSlots
  .map(slot => slot.warning)
  .filter((warning): warning is string => Boolean(warning)) ?? [])
const materialBindings = computed<StudioAnimationMaterialBinding[]>(() =>
  props.appearance?.materialSlots
    .filter(slot => slot.overrideMaterial?.material)
    .map(slot => ({
      sectionIndex: slot.sectionIndex,
      name: (slot.overrideMaterial?.material?.name ?? slot.overrideMaterial?.reference) || `Appearance material ${slot.sectionIndex + 1}`,
      diffuseUrl: slot.overrideMaterial?.url,
      material: slot.overrideMaterial?.material
        ? {
            ...slot.overrideMaterial.material,
            windMode: slot.overrideMaterial.material.windMode === 'none' ? null : slot.overrideMaterial.material.windMode
          } as unknown as PublishedStaticMeshMaterial
        : null
    })) ?? []
)

function setPreviewMaterials(materials: StaticMeshMaterialInspection[]) {
  previewMaterials.value = materials
}
</script>

<template>
  <div class="space-y-4">
    <UCard>
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 class="text-sm font-semibold text-highlighted">Published appearance manifest</h2>
          <p class="mt-1 text-xs text-muted">Raw JSON stored for this NPC in the generated appearance artifact.</p>
          <p class="mt-2 text-sm text-muted">Appearance ID <code class="ml-1 text-highlighted">{{ npc.appearanceId ?? 'Unmapped' }}</code></p>
        </div>
        <div class="flex gap-2">
          <UButton :label="manifestOpen ? 'Hide JSON' : 'Show JSON'" :icon="manifestOpen ? 'i-lucide-chevron-up' : 'i-lucide-code-2'" color="neutral" variant="outline" size="sm" @click="manifestOpen = !manifestOpen" />
          <UButton v-if="publishedManifestUrl" label="Open manifest" icon="i-lucide-external-link" color="neutral" variant="ghost" size="sm" :to="publishedManifestUrl" target="_blank" />
        </div>
      </div>
      <div v-if="manifestOpen" class="mt-4">
        <div v-if="appearanceLoading" class="flex items-center gap-2 text-sm text-muted"><UIcon name="i-lucide-loader-circle" class="size-4 animate-spin" /> Loading appearance manifest…</div>
        <UAlert v-else-if="appearanceError" color="warning" variant="subtle" :description="appearanceError" />
        <div v-else-if="rawAppearance" class="overflow-x-auto rounded-md bg-muted/40 p-3"><StudioJsonTree :value="rawAppearance" label="npc" /></div>
      </div>
    </UCard>

    <UAlert v-if="appearanceError" color="warning" variant="subtle" title="Appearance preview unavailable" :description="appearanceError" />
    <div v-else-if="appearanceLoading" class="grid min-h-64 place-items-center text-sm text-muted"><span class="flex items-center gap-2"><UIcon name="i-lucide-loader-circle" class="size-4 animate-spin" /> Loading NPC appearance…</span></div>
    <template v-else-if="appearance">
      <UAlert v-if="previewError" color="error" variant="subtle" title="Preview unavailable" :description="previewError" />
      <UAlert v-if="previewMaterialWarning" color="warning" variant="subtle" title="Material fallback" :description="previewMaterialWarning" />
      <UAlert v-if="slotWarnings.length" color="warning" variant="subtle" title="Appearance material warnings" :description="slotWarnings.join(' ')" />
      <UCard v-if="appearance.mesh.url" :ui="{ body: 'p-0 sm:p-0' }"><StudioAnimationPreview :url="appearance.mesh.url" :animation-url="appearance.mesh.animationUrl" :material-bindings="materialBindings" @error="previewError = $event" @material-warning="previewMaterialWarning = $event" @materials="setPreviewMaterials" /></UCard>
      <UAlert v-else color="warning" variant="subtle" title="Mesh unresolved" :description="`${appearance.mesh.reference || 'This NPC mesh'} is not available in the active animation catalog.`" />

      <div class="grid gap-4 lg:grid-cols-2">
        <UCard><h2 class="text-sm font-semibold text-highlighted">Appearance</h2><dl class="mt-4 grid grid-cols-2 gap-4 text-sm"><div><dt class="text-xs text-muted">Class</dt><dd class="mt-1 break-all text-highlighted">{{ appearance.className }}</dd></div><div><dt class="text-xs text-muted">Speed</dt><dd class="mt-1 text-highlighted">{{ appearance.speed }}</dd></div><div><dt class="text-xs text-muted">Collision radius</dt><dd class="mt-1 text-highlighted">{{ appearance.collisionRadius }}</dd></div><div><dt class="text-xs text-muted">Collision height</dt><dd class="mt-1 text-highlighted">{{ appearance.collisionHeight }}</dd></div></dl></UCard>
        <UCard>
          <h2 class="text-sm font-semibold text-highlighted">Material resolution</h2>
          <p class="mt-1 text-xs text-muted">{{ appearance.materialSlots.length }} mesh sections · {{ appearance.textures.length }} npcgrp overrides · {{ previewMaterials.length }} rendered materials</p>
          <div class="mt-4 space-y-2">
            <div v-for="slot in appearance.materialSlots" :key="slot.sectionIndex" class="rounded-md bg-elevated p-3">
              <div class="flex min-w-0 items-center gap-3">
                <img v-if="slot.effectiveMaterial?.url" :src="slot.effectiveMaterial.url" :alt="`Section ${slot.sectionIndex + 1} texture`" class="size-10 shrink-0 rounded object-cover">
                <div class="min-w-0 flex-1">
                  <p class="truncate text-xs font-medium text-highlighted">Section {{ slot.sectionIndex + 1 }} · {{ slot.effectiveMaterial?.material?.name ?? slot.effectiveMaterial?.reference ?? 'Fallback material' }}</p>
                  <p class="truncate text-xs text-muted">Default: {{ slot.defaultMaterial?.reference ?? 'none' }}</p>
                  <p class="truncate text-xs text-muted">Override: {{ slot.overrideMaterial?.reference ?? 'none' }}</p>
                </div>
                <UBadge :color="slot.effectiveSource === 'fallback' ? 'warning' : slot.effectiveSource === 'override' ? 'primary' : 'success'" variant="subtle">{{ slot.effectiveSource }}</UBadge>
              </div>
              <p v-if="slot.warning" class="mt-2 text-xs text-warning">{{ slot.warning }}</p>
            </div>
          </div>
        </UCard>
      </div>
    </template>
  </div>
</template>
