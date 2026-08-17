<script setup lang="ts">
import { getSkillDefinition } from '~/services/studio-api'
import type { SkillRecord } from '~/types/models/content-directory'

const route = useRoute()
const skill = ref<SkillRecord>()
const loading = ref(true)
const error = ref<string>()
const skillId = computed(() => {
  const value = Array.isArray(route.params.id) ? route.params.id[0] : route.params.id
  const id = Number(value)
  return Number.isSafeInteger(id) && id > 0 ? id : undefined
})

async function load() {
  if (skillId.value === undefined) {
    skill.value = undefined
    loading.value = false
    error.value = 'The skill identifier is invalid.'
    return
  }
  loading.value = true
  try {
    skill.value = await getSkillDefinition(skillId.value)
    error.value = undefined
  } catch {
    skill.value = undefined
    error.value = 'The skill definition could not be loaded.'
  } finally {
    loading.value = false
  }
}

watch(skillId, () => void load(), { immediate: true })
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Game content"
      :title="skill?.name ?? 'Skill definition'"
      :description="skill ? `ID: ${skill.id} · ${skill.levels} levels` : 'View a normalized skill definition.'"
      icon="i-lucide-sparkles"
    >
      <template #actions>
        <UButton label="Back to skill definitions" icon="i-lucide-arrow-left" color="neutral" variant="outline" to="/authoring/skills" />
      </template>
    </StudioPageHeader>

    <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" title="Skill definition unavailable" :description="error">
      <template #actions><UButton color="error" variant="soft" size="sm" @click="load">Try again</UButton></template>
    </UAlert>

    <UCard v-else-if="skill">
      <template #header><h2 class="text-sm font-semibold text-highlighted">Definition</h2></template>
      <dl class="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <div class="rounded-md bg-muted/40 p-3"><dt class="text-xs font-medium text-muted">ID</dt><dd class="mt-1 text-sm text-highlighted">#{{ skill.id }}</dd></div>
        <div class="rounded-md bg-muted/40 p-3"><dt class="text-xs font-medium text-muted">Levels</dt><dd class="mt-1 text-sm text-highlighted">{{ skill.levels }}</dd></div>
        <div class="rounded-md bg-muted/40 p-3"><dt class="text-xs font-medium text-muted">Operate type</dt><dd class="mt-1 text-sm text-highlighted">{{ skill.skillOperateTypeDisplayName ?? 'Unassigned' }}</dd></div>
        <div class="rounded-md bg-muted/40 p-3"><dt class="text-xs font-medium text-muted">Target type</dt><dd class="mt-1 text-sm text-highlighted">{{ skill.skillTargetTypeDisplayName ?? 'Unassigned' }}</dd></div>
        <div class="rounded-md bg-muted/40 p-3"><dt class="text-xs font-medium text-muted">Icons</dt><dd class="mt-1 text-sm text-highlighted">{{ skill.iconCount }}</dd></div>
      </dl>
    </UCard>

    <UCard v-else :ui="{ body: 'p-6' }"><USkeleton class="h-24 w-full" /></UCard>
  </div>
</template>
