<script setup lang="ts">
import type { StorageEntry } from '../../../types/models/storage'
import { droppedStorageFiles } from '../../../utils/storage-browser'

const props = defineProps<{
  entries: StorageEntry[]
  totalEntries: number
  currentPath: string
  loading: boolean
  writable: boolean
  query: string
  selectedPaths: string[]
}>()

const emit = defineEmits<{
  navigate: [path: string]
  refresh: []
  download: [entry: StorageEntry]
  rename: [entry: StorageEntry]
  remove: [entry: StorageEntry]
  'remove-selected': []
  'select-entry': [path: string, selected: boolean]
  'select-all': [paths: string[], selected: boolean]
  'update:query': [value: string]
  'drop-files': [files: File[]]
  'reject-folder-drop': []
  'choose-files': []
  'create-folder': []
}>()

const dragDepth = ref(0)
const dragActive = ref(false)
const breadcrumbs = computed(() => {
  const parts = props.currentPath ? props.currentPath.split('/') : []
  return [
    { label: 'Version root', path: '' },
    ...parts.map((label, index) => ({
      label,
      path: parts.slice(0, index + 1).join('/')
    }))
  ]
})
const selectedCount = computed(() => props.selectedPaths.length)
const allVisibleSelected = computed(() =>
  props.entries.length > 0 && props.entries.every(entry =>
    props.selectedPaths.includes(entry.path)
  )
)
const someVisibleSelected = computed(() =>
  props.entries.some(entry => props.selectedPaths.includes(entry.path)) &&
  !allVisibleSelected.value
)

function parentPath(path: string) {
  return path.split('/').slice(0, -1).join('/')
}

function open(entry: StorageEntry) {
  if (entry.type === 'directory') emit('navigate', entry.path)
}

function desktopMenu(entry: StorageEntry) {
  return [[
    ...(entry.type === 'directory'
      ? [{ label: 'Open', icon: 'i-lucide-folder-open', onSelect: () => open(entry) }]
      : []),
    ...(props.writable
      ? [{ label: 'Rename', icon: 'i-lucide-pencil', onSelect: () => emit('rename', entry) }]
      : [])
  ], [
    ...(props.writable
      ? [{
          label: 'Delete',
          icon: 'i-lucide-trash-2',
          color: 'error' as const,
          onSelect: () => emit('remove', entry)
        }]
      : [])
  ]].filter(group => group.length)
}

function mobileMenu(entry: StorageEntry) {
  return [[
    ...(entry.type === 'directory'
      ? [{ label: 'Open', icon: 'i-lucide-folder-open', onSelect: () => open(entry) }]
      : [{
          label: 'Download',
          icon: 'i-lucide-download',
          onSelect: () => emit('download', entry)
        }]),
    ...(props.writable
      ? [{ label: 'Rename', icon: 'i-lucide-pencil', onSelect: () => emit('rename', entry) }]
      : [])
  ], [
    ...(props.writable
      ? [{
          label: 'Delete',
          icon: 'i-lucide-trash-2',
          color: 'error' as const,
          onSelect: () => emit('remove', entry)
        }]
      : [])
  ]].filter(group => group.length)
}

function handleDragEnter(event: DragEvent) {
  if (!props.writable || !event.dataTransfer?.types.includes('Files')) return
  event.preventDefault()
  dragDepth.value++
  dragActive.value = true
}

function handleDragOver(event: DragEvent) {
  if (!props.writable || !event.dataTransfer?.types.includes('Files')) return
  event.preventDefault()
  event.dataTransfer.dropEffect = 'copy'
}

function handleDragLeave(event: DragEvent) {
  if (!props.writable) return
  event.preventDefault()
  dragDepth.value = Math.max(0, dragDepth.value - 1)
  if (!dragDepth.value) dragActive.value = false
}

function handleDrop(event: DragEvent) {
  if (!props.writable || !event.dataTransfer) return
  event.preventDefault()
  dragDepth.value = 0
  dragActive.value = false
  const dropped = droppedStorageFiles(event.dataTransfer)
  if (dropped.containsDirectory) emit('reject-folder-drop')
  if (dropped.files.length) emit('drop-files', dropped.files)
}

