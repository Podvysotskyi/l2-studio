<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useDirectoryRouteSync } from '~/composables/use-directory-route-sync'
import { useItemSetDirectoryStore } from '~/stores/item-set-directory'

const store = useItemSetDirectoryStore()
const { items, total, query, page, pageSize, loading, error } = storeToRefs(store)

useDirectoryRouteSync('/authoring/items/sets', { query, page, pageSize }, store.load)
</script>

<template><ItemSetDirectory v-model:query="query" v-model:page="page" v-model:page-size="pageSize" :items="items" :total="total" :loading="loading" :error="error" @refresh="store.load" /></template>
