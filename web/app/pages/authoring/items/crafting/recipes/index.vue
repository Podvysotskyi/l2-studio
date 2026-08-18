<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useDirectoryRouteSync } from '~/composables/use-directory-route-sync'
import { useItemRecipeDirectoryStore } from '~/stores/item-recipe-directory'

const store = useItemRecipeDirectoryStore()
const { items, total, query, page, pageSize, loading, error } = storeToRefs(store)

useDirectoryRouteSync('/authoring/items/crafting/recipes', { query, page, pageSize }, store.load)
</script>

<template><ItemRecipeDirectory v-model:query="query" v-model:page="page" v-model:page-size="pageSize" :items="items" :total="total" :loading="loading" :error="error" @refresh="store.load" /></template>
