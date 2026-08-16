<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useDirectoryRouteSync } from '~/composables/use-directory-route-sync'
import { useItemDirectoryStore } from '~/stores/item-directory'
import { isItemFamily } from '~/utils/item-family'
import { itemDirectoryRouteQuery, itemDirectoryRouteState } from '~/utils/item-directory'

const route = useRoute()
const familyValue = Array.isArray(route.params.family) ? route.params.family[0] : route.params.family
if (!familyValue || !isItemFamily(familyValue)) throw createError({ statusCode: 404, statusMessage: 'Item family not found' })
const family = familyValue
const store = useItemDirectoryStore()
const { items, total, query, page, pageSize, itemTypeName, itemActionName, itemBodyPartName, itemMaterialName, itemCrystalTypeName, handlerName, loading, error } = storeToRefs(store)
store.family = family

useDirectoryRouteSync(`/authoring/items/${family}`, { query, page, pageSize }, store.load, {
  filterRefs: [itemTypeName, itemActionName, itemBodyPartName, itemMaterialName, itemCrystalTypeName, handlerName],
  readFilters: routeQuery => {
    const filters = itemDirectoryRouteState(routeQuery)
    itemTypeName.value = filters.itemTypeName
    itemActionName.value = filters.itemActionName
    itemBodyPartName.value = filters.itemBodyPartName
    itemMaterialName.value = filters.itemMaterialName
    itemCrystalTypeName.value = filters.itemCrystalTypeName
    handlerName.value = filters.handlerName
  },
  filterQuery: () => itemDirectoryRouteQuery({ query: query.value, page: page.value, pageSize: pageSize.value, itemTypeName: itemTypeName.value, itemActionName: itemActionName.value, itemBodyPartName: itemBodyPartName.value, itemMaterialName: itemMaterialName.value, itemCrystalTypeName: itemCrystalTypeName.value, handlerName: handlerName.value })
})
</script>

<template>
  <ItemDirectory
    v-model:query="query" v-model:page="page" v-model:page-size="pageSize"
    v-model:item-type-name="itemTypeName" v-model:item-action-name="itemActionName"
    v-model:item-body-part-name="itemBodyPartName" v-model:item-material-name="itemMaterialName"
    v-model:item-crystal-type-name="itemCrystalTypeName" v-model:handler-name="handlerName"
    :items="items" :total="total" :loading="loading" :error="error" :family="family"
    @refresh="store.load"
  />
</template>
