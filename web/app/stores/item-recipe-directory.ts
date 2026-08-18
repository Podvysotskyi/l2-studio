import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getItemRecipeDirectory } from '~/services/studio-api'
import type { ItemRecipeRecord } from '~/types/models/item-recipe'

export const useItemRecipeDirectoryStore = defineStore('item-recipe-directory', () => {
  const items = ref<ItemRecipeRecord[]>([])
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
      const result = await getItemRecipeDirectory({ query: query.value || undefined, page: page.value, pageSize: pageSize.value })
      if (version !== requestVersion) return
      items.value = result.items
      total.value = result.total
    } catch {
      if (version === requestVersion) error.value = 'The recipe directory could not be loaded.'
    } finally {
      if (version === requestVersion) loading.value = false
    }
  }

  return { items, total, query, page, pageSize, loading, error, load }
})
