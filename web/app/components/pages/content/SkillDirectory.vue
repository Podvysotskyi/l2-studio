<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { getSkillImportRuns, startSkillImport } from '../../../services/studio-api'
import type { SkillRecord } from '../../../types/models/content-directory'
import type { SkillImportMode, SkillImportRun } from '../../../types/models/skill-import'

const query = defineModel<string>('query', { required: true })
const page = defineModel<number>('page', { required: true })
const pageSize = defineModel<number>('pageSize', { required: true })

defineProps<{
  items: SkillRecord[]
  total: number
  loading: boolean
  error?: string
}>()

defineEmits<{ refresh: [] }>()

const run = ref<SkillImportRun>()
const importing = ref<SkillImportMode>()
const toast = useStudioToasts()

async function importSkills(mode: SkillImportMode) {
  importing.value = mode
  try {
    run.value = await startSkillImport(mode)
    toast.success({ title: 'Skill import queued' })
  } catch {
    toast.error({ title: 'Skill import could not be queued' })
  } finally {
    importing.value = undefined
  }
}

async function loadRun() {
  try {
    run.value = (await getSkillImportRuns())[0]
  } catch {
    // The directory remains available while import history is unavailable.
  }
}

onMounted(() => void loadRun())

const columns: TableColumn<SkillRecord>[] = [
  { accessorKey: 'id', header: 'ID' },
  { accessorKey: 'name', header: 'Skill' },
  { accessorKey: 'levels', header: 'Levels' },
  { accessorKey: 'skillOperateType', header: 'Operate type' },
  { accessorKey: 'skillTargetType', header: 'Target type' },
  { accessorKey: 'iconCount', header: 'Icons' }
]
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
          label="Import missing"
          icon="i-lucide-download"
          :loading="importing === 'add_missing'"
          @click="importSkills('add_missing')"
        />
        <UButton
          label="Restore defaults"
          color="neutral"
          variant="outline"
          :loading="importing === 'restore_defaults'"
          @click="importSkills('restore_defaults')"
        />
        <UButton
          label="Refresh"
          icon="i-lucide-refresh-cw"
          color="neutral"
          variant="outline"
          :loading="loading"
          @click="$emit('refresh')"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="run"
      :color="run.status === 'failed' ? 'error' : 'neutral'"
      variant="subtle"
      :title="`Latest import: ${run.status}`"
      :description="run.error || `${run.insertedCount} inserted, ${run.restoredCount} restored of ${run.totalCount}.`"
    />

    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="Skill directory unavailable"
      :description="error"
    >
      <template #actions>
        <UButton color="error" variant="soft" size="sm" @click="$emit('refresh')">
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
            {{ total.toLocaleString() }} definitions
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
          :data="items"
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
        :total="total"
        :page-size-options="[10, 25, 50, 100]"
      />
    </UCard>
  </div>
</template>
