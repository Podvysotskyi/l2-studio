<script setup lang="ts">
import { getItemSet } from '~/services/studio-api'
import type { ItemSetRecord } from '~/types/models/item-set'

const route = useRoute()
const itemSet = ref<ItemSetRecord>()
const loading = ref(true)
const error = ref<string>()
const setId = computed(() => Number(Array.isArray(route.params.id) ? route.params.id[0] : route.params.id))

async function load() {
  if (!Number.isSafeInteger(setId.value) || setId.value < 1) { error.value = 'The item-set identifier is invalid.'; loading.value = false; return }
  loading.value = true
  try { itemSet.value = await getItemSet(setId.value); error.value = undefined } catch { itemSet.value = undefined; error.value = 'The item set could not be loaded.' } finally { loading.value = false }
}

watch(setId, () => void load(), { immediate: true })
</script>

<template><div class="space-y-6"><StudioPageHeader eyebrow="Game content" :title="itemSet ? `Item set #${itemSet.setId}` : 'Item set'" description="C1 armor-set equipment requirements and effect." icon="i-lucide-shield-check"><template #actions><UButton label="Back to item sets" icon="i-lucide-arrow-left" color="neutral" variant="outline" to="/authoring/items/sets" /></template></StudioPageHeader><UAlert v-if="error" color="error" variant="subtle" :description="error"><template #actions><UButton size="sm" @click="load">Try again</UButton></template></UAlert><ItemSetDetails v-else-if="itemSet" :item-set="itemSet" /><UCard v-else :ui="{ body: 'p-6' }"><USkeleton class="h-24 w-full" /></UCard></div></template>
