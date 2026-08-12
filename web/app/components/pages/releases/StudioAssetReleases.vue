<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import {
  activateAssetRelease,
  cloneAssetRelease,
  createAssetRelease,
  deleteAssetRelease,
  getAssetRelease,
  getAssetReleaseResources,
  getAssetReleases,
  publishAssetRelease,
  refreshAssetRelease,
  retireAssetRelease,
  updateAssetRelease,
  validateAssetRelease
} from '../../../services/studio-api'
import type {
  AssetReleaseDetail,
  AssetReleasePage,
  AssetReleaseResourceOption,
  AssetReleaseStatus,
  AssetReleaseSummary
} from '../../../types/models/asset-release'

const releases = ref<AssetReleasePage>()
const selected = ref<AssetReleaseDetail>()
const loading = ref(false)
const working = ref(false)
const error = ref<string>()
const status = ref<'all' | AssetReleaseStatus>('all')
const createOpen = ref(false)
const cloneSource = ref<string>()
const createName = ref('')
const createNotes = ref('')
const scenes = ref<AssetReleaseResourceOption[]>([])
const audio = ref<AssetReleaseResourceOption[]>([])
const images = ref<AssetReleaseResourceOption[]>([])
const form = ref(emptyForm())
const dialogs = useStudioDialogs()

const statusOptions = [
  { label: 'All releases', value: 'all' },
  { label: 'Drafts', value: 'draft' },
  { label: 'Published', value: 'published' },
  { label: 'Active', value: 'active' },
  { label: 'Retired', value: 'retired' }
]
const sceneItems = computed(() => scenes.value.map(item => ({ label: item.label, value: item.fileId })))
const audioItems = computed(() => audio.value.map(item => ({ label: item.label, value: item.fileId })))
const imageItems = computed(() => images.value.map(item => ({ label: item.label, value: item.fileId })))
const loginCameras = computed(() => cameraItems(form.value.loginSceneFileId))
const characterCameras = computed(() => cameraItems(form.value.characterSelectionSceneFileId))

watch(status, () => void load())

async function load() {
  loading.value = true
  error.value = undefined
  try {
    releases.value = await getAssetReleases({
      ...(status.value === 'all' ? {} : { status: status.value }),
      pageSize: 100
    })
  } catch {
    error.value = 'Asset releases could not be loaded.'
  } finally {
    loading.value = false
  }
}

async function inspect(release: AssetReleaseSummary) {
  error.value = undefined
  try {
    selected.value = await getAssetRelease(release.id)
    form.value = formFrom(selected.value)
    if (release.status === 'draft') await loadResources(release.id)
  } catch {
    error.value = 'The selected release could not be loaded.'
  }
}

async function loadResources(id: string) {
  const [scenePage, audioPage, imagePage] = await Promise.all([
    getAssetReleaseResources(id, 'scene'),
    getAssetReleaseResources(id, 'audio'),
    getAssetReleaseResources(id, 'image')
  ])
  scenes.value = scenePage.items
  audio.value = audioPage.items
  images.value = imagePage.items
}

function openCreate(source?: AssetReleaseSummary) {
  cloneSource.value = source?.id
  createName.value = source ? `${source.name} copy` : ''
  createNotes.value = source?.notes ?? ''
  createOpen.value = true
}

async function create() {
  if (!createName.value.trim()) return
  await run(async () => {
    const result = cloneSource.value
      ? await cloneAssetRelease(cloneSource.value, { name: createName.value, notes: createNotes.value })
      : await createAssetRelease({ name: createName.value, notes: createNotes.value })
    createOpen.value = false
    await load()
    await inspect(result.release)
  })
}

async function save() {
  if (!selected.value) return
  await run(async () => {
    selected.value = await updateAssetRelease(selected.value!.release.id, form.value)
    form.value = formFrom(selected.value)
    await load()
  })
}

async function refreshSnapshot() {
  if (!selected.value) return
  const releaseId = selected.value.release.id
  const confirmed = await dialogs.confirm({
    title: 'Refresh draft snapshot?',
    description: 'Replace this draft snapshot with the current artifact registry.',
    confirmLabel: 'Refresh snapshot'
  })
  if (!confirmed) return
  await run(async () => {
    selected.value = await refreshAssetRelease(releaseId)
    form.value = formFrom(selected.value)
    await loadResources(selected.value.release.id)
    await load()
  })
}

