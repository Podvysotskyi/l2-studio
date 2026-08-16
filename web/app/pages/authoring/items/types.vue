<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useItemTypeDirectoryStore } from '~/stores/item-type-directory'

const store = useItemTypeDirectoryStore()
const { records, loading, error } = storeToRefs(store)

onMounted(() => void store.load())
</script>

<template>
  <ItemTypeDirectory
    :records="records"
    :loading="loading"
    :error="error"
    @refresh="store.load"
  />
</template>
