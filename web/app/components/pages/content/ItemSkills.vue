<script setup lang="ts">
import {
  clearItemPrimarySkill,
  createItemSkill,
  deleteItemSkill,
  getItemLookups,
  getSkillDirectory,
  setItemPrimarySkill,
  updateItemSkill
} from '~/services/studio-api'
import type { SkillRecord } from '~/types/models/content-directory'
import type { ItemDetailRecord, ItemLookupRecord, ItemSkillRecord } from '~/types/models/item'
import type { ItemFamily } from '~/types/requests/directory-request'
import { loadDirectoryOptions } from '~/utils/directory-pages'

const props = defineProps<{ item: ItemDetailRecord; family: ItemFamily }>()
const emit = defineEmits<{ changed: [] }>()
const dialogs = useStudioDialogs()
const notifications = useStudioToasts()

type EditorMode = 'primary' | 'add' | 'edit'

const editorOpen = ref(false)
const editorMode = ref<EditorMode>('add')
const saving = ref(false)
const deletingKey = ref<string>()
const {
  pageError: editorError,
  capture: captureEditorError,
  clear: clearEditorError,
  fieldError: editorFieldError,
  set: setEditorError
} = useStudioApiError()
const attachedSkill = ref<ItemSkillRecord>()
const selectedSkill = ref<SkillRecord>()
const skillQuery = ref('')
const skillResults = ref<SkillRecord[]>([])
const skillSearchLoading = ref(false)
const skillTypesLoading = ref(false)
const skillTypes = ref<ItemLookupRecord[]>([])
const form = reactive({
  skillLevel: 1,
  itemSkillTypeName: undefined as string | undefined,
  chance: null as number | null
})

const definition = computed(() => props.item.item)
const properties = computed(() => props.item.properties)
const primarySkill = computed(() => props.item.primarySkill)
const editorTitle = computed(() => ({
  primary: 'Set primary item skill',
  add: 'Attach skill',
  edit: 'Edit attached skill'
}[editorMode.value]))
const skillTypeOptions = computed(() => [
  { label: 'No trigger type', value: undefined },
  ...skillTypes.value.map(type => ({
    label: type.displayName === type.name ? type.name : `${type.displayName} (${type.name})`,
    value: type.name
  }))
])
const selectedSkillLabel = computed(() => selectedSkill.value
  ? `${selectedSkill.value.name} (#${selectedSkill.value.id})`
  : undefined)
const primarySkillLabel = computed(() => {
  const skill = primarySkill.value
  if (!skill) return undefined
  if (skill.skillId === null || skill.skillLevel === null) return `Unresolved source value: ${skill.value}`
  return `${skill.skillName ?? `Skill #${skill.skillId}`} · Level ${skill.skillLevel}`
})

async function loadSkillTypes() {
  if (skillTypesLoading.value || skillTypes.value.length) return
  skillTypesLoading.value = true
  try {
    skillTypes.value = await loadDirectoryOptions((page, pageSize) =>
      getItemLookups('item-skill-types', { page, pageSize }))
  } catch (cause) {
    captureEditorError(cause, 'Item skill trigger types could not be loaded.')
  } finally {
    skillTypesLoading.value = false
  }
}

async function searchSkills() {
  const query = skillQuery.value.trim()
  if (!query) {
    skillResults.value = []
    return
  }
  skillSearchLoading.value = true
  try {
    const page = await getSkillDirectory({ query, page: 1, pageSize: 20 })
    skillResults.value = page.items
  } catch (cause) {
    captureEditorError(cause, 'The skill catalog could not be searched.')
  } finally {
    skillSearchLoading.value = false
  }
}

function selectSkill(skill: SkillRecord) {
  selectedSkill.value = skill
  form.skillLevel = 1
  skillResults.value = []
  skillQuery.value = ''
}

async function openEditor(mode: EditorMode, skill?: ItemSkillRecord) {
  editorMode.value = mode
  attachedSkill.value = skill
  selectedSkill.value = undefined
  skillQuery.value = ''
  skillResults.value = []
  clearEditorError()
  form.skillLevel = skill?.skillLevel ?? 1
  form.itemSkillTypeName = skill?.itemSkillTypeName ?? undefined
  form.chance = skill?.chance ?? null
  editorOpen.value = true
  await loadSkillTypes()
}

function validSelection() {
  return selectedSkill.value && form.skillLevel >= 1 && form.skillLevel <= selectedSkill.value.levels
}