async function validate() {
  if (!selected.value) return
  await run(async () => {
    selected.value = await validateAssetRelease(selected.value!.release.id)
    for (let attempt = 0; attempt < 120 && ['queued', 'running'].includes(selected.value.release.validationStatus); attempt++) {
      await new Promise(resolve => setTimeout(resolve, 1000))
      selected.value = await getAssetRelease(selected.value!.release.id)
    }
    await load()
  })
}

async function publish() {
  if (!selected.value) return
  const releaseId = selected.value.release.id
  const confirmed = await dialogs.confirm({
    title: 'Publish release?',
    description: 'This release will become immutable and can no longer be edited.',
    confirmLabel: 'Publish'
  })
  if (!confirmed) return
  await releaseAction(() => publishAssetRelease(releaseId))
}

async function activate() {
  if (!selected.value) return
  const releaseId = selected.value.release.id
  const confirmed = await dialogs.confirm({
    title: 'Activate release?',
    description: 'Make this the live release for the selected game version.',
    confirmLabel: 'Activate'
  })
  if (!confirmed) return
  await releaseAction(() => activateAssetRelease(releaseId))
}

async function retire() {
  if (!selected.value) return
  const releaseId = selected.value.release.id
  const confirmed = await dialogs.confirm({
    title: 'Retire release?',
    description: 'The release will remain stored but cannot be activated.',
    confirmLabel: 'Retire'
  })
  if (!confirmed) return
  await releaseAction(() => retireAssetRelease(releaseId))
}

async function removeDraft() {
  if (!selected.value) return
  const releaseId = selected.value.release.id
  const confirmed = await dialogs.confirm({
    title: 'Delete draft release?',
    description: 'Permanently delete this draft release? This cannot be undone.',
    confirmLabel: 'Delete draft',
    confirmColor: 'error'
  })
  if (!confirmed) return
  await run(async () => {
    await deleteAssetRelease(releaseId)
    selected.value = undefined
    await load()
  })
}

async function releaseAction(action: () => Promise<AssetReleaseDetail>) {
  await run(async () => {
    selected.value = await action()
    await load()
  })
}

async function reloadSelected() {
  if (!selected.value) return
  await run(async () => {
    selected.value = await getAssetRelease(selected.value!.release.id)
    await load()
  })
}

async function run(action: () => Promise<void>) {
  working.value = true
  error.value = undefined
  try {
    await action()
  } catch (value) {
    error.value = value instanceof Error ? value.message : 'The release operation failed.'
  } finally {
    working.value = false
  }
}

function cameraItems(fileId: number | undefined) {
  return (scenes.value.find(item => item.fileId === fileId)?.cameraSequences ?? [])
    .map(value => ({ label: value, value }))
}

function formFrom(detail: AssetReleaseDetail) {
  return {
    name: detail.release.name,
    notes: detail.release.notes ?? '',
    loginSceneFileId: detail.entrypoints.loginSceneFileId ?? undefined,
    loginCameraSequence: detail.entrypoints.loginCameraSequence ?? undefined,
    loginMusicFileId: detail.entrypoints.loginMusicFileId ?? undefined,
    primaryLogoFileId: detail.entrypoints.primaryLogoFileId ?? undefined,
    versionLogoFileId: detail.entrypoints.versionLogoFileId ?? undefined,
    loadingArtworkFileId: detail.entrypoints.loadingArtworkFileId ?? undefined,
    characterSelectionSceneFileId: detail.entrypoints.characterSelectionSceneFileId ?? undefined,
    characterSelectionCameraSequence: detail.entrypoints.characterSelectionCameraSequence ?? undefined
  }
}

