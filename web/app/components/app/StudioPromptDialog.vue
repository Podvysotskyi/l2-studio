<script setup lang="ts">
const props = withDefaults(defineProps<{
  open?: boolean
  title: string
  description?: string
  label: string
  initialValue?: string
  confirmLabel?: string
  cancelLabel?: string
}>(), {
  open: false,
  description: undefined,
  initialValue: '',
  confirmLabel: 'Save',
  cancelLabel: 'Cancel'
})

const emit = defineEmits<{
  close: [value?: string]
  'update:open': [open: boolean]
}>()

const value = ref('')
const normalizedValue = computed(() => value.value.trim())
const settled = ref(false)

watch(
  () => [props.open, props.initialValue] as const,
  ([open, initialValue]) => {
    if (open) {
      value.value = initialValue
      settled.value = false
    }
  },
  { immediate: true }
)

function close(value?: string) {
  if (settled.value) return
  settled.value = true
  emit('close', value)
}

function updateOpen(open: boolean) {
  emit('update:open', open)
  if (!open) close()
}

function submit() {
  if (!normalizedValue.value) return
  close(normalizedValue.value)
}
</script>

<template>
  <UModal
    :open="open"
    :title="title"
    :description="description"
    @update:open="updateOpen"
  >
    <template #body>
      <form @submit.prevent="submit">
        <UFormField :label="label" required>
          <UInput
            v-model="value"
            class="w-full"
            autofocus
          />
        </UFormField>
      </form>
    </template>

    <template #footer>
      <div class="flex w-full justify-end gap-2">
        <UButton
          :label="cancelLabel"
          color="neutral"
          variant="outline"
          @click="close()"
        />
        <UButton
          :label="confirmLabel"
          :disabled="!normalizedValue"
          @click="submit"
        />
      </div>
    </template>
  </UModal>
</template>
