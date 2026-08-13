<script setup lang="ts">
import { definePageMeta } from '#imports'
import { computed } from 'vue'
import { useRoute } from 'vue-router'

definePageMeta({ layout: false })

const route = useRoute()
const manifestUrl = computed(() => {
  const value = route.query.manifestUrl
  return typeof value === 'string' && value.startsWith('/versions/')
    ? value
    : ''
})
</script>

<template>
  <main class="h-[512px] w-[512px] overflow-hidden bg-black">
    <MapPreviewCapture :manifest-url="manifestUrl" />
  </main>
</template>
