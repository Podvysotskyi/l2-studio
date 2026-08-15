<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useDirectoryRouteSync } from '~/composables/use-directory-route-sync'
import { useNpcDirectoryStore } from '~/stores/npc-directory'
import { npcDirectoryRouteQuery, npcDirectoryRouteState } from '~/utils/npc-directory'

const store = useNpcDirectoryStore()
const {
  items,
  total,
  query,
  page,
  pageSize,
  npcTypeName,
  npcRaceName,
  npcSexName,
  visualFilter,
  loading,
  error
} =
  storeToRefs(store)

useDirectoryRouteSync('/authoring/npcs', { query, page, pageSize }, store.load, {
  filterRefs: [npcTypeName, npcRaceName, npcSexName, visualFilter],
  readFilters: (routeQuery) => {
    const filters = npcDirectoryRouteState(routeQuery)
    npcTypeName.value = filters.npcTypeName
    npcRaceName.value = filters.npcRaceName
    npcSexName.value = filters.npcSexName
    visualFilter.value = filters.visualFilter
  },
  filterQuery: () => npcDirectoryRouteQuery({
    query: query.value,
    page: page.value,
    pageSize: pageSize.value,
    npcTypeName: npcTypeName.value,
    npcRaceName: npcRaceName.value,
    npcSexName: npcSexName.value,
    visualFilter: visualFilter.value
  })
})
</script>

<template>
  <NpcDirectory
    v-model:query="query"
    v-model:page="page"
    v-model:page-size="pageSize"
    v-model:npc-type-name="npcTypeName"
    v-model:npc-race-name="npcRaceName"
    v-model:npc-sex-name="npcSexName"
    v-model:visual-filter="visualFilter"
    :items="items"
    :total="total"
    :loading="loading"
    :error="error"
    @refresh="store.load"
  />
</template>
