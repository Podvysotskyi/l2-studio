<script setup lang="ts">
const props = withDefaults(defineProps<{
  viewTo?: string
  showEdit?: boolean
  showDelete?: boolean
  viewLabel?: string
  editTo?: string
  editLabel?: string
  deleteLabel?: string
  editLoading?: boolean
  deleteLoading?: boolean
  editDisabled?: boolean
  deleteDisabled?: boolean
}>(), {
  viewTo: undefined,
  editTo: undefined,
  showEdit: false,
  showDelete: false,
  viewLabel: 'View',
  editLabel: 'Edit',
  deleteLabel: 'Delete',
  editLoading: false,
  deleteLoading: false,
  editDisabled: false,
  deleteDisabled: false
})

const emit = defineEmits<{
  edit: []
  delete: []
}>()
</script>

<template>
  <div class="flex justify-end gap-1">
    <UButton
      v-if="viewTo"
      :label="viewLabel"
      icon="i-lucide-arrow-up-right"
      color="neutral"
      variant="ghost"
      size="sm"
      :to="viewTo"
    />
    <UButton
      v-if="showEdit"
      :label="editLabel"
      icon="i-lucide-pencil"
      color="neutral"
      variant="ghost"
      size="sm"
      :loading="editLoading"
      :disabled="editDisabled"
      :to="editTo"
      @click="emit('edit')"
    />
    <UButton
      v-if="showDelete"
      :label="deleteLabel"
      icon="i-lucide-trash-2"
      color="error"
      variant="ghost"
      size="sm"
      :loading="deleteLoading"
      :disabled="deleteDisabled"
      @click="emit('delete')"
    />
  </div>
</template>
