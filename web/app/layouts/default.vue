<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useSystemStore } from '../stores/system'
import { useGameVersionStore } from '../stores/game-version'
import { studioRouteTitle } from '../utils/studio-navigation'

const route = useRoute()
const systemStore = useSystemStore()
const gameVersionStore = useGameVersionStore()
const { serviceState } = storeToRefs(systemStore)

const routeTitle = computed(() => studioRouteTitle(route.path))
const statusColor = computed<'success' | 'error' | 'neutral'>(() =>
  serviceState.value === 'connected'
    ? 'success'
    : serviceState.value === 'error'
      ? 'error'
      : 'neutral'
)

onMounted(() => {
  void systemStore.load().catch(() => undefined)
  void gameVersionStore.load().catch(() => undefined)
})

function retryGameVersions() {
  void gameVersionStore.load().catch(() => undefined)
}

function reloadPage() {
  window.location.reload()
}
</script>

<template>
  <div
    v-if="gameVersionStore.error"
    class="flex min-h-screen items-center justify-center p-6"
  >
    <UCard class="w-full max-w-md">
      <div class="space-y-4 text-center">
        <UIcon name="i-lucide-circle-alert" class="mx-auto size-8 text-error" />
        <div class="space-y-1">
          <h1 class="text-lg font-semibold text-highlighted">Game versions unavailable</h1>
          <p class="text-sm text-muted">
            Studio could not load the available game versions. Check the connection and try again.
          </p>
        </div>
        <div class="flex justify-center gap-3">
          <UButton icon="i-lucide-refresh-cw" @click="retryGameVersions">
            Retry
          </UButton>
          <UButton color="neutral" variant="outline" @click="reloadPage">
            Reload
          </UButton>
        </div>
      </div>
    </UCard>
  </div>

  <UDashboardGroup v-else unit="rem" class="min-h-screen">
    <StudioSidebar />

    <UDashboardPanel id="studio-panel">
      <template #header>
        <UDashboardNavbar :title="routeTitle" icon="i-lucide-database">
          <template #right>
            <USelect
              v-if="!gameVersionStore.loading"
              :model-value="gameVersionStore.selected"
              :items="gameVersionStore.options"
              aria-label="Game version"
              class="w-40"
              @update:model-value="value => gameVersionStore.select(value as string)"
            />
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
