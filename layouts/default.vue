<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui'
import type { ServiceState } from '@l2/ui'
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { systemInfoUrl, type SystemInfo } from '../lib/system-info'

const route = useRoute()
const config = useRuntimeConfig()
const serviceState = ref<ServiceState>('connecting')
const systemInfo = ref<SystemInfo>()

const navigation: NavigationMenuItem[] = [
  {
    label: 'Overview',
    type: 'label'
  },
  {
    label: 'Dashboard',
    icon: 'i-lucide-layout-dashboard',
    to: '/',
    exact: true
  },
  {
    label: 'Content',
    type: 'label'
  },
  {
    label: 'NPCs',
    icon: 'i-lucide-users-round',
    defaultOpen: true,
    children: [
      { label: 'Definitions', icon: 'i-lucide-list', to: '/content/npcs' },
      { label: 'Races', icon: 'i-lucide-orbit', to: '/content/races' },
      { label: 'Sexes', icon: 'i-lucide-tags', to: '/content/sexes' },
      { label: 'Types', icon: 'i-lucide-workflow', to: '/content/types' }
    ]
  },
  {
    label: 'Skills',
    icon: 'i-lucide-sparkles',
    defaultOpen: true,
    children: [
      { label: 'Definitions', icon: 'i-lucide-list', to: '/content/skills' },
      {
        label: 'Operate types',
        icon: 'i-lucide-play',
        to: '/content/skill-operate-types'
      },
      {
        label: 'Target types',
        icon: 'i-lucide-crosshair',
        to: '/content/skill-target-types'
      }
    ]
  },
  {
    label: 'Assets',
    type: 'label'
  },
  {
    label: 'Asset library',
    icon: 'i-lucide-package-open',
    defaultOpen: true,
    children: [
      {
        label: 'System textures',
        icon: 'i-lucide-panels-top-left',
        to: '/assets/systextures'
      },
      {
        label: 'World textures',
        icon: 'i-lucide-mountain',
        to: '/assets/textures'
      },
      {
        label: 'Music',
        icon: 'i-lucide-music-2',
        to: '/assets/music'
      },
      {
        label: 'Static meshes',
        icon: 'i-lucide-box',
        to: '/assets/staticmeshes'
      },
      {
        label: 'Levels',
        icon: 'i-lucide-map',
        to: '/assets/levels'
      },
      {
        label: 'Scenes',
        icon: 'i-lucide-clapperboard',
        to: '/assets/scenes'
      }
    ]
  },
  {
    label: 'Operations',
    type: 'label'
  },
  {
    label: 'Background jobs',
    icon: 'i-lucide-activity',
    defaultOpen: true,
    children: [
      {
        label: 'Import jobs',
        icon: 'i-lucide-history',
        to: '/assets/jobs'
      }
    ]
  }
]

const routeTitle = computed(() => {
  if (route.path === '/') return 'Dashboard'
  if (route.path === '/content/npcs') return 'NPC definitions'
  if (route.path === '/content/races') return 'NPC races'
  if (route.path === '/content/sexes') return 'NPC sexes'
  if (route.path === '/content/types') return 'NPC types'
  if (route.path === '/content/skills') return 'Skill definitions'
  if (route.path === '/content/skill-operate-types')
    return 'Skill operate types'
  if (route.path === '/content/skill-target-types') return 'Skill target types'
  if (route.path === '/assets/imports') return 'System texture imports'
  if (route.path === '/assets/systextures') return 'System texture imports'
  if (route.path === '/assets/textures') return 'World texture imports'
  if (route.path === '/assets/music') return 'Music assets'
  if (route.path === '/assets/staticmeshes') return 'Static meshes'
  if (route.path === '/assets/levels') return 'Levels'
  if (route.path.startsWith('/assets/levels/')) return 'Level map'
  if (route.path === '/assets/scenes') return 'Scenes'
  if (route.path.startsWith('/assets/scenes/')) return 'Client scene'
  if (route.path === '/assets/jobs') return 'Asset import jobs'
  return 'Studio'
})
const statusColor = computed<'success' | 'error' | 'neutral'>(() =>
  serviceState.value === 'connected'
    ? 'success'
    : serviceState.value === 'error'
      ? 'error'
      : 'neutral'
)

onMounted(async () => {
  try {
    systemInfo.value = await $fetch<SystemInfo>(
      systemInfoUrl(config.public.apiBase)
    )
    serviceState.value = 'connected'
  } catch {
    serviceState.value = 'error'
  }
})
</script>

<template>
  <UDashboardGroup unit="rem" class="min-h-screen">
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
              Content operations
            </small>
          </span>
        </NuxtLink>
      </template>

      <template #default="{ collapsed }">
        <UNavigationMenu
          :items="navigation"
          orientation="vertical"
          :collapsed="collapsed"
          :tooltip="collapsed"
          :popover="collapsed"
          highlight
          class="w-full"
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
                {{
                  systemInfo
                    ? `${systemInfo.environment} · ${systemInfo.buildVersion}`
                    : config.public.apiBase
                }}
              </span>
            </span>
          </div>
        </UTooltip>
      </template>
    </UDashboardSidebar>

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
