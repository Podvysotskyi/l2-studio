<script setup lang="ts">
import type { NpcAppearanceManifestEntry } from '~/types/studio'
import type { NpcRecord } from '~/types/models/content-directory'

defineProps<{
  npc: NpcRecord
  appearanceLoading: boolean
  appearanceError?: string
  manifestUrl?: string
  rawAppearance?: NpcAppearanceManifestEntry
  appearance?: NpcAppearanceManifestEntry
}>()
</script>

<template>
  <NpcVisuals
    :npc="npc"
    :appearance-loading="appearanceLoading"
    :appearance-error="appearanceError"
    :manifest-url="manifestUrl"
    :raw-appearance="rawAppearance"
    :appearance="appearance"
  />
</template>
