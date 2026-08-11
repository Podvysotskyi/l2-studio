import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getPlayerClasses } from '../services/studio-api'
import type { PlayerClassRecord } from '../types/models/content-directory'

export const usePlayerClassDirectoryStore = defineStore(
  'player-class-directory',
  () => {
    const records = ref<PlayerClassRecord[]>([])
    const loading = ref(true)
    const error = ref<string>()

    async function load() {
      loading.value = true
      error.value = undefined
      try {
        records.value = await getPlayerClasses()
      } catch {
        error.value =
          'The player class hierarchy could not be loaded from the Studio API.'
      } finally {
        loading.value = false
      }
    }

    return { records, loading, error, load }
  }
)
