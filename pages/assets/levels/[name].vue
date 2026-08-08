<script setup lang="ts">
import type {
  LevelActorManifestEntry,
  LevelCatalogEntry,
  LevelCatalogManifest,
  LevelManifest
} from '@l2/ui'
import { levelCatalogManifestUrl } from '@l2/ui'
import { computed, nextTick, watch } from 'vue'
import { useRoute } from 'vue-router'
import { filterLevelActors } from '../../../lib/level-map'
import { paginate } from '../../../lib/studio-content'

interface LevelPreviewApi {
  focusActor(name: string): void
}

const route = useRoute()
const catalogEntry = ref<LevelCatalogEntry>()
const manifest = ref<LevelManifest>()
const preview = ref<LevelPreviewApi>()
const selectedActorName = ref<string>()
const query = ref('')
const page = ref(1)
const pageSize = ref(50)
const loading = ref(true)
const error = ref<string>()
const previewError = ref<string>()

const routeName = computed(() =>
  Array.isArray(route.params.name)
    ? (route.params.name[0] ?? '')
    : (route.params.name ?? '')
)
const unresolvedActors = computed(
  () => manifest.value?.actors.filter((actor) => !actor.meshUrl).length ?? 0
)
const filteredActors = computed(() =>
  filterLevelActors(manifest.value?.actors ?? [], query.value)
)
const visibleActors = computed(() =>
  paginate(filteredActors.value, page.value, pageSize.value)
)

watch([query, pageSize], () => (page.value = 1))
watch(routeName, () => void loadLevel(), { immediate: true })

function selectActor(actor: LevelActorManifestEntry) {
  selectedActorName.value = actor.name
}

async function focusActor(actor: LevelActorManifestEntry) {
  if (!actor.meshUrl) return
  selectActor(actor)
  await nextTick()
  preview.value?.focusActor(actor.name)
}

