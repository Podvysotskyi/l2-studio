import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getItemSetDirectory } from '~/services/studio-api'
import type { ItemSetRecord } from '~/types/models/item-set'

export const useItemSetDirectoryStore = defineStore('item-set-directory', () => {
  const items = ref<ItemSetRecord[]>([])
  const total = ref(0)
  const query = ref('')
  const page = ref(1)
  const pageSize = ref(25)
  const loading = ref(true)
  const error = ref<string>()
  let requestVersion = 0

  async function load() {
    const version = ++requestVersion
    loading.value = true
    error.value = undefined
    try {
      const result = await getItemSetDirectory({ query: query.value || undefined, page: page.value, pageSize: pageSize.value })
      if (version !== requestVersion) return
      items.value = result.items
      total.value = result.total
    } catch {
      if (version === requestVersion) error.value = 'The item-set directory could not be loaded.'
    } finally {
      if (version === requestVersion) loading.value = false
    }
  }
  function reset() { requestVersion++; items.value = []; total.value = 0; loading.value = false; error.value = undefined }

  return { items, total, query, page, pageSize, loading, error, load, reset }
})
