<script setup lang="ts">
import type { AssetImportKind } from '~/types/studio'
import type { AssetImportJob } from '../../../types/models/asset-import-job'
import { storeToRefs } from 'pinia'
import { computed } from 'vue'
import { useDashboardStore } from '../../../stores/dashboard'

const dashboard = useDashboardStore()
const { loading, contentError, assetError, jobsError, counts, assets, jobs } =
  storeToRefs(dashboard)

const totalDefinitions = computed(
  () => counts.value.npcs + counts.value.skills + counts.value.playerClasses
)
const totalLookups = computed(
  () =>
    counts.value.npcRaces +
    counts.value.npcSexes +
    counts.value.npcTypes +
    counts.value.playerRaces +
    counts.value.playerSexes +
    counts.value.skillOperateTypes +
    counts.value.skillTargetTypes
)
const totalAssets = computed(() =>
  assets.value.reduce((total, asset) => total + asset.resolved, 0)
)
const skippedAssets = computed(() =>
  assets.value.reduce((total, asset) => total + asset.skipped, 0)
)
const activeJobs = computed(() =>
  jobs.value.filter(
    (job) => ['queued', 'discovering', 'running'].includes(job.status)
  )
)
const recentJobs = computed(() => jobs.value.slice(0, 6))
const latestFinishedJob = computed(() =>
  jobs.value.find((job) => job.finishedAt !== null)
)

const headlineStats = computed(() => [
  {
    label: 'Content definitions',
    value: totalDefinitions.value,
    detail: `${counts.value.npcs.toLocaleString()} NPCs · ${counts.value.skills.toLocaleString()} skills · ${counts.value.playerClasses.toLocaleString()} classes`,
    icon: 'i-lucide-database',
    color: 'text-primary'
  },
  {
    label: 'Lookup values',
    value: totalLookups.value,
    detail: 'Seven normalized vocabularies',
    icon: 'i-lucide-tags',
    color: 'text-info'
  },
  {
    label: 'Generated assets',
    value: totalAssets.value,
    detail: `${assets.value.filter((asset) => asset.available).length} collections available`,
    icon: 'i-lucide-package-open',
    color: 'text-success'
  },
  {
    label: 'Needs attention',
    value: skippedAssets.value,
    detail: activeJobs.value.length
      ? `${activeJobs.value.length} imports currently active`
      : 'Skipped generated assets',
    icon: 'i-lucide-circle-alert',
    color: skippedAssets.value ? 'text-warning' : 'text-success'
  }
])

const contentCatalogs = computed(() => [
  {
    label: 'NPC definitions',
    value: counts.value.npcs,
    description: 'World actors with race, sex, and behavior classifications.',
    icon: 'i-lucide-users-round',
    to: '/authoring/npcs'
  },
  {
    label: 'Skill definitions',
    value: counts.value.skills,
    description: 'Skills with levels, icons, operate types, and target types.',
    icon: 'i-lucide-sparkles',
    to: '/authoring/skills'
  },
  {
    label: 'Player classes',
    value: counts.value.playerClasses,
    description:
      'Expandable progression from base professions to third classes.',
    icon: 'i-lucide-git-branch',
    to: '/authoring/players/classes'
  }
])

const lookupCatalogs = computed(() => [
  {
    label: 'Player races',
    value: counts.value.playerRaces,
    to: '/authoring/players/races'
  },
  {
    label: 'Player sexes',
    value: counts.value.playerSexes,
    to: '/authoring/players/sexes'
  },
  { label: 'NPC races', value: counts.value.npcRaces, to: '/authoring/npcs/races' },
  { label: 'NPC sexes', value: counts.value.npcSexes, to: '/authoring/npcs/sexes' },
  { label: 'NPC types', value: counts.value.npcTypes, to: '/authoring/npcs/types' },
  {
    label: 'Skill operate types',
    value: counts.value.skillOperateTypes,
    to: '/authoring/skills/operate-types'
  },
  {
    label: 'Skill target types',
    value: counts.value.skillTargetTypes,
    to: '/authoring/skills/target-types'
  }
])