async function save() {
  const item = definition.value
  if (editorMode.value !== 'edit' && !validSelection()) {
    setEditorError('Choose a skill and a level supported by that skill.', {
      itemSkill: ['Choose a skill and a level supported by that skill.']
    })
    return
  }
  if (editorMode.value !== 'primary' && (form.chance == null ? false : form.chance < 0 || form.chance > 100)) {
    setEditorError('Chance must be between 0 and 100.', {
      itemSkill: ['Chance must be between 0 and 100.']
    })
    return
  }

  saving.value = true
  clearEditorError()
  try {
    if (editorMode.value === 'primary') {
      await setItemPrimarySkill(props.family, item.id, {
        skillId: selectedSkill.value!.id,
        skillLevel: form.skillLevel
      })
      notifications.success({ title: 'Primary item skill saved' })
    } else if (editorMode.value === 'add') {
      await createItemSkill(props.family, item.id, {
        skillId: selectedSkill.value!.id,
        skillLevel: form.skillLevel,
        itemSkillTypeName: form.itemSkillTypeName,
        chance: form.chance
      })
      notifications.success({ title: 'Item skill attached' })
    } else if (attachedSkill.value) {
      await updateItemSkill(props.family, item.id, attachedSkill.value.skillId, attachedSkill.value.skillLevel, {
        itemSkillTypeName: form.itemSkillTypeName,
        chance: form.chance
      })
      notifications.success({ title: 'Item skill saved' })
    }
    editorOpen.value = false
    emit('changed')
  } catch (cause) {
    captureEditorError(cause, 'The item skill could not be saved. Check the selected skill, level, and trigger type.')
  } finally {
    saving.value = false
  }
}

async function removeSkill(skill: ItemSkillRecord) {
  const confirmed = await dialogs.confirm({
    title: `Remove ${skill.skillName ?? `skill #${skill.skillId}`}?`,
    description: 'This removes the skill association from this item. A restore-defaults import can restore the source association.',
    confirmLabel: 'Remove skill',
    confirmColor: 'error'
  })
  if (!confirmed) return
  const key = `${skill.skillId}-${skill.skillLevel}`
  deletingKey.value = key
  try {
    await deleteItemSkill(props.family, definition.value.id, skill.skillId, skill.skillLevel)
    notifications.success({ title: 'Item skill removed' })
    emit('changed')
  } catch {
    notifications.error({ title: 'Item skill could not be removed' })
  } finally {
    deletingKey.value = undefined
  }
}

async function clearPrimarySkill() {
  const confirmed = await dialogs.confirm({
    title: 'Clear primary item skill?',
    description: 'This removes the item_skill source value. A restore-defaults item import can restore it.',
    confirmLabel: 'Clear primary skill',
    confirmColor: 'error'
  })
  if (!confirmed) return
  try {
    await clearItemPrimarySkill(props.family, definition.value.id)
    notifications.success({ title: 'Primary item skill cleared' })
    emit('changed')
  } catch {
    notifications.error({ title: 'Primary item skill could not be cleared' })
  }
}

function state(value: boolean | null) {
  if (value === null) return 'Unspecified'
  return value ? 'Enabled' : 'Disabled'
}
</script>

