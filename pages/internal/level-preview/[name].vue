<script setup lang="ts">
import { definePageMeta } from '#imports'
import { computed } from 'vue'
import { useRoute } from 'vue-router'

definePageMeta({ layout: false })

const route = useRoute()
const name = computed(() => String(route.params.name))
const manifestUrl = computed(
  () => `/levels/${encodeURIComponent(name.value)}/manifest.json`
)
</script>

<template>
  <main class="h-[512px] w-[512px] overflow-hidden bg-black">
    <LevelPreviewCapture :manifest-url="manifestUrl" />
  </main>
</template>