function formatSize(size: number | null) {
  if (size === null) return 'Folder'
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

function formatModified(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(new Date(value))
}
</script>

<template>
  <UCard
    class="relative overflow-hidden"
    :ui="{ body: 'p-0 sm:p-0' }"
    @dragenter="handleDragEnter"
    @dragover="handleDragOver"
    @dragleave="handleDragLeave"
    @drop="handleDrop"
  >
    <template #header>
      <div class="space-y-3">
        <div class="flex min-w-0 items-center justify-between gap-3">
          <nav aria-label="Storage path" class="flex min-w-0 items-center gap-1 overflow-hidden">
            <template v-for="(item, index) in breadcrumbs" :key="item.path">
              <UIcon
                v-if="index"
                name="i-lucide-chevron-right"
                class="size-3 shrink-0 text-muted"
              />
              <UButton
                :label="item.label"
                color="neutral"
                variant="ghost"
                size="xs"
                class="min-w-0 shrink"
                :ui="{ label: 'truncate' }"
                @click="emit('navigate', item.path)"
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
            @click="emit('refresh')"
          />
        </div>

        <slot name="toolbar" />

        <div
          v-if="writable && selectedCount"
          class="flex flex-wrap items-center justify-between gap-2 rounded-lg bg-primary/5 px-3 py-2 ring-1 ring-primary/15"
        >
          <p class="text-sm font-medium text-highlighted">
            {{ selectedCount }} {{ selectedCount === 1 ? 'entry' : 'entries' }} selected
          </p>
          <div class="flex items-center gap-1">
            <UButton
              label="Clear"
              color="neutral"
              variant="ghost"
              size="sm"
              @click="emit('select-all', selectedPaths, false)"
            />
            <UButton
              label="Delete"
              icon="i-lucide-trash-2"
              color="error"
              variant="soft"
              size="sm"
              @click="emit('remove-selected')"
            />
          </div>
        </div>
      </div>
    </template>

    <div class="relative min-h-80">
      <div
        v-if="dragActive"
        class="absolute inset-2 z-20 grid place-items-center rounded-xl border-2 border-dashed border-primary bg-default/95 p-6 text-center"
      >
        <div>
          <span class="mx-auto grid size-12 place-items-center rounded-xl bg-primary/10 text-primary">
            <UIcon name="i-lucide-cloud-upload" class="size-6" />
          </span>
          <p class="mt-3 font-semibold text-highlighted">Drop files to upload</p>
          <p class="mt-1 text-sm text-muted">
            Destination: {{ currentPath ? `/${currentPath}` : 'version root' }}
          </p>
        </div>
      </div>

      <div
        v-if="loading"
        class="grid min-h-72 place-items-center gap-3 p-8 text-center"
        role="status"
        aria-live="polite"
      >
        <div>
          <UIcon
            name="i-lucide-loader-circle"
            class="mx-auto size-6 animate-spin text-primary"
          />
          <p class="mt-3 text-sm text-muted">Loading directory…</p>
        </div>
      </div>

      <template v-else-if="entries.length">
        <div class="hidden overflow-x-auto sm:block">
          <table class="w-full text-left text-sm">
            <thead class="border-b border-default bg-elevated/50 text-xs text-muted">
              <tr>
                <th class="px-4 py-3 font-medium">
                  <div class="flex items-center gap-3">
                    <UCheckbox
                      v-if="writable"
                      :model-value="allVisibleSelected"
                      :indeterminate="someVisibleSelected"
                      aria-label="Select all visible entries"
                      @update:model-value="emit('select-all', entries.map(entry => entry.path), Boolean($event))"
                    />
                    <span>Name</span>
                  </div>
                </th>
                <th class="px-4 py-3 font-medium">Size</th>
                <th class="px-4 py-3 font-medium">Modified</th>
                <th class="w-32 px-4 py-3 text-right font-medium">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-default">
              <tr v-if="currentPath" class="hover:bg-elevated/30">
                <td colspan="4" class="px-4 py-2.5">
                  <button
                    type="button"
                    class="flex items-center gap-2 text-sm text-muted hover:text-highlighted"
                    @click="emit('navigate', parentPath(currentPath))"
                  >
                    <UIcon name="i-lucide-corner-left-up" class="size-4" />
                    Parent directory
                  </button>
                </td>
              </tr>
              <tr
                v-for="entry in entries"
                :key="entry.path"
                class="h-13 hover:bg-elevated/30"
                :class="selectedPaths.includes(entry.path) ? 'bg-primary/5' : ''"
              >
                <td class="max-w-md px-4 py-2.5">
                  <div class="flex max-w-full items-center gap-3">
                    <UCheckbox
                      v-if="writable"
                      :model-value="selectedPaths.includes(entry.path)"
                      :aria-label="`Select ${entry.name}`"
                      @update:model-value="emit('select-entry', entry.path, Boolean($event))"
                    />
                    <button
                      type="button"
                      class="flex min-w-0 flex-1 items-center gap-3 text-left"
                      :class="entry.type === 'directory' ? 'cursor-pointer' : 'cursor-default'"
                      @click="open(entry)"
                    >
                      <span
                        class="grid size-8 shrink-0 place-items-center rounded-lg"
                        :class="entry.type === 'directory' ? 'bg-primary/10 text-primary' : 'bg-elevated text-muted'"
                      >
                        <UIcon
                          :name="entry.type === 'directory' ? 'i-lucide-folder' : 'i-lucide-file'"
                          class="size-4"
                        />
                      </span>
                      <span class="truncate font-medium text-highlighted">{{ entry.name }}</span>
                    </button>
                  </div>
                </td>
                <td class="whitespace-nowrap px-4 py-2.5 text-muted">
                  {{ formatSize(entry.size) }}
                </td>
                <td class="whitespace-nowrap px-4 py-2.5 text-muted">
                  {{ formatModified(entry.modifiedAt) }}
                </td>
                <td class="px-4 py-2.5">
                  <div class="flex items-center justify-end gap-1">
                    <UButton
                      v-if="entry.type === 'file'"
                      label="Download"
                      icon="i-lucide-download"
                      color="neutral"
                      variant="ghost"
                      size="sm"
                      @click="emit('download', entry)"
                    />
                    <UDropdownMenu
                      v-if="desktopMenu(entry).length"
                      :items="desktopMenu(entry)"
                      :content="{ align: 'end' }"
                    >
                      <UButton
                        icon="i-lucide-ellipsis"
                        :aria-label="`Actions for ${entry.name}`"
                        color="neutral"
                        variant="ghost"
                        size="sm"
                      />
                    </UDropdownMenu>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="divide-y divide-default sm:hidden">
          <button
            v-if="currentPath"
            type="button"
            class="flex w-full items-center gap-2 px-4 py-3 text-left text-sm text-muted"
            @click="emit('navigate', parentPath(currentPath))"
          >
            <UIcon name="i-lucide-corner-left-up" class="size-4" />
            Parent directory
          </button>
          <div
            v-for="entry in entries"
            :key="entry.path"
            class="flex min-w-0 items-center gap-3 px-4 py-3"
            :class="selectedPaths.includes(entry.path) ? 'bg-primary/5' : ''"
          >
            <UCheckbox
              v-if="writable"
              :model-value="selectedPaths.includes(entry.path)"
              :aria-label="`Select ${entry.name}`"
              @update:model-value="emit('select-entry', entry.path, Boolean($event))"
            />
            <button
              type="button"
              class="flex min-w-0 flex-1 items-center gap-3 text-left"
              @click="open(entry)"
            >
              <span
                class="grid size-9 shrink-0 place-items-center rounded-lg"
                :class="entry.type === 'directory' ? 'bg-primary/10 text-primary' : 'bg-elevated text-muted'"
              >
                <UIcon
                  :name="entry.type === 'directory' ? 'i-lucide-folder' : 'i-lucide-file'"
                  class="size-4"
                />
              </span>
              <span class="min-w-0">
                <span class="block truncate text-sm font-medium text-highlighted">{{ entry.name }}</span>
                <span class="mt-0.5 block truncate text-xs text-muted">
                  {{ formatSize(entry.size) }} · {{ formatModified(entry.modifiedAt) }}
                </span>
              </span>
            </button>
            <UDropdownMenu :items="mobileMenu(entry)" :content="{ align: 'end' }">
              <UButton
                icon="i-lucide-ellipsis-vertical"
                :aria-label="`Actions for ${entry.name}`"
                color="neutral"
                variant="ghost"
                size="sm"
              />
            </UDropdownMenu>
          </div>
        </div>
      </template>

      <div
        v-else
        class="grid min-h-64 place-items-center p-8 text-center"
      >
        <div class="max-w-sm">
          <span class="mx-auto grid size-12 place-items-center rounded-xl bg-elevated text-muted">
            <UIcon
              :name="query ? 'i-lucide-search-x' : 'i-lucide-folder-open'"
              class="size-6"
            />
          </span>
          <p class="mt-3 text-sm font-medium text-highlighted">
            {{ query ? 'No matching entries' : 'This directory is empty' }}
          </p>
          <p class="mt-1 text-xs leading-5 text-muted">
            {{ query
              ? `Nothing in this directory matches “${query}”.`
              : writable
                ? 'Upload files or create a folder to get started.'
                : 'No generated assets are published here.' }}
          </p>
          <div v-if="query || writable" class="mt-4 flex flex-wrap justify-center gap-2">
            <UButton
              v-if="query"
              label="Clear search"
              color="neutral"
              variant="outline"
              size="sm"
              @click="emit('update:query', '')"
            />
            <template v-else-if="writable">
              <UButton
                label="Upload files"
                icon="i-lucide-upload"
                size="sm"
                @click="emit('choose-files')"
              />
              <UButton
                label="New folder"
                icon="i-lucide-folder-plus"
                color="neutral"
                variant="outline"
                size="sm"
                @click="emit('create-folder')"
              />
            </template>
          </div>
        </div>
      </div>

      <p
        v-if="!loading && totalEntries && query"
        class="border-t border-default px-4 py-2 text-xs text-muted"
        aria-live="polite"
      >
        Showing {{ entries.length }} of {{ totalEntries }} entries
      </p>
    </div>
  </UCard>
</template>