<template>
  <div class="space-y-6">
    <UCard>
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h2 class="text-sm font-semibold text-highlighted">Skill behavior</h2>
          <p class="mt-1 text-xs text-muted">Handler and use settings defined on the item itself.</p>
        </div>
        <div v-if="props.family === 'etc'" class="flex gap-2">
          <UButton label="Set primary skill" icon="i-lucide-sparkles" size="sm" @click="openEditor('primary')" />
          <UButton v-if="primarySkill" label="Clear" icon="i-lucide-trash-2" color="error" variant="soft" size="sm" @click="clearPrimarySkill" />
        </div>
      </div>
      <dl class="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <div v-for="[label, value] in [['Handler', definition.handlerDisplayName ?? definition.handlerName], ['Primary item skill', primarySkillLabel], ['Use condition', properties.useCondition], ['MP consume', properties.mpConsume], ['Reduced MP consume', properties.reducedMpConsume], ['Reuse delay (ms)', properties.reuseDelay], ['Use weapon skills only', state(properties.useWeaponSkillsOnly)]]" :key="label" class="rounded-md bg-muted/40 px-3 py-2">
          <dt class="text-xs font-medium text-muted">{{ label }}</dt><dd class="mt-1 text-sm text-highlighted">{{ value ?? '—' }}</dd>
        </div>
      </dl>
      <UAlert v-if="primarySkill && primarySkill.skillId === null" class="mt-4" color="warning" variant="subtle" title="Primary skill could not be resolved" :description="`The imported value '${primarySkill.value}' is preserved. Set a valid primary skill to replace it.`" />
    </UCard>

    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <template #header>
        <div class="flex flex-wrap items-center justify-between gap-3">
          <div><h2 class="text-sm font-semibold text-highlighted">Attached skills</h2><p class="mt-1 text-xs text-muted">{{ definition.skills.length }} skill association{{ definition.skills.length === 1 ? '' : 's' }}</p></div>
          <UButton label="Attach skill" icon="i-lucide-plus" size="sm" @click="openEditor('add')" />
        </div>
      </template>
      <div v-if="definition.skills.length" class="overflow-x-auto">
        <table class="w-full min-w-[44rem] text-left text-sm">
          <thead class="border-b border-default text-xs uppercase tracking-wide text-muted"><tr><th class="p-4">Skill</th><th class="p-4">Level</th><th class="p-4">Trigger</th><th class="p-4">Chance</th><th class="p-4" /></tr></thead>
          <tbody>
            <tr v-for="skill in definition.skills" :key="`${skill.skillId}-${skill.skillLevel}`" class="border-b border-default last:border-0">
              <td class="p-4 font-medium text-highlighted">{{ skill.skillName ?? `Skill #${skill.skillId}` }} <span class="text-xs text-muted">(#{{ skill.skillId }})</span></td>
              <td class="p-4">{{ skill.skillLevel }}</td>
              <td class="p-4">{{ skill.itemSkillTypeDisplayName ?? skill.itemSkillTypeName ?? 'Always' }}</td>
              <td class="p-4">{{ skill.chance == null ? '—' : `${skill.chance}%` }}</td>
              <td class="p-4 text-right"><div class="flex justify-end gap-2"><UButton icon="i-lucide-pencil" color="neutral" variant="ghost" size="xs" aria-label="Edit item skill" @click="openEditor('edit', skill)" /><UButton icon="i-lucide-trash-2" color="error" variant="ghost" size="xs" aria-label="Remove item skill" :loading="deletingKey === `${skill.skillId}-${skill.skillLevel}`" @click="removeSkill(skill)" /></div></td>
            </tr>
          </tbody>
        </table>
      </div>
      <p v-else class="p-6 text-sm text-muted">No skills are attached to this item.</p>
    </UCard>

    <UModal v-model:open="editorOpen" :title="editorTitle">
      <template #body>
        <form class="space-y-4" @submit.prevent="save">
          <UAlert v-if="editorError" color="error" variant="subtle" :description="editorError" />
          <template v-if="editorMode !== 'edit'">
            <UFormField label="Search skill catalog" hint="Search by skill name or exact ID">
              <div class="flex gap-2"><UInput v-model="skillQuery" placeholder="e.g. Bleed or 3005" class="w-full" @keyup.enter.prevent="searchSkills" /><UButton label="Search" color="neutral" variant="outline" :loading="skillSearchLoading" @click="searchSkills" /></div>
            </UFormField>
            <div v-if="skillResults.length" class="max-h-48 space-y-1 overflow-y-auto rounded-md border border-default p-2">
              <UButton v-for="skill in skillResults" :key="skill.id" color="neutral" variant="ghost" class="w-full justify-start" @click="selectSkill(skill)">{{ skill.name }} <span class="ml-1 text-xs text-muted">#{{ skill.id }} · {{ skill.levels }} levels</span></UButton>
            </div>
            <UAlert v-if="selectedSkillLabel" color="primary" variant="subtle" :title="selectedSkillLabel" :description="`${selectedSkill!.levels} level${selectedSkill!.levels === 1 ? '' : 's'} available`" />
          </template>
          <UFormField v-else label="Skill" :error="editorFieldError('itemSkill')"><UInput :model-value="`${attachedSkill?.skillName ?? `Skill #${attachedSkill?.skillId}`} · Level ${attachedSkill?.skillLevel}`" disabled class="w-full" /></UFormField>
          <UFormField v-if="editorMode !== 'edit'" label="Level" required :error="editorFieldError('itemSkill')"><UInput v-model.number="form.skillLevel" type="number" min="1" :max="selectedSkill?.levels" :disabled="!selectedSkill" class="w-full" /></UFormField>
          <template v-if="editorMode !== 'primary'">
            <UFormField label="Trigger type" :error="editorFieldError('itemSkillTypeName')"><USelect v-model="form.itemSkillTypeName" :items="skillTypeOptions" :loading="skillTypesLoading" class="w-full" /></UFormField>
            <UFormField label="Chance (%)" :error="editorFieldError('itemSkill')"><UInput v-model.number="form.chance" type="number" min="0" max="100" class="w-full" /></UFormField>
          </template>
          <div class="flex justify-end gap-3 pt-2"><UButton label="Cancel" color="neutral" variant="outline" @click="editorOpen = false" /><UButton type="submit" :label="editorMode === 'add' ? 'Attach skill' : 'Save changes'" icon="i-lucide-save" :loading="saving" :disabled="skillTypesLoading" /></div>
        </form>
      </template>
    </UModal>
  </div>
</template>
