<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useSystemStore } from '../../stores/system'
import {
  studioNavigation,
  withStudioRouteGroup
} from '../../utils/studio-navigation'

const route = useRoute()
const systemStore = useSystemStore()
const { serviceState, description } = storeToRefs(systemStore)
const expandedGroups = ref(withStudioRouteGroup([], route.path))

watch(
  () => route.path,
  path => {
    expandedGroups.value = withStudioRouteGroup(expandedGroups.value, path)
  }
)
</script>

<template>
  <UDashboardSidebar
    id="studio-sidebar"
    collapsible
    resizable
    :default-size="17"
    :min-size="15"
    :max-size="22"
    :collapsed-size="4"
    :ui="{ footer: 'border-t border-default' }"
  >
    <template #header="{ collapsed }">
      <NuxtLink to="/" class="flex min-w-0 items-center gap-3">
        <span
          class="grid size-9 shrink-0 place-items-center rounded-lg bg-primary text-sm font-black text-inverted shadow-sm shadow-primary/30"
        >
          L2
        </span>
        <span v-if="!collapsed" class="min-w-0">
          <strong class="block truncate text-sm text-highlighted">
            Studio
          </strong>
          <small class="block truncate text-xs text-muted">
            Content workspace
          </small>
        </span>
      </NuxtLink>
    </template>

    <template #default="{ collapsed }">
      <UNavigationMenu
        v-model="expandedGroups"
        :items="studioNavigation"
        orientation="vertical"
        type="multiple"
        :collapsed="collapsed"
        :tooltip="collapsed"
        :popover="collapsed"
        highlight
        class="w-full"
        :ui="{
          label: 'px-2 pt-4 text-[11px] font-semibold uppercase tracking-wider text-dimmed',
          link: 'rounded-md'
        }"
      />
    </template>

    <template #footer="{ collapsed }">
      <UTooltip
        :text="`Studio API: ${serviceState}`"
        :disabled="!collapsed"
        :content="{ side: 'right' }"
      >
        <div
          class="flex min-w-0 items-center gap-3 rounded-lg px-2 py-1.5"
          :class="collapsed ? 'justify-center' : ''"
        >
          <span class="relative flex size-2.5 shrink-0">
            <span
              v-if="serviceState === 'connecting'"
              class="absolute inline-flex size-full animate-ping rounded-full bg-warning opacity-50"
            />
            <span
              class="relative inline-flex size-2.5 rounded-full"
              :class="{
                'bg-success': serviceState === 'connected',
                'bg-error': serviceState === 'error',
                'bg-warning': serviceState === 'connecting'
              }"
            />
          </span>
          <span v-if="!collapsed" class="min-w-0">
            <span class="block text-xs font-medium text-highlighted">
              Studio API
            </span>
            <span class="block truncate text-xs text-muted">
              {{ description }}
            </span>
          </span>
        </div>
      </UTooltip>
    </template>
  </UDashboardSidebar>
</template>
