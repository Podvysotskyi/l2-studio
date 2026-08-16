import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getItemDirectory } from '../services/studio-api'
import type { ItemRecord } from '../types/models/item'
import type { ItemFamily } from '../types/requests/directory-request'

export const useItemDirectoryStore = defineStore('item-directory', () => {
  const items = ref<ItemRecord[]>([])
  const total = ref(0)
  const query = ref('')
  const page = ref(1)
  const pageSize = ref(25)
  const family = ref<ItemFamily>('etc')
  const itemTypeName = ref<string>()
  const itemActionName = ref<string>()
  const itemBodyPartName = ref<string>()
  const itemMaterialName = ref<string>()
  const itemCrystalTypeName = ref<string>()
  const handlerName = ref<string>()
  const loading = ref(true)
  const error = ref<string>()
  let requestVersion = 0
  async function load() {
    const version = ++requestVersion
    loading.value = true
    error.value = undefined
    try {
      const response = await getItemDirectory(family.value, {
        query: query.value,
        page: page.value,
        pageSize: pageSize.value,
        itemTypeName: itemTypeName.value,
        itemActionName: itemActionName.value,
        itemBodyPartName: itemBodyPartName.value,
        itemMaterialName: itemMaterialName.value,
        itemCrystalTypeName: itemCrystalTypeName.value,
        handlerName: handlerName.value
      })
      if (version !== requestVersion) return
      items.value = response.items
      total.value = response.total
    } catch {
      if (version === requestVersion) error.value = 'The item directory could not be loaded from the Studio API.'
    } finally {
      if (version === requestVersion) loading.value = false
    }
  }
  return {
    items,
    total,
    query,
    page,
    pageSize,
    family,
    itemTypeName,
    itemActionName,
    itemBodyPartName,
    itemMaterialName,
    itemCrystalTypeName,
    handlerName,
    loading,
    error,
    load
  }
})
