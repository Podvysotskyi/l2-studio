<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { deleteSkillDefinition, getSkillLookupDirectory, updateSkillDefinition } from '../../../services/studio-api'
import type { SkillLookupRecord, SkillRecord } from '../../../types/models/content-directory'
import { loadDirectoryOptions } from '../../../utils/directory-pages'

const query = defineModel<string>('query', { required: true })
const page = defineModel<number>('page', { required: true })
const pageSize = defineModel<number>('pageSize', { required: true })

defineProps<{
  items: SkillRecord[]
  total: number
  loading: boolean
  error?: string
}>()

const emit = defineEmits<{ refresh: [] }>()
const dialogs = useStudioDialogs()
const notifications = useStudioToasts()
const selectedSkill = ref<SkillRecord>()
const editOpen = ref(false)
const saving = ref(false)
const deletingId = ref<number>()
const editError = ref<string>()
const lookupsLoading = ref(false)
const operateTypes = ref<SkillLookupRecord[]>([])
const targetTypes = ref<SkillLookupRecord[]>([])
const editForm = reactive({
  name: '',
  levels: 1,
  skillOperateTypeName: undefined as string | undefined,
  skillTargetTypeName: undefined as string | undefined
})

const columns: TableColumn<SkillRecord>[] = [
  { accessorKey: 'id', header: 'ID' },
  { accessorKey: 'name', header: 'Skill' },
  { accessorKey: 'levels', header: 'Levels' },
  { accessorKey: 'skillOperateTypeDisplayName', header: 'Operate type' },
  { accessorKey: 'skillTargetTypeDisplayName', header: 'Target type' },
  { accessorKey: 'iconCount', header: 'Icons' },
  { id: 'actions', header: '' }
]

const operateTypeOptions = computed(() => [{ label: 'Unassigned', value: undefined }, ...lookupOptions(operateTypes.value)])
const targetTypeOptions = computed(() => [{ label: 'Unassigned', value: undefined }, ...lookupOptions(targetTypes.value)])

function lookupOptions(records: SkillLookupRecord[]) {
  return records.map(record => ({
    label: record.displayName === record.name ? record.name : `${record.displayName} (${record.name})`,
    value: record.name
  }))
}

async function loadLookups() {
  if (lookupsLoading.value || (operateTypes.value.length && targetTypes.value.length)) return
  lookupsLoading.value = true
  try {
    const [operate, target] = await Promise.all([
      loadDirectoryOptions((page, pageSize) => getSkillLookupDirectory('skill-operate-types', { page, pageSize })),
      loadDirectoryOptions((page, pageSize) => getSkillLookupDirectory('skill-target-types', { page, pageSize }))
    ])
    operateTypes.value = operate
    targetTypes.value = target
  } catch {
    editError.value = 'Skill lookup values could not be loaded.'
  } finally {
    lookupsLoading.value = false
  }
}

async function edit(skill: SkillRecord) {
  selectedSkill.value = skill
  editForm.name = skill.name
  editForm.levels = skill.levels
  editForm.skillOperateTypeName = skill.skillOperateTypeName ?? undefined
  editForm.skillTargetTypeName = skill.skillTargetTypeName ?? undefined
  editError.value = undefined
  editOpen.value = true
  await loadLookups()
}

async function save() {
  const skill = selectedSkill.value
  if (!skill) return
  const name = editForm.name.trim()
  if (!name || name.length > 100 || !Number.isInteger(editForm.levels) || editForm.levels < 1 || editForm.levels > 255) {
    editError.value = 'Name must contain between 1 and 100 characters and levels must be between 1 and 255.'
    return
  }
  saving.value = true
  editError.value = undefined
  try {
    await updateSkillDefinition(skill.id, { ...editForm, name })
    editOpen.value = false
    notifications.success({ title: 'Skill definition saved' })
    emit('refresh')
  } catch {
    editError.value = 'Skill definition could not be saved. Check the selected lookup values and try again.'
  } finally {
    saving.value = false
  }
}

