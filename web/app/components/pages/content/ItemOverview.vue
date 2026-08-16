<script setup lang="ts">
import type { ItemDetailRecord } from '~/types/models/item'
import type { ItemFamily } from '~/types/requests/directory-request'

const props = defineProps<{ item: ItemDetailRecord; family: ItemFamily }>()

const definition = computed(() => props.item.item)
const properties = computed(() => props.item.properties)
const behaviorAvailability = computed(() => props.item.behaviorAvailability)
const typeLabel = computed(() => {
  if (!definition.value.itemParentTypeName) return definition.value.itemTypeDisplayName
  return `${definition.value.itemParentTypeDisplayName ?? definition.value.itemParentTypeName} › ${definition.value.itemTypeDisplayName}`
})
const identityFields = computed(() => {
  const fields = [
    { label: 'Type', value: typeLabel.value },
    { label: 'Material', value: definition.value.itemMaterialDisplayName },
    { label: 'Icon', value: definition.value.icon }
  ]
  if (props.family !== 'material') fields.splice(1, 0, { label: 'Action', value: definition.value.itemActionDisplayName })
  if (['armor', 'weapon', 'arrow', 'etc'].includes(props.family)) {
    fields.splice(2, 0,
      { label: 'Body part', value: definition.value.itemBodyPartDisplayName },
      { label: 'Crystal type', value: definition.value.itemCrystalTypeDisplayName })
  }
  if (['potion', 'recipe', 'enchant', 'scroll', 'pet-collar', 'etc'].includes(props.family))
    fields.push({ label: 'Handler', value: definition.value.handlerDisplayName })
  return fields
})
const familyFields = computed(() => {
  const common = [
    { label: 'Price', value: definition.value.price },
    { label: 'Weight', value: definition.value.weight }
  ]
  const family = {
    armor: [{ label: 'Crystal count', value: properties.value.crystalCount }],
    weapon: [
      { label: 'Display ID', value: properties.value.displayId },
      { label: 'Crystal count', value: properties.value.crystalCount },
      { label: 'Soulshots', value: properties.value.soulshots },
      { label: 'Spiritshots', value: properties.value.spiritshots },
      { label: 'MP consume', value: properties.value.mpConsume },
      { label: 'Reduced MP consume', value: properties.value.reducedMpConsume },
      { label: 'Reuse delay', value: properties.value.reuseDelay }
    ],
    arrow: [], material: [],
    potion: [{ label: 'Reuse delay', value: properties.value.reuseDelay }],
    recipe: [{ label: 'Recipe ID', value: properties.value.recipeId }],
    enchant: [], scroll: [],
    'pet-collar': [{ label: 'Use condition', value: properties.value.useCondition }],
    etc: [
      { label: 'Display ID', value: properties.value.displayId },
      { label: 'Reuse delay', value: properties.value.reuseDelay },
      { label: 'Primary skill', value: properties.value.itemSkill },
      { label: 'Use condition', value: properties.value.useCondition }
    ]
  }[props.family]
  return [...common, ...family]
})
const flags = computed(() => {
  const all = {
    elementEnabled: { label: 'Element enabled', value: properties.value.elementEnabled },
    enchantEnabled: { label: 'Enchant enabled', value: behaviorAvailability.value?.enchantEnabled ?? null },
    forNpc: { label: 'For NPC', value: behaviorAvailability.value?.forNpc ?? null },
    immediateEffect: { label: 'Immediate effect', value: behaviorAvailability.value?.immediateEffect ?? null },
    isAttackWeapon: { label: 'Attack weapon', value: properties.value.isAttackWeapon },
    isForceEquip: { label: 'Force equip', value: properties.value.isForceEquip },
    isDepositable: { label: 'Depositable', value: behaviorAvailability.value?.isDepositable ?? null },
    isDestroyable: { label: 'Destroyable', value: behaviorAvailability.value?.isDestroyable ?? null },
    isDropable: { label: 'Dropable', value: behaviorAvailability.value?.isDropable ?? null },
    isMagicWeapon: { label: 'Magic weapon', value: properties.value.isMagicWeapon },
    isOlyRestricted: { label: 'Olympiad restricted', value: behaviorAvailability.value?.isOlyRestricted ?? null },
    isQuestItem: { label: 'Quest item', value: properties.value.isQuestItem },
    isSellable: { label: 'Sellable', value: behaviorAvailability.value?.isSellable ?? null },
    isStackable: { label: 'Stackable', value: behaviorAvailability.value?.isStackable ?? null },
    isTradable: { label: 'Tradable', value: behaviorAvailability.value?.isTradable ?? null },
    useWeaponSkillsOnly: { label: 'Weapon skills only', value: properties.value.useWeaponSkillsOnly }
  }
  const keys = {
    armor: ['enchantEnabled', 'forNpc', 'immediateEffect', 'isDepositable', 'isDestroyable', 'isDropable', 'isSellable', 'isTradable'],
    weapon: ['elementEnabled', 'enchantEnabled', 'forNpc', 'immediateEffect', 'isAttackWeapon', 'isForceEquip', 'isDepositable', 'isDestroyable', 'isDropable', 'isMagicWeapon', 'isSellable', 'isTradable', 'useWeaponSkillsOnly'],
    arrow: ['immediateEffect', 'isStackable'], material: ['immediateEffect', 'isStackable'],
    potion: ['forNpc', 'immediateEffect', 'isOlyRestricted', 'isStackable'],
    recipe: ['immediateEffect', 'isDepositable', 'isDestroyable', 'isDropable', 'isSellable', 'isStackable', 'isTradable'],
    enchant: ['immediateEffect', 'isOlyRestricted', 'isStackable'],
    scroll: ['forNpc', 'isOlyRestricted', 'isStackable'],
    'pet-collar': ['isOlyRestricted'],
    etc: ['forNpc', 'immediateEffect', 'isDepositable', 'isDestroyable', 'isDropable', 'isOlyRestricted', 'isQuestItem', 'isSellable', 'isStackable', 'isTradable']
  }[props.family] as (keyof typeof all)[]
  return keys.map(key => all[key])
})

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
        <div v-for="field in identityFields" :key="field.label" class="space-y-1 p-5">
          <dt class="text-xs font-medium uppercase tracking-wide text-muted">{{ field.label }}</dt>
          <dd class="text-sm text-highlighted">{{ display(field.value) }}</dd>
        </div>
      </dl>
    </UCard>

    <div class="grid gap-6 xl:grid-cols-2">
      <UCard>
        <h2 class="text-sm font-semibold text-highlighted">Economy and family properties</h2>
        <dl class="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3">
          <div v-for="field in familyFields" :key="field.label" class="rounded-md bg-muted/40 px-3 py-2">
            <dt class="text-xs font-medium text-muted">{{ field.label }}</dt><dd class="mt-1 text-sm text-highlighted">{{ display(field.value) }}</dd>
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
