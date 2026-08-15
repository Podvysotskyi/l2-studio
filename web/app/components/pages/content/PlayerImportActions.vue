<script setup lang="ts">
import {
  getPlayerImportRun,
  getPlayerImportRuns,
  startPlayerImport
} from '../../../services/studio-api'
import type { PlayerImportMode, PlayerImportRun } from '../../../types/models/player-import'

const run = ref<PlayerImportRun>()
const importing = ref<PlayerImportMode>()
const statusError = ref<string>()
const toast = useStudioToasts()
const dialogs = useStudioDialogs()
const activeRun = computed(() => run.value && ['queued', 'running'].includes(run.value.status))
let pollTimer: ReturnType<typeof setTimeout> | undefined

async function importPlayers(mode: PlayerImportMode) {
  if (mode === 'restore_defaults') {
    const confirmed = await dialogs.confirm({
      title: 'Restore default player catalog?',
      description: 'Edited player classes and appearance option names will be restored. Extra records are preserved.',
      confirmLabel: 'Restore defaults',
      confirmColor: 'warning'
    })
    if (!confirmed) return
  }
  importing.value = mode
  statusError.value = undefined
  try {
    run.value = await startPlayerImport(mode)
    toast.success({ title: 'Player catalog import queued' })
    schedulePoll()
  } catch {
    toast.error({ title: mode === 'restore_defaults' ? 'Player defaults could not be restored' : 'Player catalog import could not be queued' })
  } finally {
    importing.value = undefined
  }
}

async function loadLatestRun(schedule = true) {
  try {
    run.value = (await getPlayerImportRuns())[0]
    statusError.value = undefined
    if (schedule && activeRun.value) schedulePoll()
  } catch {
    statusError.value = 'The latest player import status could not be loaded.'
  }
}

function schedulePoll() {
  clearTimeout(pollTimer)
  pollTimer = setTimeout(() => void pollRun(), 1000)
}

async function pollRun() {
  if (!run.value) return
  try {
    run.value = await getPlayerImportRun(run.value.id)
    statusError.value = undefined
    if (activeRun.value) schedulePoll()
  } catch {
    statusError.value = 'The active player import status could not be refreshed.'
  }
}

onMounted(() => void loadLatestRun())
onUnmounted(() => clearTimeout(pollTimer))
</script>

<template>
  <div class="space-y-3">
    <div class="flex flex-wrap gap-2">
      <UButton
        label="Import missing"
        icon="i-lucide-download"
        :loading="importing === 'add_missing'"
        :disabled="Boolean(activeRun) || Boolean(importing)"
        @click="importPlayers('add_missing')"
      />
      <UButton
        label="Restore defaults"
        color="neutral"
        variant="outline"
        :loading="importing === 'restore_defaults'"
        :disabled="Boolean(activeRun) || Boolean(importing)"
        @click="importPlayers('restore_defaults')"
      />
    </div>
    <UAlert
      v-if="statusError || run"
      :color="run?.status === 'failed' ? 'error' : 'neutral'"
      variant="subtle"
      :title="statusError || `Latest player import: ${run?.status}`"
      :description="run?.error || `${run?.insertedCount ?? 0} inserted, ${run?.restoredCount ?? 0} restored of ${run?.totalCount ?? 0}.`"
    />
  </div>
</template>
