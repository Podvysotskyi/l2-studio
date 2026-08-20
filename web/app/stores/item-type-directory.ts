import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getItemLookups } from '../services/studio-api'
import type { ItemLookupRecord } from '../types/models/item'
import { loadDirectoryOptions } from '../utils/directory-pages'

export const useItemTypeDirectoryStore = defineStore('item-type-directory', () => {
  const records = ref<ItemLookupRecord[]>([])
  const loading = ref(true)
  const error = ref<string>()

  async function load() {
    loading.value = true
    error.value = undefined
    try {
      records.value = await loadDirectoryOptions((page, pageSize) =>
        getItemLookups('item-types', { page, pageSize })
      )
    } catch {
      error.value = 'The item type hierarchy could not be loaded from the Studio API.'
    } finally {
      loading.value = false
    }
  }
  function reset() { records.value = []; loading.value = false; error.value = undefined }

  return { records, loading, error, load, reset }
})
