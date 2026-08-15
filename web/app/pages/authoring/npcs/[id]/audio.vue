<script setup lang="ts">
import type { NpcAppearanceManifestEntry } from '~/types/studio'

defineProps<{
  appearanceLoading: boolean
  appearanceError?: string
  appearance?: NpcAppearanceManifestEntry
}>()
</script>

<template>
  <NpcAudioEffects
    :appearance-loading="appearanceLoading"
    :appearance-error="appearanceError"
    :appearance="appearance"
  />
</template>
