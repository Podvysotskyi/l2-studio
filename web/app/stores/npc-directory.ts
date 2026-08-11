import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getNpcDirectory } from '../services/studio-api'
import type { NpcRecord } from '../types/models/content-directory'

export const useNpcDirectoryStore = defineStore('npc-directory', () => {
  const items = ref<NpcRecord[]>([])
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
      const response = await getNpcDirectory({
        query: query.value,
        page: page.value,
        pageSize: pageSize.value
      })
      if (version !== requestVersion) return
      items.value = response.items
      total.value = response.total
    } catch {
      if (version !== requestVersion) return
      error.value = 'The NPC directory could not be loaded from the Studio API.'
    } finally {
      if (version === requestVersion) loading.value = false
    }
  }

  return { items, total, query, page, pageSize, loading, error, load }
})