function kindLabel(kind: AssetImportKind) {
  return assets.value.find((asset) => asset.kind === kind)?.label ?? kind
}

function statusColor(status: AssetImportJob['status']) {
  if (status === 'succeeded') return 'success'
  if (status === 'succeeded_with_warnings') return 'warning'
  if (status === 'failed') return 'error'
  return 'info'
}

function formatDate(value: string | null) {
  return value ? new Date(value).toLocaleString() : 'Pending'
}

onMounted(() => void dashboard.load())
</script>

<template>
  <div class="space-y-8">
    <StudioPageHeader
      eyebrow="Content operations"
      title="Studio overview"
      description="A live summary of authoritative content, generated game assets, and the pipelines that maintain them."
      icon="i-lucide-panels-top-left"
    >
      <template #actions>
        <UButton
          label="Import jobs"
          icon="i-lucide-history"
          color="neutral"
          variant="outline"
          to="/pipeline/imports"
        />
        <UButton
          label="Refresh data"
          icon="i-lucide-refresh-cw"
          color="neutral"
          variant="outline"
          :loading="loading"
          @click="dashboard.load"
        />
      </template>
    </StudioPageHeader>

    <div v-if="contentError || assetError || jobsError" class="space-y-3">
      <UAlert
        v-if="contentError"
        color="error"
        variant="subtle"
        icon="i-lucide-database-zap"
        title="Content summary unavailable"
        description="Studio could not read the PostgreSQL content catalogs. Asset summaries remain available below."
      />
      <UAlert
        v-if="assetError"
        color="warning"
        variant="subtle"
        icon="i-lucide-package-x"
        title="Some asset collections are unavailable"
        description="Collections without an active database catalog are shown as not imported; available collections still report their current inventory."
      />
      <UAlert
        v-if="jobsError"
        color="warning"
        variant="subtle"
        icon="i-lucide-history"
        title="Import activity unavailable"
        description="Recent import jobs could not be read from the Studio API."
      />
    </div>

    <section aria-labelledby="workspace-summary">
      <div class="mb-3 flex items-center justify-between">
        <h2
          id="workspace-summary"
          class="text-sm font-semibold text-highlighted"
        >
          Workspace summary
        </h2>
        <UBadge color="neutral" variant="subtle">Live inventory</UBadge>
      </div>
      <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <UCard
          v-for="stat in headlineStats"
          :key="stat.label"
          :ui="{ body: 'flex min-h-36 flex-col gap-4' }"
        >
          <div class="flex items-start justify-between gap-4">
            <div class="grid size-10 place-items-center rounded-lg bg-elevated">
              <UIcon :name="stat.icon" class="size-5" :class="stat.color" />
            </div>
            <USkeleton v-if="loading" class="h-8 w-20" />
            <strong v-else class="text-3xl font-semibold tabular-nums">
              {{ stat.value.toLocaleString() }}
            </strong>
          </div>
          <div class="mt-auto">
            <p class="text-sm font-medium text-highlighted">{{ stat.label }}</p>
            <p class="mt-1 text-xs text-muted">{{ stat.detail }}</p>
          </div>
        </UCard>
      </div>
    </section>

    <section aria-labelledby="content-summary">
      <div class="mb-3 flex flex-wrap items-center justify-between gap-2">
        <div>
          <h2
            id="content-summary"
            class="text-sm font-semibold text-highlighted"
          >
            Content catalogs
          </h2>
          <p class="mt-1 text-xs text-muted">
            Authoritative definitions and normalized server vocabularies.
          </p>
        </div>
        <UBadge color="neutral" variant="subtle">PostgreSQL · content</UBadge>
      </div>
      <div class="grid gap-4 xl:grid-cols-4">
        <UCard
          v-for="catalog in contentCatalogs"
          :key="catalog.to"
          :ui="{ body: 'flex h-full flex-col gap-4' }"
        >
          <div class="flex items-start justify-between gap-4">
            <div
              class="grid size-11 place-items-center rounded-lg bg-primary/10 text-primary"
            >
              <UIcon :name="catalog.icon" class="size-5" />
            </div>
            <strong class="text-3xl font-semibold tabular-nums">{{
              catalog.value.toLocaleString()
            }}</strong>
          </div>
          <div>
            <h3 class="text-sm font-medium text-highlighted">
              {{ catalog.label }}
            </h3>
            <p class="mt-1 text-xs leading-5 text-muted">
              {{ catalog.description }}
            </p>
          </div>
          <UButton
            class="mt-auto"
            :to="catalog.to"
            label="Open catalog"
            trailing-icon="i-lucide-arrow-right"
            color="neutral"
            variant="ghost"
            size="sm"
            block
          />
        </UCard>

        <UCard :ui="{ body: 'p-0 sm:p-0' }">
          <template #header>
            <div class="flex items-center gap-3">
              <UIcon name="i-lucide-tags" class="size-5 text-info" />
              <div>
                <h3 class="text-sm font-semibold text-highlighted">
                  Lookup vocabularies
                </h3>
                <p class="text-xs text-muted">
                  {{ totalLookups }} stable values
                </p>
              </div>
            </div>
          </template>
          <div class="divide-y divide-default">
            <NuxtLink
              v-for="lookup in lookupCatalogs"
              :key="lookup.to"
              :to="lookup.to"
              class="flex items-center justify-between gap-4 px-4 py-3 text-sm hover:bg-elevated sm:px-6"
            >
              <span class="text-muted">{{ lookup.label }}</span>
              <span class="flex items-center gap-2"
                ><strong class="tabular-nums text-highlighted">{{
                  lookup.value
                }}</strong
                ><UIcon
                  name="i-lucide-chevron-right"
                  class="size-4 text-dimmed"
              /></span>
            </NuxtLink>
          </div>
        </UCard>
      </div>
    </section>

    <section aria-labelledby="asset-summary">
      <div class="mb-3 flex flex-wrap items-center justify-between gap-2">
        <div>
          <h2 id="asset-summary" class="text-sm font-semibold text-highlighted">
            Asset library
          </h2>
          <p class="mt-1 text-xs text-muted">
            Locally generated, browser-ready assets grouped by source
            collection.
          </p>
        </div>
        <UBadge color="neutral" variant="subtle">
          Generated · Git ignored
        </UBadge>
      </div>
      <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <UCard
          v-for="asset in assets"
          :key="asset.kind"
          :ui="{ body: 'flex h-full flex-col gap-4' }"
        >
          <div class="flex items-start justify-between gap-4">
            <div
              class="grid size-11 place-items-center rounded-lg bg-elevated text-primary"
            >
              <UIcon :name="asset.icon" class="size-5" />
            </div>
            <UBadge
              :color="
                asset.available
                  ? asset.skipped
                    ? 'warning'
                    : 'success'
                  : 'neutral'
              "
              variant="subtle"
            >
              {{
                asset.available
                  ? asset.skipped
                    ? `${asset.skipped} skipped`
                    : 'Ready'
                  : 'Not imported'
              }}
            </UBadge>
          </div>
          <div>
            <strong class="text-3xl font-semibold tabular-nums">{{
              asset.resolved.toLocaleString()
            }}</strong>
            <h3 class="mt-2 text-sm font-medium text-highlighted">
              {{ asset.label }}
            </h3>
            <p class="mt-1 text-xs leading-5 text-muted">
              {{ asset.description }}
            </p>
          </div>
          <div class="flex items-center gap-3 text-xs text-muted">
            <span>{{ asset.total.toLocaleString() }} inventoried</span
            ><span aria-hidden="true">·</span
            ><span
              >{{ asset.groups?.toLocaleString() ?? '—' }}
              {{ asset.groupLabel }}</span
            >
          </div>
          <UButton
            class="mt-auto"
            :to="asset.to"
            label="Manage assets"
            trailing-icon="i-lucide-arrow-right"
            color="neutral"
            variant="ghost"
            size="sm"
            block
          />
        </UCard>
      </div>
    </section>

    <section
      class="grid gap-4 lg:grid-cols-[minmax(0,2fr)_minmax(18rem,1fr)]"
      aria-labelledby="import-activity"
    >
      <UCard :ui="{ body: 'p-0 sm:p-0' }">
        <template #header>
          <div class="flex items-center justify-between gap-4">
            <div class="flex items-center gap-3">
              <UIcon name="i-lucide-activity" class="size-5 text-primary" />
              <div>
                <h2
                  id="import-activity"
                  class="text-sm font-semibold text-highlighted"
                >
                  Recent import activity
                </h2>
                <p class="text-xs text-muted">
                  Latest jobs across every asset collection
                </p>
              </div>
            </div>
            <UButton
              label="View all"
              to="/pipeline/imports"
              color="neutral"
              variant="ghost"
              size="sm"
              trailing-icon="i-lucide-arrow-right"
            />
          </div>
        </template>
        <div v-if="recentJobs.length" class="divide-y divide-default">
          <div
            v-for="job in recentJobs"
            :key="job.id"
            class="flex flex-wrap items-center gap-3 px-4 py-3 sm:px-6"
          >
            <UBadge color="neutral" variant="subtle">
              {{ kindLabel(job.kind) }}
            </UBadge>
            <UBadge :color="statusColor(job.status)" variant="subtle">
              {{ job.status.replaceAll('_', ' ') }}
            </UBadge>
            <span class="min-w-32 flex-1 text-xs text-muted"
              >{{ job.completedFileCount.toLocaleString() }} /
              {{ job.discoveredFileCount.toLocaleString() }} completed</span
            >
            <time
              class="text-xs text-dimmed"
              :datetime="job.finishedAt ?? job.requestedAt"
              >{{ formatDate(job.finishedAt ?? job.requestedAt) }}</time
            >
          </div>
        </div>
        <div
          v-else
          class="grid min-h-48 place-items-center p-8 text-sm text-muted"
        >
          {{
            loading
              ? 'Loading import activity…'
              : 'No asset imports have been recorded.'
          }}
        </div>
      </UCard>

      <UCard>
        <template #header>
          <h2 class="text-sm font-semibold text-highlighted">
            Pipeline health
          </h2>
        </template>
        <dl class="space-y-4 text-sm">
          <div class="flex items-center justify-between gap-4">
            <dt class="text-muted">Active imports</dt>
            <dd>
              <UBadge
                :color="activeJobs.length ? 'info' : 'success'"
                variant="subtle"
              >
                {{ activeJobs.length || 'None' }}
              </UBadge>
            </dd>
          </div>
          <div class="flex items-center justify-between gap-4">
            <dt class="text-muted">Available collections</dt>
            <dd class="font-medium tabular-nums text-highlighted">
              {{ assets.filter((asset) => asset.available).length }} /
              {{ assets.length }}
            </dd>
          </div>
          <div class="flex items-center justify-between gap-4">
            <dt class="text-muted">Skipped assets</dt>
            <dd>
              <UBadge
                :color="skippedAssets ? 'warning' : 'success'"
                variant="subtle"
              >
                {{ skippedAssets.toLocaleString() }}
              </UBadge>
            </dd>
          </div>
          <div class="flex items-start justify-between gap-4">
            <dt class="text-muted">Last completed</dt>
            <dd class="max-w-44 text-right text-xs text-highlighted">
              {{
                latestFinishedJob
                  ? `${kindLabel(latestFinishedJob.kind)} · ${formatDate(latestFinishedJob.finishedAt)}`
                  : 'No completed imports'
              }}
            </dd>
          </div>
        </dl>
      </UCard>
    </section>
  </div>
</template>
