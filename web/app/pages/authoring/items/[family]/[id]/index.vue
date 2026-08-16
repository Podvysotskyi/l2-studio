<script setup lang="ts">
import type { ItemDetailRecord } from '~/types/models/item'
import type { ItemFamily } from '~/types/requests/directory-request'

defineProps<{ item: ItemDetailRecord; family: ItemFamily }>()
</script>

<template><ItemOverview :item="item" :family="family" /></template>
