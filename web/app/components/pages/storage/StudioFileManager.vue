<script setup lang="ts">
import type {
  StorageEntry,
  StorageKind,
  StorageUploadProgress
} from '../../../types/models/storage'
import { StorageRequestError } from '../../../types/models/storage'
import {
  createStorageFolder,
  deleteStorageEntry,
  getStorageEntries,
  moveStorageEntry,
  storageDownloadUrl,
  uploadStorageFile
} from '../../../services/storage-api'

interface UploadItem {
  id: string
  path: string
  file: File
  loaded: number
  total: number
  status: 'queued' | 'uploading' | 'complete' | 'failed'
  error?: string
}

const kind = ref<StorageKind>('resources')
const currentPath = ref('')
const entries = ref<StorageEntry[]>([])
const loading = ref(false)
const error = ref<string>()
const uploads = ref<UploadItem[]>([])
const fileInput = useTemplateRef<HTMLInputElement>('fileInput')
const folderInput = useTemplateRef<HTMLInputElement>('folderInput')

const writable = computed(() => kind.value === 'resources')
const breadcrumbs = computed(() => {
  const parts = currentPath.value ? currentPath.value.split('/') : []
  return [
    { label: 'Version root', path: '' },
    ...parts.map((label, index) => ({
      label,
      path: parts.slice(0, index + 1).join('/')
    }))
  ]
})
const activeUploads = computed(() =>
  uploads.value.filter(item => item.status === 'queued' || item.status === 'uploading')
)
const uploadProgress = computed(() => {
  const total = uploads.value.reduce((sum, item) => sum + item.total, 0)
  const loaded = uploads.value.reduce((sum, item) => sum + item.loaded, 0)
  return total ? Math.round((loaded / total) * 100) : 0
})

watch(kind, () => {
  currentPath.value = ''
  uploads.value = []
  void load()
})

async function load() {
  loading.value = true
  error.value = undefined
  try {
    const listing = await getStorageEntries(kind.value, currentPath.value)
    entries.value = listing.entries
  } catch (reason) {
    entries.value = []
    error.value = requestMessage(reason, 'Storage contents could not be loaded.')
  } finally {
    loading.value = false
  }
}

function open(entry: StorageEntry) {
  if (entry.type === 'directory') {
    currentPath.value = entry.path
    void load()
    return
  }
  download(entry)
}

function navigate(path: string) {
  currentPath.value = path
  void load()
}

function download(entry: StorageEntry) {
  const anchor = document.createElement('a')
  anchor.href = storageDownloadUrl(kind.value, entry.path)
  anchor.download = entry.name
  anchor.click()
}

async function createFolder() {
  const name = window.prompt('Folder name')?.trim()
  if (!name) return
  await runMutation(
    () => createStorageFolder(childPath(name)),
    'The folder could not be created.'
  )
}

async function rename(entry: StorageEntry) {
  const name = window.prompt('New name', entry.name)?.trim()
  if (!name || name === entry.name) return
  const parent = parentPath(entry.path)
  await moveWithConflict(entry.path, parent ? `${parent}/${name}` : name)
}

async function move(entry: StorageEntry) {
  const destination = window
    .prompt('Destination path from the selected version root', entry.path)
    ?.trim()
  if (!destination || destination === entry.path) return
  await moveWithConflict(entry.path, destination)
}

async function moveWithConflict(path: string, destination: string) {
  try {
    await moveStorageEntry(path, destination)
  } catch (reason) {
    if (
      requestStatus(reason) === 409 &&
      window.confirm(`Replace the existing entry at ${destination}?`)
    ) {
      await runMutation(
        () => moveStorageEntry(path, destination, true),
        'The entry could not be moved.'
      )
      return
    }
    error.value = requestMessage(reason, 'The entry could not be moved.')
    return
  }
  await load()
}

async function remove(entry: StorageEntry) {
  if (!window.confirm(`Permanently delete ${entry.path}?`)) return
  await runMutation(
    () => deleteStorageEntry(entry.path),
    'The entry could not be deleted.'
  )
}

async function runMutation(action: () => Promise<unknown>, fallback: string) {
  error.value = undefined
  try {
    await action()
    await load()
  } catch (reason) {
    error.value = requestMessage(reason, fallback)
  }
}

function chooseFiles(folder: boolean) {
  if (activeUploads.value.length) return
  ;(folder ? folderInput.value : fileInput.value)?.click()
}

async function selectedFiles(event: Event, preservePaths: boolean) {
  const input = event.target as HTMLInputElement
  const selected = Array.from(input.files ?? [])
  input.value = ''
  if (!selected.length) return

  uploads.value = selected.map((file, index) => {
    const relativePath = preservePaths
      ? file.webkitRelativePath || file.name
      : file.name
    return {
      id: `${Date.now()}-${index}`,
      path: childPath(relativePath),
      file,
      loaded: 0,
      total: file.size,
      status: 'queued'
    }
  })
  await runUploadQueue()
  await load()
}

