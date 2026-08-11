<script setup lang="ts">
import { useHead } from '#imports'
import faviconUrl from '~/assets/favicon.ico'

useHead({
  link: [
    {
      rel: 'icon',
      href: faviconUrl,
      type: 'image/x-icon',
      sizes: '16x16 32x32'
    }
  ]
})
</script>

<template>
  <UApp>
    <NuxtLayout>
      <NuxtPage />
    </NuxtLayout>
  </UApp>
</template>
