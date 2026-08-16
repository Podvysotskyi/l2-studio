<script setup lang="ts">
import type { ItemDetailRecord } from '~/types/models/item'

defineProps<{ item: ItemDetailRecord }>()
const emit = defineEmits<{ changed: [] }>()
</script>

<template>
  <ItemSkills :item="item" @changed="emit('changed')" />
</template>