async function loadLevel() {
  loading.value = true
  error.value = undefined
  previewError.value = undefined
  catalogEntry.value = undefined
  manifest.value = undefined
  selectedActorName.value = undefined

  try {
    const catalog = await $fetch<LevelCatalogManifest>(
      levelCatalogManifestUrl(),
      { query: { refresh: Date.now() } }
    )
    const entry = catalog.levels.find((level) => level.name === routeName.value)
    if (!entry) {
      error.value =
        'Map “' +
        routeName.value +
        '” is not present in the generated level catalog.'
      return
    }
    catalogEntry.value = entry
    if (!entry.manifestUrl) {
      error.value = entry.error ?? 'Map “' + entry.name + '” was not imported.'
      return
    }
    manifest.value = await $fetch<LevelManifest>(entry.manifestUrl)
  } catch {
    error.value = 'Map “' + routeName.value + '” could not be loaded.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Level map"
      :title="catalogEntry?.name ?? routeName"
      description="Inspect the reconstructed map and its placed static-mesh instances."
      icon="i-lucide-map-pinned"
    >
      <template #actions>
        <UButton
          label="All levels"
          icon="i-lucide-arrow-left"
          color="neutral"
          variant="outline"
          to="/assets/levels"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      title="Map unavailable"
      :description="error"
    >
      <template #actions>
        <UButton color="error" variant="soft" size="sm" @click="loadLevel">
          Try again
        </UButton>
      </template>
    </UAlert>

    <div v-if="loading" class="grid min-h-64 place-items-center">
      <div class="flex items-center gap-3 text-sm text-muted">
        <UIcon name="i-lucide-loader-circle" class="size-5 animate-spin" />
        Loading map…
      </div>
    </div>

    <template v-else-if="manifest">
      <div class="grid gap-3 sm:grid-cols-4">
        <UCard>
          <p class="text-xs text-muted">Terrains</p>
          <p class="text-2xl font-semibold">{{ manifest.terrains.length }}</p>
        </UCard>
        <UCard>
          <p class="text-xs text-muted">Placed meshes</p>
          <p class="text-2xl font-semibold">
            {{ manifest.actors.length.toLocaleString() }}
          </p>
        </UCard>
        <UCard>
          <p class="text-xs text-muted">Lights</p>
          <p class="text-2xl font-semibold">{{ manifest.lights.length }}</p>
        </UCard>
        <UCard>
          <p class="text-xs text-muted">Unresolved</p>
          <p class="text-2xl font-semibold">
            {{ unresolvedActors.toLocaleString() }}
          </p>
        </UCard>
      </div>

      <UAlert
        v-if="previewError"
        color="error"
        variant="subtle"
        title="Preview unavailable"
        :description="previewError"
      />

      <div
        class="grid items-start gap-4 xl:grid-cols-[minmax(0,2fr)_minmax(24rem,1fr)]"
      >
        <UCard :ui="{ body: 'p-2 sm:p-2' }">
          <StudioLevelPreview
            ref="preview"
            :manifest="manifest"
            :selected-actor-name="selectedActorName"
            @error="previewError = $event"
          />
          <p class="mt-2 text-center text-xs text-muted">
            Drag to orbit · scroll to zoom toward the pointer · right-drag to
            pan · double-click the preview to restore the framed view
          </p>
        </UCard>

        <UCard class="xl:sticky xl:top-4" :ui="{ body: 'p-0 sm:p-0' }">
          <template #header>
            <div class="space-y-3">
              <div>
                <h2 class="text-sm font-semibold text-highlighted">
                  Placed mesh instances
                </h2>
                <p class="text-xs text-muted">
                  {{ filteredActors.length.toLocaleString() }} of
                  {{ manifest.actors.length.toLocaleString() }} instances
                </p>
              </div>
              <UInput
                v-model="query"
                icon="i-lucide-search"
                placeholder="Search actors or meshes"
                aria-label="Search placed mesh instances"
                class="w-full"
              />
            </div>
          </template>

          <div class="max-h-[62vh] divide-y divide-default overflow-y-auto">
            <div
              v-for="actor in visibleActors"
              :key="actor.name"
              class="flex items-center gap-2 p-2"
              :class="selectedActorName === actor.name ? 'bg-primary/10' : ''"
            >
              <button
                type="button"
                class="min-w-0 flex-1 rounded-md p-2 text-left hover:bg-elevated focus-visible:outline-2 focus-visible:outline-primary"
                @click="selectActor(actor)"
                @dblclick="focusActor(actor)"
              >
                <span class="flex items-center gap-2">
                  <span class="truncate text-sm font-medium text-highlighted">{{
                    actor.name
                  }}</span>
                  <UBadge
                    :color="actor.meshUrl ? 'success' : 'warning'"
                    variant="subtle"
                    size="sm"
                  >
                    {{ actor.meshUrl ? 'resolved' : 'unresolved' }}
                  </UBadge>
                </span>
                <span class="mt-1 block truncate text-xs text-muted">
                  {{ actor.meshPackage ?? 'Unknown package' }}.{{
                    actor.meshObject ?? 'Unknown mesh'
                  }}
                </span>
                <span class="mt-1 block truncate text-xs text-dimmed">
                  {{ actor.className }} · X {{ actor.location.x.toFixed(0) }} ·
                  Y {{ actor.location.y.toFixed(0) }} · Z
                  {{ actor.location.z.toFixed(0) }}
                </span>
              </button>
              <UButton
                icon="i-lucide-focus"
                color="neutral"
                variant="ghost"
                size="sm"
                :disabled="!actor.meshUrl"
                :aria-label="'Focus ' + actor.name"
                @click="focusActor(actor)"
              />
            </div>
            <div
              v-if="visibleActors.length === 0"
              class="grid min-h-48 place-items-center p-8 text-center text-sm text-muted"
            >
              No placed meshes match this search.
            </div>
          </div>
          <StudioTableFooter
            v-model:page="page"
            v-model:page-size="pageSize"
            :total="filteredActors.length"
            :page-size-options="[50, 100, 200]"
          />
        </UCard>
      </div>
    </template>
  </div>
</template>
