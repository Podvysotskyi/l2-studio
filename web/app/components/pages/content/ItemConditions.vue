<script setup lang="ts">
import { createContentApi } from '~/services/content-api'
import { createStudioVersionClient } from '~/services/studio-version-client'
import { useGameVersionStore } from '~/stores/game-version'
import type { ItemDetailRecord } from '~/types/models/item'
import type { ItemFamily } from '~/types/requests/directory-request'

const props = defineProps<{ item: ItemDetailRecord; family: ItemFamily }>()
const emit = defineEmits<{ changed: [] }>()
const dialogs = useStudioDialogs()
const toasts = useStudioToasts()
const gameVersionStore = useGameVersionStore()
const saving = ref(false)
const {
  pageError: error,
  capture: captureError,
  clear: clearError,
  fieldError,
  set: setError
} = useStudioApiError()
const condition = computed(() => props.item.condition)
const form = reactive({
  messageId: 1518,
  addName: false,
  isPvpFlagged: undefined as boolean | undefined,
  playerRaces: [] as string[],
  playerCategoryTypes: [] as string[]
})

const raceOptions: Array<[string, string]> = [
  ['HUMAN', 'Human'], ['ELF', 'Elf'], ['DARK_ELF', 'Dark Elf'], ['ORC', 'Orc'], ['DWARF', 'Dwarf']
]
const categoryOptions: Array<[string, string]> = [
  ['WOLF', 'Wolf'], ['HATCHLING_GROUP', 'Hatchling group'], ['SIN_EATER_GROUP', 'Sin Eater group']
]

function reset() {
  const value = condition.value
  form.messageId = value?.messageId ?? 1518
  form.addName = value?.addName ?? false
  form.isPvpFlagged = value?.isPvpFlagged ?? undefined
  form.playerRaces = [...(value?.playerRaces ?? [])]
  form.playerCategoryTypes = [...(value?.playerCategoryTypes ?? [])]
  clearError()
}

function toggle(values: string[], value: string, checked: boolean | 'indeterminate') {
  const next = checked === true ? [...values, value] : values.filter(entry => entry !== value)
  values.splice(0, values.length, ...next)
}

async function save() {
  if (form.messageId < 1 || (form.isPvpFlagged === undefined && !form.playerRaces.length && !form.playerCategoryTypes.length)) {
    setError('Set a positive message ID and at least one player restriction.')
    return
  }
  saving.value = true
  clearError()
  try {
    await createContentApi(createStudioVersionClient(gameVersionStore.selected)).updateItemCondition(props.family, props.item.item.id, {
      messageId: form.messageId,
      addName: form.addName,
      isPvpFlagged: form.isPvpFlagged ?? null,
      playerRaces: form.playerRaces,
      playerCategoryTypes: form.playerCategoryTypes
    })
    toasts.success({ title: 'Item condition saved' })
    emit('changed')
  } catch (cause) {
    captureError(cause, 'The item condition could not be saved.')
  } finally {
    saving.value = false
  }
}

async function remove() {
  if (!await dialogs.confirm({ title: 'Remove item condition?', description: 'The item will no longer have this server-enforced restriction.', confirmLabel: 'Remove condition', confirmColor: 'error' })) return
  saving.value = true
  try {
    await createContentApi(createStudioVersionClient(gameVersionStore.selected)).deleteItemCondition(props.family, props.item.item.id)
    toasts.success({ title: 'Item condition removed' })
    reset()
    emit('changed')
  } catch (cause) {
    captureError(cause, 'The item condition could not be removed.')
  } finally {
    saving.value = false
  }
}

watch(condition, reset, { immediate: true })
</script>

<template>
  <UCard>
    <template #header>
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div><h2 class="text-sm font-semibold text-highlighted">Player use restriction</h2><p class="mt-1 text-xs text-muted">All selected fields must match before this item can be used or equipped.</p></div>
        <UButton v-if="condition" label="Remove" icon="i-lucide-trash-2" color="error" variant="soft" size="sm" :loading="saving" @click="remove" />
      </div>
    </template>
    <form class="space-y-5" @submit.prevent="save">
      <UAlert v-if="error" color="error" variant="subtle" :description="error" />
      <div class="grid gap-4 sm:grid-cols-2">
        <UFormField label="System message ID" required :error="fieldError('messageId') ?? fieldError('condition')"><UInput v-model.number="form.messageId" type="number" min="1" class="w-full" /></UFormField>
        <UFormField label="PvP flag"><USelect v-model="form.isPvpFlagged" :items="[{ label: 'Any state', value: undefined }, { label: 'Must be flagged', value: true }, { label: 'Must not be flagged', value: false }]" class="w-full" /></UFormField>
      </div>
      <UCheckbox v-model="form.addName" label="Include the item name in the system message" />
      <div class="grid gap-5 lg:grid-cols-2">
        <fieldset class="space-y-2"><legend class="text-sm font-medium text-highlighted">Allowed races</legend><UCheckbox v-for="[value, label] in raceOptions" :key="value" :model-value="form.playerRaces.includes(value)" :label="label" @update:model-value="toggle(form.playerRaces, value, $event)" /></fieldset>
        <fieldset class="space-y-2"><legend class="text-sm font-medium text-highlighted">Allowed pet categories</legend><UCheckbox v-for="[value, label] in categoryOptions" :key="value" :model-value="form.playerCategoryTypes.includes(value)" :label="label" @update:model-value="toggle(form.playerCategoryTypes, value, $event)" /></fieldset>
      </div>
      <div class="flex justify-end gap-3"><UButton label="Reset" color="neutral" variant="outline" :disabled="saving" @click="reset" /><UButton type="submit" label="Save condition" icon="i-lucide-save" :loading="saving" /></div>
    </form>
  </UCard>
</template>
