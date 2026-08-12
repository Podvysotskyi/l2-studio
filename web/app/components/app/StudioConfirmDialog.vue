<script setup lang="ts">
type DialogColor = 'primary' | 'neutral' | 'error' | 'warning' | 'success' | 'info'
type DialogResult = boolean | 'all'

const props = withDefaults(defineProps<{
  open?: boolean
  title: string
  description?: string
  confirmLabel?: string
  cancelLabel?: string
  confirmColor?: DialogColor
  alternativeLabel?: string
}>(), {
  open: false,
  description: undefined,
  confirmLabel: 'Confirm',
  cancelLabel: 'Cancel',
  confirmColor: 'primary'
})

const emit = defineEmits<{
  close: [confirmed?: DialogResult]
  'update:open': [open: boolean]
}>()

const settled = ref(false)

watch(
  () => props.open,
  (open) => {
    if (open) settled.value = false
  },
  { immediate: true }
)

function close(confirmed: DialogResult = false) {
  if (settled.value) return
  settled.value = true
  emit('close', confirmed)
}

function updateOpen(open: boolean) {
  emit('update:open', open)
  if (!open) close()
}
</script>

<template>
  <UModal
    :open="open"
    :title="title"
    :description="description"
    @update:open="updateOpen"
  >
    <template #footer>
      <div class="flex w-full justify-end gap-2">
        <UButton
          :label="cancelLabel"
          color="neutral"
          variant="outline"
          @click="close()"
        />
        <UButton
          v-if="alternativeLabel"
          :label="alternativeLabel"
          :color="confirmColor"
          variant="outline"
          @click="close('all')"
        />
        <UButton
          :label="confirmLabel"
          :color="confirmColor"
          @click="close(true)"
        />
      </div>
    </template>
  </UModal>
</template>
