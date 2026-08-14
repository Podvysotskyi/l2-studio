<script setup lang="ts">
import {
  isJsonTreeBranch,
  jsonTreeBranchLabel,
  jsonTreeEntries,
  jsonTreePrimitiveLabel
} from '../../utils/json-tree'

defineOptions({ name: 'StudioJsonTree' })

const props = withDefaults(defineProps<{
  value: unknown
  label?: string
  depth?: number
}>(), {
  label: undefined,
  depth: 0
})

const expanded = ref(props.depth === 0)
const branch = computed(() => isJsonTreeBranch(props.value))
const entries = computed(() => jsonTreeEntries(props.value))
const branchLabel = computed(() => jsonTreeBranchLabel(props.value))
const primitiveLabel = computed(() => jsonTreePrimitiveLabel(props.value))
const toggleLabel = computed(() =>
  `${expanded.value ? 'Collapse' : 'Expand'} ${props.label ?? 'manifest'}`
)
</script>

<template>
  <div class="font-mono text-xs leading-5">
    <div class="flex min-w-0 items-start gap-1">
      <button
        v-if="branch"
        type="button"
        class="mt-0.5 flex size-4 shrink-0 items-center justify-center rounded text-muted hover:bg-elevated hover:text-highlighted focus-visible:outline-2 focus-visible:outline-primary"
        :aria-label="toggleLabel"
        :aria-expanded="expanded"
        @click="expanded = !expanded"
      >
        <UIcon
          :name="expanded ? 'i-lucide-chevron-down' : 'i-lucide-chevron-right'"
          class="size-3"
        />
      </button>
      <span v-else class="size-4 shrink-0" aria-hidden="true" />
      <span v-if="label" class="shrink-0 text-primary">{{ label }}:</span>
      <span :class="branch ? 'text-muted' : 'break-all text-highlighted'">
        {{ branch ? branchLabel : primitiveLabel }}
      </span>
    </div>
    <div v-if="branch && expanded" class="ml-2 border-l border-default pl-2">
      <StudioJsonTree
        v-for="[key, child] in entries"
        :key="key"
        :label="key"
        :value="child"
        :depth="depth + 1"
      />
    </div>
  </div>
</template>
