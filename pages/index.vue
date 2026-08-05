<script setup lang="ts">
import { computed } from 'vue'
import {
  lookupUrl,
  npcDirectoryUrl,
  type LookupKind,
  type LookupRecord,
  type NpcPage
} from '../lib/studio-content'

const config = useRuntimeConfig()
const loading = ref(true)
const error = ref<string>()
const counts = ref({ npcs: 0, races: 0, sexes: 0, types: 0 })

const sections = computed(() => [
  {
    label: 'NPC definitions',
    value: counts.value.npcs,
    description: 'Authoritative world actors',
    icon: 'i-lucide-users-round',
    to: '/content/npcs',
    color: 'text-primary'
  },
  {
    label: 'Race values',
    value: counts.value.races,
    description: 'Stable reference vocabulary',
    icon: 'i-lucide-orbit',
    to: '/content/races',
    color: 'text-info'
  },
  {
    label: 'Sex values',
    value: counts.value.sexes,
    description: 'Stable reference vocabulary',
    icon: 'i-lucide-tags',
    to: '/content/sexes',
    color: 'text-warning'
  },
  {
    label: 'Behavior types',
    value: counts.value.types,
    description: 'Server behavior categories',
    icon: 'i-lucide-workflow',
    to: '/content/types',
    color: 'text-success'
  }
])

async function fetchLookupCount(kind: LookupKind): Promise<number> {
  const records = await $fetch<LookupRecord[]>(
    lookupUrl(config.public.apiBase, kind)
  )
  return records.length
}

async function loadSummary() {
  loading.value = true
  error.value = undefined
  try {
    const [npcs, races, sexes, types] = await Promise.all([
      $fetch<NpcPage>(
        npcDirectoryUrl(config.public.apiBase, { page: 1, pageSize: 1 })
      ),
      fetchLookupCount('npc-races'),
      fetchLookupCount('npc-sexes'),
      fetchLookupCount('npc-types')
    ])
    counts.value = { npcs: npcs.total, races, sexes, types }
  } catch {
    error.value = 'Studio could not read the game-content catalog.'
  } finally {
    loading.value = false
  }
}

onMounted(loadSummary)
</script>

<template>
  <div class="space-y-8">
    <StudioPageHeader
      eyebrow="Content operations"
      title="Studio overview"
      description="Inspect the authoritative PostgreSQL content model and track the catalogs available to the game server."
      icon="i-lucide-panels-top-left"
    >
      <template #actions>
        <UButton
          label="Refresh data"
          icon="i-lucide-refresh-cw"
          color="neutral"
          variant="outline"
          :loading="loading"
          @click="loadSummary"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      icon="i-lucide-database-zap"
      title="Content API unavailable"
      :description="error"
    >
      <template #actions>
        <UButton color="error" variant="soft" size="sm" @click="loadSummary">
          Try again
        </UButton>
      </template>
    </UAlert>

    <section aria-labelledby="catalog-summary">
      <div class="mb-3 flex items-center justify-between">
        <h2 id="catalog-summary" class="text-sm font-semibold text-highlighted">
          Catalog summary
        </h2>
        <UBadge color="neutral" variant="subtle">PostgreSQL · content</UBadge>
      </div>
      <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <UCard
          v-for="section in sections"
          :key="section.to"
          :ui="{ body: 'flex min-h-40 flex-col gap-5' }"
        >
          <div class="flex items-start justify-between gap-4">
            <div class="grid size-10 place-items-center rounded-lg bg-elevated">
              <UIcon
                :name="section.icon"
                class="size-5"
                :class="section.color"
              />
            </div>
            <USkeleton v-if="loading" class="h-8 w-16" />
            <strong v-else class="text-3xl font-semibold tabular-nums">
              {{ section.value.toLocaleString() }}
            </strong>
          </div>
          <div class="mt-auto">
            <p class="text-sm font-medium text-highlighted">
              {{ section.label }}
            </p>
            <p class="mt-1 text-xs text-muted">{{ section.description }}</p>
          </div>
          <UButton
            :to="section.to"
            label="Open catalog"
            trailing-icon="i-lucide-arrow-right"
            color="neutral"
            variant="ghost"
            size="sm"
            block
          />
        </UCard>
      </div>
    </section>

    <div class="grid gap-4 lg:grid-cols-[minmax(0,2fr)_minmax(18rem,1fr)]">
      <UCard>
        <template #header>
          <div class="flex items-center gap-3">
            <UIcon name="i-lucide-route" class="size-5 text-primary" />
            <div>
              <h2 class="text-sm font-semibold text-highlighted">
                Content flow
              </h2>
              <p class="text-xs text-muted">Current authoring architecture</p>
            </div>
          </div>
        </template>
        <ol class="grid gap-3 sm:grid-cols-3">
          <li class="rounded-lg bg-elevated p-4">
            <span class="text-xs font-semibold text-primary">01 · SOURCE</span>
            <p class="mt-2 text-sm font-medium">Reference datapack</p>
            <p class="mt-1 text-xs leading-5 text-muted">
              XML remains an import source, not a runtime dependency.
            </p>
          </li>
          <li class="rounded-lg bg-elevated p-4">
            <span class="text-xs font-semibold text-primary">02 · AUTHOR</span>
            <p class="mt-2 text-sm font-medium">Content schema</p>
            <p class="mt-1 text-xs leading-5 text-muted">
              Studio reads normalized definitions from PostgreSQL.
            </p>
          </li>
          <li class="rounded-lg bg-elevated p-4">
            <span class="text-xs font-semibold text-primary">03 · SERVE</span>
            <p class="mt-2 text-sm font-medium">Game runtime</p>
            <p class="mt-1 text-xs leading-5 text-muted">
              The game server caches published definitions in memory.
            </p>
          </li>
        </ol>
      </UCard>

      <UCard>
        <template #header>
          <h2 class="text-sm font-semibold text-highlighted">
            Workspace state
          </h2>
        </template>
        <dl class="space-y-4 text-sm">
          <div class="flex items-center justify-between gap-4">
            <dt class="text-muted">Schema</dt>
            <dd><UBadge color="neutral" variant="subtle">content</UBadge></dd>
          </div>
          <div class="flex items-center justify-between gap-4">
            <dt class="text-muted">Access</dt>
            <dd><UBadge color="info" variant="subtle">Read only</UBadge></dd>
          </div>
          <div class="flex items-center justify-between gap-4">
            <dt class="text-muted">Lookup seeds</dt>
            <dd><UBadge color="success" variant="subtle">Enabled</UBadge></dd>
          </div>
          <div class="flex items-center justify-between gap-4">
            <dt class="text-muted">NPC importer</dt>
            <dd><UBadge color="warning" variant="subtle">Planned</UBadge></dd>
          </div>
        </dl>
      </UCard>
    </div>
  </div>
</template>
