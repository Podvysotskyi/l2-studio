<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useSystemStore } from '../stores/system'
import { studioRouteTitle } from '../utils/studio-navigation'

const route = useRoute()
const systemStore = useSystemStore()
const { serviceState } = storeToRefs(systemStore)

const routeTitle = computed(() => studioRouteTitle(route.path))
const statusColor = computed<'success' | 'error' | 'neutral'>(() =>
  serviceState.value === 'connected'
    ? 'success'
    : serviceState.value === 'error'
      ? 'error'
      : 'neutral'
)

onMounted(() => void systemStore.load().catch(() => undefined))
</script>

<template>
  <UDashboardGroup unit="rem" class="min-h-screen">
    <StudioSidebar />

    <UDashboardPanel id="studio-panel">
      <template #header>
        <UDashboardNavbar :title="routeTitle" icon="i-lucide-database">
          <template #right>
            <UBadge
              :color="statusColor"
              variant="subtle"
              class="hidden sm:flex"
            >
              API {{ serviceState }}
            </UBadge>
            <UColorModeButton color="neutral" variant="ghost" />
          </template>
        </UDashboardNavbar>
      </template>

      <template #body>
        <div class="studio-page">
          <slot />
        </div>
      </template>
    </UDashboardPanel>
  </UDashboardGroup>
</template>
