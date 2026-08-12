<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useDirectoryRouteSync } from '~/composables/use-directory-route-sync'
import { useSkillDirectoryStore } from '~/stores/skill-directory'

const store = useSkillDirectoryStore()
const { items, total, query, page, pageSize, loading, error } =
  storeToRefs(store)

useDirectoryRouteSync('/authoring/skills', { query, page, pageSize }, store.load)
</script>

<template>
  <SkillDirectory
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