function emptyForm() {
  return {
    name: '', notes: '', loginSceneFileId: undefined as number | undefined,
    loginCameraSequence: undefined as string | undefined, loginMusicFileId: undefined as number | undefined,
    primaryLogoFileId: undefined as number | undefined, versionLogoFileId: undefined as number | undefined,
    loadingArtworkFileId: undefined as number | undefined, characterSelectionSceneFileId: undefined as number | undefined,
    characterSelectionCameraSequence: undefined as string | undefined
  }
}

function formatBytes(value: number) {
  if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`
  return `${(value / 1024 / 1024).toFixed(1)} MB`
}

function badgeColor(value: string): 'success' | 'warning' | 'error' | 'neutral' | 'primary' {
  if (value === 'active' || value === 'valid') return 'success'
  if (value === 'draft' || value === 'queued' || value === 'running') return 'warning'
  if (value === 'retired' || value === 'invalid') return 'error'
  return 'neutral'
}

onMounted(() => void load())
</script>

<template>
  <div class="space-y-5">
    <div class="flex flex-wrap items-end justify-between gap-3">
      <div>
        <h1 class="text-xl font-semibold text-highlighted">Asset releases</h1>
        <p class="mt-1 text-sm text-muted">Build, validate, publish and roll back immutable client asset sets.</p>
      </div>
      <UButton icon="i-lucide-plus" label="New draft" @click="openCreate()" />
    </div>

    <UAlert v-if="error" color="error" icon="i-lucide-circle-alert" :description="error" />

    <UCard>
      <USelect v-model="status" :items="statusOptions" class="w-52" />
    </UCard>

    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <div v-if="loading" class="p-8 text-center text-sm text-muted">Loading releases…</div>
      <div v-else-if="!releases?.items.length" class="p-8 text-center text-sm text-muted">No releases match this filter.</div>
      <div v-else class="divide-y divide-default">
        <button
          v-for="release in releases.items"
          :key="release.id"
          class="grid w-full gap-3 p-4 text-left hover:bg-elevated md:grid-cols-[1fr_auto_auto]"
          @click="inspect(release)"
        >
          <span>
            <span class="flex items-center gap-2">
              <strong class="text-sm text-highlighted">{{ release.name }}</strong>
              <UBadge v-if="release.isDesired && !release.isActive" color="warning" variant="subtle">Activation pending</UBadge>
            </span>
            <span class="mt-1 block text-xs text-muted">{{ release.rootArtifactCount }} current + {{ release.artifactCount - release.rootArtifactCount }} dependencies · {{ formatBytes(release.sizeBytes) }}</span>
          </span>
          <UBadge :color="badgeColor(release.validationStatus)" variant="subtle">{{ release.validationStatus }}</UBadge>
          <UBadge :color="badgeColor(release.status)" variant="subtle">{{ release.status }}</UBadge>
        </button>
      </div>
    </UCard>

    <UModal v-model:open="createOpen" :title="cloneSource ? 'Clone release' : 'Create release draft'">
      <template #body>
        <div class="space-y-4">
          <UFormField label="Release name" required><UInput v-model="createName" class="w-full" /></UFormField>
          <UFormField label="Notes"><UTextarea v-model="createNotes" class="w-full" /></UFormField>
          <UButton block :loading="working" :disabled="!createName.trim()" @click="create">Create draft</UButton>
        </div>
      </template>
    </UModal>

    <USlideover
      :open="Boolean(selected)"
      title="Release management"
      :description="selected?.release.name"
      :ui="{ content: 'max-w-2xl' }"
      @update:open="open => { if (!open) selected = undefined }"
    >
      <template #body>
        <div v-if="selected" class="space-y-6">
          <div class="flex flex-wrap gap-2">
            <UBadge :color="badgeColor(selected.release.status)">{{ selected.release.status }}</UBadge>
            <UBadge :color="badgeColor(selected.release.validationStatus)" variant="subtle">{{ selected.release.validationStatus }}</UBadge>
            <UBadge v-if="selected.release.isDesired && !selected.release.isActive" color="warning">{{ selected.pointerStatus }}</UBadge>
          </div>
          <UAlert v-if="selected.pointerError" color="error" title="Activation failed" :description="selected.pointerError" />

          <template v-if="selected.release.status === 'draft'">
            <div class="grid gap-4 md:grid-cols-2">
              <UFormField label="Name"><UInput v-model="form.name" class="w-full" /></UFormField>
              <UFormField label="Notes"><UInput v-model="form.notes" class="w-full" /></UFormField>
              <UFormField label="Login scene"><USelect v-model="form.loginSceneFileId" :items="sceneItems" class="w-full" /></UFormField>
              <UFormField label="Login camera"><USelect v-model="form.loginCameraSequence" :items="loginCameras" class="w-full" /></UFormField>
              <UFormField label="Character scene"><USelect v-model="form.characterSelectionSceneFileId" :items="sceneItems" class="w-full" /></UFormField>
              <UFormField label="Character camera"><USelect v-model="form.characterSelectionCameraSequence" :items="characterCameras" class="w-full" /></UFormField>
              <UFormField label="Login music"><USelect v-model="form.loginMusicFileId" :items="audioItems" class="w-full" /></UFormField>
              <UFormField label="Primary logo"><USelect v-model="form.primaryLogoFileId" :items="imageItems" class="w-full" /></UFormField>
              <UFormField label="Version logo (optional)"><USelect v-model="form.versionLogoFileId" :items="imageItems" class="w-full" /></UFormField>
              <UFormField label="Loading artwork"><USelect v-model="form.loadingArtworkFileId" :items="imageItems" class="w-full" /></UFormField>
            </div>
            <div class="grid grid-cols-2 gap-2">
              <UButton label="Save draft" icon="i-lucide-save" :loading="working" @click="save" />
              <UButton label="Refresh snapshot" color="neutral" variant="outline" icon="i-lucide-refresh-cw" :loading="working" @click="refreshSnapshot" />
              <UButton label="Validate files" color="neutral" variant="outline" icon="i-lucide-shield-check" :loading="working" @click="validate" />
              <UButton label="Publish" icon="i-lucide-package-check" :disabled="selected.release.validationStatus !== 'valid'" :loading="working" @click="publish" />
            </div>
          </template>

          <div v-else class="grid grid-cols-2 gap-2">
            <UButton v-if="selected.release.status === 'published'" label="Activate / roll back" icon="i-lucide-rocket" :loading="working" @click="activate" />
            <UButton v-if="selected.release.status === 'published'" label="Retire" color="neutral" variant="outline" icon="i-lucide-archive" :loading="working" @click="retire" />
            <UButton label="Clone to draft" color="neutral" variant="outline" icon="i-lucide-copy" @click="openCreate(selected.release)" />
            <UButton label="Refresh status" color="neutral" variant="outline" icon="i-lucide-refresh-cw" :loading="working" @click="reloadSelected" />
          </div>

          <UAlert
            v-for="issue in selected.validationIssues"
            :key="`${issue.code}:${issue.field}:${issue.message}`"
            color="error"
            :title="issue.code"
            :description="issue.message"
          />

          <UCard>
            <dl class="grid grid-cols-[auto_1fr] gap-x-3 gap-y-2 text-sm">
              <dt class="text-muted">Snapshot</dt><dd class="break-all font-mono text-xs">{{ selected.release.snapshotHash }}</dd>
              <dt class="text-muted">Artifacts</dt><dd>{{ selected.release.artifactCount }} ({{ selected.release.rootArtifactCount }} roots)</dd>
              <dt class="text-muted">Manifest</dt><dd class="break-all text-xs">{{ selected.release.manifestPath ?? 'Not published' }}</dd>
            </dl>
          </UCard>

          <section>
            <h2 class="mb-2 text-sm font-semibold text-highlighted">Audit timeline</h2>
            <div v-for="event in selected.events" :key="event.id" class="mb-2 flex justify-between rounded-md border border-default p-3 text-sm">
              <span>{{ event.action.replaceAll('_', ' ') }}</span><span class="text-xs text-muted">{{ new Date(event.occurredAt).toLocaleString() }}</span>
            </div>
          </section>

          <UButton v-if="selected.release.status === 'draft'" block color="error" variant="soft" label="Delete draft" icon="i-lucide-trash-2" @click="removeDraft" />
        </div>
      </template>
    </USlideover>
  </div>
</template>
