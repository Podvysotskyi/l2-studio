<script setup lang="ts">
import type { StorageEntry, StorageKind, StorageUploadProgress } from '../../../types/models/storage'
import { StorageRequestError } from '../../../types/models/storage'
import {
  createStorageFolder,
  deleteStorageEntry,
  getStorageEntries,
  moveStorageEntry,
  storageDownloadUrl,
  uploadStorageFile
} from '../../../services/storage-api'
import {
  storageSortOptions,
  type StorageSort,
  type StorageUploadItem,
  visibleStorageEntries
} from '../../../utils/storage-browser'
import { storageUploadPath } from '../../../utils/storage-upload'
import { paginate } from '../../../utils/directory'

const props = defineProps<{ kind: StorageKind }>()

const kind = computed(() => props.kind)
const currentPath = ref('')
const entries = ref<StorageEntry[]>([])
const query = ref('')
const sort = ref<StorageSort>('name-asc')
const page = ref(1)
const pageSize = ref(25)
const loading = ref(false)
const loadError = ref<string>()
const uploads = ref<StorageUploadItem[]>([])
const uploadDrawerOpen = ref(false)
const selectedPaths = ref<string[]>([])
const fileInput = useTemplateRef<HTMLInputElement>('fileInput')
const folderInput = useTemplateRef<HTMLInputElement>('folderInput')
const dialogs = useStudioDialogs()
const notifications = useStudioToasts()

const writable = computed(() => kind.value === 'resources')
const visibleEntries = computed(() => visibleStorageEntries(entries.value, query.value, sort.value))
const displayedEntries = computed(() => paginate(visibleEntries.value, page.value, pageSize.value))
const selectedEntries = computed(() =>
  entries.value.filter(entry => selectedPaths.value.includes(entry.path))
)
const activeUploads = computed(() =>
  uploads.value.filter(item => item.status === 'queued' || item.status === 'uploading')
)
const resourceStorageDescription = computed(() =>
  `Upload destination: ${currentPath.value ? `/${currentPath.value}` : 'version root'}`
)
const storagePage = computed(() => kind.value === 'resources'
  ? {
      title: 'Original resources',
      description: 'Manage original resources for the selected game version.',
      icon: 'i-lucide-archive'
    }
  : {
      title: 'Generated assets',
      description: 'Inspect generated assets for the selected game version.',
      icon: 'i-lucide-package-open'
    }
)
const uploadMenuItems = computed(() => [[
  {
    label: 'Choose files',
    description: 'Upload one or more files to this folder.',
    icon: 'i-lucide-files',
    onSelect: () => chooseFiles(false)
  },
  {
    label: 'Choose folder',
    description: 'Upload the contents of a folder.',
    icon: 'i-lucide-folder-up',
    onSelect: () => chooseFiles(true)
  }
]])

watch(kind, () => {
  currentPath.value = ''
  query.value = ''
  page.value = 1
  selectedPaths.value = []
  void load()
})

async function load() {
  loading.value = true
  loadError.value = undefined
  try {
    const listing = await getStorageEntries(kind.value, currentPath.value)
    entries.value = listing.entries
    selectedPaths.value = selectedPaths.value.filter(path =>
      listing.entries.some(entry => entry.path === path)
    )
  } catch (reason) {
    entries.value = []
    loadError.value = requestMessage(reason, 'Storage contents could not be loaded.')
  } finally {
    loading.value = false
  }
}

function navigate(path: string) {
  currentPath.value = path
  query.value = ''
  page.value = 1
  selectedPaths.value = []
  void load()
}

function download(entry: StorageEntry) {
  const anchor = document.createElement('a')
  anchor.href = storageDownloadUrl(kind.value, entry.path)
  anchor.download = entry.name
  anchor.click()
}

async function createFolder() {
  const name = await dialogs.prompt({
    title: 'Create folder',
    description: currentPath.value
      ? `Add a folder inside ${currentPath.value}.`
      : 'Add a folder at the version root.',
    label: 'Folder name',
    confirmLabel: 'Create folder'
  })
  if (!name) return
  await runMutation(
    () => createStorageFolder(childPath(name)),
    'The folder could not be created.',
    'Folder created'
  )
}

async function rename(entry: StorageEntry) {
  const name = await dialogs.prompt({
    title: `Rename ${entry.type}`,
    description: entry.path,
    label: 'New name',
    initialValue: entry.name,
    confirmLabel: 'Rename'
  })
  if (!name || name === entry.name) return
  const parent = parentPath(entry.path)
  await moveWithConflict(entry.path, parent ? `${parent}/${name}` : name)
}

