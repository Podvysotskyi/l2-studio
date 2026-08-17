<script setup lang="ts">
import type { ItemSetRecord } from '~/types/models/item-set'

defineProps<{ itemSet: ItemSetRecord }>()

const statLabels: Array<[keyof NonNullable<ItemSetRecord['stats']>, string]> = [
  ['str', 'STR'], ['dex', 'DEX'], ['con', 'CON'], ['int', 'INT'], ['wit', 'WIT'], ['men', 'MEN']
]
</script>

<template>
  <div class="space-y-5">
    <UCard>
      <template #header><h2 class="text-sm font-semibold text-highlighted">Required equipment</h2></template>
      <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <div v-for="part in itemSet.bodyParts" :key="part.bodyPartName" class="rounded-md bg-muted/40 p-3">
          <p class="text-xs font-medium text-muted">{{ part.bodyPartDisplayName }}</p>
          <NuxtLink v-if="part.itemName" :to="`/authoring/items/armor/${part.itemId}`" class="mt-1 block text-sm text-primary hover:underline">
            {{ part.itemName }} <span class="text-muted">#{{ part.itemId }}</span>
          </NuxtLink>
          <p v-else class="mt-1 text-sm text-muted">Unavailable source item #{{ part.itemId }}</p>
        </div>
      </div>
    </UCard>

    <UCard>
      <template #header><h2 class="text-sm font-semibold text-highlighted">Set skill</h2></template>
      <NuxtLink v-if="itemSet.skill" :to="`/authoring/skills/${itemSet.skill.skillId}`" class="inline-flex items-center gap-2 text-sm text-primary hover:underline">
        <span>{{ itemSet.skill.skillName ?? `Skill #${itemSet.skill.skillId}` }}</span>
        <span class="text-muted">#{{ itemSet.skill.skillId }} · Level {{ itemSet.skill.skillLevel }}</span>
      </NuxtLink>
      <p v-else class="text-sm text-muted">No set skill is configured.</p>
    </UCard>

    <UCard>
      <template #header><h2 class="text-sm font-semibold text-highlighted">Set stats</h2></template>
      <dl v-if="itemSet.stats" class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <template v-for="[key, label] in statLabels" :key="key">
          <div v-if="itemSet.stats[key] != null" class="rounded-md bg-muted/40 p-3">
            <dt class="text-xs font-medium text-muted">{{ label }}</dt>
            <dd class="mt-1 text-sm text-highlighted">{{ itemSet.stats[key]! > 0 ? '+' : '' }}{{ itemSet.stats[key] }}</dd>
          </div>
        </template>
      </dl>
      <p v-else class="text-sm text-muted">This set has no base-stat modifiers.</p>
    </UCard>
  </div>
</template>
