<script setup lang="ts">
import { defineAsyncComponent } from 'vue'

const NpcSpawnWorldMap = defineAsyncComponent(
  () => import('~/components/pages/npcs/NpcSpawnWorldMap.client.vue')
)
</script>

<template><NpcSpawnWorldMap /></template>
