<script setup lang="ts">
import { getItemDefinition } from '~/services/studio-api'
import type { ItemDetailRecord } from '~/types/models/item'
import type { ItemFamily } from '~/types/requests/directory-request'
import { itemFamilyLabels, skillItemFamilies } from '~/utils/item-family'

const props = defineProps<{ family: ItemFamily }>()

const route = useRoute()
const item = ref<ItemDetailRecord>()
const loading = ref(true)
const error = ref<string>()
const typeLabel = computed(() => {
  if (!item.value) return ''
  if (!item.value.item.itemParentTypeName) return item.value.item.itemTypeDisplayName
  return `${item.value.item.itemParentTypeDisplayName ?? item.value.item.itemParentTypeName} › ${item.value.item.itemTypeDisplayName}`
})
const itemId = computed(() => {
  const value = Array.isArray(route.params.id) ? route.params.id[0] : route.params.id
  const id = Number(value)
  return Number.isSafeInteger(id) && id >= 0 ? id : undefined
})
const activeTab = computed(() => route.path.endsWith('/skills') ? 'skills' : 'overview')
const tabs = computed(() => [
  { label: 'Overview', icon: 'i-lucide-notebook-tabs', value: 'overview' },
  ...(skillItemFamilies.includes(props.family) ? [{ label: 'Skills', icon: 'i-lucide-sparkles', value: 'skills' }] : [])
])
const categoryLabel = computed(() => itemFamilyLabels[props.family])
const directoryPath = computed(() => `/authoring/items/${props.family}`)

async function load() {
  if (itemId.value === undefined) {
    item.value = undefined
    loading.value = false
    error.value = 'The item identifier is invalid.'
    return
  }
  loading.value = true
  try {
    const result = await getItemDefinition(props.family, itemId.value)
    item.value = result
    error.value = undefined
  } catch {
    item.value = undefined
    error.value = 'The item definition could not be loaded.'
  } finally {
    loading.value = false
  }
}

function selectTab(value: string | number) {
  if (itemId.value === undefined) return
  void navigateTo(detailPath(props.family, itemId.value, value === 'skills' ? 'skills' : 'overview'))
}

function detailPath(family: ItemFamily, id: number, tab: string) {
  const suffix = tab === 'skills' ? '/skills' : ''
  return `/authoring/items/${family}/${id}${suffix}`
}

watch(itemId, () => void load(), { immediate: true })
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Game content"
      :title="item ? item.item.name : `${categoryLabel} definition`"
      :description="item ? `ID: ${item.item.id} · ${typeLabel}` : `View and curate a normalized ${categoryLabel.toLowerCase()} record.`"
      icon="i-lucide-swords"
    >
      <template #actions>
        <UButton :label="`Back to ${categoryLabel.toLowerCase()} definitions`" icon="i-lucide-arrow-left" color="neutral" variant="outline" :to="directoryPath" />
      </template>
    </StudioPageHeader>

    <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="`${categoryLabel} definition unavailable`" :description="error">
      <template #actions><UButton color="error" variant="soft" size="sm" @click="load">Try again</UButton></template>
    </UAlert>

    <template v-else-if="item">
      <UTabs :items="tabs" :model-value="activeTab" :content="false" variant="link" @update:model-value="selectTab" />
      <NuxtPage :item="item" :family="props.family" @changed="load" />
    </template>

    <UCard v-else :ui="{ body: 'p-6' }"><USkeleton class="h-6 w-48" /></UCard>
  </div>
</template>
