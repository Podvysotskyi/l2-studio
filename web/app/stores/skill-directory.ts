import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getSkillDirectory } from '../services/studio-api'
import type { SkillRecord } from '../types/models/content-directory'

export const useSkillDirectoryStore = defineStore('skill-directory', () => {
  const items = ref<SkillRecord[]>([])
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
      const response = await getSkillDirectory({
        query: query.value,
        page: page.value,
        pageSize: pageSize.value
      })
      if (version !== requestVersion) return
      items.value = response.items
      total.value = response.total
    } catch {
      if (version !== requestVersion) return
      error.value =
        'The skill directory could not be loaded from the Studio API.'
    } finally {
      if (version === requestVersion) loading.value = false
    }
  }
  function reset() { requestVersion++; items.value = []; total.value = 0; loading.value = false; error.value = undefined }

  return { items, total, query, page, pageSize, loading, error, load, reset }
})
