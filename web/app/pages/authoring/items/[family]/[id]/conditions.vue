<script setup lang="ts">
import type { ItemDetailRecord } from '~/types/models/item'
import type { ItemFamily } from '~/types/requests/directory-request'

defineProps<{ item: ItemDetailRecord; family: ItemFamily }>()
defineEmits<{ changed: [] }>()
</script>

<template><ItemConditions :item="item" :family="family" @changed="$emit('changed')" /></template>