async function moveWithConflict(path: string, destination: string) {
  try {
    await moveStorageEntry(path, destination)
  } catch (reason) {
    const conflict = requestStatus(reason) === 409
    const replace = conflict && await dialogs.confirm({
      title: 'Replace existing entry?',
      description: `An entry already exists at ${destination}. Replacing it cannot be undone.`,
      confirmLabel: 'Replace',
      confirmColor: 'error'
    })
    if (replace) {
      await runMutation(
        () => moveStorageEntry(path, destination, true),
        'The entry could not be moved.',
        'Entry moved'
      )
      return
    }
    if (conflict) return
    notifications.error({
      title: 'Entry could not be moved',
      description: requestMessage(reason, 'Try the move again.')
    })
    return
  }
  await load()
  notifications.success({ title: 'Entry moved' })
}

async function remove(entry: StorageEntry) {
  const confirmed = await dialogs.confirm({
    title: `Delete ${entry.type}?`,
    description: `Permanently delete ${entry.path}? This cannot be undone.`,
    confirmLabel: 'Delete',
    confirmColor: 'error'
  })
  if (!confirmed) return
  await runMutation(
    () => deleteStorageEntry(entry.path),
    'The entry could not be deleted.',
    'Entry deleted'
  )
}

function selectEntry(path: string, selected: boolean) {
  selectedPaths.value = selected
    ? [...new Set([...selectedPaths.value, path])]
    : selectedPaths.value.filter(value => value !== path)
}

function selectVisible(paths: string[], selected: boolean) {
  if (selected) {
    selectedPaths.value = [...new Set([...selectedPaths.value, ...paths])]
    return
  }
  selectedPaths.value = selectedPaths.value.filter(path => !paths.includes(path))
}

async function removeSelected() {
  const selected = selectedEntries.value
  if (!selected.length) return
  const confirmed = await dialogs.confirm({
    title: `Delete ${selected.length} selected ${selected.length === 1 ? 'entry' : 'entries'}?`,
    description: 'Permanently delete the selected files and folders? This cannot be undone.',
    confirmLabel: `Delete ${selected.length} ${selected.length === 1 ? 'entry' : 'entries'}`,
    confirmColor: 'error'
  })
  if (!confirmed) return

  let failure: unknown
  try {
    await Promise.all(selected.map(entry => deleteStorageEntry(entry.path)))
    selectedPaths.value = []
  } catch (reason) {
    failure = reason
  }
  await load()
  if (failure) {
    notifications.error({
      title: 'Some entries could not be deleted',
      description: requestMessage(failure, 'Try the delete again.')
    })
    return
  }
  notifications.success({ title: 'Selected entries deleted' })
}

