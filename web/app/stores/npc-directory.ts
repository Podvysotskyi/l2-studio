import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getNpcDirectory } from '../services/studio-api'
import type { NpcRecord, NpcVisualFilter } from '../types/models/content-directory'
import { npcRaceNoneValue } from '../utils/npc-directory'

export const useNpcDirectoryStore = defineStore('npc-directory', () => {
  const items = ref<NpcRecord[]>([])
  const total = ref(0)
  const query = ref('')
  const page = ref(1)
  const pageSize = ref(25)
  const npcTypeName = ref<string>()
  const npcRaceName = ref<string>()
  const npcSexName = ref<string>()
  const visualFilter = ref<NpcVisualFilter>()
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
        pageSize: pageSize.value,
        npcTypeName: npcTypeName.value,
        npcRaceName: npcRaceName.value && npcRaceName.value !== npcRaceNoneValue
          ? npcRaceName.value
          : undefined,
        withoutRace: npcRaceName.value === npcRaceNoneValue || undefined,
        npcSexName: npcSexName.value,
        hasVisuals: visualFilter.value === 'with'
          ? true
          : visualFilter.value === 'without'
            ? false
            : undefined
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

  return {
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
    error,
    load
  }
})
