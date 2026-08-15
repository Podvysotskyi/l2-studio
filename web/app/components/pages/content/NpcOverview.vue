<script setup lang="ts">
import { computed } from 'vue'
import type { NpcRecord } from '~/types/models/content-directory'

const props = defineProps<{
  npc: NpcRecord
}>()

const statusFields = computed(() => props.npc.status
  ? [
      { label: 'Attackable', enabled: props.npc.status.attackable },
      { label: 'Targetable', enabled: props.npc.status.targetable },
      { label: 'Talkable', enabled: props.npc.status.talkable },
      { label: 'Undying', enabled: props.npc.status.undying },
      { label: 'Show name', enabled: props.npc.status.showName },
      { label: 'Random walk', enabled: props.npc.status.randomWalk },
      { label: 'Can move', enabled: props.npc.status.canMove },
      { label: 'No sleep mode', enabled: props.npc.status.noSleepMode },
      { label: 'Can be sown', enabled: props.npc.status.canBeSown }
    ]
  : [])

const display = (value: string | number | null) => value ?? '—'

</script>

<template>
  <div class="space-y-6">
    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <dl class="grid divide-y divide-default sm:grid-cols-2 sm:divide-x sm:divide-y-0">
        <div class="space-y-1 p-5"><dt class="text-xs font-medium uppercase tracking-wide text-muted">Type</dt><dd class="text-sm text-highlighted">{{ npc.npcTypeDisplayName }} <span class="text-muted">({{ npc.npcTypeName }})</span></dd></div>
        <div class="space-y-1 border-t border-default p-5 sm:border-t-0"><dt class="text-xs font-medium uppercase tracking-wide text-muted">Race</dt><dd class="text-sm text-highlighted">{{ npc.npcRaceDisplayName ?? 'No race' }}<span v-if="npc.npcRaceName" class="text-muted"> ({{ npc.npcRaceName }})</span></dd></div>
        <div class="space-y-1 border-t border-default p-5 sm:border-t-0"><dt class="text-xs font-medium uppercase tracking-wide text-muted">Sex</dt><dd class="text-sm text-highlighted">{{ npc.npcSexDisplayName }} <span class="text-muted">({{ npc.npcSexName }})</span></dd></div>
      </dl>
    </UCard>

    <UCard>
      <div>
        <h2 class="text-sm font-semibold text-highlighted">Status</h2>
        <p class="mt-1 text-xs text-muted">Imported NPC behavior flags.</p>
      </div>
      <dl v-if="statusFields.length" class="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <div v-for="field in statusFields" :key="field.label" class="flex items-center justify-between gap-3 rounded-md bg-muted/40 px-3 py-2">
          <dt class="text-sm text-highlighted">{{ field.label }}</dt>
          <dd><UBadge :color="field.enabled ? 'success' : 'neutral'" variant="subtle" :icon="field.enabled ? 'i-lucide-circle-check' : 'i-lucide-circle-x'">{{ field.enabled ? 'Enabled' : 'Disabled' }}</UBadge></dd>
        </div>
      </dl>
      <p v-else class="mt-4 text-sm text-muted">Status data has not been imported for this NPC.</p>
    </UCard>

    <div v-if="npc.stats || npc.statsVitals || npc.statsAttack || npc.statsDefence || npc.statsSpeed" class="grid gap-6 xl:grid-cols-2">
      <UCard v-if="npc.stats">
        <h2 class="text-sm font-semibold text-highlighted">Attributes</h2>
        <dl class="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3">
          <div v-for="field in [['STR', npc.stats.str], ['INT', npc.stats.int], ['DEX', npc.stats.dex], ['WIT', npc.stats.wit], ['CON', npc.stats.con], ['MEN', npc.stats.men]]" :key="field[0]" class="rounded-md bg-muted/40 px-3 py-2"><dt class="text-xs font-medium text-muted">{{ field[0] }}</dt><dd class="mt-1 text-sm text-highlighted">{{ display(field[1] as number | null) }}</dd></div>
        </dl>
      </UCard>

      <UCard v-if="npc.statsVitals">
        <h2 class="text-sm font-semibold text-highlighted">Vitals</h2>
        <dl class="mt-4 grid grid-cols-2 gap-3">
          <div v-for="field in [['HP', npc.statsVitals.hp], ['HP regen', npc.statsVitals.hpRegen], ['MP', npc.statsVitals.mp], ['MP regen', npc.statsVitals.mpRegen]]" :key="field[0]" class="rounded-md bg-muted/40 px-3 py-2"><dt class="text-xs font-medium text-muted">{{ field[0] }}</dt><dd class="mt-1 text-sm text-highlighted">{{ display(field[1] as number | null) }}</dd></div>
        </dl>
      </UCard>

      <UCard v-if="npc.statsAttack || npc.stats?.hitTime != null">
        <h2 class="text-sm font-semibold text-highlighted">Attack</h2>
        <dl class="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3">
          <div v-for="field in [['Physical', npc.statsAttack?.physical ?? null], ['Magical', npc.statsAttack?.magical ?? null], ['Random', npc.statsAttack?.random ?? null], ['Critical', npc.statsAttack?.critical ?? null], ['Accuracy', npc.statsAttack?.accuracy ?? null], ['Attack speed', npc.statsAttack?.attackSpeed ?? null], ['Reuse delay', npc.statsAttack?.reuseDelay ?? null], ['Hit time (ms)', npc.stats?.hitTime ?? null], ['Type', npc.statsAttack?.type ?? null], ['Range', npc.statsAttack?.range ?? null], ['Distance', npc.statsAttack?.distance ?? null], ['Width', npc.statsAttack?.width ?? null]]" :key="field[0]" class="rounded-md bg-muted/40 px-3 py-2"><dt class="text-xs font-medium text-muted">{{ field[0] }}</dt><dd class="mt-1 text-sm text-highlighted">{{ display(field[1] as string | number | null) }}</dd></div>
        </dl>
      </UCard>

      <UCard v-if="npc.statsDefence">
        <h2 class="text-sm font-semibold text-highlighted">Defence</h2>
        <dl class="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3">
          <div v-for="field in [['Physical', npc.statsDefence.physical], ['Magical', npc.statsDefence.magical], ['Evasion', npc.statsDefence.evasion], ['Shield', npc.statsDefence.shield], ['Shield rate', npc.statsDefence.shieldRate]]" :key="field[0]" class="rounded-md bg-muted/40 px-3 py-2"><dt class="text-xs font-medium text-muted">{{ field[0] }}</dt><dd class="mt-1 text-sm text-highlighted">{{ display(field[1] as number | null) }}</dd></div>
        </dl>
      </UCard>

      <UCard v-if="npc.statsSpeed">
        <h2 class="text-sm font-semibold text-highlighted">Speed</h2>
        <dl class="mt-4 grid grid-cols-2 gap-3">
          <div v-for="field in [['Walk (ground)', npc.statsSpeed.walkGround], ['Run (ground)', npc.statsSpeed.runGround]]" :key="field[0]" class="rounded-md bg-muted/40 px-3 py-2"><dt class="text-xs font-medium text-muted">{{ field[0] }}</dt><dd class="mt-1 text-sm text-highlighted">{{ display(field[1] as number | null) }}</dd></div>
        </dl>
      </UCard>
    </div>
  </div>
</template>
