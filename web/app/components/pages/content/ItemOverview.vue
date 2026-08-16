<script setup lang="ts">
import type { ItemDetailRecord } from '~/types/models/item'

const props = defineProps<{ item: ItemDetailRecord }>()

const definition = computed(() => props.item.item)
const properties = computed(() => props.item.properties)
const typeLabel = computed(() => {
  if (!definition.value.itemParentTypeName) return definition.value.itemTypeDisplayName
  return `${definition.value.itemParentTypeDisplayName ?? definition.value.itemParentTypeName} › ${definition.value.itemTypeDisplayName}`
})
const flags = computed(() => [
  { label: 'Element enabled', value: properties.value.elementEnabled },
  { label: 'Enchant enabled', value: properties.value.enchantEnabled },
  { label: 'For NPC', value: properties.value.forNpc },
  { label: 'Immediate effect', value: properties.value.immediateEffect },
  { label: 'Attack weapon', value: properties.value.isAttackWeapon },
  { label: 'Force equip', value: properties.value.isForceEquip },
  { label: 'Depositable', value: properties.value.isDepositable },
  { label: 'Destroyable', value: properties.value.isDestroyable },
  { label: 'Dropable', value: properties.value.isDropable },
  { label: 'Magic weapon', value: properties.value.isMagicWeapon },
  { label: 'Olympiad restricted', value: properties.value.isOlyRestricted },
  { label: 'Quest item', value: properties.value.isQuestItem },
  { label: 'Sellable', value: properties.value.isSellable },
  { label: 'Stackable', value: properties.value.isStackable },
  { label: 'Tradable', value: properties.value.isTradable }
])

function display(value: string | number | null | undefined) {
  return value ?? '—'
}

function state(value: boolean | null) {
  if (value === null) return { label: 'Unspecified', color: 'neutral' as const, icon: 'i-lucide-minus' }
  return value
    ? { label: 'Enabled', color: 'success' as const, icon: 'i-lucide-circle-check' }
    : { label: 'Disabled', color: 'error' as const, icon: 'i-lucide-circle-x' }
}
</script>

<template>
  <div class="space-y-6">
    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <dl class="grid divide-y divide-default sm:grid-cols-2 sm:divide-x sm:divide-y-0">
        <div v-for="[label, value] in [['Type', typeLabel], ['Action', definition.itemActionDisplayName], ['Body part', definition.itemBodyPartDisplayName], ['Material', definition.itemMaterialDisplayName], ['Crystal type', definition.itemCrystalTypeDisplayName], ['Icon', definition.icon], ['Display ID', properties.displayId], ['Recipe ID', properties.recipeId]]" :key="label" class="space-y-1 p-5">
          <dt class="text-xs font-medium uppercase tracking-wide text-muted">{{ label }}</dt>
          <dd class="text-sm text-highlighted">{{ display(value as string | number | null) }}</dd>
        </div>
      </dl>
    </UCard>

    <div class="grid gap-6 xl:grid-cols-2">
      <UCard>
        <h2 class="text-sm font-semibold text-highlighted">Economy and resources</h2>
        <dl class="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3">
          <div v-for="[label, value] in [['Price', definition.price], ['Weight', definition.weight], ['Crystal count', properties.crystalCount], ['Soulshots', properties.soulshots], ['Spiritshots', properties.spiritshots]]" :key="label" class="rounded-md bg-muted/40 px-3 py-2">
            <dt class="text-xs font-medium text-muted">{{ label }}</dt><dd class="mt-1 text-sm text-highlighted">{{ display(value as number | null) }}</dd>
          </div>
        </dl>
      </UCard>

      <UCard v-if="definition.attackGeometry">
        <h2 class="text-sm font-semibold text-highlighted">Client attack geometry</h2>
        <dl class="mt-4 grid grid-cols-2 gap-3">
          <div v-for="[label, value] in [['Start offset X', definition.attackGeometry.offsetX], ['Start offset Y', definition.attackGeometry.offsetY], ['Sweep radius', definition.attackGeometry.radius], ['Forward length', definition.attackGeometry.length]]" :key="label" class="rounded-md bg-muted/40 px-3 py-2">
            <dt class="text-xs font-medium text-muted">{{ label }}</dt><dd class="mt-1 text-sm text-highlighted">{{ value }}</dd>
          </div>
        </dl>
      </UCard>

      <UCard v-if="definition.stats">
        <h2 class="text-sm font-semibold text-highlighted">Combat statistics</h2>
        <dl class="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3">
          <div v-for="[label, value] in [['Accuracy', definition.stats.accuracyCombat], ['Critical rate', definition.stats.criticalRate], ['Physical attack', definition.stats.physicalAttack], ['Magical attack', definition.stats.magicalAttack], ['Physical defence', definition.stats.physicalDefence], ['Magical defence', definition.stats.magicalDefence], ['Attack range', definition.stats.physicalAttackRange], ['Attack speed', definition.stats.physicalAttackSpeed], ['Maximum MP', definition.stats.maximumMp], ['Evasion', definition.stats.evasion], ['Shield rate', definition.stats.shieldRate], ['Shield defence', definition.stats.shieldDefence], ['Random damage', definition.stats.randomDamage]]" :key="label" class="rounded-md bg-muted/40 px-3 py-2">
            <dt class="text-xs font-medium text-muted">{{ label }}</dt><dd class="mt-1 text-sm text-highlighted">{{ display(value as number | null) }}</dd>
          </div>
        </dl>
      </UCard>
    </div>

    <UCard>
      <div>
        <h2 class="text-sm font-semibold text-highlighted">Behavior and availability</h2>
        <p class="mt-1 text-xs text-muted">Imported item flags. Unspecified values are not defined by the source record.</p>
      </div>
      <dl class="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <div v-for="flag in flags" :key="flag.label" class="flex items-center justify-between gap-3 rounded-md bg-muted/40 px-3 py-2">
          <dt class="text-sm text-highlighted">{{ flag.label }}</dt>
          <dd><UBadge :color="state(flag.value).color" variant="subtle" :icon="state(flag.value).icon">{{ state(flag.value).label }}</UBadge></dd>
        </div>
      </dl>
    </UCard>
  </div>
</template>
