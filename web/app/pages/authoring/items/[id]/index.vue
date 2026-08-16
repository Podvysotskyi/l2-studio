<script setup lang="ts">
import type { ItemDetailRecord } from '~/types/models/item'

defineProps<{ item: ItemDetailRecord }>()
</script>

<template>
  <ItemOverview :item="item" />
</template>