async function runUploadQueue() {
  let next = 0
  async function worker() {
    while (next < uploads.value.length) {
      const item = uploads.value[next++]!
      item.status = 'uploading'
      try {
        await uploadStorageFile(item.path, item.file, progress =>
          updateProgress(item, progress)
        )
        item.loaded = item.total
        item.status = 'complete'
      } catch (reason) {
        if (
          reason instanceof StorageRequestError &&
          reason.status === 409 &&
          window.confirm(`Replace the existing file at ${item.path}?`)
        ) {
          try {
            await uploadStorageFile(
              item.path,
              item.file,
              progress => updateProgress(item, progress),
              true
            )
            item.loaded = item.total
            item.status = 'complete'
            continue
          } catch (retryReason) {
            reason = retryReason
          }
        }
        item.status = 'failed'
        item.error = requestMessage(reason, 'Upload failed.')
      }
    }
  }
  await Promise.all(Array.from({ length: Math.min(3, uploads.value.length) }, worker))
}

function updateProgress(item: UploadItem, progress: StorageUploadProgress) {
  item.loaded = progress.loaded
  item.total = progress.total
}

function childPath(name: string) {
  return currentPath.value ? `${currentPath.value}/${name}` : name
}

function parentPath(path: string) {
  return path.split('/').slice(0, -1).join('/')
}

function formatSize(size: number | null) {
  if (size === null) return '—'
  if (size < 1024) return `${size} B`
  const units = ['KB', 'MB', 'GB', 'TB']
  let value = size / 1024
  let unit = 0
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024
    unit++
  }
  return `${value.toFixed(value >= 10 ? 1 : 2)} ${units[unit]}`
}

function requestStatus(reason: unknown) {
  if (reason instanceof StorageRequestError) return reason.status
  if (reason && typeof reason === 'object' && 'statusCode' in reason)
    return Number(reason.statusCode)
  return 0
}

function requestMessage(reason: unknown, fallback: string) {
  if (reason instanceof Error && reason.message) return reason.message
  return fallback
}

onMounted(() => void load())
</script>

