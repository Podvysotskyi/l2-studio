<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { onBeforeUnmount, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  positiveInteger,
  skillDirectoryUrl,
  type SkillPage,
  type SkillRecord
} from '../../utils/studio-content'

const apiBase = ''
const route = useRoute()
const router = useRouter()
const query = ref(
  typeof route.query.query === 'string' ? route.query.query : ''
)
const page = ref(positiveInteger(route.query.page, 1))
const pageSize = ref(positiveInteger(route.query.pageSize, 25))
const result = ref<SkillPage>()
const loading = ref(true)
const error = ref<string>()
let searchTimer: ReturnType<typeof setTimeout> | undefined
let requestVersion = 0

const columns: TableColumn<SkillRecord>[] = [
  { accessorKey: 'id', header: 'ID' },
  { accessorKey: 'name', header: 'Skill' },
  { accessorKey: 'levels', header: 'Levels' },
  { accessorKey: 'skillOperateType', header: 'Operate type' },
  { accessorKey: 'skillTargetType', header: 'Target type' },
  { accessorKey: 'iconCount', header: 'Icons' }
]

function syncRoute() {
  void router.replace({
    path: '/content/skills',
    query: {
      ...(query.value.trim() ? { query: query.value.trim() } : {}),
      ...(page.value > 1 ? { page: String(page.value) } : {}),
      ...(pageSize.value !== 25 ? { pageSize: String(pageSize.value) } : {})
    }
  })
}

async function loadSkills() {
  const version = ++requestVersion
  loading.value = true
  error.value = undefined
  try {
    const response = await $fetch<SkillPage>(
      skillDirectoryUrl(apiBase, {
        query: query.value,
        page: page.value,
        pageSize: pageSize.value
      })
    )
    if (version === requestVersion) result.value = response
  } catch {
    if (version === requestVersion) {
      error.value =
        'The skill directory could not be loaded from the Studio API.'
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
      void loadSkills()
    }
  }, 300)
})

watch(page, () => {
  syncRoute()
  void loadSkills()
})

watch(pageSize, () => {
  if (page.value !== 1) page.value = 1
  else {
    syncRoute()
    void loadSkills()
  }
})

onMounted(loadSkills)
onBeforeUnmount(() => clearTimeout(searchTimer))
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Game content"
      title="Skill definitions"
      description="Browse normalized skill records, activation modes, targeting modes, and level-specific icon coverage."
      icon="i-lucide-sparkles"
    >
      <template #actions>
        <UButton
          label="Refresh"
          icon="i-lucide-refresh-cw"
          color="neutral"
          variant="outline"
          :loading="loading"
          @click="loadSkills"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="Skill directory unavailable"
      :description="error"
    >
      <template #actions>
        <UButton color="error" variant="soft" size="sm" @click="loadSkills">
          Try again
        </UButton>
      </template>
    </UAlert>

    <UCard v-else :ui="{ body: 'p-0 sm:p-0' }">
      <div
        class="flex flex-wrap items-center justify-between gap-4 border-b border-default px-4 py-3"
      >
        <div>
          <p class="text-sm font-medium text-highlighted">Skill catalog</p>
          <p class="text-xs text-muted">
            {{ result?.total.toLocaleString() ?? 0 }} definitions
          </p>
        </div>
        <UInput
          v-model="query"
          icon="i-lucide-search"
          placeholder="Search skill name"
          aria-label="Search skill name"
          maxlength="100"
          class="w-full sm:w-80"
        />
      </div>

      <div class="overflow-x-auto">
        <UTable
          :data="result?.items ?? []"
          :columns="columns"
          :loading="loading"
          empty="No skill definitions match this search."
          class="min-w-[62rem]"
        >
          <template #id-cell="{ row }">
            <code class="text-xs text-muted">{{ row.original.id }}</code>
          </template>
          <template #name-cell="{ row }">
            <div class="flex items-center gap-3">
              <span
                class="grid size-8 shrink-0 place-items-center rounded-lg bg-elevated"
              >
                <UIcon name="i-lucide-sparkles" class="size-4 text-muted" />
              </span>
              <span class="font-medium text-highlighted">
                {{ row.original.name || 'Unnamed skill' }}
              </span>
            </div>
          </template>
          <template #levels-cell="{ row }">
            <UBadge color="neutral" variant="subtle">
              {{ row.original.levels }}
            </UBadge>
          </template>
          <template #skillOperateType-cell="{ row }">
            <div>
              <span class="text-sm">{{
                row.original.skillOperateType ?? 'Unassigned'
              }}</span>
              <span
                v-if="row.original.skillOperateTypeId !== null"
                class="ml-2 text-xs text-dimmed"
                >#{{ row.original.skillOperateTypeId }}</span
              >
            </div>
          </template>
          <template #skillTargetType-cell="{ row }">
            <div>
              <span class="text-sm">{{
                row.original.skillTargetType ?? 'Unassigned'
              }}</span>
              <span
                v-if="row.original.skillTargetTypeId !== null"
                class="ml-2 text-xs text-dimmed"
                >#{{ row.original.skillTargetTypeId }}</span
              >
            </div>
          </template>
          <template #iconCount-cell="{ row }">
            <span class="text-sm text-muted">{{ row.original.iconCount }}</span>
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
