<script setup lang="ts">
import type { NpcAppearanceManifestEntry } from '~/types/studio'

defineProps<{
  appearanceLoading: boolean
  appearanceError?: string
  appearance?: NpcAppearanceManifestEntry
}>()

function soundLabel(reference: { reference: string }, index: number) {
  return reference.reference || `Sound ${index + 1}`
}
</script>

<template>
  <div class="space-y-4">
    <UAlert v-if="appearanceError" color="warning" variant="subtle" title="Appearance audio unavailable" :description="appearanceError" />
    <div v-else-if="appearanceLoading" class="grid min-h-64 place-items-center text-sm text-muted"><span class="flex items-center gap-2"><UIcon name="i-lucide-loader-circle" class="size-4 animate-spin" /> Loading appearance audio…</span></div>
    <template v-else-if="appearance">
      <div class="grid gap-4 xl:grid-cols-3">
        <UCard v-for="[title, icon, sounds] in [['Attack sounds', 'i-lucide-swords', appearance.attackSounds], ['Defence sounds', 'i-lucide-shield', appearance.defenceSounds], ['Damage sounds', 'i-lucide-heart-crack', appearance.damageSounds]]" :key="title"><div class="flex items-center gap-2"><UIcon :name="icon" class="size-4 text-primary" /><h2 class="text-sm font-semibold text-highlighted">{{ title }}</h2></div><div v-if="sounds.length" class="mt-4 space-y-3"><div v-for="(sound, index) in sounds" :key="`${index}-${sound.reference}`" class="space-y-2 rounded-md bg-elevated p-3"><p class="break-all text-xs text-highlighted">{{ soundLabel(sound, index) }}</p><audio v-if="sound.url" :src="sound.url" controls preload="metadata" class="h-9 w-full" /><UBadge v-else color="warning" variant="subtle">Unresolved</UBadge></div></div><p v-else class="mt-4 text-sm text-muted">No references.</p></UCard>
      </div>
      <UCard><h2 class="text-sm font-semibold text-highlighted">Sound and effect settings</h2><dl class="mt-4 grid gap-4 sm:grid-cols-4 text-sm"><div><dt class="text-xs text-muted">Volume</dt><dd class="mt-1 text-highlighted">{{ appearance.soundVolume }}</dd></div><div><dt class="text-xs text-muted">Radius</dt><dd class="mt-1 text-highlighted">{{ appearance.soundRadius }}</dd></div><div><dt class="text-xs text-muted">Randomness</dt><dd class="mt-1 text-highlighted">{{ appearance.soundRandomness }}</dd></div><div><dt class="text-xs text-muted">Attack effect</dt><dd class="mt-1 break-all text-highlighted">{{ appearance.attackEffect.reference || 'None' }}</dd><UBadge v-if="!appearance.attackEffect.url" color="warning" variant="subtle" size="xs">Unresolved</UBadge></div></dl></UCard>
    </template>
  </div>
</template>
