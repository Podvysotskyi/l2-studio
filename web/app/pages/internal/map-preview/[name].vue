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
const assetBaseUrl = computed(() => {
  const value = route.query.assetBaseUrl
  if (typeof value !== 'string') return ''
  try {
    const url = new URL(value)
    return url.protocol === 'http:' || url.protocol === 'https:' ? value : ''
  } catch {
    return ''
  }
})
const animationTimeSeconds = computed(() => {
  const value = route.query.animationTimeSeconds
  if (typeof value !== 'string') return undefined
  const seconds = Number(value)
  return Number.isFinite(seconds) && seconds >= 0 ? seconds : undefined
})
</script>

<template>
  <main class="h-[512px] w-[512px] overflow-hidden bg-black">
    <MapPreviewCapture
      :manifest-url="manifestUrl"
      :asset-base-url="assetBaseUrl"
      :animation-time-seconds="animationTimeSeconds"
    />
  </main>
</template>
