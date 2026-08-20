<script setup lang="ts">
import { defineAsyncComponent } from 'vue'

const StudioMapDetails = defineAsyncComponent(
  () => import('~/components/pages/maps/StudioMapDetails.vue')
)
</script>

<template><StudioMapDetails /></template>