async function remove(skill: SkillRecord) {
  const confirmed = await dialogs.confirm({
    title: `Delete ${skill.name}?`,
    description: `Skill #${skill.id} and its level icons will be permanently removed. A later import can restore the source record.`,
    confirmLabel: 'Delete skill',
    confirmColor: 'error'
  })
  if (!confirmed) return
  deletingId.value = skill.id
  try {
    await deleteSkillDefinition(skill.id)
    notifications.success({ title: 'Skill definition deleted' })
    emit('refresh')
  } catch {
    notifications.error({ title: 'Skill definition could not be deleted' })
  } finally {
    deletingId.value = undefined
  }
}
</script>

<template>
  <StudioContentDirectoryLayout
      title="Skill definitions"
      description="Browse normalized skill records, activation modes, targeting modes, and level-specific icon coverage."
      icon="i-lucide-sparkles"
      import-target="skills"
      import-label="skills"
      :loading="loading"
      :error="error"
      @refresh="emit('refresh')"
    >
    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <StudioDataTable
        v-model:query="query"
        v-model:page="page"
        v-model:page-size="pageSize"
        :data="items"
        :total="total"
        :columns="columns"
        :loading="loading"
        empty="No skill definitions match this search."
        search-placeholder="Search skill name"
        search-aria-label="Search skill name"
        :page-size-options="[10, 25, 50, 100]"
        table-class="min-w-[62rem]"
      >
        <template #toolbar-start>
          <div>
            <p class="text-sm font-medium text-highlighted">Skill catalog</p>
            <p class="text-xs text-muted">{{ total.toLocaleString() }} definitions</p>
          </div>
        </template>
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
          <template #skillOperateTypeDisplayName-cell="{ row }">
            <div>
              <span class="text-sm">{{
                row.original.skillOperateTypeDisplayName ?? 'Unassigned'
              }}</span>
              <span
                v-if="row.original.skillOperateTypeName !== null"
                class="ml-2 text-xs text-dimmed"
                >{{ row.original.skillOperateTypeName }}</span
              >
            </div>
          </template>
          <template #skillTargetTypeDisplayName-cell="{ row }">
            <div>
              <span class="text-sm">{{
                row.original.skillTargetTypeDisplayName ?? 'Unassigned'
              }}</span>
              <span
                v-if="row.original.skillTargetTypeName !== null"
                class="ml-2 text-xs text-dimmed"
                >{{ row.original.skillTargetTypeName }}</span
              >
            </div>
          </template>
          <template #iconCount-cell="{ row }">
            <span class="text-sm text-muted">{{ row.original.iconCount }}</span>
          </template>
          <template #actions-cell="{ row }">
            <StudioTableRowActions
              :show-edit="true"
              :show-delete="true"
              :delete-loading="deletingId === row.original.id"
              @edit="edit(row.original)"
              @delete="remove(row.original)"
            />
          </template>
      </StudioDataTable>
    </UCard>
    <UModal v-model:open="editOpen" title="Edit skill definition">
      <template #body>
        <form class="space-y-4" @submit.prevent="save">
          <UAlert v-if="editError" color="error" variant="subtle" :description="editError" />
          <UFormField label="Name" required><UInput v-model="editForm.name" maxlength="100" class="w-full" /></UFormField>
          <UFormField label="Levels" required><UInput v-model.number="editForm.levels" type="number" min="1" max="255" class="w-full" /></UFormField>
          <UFormField label="Operate type"><USelect v-model="editForm.skillOperateTypeName" :items="operateTypeOptions" :loading="lookupsLoading" class="w-full" /></UFormField>
          <UFormField label="Target type"><USelect v-model="editForm.skillTargetTypeName" :items="targetTypeOptions" :loading="lookupsLoading" class="w-full" /></UFormField>
          <div class="flex justify-end gap-3 pt-2"><UButton label="Cancel" color="neutral" variant="outline" @click="editOpen = false" /><UButton type="submit" label="Save changes" icon="i-lucide-save" :loading="saving" :disabled="lookupsLoading" /></div>
        </form>
      </template>
    </UModal>
  </StudioContentDirectoryLayout>
</template>
