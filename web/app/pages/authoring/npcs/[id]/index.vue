<script setup lang="ts">
import type { NpcRecord } from '~/types/models/content-directory'

defineProps<{ npc: NpcRecord }>()
</script>

<template>
  <NpcOverview :npc="npc" />
</template>
