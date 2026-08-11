<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { onBeforeUnmount, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  npcDirectoryUrl,
  positiveInteger,
  type NpcPage,
  type NpcRecord
} from '../../utils/studio-content'

const apiBase = ''
const route = useRoute()
const router = useRouter()
const query = ref(
  typeof route.query.query === 'string' ? route.query.query : ''
)
const page = ref(positiveInteger(route.query.page, 1))
const pageSize = ref(positiveInteger(route.query.pageSize, 25))
const result = ref<NpcPage>()
const loading = ref(true)
const error = ref<string>()
let searchTimer: ReturnType<typeof setTimeout> | undefined
let requestVersion = 0

const columns: TableColumn<NpcRecord>[] = [
  { accessorKey: 'id', header: 'ID' },
  { accessorKey: 'name', header: 'NPC' },
  { accessorKey: 'level', header: 'Level' },
  { accessorKey: 'npcType', header: 'Type' },
  { accessorKey: 'npcRace', header: 'Race' },
  { accessorKey: 'npcSex', header: 'Sex' }
]

function syncRoute() {
  void router.replace({
    path: '/content/npcs',
    query: {
      ...(query.value.trim() ? { query: query.value.trim() } : {}),
      ...(page.value > 1 ? { page: String(page.value) } : {}),
      ...(pageSize.value !== 25 ? { pageSize: String(pageSize.value) } : {})
    }
  })
}

async function loadNpcs() {
  const version = ++requestVersion
  loading.value = true
  error.value = undefined
  try {
    const response = await $fetch<NpcPage>(
      npcDirectoryUrl(apiBase, {
        query: query.value,
        page: page.value,
        pageSize: pageSize.value
      })
    )
    if (version === requestVersion) result.value = response
  } catch {
    if (version === requestVersion) {
      error.value = 'The NPC directory could not be loaded from the Studio API.'
    }
  } finally {
    if (version === requestVersion) loading.value = false
  }
}

watch(query, () => {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    if (page.value !== 1) page.value = 1
    else {
      syncRoute()
      void loadNpcs()
    }
  }, 300)
})

watch(page, () => {
  syncRoute()
  void loadNpcs()
})

watch(pageSize, () => {
  if (page.value !== 1) page.value = 1
  else {
    syncRoute()
    void loadNpcs()
  }
})

onMounted(loadNpcs)
onBeforeUnmount(() => clearTimeout(searchTimer))
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Game content"
      title="NPC definitions"
      description="Browse normalized NPC records and the lookup values that classify their server behavior."
      icon="i-lucide-users-round"
    >
      <template #actions>
        <UButton
          label="Refresh"
          icon="i-lucide-refresh-cw"
          color="neutral"
          variant="outline"
          :loading="loading"
          @click="loadNpcs"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="NPC directory unavailable"
      :description="error"
    >
      <template #actions>
        <UButton color="error" variant="soft" size="sm" @click="loadNpcs">
          Try again
        </UButton>
      </template>
    </UAlert>

    <UCard v-else :ui="{ body: 'p-0 sm:p-0' }">
      <div
        class="flex flex-wrap items-center justify-between gap-4 border-b border-default px-4 py-3"
      >
        <div>
          <p class="text-sm font-medium text-highlighted">NPC catalog</p>
          <p class="text-xs text-muted">
            {{ result?.total.toLocaleString() ?? 0 }} definitions
          </p>
        </div>
        <UInput
          v-model="query"
          icon="i-lucide-search"
          placeholder="Search NPC name"
          aria-label="Search NPC name"
          maxlength="100"
          class="w-full sm:w-80"
        />
      </div>

      <div class="overflow-x-auto">
        <UTable
          :data="result?.items ?? []"
          :columns="columns"
          :loading="loading"
          empty="No NPC definitions match this search."
          class="min-w-[58rem]"
        >
          <template #id-cell="{ row }">
            <code class="text-xs text-muted">{{ row.original.id }}</code>
          </template>
          <template #name-cell="{ row }">
            <div class="flex items-center gap-3">
              <span
                class="grid size-8 shrink-0 place-items-center rounded-lg bg-elevated"
              >
                <UIcon name="i-lucide-user-round" class="size-4 text-muted" />
              </span>
              <span class="font-medium text-highlighted">
                {{ row.original.name ?? 'Unnamed NPC' }}
              </span>
            </div>
          </template>
          <template #level-cell="{ row }">
            <UBadge color="neutral" variant="subtle">
              {{ row.original.level }}
            </UBadge>
          </template>
          <template #npcType-cell="{ row }">
            <div>
              <span class="text-sm">{{ row.original.npcType }}</span>
              <span class="ml-2 text-xs text-dimmed"
                >#{{ row.original.npcTypeId }}</span
              >
            </div>
          </template>
          <template #npcRace-cell="{ row }">
            <div>
              <span class="text-sm">{{
                row.original.npcRace ?? 'No race'
              }}</span>
              <span
                v-if="row.original.npcRaceId !== null"
                class="ml-2 text-xs text-dimmed"
                >#{{ row.original.npcRaceId }}</span
              >
            </div>
          </template>
          <template #npcSex-cell="{ row }">
            <div>
              <span class="text-sm">{{ row.original.npcSex }}</span>
              <span class="ml-2 text-xs text-dimmed"
                >#{{ row.original.npcSexId }}</span
              >
            </div>
          </template>
        </UTable>
      </div>

      <StudioTableFooter
        v-model:page="page"
        v-model:page-size="pageSize"
        :total="result?.total ?? 0"
        :page-size-options="[10, 25, 50, 100]"
      />
    </UCard>
  </div>
</template>