async function runMutation(action: () => Promise<unknown>, fallback: string, successTitle: string) {
  try {
    await action()
    await load()
    notifications.success({ title: successTitle })
  } catch (reason) {
    notifications.error({
      title: fallback,
      description: requestMessage(reason, 'Try the action again.')
    })
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
  await queueUploads(selected, preservePaths)
}

async function queueDroppedFiles(files: File[]) {
  await queueUploads(files, false)
}

async function queueUploads(files: File[], preservePaths: boolean) {
  if (!files.length || activeUploads.value.length) return
  uploads.value = files.map((file, index) => ({
    id: `${Date.now()}-${index}`,
    path: storageUploadPath(
      currentPath.value,
      file.name,
      preservePaths ? file.webkitRelativePath : undefined
    ),
    file,
    loaded: 0,
    total: file.size,
    status: 'queued'
  }))
  uploadDrawerOpen.value = true
  await runUploadQueue()
  await load()
}

function rejectFolderDrop() {
  notifications.error({
    title: 'Dropped folders are not supported',
    description: 'Use Upload → Choose folder to upload folder contents.'
  })
}

async function runUploadQueue() {
  let next = 0
  let replaceAll = false
  let pendingConflictDecision: Promise<boolean | 'all'> | undefined

  async function resolveConflict(item: StorageUploadItem): Promise<boolean | 'all'> {
    if (pendingConflictDecision) {
      const decision = await pendingConflictDecision
      return decision === true ? resolveConflict(item) : decision
    }
    const decision = dialogs.confirm({
      title: 'Replace existing file?',
      description: `A file already exists at ${item.path}. Replacing it cannot be undone.`,
      confirmLabel: 'Replace',
      alternativeLabel: 'Replace all',
      confirmColor: 'error'
    })
    pendingConflictDecision = decision
    try {
      return await decision
    } finally {
      if (pendingConflictDecision === decision) pendingConflictDecision = undefined
    }
  }

  async function worker() {
    while (next < uploads.value.length) {
      const item = uploads.value[next++]!
      item.status = 'uploading'
      try {
        await uploadStorageFile(item.path, item.file, progress => updateProgress(item, progress), replaceAll)
        item.loaded = item.total
        item.status = 'complete'
      } catch (reason) {
        const decision = reason instanceof StorageRequestError && reason.status === 409
          ? await resolveConflict(item)
          : false
        if (decision) {
          if (decision === 'all') replaceAll = true
          try {
            await uploadStorageFile(item.path, item.file, progress => updateProgress(item, progress), true)
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

function updateProgress(item: StorageUploadItem, progress: StorageUploadProgress) {
  item.loaded = progress.loaded
  item.total = progress.total
}

function childPath(name: string) {
  return currentPath.value ? `${currentPath.value}/${name}` : name
}

function parentPath(path: string) {
  return path.split('/').slice(0, -1).join('/')
}

function requestStatus(reason: unknown) {
  if (reason instanceof StorageRequestError) return reason.status
  if (reason && typeof reason === 'object' && 'statusCode' in reason) return Number(reason.statusCode)
  return 0
}

function requestMessage(reason: unknown, fallback: string) {
  if (reason instanceof Error && reason.message) return reason.message
  return fallback
}

onMounted(() => void load())
watch(query, () => { page.value = 1 })
watch([visibleEntries, pageSize], () => {
  const lastPage = Math.max(1, Math.ceil(visibleEntries.value.length / pageSize.value))
  if (page.value > lastPage) page.value = lastPage
})
</script>

<template>
  <div class="space-y-5">
    <StudioPageHeader
      eyebrow="Storage"
      :title="storagePage.title"
      :description="storagePage.description"
      :icon="storagePage.icon"
    />

    <input ref="fileInput" class="hidden" type="file" multiple @change="selectedFiles($event, false)">
    <input ref="folderInput" class="hidden" type="file" multiple webkitdirectory @change="selectedFiles($event, true)">

    <div class="flex items-center gap-2 text-sm text-muted">
      <UIcon :name="writable ? 'i-lucide-pencil-line' : 'i-lucide-lock-keyhole'" class="size-4" />
      <span>{{ writable ? resourceStorageDescription : 'Generated assets are read-only.' }}</span>
    </div>

    <UAlert
      v-if="loadError"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      title="Storage operation failed"
      :description="loadError"
    />

    <StudioStorageExplorer
      v-model:page="page"
      v-model:page-size="pageSize"
      :entries="displayedEntries"
      :total-entries="visibleEntries.length"
      :current-path="currentPath"
      :loading="loading"
      :writable="writable"
      :query="query"
      :selected-paths="selectedPaths"
      @navigate="navigate"
      @refresh="load"
      @download="download"
      @rename="rename"
      @remove="remove"
      @remove-selected="removeSelected"
      @select-entry="selectEntry"
      @select-all="selectVisible"
      @update:query="query = $event"
      @drop-files="queueDroppedFiles"
      @reject-folder-drop="rejectFolderDrop"
      @choose-files="chooseFiles(false)"
      @create-folder="createFolder"
    >
      <template #toolbar>
        <div class="flex flex-wrap items-center gap-2">
          <UInput
            :model-value="query"
            icon="i-lucide-search"
            placeholder="Search this folder"
            aria-label="Search this folder"
            class="min-w-48 flex-1 sm:max-w-xs"
            @update:model-value="query = String($event)"
          />
          <USelect
            v-model="sort"
            :items="storageSortOptions"
            aria-label="Sort entries"
            class="w-40"
          />
          <template v-if="writable">
            <UButton
              label="New folder"
              icon="i-lucide-folder-plus"
              color="neutral"
              variant="outline"
              :disabled="Boolean(activeUploads.length)"
              @click="createFolder"
            />
            <UDropdownMenu :items="uploadMenuItems" :content="{ align: 'end' }">
              <UButton
                label="Upload"
                icon="i-lucide-upload"
                trailing-icon="i-lucide-chevron-down"
                :disabled="Boolean(activeUploads.length)"
              />
            </UDropdownMenu>
          </template>
        </div>
      </template>
    </StudioStorageExplorer>

    <StudioStorageUploadDrawer v-model:open="uploadDrawerOpen" :uploads="uploads" />
  </div>
</template>
