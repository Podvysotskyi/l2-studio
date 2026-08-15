<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import type { NpcAppearanceManifest, NpcAppearanceManifestEntry } from '~/types/studio'
import { getNpcAppearanceManifest, getNpcDefinition } from '~/services/studio-api'
import { getPublishedManifestWithRaw } from '~/services/published-assets'
import type { NpcRecord } from '~/types/models/content-directory'
import { selectedGameVersionKey } from '~/utils/game-version'

type NpcTab = 'overview' | 'visuals' | 'audio'

const route = useRoute()
const npc = ref<NpcRecord>()
const loading = ref(true)
const error = ref<string>()
const appearanceLoading = ref(false)
const appearanceLoadedId = ref<number>()
const appearanceError = ref<string>()
const manifestUrl = ref<string>()
const rawAppearance = ref<NpcAppearanceManifestEntry>()
const appearance = ref<NpcAppearanceManifestEntry>()
let appearanceRequest = 0

const npcId = computed(() => {
  const value = Array.isArray(route.params.id) ? route.params.id[0] : route.params.id
  const id = Number(value)
  return Number.isSafeInteger(id) && id >= 0 ? id : undefined
})
const activeTab = computed<NpcTab>(() => {
  if (route.path.endsWith('/visuals')) return 'visuals'
  if (route.path.endsWith('/audio')) return 'audio'
  return 'overview'
})
const tabs = [
  { label: 'Overview', icon: 'i-lucide-notebook-tabs', value: 'overview' },
  { label: 'Visuals', icon: 'i-lucide-person-standing', value: 'visuals' },
  { label: 'Audio & effects', icon: 'i-lucide-volume-2', value: 'audio' }
]

watch(npcId, () => void loadNpc(), { immediate: true })
watch(() => npc.value?.id, resetAppearance)
watch([activeTab, () => npc.value?.id], () => {
  if (activeTab.value !== 'overview') void loadAppearance()
}, { immediate: true })

async function loadNpc() {
  if (npcId.value === undefined) {
    npc.value = undefined
    loading.value = false
    error.value = 'The NPC identifier is invalid.'
    return
  }
  loading.value = true
  error.value = undefined
  try {
    npc.value = await getNpcDefinition(npcId.value)
  } catch {
    npc.value = undefined
    error.value = 'The NPC definition could not be loaded.'
  } finally {
    loading.value = false
  }
}

function resetAppearance() {
  appearanceRequest++
  appearanceLoading.value = false
  appearanceLoadedId.value = undefined
  appearanceError.value = undefined
  manifestUrl.value = undefined
  rawAppearance.value = undefined
  appearance.value = undefined
}

async function loadAppearance() {
  const currentNpc = npc.value
  if (!currentNpc || appearanceLoading.value || appearanceLoadedId.value === currentNpc.id) return
  if (selectedGameVersionKey() !== 'c1') {
    appearanceLoadedId.value = currentNpc.id
    appearanceError.value = 'NPC appearance previews are currently available only for Chronicle 1.'
    return
  }
  if (!currentNpc.hasVisuals) {
    appearanceLoadedId.value = currentNpc.id
    appearanceError.value = `NPC ${currentNpc.id} has no entry in the active appearance catalog.`
    return
  }

  appearanceLoading.value = true
  const request = ++appearanceRequest
  appearanceError.value = undefined
  try {
    const manifestReference = await getNpcAppearanceManifest(currentNpc.id)
    const manifest = await getPublishedManifestWithRaw<NpcAppearanceManifest>(manifestReference.manifestUrl)
    if (request !== appearanceRequest) return
    if (manifest.raw.npc.id !== currentNpc.id) {
      appearanceError.value = `The published appearance manifest does not belong to NPC ${currentNpc.id}.`
      return
    }
    manifestUrl.value = manifestReference.manifestUrl
    rawAppearance.value = manifest.raw.npc
    appearance.value = manifest.resolved.npc
  } catch {
    if (request === appearanceRequest)
      appearanceError.value = 'NPC appearance data is unavailable. Import NPC appearances, animations, and textures before previewing this NPC.'
  } finally {
    if (request === appearanceRequest) {
      appearanceLoadedId.value = currentNpc.id
      appearanceLoading.value = false
    }
  }
}

function selectTab(value: string | number) {
  const id = npcId.value
  if (id === undefined) return
  const tab = value as NpcTab
  const suffix = tab === 'overview' ? '' : `/${tab}`
  void navigateTo(`/authoring/npcs/${id}${suffix}`)
}
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Game content"
      :title="npc ? `${npc.name ?? `NPC ${npc.id}`} · Level ${npc.level}` : 'NPC definition'"
      :description="npc ? `ID: ${npc.id}` : 'View a normalized NPC record.'"
      icon="i-lucide-user-round"
    >
      <template #actions>
        <UButton label="Back to NPCs" icon="i-lucide-arrow-left" color="neutral" variant="outline" to="/authoring/npcs" />
      </template>
    </StudioPageHeader>

    <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" title="NPC definition unavailable" :description="error">
      <template #actions><UButton color="error" variant="soft" size="sm" @click="loadNpc">Try again</UButton></template>
    </UAlert>

    <template v-else-if="npc">
      <UTabs :items="tabs" :model-value="activeTab" :content="false" variant="link" @update:model-value="selectTab" />
      <NuxtPage
        :npc="npc"
        :appearance-loading="appearanceLoading"
        :appearance-error="appearanceError"
        :manifest-url="manifestUrl"
        :raw-appearance="rawAppearance"
        :appearance="appearance"
        @request-appearance="loadAppearance"
      />
    </template>

    <UCard v-else :ui="{ body: 'p-6' }"><USkeleton class="h-6 w-48" /></UCard>
  </div>
</template>
