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
      <UCard><dl class="grid grid-cols-1 gap-x-6 gap-y-3 text-sm md:grid-cols-2"><template v-for="[label, value] in [['Type', item.itemTypeDisplayName], ['Action', item.itemActionDisplayName], ['Handler', item.handlerDisplayName], ['Body part', item.itemBodyPartDisplayName], ['Material', item.itemMaterialDisplayName], ['Crystal type', item.itemCrystalTypeDisplayName], ['Icon', item.icon], ['Weight', item.weight], ['Price', item.price], ['Weapon type', item.weaponType], ['Armor type', item.armorType], ['Etc item type', item.etcItemType]]" :key="label"><dt class="text-muted">{{ label }}</dt><dd class="font-medium text-highlighted">{{ value ?? '—' }}</dd></template></dl></UCard>
      <UCard v-if="item.skills.length"><template #header>Item skills</template><div class="overflow-x-auto"><table class="w-full text-left text-sm"><thead class="border-b border-default text-muted"><tr><th class="p-2">Skill</th><th class="p-2">Level</th><th class="p-2">Type</th><th class="p-2">Chance</th></tr></thead><tbody><tr v-for="skill in item.skills" :key="`${skill.skillId}-${skill.skillLevel}`" class="border-b border-default"><td class="p-2 font-medium text-highlighted">{{ skill.skillName ? `${skill.skillName} (#${skill.skillId})` : `#${skill.skillId}` }}</td><td class="p-2">{{ skill.skillLevel }}</td><td class="p-2">{{ skill.itemSkillTypeDisplayName ?? '—' }}</td><td class="p-2">{{ skill.chance == null ? '—' : `${skill.chance}%` }}</td></tr></tbody></table></div></UCard>
      <UCard v-if="item.attackGeometry"><template #header>Client attack geometry</template><dl class="grid grid-cols-1 gap-x-6 gap-y-3 text-sm md:grid-cols-2"><template v-for="[label, value] in [['Start offset X', item.attackGeometry.offsetX], ['Start offset Y', item.attackGeometry.offsetY], ['Sweep radius', item.attackGeometry.radius], ['Forward length', item.attackGeometry.length]]" :key="label"><dt class="text-muted">{{ label }}</dt><dd class="font-medium text-highlighted">{{ value }}</dd></template></dl></UCard>
      <UCard v-if="item.stats"><template #header>Statistics</template><pre class="text-xs">{{ item.stats }}</pre></UCard>
    </template>
  </div>
</template>
