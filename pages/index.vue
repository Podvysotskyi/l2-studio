<script setup lang="ts">
import { serviceLabels, type ServiceState } from '@l2/ui'
import { computed } from 'vue'
import { systemInfoUrl, type SystemInfo } from '../lib/system-info'

const config = useRuntimeConfig()
const state = ref<ServiceState>('connecting')
const info = ref<SystemInfo>()
const error = ref<string>()
const statusColor = computed<'success' | 'error' | 'neutral'>(() =>
  state.value === 'connected'
    ? 'success'
    : state.value === 'error'
      ? 'error'
      : 'neutral'
)

onMounted(async () => {
  try {
    info.value = await $fetch<SystemInfo>(systemInfoUrl(config.public.apiBase))
    state.value = 'connected'
  } catch {
    state.value = 'error'
    error.value = 'Could not connect to the Studio API.'
  }
})
</script>

<template>
  <main class="workspace-shell">
    <header>
      <div>
        <p class="eyebrow">L2 Content Pipeline</p>
        <h1>Studio</h1>
      </div>
      <UCard
        variant="subtle"
        class="min-w-72"
        :ui="{ body: 'flex items-center gap-3 p-4' }"
      >
        <UBadge :color="statusColor" variant="subtle">
          Studio API: {{ serviceLabels[state] }}
        </UBadge>
        <div class="grid gap-1 text-xs text-muted">
          <span v-if="info"
            >{{ info.service }} · {{ info.buildVersion }} ·
            {{ info.environment }}</span
          >
          <span v-else>{{ error ?? config.public.apiBase }}</span>
        </div>
      </UCard>
    </header>
    <UCard
      variant="subtle"
      class="mt-20 min-h-[55vh]"
      :ui="{
        body: 'grid min-h-[55vh] place-content-center justify-items-center p-12 text-center'
      }"
    >
      <UBadge color="primary" variant="soft">01 · Foundation</UBadge>
      <h2 class="mt-4 text-3xl font-medium">Asset workspace prepared</h2>
      <p class="mt-3 max-w-xl text-muted">
        Import, conversion, preview, validation, and publishing arrive in the
        next milestones.
      </p>
    </UCard>
  </main>
</template>
