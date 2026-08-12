<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useDirectoryRouteSync } from '~/composables/use-directory-route-sync'
import { useNpcDirectoryStore } from '~/stores/npc-directory'

const store = useNpcDirectoryStore()
const { items, total, query, page, pageSize, loading, error } =
  storeToRefs(store)

useDirectoryRouteSync('/authoring/npcs', { query, page, pageSize }, store.load)
</script>

<template>
  <NpcDirectory
    v-model:query="query"
    v-model:page="page"
    v-model:page-size="pageSize"
    :items="items"
    :total="total"
    :loading="loading"
    :error="error"
    @refresh="store.load"
  />
</template>
