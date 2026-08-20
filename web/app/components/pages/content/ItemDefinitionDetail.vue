<script setup lang="ts">
import { getItemDefinition, resolveItemIcons } from '~/services/studio-api'
import type { ItemDetailRecord } from '~/types/models/item'
import type { ItemFamily } from '~/types/requests/directory-request'
import { itemFamilyLabels, skillItemFamilies } from '~/utils/item-family'

const props = defineProps<{ family: ItemFamily }>()

const route = useRoute()
const item = ref<ItemDetailRecord>()
const iconUrl = ref<string>()
const loading = ref(true)
const error = ref<string>()
let loadVersion = 0
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
const activeTab = computed(() => route.path.endsWith('/skills') ? 'skills' : route.path.endsWith('/conditions') ? 'conditions' : 'overview')
const tabs = computed(() => [
  { label: 'Overview', icon: 'i-lucide-notebook-tabs', value: 'overview' },
  { label: 'Conditions', icon: 'i-lucide-shield-check', value: 'conditions' },
  ...(skillItemFamilies.includes(props.family) ? [{ label: 'Skills', icon: 'i-lucide-sparkles', value: 'skills' }] : [])
])
const categoryLabel = computed(() => itemFamilyLabels[props.family])
const directoryPath = computed(() => `/authoring/items/${props.family}`)

async function load() {
  const version = ++loadVersion
  if (itemId.value === undefined) {
    item.value = undefined
    iconUrl.value = undefined
    loading.value = false
    error.value = 'The item identifier is invalid.'
    return
  }
  loading.value = true
  try {
    const result = await getItemDefinition(props.family, itemId.value)
    if (version !== loadVersion) return
    item.value = result
    iconUrl.value = undefined
    error.value = undefined
    if (result.item.icon)
      void loadIcon(result, version)
  } catch {
    if (version !== loadVersion) return
    item.value = undefined
    iconUrl.value = undefined
    error.value = 'The item definition could not be loaded.'
  } finally {
    if (version === loadVersion) loading.value = false
  }
}

async function loadIcon(result: ItemDetailRecord, version: number) {
  try {
    const [resolved] = await resolveItemIcons([{
      itemId: result.item.id,
      icon: result.item.icon!,
      itemBodyPartName: result.item.itemBodyPartName
    }])
    if (version === loadVersion) iconUrl.value = resolved?.url
  } catch {
    // Item artwork is supplemental; the definition remains usable without it.
  }
}

function selectTab(value: string | number) {
  if (itemId.value === undefined) return
  void navigateTo(detailPath(props.family, itemId.value, value === 'skills' || value === 'conditions' ? value : 'overview'))
}

function detailPath(family: ItemFamily, id: number, tab: string) {
  const suffix = tab === 'overview' ? '' : `/${tab}`
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
      <template #icon>
        <ItemIconThumbnail :url="iconUrl" :alt="item ? item.item.name : `${categoryLabel} definition`" variant="header" />
      </template>
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