<template>
  <div class="space-y-6">
    <StudioPageHeader
      eyebrow="Storage"
      title="File storage"
      description="Upload original game resources and inspect generated assets for the selected game version."
      icon="i-lucide-hard-drive"
    >
      <template v-if="writable" #actions>
        <UButton
          label="New folder"
          icon="i-lucide-folder-plus"
          color="neutral"
          variant="outline"
          :disabled="Boolean(activeUploads.length)"
          @click="createFolder"
        />
        <UButton
          label="Upload files"
          icon="i-lucide-upload"
          color="neutral"
          variant="outline"
          :disabled="Boolean(activeUploads.length)"
          @click="chooseFiles(false)"
        />
        <UButton
          label="Upload folder"
          icon="i-lucide-folder-up"
          :disabled="Boolean(activeUploads.length)"
          @click="chooseFiles(true)"
        />
      </template>
    </StudioPageHeader>

    <input
      ref="fileInput"
      class="hidden"
      type="file"
      multiple
      @change="selectedFiles($event, false)"
    >
    <input
      ref="folderInput"
      class="hidden"
      type="file"
      multiple
      webkitdirectory
      @change="selectedFiles($event, true)"
    >

    <div class="flex gap-2">
      <UButton
        label="Original resources"
        icon="i-lucide-archive"
        :variant="kind === 'resources' ? 'solid' : 'outline'"
        :color="kind === 'resources' ? 'primary' : 'neutral'"
        @click="kind = 'resources'"
      />
      <UButton
        label="Generated assets"
        icon="i-lucide-package-open"
        :variant="kind === 'assets' ? 'solid' : 'outline'"
        :color="kind === 'assets' ? 'primary' : 'neutral'"
        @click="kind = 'assets'"
      />
    </div>

    <UAlert
      v-if="kind === 'assets'"
      color="info"
      variant="subtle"
      icon="i-lucide-shield-check"
      title="Generated assets are read-only"
      description="Use asset imports to publish or remove generated outputs without desynchronizing the Studio catalog."
    />
    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="Storage operation failed"
      :description="error"
    />

    <UCard v-if="uploads.length" variant="subtle">
      <div class="flex items-center justify-between gap-4">
        <div>
          <p class="text-sm font-medium text-highlighted">
            {{ activeUploads.length ? 'Uploading resources' : 'Upload finished' }}
          </p>
          <p class="text-xs text-muted">
            {{ uploads.filter(item => item.status === 'complete').length }} completed ·
            {{ uploads.filter(item => item.status === 'failed').length }} failed
          </p>
        </div>
        <span class="text-sm tabular-nums text-muted">{{ uploadProgress }}%</span>
      </div>
      <UProgress class="mt-3" :model-value="uploadProgress" :max="100" />
      <div class="mt-3 max-h-36 space-y-1 overflow-y-auto text-xs">
        <div
          v-for="item in uploads"
          :key="item.id"
          class="flex items-center gap-2"
          :class="item.status === 'failed' ? 'text-error' : 'text-muted'"
        >
          <UIcon
            :name="
              item.status === 'complete'
                ? 'i-lucide-circle-check'
                : item.status === 'failed'
                  ? 'i-lucide-circle-x'
                  : 'i-lucide-loader-circle'
            "
            class="size-3.5 shrink-0"
            :class="item.status === 'uploading' ? 'animate-spin' : ''"
          />
          <span class="truncate">{{ item.path }}</span>
          <span v-if="item.error" class="ml-auto">{{ item.error }}</span>
        </div>
      </div>
    </UCard>

    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <template #header>
        <div class="flex flex-wrap items-center justify-between gap-3">
          <nav aria-label="Storage path" class="flex min-w-0 flex-wrap items-center gap-1">
            <template v-for="(item, index) in breadcrumbs" :key="item.path">
              <UIcon v-if="index" name="i-lucide-chevron-right" class="size-3 text-muted" />
              <UButton
                :label="item.label"
                color="neutral"
                variant="ghost"
                size="xs"
                @click="navigate(item.path)"
              />
            </template>
          </nav>
          <UButton
            icon="i-lucide-refresh-cw"
            aria-label="Refresh storage"
            color="neutral"
            variant="ghost"
            size="sm"
            :loading="loading"
            @click="load"
          />
        </div>
      </template>

      <div class="min-h-80 overflow-x-auto">
        <table class="w-full text-left text-sm">
          <thead class="border-b border-default bg-elevated/50 text-xs text-muted">
            <tr>
              <th class="px-4 py-3 font-medium">Name</th>
              <th class="px-4 py-3 font-medium">Size</th>
              <th class="px-4 py-3 font-medium">Modified</th>
              <th class="px-4 py-3 text-right font-medium">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-default">
            <tr v-if="currentPath">
              <td colspan="4" class="px-4 py-2">
                <button
                  type="button"
                  class="flex items-center gap-2 text-muted hover:text-highlighted"
                  @click="navigate(parentPath(currentPath))"
                >
                  <UIcon name="i-lucide-corner-left-up" class="size-4" />
                  Parent directory
                </button>
              </td>
            </tr>
            <tr v-for="entry in entries" :key="entry.path" class="hover:bg-elevated/30">
              <td class="max-w-md px-4 py-3">
                <button
                  type="button"
                  class="flex max-w-full items-center gap-3 text-left"
                  @dblclick="open(entry)"
                  @click="entry.type === 'directory' && open(entry)"
                >
                  <UIcon
                    :name="entry.type === 'directory' ? 'i-lucide-folder' : 'i-lucide-file'"
                    class="size-4 shrink-0"
                    :class="entry.type === 'directory' ? 'text-primary' : 'text-muted'"
                  />
                  <span class="truncate font-medium text-highlighted">{{ entry.name }}</span>
                </button>
              </td>
              <td class="whitespace-nowrap px-4 py-3 text-muted">
                {{ formatSize(entry.size) }}
              </td>
              <td class="whitespace-nowrap px-4 py-3 text-muted">
                {{ new Date(entry.modifiedAt).toLocaleString() }}
              </td>
              <td class="px-4 py-3">
                <div class="flex justify-end gap-1">
                  <UButton
                    v-if="entry.type === 'file'"
                    icon="i-lucide-download"
                    :aria-label="`Download ${entry.name}`"
                    color="neutral"
                    variant="ghost"
                    size="xs"
                    @click="download(entry)"
                  />
                  <template v-if="writable">
                    <UButton
                      icon="i-lucide-pencil"
                      :aria-label="`Rename ${entry.name}`"
                      color="neutral"
                      variant="ghost"
                      size="xs"
                      @click="rename(entry)"
                    />
                    <UButton
                      icon="i-lucide-folder-input"
                      :aria-label="`Move ${entry.name}`"
                      color="neutral"
                      variant="ghost"
                      size="xs"
                      @click="move(entry)"
                    />
                    <UButton
                      icon="i-lucide-trash-2"
                      :aria-label="`Delete ${entry.name}`"
                      color="error"
                      variant="ghost"
                      size="xs"
                      @click="remove(entry)"
                    />
                  </template>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
        <div
          v-if="!loading && entries.length === 0"
          class="grid min-h-56 place-items-center p-8 text-center"
        >
          <div>
            <UIcon name="i-lucide-folder-open" class="mx-auto size-8 text-muted" />
            <p class="mt-3 text-sm font-medium text-highlighted">This directory is empty</p>
            <p class="mt-1 text-xs text-muted">
              {{ writable ? 'Upload files or create a folder to get started.' : 'No generated assets are published here.' }}
            </p>
          </div>
        </div>
      </div>
    </UCard>
  </div>
</template>
