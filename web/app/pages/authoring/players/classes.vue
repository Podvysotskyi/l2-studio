<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { usePlayerClassDirectoryStore } from '~/stores/player-class-directory'

const store = usePlayerClassDirectoryStore()
const { records, loading, error } = storeToRefs(store)

onMounted(() => void store.load())
</script>

<template>
  <PlayerClassDirectory
    :records="records"
    :loading="loading"
    :error="error"
    @refresh="store.load"
  />
</template>
