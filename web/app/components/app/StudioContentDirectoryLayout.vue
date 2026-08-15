<script setup lang="ts">
import type {
  ContentImportMode,
  ContentImportTarget,
  ImportJob
} from '../../types/models/import-job'
import { getImportJob, getImportJobs, startContentImport } from '../../services/studio-api'
import { selectedGameVersionKey } from '../../utils/game-version'
import { importJobProgressItem } from '../../utils/import-progress'

const props = withDefaults(defineProps<{
  title: string
  description: string
  icon: string
  importTarget?: ContentImportTarget
  importLabel?: string
  loading?: boolean
  error?: string
  eyebrow?: string
}>(), {
  importTarget: undefined,
  importLabel: undefined,
  loading: false,
  error: undefined,
  eyebrow: 'Game content'
})

const emit = defineEmits<{
  refresh: []
  importComplete: [job: ImportJob]
}>()

const modalOpen = ref(false)
const selectedMode = ref<ContentImportMode>('add_missing')
const latestJob = ref<ImportJob>()
const queueing = ref(false)
const statusError = ref<string>()
const dismissedJobId = ref<string>()
const drawerOpen = ref(false)
const toast = useStudioToasts()
let pollTimer: ReturnType<typeof setTimeout> | undefined

const active = computed(() => latestJob.value
  ? ['queued', 'discovering', 'running'].includes(latestJob.value.status)
  : false)
const importSupported = computed(() => {
  if (!props.importTarget) return false
  const version = selectedGameVersionKey()
  return version === 'c1' ||
    ['npc-types', 'npc-races', 'npc-sexes'].includes(props.importTarget) &&
    ['c4', 'interlude'].includes(version)
})
const progressItems = computed(() => {
  const job = latestJob.value
  if (!job || job.id === dismissedJobId.value) return []
  return [importJobProgressItem(job, props.importLabel ?? props.title)]
})

function dismissalKey() {
  return props.importTarget
    ? `studio-import-dismissed:${selectedGameVersionKey()}:${props.importTarget}`
    : undefined
}

function restoreDismissal() {
  const key = dismissalKey()
  dismissedJobId.value = key ? localStorage.getItem(key) ?? undefined : undefined
}

function dismissStatus() {
  if (!latestJob.value) return
  dismissedJobId.value = latestJob.value.id
  const key = dismissalKey()
  if (key) localStorage.setItem(key, latestJob.value.id)
}

function updateDrawerOpen(open: boolean) {
  drawerOpen.value = open
  if (!open) dismissStatus()
}

async function loadLatest(schedule = true) {
  if (!props.importTarget) return
  try {
    const page = await getImportJobs({ category: 'content', target: props.importTarget, pageSize: 1 })
    const job = page.items[0]
    latestJob.value = job
    if (job && dismissedJobId.value && job.id !== dismissedJobId.value) clearDismissal()
    statusError.value = undefined
    if (job && job.id !== dismissedJobId.value && ['queued', 'discovering', 'running'].includes(job.status))
      drawerOpen.value = true
    if (schedule && active.value) schedulePoll()
  } catch {
    statusError.value = 'The latest import status could not be loaded.'
  }
}

function schedulePoll() {
  clearTimeout(pollTimer)
  pollTimer = setTimeout(() => void poll(), 1000)
}

async function poll() {
  const current = latestJob.value
  if (!current || !active.value) return
  try {
    const next = await getImportJob(current.id)
    latestJob.value = next
    statusError.value = undefined
    if (['queued', 'discovering', 'running'].includes(next.status)) schedulePoll()
    else if (next.status === 'succeeded' || next.status === 'succeeded_with_warnings') {
      emit('importComplete', next)
      emit('refresh')
    }
  } catch {
    statusError.value = 'The active import status could not be refreshed.'
  }
}

function openImport() {
  selectedMode.value = 'add_missing'
  modalOpen.value = true
}

async function queueImport() {
  if (!props.importTarget) return
  queueing.value = true
  try {
    latestJob.value = await startContentImport(props.importTarget, selectedMode.value)
    modalOpen.value = false
    clearDismissal()
    drawerOpen.value = true
    schedulePoll()
  } catch {
    toast.error({
      title: `${props.importLabel ?? props.title} import could not be queued`,
      description: 'Another related import may already be active.'
    })
  } finally {
    queueing.value = false
  }
}

function clearDismissal() {
  dismissedJobId.value = undefined
  const key = dismissalKey()
  if (key) localStorage.removeItem(key)
}

function refreshPage() {
  emit('refresh')
  void loadLatest()
}

watch(() => props.importTarget, () => {
  clearTimeout(pollTimer)
  drawerOpen.value = false
  restoreDismissal()
  void loadLatest()
})
onMounted(() => {
  restoreDismissal()
  void loadLatest()
})
onUnmounted(() => clearTimeout(pollTimer))
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      :eyebrow="eyebrow"
      :title="title"
      :description="description"
      :icon="icon"
    >
      <template #actions>
        <UButton
          v-if="importTarget"
          :label="`Import ${importLabel ?? title.toLowerCase()}`"
          icon="i-lucide-download"
          :disabled="!importSupported || active || queueing"
          @click="openImport"
        />
        <slot name="actions" />
        <UButton
          label="Refresh"
          icon="i-lucide-refresh-cw"
          color="neutral"
          variant="outline"
          :loading="loading"
          @click="refreshPage"
        />
      </template>
    </StudioPageHeader>

    <UAlert
      v-if="statusError"
      color="error"
      variant="subtle"
      title="Import status unavailable"
      :description="statusError"
    />

    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="Directory unavailable"
      :description="error"
    />

    <slot name="alerts" />
    <slot />

    <StudioImportProgressDrawer
      :open="drawerOpen"
      :items="progressItems"
      @update:open="updateDrawerOpen"
    />

    <UModal v-model:open="modalOpen" :title="`Import ${importLabel ?? title.toLowerCase()}`">
      <template #body>
        <div class="space-y-5">
          <p class="text-sm text-muted">Choose how source catalog records should be reconciled.</p>
          <div class="grid gap-3">
            <button
              type="button"
              class="rounded-lg border p-4 text-left"
              :class="selectedMode === 'add_missing' ? 'border-primary bg-primary/5' : 'border-default'"
              @click="selectedMode = 'add_missing'"
            >
              <span class="font-medium text-highlighted">Import missing</span>
              <span class="mt-1 block text-sm text-muted">Add missing source records and preserve every existing value.</span>
            </button>
            <button
              type="button"
              class="rounded-lg border p-4 text-left"
              :class="selectedMode === 'restore_defaults' ? 'border-warning bg-warning/5' : 'border-default'"
              @click="selectedMode = 'restore_defaults'"
            >
              <span class="font-medium text-highlighted">Restore defaults</span>
              <span class="mt-1 block text-sm text-muted">Add missing records and overwrite source-backed values. Custom-only records are preserved.</span>
            </button>
          </div>
          <div class="flex justify-end gap-3">
            <UButton label="Cancel" color="neutral" variant="outline" @click="modalOpen = false" />
            <UButton
              label="Start import"
              :color="selectedMode === 'restore_defaults' ? 'warning' : 'primary'"
              :loading="queueing"
              @click="queueImport"
            />
          </div>
        </div>
      </template>
    </UModal>
  </div>
</template>
