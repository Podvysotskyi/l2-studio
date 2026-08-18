<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useDirectoryRouteSync } from '~/composables/use-directory-route-sync'
import { useItemRecipeTypeDirectoryStore } from '~/stores/item-recipe-type-directory'

const store = useItemRecipeTypeDirectoryStore()
const { items, total, query, page, pageSize, loading, error } = storeToRefs(store)

useDirectoryRouteSync('/authoring/items/crafting/types', { query, page, pageSize }, store.load)
</script>

<template><ItemRecipeTypeDirectory v-model:query="query" v-model:page="page" v-model:page-size="pageSize" :items="items" :total="total" :loading="loading" :error="error" @refresh="store.load" /></template>
