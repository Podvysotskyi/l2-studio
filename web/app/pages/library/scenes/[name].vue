<script setup lang="ts">
import { defineAsyncComponent } from 'vue'

const StudioSceneDetails = defineAsyncComponent(
  () => import('~/components/pages/scenes/StudioSceneDetails.vue')
)
</script>

<template><StudioSceneDetails /></template>
