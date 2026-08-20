import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getItemDirectory, resolveItemIcons } from '../services/studio-api'
import type { ItemIconReference, ItemPage, ItemRecord } from '../types/models/item'
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
  const iconUrls = ref<Record<number, string>>({})
  const loading = ref(true)
  const error = ref<string>()
  let requestVersion = 0
  async function load() {
    const version = ++requestVersion
    loading.value = true
    error.value = undefined
    let response: ItemPage
    try {
      response = await getItemDirectory(family.value, {
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
    } catch {
      if (version === requestVersion) error.value = 'The item directory could not be loaded from the Studio API.'
      if (version === requestVersion) loading.value = false
      return
    }
    if (version !== requestVersion) return
    items.value = response.items
    total.value = response.total
    iconUrls.value = {}
    loading.value = false

    const iconReferences: ItemIconReference[] = response.items
      .filter((item): item is ItemRecord & { icon: string } => item.icon !== null && item.icon.length > 0)
      .map(item => ({ itemId: item.id, icon: item.icon, itemBodyPartName: item.itemBodyPartName }))
    if (!iconReferences.length) return
    try {
      const resolvedIcons = await resolveItemIcons(iconReferences)
      if (version === requestVersion)
        iconUrls.value = Object.fromEntries(resolvedIcons.map(icon => [icon.itemId, icon.url]))
    } catch {
      // Icon artwork is supplemental; item definitions remain usable without it.
    }
  }
  function reset() { requestVersion++; items.value = []; total.value = 0; iconUrls.value = {}; loading.value = false; error.value = undefined }
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
    iconUrls,
    loading,
    error,
    load,
    reset
  }
})
