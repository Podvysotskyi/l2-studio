<script setup lang="ts">
import { getItemDefinition } from '~/services/studio-api'
import type { ItemRecord } from '~/types/models/item'

const route = useRoute()
const item = ref<ItemRecord>()
const error = ref<string>()

async function load() {
  try {
    item.value = await getItemDefinition(Number(route.params.id))
    error.value = undefined
  } catch {
    error.value = 'The item definition could not be loaded.'
  }
}

watch(() => route.params.id, () => void load(), { immediate: true })
</script>

<template>
  <div class="space-y-4">
    <UAlert v-if="error" color="error" :description="error" />
    <template v-else-if="item">
      <UPageHeader :title="item.name" :description="`Item #${item.id} · ${item.itemTypeDisplayName}`" />
      <UCard><dl class="grid grid-cols-1 gap-x-6 gap-y-3 text-sm md:grid-cols-2"><template v-for="[label, value] in [['Type', item.itemTypeDisplayName], ['Action', item.itemActionDisplayName], ['Body part', item.itemBodyPartDisplayName], ['Material', item.itemMaterialDisplayName], ['Crystal type', item.itemCrystalTypeDisplayName], ['Icon', item.icon], ['Weight', item.weight], ['Price', item.price], ['Weapon type', item.weaponType], ['Armor type', item.armorType], ['Etc item type', item.etcItemType], ['Damage range', item.damageRange]]" :key="label"><dt class="text-muted">{{ label }}</dt><dd class="font-medium text-highlighted">{{ value ?? '—' }}</dd></template></dl></UCard>
      <UCard v-if="item.stats"><template #header>Statistics</template><pre class="text-xs">{{ item.stats }}</pre></UCard>
    </template>
  </div>
</template>
