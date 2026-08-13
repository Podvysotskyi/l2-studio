<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import type { NpcRecord } from '../../../types/models/content-directory'

const query = defineModel<string>('query', { required: true })
const page = defineModel<number>('page', { required: true })
const pageSize = defineModel<number>('pageSize', { required: true })

defineProps<{
  items: NpcRecord[]
  total: number
  loading: boolean
  error?: string
}>()

defineEmits<{ refresh: [] }>()

const columns: TableColumn<NpcRecord>[] = [
  { accessorKey: 'id', header: 'ID' },
  { accessorKey: 'name', header: 'NPC' },
  { accessorKey: 'level', header: 'Level' },
  { accessorKey: 'npcType', header: 'Type' },
  { accessorKey: 'npcRace', header: 'Race' },
  { accessorKey: 'npcSex', header: 'Sex' }
]
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Game content"
      title="NPC definitions"
      description="Browse normalized NPC records and the lookup values that classify their server behavior."
      icon="i-lucide-users-round"
    >
      <template #actions>
        <UButton
          label="Refresh"
          icon="i-lucide-refresh-cw"
          color="neutral"
          variant="outline"
          :loading="loading"
          @click="$emit('refresh')"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="NPC directory unavailable"
      :description="error"
    >
      <template #actions>
        <UButton color="error" variant="soft" size="sm" @click="$emit('refresh')">
          Try again
        </UButton>
      </template>
    </UAlert>

    <UCard v-else :ui="{ body: 'p-0 sm:p-0' }">
      <div
        class="flex flex-wrap items-center justify-between gap-4 border-b border-default px-4 py-3"
      >
        <div>
          <p class="text-sm font-medium text-highlighted">NPC catalog</p>
          <p class="text-xs text-muted">
            {{ total.toLocaleString() }} definitions
          </p>
        </div>
        <UInput
          v-model="query"
          icon="i-lucide-search"
          placeholder="Search NPC name"
          aria-label="Search NPC name"
          maxlength="100"
          class="w-full sm:w-80"
        />
      </div>

      <div class="overflow-x-auto">
        <UTable
          :data="items"
          :columns="columns"
          :loading="loading"
          empty="No NPC definitions match this search."
          class="min-w-[58rem]"
        >
          <template #id-cell="{ row }">
            <code class="text-xs text-muted">{{ row.original.id }}</code>
          </template>
          <template #name-cell="{ row }">
            <div class="flex items-center gap-3">
              <span
                class="grid size-8 shrink-0 place-items-center rounded-lg bg-elevated"
              >
                <UIcon name="i-lucide-user-round" class="size-4 text-muted" />
              </span>
              <span class="font-medium text-highlighted">
                {{ row.original.name ?? 'Unnamed NPC' }}
              </span>
            </div>
          </template>
          <template #level-cell="{ row }">
            <UBadge color="neutral" variant="subtle">
              {{ row.original.level }}
            </UBadge>
          </template>
          <template #npcType-cell="{ row }">
            <div>
              <span class="text-sm">{{ row.original.npcTypeDisplayName }}</span>
              <span class="ml-2 text-xs text-dimmed">{{ row.original.npcTypeName }}</span>
            </div>
          </template>
          <template #npcRace-cell="{ row }">
            <div>
              <span class="text-sm">{{
                row.original.npcRaceDisplayName ?? 'No race'
              }}</span>
              <span
                v-if="row.original.npcRaceName !== null"
                class="ml-2 text-xs text-dimmed"
                >{{ row.original.npcRaceName }}</span
              >
            </div>
          </template>
          <template #npcSex-cell="{ row }">
            <div>
              <span class="text-sm">{{ row.original.npcSexDisplayName }}</span>
              <span class="ml-2 text-xs text-dimmed">{{ row.original.npcSexName }}</span>
            </div>
          </template>
        </UTable>
      </div>

      <StudioTableFooter
        v-model:page="page"
        v-model:page-size="pageSize"
        :total="total"
        :page-size-options="[10, 25, 50, 100]"
      />
    </UCard>
  </div>
</template>
